// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Test.Console;
using Microsoft.Extensions.Options;
using Xunit;
#pragma warning disable CS0618

namespace Microsoft.Extensions.Logging.Console.Test
{
    public class ConsoleLoggerTest
    {
        private const string _paddingString = "      ";
        private const string _loggerName = "test";
        private const string _state = "This is a test, and {curly braces} are just fine!";
        private readonly Func<object, Exception, string> _defaultFormatter = (state, exception) => state.ToString();

        internal static IEnumerable<ConsoleFormatter> GetFormatters(
            SimpleConsoleFormatterOptions simpleOptions = null,
            ConsoleFormatterOptions systemdOptions = null,
            JsonConsoleFormatterOptions jsonOptions = null)
        {
            var defaultMonitor = new TestFormatterOptionsMonitor<SimpleConsoleFormatterOptions>(simpleOptions ?? new SimpleConsoleFormatterOptions());
            var systemdMonitor = new TestFormatterOptionsMonitor<ConsoleFormatterOptions>(systemdOptions ?? new ConsoleFormatterOptions());
            var jsonMonitor = new TestFormatterOptionsMonitor<JsonConsoleFormatterOptions>(jsonOptions ?? new JsonConsoleFormatterOptions());
            var formatters = new List<ConsoleFormatter>() { 
                new SimpleConsoleFormatter(defaultMonitor),
                new SystemdConsoleFormatter(systemdMonitor),
                new JsonConsoleFormatter(jsonMonitor)
            };
            return formatters;
        }

        private static (ConsoleLogger Logger, ConsoleSink Sink, ConsoleSink ErrorSink, Func<LogLevel, string> GetLevelPrefix, int WritesPerMsg) SetUp(ConsoleLoggerOptions options = null)
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
            var formatters = new ConcurrentDictionary<string, ConsoleFormatter>(GetFormatters().ToDictionary(f => f.Name));

            Func<LogLevel, string> levelAsString;
            int writesPerMsg;
            switch (logger.Options.Format)
            {
                case ConsoleLoggerFormat.Default:
                    levelAsString = LogLevelAsStringDefault;
                    writesPerMsg = 2;
                    logger.Formatter = formatters[ConsoleFormatterNames.Simple];
                    break;
                case ConsoleLoggerFormat.Systemd:
                    levelAsString = GetSyslogSeverityString;
                    writesPerMsg = 1;
                    logger.Formatter = formatters[ConsoleFormatterNames.Systemd];
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logger.Options.Format));
            }

            UpdateFormatterOptions(logger.Formatter, logger.Options);
            VerifyDeprecatedPropertiesUsedOnNullFormatterName(logger);
            return (logger, sink, errorSink, levelAsString, writesPerMsg);
        }

        private static void VerifyDeprecatedPropertiesUsedOnNullFormatterName(ConsoleLogger logger)
        {
            Assert.Null(logger.Options.FormatterName);
            if (ConsoleLoggerFormat.Default == logger.Options.Format)
            {
                var formatter = Assert.IsType<SimpleConsoleFormatter>(logger.Formatter);
                Assert.Equal(formatter.FormatterOptions.IncludeScopes, logger.Options.IncludeScopes);
                Assert.Equal(formatter.FormatterOptions.UseUtcTimestamp, logger.Options.UseUtcTimestamp);
                Assert.Equal(formatter.FormatterOptions.TimestampFormat, logger.Options.TimestampFormat);
                Assert.Equal(formatter.FormatterOptions.ColorBehavior, 
                    logger.Options.DisableColors ? LoggerColorBehavior.Disabled : LoggerColorBehavior.Enabled);   
            }
            else
            {
                var formatter = Assert.IsType<SystemdConsoleFormatter>(logger.Formatter);
                Assert.Equal(formatter.FormatterOptions.IncludeScopes, logger.Options.IncludeScopes);   
                Assert.Equal(formatter.FormatterOptions.UseUtcTimestamp, logger.Options.UseUtcTimestamp);
                Assert.Equal(formatter.FormatterOptions.TimestampFormat, logger.Options.TimestampFormat);
            }
        }

        private static void UpdateFormatterOptions(ConsoleFormatter formatter, ConsoleLoggerOptions deprecatedFromOptions)
        {
            // kept for deprecated apis:
            if (formatter is SimpleConsoleFormatter defaultFormatter)
            {
                defaultFormatter.FormatterOptions.ColorBehavior = deprecatedFromOptions.DisableColors ? 
                    LoggerColorBehavior.Disabled : LoggerColorBehavior.Enabled;
                defaultFormatter.FormatterOptions.IncludeScopes = deprecatedFromOptions.IncludeScopes;
                defaultFormatter.FormatterOptions.TimestampFormat = deprecatedFromOptions.TimestampFormat;
                defaultFormatter.FormatterOptions.UseUtcTimestamp = deprecatedFromOptions.UseUtcTimestamp;
            }
            else
            if (formatter is SystemdConsoleFormatter systemdFormatter)
            {
                systemdFormatter.FormatterOptions.IncludeScopes = deprecatedFromOptions.IncludeScopes;
                systemdFormatter.FormatterOptions.TimestampFormat = deprecatedFromOptions.TimestampFormat;
                systemdFormatter.FormatterOptions.UseUtcTimestamp = deprecatedFromOptions.UseUtcTimestamp;
            }
        }

        internal static string LogLevelAsStringDefault(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    return "trce";
                case LogLevel.Debug:
                    return "dbug";
                case LogLevel.Information:
                    return "info";
                case LogLevel.Warning:
                    return "warn";
                case LogLevel.Error:
                    return "fail";
                case LogLevel.Critical:
                    return "crit";
                default:
                    throw new ArgumentOutOfRangeException(nameof(level));
            }
        }

        internal static string GetSyslogSeverityString(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    return "<7>";
                case LogLevel.Information:
                    return "<6>";
                case LogLevel.Warning:
                    return "<4>";
                case LogLevel.Error:
                    return "<3>";
                case LogLevel.Critical:
                    return "<2>";
                default:
                    throw new ArgumentOutOfRangeException(nameof(level));
            }
        }

        internal static string GetJsonLogLevelString(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => "Trace",
                LogLevel.Debug => "Debug",
                LogLevel.Information => "Information",
                LogLevel.Warning => "Warning",
                LogLevel.Error => "Error",
                LogLevel.Critical => "Critical",
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
            };
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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
                _paddingString + "[null]" + Environment.NewLine,
                GetMessage(sink.Writes.GetRange(0 * t.WritesPerMsg, t.WritesPerMsg)));
            Assert.Equal(
                "crit: test[0]" + Environment.NewLine +
                _paddingString + "[null]" + Environment.NewLine,
                GetMessage(sink.Writes.GetRange(1 * t.WritesPerMsg, t.WritesPerMsg)));

            Assert.Equal(
                "crit: test[0]" + Environment.NewLine +
                _paddingString + "[null]" + Environment.NewLine +
                _paddingString + "System.InvalidOperationException: Invalid value" + Environment.NewLine,
                GetMessage(sink.Writes.GetRange(2 * t.WritesPerMsg, t.WritesPerMsg)));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void DoesNotLog_NewLine_WhenNoExceptionIsProvided()
        {
            // Arrange
            var t = SetUp();
            var logger = (ILogger)t.Logger;
            var sink = t.Sink;
            var logMessage = "Route with name 'Simple' was not found.";
            var expected1 = @"crit: test[0]" + Environment.NewLine +
                            _paddingString + "Route with name 'Simple' was not found." + Environment.NewLine;

            var expected2 = @"crit: test[10]" + Environment.NewLine +
                            _paddingString + "Route with name 'Simple' was not found." + Environment.NewLine;

            // Act
            logger.LogCritical(logMessage);
            logger.LogCritical(eventId: 10, message: logMessage, exception: null);
            logger.LogCritical(eventId: 10, message: logMessage);
            logger.LogCritical(eventId: 10, message: logMessage, exception: null);

            // Assert
            Assert.Equal(8, sink.Writes.Count);
            Assert.Equal(expected1, GetMessage(sink.Writes.GetRange(0 * t.WritesPerMsg, t.WritesPerMsg)));
            Assert.Equal(expected2, GetMessage(sink.Writes.GetRange(1 * t.WritesPerMsg, t.WritesPerMsg)));
            Assert.Equal(expected2, GetMessage(sink.Writes.GetRange(2 * t.WritesPerMsg, t.WritesPerMsg)));
            Assert.Equal(expected2, GetMessage(sink.Writes.GetRange(3 * t.WritesPerMsg, t.WritesPerMsg)));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(null, 0)]
        [InlineData(null, 1)]
        [InlineData("missingFormatter", 0)]
        [InlineData("missingFormatter", 1)]
        public void Options_FormatterNameNull_UsesDeprecatedProperties(string formatterName, int formatNumber)
        {
            // Arrange
            ConsoleLoggerFormat format = (ConsoleLoggerFormat)formatNumber;
            var options = new ConsoleLoggerOptions() { FormatterName = formatterName };
            var monitor = new TestOptionsMonitor(options);
            var loggerProvider = new ConsoleLoggerProvider(monitor, GetFormatters());
            var logger = (ConsoleLogger)loggerProvider.CreateLogger("Name");

            // Act
            monitor.Set(new ConsoleLoggerOptions() { FormatterName = null, Format = format, TimestampFormat = "HH:mm:ss " });

            // Assert
            switch (format)
            {
                case ConsoleLoggerFormat.Default:
                {
                    Assert.Null(logger.Options.FormatterName);
                    Assert.Equal(ConsoleFormatterNames.Simple, logger.Formatter.Name);
                    var formatter = Assert.IsType<SimpleConsoleFormatter>(logger.Formatter);
                    Assert.Equal("HH:mm:ss ", formatter.FormatterOptions.TimestampFormat);
                }
                break;
                case ConsoleLoggerFormat.Systemd:
                {
                    Assert.Null(logger.Options.FormatterName);
                    Assert.Equal(ConsoleFormatterNames.Systemd, logger.Formatter.Name);
                    var formatter = Assert.IsType<SystemdConsoleFormatter>(logger.Formatter);
                    Assert.Equal("HH:mm:ss ", formatter.FormatterOptions.TimestampFormat);
                }
                break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }
            loggerProvider?.Dispose();
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData("Route with name 'Simple' was not found.")]
        public void Writes_NewLine_WhenExceptionIsProvided(string message)
        {
            // Arrange
            var t = SetUp();
            var logger = (ILogger)t.Logger;
            var sink = t.Sink;
            var eventId = 10;
            var exception = new InvalidOperationException("Invalid value");
            var expectedHeader = CreateHeader(ConsoleLoggerFormat.Default, eventId) + Environment.NewLine;
            var expectedMessage =
                _paddingString + message + Environment.NewLine;
            var expectedExceptionMessage =
                _paddingString + exception.ToString() + Environment.NewLine;

            // Act
            logger.LogCritical(eventId, exception, message);

            // Assert
            Assert.Equal(2, sink.Writes.Count);
            Assert.Equal(expectedHeader + expectedMessage + expectedExceptionMessage, sink.Writes[1].Message);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void ThrowsException_WhenNoFormatterIsProvided()
        {
            // Arrange
            var t = SetUp();
            var logger = (ILogger)t.Logger;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => logger.Log<object>(LogLevel.Trace, 1, "empty", new Exception(), null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void LogsWhenNullFilterGiven()
        {
            // Arrange
            var t = SetUp();
            var logger = t.Logger;
            var sink = t.Sink;
            var expectedHeader = CreateHeader(ConsoleLoggerFormat.Default) + Environment.NewLine;
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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
            Assert.Equal(ConsoleColor.DarkRed, write.BackgroundColor);
            Assert.Equal(ConsoleColor.White, write.ForegroundColor);
            write = sink.Writes[1];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(TestConsole.DefaultForegroundColor, write.ForegroundColor);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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
            Assert.Equal(ConsoleColor.DarkRed, write.BackgroundColor);
            Assert.Equal(ConsoleColor.Black, write.ForegroundColor);
            write = sink.Writes[1];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(TestConsole.DefaultForegroundColor, write.ForegroundColor);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void WriteAllLevelsDisabledColors_LogsNoColors()
        {
            // Arrange
            var t = SetUp(new ConsoleLoggerOptions { DisableColors = true });
            var logger = t.Logger;
            var sink = t.Sink;

            int levelSequence;
            // Act
            for (levelSequence = (int)LogLevel.Trace; levelSequence < (int)LogLevel.None; levelSequence++)
            {
                logger.Log((LogLevel)levelSequence, 0, _state, null, _defaultFormatter);
            }

            // Assert
            Assert.Equal(levelSequence, sink.Writes.Count);
            foreach (ConsoleContext write in sink.Writes)
            {
                Assert.Null(write.ForegroundColor);
                Assert.Null(write.BackgroundColor);
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(FormatsAndLevels))]
        public void Log_LogsCorrectTimestamp(ConsoleLoggerFormat format, LogLevel level)
        {
            // Arrange
            var t = SetUp(new ConsoleLoggerOptions { TimestampFormat = "yyyy-MM-ddTHH:mm:sszz ", Format = format, UseUtcTimestamp = false });
            var levelPrefix = t.GetLevelPrefix(level);
            var logger = t.Logger;
            var sink = t.Sink;
            var ex = new Exception("Exception message" + Environment.NewLine + "with a second line");

            // Act
            logger.Log(level, 0, _state, ex, _defaultFormatter);

            // Assert
            switch (format)
            {
                case ConsoleLoggerFormat.Default:
                {
                    Assert.Equal(3, sink.Writes.Count);
                    Assert.StartsWith(levelPrefix, sink.Writes[1].Message);
                    Assert.Matches(@"^\d{4}\D\d{2}\D\d{2}\D\d{2}\D\d{2}\D\d{2}\D\d{2}\s$", sink.Writes[0].Message);
                    var parsedDateTime = DateTimeOffset.Parse(sink.Writes[0].Message.Trim());
                    Assert.Equal(DateTimeOffset.Now.Offset, parsedDateTime.Offset);
                }
                break;
                case ConsoleLoggerFormat.Systemd:
                {
                    Assert.Single(sink.Writes);
                    Assert.StartsWith(levelPrefix, sink.Writes[0].Message);
                    var regexMatch = Regex.Match(sink.Writes[0].Message, @"^<\d>(\d{4}\D\d{2}\D\d{2}\D\d{2}\D\d{2}\D\d{2}\D\d{2})\s[^\s]");
                    Assert.True(regexMatch.Success);
                    var parsedDateTime = DateTimeOffset.Parse(regexMatch.Groups[1].Value);
                    Assert.Equal(DateTimeOffset.Now.Offset, parsedDateTime.Offset);
                }
                break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(FormatsAndLevels))]
        public void WriteCore_LogsCorrectTimestampInUtc(ConsoleLoggerFormat format, LogLevel level)
        {
            // Arrange
            var t = SetUp(new ConsoleLoggerOptions { TimestampFormat = "yyyy-MM-ddTHH:mm:sszz ", Format = format, UseUtcTimestamp = true });
            var levelPrefix = t.GetLevelPrefix(level);
            var logger = t.Logger;
            var sink = t.Sink;
            var ex = new Exception("Exception message" + Environment.NewLine + "with a second line");

            // Act
            logger.Log(level, 0, _state, ex, _defaultFormatter);

            // Assert
            switch (format)
            {
                case ConsoleLoggerFormat.Default:
                {
                    Assert.Equal(3, sink.Writes.Count);
                    Assert.StartsWith(levelPrefix, sink.Writes[1].Message);
                    Assert.Matches(@"^\d{4}\D\d{2}\D\d{2}\D\d{2}\D\d{2}\D\d{2}\D\d{2}\s$", sink.Writes[0].Message);
                    var parsedDateTime = DateTimeOffset.Parse(sink.Writes[0].Message.Trim());
                    Assert.Equal(DateTimeOffset.UtcNow.Offset, parsedDateTime.Offset);
                }
                break;
                case ConsoleLoggerFormat.Systemd:
                {
                    Assert.Single(sink.Writes);
                    Assert.StartsWith(levelPrefix, sink.Writes[0].Message);
                    var regexMatch = Regex.Match(sink.Writes[0].Message, @"^<\d>(\d{4}\D\d{2}\D\d{2}\D\d{2}\D\d{2}\D\d{2}\D\d{2})\s[^\s]");
                    Assert.True(regexMatch.Success);
                    var parsedDateTime = DateTimeOffset.Parse(regexMatch.Groups[1].Value);
                    Assert.Equal(DateTimeOffset.UtcNow.Offset, parsedDateTime.Offset);
                }
                break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(FormatsAndLevels))]
        public void WriteCore_LogsCorrectMessages(ConsoleLoggerFormat format, LogLevel level)
        {
            // Arrange
            var t = SetUp(new ConsoleLoggerOptions { Format = format });
            var levelPrefix = t.GetLevelPrefix(level);
            var logger = t.Logger;
            var sink = t.Sink;
            var ex = new Exception("Exception message" + Environment.NewLine + "with a second line");

            // Act
            logger.Log(level, 0, _state, ex, _defaultFormatter);

            // Assert
            switch (format)
            {
                case ConsoleLoggerFormat.Default:
                {
                    Assert.Equal(2, sink.Writes.Count);
                    Assert.Equal(
                        levelPrefix + ": test[0]" + Environment.NewLine +
                        _paddingString + "This is a test, and {curly braces} are just fine!" + Environment.NewLine +
                        _paddingString + "System.Exception: Exception message" + Environment.NewLine +
                        _paddingString + "with a second line" + Environment.NewLine,
                        GetMessage(sink.Writes));
                }
                break;
                case ConsoleLoggerFormat.Systemd:
                {
                    Assert.Single(sink.Writes);
                    Assert.Equal(
                        levelPrefix + "test[0]" + " " +
                        "This is a test, and {curly braces} are just fine!" + " " +
                        "System.Exception: Exception message" + " " +
                        "with a second line" + Environment.NewLine,
                        GetMessage(sink.Writes));
                }
                break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(Formats))]
        public void WritingScopes_LogsExpectedMessage(ConsoleLoggerFormat format)
        {
            // Arrange
            var t = SetUp(new ConsoleLoggerOptions { IncludeScopes = true, Format = format });
            var logger = t.Logger;
            var sink = t.Sink;
            var level = LogLevel.Information;
            var levelPrefix = t.GetLevelPrefix(level);
            var header = CreateHeader(format);
            var scope = "=> RequestId: 100";
            var message = _state;

            // Act
            using (logger.BeginScope("RequestId: {RequestId}", 100))
            {
                logger.Log(level, 0, _state, null, _defaultFormatter);
            }

            // Assert
            switch (format)
            {
                case ConsoleLoggerFormat.Default:
                {
                    // Assert
                    Assert.Equal(2, sink.Writes.Count);
                    // scope
                    var write = sink.Writes[1];
                    Assert.Equal(header + Environment.NewLine
                                 + _paddingString + scope + Environment.NewLine
                                 + _paddingString + message + Environment.NewLine, write.Message);
                    Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
                    Assert.Equal(TestConsole.DefaultForegroundColor, write.ForegroundColor);
                }
                break;
                case ConsoleLoggerFormat.Systemd:
                {
                    Assert.Single(sink.Writes);
                    // scope
                    var write = sink.Writes[0];
                    Assert.Equal(levelPrefix + header + " " + scope + " " + message + Environment.NewLine, write.Message);
                    Assert.Null(write.BackgroundColor);
                    Assert.Null(write.ForegroundColor);
                }
                break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(Formats))]
        public void WritingNestedScope_LogsNullScopeName(ConsoleLoggerFormat format)
        {
            // Arrange
            var t = SetUp(new ConsoleLoggerOptions { IncludeScopes = true, Format = format });
            var logger = t.Logger;
            var sink = t.Sink;
            var level = LogLevel.Information;
            var levelPrefix = t.GetLevelPrefix(level);
            var header = CreateHeader(format);
            var scope = "=> [null] => Request matched action: (null)";
            var message = _state;

            // Act
            using (logger.BeginScope(null))
            {
                using (logger.BeginScope("Request matched action: {ActionName}", new object[] { null }))
                {
                    logger.Log(level, 0, _state, null, _defaultFormatter);
                }
            }

            // Assert
            switch (format)
            {
                case ConsoleLoggerFormat.Default:
                {
                    Assert.Equal(2, sink.Writes.Count);
                    // scope
                    var write = sink.Writes[1];
                    Assert.Equal(header + Environment.NewLine
                                + _paddingString + scope + Environment.NewLine
                                + _paddingString + message + Environment.NewLine, write.Message);
                }
                break;
                case ConsoleLoggerFormat.Systemd:
                {
                    Assert.Single(sink.Writes);
                    // scope
                    var write = sink.Writes[0];
                    Assert.Equal(levelPrefix + header + " " + scope + " " + message + Environment.NewLine, write.Message);
                }
                break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(Formats))]
        public void WritingNestedScopes_LogsExpectedMessage(ConsoleLoggerFormat format)
        {
            // Arrange
            var t = SetUp(new ConsoleLoggerOptions { IncludeScopes = true, Format = format });
            var logger = t.Logger;
            var sink = t.Sink;
            var level = LogLevel.Information;
            var levelPrefix = t.GetLevelPrefix(level);
            var header = CreateHeader(format);
            var scope = "=> RequestId: 100 => Request matched action: Index";
            var message = _state;

            // Act
            using (logger.BeginScope("RequestId: {RequestId}", 100))
            {
                using (logger.BeginScope("Request matched action: {ActionName}", "Index"))
                {
                    logger.Log(level, 0, _state, null, _defaultFormatter);
                }
            }

            switch (format)
            {
                case ConsoleLoggerFormat.Default:
                {
                    // Assert
                    Assert.Equal(2, sink.Writes.Count);
                    var formatter = Assert.IsType<SimpleConsoleFormatter>(logger.Formatter);
                    Assert.True(formatter.FormatterOptions.IncludeScopes);
                    // scope
                    var write = sink.Writes[1];
                    Assert.Equal(header + Environment.NewLine
                                + _paddingString + scope + Environment.NewLine
                                + _paddingString + message + Environment.NewLine, write.Message);
                    Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
                    Assert.Equal(TestConsole.DefaultForegroundColor, write.ForegroundColor);
                }
                break;
                case ConsoleLoggerFormat.Systemd:
                {
                    // Assert
                    Assert.Single(sink.Writes);
                    var formatter = Assert.IsType<SystemdConsoleFormatter>(logger.Formatter);
                    Assert.True(formatter.FormatterOptions.IncludeScopes);
                    // scope
                    var write = sink.Writes[0];
                    Assert.Equal(levelPrefix + header + " " + scope + " " + message + Environment.NewLine, write.Message);
                    Assert.Null(write.BackgroundColor);
                    Assert.Null(write.ForegroundColor);
                }
                break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(Formats))]
        public void WritingMultipleScopes_LogsExpectedMessage(ConsoleLoggerFormat format)
        {
            // Arrange
            var t = SetUp(new ConsoleLoggerOptions { IncludeScopes = true, Format = format });
            var logger = t.Logger;
            var sink = t.Sink;
            var level = LogLevel.Information;
            var levelPrefix = t.GetLevelPrefix(level);
            var header = CreateHeader(format);
            var message = _state;
            var scope1 = "=> RequestId: 100 => Request matched action: Index";
            var scope2 = "=> RequestId: 100 => Created product: Car";

            // Act
            using (logger.BeginScope("RequestId: {RequestId}", 100))
            {
                using (logger.BeginScope("Request matched action: {ActionName}", "Index"))
                {
                    logger.Log(level, 0, _state, null, _defaultFormatter);
                }

                using (logger.BeginScope("Created product: {ProductName}", "Car"))
                {
                    logger.Log(level, 0, _state, null, _defaultFormatter);
                }
            }

            // Assert
            switch (format)
            {
                case ConsoleLoggerFormat.Default:
                {
                    Assert.Equal(4, sink.Writes.Count);
                    // scope
                    var write = sink.Writes[1];
                    Assert.Equal(header + Environment.NewLine
                                + _paddingString + scope1 + Environment.NewLine
                                + _paddingString + message + Environment.NewLine, write.Message);
                    Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
                    Assert.Equal(TestConsole.DefaultForegroundColor, write.ForegroundColor);
                    write = sink.Writes[3];
                    Assert.Equal(header + Environment.NewLine
                                + _paddingString + scope2 + Environment.NewLine
                                + _paddingString + message + Environment.NewLine, write.Message);
                    Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
                    Assert.Equal(TestConsole.DefaultForegroundColor, write.ForegroundColor);
                }
                break;
                case ConsoleLoggerFormat.Systemd:
                {
                    Assert.Equal(2, sink.Writes.Count);
                    // scope
                    var write = sink.Writes[0];
                    Assert.Equal(levelPrefix + header + " " + scope1 + " " + message + Environment.NewLine, write.Message);
                    Assert.Null(write.BackgroundColor);
                    Assert.Null(write.ForegroundColor);
                    write = sink.Writes[1];
                    Assert.Equal(levelPrefix + header + " " + scope2 + " " + message + Environment.NewLine, write.Message);
                    Assert.Null(write.BackgroundColor);
                    Assert.Null(write.ForegroundColor);
                }
                break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(Formats))]
        public void ConsoleLoggerLogsToError_WhenOverErrorLevel(ConsoleLoggerFormat format)
        {
            // Arrange
            var t = SetUp(new ConsoleLoggerOptions { LogToStandardErrorThreshold = LogLevel.Warning, Format = format });
            var logger = t.Logger;
            var sink = t.Sink;
            var errorSink = t.ErrorSink;

            // Act
            logger.LogInformation("Info");
            logger.LogWarning("Warn");

            // Assert
            switch (format)
            {
                case ConsoleLoggerFormat.Default:
                {
                    Assert.Equal(2, sink.Writes.Count);
                    Assert.Equal(
                        "info: test[0]" + Environment.NewLine +
                        _paddingString + "Info" + Environment.NewLine,
                        GetMessage(sink.Writes));

                    Assert.Equal(2, errorSink.Writes.Count);
                    Assert.Equal(
                        "warn: test[0]" + Environment.NewLine +
                        _paddingString + "Warn" + Environment.NewLine,
                        GetMessage(errorSink.Writes));
                }
                break;
                case ConsoleLoggerFormat.Systemd:
                {
                    Assert.Single(sink.Writes);
                    Assert.Equal(
                        "<6>test[0] Info" + Environment.NewLine,
                        GetMessage(sink.Writes));

                    Assert.Single(errorSink.Writes);
                    Assert.Equal(
                        "<4>test[0] Warn" + Environment.NewLine,
                        GetMessage(errorSink.Writes));
                }
                break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(FormatsAndLevels))]
        public void WriteCore_NullMessageWithException(ConsoleLoggerFormat format, LogLevel level)
        {
            // Arrange
            var t = SetUp(new ConsoleLoggerOptions { Format = format });
            var levelPrefix = t.GetLevelPrefix(level);
            var logger = t.Logger;
            var sink = t.Sink;

            var ex = new Exception("Exception message" + Environment.NewLine + "with a second line");
            string message = null;

            // Act
            logger.Log(level, 0, message, ex, (s, e) => s);

            // Assert
            switch (format)
            {
                case ConsoleLoggerFormat.Default:
                {
                    Assert.Equal(2, sink.Writes.Count);
                    Assert.Equal(
                        levelPrefix + ": test[0]" + Environment.NewLine +
                        _paddingString + "System.Exception: Exception message" + Environment.NewLine +
                        _paddingString + "with a second line" + Environment.NewLine,
                        GetMessage(sink.Writes));
                }
                break;
                case ConsoleLoggerFormat.Systemd:
                {
                    Assert.Single(sink.Writes);
                    Assert.Equal(
                        levelPrefix + "test[0]" + " " +
                        "System.Exception: Exception message" + " " +
                        "with a second line" + Environment.NewLine,
                        GetMessage(sink.Writes));
                }
                break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(FormatsAndLevels))]
        public void WriteCore_EmptyMessageWithException(ConsoleLoggerFormat format, LogLevel level)
        {
            // Arrange
            var t = SetUp(new ConsoleLoggerOptions { Format = format });
            var levelPrefix = t.GetLevelPrefix(level);
            var logger = t.Logger;
            var sink = t.Sink;
            var ex = new Exception("Exception message" + Environment.NewLine + "with a second line");
            string message = string.Empty;

            // Act
            logger.Log(level, 0, message, ex, (s, e) => s);

            // Assert
            switch (format)
            {
                case ConsoleLoggerFormat.Default:
                {
                    Assert.Equal(2, sink.Writes.Count);
                    Assert.Equal(
                        levelPrefix + ": test[0]" + Environment.NewLine +
                        _paddingString + "System.Exception: Exception message" + Environment.NewLine +
                        _paddingString + "with a second line" + Environment.NewLine,
                        GetMessage(sink.Writes));
                }
                break;
                case ConsoleLoggerFormat.Systemd:
                {
                    Assert.Single(sink.Writes);
                    Assert.Equal(
                        levelPrefix + "test[0]" + " " +
                        "System.Exception: Exception message" + " " +
                        "with a second line" + Environment.NewLine,
                        GetMessage(sink.Writes));
                }
                break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(FormatsAndLevels))]
        public void WriteCore_MessageWithNullException(ConsoleLoggerFormat format, LogLevel level)
        {
            // Arrange
            var t = SetUp(new ConsoleLoggerOptions { Format = format });
            var levelPrefix = t.GetLevelPrefix(level);
            var logger = t.Logger;
            var sink = t.Sink;
            Exception ex = null;

            // Act
            logger.Log(level, 0, _state, ex, (s, e) => s);

            // Assert
            switch (format)
            {
                case ConsoleLoggerFormat.Default:
                {
                    Assert.Equal(2, sink.Writes.Count);
                    Assert.Equal(
                        levelPrefix + ": test[0]" + Environment.NewLine +
                        _paddingString + "This is a test, and {curly braces} are just fine!" + Environment.NewLine,
                        GetMessage(sink.Writes));
                }
                break;
                case ConsoleLoggerFormat.Systemd:
                {
                    Assert.Single(sink.Writes);
                    Assert.Equal(
                        levelPrefix + "test[0]" + " " +
                        "This is a test, and {curly braces} are just fine!" + Environment.NewLine,
                        GetMessage(sink.Writes));
                }
                break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void LogAfterDisposeWritesLog()
        {
            // Arrange
            var sink = new ConsoleSink();
            var console = new TestConsole(sink);
            var processor = new ConsoleLoggerProcessor();
            processor.Console = console;
            var formatters = new ConcurrentDictionary<string, ConsoleFormatter>(GetFormatters().ToDictionary(f => f.Name));

            var logger = new ConsoleLogger(_loggerName, loggerProcessor: processor);
            logger.Options = new ConsoleLoggerOptions();
            Assert.Null(logger.Options.FormatterName);
            logger.Formatter = formatters[ConsoleFormatterNames.Simple];
            UpdateFormatterOptions(logger.Formatter, logger.Options);
            // Act
            processor.Dispose();
            logger.LogInformation("Logging after dispose");

            // Assert
            Assert.True(sink.Writes.Count == 2);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void IsEnabledReturnsCorrectValue()
        {
            var logger = SetUp().Logger;

            Assert.False(logger.IsEnabled(LogLevel.None));
            Assert.True(logger.IsEnabled(LogLevel.Critical));
            Assert.True(logger.IsEnabled(LogLevel.Error));
            Assert.True(logger.IsEnabled(LogLevel.Warning));
            Assert.True(logger.IsEnabled(LogLevel.Information));
            Assert.True(logger.IsEnabled(LogLevel.Debug));
            Assert.True(logger.IsEnabled(LogLevel.Trace));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void ConsoleLoggerOptions_InvalidFormat_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ConsoleLoggerOptions() { Format = (ConsoleLoggerFormat)10 });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void ConsoleLoggerOptions_TimeStampFormat_IsReloaded()
        {
            // Arrange
            var monitor = new TestOptionsMonitor(new ConsoleLoggerOptions());
            var loggerProvider = new ConsoleLoggerProvider(monitor);
            var logger = (ConsoleLogger)loggerProvider.CreateLogger("Name");

            // Act & Assert
            Assert.Null(logger.Options.TimestampFormat);
            monitor.Set(new ConsoleLoggerOptions() { TimestampFormat = "yyyyMMddHHmmss" });
            Assert.Equal("yyyyMMddHHmmss", logger.Options.TimestampFormat);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void ConsoleLoggerOptions_TimeStampFormat_MultipleReloads()
        {
            var monitor = new TestOptionsMonitor(new ConsoleLoggerOptions());
            var loggerProvider = new ConsoleLoggerProvider(monitor);
            var logger = (ConsoleLogger)loggerProvider.CreateLogger("Name");

            Assert.Null(logger.Options.TimestampFormat);
            monitor.Set(new ConsoleLoggerOptions() { TimestampFormat = "yyyyMMdd" });
            Assert.Equal("yyyyMMdd", logger.Options.TimestampFormat);
            monitor.Set(new ConsoleLoggerOptions() { TimestampFormat = "yyyyMMddHHmmss" });
            Assert.Equal("yyyyMMddHHmmss", logger.Options.TimestampFormat);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void ConsoleLoggerOptions_LogAsErrorLevel_IsAppliedToLoggers()
        {
            // Arrange
            var monitor = new TestOptionsMonitor(new ConsoleLoggerOptions());
            var loggerProvider = new ConsoleLoggerProvider(monitor);
            var logger = (ConsoleLogger)loggerProvider.CreateLogger("Name");

            // Act & Assert
            Assert.Equal(LogLevel.None, logger.Options.LogToStandardErrorThreshold);
            monitor.Set(new ConsoleLoggerOptions() { LogToStandardErrorThreshold = LogLevel.Error });
            Assert.Equal(LogLevel.Error, logger.Options.LogToStandardErrorThreshold);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void ConsoleLoggerOptions_UseUtcTimestamp_IsAppliedToLoggers()
        {
            // Arrange
            var monitor = new TestOptionsMonitor(new ConsoleLoggerOptions());
            var loggerProvider = new ConsoleLoggerProvider(monitor);
            var logger = (ConsoleLogger)loggerProvider.CreateLogger("Name");

            // Act & Assert
            Assert.False(logger.Options.UseUtcTimestamp);
            monitor.Set(new ConsoleLoggerOptions() { UseUtcTimestamp = true });
            Assert.True(logger.Options.UseUtcTimestamp);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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
            var formatter = Assert.IsType<SimpleConsoleFormatter>(logger.Formatter);
            Assert.True(formatter.FormatterOptions.IncludeScopes);
        }

        public static TheoryData<ConsoleLoggerFormat, LogLevel> FormatsAndLevels
        {
            get
            {
                var data = new TheoryData<ConsoleLoggerFormat, LogLevel>();
                foreach (ConsoleLoggerFormat format in Enum.GetValues(typeof(ConsoleLoggerFormat)))
                {
                    foreach (LogLevel level in Enum.GetValues(typeof(LogLevel)))
                    {
                        if (level == LogLevel.None)
                        {
                            continue;
                        }

                        data.Add(format, level);
                    }
                }
                return data;
            }
        }

        public static TheoryData<ConsoleLoggerFormat> Formats
        {
            get
            {
                var data = new TheoryData<ConsoleLoggerFormat>();
                foreach (ConsoleLoggerFormat value in Enum.GetValues(typeof(ConsoleLoggerFormat)))
                {
                    data.Add(value);
                }
                return data;
            }
        }

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

        private string CreateHeader(ConsoleLoggerFormat format, int eventId = 0)
        {
            switch (format)
            {
                case ConsoleLoggerFormat.Default:
                    return $": {_loggerName}[{eventId}]";
                case ConsoleLoggerFormat.Systemd:
                    return $"{_loggerName}[{eventId}]";
                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }
        }
    }

    internal class TestLoggerProcessor : ConsoleLoggerProcessor
    {
        public TestLoggerProcessor()
        {
        }

        public override void EnqueueMessage(LogMessageEntry message)
        {
            WriteMessage(message);
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
#pragma warning restore CS0618
