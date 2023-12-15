// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging.Test.Console;
using Xunit;

namespace Microsoft.Extensions.Logging.Console.Test
{
    public class ConsoleLoggerProcessorTests
    {
        private const string _loggerName = "test";

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void LogAfterDisposeWritesLog()
        {
            // Arrange
            var sink = new ConsoleSink();
            var console = new TestConsole(sink);
            var processor = new ConsoleLoggerProcessor(console, null!, ConsoleLoggerQueueFullMode.Wait, 1024);

            var logger = new ConsoleLogger(_loggerName, loggerProcessor: processor,
                new SimpleConsoleFormatter(new TestFormatterOptionsMonitor<SimpleConsoleFormatterOptions>(new SimpleConsoleFormatterOptions())),
                null, new ConsoleLoggerOptions());
            Assert.Null(logger.Options.FormatterName);
            UpdateFormatterOptions(logger.Formatter, logger.Options);

            // Act
            processor.Dispose();
            logger.LogInformation("Logging after dispose");

            // Assert
            Assert.Equal(2, sink.Writes.Count);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void LogsFlushedAfterDispose()
        {
            // Arrange
            var sink = new ConsoleSink();
            var console = new TestConsole(sink);
            var processor = new ConsoleLoggerProcessor(console, null!, ConsoleLoggerQueueFullMode.Wait, 1024);

            var logger = new ConsoleLogger(_loggerName, loggerProcessor: processor,
                new SimpleConsoleFormatter(new TestFormatterOptionsMonitor<SimpleConsoleFormatterOptions>(new SimpleConsoleFormatterOptions())),
                null, new ConsoleLoggerOptions());
            Assert.Null(logger.Options.FormatterName);
            UpdateFormatterOptions(logger.Formatter, logger.Options);

            // Act
            const int repetitions = 1000;
            for (int i = 0; i < repetitions; i++)
            {
                logger.LogInformation("Message #{i}", i);
            }
            processor.Dispose();

            // Assert
            Assert.Equal(2*repetitions, sink.Writes.Count); // 2 writes per message (category and msg)
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(-1)]
        [InlineData(0)]
        public static void MaxQueueLength_SetInvalid_Throws(int invalidMaxQueueLength)
        {
            // Arrange
            var sink = new ConsoleSink();
            var console = new TestConsole(sink);
            var processor = new ConsoleLoggerProcessor(console, null!, ConsoleLoggerQueueFullMode.Wait, 1024);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => processor.MaxQueueLength = invalidMaxQueueLength);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void FullMode_SetInvalid_Throws()
        {
            // Arrange
            var sink = new ConsoleSink();
            var console = new TestConsole(sink);
            var processor = new ConsoleLoggerProcessor(console, null!, ConsoleLoggerQueueFullMode.Wait, 1024);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => processor.FullMode = (ConsoleLoggerQueueFullMode)10);
        }

        [OuterLoop]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public void CheckForNotificationWhenQueueIsFull(bool okToDrop)
        {
            // Arrange
            var sink = new ConsoleSink();
            var console = new TestConsole(sink);
            var errorConsole = new TimesWriteCalledConsole();
            string queueName = nameof(CheckForNotificationWhenQueueIsFull) + (okToDrop ? "InDropWriteMode" : "InWaitMode");
            var fullMode = okToDrop ? ConsoleLoggerQueueFullMode.DropWrite : ConsoleLoggerQueueFullMode.Wait;
            var processor = new ConsoleLoggerProcessor(console, errorConsole, fullMode, maxQueueLength: 1);
            var formatter = new SimpleConsoleFormatter(new TestFormatterOptionsMonitor<SimpleConsoleFormatterOptions>(
                new SimpleConsoleFormatterOptions()));

            var logger = new ConsoleLogger(_loggerName, processor, formatter, null, new ConsoleLoggerOptions());
            Assert.Null(logger.Options.FormatterName);
            UpdateFormatterOptions(logger.Formatter, logger.Options);
            string messageTemplate = string.Join(", ", Enumerable.Range(1, 100).Select(x => "{A" + x + "}"));
            object[] messageParams = Enumerable.Range(1, 100).Select(x => (object)x).ToArray();

            // Act
            for (int i = 0; i < 20000; i++)
            {
                logger.LogInformation(messageTemplate, messageParams);
            }

            // Assert
            if (okToDrop)
            {
                Assert.True(errorConsole.TimesWriteCalled > 0);
            }
            else
            {
                Assert.Equal(0, errorConsole.TimesWriteCalled);
            }
        }

        private class TimesWriteCalledConsole : IConsole
        {
            public volatile int TimesWriteCalled;
            public void Write(string message)
            {
                TimesWriteCalled++;
            }
        }

        private class WriteThrowingConsole : IConsole
        {
            public void Write(string message)
            {
                throw new InvalidOperationException();
            }
        }

        [OuterLoop]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void ThrowDuringProcessLog_ShutsDownGracefully()
        {
            var console = new TimesWriteCalledConsole();
            var writeThrowingConsole = new WriteThrowingConsole();
            var processor = new ConsoleLoggerProcessor(
                console,
                writeThrowingConsole,
                ConsoleLoggerQueueFullMode.Wait,
                1024);

            var formatter = new SimpleConsoleFormatter(
                new TestFormatterOptionsMonitor<SimpleConsoleFormatterOptions>(
                    new SimpleConsoleFormatterOptions()));

            var logger = new ConsoleLogger(_loggerName, processor, formatter, null, new ConsoleLoggerOptions()
            {
                LogToStandardErrorThreshold = LogLevel.Error
            });

            Assert.Null(logger.Options.FormatterName);
            UpdateFormatterOptions(logger.Formatter, logger.Options);
            logger.LogInformation("Process 1st log normally using {DesiredConsole}", nameof(TimesWriteCalledConsole));
            logger.LogInformation("Process 2nd log normally using {DesiredConsole}", nameof(TimesWriteCalledConsole));
            while (console.TimesWriteCalled != 2); // wait until the logs are processed
            Assert.Equal(2, console.TimesWriteCalled);
            logger.LogError("Causing exception to throw in {ClassName} using {DesiredConsole}", nameof(ConsoleLoggerProcessor), nameof(WriteThrowingConsole));
            logger.LogInformation("After the write logic threw exception, {ClassName} stopped gracefully, finish processing queued logs", nameof(ConsoleLoggerProcessor));
            // disposing makes sure that all queued messages are flushed
            processor.Dispose();
            Assert.Equal(3, console.TimesWriteCalled);
        }

        private static void UpdateFormatterOptions(ConsoleFormatter formatter, ConsoleLoggerOptions deprecatedFromOptions)
        {
#pragma warning disable CS0618
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
#pragma warning restore CS0618
        }
    }
}
