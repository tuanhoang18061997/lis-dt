// Tool tip cho control của form
//$(function () {
//    $('[data-toggle="tooltip"]').tooltip()
//})


// *********************************************************************************** Select 2
$(function () {
    GetSample_SetSelect2_01(true);
})

function GetSample_SetSelect2_02() {
    $(document).ready(function () {
        $('#sa_getsample_service').select2({
            dropdownParent: $('#addServiceForm'),
            placeholder: "-- Chọn dịch vụ --"
        });
    });
    $('#sa_getsample_service').val('');
}

function GetSample_SetSelect2_01(isLoadPage) {
    if (isLoadPage) {
        $('#sa_getsample_location').val("");
    }
    $(document).ready(function () {
        $('#sa_getsample_location').select2({
            placeholder: "-- Chọn --"
        });
    });

    if (isLoadPage) {
        $('#sa_getsample_doctor').val("");
    }
    $(document).ready(function () {
        $('#sa_getsample_doctor').select2({
            placeholder: "-- Chọn --"
        });
    });


    if (isLoadPage) {
        $('#sa_process_sampleresult').val("");
    }
    $(document).ready(function () {
        $('#sa_process_sampleresult').select2({
            placeholder: "-- Chọn --"
        });
    });
}

function Process_SetSelect2_03() {
    $(document).ready(function () {
        $('#sa_process_userReturnResultSA').select2({
            placeholder: "-- Chọn --"
        });
    });
}




// *********************************************************************************** Get sample

// Kiểm tra nhập liệu trên form => Trường nào có class = valid thì sẽ kiểm tra rỗng và bắt nhập
function GetSample_ValidateInput(id) {
    var flag = true;
    $("#" + id).find(".valid").each(function (i, e) {
        var value = $(e).val();
        if (value === "" || !value) {
            if (flag) {
                if (e.id === "sa_getsample_location") {
                    $('#sa_getsample_location').select2('focus');
                }
                else if (e.id === "sa_getsample_doctor") {
                    $('#sa_getsample_doctor').select2('focus');
                }
                else if (e.id === "sa_getsample_userReturnResultSA") {
                    $('#sa_getsample_userReturnResultSA').select2('focus');
                }
                else {
                    $(e).focus();
                }
            }
            flag = false;
        }
    })
    return flag;
}

function GetSample_Refresh() {
    $.ajax({
        url: "/SA_GetSample/Refresh/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            var date = new Date();
            var today = date.getFullYear() + "-" + ("0" + (date.getMonth() + 1)).slice(-2) + "-" + ("0" + date.getDate()).slice(-2);
            $("#listPatient").html(result);
            $("#sa_getsample_timeSearchFrom").val(today);
            $("#sa_getsample_timeSearchTo").val(today);
            GetSample_ResetInput();
            GetSample_Get_Count();
        },
        error: function () {
            $("#listPatient").empty();
        }
    });
}

function GetSample_Refresh_No_Clear_PatientInfo() {
    $.ajax({
        url: "/SA_GetSample/Refresh/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            var date = new Date();
            var today = date.getFullYear() + "-" + ("0" + (date.getMonth() + 1)).slice(-2) + "-" + ("0" + date.getDate()).slice(-2);
            $("#listPatient").html(result);
            $("#sa_getsample_timeSearchFrom").val(today);
            $("#sa_getsample_timeSearchTo").val(today);
        },
        error: function () {
            $("#listPatient").empty();
        }
    });
}

function GetSample_Search() {
    var sa_getsample_pidorseq = $("#sa_getsample_pidorseq").val();
    var timeSearchFrom = $("#sa_getsample_timeSearchFrom").val();
    var timeSearchTo = $("#sa_getsample_timeSearchTo").val();
    $.ajax({
        url: "/SA_GetSample/Search?" + "pidorseq=" + sa_getsample_pidorseq + "&timeSearchFrom=" + timeSearchFrom + "&timeSearchTo=" + timeSearchTo,
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

// Hiden SHow button
$("#sa_getsample_savepatient").hide();
$("#sa_getsample_cancelpatient").hide();
function GetSample_HideButton(_new, _save, _delete, _cancel, _addservice, _getsample) {
    if (_new == 1) {
        $("#sa_getsample_newpatient").hide();
    }
    else {
        $("#sa_getsample_newpatient").show();
    }

    if (_save == 1) {
        $("#sa_getsample_savepatient").hide();
    }
    else {
        $("#sa_getsample_savepatient").show();
    }

    if (_delete == 1) {
        $("#sa_getsample_deletepatient").hide();
    }
    else {
        $("#sa_getsample_deletepatient").show();
    }

    if (_cancel == 1) {
        $("#sa_getsample_cancelpatient").hide();
    }
    else {
        $("#sa_getsample_cancelpatient").show();
    }

    if (_addservice == 1) {
        $("#sa_getsample_addService").hide();
    }
    else {
        $("#sa_getsample_addService").show();
    }

    if (_getsample == 1) {
        $("#sa_getsample_processresult").hide();
    }
    else {
        $("#sa_getsample_processresult").show();
    }
}


function GetSample_GetPatientInfo(id) {
    GetSample_HideButton(false, true, false, true, false, false);
    $.ajax({
        url: "/SA_GetSample/GetPatientInfo?id=" + id,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#patientInfo").html(result);
            GetSample_SetSelect2_01(false);
            GetSample_GetListServiceForPatient(id);
        },
        error: function () {
            $("#patientInfo").empty();
            $('#tbody-gridview-service').empty();
        }
    });
}

function GetSample_GetListServiceForPatient(id) {
    $.ajax({
        url: "/sa_GetSample/GetServiceForPatient?patientId=" + id,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#sa-right-service-gridview").html(result);
        },
        error: function () {
            $('#tbody-gridview-service').empty();
        }
    });
}

function GetSample_ResetInput() {
    var date = new Date();
    var today = date.getFullYear() + "-" + ("0" + (date.getMonth() + 1)).slice(-2) + "-" + ("0" + date.getDate()).slice(-2);
    var time = ("0" + date.getHours()).slice(-2) + ":" + ("0" + date.getMinutes()).slice(-2);
    var dateTime = today + " " + time;
    $("#sa_getsample_pidorseq").val('');
    $('#sa_getsample_id').val('');
    $('#sa_getsample_patientId').val('');
    $('#sa_getsample_seq').val('');
    $('#sa_getsample_sid').val('');
    $('#sa_getsample_patientName').val('');
    $('#sa_getsample_age').val('');
    $('#sa_getsample_sex').val('');
    $('#sa_getsample_obj').val('');
    $('#sa_getsample_type').val('');
    $('#sa_getsample_location').val('');
    $('#sa_getsample_doctor').val('');
    $('#sa_getsample_getSampleTime').val(dateTime);
    $('#sa_getsample_address').val('');
    $('#sa_getsample_diagnostic').val('');
    $('#select-category').val('');
    $('#select-service').val('');
    $('#tbody-gridview-service').empty();
    GetSample_SetSelect2_01(false);
}

function GetSample_NewPatient() {
    $('#sa_getsample_patientId').focus();
    GetSample_HideButton(true, false, true, false, true, true);
    GetSample_ResetInput();
}

function GetSample_DeletePatient() {
    var id = $('#sa_getsample_id').val();
    if (id == '') {
        alert("Vui lòng chọn bệnh nhân!");
    }
    else {
        var choice = confirm("Bạn muốn xoá bệnh nhân và tất cả chỉ định xét nghiệm?");
        if (choice) {
            $.ajax({
                url: "/SA_GetSample/DeletePatientAndService?id=" + id,
                type: "POST",
                dataType: "text",
                cache: false,
                success: function (result) {
                    if (result === '') {
                        alert("Xoá không thành công. Vui lòng kiểm tra lại!");
                    }
                    else {
                        GetSample_HideButton(false, true, false, true, false, false)
                        GetSample_Refresh(result);
                        GetSample_Get_Count();
                    }
                },
                error: function () {
                    alert("Xoá không thành công. Vui lòng kiểm tra lại!");
                }
            });
        }
    }
}

function GetSample_CancelPatient() {
    GetSample_HideButton(false, true, false, true, false, false);
    var id = $('#sa_getsample_id').val();
    if (id == '') {
        GetSample_ResetInput()
    }
    else {
        GetSample_GetPatientInfo(id);
    }
}

function GetSample_GetSID(seq) {
    $.ajax({
        url: "/SA_GetSample/GetSID?seq=" + seq,
        type: "GET",
        dataType: "text",
        cache: false,
        success: function (result) {
            $('#sa_getsample_sid').val(result);
        }
    });
}

function GetSample_SavePatient() {
    var validate = GetSample_ValidateInput('patientInfo');
    if (validate) {
        var id = $('#sa_getsample_id').val();
        var patientId = $('#sa_getsample_patientId').val();
        var seq = $('#sa_getsample_seq').val();
        var sid = $('#sa_getsample_sid').val();
        var patientName = $('#sa_getsample_patientName').val();
        var age = $('#sa_getsample_age').val();
        var sex = $('#sa_getsample_sex').val();
        var obj = $('#sa_getsample_obj').val();
        var type = $('#sa_getsample_type').val();
        var location = $('#sa_getsample_location').val();
        var doctor = $('#sa_getsample_doctor').val();
        var getSampleTime = $('#sa_getsample_getSampleTime').val();
        var address = $('#sa_getsample_address').val();
        var diagnostic = $('#sa_getsample_diagnostic').val();
        var category = $('#select-category').val();
        var service = $('#select-service').val();

        $.ajax({
            url: "/SA_GetSample/SavePatient?id= " + id + "&&patientId=" + patientId + "&&seq=" + seq + "&&sid=" + sid + "&&patientName=" + patientName + "&&age=" + age + "&&sex=" + sex + "&&obj=" + obj + "&&type=" + type + "&&location=" + location + "&&doctor=" + doctor + "&&getSampleTime=" + getSampleTime + "&&address=" + address + "&&diagnostic=" + diagnostic + "&&service=" + service,
            type: 'POST',
            dataType: 'text',
            success: function (result) {
                if (result == "") {
                    alert("Lưu không thành công. Vui lòng kiểm tra lại!");
                }
                else {
                    GetSample_GetPatientInfo(result);
                    GetSample_Refresh_No_Clear_PatientInfo();
                    GetSample_SetSelect2_01(false);
                    GetSample_Get_Count();
                }
            },
            error: function () {
                alert("Lưu không thành công. Vui lòng kiểm tra lại!");
            }
        });
    }
}

function GetSample_ProcessResult() {
    var id = $('#sa_getsample_id').val();
    if (id === "") {
        alert("Vui lòng chọn bệnh nhân!");
    }
    else {
        var getSampleTime = $('#sa_getsample_getSampleTime').val();
        $.ajax({
            url: "/SA_GetSample/ProcessResult?id= " + id + "&getSampleTime=" + getSampleTime,
            type: 'POST',
            dataType: 'text',
            success: function (result) {
                if (result === 'True') {
                    GetSample_Refresh();
                    GetSample_Get_Count();
                }
                else {
                    alert("Xử lý kết quả không thành công. Vui lòng kiểm tra lại!");
                }
            },
            error: function () {
                alert("Xử lý kết quả không thành công. Vui lòng kiểm tra lại!");
            }
        });
    }
}

function GetSample_AddService() {
    var id = $('#sa_getsample_id').val();
    if (id === "") {
        $('#addServiceForm').modal('hide');
        alert("Vui lòng chọn bệnh nhân!");
    }
    else {
        GetSample_SetSelect2_02();
    }
}

function GetSample_DeleteServiceForPatient(idResultCDHA) {
    var patientId = $('#sa_getsample_id').val();
    $.ajax({
        url: "/SA_GetSample/DeleteServiceForPatient?id=" + idResultCDHA,
        type: "POST",
        dataType: "text",
        cache: false,
        success: function (result) {
            if (result == 'True') {
                GetSample_GetListServiceForPatient(patientId);
            }
            else {
                alert("Không thể xoá. Vui lòng kiểm tra lại !");
            }
        }
    });
}

function GetSample_AddServiceForPatient() {
    var serviceId = $('#sa_getsample_service').val();
    var patientId = $('#sa_getsample_id').val();
    var doctorId = $('#sa_getsample_doctor').val();
    if (serviceId === "" || patientId === "") {
        alert("Chỉ định dịch vụ không thành công. Vui lòng kiểm tra lại !")
    }
    else {
        $.ajax({
            url: "/SA_GetSample/AddServiceForPatient?patientId=" + patientId + "&&serviceId=" + serviceId + "&&doctorId=" + doctorId,
            type: 'POST',
            dataType: 'text',
            success: function (result) {
                if (result == 'True') {
                    GetSample_GetListServiceForPatient(patientId);
                }
                else {
                    alert("Thêm dịch vụ không thành công. Vui lòng kiểm tra lại!");
                }
            },
            error: function () {
                alert("Thêm dịch vụ không thành công. Vui lòng kiểm tra lại!");
            }
        });

        GetSample_SetSelect2_02();
    }
}

function GetSample_Get_Count() {
    $.ajax({
        url: "/SA_GetSample/Get_Count/",
        type: 'GET',
        dataType: 'text',
        success: function (result) {
            var arrayResult = result.split(';');
            document.getElementById("countGetSample").innerHTML = arrayResult[0];
            document.getElementById("countProcess").innerHTML = arrayResult[1];
            document.getElementById("countReturnResult").innerHTML = arrayResult[2];
        }
    });
}


//***************************************************************************************** Process

function Process_ValidateInput(id) {
    var flag = true;
    $("#" + id).find(".valid").each(function (i, e) {
        var value = $(e).val();
        if (value === "" || !value) {
            if (flag) {
                if (e.id === "sa_process_userReturnResultSA") {
                    $('#sa_process_userReturnResultSA').select2('focus');
                }
                else {
                    $(e).focus();
                }
            }
            flag = false;
        }
    })
    return flag;
}

function Process_Refresh() {
    $.ajax({
        url: "/SA_Process/Refresh/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            var date = new Date();
            var today = date.getFullYear() + "-" + ("0" + (date.getMonth() + 1)).slice(-2) + "-" + ("0" + date.getDate()).slice(-2);
            $("#process_listPatient").html(result);
            $("#sa_getsample_timeSearchFrom").val(today);
            $("#sa_getsample_timeSearchTo").val(today);
            Process_ResetInput();
            Process_Get_Count();
        },
        error: function () {
            $("#process_listPatient").empty();
        }
    });
}

function Process_Search() {
    var sa_getsample_pidorseq = $("#sa_getsample_pidorseq").val();
    var timeSearchFrom = $("#sa_getsample_timeSearchFrom").val();
    var timeSearchTo = $("#sa_getsample_timeSearchTo").val();
    $.ajax({
        url: "/SA_Process/Search?" + "pidorseq=" + sa_getsample_pidorseq + "&timeSearchFrom=" + timeSearchFrom + "&timeSearchTo=" + timeSearchTo,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#process_listPatient").html(result);
        },
        error: function () {
            $("#process_listPatient").empty();
        }
    });
}

function Process_ResetInput() {
    $("#sa_process_pidorseq").val('');
    $('#sa_process_id').val('');
    $('#sa_process_patientId').val('');
    $('#sa_process_seq').val('');
    $('#sa_process_sid').val('');
    $('#sa_process_patientName').val('');
    $('#sa_process_age').val('');
    $('#sa_process_sex').val('');
    $('#sa_process_obj').val('');
    $('#sa_process_type').val('');
    $('#sa_process_location').val('');
    $('#sa_process_doctor').val('');
    $('#sa_process_getSampleTime').val('');
    $('#sa_process_returnResultTime').val('');
    $('#sa_process_location').val('');
    $('#sa_process_doctor').val('');
    $('#sa_process_userReturnResultSA').val('');
    $('#sa_process_address').val('');
    $('#sa_process_diagnostic').val('');
    $('#tbody-gridview-service').empty();
    $('#imageCDHA').empty();
    $('#sa_process_result').empty();
    $('#sa_process_suggest').empty();
    CKEDITOR.instances["sa_process_description"].setData("");
    Process_SetSelect2_03(true);
}

function Process_GetPatientInfo(id) {
    $.ajax({
        url: "/SA_Process/GetPatientInfo?id=" + id,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#process_patientInfo").html(result);
            Process_SetSelect2_03();
            // Auto-chọn bác sĩ = user đang login (nếu chưa có giá trị)
            var loginId = $("#sa_process_userLoginId").val();
            var $sel = $("#sa_process_userReturnResultSA");
            if (loginId && (!$sel.val() || $sel.val() === "")) {
                $sel.val(loginId).trigger("change");
            }

            // Gắn handler đổi bác sĩ
            $(document)
                .off("change", "#sa_process_userReturnResultSA")
                .on("change", "#sa_process_userReturnResultSA", Process_Load_SelectedDoctorInfo);
            // Lần đầu nếu có sẵn value thì cũng load info
            Process_Load_SelectedDoctorInfo();

            Process_GetListServiceForPatient(id);
        },
        error: function () {
            $("#process_patientInfo").empty();
            $('#tbody-gridview-service').empty();
        }
    });
}

function Process_GetListServiceForPatient(id) {
    $.ajax({
        url: "/sa_Process/GetServiceForPatient?patientId=" + id,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#process_sa-right-service-gridview").html(result);
        },
        error: function () {
            $('#tbody-gridview-service').empty();
        }
    });
}

function Process_SaveResult() {
    var patientId = $('#sa_process_id').val();
    if (patientId === "") {
        alert("Vui lòng chọn bệnh nhân!");
    }
    else {
        var validate = Process_ValidateInput('process_patientInfo');
        if (validate) {

            // Lấy ServiceId
            var resultCDHAId = "";
            $(".row-service").each(function () {
                $(this).find(".form-check-input").each(function () {
                    var isChecked = $(this).is(':checked');
                    if (isChecked) {
                        resultCDHAId = $(this).val();
                    }
                })
            })

            if (resultCDHAId === "") {
                alert("Vui lòng chọn dịch vụ!");
            }
            else {
                var returnResultTime = $('#sa_process_returnResultTime').val();
                var userReturnResult = $('#sa_process_userReturnResultSA').val();
                var description = CKEDITOR.instances['sa_process_description'].getData();
                var result = $('#sa_process_result').val();
                var suggest = $('#sa_process_suggest').val();

                var data = {
                    patientId: patientId,
                    resultCDHAId: resultCDHAId,
                    returnResultTime: returnResultTime,
                    userReturnResult: userReturnResult,
                    description: description,
                    result: result,
                    suggest: suggest
                };

                $.ajax({
                    url: "/SA_Process/SaveResult/",
                    data: JSON.stringify(data),
                    contentType: "application/json; charset=utf-8",
                    dataType: "text",
                    type: "POST",
                    success: function (result) {
                        if (result == 'True') {
                        }
                        else {
                            alert("Lưu không thành công. Vui lòng kiểm tra lại!");
                        }
                    },
                    error: function () {
                        alert("Lưu không thành công. Vui lòng kiểm tra lại!");
                    }
                });
                return true;
            }
        }
        else {
            return false
        }
    }
}

function Process_ValidPrint() {
    var patientId = $('#sa_process_id').val();
    if (patientId === "") {
        alert("Vui lòng chọn bệnh nhân!");
    }
    else {
        var validate = Process_ValidateInput('process_patientInfo');
        if (validate) {

            // Lấy ServiceId
            var resultCDHAId = "";
            $(".row-service").each(function () {
                $(this).find(".form-check-input").each(function () {
                    var isChecked = $(this).is(':checked');
                    if (isChecked) {
                        resultCDHAId = $(this).val();
                    }
                })
            })

            if (resultCDHAId === "") {
                alert("Vui lòng chọn dịch vụ!");
            }
            else {
                $('#showWaitting').modal('show');
                var returnResultTime = $('#sa_process_returnResultTime').val();
                var userReturnResult = $('#sa_process_userReturnResultSA').val();
                var description = CKEDITOR.instances['sa_process_description'].getData();
                var result = $('#sa_process_result').val();
                var suggest = $('#sa_process_suggest').val();

                var data = {
                    patientId: patientId,
                    resultCDHAId: resultCDHAId,
                    returnResultTime: returnResultTime,
                    userReturnResult: userReturnResult,
                    description: description,
                    result: result,
                    suggest: suggest
                };

                $.ajax({
                    url: "/SA_Process/ValidPrint/",
                    data: JSON.stringify(data),
                    contentType: "application/json; charset=utf-8",
                    dataType: "text",
                    type: "POST",
                    success: function (response) {
                        $('#showWaitting').modal('hide');
                        if (response === "") {
                            alert("Valid & In không thành công. Vui lòng kiểm tra lại!");
                        }
                        else {
                            Process_Refresh();
                            Process_Get_Count();
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
                        alert("Lưu không thành công. Vui lòng kiểm tra lại!");
                    }
                });
                return true;
            }
        }
        else {
            return false
        }
    }
}

function Process_GetSample() {
    var id = $('#sa_process_id').val();
    if (id === "") {
        alert("Vui lòng chọn bệnh nhân!");
    }
    else {
        $.ajax({
            url: "/SA_Process/GetSample?id= " + id,
            type: 'POST',
            dataType: 'text',
            success: function (result) {
                if (result === 'True') {
                    Process_Refresh();
                }
                else {
                    alert("Lấy mẫu không thành công. Vui lòng kiểm tra lại!");
                }
            },
            error: function () {
                alert("Lấy mẫu không thành công. Vui lòng kiểm tra lại!");
            }
        });
    }
}

function Process_SetStatus_ClickButtonStatus(btn, value, index) {
    var idResult = "result" + index;
    var result = document.getElementById(idResult);
    if (value === '0') {
        btn.style.color = 'blue';
        result.style.color = 'blue';
        btn.value = 1;
    }
    if (value === '1') {
        btn.style.color = 'red';
        btn.value = 2;
        result.style.color = 'red';
    }
    if (value === '2') {
        btn.style.color = 'dimgrey';
        btn.value = 0;
        result.style.color = 'black';
    }
}

function Process_Get_Count() {
    $.ajax({
        url: "/SA_Process/Get_Count/",
        type: 'GET',
        dataType: 'text',
        success: function (result) {
            var arrayResult = result.split(';');
            document.getElementById("countGetSample").innerHTML = arrayResult[0];
            document.getElementById("countProcess").innerHTML = arrayResult[1];
            document.getElementById("countReturnResult").innerHTML = arrayResult[2];
        }
    });
}

function Process_CheckedBoxOnRow(id) {
    $(".row-service").each(function () { // Lấy value trên từng Row
        $(this).find(".form-check-input").each(function () {
            var value = $(this).val();
            if (value == id) {
                $(this).prop("checked", true);
                Process_Load_ResultAndImage_ForService(id);
            }
            else {
                $(this).prop("checked", false);
            }
        })

    })
}

function Process_Load_ResultAndImage_ForService(id) {
    $("#imageCDHA").empty();
    CKEDITOR.instances["sa_process_description"].setData("");
    $("#sa_process_result").val("");
    $("#sa_process_suggest").val("");
    $.ajax({
        url: "/SA_Process/GetImageForService?id=" + id,
        type: 'GET',
        dataType: 'json',
        success: function (response) {
            $.each(response, function (key, value) {
                if (value.id) {
                    $("#imageCDHA").append('<img class="image-item" id="' + value.id + '" src="' + value.name + '" onclick="Process_GetImageCDHAId(' + value.id + ')"/>');
                }
                CKEDITOR.instances["sa_process_description"].setData(value.description);
                $("#sa_process_result").val(value.result);
                $("#sa_process_suggest").val(value.suggest);
            });
        }
    });
}

function Process_GetImageCDHAId(imageCDHAId) {
    $("#sa_process_deletephoto").val(imageCDHAId);
}

// Begin chụp hình
function StartCamera() {
    $.ajax({
        url: "/SA_Process/Check_SelectDevice/",
        type: 'GET',
        dataType: 'text',
        success: function (result) {
            if (result === 'True') {
                StartCamera_01();
            }
            else {
                $('#addDeviceForm').modal('show');
            }
        },
        error: function () {
            alert("Không thể chọn thiết bị. Vui lòng kiểm tra lại!");
        }
    });
}

let camera_stream = null;
function StartCamera_01() {
    var video = document.querySelector("#video");
    navigator.mediaDevices.getUserMedia({ video: true, audio: false }).then(function success(stream) {
        video.srcObject = stream;
        camera_stream = stream;
    });
    document.getElementById("sa_process_startcamera").style.display = "none";
    document.getElementById("sa_process_snapshot").style.display = "unset";
    document.getElementById("sa_process_deletephoto").style.display = "unset";
    document.getElementById("sa_process_startvideo").style.display = "unset";
}

function Process_SelectDevice() {
    var deviceId = $('#sa_getsample_select_device_select').val();
    if (deviceId === "") {
        alert("Vui lòng chọn thiết bị !")
    }
    else {
        $.ajax({
            url: "/SA_Process/SelectDevice?deviceId=" + deviceId,
            type: 'GET',
            dataType: 'text',
            success: function (result) {
                if (result == 'False') {
                    alert("Không thể chọn thiết bị. Kiểm tra lại !");
                }
                else {
                    StartCamera_01();
                }
            },
            error: function () {
                alert("Không thể chọn thiết bị. Kiểm tra lại !");
            }
        });
    }
}

function SnapShot() {
    var countImage = 0;
    $(".imageCDHA").each(function () { // Đếm Image đã chụp
        $(this).find(".image-item").each(function () {
            countImage++;
        })
    })

    if (countImage === 4) {
        alert("Chỉ cho phép chụp tối đa 4 hình !");
    }
    else {
        var canvas = document.getElementById("canvas");
        //canvas.width = 1500;
        //canvas.height = 1133;
        canvas.width = 750;
        canvas.height = 567;
        //canvas.width = 300;
        //canvas.height = 228;
        canvas.getContext('2d').drawImage(video, 0, 0, canvas.width, canvas.height);
        var image_data_url = document.getElementById("canvas").toDataURL("image/png");
        var resultCDHAId = null;

        $(".row-service").each(function () { // Lấy value trên từng Row
            $(this).find(".form-check-input").each(function () {
                var isChecked = $(this).prop('checked');
                if (isChecked) {
                    resultCDHAId = $(this).val();
                }
            })
        })

        if (resultCDHAId !== null) {
            var data = {
                imageString: image_data_url,
                resultCDHAId: resultCDHAId
            };
            $.ajax({
                url: "/SA_Process/SaveImageCDHA/",
                type: 'POST',
                contentType: "application/json; charset=utf-8",
                data: JSON.stringify(data),
                dataType: 'text',
                success: function (response) {
                    if (response !== 'True') {
                        alert("Không thể lưu ảnh. Vui lòng kiểm tra lại!");
                    }
                    else {
                        Process_Load_Image_ForService(resultCDHAId);
                    }
                }
            });
        }
        else {
            alert("Vui lòng chọn dịch vụ !");
        }
    }
}

function DeletePhoto() {
    var imageCDHAId = $("#sa_process_deletephoto").val();
    if (imageCDHAId !== null) {
        $.ajax({
            url: "/SA_Process/DeleteImageCDHA?imageCDHAId=" + imageCDHAId,
            type: 'POST',
            dataType: 'text',
            success: function (response) {
                if (response === "") {
                    alert("Không thể xoá ảnh. Vui lòng kiểm tra lại!");
                }
                else {
                    Process_Load_Image_ForService(response);
                }
            }
        });
    }
    else {
        alert("Không thể xoá ảnh. Vui lòng kiểm tra lại!");
    }
}
// End chụp hình

// Begin ghi video màn hình
let media_recorder = null;
let blobs_recorded = [];
let download_link = document.querySelector("#sa_process_downloadvideo_a");
function StartVideo() {
    document.getElementById("sa_process_startvideo").style.display = "none";
    document.getElementById("sa_process_stopvideo").style.display = "unset";

    media_recorder = new MediaRecorder(camera_stream, { mimeType: 'video/webm' });
    media_recorder.addEventListener('dataavailable', function (e) {
        blobs_recorded.push(e.data);
    });

    media_recorder.addEventListener('stop', function () {
        let video_local = URL.createObjectURL(new Blob(blobs_recorded, { type: 'video/webm' }));
        download_link.href = video_local;
    });
    media_recorder.start(1000);
}

function StopVideo() {
    document.getElementById("sa_process_stopvideo").style.display = "none";
    document.getElementById("sa_process_deletevideo").style.display = "unset";
    document.getElementById("sa_process_downloadvideo").style.display = "unset";

    media_recorder.stop();
}

function DeleteVideo() {
    document.getElementById("sa_process_startvideo").style.display = "unset";
    document.getElementById("sa_process_deletevideo").style.display = "none";
    document.getElementById("sa_process_downloadvideo").style.display = "none";

    media_recorder = null;
    blobs_recorded = [];
    download_link.href = null;
}

function DownloadVideo() {
    document.getElementById("sa_process_startvideo").style.display = "unset";
    document.getElementById("sa_process_deletevideo").style.display = "none";
    document.getElementById("sa_process_downloadvideo").style.display = "none";

    download_link.click();
    media_recorder = null;
    blobs_recorded = [];
    download_link.href = null;
}
// End ghi video màn hình

function Process_Load_Image_ForService(id) {
    $("#imageCDHA").empty();
    $.ajax({
        url: "/SA_Process/GetImageForService?id=" + id,
        type: 'GET',
        dataType: 'json',
        success: function (response) {
            $.each(response, function (key, value) {
                if (value.id) {
                    $("#imageCDHA").append('<img class="image-item" id="' + value.id + '" src="' + value.name + '" onclick="Process_GetImageCDHAId(' + value.id + ')"/>');
                }
            });
        }
    });
}

function GetSampleForService(id) {
    $.ajax({
        url: "/SA_Process/GetSampleForService?id=" + id,
        type: 'GET',
        dataType: 'json',
        success: function (response) {
            CKEDITOR.instances["sa_process_description"].setData(response.description);
            $("#sa_process_result").val(response.result);
            $("#sa_process_suggest").val(response.suggest);
        }
    });
}

// Tải thông tin bác sĩ khi đổi select
function Process_Load_SelectedDoctorInfo() {
    var userId = $("#sa_process_userReturnResultSA").val();
    if (!userId) {
        $("#sa_process_signerCCCD").empty();
        return;
    }
    $.ajax({
        url: "/SA_Process/GetUserInfo?userId=" + userId,
        type: "GET",
        dataType: "json",
        success: function (u) {
            if (!u) {
                $("#sa_process_signerCCCD").text("Không lấy được thông tin bác sĩ.");
                return;
            }
            // Hiển thị nhẹ ở UI
            var cccd = (u.cccd || "");
            $("#sa_process_signerCCCD").val(cccd);
        },
        error: function () {
            $("#sa_process_signerCCCD").text("Không lấy được thông tin bác sĩ.");
        }
    });
}

// ========================== KÝ SỐ PDF (Form: SIÊU ÂM) ==========================

async function testKySoTruyenFilePdf() {
    const fileUrl = `http://172.16.0.2:6868/download/Bien_ban_hoi_chan_thong_qua_phau_thuat.pdf`;
    const apiUrl = "http://172.16.0.7:8080/signserver/api/ext/signPdfWithMysign";
    const signerCCCD = "089097018854";// CCCD bác sĩ
    const taxCode = "0316876869";// Mã số thuế công ty
    const targetText = "Trần Minh Tuấn"; // Tên bác sĩ

    try {
        // 1. Tải file về dạng blob
        const pdfResponse = await axios.get(fileUrl, {
            responseType: 'blob'
        });

        // 2. Tạo đối tượng File từ blob
        const pdfFile = new File([pdfResponse.data], 'Bien_ban_hoi_chan.pdf', {
            type: 'application/pdf'
        });

        // 3. Tạo FormData
        const formData = new FormData();
        formData.append('file', pdfFile);
        formData.append('signUserId', signerCCCD);
        formData.append('taxcode', taxCode);
        formData.append('targetText', targetText);

        // 4. Gửi request ký số
        const result = await axios.post(apiUrl, formData);

        console.log("Đã ký PDF:", result.data);
        // Xử lý kết quả tại đây (ví dụ: lưu đường dẫn, reload...)
    } catch (err) {
        console.error("Lỗi khi ký PDF:", err.response?.data || err.message);
        showError("Ký thất bại.");
    }
}
function Process_SignPdf() {
    var patientId = $('#sa_process_id').val();
    if (!patientId) {
        alert("Vui lòng chọn bệnh nhân!");
        return;
    }

    // Lấy ResultCDHAId (dịch vụ đang chọn) – giống logic ValidPrint
    var resultCDHAId = "";
    $(".row-service").each(function () {
        $(this).find(".form-check-input").each(function () {
            if ($(this).is(':checked')) {
                resultCDHAId = $(this).val();
            }
        })
    });
    if (!resultCDHAId) {
        alert("Vui lòng chọn dịch vụ!");
        return;
    }

    // Lấy CCCD người ký (bác sĩ)
    var signerCCCD = $('#sa_process_signerCCCD').val();
    if (!signerCCCD || $.trim(signerCCCD) === "") {
        alert("Vui lòng nhập CCCD người ký (Viettel MySign)!");
        $('#sa_process_signerCCCD').focus();
        return;
    }
    console.log(signerCCCD);

    // Lấy context để backend đặt tên file đẹp theo template
    var patientCode = $('#sa_process_patientId').val() || $('#sa_process_sid').val() || "";
    var patientName = $('#sa_process_patientName').val() || "";
    var doctorName = $('#sa_process_userReturnResultSA option:selected').text() || $('#sa_process_userReturnResultSA').val() || "";
    var performedAt = $('#sa_process_returnResultTime').val() || ""; // thời điểm trả kết quả
    var serviceCode = ""; // nếu cần, bạn lấy từ label dịch vụ đang chọn

    // 1) Gọi API xuất PDF base64 (tận dụng endpoint ValidPrint đang có)
    //    Nếu bạn có endpoint riêng chỉ "Export" (không đổi trạng thái), thay URL ở đây là tốt nhất.
    $('#showWaitting').modal('show');
    var dataExport = {
        patientId: patientId,
        resultCDHAId: resultCDHAId,
        returnResultTime: $('#sa_process_returnResultTime').val(),
        userReturnResult: $('#sa_process_userReturnResultSA').val(),
        description: CKEDITOR.instances['sa_process_description'].getData(),
        result: $('#sa_process_result').val(),
        suggest: $('#sa_process_suggest').val()
    };

    $.ajax({
        url: "/SA_Process/ValidPrint/",
        data: JSON.stringify(dataExport),
        contentType: "application/json; charset=utf-8",
        dataType: "text",
        type: "POST",
        success: function (base64Pdf) {
            console.log("Sau khi validPrint: ", base64Pdf)
            //if (!base64Pdf) {
            //    $('#showWaitting').modal('hide');
            //    alert("Không thể xuất PDF để ký. Vui lòng kiểm tra lại!");
            //    return;
            //}

            // 2) Chuyển base64 -> File để gửi multipart/form-data
            var file = base64ToFile(base64Pdf, "temp.pdf", "application/pdf");
            console.log(file);
            // 3) Chuẩn bị FormData gửi đến API ký số chung (server sẽ tự đặt tên file)
            var formData = new FormData();
            formData.append("signerCCCD", signerCCCD);
            formData.append("file", file);

            // ---- context để server đặt tên theo template MySign:FileNameTemplates (FormType = SieuAm)
            formData.append("formType", "SieuAm");
            formData.append("patientCode", patientCode);
            formData.append("patientName", patientName);
            formData.append("serviceCode", serviceCode);
            formData.append("doctorName", doctorName);
            if (performedAt) formData.append("performedAt", performedAt); // ISO/yyyy-MM-dd HH:mm:ss
            // formData.append("extra", "SA-Extra"); // nếu cần
            console.log(formData);
            $.ajax({
                url: "/api/ExternalSign/sign-pdf",
                data: formData,
                processData: false,
                contentType: false,
                type: "POST",
                success: function (resp) {
                    console.log(resp)
                    $('#showWaitting').modal('hide');
                    // Gỉa sử object trả về từ response như sau
                    //var response = {
                    //    "id": "51627", "provider": "0", "documentId": "null", "documentType": "null", "documentName": "27433568_phieu_danh_gia_ban_dau.pdf", "documentDesc": "null", "documentHash": "7X38RRgfWpYULcvtZc863HnTfocDsSsji023UGoxBLM=", "timestamp": "2025-09-19T07:11:06.763046209Z", "transactionId": "308706a6-5dc7-4f0b-ba08-01448c15a306", "credentialId": "066194014120_7500452_20250821162101", "signatures": "SJpUV2IQVRR3FzMzK5WWLucKNX2jtFc4\/\/+rvi19Izj6yL1epn7xo1cxGhW\/67fI\r\niqLqz104zwke0xbWf9BZPjAEmSWD8Xn2RaouyGCBQIj4tnCnMZQGnPCBZ2laN4Ec\r\n3qjMU3lpoh22OnVBgLYZlQZSSvnNii1MF7v1vKRsdggl4YZpHEiZlgMef7iclkga\r\nMGUryw11teXAg77s15jGKYCjkM49jrJlGq+M5p0IVCj\/ZeUXByd3MsBeWAnWAHs1\r\n\/WoyEfBhwK3YABb37cjO6RNFACj+KJJv8910AyQwZeowyhmeQDdjLdGgfJRILORt\r\nDFZF1iABlsXIbp6pf0ek8w==", "status": "1", "filepath": "null", "url": "null", "docControlId": "null", "docControlType": "null", "error": "null", "login": "066194014120", "taxcode": "6001362081", "metadata": "null", "requesttime": "1758265850532", "contentContentType": "application\/pdf", "signUserId": "066194014120",
                    //}
                    // Controller trả về "response.data" nếu có; tùy MySign:
                    // - Nếu có resp.fileUrl -> mở luôn
                    // - Nếu có resp.base64Pdf -> mở PDF
                    // - Nếu là string -> thử parse
                    try {
                        var obj = (typeof resp === "string") ? JSON.parse(resp) : resp;

                        // 1) THU THẬP signStoreId TỪ RESPONSE (ưu tiên data.id)
                        var signStoreId =
                            (obj && obj.data && (obj.data.id || obj.data.signStoreId)) ||
                            obj.id || obj.signStoreId || null;

                        // giả sử signStoreId đang null để test dev nên gán cứng
                        signStoreId = signStoreId;
                        var keyResult = null;

                        // 2) LƯU signStoreId THEO resultCDHAId ĐANG CHỌN
                        if (signStoreId) {
                            $.ajax({
                                url: "/SA_Process/SaveSignStoreIdForResultCDHA",
                                type: "POST",
                                dataType: "text",
                                data: { resultCDHAId: resultCDHAId, signStoreId: signStoreId },
                                success: function (res) {
                                    var objResult = JSON.parse(res);
                                    openSigned(signStoreId);
                                    if (objResult.success == true) {
                                        keyResult = objResult.keyResult;

                                        // ====== GỌI CONTROLLER LƯU DIGITAL_SIGN ======
                                        var targetText = doctorName;
                                        var statusNum = (obj && (obj.status === 1 || obj.status === "1")) ? 1 : 0;

                                        var digitalSignPayload = {
                                            referenceType: "SieuAm",                // SA / SATIM / XQUANG...
                                            referenceKeyResult: keyResult,          // keyResultForHis của resultCDHAId 
                                            signId: signStoreId ? Number(signStoreId) : null,     // Id từ resp trả về
                                            signUserId: signerCCCD,                     // CCCD người ký
                                            taxCode: (obj.taxcode || obj.taxCode) || null,
                                            targetText: targetText,
                                            requestUrl: window.location.origin + "/api/ExternalSign/sign-pdf",
                                            responseData: JSON.stringify(obj),          // lưu toàn bộ JSON
                                            status: statusNum                            // 1 thành công, 0 thất bại
                                            // CreatorId/CreatedAt/Deleted: server tự set
                                        };
                                        console.log("digitalSignPayload: ", digitalSignPayload);
                                        $.ajax({
                                            url: "/DigitalSign/Save",                    // <-- endpoint lưu bảng Digital_Sign
                                            type: "POST",
                                            data: JSON.stringify(digitalSignPayload),
                                            contentType: "application/json; charset=utf-8",
                                            dataType: "json",
                                            success: function (r) {
                                                console.log("Lưu DigitalSign thành công: ", r);
                                            },
                                            error: function (f) {
                                                console.log("Lưu DigitalSign thất bại: ", f);
                                            }
                                        })

                                        // 3) XỬ LÝ HIỂN THỊ FILE ĐÃ KÝ NHƯ CŨ
                                        if (obj && obj.fileUrl) {
                                            window.open(obj.fileUrl, "_blank");
                                            return;
                                        }
                                        if (obj && (obj.base64Pdf || obj.base64)) {
                                            openBase64Pdf(obj.base64Pdf || obj.base64);
                                            return;
                                        }
                                    }
                                },
                                error: function (xhr) {
                                    console.error("SaveSignStoreIdForResultXN error:", xhr.responseText);
                                }
                            });
                        } else {
                            alert("Ký số THẤT BẠI. Đã Valid kết quả. Chọn Invalid kết quả và tiến hành ký số lại.");
                        }
                    } catch (e) {
                        if (isProbablyBase64(resp)) {
                            openBase64Pdf(resp);
                        } else {
                            alert("Đã ký số, phản hồi:\n" + resp);
                        }
                    }
                },
                error: function (xhr) {
                    $('#showWaitting').modal('hide');
                    var msg = "Ký số THẤT BẠI. Đã Valid kết quả. Chọn Invalid kết quả và tiến hành ký số lại.";
                    //if (xhr && xhr.responseText) msg += "\n" + xhr.responseText;
                    alert(msg);
                }
            });
        },
        error: function () {
            $('#showWaitting').modal('hide');
            alert("Xuất PDF không thành công. Vui lòng kiểm tra lại!");
        }
    });
}

// Helpers
function base64ToFile(base64, filename, mime) {
    try {
        // Nếu backend trả 'data:application/pdf;base64,....' thì tách bỏ prefix
        var idx = base64.indexOf("base64,");
        var pure = (idx >= 0) ? base64.substring(idx + 7) : base64;

        var byteCharacters = atob(pure);
        var byteNumbers = new Array(byteCharacters.length);
        for (var i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        var byteArray = new Uint8Array(byteNumbers);
        var blob = new Blob([byteArray], { type: 'application/pdf' });
        console.error(blob);
        // Tên tạm – server sẽ tự đặt lại theo template nên không quan trọng
        var file = new File([blob], filename || "document.pdf", { type: 'application/pdf' });
        console.log(file)
        return file;
    } catch (e) {
        alert("Lỗi chuyển đổi base64 -> file: " + e);
        throw e;
    }
}

function openBase64Pdf(base64) {
    var idx = base64.indexOf("base64,");
    var pure = (idx >= 0) ? base64.substring(idx + 7) : base64;

    var byteCharacters = atob(pure);
    var byteNumbers = new Array(byteCharacters.length);
    for (var i = 0; i < byteCharacters.length; i++) {
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
    var byteArray = new Uint8Array(byteNumbers);
    var file = new Blob([byteArray], { type: 'application/pdf' });
    var fileURL = URL.createObjectURL(file);
    window.open(fileURL);
}

function openSigned(id) {
    if (!id) {
        alert("Chưa có signStoreId cho dịch vụ này!");
        return;
    }
    var url = "/api/ExternalSign/view-signed/" + encodeURIComponent(id);
    window.open(url, "_blank");
}
function isProbablyBase64(s) {
    if (!s || typeof s !== "string") return false;
    var re = /^[A-Za-z0-9+/=\s]+$/; // thô sơ
    return re.test(s) && s.length > 1000; // PDF base64 thường dài
}
// ===============================================================================

//***************************************************************************************** Return Result

function ReturnResult_Refresh() {
    $.ajax({
        url: "/SA_ReturnResult/Refresh/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            var date = new Date();
            var today = date.getFullYear() + "-" + ("0" + (date.getMonth() + 1)).slice(-2) + "-" + ("0" + date.getDate()).slice(-2);
            $("#returnresult_listPatient").html(result);
            $("#sa_getsample_timeSearchFrom").val(today);
            $("#sa_getsample_timeSearchTo").val(today);
            ReturnResult_ResetInput();
            ReturnResult_Get_Count();
        },
        error: function () {
            $("#returnresult_listPatient").empty();
        }
    });
}

function ReturnResult_Search() {
    var sa_getsample_pidorseq = $("#sa_returnresult_pidorseq").val();
    var timeSearchFrom = $("#sa_returnresult_timeSearchFrom").val();
    var timeSearchTo = $("#sa_returnresult_timeSearchTo").val();
    $.ajax({
        url: "/SA_ReturnResult/Search?" + "pidorseq=" + sa_getsample_pidorseq + "&timeSearchFrom=" + timeSearchFrom + "&timeSearchTo=" + timeSearchTo,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#returnresult_listPatient").html(result);
        },
        error: function () {
            $("#returnresult_listPatient").empty();
        }
    });
}

function ReturnResult_ResetInput() {
    $("#sa_returnresult_pidorseq").val('');
    $('#sa_returnresult_id').val('');
    $('#sa_returnresult_patientId').val('');
    $('#sa_returnresult_seq').val('');
    $('#sa_returnresult_sid').val('');
    $('#sa_returnresult_patientName').val('');
    $('#sa_returnresult_age').val('');
    $('#sa_returnresult_sex').val('');
    $('#sa_returnresult_obj').val('');
    $('#sa_returnresult_type').val('');
    $('#sa_returnresult_location').val('');
    $('#sa_returnresult_doctor').val('');
    $('#sa_returnresult_getSampleTime').val('');
    $('#sa_returnresult_returnResultTime').val('');
    $('#sa_returnresult_location').val('');
    $('#sa_returnresult_doctor').val('');
    $('#sa_returnresult_userReturnResultSA').val('');
    $('#sa_returnresult_address').val('');
    $('#sa_returnresult_diagnostic').val('');
    $('#tbody-gridview-service').empty();
}

function ReturnResult_GetPatientInfo(id) {
    $.ajax({
        url: "/SA_ReturnResult/GetPatientInfo?id=" + id,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#returnresult_patientInfo").html(result);
            ReturnResult_GetListServiceForPatient(id);
        },
        error: function () {
            $("#returnresult_patientInfo").empty();
            $('#tbody-gridview-service').empty();
        }
    });
}

function ReturnResult_GetListServiceForPatient(id) {
    $.ajax({
        url: "/SA_ReturnResult/GetServiceForPatient?patientId=" + id,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#returnresult_sa-right-service-gridview").html(result);
        },
        error: function () {
            $('#tbody-gridview-service').empty();
        }
    });
}

function ReturnResult_Invalid() {
    var id = $('#sa_returnresult_id').val();
    if (id === "") {
        alert("Vui lòng chọn bệnh nhân!");
        return;
    }
    // Lấy ServiceId
    var resultCDHAId = "";
    $(".row-service").each(function () {
        $(this).find(".form-check-input").each(function () {
            var isChecked = $(this).is(':checked');
            if (isChecked) {
                resultCDHAId = $(this).val();
            }
        })
    })

    if (resultCDHAId === "") {
        alert("Vui lòng chọn dịch vụ!");
        return;
    }

    if (confirm('Bạn muốn InValid kết quả của bệnh nhân (hủy Ký Số kết quả này)?')) {
        $.ajax({
            url: "/SA_ReturnResult/Invalid?id= " + id,
            type: 'POST',
            dataType: 'text',
            success: function (result) {
                if (result === 'True') {
                    UpdateSignStatus(resultCDHAId);
                    ReturnResult_Refresh();
                    ReturnResult_Get_Count();
                }
                else {
                    alert("Không thể Invalid. Vui lòng kiểm tra lại!");
                }
            },
            error: function () {
                alert("Không thể Invalid. Vui lòng kiểm tra lại!");
            }
        });
    }
}

function ReturnResult_Get_Count() {
    $.ajax({
        url: "/SA_ReturnResult/Get_Count/",
        type: 'GET',
        dataType: 'text',
        success: function (result) {
            var arrayResult = result.split(';');
            document.getElementById("countGetSample").innerHTML = arrayResult[0];
            document.getElementById("countProcess").innerHTML = arrayResult[1];
            document.getElementById("countReturnResult").innerHTML = arrayResult[2];
        }
    });
}

function ReturnResult_Print() {
    var patientId = $('#sa_returnresult_id').val();
    if (patientId === "") {
        alert("Vui lòng chọn bệnh nhân!");
    }
    else {
        // Lấy ServiceId
        var resultCDHAId = "";
        $(".row-service").each(function () {
            $(this).find(".form-check-input").each(function () {
                var isChecked = $(this).is(':checked');
                if (isChecked) {
                    resultCDHAId = $(this).val();
                }
            })
        })

        if (resultCDHAId === "") {
            alert("Vui lòng chọn dịch vụ!");
        }
        else {
            $('#showWaitting').modal('show');
            $.ajax({
                url: "/SA_ReturnResult/Print?resultCDHAId=" + resultCDHAId,
                dataType: "text",
                type: "GET",
                success: function (response) {
                    $('#showWaitting').modal('hide');
                    if (response === "") {
                        alert("In không thành công. Vui lòng kiểm tra lại!");
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
                    alert("In không thành công. Vui lòng kiểm tra lại!");
                }
            });
        }
    }
}

function ReturnResult_CheckedBoxOnRow(id) {
    $(".row-service").each(function () { // Lấy value trên từng Row
        $(this).find(".form-check-input").each(function () {
            var value = $(this).val();
            if (value == id) {
                $(this).prop("checked", true);
                ReturnResult_LoadImageForService(id);
            }
            else {
                $(this).prop("checked", false);
            }
        })

    })
}

function ReturnResult_LoadImageForService(id) {
    $("#image-returnresult").empty();
    CKEDITOR.instances["sa_returnresult_description"].setData("");
    $("#sa_returnresult_result").val("");
    $("#sa_returnresult_suggest").val("");
    $.ajax({
        url: "/SA_ReturnResult/GetImageForService?id=" + id,
        type: 'GET',
        dataType: 'json',
        success: function (response) {
            $.each(response, function (key, value) {
                if (value.id) {
                    $("#image-returnresult").append('<div class="image-item"><img class="image" id="' + value.id + '" src="' + value.name + '" /></div > ');
                }
                CKEDITOR.instances["sa_returnresult_description"].setData(value.description);
                $("#sa_returnresult_result").val(value.result);
                $("#sa_returnresult_suggest").val(value.suggest);
            });
        }
    });
}

function ReturnResult_GetImageCDHAId(imageCDHAId) {
    $("#sa_returnresult_deletephoto").val(imageCDHAId);
}


function ReturnResult_ViewSignedPdf() {
    var signStoreId = $('#sa_returnresult_signStoreId').val();

    function openSigned(id) {
        if (!id) { alert("Chưa có signStoreId cho dịch vụ này!"); return; }
        var url = "/api/ExternalSign/view-signed/" + encodeURIComponent(id);
        window.open(url, "_blank");
    }

    if (signStoreId && $.trim(signStoreId) !== "") {
        openSigned(signStoreId);
        return;
    }

    // Nếu input rỗng → lấy dịch vụ đang chọn (giống ReturnResult_Print)
    var resultId = "";
    $(".row-service").each(function () {
        $(this).find(".form-check-input").each(function () {
            if ($(this).is(':checked')) {
                resultId = $(this).val();
            }
        });
    });
    console.log(resultId);
    if (!resultId) {
        alert("Vui lòng chọn dịch vụ để xem PDF đã ký!");
        return;
    }

    // Gọi API của bạn để lấy signStoreId đã lưu trong DB theo resultId
    $.ajax({
        url: "/SA_ReturnResult/GetSignStoreId",
        type: "GET",
        data: { resultId: resultId },
        dataType: "json",
        success: function (res) {
            console.log(res);
            var id = (res && (res.signStoreId || res.id)) ? (res.signStoreId || res.id) : "";
            if (id) {
                openSigned(id);
            } else {
                alert("Chưa lưu signStoreId cho dịch vụ này.");
            }
        },
        error: function () {
            alert("Không lấy được signStoreId. Vui lòng kiểm tra!");
        }
    });
}

function UpdateSignStatus(resultCDHAId) {
    $.ajax({
        url: "/DigitalSign/UpdateSignStatus_Result?resultCDHAId=" + resultCDHAId,
        type: 'POST',
        dataType: 'text',
        success: function (res) {
            console.log("Update Status Sign: ", res);
            var response = JSON.parse(res);
            if (response.success == true) {
                console.log("Lưu thành công.");
            }
            else {
                alert("Thao tác hủy ký chưa thành công");
            }
        },
        error: function () {
            alert("Lưu không thành công. Vui lòng kiểm tra lại!");
        }
    });
}