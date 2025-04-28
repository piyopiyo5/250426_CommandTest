using System.ComponentModel;

namespace CommandTest.Models
{
    public class CommunicationSettings : INotifyPropertyChanged
    {
        private string ipAddress = "127.0.0.1";
        private int port = 8000;
        private string delimiter = "LF";

        public string IpAddress
        {
            get => ipAddress;
            set
            {
                if (ipAddress != value)
                {
                    ipAddress = value;
                    OnPropertyChanged(nameof(IpAddress));
                }
            }
        }

        public int Port
        {
            get => port;
            set
            {
                if (port != value)
                {
                    port = value;
                    OnPropertyChanged(nameof(Port));
                }
            }
        }

        public string Delimiter
        {
            get => delimiter;
            set
            {
                if (delimiter != value)
                {
                    delimiter = value;
                    OnPropertyChanged(nameof(Delimiter));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
