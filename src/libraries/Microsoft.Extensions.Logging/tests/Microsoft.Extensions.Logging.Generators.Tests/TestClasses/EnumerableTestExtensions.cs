// © Microsoft Corporation. All rights reserved.

using System.Collections;
using System.Collections.Generic;

#pragma warning disable CA1801 // Review unused parameters

namespace Microsoft.Extensions.Logging.Generators.Test.TestClasses
{
    public class Foo : IEnumerable
    {
        public IEnumerator GetEnumerator()
        {
            return null!;
        }
    }

    internal static partial class EnumerableTestExtensions
    {
        [LoggerMessage(0, LogLevel.Error, "M0")]
        public static partial void M0(ILogger logger);

        [LoggerMessage(1, LogLevel.Error, "M1{p0}")]
        public static partial void M1(ILogger logger, IEnumerable<int> p0);

        [LoggerMessage(2, LogLevel.Error, "M2{p0}{p1}")]
        public static partial void M2(ILogger logger, int p0, IEnumerable<int> p1);

        [LoggerMessage(3, LogLevel.Error, "M3{p0}{p1}{p2}")]
        public static partial void M3(ILogger logger, int p0, IEnumerable<int> p1, int p2);

        [LoggerMessage(4, LogLevel.Error, "M4{p0}{p1}{p2}{p3}")]
        public static partial void M4(ILogger logger, int p0, IEnumerable<int> p1, int p2, int p3);

        [LoggerMessage(5, LogLevel.Error, "M5{p0}{p1}{p2}{p3}{p4}")]
        public static partial void M5(ILogger logger, int p0, IEnumerable<int> p1, int p2, int p3, int p4);

        [LoggerMessage(6, LogLevel.Error, "M6{p0}{p1}{p2}{p3}{p4}{p5}")]
        public static partial void M6(ILogger logger, int p0, IEnumerable<int> p1, int p2, int p3, int p4, int p5);

#pragma warning disable S107 // Methods should not have too many parameters

        [LoggerMessage(7, LogLevel.Error, "M7{p0}{p1}{p2}{p3}{p4}{p5}{p6}")]
        public static partial void M7(ILogger logger, int p0, IEnumerable<int> p1, int p2, int p3, int p4, int p5, int p6);

        [LoggerMessage(8, LogLevel.Error, "M8{p0}{p1}{p2}{p3}{p4}{p5}{p6}{p7}")]
        public static partial void M8(ILogger logger, int p0, IEnumerable<int> p1, int p2, int p3, int p4, int p5, int p6, int p7);

        [LoggerMessage(9, LogLevel.Error, "M9{p0}{p1}{p2}{p3}{p4}{p5}{p6}{p7}{p8}")]
        public static partial void M9(ILogger logger, int p0, IEnumerable p1, int p2, int p3, int p4, int p5, int p6, int p7, int p8);
    }
}
