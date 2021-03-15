// © Microsoft Corporation. All rights reserved.

#pragma warning disable CA1801 // Review unused parameters

namespace Microsoft.Extensions.Logging.Generators.Test.TestClasses
{
    internal static partial class LevelTestExtensions
    {
        [LoggerMessage(0, LogLevel.Trace, "M0")]
        public static partial void M0(ILogger logger);

        [LoggerMessage(1, LogLevel.Debug, "M1")]
        public static partial void M1(ILogger logger);

        [LoggerMessage(2, LogLevel.Information, "M2")]
        public static partial void M2(ILogger logger);

        [LoggerMessage(3, LogLevel.Warning, "M3")]
        public static partial void M3(ILogger logger);

        [LoggerMessage(4, LogLevel.Error, "M4")]
        public static partial void M4(ILogger logger);

        [LoggerMessage(5, LogLevel.Critical, "M5")]
        public static partial void M5(ILogger logger);

        [LoggerMessage(6, LogLevel.None, "M6")]
        public static partial void M6(ILogger logger);

        [LoggerMessage(7, (LogLevel)42, "M7")]
        public static partial void M7(ILogger logger);

        [LoggerMessage(8, "M8")]
        public static partial void M8(ILogger logger, LogLevel level);

        [LoggerMessage(9, "M9")]
        public static partial void M9(LogLevel level, ILogger logger);
    }
}
