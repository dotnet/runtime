namespace Microsoft.Extensions.Logging
{
    public enum LogLevel
    {
        Debug,
        Information,
        Warning,
        Error,
    }

    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false)]
    public sealed class LoggerMessageAttribute : System.Attribute
    {
        public LoggerMessageAttribute(int eventId, LogLevel level, string message) => (EventId, Level, Message) = (eventId, level, message);
        public int EventId { get; set; }
        public string? EventName { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; }
    }

    public interface ILogger
    {
    }
}

namespace Test
{
    using Microsoft.Extensions.Logging;
}