using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebSocketSharp;
using WebSocketSharp.Server;
using PrintAssistTool.Utils;
using PrintAssist.Model;
using PrintAssist.Common;

namespace PrintAssistTool.Services
{
    class CardReaderService : WebSocketBehavior
    {
        private static CardReaderService _instance { get; set; }
        /// <summary>
        /// 获得实例
        /// </summary>
        public static CardReaderService Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new CardReaderService();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public CardReaderService()
        {

        }

        /// <summary>
        /// 客户端发送send请求会被触发，接收请求数据
        /// </summary>
        protected override void OnMessage(MessageEventArgs e)
        {
            String result;
            try
            {
                string type = JsonHelper.DeserializeJsonToObject<MessageEvent<string>>(e.Data).data;
                result = ICCardUtils.Read(type);
            }
            catch (Exception ex)
            {
                result = "读卡出错【" + ex.Message + "】";
            }
            Send(result);
        }

        protected override void OnOpen()
        {
            //Instance();
        }
    }
}
