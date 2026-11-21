// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Collections.Generic;
using Xunit;

namespace BasicEventSourceTests
{
    /// <summary>
    /// Tests the user experience for common user errors.
    /// </summary>
    public partial class TestsUserErrors
    {
        /// <summary>
        /// Try to pass a user defined class (even with EventData)
        /// to a manifest based eventSource
        /// </summary>
        [Fact]
        public void Test_BadTypes_Manifest_UserClass()
        {
            var badEventSource = new BadEventSource_Bad_Type_UserClass();
            Test_BadTypes_Manifest(badEventSource);
        }

        private void Test_BadTypes_Manifest(EventSource source)
        {
            try
            {
                using (var listener = new EventListenerListener())
                {
                    var events = new List<Event>();
                    Debug.WriteLine("Adding delegate to onevent");
                    listener.OnEvent = delegate (Event data) { events.Add(data); };

                    listener.Start();
                    listener.EventSourceCommand(source.Name, EventCommand.Enable);

                    listener.Dispose();

                    // Confirm that we get exactly one event from this whole process, that has the error message we expect.
                    Assert.Equal(1, events.Count);
                    Event _event = events[0];
                    Assert.Equal("EventSourceMessage", _event.EventName);

                    string message = _event.PayloadString(0, "message");
                    // expected message: "ERROR: Exception in Command Processing for EventSource BadEventSource_Bad_Type_ByteArray: Unsupported type Byte[] in event source. "
                    Assert.Contains("Unsupported type", message);
                }
            }
            finally
            {
                source.Dispose();
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/105293")]
        [Fact]
        public void Test_Bad_WriteRelatedID_ParameterName()
        {
            Guid oldGuid;
            Guid newGuid = Guid.NewGuid();
            Guid newGuid2 = Guid.NewGuid();
            EventSource.SetCurrentThreadActivityId(newGuid, out oldGuid);

            using (var bes = new BadEventSource_IncorrectWriteRelatedActivityIDFirstParameter())
            using (var listener = new EventListenerListener())
            {
                var events = new List<Event>();
                listener.OnEvent = delegate (Event data) { events.Add(data); };
                listener.Start();
                listener.EventSourceCommand(bes.Name, EventCommand.Enable);

                bes.RelatedActivity(newGuid2, "Hello", 42, "AA", "BB");

                // Confirm that we get exactly one event from this whole process, that has the error message we expect.
                Assert.Equal(1, events.Count);
                Event _event = events[0];
                Assert.Equal("EventSourceMessage", _event.EventName);
                string message = _event.PayloadString(0, "message");
                // expected message: "EventSource expects the first parameter of the Event method to be of type Guid and to be named "relatedActivityId" when calling WriteEventWithRelatedActivityId."
                Assert.Contains("EventSource expects the first parameter of the Event method to be of type Guid and to be named \"relatedActivityId\" when calling WriteEventWithRelatedActivityId.", message);
            }
        }
    }

    public sealed class BadEventSource_IncorrectWriteRelatedActivityIDFirstParameter : EventSource
    {
        public void E2()
        {
            this.Write("sampleevent", new { a = "a string" });
        }

        [Event(7, Keywords = Keywords.Debug, Message = "Hello Message 7", Channel = EventChannel.Admin, Opcode = EventOpcode.Send)]

        public void RelatedActivity(Guid guid, string message, int value, string componentName, string instanceId)
        {
            WriteEventWithRelatedActivityId(7, guid, message, value, componentName, instanceId);
        }

        public class Keywords
        {
            public const EventKeywords Debug = (EventKeywords)0x0002;
        }
    }

    [EventData]
    public class UserClass
    {
        public int i;
    };

    /// <summary>
    /// A manifest based provider with a bad type (only supported in self describing)
    /// </summary>
    internal class BadEventSource_Bad_Type_UserClass : EventSource
    {
        public void Event1(UserClass myClass) { WriteEvent(1, myClass); }
    }
}
