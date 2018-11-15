// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Test.Console;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class ConsoleLoggerTest
    {
        private const int WritesPerMsg = 2;
        private const string _paddingString = "      ";
        private const string _loggerName = "test";
        private const string _state = "This is a test, and {curly braces} are just fine!";
        private readonly Func<object, Exception, string> _defaultFormatter = (state, exception) => state.ToString();

        private static (ConsoleLogger Logger, ConsoleSink Sink, ConsoleSink ErrorSink) SetUp(ConsoleLoggerOptions options = null)
        {
            // Arrange
            var sink = new ConsoleSink();
            var errorSink = new ConsoleSink();
            var console = new TestConsole(sink);
            var errorConsole = new TestConsole(errorSink);
            var consoleLoggerProcessor = new TestLoggerProcessor();
            consoleLoggerProcessor.Console = console;
            consoleLoggerProcessor.ErrorConsole = errorConsole;

            var logger = new ConsoleLogger(_loggerName, consoleLoggerProcessor);
            logger.ScopeProvider = new LoggerExternalScopeProvider();
            logger.Options = options ?? new ConsoleLoggerOptions();
            return (logger, sink, errorSink);
        }

        [Fact]
        public void LogsWhenMessageIsNotProvided()
        {
            // Arrange
            var t = SetUp();
            var logger = (ILogger)t.Logger;
            var sink = t.Sink;
            var exception = new InvalidOperationException("Invalid value");

            // Act
            logger.LogCritical(eventId: 0, exception: null, message: null);
            logger.LogCritical(eventId: 0, message: null);
            logger.LogCritical(eventId: 0, message: null, exception: exception);

            // Assert
            Assert.Equal(6, sink.Writes.Count);
            Assert.Equal(
                "crit: test[0]" + Environment.NewLine +
                "      [null]" + Environment.NewLine,
                GetMessage(sink.Writes.GetRange(0 * WritesPerMsg, WritesPerMsg)));
            Assert.Equal(
                "crit: test[0]" + Environment.NewLine +
                "      [null]" + Environment.NewLine,
                GetMessage(sink.Writes.GetRange(1 * WritesPerMsg, WritesPerMsg)));

            Assert.Equal(
                "crit: test[0]" + Environment.NewLine +
                "      [null]" + Environment.NewLine +
                "System.InvalidOperationException: Invalid value" + Environment.NewLine,
                GetMessage(sink.Writes.GetRange(2 * WritesPerMsg, WritesPerMsg)));
        }

        [Fact]
        public void DoesNotLog_NewLine_WhenNoExceptionIsProvided()
        {
            // Arrange
            var t = SetUp();
            var logger = (ILogger)t.Logger;
            var sink = t.Sink;
            var logMessage = "Route with name 'Default' was not found.";
            var expected1 = @"crit: test[0]" + Environment.NewLine +
                            "      Route with name 'Default' was not found." + Environment.NewLine;

            var expected2 = @"crit: test[10]" + Environment.NewLine +
                            "      Route with name 'Default' was not found." + Environment.NewLine;

            // Act
            logger.LogCritical(logMessage);
            logger.LogCritical(eventId: 10, message: logMessage, exception: null);
            logger.LogCritical(eventId: 10, message: logMessage);
            logger.LogCritical(eventId: 10, message: logMessage, exception: null);

            // Assert
            Assert.Equal(8, sink.Writes.Count);
            Assert.Equal(expected1, GetMessage(sink.Writes.GetRange(0 * WritesPerMsg, WritesPerMsg)));
            Assert.Equal(expected2, GetMessage(sink.Writes.GetRange(1 * WritesPerMsg, WritesPerMsg)));
            Assert.Equal(expected2, GetMessage(sink.Writes.GetRange(2 * WritesPerMsg, WritesPerMsg)));
            Assert.Equal(expected2, GetMessage(sink.Writes.GetRange(3 * WritesPerMsg, WritesPerMsg)));
        }

        [Theory]
        [InlineData("Route with name 'Default' was not found.")]
        public void Writes_NewLine_WhenExceptionIsProvided(string message)
        {
            // Arrange
            var t = SetUp();
            var logger = (ILogger)t.Logger;
            var sink = t.Sink;
            var eventId = 10;
            var exception = new InvalidOperationException("Invalid value");
            var expectedHeader = CreateHeader(eventId);
            var expectedMessage =
                _paddingString + message + Environment.NewLine;
            var expectedExceptionMessage =
                exception.ToString() + Environment.NewLine;

            // Act
            logger.LogCritical(eventId, exception, message);

            // Assert
            Assert.Equal(2, sink.Writes.Count);
            Assert.Equal(expectedHeader + expectedMessage + expectedExceptionMessage, sink.Writes[1].Message);
        }

        [Fact]
        public void ThrowsException_WhenNoFormatterIsProvided()
        {
            // Arrange
            var t = SetUp();
            var logger = (ILogger)t.Logger;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => logger.Log<object>(LogLevel.Trace, 1, "empty", new Exception(), null));
        }

        [Fact]
        public void LogsWhenNullFilterGiven()
        {
            // Arrange
            var t = SetUp();
            var logger = t.Logger;
            var sink = t.Sink;
            var expectedHeader = CreateHeader();
            var expectedMessage =
                    _paddingString
                    + _state
                    + Environment.NewLine;

            // Act
            logger.Log(LogLevel.Information, 0, _state, null, _defaultFormatter);

            // Assert
            Assert.Equal(2, sink.Writes.Count);
            Assert.Equal(expectedHeader + expectedMessage, sink.Writes[1].Message);
        }

        [Fact]
        public void WriteCritical_LogsCorrectColors()
        {
            // Arrange
            var t = SetUp();
            var logger = t.Logger;
            var sink = t.Sink;

            // Act
            logger.Log(LogLevel.Critical, 0, _state, null, _defaultFormatter);

            // Assert
            Assert.Equal(2, sink.Writes.Count);
            var write = sink.Writes[0];
            Assert.Equal(ConsoleColor.Red, write.BackgroundColor);
            Assert.Equal(ConsoleColor.White, write.ForegroundColor);
            write = sink.Writes[1];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(TestConsole.DefaultForegroundColor, write.ForegroundColor);
        }

        [Fact]
        public void WriteError_LogsCorrectColors()
        {
            // Arrange
            var t = SetUp();
            var logger = t.Logger;
            var sink = t.Sink;

            // Act
            logger.Log(LogLevel.Error, 0, _state, null, _defaultFormatter);

            // Assert
            Assert.Equal(2, sink.Writes.Count);
            var write = sink.Writes[0];
            Assert.Equal(ConsoleColor.Red, write.BackgroundColor);
            Assert.Equal(ConsoleColor.Black, write.ForegroundColor);
            write = sink.Writes[1];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(TestConsole.DefaultForegroundColor, write.ForegroundColor);
        }

        [Fact]
        public void WriteWarning_LogsCorrectColors()
        {
            // Arrange
            var t = SetUp();
            var logger = t.Logger;
            var sink = t.Sink;

            // Act
            logger.Log(LogLevel.Warning, 0, _state, null, _defaultFormatter);

            // Assert
            Assert.Equal(2, sink.Writes.Count);
            var write = sink.Writes[0];
            Assert.Equal(ConsoleColor.Black, write.BackgroundColor);
            Assert.Equal(ConsoleColor.Yellow, write.ForegroundColor);
            write = sink.Writes[1];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(TestConsole.DefaultForegroundColor, write.ForegroundColor);
        }

        [Fact]
        public void WriteInformation_LogsCorrectColors()
        {
            // Arrange
            var t = SetUp();
            var logger = t.Logger;
            var sink = t.Sink;

            // Act
            logger.Log(LogLevel.Information, 0, _state, null, _defaultFormatter);

            // Assert
            Assert.Equal(2, sink.Writes.Count);
            var write = sink.Writes[0];
            Assert.Equal(ConsoleColor.Black, write.BackgroundColor);
            Assert.Equal(ConsoleColor.DarkGreen, write.ForegroundColor);
            write = sink.Writes[1];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(TestConsole.DefaultForegroundColor, write.ForegroundColor);
        }

        [Fact]
        public void WriteDebug_LogsCorrectColors()
        {
            // Arrange
            var t = SetUp();
            var logger = t.Logger;
            var sink = t.Sink;

            // Act
            logger.Log(LogLevel.Debug, 0, _state, null, _defaultFormatter);

            // Assert
            Assert.Equal(2, sink.Writes.Count);
            var write = sink.Writes[0];
            Assert.Equal(ConsoleColor.Black, write.BackgroundColor);
            Assert.Equal(ConsoleColor.Gray, write.ForegroundColor);
            write = sink.Writes[1];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(TestConsole.DefaultForegroundColor, write.ForegroundColor);
        }

        [Fact]
        public void WriteTrace_LogsCorrectColors()
        {
            // Arrange
            var t = SetUp();
            var logger = t.Logger;
            var sink = t.Sink;

            // Act
            logger.Log(LogLevel.Trace, 0, _state, null, _defaultFormatter);

            // Assert
            Assert.Equal(2, sink.Writes.Count);
            var write = sink.Writes[0];
            Assert.Equal(ConsoleColor.Black, write.BackgroundColor);
            Assert.Equal(ConsoleColor.Gray, write.ForegroundColor);
            write = sink.Writes[1];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(TestConsole.DefaultForegroundColor, write.ForegroundColor);
        }

        [Fact]
        public void WriteAllLevelsDisabledColors_LogsNoColors()
        {
            // Arrange
            var t = SetUp(new ConsoleLoggerOptions { DisableColors = true});
            var logger = t.Logger;
            var sink = t.Sink;

            int levelSequence;
            // Act
            for (levelSequence = (int) LogLevel.Trace; levelSequence < (int) LogLevel.None; levelSequence++)
            {
                logger.Log((LogLevel)levelSequence, 0, _state, null, _defaultFormatter);
            }

            // Assert
            Assert.Equal(2 * levelSequence, sink.Writes.Count);
            foreach (ConsoleContext write in sink.Writes)
            {
                Assert.Null(write.ForegroundColor);
                Assert.Null(write.BackgroundColor);
            }
        }

        [Theory]
        [MemberData(nameof(LevelsWithPrefixes))]
        public void WriteCore_LogsCorrectTimestamp(LogLevel level, string prefix)
        {
            // Arrange
            var t = SetUp(new ConsoleLoggerOptions { TimestampFormat = "yyyyMMddHHmmss "});
            var logger = t.Logger;
            var sink = t.Sink;
            var ex = new Exception("Exception message" + Environment.NewLine + "with a second line");

            // Act
            logger.Log(level, 0, _state, ex, _defaultFormatter);

            // Assert
            Assert.Equal(3, sink.Writes.Count);
            Assert.Matches("^\\d{14}\\s$", sink.Writes[0].Message);
            Assert.StartsWith(prefix, sink.Writes[1].Message);
        }


        [Theory]
        [MemberData(nameof(LevelsWithPrefixes))]
        public void WriteCore_LogsCorrectMessages(LogLevel level, string prefix)
        {
            // Arrange
            var t = SetUp();
            var logger = t.Logger;
            var sink = t.Sink;
            var ex = new Exception("Exception message" + Environment.NewLine + "with a second line");

            // Act
            logger.Log(level, 0, _state, ex, _defaultFormatter);

            // Assert
            Assert.Equal(2, sink.Writes.Count);
            Assert.Equal(
                prefix + ": test[0]" + Environment.NewLine +
                "      This is a test, and {curly braces} are just fine!" + Environment.NewLine +
                "System.Exception: Exception message" + Environment.NewLine +
                "with a second line" + Environment.NewLine,
                GetMessage(sink.Writes));
        }

        [Fact]
        public void NoLogScope_DoesNotWriteAnyScopeContentToOutput()
        {
            // Arrange
            var t = SetUp(new ConsoleLoggerOptions { IncludeScopes = true });
            var logger = t.Logger;
            var sink = t.Sink;

            // Act
            logger.Log(LogLevel.Warning, 0, _state, null, _defaultFormatter);

            // Assert
            Assert.Equal(2, sink.Writes.Count);
            var write = sink.Writes[0];
            Assert.Equal(ConsoleColor.Black, write.BackgroundColor);
            Assert.Equal(ConsoleColor.Yellow, write.ForegroundColor);
            write = sink.Writes[1];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(TestConsole.DefaultForegroundColor, write.ForegroundColor);
        }

        [Fact]
        public void WritingScopes_LogsWithCorrectColors()
        {
            // Arrange
            var t = SetUp(new ConsoleLoggerOptions { IncludeScopes = true });
            var logger = t.Logger;
            var sink = t.Sink;
            var id = Guid.NewGuid();
            var scopeMessage = "RequestId: {RequestId}";

            // Act
            using (logger.BeginScope(scopeMessage, id))
            {
                logger.Log(LogLevel.Information, 0, _state, null, _defaultFormatter);
            }

            // Assert
            Assert.Equal(2, sink.Writes.Count);
            var write = sink.Writes[0];
            Assert.Equal(ConsoleColor.Black, write.BackgroundColor);
            Assert.Equal(ConsoleColor.DarkGreen, write.ForegroundColor);
            write = sink.Writes[1];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(TestConsole.DefaultForegroundColor, write.ForegroundColor);
        }

        [Fact]
        public void WritingScopes_LogsExpectedMessage()
        {
            // Arrange
            var t = SetUp(new ConsoleLoggerOptions { IncludeScopes = true });
            var logger = t.Logger;
            var sink = t.Sink;
            var expectedHeader = CreateHeader();
            var expectedScope =
                _paddingString
                + "=> RequestId: 100"
                + Environment.NewLine;
            var expectedMessage = _paddingString + _state + Environment.NewLine;

            // Act
            using (logger.BeginScope("RequestId: {RequestId}", 100))
            {
                logger.Log(LogLevel.Information, 0, _state, null, _defaultFormatter);
            }

            // Assert
            Assert.Equal(2, sink.Writes.Count);
            // scope
            var write = sink.Writes[1];
            Assert.Equal(expectedHeader + expectedScope + expectedMessage, write.Message);
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(TestConsole.DefaultForegroundColor, write.ForegroundColor);
        }

        [Fact]
        public void WritingNestedScope_LogsNullScopeName()
        {
            // Arrange
            var t = SetUp(new ConsoleLoggerOptions { IncludeScopes = true });
            var logger = t.Logger;
            var sink = t.Sink;
            var expectedHeader = CreateHeader();
            var expectedScope =
                _paddingString
                + "=> [null] => Request matched action: (null)"
                + Environment.NewLine;
            var expectedMessage = _paddingString + _state + Environment.NewLine;

            // Act
            using (logger.BeginScope(null))
            {
                using (logger.BeginScope("Request matched action: {ActionName}", new object[] { null }))
                {
                    logger.Log(LogLevel.Information, 0, _state, null, _defaultFormatter);
                }
            }

            // Assert
            Assert.Equal(2, sink.Writes.Count);
            // scope
            var write = sink.Writes[1];
            Assert.Equal(expectedHeader + expectedScope + expectedMessage, write.Message);
        }

        [Fact]
        public void WritingNestedScopes_LogsExpectedMessage()
        {
            // Arrange
            var t = SetUp(new ConsoleLoggerOptions { IncludeScopes = true });
            var logger = t.Logger;
            var sink = t.Sink;
            var expectedHeader = CreateHeader();
            var expectedScope =
                _paddingString
                + "=> RequestId: 100 => Request matched action: Index"
                + Environment.NewLine;
            var expectedMessage = _paddingString + _state + Environment.NewLine;

            // Act
            using (logger.BeginScope("RequestId: {RequestId}", 100))
            {
                using (logger.BeginScope("Request matched action: {ActionName}", "Index"))
                {
                    logger.Log(LogLevel.Information, 0, _state, null, _defaultFormatter);
                }
            }

            // Assert
            Assert.Equal(2, sink.Writes.Count);
            // scope
            var write = sink.Writes[1];
            Assert.Equal(expectedHeader + expectedScope + expectedMessage, write.Message);
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(TestConsole.DefaultForegroundColor, write.ForegroundColor);
        }

        [Fact]
        public void WritingMultipleScopes_LogsExpectedMessage()
        {
            // Arrange
            var t = SetUp(new ConsoleLoggerOptions { IncludeScopes = true });
            var logger = t.Logger;
            var sink = t.Sink;
            var expectedHeader = CreateHeader();
            var expectedMessage = _paddingString + _state + Environment.NewLine;
            var expectedScope1 =
                _paddingString
                + "=> RequestId: 100 => Request matched action: Index"
                + Environment.NewLine;
            var expectedScope2 =
                _paddingString
                + "=> RequestId: 100 => Created product: Car"
                + Environment.NewLine;

            // Act
            using (logger.BeginScope("RequestId: {RequestId}", 100))
            {
                using (logger.BeginScope("Request matched action: {ActionName}", "Index"))
                {
                    logger.Log(LogLevel.Information, 0, _state, null, _defaultFormatter);
                }

                using (logger.BeginScope("Created product: {ProductName}", "Car"))
                {
                    logger.Log(LogLevel.Information, 0, _state, null, _defaultFormatter);
                }
            }

            // Assert
            Assert.Equal(4, sink.Writes.Count);
            // scope
            var write = sink.Writes[1];
            Assert.Equal(expectedHeader + expectedScope1 + expectedMessage, write.Message);
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(TestConsole.DefaultForegroundColor, write.ForegroundColor);
            write = sink.Writes[3];
            Assert.Equal(expectedHeader + expectedScope2 + expectedMessage, write.Message);
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(TestConsole.DefaultForegroundColor, write.ForegroundColor);
        }

        [Fact]
        public void CallingBeginScopeOnLogger_AlwaysReturnsNewDisposableInstance()
        {
            // Arrange
            var t = SetUp(new ConsoleLoggerOptions { IncludeScopes = true });
            var logger = t.Logger;
            var sink = t.Sink;

            // Act
            var disposable1 = logger.BeginScope("Scope1");
            var disposable2 = logger.BeginScope("Scope2");

            // Assert
            Assert.NotNull(disposable1);
            Assert.NotNull(disposable2);
            Assert.NotSame(disposable1, disposable2);
        }

        [Fact]
        public void CallingBeginScopeOnLogger_ReturnsNonNullableInstance()
        {
            // Arrange
            var t = SetUp();
            var logger = t.Logger;
            var sink = t.Sink;

            // Act
            var disposable = logger.BeginScope("Scope1");

            // Assert
            Assert.NotNull(disposable);
        }

        [Fact]
        public void ConsoleLoggerLogsToError_WhenOverErrorLevel()
        {
            // Arrange
            var (logger, sink, errorSink) = SetUp(new ConsoleLoggerOptions { LogToStandardErrorThreshold = LogLevel.Warning });

            // Act
            logger.LogInformation("Info");
            logger.LogWarning("Warn");

            // Assert
            Assert.Equal(2, sink.Writes.Count);
            Assert.Equal(
                "info: test[0]" + Environment.NewLine +
                "      Info" + Environment.NewLine,
                GetMessage(sink.Writes));

            Assert.Equal(2, errorSink.Writes.Count);
            Assert.Equal(
                "warn: test[0]" + Environment.NewLine +
                "      Warn" + Environment.NewLine,
                GetMessage(errorSink.Writes));
        }

        [Theory]
        [MemberData(nameof(LevelsWithPrefixes))]
        public void WriteCore_NullMessageWithException(LogLevel level, string prefix)
        {
            // Arrange
            var t = SetUp();
            var logger = t.Logger;
            var sink = t.Sink;

            var ex = new Exception("Exception message" + Environment.NewLine + "with a second line");
            string message = null;

            // Act
            logger.Log(level, 0, message, ex, (s, e) => s);

            // Assert
            Assert.Equal(2, sink.Writes.Count);
            Assert.Equal(
                prefix + ": test[0]" + Environment.NewLine +
                "System.Exception: Exception message" + Environment.NewLine +
                "with a second line" + Environment.NewLine,
                GetMessage(sink.Writes));
        }

        [Theory]
        [MemberData(nameof(LevelsWithPrefixes))]
        public void WriteCore_EmptyMessageWithException(LogLevel level, string prefix)
        {
            // Arrange
            var t = SetUp();
            var logger = t.Logger;
            var sink = t.Sink;
            var ex = new Exception("Exception message" + Environment.NewLine + "with a second line");
            string message = string.Empty;

            // Act
            logger.Log(level, 0, message, ex, (s, e) => s);

            // Assert
            Assert.Equal(2, sink.Writes.Count);
            Assert.Equal(
                prefix + ": test[0]" + Environment.NewLine +
                "System.Exception: Exception message" + Environment.NewLine +
                "with a second line" + Environment.NewLine,
                GetMessage(sink.Writes));
        }

        [Theory]
        [MemberData(nameof(LevelsWithPrefixes))]
        public void WriteCore_MessageWithNullException(LogLevel level, string prefix)
        {
            // Arrange
            var t = SetUp();
            var logger = t.Logger;
            var sink = t.Sink;
            Exception ex = null;

            // Act
            logger.Log(level, 0, _state, ex, (s, e) => s);

            // Assert

            Assert.Equal(2, sink.Writes.Count);
            Assert.Equal(
                prefix + ": test[0]" + Environment.NewLine +
                "      This is a test, and {curly braces} are just fine!" + Environment.NewLine,
                GetMessage(sink.Writes));
        }

        [Theory]
        [MemberData(nameof(Levels))]
        public void WriteCore_NullMessageWithNullException(LogLevel level)
        {
            // Arrange
            var t = SetUp();
            var logger = t.Logger;
            var sink = t.Sink;
            Exception ex = null;
            string message = null;

            // Act
            logger.Log(level, 0, message, ex, (s, e) => s);

            // Assert
            Assert.Empty(sink.Writes);
        }

        [Fact]
        public void LogAfterDisposeWritesLog()
        {
            // Arrange
            var sink = new ConsoleSink();
            var console = new TestConsole(sink);
            var processor = new ConsoleLoggerProcessor();
            processor.Console = console;

            var logger = new ConsoleLogger(_loggerName, loggerProcessor: processor);
            logger.Options = new ConsoleLoggerOptions();
            // Act
            processor.Dispose();
            logger.LogInformation("Logging after dispose");

            // Assert
            Assert.True(sink.Writes.Count == 2);
        }

        [Fact]
        public static void IsEnabledReturnsCorrectValue()
        {
            var (logger, _, _) = SetUp();

            Assert.False(logger.IsEnabled(LogLevel.None));
            Assert.True(logger.IsEnabled(LogLevel.Critical));
            Assert.True(logger.IsEnabled(LogLevel.Error));
            Assert.True(logger.IsEnabled(LogLevel.Warning));
            Assert.True(logger.IsEnabled(LogLevel.Information));
            Assert.True(logger.IsEnabled(LogLevel.Debug));
            Assert.True(logger.IsEnabled(LogLevel.Trace));
        }

        [Fact]
        public void ConsoleLoggerOptions_DisableColors_IsAppliedToLoggers()
        {
            // Arrange
            var monitor = new TestOptionsMonitor(new ConsoleLoggerOptions() { DisableColors = true });
            var loggerProvider = new ConsoleLoggerProvider(monitor);
            var logger = (ConsoleLogger)loggerProvider.CreateLogger("Name");

            // Act & Assert
            Assert.True(logger.Options.DisableColors);
            monitor.Set(new ConsoleLoggerOptions() { DisableColors = false });
            Assert.False(logger.Options.DisableColors);
        }

        [Fact]
        public void ConsoleLoggerOptions_DisableColors_IsReadFromLoggingConfiguration()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new[] { new KeyValuePair<string, string>("Console:DisableColors", "true") }).Build();

            var loggerProvider = new ServiceCollection()
                .AddLogging(builder => builder
                    .AddConfiguration(configuration)
                    .AddConsole())
                .BuildServiceProvider()
                .GetRequiredService<ILoggerProvider>();

            var consoleLoggerProvider = Assert.IsType<ConsoleLoggerProvider>(loggerProvider);
            var logger = (ConsoleLogger)consoleLoggerProvider.CreateLogger("Category");
            Assert.True(logger.Options.DisableColors);
        }

        [Fact]
        public void ConsoleLoggerOptions_DisableColors_IsReloaded()
        {
            // Arrange
            var monitor = new TestOptionsMonitor(new ConsoleLoggerOptions());
            var loggerProvider = new ConsoleLoggerProvider(monitor);
            var logger = (ConsoleLogger)loggerProvider.CreateLogger("Name");

            // Act & Assert
            Assert.Null(logger.Options.TimestampFormat);
            monitor.Set(new ConsoleLoggerOptions() { TimestampFormat = "yyyyMMddHHmmss"});
            Assert.Equal("yyyyMMddHHmmss", logger.Options.TimestampFormat);
        }

        [Fact]
        public void ConsoleLoggerOptions_TimeStampFormat_IsReadFromLoggingConfiguration()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new[] { new KeyValuePair<string, string>("Console:TimeStampFormat", "yyyyMMddHHmmss") }).Build();

            var loggerProvider = new ServiceCollection()
                .AddLogging(builder => builder
                    .AddConfiguration(configuration)
                    .AddConsole())
                .BuildServiceProvider()
                .GetRequiredService<ILoggerProvider>();

            var consoleLoggerProvider = Assert.IsType<ConsoleLoggerProvider>(loggerProvider);
            var logger = (ConsoleLogger)consoleLoggerProvider.CreateLogger("Category");
            Assert.Equal("yyyyMMddHHmmss", logger.Options.TimestampFormat);
        }

        [Fact]
        public void ConsoleLoggerOptions_IncludeScopes_IsAppliedToLoggers()
        {
            // Arrange
            var monitor = new TestOptionsMonitor(new ConsoleLoggerOptions() { IncludeScopes = true });
            var loggerProvider = new ConsoleLoggerProvider(monitor);
            var logger = (ConsoleLogger)loggerProvider.CreateLogger("Name");

            // Act & Assert
            Assert.True(logger.Options.IncludeScopes);
            monitor.Set(new ConsoleLoggerOptions() { IncludeScopes = false });
            Assert.False(logger.Options.IncludeScopes);
        }

        [Fact]
        public void ConsoleLoggerOptions_LogAsErrorLevel_IsReadFromLoggingConfiguration()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new[] { new KeyValuePair<string, string>("Console:LogToStandardErrorThreshold", "Warning") }).Build();

            var loggerProvider = new ServiceCollection()
                .AddLogging(builder => builder
                    .AddConfiguration(configuration)
                    .AddConsole())
                .BuildServiceProvider()
                .GetRequiredService<ILoggerProvider>();

            var consoleLoggerProvider = Assert.IsType<ConsoleLoggerProvider>(loggerProvider);
            var logger = (ConsoleLogger)consoleLoggerProvider.CreateLogger("Category");
            Assert.Equal(LogLevel.Warning, logger.Options.LogToStandardErrorThreshold);
        }

        [Fact]
        public void ConsoleLoggerOptions_LogAsErrorLevel_IsAppliedToLoggers()
        {
            // Arrange
            var monitor = new TestOptionsMonitor(new ConsoleLoggerOptions());
            var loggerProvider = new ConsoleLoggerProvider(monitor);
            var logger = (ConsoleLogger)loggerProvider.CreateLogger("Name");

            // Act & Assert
            Assert.Equal(LogLevel.None, logger.Options.LogToStandardErrorThreshold);
            monitor.Set(new ConsoleLoggerOptions() { LogToStandardErrorThreshold = LogLevel.Error});
            Assert.Equal(LogLevel.Error, logger.Options.LogToStandardErrorThreshold);
        }

        [Fact]
        public void ConsoleLoggerOptions_IncludeScopes_IsReadFromLoggingConfiguration()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new[] { new KeyValuePair<string, string>("Console:IncludeScopes", "true") }).Build();

            var loggerProvider = new ServiceCollection()
                .AddLogging(builder => builder
                    .AddConfiguration(configuration)
                    .AddConsole())
                .BuildServiceProvider()
                .GetRequiredService<ILoggerProvider>();

            var consoleLoggerProvider = Assert.IsType<ConsoleLoggerProvider>(loggerProvider);
            var logger = (ConsoleLogger)consoleLoggerProvider.CreateLogger("Category");
            Assert.NotNull(logger.ScopeProvider);
        }

        public static TheoryData<LogLevel, string> LevelsWithPrefixes => new TheoryData<LogLevel, string>()
        {
            {LogLevel.Critical, "crit"},
            {LogLevel.Error, "fail"},
            {LogLevel.Warning, "warn"},
            {LogLevel.Information, "info"},
            {LogLevel.Debug, "dbug"},
            {LogLevel.Trace, "trce"},
        };

        public static TheoryData<LogLevel> Levels
        {
            get
            {
                var data = new TheoryData<LogLevel>();
                foreach (LogLevel value in Enum.GetValues(typeof(LogLevel)))
                {
                    data.Add(value);
                }
                return data;
            }
        }

        private string GetMessage(List<ConsoleContext> contexts)
        {
            return string.Join("", contexts.Select(c => c.Message));
        }

        private string CreateHeader(int eventId = 0)
        {
            return $": {_loggerName}[{eventId}]{Environment.NewLine}";
        }


        private class TestLoggerProcessor : ConsoleLoggerProcessor
        {
            public TestLoggerProcessor()
            {
            }

            public override void EnqueueMessage(LogMessageEntry message)
            {
                WriteMessage(message);
            }
        }
    }

    public class TestOptionsMonitor : IOptionsMonitor<ConsoleLoggerOptions>
    {
        private ConsoleLoggerOptions _options;
        private event Action<ConsoleLoggerOptions, string> _onChange;

        public TestOptionsMonitor(ConsoleLoggerOptions options)
        {
            _options = options;
        }

        public ConsoleLoggerOptions Get(string name) => _options;

        public IDisposable OnChange(Action<ConsoleLoggerOptions, string> listener)
        {
            _onChange += listener;
            return null;
        }

        public ConsoleLoggerOptions CurrentValue => _options;

        public void Set(ConsoleLoggerOptions options)
        {
            _options = options;
            _onChange?.Invoke(options, "");
        }
    }
}
