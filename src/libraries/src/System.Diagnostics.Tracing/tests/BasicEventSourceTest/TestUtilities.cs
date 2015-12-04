using System.Diagnostics.Tracing;
using System.Diagnostics;
using Xunit;
using System;

namespace BasicEventSourceTests
{
    class TestUtilities
    {
        // Every test takes this lock to force them to run one at a time.
        public static readonly object EventSourceTestLock = new object();
        
        /// <summary>
        /// Confirms that there are no EventSources running.  
        /// </summary>
        /// <param name="message">Will be printed as part of the Assert</param>
        public static void CheckNoEventSourcesRunning(string message = "")
        {
            var eventSources = EventSource.GetSources();

            string eventSourceNames = "";
            foreach (var eventSource in EventSource.GetSources())
            {
                if (eventSource.Name != "System.Threading.Tasks.TplEventSource"
                   && eventSource.Name != "System.Diagnostics.Eventing.FrameworkEventSource")
                {
                    eventSourceNames += eventSource.Name + " ";
                }
            }

            Console.WriteLine(message);
            Assert.Equal("", eventSourceNames);
        }
    }
}
