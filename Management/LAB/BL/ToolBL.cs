using Management.Models;
using SelectPdf;
using System.Drawing;
using System.Drawing.Imaging;
using static System.Net.Mime.MediaTypeNames;
using Image = System.Drawing.Image;

namespace Management.BL
{
    public class ToolBL
    {
        private readonly LABContext _db;
        private readonly SettingBL _settingBL;
        public ToolBL(LABContext db, SettingBL settingBL)
        {
            _db = db;
            _settingBL = settingBL;
        }

        public static DateTime Get_DateNow()
        {
            return DateTime.Now;
        }

        public async Task<string> ExportPdf_Result(string folder, string file, string headerHtml, string contentHtml, string footerHtml)
        {
            try
            {
                var baseUrl = await _settingBL.GetSetting(SettingBL.BaseUrl);
                if (string.IsNullOrEmpty(baseUrl)) return null;
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                HtmlToPdf converter = new HtmlToPdf();
                converter.Options.PdfPageSize = PdfPageSize.A4;
                converter.Options.MarginTop = 20;
                converter.Options.MarginLeft = 30;
                converter.Options.MarginRight = 20;

                var headerPdf = new PdfHtmlSection(headerHtml, baseUrl);
                headerPdf.AutoFitHeight = HtmlToPdfPageFitMode.AutoFit;
                converter.Header.Add(headerPdf);
                converter.Options.DisplayHeader = true;
                converter.Header.Height = 175;

                var footerPdf = new PdfHtmlSection(footerHtml, baseUrl);
                footerPdf.AutoFitHeight = HtmlToPdfPageFitMode.AutoFit;
                converter.Footer.Add(footerPdf);
                converter.Options.DisplayFooter = true;
                converter.Footer.Height = 135;

                //Đánh số trang
                var totalPage = converter.Footer.TotalPagesOffset;
                var pageNumber = converter.Footer.FirstPageNumber;
                var text = new PdfTextSection(pageNumber, totalPage, "Trang {page_number}/{total_pages}  ", new System.Drawing.Font("Arial", 9));
                text.HorizontalAlign = PdfTextHorizontalAlign.Center;
                text.VerticalAlign = PdfTextVerticalAlign.Bottom;
                text.Y = 110;
                converter.Footer.Add(text);

                PdfDocument doc = converter.ConvertHtmlString(contentHtml, baseUrl);
                doc.Save(file);
                doc.Close();
                return Convert.ToBase64String(File.ReadAllBytes(file));
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<string> ExportPdf_Result_No_Footer_Header(string folder, string file, string contentHtml)
        {
            try
            {
                var baseUrl = await _settingBL.GetSetting(SettingBL.BaseUrl);
                if (string.IsNullOrEmpty(baseUrl)) return null;
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                HtmlToPdf converter = new HtmlToPdf();
                converter.Options.PdfPageSize = PdfPageSize.A4;
                converter.Options.MarginTop = 20;
                converter.Options.MarginLeft = 30;
                converter.Options.MarginRight = 20;

                PdfDocument doc = converter.ConvertHtmlString(contentHtml, baseUrl);

                doc.Save(file);
                doc.Close();
                return Convert.ToBase64String(File.ReadAllBytes(file));
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<string> ExportPdf_Report(string folder, string file, string headerHtml, string contentHtml, string footerHtml)
        {
            try
            {
                var baseUrl = await _settingBL.GetSetting("BaseUrl");
                if (string.IsNullOrEmpty(baseUrl)) return null;
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                HtmlToPdf converter = new HtmlToPdf();
                converter.Options.PdfPageSize = PdfPageSize.A4;
                converter.Options.MarginTop = 20;
                converter.Options.MarginLeft = 30;
                converter.Options.MarginRight = 20;

                var headerPdf = new PdfHtmlSection(headerHtml, baseUrl);
                headerPdf.AutoFitHeight = HtmlToPdfPageFitMode.AutoFit;
                converter.Header.Add(headerPdf);
                converter.Options.DisplayHeader = true;
                converter.Header.Height = 110;

                var footerPdf = new PdfHtmlSection(footerHtml, baseUrl);
                footerPdf.AutoFitHeight = HtmlToPdfPageFitMode.AutoFit;
                converter.Footer.Add(footerPdf);
                converter.Options.DisplayFooter = true;
                converter.Footer.Height = 135;

                PdfDocument doc = converter.ConvertHtmlString(contentHtml, baseUrl);

                doc.Save(file);
                doc.Close();
                return Convert.ToBase64String(File.ReadAllBytes(file));
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<string> ExportPdf_Report_Landscape(string folder, string file, string headerHtml, string contentHtml, string footerHtml)
        {
            try
            {
                var baseUrl = await _settingBL.GetSetting(SettingBL.BaseUrl);
                if (string.IsNullOrEmpty(baseUrl)) return null;
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                HtmlToPdf converter = new HtmlToPdf();
                converter.Options.PdfPageSize = PdfPageSize.A4;
                converter.Options.MarginTop = 5;
                converter.Options.MarginLeft = 30;
                converter.Options.PdfPageOrientation = PdfPageOrientation.Landscape;

                var headerPdf = new PdfHtmlSection(headerHtml, baseUrl);
                headerPdf.AutoFitHeight = HtmlToPdfPageFitMode.AutoFit;
                converter.Header.Add(headerPdf);
                converter.Options.DisplayHeader = true;
                converter.Header.Height = 110;

                var footerPdf = new PdfHtmlSection(footerHtml, baseUrl);
                footerPdf.AutoFitHeight = HtmlToPdfPageFitMode.AutoFit;
                converter.Footer.Add(footerPdf);
                converter.Options.DisplayFooter = true;
                converter.Footer.Height = 123;

                // Đánh số trang => Nhưng không hiển thị được ở cuối trang
                //var totalPage = converter.Footer.TotalPagesOffset;
                //var pageNumber = converter.Footer.FirstPageNumber;
                //var text = new PdfTextSection(pageNumber, totalPage, "Trang {page_number}/{total_pages}  ", new System.Drawing.Font("Arial", 8));
                //text.HorizontalAlign = PdfTextHorizontalAlign.Center;
                //text.VerticalAlign = PdfTextVerticalAlign.Bottom;
                //converter.Footer.Add(text);

                PdfDocument doc = converter.ConvertHtmlString(contentHtml, baseUrl);

                doc.Save(file);
                doc.Close();
                return Convert.ToBase64String(File.ReadAllBytes(file));
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public Image Base64ToImage(string base64String)
        {
            base64String = base64String.Replace("data:image/png;base64,", "");
            byte[] imageBytes = Convert.FromBase64String(base64String);
            using (var ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
            {
                Image image = Image.FromStream(ms, true);
                return image;
            }
        }

        public bool SaveImage(string folder, string file, Image image)
        {
            try
            {
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                image.Save(file);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool DeleteImage(string file)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        string CleanHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return html ?? "";
            return html
                .Replace("\u00A0", " ")     // NBSP -> space
                .Replace("\u00AD", "");     // Soft Hyphen -> remove
        }
    }
}
