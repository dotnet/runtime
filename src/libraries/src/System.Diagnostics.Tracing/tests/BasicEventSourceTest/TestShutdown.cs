// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if USE_MDT_EVENTSOURCE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif

namespace BasicEventSourceTests
{
    public class TestShutdown
    {
        // TODO: Depends on desktop APIs (AppDomainSetup and Evidence).
#if FALSE
        /// <summary>
        /// Test for manifest event being logged during AD/Process shutdown during EventSource Dispose(bool) method.
        /// </summary>
        [Fact]
        public void Test_EventSource_ShutdownManifest()
        {
            TestUtilities.CheckNoEventSourcesRunning("Start");
            Debug.WriteLine("Logging more than 1MB of events...");
            OverflowCircularBufferTest();
            Debug.WriteLine("Success...");
            TestUtilities.CheckNoEventSourcesRunning("Stop");
        }

        public void OverflowCircularBufferTest()
        {
            string dataFileName = "OverflowData.etl";

            var sessionName = Path.GetFileNameWithoutExtension(dataFileName) + "Session";
            var logger = ADShutdownEventSourceTester.ADShutdownEventSource.Log;

            // Normalize to a full path name.  
            dataFileName = Path.GetFullPath(dataFileName);
            Debug.WriteLine(String.Format("Creating data file {0}", dataFileName));

            TraceEventSession session = null;
            using (session = new TraceEventSession(sessionName, dataFileName))
            {
                // Turn on the eventSource given its name.
                session.CircularBufferMB = 1;
                session.BufferSizeMB = 1;
                session.EnableProvider(logger.Name);

                Thread.Sleep(100);  // Enabling is async. Wait a bit.

                // Generate events for all the tests, surrounded by events that tell us we are starting a test.  
                var info = new AppDomainSetup { ApplicationBase = AppDomain.CurrentDomain.BaseDirectory };
                var appDomain =
                AppDomain.CreateDomain("TestShutdownAD", new Evidence(), info);
                byte[] b = new byte[1000];
                var tester = (ADShutdownEventSourceTester)appDomain
                .CreateInstanceAndUnwrap(
                    typeof(TestShutdown).Assembly.GetName().Name,
                    typeof(ADShutdownEventSourceTester).FullName);

                for (int i = 0; i < 1500; i++)
                {
                    tester.LogBytes(b);
                }
                // Unload the AD and expect the manifest to be logged.
                AppDomain.Unload(appDomain);
                session.Flush();
            }

            // Parse ETL and search for manifest events
            using (var traceEventSource = new ETWTraceEventSource(dataFileName))
            {
                bool hasManifestEvents = false;
                Action<TraceEvent> onEvent = delegate (TraceEvent data)
                {
                    // Check for manifest events. 
                    if ((int)data.ID == 0xFFFE)
                        hasManifestEvents = true;
                };

                // Parse all the events as best we can, and also send unhandled events there as well.  
                traceEventSource.Dynamic.All += onEvent;
                traceEventSource.UnhandledEvent += onEvent;
                traceEventSource.Process();

                // Expect at least one manifest event.
                Assert.True(hasManifestEvents);
            }

            logger.Dispose();
        }
#endif

        public sealed class ADShutdownEventSourceTester //: MarshalByRefObject
        {
            public void LogBytes(byte[] b)
            {
                ADShutdownEventSource.Log.MyEvent(b);
            }

            public class ADShutdownEventSource : EventSource
            {
                public static readonly ADShutdownEventSource Log = new ADShutdownEventSource();

                [Event(1)]
                public void MyEvent(byte[] b)
                {
                    this.WriteEvent(1, b);
                }
            }
        }
    }
}
