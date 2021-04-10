// © Microsoft Corporation. All rights reserved.

#pragma warning disable CA1801 // Review unused parameters

namespace Microsoft.Extensions.Logging.Generators.Test.TestClasses
{
    internal static partial class LevelTestExtensions
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Trace, Message = "M0")]
        public static partial void M0(ILogger logger);

        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "M1")]
        public static partial void M1(ILogger logger);

        [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "M2")]
        public static partial void M2(ILogger logger);

        [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "M3")]
        public static partial void M3(ILogger logger);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "M4")]
        public static partial void M4(ILogger logger);

        [LoggerMessage(EventId = 5, Level = LogLevel.Critical, Message = "M5")]
        public static partial void M5(ILogger logger);

        [LoggerMessage(EventId = 6, Level = LogLevel.None, Message = "M6")]
        public static partial void M6(ILogger logger);

        [LoggerMessage(EventId = 7, Level = (LogLevel)42, Message = "M7")]
        public static partial void M7(ILogger logger);

        [LoggerMessage(EventId = 8, Message = "M8")]
        public static partial void M8(ILogger logger, LogLevel level);

        [LoggerMessage(EventId = 9, Message = "M9")]
        public static partial void M9(LogLevel level, ILogger logger);
    }
}
