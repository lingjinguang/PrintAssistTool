using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;

namespace PrintAssistTool
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            #region 自动更新
            String path = AppDomain.CurrentDomain.BaseDirectory + "AutoUpdate.exe";
            //同时启动自动更新程序
            if (File.Exists(path))
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo()
                {
                    FileName = "AutoUpdate.exe",
                    Arguments = " IMCISAssistTool 0"//1表示静默更新 0表示弹窗提示更新
                };
                Process proc = Process.Start(processStartInfo);
                if (proc != null)
                {
                    proc.WaitForExit();
                }
            }
            #endregion
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
