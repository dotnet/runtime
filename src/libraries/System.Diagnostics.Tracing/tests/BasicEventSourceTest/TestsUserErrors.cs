// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if USE_MDT_EVENTSOURCE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif
using Xunit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;

namespace BasicEventSourceTests
{
    /// <summary>
    /// Tests the user experience for common user errors.  
    /// </summary>

    public class TestsUserErrors
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

                    listener.EventSourceCommand(source.Name, EventCommand.Enable);

                    listener.Dispose();

                    // Confirm that we get exactly one event from this whole process, that has the error message we expect.  
                    Assert.Equal(events.Count, 1);
                    Event _event = events[0];
                    Assert.Equal("EventSourceMessage", _event.EventName);

                    // Check the exception text if not ProjectN.
                    if (!PlatformDetection.IsNetNative)
                    {
                        string message = _event.PayloadString(0, "message");
                        // expected message: "ERROR: Exception in Command Processing for EventSource BadEventSource_Bad_Type_ByteArray: Unsupported type Byte[] in event source. "
                        Assert.True(Regex.IsMatch(message, "Unsupported type"));
                    }
                }
            }
            finally
            {
                source.Dispose();
            }
        }

        /// <summary>
        /// Test the 
        /// </summary>
        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Depends on inspecting IL at runtime.")]
        public void Test_BadEventSource_MismatchedIds()
        {
#if USE_ETW // TODO: Enable when TraceEvent is available on CoreCLR. GitHub issue #4864.
            // We expect only one session to be on when running the test but if a ETW session was left
            // hanging, it will confuse the EventListener tests.   
            EtwListener.EnsureStopped();
#endif // USE_ETW

            TestUtilities.CheckNoEventSourcesRunning("Start");
            var onStartups = new bool[] { false, true };
            
                    var listenerGenerators = new Func<Listener>[]
                    {
                        () => new EventListenerListener(),
#if USE_ETW // TODO: Enable when TraceEvent is available on CoreCLR. GitHub issue #4864.
                        () => new EtwListener()
#endif // USE_ETW
                    };

                    var settings = new EventSourceSettings[] { EventSourceSettings.Default, EventSourceSettings.EtwSelfDescribingEventFormat };

                    // For every interesting combination, run the test and see that we get a nice failure message.  
                    foreach (bool onStartup in onStartups)
                    {
                        foreach (Func<Listener> listenerGenerator in listenerGenerators)
                        {
                            foreach (EventSourceSettings setting in settings)
                            {
                                Test_Bad_EventSource_Startup(onStartup, listenerGenerator(), setting);
                            }
                        }
            }

            TestUtilities.CheckNoEventSourcesRunning("Stop");
        }

        /// <summary>
        /// A helper that can run the test under a variety of conditions
        /// * Whether the eventSource is enabled at startup
        /// * Whether the listener is ETW or an EventListern
        /// * Whether the ETW output is self describing or not.  
        /// </summary>
        private void Test_Bad_EventSource_Startup(bool onStartup, Listener listener, EventSourceSettings settings)
        {
            var eventSourceName = typeof(BadEventSource_MismatchedIds).Name;
            Debug.WriteLine("***** Test_BadEventSource_Startup(OnStartUp: " + onStartup + " Listener: " + listener + " Settings: " + settings + ")");

            // Activate the source before the source exists (if told to).  
            if (onStartup)
                listener.EventSourceCommand(eventSourceName, EventCommand.Enable);

            var events = new List<Event>();
            listener.OnEvent = delegate (Event data) { events.Add(data); };

            using (var source = new BadEventSource_MismatchedIds(settings))
            {
                Assert.Equal(eventSourceName, source.Name);
                // activate the source after the source exists (if told to).  
                if (!onStartup)
                    listener.EventSourceCommand(eventSourceName, EventCommand.Enable);
                source.Event1(1);       // Try to send something.  
            }
            listener.Dispose();

            // Confirm that we get exactly one event from this whole process, that has the error message we expect.  
            Assert.Equal(events.Count, 1);
            Event _event = events[0];
            Assert.Equal("EventSourceMessage", _event.EventName);
            string message = _event.PayloadString(0, "message");
            Debug.WriteLine(String.Format("Message=\"{0}\"", message));
            // expected message: "ERROR: Exception in Command Processing for EventSource BadEventSource_MismatchedIds: Event Event2 was assigned event ID 2 but 1 was passed to WriteEvent. "
            if (!PlatformDetection.IsFullFramework) // Full framework has typo
                Assert.True(Regex.IsMatch(message, "Event Event2 was assigned event ID 2 but 1 was passed to WriteEvent"));
        }

        [Fact]
        public void Test_Bad_WriteRelatedID_ParameterName()
        {
#if true
            Debug.WriteLine("Test disabled because the fix it tests is not in CoreCLR yet.");
#else
            BadEventSource_IncorrectWriteRelatedActivityIDFirstParameter bes = null;
            EventListenerListener listener = null;
            try
            {
                Guid oldGuid;
                Guid newGuid = Guid.NewGuid();
                Guid newGuid2 = Guid.NewGuid();
                EventSource.SetCurrentThreadActivityId(newGuid, out oldGuid);

                bes = new BadEventSource_IncorrectWriteRelatedActivityIDFirstParameter();
                
                using (var listener = new EventListenerListener())
                {
                    var events = new List<Event>();
                    listener.OnEvent = delegate (Event data) { events.Add(data); };

                    listener.EventSourceCommand(bes.Name, EventCommand.Enable);

                    bes.RelatedActivity(newGuid2, "Hello", 42, "AA", "BB");

                    // Confirm that we get exactly one event from this whole process, that has the error message we expect.  
                    Assert.Equal(events.Count, 1);
                    Event _event = events[0];
                    Assert.Equal("EventSourceMessage", _event.EventName);
                    string message = _event.PayloadString(0, "message");
                    // expected message: "EventSource expects the first parameter of the Event method to be of type Guid and to be named "relatedActivityId" when calling WriteEventWithRelatedActivityId."
                    Assert.True(Regex.IsMatch(message, "EventSource expects the first parameter of the Event method to be of type Guid and to be named \"relatedActivityId\" when calling WriteEventWithRelatedActivityId."));
                }
            }
            finally
            {
                if (bes != null)
                {
                    bes.Dispose();
                }

                if (listener != null)
                {
                    listener.Dispose();
                }
            }
#endif
        }
    }

    /// <summary>
    /// This EventSource has a common user error, and we want to make sure EventSource
    /// gives a reasonable experience in that case. 
    /// </summary>
    internal class BadEventSource_MismatchedIds : EventSource
    {
        public BadEventSource_MismatchedIds(EventSourceSettings settings) : base(settings) { }
        public void Event1(int arg) { WriteEvent(1, arg); }
        // Error Used the same event ID for this event. 
        public void Event2(int arg) { WriteEvent(1, arg); }
    }

    /// <summary>
    /// A manifest based provider with a bad type byte[]
    /// </summary>
    internal class BadEventSource_Bad_Type_ByteArray : EventSource
    {
        public void Event1(byte[] myArray) { WriteEvent(1, myArray); }
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
