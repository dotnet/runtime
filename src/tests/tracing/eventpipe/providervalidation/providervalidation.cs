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
using Microsoft.Diagnostics.NETCore.Client;
using Xunit;

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
        [Fact]
        public static int TestEntryPoint()
        {
            // This test validates that the rundown events are present
            // and that providers turned on that generate events are being written to
            // the stream.

            var providers = new List<EventPipeProvider>()
            {
                new EventPipeProvider("MyEventSource", EventLevel.Verbose),
                new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Verbose)
            };

            bool enableRundown = TestLibrary.Utilities.IsNativeAot? false: true;

            Dictionary<string, ExpectedEventCount> _expectedEventCounts = TestLibrary.Utilities.IsNativeAot? _expectedEventCountsNativeAOT: _expectedEventCountsCoreCLR;
            var ret = IpcTraceTest.RunAndValidateEventCounts(_expectedEventCounts, _eventGeneratingAction, providers, 1024, enableRundownProvider: enableRundown);
            if (ret < 0)
                return ret;
            else
                return 100;
        }

        private static Dictionary<string, ExpectedEventCount> _expectedEventCountsCoreCLR = new Dictionary<string, ExpectedEventCount>()
        {
            { "MyEventSource", new ExpectedEventCount(100_000, 0.30f) },
            { "Microsoft-Windows-DotNETRuntimeRundown", -1 },
            { "Microsoft-DotNETCore-SampleProfiler", -1 }
        };

        private static Dictionary<string, ExpectedEventCount> _expectedEventCountsNativeAOT = new Dictionary<string, ExpectedEventCount>()
        {
            { "MyEventSource", 100_000 },
            { "Microsoft-DotNETCore-EventPipe", 1 }
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
