// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Profiler.Tests;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace EventPipeTests
{
    public class EventPipeTestEventSource : EventSource
    {
        public EventPipeTestEventSource() : base(EventSourceSettings.EtwSelfDescribingEventFormat)
        {

        }

        [Event(1)]
        public void MyEvent(int i)
        {
            WriteEvent(1, i);
        }

        [Event(2)]
        public void MyArrayEvent(char ch, int[] intArray, string str)
        {
            WriteEvent(2, ch, intArray, str);
        }

        [Event(3)]
        public void KeyValueEvent(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string>> Arguments)
        {
            WriteEvent(3, SourceName, EventName, Arguments);
        }
    }

    class EventPipe
    {
        static readonly Guid EventPipeReadingProfilerGuid = new Guid("9E7F78E2-B3BE-410B-AA8D-E210E4C757A4");

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                return RunTest();
            }

            return ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                          testName: "EventPipeReadBasic",
                                          profilerClsid: EventPipeReadingProfilerGuid);
        }

        public static int RunTest()
        {
            Console.WriteLine("Writing events to EventPipeTestEventSource");

            EventPipeTestEventSource myEventSource = new EventPipeTestEventSource();
            myEventSource.MyEvent(12);
            myEventSource.MyArrayEvent('d', Enumerable.Range(0, 120).ToArray(), "Hello from EventPipeTestEventSource!");

            List<KeyValuePair<string, string>> myList = new List<KeyValuePair<string, string>>()
            {
                KeyValuePair.Create("samplekey", "samplevalue" )
            };
            myEventSource.KeyValueEvent("Source", "Event", myList);

            return 100;
        }
    }
}
