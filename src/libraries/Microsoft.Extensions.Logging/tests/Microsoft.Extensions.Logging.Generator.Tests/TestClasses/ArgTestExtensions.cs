// © Microsoft Corporation. All rights reserved.

using System;

#pragma warning disable CA1801 // Review unused parameters

namespace Microsoft.Extensions.Logging.Generators.Test.TestClasses
{
    internal static partial class ArgTestExtensions
    {
        [LoggerMessage(0, LogLevel.Error, "M1")]
        public static partial void Method1(ILogger logger);

        [LoggerMessage(1, LogLevel.Error, "M2 {p1}")]
        public static partial void Method2(ILogger logger, string p1);

        [LoggerMessage(2, LogLevel.Error, "M3 {p1} {p2}")]
        public static partial void Method3(ILogger logger, string p1, int p2);

        [LoggerMessage(3, LogLevel.Error, "M4")]
        public static partial void Method4(ILogger logger, InvalidOperationException p1);

        [LoggerMessage(4, LogLevel.Error, "M5 {p2}")]
        public static partial void Method5(ILogger logger, System.InvalidOperationException p1, System.InvalidOperationException p2);

        [LoggerMessage(5, LogLevel.Error, "M6 {p2}")]
        public static partial void Method6(ILogger logger, System.InvalidOperationException p1, int p2);

        [LoggerMessage(6, LogLevel.Error, "M7 {p1}")]
        public static partial void Method7(ILogger logger, int p1, System.InvalidOperationException p2);

        [LoggerMessage(7, LogLevel.Error, "M8{p1}{p2}{p3}{p4}{p5}{p6}{p7}")]
#pragma warning disable S107 // Methods should not have too many parameters
        public static partial void Method8(ILogger logger, int p1, int p2, int p3, int p4, int p5, int p6, int p7);
#pragma warning restore S107 // Methods should not have too many parameters

        [LoggerMessage(8, LogLevel.Error, "M9 {p1} {p2} {p3} {p4} {p5} {p6} {p7}")]
#pragma warning disable S107 // Methods should not have too many parameters
        public static partial void Method9(ILogger logger, int p1, int p2, int p3, int p4, int p5, int p6, int p7);
#pragma warning restore S107 // Methods should not have too many parameters

        [LoggerMessage(9, LogLevel.Error, "M10{p1}")]
        public static partial void Method10(ILogger logger, int p1);
    }
}
