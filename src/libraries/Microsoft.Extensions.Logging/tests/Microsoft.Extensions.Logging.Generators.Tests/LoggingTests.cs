// © Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Attributes;
using Xunit;

// Used to test interfaces outside of a namespace
[LoggerExtensions]
interface ILoggerExtensionsNoNamespace
{
    [LoggerMessage(0, LogLevel.Critical, "Could not open socket to `{hostName}`")]
    void CouldNotOpenSocketNoNamespace(string hostName);
}

namespace Microsoft.Extensions.Logging.Generators.Tests
{
    // used to test interfaces inside a namespace
    [LoggerExtensions]
    interface ILoggerExtensions
    {
        [LoggerMessage(0, LogLevel.Critical, "Could not open socket to `{hostName}`")]
        void CouldNotOpenSocket(string hostName);
    }

    [LoggerExtensions]
    interface IArgTestExtensions
    {
        [LoggerMessage(0, LogLevel.Error, "M1")]
        void Method1();

        [LoggerMessage(1, LogLevel.Error, "M2")]
        void Method2(string p1);

        [LoggerMessage(2, LogLevel.Error, "M3 {p1} {p2}")]
        void Method3(string p1, int p2);

        [LoggerMessage(3, LogLevel.Error, "M4")]
        void Method4(InvalidOperationException p1);

        [LoggerMessage(4, LogLevel.Error, "M5")]
        void Method5(InvalidOperationException p1, InvalidOperationException p2);

        [LoggerMessage(5, LogLevel.Error, "M6")]
        void Method6(InvalidOperationException p1, int p2);

        [LoggerMessage(6, LogLevel.Error, "M7")]
        void Method7(int p1, InvalidOperationException p2);
    }

    public class LoggingTests
    {
        [Fact]
        public void ExtensionMethodTest()
        {
            var logger = new MockLogger();

            logger.Reset();
            logger.CouldNotOpenSocket("microsoft.com");
            Assert.Equal(LogLevel.Critical, logger.LastLogLevel);
            Assert.Null(logger.LastException);
            Assert.Equal("Could not open socket to `microsoft.com`", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            logger.CouldNotOpenSocketNoNamespace("microsoft.com");
            Assert.Equal(LogLevel.Critical, logger.LastLogLevel);
            Assert.Null(logger.LastException);
            Assert.Equal("Could not open socket to `microsoft.com`", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            // same as above, but this time with explicit type references rather than extension method syntax so we can vouch for the namespaces being used

            logger.Reset();
            global::Microsoft.Extensions.Logging.Generators.Tests.LoggerExtensions.CouldNotOpenSocket(logger, "microsoft.com");
            Assert.Equal(LogLevel.Critical, logger.LastLogLevel);
            Assert.Null(logger.LastException);
            Assert.Equal("Could not open socket to `microsoft.com`", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            global::LoggerExtensionsNoNamespace.CouldNotOpenSocketNoNamespace(logger, "microsoft.com");
            Assert.Equal(LogLevel.Critical, logger.LastLogLevel);
            Assert.Null(logger.LastException);
            Assert.Equal("Could not open socket to `microsoft.com`", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);
        }

        [Fact]
        public void WrapperTypeTest()
        {
            var logger = new MockLogger();

            logger.Reset();
            var d = global::Microsoft.Extensions.Logging.Generators.Tests.LoggerExtensions.Wrap(logger);    // make sure this is using the right namespace
            d.CouldNotOpenSocket("microsoft.com");
            Assert.Equal(LogLevel.Critical, logger.LastLogLevel);
            Assert.Null(logger.LastException);
            Assert.Equal("Could not open socket to `microsoft.com`", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            var d2 = global::LoggerExtensionsNoNamespace.Wrap(logger);      // make sure this is outside of any namespace
            d2.CouldNotOpenSocketNoNamespace("microsoft.com");
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
            var d = LoggerExtensions.Wrap(logger);
            d.CouldNotOpenSocket("microsoft.com");
            Assert.Equal(0, logger.CallCount);          // ensure the logger doesn't get called when it is disabled

            logger.CouldNotOpenSocket("microsoft.com");
            Assert.Equal(0, logger.CallCount);          // ensure the logger doesn't get called when it is disabled
        }

        [Fact]
        public void ArgTest()
        {
            var logger = new MockLogger();
            var d = ArgTestExtensions.Wrap(logger);

            logger.Reset();
            d.Method1();
            Assert.Null(logger.LastException);
            Assert.Equal("M1", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            d.Method2("arg1");
            Assert.Null(logger.LastException);
            Assert.Equal("M2", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            d.Method3("arg1", 2);
            Assert.Null(logger.LastException);
            Assert.Equal("M3 arg1 2", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            d.Method4(new InvalidOperationException("A"));
//            Assert.Equal("A", logger.LastException!.Message);
            Assert.Equal("M4", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            d.Method5(new InvalidOperationException("A"), new InvalidOperationException("B"));
//            Assert.Equal("A", logger.LastException!.Message);
            Assert.Equal("M5", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            d.Method6(new InvalidOperationException("A"), 2);
//            Assert.Equal("A", logger.LastException!.Message);
            Assert.Equal("M6", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            d.Method7(1, new InvalidOperationException("B"));
//            Assert.Equal("B", logger.LastException!.Message);
            Assert.Equal("M7", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

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
            //            Assert.Equal("A", logger.LastException!.Message);
            Assert.Equal("M4", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            ArgTestExtensions.Method5(logger, new InvalidOperationException("A"), new InvalidOperationException("B"));
            //            Assert.Equal("A", logger.LastException!.Message);
            Assert.Equal("M5", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            ArgTestExtensions.Method6(logger, new InvalidOperationException("A"), 2);
            //            Assert.Equal("A", logger.LastException!.Message);
            Assert.Equal("M6", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);

            logger.Reset();
            ArgTestExtensions.Method7(logger, 1, new InvalidOperationException("B"));
            //            Assert.Equal("B", logger.LastException!.Message);
            Assert.Equal("M7", logger.LastFormattedString);
            Assert.Equal(1, logger.CallCount);
        }

        [Fact]
        public void ReadOnlyListTest()
        {
            var logger = new MockLogger();
            var d = ArgTestExtensions.Wrap(logger);

            logger.Reset();
            d.Method1();
            var rol = logger.LastState as IReadOnlyList<KeyValuePair<string, object >>;
            Assert.Equal(0, rol!.Count);
            Assert.Empty(rol);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = rol[0]);

            logger.Reset();
            d.Method2("arg1");
            rol = logger.LastState as IReadOnlyList<KeyValuePair<string, object>>;
            Assert.Equal(1, rol!.Count);
#pragma warning disable CA1829 // Use Length/Count property instead of Count() when available
            Assert.Equal(1, rol.LongCount());
#pragma warning restore CA1829 // Use Length/Count property instead of Count() when available
            Assert.Equal("p1", (string)rol[0].Key);
            Assert.Equal("arg1", (string)rol[0].Value);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = rol[1]);

            logger.Reset();
            d.Method3("arg1", 2);
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
