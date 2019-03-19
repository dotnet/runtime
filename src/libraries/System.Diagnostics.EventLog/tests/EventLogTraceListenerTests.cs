// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class EventLogTraceListenerTests
    {
        public static IEnumerable<object[]> EventLogData()
        {
            yield return new object[] { null, string.Empty };
            yield return new object[] { new EventLog("logName", ".", "EventLogSource"), "EventLogSource" };
        }

        [Theory]
        [MemberData(nameof(EventLogData))]
        public void EventLogConstructor(EventLog eventLog, string expected)
        {
            using (var listener = new EventLogTraceListener(eventLog))
            {
                Assert.Equal(expected, listener.Name);
            }
        }

        [Fact]
        public void StringConstructor()
        {
            string source = "CustomSource";
            using (var listener = new EventLogTraceListener(source))
            {
                Assert.Equal(source, listener.EventLog.Source);
            }
        }

        [Fact]
        public void NamePropertyGetWithoutSet()
        {
            string source = "Source is my name";
            using (var listener = new EventLogTraceListener(source))
            {
                Assert.Equal(source, listener.Name);
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        public void NamePropertyGetMultipleTimesShouldNotChange()
        {
            string source = "Source multiple times";
            using (var listener = new EventLogTraceListener(source))
            {
                Assert.Equal(source, listener.Name);
                listener.EventLog.Source = "New source";
                Assert.NotEqual("New source", listener.Name);
                Assert.NotEqual(listener.EventLog.Source, listener.Name);
                Assert.Equal(source, listener.Name);
            }
        }

        [Fact]
        public void SetNamePropertyShouldAlwaysOverride()
        {
            string source = "Custom source";
            using (var listener = new EventLogTraceListener(source))
            {
                string newName = "New Name";
                listener.Name = newName;
                Assert.Equal(newName, listener.Name);
                Assert.NotEqual(listener.EventLog.Source, listener.Name);

                string anotherNewName = "This is another name";
                listener.Name = anotherNewName;
                Assert.Equal(anotherNewName, listener.Name);
                Assert.DoesNotContain(newName, listener.Name);
            }
        }

        [Fact]
        public void CloseSucceeds()
        {
            using (var listener = new EventLogTraceListener("test trace listener"))
                listener.Close(); // shouldn't fail.
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        public void WriteTest()
        {
            string log = "Write";
            string source = "Source" + nameof(WriteTest);
            try
            {
                EventLog.CreateEventSource(source, log);
                using (var listener = new EventLogTraceListener(source))
                {
                    string message = "A little message for the log";
                    listener.Write(message);
                    ValidateLastEntryMessage(listener, message, source);

                    message = "One more message for my friend";
                    listener.WriteLine(message);
                    ValidateLastEntryMessage(listener, message, source);
                }
            }
            finally
            {
                EventLog.DeleteEventSource(source);
                Helpers.RetryOnWin7(() => EventLog.Delete(log));
            }
        }

        [ConditionalTheory(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        [InlineData(TraceEventType.Information, EventLogEntryType.Information, ushort.MaxValue + 1, ushort.MaxValue)]
        [InlineData(TraceEventType.Error, EventLogEntryType.Error, ushort.MinValue - 1, ushort.MinValue)]
        [InlineData(TraceEventType.Warning, EventLogEntryType.Warning, ushort.MinValue, ushort.MinValue)]
        [InlineData(TraceEventType.Critical, EventLogEntryType.Error, 30, 30)]
        public void TraceEventTest(TraceEventType eventType, EventLogEntryType expectedType, int id, int expectedId)
        {
            string log = "TraceEvent";
            string source = "Source" + nameof(TraceEventTest);
            try
            {
                EventLog.CreateEventSource(source, log);
                using (var listener = new EventLogTraceListener(source))
                {
                    string message = "One simple message to trace";
                    listener.TraceEvent(null, source, eventType, id, message);
                    EventLogEntry eventLogEntry = ValidateLastEntryMessage(listener, message, source);

                    if (eventLogEntry != null)
                    {
                        Assert.Equal(expectedType, eventLogEntry.EntryType);
                        Assert.Equal(expectedId, eventLogEntry.InstanceId);
                    }
                }
            }
            finally
            {
                EventLog.DeleteEventSource(source);
                Helpers.RetryOnWin7(() => EventLog.Delete(log));
            }
        }

        public static IEnumerable<object[]> GetTraceDataParams_MemberData()
        {
            yield return new object[] { new object[] { "this is", "an array with", "multiple", "strings" } };
            yield return new object[] { new object[] { } };
            yield return new object[] { new object[] { "only one string" } };
            yield return new object[] { null };
            yield return new object[] { new object[] { "one string + null", null } };
        }

        [ConditionalTheory(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        [MemberData(nameof(GetTraceDataParams_MemberData))]
        public void TraceDataParamsData(object[] parameters)
        {
            string log = "TraceDataParamsData";
            string source = "Source" + nameof(TraceDataParamsData);
            try
            {
                EventLog.CreateEventSource(source, log);
                using (var listener = new EventLogTraceListener(source))
                {
                    listener.TraceData(null, source, TraceEventType.Information, 1, parameters);
                    string expectedMessage = GetExpectedMessage(parameters);
                    ValidateLastEntryMessage(listener, expectedMessage, source);
                }
            }
            finally
            {
                EventLog.DeleteEventSource(source);
                Helpers.RetryOnWin7(() => EventLog.Delete(log));
            }
        }

        public static IEnumerable<object[]> GetTraceEventFormat_MemberData()
        {
            yield return new object[] { "This is a format with 1 object {0}", new object[] { 123 } };
            yield return new object[] { "This is a weird {0}{1}{2} format that has multiple inputs {3}", new object[] { 0, 1, "two", "." } };
            yield return new object[] { "This is a weird {0}{1}{2} format that but args are null", null };
            if (!PlatformDetection.IsFullFramework)
            {
                // Full framework doesn't check for args.Length == 0 and if format is not null or empty, it calls string.Format
                yield return new object[] { "This is a weird {0}{1}{2} format that but args length is 0", new object[] { } };
            }

            yield return new object[] { string.Empty, new object[] { } };
            yield return new object[] { null, new object[] { } };
            yield return new object[] { string.Empty, new object[] { 2000, 101, "two", "string is a test." } };
            yield return new object[] { null, new object[] { "thanks, 00", "i like it...", 111 } };
        }

        [ConditionalTheory(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        [MemberData(nameof(GetTraceEventFormat_MemberData))]
        public void TraceEventFormatAndParams(string format, object[] parameters)
        {
            string log = "TraceEvent";
            string source = "Source" + nameof(TraceEventFormatAndParams);
            try
            {
                EventLog.CreateEventSource(source, log);
                using (var listener = new EventLogTraceListener(source))
                {
                    listener.TraceEvent(null, source, TraceEventType.Information, 1000, format, parameters);

                    if (parameters == null || parameters.Length == 0)
                    {
                        string[] messages;
                        if (string.IsNullOrEmpty(format))
                            messages = Array.Empty<string>();
                        else
                            messages = new string[] { format };

                        EventLogEntry eventLogEntry = listener.EventLog.Entries.LastOrDefault();
                        if (eventLogEntry != null)
                        {
                            Assert.All(messages, message => eventLogEntry.Message.Contains(message));
                        }
                    }
                    else if (string.IsNullOrEmpty(format))
                    {
                        string[] messages = new string[parameters.Length];
                        for (int i = 0; i < messages.Length; i++)
                        {
                            messages[i] = parameters[i].ToString();
                        }

                        EventLogEntry eventLogEntry = listener.EventLog.Entries.LastOrDefault();
                        if (eventLogEntry != null)
                        {
                            Assert.All(messages, message => eventLogEntry.Message.Contains(message));
                        }
                    }
                    else
                    {
                        string expectedMessage = string.Format(CultureInfo.InvariantCulture, format, parameters);
                        ValidateLastEntryMessage(listener, expectedMessage, source);
                    }
                }
            }
            finally
            {
                EventLog.DeleteEventSource(source);
                Helpers.RetryOnWin7(() => EventLog.Delete(log));
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        public void TraceWithFilters()
        {
            string log = "TraceWithFilters";
            string source = "Source" + nameof(TraceEventFormatAndParams);
            try
            {
                EventLog.CreateEventSource(source, log);
                using (var listener = new EventLogTraceListener(source))
                {
                    listener.Filter = new EventTypeFilter(SourceLevels.Critical);
                    listener.TraceData(null, source, TraceEventType.Information, 12, "string shouldn't be present");
                    EventLogEntry eventLogEntry = listener.EventLog.Entries.LastOrDefault();
                    if (eventLogEntry != null)
                        Assert.DoesNotContain("string shouldn't be present", eventLogEntry.Message);

                    listener.TraceData(null, source, TraceEventType.Information, 12, "string shouldn't be present", "neither should this");
                    eventLogEntry = listener.EventLog.Entries.LastOrDefault();
                    if (eventLogEntry != null)
                    {
                        Assert.DoesNotContain("string shouldn't be present", eventLogEntry.Message);
                        Assert.DoesNotContain("neither should this", eventLogEntry.Message);
                    }

                    listener.TraceEvent(null, source, TraceEventType.Information, 12, "trace an event casually", "one more", null);
                    eventLogEntry = listener.EventLog.Entries.LastOrDefault();
                    if (eventLogEntry != null)
                    {
                        Assert.DoesNotContain("trace an event casually", eventLogEntry.Message);
                        Assert.DoesNotContain("one more", eventLogEntry.Message);
                    }

                    listener.TraceEvent(null, source, TraceEventType.Information, 12, "i shouldn't be here");
                    eventLogEntry = listener.EventLog.Entries.LastOrDefault();
                    if (eventLogEntry != null)
                        Assert.DoesNotContain("i shouldn't be here", eventLogEntry.Message);
                }
            }
            finally
            {
                EventLog.DeleteEventSource(source);
                Helpers.RetryOnWin7(() => EventLog.Delete(log));
            }
        }

        private string GetExpectedMessage(params object[] data)
        {
            if (data == null)
                return string.Empty;

            var sb = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                if (i != 0)
                    sb.Append(", ");

                var obj = data[i];
                if (obj != null)
                    sb.Append(obj.ToString());
            }

            return sb.ToString();
        }

        private EventLogEntry ValidateLastEntryMessage(EventLogTraceListener listener, string message, string source)
        {
            EventLogEntry eventLogEntry = listener.EventLog.Entries.LastOrDefault();
            if (eventLogEntry != null)
            {
                Assert.Contains(message, eventLogEntry.Message);
                Assert.Equal(source, eventLogEntry.Source);
                Assert.StartsWith(Environment.MachineName.ToLowerInvariant(), eventLogEntry.MachineName.ToLowerInvariant());
            }

            return eventLogEntry;
        }
    }
}
