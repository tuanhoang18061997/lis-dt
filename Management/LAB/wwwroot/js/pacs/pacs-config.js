(() => {
  'use strict';
  const page = document.getElementById('pacsConfigPage');
  if (!page) return;
  const routes = { getAll: page.dataset.getall, save: page.dataset.save, test: page.dataset.test };
  const status = document.getElementById('statusLine');
  const tbody = document.getElementById('configRows');
  const count = document.getElementById('configCount');
  const form = document.getElementById('pacsCreateForm');
  const saveButton = document.getElementById('saveConfig');
  const refreshButton = document.getElementById('refreshList');
  let existing = [];
  function field(item, name) {
    const camel = name.charAt(0).toLowerCase() + name.substring(1);
    return item?.[camel] ?? item?.[name];
  }
  function show(message, tone = '') {
    status.className = `notice${tone ? ` ${tone}` : ''}`;
    status.textContent = message;
  }
  async function api(url, options = {}) {
    const res = await fetch(url, { credentials: 'same-origin', cache: 'no-store', ...options });
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    const data = await res.json();
    if (data && data.success === false) throw new Error(data.message || 'API trả về lỗi');
    return data;
  }
  function cell(tr, val) {
    const td = document.createElement('td');
    td.textContent = String(val ?? '');
    tr.appendChild(td);
    return td;
  }
  function render() {
    tbody.replaceChildren();
    count.textContent = `${existing.length} cấu hình`;
    if (existing.length === 0) {
      const tr = document.createElement('tr');
      const td = cell(tr, 'Chưa có PacsConfigs. Chỉ tạo khi đã phê duyệt dữ liệu và backup SQL.');
      td.colSpan = 7;
      td.className = 'muted';
      tbody.appendChild(tr);
      return;
    }
    for (const item of existing) {
      const tr = document.createElement('tr');
      cell(tr, field(item, 'Code'));
      cell(tr, field(item, 'Name'));
      cell(tr, field(item, 'BaseUrl'));
      cell(tr, field(item, 'IsDefault') ? 'Có' : 'Không');
      cell(tr, field(item, 'IsActive') ? 'Có' : 'Không');
      cell(tr, field(item, 'LastStatus') || 'Unknown');
      const actions = document.createElement('td');
      const btn = document.createElement('button');
      btn.type = 'button'; btn.className = 'btn secondary small'; btn.textContent = 'Test Connection';
      btn.addEventListener('click', () => testConnection(field(item, 'Id'), btn));
      actions.appendChild(btn); tr.appendChild(actions); tbody.appendChild(tr);
    }
    const duplicate = existing.some(x => String(field(x, 'Code')).toUpperCase() === 'BVNDT_ORTHANC');
    saveButton.disabled = duplicate;
    if (duplicate) show('BVNDT_ORTHANC đã tồn tại. Không tạo thêm bản ghi trùng. Có thể kiểm tra kết nối sau khi được phê duyệt.', 'success');
  }
  async function load() {
    refreshButton.disabled = true;
    try {
      const payload = await api(routes.getAll);
      existing = Array.isArray(payload.data) ? payload.data : [];
      render();
      if (!existing.some(x => String(field(x, 'Code')).toUpperCase() === 'BVNDT_ORTHANC')) show('Đã tải danh sách. Chưa thực hiện bất kỳ thay đổi dữ liệu nào.', 'success');
    } catch (e) {
      existing = []; tbody.replaceChildren();
      show(`Không lấy được danh sách PACS: ${e.message}. Không nên Save khi chưa xác định trạng thái DB.`, 'error');
      saveButton.disabled = true;
    } finally { refreshButton.disabled = false; }
  }
  async function testConnection(id, button) {
    if (!Number.isSafeInteger(Number(id)) || Number(id) <= 0) return;
    if (!window.confirm('Test Connection sẽ gọi Orthanc và GHI trạng thái kiểm tra vào PacsConfigs. Tiếp tục?')) return;
    button.disabled = true;
    show('Đang kiểm tra kết nối PACS...');
    try {
      const url = new URL(routes.test, window.location.href);
      url.searchParams.set('id', String(id));
      // TestConnection may return success=false for a partial connectivity result;
      // do not discard its useful status information.
      const res = await fetch(url.toString(), { credentials: 'same-origin', cache: 'no-store' });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data = await res.json();
      show(`TestConnection: ${data.status || 'Unknown'} | /system=${data.apiReady} | /studies=${data.studiesReady} | DICOMweb=${data.dicomWebReady} | DICOM TCP=${data.dicomPortReady}. ${data.message || ''}`, data.success ? 'success' : 'error');
      await load();
    } catch (e) { show(`Test Connection thất bại: ${e.message}`, 'error'); }
    finally { button.disabled = false; }
  }
  form.addEventListener('submit', async e => {
    e.preventDefault();
    if (saveButton.disabled) return;
    if (existing.length !== 0 && existing.some(x => String(field(x, 'Code')).toUpperCase() === 'BVNDT_ORTHANC')) {
      show('STOP: Cấu hình đã tồn tại. Không insert trùng.', 'error'); return;
    }
    const data = new FormData(form);
    if (!String(data.get('ApiPassword') || '').trim()) { show('Vui lòng nhập mật khẩu lis_sync.', 'error'); return; }
    if (!window.confirm('Đây là thao tác GHI PacsConfigs (Production nếu đang mở LIS Production). Xác nhận đã có backup DB và vẫn để SyncEnabled=OFF?')) return;
    const model = {
      Id: 0, Code: String(data.get('Code') || '').trim(), Name: String(data.get('Name') || '').trim(),
      ServerType: String(data.get('ServerType') || '').trim(), BaseUrl: String(data.get('BaseUrl') || '').trim(),
      ApiUsername: String(data.get('ApiUsername') || '').trim(), ApiPassword: String(data.get('ApiPassword') || ''),
      DicomAETitle: String(data.get('DicomAETitle') || '').trim(), DicomHost: String(data.get('DicomHost') || '').trim(),
      DicomPort: Number(data.get('DicomPort') || 4242), DicomWebRoot: String(data.get('DicomWebRoot') || '').trim(),
      OhifViewerUrl: String(data.get('OhifViewerUrl') || ''), SyncEnabled: false,
      SyncIntervalSeconds: Number(data.get('SyncIntervalSeconds') || 60),
      IsDefault: data.has('IsDefault'), IsActive: data.has('IsActive'), LastStatus: 'Unknown'
    };
    saveButton.disabled = true;
    try {
      const result = await api(routes.save, { method:'POST', headers: { 'Content-Type':'application/json' }, body: JSON.stringify(model) });
      form.elements.ApiPassword.value = '';
      show(result.message || 'Đã lưu PacsConfig.', 'success');
      await load();
    } catch (e) { show(`Không thể lưu: ${e.message}. Không thực hiện lại trước khi kiểm tra PacsConfigs.`, 'error'); }
    finally { if (!existing.some(x => String(field(x, 'Code')).toUpperCase() === 'BVNDT_ORTHANC')) saveButton.disabled = false; }
  });
  refreshButton.addEventListener('click', load);
  load();
})();
