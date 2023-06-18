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

namespace Tracing.Tests.SimpleProviderValidation
{
    public sealed class MyEventSource : EventSource
    {
        private MyEventSource() {}
        public static MyEventSource Log = new MyEventSource();
        public void MyEvent() { WriteEvent(1, "MyEvent"); }
    }

    public class ProviderValidation
    {
        public static int Main()
        {
            // This test is meant to validate NativeAOT EventPipe implementation and is meant to run in regular CI
            // Its currently not enabled in NativeAOT runs and the below issue tracks the work
            // https://github.com/dotnet/runtime/issues/84701

            var providers = new List<EventPipeProvider>()
            {
                new EventPipeProvider("MyEventSource", EventLevel.Verbose),
                new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Verbose),
                //GCKeyword (0x1): 0b1
                new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Verbose, 0xFFFF)
            };

            var ret = IpcTraceTest.RunAndValidateEventCounts(_expectedEventCounts, _eventGeneratingAction, providers, 1024, enableRundownProvider:false);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            if (ret < 0)
                return ret;
            else
                return 100;
        }

        private static Dictionary<string, ExpectedEventCount> _expectedEventCounts = new Dictionary<string, ExpectedEventCount>()
        {
            { "MyEventSource", 100 },
            { "Microsoft-DotNETCore-EventPipe", 1},
            { "Microsoft-Windows-DotNETRuntime", 1}
        };

        private static Action _eventGeneratingAction = () => 
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            List<Foo> list = new List<Foo>();
            for (int i = 0; i < 100; i++)
            {
                list.Add(new Foo(i));
                if (i % 10 == 0)
                    Logger.logger.Log($"Fired MyEvent {i:N0}/100,000 times...");
                MyEventSource.Log.MyEvent();
            }
            int rndValue = new Random().Next(0, 100);
            Console.WriteLine($"{list[rndValue].IValue}-{list[rndValue].SValue}");
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        };
    }
    public class Foo
    {
        public int IValue { get; set; }
        public string SValue { get; set; }
        public Foo(int value)
        {
            IValue = value;
            SValue = value.ToString();
        }

        ~Foo()
        {
            Console.WriteLine("In Destructor");
        }
    }

}
