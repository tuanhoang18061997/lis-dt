// *********************************************************************************** Get sample
function Search_Refresh() {
    $.ajax({
        url: "/Search/Refresh/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            var date = new Date();
            var today = date.getFullYear() + "-" + ("0" + (date.getMonth() + 1)).slice(-2) + "-" + ("0" + date.getDate()).slice(-2);
            $("#listPatient").html(result);
            $("#search_timeSearchFrom").val(today);
            $("#search_timeSearchTo").val(today);
            Search_ResetInput();
        },
        error: function () {
            $("#listPatient").empty();
        }
    });
}

function Search_Search() {
    var search_pidorseq = $("#search_pidorseq").val();
    var timeSearchFrom = $("#search_timeSearchFrom").val();
    var timeSearchTo = $("#search_timeSearchTo").val();
    $.ajax({
        url: "/Search/Search?" + "pidorseq=" + search_pidorseq + "&timeSearchFrom=" + timeSearchFrom + "&timeSearchTo=" + timeSearchTo,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#listPatient").html(result);
        },
        error: function () {
            $("#listPatient").empty();
        }
    });
}

function Search_GetPatientInfo(id) {
    $.ajax({
        url: "/Search/GetPatientInfo?id=" + id,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#patientInfo").html(result);
            Search_GetListServiceForPatient(id);
        },
        error: function () {
            $("#patientInfo").empty();
            $('#tbody-gridview-service').empty();
        }
    });
}


function Search_GetListServiceForPatient(id) {
    $.ajax({
        url: "/Search/GetServiceForPatient?patientId=" + id,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#search-right-service-gridview").html(result);
        },
        error: function () {
            $('#tbody-gridview-service').empty();
        }
    });
}

function Search_ResetInput() {
    var date = new Date();
    var today = date.getFullYear() + "-" + ("0" + (date.getMonth() + 1)).slice(-2) + "-" + ("0" + date.getDate()).slice(-2);
    var time = ("0" + date.getHours()).slice(-2) + ":" + ("0" + date.getMinutes()).slice(-2);
    var dateTime = today + " " + time;
    $("#search_pidorseq").val('');
    $('#search_id').val('');
    $('#search_patientId').val('');
    $('#search_seq').val('');
    $('#search_sid').val('');
    $('#search_patientName').val('');
    $('#search_age').val('');
    $('#search_sex').val('');
    $('#search_obj').val('');
    $('#search_type').val('');
    $('#search_location').val('');
    $('#search_doctor').val('');
    $('#search_getSampleTime').val(dateTime);
    $('#search_address').val('');
    $('#search_diagnostic').val('');
    $('#select-category').val('');
    $('#select-service').val('');
    $('#tbody-gridview-service').empty();
}

function Search_Print(keyresultforhis) {
    if (keyresultforhis === "") {
        alert("Không thể in kết quả chỉ định này. Vui lòng kiểm tra lại !");
    }
    else {
        $.ajax({
            url: "/Search/Print?keyresultforhis=" + keyresultforhis,
            dataType: "text",
            type: "GET",
            success: function (response) {
                if (response === "") {
                    alert("Không thể in kết quả chỉ định này. Vui lòng kiểm tra lại !");
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
                alert("Không thể in kết quả chỉ định này. Vui lòng kiểm tra lại !");
            }
        });
    }
}
