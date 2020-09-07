using System;
using System.IO;

namespace PrintAssist.Common
{
    public class DynamicFilePath
    {
        private static DynamicFilePath _instance;
        public string dynamicFilePath = Directory.GetCurrentDirectory();
        //public string dynamicFilePath = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), @"PrintAssist\");
        public string dynamicFilePathByIe = AppDomain.CurrentDomain.BaseDirectory;  //路径指向浏览器引用dll所在路径（正常dll放在ie安装的根目录下 C:\Program Files (x86)\Internet Explorer\）

        // 显式静态构造函数告诉C＃编译器
        // 不要将类型标记为BeforeFieldInit
        static DynamicFilePath()
        {
        }

        private DynamicFilePath(Boolean ieFlag)
        {
            dynamicFilePath = ieFlag ? dynamicFilePathByIe : dynamicFilePath;
        }

        public static DynamicFilePath GetInstance(Boolean ieFlag)
        {
            if (_instance == null)
            {
                return _instance = new DynamicFilePath(ieFlag);
            }
            return _instance;
        }
    }
}
