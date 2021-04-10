// © Microsoft Corporation. All rights reserved.

#pragma warning disable CA1801 // Review unused parameters
#pragma warning disable LG0015

namespace Microsoft.Extensions.Logging.Generators.Test.TestClasses
{
    internal static partial class MessageTestExtensions
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Trace)]
        public static partial void M0(ILogger logger);

        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "")]
        public static partial void M1(ILogger logger);

#if false
        // These are disabled due to https://github.com/dotnet/roslyn/issues/52527
        //
        // These are handled fine by the logger generator and generate warnings. Unfortunately, the above warning suppression is
        // not being observed by the C# compiler at the moment, so having these here causes build warnings.

        [LoggerMessage(EventId = 2, Level = LogLevel.Trace)]
        public static partial void M2(ILogger logger, string p1, string p2);

        [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "")]
        public static partial void M3(ILogger logger, string p1, int p2);

        [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "{p1}")]
        public static partial void M4(ILogger logger, string p1, int p2, int p3);
#endif
    }
}
