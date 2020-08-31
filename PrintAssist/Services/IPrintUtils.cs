using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using IMCISPrintAssist.Model;

namespace IMCISPrintAssist.Services
{
    [ComVisible(true)]
    [Guid("58B4F8A6-54AE-4CD4-B56D-93AE4E6ED1C4")]
    public interface IPrintUtils
    {
        string PrintImage(PrinterData printerData);
        void PrintImagePage(Setting setting, PrinterData printerData);
        string PreView(PrinterData printerData);
        //void PrintImage(PrinterData printerData);
        void ImageScaling(Image image, int paperHeight, int paperWidth, int margin, string direction, ref int imgHeight, ref int imgWidth);
        string PrintPdf(PrinterData printerData);
        string PreViewPdf(PrinterData printerData);
        void PreViewPage(object sender, PrintPageEventArgs e, PdfiumViewer.PdfDocument pdfDocument, string direction, Boolean customSizeFlag, int width, int height, ref int? pages);
        BaseResult PrintTxt(string data, string printType);
        void PdPrintPage(object sender, PrintPageEventArgs ev);
        Image Base64ToImg(string base64str);
        void deleteTemFile();
        T Mapper<T, TS>(TS ts);
        string GetPrinterNames();
        string GetPageSizesByName(string printerName);
        void SaveSetting(string name);
        //void GetPageSizesByName(string paperName, ref int width, ref int height);
        PaperSize getPaperSize(PrinterSettings.PaperSizeCollection paperSizes, string paperName);
        void getPaperSize(string paperName, ref int width, ref int height);
    }
}
