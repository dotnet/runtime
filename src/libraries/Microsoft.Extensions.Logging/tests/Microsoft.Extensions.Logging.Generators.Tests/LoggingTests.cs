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
    }

    public class LoggingTests
    {
        [Fact]
        public void ExtensionMethodTest()
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
            var rol = logger.LastState as IReadOnlyList<KeyValuePair<string, object >>;
            Assert.Equal(0, rol!.Count);
            Assert.Empty(rol);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = rol[0]);

            logger.Reset();
            ArgTestExtensions.Method2(logger, "arg1");
            rol = logger.LastState as IReadOnlyList<KeyValuePair<string, object>>;
            Assert.Equal(1, rol!.Count);
#pragma warning disable CA1829 // Use Length/Count property instead of Count() when available
            Assert.Equal(1, rol.LongCount());
#pragma warning restore CA1829 // Use Length/Count property instead of Count() when available
            Assert.Equal("p1", (string)rol[0].Key);
            Assert.Equal("arg1", (string)rol[0].Value);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = rol[1]);

            logger.Reset();
            ArgTestExtensions.Method3(logger, "arg1", 2);
            rol = logger.LastState as IReadOnlyList<KeyValuePair<string, object>>;
            Assert.Equal(2, rol!.Count);
#pragma warning disable CA1829 // Use Length/Count property instead of Count() when available
            Assert.Equal(2, rol.LongCount());
#pragma warning restore CA1829 // Use Length/Count property instead of Count() when available
            Assert.Equal("p1", (string)rol[0].Key);
            Assert.Equal("arg1", (string)rol[0].Value);
            Assert.Equal("p2", (string)rol[1].Key);
            Assert.Equal(2, (int)rol[1].Value);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = rol[2]);
        }
    }
}
