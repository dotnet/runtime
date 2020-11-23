// © Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Xunit;

#pragma warning disable CA1801 // Review unused parameters

// Used to test use outside of a namespace
partial class LoggerExtensionsNoNamespace
{
    [LoggerMessage(0, LogLevel.Critical, "Could not open socket to `{hostName}`")]
    public static partial void CouldNotOpenSocketNoNamespace(ILogger logger, string hostName);
}

namespace Microsoft.Extensions.Logging.Generators.Tests
{
    // used to test use inside a namespace
    partial class LoggerExtensions
    {
        [LoggerMessage(0, LogLevel.Critical, "Could not open socket to `{hostName}`")]
        public static partial void CouldNotOpenSocket(ILogger logger, string hostName);
    }

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

    public class GeneratedCodeTests
    {
        [Fact]
        public void BasicTests()
        {
            var logger = new MockLogger();

            logger.Reset();
            LoggerExtensions.CouldNotOpenSocket(logger, "microsoft.com");
            Assert.Equal(LogLevel.Critical, logger.LastLogLevel);
            Assert.Null(logger.LastException);
            Assert.Equal("Could not open socket to `microsoft.com`", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            LoggerExtensionsNoNamespace.CouldNotOpenSocketNoNamespace(logger, "microsoft.com");
            Assert.Equal(LogLevel.Critical, logger.LastLogLevel);
            Assert.Null(logger.LastException);
            Assert.Equal("Could not open socket to `microsoft.com`", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);
        }

        [Fact]
        public void EnableTest()
        {
            var logger = new MockLogger();

            logger.Reset();
            logger.Enabled = false;
            LoggerExtensions.CouldNotOpenSocket(logger, "microsoft.com");
            Assert.Equal(0, logger.CallCount);          // ensure the logger doesn't get called when it is disabled
        }

        [Fact]
        public void ArgTest()
        {
            var logger = new MockLogger();

            logger.Reset();
            ArgTestExtensions.Method1(logger);
            Assert.Null(logger.LastException);
            Assert.Equal("M1", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            ArgTestExtensions.Method2(logger, "arg1");
            Assert.Null(logger.LastException);
            Assert.Equal("M2", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            ArgTestExtensions.Method3(logger, "arg1", 2);
            Assert.Null(logger.LastException);
            Assert.Equal("M3 arg1 2", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            ArgTestExtensions.Method4(logger, new InvalidOperationException("A"));
            Assert.Equal("A", logger.LastException!.Message);
            Assert.Equal("M4", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            ArgTestExtensions.Method5(logger, new InvalidOperationException("A"), new InvalidOperationException("B"));
            Assert.Equal("A", logger.LastException!.Message);
            Assert.Equal("M5", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            ArgTestExtensions.Method6(logger, new InvalidOperationException("A"), 2);
            Assert.Equal("A", logger.LastException!.Message);
            Assert.Equal("M6", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            ArgTestExtensions.Method7(logger, 1, new InvalidOperationException("B"));
            Assert.Equal("B", logger.LastException!.Message);
            Assert.Equal("M7", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            ArgTestExtensions.Method8(logger, 1, 2, 3, 4, 5, 6, 7);
            Assert.Equal("M8", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            ArgTestExtensions.Method9(logger, 1, 2, 3, 4, 5, 6, 7);
            Assert.Equal("M9 1 2 3 4 5 6 7", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);
        }

        [Fact]
        public void ReadOnlyListTest()
        {
            var logger = new MockLogger();

            logger.Reset();
            ReadOnlyListExtensions.M0(logger);
            TestCollection(0, logger);

            logger.Reset();
            ReadOnlyListExtensions.M1(logger, 0);
            TestCollection(1, logger);

            logger.Reset();
            ReadOnlyListExtensions.M2(logger, 0, 1);
            TestCollection(2, logger);

            logger.Reset();
            ReadOnlyListExtensions.M3(logger, 0, 1, 2);
            TestCollection(3, logger);

            logger.Reset();
            ReadOnlyListExtensions.M4(logger, 0, 1, 2, 3);
            TestCollection(4, logger);

            logger.Reset();
            ReadOnlyListExtensions.M5(logger, 0, 1, 2, 3, 4);
            TestCollection(5, logger);

            logger.Reset();
            ReadOnlyListExtensions.M6(logger, 0, 1, 2, 3, 4, 5);
            TestCollection(6, logger);

            logger.Reset();
            ReadOnlyListExtensions.M7(logger, 0, 1, 2, 3, 4, 5, 6);
            TestCollection(7, logger);
        }

        private static void TestCollection(int expected, MockLogger logger)
        {
            var rol = (logger.LastState as IReadOnlyList<KeyValuePair<string, object?>>)!;
            Assert.NotNull(rol);

            Assert.Equal(expected, rol.Count);
            for (int i = 0; i < expected; i++)
            {
                var kvp = new KeyValuePair<string, object?>($"p{i}", i);
                Assert.Equal(kvp, rol[i]);
            }

            int count = 0;
            foreach (var actual in rol)
            {
                var kvp = new KeyValuePair<string, object?>($"p{count}", count);
                Assert.Equal(kvp, actual);
                count++;
            }

            Assert.Equal(expected, count);

            Assert.Throws<ArgumentOutOfRangeException>(() => _ = rol[expected]);
        }
    }
}
