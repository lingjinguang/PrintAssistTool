using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace PrintAssist.Utils
{
    class Millimeters2PixelsUtils
    {
        public static double Millimeters2Pixels(double length) //length是毫米，1厘米=10毫米
        {
            if (length == 0) return 0;
            System.Windows.Forms.Panel p = new System.Windows.Forms.Panel();
            System.Drawing.Graphics g = System.Drawing.Graphics.FromHwnd(p.Handle);
            IntPtr hdc = g.GetHdc();
            int width = GetDeviceCaps(hdc, 4);     // HORZRES
            int pixels = GetDeviceCaps(hdc, 8);     // BITSPIXEL
            g.ReleaseHdc(hdc);
            return (((double)pixels / (double)width) * (double)length);
        }
        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int Index);
    }
}
