// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using Tracing.Tests.Common;
using System.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.Tracing.Session;

namespace Tracing.Tests
{
    [EventSource(Name = "EventPipeAndEtwEventSource")]
    class EventPipeAndEtwEventSource : EventSource
    {
        public class Keywords
        {
            public const EventKeywords EventPipeKeyword = (EventKeywords)0x1;
            public const EventKeywords EtwKeyword = (EventKeywords)0x2;
        }

        public static EventPipeAndEtwEventSource Log = new EventPipeAndEtwEventSource();

        private EventPipeAndEtwEventSource() : base(true) { }

        [Event(1, Keywords = Keywords.EventPipeKeyword)]
        public void Event1()
        {
            WriteEvent(1);
        }

        [Event(2, Keywords = Keywords.EtwKeyword)]
        public void Event2()
        {
            WriteEvent(2);
        }

        [Event(3, Keywords = Keywords.EventPipeKeyword | Keywords.EtwKeyword)]
        public void Event3()
        {
            WriteEvent(3);
        }
    }

    class EventResults
    {
        public int Event1Count { get; private set; }
        public int Event2Count { get; private set; }
        public int Event3Count { get; private set; }

        public void AddEvent(TraceEvent data)
        {
            if (data.ProviderName == EventPipeAndEtwEventSource.Log.Name)
            {
                if (data.EventName == "ManifestData")
                {
                    return;
                }

                if (data.EventName == "Event1")
                {
                    Event1Count++;
                }
                else if (data.EventName == "Event2")
                {
                    Event2Count++;
                }
                else if (data.EventName == "Event3")
                {
                    Event3Count++;
                }
                else
                {
                    Console.WriteLine($"\tEncountered unexpected event with name '{data.EventName}'.");
                    throw new InvalidOperationException();
                }
            }
        }

        public void Print(string header)
        {
            Console.WriteLine("\n\t" + header);
            Console.WriteLine($"\t\tEvent1Count: {Event1Count}");
            Console.WriteLine($"\t\tEvent2Count: {Event2Count}");
            Console.WriteLine($"\t\tEvent3Count: {Event3Count}\n\n");
        }
    }

    class EventPipeAndEtw
    {
        private static TraceConfiguration EventPipeGetConfig(EventSource eventSource, EventKeywords keywords, string outputFile="default.netperf")
        {
            // Setup the configuration values.
            uint circularBufferMB = 1024; // 1 GB
            uint level = 5;

            // Create a new instance of EventPipeConfiguration.
            TraceConfiguration config = new TraceConfiguration(outputFile, circularBufferMB);

            // Enable the provider.
            config.EnableProvider(eventSource.Name, (ulong)keywords, level);

            return config;
        }

        private static TraceEventSession EnableETW(EventSource eventSource, EventKeywords keywords, string outputFile="default.etl")
        {
            outputFile = Path.GetFullPath(outputFile);
            TraceEventSession session = new TraceEventSession("EventSourceEventPipeSession", outputFile);
            session.EnableProvider(eventSource.Name, TraceEventLevel.Verbose, (ulong)keywords, null);
            Thread.Sleep(200);  // Calls are async.
            return session;
        }

        private static void DisableETW(TraceEventSession traceEventSession)
        {
            traceEventSession.Flush();
            Thread.Sleep(1010);  // Calls are async.
            traceEventSession.Dispose();
        }

        private static void WriteAllEvents(EventPipeAndEtwEventSource eventSource)
        {
            Console.WriteLine("\tStart: Write events.");
            eventSource.Event1();
            eventSource.Event2();
            eventSource.Event3();
            Console.WriteLine("\tEnd: Writing events.\n");
        }

        private static void RoundOne(string[] args)
        {
            using (var netPerfFile = NetPerfFile.Create(args))
            {
                using (var etlFile = EtlFile.Create(args))
                {
                    Console.WriteLine("\tStart: Enable EventPipe.");
                    TraceControl.Enable(EventPipeGetConfig(EventPipeAndEtwEventSource.Log, EventPipeAndEtwEventSource.Keywords.EventPipeKeyword, netPerfFile.Path));
                    Console.WriteLine("\tEnd: Enable EventPipe.\n");

                    Console.WriteLine("\tStart: Enable ETW.");
                    TraceEventSession etwSession = EnableETW(EventPipeAndEtwEventSource.Log, EventPipeAndEtwEventSource.Keywords.EtwKeyword, etlFile.Path);
                    Console.WriteLine("\tEnd: Enable ETW.\n");

                    WriteAllEvents(EventPipeAndEtwEventSource.Log);

                    Console.WriteLine("\tStart: Disable ETW.");
                    DisableETW(etwSession);
                    Console.WriteLine("\tEnd: Disable ETW.\n");

                    WriteAllEvents(EventPipeAndEtwEventSource.Log);

                    Console.WriteLine("\tStart: Disable EventPipe.");
                    TraceControl.Disable();
                    Console.WriteLine("\tEnd: Disable EventPipe.\n");

                    Console.WriteLine("\tStart: Processing events from EventPipe file.");

                    EventResults eventPipeResults = new EventResults();
                    EventResults etwResults = new EventResults();

                    using (var trace = new TraceLog(TraceLog.CreateFromEventPipeDataFile(netPerfFile.Path)).Events.GetSource())
                    {
                        trace.Dynamic.All += delegate (TraceEvent data)
                        {
                            eventPipeResults.AddEvent(data);
                        };

                        trace.Process();
                    }

                    // Validate EventPipe results.
                    eventPipeResults.Print("EventPipe Results:");
                    Assert.Equal("EventPipeEvent1Count", eventPipeResults.Event1Count, 2);
                    Assert.Equal("EventPipeEvent2Count", eventPipeResults.Event2Count, 0);
                    Assert.Equal("EventPipeEvent3Count", eventPipeResults.Event3Count, 2);

                    Console.WriteLine("\tEnd: Processing events from EventPipe file.\n");

                    Console.WriteLine("\tStart: Processing events from ETW file.");

                    using (var trace = new ETWTraceEventSource(etlFile.Path))
                    {
                        trace.Dynamic.All += delegate (TraceEvent data)
                        {
                            etwResults.AddEvent(data);
                        };

                        trace.Process();
                    }

                    // Validate ETW results.
                    etwResults.Print("ETW Results:");
                    Assert.Equal("EventPipeEvent1Count", etwResults.Event1Count, 0);
                    Assert.Equal("EventPipeEvent2Count", etwResults.Event2Count, 1);
                    Assert.Equal("EventPipeEvent3Count", etwResults.Event3Count, 1);

                    Console.WriteLine("\tEnd: Processing events from ETW file.");
                }
            }
        }

        private static void RoundTwo(string[] args)
        {
            using (var netPerfFile = NetPerfFile.Create(args))
            {
                using (var etlFile = EtlFile.Create(args))
                {
                    Console.WriteLine("\tStart: Enable EventPipe.");
                    TraceControl.Enable(EventPipeGetConfig(EventPipeAndEtwEventSource.Log, EventPipeAndEtwEventSource.Keywords.EventPipeKeyword, netPerfFile.Path));
                    Console.WriteLine("\tEnd: Enable EventPipe.\n");

                    Console.WriteLine("\tStart: Enable ETW.");
                    TraceEventSession etwSession = EnableETW(EventPipeAndEtwEventSource.Log, EventPipeAndEtwEventSource.Keywords.EtwKeyword, etlFile.Path);
                    Console.WriteLine("\tEnd: Enable ETW.\n");

                    WriteAllEvents(EventPipeAndEtwEventSource.Log);

                    Console.WriteLine("\tStart: Disable EventPipe.");
                    TraceControl.Disable();
                    Console.WriteLine("\tEnd: Disable EventPipe.\n");

                    WriteAllEvents(EventPipeAndEtwEventSource.Log);

                    Console.WriteLine("\tStart: Disable ETW.");
                    DisableETW(etwSession);
                    Console.WriteLine("\tEnd: Disable ETW.\n");

                    Console.WriteLine("\tStart: Processing events from EventPipe file.");

                    EventResults eventPipeResults = new EventResults();
                    EventResults etwResults = new EventResults();

                    using (var trace = new TraceLog(TraceLog.CreateFromEventPipeDataFile(netPerfFile.Path)).Events.GetSource())
                    {
                        trace.Dynamic.All += delegate (TraceEvent data)
                        {
                            eventPipeResults.AddEvent(data);
                        };

                        trace.Process();
                    }

                    // Validate EventPipe results.
                    eventPipeResults.Print("EventPipe Results:");
                    Assert.Equal("EventPipeEvent1Count", eventPipeResults.Event1Count, 1);
                    Assert.Equal("EventPipeEvent2Count", eventPipeResults.Event2Count, 0);
                    Assert.Equal("EventPipeEvent3Count", eventPipeResults.Event3Count, 1);

                    Console.WriteLine("\tEnd: Processing events from EventPipe file.\n");

                    Console.WriteLine("\tStart: Processing events from ETW file.");

                    using (var trace = new ETWTraceEventSource(etlFile.Path))
                    {
                        trace.Dynamic.All += delegate (TraceEvent data)
                        {
                            etwResults.AddEvent(data);
                        };

                        trace.Process();
                    }

                    // Validate ETW results.
                    etwResults.Print("ETW Results:");
                    Assert.Equal("EventPipeEvent1Count", etwResults.Event1Count, 0);
                    Assert.Equal("EventPipeEvent2Count", etwResults.Event2Count, 2);
                    Assert.Equal("EventPipeEvent3Count", etwResults.Event3Count, 2);

                    Console.WriteLine("\tEnd: Processing events from ETW file.");
                }
            }
        }

        static int Main(string[] args)
        {
            // This test can only run with elevation.
            if (TraceEventSession.IsElevated() != true)
            {
                Console.WriteLine("Test skipped because the shell is not elevated.");
                return 100;
            }

            RoundOne(args);
            RoundTwo(args);
 

            return 100;
        }
    }
}
