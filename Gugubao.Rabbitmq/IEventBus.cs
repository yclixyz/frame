using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Gugubao.Rabbitmq
{
    /// <summary>
    /// 消息队列接口
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 发布消息
        /// </summary>
        void Publish<T>(T integrationEvent) where T : IntegrationEvent;

        /// <summary>
        /// 消息订阅
        /// </summary>
        void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;
    }
}
