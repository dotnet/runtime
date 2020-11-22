// © Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
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
    partial class SignatureTests
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

        public static void Combo(ILogger logger, ILogger<int> logger2)
        {
            M1(logger);
            M2(logger);
            M3(logger);
            M4(logger2);
            M5(logger, new string[] { "A" });
            M6(logger);
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

    public class GeneratedTests
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
        }

        [Fact]
        public void ReadOnlyListTest()
        {
            var logger = new MockLogger();

            logger.Reset();
            ArgTestExtensions.Method1(logger);
            var rol = logger.LastState as IReadOnlyList<KeyValuePair<string, object?>>;
            Assert.Equal(0, rol!.Count);
            Assert.Empty(rol);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = rol[0]);

            logger.Reset();
            ArgTestExtensions.Method2(logger, "arg1");
            rol = logger.LastState as IReadOnlyList<KeyValuePair<string, object?>>;
            Assert.Equal(1, rol!.Count);
#pragma warning disable CA1829 // Use Length/Count property instead of Count() when available
#pragma warning disable xUnit2013 // Do not use equality check to check for collection size.
#pragma warning disable CA1826 // Do not use Enumerable methods on indexable collections
            Assert.Equal(1, rol.Count());
#pragma warning restore CA1826 // Do not use Enumerable methods on indexable collections
#pragma warning restore xUnit2013 // Do not use equality check to check for collection size.
#pragma warning restore CA1829 // Use Length/Count property instead of Count() when available
            Assert.Equal("p1", (string)rol[0].Key);
            Assert.Equal("arg1", (string?)rol[0].Value);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = rol[1]);

            logger.Reset();
            ArgTestExtensions.Method3(logger, "arg1", 2);
            rol = logger.LastState as IReadOnlyList<KeyValuePair<string, object?>>;
            Assert.Equal(2, rol!.Count);
#pragma warning disable CA1829 // Use Length/Count property instead of Count() when available
#pragma warning disable xUnit2013 // Do not use equality check to check for collection size.
#pragma warning disable CA1826 // Do not use Enumerable methods on indexable collections
            Assert.Equal(2, rol.Count());
#pragma warning restore CA1826 // Do not use Enumerable methods on indexable collections
#pragma warning restore xUnit2013 // Do not use equality check to check for collection size.
#pragma warning restore CA1829 // Use Length/Count property instead of Count() when available
            Assert.Equal("p1", (string)rol[0].Key);
            Assert.Equal("arg1", (string?)rol[0].Value);
            Assert.Equal("p2", (string)rol[1].Key);
            Assert.Equal(2, (int?)rol[1].Value);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = rol[2]);
        }
    }
}
