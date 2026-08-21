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
        $('#xn_getsample_category').select2({
            dropdownParent: $('#addServiceForm'),
            placeholder: "-- Chọn danh mục --"
        });
    });
    $(document).ready(function () {
        $('#xn_getsample_service').select2({
            dropdownParent: $('#addServiceForm'),
            placeholder: "-- Chọn dịch vụ --"
        });
    });
    $('#xn_getsample_category').val('');
    $('#xn_getsample_service').val('');
}

function GetSample_SetSelect2_01(isLoadPage) {
    if (isLoadPage) {
        $('#xn_getsample_location').val("");
    }
    $(document).ready(function () {
        $('#xn_getsample_location').select2({
            placeholder: "-- Chọn --"
        });
    });

    if (isLoadPage) {
        $('#xn_getsample_doctor').val("");
    }
    $(document).ready(function () {
        $('#xn_getsample_doctor').select2({
            placeholder: "-- Chọn --"
        });
    });
}

function Process_SetSelect2_03() {
    $(document).ready(function () {
        $('#xn_process_userReturnResultXN').select2({
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
                if (e.id === "xn_getsample_location") {
                    $('#xn_getsample_location').select2('focus');
                }
                else if (e.id === "xn_getsample_doctor") {
                    $('#xn_getsample_doctor').select2('focus');
                }
                else if (e.id === "xn_getsample_userReturnResultXN") {
                    $('#xn_getsample_userReturnResultXN').select2('focus');
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
        url: "/XN_GetSample/Refresh/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            var date = new Date();
            var today = date.getFullYear() + "-" + ("0" + (date.getMonth() + 1)).slice(-2) + "-" + ("0" + date.getDate()).slice(-2);
            $("#listPatient").html(result);
            $("#xn_getsample_timeSearchFrom").val(today);
            $("#xn_getsample_timeSearchTo").val(today);
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
        url: "/XN_GetSample/Refresh/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            var date = new Date();
            var today = date.getFullYear() + "-" + ("0" + (date.getMonth() + 1)).slice(-2) + "-" + ("0" + date.getDate()).slice(-2);
            $("#listPatient").html(result);
            $("#xn_getsample_timeSearchFrom").val(today);
            $("#xn_getsample_timeSearchTo").val(today);
        },
        error: function () {
            $("#listPatient").empty();
        }
    });
}

function GetSample_Search() {
    var xn_getsample_pidorseq = $("#xn_getsample_pidorseq").val();
    var timeSearchFrom = $("#xn_getsample_timeSearchFrom").val();
    var timeSearchTo = $("#xn_getsample_timeSearchTo").val();
    $.ajax({
        url: "/XN_GetSample/Search?" + "pidorseq=" + xn_getsample_pidorseq + "&timeSearchFrom=" + timeSearchFrom + "&timeSearchTo=" + timeSearchTo,
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
$("#xn_getsample_savepatient").hide();
$("#xn_getsample_cancelpatient").hide();
function GetSample_HideButton(_new, _save, _delete, _cancel, _addservice, _getsample) {
    if (_new == 1) {
        $("#xn_getsample_newpatient").hide();
    }
    else {
        $("#xn_getsample_newpatient").show();
    }

    if (_save == 1) {
        $("#xn_getsample_savepatient").hide();
    }
    else {
        $("#xn_getsample_savepatient").show();
    }

    if (_delete == 1) {
        $("#xn_getsample_deletepatient").hide();
    }
    else {
        $("#xn_getsample_deletepatient").show();
    }

    if (_cancel == 1) {
        $("#xn_getsample_cancelpatient").hide();
    }
    else {
        $("#xn_getsample_cancelpatient").show();
    }

    if (_addservice == 1) {
        $("#xn_getsample_addService").hide();
    }
    else {
        $("#xn_getsample_addService").show();
    }

    if (_getsample == 1) {
        $("#xn_getsample_processresult").hide();
    }
    else {
        $("#xn_getsample_processresult").show();
    }
}


function GetSample_GetPatientInfo(id) {
    GetSample_HideButton(false, true, false, true, false, false);
    $.ajax({
        url: "/XN_GetSample/GetPatientInfo?id=" + id,
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
        url: "/XN_GetSample/GetServiceForPatient?patientId=" + id,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#xn-right-service-gridview").html(result);
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
    $("#xn_getsample_pidorseq").val('');
    $('#xn_getsample_id').val('');
    $('#xn_getsample_patientId').val('');
    $('#xn_getsample_seq').val('');
    $('#xn_getsample_sid').val('');
    $('#xn_getsample_patientName').val('');
    $('#xn_getsample_age').val('');
    $('#xn_getsample_sex').val('');
    $('#xn_getsample_obj').val('');
    $('#xn_getsample_type').val('');
    $('#xn_getsample_location').val('');
    $('#xn_getsample_doctor').val('');
    $('#xn_getsample_getSampleTime').val(dateTime);
    $('#xn_getsample_address').val('');
    $('#xn_getsample_diagnostic').val('');
    $('#xn_getsample_hospital').val('');
    $('#select-category').val('');
    $('#select-service').val('');
    $('#tbody-gridview-service').empty();
    GetSample_SetSelect2_01(false);
}

function GetSample_NewPatient() {
    $('#xn_getsample_patientId').focus();
    GetSample_HideButton(true, false, true, false, true, true);
    GetSample_ResetInput();
}


function GetSample_DeletePatient() {
    var id = $('#xn_getsample_id').val();
    if (id == '') {
        alert("Vui lòng chọn bệnh nhân!");
    }
    else {
        var choice = confirm("Bạn muốn xoá bệnh nhân và tất cả chỉ định xét nghiệm?");
        if (choice) {
            $.ajax({
                url: "/XN_GetSample/DeletePatientAndService?id=" + id,
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
    var id = $('#xn_getsample_id').val();
    if (id == '') {
        GetSample_ResetInput()
    }
    else {
        GetSample_GetPatientInfo(id);
    }
}

function GetSample_GetSID(seq) {
    $.ajax({
        url: "/XN_GetSample/GetSID?seq=" + seq,
        type: "GET",
        dataType: "text",
        cache: false,
        success: function (result) {
            $('#xn_getsample_sid').val(result);
        }
    });
}

function GetSample_SavePatient() {
    var validate = GetSample_ValidateInput('patientInfo');
    if (validate) {
        var id = $('#xn_getsample_id').val();
        var patientId = $('#xn_getsample_patientId').val();
        var seq = $('#xn_getsample_seq').val();
        var sid = $('#xn_getsample_sid').val();
        var patientName = $('#xn_getsample_patientName').val();
        var age = $('#xn_getsample_age').val();
        var sex = $('#xn_getsample_sex').val();
        var obj = $('#xn_getsample_obj').val();
        var type = $('#xn_getsample_type').val();
        var location = $('#xn_getsample_location').val();
        var doctor = $('#xn_getsample_doctor').val();
        var getSampleTime = $('#xn_getsample_getSampleTime').val();
        var address = $('#xn_getsample_address').val();
        var diagnostic = $('#xn_getsample_diagnostic').val();
        var hospital = $('#xn_getsample_hospital').val();
        var category = $('#select-category').val();
        var service = $('#select-service').val();

        $.ajax({
            url: "/XN_GetSample/SavePatient?id= " + id + "&&patientId=" + patientId + "&&seq=" + seq + "&&sid=" + sid + "&&patientName=" + patientName + "&&age=" + age + "&&sex=" + sex + "&&obj=" + obj + "&&type=" + type + "&&location=" + location + "&&doctor=" + doctor + "&&getSampleTime=" + getSampleTime + "&&address=" + address + "&&diagnostic=" + diagnostic + "&&service=" + service + "&&hospital=" + hospital,
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
    var id = $('#xn_getsample_id').val();
    if (id === "") {
        alert("Vui lòng chọn bệnh nhân!");
    }
    else {
        var getSampleTime = $('#xn_getsample_getSampleTime').val();
        $.ajax({
            url: "/XN_GetSample/ProcessResult?id= " + id + "&getSampleTime=" + getSampleTime,
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
    var id = $('#xn_getsample_id').val();
    if (id === "") {
        $('#addServiceForm').modal('hide');
        alert("Vui lòng chọn bệnh nhân!");
    }
    else {
        GetSample_SetSelect2_02();
    }
}

function GetSample_DeleteServiceForPatient(idResultXN) {
    var patientId = $('#xn_getsample_id').val();
    $.ajax({
        url: "/XN_GetSample/DeleteServiceForPatient?id=" + idResultXN,
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


function GetSample_LoadService(categoryId) {
    $('#xn_getsample_service').empty();
    $.ajax({
        url: "/XN_GetSample/GetServiceByCategory?categoryId=" + categoryId,
        type: 'GET',
        dataType: 'json',
        success: function (response) {
            $.each(response, function (key, value) {
                $("#xn_getsample_service").append('<option value=' + value.id + '>' + value.name + '</option>');
                $("#xn_getsample_service").val('');
            });
        }
    });
}

function GetSample_AddServiceForPatient() {
    var serviceId = $('#xn_getsample_service').val();
    var patientId = $('#xn_getsample_id').val();
    var doctorId = $('#xn_getsample_doctor').val();
    if (serviceId === "" || patientId === "") {
        alert("Chỉ định dịch vụ không thành công. Vui lòng kiểm tra lại !")
    }
    else {
        $.ajax({
            url: "/XN_GetSample/AddServiceForPatient?patientId=" + patientId + "&&serviceId=" + serviceId + "&&doctorId=" + doctorId,
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
    }
}

function GetSample_Get_Count() {
    $.ajax({
        url: "/XN_GetSample/Get_Count/",
        type: 'GET',
        dataType: 'text',
        success: function (result) {
            var arrayResult = result.split(';');
            document.getElementById("countGetSample").innerHTML = arrayResult[0];
            document.getElementById("countProcessNotFullResult").innerHTML = arrayResult[1];
            document.getElementById("countProcessFullResult").innerHTML = arrayResult[2];
            document.getElementById("countReturnResult").innerHTML = arrayResult[3];
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
                if (e.id === "xn_process_userReturnResultXN") {
                    $('#xn_process_userReturnResultXN').select2('focus');
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
        url: "/XN_Process/Refresh/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            var date = new Date();
            var today = date.getFullYear() + "-" + ("0" + (date.getMonth() + 1)).slice(-2) + "-" + ("0" + date.getDate()).slice(-2);
            $("#process_listPatient").html(result);
            $("#xn_getsample_timeSearchFrom").val(today);
            $("#xn_getsample_timeSearchTo").val(today);
            Process_ResetInput();
            Process_Get_Count();
        },
        error: function () {
            $("#process_listPatient").empty();
        }
    });
}

function Process_Search() {
    var xn_getsample_pidorseq = $("#xn_getsample_pidorseq").val();
    var timeSearchFrom = $("#xn_getsample_timeSearchFrom").val();
    var timeSearchTo = $("#xn_getsample_timeSearchTo").val();
    $.ajax({
        url: "/XN_Process/Search?" + "pidorseq=" + xn_getsample_pidorseq + "&timeSearchFrom=" + timeSearchFrom + "&timeSearchTo=" + timeSearchTo,
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
    $("#xn_process_pidorseq").val('');
    $('#xn_process_id').val('');
    $('#xn_process_patientId').val('');
    $('#xn_process_seq').val('');
    $('#xn_process_sid').val('');
    $('#xn_process_patientName').val('');
    $('#xn_process_age').val('');
    $('#xn_process_sex').val('');
    $('#xn_process_obj').val('');
    $('#xn_process_type').val('');
    $('#xn_process_location').val('');
    $('#xn_process_doctor').val('');
    $('#xn_process_getSampleTime').val('');
    $('#xn_process_returnResultTime').val('');
    $('#xn_process_location').val('');
    $('#xn_process_doctor').val('');
    $('#xn_process_userReturnResultXN').val('');
    $('#xn_process_address').val('');
    $('#xn_process_diagnostic').val('');
    $('#tbody-gridview-service').empty();
    Process_SetSelect2_03();
}

function Process_GetPatientInfo(id) {
    $.ajax({
        url: "/XN_Process/GetPatientInfo?id=" + id,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#process_patientInfo").html(result);
            Process_SetSelect2_03();
            // Auto-chọn bác sĩ = user đang login (nếu chưa có giá trị)
            var loginId = $("#xn_process_userLoginId").val();
            var $sel = $("#xn_process_userReturnResultXN");
            if (loginId && (!$sel.val() || $sel.val() === "")) {
                $sel.val(loginId).trigger("change");
            }

            // Gắn handler đổi bác sĩ
            $(document)
                .off("change", "#xn_process_userReturnResultXN")
                .on("change", "#xn_process_userReturnResultXN", Process_Load_SelectedDoctorInfo);
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
        url: "/XN_Process/GetServiceForPatient?patientId=" + id,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#process_xn-right-service-gridview").html(result);
        },
        error: function () {
            $('#tbody-gridview-service').empty();
        }
    });
}


function Process_SaveResult() {
    var patientId = $('#xn_process_id').val();
    if (patientId === "") {
        alert("Vui lòng chọn bệnh nhân!");
    }
    else {
        var validate = Process_ValidateInput('process_patientInfo');
        if (validate) {
            var returnResultTime = $('#xn_process_returnResultTime').val();
            var userReturnResult = $('#xn_process_userReturnResultXN').val();
            var DATA = [];
            $(".process-row-result").each(function () { // Lấy value trên từng Row
                var id = "";
                var status = "";
                var result = "";
                var validPrint = "";

                $(this).find(".td-id").each(function () {
                    id = $(this).html();
                })

                $(this).find(".btn-status").each(function () {
                    status = $(this).val();
                })

                $(this).find(".input-result").each(function () {
                    result = $(this).val();
                })

                $(this).find(".validPrint").each(function () {
                    validPrint = $(this).prop('checked');
                })

                DATA.push({ patientId: patientId, returnResultTime: returnResultTime, userReturnResult: userReturnResult, id: id, status: status, result: result, validPrint: validPrint });
            })

            $.ajax({
                url: "/XN_Process/SaveResult/",
                data: JSON.stringify(DATA),
                contentType: "application/json; charset=utf-8",
                dataType: "text",
                type: "POST",
                success: function (result) {
                    if (result == 'True') {
                        Process_GetListServiceForPatient(id);
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
        else {
            return false
        }
    }
}

function Process_ValidPrint() {
    var patientId = $('#xn_process_id').val();
    var sid = $('#xn_process_sid').val();
    if (patientId === "") {
        alert("Vui lòng chọn bệnh nhân!");
    }
    else {
        $('#showWaitting').modal('show');
        var validate = Process_ValidateInput('process_patientInfo');
        if (validate) {
            var returnResultTime = $('#xn_process_returnResultTime').val();
            var userReturnResult = $('#xn_process_userReturnResultXN').val();
            var DATA = [];
            $(".process-row-result").each(function () { // Lấy value trên từng Row
                var id = "";
                var status = "";
                var result = "";
                var validPrint = "";

                $(this).find(".td-id").each(function () {
                    id = $(this).html();
                })

                $(this).find(".btn-status").each(function () {
                    status = $(this).val();
                })

                $(this).find(".input-result").each(function () {
                    result = $(this).val();
                })

                $(this).find(".validPrint").each(function () {
                    validPrint = $(this).prop('checked');
                })

                DATA.push({ patientId: patientId, sid: sid, returnResultTime: returnResultTime, userReturnResult: userReturnResult, id: id, status: status, result: result, validPrint: validPrint });
            })

            $.ajax({
                url: "/XN_Process/ValidPrint/",
                data: JSON.stringify(DATA),
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
                    alert("Valid & In không thành công. Vui lòng kiểm tra lại!");
                }
            });
            return true;
        }
        else {
            return false
        }
    }
}


function Process_GetSample() {
    var id = $('#xn_process_id').val();
    if (id === "") {
        alert("Vui lòng chọn bệnh nhân!");
    }
    else {
        $.ajax({
            url: "/XN_Process/GetSample?id= " + id,
            type: 'POST',
            dataType: 'text',
            success: function (result) {
                if (result === 'True') {
                    Process_Refresh();
                    Process_Get_Count();
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
        url: "/XN_Process/Get_Count/",
        type: 'GET',
        dataType: 'text',
        success: function (result) {
            var arrayResult = result.split(';');
            document.getElementById("countGetSample").innerHTML = arrayResult[0];
            document.getElementById("countProcessNotFullResult").innerHTML = arrayResult[1];
            document.getElementById("countProcessFullResult").innerHTML = arrayResult[2];
            document.getElementById("countReturnResult").innerHTML = arrayResult[3];
        }
    });
}

// Tải thông tin bác sĩ khi đổi select
function Process_Load_SelectedDoctorInfo() {
    var userId = $("#xn_process_userReturnResultXN").val();
    if (!userId) {
        $("#xn_process_signerCCCD").empty();
        return;
    }
    $.ajax({
        url: "/XN_Process/GetUserInfo?userId=" + userId,
        type: "GET",
        dataType: "json",
        success: function (u) {
            if (!u) {
                $("#xn_process_signerCCCD").text("Không lấy được thông tin bác sĩ.");
                return;
            }
            // Hiển thị nhẹ ở UI
            var cccd = (u.cccd || "");
            $("#xn_process_signerCCCD").val(cccd);
        },
        error: function () {
            $("#xn_process_signerCCCD").text("Không lấy được thông tin bác sĩ.");
        }
    });
}

// ========================== KÝ SỐ PDF (Form: SIÊU ÂM) ==========================
function Process_SignPdf() {
    var patientId = $('#xn_process_id').val();
    var sid = $('#xn_process_sid').val();
    if (patientId === "") {
        alert("Vui lòng chọn bệnh nhân!");
        return;
    }

    // Lấy CCCD người ký (bác sĩ)
    var signerCCCD = $('#xn_process_signerCCCD').val();
    if (!signerCCCD || $.trim(signerCCCD) === "") {
        alert("Vui lòng nhập CCCD người ký (Viettel MySign)!");
        $('#xn_process_signerCCCD').focus();
        return;
    }
    console.log(signerCCCD);

    // Lấy context để backend đặt tên file đẹp theo template
    var patientCode = $('#xn_process_patientId').val() || $('#sa_process_sid').val() || "";
    var patientName = $('#xn_process_patientName').val() || "";
    var doctorName = $('#xn_process_userReturnResultXN option:selected').text() || $('#xn_process_userReturnResultXN').val() || "";
    var performedAt = $('#xn_process_returnResultTime').val() || ""; // thời điểm trả kết quả
    var serviceCode = ""; // nếu cần, bạn lấy từ label dịch vụ đang chọn

    $('#showWaitting').modal('show');
    var validate = Process_ValidateInput('process_patientInfo');
    if (validate) {
        var returnResultTime = $('#xn_process_returnResultTime').val();
        var userReturnResult = $('#xn_process_userReturnResultXN').val();
        var DATA = [];
        $(".process-row-result").each(function () { // Lấy value trên từng Row
            var id = "";
            var status = "";
            var result = "";
            var validPrint = "";

            $(this).find(".td-id").each(function () {
                id = $(this).html();
            })

            $(this).find(".btn-status").each(function () {
                status = $(this).val();
            })

            $(this).find(".input-result").each(function () {
                result = $(this).val();
            })

            $(this).find(".validPrint").each(function () {
                validPrint = $(this).prop('checked');
            })

            DATA.push({ patientId: patientId, sid: sid, returnResultTime: returnResultTime, userReturnResult: userReturnResult, id: id, status: status, result: result, validPrint: validPrint });
        })
        console.log(DATA);
        $.ajax({
            url: "/XN_Process/ValidPrint/",
            data: JSON.stringify(DATA),
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
                formData.append("formType", "XetNghiem");
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
                        //resp = {
                        //    "id": "51626", "provider": "0", "documentId": "null", "documentType": "null", "documentName": "27433568_phieu_danh_gia_ban_dau.pdf", "documentDesc": "null", "documentHash": "7X38RRgfWpYULcvtZc863HnTfocDsSsji023UGoxBLM=", "timestamp": "2025-09-19T07:11:06.763046209Z", "transactionId": "308706a6-5dc7-4f0b-ba08-01448c15a306", "credentialId": "066194014120_7500452_20250821162101", "signatures": "SJpUV2IQVRR3FzMzK5WWLucKNX2jtFc4\/\/+rvi19Izj6yL1epn7xo1cxGhW\/67fI\r\niqLqz104zwke0xbWf9BZPjAEmSWD8Xn2RaouyGCBQIj4tnCnMZQGnPCBZ2laN4Ec\r\n3qjMU3lpoh22OnVBgLYZlQZSSvnNii1MF7v1vKRsdggl4YZpHEiZlgMef7iclkga\r\nMGUryw11teXAg77s15jGKYCjkM49jrJlGq+M5p0IVCj\/ZeUXByd3MsBeWAnWAHs1\r\n\/WoyEfBhwK3YABb37cjO6RNFACj+KJJv8910AyQwZeowyhmeQDdjLdGgfJRILORt\r\nDFZF1iABlsXIbp6pf0ek8w==", "status": "1", "filepath": "null", "url": "null", "docControlId": "null", "docControlType": "null", "error": "null", "login": "066194014120", "taxcode": "6001362081", "metadata": "null", "requesttime": "1758265850532", "contentContentType": "application\/pdf", "signUserId": "066194014120",
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

                            console.log("payloadXN: ", signStoreId)
                            // 2) LƯU signStoreId THEO resultCDHAId ĐANG CHỌN
                            if (signStoreId) {
                                var ids = DATA.map(function (x) { return Number(x.id); });

                                $.ajax({
                                    url: "/XN_Process/SaveSignStoreIdForResultXN?signStoreId=" + encodeURIComponent(signStoreId),
                                    type: "POST",
                                    data: JSON.stringify(ids),
                                    contentType: "application/json; charset=utf-8",               // <- BẮT BUỘC
                                    dataType: "json",
                                    success: function (res) {
                                        var objResult = res;
                                        console.log(objResult);
                                        openSigned(signStoreId);
                                        if (objResult.success) {
                                            keyResult = objResult.keyResult;

                                            // ====== GỌI CONTROLLER LƯU DIGITAL_SIGN ======
                                            var targetText = doctorName;
                                            var statusNum = (obj && (obj.status === 1 || obj.status === "1")) ? 1 : 0;

                                            var digitalSignPayload = {
                                                referenceType: "XetNghiem",                // SA / SieuAmTim / XQUANG...
                                                referenceKeyResult: keyResult,          // Id dịch vụ
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
                                            // Fallback: nếu không có signStoreId thì xử lý như cũ
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
        return true;
    }
    else {
        return false
    }
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
        url: "/XN_ReturnResult/Refresh/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            var date = new Date();
            var today = date.getFullYear() + "-" + ("0" + (date.getMonth() + 1)).slice(-2) + "-" + ("0" + date.getDate()).slice(-2);
            $("#returnresult_listPatient").html(result);
            $("#xn_returnresult_timeSearchFrom").val(today);
            $("#xn__returnresult_timeSearchTo").val(today);
            ReturnResult_ResetInput();
            ReturnResult_Get_Count();
        },
        error: function () {
            $("#returnresult_listPatient").empty();
        }
    });
}

function ReturnResult_Search() {
    var xn_getsample_pidorseq = $("#xn_returnresult_pidorseq").val();
    var timeSearchFrom = $("#xn_returnresult_timeSearchFrom").val();
    var timeSearchTo = $("#xn_returnresult_timeSearchTo").val();
    $.ajax({
        url: "/XN_ReturnResult/Search?" + "pidorseq=" + xn_getsample_pidorseq + "&timeSearchFrom=" + timeSearchFrom + "&timeSearchTo=" + timeSearchTo,
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
    $("#xn_returnresult_pidorseq").val('');
    $('#xn_returnresult_id').val('');
    $('#xn_returnresult_patientId').val('');
    $('#xn_returnresult_seq').val('');
    $('#xn_returnresult_sid').val('');
    $('#xn_returnresult_patientName').val('');
    $('#xn_returnresult_age').val('');
    $('#xn_returnresult_sex').val('');
    $('#xn_returnresult_obj').val('');
    $('#xn_returnresult_type').val('');
    $('#xn_returnresult_location').val('');
    $('#xn_returnresult_doctor').val('');
    $('#xn_returnresult_getSampleTime').val('');
    $('#xn_returnresult_returnResultTime').val('');
    $('#xn_returnresult_location').val('');
    $('#xn_returnresult_doctor').val('');
    $('#xn_returnresult_userReturnResultXN').val('');
    $('#xn_returnresult_address').val('');
    $('#xn_returnresult_diagnostic').val('');
    $('#tbody-gridview-service').empty();
}

function ReturnResult_GetPatientInfo(id) {
    $.ajax({
        url: "/XN_ReturnResult/GetPatientInfo?id=" + id,
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
        url: "/XN_ReturnResult/GetServiceForPatient?patientId=" + id,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#returnresult_xn-right-service-gridview").html(result);
        },
        error: function () {
            $('#tbody-gridview-service').empty();
        }
    });
}

function ReturnResult_Invalid() {
    var id = $('#xn_returnresult_id').val();
    if (id === "") {
        alert("Vui lòng chọn bệnh nhân!");
    }
    else {
        if (confirm('Bạn muốn InValid kết quả của bệnh nhân ?')) {
            $.ajax({
                url: "/XN_ReturnResult/Invalid?id= " + id,
                type: 'POST',
                dataType: 'text',
                success: function (result) {
                    if (result === 'True') {
                        UpdateSignStatus(id);
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
        url: "/XN_ReturnResult/Get_Count/",
        type: 'GET',
        dataType: 'text',
        success: function (result) {
            var arrayResult = result.split(';');
            document.getElementById("countGetSample").innerHTML = arrayResult[0];
            document.getElementById("countProcessNotFullResult").innerHTML = arrayResult[1];
            document.getElementById("countProcessFullResult").innerHTML = arrayResult[2];
            document.getElementById("countReturnResult").innerHTML = arrayResult[3];
        }
    });
}

function ReturnResult_Print() {
    var patientId = $('#xn_returnresult_id').val();
    var sid = $('#xn_returnresult_sid').val();
    if (patientId === "") {
        alert("Vui lòng chọn bệnh nhân!");
    }
    else {
        $('#showWaitting').modal('show');
        $.ajax({
            url: "/XN_ReturnResult/Print?patientId=" + patientId + "&&sid=" + sid,
            dataType: "text",
            type: "POST",
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

function ReturnResult_ViewSignedPdf() {
    var signStoreId = $('#xn_returnresult_signStoreId').val();

    function openSigned(id) {
        if (!id) { alert("Chưa có signStoreId cho dịch vụ này!"); return; }
        var url = "/api/ExternalSign/view-signed/" + encodeURIComponent(id);
        window.open(url, "_blank");
    }

    if (signStoreId && $.trim(signStoreId) !== "") {
        openSigned(signStoreId);
        return;
    }

    // Nếu input rỗng → Chọn theo PatientId 
    var patientId = $("#xn_returnresult_id").val(); // patientId này là cột id của bảng Patient chứ không phải cột PatientId

    if (!patientId) {
        alert("Vui lòng chọn bệnh nhân để xem in lại kết quả!");
        return;
    }
    console.log(patientId);
    // Gọi API của bạn để lấy signStoreId đã lưu trong DB theo resultId
    $.ajax({
        url: "/XN_ReturnResult/GetSignStoreId",
        type: "GET",
        data: { patientId: patientId },
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

function UpdateSignStatus(patientId) {
    $.ajax({
        url: "/DigitalSign/UpdateSignStatus_ResultXN?patientId=" + patientId,
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
