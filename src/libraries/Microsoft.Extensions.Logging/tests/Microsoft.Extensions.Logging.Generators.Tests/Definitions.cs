// © Microsoft Corporation. All rights reserved.

using Microsoft.Extensions.Logging;
using System;

// NOTE: This source file serves two purposes.
//
// #1. It is used to trigger the source generator during compilation of the test suite itself. The resulting generated code
//     is then tested by GeneratedCodeTests.cs. This ensures the generated code works reliably.
//
// #2. It is loaded as a file from GeneratorEmitTests.cs, and then fed manually to the parser and then the generator. This is used
//     strictly to calculate code coverage attained by the #1 case above.

#pragma warning disable CA1801 // Review unused parameters

// Used to test use outside of a namespace
partial class NoNamespace
{
    [LoggerMessage(0, LogLevel.Critical, "Could not open socket to `{hostName}`")]
    public static partial void CouldNotOpenSocket(ILogger logger, string hostName);
}

namespace Level1
{
    // used to test use inside a one-level namespace
    partial class OneLevelNamespace
    {
        [LoggerMessage(0, LogLevel.Critical, "Could not open socket to `{hostName}`")]
        public static partial void CouldNotOpenSocket(ILogger logger, string hostName);
    }
}

namespace Level1
{
    namespace Level2
    {
        // used to test use inside a two-level namespace
        partial class TwoLevelNamespace
        {
            [LoggerMessage(0, LogLevel.Critical, "Could not open socket to `{hostName}`")]
            public static partial void CouldNotOpenSocket(ILogger logger, string hostName);
        }
    }
}

namespace Microsoft.Extensions.Logging.Generators.Tests
{
    // test particular method signature variations are generated correctly
    partial class SignatureTests<T> where T : class
    {
        // normal public method
        [LoggerMessage(0, LogLevel.Critical, "Message1")]
        public static partial void M1(ILogger logger);

        // internal method
        [LoggerMessage(1, LogLevel.Critical, "Message2")]
        internal static partial void M2(ILogger logger);

        // private method
        [LoggerMessage(2, LogLevel.Critical, "Message3")]
        private static partial void M3(ILogger logger);

        // generic ILogger
        [LoggerMessage(3, LogLevel.Critical, "Message4")]
        private static partial void M4(ILogger<int> logger);

        // random type method parameter
        [LoggerMessage(4, LogLevel.Critical, "Message5")]
        private static partial void M5(ILogger logger, System.Collections.IEnumerable items);

        // linefeeds and quotes in the message string
        [LoggerMessage(5, LogLevel.Critical, "Message6\n\"\r")]
        private static partial void M6(ILogger logger);

        // generic parameter
        [LoggerMessage(6, LogLevel.Critical, "Message7\n\"\r")]
        private static partial void M7(ILogger logger, T p1);

        // normal public method
        [LoggerMessage(7, LogLevel.Critical, "Message8")]
        private protected static partial void M8(ILogger logger);

        // internal method
        [LoggerMessage(8, LogLevel.Critical, "Message9")]
        internal protected static partial void M9(ILogger logger);

        // nullable parameter
        [LoggerMessage(9, LogLevel.Critical, "Message10")]
        internal static partial void M10(ILogger logger, string? optional);

        public static void Combo(ILogger logger, ILogger<int> logger2)
        {
            M1(logger);
            M2(logger);
            M3(logger);
            M4(logger2);
            M5(logger, new string[] { "A" });
            M6(logger);
            M8(logger);
            M9(logger);
            M10(logger, null);
        }
    }

    // test particular method signature variations are generated correctly
    static partial class SignatureTests
    {
        // extension method
        [LoggerMessage(10, LogLevel.Critical, "Message11")]
        internal static partial void M11(this ILogger logger);

        public static void Combo(ILogger logger)
        {
            logger.M11();
        }
    }

    partial class ArgTestExtensions
    {
        [LoggerMessage(0, LogLevel.Error, "M1")]
        public static partial void Method1(ILogger logger);

        [LoggerMessage(1, LogLevel.Error, "M2")]
        public static partial void Method2(ILogger logger, string p1);

        [LoggerMessage(2, LogLevel.Error, "M3 {p1} {p2}")]
        public static partial void Method3(ILogger logger, string p1, int p2);

        [LoggerMessage(3, LogLevel.Error, "M4")]
        public static partial void Method4(ILogger logger, InvalidOperationException p1);

        [LoggerMessage(4, LogLevel.Error, "M5")]
        public static partial void Method5(ILogger logger, InvalidOperationException p1, InvalidOperationException p2);

        [LoggerMessage(5, LogLevel.Error, "M6")]
        public static partial void Method6(ILogger logger, InvalidOperationException p1, int p2);

        [LoggerMessage(6, LogLevel.Error, "M7")]
        public static partial void Method7(ILogger logger, int p1, InvalidOperationException p2);

        [LoggerMessage(7, LogLevel.Error, "M8")]
        public static partial void Method8(ILogger logger, int p1, int p2, int p3, int p4, int p5, int p6, int p7);

        [LoggerMessage(8, LogLevel.Error, "M9 {p1} {p2} {p3} {p4} {p5} {p6} {p7}")]
        public static partial void Method9(ILogger logger, int p1, int p2, int p3, int p4, int p5, int p6, int p7);
    }

    partial class ReadOnlyListExtensions
    {
        [LoggerMessage(0, LogLevel.Error, "M0")]
        public static partial void M0(ILogger logger);

        [LoggerMessage(1, LogLevel.Error, "M1")]
        public static partial void M1(ILogger logger, int p0);

        [LoggerMessage(2, LogLevel.Error, "M2")]
        public static partial void M2(ILogger logger, int p0, int p1);

        [LoggerMessage(3, LogLevel.Error, "M3")]
        public static partial void M3(ILogger logger, int p0, int p1, int p2);

        [LoggerMessage(4, LogLevel.Error, "M4")]
        public static partial void M4(ILogger logger, int p0, int p1, int p2, int p3);

        [LoggerMessage(5, LogLevel.Error, "M5")]
        public static partial void M5(ILogger logger, int p0, int p1, int p2, int p3, int p4);

        [LoggerMessage(6, LogLevel.Error, "M6")]
        public static partial void M6(ILogger logger, int p0, int p1, int p2, int p3, int p4, int p5);

        [LoggerMessage(7, LogLevel.Error, "M7")]
        public static partial void M7(ILogger logger, int p0, int p1, int p2, int p3, int p4, int p5, int p6);
    }

    partial class LevelTestExtensions
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

        [LoggerMessage(7, LogLevel.None, "M7")]
        public static partial void M7(ILogger logger);
    }
}
