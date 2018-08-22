#define REFLECTION

using System;
using System.IO;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Threading;
using Tracing.Tests.Common;

#if REFLECTION
using System.Reflection;
#endif

namespace Tracing.Tests
{
    public sealed class RuntimeEventSourceTest
    {
        static int Main(string[] args)
        {
            SimpleEventListener.EnableKeywords = (EventKeywords)0;
            using (SimpleEventListener noEventsListener = new SimpleEventListener("NoEvents"))
            {
                // Create an EventListener.
                SimpleEventListener.EnableKeywords = (EventKeywords)0x4c14fccbd;
                using (SimpleEventListener listener = new SimpleEventListener("Simple"))
                {
                    // Trigger the allocator task.
                    System.Threading.Tasks.Task.Run(new Action(Allocator));

                    // Wait for events.
                    Thread.Sleep(1000);

                    // Generate some GC events.
                    GC.Collect(2, GCCollectionMode.Forced);

                    // Wait for more events.
                    Thread.Sleep(1000);

                    // Ensure that we've seen some events.
                    Assert.True("listener.EventCount > 0", listener.EventCount > 0);
                }

                // Generate some more GC events.
                GC.Collect(2, GCCollectionMode.Forced);

                // Ensure that we've seen no events.
                Assert.True("noEventsListener.EventCount == 0", noEventsListener.EventCount == 0);
            }

            return 100;
        }

        private static void Allocator()
        {
            while (true)
            {
                for(int i=0; i<1000; i++)
                    GC.KeepAlive(new object());

                Thread.Sleep(10);
            }
        }
    }

    internal sealed class SimpleEventListener : EventListener
    {
        private string m_name;

        // Keep track of the set of keywords to be enabled.
        public static EventKeywords EnableKeywords
        {
            get;
            set;
        }

        public SimpleEventListener(string name)
        {
            m_name = name;
        }

        public int EventCount { get; private set; } = 0;

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name.Equals("Microsoft-Windows-DotNETRuntime"))
            {
                if (EnableKeywords != 0)
                {
                    // Enable events.
                    EnableEvents(eventSource, EventLevel.Verbose, EnableKeywords);
                }
                else
                {
                    // Enable the provider, but not any keywords, so we should get no events as long as no rundown occurs.
                    EnableEvents(eventSource, EventLevel.Critical, EnableKeywords);
                }
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            long osThreadId = -1;
            DateTime timeStamp;
#if REFLECTION
            PropertyInfo threadProperty = typeof(EventWrittenEventArgs).GetProperty("OSThreadId");
            MethodInfo threadMethod = threadProperty.GetGetMethod();
            osThreadId = (long)threadMethod.Invoke(eventData, null);
            PropertyInfo timeStampProperty = typeof(EventWrittenEventArgs).GetProperty("TimeStamp");
            MethodInfo timeStampMethod = timeStampProperty.GetGetMethod();
            timeStamp = (DateTime)timeStampMethod.Invoke(eventData, null);
#endif

            Console.WriteLine($"[{m_name}] ThreadID = {osThreadId} ID = {eventData.EventId} Name = {eventData.EventName}");
            Console.WriteLine($"TimeStamp: {timeStamp.ToLocalTime()}");
            Console.WriteLine($"LocalTime: {DateTime.Now}");
            Console.WriteLine($"Difference: {DateTime.UtcNow - timeStamp}");
            Assert.True("timeStamp < DateTime.UtcNow", timeStamp < DateTime.UtcNow);
            for (int i = 0; i < eventData.Payload.Count; i++)
            {
                string payloadString = eventData.Payload[i] != null ? eventData.Payload[i].ToString() : string.Empty;
                Console.WriteLine($"\tName = \"{eventData.PayloadNames[i]}\" Value = \"{payloadString}\"");
            }
            Console.WriteLine("\n");


            EventCount++;
        }
    }
}
