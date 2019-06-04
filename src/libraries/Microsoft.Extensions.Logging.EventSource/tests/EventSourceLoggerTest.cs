// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging.EventSource;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Microsoft.Extensions.Logging.Test
{
    public class EventSourceLoggerTest: IDisposable
    {
        private ILoggerFactory _loggerFactory;

        protected ILoggerFactory CreateLoggerFactory()
        {
            return _loggerFactory = LoggerFactory.Create(builder => builder.AddEventSourceLogger());
        }

        public void Dispose()
        {
            _loggerFactory?.Dispose();
        }

        [Fact]
        public void IsEnabledReturnsCorrectValue()
        {
            using (var testListener = new TestEventListener())
            {
                var loggerFactory = CreateLoggerFactory();

                var listenerSettings = new TestEventListener.ListenerSettings();
                listenerSettings.Keywords = LoggingEventSource.Keywords.JsonMessage;
                listenerSettings.FilterSpec = null;
                listenerSettings.Level = EventLevel.Warning;
                testListener.EnableEvents(listenerSettings);

                var logger = loggerFactory.CreateLogger("Logger1");

                Assert.False(logger.IsEnabled(LogLevel.None));
                Assert.True(logger.IsEnabled(LogLevel.Critical));
                Assert.True(logger.IsEnabled(LogLevel.Error));
                Assert.True(logger.IsEnabled(LogLevel.Warning));
                Assert.False(logger.IsEnabled(LogLevel.Information));
                Assert.False(logger.IsEnabled(LogLevel.Debug));
                Assert.False(logger.IsEnabled(LogLevel.Trace));
            }
        }

        [Fact]
        public void Logs_AsExpected_WithDefaults()
        {
            using (var testListener = new TestEventListener())
            {
                var factory = CreateLoggerFactory();

                var listenerSettings = new TestEventListener.ListenerSettings();
                listenerSettings.Keywords = (EventKeywords)(-1);
                listenerSettings.FilterSpec = null;
                listenerSettings.Level = default(EventLevel);
                testListener.EnableEvents(listenerSettings);

                LogStuff(factory);

                // Use testListener.DumpEvents as necessary to examine what exactly the listener received

                VerifyEvents(testListener,
                    "E1FM", "E1MSG", "E1JS",
                    // Second event is omitted because default LogLevel == Debug
                    "E3FM", "E3MSG", "E3JS",
                    "OuterScopeJsonStart",
                    "E4FM", "E4MSG", "E4JS",
                    "E5FM", "E5MSG", "E5JS",
                    "InnerScopeJsonStart",
                    "E6FM", "E6MSG", "E6JS",
                    "InnerScopeJsonStop",
                    "E7FM", "E7MSG", "E7JS",
                    "OuterScopeJsonStop",
                    "E8FM", "E8MSG", "E8JS");
            }
        }

        [Fact]
        public void Logs_AsExpected_WithDefaults_EnabledEarly()
        {
            using (var testListener = new TestEventListener())
            {
                var listenerSettings = new TestEventListener.ListenerSettings();
                listenerSettings.Keywords = (EventKeywords)(-1);
                listenerSettings.FilterSpec = null;
                listenerSettings.Level = default(EventLevel);
                testListener.EnableEvents(listenerSettings);

                LogStuff(CreateLoggerFactory());

                // Use testListener.DumpEvents as necessary to examine what exactly the listener received

                VerifyEvents(testListener,
                    "E1FM", "E1MSG", "E1JS",
                    // Second event is omitted because default LogLevel == Debug
                    "E3FM", "E3MSG", "E3JS",
                    "OuterScopeJsonStart",
                    "E4FM", "E4MSG", "E4JS",
                    "E5FM", "E5MSG", "E5JS",
                    "InnerScopeJsonStart",
                    "E6FM", "E6MSG", "E6JS",
                    "InnerScopeJsonStop",
                    "E7FM", "E7MSG", "E7JS",
                    "OuterScopeJsonStop",
                    "E8FM", "E8MSG", "E8JS");
            }
        }

        [Fact]
        public void Logs_Nothing_IfNotEnabled()
        {
            using (var testListener = new TestEventListener())
            {
                var factory = CreateLoggerFactory();

                LogStuff(factory);

                VerifyEvents(testListener); // No verifiers = 0 events expected
            }
        }

        [Fact]
        public void Logs_OnlyFormattedMessage_IfKeywordSet()
        {
            using (var testListener = new TestEventListener())
            {
                var factory = CreateLoggerFactory();

                var listenerSettings = new TestEventListener.ListenerSettings();
                listenerSettings.Keywords = LoggingEventSource.Keywords.FormattedMessage;
                listenerSettings.FilterSpec = null;
                listenerSettings.Level = EventLevel.Verbose;
                testListener.EnableEvents(listenerSettings);

                LogStuff(factory);

                VerifyEvents(testListener,
                    "E1FM",
                    // Second event is omitted because default LogLevel == Debug
                    "E3FM",
                    "OuterScopeStart",
                    "E4FM",
                    "E5FM",
                    "InnerScopeStart",
                    "E6FM",
                    "InnerScopeStop",
                    "E7FM",
                    "OuterScopeStop",
                    "E8FM");
            }
        }

        [Fact]
        public void Logs_OnlyJson_IfKeywordSet()
        {
            using (var testListener = new TestEventListener())
            {
                var factory = CreateLoggerFactory();

                var listenerSettings = new TestEventListener.ListenerSettings();
                listenerSettings.Keywords = LoggingEventSource.Keywords.JsonMessage;
                listenerSettings.FilterSpec = null;
                listenerSettings.Level = EventLevel.Verbose;
                testListener.EnableEvents(listenerSettings);

                LogStuff(factory);

                VerifyEvents(testListener,
                    "E1JS",
                    // Second event is omitted because default LogLevel == Debug
                    "E3JS",
                    "OuterScopeJsonStart",
                    "E4JS",
                    "E5JS",
                    "InnerScopeJsonStart",
                    "E6JS",
                    "InnerScopeJsonStop",
                    "E7JS",
                    "OuterScopeJsonStop",
                    "E8JS");
            }
        }

        [Fact]
        public void Logs_OnlyMessage_IfKeywordSet()
        {
            using (var testListener = new TestEventListener())
            {
                var factory = CreateLoggerFactory();

                var listenerSettings = new TestEventListener.ListenerSettings();
                listenerSettings.Keywords = LoggingEventSource.Keywords.Message;
                listenerSettings.FilterSpec = null;
                listenerSettings.Level = EventLevel.Verbose;
                testListener.EnableEvents(listenerSettings);

                LogStuff(factory);

                VerifyEvents(testListener,
                    "E1MSG",
                    // Second event is omitted because default LogLevel == Debug
                    "E3MSG",
                    "OuterScopeStart",
                    "E4MSG",
                    "E5MSG",
                    "InnerScopeStart",
                    "E6MSG",
                    "InnerScopeStop",
                    "E7MSG",
                    "OuterScopeStop",
                    "E8MSG");
            }
        }

        [Fact]
        public void Logs_AllEvents_IfTraceSet()
        {
            using (var testListener = new TestEventListener())
            {
                var factory = CreateLoggerFactory();

                var listenerSettings = new TestEventListener.ListenerSettings();
                listenerSettings.Keywords = LoggingEventSource.Keywords.JsonMessage;
                listenerSettings.FilterSpec = "Logger1:Trace;Logger2:Trace;Logger3:Trace";
                listenerSettings.Level = EventLevel.Verbose;
                testListener.EnableEvents(listenerSettings);

                LogStuff(factory);

                VerifyEvents(testListener,
                    "E1JS",
                    "E2JS",
                    "E3JS",
                    "OuterScopeJsonStart",
                    "E4JS",
                    "E5JS",
                    "InnerScopeJsonStart",
                    "E6JS",
                    "InnerScopeJsonStop",
                    "E7JS",
                    "OuterScopeJsonStop",
                    "E8JS");
            }
        }

        [Fact]
        public void Logs_AsExpected_AtErrorLevel()
        {
            using (var testListener = new TestEventListener())
            {
                var factory = CreateLoggerFactory();

                var listenerSettings = new TestEventListener.ListenerSettings();
                listenerSettings.Keywords = LoggingEventSource.Keywords.JsonMessage;
                listenerSettings.FilterSpec = null;
                listenerSettings.Level = EventLevel.Error;
                testListener.EnableEvents(listenerSettings);

                LogStuff(factory);

                VerifyEvents(testListener,
                    "OuterScopeJsonStart",
                    "E4JS",
                    "E5JS",
                    "InnerScopeJsonStart",
                    "InnerScopeJsonStop",
                    "OuterScopeJsonStop");
            }
        }

        [Fact]
        public void Logs_AsExpected_AtWarningLevel()
        {
            using (var testListener = new TestEventListener())
            {
                var factory = CreateLoggerFactory();

                var listenerSettings = new TestEventListener.ListenerSettings();
                listenerSettings.Keywords = LoggingEventSource.Keywords.JsonMessage;
                listenerSettings.FilterSpec = null;
                listenerSettings.Level = EventLevel.Warning;
                testListener.EnableEvents(listenerSettings);

                LogStuff(factory);

                VerifyEvents(testListener,
                    "OuterScopeJsonStart",
                    "E4JS",
                    "E5JS",
                    "InnerScopeJsonStart",
                    "E6JS",
                    "InnerScopeJsonStop",
                    "OuterScopeJsonStop",
                    "E8JS");
            }
        }

        [Fact]
        public void Logs_AsExpected_WithSingleLoggerSpec()
        {
            using (var testListener = new TestEventListener())
            {
                var factory = CreateLoggerFactory();

                var listenerSettings = new TestEventListener.ListenerSettings();
                listenerSettings.Keywords = LoggingEventSource.Keywords.JsonMessage;
                listenerSettings.FilterSpec = "Logger2";
                listenerSettings.Level = EventLevel.Verbose;
                testListener.EnableEvents(listenerSettings);

                LogStuff(factory);

                VerifyEvents(testListener,
                    "E5JS",
                    "E6JS",
                    "E8JS");
            }
        }

        [Fact]
        public void Logs_AsExpected_WithSingleLoggerSpecWithVerbosity()
        {
            using (var testListener = new TestEventListener())
            {
                var factory = CreateLoggerFactory();

                var listenerSettings = new TestEventListener.ListenerSettings();
                listenerSettings.Keywords = LoggingEventSource.Keywords.JsonMessage;
                listenerSettings.FilterSpec = "Logger2:Error";
                listenerSettings.Level = EventLevel.Error;
                testListener.EnableEvents(listenerSettings);

                LogStuff(factory);

                VerifyEvents(testListener,
                    "E5JS");
            }
        }

        [Fact]
        public void Logs_AsExpected_AfterSettingsReload()
        {
            using (var testListener = new TestEventListener())
            {
                var factory = CreateLoggerFactory();

                var listenerSettings = new TestEventListener.ListenerSettings();
                listenerSettings.Keywords = LoggingEventSource.Keywords.JsonMessage;
                listenerSettings.FilterSpec = "Logger2:Error";
                listenerSettings.Level = EventLevel.Error;
                testListener.EnableEvents(listenerSettings);

                LogStuff(factory);

                VerifyEvents(testListener,
                    "E5JS");

                listenerSettings = new TestEventListener.ListenerSettings();
                listenerSettings.Keywords = LoggingEventSource.Keywords.JsonMessage;
                listenerSettings.FilterSpec = "Logger1:Error";
                listenerSettings.Level = EventLevel.Error;
                testListener.EnableEvents(listenerSettings);

                LogStuff(factory);

                VerifyEvents(testListener,
                    "E5JS",
                    "OuterScopeJsonStart",
                    "E4JS",
                    "OuterScopeJsonStop");
            }
        }

        [Fact]
        public void Logs_AsExpected_WithComplexLoggerSpec()
        {
            using (var testListener = new TestEventListener())
            {
                var factory = CreateLoggerFactory();

                var listenerSettings = new TestEventListener.ListenerSettings();
                listenerSettings.Keywords = LoggingEventSource.Keywords.JsonMessage;
                listenerSettings.FilterSpec = "Logger1:Warning;Logger2:Error";
                listenerSettings.Level = EventLevel.Verbose;
                testListener.EnableEvents(listenerSettings);

                LogStuff(factory);

                VerifyEvents(testListener,
                    "OuterScopeJsonStart",
                    "E4JS",
                    "E5JS",
                    "OuterScopeJsonStop");
            }
        }

        [Fact]
        public void Logs_Nothing_AfterDispose()
        {
            using (var testListener = new TestEventListener())
            {
                var factory = CreateLoggerFactory();

                var listenerSettings = new TestEventListener.ListenerSettings();
                listenerSettings.Keywords = LoggingEventSource.Keywords.JsonMessage;
                listenerSettings.FilterSpec = null;
                listenerSettings.Level = EventLevel.Verbose;
                testListener.EnableEvents(listenerSettings);

                var logger = factory.CreateLogger("Logger1");

                Dispose();

                logger.LogDebug(new EventId(1), "Logger1 Event1 Debug {intParam}", 1);

                VerifyEvents(testListener);
            }
        }

        private void LogStuff(ILoggerFactory factory)
        {
            var logger1 = factory.CreateLogger("Logger1");
            var logger2 = factory.CreateLogger("Logger2");
            var logger3 = factory.CreateLogger("Logger3");

            logger1.LogDebug(new EventId(1), "Logger1 Event1 Debug {intParam}", 1);
            logger2.LogTrace(new EventId(2), "Logger2 Event2 Trace {doubleParam} {timeParam} {doubleParam2}", DoubleParam1, TimeParam.ToString("O"), DoubleParam2);
            logger3.LogInformation(new EventId(3), "Logger3 Event3 Information {string1Param} {string2Param} {string3Param}", "foo", "bar", "baz");

            using (logger1.BeginScope("Outer scope {stringParam} {intParam} {doubleParam}", "scoped foo", 13, DoubleParam1))
            {
                logger1.LogError(new EventId(4, "ErrorEvent"), "Logger1 Event4 Error {stringParam} {guidParam}", "foo", GuidParam);

                logger2.LogCritical(new EventId(5), new Exception("oops", new Exception("inner oops")),
                    "Logger2 Event5 Critical {stringParam} {int1Param} {int2Param}", "bar", 23, 45);

                using (logger3.BeginScope("Inner scope {timeParam} {guidParam}", TimeParam, GuidParam))
                {
                    logger2.LogWarning(new EventId(6), "Logger2 Event6 Warning NoParams");
                }

                logger3.LogInformation(new EventId(7), "Logger3 Event7 Information {stringParam} {doubleParam} {intParam}", "inner scope closed", DoubleParam2, 37);
            }

            logger2.LogWarning(new EventId(8), "Logger2 Event8 Warning {stringParam} {timeParam}", "Outer scope closed", TimeParam.ToString("O"));
        }

        private static void VerifyEvents(TestEventListener eventListener, params string[] verifierIDs)
        {
            Assert.Collection(eventListener.Events, verifierIDs.Select(id => EventVerifiers[id]).ToArray());
        }

        private static void VerifySingleEvent(string eventJson, string loggerName, string eventName, int? eventId, string eventIdName, LogLevel? level, params string[] fragments)
        {
            Assert.True(eventJson.Contains(@"""__EVENT_NAME"":""" + eventName + @""""), $"Event name does not match. Expected {eventName}, event data is '{eventJson}'");
            Assert.True(eventJson.Contains(@"""LoggerName"":""" + loggerName + @""""), $"Logger name does not match. Expected {loggerName}, event data is '{eventJson}'");

            if (level.HasValue)
            {
                Assert.True(eventJson.Contains(@"""Level"":" + ((int)level.Value).ToString()), $"Log level does not match. Expected level {((int)level.Value).ToString()}, event data is '{eventJson}'");
            }

            if (eventId.HasValue)
            {
                Assert.True(eventJson.Contains(@"""EventId"":" + eventId.Value.ToString()), $"Event id does not match. Expected id {eventId.Value}, event data is '{eventJson}'");
            }

            if (eventIdName != null)
            {
                Assert.True(eventJson.Contains(@"""EventName"":""" + eventIdName), $"Event name does not match. Expected id {eventIdName}, event data is '{eventJson}'");
            }

            for (int i = 0; i < fragments.Length; i++)
            {
                Assert.True(eventJson.Contains(fragments[i]), $"Event data '{eventJson}' does not contain expected fragment {fragments[i]}");
            }
        }

        private class TestEventListener : EventListener
        {
            public class ListenerSettings
            {
                public EventKeywords Keywords;
                public EventLevel Level;
                public string FilterSpec;
            }

            private System.Diagnostics.Tracing.EventSource _loggingEventSource;

            private ListenerSettings _enableWhenCreated;

            public TestEventListener()
            {
                Events = new List<string>();
            }

            public List<string> Events;

            public void EnableEvents(ListenerSettings settings)
            {
                if (_loggingEventSource != null)
                {
                    EnableEvents(_loggingEventSource, settings.Level, settings.Keywords,  GetArguments(settings));
                }
                else
                {
                    _enableWhenCreated = settings;
                }
            }

            private static Dictionary<string, string> GetArguments(ListenerSettings settings)
            {
                var args = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(settings.FilterSpec))
                {
                    args["FilterSpecs"] = settings.FilterSpec;
                }

                return args;
            }

            protected override void OnEventSourceCreated(System.Diagnostics.Tracing.EventSource eventSource)
            {
                if (eventSource.Name == "Microsoft-Extensions-Logging")
                {
                    _loggingEventSource = eventSource;
                }

                if (_enableWhenCreated != null)
                {
                    EnableEvents(_loggingEventSource, _enableWhenCreated.Level, _enableWhenCreated.Keywords, GetArguments(_enableWhenCreated));
                    _enableWhenCreated = null;
                }
            }

            public override void Dispose()
            {
                if (_loggingEventSource != null)
                {
                    DisableEvents(_loggingEventSource);
                }
                base.Dispose();
            }

            protected override void OnEventWritten(EventWrittenEventArgs eventWrittenArgs)
            {
                // We cannot hold onto EventWrittenEventArgs for long because they are agressively reused.
                StringWriter sw = new StringWriter();
                JsonTextWriter writer = new JsonTextWriter(sw);
                writer.DateFormatString = "O";

                writer.WriteStartObject();

                writer.WritePropertyName("__EVENT_NAME");
                writer.WriteValue(eventWrittenArgs.EventName);

                string propertyName;
                for (int i = 0; i < eventWrittenArgs.PayloadNames.Count; i++)
                {
                    propertyName = eventWrittenArgs.PayloadNames[i];

                    writer.WritePropertyName(propertyName, true);
                    if (IsJsonProperty(eventWrittenArgs.EventId, i, propertyName))
                    {
                        writer.WriteRawValue(eventWrittenArgs.Payload[i].ToString());
                    }
                    else
                    {
                        if (eventWrittenArgs.Payload[i] == null || IsPrimitive(eventWrittenArgs.Payload[i].GetType()))
                        {
                            writer.WriteValue(eventWrittenArgs.Payload[i]);
                        }
                        else if (eventWrittenArgs.Payload[i] is IDictionary<string, object>)
                        {
                            var dictProperty = (IDictionary<string, object>)eventWrittenArgs.Payload[i];
                            // EventPayload claims to support IDictionary<string, object>, but you cannot get a KeyValuePair enumerator out of it
                            // So we need to serialize manually
                            writer.WriteStartObject();

                            for (int j = 0; j < dictProperty.Keys.Count; j++)
                            {
                                writer.WritePropertyName(dictProperty.Keys.ElementAt(j));
                                writer.WriteValue(dictProperty.Values.ElementAt(j));
                            }

                            writer.WriteEndObject();
                        }
                        else
                        {
                            string serializedComplexValue = JsonConvert.SerializeObject(eventWrittenArgs.Payload[i]);
                            writer.WriteRawValue(serializedComplexValue);
                        }
                    }
                }

                writer.WriteEndObject();
                Events.Add(sw.ToString());
            }

            private bool IsPrimitive(Type type)
            {
                return type == typeof(string) || type == typeof(int) || type == typeof(bool) || type == typeof(double);
            }

            private bool IsJsonProperty(int eventId, int propertyOrdinal, string propertyName)
            {
                // __payload_nn is an artificial property name that we are using in the .NET 4.5 case, where EventWrittenEventArgs does not carry payload name information
                if (!propertyName.StartsWith("__payload"))
                {
                    return propertyName.EndsWith("Json");
                }
                else
                {
                    // Refers to events as they are defined by LoggingEventSource
                    // MessageJson has ExceptionJson (#4) and ArgumentsJson (#5)
                    bool messageJsonProperties = eventId == 5 && (propertyOrdinal == 4 || propertyOrdinal == 5);
                    // ActivityJsonStart has ArgumentsJson (#3)
                    bool activityJsonStartProperty = eventId == 6 && propertyOrdinal == 3;
                    return messageJsonProperties || activityJsonStartProperty;
                }
            }
        }

        private static class EventTypes
        {
            public static readonly string FormattedMessage = "FormattedMessage";
            public static readonly string MessageJson = "MessageJson";
            public static readonly string Message = "Message";
            public static readonly string ActivityJsonStart = "ActivityJsonStart";
            public static readonly string ActivityJsonStop = "ActivityJsonStop";
            public static readonly string ActivityStart = "ActivityStart";
            public static readonly string ActivityStop = "ActivityStop";
        }

        private static readonly Guid GuidParam = new Guid("29bebd2c-7fa6-4e97-af68-b91fdaae24b6");
        private static readonly double DoubleParam1 = 3.1416;
        private static readonly double DoubleParam2 = -273.15;
        private static readonly DateTime TimeParam = new DateTime(2016, 5, 3, 19, 0, 0, DateTimeKind.Utc);

        private static readonly IDictionary<string, Action<string>> EventVerifiers = new Dictionary<string, Action<string>>
        {
            { "E1FM", (e) => VerifySingleEvent(e, "Logger1", EventTypes.FormattedMessage, 1, null, LogLevel.Debug,

                @"""FormattedMessage"":""Logger1 Event1 Debug 1""") },
            { "E1JS", (e) => VerifySingleEvent(e, "Logger1", EventTypes.MessageJson, 1, null, LogLevel.Debug,
                        @"""ArgumentsJson"":{""intParam"":""1""") },
            { "E1MSG", (e) => VerifySingleEvent(e, "Logger1", EventTypes.Message, 1, null, LogLevel.Debug,
                        @"{""Key"":""intParam"",""Value"":""1""}") },

            { "E2FM", (e) => VerifySingleEvent(e, "Logger2", EventTypes.FormattedMessage, 2, null, LogLevel.Trace,
                @"""FormattedMessage"":""Logger2 Event2 Trace " + DoubleParam1.ToString() + " " + GetEscapedForwardSlash(TimeParam.ToString("O")) + " " + DoubleParam2.ToString()) },
            { "E2JS", (e) => VerifySingleEvent(e, "Logger2", EventTypes.MessageJson, 2, null, LogLevel.Trace,
                        @"""ArgumentsJson"":{""doubleParam"":""" + DoubleParam1.ToString() + @""",""timeParam"":"""
                        + GetEscapedForwardSlash(TimeParam.ToString("O")) +@""",""doubleParam2"":""" + DoubleParam2.ToString()) },
            { "E2MSG", (e) => VerifySingleEvent(e, "Logger2", EventTypes.Message, 2, null, LogLevel.Trace,
                @"{""Key"":""doubleParam"",""Value"":""" + DoubleParam1.ToString() +@"""}",
                @"{""Key"":""timeParam"",""Value"":""" + GetEscapedForwardSlash(TimeParam.ToString("O")) +@"""}",
                @"{""Key"":""doubleParam2"",""Value"":""" + DoubleParam2.ToString() +@"""}") },

            { "E3FM", (e) => VerifySingleEvent(e, "Logger3", EventTypes.FormattedMessage, 3, null, LogLevel.Information,
                @"""FormattedMessage"":""Logger3 Event3 Information foo bar baz") },
            { "E3JS", (e) => VerifySingleEvent(e, "Logger3", EventTypes.MessageJson, 3, null, LogLevel.Information,
                        @"""ArgumentsJson"":{""string1Param"":""foo"",""string2Param"":""bar"",""string3Param"":""baz""") },
            { "E3MSG", (e) => VerifySingleEvent(e, "Logger3", EventTypes.Message, 3, null, LogLevel.Information,
                @"{""Key"":""string1Param"",""Value"":""foo""}",
                @"{""Key"":""string2Param"",""Value"":""bar""}",
                @"{""Key"":""string3Param"",""Value"":""baz""}") },

            { "E4FM", (e) => VerifySingleEvent(e, "Logger1", EventTypes.FormattedMessage, 4, "ErrorEvent", LogLevel.Error,

                @"""FormattedMessage"":""Logger1 Event4 Error foo " + GuidParam.ToString("D") + @"""") },

            { "E4JS", (e) => VerifySingleEvent(e, "Logger1", EventTypes.MessageJson, 4, "ErrorEvent", LogLevel.Error,
                @"""ArgumentsJson"":{""stringParam"":""foo"",""guidParam"":""" + GuidParam.ToString("D") + @"""") },

            { "E4MSG", (e) => VerifySingleEvent(e, "Logger1", EventTypes.Message, 4, "ErrorEvent", LogLevel.Error,
                @"{""Key"":""stringParam"",""Value"":""foo""}",
                @"{""Key"":""guidParam"",""Value"":""" + GuidParam.ToString("D") +@"""}") },

            { "E5FM", (e) => VerifySingleEvent(e, "Logger2", EventTypes.FormattedMessage, 5, null, LogLevel.Critical,
                @"""FormattedMessage"":""Logger2 Event5 Critical bar 23 45") },

            { "E5JS", (e) => VerifySingleEvent(e, "Logger2", EventTypes.MessageJson, 5, null, LogLevel.Critical,
                @"""ArgumentsJson"":{""stringParam"":""bar"",""int1Param"":""23"",""int2Param"":""45""",
                @"""ExceptionJson"":{""TypeName"":""System.Exception"",""Message"":""oops"",""HResult"":""-2146233088"",""VerboseMessage"":""System.Exception: oops ---\u003e System.Exception: inner oops") },

            { "E5MSG", (e) => VerifySingleEvent(e, "Logger2", EventTypes.Message, 5, null, LogLevel.Critical,
                 @"{""Key"":""stringParam"",""Value"":""bar""}",
                @"{""Key"":""int1Param"",""Value"":""23""}",
                @"{""Key"":""int2Param"",""Value"":""45""}",
                @"""Exception"":{""TypeName"":""System.Exception"",""Message"":""oops"",""HResult"":-2146233088,""VerboseMessage"":""System.Exception: oops ---> System.Exception: inner oops") },

            { "E6FM", (e) => VerifySingleEvent(e, "Logger2", EventTypes.FormattedMessage, 6, null, LogLevel.Warning,
                @"""FormattedMessage"":""Logger2 Event6 Warning NoParams""") },
            { "E6JS", (e) => VerifySingleEvent(e, "Logger2", EventTypes.MessageJson, 6, null, LogLevel.Warning) },
            { "E6MSG", (e) => VerifySingleEvent(e, "Logger2", EventTypes.Message, 6, null, LogLevel.Warning) },

            { "E7FM", (e) => VerifySingleEvent(e, "Logger3", EventTypes.FormattedMessage, 7, null, LogLevel.Information,
                @"""FormattedMessage"":""Logger3 Event7 Information inner scope closed " + DoubleParam2.ToString() + " 37") },
            { "E7JS", (e) => VerifySingleEvent(e, "Logger3", EventTypes.MessageJson, 7, null, LogLevel.Information,
                        @"""ArgumentsJson"":{""stringParam"":""inner scope closed"",""doubleParam"":""" + DoubleParam2.ToString() + @""",""intParam"":""37""") },
            { "E7MSG", (e) => VerifySingleEvent(e, "Logger3", EventTypes.Message, 7, null, LogLevel.Information,
                @"{""Key"":""stringParam"",""Value"":""inner scope closed""}",
                @"{""Key"":""doubleParam"",""Value"":""" + DoubleParam2.ToString() +@"""}",
                @"{""Key"":""intParam"",""Value"":""37""}") },

            { "E8FM", (e) => VerifySingleEvent(e, "Logger2", EventTypes.FormattedMessage, 8, null, LogLevel.Warning,
                @"""FormattedMessage"":""Logger2 Event8 Warning Outer scope closed " + GetEscapedForwardSlash(TimeParam.ToString("O"))) },
            { "E8JS", (e) => VerifySingleEvent(e, "Logger2", EventTypes.MessageJson, 8, null, LogLevel.Warning,
                        @"""ArgumentsJson"":{""stringParam"":""Outer scope closed"",""timeParam"":""" + GetEscapedForwardSlash(TimeParam.ToString("O"))) },

            { "E8MSG", (e) => VerifySingleEvent(e, "Logger2", EventTypes.Message, 8, null, LogLevel.Warning,
                @"{""Key"":""stringParam"",""Value"":""Outer scope closed""}",
                @"{""Key"":""timeParam"",""Value"":""" + GetEscapedForwardSlash(TimeParam.ToString("O")) +@"""}") },


            { "OuterScopeJsonStart", (e) => VerifySingleEvent(e, "Logger1", EventTypes.ActivityJsonStart, null, null, null,
                        @"""ArgumentsJson"":{""stringParam"":""scoped foo"",""intParam"":""13"",""doubleParam"":""" + DoubleParam1.ToString()) },
            { "OuterScopeJsonStop", (e) => VerifySingleEvent(e, "Logger1", EventTypes.ActivityJsonStop, null, null, null) },

            { "OuterScopeStart", (e) => VerifySingleEvent(e, "Logger1", EventTypes.ActivityStart, null, null, null) },
            { "OuterScopeStop", (e) => VerifySingleEvent(e, "Logger1", EventTypes.ActivityStop, null, null, null) },

            { "InnerScopeJsonStart", (e) => VerifySingleEvent(e, "Logger3", EventTypes.ActivityJsonStart, null, null, null,
                        @"""ArgumentsJson"":{""timeParam"":""" + GetEscapedForwardSlash(TimeParam.ToString()) + @""",""guidParam"":""" + GuidParam.ToString("D")) },
            { "InnerScopeJsonStop", (e) => VerifySingleEvent(e, "Logger3", EventTypes.ActivityJsonStop, null, null, null) },

            { "InnerScopeStart", (e) => VerifySingleEvent(e, "Logger3", EventTypes.ActivityStart, null, null, null) },
            { "InnerScopeStop", (e) => VerifySingleEvent(e, "Logger3", EventTypes.ActivityStop, null, null, null) },
        };

        // Temporary until: https://github.com/dotnet/corefx/issues/37192
        static string GetEscapedForwardSlash(string input)
        {
            var output = "";
            foreach (var c in input)
            {
                if (c == '/')
                {
                    output += "\\u" + ((int)c).ToString("x4");
                }
                else
                {
                    output += c;
                }
            }

            return output;
        }
    }
}
