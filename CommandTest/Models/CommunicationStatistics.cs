using System;
using System.ComponentModel;

namespace CommandTest.Models
{
    public class CommunicationStatistics : INotifyPropertyChanged
    {
        private TimeSpan currentConnectionTime;
        private TimeSpan totalConnectionTime;
        private int connectionCount;
        private int sendSuccessCount;
        private int sendFailureCount;
        private int receiveSuccessCount;
        private int receiveFailureCount;
        private double minResponseTime = double.MaxValue;
        private double maxResponseTime;
        private double averageResponseTime;
        private double currentResponseTime;
        private int timeoutCount;
        private int errorCount;
        private bool hasResponseTimeData;

        public TimeSpan CurrentConnectionTime
        {
            get => currentConnectionTime;
            private set
            {
                if (currentConnectionTime != value)
                {
                    currentConnectionTime = value;
                    OnPropertyChanged(nameof(CurrentConnectionTime));
                }
            }
        }

        public TimeSpan TotalConnectionTime
        {
            get => totalConnectionTime;
            private set
            {
                if (totalConnectionTime != value)
                {
                    totalConnectionTime = value;
                    OnPropertyChanged(nameof(TotalConnectionTime));
                }
            }
        }

        public int ConnectionCount
        {
            get => connectionCount;
            private set
            {
                if (connectionCount != value)
                {
                    connectionCount = value;
                    OnPropertyChanged(nameof(ConnectionCount));
                }
            }
        }

        public int SendSuccessCount
        {
            get => sendSuccessCount;
            private set
            {
                if (sendSuccessCount != value)
                {
                    sendSuccessCount = value;
                    OnPropertyChanged(nameof(SendSuccessCount));
                }
            }
        }

        public int SendFailureCount
        {
            get => sendFailureCount;
            private set
            {
                if (sendFailureCount != value)
                {
                    sendFailureCount = value;
                    OnPropertyChanged(nameof(SendFailureCount));
                }
            }
        }

        public int ReceiveSuccessCount
        {
            get => receiveSuccessCount;
            private set
            {
                if (receiveSuccessCount != value)
                {
                    receiveSuccessCount = value;
                    OnPropertyChanged(nameof(ReceiveSuccessCount));
                }
            }
        }

        public int ReceiveFailureCount
        {
            get => receiveFailureCount;
            private set
            {
                if (receiveFailureCount != value)
                {
                    receiveFailureCount = value;
                    OnPropertyChanged(nameof(ReceiveFailureCount));
                }
            }
        }

        public double? MinResponseTime
        {
            get => hasResponseTimeData ? minResponseTime : null;
            private set
            {
                if (!value.HasValue)
                {
                    if (hasResponseTimeData)
                    {
                        hasResponseTimeData = false;
                        OnPropertyChanged(nameof(MinResponseTime));
                    }
                    return;
                }

                if (!hasResponseTimeData || minResponseTime != value.Value)
                {
                    minResponseTime = value.Value;
                    hasResponseTimeData = true;
                    OnPropertyChanged(nameof(MinResponseTime));
                }
            }
        }

        public double? MaxResponseTime
        {
            get => hasResponseTimeData ? maxResponseTime : null;
            private set
            {
                if (!value.HasValue)
                {
                    if (hasResponseTimeData)
                    {
                        hasResponseTimeData = false;
                        OnPropertyChanged(nameof(MaxResponseTime));
                    }
                    return;
                }

                if (!hasResponseTimeData || maxResponseTime != value.Value)
                {
                    maxResponseTime = value.Value;
                    hasResponseTimeData = true;
                    OnPropertyChanged(nameof(MaxResponseTime));
                }
            }
        }

        public double? AverageResponseTime
        {
            get => hasResponseTimeData ? averageResponseTime : null;
            private set
            {
                if (!value.HasValue)
                {
                    if (hasResponseTimeData)
                    {
                        hasResponseTimeData = false;
                        OnPropertyChanged(nameof(AverageResponseTime));
                    }
                    return;
                }

                if (!hasResponseTimeData || averageResponseTime != value.Value)
                {
                    averageResponseTime = value.Value;
                    hasResponseTimeData = true;
                    OnPropertyChanged(nameof(AverageResponseTime));
                }
            }
        }

        public double? CurrentResponseTime
        {
            get => hasResponseTimeData ? currentResponseTime : null;
            private set
            {
                if (!value.HasValue)
                {
                    if (hasResponseTimeData)
                    {
                        hasResponseTimeData = false;
                        OnPropertyChanged(nameof(CurrentResponseTime));
                    }
                    return;
                }

                if (!hasResponseTimeData || currentResponseTime != value.Value)
                {
                    currentResponseTime = value.Value;
                    hasResponseTimeData = true;
                    OnPropertyChanged(nameof(CurrentResponseTime));
                }
            }
        }

        public int TimeoutCount
        {
            get => timeoutCount;
            private set
            {
                if (timeoutCount != value)
                {
                    timeoutCount = value;
                    OnPropertyChanged(nameof(TimeoutCount));
                }
            }
        }

        public int ErrorCount
        {
            get => errorCount;
            private set
            {
                if (errorCount != value)
                {
                    errorCount = value;
                    OnPropertyChanged(nameof(ErrorCount));
                }
            }
        }

        private DateTime? connectionStartTime;
        private double totalResponseTime;
        private int responseCount;

        public void Reset()
        {
            CurrentConnectionTime = TimeSpan.Zero;
            TotalConnectionTime = TimeSpan.Zero;
            ConnectionCount = 0;
            SendSuccessCount = 0;
            SendFailureCount = 0;
            ReceiveSuccessCount = 0;
            ReceiveFailureCount = 0;
            MinResponseTime = null;
            MaxResponseTime = null;
            AverageResponseTime = null;
            CurrentResponseTime = null;
            hasResponseTimeData = false;
            TimeoutCount = 0;
            ErrorCount = 0;
            connectionStartTime = null;
            totalResponseTime = 0;
            responseCount = 0;
            OnPropertyChanged(nameof(ConnectionCount));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void UpdateConnectionTime()
        {
            if (connectionStartTime.HasValue)
            {
                var now = DateTime.Now;
                CurrentConnectionTime = now - connectionStartTime.Value;
            }
        }

        public void StartConnection()
        {
            connectionStartTime = DateTime.Now;
            ConnectionCount++;
        }

        public void EndConnection()
        {
            if (connectionStartTime.HasValue)
            {
                TotalConnectionTime += DateTime.Now - connectionStartTime.Value;
                connectionStartTime = null;
                CurrentConnectionTime = TimeSpan.Zero;
            }
        }

        public void IncrementSendSuccess() => SendSuccessCount++;
        public void IncrementSendFailure() => SendFailureCount++;
        public void IncrementReceiveSuccess() => ReceiveSuccessCount++;
        public void IncrementReceiveFailure() => ReceiveFailureCount++;
        public void IncrementTimeout() => TimeoutCount++;
        public void IncrementError() => ErrorCount++;

        public void UpdateResponseTime(double responseTime)
        {
            if (responseTime < 0)
                return;

            CurrentResponseTime = responseTime;
            MinResponseTime = hasResponseTimeData ? Math.Min(minResponseTime, responseTime) : responseTime;
            MaxResponseTime = hasResponseTimeData ? Math.Max(maxResponseTime, responseTime) : responseTime;

            totalResponseTime += responseTime;
            responseCount++;
            AverageResponseTime = totalResponseTime / responseCount;
            hasResponseTimeData = true;
        }

        public string ToCsv()
        {
            return $"{CurrentConnectionTime.TotalSeconds:F3},{TotalConnectionTime.TotalSeconds:F3}," +
                   $"{ConnectionCount},{SendSuccessCount},{SendFailureCount}," +
                   $"{ReceiveSuccessCount},{ReceiveFailureCount}," +
                   $"{(MinResponseTime?.ToString("F3") ?? "---")}," +
                   $"{(MaxResponseTime?.ToString("F3") ?? "---")}," +
                   $"{(AverageResponseTime?.ToString("F3") ?? "---")}," +
                   $"{(CurrentResponseTime?.ToString("F3") ?? "---")}," +
                   $"{TimeoutCount},{ErrorCount}";
        }

        public static string CsvHeader => 
            "CurrentConnectionTime(s),TotalConnectionTime(s),ConnectionCount," +
            "SendSuccessCount,SendFailureCount,ReceiveSuccessCount,ReceiveFailureCount," +
            "MinResponseTime,MaxResponseTime,AverageResponseTime,CurrentResponseTime," +
            "TimeoutCount,ErrorCount";
    }
}
