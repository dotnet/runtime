// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
{
    internal static partial class ArgTestExtensions
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "M1")]
        public static partial void Method1(ILogger logger);

        [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "M2 {p1}")]
        public static partial void Method2(ILogger logger, string p1);

        [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "M3 {p1} {p2}")]
        public static partial void Method3(ILogger logger, string p1, int p2);

        [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "M4")]
        public static partial void Method4(ILogger logger, InvalidOperationException p1);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "M5 {p2}")]
        public static partial void Method5(ILogger logger, System.InvalidOperationException p1, System.InvalidOperationException p2);

        [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "M6 {p2}")]
        public static partial void Method6(ILogger logger, System.InvalidOperationException p1, int p2);

        [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "M7 {p1}")]
        public static partial void Method7(ILogger logger, int p1, System.InvalidOperationException p2);

        [LoggerMessage(EventId = 7, Level = LogLevel.Error, Message = "M8{p1}{p2}{p3}{p4}{p5}{p6}{p7}")]
        public static partial void Method8(ILogger logger, int p1, int p2, int p3, int p4, int p5, int p6, int p7);

        [LoggerMessage(EventId = 8, Level = LogLevel.Error, Message = "M9 {p1} {p2} {p3} {p4} {p5} {p6} {p7}")]
        public static partial void Method9(ILogger logger, int p1, int p2, int p3, int p4, int p5, int p6, int p7);

        [LoggerMessage(EventId = 9, Level = LogLevel.Error, Message = "M10{p1}")]
        public static partial void Method10(ILogger logger, int p1);
    }
}
