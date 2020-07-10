using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Gugubao.Rabbitmq
{
    /// <summary>
    /// 消息订阅Handler接口
    /// </summary>
    /// <typeparam name="T">IntegrationEvent</typeparam>
    public interface IIntegrationEventHandler<T> where T : IntegrationEvent
    {
        /// <summary>
        /// 执行Handler处理程序
        /// </summary>
        /// <param name="integrationEvent">IntegrationEvent</param>
        /// <returns>void</returns>
        Task Handle(T integrationEvent);
    }
}
