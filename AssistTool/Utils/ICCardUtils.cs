using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using PrintAssistTool.Model.ICCard;

namespace PrintAssistTool.Utils
{
    public class ICCardUtils
    {
        //读取IC卡号
        [DllImport("ICCardR.dll", EntryPoint = "GetICCardNo")]
        public static extern string GetICCardNo();

        //读取IC卡信息
        [DllImport("ICCardR.dll", EntryPoint = "GetICCardInfo")]
        public static extern string GetICCardInfo(int type);

        //获取读卡错误信息
        [DllImport("ICCardR.dll", EntryPoint = "GetLastError")]
        public static extern string GetLastError();

        /// <summary>
        /// 读卡
        /// </summary>
        public static string Read(string type)
        {
            try
            {
                switch (type.ToUpper())
                {
                    case "GETICCARDNO":
                        return GetICCardNo();
                    case "GETICCARDIDCARDNO":
                        return GetICCardInfo(1);
                    case "GETICCARDINFO":
                        return GetICCardInfo(0);
                    case "GETICCARDNOLASTERROR":
                        return GetLastError();
                    default:
                        return string.Format("读卡出错【未知的type：{0}】", type);
                }
                /* *
                if (string.IsNullOrEmpty(type) || type.Equals("cardNo", StringComparison.CurrentCultureIgnoreCase))
                {
                    var intPtrCardNo = GetICCardNo();
                    var cardNo = Marshal.PtrToStringAnsi(intPtrCardNo);
                    if (string.IsNullOrEmpty(cardNo))
                    {
                        var erro = GetLastError();
                        return "读卡出错" + "【" + Marshal.PtrToStringAnsi(erro) + "】";
                    }
                    //数据类型:数据
                    return "cardNo:" + cardNo;
                }
                else if (type.Equals("cardInfo", StringComparison.CurrentCultureIgnoreCase))
                {
                    var cardInfo = new CardInfo();
                    //身份证号
                    var intPtrCardInfo = GetICCardInfo(1);
                    cardInfo.IdNo = Marshal.PtrToStringAnsi(intPtrCardInfo);
                    //姓名
                    var name = GetICCardInfo(2);
                    cardInfo.Name = Marshal.PtrToStringAnsi(name);
                    //性别
                    var sex = GetICCardInfo(3);
                    cardInfo.Sex = Marshal.PtrToStringAnsi(sex);
                    //数据类型:数据1,数据2,数据3
                    return "cardInfo:" + cardInfo.IdNo + "," + cardInfo.Name + "," + cardInfo.Sex;
                }
                else
                {
                    return "读卡出错【参数(data)只能是空值、cardInfo、cardNo】";
                }
                 * */
            }
            catch (Exception e)
            {
                return "读卡出错【" + e.Message + "】";
            }
        }
    }
}
