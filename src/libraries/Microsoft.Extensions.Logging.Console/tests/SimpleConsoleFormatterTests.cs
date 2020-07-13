// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Test.Console;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Logging.Console.Test
{
    public class SimpleConsoleFormatterTests : ConsoleFormatterTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void Log_WritingScopes_LogsWithCorrectColors()
        {
            // Arrange
            var t = SetUp(
                new ConsoleLoggerOptions { FormatterName = ConsoleFormatterNames.Simple },
                new SimpleConsoleFormatterOptions { IncludeScopes = true }
                );
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void Log_NoLogScope_DoesNotWriteAnyScopeContentToOutput()
        {
            // Arrange
            var t = SetUp(
                new ConsoleLoggerOptions { FormatterName = ConsoleFormatterNames.Simple },
                new SimpleConsoleFormatterOptions { IncludeScopes = true }
            );
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
        public void Log_SingleLine_LogsWhenMessageIsNotProvided()
        {
            // Arrange
            var t = SetUp(
                new ConsoleLoggerOptions { FormatterName = ConsoleFormatterNames.Simple },
                new SimpleConsoleFormatterOptions { SingleLine = true }
            );
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
                "crit: test[0]" + " " + "[null]" + Environment.NewLine,
                GetMessage(sink.Writes.GetRange(0 * t.WritesPerMsg, t.WritesPerMsg)));
            Assert.Equal(
                "crit: test[0]" + " " + "[null]" + Environment.NewLine,
                GetMessage(sink.Writes.GetRange(1 * t.WritesPerMsg, t.WritesPerMsg)));

            Assert.Equal(
                "crit: test[0]" + " " + "[null]" + "System.InvalidOperationException: Invalid value" + Environment.NewLine,
                GetMessage(sink.Writes.GetRange(2 * t.WritesPerMsg, t.WritesPerMsg)));
        }
    }
}