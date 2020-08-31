using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;

namespace PrintAssist.Common
{
    public static class ConfigDefine
    {
        public static decimal Pdf2JpgScaleRatio
        {
            get
            {
                var pdf2JpgScaleRatio = ConfigurationManager.AppSettings["Pdf2JpgScaleRatio"];
                decimal ratioValue = 0;
                if (!string.IsNullOrEmpty(pdf2JpgScaleRatio))
                {
                    if (decimal.TryParse(pdf2JpgScaleRatio, out ratioValue) == false)
                    {
                        ratioValue = 0;
                    }
                }

                return ratioValue;
            }
        }

        public static int Pdf2JpgResolution
        {
            get
            {
                var resolution = ConfigurationManager.AppSettings["Pdf2JpgResolution"];
                int resolutionValue = 300;
                if (!string.IsNullOrEmpty(resolution))
                {
                    if (int.TryParse(resolution, out resolutionValue) == false)
                    {
                        resolutionValue = 300;
                    }
                }

                return resolutionValue;
            }
        }

    }
}