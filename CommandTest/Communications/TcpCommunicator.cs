using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using CommandTest.Models;

namespace CommandTest.Communications
{
    public class TcpCommunicator : ICommunicator, IDisposable
    {
        private TcpClient? client;
        private NetworkStream? stream;
        private readonly CommunicationSettings settings;
        private bool disposed;

        public bool IsConnected => client?.Connected ?? false;

        public TcpCommunicator(CommunicationSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task Connect()
        {
            if (IsConnected)
                return;

            try
            {
                client = new TcpClient();
                await client.ConnectAsync(settings.IpAddress, settings.Port);
                stream = client.GetStream();
            }
            catch (Exception)
            {
                await Disconnect();
                throw;
            }
        }

        public async Task Disconnect()
        {
            if (stream != null)
            {
                await stream.DisposeAsync();
                stream = null;
            }

            if (client != null)
            {
                client.Dispose();
                client = null;
            }
        }

        public async Task Send(string data)
        {
            if (!IsConnected || stream == null)
                throw new InvalidOperationException("Not connected");

            var delimiter = settings.Delimiter switch
            {
                "CR" => "\r",
                "LF" => "\n",
                "CRLF" => "\r\n",
                _ => throw new ArgumentException("Invalid delimiter")
            };

            var bytes = Encoding.UTF8.GetBytes(data + delimiter);
            await stream.WriteAsync(bytes);
        }

        public async Task<string> Receive()
        {
            if (!IsConnected || stream == null)
                throw new InvalidOperationException("Not connected");

            var buffer = new byte[1024];
            var builder = new StringBuilder();

            do
            {
                var bytesRead = await stream.ReadAsync(buffer);
                if (bytesRead == 0)
                    break;

                builder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));

                // デリミタに基づいて読み取り完了を判定
                var data = builder.ToString();
                if (data.EndsWith(settings.Delimiter switch
                {
                    "CR" => "\r",
                    "LF" => "\n",
                    "CRLF" => "\r\n",
                    _ => throw new ArgumentException("Invalid delimiter")
                }))
                {
                    return data.TrimEnd('\r', '\n');
                }
            } while (stream.DataAvailable);

            return builder.ToString().TrimEnd('\r', '\n');
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                stream?.Dispose();
                client?.Dispose();
            }

            disposed = true;
        }
    }
}
