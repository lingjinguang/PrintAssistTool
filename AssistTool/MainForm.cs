using System;
using System.IO;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.NetworkInformation;
using System.Diagnostics;
using PrintAssist.Model;
using PrintAssist.Utils;
using PrintAssist.Common;
using PrintAssistTool.Services;
using WebSocketSharp.Server;
using PrintAssistTool.Utils;

namespace PrintAssistTool
{
    public partial class MainForm : Form
    {
        private static readonly DynamicFilePath dfp = DynamicFilePath.GetInstance(false);
        HttpServer httpsv = null;
        private System.Windows.Forms.Timer timer;

        public MainForm()
        {
            #region 开机启动
            //将文件复制到系统启动文件夹路径下
            string StartupPath = System.Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            //获得文件的当前路径  
            string dir = Directory.GetCurrentDirectory();
            //获取可执行文件的全部路径  
            string exeDir = dir + @"\PrintAssistTool.lnk";
            if (!File.Exists(StartupPath + @"\PrintAssistTool.exe.lnk") && File.Exists(exeDir))
            {
                try
                {
                    File.Copy(exeDir, StartupPath + @"\PrintAssistTool.lnk", true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("添加启动项异常：" + ex.Message);
                }
            }
            #endregion

            InitializeComponent();

            KillWinNameProcess(this.Text);

            //显示默认端口
            this.txt_port.Text = PortInUse(18001) ? "28001" : "18001";
            this.StartService(true);

            //初始化打印列表
            InitPrinterList();

            //初始化读卡服务
            InitReadCard();

            // 初始化读取数据类型
            InitReadCardType();

            if (this.lab_ServiceState.Text == "服务运行正常")
            {
                timer = new System.Windows.Forms.Timer();
                timer.Enabled = false;
                timer.Interval = 4000;
                timer.Tick += new EventHandler(TimerShowInTaskbar);
                timer.Start();
            }
        }
        #region 当程序已经打开：结束之前的进程，并把托盘中的图标清除
        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32", EntryPoint = "GetWindowThreadProcessId")]
        public static extern int GetWindowThreadProcessId(IntPtr hwnd, out int pid);

        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessage(int hWnd, int Msg, int wParam, ref COPYDATASTRUCT lParam);

        const int WM_COPYDATA = 0x004A; //当一个应用程序传递数据给另一个应用程序时发送此消息

        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            [MarshalAs(UnmanagedType.LPStr)]
            public string lpData;
        }
        /// <summary>
        /// 根据进程名称结束某个进程
        /// </summary>
        /// <param name="windowName">IMCISAssistTool</param>
        private void KillWinNameProcess(string windowName)
        {
            int pid = 0;
            try
            {
                IntPtr hwnd = FindWindow(null, windowName);
                int ihWnd = (int)hwnd;
                GetWindowThreadProcessId(hwnd, out pid);
                var p = Process.GetProcessById(pid);
                if (p != null)
                {
                    //利用发送消息方式，通知进程自杀（这种模式可以自动销毁任务栏图标）
                    byte[] sarr = System.Text.Encoding.Default.GetBytes("SelfKill");
                    int len = sarr.Length;
                    COPYDATASTRUCT cds;

                    cds.dwData = (IntPtr)Convert.ToInt16("1");//可以是任意值 
                    cds.cbData = len + 1;//指定lpData内存区域的字节数 
                    cds.lpData = "SelfKill";//发送给目标窗口所在进程的数据

                    int iflag = SendMessage(ihWnd, WM_COPYDATA, 0, ref cds);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        /// <summary>
        /// 消息接受处理函数
        /// </summary>
        /// <param name="m"></param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_COPYDATA:
                    COPYDATASTRUCT cds = new COPYDATASTRUCT();
                    Type t = cds.GetType();
                    cds = (COPYDATASTRUCT)m.GetLParam(t);
                    string strResult = cds.dwData.ToString() + ":" + cds.lpData;

                    //强制退出
                    if (strResult == "1:SelfKill")
                    {
                        this.notifyIcon1.Dispose();
                        System.Environment.Exit(0);
                    }
                    break;
                default:
                    base.DefWndProc(ref m);
                    break;
            }
        }
        #endregion
        /// <summary>
        /// 判断端口是否被使用
        /// </summary>
        public static bool PortInUse(int port)
        {
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipProperties.GetActiveTcpListeners();

            foreach (IPEndPoint endPoint in ipEndPoints)
            {
                if (endPoint.Port == port)
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 服务启动
        /// </summary>
        private void StartService(bool isInit)
        {
            try
            {
                if (httpsv != null) httpsv.Stop();
                httpsv = null;

                //监听服务开启
                int post = Convert.ToInt32(this.txt_port.Text);
                httpsv = new HttpServer(post);

                // 添加WebSocket服务
                httpsv.AddWebSocketService<PrinterService>("/PrinterService");
                httpsv.AddWebSocketService<CardReaderService>("/CardReaderService");
                httpsv.Start();
        
                if (httpsv.IsListening)
                {
                    this.lab_ServiceState.Text = "服务运行正常";
                    this.lab_ServiceState.ForeColor = System.Drawing.Color.Green;
                }
                else
                {
                    this.lab_Tips.Visible = false;
                    this.lab_ServiceState.Text = "服务运行异常";
                    this.lab_ServiceState.ForeColor = System.Drawing.Color.Red;
                }
            }
            catch (Exception ex)
            {
                this.lab_Tips.Visible = false;
                this.lab_ServiceState.Text = "服务启动失败，" + ex.Message;
                this.lab_ServiceState.ForeColor = System.Drawing.Color.Red;
            }
        }

        /// <summary>
        /// 初始化打印机列表 如果有默认的选中默认
        /// </summary>
        private void InitPrinterList()
        {
            comboBox1.Items.Clear();
            PrintDocument fPrintDocument = new PrintDocument();
            for (int i = 0; i < PrinterSettings.InstalledPrinters.Count; i++)       //获取当前打印机
            {
                comboBox1.Items.Add(PrinterSettings.InstalledPrinters[i]);
                if (fPrintDocument.PrinterSettings.PrinterName == PrinterSettings.InstalledPrinters[i])
                {
                    //显示默认打印机名称
                    comboBox1.SelectedIndex = i;
                }
            }
        }
        /// <summary>
        /// 初始化读取数据类型
        /// </summary>
        private void InitReadCardType()
        {
            comboBox2.Items.Clear();
            comboBox2.Items.Add("GetICCardNo");
            comboBox2.Items.Add("GetICCardIDCardNo");
            comboBox2.Items.Add("GetICCardInfo");
            comboBox2.Items.Add("GetICCardNoLastError");
            //显示默认打印机名称
            comboBox2.SelectedIndex = 0;
        }
        [DllImport("winspool.drv")]
        public static extern bool SetDefaultPrinter(String Name);

        /// <summary>
        /// 重新启动
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            this.lab_ServiceState.Text = "正在重启...";
            StartService(true);
        }

        /// <summary>
        /// 刷新打印机列表
        /// </summary>
        private void link_Refresh_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            InitPrinterList();
        }

        /// <summary>
        /// 读卡服务检测
        /// </summary>
        private void InitReadCard()
        {
            //lab_readCard
            if (!File.Exists("ICCardR.dll"))
                this.lab_readCard.Text = "读卡服务异常：缺少必要的组建ICCardR.dll";
        }

        /// <summary>
        /// 设置默认打印机
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            if (SetDefaultPrinter(this.comboBox1.Text)) //设置默认打印机
            {
                MessageBox.Show("设置为默认打印机成功！");
            }
            else
            {
                MessageBox.Show("设置为默认打印机失败！");
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            System.Environment.Exit(System.Environment.ExitCode);
            this.Dispose();
            this.Close();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.WindowState = FormWindowState.Minimized;
            this.Visible = false;
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            WindowState = FormWindowState.Normal;
            this.Focus();
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                this.mymenu.Show();
            }
            else
            {
                this.Show();
                WindowState = FormWindowState.Normal;
                this.Focus();
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Visible = false;
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            var version = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetAssembly(this.GetType()).Location).ProductVersion;
            var copyrightInfo = "XXXXXXXX@版权所有 版本号：";

            tssCompany.Text = copyrightInfo + version;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string res = ICCardUtils.Read(this.comboBox2.Text);
            if (res.IndexOf("读卡出错") >= 0)
            {
                this.lab_readCard.ForeColor = System.Drawing.Color.Red;
            }
            this.lab_readCard.Text = res;
        }

        /// <summary>
        /// 定时窗口最小化到任务栏托盘中
        /// </summary>
        private void TimerShowInTaskbar(object sender, EventArgs e)
        {
            timer.Stop();
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            lab_Tips.Visible = false;
        }
    }
}
