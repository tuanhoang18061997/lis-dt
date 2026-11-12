using Microsoft.Extensions.Configuration;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Management.BL.Pacs
{
    /// <summary>
    /// Scoped SMB session for IIS -> Ubuntu PACS MWL. Does not change AppPool identity,
    /// create drive mappings, or write outside the already-configured worklist share.
    /// Machine-scoped DPAPI is decoded through the OS crypt32 API, avoiding a runtime
    /// dependency on System.Security.Cryptography.ProtectedData for this operation.
    /// </summary>
    internal sealed class PacsSmbConnectionScope : IAsyncDisposable
    {
        private const int ResourceTypeDisk = 1;
        private const int ErrorCredentialConflict = 1219;
        private const int CryptprotectUiForbidden = 0x1;
        private const int MaxSecretBytes = 16384;
        private static readonly SemaphoreSlim ConnectionGate = new(1, 1);

        private readonly string? _remoteShare;
        private readonly bool _ownsGate;
        private bool _disposed;

        private PacsSmbConnectionScope(string? remoteShare, bool ownsGate)
        {
            _remoteShare = remoteShare;
            _ownsGate = ownsGate;
        }

        public static async Task<PacsSmbConnectionScope> OpenAsync(
            IConfiguration configuration,
            CancellationToken cancellationToken = default)
        {
            var published = (configuration["Pacs:WorklistFolder"] ?? "").Trim();
            var enabled = bool.TryParse(
                configuration["Pacs:WorklistSmbEnabled"], out var isEnabled)
                && isEnabled;
            var remoteFolder = published.StartsWith(@"\\", StringComparison.Ordinal);

            if (!enabled)
            {
                if (remoteFolder)
                    throw new InvalidOperationException(
                        "PACS_SMB_CONFIG_DISABLED_UNC: WorklistFolder UNC requires WorklistSmbEnabled.");

                return new PacsSmbConnectionScope(null, false);
            }

            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("PACS_SMB_WINDOWS_ONLY");

            var share = (configuration["Pacs:WorklistSmbShare"] ?? "").Trim().TrimEnd('\\');
            var username = (configuration["Pacs:WorklistSmbUsername"] ?? "").Trim();
            var secretFile = (configuration["Pacs:WorklistSmbSecretFile"] ?? "").Trim();

            if (!share.StartsWith(@"\\", StringComparison.Ordinal)
                || share.Split('\\', StringSplitOptions.RemoveEmptyEntries).Length != 2
                || !published.Equals(share, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("PACS_SMB_CONFIG_SHARE_MISMATCH");

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(secretFile))
                throw new InvalidOperationException("PACS_SMB_CONFIG_CREDENTIAL_MISSING");

            if (!Path.IsPathFullyQualified(secretFile)
                || secretFile.StartsWith(@"\\", StringComparison.Ordinal))
                throw new InvalidOperationException("PACS_SMB_SECRET_PATH_NOT_LOCAL");

            var secretFullPath = Path.GetFullPath(secretFile);
            var siteRoot = Path.GetFullPath(AppContext.BaseDirectory)
                .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (secretFullPath.StartsWith(siteRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("PACS_SMB_SECRET_INSIDE_WEBROOT");

            if (!File.Exists(secretFullPath))
                throw new InvalidOperationException("PACS_SMB_SECRET_NOT_FOUND_OR_UNREADABLE");

            await ConnectionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                byte[] protectedBytes = Array.Empty<byte>();
                byte[] plainBytes = Array.Empty<byte>();
                try
                {
                    try
                    {
                        protectedBytes = await File.ReadAllBytesAsync(secretFullPath, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        throw StageError("SECRET_READ", ex);
                    }

                    if (protectedBytes.Length == 0 || protectedBytes.Length > MaxSecretBytes)
                        throw new InvalidOperationException("PACS_SMB_SECRET_INVALID_LENGTH");

                    // Win32 DPAPI uses the SAME LocalMachine machine keys and blob format
                    // as ProtectedData.Unprotect(..., DataProtectionScope.LocalMachine).
                    // This does not migrate or re-encrypt the existing secret.
                    plainBytes = UnprotectViaWin32(protectedBytes);
                    string password;
                    try
                    {
                        password = new UTF8Encoding(false, true).GetString(plainBytes);
                    }
                    catch (DecoderFallbackException ex)
                    {
                        throw StageError("SECRET_UTF8", ex);
                    }
                    if (string.IsNullOrEmpty(password))
                        throw new InvalidOperationException("PACS_SMB_SECRET_EMPTY");

                    var remote = new NetResource
                    {
                        Type = ResourceTypeDisk,
                        RemoteName = share,
                        LocalName = null
                    };
                    int rc;
                    try
                    {
                        rc = WNetAddConnection2(ref remote, password, username, 0);
                    }
                    catch (Exception ex)
                    {
                        throw StageError("CONNECT_API", ex);
                    }
                    finally
                    {
                        // WNet* requires a string. Avoid retaining it beyond the call.
                        password = string.Empty;
                    }

                    if (rc == ErrorCredentialConflict)
                        throw new IOException("PACS_SMB_CONNECT_WIN32_1219: credentials conflict; do not disconnect other sessions.");
                    if (rc != 0)
                        throw new IOException($"PACS_SMB_CONNECT_WIN32_{rc}");

                    return new PacsSmbConnectionScope(share, true);
                }
                finally
                {
                    if (protectedBytes.Length != 0)
                        CryptographicOperations.ZeroMemory(protectedBytes);
                    if (plainBytes.Length != 0)
                        CryptographicOperations.ZeroMemory(plainBytes);
                }
            }
            catch
            {
                ConnectionGate.Release();
                throw;
            }
        }

        private static byte[] UnprotectViaWin32(byte[] encrypted)
        {
            GCHandle inputHandle = default;
            DataBlob output = default;
            try
            {
                inputHandle = GCHandle.Alloc(encrypted, GCHandleType.Pinned);
                var input = new DataBlob
                {
                    Length = encrypted.Length,
                    Data = inputHandle.AddrOfPinnedObject()
                };

                if (!CryptUnprotectData(
                    ref input, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                    CryptprotectUiForbidden, out output))
                {
                    int lastError = Marshal.GetLastWin32Error();
                    throw new IOException($"PACS_SMB_DPAPI_WIN32_{lastError}");
                }

                if (output.Data == IntPtr.Zero || output.Length <= 0 || output.Length > MaxSecretBytes)
                    throw new InvalidOperationException("PACS_SMB_DPAPI_INVALID_OUTPUT_LENGTH");

                var result = new byte[output.Length];
                Marshal.Copy(output.Data, result, 0, output.Length);
                return result;
            }
            catch (IOException) { throw; }
            catch (InvalidOperationException) { throw; }
            catch (Exception ex)
            {
                throw StageError("DPAPI_NATIVE", ex);
            }
            finally
            {
                if (output.Data != IntPtr.Zero)
                {
                    // CryptUnprotectData allocates with LocalAlloc. Clear memory BEFORE LocalFree.
                    if (output.Length > 0 && output.Length <= MaxSecretBytes)
                        Marshal.Copy(new byte[output.Length], 0, output.Data, output.Length);
                    LocalFree(output.Data);
                }
                if (inputHandle.IsAllocated)
                    inputHandle.Free();
            }
        }

        private static IOException StageError(string stage, Exception exception)
        {
            // Deliberately excludes file paths, passwords, UNC credentials, and inner exceptions.
            return new IOException(
                $"PACS_SMB_{stage}_FAILED type={exception.GetType().Name} " +
                $"hresult=0x{unchecked((uint)exception.HResult):X8}");
        }

        public ValueTask DisposeAsync()
        {
            if (_disposed) return ValueTask.CompletedTask;
            _disposed = true;
            if (!_ownsGate) return ValueTask.CompletedTask;

            try
            {
                int rc = WNetCancelConnection2(_remoteShare!, 0, false);
                if (rc != 0)
                    throw new IOException($"PACS_SMB_DISCONNECT_WIN32_{rc}");
            }
            finally
            {
                ConnectionGate.Release();
            }
            return ValueTask.CompletedTask;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DataBlob
        {
            public int Length;
            public IntPtr Data;
        }

        [DllImport("crypt32.dll", EntryPoint = "CryptUnprotectData", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptUnprotectData(
            ref DataBlob encrypted, IntPtr description, IntPtr entropy,
            IntPtr reserved, IntPtr prompt, int flags, out DataBlob decrypted);

        [DllImport("kernel32.dll", EntryPoint = "LocalFree", SetLastError = false)]
        private static extern IntPtr LocalFree(IntPtr memory);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NetResource
        {
            public int Scope;
            public int Type;
            public int DisplayType;
            public int Usage;
            [MarshalAs(UnmanagedType.LPWStr)] public string? LocalName;
            [MarshalAs(UnmanagedType.LPWStr)] public string? RemoteName;
            [MarshalAs(UnmanagedType.LPWStr)] public string? Comment;
            [MarshalAs(UnmanagedType.LPWStr)] public string? Provider;
        }

        [DllImport("mpr.dll", EntryPoint = "WNetAddConnection2W", CharSet = CharSet.Unicode)]
        private static extern int WNetAddConnection2(
            ref NetResource netResource, string password, string username, int flags);

        [DllImport("mpr.dll", EntryPoint = "WNetCancelConnection2W", CharSet = CharSet.Unicode)]
        private static extern int WNetCancelConnection2(
            string name, int flags, [MarshalAs(UnmanagedType.Bool)] bool force);
    }
}
