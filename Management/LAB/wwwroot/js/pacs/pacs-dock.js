/* =========================================================
   PACS DOCK - SHARED
   Dùng chung cho CT / XQ / MRI / MG
   và có thể tái sử dụng cho Process / ReturnResult.

   Phụ thuộc:
   - jQuery
   - SwalHelper (không bắt buộc)

   API mặc định:
   GET /PacsViewer/GetOhifViewerUrl?resultCDHAId=...

   HTML:
   - Views/Shared/_PacsDock.cshtml
   CSS:
   - wwwroot/css/pacs-dock.css
   ========================================================= */

(function (window, document, $) {
    'use strict';

    if (!$) {
        console.error('[PacsDock] jQuery chưa được load.');
        return;
    }

    var state = {
        currentUrl: '',
        currentResultCDHAId: '',
        loadingTimer: null,
        configured: false,
        thumbnailRequest: null,
        thumbnailResultCDHAId: '',

        // HOTFIX 03C-03:
        // Theo dõi request lấy Viewer URL để có thể hủy/invalidate
        // khi user đổi service/patient trước lúc request hoàn tất.
        viewerRequest: null,
        viewerRequestToken: 0,
        viewerRequestResultCDHAId: '',
        serviceSafetyTimer: null
    };

    var config = {
        module: '',
        screen: '',
        apiUrl: '/PacsViewer/GetOhifViewerUrl',
        seriesApiUrl: '/PacsViewer/GetPacsSeriesThumbnails',

        shellSelector: '#pacs_shell',
        mainSelector: '.pacs-main',
        dockSelector: '#pacs_dock',
        iframeSelector: '#pacs_iframe',
        loadingSelector: '#pacs_loading',
        subtitleSelector: '#pacs_subtitle',

        checkboxSelector: '',
        patientIdSelector: '',
        patientNameSelector: '',

        mediaContainerSelector: '.contain-hinh',
        mediaPanelSelector: '#pacs_media_panel',
        thumbnailListSelector: '#pacs_thumbnail_list',
        manualImageListSelector: '#pacs_manual_image_list',

        defaultSize: '75',
        loadingTimeoutMs: 15000,

        closeOnPatientChange: true
    };

    var selectors = {
        close: '#pacs_close',
        back: '#pacs_back_to_result',
        mobileBack: '#pacs_mobile_back',

        fullscreen: '#pacs_fullscreen',
        mobileFullscreen: '#pacs_mobile_fullscreen',

        openNewTab: '#pacs_open_new_tab',
        mobileNewTab: '#pacs_mobile_new_tab',

        sizeButton: '[data-pacs-size]'
    };


    /* =====================================================
       UI HELPERS
       ===================================================== */

    function toastWarning(message) {
        if (
            window.SwalHelper &&
            SwalHelper.Toast &&
            typeof SwalHelper.Toast.warning === 'function'
        ) {
            SwalHelper.Toast.warning(message);
            return;
        }

        window.alert(message);
    }

    function alertError(title, message) {
        if (
            window.SwalHelper &&
            SwalHelper.Alert &&
            typeof SwalHelper.Alert.error === 'function'
        ) {
            SwalHelper.Alert.error(
                title || 'Lỗi',
                message || 'Có lỗi xảy ra.'
            );
            return;
        }

        window.alert(
            (title ? title + '\n' : '') +
            (message || 'Có lỗi xảy ra.')
        );
    }

    function getShell() {
        return $(config.shellSelector).first();
    }

    function getDock() {
        return $(config.dockSelector).first();
    }

    function getIframe() {
        return $(config.iframeSelector).first();
    }

    function getLoading() {
        return $(config.loadingSelector).first();
    }

    function normalizeSize(size) {
        size = String(size || config.defaultSize || '75');

        if (
            size !== '60' &&
            size !== '75' &&
            size !== '90'
        ) {
            size = '75';
        }

        return size;
    }

    function isOverlayMode() {
        // Mobile luôn overlay.
        if (window.matchMedia('(max-width: 768px)').matches) {
            return true;
        }

        // Tablet dọc cũng overlay.
        if (
            window.matchMedia(
                '(min-width: 769px) and (max-width: 1199px) and (orientation: portrait)'
            ).matches
        ) {
            return true;
        }

        return false;
    }

    function syncBodyState() {
        var $dock = getDock();

        if (
            $dock.length &&
            $dock.hasClass('is-open') &&
            isOverlayMode()
        ) {
            $('body').addClass('pacs-mobile-open');
        } else {
            $('body').removeClass('pacs-mobile-open');
        }
    }

    function showLoading() {
        window.clearTimeout(state.loadingTimer);

        getLoading().addClass('show');

        state.loadingTimer = window.setTimeout(function () {
            hideLoading();
        }, config.loadingTimeoutMs);
    }

    function hideLoading() {
        window.clearTimeout(state.loadingTimer);
        state.loadingTimer = null;

        getLoading().removeClass('show');
    }

    function setSubtitle(text) {
        $(config.subtitleSelector)
            .first()
            .text(text || '');
    }


    /* =====================================================
       CONFIG
       ===================================================== */

    function readConfigFromDock() {
        var $dock = getDock();

        if (!$dock.length) {
            return;
        }

        var module = (
            $dock.attr('data-module') ||
            config.module ||
            ''
        ).toString().trim().toLowerCase();

        var screen = (
            $dock.attr('data-screen') ||
            config.screen ||
            ''
        ).toString().trim().toLowerCase();

        var defaultSize = (
            $dock.attr('data-default-size') ||
            config.defaultSize ||
            '75'
        ).toString();

        config.module = module;
        config.screen = screen;
        config.defaultSize = normalizeSize(defaultSize);

        buildDefaultContextSelectors();
    }

    function buildDefaultContextSelectors() {
        var module = config.module;
        var screen = config.screen;

        if (!module) {
            return;
        }

        if (!config.patientIdSelector && screen) {
            config.patientIdSelector =
                '#' + module + '_' + screen + '_patientId';
        }

        if (!config.patientNameSelector && screen) {
            config.patientNameSelector =
                '#' + module + '_' + screen + '_patientName';
        }

        /*
            ReturnResult hiện tại của các module có class:
            .ct-returnresult-chk-service
            .xq-returnresult-chk-service
            .mri-returnresult-chk-service
            .mg-returnresult-chk-service

            Process hiện tại dùng .row-service .form-check-input.
            Khi refactor Process sau này vẫn có thể truyền selector riêng
            bằng PacsDock.configure().
        */
        if (!config.checkboxSelector) {
            if (screen === 'returnresult') {
                config.checkboxSelector =
                    '.' + module + '-returnresult-chk-service';
            } else {
                config.checkboxSelector =
                    '.row-service .form-check-input';
            }
        }
    }

    function configure(options) {
        options = options || {};

        Object.keys(options).forEach(function (key) {
            if (
                Object.prototype.hasOwnProperty.call(config, key) &&
                options[key] !== undefined &&
                options[key] !== null
            ) {
                config[key] = options[key];
            }
        });

        config.module = String(config.module || '')
            .trim()
            .toLowerCase();

        config.screen = String(config.screen || '')
            .trim()
            .toLowerCase();

        config.defaultSize = normalizeSize(config.defaultSize);

        buildDefaultContextSelectors();

        state.configured = true;

        setSize(config.defaultSize);

        return window.PacsDock;
    }


    /* =====================================================
       CURRENT PATIENT / SERVICE
       ===================================================== */

    function getPatientId() {
        if (!config.patientIdSelector) {
            return '';
        }

        return $(
            config.patientIdSelector
        ).first().val() || '';
    }

    function getPatientName() {
        if (!config.patientNameSelector) {
            return '';
        }

        return $(
            config.patientNameSelector
        ).first().val() || '';
    }

    function getServiceCheckboxes(checkedOnly) {
        var selector = config.checkboxSelector;

        if (!selector) {
            selector = '.row-service .form-check-input';
        }

        var $items = $(selector);

        /*
            Nếu selector module-specific chưa match do partial cũ chưa
            có class chuẩn, fallback về cấu trúc đang được các JS
            ReturnResult sử dụng.
        */
        if (!$items.length) {
            $items = $('.row-service .form-check-input');
        }

        if (checkedOnly === true) {
            $items = $items.filter(':checked');
        }

        return $items;
    }

    function getSelectedResultCDHAIds() {
        var ids = [];
        var seen = {};

        getServiceCheckboxes(true).each(function () {
            var value = $(this).val();

            if (
                value !== undefined &&
                value !== null &&
                String(value).trim() !== ''
            ) {
                value = String(value).trim();

                if (!seen[value]) {
                    seen[value] = true;
                    ids.push(value);
                }
            }
        });

        return ids;
    }

    // =====================================================
    // HOTFIX 03C-03 - VIEWER CONTEXT SAFETY
    // =====================================================

    function isSingleSelectedResult(resultCDHAId) {
        var normalizedId =
            resultCDHAId !== undefined &&
                resultCDHAId !== null
                ? String(resultCDHAId).trim()
                : '';

        if (!normalizedId) {
            return false;
        }

        var ids = getSelectedResultCDHAIds();

        return (
            ids.length === 1 &&
            String(ids[0]) === normalizedId
        );
    }

    function cancelPendingViewerRequest() {
        var request = state.viewerRequest;

        /*
            Tăng token trước khi abort để callback cũ, kể cả khi đã
            được queue bởi browser, không còn quyền mở viewer.
        */
        state.viewerRequestToken += 1;
        state.viewerRequest = null;
        state.viewerRequestResultCDHAId = '';

        if (
            request &&
            request.readyState !== 4 &&
            typeof request.abort === 'function'
        ) {
            try {
                request.abort();
            } catch (e) {
                console.warn(
                    '[PacsDock] Không hủy được viewer request:',
                    e
                );
            }
        }
    }

    function scheduleServiceContextSafetyCheck() {
        window.clearTimeout(state.serviceSafetyTimer);

        /*
            Process hiện đổi checkbox bằng .prop('checked', ... )
            trong inline row onclick. Chờ hết call stack để đọc đúng
            selection cuối cùng, thay vì tranh lượt với handler cũ.
        */
        state.serviceSafetyTimer = window.setTimeout(function () {
            state.serviceSafetyTimer = null;

            var ids = getSelectedResultCDHAIds();
            var selectedId =
                ids.length === 1
                    ? String(ids[0])
                    : '';

            var currentMismatch =
                state.currentResultCDHAId &&
                state.currentResultCDHAId !== selectedId;

            var pendingMismatch =
                state.viewerRequestResultCDHAId &&
                state.viewerRequestResultCDHAId !== selectedId;

            if (currentMismatch || pendingMismatch) {
                close();
            }
        }, 0);
    }

    function getServiceNameByResultCDHAId(resultCDHAId) {
        if (!resultCDHAId) {
            return '';
        }

        var serviceName = '';
        var $target = $();

        getServiceCheckboxes(false).each(function () {
            if (
                String($(this).val()) ===
                String(resultCDHAId)
            ) {
                $target = $(this);
                return false;
            }
        });

        if (!$target.length) {
            return '';
        }

        serviceName =
            $target.attr('data-service-name') ||
            $target.data('service-name') ||
            $target.data('servicename') ||
            '';

        if (serviceName) {
            return $.trim(String(serviceName));
        }

        var $row = $target.closest('.row-service');

        if (!$row.length) {
            return '';
        }

        var module = config.module || '';

        var $serviceText = $row
            .find(
                '.serviceName, .service-name' +
                (module
                    ? ', .' + module + '-service-name'
                    : '')
            )
            .first();

        if ($serviceText.length) {
            serviceName = $.trim($serviceText.text());
        }

        /*
            Không dùng toàn bộ row text làm fallback.
            Row ReturnResult còn chứa icon/trạng thái/nút, ghép hết vào
            subtitle sẽ thành một câu đố hơn là thông tin bệnh nhân.
        */
        return serviceName || '';
    }

    function buildSubtitle(resultCDHAId) {
        var patientId = getPatientId();
        var patientName = getPatientName();
        var serviceName =
            getServiceNameByResultCDHAId(resultCDHAId);

        var parts = [];

        if (patientId) {
            parts.push(patientId);
        }

        if (patientName) {
            parts.push(patientName);
        }

        if (serviceName) {
            parts.push(serviceName);
        }

        return parts.join(' - ');
    }


    /* =====================================================
       THUMBNAIL / MEDIA PANEL
       ===================================================== */

    function escapeHtml(value) {
        return String(value == null ? '' : value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    function escapeAttr(value) {
        return escapeHtml(value).replace(/`/g, '&#096;');
    }

    function ensureMediaPanel() {
        var $container =
            $(config.mediaContainerSelector).first();

        if (!$container.length) {
            return null;
        }

        var $panel =
            $(config.mediaPanelSelector).first();

        if (!$panel.length) {
            $container.html(
                '<div id="pacs_media_panel" class="pacs-media-panel">' +
                '<section class="pacs-thumbnail-section">' +
                '<div class="pacs-media-section-header">' +
                '<i class="bi bi-images"></i>' +
                '<span>Ảnh PACS / DICOM</span>' +
                '</div>' +
                '<div id="pacs_thumbnail_list" class="pacs-thumbnail-list" aria-live="polite">' +
                '<div class="pacs-thumbnail-empty">Chọn dịch vụ để tải thumbnail PACS.</div>' +
                '</div>' +
                '</section>' +
                '<section class="pacs-manual-image-section">' +
                '<div class="pacs-media-section-header">' +
                '<i class="bi bi-card-image"></i>' +
                '<span>Ảnh tải lên / chụp thủ công</span>' +
                '</div>' +
                '<div id="pacs_manual_image_list" class="pacs-manual-image-list imageCDHA">' +
                '<div class="pacs-manual-image-empty">Chưa có ảnh tải lên/chụp thủ công.</div>' +
                '</div>' +
                '</section>' +
                '</div>'
            );

            $panel =
                $(config.mediaPanelSelector).first();
        }

        return $panel;
    }

    function getManualImageList() {
        ensureMediaPanel();

        return $(config.manualImageListSelector).first();
    }

    function clearThumbnails(message) {
        ensureMediaPanel();

        state.thumbnailResultCDHAId = '';

        if (
            state.thumbnailRequest &&
            typeof state.thumbnailRequest.abort === 'function'
        ) {
            try {
                state.thumbnailRequest.abort();
            } catch (e) {
            }
        }

        state.thumbnailRequest = null;

        $(config.thumbnailListSelector)
            .first()
            .html(
                '<div class="pacs-thumbnail-empty">' +
                escapeHtml(
                    message ||
                    'Chọn dịch vụ để tải thumbnail PACS.'
                ) +
                '</div>'
            );
    }

    function clearMediaPanel() {
        ensureMediaPanel();

        clearThumbnails(
            'Chọn 1 dịch vụ để tải thumbnail PACS.'
        );

        getManualImageList().html(
            '<div class="pacs-manual-image-empty">' +
            'Chưa có ảnh tải lên/chụp thủ công.' +
            '</div>'
        );
    }

    function loadThumbnails(resultCDHAId, options) {
        options = options || {};

        ensureMediaPanel();

        var $box =
            $(config.thumbnailListSelector).first();

        if (!$box.length) {
            return;
        }

        if (
            resultCDHAId === undefined ||
            resultCDHAId === null ||
            String(resultCDHAId).trim() === ''
        ) {
            clearThumbnails();
            return;
        }

        resultCDHAId =
            String(resultCDHAId).trim();

        if (
            state.thumbnailRequest &&
            typeof state.thumbnailRequest.abort === 'function'
        ) {
            try {
                state.thumbnailRequest.abort();
            } catch (e) {
            }
        }

        state.thumbnailResultCDHAId =
            resultCDHAId;

        $box.html(
            '<div class="pacs-thumbnail-loading">' +
            '<span class="spinner-border spinner-border-sm" role="presentation"></span>' +
            '<span>Đang tải thumbnail PACS...</span>' +
            '</div>'
        );

        state.thumbnailRequest = $.ajax({
            url: options.apiUrl ||
                config.seriesApiUrl,
            type: 'GET',
            dataType: 'json',
            cache: false,
            data: {
                resultCDHAId:
                    resultCDHAId
            },
            success: function (res) {
                if (
                    state.thumbnailResultCDHAId !==
                    resultCDHAId
                ) {
                    return;
                }

                if (
                    !res ||
                    res.success !== true ||
                    !Array.isArray(res.items) ||
                    res.items.length === 0
                ) {
                    $box.html(
                        '<div class="pacs-thumbnail-empty">' +
                        escapeHtml(
                            res && res.message
                                ? res.message
                                : 'Chưa có ảnh PACS/DICOM.'
                        ) +
                        '</div>'
                    );
                    return;
                }

                var cards = [];

                res.items.forEach(function (item) {
                    if (
                        !item ||
                        !item.thumbnailUrl
                    ) {
                        return;
                    }

                    var viewerUrl =
                        item.viewerUrl ||
                        res.viewerUrl ||
                        '';

                    var seriesDescription =
                        item.seriesDescription ||
                        'Không có mô tả';

                    var modality =
                        item.modality || '';

                    var instanceCount =
                        item.instanceCount || 0;

                    cards.push(
                        '<button type="button" ' +
                        'class="pacs-thumbnail-item" ' +
                        'data-result-cdha-id="' +
                        escapeAttr(resultCDHAId) +
                        '" ' +
                        'data-viewer-url="' +
                        escapeAttr(viewerUrl) +
                        '" ' +
                        'data-series-description="' +
                        escapeAttr(seriesDescription) +
                        '" ' +
                        'data-series-uid="' +
                        escapeAttr(
                            item.seriesInstanceUID || ''
                        ) +
                        '">' +
                        '<span class="pacs-thumbnail-image-wrap">' +
                        '<img class="pacs-thumbnail-image" ' +
                        'src="' +
                        escapeAttr(item.thumbnailUrl) +
                        '" ' +
                        'loading="lazy" ' +
                        'alt="' +
                        escapeAttr(seriesDescription) +
                        '" />' +
                        '</span>' +
                        '<span class="pacs-thumbnail-info">' +
                        '<span class="pacs-thumbnail-title">' +
                        escapeHtml(seriesDescription) +
                        '</span>' +
                        '<span class="pacs-thumbnail-meta">' +
                        escapeHtml(modality) +
                        (modality ? ' - ' : '') +
                        escapeHtml(instanceCount) +
                        ' ảnh' +
                        '</span>' +
                        '</span>' +
                        '</button>'
                    );
                });

                if (!cards.length) {
                    $box.html(
                        '<div class="pacs-thumbnail-empty">' +
                        'Có Series PACS nhưng chưa có thumbnail.' +
                        '</div>'
                    );
                    return;
                }

                $box.html(cards.join(''));
            },
            error: function (xhr, status) {
                if (status === 'abort') {
                    return;
                }

                if (
                    state.thumbnailResultCDHAId !==
                    resultCDHAId
                ) {
                    return;
                }

                $box.html(
                    '<div class="pacs-thumbnail-empty">' +
                    'Không tải được thumbnail PACS.' +
                    '</div>'
                );
            },
            complete: function () {
                state.thumbnailRequest = null;
            }
        });
    }

    /* =====================================================
       SIZE
       ===================================================== */

    function setSize(size) {
        size = normalizeSize(size);

        var $shell = getShell();

        if (!$shell.length) {
            return;
        }

        $shell
            .removeClass(
                'pacs-size-60 pacs-size-75 pacs-size-90'
            )
            .addClass('pacs-size-' + size);

        $(selectors.sizeButton)
            .removeClass('active');

        $(
            selectors.sizeButton +
            '[data-pacs-size="' + size + '"]'
        ).addClass('active');

        config.defaultSize = size;
    }


    /* =====================================================
       OPEN / CLOSE VIEWER
       ===================================================== */

    function ensureDockExists() {
        var $shell = getShell();
        var $dock = getDock();
        var $iframe = getIframe();

        if (
            !$shell.length ||
            !$dock.length ||
            !$iframe.length
        ) {
            alertError(
                'Lỗi PACS Dock',
                'Thiếu #pacs_shell, #pacs_dock hoặc #pacs_iframe. Kiểm tra lại ReturnResult.cshtml và _PacsDock.cshtml.'
            );

            return false;
        }

        return true;
    }

    function openViewer(
        viewerUrl,
        resultCDHAId,
        subtitle
    ) {
        if (!viewerUrl) {
            toastWarning('Không có link OHIF Viewer.');
            return;
        }

        if (!ensureDockExists()) {
            return;
        }

        var $shell = getShell();
        var $dock = getDock();
        var $iframe = getIframe();

        var normalizedResultId =
            resultCDHAId !== undefined &&
                resultCDHAId !== null
                ? String(resultCDHAId)
                : '';

        var sameStudy =
            state.currentUrl === viewerUrl &&
            state.currentResultCDHAId ===
            normalizedResultId &&
            $dock.hasClass('is-open');

        state.currentUrl = viewerUrl;
        state.currentResultCDHAId =
            normalizedResultId;

        setSubtitle(
            subtitle ||
            buildSubtitle(normalizedResultId)
        );

        setSize(config.defaultSize);

        $shell.addClass('has-pacs-open');

        $dock
            .addClass('is-open')
            .attr('aria-hidden', 'false');

        syncBodyState();

        if (sameStudy) {
            hideLoading();
            return;
        }

        showLoading();

        $iframe
            .off(
                'load.sharedPacsDock error.sharedPacsDock'
            )
            .on(
                'load.sharedPacsDock',
                function () {
                    hideLoading();
                }
            )
            .on(
                'error.sharedPacsDock',
                function () {
                    hideLoading();
                }
            );

        /*
            Chờ dock hiện trước rồi mới set src.
            OHIF cần kích thước container thực để tính viewport.
        */
        window.setTimeout(function () {
            $iframe.attr('src', viewerUrl);
        }, 30);
    }

    function openByResultCDHAId(
        resultCDHAId,
        options
    ) {
        options = options || {};

        if (
            resultCDHAId === undefined ||
            resultCDHAId === null ||
            String(resultCDHAId).trim() === ''
        ) {
            toastWarning(
                'Không xác định được dịch vụ cần xem PACS.'
            );
            return;
        }

        resultCDHAId =
            String(resultCDHAId).trim();

        /*
            Mỗi lần mở một ResultCDHA mới, invalidate request cũ.
            Với openSelected(), enforceSelection=true để response cũ
            không thể mở Study sau khi user đã chuyển sang service khác.
        */
        cancelPendingViewerRequest();

        var requestToken = state.viewerRequestToken;
        var enforceSelection =
            options.enforceSelection === true;

        state.viewerRequestResultCDHAId =
            resultCDHAId;

        state.viewerRequest = $.ajax({
            url: options.apiUrl || config.apiUrl,
            type: 'GET',
            dataType: 'json',
            cache: false,
            data: {
                resultCDHAId: resultCDHAId
            },
            success: function (res) {
                // Request đã bị invalidate bởi đổi service/patient/close.
                if (
                    requestToken !==
                    state.viewerRequestToken
                ) {
                    return;
                }

                // Selection hiện tại không còn thuộc request này.
                if (
                    enforceSelection &&
                    !isSingleSelectedResult(resultCDHAId)
                ) {
                    cancelPendingViewerRequest();
                    return;
                }

                state.viewerRequest = null;
                state.viewerRequestResultCDHAId = '';

                if (
                    !res ||
                    res.success !== true ||
                    !res.viewerUrl
                ) {
                    toastWarning(
                        res && res.message
                            ? res.message
                            : 'Dịch vụ này chưa có ảnh PACS hoặc chưa sync StudyInstanceUID.'
                    );
                    return;
                }

                openViewer(
                    res.viewerUrl,
                    resultCDHAId,
                    options.subtitle ||
                    buildSubtitle(resultCDHAId)
                );
            },
            error: function (xhr, textStatus) {
                // abort do safety guard là luồng bình thường, không báo lỗi.
                if (
                    textStatus === 'abort' ||
                    requestToken !==
                    state.viewerRequestToken
                ) {
                    return;
                }

                state.viewerRequest = null;
                state.viewerRequestResultCDHAId = '';

                var message =
                    'Không gọi được API lấy link OHIF Viewer.';

                if (
                    xhr &&
                    xhr.responseJSON &&
                    xhr.responseJSON.message
                ) {
                    message =
                        xhr.responseJSON.message;
                } else if (
                    xhr &&
                    (xhr.status === 401 ||
                        xhr.status === 403)
                ) {
                    message =
                        'Phiên đăng nhập không còn quyền truy cập PACS.';
                }

                alertError(
                    'Lỗi PACS',
                    message
                );
            }
        });
    }

    function openSelected() {
        var ids =
            getSelectedResultCDHAIds();

        if (!ids.length) {
            toastWarning(
                'Vui lòng chọn 1 dịch vụ cần xem ảnh PACS.'
            );
            return;
        }

        if (ids.length > 1) {
            toastWarning(
                'Vui lòng chỉ chọn 1 dịch vụ để xem ảnh PACS.'
            );
            return;
        }

        openByResultCDHAId(
            ids[0],
            {
                enforceSelection: true
            }
        );
    }

    function exitDockFullscreenIfNeeded() {
        var dockElement =
            getDock().get(0);

        if (!dockElement) {
            return;
        }

        var fullscreenElement =
            document.fullscreenElement ||
            document.webkitFullscreenElement;

        if (
            fullscreenElement !==
            dockElement
        ) {
            return;
        }

        try {
            if (document.exitFullscreen) {
                document.exitFullscreen();
            } else if (
                document.webkitExitFullscreen
            ) {
                document.webkitExitFullscreen();
            }
        } catch (e) {
            console.warn(
                '[PacsDock] Không thoát được fullscreen:',
                e
            );
        }
    }

    function close() {
        var $shell = getShell();
        var $dock = getDock();
        var $iframe = getIframe();

        // Đóng context PACS cũng phải vô hiệu mọi Viewer API đang pending.
        cancelPendingViewerRequest();

        exitDockFullscreenIfNeeded();

        $shell.removeClass(
            'has-pacs-open'
        );

        $dock
            .removeClass('is-open')
            .attr('aria-hidden', 'true');

        hideLoading();

        setSubtitle('');

        /*
            Quan trọng trên mobile/tablet:
            bỏ OHIF khỏi iframe để trình duyệt giải phóng RAM/GPU.
        */
        if ($iframe.length) {
            $iframe
                .off(
                    'load.sharedPacsDock error.sharedPacsDock'
                )
                .attr('src', 'about:blank');
        }

        state.currentUrl = '';
        state.currentResultCDHAId = '';

        $('body').removeClass(
            'pacs-mobile-open'
        );
    }


    /* =====================================================
       NEW TAB
       ===================================================== */

    function openNewTab() {
        if (!state.currentUrl) {
            toastWarning(
                'Chưa có ca PACS đang mở.'
            );
            return;
        }

        window.open(
            state.currentUrl,
            '_blank',
            'noopener'
        );
    }


    /* =====================================================
       FULLSCREEN
       ===================================================== */

    function toggleFullscreen() {
        var dockElement =
            getDock().get(0);

        if (!dockElement) {
            return;
        }

        var fullscreenElement =
            document.fullscreenElement ||
            document.webkitFullscreenElement;

        if (fullscreenElement) {
            try {
                if (document.exitFullscreen) {
                    document.exitFullscreen();
                } else if (
                    document.webkitExitFullscreen
                ) {
                    document.webkitExitFullscreen();
                }
            } catch (e) {
                console.warn(
                    '[PacsDock] exitFullscreen lỗi:',
                    e
                );
            }

            return;
        }

        try {
            if (
                dockElement.requestFullscreen
            ) {
                dockElement
                    .requestFullscreen()
                    .catch(function () {
                        toastWarning(
                            'Trình duyệt không cho phép mở toàn màn hình.'
                        );
                    });
            } else if (
                dockElement.webkitRequestFullscreen
            ) {
                dockElement
                    .webkitRequestFullscreen();
            } else {
                toastWarning(
                    'Thiết bị/trình duyệt này không hỗ trợ chế độ toàn màn hình.'
                );
            }
        } catch (e) {
            toastWarning(
                'Không thể mở PACS toàn màn hình.'
            );
        }
    }


    /* =====================================================
       EVENTS
       ===================================================== */

    function bindEvents() {
        $(document)
            .off(
                'click.sharedPacsDockClose',
                selectors.close
            )
            .on(
                'click.sharedPacsDockClose',
                selectors.close,
                function (e) {
                    e.preventDefault();
                    close();
                }
            );

        $(document)
            .off(
                'click.sharedPacsDockBack',
                selectors.back + ', ' +
                selectors.mobileBack
            )
            .on(
                'click.sharedPacsDockBack',
                selectors.back + ', ' +
                selectors.mobileBack,
                function (e) {
                    e.preventDefault();
                    close();
                }
            );

        $(document)
            .off(
                'click.sharedPacsDockNewTab',
                selectors.openNewTab + ', ' +
                selectors.mobileNewTab
            )
            .on(
                'click.sharedPacsDockNewTab',
                selectors.openNewTab + ', ' +
                selectors.mobileNewTab,
                function (e) {
                    e.preventDefault();
                    openNewTab();
                }
            );

        $(document)
            .off(
                'click.sharedPacsDockFullscreen',
                selectors.fullscreen + ', ' +
                selectors.mobileFullscreen
            )
            .on(
                'click.sharedPacsDockFullscreen',
                selectors.fullscreen + ', ' +
                selectors.mobileFullscreen,
                function (e) {
                    e.preventDefault();
                    toggleFullscreen();
                }
            );

        $(document)
            .off(
                'click.sharedPacsDockSize',
                selectors.sizeButton
            )
            .on(
                'click.sharedPacsDockSize',
                selectors.sizeButton,
                function (e) {
                    e.preventDefault();

                    setSize(
                        $(this).attr(
                            'data-pacs-size'
                        )
                    );
                }
            );

        $(document)
            .off(
                'click.sharedPacsThumbnail',
                '.pacs-thumbnail-item'
            )
            .on(
                'click.sharedPacsThumbnail',
                '.pacs-thumbnail-item',
                function (e) {
                    e.preventDefault();
                    e.stopPropagation();

                    var $item = $(this);

                    var viewerUrl =
                        $item.attr(
                            'data-viewer-url'
                        ) || '';

                    var resultCDHAId =
                        $item.attr(
                            'data-result-cdha-id'
                        ) || '';

                    var seriesDescription =
                        $item.attr(
                            'data-series-description'
                        ) || '';

                    if (!viewerUrl) {
                        toastWarning(
                            'Không có link OHIF Viewer.'
                        );
                        return;
                    }

                    $('.pacs-thumbnail-item')
                        .removeClass('active');

                    $item.addClass('active');

                    var subtitle =
                        buildSubtitle(
                            resultCDHAId
                        );

                    if (seriesDescription) {
                        subtitle +=
                            (subtitle ? ' - ' : '') +
                            seriesDescription;
                    }

                    openViewer(
                        viewerUrl,
                        resultCDHAId,
                        subtitle
                    );
                }
            );

        /*
            An toàn service:
            - Process có inline row onclick rồi mới bubble tới document.
            - scheduleServiceContextSafetyCheck() chờ 0ms để đọc
              selection sau cùng.
            - Nếu service đang chọn khác Study đang mở hoặc request
              đang pending, đóng PACS và invalidate request cũ.
        */
        var serviceSafetySelector =
            '.row-service';

        if (config.checkboxSelector) {
            serviceSafetySelector +=
                ', ' + config.checkboxSelector;
        } else {
            serviceSafetySelector +=
                ', .row-service .form-check-input';
        }

        $(document)
            .off(
                'click.sharedPacsServiceContext ' +
                'change.sharedPacsServiceContext',
                serviceSafetySelector
            )
            .on(
                'click.sharedPacsServiceContext ' +
                'change.sharedPacsServiceContext',
                serviceSafetySelector,
                function () {
                    scheduleServiceContextSafetyCheck();
                }
            );

        /*
            An toàn bệnh nhân:
            đóng viewer và cả request đang pending ngay khi đổi patient.
            Guard này phải chạy kể cả dock chưa kịp mở, nếu không response
            cũ vẫn có thể bật Study của patient trước đó.
        */
        $(document)
            .off(
                'click.sharedPacsPatientChange',
                '.list-group-item-action'
            )
            .on(
                'click.sharedPacsPatientChange',
                '.list-group-item-action',
                function () {
                    if (
                        config.closeOnPatientChange &&
                        (
                            isOpen() ||
                            state.viewerRequestResultCDHAId
                        )
                    ) {
                        close();
                    }
                }
            );

        $(window)
            .off(
                'resize.sharedPacsDock orientationchange.sharedPacsDock'
            )
            .on(
                'resize.sharedPacsDock orientationchange.sharedPacsDock',
                function () {
                    syncBodyState();
                }
            );

        $(window)
            .off(
                'pagehide.sharedPacsDock'
            )
            .on(
                'pagehide.sharedPacsDock',
                function () {
                    /*
                        Không gọi close() ở đây để tránh tác động DOM
                        lúc browser đang unload. Invalidate request và
                        clear iframe là đủ.
                    */
                    cancelPendingViewerRequest();

                    var $iframe =
                        getIframe();

                    if ($iframe.length) {
                        $iframe.attr(
                            'src',
                            'about:blank'
                        );
                    }
                }
            );
    }


    /* =====================================================
       STATE / PUBLIC HELPERS
       ===================================================== */

    function isOpen() {
        return getDock()
            .hasClass('is-open');
    }

    function getCurrentUrl() {
        return state.currentUrl;
    }

    function getCurrentResultCDHAId() {
        return state.currentResultCDHAId;
    }

    function init() {
        readConfigFromDock();
        bindEvents();
        setSize(config.defaultSize);

        state.configured = true;
    }


    /* =====================================================
       PUBLIC API
       ===================================================== */

    window.PacsDock = {
        init: init,
        configure: configure,

        open: openViewer,
        openSelected: openSelected,
        openByResultCDHAId:
            openByResultCDHAId,

        close: close,
        setSize: setSize,
        toggleFullscreen:
            toggleFullscreen,
        openNewTab: openNewTab,

        getSelectedResultCDHAIds:
            getSelectedResultCDHAIds,
        getCurrentUrl:
            getCurrentUrl,
        getCurrentResultCDHAId:
            getCurrentResultCDHAId,

        ensureMediaPanel:
            ensureMediaPanel,
        getManualImageList:
            getManualImageList,
        loadThumbnails:
            loadThumbnails,
        clearThumbnails:
            clearThumbnails,
        clearMediaPanel:
            clearMediaPanel,

        isOpen: isOpen
    };


    /* =====================================================
       AUTO INIT
       ===================================================== */

    $(function () {
        /*
            Chỉ init khi partial PACS tồn tại trên trang.
            Vì file JS này sau này có thể load chung ở layout.
        */
        if ($(config.dockSelector).length) {
            init();
        }
    });

})(window, document, window.jQuery);
