using System;

namespace CommandTest.Models
{
    public class CommunicationStatistics
    {
        public TimeSpan CurrentConnectionTime { get; private set; }
        public TimeSpan TotalConnectionTime { get; private set; }
        public int ConnectionCount { get; private set; }
        public int SendSuccessCount { get; private set; }
        public int SendFailureCount { get; private set; }
        public int ReceiveSuccessCount { get; private set; }
        public int ReceiveFailureCount { get; private set; }
        public double MinResponseTime { get; private set; } = double.MaxValue;
        public double MaxResponseTime { get; private set; }
        public double AverageResponseTime { get; private set; }
        public double CurrentResponseTime { get; private set; }
        public int TimeoutCount { get; private set; }
        public int ErrorCount { get; private set; }

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
            MinResponseTime = double.MaxValue;
            MaxResponseTime = 0;
            AverageResponseTime = 0;
            CurrentResponseTime = 0;
            TimeoutCount = 0;
            ErrorCount = 0;
            connectionStartTime = null;
            totalResponseTime = 0;
            responseCount = 0;
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
            MinResponseTime = Math.Min(MinResponseTime, responseTime);
            MaxResponseTime = Math.Max(MaxResponseTime, responseTime);

            totalResponseTime += responseTime;
            responseCount++;
            AverageResponseTime = totalResponseTime / responseCount;
        }

        public string ToCsv()
        {
            return $"{CurrentConnectionTime.TotalSeconds},{TotalConnectionTime.TotalSeconds}," +
                   $"{ConnectionCount},{SendSuccessCount},{SendFailureCount}," +
                   $"{ReceiveSuccessCount},{ReceiveFailureCount}," +
                   $"{MinResponseTime},{MaxResponseTime},{AverageResponseTime},{CurrentResponseTime}," +
                   $"{TimeoutCount},{ErrorCount}";
        }

        public static string CsvHeader => 
            "CurrentConnectionTime,TotalConnectionTime,ConnectionCount," +
            "SendSuccessCount,SendFailureCount,ReceiveSuccessCount,ReceiveFailureCount," +
            "MinResponseTime,MaxResponseTime,AverageResponseTime,CurrentResponseTime," +
            "TimeoutCount,ErrorCount";
    }
}
