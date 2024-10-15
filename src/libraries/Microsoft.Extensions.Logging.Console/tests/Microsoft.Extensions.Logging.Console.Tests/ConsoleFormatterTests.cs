// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Test.Console;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Logging.Console.Test
{
    public class ConsoleFormatterTests : ConsoleTestsBase
    {
        protected const string _loggerName = "test";
        protected const string _state = "This is a test, and {curly braces} are just fine!";
        protected readonly Func<object, Exception, string> _defaultFormatter = (state, exception) => state.ToString();

        protected string GetMessage(List<ConsoleContext> contexts)
        {
            return string.Join("", contexts.Select(c => c.Message));
        }

        internal static SetupDisposeHelper SetUp(
            ConsoleLoggerOptions options = null,
            SimpleConsoleFormatterOptions simpleOptions = null,
            ConsoleFormatterOptions systemdOptions = null,
            JsonConsoleFormatterOptions jsonOptions = null)
        {
            // Arrange
            var sink = new ConsoleSink();
            var errorSink = new ConsoleSink();
            var console = new TestConsole(sink);
            var errorConsole = new TestConsole(errorSink);
            var bufferMode = options == null ? ConsoleLoggerQueueFullMode.Wait : options.QueueFullMode;
            var maxQueueLength = options == null ? ConsoleLoggerOptions.DefaultMaxQueueLengthValue : options.MaxQueueLength;
            var consoleLoggerProcessor = new TestLoggerProcessor(console, errorConsole, bufferMode, maxQueueLength);

            var formatters = new ConcurrentDictionary<string, ConsoleFormatter>(ConsoleLoggerTest.GetFormatters(simpleOptions, systemdOptions, jsonOptions).ToDictionary(f => f.Name));

            ConsoleFormatter? formatter = null;
            var loggerOptions = options ?? new ConsoleLoggerOptions();
            Func<LogLevel, string> levelAsString;
            int writesPerMsg;
            switch (loggerOptions.FormatterName)
            {
                case ConsoleFormatterNames.Simple:
                    levelAsString = ConsoleLoggerTest.LogLevelAsStringDefault;
                    writesPerMsg = 2;
                    formatter = formatters[ConsoleFormatterNames.Simple];
                    break;
                case ConsoleFormatterNames.Systemd:
                    levelAsString = ConsoleLoggerTest.GetSyslogSeverityString;
                    writesPerMsg = 1;
                    formatter = formatters[ConsoleFormatterNames.Systemd];
                    break;
                case ConsoleFormatterNames.Json:
                    levelAsString = ConsoleLoggerTest.GetJsonLogLevelString;
                    writesPerMsg = 1;
                    formatter = formatters[ConsoleFormatterNames.Json];
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(loggerOptions.FormatterName));
            }
            var logger = new ConsoleLogger(_loggerName, consoleLoggerProcessor, formatter, new LoggerExternalScopeProvider(), loggerOptions);

            return new SetupDisposeHelper(logger, sink, errorSink, levelAsString, writesPerMsg, consoleLoggerProcessor);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void ConsoleLoggerOptions_TimeStampFormat_IsReloaded()
        {
            // Arrange
            var monitor = new TestOptionsMonitor(new ConsoleLoggerOptions() { FormatterName = "NonExistentFormatter" });
            using var loggerProvider = new ConsoleLoggerProvider(monitor, ConsoleLoggerTest.GetFormatters());
            var logger = (ConsoleLogger)loggerProvider.CreateLogger("Name");

            // Act & Assert
            Assert.Equal("NonExistentFormatter", logger.Options.FormatterName);
            Assert.Equal(ConsoleFormatterNames.Simple, logger.Formatter.Name);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(FormatterNames))]
        public void InvalidLogLevel_Throws(string formatterName)
        {
            // Arrange
            using var t = SetUp(
                new ConsoleLoggerOptions { FormatterName = formatterName }
            );
            var logger = (ILogger)t.Logger;

            // Act/Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => logger.Log((LogLevel)8, 0, _state, null, _defaultFormatter));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(FormatterNamesAndLevels))]
        public void NoMessageOrException_Noop(string formatterName, LogLevel level)
        {
            // Arrange
            using var t = SetUp(new ConsoleLoggerOptions { FormatterName = formatterName });
            var levelPrefix = t.GetLevelPrefix(level);
            var logger = t.Logger;
            var sink = t.Sink;
            var ex = new Exception("Exception message" + Environment.NewLine + "with a second line");

            // Act
            Func<object, Exception, string> formatter = (state, exception) => null;
            logger.Log(level, 0, _state, null, formatter);

            // Assert
            Assert.Equal(0, sink.Writes.Count);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(FormatterNamesAndLevels))]
        public void Log_LogsCorrectTimestamp(string formatterName, LogLevel level)
        {
            // Arrange
            using var t = SetUp(
                new ConsoleLoggerOptions { FormatterName = formatterName },
                new SimpleConsoleFormatterOptions { TimestampFormat = "yyyy-MM-ddTHH:mm:sszz ", UseUtcTimestamp = false, ColorBehavior = LoggerColorBehavior.Enabled },
                new ConsoleFormatterOptions { TimestampFormat = "yyyy-MM-ddTHH:mm:sszz ", UseUtcTimestamp = false },
                new JsonConsoleFormatterOptions {
                    TimestampFormat = "yyyy-MM-ddTHH:mm:sszz ",
                    UseUtcTimestamp = false,
                    JsonWriterOptions = new JsonWriterOptions()
                    {
                        // otherwise escapes for timezone formatting from + to \u002b
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                        Indented = true
                    }
                });
            var levelPrefix = t.GetLevelPrefix(level);
            var logger = t.Logger;
            var sink = t.Sink;
            var ex = new Exception("Exception message" + Environment.NewLine + "with a second line");

            // Act
            logger.Log(level, 0, _state, ex, _defaultFormatter);

            // Assert
            switch (formatterName)
            {
                case ConsoleFormatterNames.Simple:
                {
                    Assert.Equal(3, sink.Writes.Count);
                    Assert.StartsWith(levelPrefix, sink.Writes[1].Message);
                    Assert.Matches(@"^\d{4}\D\d{2}\D\d{2}\D\d{2}\D\d{2}\D\d{2}\D\d{2}\s$", sink.Writes[0].Message);
                    var parsedDateTime = DateTimeOffset.Parse(sink.Writes[0].Message.Trim());
                    Assert.Equal(DateTimeOffset.Now.Offset, parsedDateTime.Offset);
                }
                break;
                case ConsoleFormatterNames.Systemd:
                {
                    Assert.Single(sink.Writes);
                    Assert.StartsWith(levelPrefix, sink.Writes[0].Message);
                    var regexMatch = Regex.Match(sink.Writes[0].Message, @"^<\d>(\d{4}\D\d{2}\D\d{2}\D\d{2}\D\d{2}\D\d{2}\D\d{2})\s[^\s]");
                    Assert.True(regexMatch.Success);
                    var parsedDateTime = DateTimeOffset.Parse(regexMatch.Groups[1].Value);
                    Assert.Equal(DateTimeOffset.Now.Offset, parsedDateTime.Offset);
                }
                break;
                case ConsoleFormatterNames.Json:
                {
                    Assert.Single(sink.Writes);
                    var regexMatch = Regex.Match(sink.Writes[0].Message, @"(\d{4}\D\d{2}\D\d{2}\D\d{2}\D\d{2}\D\d{2}\D\d{2})");
                    Assert.True(regexMatch.Success, sink.Writes[0].Message);
                    var parsedDateTime = DateTimeOffset.Parse(regexMatch.Groups[1].Value);
                    Assert.Equal(DateTimeOffset.Now.Offset, parsedDateTime.Offset);
                }
                break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(formatterName));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void NullFormatterName_Throws()
        {
            // Arrange
            Assert.Throws<ArgumentNullException>(() => new NullNameConsoleFormatter());
        }

        private class NullNameConsoleFormatter : ConsoleFormatter
        {
            public NullNameConsoleFormatter() : base(null) { }
            public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter) { }
        }

        public static TheoryData<string, LogLevel> FormatterNamesAndLevels
        {
            get
            {
                var data = new TheoryData<string, LogLevel>();
                foreach (LogLevel level in Enum.GetValues(typeof(LogLevel)))
                {
                    if (level == LogLevel.None)
                    {
                        continue;
                    }
                    data.Add(ConsoleFormatterNames.Simple, level);
                    data.Add(ConsoleFormatterNames.Systemd, level);
                    data.Add(ConsoleFormatterNames.Json, level);
                }
                return data;
            }
        }

        public static TheoryData<string> FormatterNames
        {
            get
            {
                var data = new TheoryData<string>();
                data.Add(ConsoleFormatterNames.Simple);
                data.Add(ConsoleFormatterNames.Systemd);
                data.Add(ConsoleFormatterNames.Json);
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

    }

    public class TestFormatter : ConsoleFormatter, IDisposable
    {
        private IDisposable _optionsReloadToken;

        public TestFormatter(IOptionsMonitor<SimpleConsoleFormatterOptions> options)
            : base ("TestFormatter")
        {
            FormatterOptions = options.CurrentValue;
            ReloadLoggerOptions(options.CurrentValue);
            _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
        }

        private void ReloadLoggerOptions(SimpleConsoleFormatterOptions options)
        {
            FormatterOptions = options;
        }

        public void Dispose()
        {
            _optionsReloadToken?.Dispose();
        }

        internal SimpleConsoleFormatterOptions FormatterOptions { get; set; }
        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
        {
            ;
        }
    }
}
