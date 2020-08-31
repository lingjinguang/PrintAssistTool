using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace PrintAssist.Model
{
    public class PrinterData : MessageEvent<string>
    {
        public string printerName { get; set; }
        /// <summary>
        /// 打印纸张名称
        /// </summary>
        public string paperName { get; set; }
        /// <summary>
        /// 打印方向
        /// </summary>
        public string direction { get; set; }
        /// <summary>
        /// 图片宽
        /// </summary>
        public int width { get; set; }
        /// <summary>
        /// 图片高
        /// </summary>
        public int height { get; set; }
        /// <summary>
        /// 是否自定义大小
        /// </summary>
        public Boolean customSizeFlag { get; set; }
        /// <summary>
        /// 文件路径
        /// </summary>
        public string filePath { get; set; }
        /// <summary>
        /// 图片Base64格式数据
        /// </summary>
        public string imgBase64 { get; set; }
        /// <summary>
        /// 图片
        /// </summary>
        public Image image { get; set; }
    }
}
