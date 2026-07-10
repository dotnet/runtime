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
using TestLibrary;

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
        [ActiveIssue(" needs triage ", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoAnyAOT))]
        [ActiveIssue(" needs triage ", TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        [ActiveIssue("Can't find file dotnet-diagnostic-{pid}-*-socket", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime), nameof(PlatformDetection.IsRiscv64Process))]
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

    [EventSource(Name = "Local.FilterRegressionEventSource")]
    public sealed class FilterRegressionEventSource : EventSource
    {
        private readonly ConcurrentQueue<bool> _isEnabledOnDisable = new ConcurrentQueue<bool>();

        public bool[] IsEnabledOnDisableResults => _isEnabledOnDisable.ToArray();

        private FilterRegressionEventSource() { }

        public static FilterRegressionEventSource Log = new FilterRegressionEventSource();

        [Event(1)]
        public void TestEvent() { WriteEvent(1); }

        [NonEvent]
        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            base.OnEventCommand(command);
            if (command.Command == EventCommand.Disable)
            {
                _isEnabledOnDisable.Enqueue(this.IsEnabled(EventLevel.Informational, (EventKeywords)0x2));
            }
        }
    }

    public class KeywordLevelFilterValidation
    {
        [ActiveIssue(" needs triage ", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoAnyAOT))]
        [ActiveIssue(" needs triage ", TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        [ActiveIssue("Can't find file dotnet-diagnostic-{pid}-*-socket", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime), nameof(PlatformDetection.IsRiscv64Process))]
        [Fact]
        public static int TestEntryPoint()
        {
            // Verify that when one of multiple EventPipe sessions is stopped, the global keyword and level
            // filters are recomputed to reflect only the remaining active sessions.
            //
            // Session 1 tracks keyword=0x2 at Informational level (the broader filter).
            // Session 2 tracks keyword=0x1 at Error level (the narrower filter).
            // After session 1 stops, only session 2 is active, so IsEnabled(Informational, keyword=0x2)
            // must return false because neither the level (Error < Informational) nor keyword (0x1 & 0x2 == 0)
            // matches session 2's configuration.

            // Force the EventSource to register with EventPipe before starting any sessions. If the
            // type initializer has not run by the time a session starts, the provider callback won't fire.
            FilterRegressionEventSource.Log.TestEvent();

            var providers1 = new List<EventPipeProvider>
            {
                new EventPipeProvider("Local.FilterRegressionEventSource", EventLevel.Informational, 0x2)
            };
            var providers2 = new List<EventPipeProvider>
            {
                new EventPipeProvider("Local.FilterRegressionEventSource", EventLevel.Error, 0x1)
            };

            DiagnosticsClient client = new DiagnosticsClient(Process.GetCurrentProcess().Id);
#if DIAGNOSTICS_RUNTIME
            if (OperatingSystem.IsAndroid())
                client = new DiagnosticsClient(new IpcEndpointConfig("127.0.0.1:9000", IpcEndpointConfig.TransportType.TcpSocket, IpcEndpointConfig.PortType.Listen));
#endif

            EventPipeSession session1 = client.StartEventPipeSession(providers1);
            EventPipeEventSource source1 = new EventPipeEventSource(session1.EventStream);

            EventPipeSession session2 = client.StartEventPipeSession(providers2);
            EventPipeEventSource source2 = new EventPipeEventSource(session2.EventStream);

            // Stop session1 (Informational, keyword=0x2) while session2 (Error, keyword=0x1) is still active.
            // The global filter must be recomputed to only reflect session2's configuration.
            StopSession(session1, source1);
            StopSession(session2, source2);

            session1.Dispose();
            session2.Dispose();

            bool[] results = FilterRegressionEventSource.Log.IsEnabledOnDisableResults;

            if (results.Length < 2)
            {
                Console.WriteLine($"Test failed: expected at least 2 Disable callbacks, got {results.Length}");
                return -1;
            }

            // After session1 stops with session2 still active, the filter should reflect session2 only.
            // IsEnabled(Informational, keyword=0x2) must be false: Error level (2) < Informational (4).
            if (results[0])
            {
                Console.WriteLine("Test failed: IsEnabled(Informational, keyword=0x2) should be false after the Informational session stops while the Error session remains active");
                return -1;
            }

            // After all sessions stop, IsEnabled must still be false.
            if (results[1])
            {
                Console.WriteLine("Test failed: IsEnabled(Informational, keyword=0x2) should be false after all sessions stop");
                return -1;
            }

            return 100;
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
