let button = document.querySelector("#report-excel");
button.addEventListener("click", e => {
    let table = document.querySelector("#table-export-to-excel");
    TableToExcel.convert(table);
});


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
function Report_Search() {
    var validate = ValidateInput('report-option-search');
    if (validate) {
        var from = $("#report-time-from").val();
        var to = $("#report-time-to").val();
        var type = $("#report-type").val();
        var location = $("#report-location").val();
        var userid = $("#report-user").val();
        var hospitalId = $("#report-hospital").val();

        $.ajax({
            url: "/Report_BaoCaoThongKe/Search?" + "from=" + from + "&to=" + to + "&type=" + type + "&location=" + location + "&userid=" + userid + "&hospitalId=" + hospitalId,
            type: "GET",
            dataType: "html",
            cache: false,
            success: function (result) {
                $("#report-result").html(result);
            },
            error: function () {
                $("#report-result").empty();
            }
        });
    }
}

function Report_PDF() {
    var validate = ValidateInput('report-option-search');
    if (validate) {
        $('#showWaitting').modal('show');
        var from = $("#report-time-from").val();
        var to = $("#report-time-to").val();
        var type = $("#report-type").val();
        var location = $("#report-location").val();
        var userid = $("#report-user").val();
        var hospitalId = $("#report-hospital").val();

        $.ajax({
            url: "/Report_BaoCaoThongKe/Export_Pdf?" + "from=" + from + "&to=" + to + "&type=" + type + "&location=" + location + "&userid=" + userid + "&hospitalId=" + hospitalId,
            type: "GET",
            dataType: "text",
            success: function (response) {
                $('#showWaitting').modal('hide');
                if (response === "") {
                    alert("Valid & In không thành công. Vui lòng kiểm tra lại!");
                }
                else {
                    var byteCharacters = atob(response);
                    var byteNumbers = new Array(byteCharacters.length);
                    for (var i = 0; i < byteCharacters.length; i++) {
                        byteNumbers[i] = byteCharacters.charCodeAt(i);
                    }
                    var byteArray = new Uint8Array(byteNumbers);
                    var file = new Blob([byteArray], { type: 'application/pdf;base64' });
                    var fileURL = URL.createObjectURL(file);
                    window.open(fileURL);
                }
            },
            error: function () {
                $('#showWaitting').modal('hide');
                alert("Valid & In không thành công. Vui lòng kiểm tra lại!");
            }
        });
    }
}

