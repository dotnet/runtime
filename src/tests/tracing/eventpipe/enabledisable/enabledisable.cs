// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;

namespace Tracing.Tests.EnableDisableValidation
{
    [EventSource(Name = "Local.TestEventSource")]
    public sealed class TestEventSource : EventSource
    {
        private int _disables;
        private int _enables;

        public int Enables => _enables;
        public int Disables => _disables;

        private TestEventSource()
        {
        }

        public static TestEventSource Log = new TestEventSource();

        [Event(1)]
        public void TestEvent()
        {
            WriteEvent(1);
        }

        [NonEvent]
        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            base.OnEventCommand(command);

            if (command.Command == EventCommand.Enable)
            {
                Interlocked.Increment(ref _enables);
            }
            else if (command.Command == EventCommand.Disable)
            {
                Interlocked.Increment(ref _disables);
            }
        }
    }

    public class EnableDisableValidation
    {
        [Fact]
        public static int TestEntryPoint()
        {
            // There is a potential deadlock because EventPipeEventSource uses ConcurrentDictionary, which
            // triggers loading the CDSCollectionETWBCLProvider EventSource, and registering the provider
            // can deadlock with the writing thread. Force it to be created now.
            ConcurrentDictionary<int, int> cd = new ConcurrentDictionary<int, int>(Environment.ProcessorCount, 0);
            if (cd.Count > 0)
            {
                throw new Exception("This shouldn't ever happen");
            }

            var providers = new List<EventPipeProvider>()
            {
                new EventPipeProvider("Local.TestEventSource", EventLevel.Verbose)
            };

            DiagnosticsClient client = new DiagnosticsClient(Process.GetCurrentProcess().Id);
#if DIAGNOSTICS_RUNTIME
            if (OperatingSystem.IsAndroid())
                client = new DiagnosticsClient(new IpcEndpointConfig("127.0.0.1:9000", IpcEndpointConfig.TransportType.TcpSocket, IpcEndpointConfig.PortType.Listen));
#endif
            using (EventPipeSession session1 = client.StartEventPipeSession(providers))
            {
                EventPipeEventSource source1 = new EventPipeEventSource(session1.EventStream);

                using (EventPipeSession session2 = client.StartEventPipeSession(providers))
                {
                    EventPipeEventSource source2 = new EventPipeEventSource(session2.EventStream);

                    using (EventPipeSession session3 = client.StartEventPipeSession(providers))
                    {
                        EventPipeEventSource source3 = new EventPipeEventSource(session3.EventStream);
                        for (int i = 0; i < 10; ++i)
                        {
                            TestEventSource.Log.TestEvent();
                        }

                        StopSession(session3, source3);
                    }

                    StopSession(session2, source2);
                }

                StopSession(session1, source1);
            }

            if (TestEventSource.Log.Enables > 0 &&
                TestEventSource.Log.Enables == TestEventSource.Log.Disables)
            {
                return 100;
            }

            Console.WriteLine($"Test failed, enables={TestEventSource.Log.Enables} disables={TestEventSource.Log.Disables}");
            return -1;
        }

        private static void StopSession(EventPipeSession session, EventPipeEventSource source)
        {
            source.Dynamic.All += (TraceEvent traceEvent) =>
            {
            };

            Task.Run(source.Process);
            session.Stop();
        }
    }
}
