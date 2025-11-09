// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tracing.Tests.SessionFiltersValidation
{
    [EventSource(Name = "MyEventSource")]
    public sealed class MyEventSource : EventSource
    {
        private readonly List<string> _isEnabledResults = new List<string>();

        public IReadOnlyList<string> IsEnabledResults => _isEnabledResults;

        private MyEventSource()
        {
        }

        public static MyEventSource Log = new MyEventSource();

        [Event(1, Level = EventLevel.Informational)]
        public void Info1(string message)
        {
            WriteEvent(1, message);
        }

        [NonEvent]
        protected override void OnEventCommand(EventCommandEventArgs args)
        {
            base.OnEventCommand(args);
            // Record the result of IsEnabled for keyword 2 at Informational level
            bool isEnabled = this.IsEnabled(EventLevel.Informational, (EventKeywords)0x2);
            _isEnabledResults.Add($"IsEnabled(Level=Info,Keyword=2): {isEnabled}");
        }
    }

    public class SessionFiltersValidation
    {
        [Fact]
        public static int TestEntryPoint()
        {
            var providers1 = new List<EventPipeProvider>()
            {
                new EventPipeProvider("MyEventSource", EventLevel.Error, 0x1)
            };

            var providers2 = new List<EventPipeProvider>()
            {
                new EventPipeProvider("MyEventSource", EventLevel.Informational, 0x2)
            };

            DiagnosticsClient client = new DiagnosticsClient(Process.GetCurrentProcess().Id);
#if DIAGNOSTICS_RUNTIME
            if (OperatingSystem.IsAndroid())
                client = new DiagnosticsClient(new IpcEndpointConfig("127.0.0.1:9000", IpcEndpointConfig.TransportType.TcpSocket, IpcEndpointConfig.PortType.Listen));
#endif

            // Trigger initial event to create the EventSource
            MyEventSource.Log.Info1("Initial");

            // Clear any initial results
            MyEventSource.Log.IsEnabledResults.Clear();

            // Step 1: Start first session with Error level, keyword 1
            using (EventPipeSession session1 = client.StartEventPipeSession(providers1))
            {
                EventPipeEventSource source1 = new EventPipeEventSource(session1.EventStream);
                Task.Run(source1.Process);

                // Wait for the session to start
                Thread.Sleep(500);

                // Step 2: Start second session with Informational level, keyword 2
                using (EventPipeSession session2 = client.StartEventPipeSession(providers2))
                {
                    EventPipeEventSource source2 = new EventPipeEventSource(session2.EventStream);
                    Task.Run(source2.Process);

                    // Wait for the session to start
                    Thread.Sleep(500);

                    // Step 3: Stop the second session (Informational level, keyword 2)
                    session2.Stop();
                    Thread.Sleep(500);
                }

                // Step 4: Stop the first session (Error level, keyword 1)
                session1.Stop();
                Thread.Sleep(500);
            }

            // Validate the results
            // Expected results:
            // 0: IsEnabled(Level=Info,Keyword=2): False  (after session1 starts with Error/keyword 1)
            // 1: IsEnabled(Level=Info,Keyword=2): True   (after session2 starts with Info/keyword 2)
            // 2: IsEnabled(Level=Info,Keyword=2): False  (after session2 stops, only session1 remains with Error/keyword 1)
            // 3: IsEnabled(Level=Info,Keyword=2): False  (after session1 stops, no sessions)

            var results = MyEventSource.Log.IsEnabledResults;

            if (results.Count != 4)
            {
                Console.WriteLine($"Test failed: Expected 4 OnEventCommand callbacks, got {results.Count}");
                for (int i = 0; i < results.Count; i++)
                {
                    Console.WriteLine($"  [{i}] {results[i]}");
                }
                return -1;
            }

            bool[] expectedResults = { false, true, false, false };
            string[] expectedStrings = {
                "IsEnabled(Level=Info,Keyword=2): False",
                "IsEnabled(Level=Info,Keyword=2): True",
                "IsEnabled(Level=Info,Keyword=2): False",
                "IsEnabled(Level=Info,Keyword=2): False"
            };

            for (int i = 0; i < 4; i++)
            {
                if (results[i] != expectedStrings[i])
                {
                    Console.WriteLine($"Test failed at step {i + 1}:");
                    Console.WriteLine($"  Expected: {expectedStrings[i]}");
                    Console.WriteLine($"  Actual:   {results[i]}");
                    Console.WriteLine("\nAll results:");
                    for (int j = 0; j < results.Count; j++)
                    {
                        Console.WriteLine($"  [{j}] {results[j]}");
                    }
                    return -1;
                }
            }

            Console.WriteLine("Test passed: All IsEnabled results matched expected values");
            return 100;
        }
    }
}
