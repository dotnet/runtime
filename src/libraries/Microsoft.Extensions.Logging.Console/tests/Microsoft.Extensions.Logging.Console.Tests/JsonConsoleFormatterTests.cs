// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Text.RegularExpressions;
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
                new JsonConsoleFormatterOptions
                {
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void Log_TimestampFormatSet_ContainsTimestamp()
        {
            // Arrange
            var t = SetUp(
                new ConsoleLoggerOptions { FormatterName = ConsoleFormatterNames.Json },
                simpleOptions: null,
                systemdOptions: null,
                jsonOptions: new JsonConsoleFormatterOptions
                {
                    TimestampFormat = "hh:mm:ss ",
                }
            );
            var logger = (ILogger)t.Logger;
            var sink = t.Sink;
            var exception = new InvalidOperationException("Invalid value");

            // Act
            logger.LogCritical(eventId: 0, message: null);

            // Assert
            Assert.Equal(1, sink.Writes.Count);
            Assert.Contains(
                "\"Timestamp\":",
                GetMessage(sink.Writes.GetRange(0 * t.WritesPerMsg, t.WritesPerMsg)));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void Log_NullMessage_LogsWhenMessageIsNotProvided()
        {
            // Arrange
            var t = SetUp(
                new ConsoleLoggerOptions { FormatterName = ConsoleFormatterNames.Json },
                simpleOptions: null,
                systemdOptions: null,
                jsonOptions: new JsonConsoleFormatterOptions
                {
                    JsonWriterOptions = new JsonWriterOptions() { Indented = false }
                }
            );
            var logger = (ILogger)t.Logger;
            var sink = t.Sink;
            var exception = new InvalidOperationException("Invalid value");

            // Act
            logger.LogCritical(eventId: 0, exception: null, message: null);
            logger.LogCritical(eventId: 0, message: null);
            logger.LogCritical(eventId: 0, message: null, exception: exception);

            // Assert
            Assert.Equal(3, sink.Writes.Count);
            Assert.Equal(
                "{\"EventId\":0,\"LogLevel\":\"Critical\",\"Category\":\"test\",\"Message\":\"[null]\""
                + ",\"State\":{\"Message\":\"[null]\",\"{OriginalFormat}\":\"[null]\"}}"
                + Environment.NewLine,
                GetMessage(sink.Writes.GetRange(0 * t.WritesPerMsg, t.WritesPerMsg)));
            Assert.Equal(
                "{\"EventId\":0,\"LogLevel\":\"Critical\",\"Category\":\"test\",\"Message\":\"[null]\""
                + ",\"State\":{\"Message\":\"[null]\",\"{OriginalFormat}\":\"[null]\"}}"
                + Environment.NewLine,
                GetMessage(sink.Writes.GetRange(1 * t.WritesPerMsg, t.WritesPerMsg)));

            Assert.Equal(
                "{\"EventId\":0,\"LogLevel\":\"Critical\",\"Category\":\"test\""
                + ",\"Message\":\"[null]\""
                + ",\"Exception\":{\"Message\":\"Invalid value\",\"Type\":\"System.InvalidOperationException\",\"StackTrace\":[],\"HResult\":-2146233079}"
                + ",\"State\":{\"Message\":\"[null]\",\"{OriginalFormat}\":\"[null]\"}}"
                + Environment.NewLine,
                GetMessage(sink.Writes.GetRange(2 * t.WritesPerMsg, t.WritesPerMsg)));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void Log_ExceptionWithMessage_ExtractsInfo()
        {
            // Arrange
            var t = SetUp(
                new ConsoleLoggerOptions { FormatterName = ConsoleFormatterNames.Json },
                simpleOptions: null,
                systemdOptions: null,
                jsonOptions: new JsonConsoleFormatterOptions
                {
                    JsonWriterOptions = new JsonWriterOptions() { Indented = false },
                    IncludeScopes = true
                }
            );
            var logger = (ILogger)t.Logger;
            var sink = t.Sink;
            var exception = new InvalidOperationException("Invalid value");

            // Act
            logger.LogInformation(exception, "exception message with {0}", "stacktrace");
            logger.Log(LogLevel.Information, 0, state: "exception message", exception: exception, formatter: (a, b) => a);

            using (logger.BeginScope("scope1 {name1}", 123))
            using (logger.BeginScope("scope2 {name1} {name2}", 456, 789))
                logger.Log(LogLevel.Information, 0, state: "exception message", exception: exception, formatter: (a, b) => a);

            // Assert
            Assert.Equal(3, sink.Writes.Count);
            Assert.Equal(
                "{\"EventId\":0,\"LogLevel\":\"Information\",\"Category\":\"test\""
                + ",\"Message\":\"exception message with stacktrace\""
                + ",\"Exception\":{\"Message\":\"Invalid value\",\"Type\":\"System.InvalidOperationException\",\"StackTrace\":[],\"HResult\":-2146233079}"
                + ",\"State\":{\"Message\":\"exception message with stacktrace\",\"0\":\"stacktrace\",\"{OriginalFormat}\":\"exception message with {0}\"}"
                + ",\"Scopes\":[]"
                + "}" + Environment.NewLine,
                GetMessage(sink.Writes.GetRange(0 * t.WritesPerMsg, t.WritesPerMsg)));
            Assert.Equal(
                "{\"EventId\":0,\"LogLevel\":\"Information\",\"Category\":\"test\""
                + ",\"Message\":\"exception message\""
                + ",\"Exception\":{\"Message\":\"Invalid value\",\"Type\":\"System.InvalidOperationException\",\"StackTrace\":[],\"HResult\":-2146233079}"
                + ",\"State\":{\"Message\":\"exception message\"}"
                + ",\"Scopes\":[]"
                + "}" + Environment.NewLine,
                GetMessage(sink.Writes.GetRange(1 * t.WritesPerMsg, t.WritesPerMsg)));
            Assert.Equal(
                "{\"EventId\":0,\"LogLevel\":\"Information\",\"Category\":\"test\""
                + ",\"Message\":\"exception message\""
                + ",\"Exception\":{\"Message\":\"Invalid value\",\"Type\":\"System.InvalidOperationException\",\"StackTrace\":[],\"HResult\":-2146233079}"
                + ",\"State\":{\"Message\":\"exception message\"}"
                + ",\"Scopes\":[{\"Message\":\"scope1 123\",\"name1\":123,\"{OriginalFormat}\":\"scope1 {name1}\"},{\"Message\":\"scope2 456 789\",\"name1\":456,\"name2\":789,\"{OriginalFormat}\":\"scope2 {name1} {name2}\"}]"
                + "}" + Environment.NewLine,
                GetMessage(sink.Writes.GetRange(2 * t.WritesPerMsg, t.WritesPerMsg)));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void Log_IncludeScopes_ContainsDuplicateNamedPropertiesInScope_AcceptableJson()
        {
            // Arrange
            var t = SetUp(
                new ConsoleLoggerOptions { FormatterName = ConsoleFormatterNames.Json },
                simpleOptions: null,
                systemdOptions: null,
                jsonOptions: new JsonConsoleFormatterOptions
                {
                    JsonWriterOptions = new JsonWriterOptions() { Indented = false },
                    IncludeScopes = true
                }
            );
            var logger = (ILogger)t.Logger;
            var sink = t.Sink;

            // Act
            using (logger.BeginScope("scope1 {name1}", 123))
            using (logger.BeginScope("scope2 {name1} {name2}", 456, 789))
                logger.Log(LogLevel.Information, 0, state: "exception message", exception: null, formatter: (a, b) => a);

            // Assert
            Assert.Equal(1, sink.Writes.Count);
            Assert.Equal(
                "{\"EventId\":0,\"LogLevel\":\"Information\",\"Category\":\"test\""
                + ",\"Message\":\"exception message\""
                + ",\"State\":{\"Message\":\"exception message\"}"
                + ",\"Scopes\":[{\"Message\":\"scope1 123\",\"name1\":123,\"{OriginalFormat}\":\"scope1 {name1}\"},{\"Message\":\"scope2 456 789\",\"name1\":456,\"name2\":789,\"{OriginalFormat}\":\"scope2 {name1} {name2}\"}]"
                + "}" + Environment.NewLine,
                GetMessage(sink.Writes.GetRange(0 * t.WritesPerMsg, t.WritesPerMsg)));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void Log_StateAndScopeAreCollections_IncludesMessageAndCollectionValues()
        {
            // Arrange
            var t = SetUp(
                new ConsoleLoggerOptions { FormatterName = ConsoleFormatterNames.Json },
                simpleOptions: null,
                systemdOptions: null,
                jsonOptions: new JsonConsoleFormatterOptions
                {
                    JsonWriterOptions = new JsonWriterOptions() { Indented = false },
                    IncludeScopes = true
                }
            );
            var logger = (ILogger)t.Logger;
            var sink = t.Sink;

            // Act
            using (logger.BeginScope("{Number}", 2))
            using (logger.BeginScope("{AnotherNumber}", 3))
            {
                logger.LogInformation("{LogEntryNumber}", 1);
            }

            // Assert
            Assert.Equal(1, sink.Writes.Count);
            Assert.Equal(
                "{\"EventId\":0,\"LogLevel\":\"Information\",\"Category\":\"test\""
                + ",\"Message\":\"1\""
                + ",\"State\":{\"Message\":\"1\",\"LogEntryNumber\":1,\"{OriginalFormat}\":\"{LogEntryNumber}\"}"
                + ",\"Scopes\":[{\"Message\":\"2\",\"Number\":2,\"{OriginalFormat}\":\"{Number}\"},{\"Message\":\"3\",\"AnotherNumber\":3,\"{OriginalFormat}\":\"{AnotherNumber}\"}]"
                + "}" + Environment.NewLine,
                GetMessage(sink.Writes.GetRange(0 * t.WritesPerMsg, t.WritesPerMsg)));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(true, "true")]
        [InlineData((byte)1, "1")]
        [InlineData((sbyte)1, "1")]
        [InlineData('a', "\"a\"")]
        [InlineData(1, "1")]
        [InlineData((uint)1, "1")]
        [InlineData((long)1, "1")]
        [InlineData((ulong)1, "1")]
        [InlineData((short)1, "1")]
        [InlineData((ushort)1, "1")]
        public void Log_StateAndScopeContainsBuiltInValueType_SerializesValue(object value, string expectedJsonValue)
        {
            // Arrange
            var t = SetUp(
                new ConsoleLoggerOptions { FormatterName = ConsoleFormatterNames.Json },
                simpleOptions: null,
                systemdOptions: null,
                jsonOptions: new JsonConsoleFormatterOptions
                {
                    JsonWriterOptions = new JsonWriterOptions() { Indented = false },
                    IncludeScopes = true
                }
            );
            var logger = (ILogger)t.Logger;
            var sink = t.Sink;

            // Act
            using (logger.BeginScope("{Value}", value))
            {
                logger.LogInformation("{LogEntryValue}", value);
            }

            // Assert
            string message = sink.Writes[0].Message;
            Assert.Contains("\"Value\":" + expectedJsonValue + ",", message);
            Assert.Contains("\"LogEntryValue\":" + expectedJsonValue + ",", message);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(1.2d)]
        [InlineData(1.2f)]
        public void Log_StateAndScopeContainsFloatingPointType_SerializesValue(object value)
        {
            // Arrange
            var t = SetUp(
                new ConsoleLoggerOptions { FormatterName = ConsoleFormatterNames.Json },
                simpleOptions: null,
                systemdOptions: null,
                jsonOptions: new JsonConsoleFormatterOptions
                {
                    JsonWriterOptions = new JsonWriterOptions() { Indented = false },
                    IncludeScopes = true
                }
            );
            var logger = (ILogger)t.Logger;
            var sink = t.Sink;

            // Act
            using (logger.BeginScope("{Value}", value))
            {
                logger.LogInformation("{LogEntryValue}", value);
            }

            // Assert
            string message = sink.Writes[0].Message;
            AssertMessageValue(message, "Value");
            AssertMessageValue(message, "LogEntryValue");

            static void AssertMessageValue(string message, string propertyName)
            {
                var serializedValueMatch = Regex.Match(message, "\"" + propertyName + "\":(.*?),");
                Assert.Equal(2, serializedValueMatch.Groups.Count);
                string jsonValue = serializedValueMatch.Groups[1].Value;
                Assert.True(double.TryParse(jsonValue, out var floatingPointValue), "The json doesn not contain a floating point value: " + jsonValue);
                Assert.Equal(1.2, floatingPointValue, 2);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void Log_StateAndScopeContainsDecimal_SerializesValue()
        {
            // Arrange
            var t = SetUp(
                new ConsoleLoggerOptions { FormatterName = ConsoleFormatterNames.Json },
                simpleOptions: null,
                systemdOptions: null,
                jsonOptions: new JsonConsoleFormatterOptions
                {
                    JsonWriterOptions = new JsonWriterOptions() { Indented = false },
                    IncludeScopes = true
                }
            );
            var logger = (ILogger)t.Logger;
            var sink = t.Sink;

            // Act
            using (logger.BeginScope("{Value}", 1.2m))
            {
                logger.LogInformation("{LogEntryValue}", 1.2m);
            }

            // Assert
            string message = sink.Writes[0].Message;
            Assert.Contains("\"Value\":1.2,", message);
            Assert.Contains("\"LogEntryValue\":1.2,", message);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void Log_StateAndScopeContainsDyanmicObject_SerializesAsString()
        {
            // Arrange
            var t = SetUp(
                new ConsoleLoggerOptions { FormatterName = ConsoleFormatterNames.Json },
                simpleOptions: null,
                systemdOptions: null,
                jsonOptions: new JsonConsoleFormatterOptions
                {
                    JsonWriterOptions = new JsonWriterOptions() { Indented = false },
                    IncludeScopes = true
                }
            );
            var logger = (ILogger)t.Logger;
            var sink = t.Sink;

            // Act
            using (logger.BeginScope("{Object}", new { a = 1, b = 2 }))
            {
                logger.LogInformation("{LogEntryObject}", new { c = 1, d = 2 });
            }

            // Assert
            string message = sink.Writes[0].Message;
            Assert.Contains("\"Object\":\"{ a = 1, b = 2 }\"", message);
            Assert.Contains("\"LogEntryObject\":\"{ c = 1, d = 2 }\"", message);
        }
    }
}
