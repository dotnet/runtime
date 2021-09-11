// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
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

        [LoggerMessage(eventId: 10, level: LogLevel.Warning, message: "event ID 10 vs. 11", EventId = 11)]
        public static partial void M10vs11(ILogger logger);
    }
}
