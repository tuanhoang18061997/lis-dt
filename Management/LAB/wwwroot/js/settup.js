// Tool tip cho control của form
//$(function () {
//    $('[data-toggle="tooltip"]').tooltip()
//})


// *********************************************************************************** Select 2
$(function () {
    GetSample_SetSelect2_01(true);
    Map_SetSelect2();
})

function GetSample_SetSelect2_02() {
    $(document).ready(function () {
        $('#ns_getsample_service').select2({
            dropdownParent: $('#addServiceForm'),
            placeholder: "-- Chọn dịch vụ --"
        });
    });
    $('#ns_getsample_service').val('');
}

function GetSample_SetSelect2_01(isLoadPage) {
    if (isLoadPage) {
        $('#ns_getsample_location').val("");
    }
    $(document).ready(function () {
        $('#ns_getsample_location').select2({
            placeholder: "-- Chọn --"
        });
    });

    if (isLoadPage) {
        $('#ns_getsample_doctor').val("");
    }
    $(document).ready(function () {
        $('#ns_getsample_doctor').select2({
            placeholder: "-- Chọn --"
        });
    });


    if (isLoadPage) {
        $('#ns_process_sampleresult').val("");
    }
    $(document).ready(function () {
        $('#ns_process_sampleresult').select2({
            placeholder: "-- Chọn --"
        });
    });
}

function Process_SetSelect2_03() {
    $(document).ready(function () {
        $('#map-testcode').select2({
            placeholder: "-- Chọn --"
        });
    });
}

function Map_SetSelect2() {
    $(document).ready(function () {
        $('#map-addtestcode-testcode').select2({
            dropdownParent: $('#addTestCode'),
            placeholder: "-- Chọn mã xét nghiệm --"
        });
    });
}

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

// ****************************************************************************** User

function User_HideButton(_new, _save, _delete, _cancel, _function) {
    if (_new == 1) {
        $("#user-new").hide();
    }
    else {
        $("#user-new").show();
    }

    if (_save == 1) {
        $("#user-save").hide();
    }
    else {
        $("#user-save").show();
    }

    if (_delete == 1) {
        $("#user-delete").hide();
    }
    else {
        $("#user-delete").show();
    }

    if (_cancel == 1) {
        $("#user-cancel").hide();
    }
    else {
        $("#user-cancel").show();
    }

    if (_function == 1) {
        $("#user-function").hide();
    }
    else {
        $("#user-function").show();
    }
}

function User_ResetInput() {
    $("#user-code").val("");
    $("#user-name").val("");
    $("#user-pass").val("");
    $("#user-mabhyt").val("");
    $("#user-cccd").val("");
    $("#user-his-employeeid").val("");
    $('input:checkbox').removeAttr('checked');
}

function User_Search(value) {
    $.ajax({
        url: "/User/Search?value=" + value,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#user-settup-left-list").html(result);
        },
        error: function () {
            $("#user-settup-left-list").empty();
        }
    });
}

function User_CheckedBoxOnRow(code) {
    $(".row-service").each(function () { // Lấy value trên từng Row
        $(this).find(".form-check-input").each(function () {
            var value = $(this).val();
            if (value == code) {
                $(this).prop("checked", true);
                User_LoadInfo(code);
                User_LoadFunction(code);
                User_HideButton(false, true, false, true, false);
            }
            else {
                $(this).prop("checked", false);
            }
        })

    })
}

function User_LoadList() {
    $.ajax({
        url: "/User/GetList/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#user-settup-left-list").html(result);
        },
        error: function () {
            $("#user-settup-left-list").empty();
        }
    });
}

function User_LoadInfo(code) {
    $.ajax({
        url: "/User/GetInfo?code=" + code,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#user-settup-right-info").html(result);
        },
        error: function () {
            $("#user-settup-right-info").empty();
        }
    });
}

function User_LoadFunction(code) {
    $.ajax({
        url: "/User/GetFunction?code=" + code,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#user-settup-function").html(result);
        },
        error: function () {
            $("#user-settup-function").empty();
        }
    });
}

function User_New() {
    $("#user-code").prop('disabled', false);
    $("#user-code").focus();
    User_ResetInput();
    User_HideButton(true, false, true, false, true);
}

function User_Save() {
    var validate = ValidateInput('user-settup-right-info');
    if (validate) {
        var code = $("#user-code").val();
        var name = $("#user-name").val();
        var pass = $("#user-pass").val();
        var mabhyt = $("#user-mabhyt").val();
        var cccd = $("#user-cccd").val();
        var hisEmployeeId = $("#user-his-employeeid").val();
        var active = $("#user-active").prop('checked');
        var type = "";
        $(".div-input").each(function () {
            $(this).find(".type").each(function () {
                var checked = $(this).prop('checked');
                if (checked === true) {
                    type = type + $(this).val() + ";";
                }
            })
        })

        var data = {
            code: code,
            name: name,
            pass: pass,
            mabhyt: mabhyt,
            cccd: cccd,
            hisEmployeeId: hisEmployeeId,
            active: active,
            type: type
        };

        $.ajax({
            url: "/User/Save/",
            data: JSON.stringify(data),
            contentType: "application/json; charset=utf-8",
            dataType: "text",
            type: "POST",
            success: function (result) {
                if (result == 'True') {
                    $("#user-code").prop('disabled', true);
                    User_LoadList();
                    User_HideButton(false, true, false, true, false);
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

function User_Delete() {
    var choice = confirm("Bạn muốn xoá người dùng này ?");
    if (choice) {
        $("#user-code").prop('disabled', true);
        var code = $("#user-code").val();
        if (code === "" || !code) {
            alert("Vui lòng chọn người dùng cần xoá");
        }
        else {
            $.ajax({
                url: "/User/Delete?code=" + code,
                type: "POST",
                dataType: "text",
                success: function (result) {
                    if (result == 'True') {
                        User_ResetInput();
                        User_LoadList();
                        User_HideButton(false, true, true, true, true);
                    }
                    else {
                        alert("Dữ liệu đã được sử dụng nên không thể xoá. Chỉ có thể InActive");
                    }
                },
                error: function () {
                    alert("Dữ liệu đã được sử dụng nên không thể xoá. Chỉ có thể InActive");
                }
            });
        }
    }
}

function User_Cancel() {
    $("#user-code").prop('disabled', true);
    var code = $("#user-code").val();
    if (code === "" || !code) {
        User_ResetInput();
        User_HideButton(false, true, true, true, true);
    }
    else {
        User_LoadInfo(code);
        User_HideButton(false, true, false, true, false);
    }
}

function User_SaveUserFunction() {
    var code = $('#user-code').val();
    if (code === "" || !code) {
        alert("Vui lòng chọn người dùng để phân quyền!");
    }
    else {
        var functions = "";
        $(".user-function-content").each(function () {
            $(this).find(".function").each(function () {
                var checked = $(this).prop('checked');
                if (checked === true) {
                    functions = functions + $(this).val() + ";";
                }
            })
        })

        var data = {
            code: code,
            functions: functions
        };

        $.ajax({
            url: "/User/Save_UserFunction/",
            data: JSON.stringify(data),
            contentType: "application/json; charset=utf-8",
            dataType: "text",
            type: "POST",
            success: function (result) {
                if (result == 'False') {
                    alert("Lưu không thành công. Vui lòng kiểm tra lại!");
                }
            },
            error: function () {
                alert("Lưu không thành công. Vui lòng kiểm tra lại!");
            }
        });
    }
}

function User_UserFunction() {
    $("#user-code").prop('disabled', true);
    var code = $("#user-code").val();
    if (code === "" || !code) {
        $('#addFunction').modal('hide');
        alert("Vui lòng chọn người dùng để phân quyền !");
    }
}


// ****************************************************************************** Hospital
function Hospital_CheckedBoxOnRow(code) {
    $(".row-service").each(function () {
        $(this).find(".form-check-input").each(function () {
            var value = $(this).val();
            if (value == code) {
                $(this).prop("checked", true);
                Hospital_LoadInfo(code);
            }
            else {
                $(this).prop("checked", false);
            }
        })

    })
}

function Hospital_LoadList() {
    $.ajax({
        url: "/Hospital/GetList/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#hospital-settup-left-list").html(result);
        },
        error: function () {
            $("#hospital-settup-left-list").empty();
        }
    });
}

function Hospital_LoadInfo(code) {
    $.ajax({
        url: "/Hospital/GetInfo?code=" + code,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#hospital-settup-right-info").html(result);
        },
        error: function () {
            $("#hospital-settup-right-info").empty();
        }
    });
}

function Hospital_Save() {
    var validate = ValidateInput('hospital-settup-right-info');
    if (validate) {
        var code = $("#hospital-code").val();
        var name = $("#hospital-name").val();
        var nameen = $("#hospital-nameen").val();
        var address = $("#hospital-address").val();
        var phone = $("#hospital-phone").val();
        var website = $("#hospital-website").val();
        var email = $("#hospital-email").val();
        var logo = $("#hospital-logo").val();

        var data = {
            code: code,
            name: name,
            nameen: nameen,
            address: address,
            phone: phone,
            website: website,
            email: email,
            logo: logo
        };

        $.ajax({
            url: "/Hospital/Save/",
            data: JSON.stringify(data),
            contentType: "application/json; charset=utf-8",
            dataType: "text",
            type: "POST",
            success: function (result) {
                if (result == 'True') {
                    $("#hospital-code").prop('disabled', true);
                    Hospital_LoadList();
                    Hospital_HideButton(false, true, true);
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

function Hospital_Cancel() {
    $("#hospital-code").prop('disabled', true);
    var code = $("#hospital-code").val();
    Hospital_LoadInfo(code);

    if (code) {
        Hospital_LoadInfo(code);
        Hospital_HideButton(false, true, true);       
    }
    else {
        Hospital_ResetInput();
        Hospital_HideButton(false, true, true);
    }
}

function Hospital_New() {
    $("#hospital-code").prop('disabled', false);
    $("#hospital-code").focus();
    Hospital_ResetInput();
    Hospital_HideButton(true, false, false);
}

function Hospital_ResetInput() {
    $("#hospital-code").val("");
    $("#hospital-name").val("");
    $("#hospital-nameen").val("");
    $("#hospital-address").val("");
    $("#hospital-phone").val("");
    $("#hospital-website").val("");
    $("#hospital-email").val("");
    $("#hospital-logo").val("");
}

function Hospital_HideButton(_new, _save, _cancel) {
    if (_new == 1) {
        $("#hospital-new").hide();
    }
    else {
        $("#hospital-new").show();
    }

    if (_save == 1) {
        $("#hospital-save").hide();
    }
    else {
        $("#hospital-save").show();
    }

    if (_cancel == 1) {
        $("#hospital-cancel").hide();
    }
    else {
        $("#hospital-cancel").show();
    }
}


// ****************************************************************************** Location

function Location_HideButton(_new, _save, _delete, _cancel) {
    if (_new == 1) {
        $("#location-new").hide();
    }
    else {
        $("#location-new").show();
    }

    if (_save == 1) {
        $("#location-save").hide();
    }
    else {
        $("#location-save").show();
    }

    if (_delete == 1) {
        $("#location-delete").hide();
    }
    else {
        $("#location-delete").show();
    }

    if (_cancel == 1) {
        $("#location-cancel").hide();
    }
    else {
        $("#location-cancel").show();
    }
}

function Location_ResetInput() {
    $("#location-code").val("");
    $("#location-name").val("");
    $('input:checkbox').removeAttr('checked');
}

function Location_Search(value) {
    $.ajax({
        url: "/Location/Search?value=" + value,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#location-settup-left-list").html(result);
        },
        error: function () {
            $("#location-settup-left-list").empty();
        }
    });
}

function Location_CheckedBoxOnRow(code) {
    $(".row-service").each(function () {
        $(this).find(".form-check-input").each(function () {
            var value = $(this).val();
            if (value == code) {
                $(this).prop("checked", true);
                Location_LoadInfo(code);
                Location_HideButton(false, true, false, true);
            }
            else {
                $(this).prop("checked", false);
            }
        })

    })
}

function Location_LoadList() {
    $.ajax({
        url: "/Location/GetList/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#location-settup-left-list").html(result);
        },
        error: function () {
            $("#location-settup-left-list").empty();
        }
    });
}

function Location_LoadInfo(code) {
    $.ajax({
        url: "/Location/GetInfo?code=" + code,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#location-settup-right-info").html(result);
        },
        error: function () {
            $("#location-settup-right-info").empty();
        }
    });
}

function Location_New() {
    $("#location-code").prop('disabled', false);
    $("#location-code").focus();
    Location_ResetInput();
    Location_HideButton(true, false, true, false);
}

function Location_Save() {
    var validate = ValidateInput('location-settup-right-info');
    if (validate) {
        var code = $("#location-code").val();
        var name = $("#location-name").val();
        var active = $("#location-active").prop('checked');

        var data = {
            code: code,
            name: name,
            active: active
        };

        $.ajax({
            url: "/Location/Save/",
            data: JSON.stringify(data),
            contentType: "application/json; charset=utf-8",
            dataType: "text",
            type: "POST",
            success: function (result) {
                if (result == 'True') {
                    $("#location-code").prop('disabled', true);
                    Location_LoadList();
                    Location_HideButton(false, true, false, true);
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

function Location_Delete() {
    var choice = confirm("Bạn muốn xoá khoa phòng này ?");
    if (choice) {
        $("#location-code").prop('disabled', true);
        var code = $("#location-code").val();
        if (code === "" || !code) {
            alert("Vui lòng chọn người dùng cần xoá");
        }
        else {
            $.ajax({
                url: "/Location/Delete?code=" + code,
                type: "POST",
                dataType: "text",
                success: function (result) {
                    if (result == 'True') {
                        Location_ResetInput();
                        Location_LoadList();
                        Location_HideButton(false, true, true, true);
                    }
                    else {
                        alert("Dữ liệu đã được sử dụng nên không thể xoá. Chỉ có thể InActive");
                    }
                },
                error: function () {
                    alert("Dữ liệu đã được sử dụng nên không thể xoá. Chỉ có thể InActive");
                }
            });
        }
    }
}

function Location_Cancel() {
    $("#location-code").prop('disabled', true);
    var code = $("#location-code").val();
    if (code === "" || !code) {
        Location_ResetInput();
        Location_HideButton(false, true, true, true);
    }
    else {
        Location_LoadInfo(code);
        Location_HideButton(false, true, false, true);
    }
}

// ****************************************************************************** Object

function Object_HideButton(_new, _save, _delete, _cancel) {
    if (_new == 1) {
        $("#object-new").hide();
    }
    else {
        $("#object-new").show();
    }

    if (_save == 1) {
        $("#object-save").hide();
    }
    else {
        $("#object-save").show();
    }

    if (_delete == 1) {
        $("#object-delete").hide();
    }
    else {
        $("#object-delete").show();
    }

    if (_cancel == 1) {
        $("#object-cancel").hide();
    }
    else {
        $("#object-cancel").show();
    }
}

function Object_ResetInput() {
    $("#object-code").val("");
    $("#object-name").val("");
    $('input:checkbox').removeAttr('checked');
}

function Object_Search(value) {
    $.ajax({
        url: "/Object/Search?value=" + value,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#object-settup-left-list").html(result);
        },
        error: function () {
            $("#object-settup-left-list").empty();
        }
    });
}

function Object_CheckedBoxOnRow(code) {
    $(".row-service").each(function () {
        $(this).find(".form-check-input").each(function () {
            var value = $(this).val();
            if (value == code) {
                $(this).prop("checked", true);
                Object_LoadInfo(code);
                Object_HideButton(false, true, false, true);
            }
            else {
                $(this).prop("checked", false);
            }
        })

    })
}

function Object_LoadList() {
    $.ajax({
        url: "/Object/GetList/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#object-settup-left-list").html(result);
        },
        error: function () {
            $("#object-settup-left-list").empty();
        }
    });
}

function Object_LoadInfo(code) {
    $.ajax({
        url: "/Object/GetInfo?code=" + code,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#object-settup-right-info").html(result);
        },
        error: function () {
            $("#object-settup-right-info").empty();
        }
    });
}

function Object_New() {
    $("#object-code").prop('disabled', false);
    $("#object-code").focus();
    Object_ResetInput();
    Object_HideButton(true, false, true, false);
}

function Object_Save() {
    var validate = ValidateInput('object-settup-right-info');
    if (validate) {
        var code = $("#object-code").val();
        var name = $("#object-name").val();
        var active = $("#object-active").prop('checked');

        var data = {
            code: code,
            name: name,
            active: active
        };

        $.ajax({
            url: "/Object/Save/",
            data: JSON.stringify(data),
            contentType: "application/json; charset=utf-8",
            dataType: "text",
            type: "POST",
            success: function (result) {
                if (result == 'True') {
                    $("#object-code").prop('disabled', true);
                    Object_LoadList();
                    Object_HideButton(false, true, false, true);
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

function Object_Delete() {
    var choice = confirm("Bạn muốn xoá khoa phòng này ?");
    if (choice) {
        $("#object-code").prop('disabled', true);
        var code = $("#object-code").val();
        if (code === "" || !code) {
            alert("Vui lòng chọn người dùng cần xoá");
        }
        else {
            $.ajax({
                url: "/Object/Delete?code=" + code,
                type: "POST",
                dataType: "text",
                success: function (result) {
                    if (result == 'True') {
                        Object_ResetInput();
                        Object_LoadList();
                        Object_HideButton(false, true, true, true);
                    }
                    else {
                        alert("Dữ liệu đã được sử dụng nên không thể xoá. Chỉ có thể InActive");
                    }
                },
                error: function () {
                    alert("Dữ liệu đã được sử dụng nên không thể xoá. Chỉ có thể InActive");
                }
            });
        }
    }
}

function Object_Cancel() {
    $("#object-code").prop('disabled', true);
    var code = $("#object-code").val();
    if (code === "" || !code) {
        Object_ResetInput();
        Object_HideButton(false, true, true, true);
    }
    else {
        Object_LoadInfo(code);
        Object_HideButton(false, true, false, true);
    }
}

// ****************************************************************************** Doctor

function Doctor_HideButton(_new, _save, _delete, _cancel) {
    if (_new == 1) {
        $("#doctor-new").hide();
    }
    else {
        $("#doctor-new").show();
    }

    if (_save == 1) {
        $("#doctor-save").hide();
    }
    else {
        $("#doctor-save").show();
    }

    if (_delete == 1) {
        $("#doctor-delete").hide();
    }
    else {
        $("#doctor-delete").show();
    }

    if (_cancel == 1) {
        $("#doctor-cancel").hide();
    }
    else {
        $("#doctor-cancel").show();
    }
}

function Doctor_ResetInput() {
    $("#doctor-code").val("");
    $("#doctor-name").val("");
    $('input:checkbox').removeAttr('checked');
}

function Doctor_Search(value) {
    $.ajax({
        url: "/Doctor/Search?value=" + value,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#doctor-settup-left-list").html(result);
        },
        error: function () {
            $("#doctor-settup-left-list").empty();
        }
    });
}

function Doctor_CheckedBoxOnRow(code) {
    $(".row-service").each(function () {
        $(this).find(".form-check-input").each(function () {
            var value = $(this).val();
            if (value == code) {
                $(this).prop("checked", true);
                Doctor_LoadInfo(code);
                Doctor_HideButton(false, true, false, true);
            }
            else {
                $(this).prop("checked", false);
            }
        })

    })
}

function Doctor_LoadList() {
    $.ajax({
        url: "/Doctor/GetList/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#doctor-settup-left-list").html(result);
        },
        error: function () {
            $("#doctor-settup-left-list").empty();
        }
    });
}

function Doctor_LoadInfo(code) {
    $.ajax({
        url: "/Doctor/GetInfo?code=" + code,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#doctor-settup-right-info").html(result);
        },
        error: function () {
            $("#doctor-settup-right-info").empty();
        }
    });
}

function Doctor_New() {
    $("#doctor-code").prop('disabled', false);
    $("#doctor-code").focus();
    Doctor_ResetInput();
    Doctor_HideButton(true, false, true, false);
}

function Doctor_Save() {
    var validate = ValidateInput('doctor-settup-right-info');
    if (validate) {
        var code = $("#doctor-code").val();
        var name = $("#doctor-name").val();
        var active = $("#doctor-active").prop('checked');

        var data = {
            code: code,
            name: name,
            active: active
        };

        $.ajax({
            url: "/Doctor/Save/",
            data: JSON.stringify(data),
            contentType: "application/json; charset=utf-8",
            dataType: "text",
            type: "POST",
            success: function (result) {
                if (result == 'True') {
                    $("#doctor-code").prop('disabled', true);
                    Doctor_LoadList();
                    Doctor_HideButton(false, true, false, true);
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

function Doctor_Delete() {
    var choice = confirm("Bạn muốn xoá bác sĩ này ?");
    if (choice) {
        $("#doctor-code").prop('disabled', true);
        var code = $("#doctor-code").val();
        if (code === "" || !code) {
            alert("Vui lòng chọn người dùng cần xoá");
        }
        else {
            $.ajax({
                url: "/Doctor/Delete?code=" + code,
                type: "POST",
                dataType: "text",
                success: function (result) {
                    if (result == 'True') {
                        Doctor_ResetInput();
                        Doctor_LoadList();
                        Doctor_HideButton(false, true, true, true);
                    }
                    else {
                        alert("Dữ liệu đã được sử dụng nên không thể xoá. Chỉ có thể InActive");
                    }
                },
                error: function () {
                    alert("Dữ liệu đã được sử dụng nên không thể xoá. Chỉ có thể InActive");
                }
            });
        }
    }
}

function Doctor_Cancel() {
    $("#doctor-code").prop('disabled', true);
    var code = $("#doctor-code").val();
    if (code === "" || !code) {
        Doctor_ResetInput();
        Doctor_HideButton(false, true, true, true);
    }
    else {
        Doctor_LoadInfo(code);
        Doctor_HideButton(false, true, false, true);
    }
}

// ****************************************************************************** TestType

function TestType_HideButton(_new, _save, _delete, _cancel) {
    if (_new == 1) {
        $("#testtype-new").hide();
    }
    else {
        $("#testtype-new").show();
    }

    if (_save == 1) {
        $("#testtype-save").hide();
    }
    else {
        $("#testtype-save").show();
    }

    if (_delete == 1) {
        $("#testtype-delete").hide();
    }
    else {
        $("#testtype-delete").show();
    }

    if (_cancel == 1) {
        $("#testtype-cancel").hide();
    }
    else {
        $("#testtype-cancel").show();
    }
}

function TestType_ResetInput() {
    $("#testtype-code").val("");
    $("#testtype-name").val("");
    $('input:checkbox').removeAttr('checked');
}

function TestType_Search(value) {
    $.ajax({
        url: "/TestType/Search?value=" + value,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#testtype-settup-left-list").html(result);
        },
        error: function () {
            $("#testtype-settup-left-list").empty();
        }
    });
}

function TestType_CheckedBoxOnRow(code) {
    $(".row-service").each(function () {
        $(this).find(".form-check-input").each(function () {
            var value = $(this).val();
            if (value == code) {
                $(this).prop("checked", true);
                TestType_LoadInfo(code);
                TestType_HideButton(false, true, false, true);
            }
            else {
                $(this).prop("checked", false);
            }
        })

    })
}

function TestType_LoadList() {
    $.ajax({
        url: "/TestType/GetList/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#testtype-settup-left-list").html(result);
        },
        error: function () {
            $("#testtype-settup-left-list").empty();
        }
    });
}

function TestType_LoadInfo(code) {
    $.ajax({
        url: "/TestType/GetInfo?code=" + code,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#testtype-settup-right-info").html(result);
        },
        error: function () {
            $("#testtype-settup-right-info").empty();
        }
    });
}

function TestType_New() {
    $("#testtype-code").prop('disabled', false);
    $("#testtype-code").focus();
    TestType_ResetInput();
    TestType_HideButton(true, false, true, false);
}

function TestType_Save() {
    var validate = ValidateInput('testtype-settup-right-info');
    if (validate) {
        var code = $("#testtype-code").val();
        var name = $("#testtype-name").val();
        var active = $("#testtype-active").prop('checked');

        var data = {
            code: code,
            name: name,
            active: active
        };

        $.ajax({
            url: "/TestType/Save/",
            data: JSON.stringify(data),
            contentType: "application/json; charset=utf-8",
            dataType: "text",
            type: "POST",
            success: function (result) {
                if (result == 'True') {
                    $("#testtype-code").prop('disabled', true);
                    TestType_LoadList();
                    TestType_HideButton(false, true, false, true);
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

function TestType_Delete() {
    var choice = confirm("Bạn muốn xoá loại mẫu này ?");
    if (choice) {
        $("#testtype-code").prop('disabled', true);
        var code = $("#testtype-code").val();
        if (code === "" || !code) {
            alert("Vui lòng chọn người dùng cần xoá");
        }
        else {
            $.ajax({
                url: "/TestType/Delete?code=" + code,
                type: "POST",
                dataType: "text",
                success: function (result) {
                    if (result == 'True') {
                        TestType_ResetInput();
                        TestType_LoadList();
                        TestType_HideButton(false, true, true, true);
                    }
                    else {
                        alert("Dữ liệu đã được sử dụng nên không thể xoá. Chỉ có thể InActive");
                    }
                },
                error: function () {
                    alert("Dữ liệu đã được sử dụng nên không thể xoá. Chỉ có thể InActive");
                }
            });
        }
    }
}

function TestType_Cancel() {
    $("#testtype-code").prop('disabled', true);
    var code = $("#testtype-code").val();
    if (code === "" || !code) {
        TestType_ResetInput();
        TestType_HideButton(false, true, true, true);
    }
    else {
        TestType_LoadInfo(code);
        TestType_HideButton(false, true, false, true);
    }
}

// ****************************************************************************** Category

function Category_HideButton(_new, _save, _delete, _cancel) {
    if (_new == 1) {
        $("#category-new").hide();
    }
    else {
        $("#category-new").show();
    }

    if (_save == 1) {
        $("#category-save").hide();
    }
    else {
        $("#category-save").show();
    }

    if (_delete == 1) {
        $("#category-delete").hide();
    }
    else {
        $("#category-delete").show();
    }

    if (_cancel == 1) {
        $("#category-cancel").hide();
    }
    else {
        $("#category-cancel").show();
    }
}

function Category_ResetInput() {
    $("#category-code").val("");
    $("#category-name").val("");
    $('input:checkbox').removeAttr('checked');
}

function Category_Search(value) {
    $.ajax({
        url: "/Category/Search?value=" + value,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#category-settup-left-list").html(result);
        },
        error: function () {
            $("#category-settup-left-list").empty();
        }
    });
}

function Category_CheckedBoxOnRow(code) {
    $(".row-service").each(function () {
        $(this).find(".form-check-input").each(function () {
            var value = $(this).val();
            if (value == code) {
                $(this).prop("checked", true);
                Category_LoadInfo(code);
                Category_HideButton(false, true, false, true);
            }
            else {
                $(this).prop("checked", false);
            }
        })

    })
}

function Category_LoadList() {
    $.ajax({
        url: "/Category/GetList/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#category-settup-left-list").html(result);
        },
        error: function () {
            $("#category-settup-left-list").empty();
        }
    });
}

function Category_LoadInfo(code) {
    $.ajax({
        url: "/Category/GetInfo?code=" + code,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#category-settup-right-info").html(result);
        },
        error: function () {
            $("#category-settup-right-info").empty();
        }
    });
}

function Category_New() {
    $("#category-code").prop('disabled', false);
    $("#category-code").focus();
    Category_ResetInput();
    Category_HideButton(true, false, true, false);
}

function Category_Save() {
    var validate = ValidateInput('category-settup-right-info');
    if (validate) {
        var code = $("#category-code").val();
        var name = $("#category-name").val();
        var active = $("#category-active").prop('checked');

        var data = {
            code: code,
            name: name,
            active: active
        };

        $.ajax({
            url: "/Category/Save/",
            data: JSON.stringify(data),
            contentType: "application/json; charset=utf-8",
            dataType: "text",
            type: "POST",
            success: function (result) {
                if (result == 'True') {
                    $("#category-code").prop('disabled', true);
                    Category_LoadList();
                    Category_HideButton(false, true, false, true);
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

function Category_Delete() {
    var choice = confirm("Bạn muốn xoá danh mục này ?");
    if (choice) {
        $("#category-code").prop('disabled', true);
        var code = $("#category-code").val();
        if (code === "" || !code) {
            alert("Vui lòng chọn người dùng cần xoá");
        }
        else {
            $.ajax({
                url: "/Category/Delete?code=" + code,
                type: "POST",
                dataType: "text",
                success: function (result) {
                    if (result == 'True') {
                        Category_ResetInput();
                        Category_LoadList();
                        Category_HideButton(false, true, true, true);
                    }
                    else {
                        alert("Dữ liệu đã được sử dụng nên không thể xoá. Chỉ có thể InActive");
                    }
                },
                error: function () {
                    alert("Dữ liệu đã được sử dụng nên không thể xoá. Chỉ có thể InActive");
                }
            });
        }
    }
}

function Category_Cancel() {
    $("#category-code").prop('disabled', true);
    var code = $("#category-code").val();
    if (code === "" || !code) {
        Category_ResetInput();
        Category_HideButton(false, true, true, true);
    }
    else {
        Category_LoadInfo(code);
        Category_HideButton(false, true, false, true);
    }
}

// ****************************************************************************** Service

$(function () {
    Service_SetSelect2(true);
})

function Service_SetSelect2(isLoadPage) {
    if (isLoadPage) {
        $('#service-category').val("");
    }
    $(document).ready(function () {
        $('#service-category').select2({
            placeholder: "-- Chọn --"
        });
    });

    if (isLoadPage) {
        $('#service-printsample').val("");
    }
    $(document).ready(function () {
        $('#service-printsample').select2({
            placeholder: "-- Chọn --"
        });
    });
}

function Service_HideButton(_new, _save, _delete, _cancel) {
    if (_new == 1) {
        $("#service-new").hide();
    }
    else {
        $("#service-new").show();
    }

    if (_save == 1) {
        $("#service-save").hide();
    }
    else {
        $("#service-save").show();
    }

    if (_delete == 1) {
        $("#service-delete").hide();
    }
    else {
        $("#service-delete").show();
    }

    if (_cancel == 1) {
        $("#service-cancel").hide();
    }
    else {
        $("#service-cancel").show();
    }
}

function Service_ResetInput() {
    $("#service-code").val("");
    $("#service-name").val("");
    $('#service-category').val('');
    $('#service-printsample').val('');
    $('#service-printorder').val('');
    $('#service-processtype').val('');
    $('input:checkbox').removeAttr('checked');
    Service_SetSelect2(false);
}

function Service_Search(value) {
    $.ajax({
        url: "/Service/Search?value=" + value,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#service-settup-left-list").html(result);
        },
        error: function () {
            $("#service-settup-left-list").empty();
        }
    });
}

function Service_CheckedBoxOnRow(code) {
    $(".row-service").each(function () {
        $(this).find(".form-check-input").each(function () {
            var value = $(this).val();
            if (value == code) {
                $(this).prop("checked", true);
                Service_LoadInfo(code);
                Service_HideButton(false, true, false, true);
            }
            else {
                $(this).prop("checked", false);
            }
        })

    })
}

function Service_LoadList() {
    $.ajax({
        url: "/Service/GetList/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#service-settup-left-list").html(result);
        },
        error: function () {
            $("#service-settup-left-list").empty();
        }
    });
}

function Service_LoadInfo(code) {
    $.ajax({
        url: "/Service/GetInfo?code=" + code,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#service-settup-right-info").html(result);
            Service_SetSelect2(false);
        },
        error: function () {
            $("#service-settup-right-info").empty();
        }
    });
}

function Service_New() {
    $("#service-code").prop('disabled', false);
    $("#service-code").focus();
    Service_ResetInput();
    Service_HideButton(true, false, true, false);
}

function Service_Save() {
    var validate = ValidateInput('service-settup-right-info');
    if (validate) {
        var code = $("#service-code").val();
        var name = $("#service-name").val();
        var categoryid = $("#service-category").val();
        var printsampleid = $("#service-printsample").val();
        var processtype = $("#service-processtype").prop('checked');
        var printorder = $("#service-printorder").val();
        var active = $("#service-active").prop('checked');

        var data = {
            code: code,
            name: name,
            categoryid: categoryid,
            printsampleid: printsampleid,
            processtype: processtype,
            printorder: printorder,
            active: active
        };

        $.ajax({
            url: "/Service/Save/",
            data: JSON.stringify(data),
            contentType: "application/json; charset=utf-8",
            dataType: "text",
            type: "POST",
            success: function (result) {
                if (result == 'True') {
                    $("#service-code").prop('disabled', true);
                    Service_LoadList();
                    Service_HideButton(false, true, false, true);
                    Service_SetSelect2(false);
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

function Service_Delete() {
    var choice = confirm("Bạn muốn xoá dịch vụ này ?");
    if (choice) {
        $("#service-code").prop('disabled', true);
        var code = $("#service-code").val();
        if (code === "" || !code) {
            alert("Vui lòng chọn người dùng cần xoá");
        }
        else {
            $.ajax({
                url: "/Service/Delete?code=" + code,
                type: "POST",
                dataType: "text",
                success: function (result) {
                    if (result == 'True') {
                        Service_ResetInput();
                        Service_LoadList();
                        Service_HideButton(false, true, true, true);
                    }
                    else {
                        alert("Dữ liệu đã được sử dụng nên không thể xoá. Chỉ có thể InActive");
                    }
                },
                error: function () {
                    alert("Dữ liệu đã được sử dụng nên không thể xoá. Chỉ có thể InActive");
                }
            });
        }
    }
}

function Service_Cancel() {
    $("#service-code").prop('disabled', true);
    var code = $("#service-code").val();
    if (code === "" || !code) {
        Service_ResetInput();
        Service_HideButton(false, true, true, true);
    }
    else {
        Service_LoadInfo(code);
        Service_HideButton(false, true, false, true);
    }
}

// ****************************************************************************** Device

function Device_HideButton(_new, _save, _delete, _cancel) {
    if (_new == 1) {
        $("#device-new").hide();
    }
    else {
        $("#device-new").show();
    }

    if (_save == 1) {
        $("#device-save").hide();
    }
    else {
        $("#device-save").show();
    }

    if (_delete == 1) {
        $("#device-delete").hide();
    }
    else {
        $("#device-delete").show();
    }

    if (_cancel == 1) {
        $("#device-cancel").hide();
    }
    else {
        $("#device-cancel").show();
    }
}

function Device_ResetInput() {
    $("#device-code").val("");
    $("#device-name").val("");
    $("#device-protocol").val("");
    $("#device-codebhyt").val("");
    $('input:checkbox').removeAttr('checked');
}

function Device_Search(value) {
    $.ajax({
        url: "/Device/Search?value=" + value,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#device-settup-left-list").html(result);
        },
        error: function () {
            $("#device-settup-left-list").empty();
        }
    });
}

function Device_CheckedBoxOnRow(code) {
    $(".row-service").each(function () {
        $(this).find(".form-check-input").each(function () {
            var value = $(this).val();
            if (value == code) {
                $(this).prop("checked", true);
                Device_LoadInfo(code);
                Device_HideButton(false, true, false, true);
            }
            else {
                $(this).prop("checked", false);
            }
        })

    })
}

function Device_LoadList() {
    $.ajax({
        url: "/Device/GetList/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#device-settup-left-list").html(result);
        },
        error: function () {
            $("#device-settup-left-list").empty();
        }
    });
}

function Device_LoadInfo(code) {
    $.ajax({
        url: "/Device/GetInfo?code=" + code,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#device-settup-right-info").html(result);
        },
        error: function () {
            $("#device-settup-right-info").empty();
        }
    });
}

function Device_New() {
    $("#device-code").prop('disabled', false);
    $("#device-code").focus();
    Device_ResetInput();
    Device_HideButton(true, false, true, false);
}

function Device_Save() {
    var validate = ValidateInput('device-settup-right-info');
    if (validate) {
        var code = $("#device-code").val();
        var name = $("#device-name").val();
        var protocol = $("#device-protocol").val();
        var codebhyt = $("#device-codebhyt").val();
        var processqc = $("#device-processqc").prop('checked');
        var active = $("#device-active").prop('checked');

        var data = {
            code: code,
            name: name,
            protocol: protocol,
            codebhyt: codebhyt,
            processqc: processqc,
            active: active
        };

        $.ajax({
            url: "/Device/Save/",
            data: JSON.stringify(data),
            contentType: "application/json; charset=utf-8",
            dataType: "text",
            type: "POST",
            success: function (result) {
                if (result == 'True') {
                    $("#device-code").prop('disabled', true);
                    Device_LoadList();
                    Device_HideButton(false, true, false, true);
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

function Device_Delete() {
    var choice = confirm("Bạn muốn xoá thiết bị này ?");
    if (choice) {
        $("#device-code").prop('disabled', true);
        var code = $("#device-code").val();
        if (code === "" || !code) {
            alert("Vui lòng chọn thiết bị cần xoá");
        }
        else {
            $.ajax({
                url: "/Device/Delete?code=" + code,
                type: "POST",
                dataType: "text",
                success: function (result) {
                    if (result == 'True') {
                        Device_ResetInput();
                        Device_LoadList();
                        Device_HideButton(false, true, true, true);
                    }
                    else {
                        alert("Dữ liệu đã được sử dụng nên không thể xoá. Chỉ có thể InActive");
                    }
                },
                error: function () {
                    alert("Dữ liệu đã được sử dụng nên không thể xoá. Chỉ có thể InActive");
                }
            });
        }
    }
}

function Device_Cancel() {
    $("#device-code").prop('disabled', true);
    var code = $("#device-code").val();
    if (code === "" || !code) {
        Device_ResetInput();
        Device_HideButton(false, true, true, true);
    }
    else {
        Device_LoadInfo(code);
        Device_HideButton(false, true, false, true);
    }
}

// ****************************************************************************** Sample

function Sample_HideButton(_new, _save, _delete, _cancel) {
    if (_new == 1) {
        $("#sample-new").hide();
    }
    else {
        $("#sample-new").show();
    }

    if (_save == 1) {
        $("#sample-save").hide();
    }
    else {
        $("#sample-save").show();
    }

    if (_delete == 1) {
        $("#sample-delete").hide();
    }
    else {
        $("#sample-delete").show();
    }

    if (_cancel == 1) {
        $("#sample-cancel").hide();
    }
    else {
        $("#sample-cancel").show();
    }
}

function Sample_ResetInput() {
    $("#sample-code").val("");
    $("#sample-name").val("");
    CKEDITOR.instances["sample-description"].setData("");
    $("#sample-result").val("");
    $("#sample-suggest").val("");
    $("#sample-category").val("");
    $('input:checkbox').removeAttr('checked');
}

function Sample_Search(value) {
    $.ajax({
        url: "/Sample/Search?value=" + value,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#sample-settup-left-list").html(result);
        },
        error: function () {
            $("#sample-settup-left-list").empty();
        }
    });
}

function Sample_CheckedBoxOnRow(code) {
    $(".row-service").each(function () {
        $(this).find(".form-check-input").each(function () {
            var value = $(this).val();
            if (value == code) {
                $(this).prop("checked", true);
                Sample_LoadInfo(code);
                Sample_HideButton(false, true, false, true);
            }
            else {
                $(this).prop("checked", false);
            }
        })

    })
}

function Sample_LoadList() {
    $.ajax({
        url: "/Sample/GetList/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#sample-settup-left-list").html(result);
        },
        error: function () {
            $("#sample-settup-left-list").empty();
        }
    });
}

function Sample_LoadInfo(code) {
    $.ajax({
        url: "/Sample/GetInfo?code=" + code,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#sample-settup-right-info").html(result);
        },
        error: function () {
            $("#sample-settup-right-info").empty();
        }
    });
}

function Sample_New() {
    CKEDITOR.replace("sample-description", { height: '240px' });
    $("#sample-code").prop('disabled', false);
    $("#sample-code").focus();
    Sample_ResetInput();
    Sample_HideButton(true, false, true, false);
}

function Sample_Save() {
    var validate = ValidateInput('sample-settup-right-info');
    if (validate) {
        var code = $("#sample-code").val();
        var name = $("#sample-name").val();
        var description = CKEDITOR.instances['sample-description'].getData();
        var result = $("#sample-result").val();
        var suggest = $("#sample-suggest").val();
        var categoryid = $("#sample-category").val();
        var active = $("#sample-active").prop('checked');

        var data = {
            code: code,
            name: name,
            description: description,
            result: result,
            suggest: suggest,
            categoryid: categoryid,
            active: active
        };

        $.ajax({
            url: "/Sample/Save/",
            data: JSON.stringify(data),
            contentType: "application/json; charset=utf-8",
            dataType: "text",
            type: "POST",
            success: function (result) {
                if (result == 'True') {
                    $("#sample-code").prop('disabled', true);
                    Sample_LoadList();
                    Sample_HideButton(false, true, false, true);
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

function Sample_Delete() {
    var choice = confirm("Bạn muốn xoá mẫu kết quả này ?");
    if (choice) {
        $("#sample-code").prop('disabled', true);
        var code = $("#sample-code").val();
        if (code === "" || !code) {
            alert("Vui lòng chọn mẫu kết quả cần xoá");
        }
        else {
            $.ajax({
                url: "/Sample/Delete?code=" + code,
                type: "POST",
                dataType: "text",
                success: function (result) {
                    if (result == 'True') {
                        Sample_ResetInput();
                        Sample_LoadList();
                        Sample_HideButton(false, true, true, true);
                    }
                    else {
                        alert("Dữ liệu đã được sử dụng nên không thể xoá. Chỉ có thể InActive");
                    }
                },
                error: function () {
                    alert("Dữ liệu đã được sử dụng nên không thể xoá. Chỉ có thể InActive");
                }
            });
        }
    }
}

function Sample_Cancel() {
    $("#sample-code").prop('disabled', true);
    var code = $("#sample-code").val();
    if (code === "" || !code) {
        Sample_ResetInput();
        Sample_HideButton(false, true, true, true);
    }
    else {
        Sample_LoadInfo(code);
        Sample_HideButton(false, true, false, true);
    }
}

// ****************************************************************************** TestCode

$(function () {
    TestCode_SetSelect2(true);
})

function TestCode_SetSelect2(isLoadPage) {
    if (isLoadPage) {
        $('#testcode-category').val("");
    }
    $(document).ready(function () {
        $('#testcode-category').select2({
            placeholder: "-- Chọn --"
        });
    });

    if (isLoadPage) {
        $('#testcode-testtype').val("");
    }
    $(document).ready(function () {
        $('#testcode-testtype').select2({
            placeholder: "-- Chọn --"
        });
    });
}

function TestCode_HideButton(_new, _save, _delete, _cancel) {
    if (_new == 1) {
        $("#testcode-new").hide();
    }
    else {
        $("#testcode-new").show();
    }

    if (_save == 1) {
        $("#testcode-save").hide();
    }
    else {
        $("#testcode-save").show();
    }

    if (_delete == 1) {
        $("#testcode-delete").hide();
    }
    else {
        $("#testcode-delete").show();
    }

    if (_cancel == 1) {
        $("#testcode-cancel").hide();
    }
    else {
        $("#testcode-cancel").show();
    }
}

function TestCode_ResetInput() {
    $("#testcode-code").val("");
    $("#testcode-name").val("");
    $('#testcode-testtype').val('');
    $('#testcode-category').val('');
    $('#testcode-normalrangem').val('');
    $('#testcode-normalrangef').val('');
    $('#testcode-normalresult').val('');
    $('#testcode-lowerlimit').val('');
    $('#testcode-higherlimit').val('');
    $('#testcode-unit').val('');
    $('#testcode-printorder').val('');
    $('#testcode-codebhyt').val('');
    $('#testcode-namebhyt').val('');
    $('input:checkbox').removeAttr('checked');
    TestCode_SetSelect2(false);
}

function TestCode_Search(value) {
    $.ajax({
        url: "/TestCode/Search?value=" + value,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#testcode-settup-left-list").html(result);
        },
        error: function () {
            $("#testcode-settup-left-list").empty();
        }
    });
}

function TestCode_CheckedBoxOnRow(id) {
    $(".row-service").each(function () {
        $(this).find(".form-check-input").each(function () {
            var value = $(this).val();
            if (value == id) {
                $(this).prop("checked", true);
                TestCode_LoadInfo(id);
                TestCode_HideButton(false, true, false, true);
            }
            else {
                $(this).prop("checked", false);
            }
        })

    })
}

function TestCode_LoadList() {
    $.ajax({
        url: "/TestCode/GetList/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#testcode-settup-left-list").html(result);
        },
        error: function () {
            $("#testcode-settup-left-list").empty();
        }
    });
}

function TestCode_LoadInfo(id) {
    $.ajax({
        url: "/TestCode/GetInfo?id=" + id,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#testcode-settup-right-info").html(result);
            TestCode_SetSelect2(false);
        },
        error: function () {
            $("#testcode-settup-right-info").empty();
        }
    });
}

function TestCode_New() {
    $("#testcode-code").prop('disabled', false);
    $("#testcode-code").focus();
    TestCode_ResetInput();
    TestCode_HideButton(true, false, true, false);
}

function TestCode_Save() {
    var validate = ValidateInput('testcode-settup-right-info');
    if (validate) {
        var code = $("#testcode-code").val();
        var name = $("#testcode-name").val();
        var testtypeid = $('#testcode-testtype').val();
        var categoryid = $('#testcode-category').val();
        var normalrangem = $('#testcode-normalrangem').val();
        var normalrangef = $('#testcode-normalrangef').val();
        var normalresult = $('#testcode-normalresult').val();
        var lowerlimit = $('#testcode-lowerlimit').val();
        var higherlimit = $('#testcode-higherlimit').val();
        var unit = $('#testcode-unit').val();
        var printorder = $('#testcode-printorder').val();
        var codebhyt = $('#testcode-codebhyt').val();
        var namebhyt = $('#testcode-namebhyt').val();
        var istesthead = $("#testcode-istesthead").prop('checked');
        var istestchild = $("#testcode-istestchild").prop('checked');
        var active = $("#testcode-active").prop('checked');

        var data = {
            code: code,
            name: name,
            testtypeid: testtypeid,
            categoryid: categoryid,
            normalrangem: normalrangem,
            normalrangef: normalrangef,
            normalresult: normalresult,
            lowerlimit: lowerlimit,
            higherlimit: higherlimit,
            unit: unit,
            printorder: printorder,
            codebhyt: codebhyt,
            namebhyt: namebhyt,
            istesthead: istesthead,
            istestchild: istestchild,
            active: active
        };

        $.ajax({
            url: "/TestCode/Save/",
            data: JSON.stringify(data),
            contentType: "application/json; charset=utf-8",
            dataType: "text",
            type: "POST",
            success: function (result) {
                if (result == 'True') {
                    $("#testcode-code").prop('disabled', true);
                    TestCode_LoadList();
                    TestCode_HideButton(false, true, false, true);
                    TestCode_SetSelect2(false);
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

function TestCode_Delete() {
    var choice = confirm("Bạn muốn xoá mã xét nghiệm này ?");
    if (choice) {
        $("#testcode-code").prop('disabled', true);
        var code = $("#testcode-code").val();
        if (code === "" || !code) {
            alert("Vui lòng chọnmã xét nghiệm cần xoá");
        }
        else {
            $.ajax({
                url: "/TestCode/Delete?code=" + code,
                type: "POST",
                dataType: "text",
                success: function (result) {
                    if (result == 'True') {
                        TestCode_ResetInput();
                        TestCode_LoadList();
                        TestCode_HideButton(false, true, true, true);
                    }
                    else {
                        alert("Dữ liệu đã được sử dụng nên không thể xoá. Chỉ có thể InActive");
                    }
                },
                error: function () {
                    alert("Dữ liệu đã được sử dụng nên không thể xoá. Chỉ có thể InActive");
                }
            });
        }
    }
}

function TestCode_Cancel() {
    $("#testcode-code").prop('disabled', true);
    var code = $("#testcode-code").val();
    if (code === "" || !code) {
        TestCode_ResetInput();
        TestCode_HideButton(false, true, true, true);
    }
    else {
        TestCode_LoadInfo(code);
        TestCode_HideButton(false, true, false, true);
    }
}


// ****************************************************************************** ServiceTest

function ServiceTest_Search(value) {
    $.ajax({
        url: "/ServiceTest/Search?value=" + value,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#servicetest-settup-left-list").html(result);
        },
        error: function () {
            $("#servicetest-settup-left-list").empty();
        }
    });
}

function ServiceTest_Search_AddTestCode(value) {
    $.ajax({
        url: "/ServiceTest/Search_AddTestCode?value=" + value,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#servicetest-table-addtestcode").html(result);
        },
        error: function () {
            $("#servicetest-table-addtestcode").empty();
        }
    });
}

function ServiceTest_Service_CheckedBoxOnRow(id) {
    $(".row-servicetest-service").each(function () {
        $(this).find(".form-check-input").each(function () {
            var value = $(this).val();
            if (value == id) {
                $(this).prop("checked", true);
                ServiceTest_LoadInfo(id);
            }
            else {
                $(this).prop("checked", false);
            }
        })

    })
}

function ServiceTest_TestCode_CheckedBoxOnRow(id) {
    $(".row-servicetest-testcode").each(function () {
        $(this).find(".form-check-input").each(function () {
            var value = $(this).val();
            var checked = $(this).prop('checked');
            if (value == id) {
                if (checked == true) {
                    $(this).prop("checked", false);
                }
                else {
                    $(this).prop("checked", true);
                }
            }
        })
    })
}

function ServiceTest_LoadInfo(id) {
    $.ajax({
        url: "/ServiceTest/GetInfo?id=" + id,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#servicetest-settup-right-list").html(result);
        },
        error: function () {
            $("#servicetest-settup-right-list").empty();
        }
    });
}

function ServiceTest_Delete() {
    var choice = confirm("Bạn muốn xoá mã xét nghiệm này ?");
    if (choice) {
        var data = [];
        var serviceid = "";
        $(".row-servicetest-service").each(function () {
            $(this).find(".form-check-input").each(function () {
                var checked = $(this).prop('checked');
                if (checked == true) {
                    serviceid = $(this).val();
                }
            })
        })

        $(".row-servicetest-testcode").each(function () {
            $(this).find(".form-check-input").each(function () {
                var checked = $(this).prop('checked');
                if (checked == true) {
                    data.push({ serviceid: serviceid, testcodeid: $(this).val() });
                }
            })
        })


        if (data.length == 0) {
            alert("Vui lòng chọn mã xét nghiệm cần xoá");
        }
        else {
            $.ajax({
                url: "/ServiceTest/Delete/",
                data: JSON.stringify(data),
                contentType: "application/json; charset=utf-8",
                dataType: "text",
                type: "POST",
                success: function (result) {
                    if (result == 'True') {
                        ServiceTest_LoadInfo(serviceid);
                    }
                    else {
                        alert("Dữ liệu đã được sử dụng nên không thể xoá");
                    }
                },
                error: function () {
                    alert("Dữ liệu đã được sử dụng nên không thể xoá");
                }
            });
        }
    }
}

function ServiceTest_AddTestCode_CheckedBoxOnRow(id) {
    $(".row-servicetest-addtestcode").each(function () {
        $(this).find(".form-check-input").each(function () {
            var value = $(this).val();
            var checked = $(this).prop('checked');
            if (value == id) {
                if (checked == true) {
                    $(this).prop("checked", false);
                }
                else {
                    $(this).prop("checked", true);
                }
            }
        })
    })
}

function ServiceTest_Save() {
    var data = [];
    var serviceid = "";
    $(".row-servicetest-service").each(function () {
        $(this).find(".form-check-input").each(function () {
            var checked = $(this).prop('checked');
            if (checked == true) {
                serviceid = $(this).val();
            }
        })
    })

    $(".row-servicetest-addtestcode").each(function () {
        $(this).find(".form-check-input").each(function () {
            var checked = $(this).prop('checked');
            if (checked == true) {
                data.push({ serviceid: serviceid, testcodeid: $(this).val() });
            }
        })
    })

    if (data.length == 0) {
        alert("Vui lòng chọn mã xét nghiệm !");
    }
    else {
        $.ajax({
            url: "/ServiceTest/Save/",
            data: JSON.stringify(data),
            contentType: "application/json; charset=utf-8",
            dataType: "text",
            type: "POST",
            success: function (result) {
                if (result == 'True') {
                    ServiceTest_LoadInfo(serviceid);
                }
                else {
                    alert("Mã xét nghiệm đã được chọn. Vui lòng kiểm tra lại");
                }
            },
            error: function () {
                alert("Mã xét nghiệm đã được chọn. Vui lòng kiểm tra lại");
            }
        });
    }
}

function ServiceTest_Refresh_AddTestCode() {
    $(".row-servicetest-addtestcode").each(function () {
        $(this).find(".form-check-input").each(function () {
            $(this).prop("checked", false);
        })
    })
}

// ****************************************************************************** Map

function Map_Device_CheckedBoxOnRow(id) {
    $(".row-map-device").each(function () {
        $(this).find(".form-check-input").each(function () {
            var value = $(this).val();
            if (value == id) {
                $(this).prop("checked", true);
                Map_LoadInfo(id);
            }
            else {
                $(this).prop("checked", false);
            }
        })

    })
}

function Map_TestCode_CheckedBoxOnRow(id) {
    $(".row-map-testcode").each(function () {
        $(this).find(".form-check-input").each(function () {
            var value = $(this).val();
            var checked = $(this).prop('checked');
            if (value == id) {
                if (checked == true) {
                    $(this).prop("checked", false);
                }
                else {
                    $(this).prop("checked", true);
                }
            }
        })
    })
}

function Map_LoadInfo(id) {
    $.ajax({
        url: "/Map/GetInfo?id=" + id,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#map-settup-right-list").html(result);
        },
        error: function () {
            $("#map-settup-right-list").empty();
        }
    });
}

function Map_Delete() {
    var choice = confirm("Bạn muốn xoá mã xét nghiệm này ?");
    if (choice) {
        var deviceid = "";
        $(".row-map-device").each(function () {
            $(this).find(".form-check-input").each(function () {
                var checked = $(this).prop('checked');
                if (checked == true) {
                    deviceid = $(this).val();
                }
            })
        })

        var data = [];
        $(".row-map-testcode").each(function () {
            $(this).find(".form-check-input").each(function () {
                var checked = $(this).prop('checked');
                if (checked == true) {
                    data.push({ id: $(this).val() });
                }
            })
        })

        if (data.length == 0) {
            alert("Vui lòng chọn mã xét nghiệm cần xoá");
        }
        else {
            $.ajax({
                url: "/Map/Delete/",
                data: JSON.stringify(data),
                contentType: "application/json; charset=utf-8",
                dataType: "text",
                type: "POST",
                success: function (result) {
                    if (result == 'True') {
                        Map_LoadInfo(deviceid);
                    }
                    else {
                        alert("Dữ liệu đã được sử dụng nên không thể xoá");
                    }
                },
                error: function () {
                    alert("Dữ liệu đã được sử dụng nên không thể xoá");
                }
            });
        }
    }
}

function Map_AddTestCode_New() {
    var deviceid = "";
    $(".row-map-device").each(function () {
        $(this).find(".form-check-input").each(function () {
            var checked = $(this).prop('checked');
            if (checked == true) {
                deviceid = $(this).val();
            }
        })
    })

    if (deviceid === "" || !deviceid) {
        $('#addTestCode').modal('hide');
        alert("Vui lòng chọn thiết bị !");
    }
    else {
        Map_SetSelect2();
        $("#map-addtestcode-id").val('');
        $("#map-addtestcode-testcode").val('');
        $("#map-addtestcode-testcodein").val('');
        $("#map-addtestcode-testcodein2").val('');
        $("#map-addtestcode-note").val('');
        $("#map-addtestcode-formatnumber").val('');
    }
}


function Map_AddTestCode_Edit() {
    var id = "";
    $(".row-map-testcode").each(function () {
        $(this).find(".form-check-input").each(function () {
            var checked = $(this).prop('checked');
            if (checked == true) {
                id = $(this).val();
            }
        })
    })

    if (id === "" || !id) {
        $('#addTestCode').modal('hide');
        alert("Vui lòng chọn mã xét nghiệm máy cần sửa !");
    }
    else {
        Map_SetSelect2();
        $.ajax({
            url: "/Map/GetMap?id=" + id,
            type: "GET",
            dataType: "json",
            success: function (result) {
                $("#map-addtestcode-id").val(result.id);
                $('#map-addtestcode-testcode').val(result.testcodeid);
                $('#map-addtestcode-testcode').trigger('change');
                $("#map-addtestcode-testcodein").val(result.testcodein);
                $("#map-addtestcode-testcodein2").val(result.testcodein2);
                $("#map-addtestcode-note").val(result.note);
                $("#map-addtestcode-formatnumber").val(result.formatnumber);
            }
        });
    }
}

function Map_Save() {
    var validate = ValidateInput('map-addtestcode-content');
    if (validate) {
        var deviceid = "";
        $(".row-map-device").each(function () {
            $(this).find(".form-check-input").each(function () {
                var checked = $(this).prop('checked');
                if (checked == true) {
                    deviceid = $(this).val();
                }
            })
        })

        var id = $("#map-addtestcode-id").val();
        if (id === "" || !id) id = 0;
        var testcodeid = $("#map-addtestcode-testcode").val();
        var testcodein = $("#map-addtestcode-testcodein").val();
        var testcodein2 = $("#map-addtestcode-testcodein2").val();
        var note = $("#map-addtestcode-note").val();
        var formatnumber = $("#map-addtestcode-formatnumber").val();
        var data = { id: id, deviceid: deviceid, testcodeid: testcodeid, testcodein: testcodein, testcodein2: testcodein2, note: note, formatnumber: formatnumber };

        $.ajax({
            url: "/Map/Save/",
            data: JSON.stringify(data),
            contentType: "application/json; charset=utf-8",
            dataType: "text",
            type: "POST",
            success: function (result) {
                if (result == 'True') {
                    Map_LoadInfo(deviceid);
                }
                else {
                    alert("Không thể lưu. Vui lòng kiểm tra lại !");
                }
            },
            error: function () {
                alert("Không thể lưu. Vui lòng kiểm tra lại !");
            }
        });
    }
    else {
        alert("Không thể lưu. Vui lòng kiểm tra lại !");
    }
}


// ****************************************************************************** Setting

function Setting_HideButton(_new, _save, _delete, _cancel) {
    if (_new == 1) {
        $("#setting-new").hide();
    }
    else {
        $("#setting-new").show();
    }

    if (_save == 1) {
        $("#setting-save").hide();
    }
    else {
        $("#setting-save").show();
    }

    if (_delete == 1) {
        $("#setting-delete").hide();
    }
    else {
        $("#setting-delete").show();
    }

    if (_cancel == 1) {
        $("#setting-cancel").hide();
    }
    else {
        $("#setting-cancel").show();
    }
}

function Setting_ResetInput() {
    $("#setting-code").val("");
    $("#setting-name").val("");
    $("#setting-value").val("");
}

function Setting_Search(value) {
    $.ajax({
        url: "/Setting/Search?value=" + value,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#setting-settup-left-list").html(result);
        },
        error: function () {
            $("#setting-settup-left-list").empty();
        }
    });
}

function Setting_CheckedBoxOnRow(code) {
    $(".row-setting").each(function () {
        $(this).find(".form-check-input").each(function () {
            var value = $(this).val();
            if (value == code) {
                $(this).prop("checked", true);
                Setting_LoadInfo(code);
                Setting_HideButton(false, true, false, true);
            }
            else {
                $(this).prop("checked", false);
            }
        })

    })
}

function Setting_LoadList() {
    $.ajax({
        url: "/Setting/GetList/",
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#setting-settup-left-list").html(result);
        },
        error: function () {
            $("#setting-settup-left-list").empty();
        }
    });
}

function Setting_LoadInfo(code) {
    $.ajax({
        url: "/Setting/GetInfo?code=" + code,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#setting-settup-right-info").html(result);
        },
        error: function () {
            $("#setting-settup-right-info").empty();
        }
    });
}

function Setting_New() {
    $("#setting-code").prop('disabled', false);
    $("#setting-code").focus();
    Setting_ResetInput();
    Setting_HideButton(true, false, true, false);
}

function Setting_Save() {
    var validate = ValidateInput('setting-settup-right-info');
    if (validate) {
        var code = $("#setting-code").val();
        var name = $("#setting-name").val();
        var value = $("#setting-value").val();

        var data = {
            code: code,
            name: name,
            value: value
        };

        $.ajax({
            url: "/Setting/Save/",
            data: JSON.stringify(data),
            contentType: "application/json; charset=utf-8",
            dataType: "text",
            type: "POST",
            success: function (result) {
                if (result == 'True') {
                    $("#setting-code").prop('disabled', true);
                    Setting_LoadList();
                    Setting_HideButton(false, true, false, true);
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

function Setting_Delete() {
    var choice = confirm("Bạn muốn xoá cấu hình này ?");
    if (choice) {
        $("#setting-code").prop('disabled', true);
        var code = $("#setting-code").val();
        if (code === "" || !code) {
            alert("Vui lòng chọn cấu hình cần xoá");
        }
        else {
            $.ajax({
                url: "/Setting/Delete?code=" + code,
                type: "POST",
                dataType: "text",
                success: function (result) {
                    if (result == 'True') {
                        Setting_ResetInput();
                        Setting_LoadList();
                        Setting_HideButton(false, true, true, true);
                    }
                    else {
                        alert("Dữ liệu đã được sử dụng nên không thể xoá. Chỉ có thể InActive");
                    }
                },
                error: function () {
                    alert("Dữ liệu đã được sử dụng nên không thể xoá. Chỉ có thể InActive");
                }
            });
        }
    }
}

function Setting_Cancel() {
    $("#setting-code").prop('disabled', true);
    var code = $("#setting-code").val();
    if (code === "" || !code) {
        Setting_ResetInput();
        Setting_HideButton(false, true, true, true);
    }
    else {
        Setting_LoadInfo(code);
        Setting_HideButton(false, true, false, true);
    }
}

function Setting_Start_AutoTask() {
    $.ajax({
        url: "/Setting/Setting_Start_AutoTask/",
        type: "GET",
        dataType: "text",
        success: function (result) {
            if (result == 'False') {
                alert("Không thể Start Auto Task. Vui lòng kiểm tra lại!");
            }
            else {
                alert("Start Auto Task thành công !");
            }
        },
        error: function () {
            alert("Không thể Start Auto Task. Vui lòng kiểm tra lại!");
        }
    }); 
}

function Setting_Stop_AutoTask() {
    $.ajax({
        url: "/Setting/Setting_Stop_AutoTask/",
        type: "GET",
        dataType: "text",
        success: function (result) {
            if (result == 'False') {
                alert("Không thể Stop Auto Task. Vui lòng kiểm tra lại!");
            }
            else {
                alert("Stop Auto Task thành công !");
            }
        },
        error: function () {
            alert("Không thể Stop Auto Task. Vui lòng kiểm tra lại!");
        }
    });
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
        url: "/Data/Search?" + "from=" + from + "&to=" + to + "&deviceid=" + deviceid + "&seq=" + seq,
        type: "GET",
        dataType: "html",
        cache: false,
        success: function (result) {
            $("#resultstandard-settup-left-list").html(result);
        },
        error: function () {
            $("#resultstandard-settup-left-list").empty();
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
            url: "/Data/UpdateStatus?from=" + from + "&to=" + to + "&seq=" + seq + "&status=" + status,
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