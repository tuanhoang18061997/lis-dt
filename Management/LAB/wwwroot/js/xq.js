// Tool tip cho control của form
//$(function () {
//    $('[data-toggle="tooltip"]').tooltip()
//})

//$(window).load(function () {
//    $('#addDeviceForm').modal('show');
//});

// *********************************************************************************** Select 2
$(function () {
    GetSample_SetSelect2_01(true);
})

function GetSample_SetSelect2_02() {
    $(document).ready(function () {
        $('#xq_getsample_service').select2({
            dropdownParent: $('#addServiceForm'),
            placeholder: "-- Chọn dịch vụ --"
        });
    });
    $('#xq_getsample_service').val('');
}

function GetSample_SetSelect2_01(isLoadPage) {
    if (isLoadPage) {
        $('#xq_getsample_location').val("");
    }
    $(document).ready(function () {
        $('#xq_getsample_location').select2({
            placeholder: "-- Chọn --"
        });
    });

    if (isLoadPage) {
        $('#xq_getsample_doctor').val("");
    }
    $(document).ready(function () {
        $('#xq_getsample_doctor').select2({
            placeholder: "-- Chọn --"
        });
    });


    if (isLoadPage) {
        $('#xq_process_sampleresult').val("");
    }
    $(document).ready(function () {
        $('#xq_process_sampleresult').select2({
            placeholder: "-- Chọn --"
        });
    });
}

function Process_SetSelect2_03() {
    $(document).ready(function () {
        $('#xq_process_userReturnResultXQ').select2({
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
                if (e.id === "xq_getsample_location") {
                    $('#xq_getsample_location').select2('focus');
                }
                else if (e.id === "xq_getsample_doctor") {
                    $('#xq_getsample_doctor').select2('focus');
                }
                else if (e.id === "xq_getsample_userReturnResultXQ") {
                    $('#xq_getsample_userReturnResultXQ').select2('focus');
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
        url: "/XQ_GetSample/Refresh/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            var date = new Date();
            var today = date.getFullYear() + "-" + ("0" + (date.getMonth() + 1)).slice(-2) + "-" + ("0" + date.getDate()).slice(-2);
            $("#listPatient").html(result);
            $("#xq_getsample_timeSearchFrom").val(today);
            $("#xq_getsample_timeSearchTo").val(today);
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
        url: "/XQ_GetSample/Refresh/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            var date = new Date();
            var today = date.getFullYear() + "-" + ("0" + (date.getMonth() + 1)).slice(-2) + "-" + ("0" + date.getDate()).slice(-2);
            $("#listPatient").html(result);
            $("#xq_getsample_timeSearchFrom").val(today);
            $("#xq_getsample_timeSearchTo").val(today);
        },
        error: function () {
            $("#listPatient").empty();
        }
    });
}

function GetSample_Search() {
    var xq_getsample_pidorseq = $("#xq_getsample_pidorseq").val();
    var timeSearchFrom = $("#xq_getsample_timeSearchFrom").val();
    var timeSearchTo = $("#xq_getsample_timeSearchTo").val();
    $.ajax({
        url: "/XQ_GetSample/Search?" + "pidorseq=" + xq_getsample_pidorseq + "&timeSearchFrom=" + timeSearchFrom + "&timeSearchTo=" + timeSearchTo,
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
$("#xq_getsample_savepatient").hide();
$("#xq_getsample_cancelpatient").hide();
function GetSample_HideButton(_new, _save, _delete, _cancel, _addservice, _getsample) {
    if (_new == 1) {
        $("#xq_getsample_newpatient").hide();
    }
    else {
        $("#xq_getsample_newpatient").show();
    }

    if (_save == 1) {
        $("#xq_getsample_savepatient").hide();
    }
    else {
        $("#xq_getsample_savepatient").show();
    }

    if (_delete == 1) {
        $("#xq_getsample_deletepatient").hide();
    }
    else {
        $("#xq_getsample_deletepatient").show();
    }

    if (_cancel == 1) {
        $("#xq_getsample_cancelpatient").hide();
    }
    else {
        $("#xq_getsample_cancelpatient").show();
    }

    if (_addservice == 1) {
        $("#xq_getsample_addService").hide();
    }
    else {
        $("#xq_getsample_addService").show();
    }

    if (_getsample == 1) {
        $("#xq_getsample_processresult").hide();
    }
    else {
        $("#xq_getsample_processresult").show();
    }
}


function GetSample_GetPatientInfo(id) {
    GetSample_HideButton(false, true, false, true, false, false);
    $.ajax({
        url: "/XQ_GetSample/GetPatientInfo?id=" + id,
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
        url: "/XQ_GetSample/GetServiceForPatient?patientId=" + id,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#xq-right-service-gridview").html(result);
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
    $("#xq_getsample_pidorseq").val('');
    $('#xq_getsample_id').val('');
    $('#xq_getsample_patientId').val('');
    $('#xq_getsample_seq').val('');
    $('#xq_getsample_sid').val('');
    $('#xq_getsample_patientName').val('');
    $('#xq_getsample_age').val('');
    $('#xq_getsample_sex').val('');
    $('#xq_getsample_obj').val('');
    $('#xq_getsample_type').val('');
    $('#xq_getsample_location').val('');
    $('#xq_getsample_doctor').val('');
    $('#xq_getsample_getSampleTime').val(dateTime);
    $('#xq_getsample_address').val('');
    $('#xq_getsample_diagnostic').val('');
    $('#select-category').val('');
    $('#select-service').val('');
    $('#tbody-gridview-service').empty();
    GetSample_SetSelect2_01(false);
}

function GetSample_NewPatient() {
    $('#xq_getsample_patientId').focus();
    GetSample_HideButton(true, false, true, false, true, true);
    GetSample_ResetInput();
}


function GetSample_DeletePatient() {
    var id = $('#xq_getsample_id').val();
    if (id == '') {
        alert("Vui lòng chọn bệnh nhân!");
    }
    else {
        var choice = confirm("Bạn muốn xoá bệnh nhân và tất cả chỉ định xét nghiệm?");
        if (choice) {
            $.ajax({
                url: "/XQ_GetSample/DeletePatientAndService?id=" + id,
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
    var id = $('#xq_getsample_id').val();
    if (id == '') {
        GetSample_ResetInput()
    }
    else {
        GetSample_GetPatientInfo(id);
    }
}

function GetSample_GetSID(seq) {
    $.ajax({
        url: "/XQ_GetSample/GetSID?seq=" + seq,
        type: "GET",
        dataType: "text",
        cache: false,
        success: function (result) {
            $('#xq_getsample_sid').val(result);
        }
    });
}

function GetSample_SavePatient() {
    var validate = GetSample_ValidateInput('patientInfo');
    if (validate) {
        var id = $('#xq_getsample_id').val();
        var patientId = $('#xq_getsample_patientId').val();
        var seq = $('#xq_getsample_seq').val();
        var sid = $('#xq_getsample_sid').val();
        var patientName = $('#xq_getsample_patientName').val();
        var age = $('#xq_getsample_age').val();
        var sex = $('#xq_getsample_sex').val();
        var obj = $('#xq_getsample_obj').val();
        var type = $('#xq_getsample_type').val();
        var location = $('#xq_getsample_location').val();
        var doctor = $('#xq_getsample_doctor').val();
        var getSampleTime = $('#xq_getsample_getSampleTime').val();
        var address = $('#xq_getsample_address').val();
        var diagnostic = $('#xq_getsample_diagnostic').val();
        var category = $('#select-category').val();
        var service = $('#select-service').val();

        $.ajax({
            url: "/XQ_GetSample/SavePatient?id= " + id + "&&patientId=" + patientId + "&&seq=" + seq + "&&sid=" + sid + "&&patientName=" + patientName + "&&age=" + age + "&&sex=" + sex + "&&obj=" + obj + "&&type=" + type + "&&location=" + location + "&&doctor=" + doctor + "&&getSampleTime=" + getSampleTime + "&&address=" + address + "&&diagnostic=" + diagnostic + "&&service=" + service,
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
    var id = $('#xq_getsample_id').val();
    if (id === "") {
        alert("Vui lòng chọn bệnh nhân!");
    }
    else {
        var getSampleTime = $('#xq_getsample_getSampleTime').val();
        $.ajax({
            url: "/XQ_GetSample/ProcessResult?id= " + id + "&getSampleTime=" + getSampleTime,
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
    var id = $('#xq_getsample_id').val();
    if (id === "") {
        $('#addServiceForm').modal('hide');
        alert("Vui lòng chọn bệnh nhân!");
    }
    else {
        GetSample_SetSelect2_02();
    }
}

function GetSample_DeleteServiceForPatient(idResultCDHA) {
    var patientId = $('#xq_getsample_id').val();
    $.ajax({
        url: "/XQ_GetSample/DeleteServiceForPatient?id=" + idResultCDHA,
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
    var serviceId = $('#xq_getsample_service').val();
    var patientId = $('#xq_getsample_id').val();
    var doctorId = $('#xq_getsample_doctor').val();
    if (serviceId === "" || patientId === "") {
        alert("Chỉ định dịch vụ không thành công. Vui lòng kiểm tra lại !")
    }
    else {
        $.ajax({
            url: "/XQ_GetSample/AddServiceForPatient?patientId=" + patientId + "&&serviceId=" + serviceId + "&&doctorId=" + doctorId,
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
        url: "/XQ_GetSample/Get_Count/",
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
                if (e.id === "xq_process_userReturnResultXQ") {
                    $('#xq_process_userReturnResultXQ').select2('focus');
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
        url: "/XQ_Process/Refresh/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            var date = new Date();
            var today = date.getFullYear() + "-" + ("0" + (date.getMonth() + 1)).slice(-2) + "-" + ("0" + date.getDate()).slice(-2);
            $("#process_listPatient").html(result);
            $("#xq_getsample_timeSearchFrom").val(today);
            $("#xq_getsample_timeSearchTo").val(today);
            Process_ResetInput();
            Process_Get_Count();
        },
        error: function () {
            $("#process_listPatient").empty();
        }
    });
}

function Process_Search() {
    var xq_getsample_pidorseq = $("#xq_getsample_pidorseq").val();
    var timeSearchFrom = $("#xq_getsample_timeSearchFrom").val();
    var timeSearchTo = $("#xq_getsample_timeSearchTo").val();
    $.ajax({
        url: "/XQ_Process/Search?" + "pidorseq=" + xq_getsample_pidorseq + "&timeSearchFrom=" + timeSearchFrom + "&timeSearchTo=" + timeSearchTo,
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
    $("#xq_process_pidorseq").val('');
    $('#xq_process_id').val('');
    $('#xq_process_patientId').val('');
    $('#xq_process_seq').val('');
    $('#xq_process_sid').val('');
    $('#xq_process_patientName').val('');
    $('#xq_process_age').val('');
    $('#xq_process_sex').val('');
    $('#xq_process_obj').val('');
    $('#xq_process_type').val('');
    $('#xq_process_location').val('');
    $('#xq_process_doctor').val('');
    $('#xq_process_getSampleTime').val('');
    $('#xq_process_returnResultTime').val('');
    $('#xq_process_location').val('');
    $('#xq_process_doctor').val('');
    $('#xq_process_userReturnResultXQ').val('');
    $('#xq_process_address').val('');
    $('#xq_process_diagnostic').val('');
    $('#tbody-gridview-service').empty();
    $('#imageCDHA').empty();
    $('#xq_process_result').empty();
    $('#xq_process_suggest').empty();
    CKEDITOR.instances["xq_process_description"].setData("");
    Process_SetSelect2_03(true);
}

function Process_GetPatientInfo(id) {
    $.ajax({
        url: "/XQ_Process/Check_SelectDevice/",
        type: 'GET',
        dataType: 'text',
        success: function (result) {
            if (result === "False") {
                $('#addDeviceForm').modal('show');
            } else {
                $.ajax({
                    url: "/XQ_Process/GetPatientInfo?id=" + id,
                    type: "GET",
                    dataType: "html",
                    cache: false,
                    success: function (result) {
                        $("#process_patientInfo").html(result);
                        Process_SetSelect2_03();
                        // Auto-chọn bác sĩ = user đang login (nếu chưa có giá trị)
                        var loginId = $("#xq_process_userLoginId").val();
                        var $sel = $("#xq_process_userReturnResultXQ");
                        if (loginId && (!$sel.val() || $sel.val() === "")) {
                            $sel.val(loginId).trigger("change");
                        }

                        // Gắn handler đổi bác sĩ
                        $(document)
                            .off("change", "#xq_process_userReturnResultXQ")
                            .on("change", "#xq_process_userReturnResultXQ", Process_Load_SelectedDoctorInfo);
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
        }
    })
}

function Process_GetListServiceForPatient(id) {
    $.ajax({
        url: "/XQ_Process/GetServiceForPatient?patientId=" + id,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#process_xq-right-service-gridview").html(result);
        },
        error: function () {
            $('#tbody-gridview-service').empty();
        }
    });
}


function Process_SaveResult() {
    var patientId = $('#xq_process_id').val();
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
                var returnResultTime = $('#xq_process_returnResultTime').val();
                var userReturnResult = $('#xq_process_userReturnResultXQ').val();
                var description = CKEDITOR.instances['xq_process_description'].getData();
                var result = $('#xq_process_result').val();
                var suggest = $('#xq_process_suggest').val();

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
                    url: "/XQ_Process/SaveResult/",
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

function Process_SelectDevice() {
    var deviceId = $('#xq_getsample_select_device_select').val();
    if (deviceId === "") {
        alert("Vui lòng chọn thiết bị !")
    }
    else {
        $.ajax({
            url: "/XQ_Process/SelectDevice?deviceId=" + deviceId,
            type: 'GET',
            dataType: 'text',
            success: function (result) {
                if (result === 'False') {
                    alert("Không thể chọn thiết bị. Kiểm tra lại !");
                }
                else {
                    Process_ValidPrint();
                }
            },
            error: function () {
                alert("Không thể chọn thiết bị. Kiểm tra lại !");
            }
        });
    }
}


function Process_ValidPrint() {
    $.ajax({
        url: "/XQ_Process/Check_SelectDevice/",
        type: 'GET',
        dataType: 'text',
        success: function (result) {
            if (result === "False") {
                $('#addDeviceForm').modal('show');
            }
            else {
                var patientId = $('#xq_process_id').val();
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
                            var returnResultTime = $('#xq_process_returnResultTime').val();
                            var userReturnResult = $('#xq_process_userReturnResultXQ').val();
                            var description = CKEDITOR.instances['xq_process_description'].getData();
                            var result = $('#xq_process_result').val();
                            var suggest = $('#xq_process_suggest').val();

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
                                url: "/XQ_Process/ValidPrint/",
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
        }
    });
}


function Process_GetSample() {
    var id = $('#xq_process_id').val();
    if (id === "") {
        alert("Vui lòng chọn bệnh nhân!");
    }
    else {
        $.ajax({
            url: "/XQ_Process/GetSample?id= " + id,
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
        url: "/XQ_Process/Get_Count/",
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
                Process_LoadImageForService(id);
            }
            else {
                $(this).prop("checked", false);
            }
        })

    })
}

function Process_LoadImageForService(id) {
    CKEDITOR.instances["xq_process_description"].setData("");
    $("#xq_process_result").val("");
    $("#xq_process_suggest").val("");
    $.ajax({
        url: "/XQ_Process/Get_Description_Result_Suggest_ForService?id=" + id,
        type: 'GET',
        dataType: 'json',
        success: function (response) {
            $.each(response, function (key, value) {
                CKEDITOR.instances["xq_process_description"].setData(value.description);
                $("#xq_process_result").val(value.result);
                $("#xq_process_suggest").val(value.suggest);
            });
        }
    });
}

function GetSampleForService(id) {
    $.ajax({
        url: "/XQ_Process/GetSampleForService?id=" + id,
        type: 'GET',
        dataType: 'json',
        success: function (response) {
            CKEDITOR.instances["xq_process_description"].setData(response.description);
            $("#xq_process_result").val(response.result);
            $("#xq_process_suggest").val(response.suggest);
        }
    });
}

// Tải thông tin bác sĩ khi đổi select
function Process_Load_SelectedDoctorInfo() {
    var userId = $("#xq_process_userReturnResultXQ").val();
    if (!userId) {
        $("#xq_process_signerCCCD").empty();
        return;
    }
    $.ajax({
        url: "/XQ_Process/GetUserInfo?userId=" + userId,
        type: "GET",
        dataType: "json",
        success: function (u) {
            if (!u) {
                $("#xq_process_signerCCCD").text("Không lấy được thông tin bác sĩ.");
                return;
            }
            // Hiển thị nhẹ ở UI
            var cccd = (u.cccd || "");
            $("#xq_process_signerCCCD").val(cccd);
        },
        error: function () {
            $("#xq_process_signerCCCD").text("Không lấy được thông tin bác sĩ.");
        }
    });
}

// ========================== KÝ SỐ PDF (Form: XQUANG) ==========================
function Process_SignPdf() {
    var patientId = $('#xq_process_id').val();
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
    var signerCCCD = $('#xq_process_signerCCCD').val();
    if (!signerCCCD || $.trim(signerCCCD) === "") {
        alert("Vui lòng nhập CCCD người ký (Viettel MySign)!");
        $('#xq_process_signerCCCD').focus();
        return;
    }
    console.log(signerCCCD);

    // Lấy context để backend đặt tên file đẹp theo template
    var patientCode = $('#xq_process_patientId').val() || $('#xq_process_sid').val() || "";
    var patientName = $('#xq_process_patientName').val() || "";
    var doctorName = $('#xq_process_userReturnResultXQ option:selected').text() || $('#xq_process_userReturnResultXQ').val() || "";
    var performedAt = $('#xq_process_returnResultTime').val() || ""; // thời điểm trả kết quả
    var serviceCode = ""; // nếu cần, bạn lấy từ label dịch vụ đang chọn

    // 1) Gọi API xuất PDF base64 (tận dụng endpoint ValidPrint đang có)
    //    Nếu bạn có endpoint riêng chỉ "Export" (không đổi trạng thái), thay URL ở đây là tốt nhất.
    $('#showWaitting').modal('show');
    var dataExport = {
        patientId: patientId,
        resultCDHAId: resultCDHAId,
        returnResultTime: $('#xq_process_returnResultTime').val(),
        userReturnResult: $('#xq_process_userReturnResultXQ').val(),
        description: CKEDITOR.instances['xq_process_description'].getData(),
        result: $('#xq_process_result').val(),
        suggest: $('#xq_process_suggest').val()
    };

    $.ajax({
        url: "/XQ_Process/ValidPrint/",
        data: JSON.stringify(dataExport),
        contentType: "application/json; charset=utf-8",
        dataType: "text",
        type: "POST",
        success: function (base64Pdf) {
            if (!base64Pdf) {
                $('#showWaitting').modal('hide');
                alert("Không thể xuất PDF để ký. Vui lòng kiểm tra lại!");
                return;
            }

            // 2) Chuyển base64 -> File để gửi multipart/form-data
            var file = base64ToFile(base64Pdf, "temp.pdf", "application/pdf");
            console.log(file);
            // 3) Chuẩn bị FormData gửi đến API ký số chung (server sẽ tự đặt tên file)
            var formData = new FormData();
            formData.append("signerCCCD", signerCCCD);
            formData.append("file", file, file.name);

            // ---- context để server đặt tên theo template MySign:FileNameTemplates (FormType = SieuAm)
            formData.append("formType", "XQuang");
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
                    // Gỉa sử object trả về từ response như sau
                    //resp = {
                    //    "id": "51626", "provider": "0", "documentId": "null", "documentType": "null", "documentName": "27433568_phieu_danh_gia_ban_dau.pdf", "documentDesc": "null", "documentHash": "7X38RRgfWpYULcvtZc863HnTfocDsSsji023UGoxBLM=", "timestamp": "2025-09-19T07:11:06.763046209Z", "transactionId": "308706a6-5dc7-4f0b-ba08-01448c15a306", "credentialId": "066194014120_7500452_20250821162101", "signatures": "SJpUV2IQVRR3FzMzK5WWLucKNX2jtFc4\/\/+rvi19Izj6yL1epn7xo1cxGhW\/67fI\r\niqLqz104zwke0xbWf9BZPjAEmSWD8Xn2RaouyGCBQIj4tnCnMZQGnPCBZ2laN4Ec\r\n3qjMU3lpoh22OnVBgLYZlQZSSvnNii1MF7v1vKRsdggl4YZpHEiZlgMef7iclkga\r\nMGUryw11teXAg77s15jGKYCjkM49jrJlGq+M5p0IVCj\/ZeUXByd3MsBeWAnWAHs1\r\n\/WoyEfBhwK3YABb37cjO6RNFACj+KJJv8910AyQwZeowyhmeQDdjLdGgfJRILORt\r\nDFZF1iABlsXIbp6pf0ek8w==", "status": "1", "filepath": "null", "url": "null", "docControlId": "null", "docControlType": "null", "error": "null", "login": "066194014120", "taxcode": "6001362081", "metadata": "null", "requesttime": "1758265850532", "contentContentType": "application\/pdf", "signUserId": "066194014120",
                    //}
                    $('#showWaitting').modal('hide');

                    // Controller trả về "response.data" nếu có; tùy MySign:
                    // - Nếu có resp.fileUrl -> mở luôn
                    // - Nếu có resp.base64Pdf -> mở PDF
                    // - Nếu là string -> thử parse
                    try {
                        var obj = (typeof resp === "string") ? JSON.parse(resp) : resp; //obj này sẽ trả về response.data luôn

                        // 1) THU THẬP signStoreId TỪ RESPONSE (ưu tiên data.id)
                        var signStoreId =
                            (obj && obj.data && (obj.data.id || obj.data.signStoreId)) ||
                            obj.id || obj.signStoreId || null;
                        // giả sử signStoreId đang null để test dev nên gán cứng
                        signStoreId = signStoreId || "51402";
                        var keyResult = null;

                        // 2) LƯU signStoreId THEO resultCDHAId ĐANG CHỌN
                        if (signStoreId) {
                            $.ajax({
                                url: "/XQ_Process/SaveSignStoreIdForResultCDHA",
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
                                            referenceType: "XQuang",                // SA / SATIM / XQUANG...
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
                        }

                        // 3) XỬ LÝ HIỂN THỊ FILE ĐÃ KÝ NHƯ CŨ
                        if (obj && obj.fileUrl) {
                            window.open(obj.fileUrl, "_blank");
                            return;
                        }
                        if (obj && (obj.base64Pdf || obj.base64)) {
                            openBase64Pdf(obj.base64Pdf || obj.base64);
                            return;
                        }
                        alert("Đã ký số xong, response:\n" + JSON.stringify(obj));

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
                    var msg = "Ký số thất bại.";
                    if (xhr && xhr.responseText) msg += "\n" + xhr.responseText;
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
        var blob = new Blob([byteArray], { type: mime || 'application/pdf' });

        // Tên tạm – server sẽ tự đặt lại theo template nên không quan trọng
        var file = new File([blob], filename || "document.pdf", { type: mime || 'application/pdf' });
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
        url: "/XQ_ReturnResult/Refresh/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            var date = new Date();
            var today = date.getFullYear() + "-" + ("0" + (date.getMonth() + 1)).slice(-2) + "-" + ("0" + date.getDate()).slice(-2);
            $("#returnresult_listPatient").html(result);
            $("#xq_getsample_timeSearchFrom").val(today);
            $("#xq_getsample_timeSearchTo").val(today);
            ReturnResult_ResetInput();
            ReturnResult_Get_Count();
        },
        error: function () {
            $("#returnresult_listPatient").empty();
        }
    });
}

function ReturnResult_Search() {
    var xq_getsample_pidorseq = $("#xq_returnresult_pidorseq").val();
    var timeSearchFrom = $("#xq_returnresult_timeSearchFrom").val();
    var timeSearchTo = $("#xq_returnresult_timeSearchTo").val();
    $.ajax({
        url: "/XQ_ReturnResult/Search?" + "pidorseq=" + xq_getsample_pidorseq + "&timeSearchFrom=" + timeSearchFrom + "&timeSearchTo=" + timeSearchTo,
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
    $("#xq_returnresult_pidorseq").val('');
    $('#xq_returnresult_id').val('');
    $('#xq_returnresult_patientId').val('');
    $('#xq_returnresult_seq').val('');
    $('#xq_returnresult_sid').val('');
    $('#xq_returnresult_patientName').val('');
    $('#xq_returnresult_age').val('');
    $('#xq_returnresult_sex').val('');
    $('#xq_returnresult_obj').val('');
    $('#xq_returnresult_type').val('');
    $('#xq_returnresult_location').val('');
    $('#xq_returnresult_doctor').val('');
    $('#xq_returnresult_getSampleTime').val('');
    $('#xq_returnresult_returnResultTime').val('');
    $('#xq_returnresult_location').val('');
    $('#xq_returnresult_doctor').val('');
    $('#xq_returnresult_userReturnResultXQ').val('');
    $('#xq_returnresult_address').val('');
    $('#xq_returnresult_diagnostic').val('');
    $('#tbody-gridview-service').empty();
}

function ReturnResult_GetPatientInfo(id) {
    $.ajax({
        url: "/XQ_ReturnResult/GetPatientInfo?id=" + id,
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
        url: "/XQ_ReturnResult/GetServiceForPatient?patientId=" + id,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#returnresult_xq-right-service-gridview").html(result);
        },
        error: function () {
            $('#tbody-gridview-service').empty();
        }
    });
}

function ReturnResult_Invalid() {
    var id = $('#xq_returnresult_id').val();
    if (id === "") {
        alert("Vui lòng chọn bệnh nhân!");
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
    if (confirm('Bạn muốn InValid kết quả của bệnh nhân (hủy Ký Số kết quả này) ?')) {
        $.ajax({
            url: "/XQ_ReturnResult/Invalid?id= " + id,
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
        url: "/XQ_ReturnResult/Get_Count/",
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
    var patientId = $('#xq_returnresult_id').val();
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
                url: "/XQ_ReturnResult/Print?resultCDHAId=" + resultCDHAId,
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
    CKEDITOR.instances["xq_returnresult_description"].setData("");
    $("#xq_returnresult_result").val("");
    $("#xq_returnresult_suggest").val("");
    $.ajax({
        url: "/XQ_ReturnResult/Get_Description_Result_Suggest_ForService?id=" + id,
        type: 'GET',
        dataType: 'json',
        success: function (response) {
            $.each(response, function (key, value) {
                CKEDITOR.instances["xq_returnresult_description"].setData(value.description);
                $("#xq_returnresult_result").val(value.result);
                $("#xq_returnresult_suggest").val(value.suggest);
            });
        }
    });
}

function ReturnResult_ViewSignedPdf() {

    function openSigned(id) {
        if (!id) { alert("Chưa có signStoreId cho dịch vụ này!"); return; }
        var url = "/api/ExternalSign/view-signed/" + encodeURIComponent(id);
        window.open(url, "_blank");
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
        url: "/XQ_ReturnResult/GetSignStoreId",
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
