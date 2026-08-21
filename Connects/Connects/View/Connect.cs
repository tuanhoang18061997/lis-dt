using Connects.BL;
using Connects.Models;
using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using Type = System.Type;

namespace Connects
{
    public partial class frmConnect : Form
    {
        #region ****************************************************************************************** PROPERTY
        private readonly ILog log4net = LogManager.GetLogger(typeof(frmConnect).Name);
        private long deviceId = 0;
        private SerialPort SerialPort_01 = new SerialPort();
        private SerialPort SerialPort_02 = new SerialPort();
        private SerialPort SerialPort_03 = new SerialPort();
        private SerialPort SerialPort_04 = new SerialPort();
        private SerialPort SerialPort_05 = new SerialPort();
        private SerialPort SerialPort_06 = new SerialPort();

        private bool isConnect01 = false;
        private bool isConnect02 = false;
        private bool isConnect03 = false;
        private bool isConnect04 = false;
        private bool isConnect05 = false;
        private bool isConnect06 = false;

        private string computer = string.Empty;
        private string isCOM = "COM";
        private string nameSpace = "Connects.Devices";
        private string method = "AnalysisPatient";
        private string clear = "Clear";
        private string method_WriteFile = "WriteFile";
        private string methodIP_Start = "Start";
        private string methodIP_Close = "Close";
        private string method_Clear = "Clear";
        private string method_CheckConnect = "CheckConnect";
        private string startTimer = "StartTimer";
        private string stopTimer = "StopTimer";
        private string receving = "Receving ...";
        private string waiting = "Waiting ...";

        private Timer timerAutoUpdateTime = new Timer();
        private Timer timerAutoGetPatientInfo = new Timer();
        private Timer timerAutoPushResultBHYT = new Timer();

        private Timer timer1;
        private decimal count1 = 0;
        private decimal timerCount1 = 0;

        private Timer timer2;
        private decimal count2 = 0;
        private decimal timerCount2 = 0;

        private Timer timer3;
        private decimal count3 = 0;
        private decimal timerCount3 = 0;

        private Timer timer4;
        private decimal count4 = 0;
        private decimal timerCount4 = 0;

        private Timer timer5;
        private decimal count5 = 0;
        private decimal timerCount5 = 0;

        private Timer timer6;
        private decimal count6 = 0;
        private decimal timerCount6 = 0;

        private bool isConnectOk = false;
        #endregion

        public frmConnect()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
            this.CheckConnectString();
        }

        #region ****************************************************************************************** EVENT

        private void frmConnect_Load(object sender, EventArgs e)
        {
            if (this.isConnectOk)
            {
                this.lblComputerName.Text = string.Empty;
                this.lbUser.Text = string.Empty;
                this.lbVersion.Text = string.Empty;

                this.computer = SystemInformation.ComputerName;

                // Xóa tất cả cổng COM không còn tồn tại trên phần cứng
                this.DeleteAllComNotExistOnComputer();

                // Cập nhật color - Cập nhật Status
                this.Update_Status_Color();

                // Load thông tin cổng COM và máy xét nghiệm đã chọn
                this.LoadComInfoAndSelectedDevice();

                // Load form login
                this.LoadLogin();
                SessionBL.Reset_Session();
            }
        }

        private void FrmConnectString_FormClosing(object sender, FormClosingEventArgs e)
        {
            frmConnectString frm = sender as frmConnectString;
            if (frm.isOk)
            {
                this.isConnectOk = true;
                this.InitializeEvent();
                this.InitializeTimer();
                this.frmConnect_Load(null, null);
            }
            else
            {
                Application.Exit();
            }
        }

        private void SerialPort_01_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            this.CallMethodProcess_1(sp, 1);
            this.count1++;
        }

        private void SerialPort_02_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            this.CallMethodProcess_2(sp, 2);
            this.count2++;
        }

        private void SerialPort_03_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            this.CallMethodProcess_3(sp, 3);
            this.count3++;
        }

        private void SerialPort_04_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            this.CallMethodProcess_4(sp, 4);
            this.count4++;
        }

        private void SerialPort_05_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            this.CallMethodProcess_5(sp, 5);
            this.count5++;
        }

        private void SerialPort_06_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            this.CallMethodProcess_6(sp, 6);
            this.count6++;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (this.count1 == 0 || this.timerCount1 == this.count1)
            {
                this.txtStatus1.Text = "Waiting ...";
                this.timerCount1 = this.count1 = 0;
            }
            else
                this.txtStatus1.Text = "Receving ...";
            this.timerCount1 = this.count1;
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (this.count2 == 0 || this.timerCount2 == this.count2)
            {
                this.txtStatus2.Text = "Waiting ...";
                this.timerCount2 = this.count2 = 0;
            }
            else
                this.txtStatus2.Text = "Receving ...";
            this.timerCount2 = this.count2;
        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            if (this.count3 == 0 || this.timerCount3 == this.count3)
            {
                this.txtStatus3.Text = "Waiting ...";
                this.timerCount3 = this.count3 = 0;
            }
            else
                this.txtStatus3.Text = "Receving ...";
            this.timerCount3 = this.count3;
        }

        private void timer4_Tick(object sender, EventArgs e)
        {
            if (this.count4 == 0 || this.timerCount4 == this.count4)
            {
                this.txtStatus4.Text = "Waiting ...";
                this.timerCount4 = this.count4 = 0;
            }
            else
                this.txtStatus4.Text = "Receving ...";
            this.timerCount4 = this.count4;
        }

        private void timer5_Tick(object sender, EventArgs e)
        {
            if (this.count5 == 0 || this.timerCount5 == this.count5)
            {
                this.txtStatus5.Text = "Waiting ...";
                this.timerCount5 = this.count5 = 0;
            }
            else
                this.txtStatus5.Text = "Receving ...";
            this.timerCount5 = this.count5;
        }

        private void timer6_Tick(object sender, EventArgs e)
        {
            if (this.count6 == 0 || this.timerCount6 == this.count6)
            {
                this.txtStatus6.Text = "Waiting ...";
                this.timerCount6 = this.count6 = 0;
            }
            else
                this.txtStatus6.Text = "Receving ...";
            this.timerCount6 = this.count6;
        }

        private void btnCauHinhCOM_Click(object sender, EventArgs e)
        {
            string computer = SystemInformation.ComputerName;
            string[] lstCom = SerialPort.GetPortNames();
            bool existConnect = false;
            int port = this.GetPort();

            if (port != 0)
            {
                var connect = ConnectBL.Get_Connect(port, computer);
                if (connect != null)
                {
                    if (connect.Status)
                    {
                        MessageBox.Show("Vui lòng ngắt kết nối !", "Thông báo", MessageBoxButtons.OK);
                        return;
                    }
                }
            }

            // Kiểm tra xem tất cả cổng COM đã được kết nối hay chưa
            var lstConnect = ConnectBL.Get_ListConnect(computer, true);
            if (lstCom != null && lstCom.Length > 0)
            {
                foreach (var com in lstCom)
                {
                    var exit = lstConnect.Where(p => p.Com == com).FirstOrDefault();
                    if (exit == null)
                    {
                        existConnect = true;
                    }
                }
            }

            if (existConnect)
            {
                try
                {
                    if (port != 0)
                    {
                        var connect = ConnectBL.Get_Connect(port, computer);
                        if (connect != null)
                        {
                            if (connect.Status)
                            {
                                MessageBox.Show("Vui lòng ngắt kết nối !", "Thông báo", MessageBoxButtons.OK);
                            }
                            else
                            {
                                frmConfigCom frmConfigCom = new frmConfigCom(port);
                                frmConfigCom.FormClosing += frmConfigCom_FormClosing;
                                frmConfigCom.Show();
                                this.Enabled = false;
                            }
                        }
                        else
                        {
                            frmConfigCom frmConfigCom = new frmConfigCom(port);
                            frmConfigCom.FormClosing += frmConfigCom_FormClosing;
                            frmConfigCom.Show();
                            this.Enabled = false;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Vui lòng chọn cổng !", "Thông báo", MessageBoxButtons.OK);
                    }
                }
                catch
                {
                    MessageBox.Show("Cổng COM không tồn tại !", "Thông báo", MessageBoxButtons.OK);
                }
            }
            else
            {
                MessageBox.Show("Không thể cấu hình. Vì Tất cả cổng COM đã được kết nối !", "Thông báo", MessageBoxButtons.OK);
            }
        }

        private void frmConfigCom_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Enabled = true;
            this.computer = SystemInformation.ComputerName;
            int port = this.GetPort();

            if (port != 0)
            {
                var connect = ConnectBL.Get_Connect(port, computer);
                if (connect != null)
                {
                    string setting = connect.Com + " Setting : " + connect.BaudRate + ", " + connect.Parity + ", " + connect.DataBit + ", " + connect.StopBit;

                    if (rdoPort1.Checked)
                    {
                        txtParametersCom1.Text = setting;
                    }
                    else if (rdoPort2.Checked)
                    {
                        txtParametersCom2.Text = setting;
                    }
                    else if (rdoPort3.Checked)
                    {
                        txtParametersCom3.Text = setting;
                    }
                    else if (rdoPort4.Checked)
                    {
                        txtParametersCom4.Text = setting;
                    }
                    else if (rdoPort5.Checked)
                    {
                        txtParametersCom5.Text = setting;
                    }
                    else if (rdoPort6.Checked)
                    {
                        txtParametersCom6.Text = setting;
                    }
                }
            }
        }

        private void btnChonMayXN_Click(object sender, EventArgs e)
        {
            this.computer = SystemInformation.ComputerName;
            try
            {
                int port = this.GetPort();
                if (port != 0)
                {
                    var connect = ConnectBL.Get_Connect(port, computer);
                    if (connect != null)
                    {
                        if (connect.Status)
                        {
                            MessageBox.Show("Vui lòng ngắt kết nối !", "Thông báo", MessageBoxButtons.OK);
                        }
                        else
                        {
                            frmDevice frmDevice = new frmDevice(port, this.computer);
                            frmDevice.FormClosing += frmDevice_FormClosing;
                            frmDevice.Show();
                            this.Enabled = false;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Vui lòng cấu hình COM !", "Thông báo", MessageBoxButtons.OK);
                    }
                }
                else
                {
                    MessageBox.Show("Vui lòng chọn cổng !", "Thông báo", MessageBoxButtons.OK);
                }
            }
            catch
            {
                MessageBox.Show("Cổng COM không tồn tại !", "Thông báo", MessageBoxButtons.OK);
            }
        }

        private void frmDevice_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Enabled = true;
            var frmDevice = sender as frmDevice;
            this.deviceId = frmDevice.idDevice;
            this.computer = SystemInformation.ComputerName;
            int port = 0;
            if (!frmDevice.isSelect)
            {
                var device = DeviceBL.Get_Device(this.deviceId);
                if (device == null)
                {
                    if (rdoPort1.Checked)
                    {
                        txtDeviceName1.Text = string.Empty;
                        txtDeviceID1.Text = string.Empty;
                        txtProtocol1.Text = string.Empty;
                    }
                    if (rdoPort2.Checked)
                    {
                        txtDeviceName2.Text = string.Empty;
                        txtDeviceID2.Text = string.Empty;
                        txtProtocol2.Text = string.Empty;
                    }
                    if (rdoPort3.Checked)
                    {
                        txtDeviceName3.Text = string.Empty;
                        txtDeviceID3.Text = string.Empty;
                        txtProtocol3.Text = string.Empty;
                    }
                    if (rdoPort4.Checked)
                    {
                        txtDeviceName4.Text = string.Empty;
                        txtDeviceID4.Text = string.Empty;
                        txtProtocol4.Text = string.Empty;
                    }
                    if (rdoPort5.Checked)
                    {
                        txtDeviceName5.Text = string.Empty;
                        txtDeviceID5.Text = string.Empty;
                        txtProtocol5.Text = string.Empty;
                    }
                    if (rdoPort6.Checked)
                    {
                        txtDeviceName6.Text = string.Empty;
                        txtDeviceID6.Text = string.Empty;
                        txtProtocol6.Text = string.Empty;
                    }
                }
                return;
            }

            var device01 = DeviceBL.Get_Device(deviceId);
            if (device01 != null)
            {
                // Lưu Id máy xét nghiệm vào table COM
                port = this.GetPort();
                if (port != 0)
                {
                    var connect = ConnectBL.Get_Connect(port, computer);
                    if (connect != null)
                    {
                        ConnectBL.Update_DeviceIdAndStatus(connect.ComputerId, connect.PortId, device01.Id, false);
                    }
                }

                // Load thông tin máy xét nghiệm đã chọn lên form kết nối
                if (rdoPort1.Checked)
                {
                    txtDeviceName1.Text = device01.Name;
                    txtDeviceID1.Text = device01.Id.ToString();
                    txtStatus1.Text = string.Empty;
                    pStatus1.BackColor = Color.Red;
                    txtProtocol1.Text = device01.Protocol;
                }
                if (rdoPort2.Checked)
                {
                    txtDeviceName2.Text = device01.Name;
                    txtDeviceID2.Text = device01.Id.ToString();
                    txtStatus2.Text = string.Empty;
                    pStatus2.BackColor = Color.Red;
                    txtProtocol2.Text = device01.Protocol;
                }
                if (rdoPort3.Checked)
                {
                    txtDeviceName3.Text = device01.Name;
                    txtDeviceID3.Text = device01.Id.ToString();
                    txtStatus3.Text = string.Empty;
                    pStatus3.BackColor = Color.Red;
                    txtProtocol3.Text = device01.Protocol;
                }
                if (rdoPort4.Checked)
                {
                    txtDeviceName4.Text = device01.Name;
                    txtDeviceID4.Text = device01.Id.ToString();
                    txtStatus4.Text = string.Empty;
                    pStatus4.BackColor = Color.Red;
                    txtProtocol4.Text = device01.Protocol;

                }
                if (rdoPort5.Checked)
                {
                    txtDeviceName5.Text = device01.Name;
                    txtDeviceID5.Text = device01.Id.ToString();
                    txtStatus5.Text = string.Empty;
                    pStatus5.BackColor = Color.Red;
                    txtProtocol5.Text = device01.Protocol;
                }
                if (rdoPort6.Checked)
                {
                    txtDeviceName6.Text = device01.Name;
                    txtDeviceID6.Text = device01.Id.ToString();
                    txtStatus6.Text = string.Empty;
                    pStatus6.BackColor = Color.Red;
                    txtProtocol6.Text = device01.Protocol;
                }
            }
            else
            {
                // Xóa text
                if (rdoPort1.Checked)
                {
                    txtDeviceName1.Text = string.Empty;
                    txtDeviceID1.Text = string.Empty;
                    txtProtocol1.Text = string.Empty;
                }
                if (rdoPort2.Checked)
                {
                    txtDeviceName2.Text = string.Empty;
                    txtDeviceID2.Text = string.Empty;
                    txtProtocol2.Text = string.Empty;
                }
                if (rdoPort3.Checked)
                {
                    txtDeviceName3.Text = string.Empty;
                    txtDeviceID3.Text = string.Empty;
                    txtProtocol3.Text = string.Empty;
                }
                if (rdoPort4.Checked)
                {
                    txtDeviceName4.Text = string.Empty;
                    txtDeviceID4.Text = string.Empty;
                    txtProtocol4.Text = string.Empty;
                }
                if (rdoPort5.Checked)
                {
                    txtDeviceName5.Text = string.Empty;
                    txtDeviceID5.Text = string.Empty;
                    txtProtocol5.Text = string.Empty;
                }
                if (rdoPort6.Checked)
                {
                    txtDeviceName6.Text = string.Empty;
                    txtDeviceID6.Text = string.Empty;
                    txtProtocol6.Text = string.Empty;
                }

                // Xóa IdMayXN trong table COM
                port = this.GetPort();
                if (port != 0)
                {
                    var connect = ConnectBL.Get_Connect(port, computer);
                    if (connect != null)
                    {
                        ConnectBL.Update_DeviceIdAndStatus(connect.ComputerId, connect.PortId, 0, false);
                    }
                }
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnClose2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            this.computer = SystemInformation.ComputerName;
            int port = this.GetPort();

            if (port == 0)
            {
                MessageBox.Show("Vui lòng chọn cổng !", "Thông báo", MessageBoxButtons.OK);
            }
            else
            {
                var connect = ConnectBL.Get_Connect(port, computer);
                if (connect != null)
                {
                    if (rdoPort1.Checked)
                    {
                        if (connect.Com.Contains(isCOM))
                            this.Connect(SerialPort_01, connect);
                        else
                            this.Open_Server_Or_Client(connect);

                        this.isConnect01 = true;
                    }
                    if (rdoPort2.Checked)
                    {
                        if (connect.Com.Contains(isCOM))
                            this.Connect(SerialPort_02, connect);
                        else
                            this.Open_Server_Or_Client(connect);

                        this.isConnect02 = true;
                    }
                    if (rdoPort3.Checked)
                    {
                        if (connect.Com.Contains(isCOM))
                            this.Connect(SerialPort_03, connect);
                        else
                            this.Open_Server_Or_Client(connect);

                        this.isConnect03 = true;
                    }
                    if (rdoPort4.Checked)
                    {
                        if (connect.Com.Contains(isCOM))
                            this.Connect(SerialPort_04, connect);
                        else
                            this.Open_Server_Or_Client(connect);

                        this.isConnect04 = true;
                    }
                    if (rdoPort5.Checked)
                    {
                        if (connect.Com.Contains(isCOM))
                            this.Connect(SerialPort_05, connect);
                        else
                            this.Open_Server_Or_Client(connect);

                        this.isConnect05 = true;
                    }
                    if (rdoPort6.Checked)
                    {
                        if (connect.Com.Contains(isCOM))
                            this.Connect(SerialPort_06, connect);
                        else
                            this.Open_Server_Or_Client(connect);

                        this.isConnect06 = true;
                    }
                }
            }
        }

        private void btnDisConnect_Click(object sender, EventArgs e)
        {
            int port = this.GetPort();
            DialogResult result = MessageBox.Show("Bạn muốn ngắt kết nối cổng " + port + " ?", "Thông báo !", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (DialogResult.Yes == result)
            {

                this.computer = SystemInformation.ComputerName;
                try
                {
                    if (port == 0)
                    {
                        MessageBox.Show("Vui lòng chọn cổng !", "Thông báo", MessageBoxButtons.OK);
                    }
                    else
                    {
                        var connect = ConnectBL.Get_Connect(port, computer);
                        if (connect != null)
                        {
                            if (rdoPort1.Checked)
                            {
                                if (connect.Com.Contains(isCOM))
                                    this.DisConnect(SerialPort_01, connect);
                                else
                                    this.Close_Server_Or_Client(connect);

                                this.isConnect01 = false;
                            }
                            if (rdoPort2.Checked)
                            {
                                if (connect.Com.Contains(isCOM))
                                    this.DisConnect(SerialPort_02, connect);
                                else
                                    this.Close_Server_Or_Client(connect);

                                this.isConnect02 = false;
                            }
                            if (rdoPort3.Checked)
                            {
                                if (connect.Com.Contains(isCOM))
                                    this.DisConnect(SerialPort_03, connect);
                                else
                                    this.Close_Server_Or_Client(connect);

                                this.isConnect03 = false;
                            }
                            if (rdoPort4.Checked)
                            {
                                if (connect.Com.Contains(isCOM))
                                    this.DisConnect(SerialPort_04, connect);
                                else
                                    this.Close_Server_Or_Client(connect);

                                this.isConnect04 = false;
                            }
                            if (rdoPort5.Checked)
                            {
                                if (connect.Com.Contains(isCOM))
                                    this.DisConnect(SerialPort_05, connect);
                                else
                                    this.Close_Server_Or_Client(connect);

                                this.isConnect05 = false;
                            }
                            if (rdoPort6.Checked)
                            {
                                if (connect.Com.Contains(isCOM))
                                    this.DisConnect(SerialPort_06, connect);
                                else
                                    this.Close_Server_Or_Client(connect);

                                this.isConnect06 = false;
                            }
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("Không thể đóng kết nối. Vui lòng thử lại !", "Thông báo", MessageBoxButtons.OK);
                }
            }
            else
                return;
        }

        private void rdoPort_CheckedChanged(object sender, EventArgs e)
        {
            this.computer = SystemInformation.ComputerName;
            int port = this.GetPort();

            var connect = ConnectBL.Get_Connect(port, computer);
            if (connect != null)
            {
                if (connect.Status)
                {
                    btnConnect.Enabled = false;
                    btnDisconnect.Enabled = true;
                }
                else
                {
                    btnConnect.Enabled = true;
                    btnDisconnect.Enabled = false;
                }
            }
        }

        private void btnConnectAll_Click(object sender, EventArgs e)
        {
            var lstConnect = ConnectBL.Get_ListConnectAll(SystemInformation.ComputerName);
            if (lstConnect == null || lstConnect.Count == 0) return;

            foreach (var connect in lstConnect)
            {
                if (connect != null)
                {
                    if (connect.PortId == int.Parse(rdoPort1.Tag.ToString()) && this.isConnect01 == false)
                    {
                        if (connect.Com.Contains(isCOM))
                            this.ConnectAll(SerialPort_01, connect);
                        else
                            this.Open_Server_Or_Client(connect);

                        this.isConnect01 = true;
                    }
                    if (connect.PortId == int.Parse(rdoPort2.Tag.ToString()) && this.isConnect02 == false)
                    {
                        if (connect.Com.Contains(isCOM))
                            this.ConnectAll(SerialPort_02, connect);
                        else
                            this.Open_Server_Or_Client(connect);

                        this.isConnect02 = true;
                    }
                    if (connect.PortId == int.Parse(rdoPort3.Tag.ToString()) && this.isConnect03 == false)
                    {
                        if (connect.Com.Contains(isCOM))
                            this.ConnectAll(SerialPort_03, connect);
                        else
                            this.Open_Server_Or_Client(connect);

                        this.isConnect03 = true;
                    }
                    if (connect.PortId == int.Parse(rdoPort4.Tag.ToString()) && this.isConnect04 == false)
                    {
                        if (connect.Com.Contains(isCOM))
                            this.ConnectAll(SerialPort_04, connect);
                        else
                            this.Open_Server_Or_Client(connect);

                        this.isConnect04 = true;
                    }
                    if (connect.PortId == int.Parse(rdoPort5.Tag.ToString()) && this.isConnect05 == false)
                    {
                        if (connect.Com.Contains(isCOM))
                            this.ConnectAll(SerialPort_05, connect);
                        else
                            this.Open_Server_Or_Client(connect);

                        this.isConnect05 = true;
                    }
                    if (connect.PortId == int.Parse(rdoPort6.Tag.ToString()) && this.isConnect06 == false)
                    {
                        if (connect.Com.Contains(isCOM))
                            this.ConnectAll(SerialPort_06, connect);
                        else
                            this.Open_Server_Or_Client(connect);

                        this.isConnect06 = true;
                    }

                    this.btnConnect.Enabled = false;
                    this.btnDisconnect.Enabled = true;
                }
            }
        }

        private void btnDisConnectAll_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Bạn muốn ngắt kết nối tất cả các cổng ?", "Thông báo !", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (DialogResult.Yes == result)
            {
                var lstConnect = ConnectBL.Get_ListConnectAll(SystemInformation.ComputerName);
                if (lstConnect == null) return;

                foreach (var connect in lstConnect)
                {
                    if (connect != null)
                    {
                        if (connect.PortId == int.Parse(rdoPort1.Tag.ToString()) && this.isConnect01 == true)
                        {
                            if (connect.Com.Contains(isCOM))
                                this.DisConnectAll(SerialPort_01, connect);
                            else
                                this.Close_Server_Or_Client(connect);

                            this.isConnect01 = false;
                        }                          
                        if (connect.PortId == int.Parse(rdoPort2.Tag.ToString()) && this.isConnect02 == true)
                        {
                            if (connect.Com.Contains(isCOM))
                                this.DisConnectAll(SerialPort_02, connect);
                            else
                                this.Close_Server_Or_Client(connect);

                            this.isConnect02 = false;
                        }
                        if (connect.PortId == int.Parse(rdoPort3.Tag.ToString()) && this.isConnect03 == true)
                        {
                            if (connect.Com.Contains(isCOM))
                                this.DisConnectAll(SerialPort_03, connect);
                            else
                                this.Close_Server_Or_Client(connect);

                            this.isConnect03 = false;
                        }
                        if (connect.PortId == int.Parse(rdoPort4.Tag.ToString()) && this.isConnect04 == true)
                        {
                            if (connect.Com.Contains(isCOM))
                                this.DisConnectAll(SerialPort_04, connect);
                            else
                                this.Close_Server_Or_Client(connect);

                            this.isConnect04 = false;
                        }
                        if (connect.PortId == int.Parse(rdoPort5.Tag.ToString()) && this.isConnect05 == true)
                        {
                            if (connect.Com.Contains(isCOM))
                                this.DisConnectAll(SerialPort_05, connect);
                            else
                                this.Close_Server_Or_Client(connect);

                            this.isConnect05 = false;
                        }
                        if (connect.PortId == int.Parse(rdoPort6.Tag.ToString()) && this.isConnect06 == true)
                        {
                            if (connect.Com.Contains(isCOM))
                                this.DisConnectAll(SerialPort_06, connect);
                            else
                                this.Close_Server_Or_Client(connect);

                            this.isConnect06 = false;
                        }

                        this.btnConnect.Enabled = true;
                        this.btnDisconnect.Enabled = false;
                    }
                }
            }
            else
                return;
        }

        private void frmConnect_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.isConnectOk)
            {
                var lstConnect = ConnectBL.Get_ListConnectAll(SystemInformation.ComputerName);
                if (lstConnect != null)
                {
                    var connect = lstConnect.Where(p => p.Status == true).FirstOrDefault();
                    if (connect != null)
                    {
                        MessageBox.Show("Vui lòng đóng tất cả các kết nối !", "Thông báo", MessageBoxButtons.OK);
                        e.Cancel = true;
                    }
                    else
                    {
                        SessionBL.UpdateStatus(SystemInformation.ComputerName, false);
                    }
                }
            }
            else
            {
                SessionBL.UpdateStatus(SystemInformation.ComputerName, false);
            }
        }

        private void FrmLogin_FormClosing(object sender, FormClosingEventArgs e)
        {
            var frmLogin = sender as frmLogin;
            if (frmLogin.isLogin)
            {
                this.Enabled = true;
                this.Set_User();
                this.HideConfigFunction();
            }
            else
            {
                Application.Exit();
            }
        }

        private void btnConfigTCP_Click(object sender, EventArgs e)
        {
            this.computer = SystemInformation.ComputerName;

            try
            {
                int port = this.GetPort();
                if (port != 0)
                {
                    var connect = ConnectBL.Get_Connect(port, computer);
                    if (connect != null)
                    {
                        if (connect.Status)
                        {
                            MessageBox.Show("Vui lòng ngắt kết nối !", "Thông báo", MessageBoxButtons.OK);
                        }
                        else
                        {
                            frmConfigTCP frmConfigForIStat = new frmConfigTCP(port);
                            frmConfigForIStat.FormClosing += FrmConfigTCP_FormClosing;
                            frmConfigForIStat.Show();
                            this.Enabled = false;
                        }
                    }
                    else
                    {
                        frmConfigTCP frmConfigForIStat = new frmConfigTCP(port);
                        frmConfigForIStat.FormClosing += FrmConfigTCP_FormClosing;
                        frmConfigForIStat.Show();
                        this.Enabled = false;
                    }
                }
                else
                {
                    MessageBox.Show("Vui lòng chọn cổng !", "Thông báo", MessageBoxButtons.OK);
                }
            }
            catch
            {
                MessageBox.Show(" Cấu hình chưa đúng !", "Thông báo", MessageBoxButtons.OK);
            }
        }

        private void FrmConfigTCP_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Enabled = true;
            this.computer = SystemInformation.ComputerName;
            int port = this.GetPort();

            if (port != 0)
            {
                var connect = ConnectBL.Get_Connect(port, computer);
                if (!connect.Com.Contains(isCOM))
                {
                    if (connect != null)
                    {
                        string setting = "IP : " + connect.Com + " : " + connect.BaudRate;

                        if (rdoPort1.Checked)
                        {
                            txtParametersCom1.Text = setting;
                        }
                        else if (rdoPort2.Checked)
                        {
                            txtParametersCom2.Text = setting;
                        }
                        else if (rdoPort3.Checked)
                        {
                            txtParametersCom3.Text = setting;
                        }
                        else if (rdoPort4.Checked)
                        {
                            txtParametersCom4.Text = setting;
                        }
                        else if (rdoPort5.Checked)
                        {
                            txtParametersCom5.Text = setting;
                        }
                        else if (rdoPort6.Checked)
                        {
                            txtParametersCom6.Text = setting;
                        }
                    }
                }
            }
        }

        private void btnReset1_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Bạn muốn xóa dữ liệu tạm ? ", "Thông báo !", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (DialogResult.Yes == result)
            {
                this.Reset(1);
            }
            else
                return;
        }

        private void btnReset2_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Bạn muốn xóa dữ liệu tạm ? ", "Thông báo !", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (DialogResult.Yes == result)
            {
                this.Reset(2);
            }
            else
                return;
        }

        private void btnReset3_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Bạn muốn xóa dữ liệu tạm ? ", "Thông báo !", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (DialogResult.Yes == result)
            {
                this.Reset(3);
            }
            else
                return;
        }

        private void btnReset4_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Bạn muốn xóa dữ liệu tạm ? ", "Thông báo !", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (DialogResult.Yes == result)
            {
                this.Reset(4);
            }
            else
                return;
        }

        private void btnReset5_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Bạn muốn xóa dữ liệu tạm ? ", "Thông báo !", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (DialogResult.Yes == result)
            {
                this.Reset(5);
            }
            else
                return;
        }

        private void btnReset6_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Bạn muốn xóa dữ liệu tạm ? ", "Thông báo !", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (DialogResult.Yes == result)
            {
                this.Reset(6);
            }
            else
                return;
        }

        private void chkQC1_CheckedChanged(object sender, EventArgs e)
        {
            var processQC = this.chkQC1.Checked;
            var deviceID = long.Parse(this.txtDeviceID1.Text);
            this.ProcessQC(deviceID, processQC);   
        }

        private void chkQC2_CheckedChanged(object sender, EventArgs e)
        {
            var processQC = this.chkQC2.Checked;
            var deviceID = long.Parse(this.txtDeviceID2.Text);
            this.ProcessQC(deviceID, processQC);
        }

        private void chkQC3_CheckedChanged(object sender, EventArgs e)
        {
            var processQC = this.chkQC3.Checked;
            var deviceID = long.Parse(this.txtDeviceID3.Text);
            this.ProcessQC(deviceID, processQC);
        }

        private void chkQC4_CheckedChanged(object sender, EventArgs e)
        {
            var processQC = this.chkQC4.Checked;
            var deviceID = long.Parse(this.txtDeviceID4.Text);
            this.ProcessQC(deviceID, processQC);
        }

        private void chkQC5_CheckedChanged(object sender, EventArgs e)
        {
            var processQC = this.chkQC5.Checked;
            var deviceID = long.Parse(this.txtDeviceID5.Text);
            this.ProcessQC(deviceID, processQC);
        }

        private void chkQC6_CheckedChanged(object sender, EventArgs e)
        {
            var processQC = this.chkQC6.Checked;
            var deviceID = long.Parse(this.txtDeviceID6.Text);
            this.ProcessQC(deviceID, processQC);
        }

        private void btnDeleteConnect_Click(object sender, EventArgs e)
        {
            this.computer = SystemInformation.ComputerName;
            int port = this.GetPort();

            DialogResult result = MessageBox.Show("Bạn muốn xóa kết nối này ?", "Thông báo !", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (DialogResult.Yes == result)
            {
                if (port == 0)
                {
                    MessageBox.Show("Vui lòng chọn kết nối!", "Thông báo", MessageBoxButtons.OK);
                }
                else
                {
                    var connect = ConnectBL.Get_Connect(port, computer);
                    if (connect.Status == true)
                    {
                        MessageBox.Show("Kết nối đang mở nên không thể xóa. Vui lòng đóng kết nối!", "Thông báo", MessageBoxButtons.OK);
                    }
                    else
                    {
                        if (ConnectBL.DeleteConect(port, this.computer))
                        {
                            this.DeleteContentControls(port);
                        }
                        else
                        {
                            MessageBox.Show("Không thể xóa kết nối này. Vui lòng kiểm tra lại!", "Thông báo", MessageBoxButtons.OK);
                        }
                    }
                }
            }
        }

        private void chkWriteResult1_CheckedChanged(object sender, EventArgs e)
        {
            Update_WriteFile(this.txtProtocol1.Text, this.chkWriteResult1.Checked);
        }

        private void chkWriteResult2_CheckedChanged(object sender, EventArgs e)
        {
            Update_WriteFile(this.txtProtocol2.Text, this.chkWriteResult2.Checked);
        }

        private void chkWriteResult3_CheckedChanged(object sender, EventArgs e)
        {
            Update_WriteFile(this.txtProtocol3.Text, this.chkWriteResult3.Checked);
        }

        private void chkWriteResult4_CheckedChanged(object sender, EventArgs e)
        {
            Update_WriteFile(this.txtProtocol4.Text, this.chkWriteResult4.Checked);
        }

        private void chkWriteResult5_CheckedChanged(object sender, EventArgs e)
        {
            Update_WriteFile(this.txtProtocol5.Text, this.chkWriteResult5.Checked);
        }

        private void chkWriteResult6_CheckedChanged(object sender, EventArgs e)
        {
            Update_WriteFile(this.txtProtocol6.Text, this.chkWriteResult6.Checked);
        }

        #endregion


        #region ****************************************************************************************** METHOD

        private void CheckConnectString()
        {
            try
            {
                if (ConfigConnectString.SetConnectString())
                {
                    this.isConnectOk = true;
                    this.InitializeEvent();
                    this.InitializeTimer();
                }
                else
                {
                    this.OpenFormConnectString();
                }
            }
            catch
            {
                this.OpenFormConnectString();
            }
        }

        private void OpenFormConnectString()
        {
            frmConnectString frm = new frmConnectString();
            frm.FormClosing += FrmConnectString_FormClosing;
            frm.Show();
            this.Enabled = false;
        }

        private void HideConfigFunction()
        {
            var sesion = SessionBL.Get_SessionByComputerIDForConnect(SystemInformation.ComputerName);
            if (sesion != null)
            {
                var user = UserBL.Get_UserByUserID(sesion.UserId);
                if (user != null)
                {
                    if(user.Code == "Admin")
                    {
                        this.btnConfigCom.Enabled = true;
                        this.btnSelectDevice.Enabled = true;
                        this.btnConfigTCP.Enabled = true;
                        this.btnDeleteConnect.Enabled = true;
                        this.HideQC(true);
                    }
                    else
                    {
                        this.btnConfigCom.Enabled = false;
                        this.btnSelectDevice.Enabled = false;
                        this.btnConfigTCP.Enabled = false;
                        this.btnDeleteConnect.Enabled = false;
                        this.HideQC(false);
                    }
                }
            }
        }

        private void HideQC(bool hide)
        {
            this.chkQC1.Enabled = hide;
            this.chkQC2.Enabled = hide;
            this.chkQC3.Enabled = hide;
            this.chkQC4.Enabled = hide;
            this.chkQC5.Enabled = hide;
            this.chkQC6.Enabled = hide;
        }

        private void InitializeEvent()
        {
            this.SerialPort_01.DataReceived += new SerialDataReceivedEventHandler(SerialPort_01_DataReceived);
            this.SerialPort_02.DataReceived += new SerialDataReceivedEventHandler(SerialPort_02_DataReceived);
            this.SerialPort_03.DataReceived += new SerialDataReceivedEventHandler(SerialPort_03_DataReceived);
            this.SerialPort_04.DataReceived += new SerialDataReceivedEventHandler(SerialPort_04_DataReceived);
            this.SerialPort_05.DataReceived += new SerialDataReceivedEventHandler(SerialPort_05_DataReceived);
            this.SerialPort_06.DataReceived += new SerialDataReceivedEventHandler(SerialPort_06_DataReceived);

            this.rdoPort1.CheckedChanged += rdoPort_CheckedChanged;
            this.rdoPort2.CheckedChanged += rdoPort_CheckedChanged;
            this.rdoPort3.CheckedChanged += rdoPort_CheckedChanged;
            this.rdoPort4.CheckedChanged += rdoPort_CheckedChanged;
            this.rdoPort5.CheckedChanged += rdoPort_CheckedChanged;
            this.rdoPort6.CheckedChanged += rdoPort_CheckedChanged;

            this.FormClosing += frmConnect_FormClosing;

        }

        private void InitializeTimer()
        {
            this.timer1 = new Timer();
            this.timer1.Tick += timer1_Tick;
            this.timer1.Interval = 3000;

            this.timer2 = new Timer();
            this.timer2.Tick += timer2_Tick;
            this.timer2.Interval = 3000;

            this.timer3 = new Timer();
            this.timer3.Tick += timer3_Tick;
            this.timer3.Interval = 3000;

            this.timer4 = new Timer();
            this.timer4.Tick += timer4_Tick;
            this.timer4.Interval = 3000;

            this.timer5 = new Timer();
            this.timer5.Tick += timer5_Tick;
            this.timer5.Interval = 3000;

            this.timer6 = new Timer();
            this.timer6.Tick += timer6_Tick;
            this.timer6.Interval = 3000;

        }

        private void DeleteAllComNotExistOnComputer()
        {
            string[] lstCom = SerialPort.GetPortNames();
            var lstConnect = ConnectBL.Get_ListConnectAll(SystemInformation.ComputerName);

            if (lstConnect != null)
            {
                foreach (var connect in lstConnect)
                {
                    bool exits = false;
                    if (lstCom != null)
                    {
                        foreach (var com in lstCom)
                        {
                            // Không xóa khi : connect.Com == com hoặc connect.Com không chứa "COM" nghĩa là IPAddress 127.0.0.1
                            if ((connect.Com == com || !connect.Com.Contains(isCOM)) && connect.ComputerId == computer)
                            {
                                exits = true;
                                break;
                            }
                        }
                    }

                    if (!exits)
                    {
                        ConnectBL.Delete(connect.ComputerId, connect.PortId);
                    }
                }
            }
        }

        private void LoadComInfoAndSelectedDevice()
        {
            var lstConnect = ConnectBL.Get_ListConnectAll(SystemInformation.ComputerName);
            if (lstConnect != null)
            {
                for (int i = 0; i < lstConnect.Count; i++)
                {
                    if (lstConnect[i] != null)
                    {
                        string setting = string.Empty;
                        if (lstConnect[i].Com.Contains(isCOM))
                            setting = lstConnect[i].Com + " Setting : " + lstConnect[i].BaudRate + ", " + lstConnect[i].Parity + ", " + lstConnect[i].DataBit + ", " + lstConnect[i].StopBit;
                        else
                            setting = "IP : " + lstConnect[i].Com + " : " + lstConnect[i].BaudRate;
                        long deviceID = lstConnect[i].DeviceId??0;
                        var device = DeviceBL.Get_Device(deviceID);
                        switch (i)
                        {
                            case 0:
                                {
                                    txtParametersCom1.Text = setting;
                                    txtStatus1.Text = lstConnect[i].Status ? "Waiting ..." : string.Empty;
                                    pStatus1.BackColor = lstConnect[i].Status ? Color.Green : Color.Red;
                                    if (device != null)
                                    {
                                        txtDeviceName1.Text = device.Name;
                                        txtDeviceID1.Text = device.Id.ToString();
                                        chkQC1.Checked = device.ProcessQc;
                                        txtProtocol1.Text = device.Protocol;
                                    }
                                    break;
                                }
                            case 1:
                                {
                                    txtParametersCom2.Text = setting;
                                    txtStatus2.Text = lstConnect[i].Status ? "Waiting ..." : string.Empty;
                                    pStatus2.BackColor = lstConnect[i].Status ? Color.Green : Color.Red;
                                    if (device != null)
                                    {
                                        txtDeviceName2.Text = device.Name;
                                        txtDeviceID2.Text = device.Id.ToString();
                                        chkQC2.Checked = device.ProcessQc;
                                        txtProtocol2.Text = device.Protocol;
                                    }
                                    break;
                                }
                            case 2:
                                {
                                    txtParametersCom3.Text = setting;
                                    txtStatus3.Text = lstConnect[i].Status ? "Waiting ..." : string.Empty;
                                    pStatus3.BackColor = lstConnect[i].Status ? Color.Green : Color.Red;
                                    if (device != null)
                                    {
                                        txtDeviceName3.Text = device.Name;
                                        txtDeviceID3.Text = device.Id.ToString();
                                        chkQC3.Checked = device.ProcessQc;
                                        txtProtocol3.Text = device.Protocol;
                                    }
                                    break;
                                }
                            case 3:
                                {
                                    txtParametersCom4.Text = setting;
                                    txtStatus4.Text = lstConnect[i].Status ? "Waiting ..." : string.Empty;
                                    pStatus4.BackColor = lstConnect[i].Status ? Color.Green : Color.Red;
                                    if (device != null)
                                    {
                                        txtDeviceName4.Text = device.Name;
                                        txtDeviceID4.Text = device.Id.ToString();
                                        chkQC4.Checked = device.ProcessQc;
                                        txtProtocol4.Text = device.Protocol;
                                    }
                                    break;
                                }
                            case 4:
                                {
                                    txtParametersCom5.Text = setting;
                                    txtStatus5.Text = lstConnect[i].Status ? "Waiting ..." : string.Empty;
                                    pStatus5.BackColor = lstConnect[i].Status ? Color.Green : Color.Red;
                                    if (device != null)
                                    {
                                        txtDeviceName5.Text = device.Name;
                                        txtDeviceID5.Text = device.Id.ToString();
                                        chkQC5.Checked = device.ProcessQc;
                                        txtProtocol5.Text = device.Protocol;
                                    }
                                    break;
                                }
                            case 5:
                                {
                                    txtParametersCom6.Text = setting;
                                    txtStatus6.Text = lstConnect[i].Status ? "Waiting ..." : string.Empty;
                                    pStatus6.BackColor = lstConnect[i].Status ? Color.Green : Color.Red;
                                    if (device != null)
                                    {
                                        txtDeviceName6.Text = device.Name;
                                        txtDeviceID6.Text = device.Id.ToString();
                                        chkQC6.Checked = device.ProcessQc;
                                        txtProtocol6.Text = device.Protocol;
                                    }
                                    break;
                                }
                            default: break;                                                                                                                                                                                     
                        }
                    }
                }
            }
        }

        private void LoadLogin()
        {
            this.Enabled = false;
            frmLogin frmLogin = new frmLogin(true);
            frmLogin.FormClosing += FrmLogin_FormClosing;
            frmLogin.Show();

        }

        private void Set_User()
        {
            this.lblComputerName.Text = "Máy - " + SystemInformation.ComputerName;
            this.lbVersion.Text = "Phiên bản phần mềm - " + Application.ProductVersion;
            var sesion = SessionBL.Get_SessionByComputerIDForConnect(SystemInformation.ComputerName);
            var hostPital = HostpitalBL.Get_Hostpital();
            if (sesion != null)
            {
                var user = UserBL.Get_UserByUserID(sesion.UserId);
                if (user != null)
                {
                    this.lbUser.Text = string.Concat("User - ", user.Code, " ( ", user.Name, " )");
                }
            }
            if (hostPital != null)
            {
                this.txtHospital.Text = hostPital.Name.ToUpper();
            }
        }

        private void Connect(SerialPort serialPort, Connect connect)
        {
            serialPort.PortName = connect.Com;
            serialPort.BaudRate = Convert.ToInt32(connect.BaudRate);
            serialPort.DataBits = Convert.ToInt32(connect.DataBit);
            serialPort.RtsEnable = connect.Rts;

            switch (connect.Parity)
            {
                case "Odd":
                    serialPort.Parity = Parity.Odd;
                    break;
                case "None":
                    serialPort.Parity = Parity.None;
                    break;
                case "Even":
                    serialPort.Parity = Parity.Even;
                    break;
            }

            switch (connect.StopBit.ToString())
            {
                case "1":
                    serialPort.StopBits = StopBits.One;
                    break;
                case "1.5":
                    serialPort.StopBits = StopBits.OnePointFive;
                    break;
                case "2":
                    serialPort.StopBits = StopBits.Two;
                    break;
            }

            try
            {
                serialPort.Open();

                btnConnect.Enabled = false;
                btnDisconnect.Enabled = true;

                if (rdoPort1.Checked)
                {
                    pStatus1.BackColor = Color.Green;
                    txtStatus1.Text = "Waiting ...";
                    this.timer1.Start();
                    this.btnReset1.Tag = connect.DeviceId;
                }
                if (rdoPort2.Checked)
                {
                    pStatus2.BackColor = Color.Green;
                    txtStatus2.Text = "Waiting ...";
                    this.timer2.Start();
                    this.btnReset2.Tag = connect.DeviceId;
                }
                if (rdoPort3.Checked)
                {
                    pStatus3.BackColor = Color.Green;
                    txtStatus3.Text = "Waiting ...";
                    this.timer3.Start();
                    this.btnReset3.Tag = connect.DeviceId;
                }
                if (rdoPort4.Checked)
                {
                    pStatus4.BackColor = Color.Green;
                    txtStatus4.Text = "Waiting ...";
                    this.timer4.Start();
                    this.btnReset4.Tag = connect.DeviceId;
                }
                if (rdoPort5.Checked)
                {
                    pStatus5.BackColor = Color.Green;
                    txtStatus5.Text = "Waiting ...";
                    this.timer5.Start();
                    this.btnReset5.Tag = connect.DeviceId;
                }
                if (rdoPort6.Checked)
                {
                    pStatus6.BackColor = Color.Green;
                    txtStatus6.Text = "Waiting ...";
                    this.timer6.Start();
                    this.btnReset6.Tag = connect.DeviceId;
                }

                ConnectBL.Update_Status(connect.ComputerId, connect.PortId, true);
                InitTimerInConnectCOM(connect, serialPort);
                Reset(connect);
            }
            catch(Exception ex)
            {
                log4net.Error(ex);
                MessageBox.Show("Không thể kết nối. Vui lòng thử lại !", "Thông báo", MessageBoxButtons.OK);
            }
        }

        private void DisConnect(SerialPort serialport, Connect connect)
        {
            try
            {
                serialport.Close();

                btnConnect.Enabled = true;
                btnDisconnect.Enabled = false;

                if (rdoPort1.Checked)
                {
                    this.timer1.Stop();
                    pStatus1.BackColor = Color.Red;
                    txtStatus1.Text = string.Empty;
                    this.btnReset1.Tag = string.Empty;
                }
                if (rdoPort2.Checked)
                {
                    this.timer2.Stop();
                    pStatus2.BackColor = Color.Red;
                    txtStatus2.Text = string.Empty;
                    this.btnReset2.Tag = string.Empty;
                }
                if (rdoPort3.Checked)
                {
                    this.timer3.Stop();
                    pStatus3.BackColor = Color.Red;
                    txtStatus3.Text = string.Empty;
                    this.btnReset3.Tag = string.Empty;
                }
                if (rdoPort4.Checked)
                {
                    this.timer4.Stop();
                    pStatus4.BackColor = Color.Red;
                    txtStatus4.Text = string.Empty;
                    this.btnReset4.Tag = string.Empty;
                }
                if (rdoPort5.Checked)
                {
                    this.timer5.Stop();
                    pStatus5.BackColor = Color.Red;
                    txtStatus5.Text = string.Empty;
                    this.btnReset5.Tag = string.Empty;
                }
                if (rdoPort6.Checked)
                {
                    this.timer6.Stop();
                    pStatus6.BackColor = Color.Red;
                    txtStatus6.Text = string.Empty;
                    this.btnReset6.Tag = string.Empty;
                }

                ConnectBL.Update_Status(connect.ComputerId, connect.PortId, false);
                RemoveTimerInConnectCOM(connect);
            }
            catch(Exception ex)
            {
                log4net.Error(ex);
                MessageBox.Show("Không thể đóng kết nối. Vui lòng thử lại !", "Thông báo", MessageBoxButtons.OK);
            }
        }

        private void ConnectAll(SerialPort serialPort, Connect connect)
        {
            serialPort.PortName = connect.Com;
            serialPort.BaudRate = Convert.ToInt32(connect.BaudRate);
            serialPort.DataBits = Convert.ToInt32(connect.DataBit);
            this.computer = SystemInformation.ComputerName;

            switch (connect.Parity)
            {
                case "Odd":
                    serialPort.Parity = Parity.Odd;
                    break;
                case "None":
                    serialPort.Parity = Parity.None;
                    break;
                case "Even":
                    serialPort.Parity = Parity.Even;
                    break;
            }

            switch (connect.StopBit.ToString())
            {
                case "1":
                    serialPort.StopBits = StopBits.One;
                    break;
                case "1.5":
                    serialPort.StopBits = StopBits.OnePointFive;
                    break;
                case "2":
                    serialPort.StopBits = StopBits.Two;
                    break;
            }

            try
            {
                serialPort.Open();

                btnConnect.Enabled = false;
                btnDisconnect.Enabled = true;

                if (connect.PortId == int.Parse(rdoPort1.Tag.ToString()) && connect.ComputerId == computer)
                {
                    pStatus1.BackColor = Color.Green;
                    txtStatus1.Text = "Waiting ...";
                    this.timer1.Start();
                    this.btnReset1.Tag = connect.DeviceId;
                }
                if (connect.PortId == int.Parse(rdoPort2.Tag.ToString()) && connect.ComputerId == computer)
                {
                    pStatus2.BackColor = Color.Green;
                    txtStatus2.Text = "Waiting ...";
                    this.timer2.Start();
                    this.btnReset2.Tag = connect.DeviceId;
                }
                if (connect.PortId == int.Parse(rdoPort3.Tag.ToString()) && connect.ComputerId == computer)
                {
                    pStatus3.BackColor = Color.Green;
                    txtStatus3.Text = "Waiting ...";
                    this.timer3.Start();
                    this.btnReset3.Tag = connect.DeviceId;
                }
                if (connect.PortId == int.Parse(rdoPort4.Tag.ToString()) && connect.ComputerId == computer)
                {
                    pStatus4.BackColor = Color.Green;
                    txtStatus4.Text = "Waiting ...";
                    this.timer4.Start();
                    this.btnReset4.Tag = connect.DeviceId;
                }
                if (connect.PortId == int.Parse(rdoPort5.Tag.ToString()) && connect.ComputerId == computer)
                {
                    pStatus5.BackColor = Color.Green;
                    txtStatus5.Text = "Waiting ...";
                    this.timer5.Start();
                    this.btnReset5.Tag = connect.DeviceId;
                }
                if (connect.PortId == int.Parse(rdoPort6.Tag.ToString()) && connect.ComputerId == computer)
                {
                    pStatus6.BackColor = Color.Green;
                    txtStatus6.Text = "Waiting ...";
                    this.timer6.Start();
                    this.btnReset6.Tag = connect.DeviceId;
                }

                ConnectBL.Update_Status(connect.ComputerId, connect.PortId, true);
                InitTimerInConnectCOM(connect, serialPort);
                Reset(connect);
            }
            catch(Exception ex)
            {
                log4net.Error(ex);
            }
        }

        private void DisConnectAll(SerialPort serialPort, Connect connect)
        {
            try
            {
                serialPort.Close();

                if (connect.PortId == int.Parse(rdoPort1.Tag.ToString()) && connect.ComputerId == computer)
                {
                    this.timer1.Stop();
                    pStatus1.BackColor = Color.Red;
                    txtStatus1.Text = string.Empty;
                    this.btnReset1.Tag = string.Empty;
                }
                if (connect.PortId == int.Parse(rdoPort2.Tag.ToString()) && connect.ComputerId == computer)
                {
                    this.timer2.Stop();
                    pStatus2.BackColor = Color.Red;
                    txtStatus2.Text = string.Empty;
                    this.btnReset2.Tag = string.Empty;
                }
                if (connect.PortId == int.Parse(rdoPort3.Tag.ToString()) && connect.ComputerId == computer)
                {
                    this.timer3.Stop();
                    pStatus3.BackColor = Color.Red;
                    txtStatus3.Text = string.Empty;
                    this.btnReset3.Tag = string.Empty;
                }
                if (connect.PortId == int.Parse(rdoPort4.Tag.ToString()) && connect.ComputerId == computer)
                {
                    this.timer4.Stop();
                    pStatus4.BackColor = Color.Red;
                    txtStatus4.Text = string.Empty;
                    this.btnReset4.Tag = string.Empty;
                }
                if (connect.PortId == int.Parse(rdoPort5.Tag.ToString()) && connect.ComputerId == computer)
                {
                    this.timer5.Stop();
                    pStatus5.BackColor = Color.Red;
                    txtStatus5.Text = string.Empty;
                    this.btnReset5.Tag = string.Empty;
                }
                if (connect.PortId == int.Parse(rdoPort6.Tag.ToString()) && connect.ComputerId == computer)
                {
                    this.timer6.Stop();
                    pStatus6.BackColor = Color.Red;
                    txtStatus6.Text = string.Empty;
                    this.btnReset6.Tag = string.Empty;
                }

                ConnectBL.Update_Status(connect.ComputerId, connect.PortId, false);
                RemoveTimerInConnectCOM(connect);
            }
            catch(Exception ex)
            {
                log4net.Error(ex);
            }
        }

        private int GetPort()
        {
            if (rdoPort1.Checked)
                return int.Parse(rdoPort1.Tag.ToString());
            if (rdoPort2.Checked)
                return int.Parse(rdoPort2.Tag.ToString());
            if (rdoPort3.Checked)
                return int.Parse(rdoPort3.Tag.ToString());
            if (rdoPort4.Checked)
                return int.Parse(rdoPort4.Tag.ToString());
            if (rdoPort5.Checked)
                return int.Parse(rdoPort5.Tag.ToString());
            if (rdoPort6.Checked)
                return int.Parse(rdoPort6.Tag.ToString());
            return 0;
        }

        public void Reset(int portID)
        {
            string computer = SystemInformation.ComputerName;
            var connect = ConnectBL.Get_Connect(portID, computer);
            var device = DeviceBL.Get_Device(connect.DeviceId??0);

            Type type = Type.GetType(this.nameSpace + "." + device.Protocol);
            MethodInfo method = type.GetMethod(this.clear); 
            var obj = Activator.CreateInstance(type); 
            method.Invoke(obj, new object[] {}); 
        }

        public void Reset(Connect connect)
        {
            Type type = Type.GetType(this.nameSpace + "." + connect.Device.Protocol);
            MethodInfo method = type.GetMethod(this.clear);
            var obj = Activator.CreateInstance(type);
            method.Invoke(obj, new object[] { });
        }

        public void Update_Status_Color()
        {
            var lstConnect = ConnectBL.Get_ListConnectAll(SystemInformation.ComputerName);
            if (lstConnect == null) return;

            foreach (var connect in lstConnect)
            {
                if (connect != null)
                {
                    if (connect.PortId == int.Parse(rdoPort1.Tag.ToString()))
                    {
                        pStatus1.BackColor = Color.Red;
                        ConnectBL.Update_Status(connect.ComputerId, connect.PortId, false);
                    }
                    if (connect.PortId == int.Parse(rdoPort2.Tag.ToString()))
                    {
                        pStatus2.BackColor = Color.Red;
                        ConnectBL.Update_Status(connect.ComputerId, connect.PortId, false);
                    }
                    if (connect.PortId == int.Parse(rdoPort3.Tag.ToString()))
                    {
                        pStatus3.BackColor = Color.Red;
                        ConnectBL.Update_Status(connect.ComputerId, connect.PortId, false);
                    }
                    if (connect.PortId == int.Parse(rdoPort4.Tag.ToString()))
                    {
                        pStatus4.BackColor = Color.Red;
                        ConnectBL.Update_Status(connect.ComputerId, connect.PortId, false);
                    }
                    if (connect.PortId == int.Parse(rdoPort5.Tag.ToString()))
                    {
                        pStatus5.BackColor = Color.Red;
                        ConnectBL.Update_Status(connect.ComputerId, connect.PortId, false);
                    }
                    if (connect.PortId == int.Parse(rdoPort6.Tag.ToString()))
                    {
                        pStatus6.BackColor = Color.Red;
                        ConnectBL.Update_Status(connect.ComputerId, connect.PortId, false);
                    }

                    this.btnConnect.Enabled = true;
                    this.btnDisconnect.Enabled = false;
                }
            }
        }

        private void Open_Server_Or_Client(Connect connect)
        {
            try
            {
                IPAddress ipServer = IPAddress.Parse(connect.Com);
                int portServer = int.Parse(connect.BaudRate.ToString());
                IPEndPoint iPEndPointServer = new IPEndPoint(ipServer, portServer);
                bool isStart = false;

                Type type = Type.GetType(this.nameSpace + "." + connect.Device.Protocol);
                MethodInfo method = type.GetMethod(this.methodIP_Start);
                var obj = Activator.CreateInstance(type);
                isStart = bool.Parse(method.Invoke(obj, new object[] { iPEndPointServer, connect.DeviceId }).ToString());

                method = type.GetMethod(this.method_CheckConnect);
                obj = Activator.CreateInstance(type);
                var checkConnect = bool.Parse(method.Invoke(obj, new object[] { }).ToString());

                if (isStart)
                {
                    ConnectBL.Update_Status(connect.ComputerId, connect.PortId, true);
                    Reset(connect);

                    btnConnect.Enabled = false;
                    btnDisconnect.Enabled = true;

                    if (connect.PortId == int.Parse(rdoPort1.Tag.ToString()) && connect.ComputerId == computer)
                    {
                        pStatus1.BackColor = checkConnect ? Color.Yellow : Color.Green;
                        txtStatus1.Text = "Waiting ...";
                        this.btnReset1.Tag = connect.DeviceId;
                    }
                    if (connect.PortId == int.Parse(rdoPort2.Tag.ToString()) && connect.ComputerId == computer)
                    {
                        pStatus2.BackColor = checkConnect ? Color.Yellow : Color.Green;
                        txtStatus2.Text = "Waiting ...";
                        this.btnReset2.Tag = connect.DeviceId;
                    }
                    if (connect.PortId == int.Parse(rdoPort3.Tag.ToString()) && connect.ComputerId == computer)
                    {
                        pStatus3.BackColor = checkConnect ? Color.Yellow : Color.Green;
                        txtStatus3.Text = "Waiting ...";
                        this.btnReset3.Tag = connect.DeviceId;
                    }
                    if (connect.PortId == int.Parse(rdoPort4.Tag.ToString()) && connect.ComputerId == computer)
                    {
                        pStatus4.BackColor = checkConnect ? Color.Yellow : Color.Green;
                        txtStatus4.Text = "Waiting ...";
                        this.btnReset4.Tag = connect.DeviceId;
                    }
                    if (connect.PortId == int.Parse(rdoPort5.Tag.ToString()) && connect.ComputerId == computer)
                    {
                        pStatus5.BackColor = checkConnect ? Color.Yellow : Color.Green;
                        txtStatus5.Text = "Waiting ...";
                        this.btnReset5.Tag = connect.DeviceId;
                    }
                    if (connect.PortId == int.Parse(rdoPort6.Tag.ToString()) && connect.ComputerId == computer)
                    {
                        pStatus6.BackColor = checkConnect ? Color.Yellow : Color.Green;
                        txtStatus6.Text = "Waiting ...";
                        this.btnReset6.Tag = connect.DeviceId;
                    }
                }
            }
            catch(Exception ex)
            {
                log4net.Error(ex);
            }          
        }

        private void Close_Server_Or_Client(Connect connect)
        {
            try
            {
                bool isStop = false;
                Type type = Type.GetType(this.nameSpace + "." + connect.Device.Protocol);
                MethodInfo method = type.GetMethod(this.methodIP_Close);
                var obj = Activator.CreateInstance(type);
                isStop = bool.Parse(method.Invoke(obj, new object[] { }).ToString());

                if (isStop)
                {
                    ConnectBL.Update_Status(connect.ComputerId, connect.PortId, false);

                    btnConnect.Enabled = true;
                    btnDisconnect.Enabled = false;

                    if (connect.PortId == int.Parse(rdoPort1.Tag.ToString()) && connect.ComputerId == computer)
                    {
                        pStatus1.BackColor = Color.Red;
                        txtStatus1.Text = string.Empty;
                        this.btnReset1.Tag = string.Empty;
                    }
                    if (connect.PortId == int.Parse(rdoPort2.Tag.ToString()) && connect.ComputerId == computer)
                    {
                        pStatus2.BackColor = Color.Red;
                        txtStatus2.Text = string.Empty;
                        this.btnReset2.Tag = string.Empty;
                    }
                    if (connect.PortId == int.Parse(rdoPort3.Tag.ToString()) && connect.ComputerId == computer)
                    {
                        pStatus3.BackColor = Color.Red;
                        txtStatus3.Text = string.Empty;
                        this.btnReset3.Tag = string.Empty;
                    }
                    if (connect.PortId == int.Parse(rdoPort4.Tag.ToString()) && connect.ComputerId == computer)
                    {
                        pStatus4.BackColor = Color.Red;
                        txtStatus4.Text = string.Empty;
                        this.btnReset4.Tag = string.Empty;
                    }
                    if (connect.PortId == int.Parse(rdoPort5.Tag.ToString()) && connect.ComputerId == computer)
                    {
                        pStatus5.BackColor = Color.Red;
                        txtStatus5.Text = string.Empty;
                        this.btnReset5.Tag = string.Empty;
                    }
                    if (connect.PortId == int.Parse(rdoPort6.Tag.ToString()) && connect.ComputerId == computer)
                    {
                        pStatus6.BackColor = Color.Red;
                        txtStatus6.Text = string.Empty;
                        this.btnReset6.Tag = string.Empty;
                    }
                }
            }
            catch(Exception ex)
            {
                log4net.Error(ex);
            }
        }

        private void CallMethodProcess_1(SerialPort sp, int portID)
        {
            string computer = SystemInformation.ComputerName;
            var connect = ConnectBL.Get_Connect(portID, computer);
            var device = DeviceBL.Get_Device(connect.DeviceId ?? 0);
            Type type = Type.GetType(this.nameSpace + "." + device.Protocol);
            MethodInfo method = type.GetMethod(this.method);
            var obj = Activator.CreateInstance(type);
            method.Invoke(obj, new object[] { sp, connect, this.txtSID.Text });
        }

        private void CallMethodProcess_2(SerialPort sp, int portID)
        {
            string computer = SystemInformation.ComputerName;
            var connect = ConnectBL.Get_Connect(portID, computer);
            var device = DeviceBL.Get_Device(connect.DeviceId??0);
            Type type = Type.GetType(this.nameSpace + "." + device.Protocol);
            MethodInfo method = type.GetMethod(this.method);
            var obj = Activator.CreateInstance(type);
            method.Invoke(obj, new object[] { sp, connect, this.txtSID.Text });
        }

        private void CallMethodProcess_3(SerialPort sp, int portID)
        {
            string computer = SystemInformation.ComputerName;
            var connect = ConnectBL.Get_Connect(portID, computer);
            var device = DeviceBL.Get_Device(connect.DeviceId??0);
            Type type = Type.GetType(this.nameSpace + "." + device.Protocol);
            MethodInfo method = type.GetMethod(this.method);
            var obj = Activator.CreateInstance(type);
            method.Invoke(obj, new object[] { sp, connect, this.txtSID.Text });
        }

        private void CallMethodProcess_4(SerialPort sp, int portID)
        {
            string computer = SystemInformation.ComputerName;
            var connect = ConnectBL.Get_Connect(portID, computer);
            var device = DeviceBL.Get_Device(connect.DeviceId??0);
            Type type = Type.GetType(this.nameSpace + "." + device.Protocol);
            MethodInfo method = type.GetMethod(this.method);
            var obj = Activator.CreateInstance(type);
            method.Invoke(obj, new object[] { sp, connect, this.txtSID.Text });
        }

        private void CallMethodProcess_5(SerialPort sp, int portID)
        {
            string computer = SystemInformation.ComputerName;
            var connect = ConnectBL.Get_Connect(portID, computer);
            var device = DeviceBL.Get_Device(connect.DeviceId??0);
            Type type = Type.GetType(this.nameSpace + "." + device.Protocol);
            MethodInfo method = type.GetMethod(this.method);
            var obj = Activator.CreateInstance(type);
            method.Invoke(obj, new object[] { sp, connect, this.txtSID.Text });
        }

        private void CallMethodProcess_6(SerialPort sp, int portID)
        {
            string computer = SystemInformation.ComputerName;
            var connect = ConnectBL.Get_Connect(portID, computer);
            var device = DeviceBL.Get_Device(connect.DeviceId??0);
            Type type = Type.GetType(this.nameSpace + "." + device.Protocol);
            MethodInfo method = type.GetMethod(this.method);
            var obj = Activator.CreateInstance(type);
            method.Invoke(obj, new object[] { sp, connect, this.txtSID.Text });
        }

        private void ProcessQC(long deviceID, bool processQC)
        {
            try
            {
                DeviceBL.UpdateProcessQC(deviceID, processQC);
            }
            catch (Exception ex)
            {
                log4net.Error(ex);
                MessageBox.Show("Không thể xử lý QC cho máy này", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void DeleteContentControls(int port)
        {
            if (port == 1)
            {
                this.txtDeviceName1.Text = string.Empty;
                this.txtDeviceID1.Text = string.Empty;
                this.txtParametersCom1.Text = string.Empty;
                this.txtStatus1.Text = string.Empty;
                this.pStatus1.BackColor = this.BackColor;
                this.chkWriteResult1.Checked = false;
                this.chkQC1.Checked = false;
                this.txtProtocol1.Text = string.Empty;
            }
            else if (port == 2)
            {
                this.txtDeviceName2.Text = string.Empty;
                this.txtDeviceID2.Text = string.Empty;
                this.txtParametersCom2.Text = string.Empty;
                this.txtStatus2.Text = string.Empty;
                this.pStatus2.BackColor = this.BackColor;
                this.chkWriteResult2.Checked = false;
                this.chkQC2.Checked = false;
                this.txtProtocol2.Text = string.Empty;
            }
            else if (port == 3)
            {
                this.txtDeviceName3.Text = string.Empty;
                this.txtDeviceID3.Text = string.Empty;
                this.txtParametersCom3.Text = string.Empty;
                this.txtStatus3.Text = string.Empty;
                this.pStatus3.BackColor = this.BackColor;
                this.chkWriteResult3.Checked = false;
                this.chkQC3.Checked = false;
                this.txtProtocol3.Text = string.Empty;
            }
            else if (port == 4)
            {
                this.txtDeviceName4.Text = string.Empty;
                this.txtDeviceID4.Text = string.Empty;
                this.txtParametersCom4.Text = string.Empty;
                this.txtStatus4.Text = string.Empty;
                this.pStatus4.BackColor = this.BackColor;
                this.chkWriteResult4.Checked = false;
                this.chkQC4.Checked = false;
                this.txtProtocol4.Text = string.Empty;
            }
            else if (port == 5)
            {
                this.txtDeviceName5.Text = string.Empty;
                this.txtDeviceID5.Text = string.Empty;
                this.txtParametersCom5.Text = string.Empty;
                this.txtStatus5.Text = string.Empty;
                this.pStatus5.BackColor = this.BackColor;
                this.chkWriteResult5.Checked = false;
                this.chkQC5.Checked = false;
                this.txtProtocol5.Text = string.Empty;
            }
            else if (port == 6)
            {
                this.txtDeviceName6.Text = string.Empty;
                this.txtDeviceID6.Text = string.Empty;
                this.txtParametersCom6.Text = string.Empty;
                this.txtStatus6.Text = string.Empty;
                this.pStatus6.BackColor = this.BackColor;
                this.chkWriteResult6.Checked = false;
                this.chkQC6.Checked = false;
                this.txtProtocol6.Text = string.Empty;
            }
        }

        private void Update_WriteFile(string prtocol, bool writeFile)
        {
            Type type = Type.GetType(this.nameSpace + "." + prtocol);
            MethodInfo method = type.GetMethod(this.method_WriteFile);
            var obj = Activator.CreateInstance(type);
            method.Invoke(obj, new object[] { writeFile });
        }

        public void Update_Waiting_Receving(Connect connect)
        {
            if (connect != null)
            {
                switch (connect.PortId)
                {
                    case 1:
                        this.count1++;
                        break;
                    case 2:
                        this.count2++;
                        break;
                    case 3:
                        this.count3++;
                        break;
                    case 4:
                        this.count4++;
                        break;
                    case 5:
                        this.count5++;
                        break;
                    case 6:
                        this.count6++;
                        break;
                }
            }
        }

        public void UpdateConnectForServer(Connect connect, bool checkConnect)
        {
            if (connect.PortId == int.Parse(rdoPort1.Tag.ToString()) && connect.ComputerId == computer)
            {
                pStatus1.BackColor = checkConnect ? Color.Green : Color.Yellow;
                txtStatus1.Text = this.waiting;
                this.btnReset1.Tag = connect.DeviceId;
            }
            if (connect.PortId == int.Parse(rdoPort2.Tag.ToString()) && connect.ComputerId == computer)
            {
                pStatus2.BackColor = checkConnect ? Color.Green : Color.Yellow;
                txtStatus2.Text = this.waiting;
                this.btnReset2.Tag = connect.DeviceId;
            }
            if (connect.PortId == int.Parse(rdoPort3.Tag.ToString()) && connect.ComputerId == computer)
            {
                pStatus3.BackColor = checkConnect ? Color.Green : Color.Yellow;
                txtStatus3.Text = this.waiting;
                this.btnReset3.Tag = connect.DeviceId;
            }
            if (connect.PortId == int.Parse(rdoPort4.Tag.ToString()) && connect.ComputerId == computer)
            {
                pStatus4.BackColor = checkConnect ? Color.Green : Color.Yellow;
                txtStatus4.Text = this.waiting;
                this.btnReset4.Tag = connect.DeviceId;
            }
            if (connect.PortId == int.Parse(rdoPort5.Tag.ToString()) && connect.ComputerId == computer)
            {
                pStatus5.BackColor = checkConnect ? Color.Green : Color.Yellow;
                txtStatus5.Text = this.waiting;
                this.btnReset5.Tag = connect.DeviceId;
            }
            if (connect.PortId == int.Parse(rdoPort6.Tag.ToString()) && connect.ComputerId == computer)
            {
                pStatus6.BackColor = checkConnect ? Color.Green : Color.Yellow;
                txtStatus6.Text = this.waiting;
                this.btnReset6.Tag = connect.DeviceId;
            }
        }

        public void UpdateConnectForClient(Connect connect, bool checkConnect)
        {
            long? deviceId = 0;
            if (connect.Device != null)
            {
                deviceId = connect.DeviceId;
            }
            if (connect.PortId == int.Parse(rdoPort1.Tag.ToString()))
            {
                pStatus1.BackColor = checkConnect ? Color.Green : Color.Red;
                txtStatus1.Text = this.waiting;
                this.btnReset1.Tag = deviceId;
            }
            if (connect.PortId == int.Parse(rdoPort2.Tag.ToString()))
            {
                pStatus2.BackColor = checkConnect ? Color.Green : Color.Red;
                txtStatus2.Text = this.waiting;
                this.btnReset2.Tag = deviceId;
            }
            if (connect.PortId == int.Parse(rdoPort3.Tag.ToString()))
            {
                pStatus3.BackColor = checkConnect ? Color.Green : Color.Red;
                txtStatus3.Text = this.waiting;
                this.btnReset3.Tag = deviceId;
            }
            if (connect.PortId == int.Parse(rdoPort4.Tag.ToString()))
            {
                pStatus4.BackColor = checkConnect ? Color.Green : Color.Red;
                txtStatus4.Text = this.waiting;
                this.btnReset4.Tag = deviceId;
            }
            if (connect.PortId == int.Parse(rdoPort5.Tag.ToString()))
            {
                pStatus5.BackColor = checkConnect ? Color.Green : Color.Red;
                txtStatus5.Text = this.waiting;
                this.btnReset5.Tag = deviceId;
            }
            if (connect.PortId == int.Parse(rdoPort6.Tag.ToString()))
            {
                pStatus6.BackColor = checkConnect ? Color.Green : Color.Red;
                txtStatus6.Text = this.waiting;
                this.btnReset6.Tag = deviceId;
            }
        }

        #endregion

        #region Tạo Timer
        public void InitTimerInConnectCOM(Connect connect, SerialPort serialPort)
        {
            try
            {
                Type type = Type.GetType(this.nameSpace + "." + connect.Device.Protocol);
                MethodInfo method = type.GetMethod("InitTimer");
                var obj = Activator.CreateInstance(type);
                method.Invoke(obj, new object[] { serialPort });
            }
            catch { }
        }

        public void RemoveTimerInConnectCOM(Connect connect)
        {
            try
            {
                Type type = Type.GetType(this.nameSpace + "." + connect.Device.Protocol);
                MethodInfo method = type.GetMethod("RemoveTimer");
                var obj = Activator.CreateInstance(type);
                method.Invoke(obj, new object[] { });
            }
            catch { }
        }
        #endregion
    }
}
