// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Diagnostics.Tracing;
using Tracing.Tests.Common;
using Xunit;

namespace Tracing.Tests
{
    [EventSource(Name = "SimpleEventSource")]
    class SimpleEventSource : EventSource
    {
        public SimpleEventSource() : base(true) { }

        [Event(1)]
        internal void MathResult(int x, int y, int z, string formula) { this.WriteEvent(1, x, y, z, formula); }

        [Event(2)]
        internal void DateTimeEvent(DateTime dateTime) => WriteEvent(2, dateTime);


        [NonEvent]  
        private unsafe void WriteEvent(int eventId, DateTime dateTime)  
        {
            EventData* desc = stackalloc EventData[1];
            long fileTime = dateTime.ToFileTimeUtc();
            desc[0].DataPointer = (IntPtr)(&fileTime);
            desc[0].Size = 8;
            WriteEventCore(eventId, 1, desc);
        }
    }
    
    internal sealed class SimpleEventListener : EventListener
    {
        private readonly string _targetSourceName;
        private readonly EventLevel _level;
        
        public int EventCount { get; private set; } = 0;
        public DateTime DateObserved { get; private set; } = DateTime.MinValue;

        public SimpleEventListener(string targetSourceName, EventLevel level)
        {
            // Store the arguments
            _targetSourceName = targetSourceName;
            _level = level;
        }
        
        protected override void OnEventSourceCreated(EventSource source)
        {
            if (source.Name.Equals(_targetSourceName))
            {
                EnableEvents(source, _level);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventId == 2)
            {
                DateObserved = (DateTime)eventData.Payload[0];
            }
            else
                EventCount++;
        }
    }

    public class EventPipeSmoke
    {
        private static int messageIterations = 100;
        private static readonly DateTime ThePast = DateTime.UtcNow;

        [Fact]
        public static int TestEntryPoint()
        {
            bool pass = false;
            using(var listener = new SimpleEventListener("SimpleEventSource", EventLevel.Verbose))
            {
                SimpleEventSource eventSource = new SimpleEventSource();
            
                Console.WriteLine("\tStart: Messaging.");
                // Send messages
                // Use random numbers and addition as a simple, human readble checksum
                Random generator = new Random();
                for(int i=0; i<messageIterations; i++)
                {
                    int x = generator.Next(1,1000);
                    int y = generator.Next(1,1000);
                    string formula = String.Format("{0} + {1} = {2}", x, y, x+y);
                    
                    eventSource.MathResult(x, y, x+y, formula);
                }
                eventSource.DateTimeEvent(ThePast);
                Console.WriteLine("\tEnd: Messaging.\n");
                
                Console.WriteLine($"\tEventListener received {listener.EventCount} event(s)\n");
                Console.WriteLine($"\tEventListener received {listener.DateObserved} vs {ThePast}\n");
                pass = listener.EventCount == messageIterations && ThePast == listener.DateObserved;
            }

            return pass ? 100 : -1;
        }
    }
}
