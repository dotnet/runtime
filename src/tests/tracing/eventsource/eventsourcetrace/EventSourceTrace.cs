// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Collections.Generic;
using Tracing.Tests.Common;
using System.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace Tracing.Tests
{
    [EventSource(Name = "SimpleEventSource")]
    class SimpleEventSource : EventSource
    {
        public SimpleEventSource() : base(true) { }

        [Event(1)]
        internal void MathResult(int x, int y, int z, string formula) { this.WriteEvent(1, x, y, z, formula); }
    }

    class EventSourceTrace
    {
        private static int messageIterations = 10000;

        public static TraceConfiguration GetConfig(EventSource eventSource, string outputFile="default.netperf")
        {
            // Setup the configuration values.
            uint circularBufferMB = 1024; // 1 GB
            uint level = 5;//(uint)EventLevel.Informational;
            TimeSpan profSampleDelay = TimeSpan.FromMilliseconds(1);

            // Create a new instance of EventPipeConfiguration.
            TraceConfiguration config = new TraceConfiguration(outputFile, circularBufferMB);
            // Setup the provider values.
            // Public provider.
            string providerName = eventSource.Name;
            UInt64 keywords = 0xffffffffffffffff;

            // Enable the provider.
            config.EnableProvider(providerName, keywords, level);

            // Set the sampling rate.
            config.SetSamplingRate(profSampleDelay);

            return config;
        }

        static int Main(string[] args)
        {
            bool pass = true;

            using (SimpleEventSource eventSource = new SimpleEventSource())
            {
                using (var netPerfFile = NetPerfFile.Create(args))
                {
                    Console.WriteLine("\tStart: Enable tracing.");
                    TraceControl.Enable(GetConfig(eventSource, netPerfFile.Path));
                    Console.WriteLine("\tEnd: Enable tracing.\n");

                    Console.WriteLine("\tStart: Messaging.");
                    // Send messages
                    // Use random numbers and addition as a simple, human readble checksum
                    Random generator = new Random();
                    for (int i = 0; i < messageIterations; i++)
                    {
                        int x = generator.Next(1, 1000);
                        int y = generator.Next(1, 1000);
                        string formula = String.Format("{0} + {1} = {2}", x, y, x + y);

                        eventSource.MathResult(x, y, x + y, formula);
                    }
                    Console.WriteLine("\tEnd: Messaging.\n");

                    Console.WriteLine("\tStart: Disable tracing.");
                    TraceControl.Disable();
                    Console.WriteLine("\tEnd: Disable tracing.\n");

                    Console.WriteLine("\tStart: Processing events from file.");
                    int msgCount = 0;
                    using (var trace = new TraceLog(TraceLog.CreateFromEventPipeDataFile(netPerfFile.Path)).Events.GetSource())
                    {
                        var names = new HashSet<string>();

                        trace.Dynamic.All += delegate (TraceEvent data)
                        {
                            if (!names.Contains(data.ProviderName))
                            {
                                Console.WriteLine("\t{0}", data.ProviderName);
                                names.Add(data.ProviderName);
                            }

                            if (data.ProviderName == "SimpleEventSource")
                            {
                                msgCount += 1;
                            }
                        };

                        trace.Process();
                    }
                    Console.WriteLine("\tEnd: Processing events from file.\n");

                    Console.WriteLine("\tProcessed {0} events from EventSource", msgCount);

                    pass &= msgCount == messageIterations;
                }
            }

            return pass ? 100 : 0;
        }
    }
}
