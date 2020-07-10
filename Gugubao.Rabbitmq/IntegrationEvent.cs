using Gugubao.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gugubao.Rabbitmq
{
    public class IntegrationEvent
    {
        public IntegrationEvent()
        {
            CreateTime = DateTime.Now;
        }

        /// <summary>
        /// 消息Id，雪花Id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 消息创建时间
        /// </summary>
        public DateTime CreateTime { get; private set; }
    }
}
