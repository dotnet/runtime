// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Diagnostics.Tracing;
using Tracing.Tests.Common;
#if (UPDATED_NETCORE_CLIENT == true)
using Microsoft.Diagnostics.NETCore.Client;
#else
using Microsoft.Diagnostics.Tools.RuntimeClient;
#endif

namespace Tracing.Tests.ProviderValidation
{
    public sealed class MyEventSource : EventSource
    {
        private MyEventSource() {}
        public static MyEventSource Log = new MyEventSource();
        public void MyEvent() { WriteEvent(1, "MyEvent"); }
    }

    public class ProviderValidation
    {
        public static int Main(string[] args)
        {
            // This test validates that the rundown events are present
            // and that providers turned on that generate events are being written to
            // the stream.
#if (UPDATED_NETCORE_CLIENT == true)
            Console.WriteLine($"NONLEGACY NETCORE.CLIENT");
            var providers = new List<EventPipeProvider>()
            {
                new EventPipeProvider("MyEventSource", EventLevel.Verbose),
                new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Verbose)
            };

            var ret = IpcTraceTest.RunAndValidateEventCounts(_expectedEventCounts, _eventGeneratingAction, providers, 1024);
#else
            Console.WriteLine($"LEGACY TOOLS.RUNTIMECLIENT");
            var providers = new List<Provider>()
            {
                new Provider("MyEventSource"),
                new Provider("Microsoft-DotNETCore-SampleProfiler")
            };

            var config = new SessionConfiguration(circularBufferSizeMB: (uint)Math.Pow(2, 10), format: EventPipeSerializationFormat.NetTrace,  providers: providers);

            var ret = IpcTraceTest.RunAndValidateEventCounts(_expectedEventCounts, _eventGeneratingAction, config);
#endif
            if (ret < 0)
                return ret;
            else
                return 100;
        }

        private static Dictionary<string, ExpectedEventCount> _expectedEventCounts = new Dictionary<string, ExpectedEventCount>()
        {
            { "MyEventSource", new ExpectedEventCount(100_000, 0.30f) },
            { "Microsoft-Windows-DotNETRuntimeRundown", -1 },
            { "Microsoft-DotNETCore-SampleProfiler", -1 }
        };

        private static Action _eventGeneratingAction = () =>
        {
            for (int i = 0; i < 100_000; i++)
            {
                if (i % 10_000 == 0)
                    Logger.logger.Log($"Fired MyEvent {i:N0}/100,000 times...");
                MyEventSource.Log.MyEvent();
            }
        };
    }
}
