// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Diagnostics.Tools.RuntimeClient;
using Microsoft.Diagnostics.Tracing;
using Tracing.Tests.Common;

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

            var providers = new List<Provider>()
            {
                new Provider("MyEventSource"),
                new Provider("Microsoft-DotNETCore-SampleProfiler")
            };

            var tests = new int[] { 4, 10 }
                .Select(x => (uint)Math.Pow(2, x))
                .Select(bufferSize => new SessionConfiguration(circularBufferSizeMB: bufferSize, format: EventPipeSerializationFormat.NetTrace,  providers: providers))
                .Select<SessionConfiguration, Func<int>>(configuration => () => IpcTraceTest.RunAndValidateEventCounts(_expectedEventCounts, _eventGeneratingAction, configuration));

            foreach (var test in tests)
            {
                var ret = test();
                if (ret < 0)
                    return ret;
            }

            return 100;
        }

        private static Dictionary<string, ExpectedEventCount> _expectedEventCounts = new Dictionary<string, ExpectedEventCount>()
        {
            { "MyEventSource", new ExpectedEventCount(1000, 0.30f) },
            { "Microsoft-Windows-DotNETRuntimeRundown", -1 },
            { "Microsoft-DotNETCore-SampleProfiler", -1 }
        };

        private static Action _eventGeneratingAction = () => 
        {
            foreach (var _ in Enumerable.Range(0,1000))
            {
                MyEventSource.Log.MyEvent();
            }
        };
    }
}