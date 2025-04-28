using System;

namespace CommandTest.Models
{
    public class Command
    {
        public string CommandText { get; set; } = string.Empty;
        public string Mode { get; set; } = "Normal";  // Normal, NoCommand, NoResponse
        public int Timeout { get; set; } = 1000;  // milliseconds
        public int Interval { get; set; } = 0;    // milliseconds
        public bool IsExecuting { get; private set; }
        public DateTime LastExecutionTime { get; private set; }
        public DateTime SendTime { get; private set; }
        public double ResponseTime { get; private set; }

        public void StartExecution()
        {
            IsExecuting = true;
            SendTime = DateTime.Now;
            LastExecutionTime = SendTime;
        }

        public void CompleteExecution()
        {
            IsExecuting = false;
            ResponseTime = CalculateResponseTime();
        }

        public double CalculateResponseTime()
        {
            return (DateTime.Now - SendTime).TotalMilliseconds;
        }
    }
}
