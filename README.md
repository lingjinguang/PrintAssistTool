**IMCISAssistTool项目**：（适配于xp系统）

​	支持webSocket服务浏览器：基于websocket-shap（https://github.com/sta/websocket-sharp）开源项目开发，实现浏览器和客户端通信。

​	ie浏览器：IMCISPrintAssist.dll通过activeX插件形式在ie上使用。

**开发工具**：Visual Studio 2010

**FrameWork版本**：.Net FrameWork 4

**项目结构图**

![image-20200827171912673](C:\Users\1\AppData\Roaming\Typora\typora-user-images\image-20200827171912673.png)

打印方法封装在IMCISPrintTool程序集中，基于WebSocket的PrinterService服务调用PrintImage/PreView进行打印预览

MainForm.cs：应用的主图形界面。

Program.cs：程序的入口Main方法

Services文件夹：目前只有一个PrintServices.cs（只支持jpg、html、pdf打印）。

Test文件夹：print.html打印测试文件（ws的创建、连接、数据传输）。

Common文件夹：放置一些公共文件。

Model文件夹：放置数据类型的cs文件。

Utils文件夹：工具文件夹，工具属性的方法另外封装起来。

**后端搭建WebSocket服务**（MainForm.cs）

```C#
引用
using WebSocketSharp.Net;
using WebSocketSharp.Server;

private Setting setting = null;
HttpServer httpsv = null;
private void StartService(bool isInit)
{
    try
    {
        if (httpsv != null) httpsv.Stop();
        httpsv = null;

        //默认保存端口
        setting.Port = this.txt_port.Text;
        FileUtils.SaveSetting(setting);

        //监听服务开启
        int post = Convert.ToInt32(this.txt_port.Text);
        httpsv = new HttpServer(post);

        // 添加WebSocket服务
        httpsv.AddWebSocketService<PrinterService>("/PrinterService");

        //开启服务
        httpsv.Start();
    }
    catch (Exception ex)
    {
        
    }
}
```

**前端连接webSocket服务**（请参考PrintControl>Test>Print.html 测试用例）

```js
//服务地址“ws://localhost:port/PrinterService” ，port值是启动打印服务时设置的端口值
var ws;
function connectWebSocket(url) {
    //实例化WebSocket对象
    ws = new WebSocket(url);
    //连接成功建立后响应
    ws.onopen = function () {
        log("成功连接到" + url);
    }
    //收到服务器消息后响应
    ws.onmessage = function (event) {
        log("收到服务器消息:"+event.data);
        var ret = JSON.parse(event.data);
        if (ret.code != 0)
            log("异常：" + ret.msg);
        else
            log("打印成功，后续需要回调打印成功接口");
    }
    //连接关闭后响应
    ws.onclose = function () {
        log("关闭连接");
        ws = null;
    }
    return false;
}

//发送信息到webSocket服务器进行通信
function preview (sendData){
    if (!ws || ws.readyState !== 1) {
        alert('请先连接服务');
        return false;
    }
    //发送数据
    ws.send(JSON.stringify(sendData));
}
var sendData = {
    file_type: "pdf",
    print_type: "filePath",
    event_type: "preview",
    direction: $("#Direction").val(),
    printerName: $("#printerNames").val(),
    paperName: $("#paperNames").val(),
    data: $('#temp').val()
};
preview(sendData);

```

**数据类型**

```js
var sendData = {
    file_type: "pdf",	//jpg、html、pdf 三种文件类型
    print_type: "filePath",	//filePath、Base64 二种打印数据来源方式类型
    event_type: "print",	//print、preview分别是打印、打印预览
    direction: 1,	//1:纵向；2:横向
    printerName: "",	//打印机名称
    paperName: "",	//打印纸张名称
    width: 210,	//自定义模版宽度
    height: 297,	//自定义模版长度
    customSizeFlag: false,	//是否使用自定义模版 true：是，false：否
    data: data	//data可以是url字符串或者图片转成base64的字符串
};
```

**后端响应前端的send事件**（OnMessage）

```c#
using WebSocketSharp;
using WebSocketSharp.Server;
using IMCISPrintAssist.Model;
using IMCISPrintAssist.Utils;
using IMCISPrintAssist.Common;

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
        string tempDir = Path.Combine(Directory.GetCurrentDirectory(), "tempImg");  //生成图片临时存放文件夹
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
                    PrintUtils.PrintTxt(printerData.data, printType);
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
                    string tempPdf = Path.Combine(Directory.GetCurrentDirectory(), "tempPdf");
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
```

**JPG打印**

```C#
//使用Drawing进行图片处理
using System.Drawing;
using System.Drawing.Printing;
/// <summary>
/// 打印图片
/// </summary>
public BaseResult PrintImage(string data, string print_type)
{
    try
    {
        var setting = FileUtils.GetSetting();

        if (string.IsNullOrEmpty(setting.Name))
            return BaseResult.Error("请先设置默认打印机");
        if(string.IsNullOrEmpty(data))
            return BaseResult.Error("要打印的数据不存在");

        Image image = print_type == "FILEPATH" ? new Bitmap(data) : Base64ToImg(data);

        PrintDocument pd = new PrintDocument();
        pd.DefaultPageSettings.PrinterSettings.PrinterName = setting.Name;

        pd.PrintPage += (o, e) =>
        {
            PageSettings settings = e.PageSettings;
            var paperHeight = settings.PaperSize.Height;
            var paperWidth = settings.PaperSize.Width;
            //Rectangle m = e.MarginBounds;
            int margin = 10;
            var imgHeight = 0;
            var imgWidth = 0;
            if ((double)image.Width / (double)image.Height > (double)paperWidth / (double)paperHeight) // 图片等比例放大，宽度超出了，先确定宽度
            {
                imgWidth = (int)(paperWidth - margin * 2);
                imgHeight = (int)((double)image.Height * (double)imgWidth / (double)image.Width - margin * 2);
            }
            else
            {
                imgHeight = (int)(paperHeight - margin * 2);
                imgWidth = (int)((double)image.Width / (double)image.Height * imgHeight - margin * 2);
            }
            e.Graphics.DrawImage(image, margin, margin, imgWidth, imgHeight);
        };
        //开始打印
        pd.Print();
        image.Dispose();

        return BaseResult.Success("");
    }
    catch (Exception e)
    {
        return BaseResult.Error(e.Message);
    }
}
```

**PDF打印（PDF转成JPG在通过PrintImage打印）**

```C#
//PdfiumViewer处理pdf文件转成Image类型数据
//该程序集兼容xp参考 https://github.com/dlynine/katahiromz_pdfium
    
public static void Pdf2Jpg(string sourcePath, string targetPath, int? pages, string folderName)
{
    if (string.IsNullOrEmpty(sourcePath))
        throw new ArgumentNullException("sourcePath");
    if (string.IsNullOrEmpty(targetPath))
        throw new ArgumentNullException("targetPath");
    if (!System.IO.File.Exists(sourcePath))
        throw new Exception("文件不存在：" + sourcePath);
    using (var pdfDocument = PdfiumViewer.PdfDocument.Load(sourcePath))
    {
        Pdf2Jpg(pdfDocument, targetPath, pages, folderName);
    }
}

public static void Pdf2Jpg(PdfiumViewer.PdfDocument pdfDocument, string targetPath, int? pages, string folderName)
{
    if (string.IsNullOrEmpty(targetPath))
        throw new ArgumentNullException("targetPath");

    var dir = Path.GetDirectoryName(targetPath);
    if (!Directory.Exists(dir))
        Directory.CreateDirectory(dir);

    if (pdfDocument.PageCount == 1)
        pages = 1;

    var imageDir = Path.Combine(dir, string.IsNullOrEmpty(folderName) ? Guid.NewGuid().ToString() : folderName);
    Directory.CreateDirectory(imageDir);
    if (pages != null)
    {
        pages--;//PdfiumViewer.PdfDocument的页数是从0开始的
        using (var image = pdfDocument.Render(pages.Value, ConfigDefine.Pdf2JpgResolution, ConfigDefine.Pdf2JpgResolution, PdfiumViewer.PdfRenderFlags.CorrectFromDpi))
        {
            if (ConfigDefine.Pdf2JpgScaleRatio != 0)
            {
                Stream imageStream = new MemoryStream();
                image.Save(imageStream, ImageFormat.Jpeg);
                int imgWidth = Convert.ToInt32(image.Width * ConfigDefine.Pdf2JpgScaleRatio);
                int imgHeight = Convert.ToInt32(image.Height * ConfigDefine.Pdf2JpgScaleRatio);
                using (var imgScaleStream = ImageScale2Stream(imageStream, imgWidth, imgHeight, ""))
                {
                    using (var imgScale = System.Drawing.Image.FromStream(imgScaleStream))
                    {
                        imgScale.Save(Path.Combine(imageDir, folderName + ".jpg"));
                        imgScale.Save(targetPath);
                    }
                }
            }
            else
            {
                image.Save(Path.Combine(imageDir, folderName + ".jpg"));
                image.Save(targetPath);
            }
        }
    }
    else
    {
        for (pages = 0; pages < pdfDocument.PageCount; pages++)
        {
            using (var image = pdfDocument.Render(pages.Value, ConfigDefine.Pdf2JpgResolution, ConfigDefine.Pdf2JpgResolution, PdfiumViewer.PdfRenderFlags.CorrectFromDpi))
            {
                if (ConfigDefine.Pdf2JpgScaleRatio != 0)
                {
                    Stream imageStream = new MemoryStream();
                    image.Save(imageStream, ImageFormat.Jpeg);
                    int imgWidth = Convert.ToInt32(image.Width * ConfigDefine.Pdf2JpgScaleRatio);
                    int imgHeight = Convert.ToInt32(image.Height * ConfigDefine.Pdf2JpgScaleRatio);
                    using (var imgScaleStream = ImageScale2Stream(imageStream, imgWidth, imgHeight, ""))
                    {
                        using (var imgScale = System.Drawing.Image.FromStream(imgScaleStream))
                        {
                            imgScale.Save(Path.Combine(imageDir, pages + ".jpg"));
                        }
                    }
                }
                else
                {
                    image.Save(Path.Combine(imageDir, pages + ".jpg"));
                }

            }
        }
    }
}
```

**HTML打印（HTML转PDF转图片打印）**

```C#
//iTextSharp将HTML转成PDF，PDF再由Pdf2Jpg方法（PdfiumViewer）转成Image数据类型
//PdfiumViewer并不完整，需要把pdfium.dll放在程序安装/运行的根目录
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.IO;
using System.Text;
using iTextSharp.tool.xml;

namespace PrintControl.Utils
{
    public static class Html2PdfUtils // : System.Web.UI.Page
    {
        public static void Html2Pdf(string htmlText, string tempPdfPath)
        {
            if (string.IsNullOrEmpty(htmlText))
                throw new Exception("传入的html无内容：" + htmlText);
            MemoryStream outputStream = new MemoryStream();//要把PDF写到哪个串流
            byte[] data = Encoding.UTF8.GetBytes(htmlText);//字串转成byte[]
            MemoryStream msInput = new MemoryStream(data);
            Document doc = new Document();//要写PDF的文件，建构子没填的话预设直式A4
            PdfWriter writer = PdfWriter.GetInstance(doc, outputStream);
            //指定文件预设开档时的缩放为100%
            PdfDestination pdfDest = new PdfDestination(PdfDestination.XYZ, 0, doc.PageSize.Height, 1f);
            //开启Document文件 
            doc.Open();

            //使用XMLWorkerHelper把Html parse到PDF档里
            XMLWorkerHelper.GetInstance().ParseXHtml(writer, doc, msInput, null, Encoding.UTF8, new UnicodeFontFactory());
            //XMLWorkerHelper.GetInstance().ParseXHtml(writer, doc, msInput, null, Encoding.UTF8,字体);

            //将pdfDest设定的资料写到PDF档
            PdfAction action = PdfAction.GotoLocalPage(1, pdfDest, writer);
            writer.SetOpenAction(action);
            doc.Close();
            msInput.Close();
            outputStream.Close();
            //回传PDF档案 
            var bytes = outputStream.ToArray();

            var ret = Convert.ToBase64String(bytes);
            try
            {
                string strbase64 = ret;
                strbase64 = strbase64.Replace(' ', '+');
                System.IO.MemoryStream stream = new System.IO.MemoryStream(Convert.FromBase64String(strbase64));
                System.IO.FileStream fs = new System.IO.FileStream(tempPdfPath, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write);
                byte[] b = stream.ToArray();
                //byte[] b = stream.GetBuffer();
                fs.Write(b, 0, b.Length);
                fs.Close();

            }
            catch (Exception ex)
            {

            }
        }
    }
}
```

