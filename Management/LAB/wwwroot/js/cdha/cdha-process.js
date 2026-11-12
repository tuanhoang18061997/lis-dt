(function (window, $) {

    'use strict';

    if (!$) {
        return;
    }


    var selectors = {
        shell:
            '.cdha-process-shell',

        container:
            '.cdha-process-container',

        patientItem:
            '.cdha-process-patient-item',

        patientToggle:
            '[data-cdha-action="toggle-patient-list"]',

        patientBackdrop:
            '[data-cdha-role="patient-backdrop"]'
    };


    function getContainer() {
        return $(
            selectors.container
        ).first();
    }


    function closePatientDrawer() {

        var $container =
            getContainer();

        if (!$container.length) {
            return;
        }

        $container.removeClass(
            'patient-drawer-open'
        );

        $('body').removeClass(
            'cdha-patient-drawer-open'
        );
    }


    function openPatientDrawer() {

        var $container =
            getContainer();

        if (!$container.length) {
            return;
        }

        $container.addClass(
            'patient-drawer-open'
        );

        $('body').addClass(
            'cdha-patient-drawer-open'
        );
    }


    function togglePatientDrawer() {

        var $container =
            getContainer();

        if (!$container.length) {
            return;
        }

        if (
            $container.hasClass(
                'patient-drawer-open'
            )
        ) {
            closePatientDrawer();
        }
        else {
            openPatientDrawer();
        }
    }


    function setActivePatient(
        $item
    ) {

        if (
            !$item ||
            !$item.length
        ) {
            return;
        }

        $(
            selectors.patientItem
        ).removeClass(
            'active'
        );

        $item.addClass(
            'active'
        );
    }


    function getCurrentPatientId() {

        var $shell =
            $(selectors.shell).first();

        if (!$shell.length) {
            return '';
        }

        var modulePrefix =
            String(
                $shell.attr(
                    'data-cdha-module'
                ) || ''
            )
                .trim();

        var screenPrefix =
            String(
                $shell.attr(
                    'data-cdha-screen'
                ) || 'process'
            )
                .trim();

        if (!modulePrefix) {
            return '';
        }

        var selector =
            '#' +
            modulePrefix +
            '_' +
            screenPrefix +
            '_id';

        return String(
            $(selector).val() || ''
        ).trim();
    }


    function syncActivePatient() {

        var patientId =
            getCurrentPatientId();

        if (!patientId) {
            return;
        }

        $(
            selectors.patientItem
        ).each(function () {

            var $item =
                $(this);

            var itemId =
                String(
                    $item.attr(
                        'data-patient-db-id'
                    ) || ''
                )
                    .trim();

            $item.toggleClass(
                'active',
                itemId === patientId
            );
        });
    }


    function bindEvents() {

        $(document)
            .off(
                'click.cdhaPatientToggle',
                selectors.patientToggle
            )
            .on(
                'click.cdhaPatientToggle',
                selectors.patientToggle,
                function (e) {

                    e.preventDefault();

                    togglePatientDrawer();
                }
            );


        $(document)
            .off(
                'click.cdhaPatientBackdrop',
                selectors.patientBackdrop
            )
            .on(
                'click.cdhaPatientBackdrop',
                selectors.patientBackdrop,
                function (e) {

                    e.preventDefault();

                    closePatientDrawer();
                }
            );


        $(document)
            .off(
                'click.cdhaPatientItem',
                selectors.patientItem
            )
            .on(
                'click.cdhaPatientItem',
                selectors.patientItem,
                function () {

                    setActivePatient(
                        $(this)
                    );

                    closePatientDrawer();
                }
            );


        $(document)
            .off(
                'keydown.cdhaPatientDrawer'
            )
            .on(
                'keydown.cdhaPatientDrawer',
                function (e) {

                    if (
                        e.key === 'Escape'
                    ) {
                        closePatientDrawer();
                    }
                }
            );
    }


    function observePatientList() {

        var host =
            document.getElementById(
                'process_listPatient'
            );

        if (!host) {
            return;
        }

        var observer =
            new MutationObserver(
                function () {

                    syncActivePatient();
                }
            );

        observer.observe(
            host,
            {
                childList:
                    true,

                subtree:
                    true
            }
        );
    }


    function init() {

        bindEvents();

        observePatientList();

        syncActivePatient();
    }


    $(init);


    window.CdhaProcessUi = {
        openPatientDrawer:
            openPatientDrawer,

        closePatientDrawer:
            closePatientDrawer,

        syncActivePatient:
            syncActivePatient
    };

})(
    window,
    window.jQuery
);