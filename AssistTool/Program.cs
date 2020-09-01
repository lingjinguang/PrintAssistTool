using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;

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
            #region 当程序已经打开：结束之前的进程，开启新进程
            //获取与当前进程同名集合（不包括当前进程）
            List<Process> processes = Process.GetProcesses().Where(p => p.ProcessName == Process.GetCurrentProcess().ProcessName && p.Id != Process.GetCurrentProcess().Id).ToList();
            foreach (Process process in processes)
            {
                try
                {
                    process.Kill();
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
            #endregion
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
