using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace PrintAssist.Common
{ 
    public class UnicodeFontFactory : FontFactoryImp
    {
        private static DynamicFilePath dfp = DynamicFilePath.GetInstance(false);
        private static readonly string arialFontPath = Path.Combine(Path.Combine(dfp.dynamicFilePath, "Fonts"), "arialuni.ttf");
        private static readonly string simkaiPath = Path.Combine(Path.Combine(dfp.dynamicFilePath, "Fonts"), "simkai.ttf");//楷体

        public override Font GetFont(string fontname, string encoding, bool embedded, float size, int style, BaseColor color,
            bool cached)
        {
            //第一个参数导入字体路径
            BaseFont baseFont = BaseFont.CreateFont(simkaiPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            return new Font(baseFont, size, style, color);
        }
    }

}