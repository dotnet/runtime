// © Microsoft Corporation. All rights reserved.

#pragma warning disable CA1801 // Review unused parameters
#pragma warning disable LG0015

namespace Microsoft.Extensions.Logging.Generators.Test.TestClasses
{
    internal static partial class MessageTestExtensions
    {
        [LoggerMessage(0, LogLevel.Trace)]
        public static partial void M0(ILogger logger);

        [LoggerMessage(1, LogLevel.Debug, "")]
        public static partial void M1(ILogger logger);

        [LoggerMessage(2, LogLevel.Trace)]
        public static partial void M2(ILogger logger, string p1, string p2);

        [LoggerMessage(3, LogLevel.Debug, "")]
        public static partial void M3(ILogger logger, string p1, int p2);

//        [LoggerMessage(4, LogLevel.Debug, "{p1}")]
//        public static partial void M4(ILogger logger, string p1, int p2, int p3);
    }
}
