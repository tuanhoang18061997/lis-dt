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
        $('#ddt_getsample_service').select2({
            dropdownParent: $('#addServiceForm'),
            placeholder: "-- Chọn dịch vụ --"
        });
    });
    $('#ddt_getsample_service').val('');
}

function GetSample_SetSelect2_01(isLoadPage) {
    if (isLoadPage) {
        $('#ddt_getsample_location').val("");
    }
    $(document).ready(function () {
        $('#ddt_getsample_location').select2({
            placeholder: "-- Chọn --"
        });
    });   

    if (isLoadPage) {
        $('#ddt_getsample_doctor').val("");
    }
    $(document).ready(function () {
        $('#ddt_getsample_doctor').select2({
            placeholder: "-- Chọn --"
        });
    });


    if (isLoadPage) {
        $('#ddt_process_sampleresult').val("");
    }
    $(document).ready(function () {
        $('#ddt_process_sampleresult').select2({
            placeholder: "-- Chọn --"
        });
    });
}

function Process_SetSelect2_03() {
    $(document).ready(function () {
        $('#ddt_process_userReturnResultDDT').select2({
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
                if (e.id === "ddt_getsample_location") {
                    $('#ddt_getsample_location').select2('focus');
                }
                else if (e.id === "ddt_getsample_doctor") {
                    $('#ddt_getsample_doctor').select2('focus');
                }
                else if (e.id === "ddt_getsample_userReturnResultDDT") {
                    $('#ddt_getsample_userReturnResultDDT').select2('focus');
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
        url: "/DDT_GetSample/Refresh/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            var date = new Date();
            var today = date.getFullYear() + "-" + ("0" + (date.getMonth() + 1)).slice(-2) + "-" + ("0" + date.getDate()).slice(-2);
            $("#listPatient").html(result);
            $("#ddt_getsample_timeSearchFrom").val(today);
            $("#ddt_getsample_timeSearchTo").val(today);
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
        url: "/DDT_GetSample/Refresh/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            var date = new Date();
            var today = date.getFullYear() + "-" + ("0" + (date.getMonth() + 1)).slice(-2) + "-" + ("0" + date.getDate()).slice(-2);
            $("#listPatient").html(result);
            $("#ddt_getsample_timeSearchFrom").val(today);
            $("#ddt_getsample_timeSearchTo").val(today);
        },
        error: function () {
            $("#listPatient").empty();
        }
    });
}

function GetSample_Search() {
    var ddt_getsample_pidorseq = $("#ddt_getsample_pidorseq").val();
    var timeSearchFrom = $("#ddt_getsample_timeSearchFrom").val();
    var timeSearchTo = $("#ddt_getsample_timeSearchTo").val();
    $.ajax({
        url: "/DDT_GetSample/Search?" + "pidorseq=" + ddt_getsample_pidorseq + "&timeSearchFrom=" + timeSearchFrom + "&timeSearchTo=" + timeSearchTo,
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
$("#ddt_getsample_savepatient").hide();
$("#ddt_getsample_cancelpatient").hide();
function GetSample_HideButton(_new, _save, _delete, _cancel, _addservice, _getsample) {
    if (_new == 1) {
        $("#ddt_getsample_newpatient").hide();
    }
    else {
        $("#ddt_getsample_newpatient").show();
    }

    if (_save == 1) {
        $("#ddt_getsample_savepatient").hide();
    }
    else {
        $("#ddt_getsample_savepatient").show();
    }

    if (_delete == 1) {
        $("#ddt_getsample_deletepatient").hide();
    }
    else {
        $("#ddt_getsample_deletepatient").show();
    }

    if (_cancel == 1) {
        $("#ddt_getsample_cancelpatient").hide();
    }
    else {
        $("#ddt_getsample_cancelpatient").show();
    }

    if (_addservice == 1) {
        $("#ddt_getsample_addService").hide();
    }
    else {
        $("#ddt_getsample_addService").show();
    }

    if (_getsample == 1) {
        $("#ddt_getsample_processresult").hide();
    }
    else {
        $("#ddt_getsample_processresult").show();
    }
}


function GetSample_GetPatientInfo(id) {
    GetSample_HideButton(false, true, false, true, false, false);
    $.ajax({
        url: "/DDT_GetSample/GetPatientInfo?id=" + id,
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
        url: "/DDT_GetSample/GetServiceForPatient?patientId=" + id,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#ddt-right-service-gridview").html(result);
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
    $("#ddt_getsample_pidorseq").val('');
    $('#ddt_getsample_id').val('');
    $('#ddt_getsample_patientId').val('');
    $('#ddt_getsample_seq').val('');
    $('#ddt_getsample_sid').val('');
    $('#ddt_getsample_patientName').val('');
    $('#ddt_getsample_age').val('');
    $('#ddt_getsample_sex').val('');
    $('#ddt_getsample_obj').val('');
    $('#ddt_getsample_type').val('');
    $('#ddt_getsample_location').val('');
    $('#ddt_getsample_doctor').val('');
    $('#ddt_getsample_getSampleTime').val(dateTime);
    $('#ddt_getsample_address').val('');
    $('#ddt_getsample_diagnostic').val('');
    $('#select-category').val('');
    $('#select-service').val('');
    $('#tbody-gridview-service').empty();
    GetSample_SetSelect2_01(false);
}

function GetSample_NewPatient() {
    $('#ddt_getsample_patientId').focus();
    GetSample_HideButton(true, false, true, false, true, true);
    GetSample_ResetInput();
}


function GetSample_DeletePatient() {
    var id = $('#ddt_getsample_id').val();
    if (id == '') {
        alert("Vui lòng chọn bệnh nhân!");
    }
    else {
        var choice = confirm("Bạn muốn xoá bệnh nhân và tất cả chỉ định xét nghiệm?");
        if (choice) {
            $.ajax({
                url: "/DDT_GetSample/DeletePatientAndService?id=" + id,
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
    var id = $('#ddt_getsample_id').val();
    if (id == '') {
        GetSample_ResetInput()
    }
    else {
        GetSample_GetPatientInfo(id);
    }
}

function GetSample_GetSID(seq) {
    $.ajax({
        url: "/DDT_GetSample/GetSID?seq=" + seq,
        type: "GET",
        dataType: "text",
        cache: false,
        success: function (result) {
            $('#ddt_getsample_sid').val(result);
        }
    });
}

function GetSample_SavePatient() {
    var validate = GetSample_ValidateInput('patientInfo');
    if (validate) {
        var id = $('#ddt_getsample_id').val();
        var patientId = $('#ddt_getsample_patientId').val();
        var seq = $('#ddt_getsample_seq').val();
        var sid = $('#ddt_getsample_sid').val();
        var patientName = $('#ddt_getsample_patientName').val();
        var age = $('#ddt_getsample_age').val();
        var sex = $('#ddt_getsample_sex').val();
        var obj = $('#ddt_getsample_obj').val();
        var type = $('#ddt_getsample_type').val();
        var location = $('#ddt_getsample_location').val();
        var doctor = $('#ddt_getsample_doctor').val();
        var getSampleTime = $('#ddt_getsample_getSampleTime').val();
        var address = $('#ddt_getsample_address').val();
        var diagnostic = $('#ddt_getsample_diagnostic').val();
        var category = $('#select-category').val();
        var service = $('#select-service').val();

        $.ajax({
            url: "/DDT_GetSample/SavePatient?id= " + id + "&&patientId=" + patientId + "&&seq=" + seq + "&&sid=" + sid + "&&patientName=" + patientName + "&&age=" + age + "&&sex=" + sex + "&&obj=" + obj + "&&type=" + type + "&&location=" + location + "&&doctor=" + doctor + "&&getSampleTime=" + getSampleTime + "&&address=" + address + "&&diagnostic=" + diagnostic + "&&service=" + service,
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
    var id = $('#ddt_getsample_id').val();
    if (id === "") {
        alert("Vui lòng chọn bệnh nhân!");
    }
    else {
        var getSampleTime = $('#ddt_getsample_getSampleTime').val();
        $.ajax({
            url: "/DDT_GetSample/ProcessResult?id= " + id + "&getSampleTime=" + getSampleTime,
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
    var id = $('#ddt_getsample_id').val();
    if (id === "") {
        $('#addServiceForm').modal('hide');
        alert("Vui lòng chọn bệnh nhân!");
    }
    else {
        GetSample_SetSelect2_02();
    }
}

function GetSample_DeleteServiceForPatient(idResultCDHA) {
    var patientId = $('#ddt_getsample_id').val();
    $.ajax({
        url: "/DDT_GetSample/DeleteServiceForPatient?id=" + idResultCDHA,
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
    var serviceId = $('#ddt_getsample_service').val();
    var patientId = $('#ddt_getsample_id').val();
    var doctorId = $('#ddt_getsample_doctor').val();
    if (serviceId === "" || patientId === "") {
        alert("Chỉ định dịch vụ không thành công. Vui lòng kiểm tra lại !")
    }
    else {
        $.ajax({
            url: "/DDT_GetSample/AddServiceForPatient?patientId=" + patientId + "&&serviceId=" + serviceId + "&&doctorId=" + doctorId,
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
        url: "/DDT_GetSample/Get_Count/",
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
                if (e.id === "ddt_process_userReturnResultDDT") {
                    $('#ddt_process_userReturnResultDDT').select2('focus');
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
        url: "/DDT_Process/Refresh/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            var date = new Date();
            var today = date.getFullYear() + "-" + ("0" + (date.getMonth() + 1)).slice(-2) + "-" + ("0" + date.getDate()).slice(-2);
            $("#process_listPatient").html(result);
            $("#ddt_getsample_timeSearchFrom").val(today);
            $("#ddt_getsample_timeSearchTo").val(today);
            Process_ResetInput();
            Process_Get_Count();
        },
        error: function () {
            $("#process_listPatient").empty();
        }
    });
}

function Process_Search() {
    var ddt_getsample_pidorseq = $("#ddt_getsample_pidorseq").val();
    var timeSearchFrom = $("#ddt_getsample_timeSearchFrom").val();
    var timeSearchTo = $("#ddt_getsample_timeSearchTo").val();
    $.ajax({
        url: "/DDT_Process/Search?" + "pidorseq=" + ddt_getsample_pidorseq + "&timeSearchFrom=" + timeSearchFrom + "&timeSearchTo=" + timeSearchTo,
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
    $("#ddt_process_pidorseq").val('');
    $('#ddt_process_id').val('');
    $('#ddt_process_patientId').val('');
    $('#ddt_process_seq').val('');
    $('#ddt_process_sid').val('');
    $('#ddt_process_patientName').val('');
    $('#ddt_process_age').val('');
    $('#ddt_process_sex').val('');
    $('#ddt_process_obj').val('');
    $('#ddt_process_type').val('');
    $('#ddt_process_location').val('');
    $('#ddt_process_doctor').val('');
    $('#ddt_process_getSampleTime').val('');
    $('#ddt_process_returnResultTime').val('');
    $('#ddt_process_location').val('');
    $('#ddt_process_doctor').val('');
    $('#ddt_process_userReturnResultDDT').val('');
    $('#ddt_process_address').val('');
    $('#ddt_process_diagnostic').val('');
    $('#tbody-gridview-service').empty();
    $('#imageCDHA').empty();
    $('#ddt_process_result').empty();
    $('#ddt_process_suggest').empty();
    CKEDITOR.instances["ddt_process_description"].setData("");
    Process_SetSelect2_03(true);
}

function Process_GetPatientInfo(id) {
    $.ajax({
        url: "/DDT_Process/GetPatientInfo?id=" + id,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#process_patientInfo").html(result);
            Process_SetSelect2_03();
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
        url: "/DDT_Process/GetServiceForPatient?patientId=" + id,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#process_ddt-right-service-gridview").html(result);
        },
        error: function () {
            $('#tbody-gridview-service').empty();
        }
    });
}


function Process_SaveResult() {
    var patientId = $('#ddt_process_id').val();
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
                var returnResultTime = $('#ddt_process_returnResultTime').val();
                var userReturnResult = $('#ddt_process_userReturnResultDDT').val();
                var description = CKEDITOR.instances['ddt_process_description'].getData();
                var result = $('#ddt_process_result').val();
                var suggest = $('#ddt_process_suggest').val();

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
                    url: "/DDT_Process/SaveResult/",
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
    var deviceId = $('#ddt_getsample_select_device_select').val();
    if (deviceId === "") {
        alert("Vui lòng chọn thiết bị !")
    }
    else {
        $.ajax({
            url: "/DDT_Process/SelectDevice?deviceId=" + deviceId,
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
        url: "/DDT_Process/Check_SelectDevice/",
        type: 'GET',
        dataType: 'text',
        success: function (result) {
            if (result === "False") {
                $('#addDeviceForm').modal('show');
            }
            else {
                var patientId = $('#ddt_process_id').val();
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
                            var returnResultTime = $('#ddt_process_returnResultTime').val();
                            var userReturnResult = $('#ddt_process_userReturnResultDDT').val();
                            var description = CKEDITOR.instances['ddt_process_description'].getData();
                            var result = $('#ddt_process_result').val();
                            var suggest = $('#ddt_process_suggest').val();

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
                                url: "/DDT_Process/ValidPrint/",
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
    var id = $('#ddt_process_id').val();
    if (id === "") {
        alert("Vui lòng chọn bệnh nhân!");
    }
    else {
        $.ajax({
            url: "/DDT_Process/GetSample?id= " + id,
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
        url: "/DDT_Process/Get_Count/",
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
    CKEDITOR.instances["ddt_process_description"].setData("");
    $("#ddt_process_result").val("");
    $("#ddt_process_suggest").val("");
    $.ajax({
        url: "/DDT_Process/Get_Description_Result_Suggest_ForService?id=" + id,
        type: 'GET',
        dataType: 'json',
        success: function (response) {
            $.each(response, function (key, value) {
                CKEDITOR.instances["ddt_process_description"].setData(value.description);
                $("#ddt_process_result").val(value.result);
                $("#ddt_process_suggest").val(value.suggest);
            });
        }
    });
}

function GetSampleForService(id) {
    $.ajax({
        url: "/DDT_Process/GetSampleForService?id=" + id,
        type: 'GET',
        dataType: 'json',
        success: function (response) {
            CKEDITOR.instances["ddt_process_description"].setData(response.description);
            $("#ddt_process_result").val(response.result);
            $("#ddt_process_suggest").val(response.suggest);
        }
    });
}


//***************************************************************************************** Return Result

function ReturnResult_Refresh() {
    $.ajax({
        url: "/DDT_ReturnResult/Refresh/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            var date = new Date();
            var today = date.getFullYear() + "-" + ("0" + (date.getMonth() + 1)).slice(-2) + "-" + ("0" + date.getDate()).slice(-2);
            $("#returnresult_listPatient").html(result);
            $("#ddt_getsample_timeSearchFrom").val(today);
            $("#ddt_getsample_timeSearchTo").val(today);
            ReturnResult_ResetInput();
            ReturnResult_Get_Count();
        },
        error: function () {
            $("#returnresult_listPatient").empty();
        }
    });
}

function ReturnResult_Search() {
    var ddt_getsample_pidorseq = $("#ddt_returnresult_pidorseq").val();
    var timeSearchFrom = $("#ddt_returnresult_timeSearchFrom").val();
    var timeSearchTo = $("#ddt_returnresult_timeSearchTo").val();
    $.ajax({
        url: "/DDT_ReturnResult/Search?" + "pidorseq=" + ddt_getsample_pidorseq + "&timeSearchFrom=" + timeSearchFrom + "&timeSearchTo=" + timeSearchTo,
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
    $("#ddt_returnresult_pidorseq").val('');
    $('#ddt_returnresult_id').val('');
    $('#ddt_returnresult_patientId').val('');
    $('#ddt_returnresult_seq').val('');
    $('#ddt_returnresult_sid').val('');
    $('#ddt_returnresult_patientName').val('');
    $('#ddt_returnresult_age').val('');
    $('#ddt_returnresult_sex').val('');
    $('#ddt_returnresult_obj').val('');
    $('#ddt_returnresult_type').val('');
    $('#ddt_returnresult_location').val('');
    $('#ddt_returnresult_doctor').val('');
    $('#ddt_returnresult_getSampleTime').val('');
    $('#ddt_returnresult_returnResultTime').val('');
    $('#ddt_returnresult_location').val('');
    $('#ddt_returnresult_doctor').val('');
    $('#ddt_returnresult_userReturnResultDDT').val('');
    $('#ddt_returnresult_address').val('');
    $('#ddt_returnresult_diagnostic').val('');
    $('#tbody-gridview-service').empty();
}

function ReturnResult_GetPatientInfo(id) {
    $.ajax({
        url: "/DDT_ReturnResult/GetPatientInfo?id=" + id,
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
        url: "/DDT_ReturnResult/GetServiceForPatient?patientId=" + id,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#returnresult_ddt-right-service-gridview").html(result);
        },
        error: function () {
            $('#tbody-gridview-service').empty();
        }
    });
}

function ReturnResult_Invalid() {
    var id = $('#ddt_returnresult_id').val();
    if (id === "") {
        alert("Vui lòng chọn bệnh nhân!");
    }
    else {
        if (confirm('Bạn muốn InValid kết quả của bệnh nhân ?')) {
            $.ajax({
                url: "/DDT_ReturnResult/Invalid?id= " + id,
                type: 'POST',
                dataType: 'text',
                success: function (result) {
                    if (result === 'True') {
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
}

function ReturnResult_Get_Count() {
    $.ajax({
        url: "/DDT_ReturnResult/Get_Count/",
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
    var patientId = $('#ddt_returnresult_id').val();
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
                url: "/DDT_ReturnResult/Print?resultCDHAId=" + resultCDHAId,
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
    CKEDITOR.instances["ddt_returnresult_description"].setData("");
    $("#ddt_returnresult_result").val("");
    $("#ddt_returnresult_suggest").val("");
    $.ajax({
        url: "/DDT_ReturnResult/Get_Description_Result_Suggest_ForService?id=" + id,
        type: 'GET',
        dataType: 'json',
        success: function (response) {
            $.each(response, function (key, value) {
                CKEDITOR.instances["ddt_returnresult_description"].setData(value.description);
                $("#ddt_returnresult_result").val(value.result);
                $("#ddt_returnresult_suggest").val(value.suggest);
            });
        }
    });
}

