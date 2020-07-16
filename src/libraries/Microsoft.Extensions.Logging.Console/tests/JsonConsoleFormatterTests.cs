// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Test.Console;
using Xunit;

namespace Microsoft.Extensions.Logging.Console.Test
{
    public class JsonConsoleFormatterTests : ConsoleFormatterTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void NoLogScope_DoesNotWriteAnyScopeContentToOutput_Json()
        {
            // Arrange
            var t = ConsoleFormatterTests.SetUp(
                new ConsoleLoggerOptions { FormatterName = ConsoleFormatterNames.Json },
                new SimpleConsoleFormatterOptions { IncludeScopes = true },
                new ConsoleFormatterOptions { IncludeScopes = true },
                new JsonConsoleFormatterOptions {
                    IncludeScopes = true,
                    JsonWriterOptions = new JsonWriterOptions() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping } 
                });
            var logger = t.Logger;
            var sink = t.Sink;

            // Act
            using (logger.BeginScope("Scope with named parameter {namedParameter}", 123))
            using (logger.BeginScope("SimpleScope"))
                logger.Log(LogLevel.Warning, 0, "Message with {args}", 73, _defaultFormatter);

            // Assert
            Assert.Equal(1, sink.Writes.Count);
            var write = sink.Writes[0];
            Assert.Equal(TestConsole.DefaultBackgroundColor, write.BackgroundColor);
            Assert.Equal(TestConsole.DefaultForegroundColor, write.ForegroundColor);
            Assert.Contains("Message with {args}", write.Message);
            Assert.Contains("73", write.Message);
            Assert.Contains("{OriginalFormat}", write.Message);
            Assert.Contains("namedParameter", write.Message);
            Assert.Contains("123", write.Message);
            Assert.Contains("SimpleScope", write.Message);
        }
    }
}
