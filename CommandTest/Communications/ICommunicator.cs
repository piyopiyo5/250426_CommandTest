using System;
using System.Threading.Tasks;

namespace CommandTest.Communications
{
    public interface ICommunicator
    {
        bool IsConnected { get; }
        Task Connect();
        Task Disconnect();
        Task Send(string data);
        Task<string> Receive();
    }
}
