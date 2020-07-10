using System;
using System.Collections.Generic;
using System.Text;

namespace Gugubao.Rabbitmq
{
    /// <summary>
    /// rabbitmq连接参数
    /// </summary>
    public class RabbitmqOption
    {
        public string UserName { get; set; }

        public string Password { get; set; }

        public string HostName { get; set; }

        public string QueueName { get; set; }

        /// <summary>
        /// 重连次数
        /// </summary>
        public int RetryCount { get; set; } = 5;

        /// <summary>
        /// 交换机名称
        /// </summary>
        public string BrokerName { get; set; } = "ggb";
    }
}
