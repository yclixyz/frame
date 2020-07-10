using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gugubao.Rabbitmq
{
    public interface IRabbitmqManager : IDisposable
    {
        bool IsConnected { get; }

        void TryConnect();

        IModel CreateModel();
    }
}
