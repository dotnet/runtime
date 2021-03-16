// © Microsoft Corporation. All rights reserved.

#pragma warning disable CA1801 // Review unused parameters

namespace Microsoft.Extensions.Logging.Generators.Test.TestClasses
{
    internal static partial class CollectionTestExtensions
    {
        [LoggerMessage(0, LogLevel.Error, "M0")]
        public static partial void M0(ILogger logger);

        [LoggerMessage(1, LogLevel.Error, "M1{p0}")]
        public static partial void M1(ILogger logger, int p0);

        [LoggerMessage(2, LogLevel.Error, "M2{p0}{p1}")]
        public static partial void M2(ILogger logger, int p0, int p1);

        [LoggerMessage(3, LogLevel.Error, "M3{p0}{p1}{p2}")]
        public static partial void M3(ILogger logger, int p0, int p1, int p2);

        [LoggerMessage(4, LogLevel.Error, "M4{p0}{p1}{p2}{p3}")]
        public static partial void M4(ILogger logger, int p0, int p1, int p2, int p3);

        [LoggerMessage(5, LogLevel.Error, "M5{p0}{p1}{p2}{p3}{p4}")]
        public static partial void M5(ILogger logger, int p0, int p1, int p2, int p3, int p4);

        [LoggerMessage(6, LogLevel.Error, "M6{p0}{p1}{p2}{p3}{p4}{p5}")]
        public static partial void M6(ILogger logger, int p0, int p1, int p2, int p3, int p4, int p5);

        [LoggerMessage(7, LogLevel.Error, "M7{p0}{p1}{p2}{p3}{p4}{p5}{p6}")]
#pragma warning disable S107 // Methods should not have too many parameters
        public static partial void M7(ILogger logger, int p0, int p1, int p2, int p3, int p4, int p5, int p6);
#pragma warning restore S107 // Methods should not have too many parameters

        [LoggerMessage(8, "M8{p0}{p1}")]
        public static partial void M8(ILogger logger, LogLevel level, int p0, System.Exception ex, int p1);
    }
}
