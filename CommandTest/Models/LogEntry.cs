using System;

namespace CommandTest.Models
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Type { get; set; } = string.Empty;  // "Send", "Receive", "Error"
        public string Data { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public string ErrorType { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public double ResponseTime { get; set; }

        public override string ToString()
        {
            if (Type == "Error")
            {
                return $"{Timestamp:yyyy/MM/dd HH:mm:ss.fff} [{Type}] {ErrorType} at {Location}: {Data}";
            }
            
            var responseTimeStr = ResponseTime > 0 ? $" ({ResponseTime:F1}ms)" : "";
            return $"{Timestamp:yyyy/MM/dd HH:mm:ss.fff} [{Type}]{responseTimeStr} {Data} => {Result}";
        }

        public string ToCsv()
        {
            return $"{Timestamp:yyyy/MM/dd HH:mm:ss.fff},{Type},{Data},{Result},{ErrorType},{Location},{ResponseTime}";
        }

        public static string CsvHeader => "Timestamp,Type,Data,Result,ErrorType,Location,ResponseTime";
    }
}
