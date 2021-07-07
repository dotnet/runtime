// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SYSLIB0027

namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
{
    internal static partial class MessageTestExtensions
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Trace)]
        public static partial void M0(ILogger logger);

        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "")]
        public static partial void M1(ILogger logger);

#if false
        // Diagnostics produced by source generators do not respect the /warnAsError or /noWarn compiler flags.
        // Disabled due to https://github.com/dotnet/roslyn/issues/52527
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

        [LoggerMessage]
        public static partial void M5(ILogger logger, LogLevel level);

        [LoggerMessage(EventId = 6, Message = "")]
        public static partial void M6(ILogger logger, LogLevel level);
    }
}
