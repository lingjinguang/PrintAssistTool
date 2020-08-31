using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Drawing;
using WebSocketSharp;
using WebSocketSharp.Server;
using PrintAssist.Model;
using PrintAssist.Utils;
using PrintAssist.Common;

namespace PrintAssistTool.Services
{
    public class PrinterService : WebSocketBehavior
    {
        //GetInstance参数 ： true（ie浏览器activeX模式，临时文件存放在C:\IMCISPrintAssistActiveX），false（应用程序模式，临时文件在程序安装根目录）
        private static readonly DynamicFilePath dfp = DynamicFilePath.GetInstance(false);
        private static PrinterService _instance { get; set; }
        /// <summary>
        /// 获得实例
        /// </summary>
        public static PrinterService Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new PrinterService();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }
       
        public PrinterService()
        {

        }

        /// <summary>
        /// 客户端发送send请求会被触发，接收请求数据
        /// </summary>
        protected override void OnMessage(MessageEventArgs e)
        {
            String result = "打印成功！";
            try
            {
                PrinterData printerData = JsonHelper.DeserializeJsonToObject<PrinterData>(e.Data);
                string eventType = (printerData.eventType).ToUpper();
                string printType = (printerData.printType).ToUpper();
                string folderName = Guid.NewGuid().ToString();  //pdf转图片所存放的文件夹名称
                string tempDir = Path.Combine(dfp.dynamicFilePath, "tempImg");  //生成图片临时存放文件夹
                string tempImageDir = Path.Combine(tempDir, folderName);
                //string targetPath = Path.Combine(tempDir, fileName + ".jpg");
                int width = printerData.width;
                int height = printerData.height;
                Boolean customSizeFlag = printerData.customSizeFlag;
                WebRequest webreq = null;
                WebResponse webres = null;
                Boolean isUrl = HttpUtils.IsUrl(Convert.ToString(printerData.data));
                string fileType = printerData.fileType.ToUpper();
                #region
                if (File.Exists(Convert.ToString(printerData.data)))   //fileType由于IMCIS没有对应变量，data是路径文件名时取值为其后缀，否则当printType时“BASE64”也是JPG，否则就是HTML打印
                {
                    fileType = Path.GetExtension(Convert.ToString(printerData.data)).Substring(1).ToUpper();
                }
                else if (isUrl) //网络文件，通过ContentType获取
                {
                    webreq = WebRequest.Create(Convert.ToString(printerData.data));
                    webres = webreq.GetResponse();
                    string[] contentType = Convert.ToString(webres.ContentType).Split('/');
                    fileType = contentType.Length > 1 ? contentType[1].ToUpper() : fileType;
                }
                else if (fileType != "PRINTERNAMES" && fileType != "PAGESIZES")
                {
                    fileType = printType == "BASE64" ? "JPG" : "HTML";
                }
                #endregion
                switch (fileType)
                {
                    case "JPG":
                    case "JPEG":
                    case "PNG":
                    case "RAW":
                    case "BMP":
                        if (isUrl)
                        {
                            using (Stream stream = webres.GetResponseStream())
                            {
                                using (Image image = Image.FromStream(stream))
                                {
                                    printerData.image = image;
                                    result = PrintUtils.PrintImage(printerData);
                                }
                            }
                        }
                        else
                        {
                            if(printType == "FILEPATH")
                            {
                                printerData.filePath = printerData.data;
                            }
                            else if(printType == "BASE64")
                            {
                                printerData.imgBase64 = printerData.data;
                            }
                            result = PrintUtils.PrintImage(printerData);
                        }
                        break;
                    case "TXT":
                        PrintUtils.PrintTxt(printerData);
                        break;
                    case "PDF":
                        string fileName = Path.GetFileNameWithoutExtension(printerData.data);  //通过完整路径取得pdf文件名称作为jpg的文件名
                        if (isUrl)
                        {
                            using (Stream stream = webres.GetResponseStream())
                            {
                                MemoryStream ms = HttpUtils.StreamToMemoryStream(stream);
                                Pdf2JpgUtils.Pdf2Jpg(ms, Path.Combine(tempDir, fileName + ".jpg"), null, folderName);
                            }
                        }
                        else
                        {
                            Pdf2JpgUtils.Pdf2Jpg(Convert.ToString(printerData.data), Path.Combine(tempDir, fileName + ".jpg"), null, folderName);
                        }

                        if (eventType == "PRINT")
                        {
                            var files = Directory.GetFiles(tempImageDir).OrderBy(f => Convert.ToInt32(Path.GetFileNameWithoutExtension(f)));
                            foreach (var file in files)
                            {
                                printerData.filePath = file;
                                result = PrintUtils.PrintImage(printerData);
                            }
                            try
                            {
                                //删除文件夹
                                Directory.Delete(tempImageDir, true);
                            }
                            catch (Exception ex)
                            {
                                result = string.Format("删除临时目录出错：{0};其他错误信息：{1}", tempImageDir, ex);
                            }
                        }
                        else if (eventType == "PREVIEW")
                        {
                            printerData.filePath = tempImageDir;
                            result = PrintUtils.PreView(printerData);
                        }
                        break;
                    case "HTML":
                        string tempPdf = Path.Combine(dfp.dynamicFilePath, "tempPdf");
                        string tempPdfDir = Path.Combine(tempPdf, folderName);
                        Directory.CreateDirectory(tempPdfDir);
                        string tempPdfPath = Path.Combine(tempPdfDir, "Html2Pdf.pdf");
                        Html2PdfUtils.Html2Pdf(Convert.ToString(printerData.data).Trim(), tempPdfPath, printerData.paperName, printerData.direction, customSizeFlag, width, height);
                        Pdf2JpgUtils.Pdf2Jpg(tempPdfPath, Path.Combine(tempDir, "Html2Pdf.jpg"), null, folderName);
                        if (eventType == "PRINT")
                        {
                            var files = Directory.GetFiles(tempImageDir).OrderBy(f => Convert.ToInt32(Path.GetFileNameWithoutExtension(f)));
                            foreach (var file in files)
                            {
                                printerData.filePath = file;
                                result = PrintUtils.PrintImage(printerData);
                            }
                            try
                            {
                                //删除临时文件夹
                                Directory.Delete(tempImageDir, true);
                                Directory.Delete(tempPdfDir, true);
                            }
                            catch (Exception ex)
                            {
                                result = string.Format("删除临时目录出错：{0};其他错误信息：{1}", tempPdfDir, ex);
                            }
                        }
                        else if (eventType == "PREVIEW")
                        {
                            printerData.filePath = tempImageDir;
                            result = PrintUtils.PreView(printerData);
                        }
                        break;
                    #region pdf不转图片直接打印
                    /*
                    case "PDF":
                        string fileName = Path.GetFileNameWithoutExtension(printerData.data);  //通过完整路径取得pdf文件名称作为jpg的文件名
                        printerData.filePath = printerData.data;
                        if (eventType == "PRINT")
                        {
                            //result = PrintUtils.PrintPdf(Convert.ToString(printerData.data), eventType, printerData.direction, printerData.printerName, printerData.paperName, printerData.customSizeFlag, printerData.width, printerData.height);
                            result = PrintUtils.PrintPdf(printerData);
                        }
                        else if (eventType == "PREVIEW")
                        {
                            //result = PrintUtils.PreViewPdf(Convert.ToString(printerData.data), eventType, printerData.direction, printerData.printerName, printerData.paperName, printerData.customSizeFlag, printerData.width, printerData.height);
                            result = PrintUtils.PreViewPdf(printerData);
                        }
                        break;
                    case "HTML":
                        string tempPdf = Path.Combine(dfp.dynamicFilePath, "tempPdf");
                        string tempPdfDir = Path.Combine(tempPdf, folderName);
                        Directory.CreateDirectory(tempPdfDir);
                        string tempPdfPath = Path.Combine(tempPdfDir, "Html2Pdf.pdf");
                        Html2PdfUtils.Html2Pdf(Convert.ToString(printerData.data).Trim(), tempPdfPath, printerData.paperName, printerData.direction, printerData.customSizeFlag, printerData.width, printerData.height);
                        printerData.filePath = tempPdfPath;
                        if (eventType == "PRINT")
                        {
                            //result = PrintUtils.PrintPdf(tempPdfPath, eventType, printerData.direction, printerData.printerName, printerData.paperName, printerData.customSizeFlag, printerData.width, printerData.height);
                            result = PrintUtils.PrintPdf(printerData);
                        }
                        else if (eventType == "PREVIEW")
                        {
                            //result = PrintUtils.PreViewPdf(tempPdfPath, eventType, printerData.direction, printerData.printerName, printerData.paperName, printerData.customSizeFlag, printerData.width, printerData.height);
                            result = PrintUtils.PreViewPdf(printerData);
                        }
                        try
                        {
                            //删除临时文件夹
                            Directory.Delete(tempPdfDir, true);
                        }
                        catch (Exception ex)
                        {
                            result = string.Format("删除临时目录出错：{0};其他错误信息：{1}", tempPdfDir, ex);
                        }
                        break;
                         * */
                    #endregion
                    case "PRINTERNAMES":
                        result = PrintUtils.GetPrinterNames();
                        break;
                    case "PAGESIZES":
                        result = PrintUtils.GetPageSizesByName(Convert.ToString(printerData.data));
                        break;
                    default:
                        result = "参数异常:fileType字段只能为jpg、pdf、html、getPrinterName、getPageSizes";
                        break;
                }
            }
            catch(Exception ex)
            {
                result = "打印出错【" + ex.Message + "】";
            }
            Send(result);
        }

        protected override void OnOpen()
        {
            //Instance();
        }
    }
}
