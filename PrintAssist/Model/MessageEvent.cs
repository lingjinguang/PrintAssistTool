using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

namespace PrintAssist.Model
{
    /// <summary>
    /// 通讯消息事件
    /// </summary>
    public class MessageEvent<T>
    {
        /// <summary>
        /// 事件类型
        /// </summary>
        public string eventType { get; set; }
        /// <summary>
        /// 打印类型
        /// </summary>
        public string printType { get; set; }
        /// <summary>
        /// 文件类型
        /// </summary>
        public string fileType { get; set; }
        
        /// <summary>
        /// 传输数据
        /// </summary>
        public T data { get; set; }
    }
}
