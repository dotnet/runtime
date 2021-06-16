// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
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
                + ",\"Exception\":\"System.InvalidOperationException: Invalid value\""
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
                + ",\"Exception\":\"System.InvalidOperationException: Invalid value\""
                + ",\"State\":{\"Message\":\"exception message with stacktrace\",\"0\":\"stacktrace\",\"{OriginalFormat}\":\"exception message with {0}\"}"
                + ",\"Scopes\":[]"
                + "}" + Environment.NewLine,
                GetMessage(sink.Writes.GetRange(0 * t.WritesPerMsg, t.WritesPerMsg)));
            Assert.Equal(
                "{\"EventId\":0,\"LogLevel\":\"Information\",\"Category\":\"test\""
                + ",\"Message\":\"exception message\""
                + ",\"Exception\":\"System.InvalidOperationException: Invalid value\""
                + ",\"State\":{\"Message\":\"exception message\"}"
                + ",\"Scopes\":[]"
                + "}" + Environment.NewLine,
                GetMessage(sink.Writes.GetRange(1 * t.WritesPerMsg, t.WritesPerMsg)));
            Assert.Equal(
                "{\"EventId\":0,\"LogLevel\":\"Information\",\"Category\":\"test\""
                + ",\"Message\":\"exception message\""
                + ",\"Exception\":\"System.InvalidOperationException: Invalid value\""
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
        [MemberData(nameof(SpecialCaseValues))]
        public void Log_StateAndScopeContainsSpecialCaseValue_SerializesValueAsExpected(object value, string expectedJsonValue)
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
        [MemberData(nameof(FloatingPointValues))]
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
                Assert.True(double.TryParse(jsonValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var floatingPointValue), "The json doesn not contain a floating point value: " + jsonValue);
                Assert.Equal(1.2, floatingPointValue, 2);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void Log_StateAndScopeContainsNullValue_SerializesNull()
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
            using (logger.BeginScope(new WithNullValue("ScopeKey")))
            {
                logger.Log(LogLevel.Information, 0, state: new WithNullValue("LogKey"), exception: null, formatter: (a, b) => string.Empty);
            }

            // Assert
            string message = sink.Writes[0].Message;
            Assert.Contains("\"ScopeKey\":null", message);
            Assert.Contains("\"LogKey\":null", message);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void Log_ScopeIsIEnumerable_SerializesKeyValuePair()
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
            using (logger.BeginScope(new[] { 2 }.Select(x => new KeyValuePair<string, object>("Value", x))))
            {
                logger.LogInformation("{LogEntryNumber}", 1);
            }

            // Assert
            string message = sink.Writes[0].Message;
            Assert.Contains("\"Message\":\"System.Linq.Enumerable", message);
            Assert.Contains("\"Value\":" + 2, message);
        }

        public static TheoryData<object, string> SpecialCaseValues
        {
            get
            {
                var data = new TheoryData<object, string>
                {
                    // primitives, excluding floating point
                    { true, "true" },
                    { (byte)1, "1" },
                    { (sbyte)1, "1" },
                    { 'a', "\"a\"" },
                    { 1, "1" },
                    { (uint)1, "1" },
                    { (long)1, "1" },
                    { (ulong)1, "1" },
                    { (short)1, "1" },
                    { (ushort)1, "1" },
                    { 1.2m, "1.2" },

                    // nullables primitives, excluding floating point
                    { (bool?)true, "true" },
                    { (byte?)1, "1" },
                    { (sbyte?)1, "1" },
                    { (char?)'a', "\"a\"" },
                    { (int?)1, "1" },
                    { (uint?)1, "1" },
                    { (long?)1, "1" },
                    { (ulong?)1, "1" },
                    { (short?)1, "1" },
                    { (ushort?)1, "1" },
                    { (decimal?)1.2m, "1.2" },

                    // Dynamic object serialized as string
                    { new { a = 1, b = 2 }, "\"{ a = 1, b = 2 }\"" },

                    // null should not be serialized as special string in the state value, only in message
                    { null, "null" }
                };
                return data;
            }
        }

        public static TheoryData<object> FloatingPointValues
        {
            get
            {
                var data = new TheoryData<object>
                {
                    { 1.2 },
                    { 1.2f },

                    // nullables
                    { (double?)1.2 },
                    { (float?)1.2f }
                };
                return data;
            }
        }

        internal class WithNullValue : IReadOnlyList<KeyValuePair<string, object>>
        {
            private readonly string _key;

            public WithNullValue(string key)
            {
                _key = key;
            }

            int IReadOnlyCollection<KeyValuePair<string, object>>.Count { get; } = 1;

            KeyValuePair<string, object> IReadOnlyList<KeyValuePair<string, object>>.this[int index]
            {
                get
                {
                    if (index == 0)
                    {
                        return new KeyValuePair<string, object>(_key, null);
                    }

                    throw new ArgumentOutOfRangeException(nameof(index));
                }
            }

            IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
            {
                yield return new KeyValuePair<string, object>(_key, null);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return ((IEnumerable<KeyValuePair<string, object>>)this).GetEnumerator();
            }
        }

        private static void EnsureStackTrace(params Exception[] exceptions)
        {
            if (exceptions == null) return;

            foreach (Exception exception in exceptions)
            {
                if (string.IsNullOrEmpty(exception.StackTrace))
                {
                    try
                    {
                        throw exception;
                    }
                    catch
                    { }
                }
                Assert.False(string.IsNullOrEmpty(exception.StackTrace));
            }
        }

        private string GetJson(Exception exception, bool indented)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));
            JsonConsoleFormatterOptions jsonOptions = new JsonConsoleFormatterOptions()
            {
                JsonWriterOptions = new JsonWriterOptions()
                { 
                    Indented = indented,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                }
            };
            var jsonMonitor = new TestFormatterOptionsMonitor<JsonConsoleFormatterOptions>(jsonOptions);
            var jsonFormatter = new JsonConsoleFormatter(jsonMonitor);
            Func<string, Exception, string> exceptionFormatter = (state, exception) => state.ToString();
            LogEntry<string> entry = new LogEntry<string>(LogLevel.Error, string.Empty, new EventId(), string.Empty, exception, exceptionFormatter);
            StringBuilder output = new StringBuilder();
            using (TextWriter writer = new StringWriter(output))
            {
                jsonFormatter.Write<string>(entry, null, writer);
            }
            return output.ToString();
        }


        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public void ShouldContainInnerException(bool indented)
        {
            Exception rootException = new Exception("root", new Exception("inner"));
            EnsureStackTrace(rootException, rootException.InnerException);
            string json = GetJson(rootException, indented);

            Assert.Contains(rootException.Message, json);
            Assert.Contains(rootException.InnerException.Message, json);
            
            Assert.Contains(GetContent(rootException, indented), json);
            Assert.Contains(GetContent(rootException.InnerException, indented), json);
        }

        static string GetContent(Exception exception, bool indented)
        {
            // Depending on OS, Environment.NewLine is either '\r\n' OR '\n'
            string newLineReplacement = indented ? (Environment.NewLine.Length == 2 ? "\\r\\n" : "\\n") : " ";

            return exception.ToString()
                .Replace(@"\", @"\\") // for paths in json content
                .Replace(Environment.NewLine, newLineReplacement);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public void ShouldContainAggregateExceptions(bool indented)
        {
            AggregateException rootException = new AggregateException("aggregate", new Exception("leaf1"), new Exception("leaf2"), new Exception("leaf3"));
            EnsureStackTrace(rootException);
            EnsureStackTrace(rootException.InnerExceptions.ToArray());
            string json = GetJson(rootException, indented);

            Assert.Contains(rootException.Message, json);
            rootException.InnerExceptions.ToList().ForEach((inner) => Assert.Contains(inner.Message, json));
            
            Assert.Contains(GetContent(rootException, indented), json);
            rootException.InnerExceptions.ToList().ForEach((inner) => Assert.Contains(GetContent(inner, indented), json));
        }
    }
}
