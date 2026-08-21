function ValidateInput(id) {
    var flag = true;
    $("#" + id).find(".valid").each(function (i, e) {
        var value = $(e).val();
        if (value === "" || !value) {
            if (flag) {
                $(e).focus();
            }
            flag = false;
        }
    })
    return flag;
}

// ****************************************************************************** Data

function ResultStandard_CheckedBoxOnRow(id) {
    $(".row-resultstandard").each(function () {
        $(this).find(".form-check-input").each(function () {
            var value = $(this).val();
            if (value == id) {
                $(this).prop("checked", true);
            }
            else {
                $(this).prop("checked", false);
            }
        })

    })
}

function ResultStandard_Search() {
    var from = $("#resultstandard-from").val();
    var to = $("#resultstandard-to").val();
    var deviceid = $("#resultstandard-device").val();
    var seq = $("#resultstandard-seq").val();
    
    $.ajax({
        url: "/Data_ResultStandard/Search?" + "from=" + from + "&to=" + to + "&deviceid=" + deviceid + "&seq=" + seq,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#resultstandard-data-left-list").html(result);
        },
        error: function () {
            $("#resultstandard-data-left-list").empty();
        }
    });
}

function ResultStandard_UpdateStatus() {
    var from = $("#resultstandard-from").val();
    var to = $("#resultstandard-to").val();
    var seq = $("#resultstandard-seq").val();
    var status = $("#resultstandard-status").val();

    if (seq == "" || !seq || status == "" || !status) {
        alert("Vui lòng nhập Seq và Status cần cập nhật !");
    }
    else {
        $.ajax({
            url: "/Data_ResultStandard/UpdateStatus?from=" + from + "&to=" + to + "&seq=" + seq + "&status=" + status,
            dataType: "text",
            type: "POST",
            success: function (result) {
                if (result == 'True') {
                    ResultStandard_Search();
                }
                else {
                    alert("Lưu không thành công. Vui lòng kiểm tra lại!");
                }
            },
            error: function () {
                alert("Lưu không thành công. Vui lòng kiểm tra lại!");
            }
        });
    }
}

// ****************************************************************************** Push result HIS
function PushResultHIS_Search() {
    var from = $("#pushresulthis-from").val();
    var to = $("#pushresulthis-to").val();
    var patientid = $("#pushresulthis-patientid").val();

    $.ajax({
        url: "/Data_PushResultHIS/Search?" + "from=" + from + "&to=" + to + "&patientid=" + patientid,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#pushresulthis-data-left-list").html(result);
        },
        error: function () {
            $("#pushresulthis-data-left-list").empty();
        }
    });
}

function PushResultHIS_UpdatePush() {
    var from = $("#pushresulthis-from").val();
    var to = $("#pushresulthis-to").val();
    var patientid = $("#pushresulthis-patientid").val();
    var push = $("#pushresulthis-push").val();

    if (push == "" || !push) {
        alert("Vui lòng nhập push (0 hoặc 1) !");
    }
    else {
        $.ajax({
            url: "/Data_PushResultHIS/UpdatePush?from=" + from + "&to=" + to + "&patientid=" + patientid + "&push=" + push,
            dataType: "text",
            type: "POST",
            success: function (result) {
                if (result == 'True') {
                    PushResultHIS_Search();
                }
                else {
                    alert("Cập nhật Push không thành công. Vui lòng kiểm tra lại!");
                }
            },
            error: function () {
                alert("Cập nhật Push không thành công. Vui lòng kiểm tra lại!");
            }
        });
    }
}

function PushResultHIS_Push() {
    var from = $("#pushresulthis-from").val();
    var to = $("#pushresulthis-to").val();
    var patientid = $("#pushresulthis-patientid").val();

    $.ajax({
        url: "/Data_PushResultHIS/Push?from=" + from + "&to=" + to + "&patientid=" + patientid,
        dataType: "text",
        type: "POST",
        success: function (result) {
            if (result == 'True') {
                PushResultHIS_Search();
            }
            else {
                alert("Đẩy kết quả không thành công. Vui lòng kiểm tra lại!");
            }
        },
        error: function () {
            alert("Đẩy kết quả không thành công. Vui lòng kiểm tra lại!");
        }
    });
}