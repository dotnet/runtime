// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Tracing.Tests.Common;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace Tracing.Tests
{
    public static class TraceValidationInducedGC
    {
        private static int InducedGCIterations = 10;

        public static int Main(string[] args)
        {
            using (var netPerfFile = NetPerfFile.Create(args))
            {
                Console.WriteLine("\tStart: Enable tracing.");
                TraceControl.EnableDefault(netPerfFile.Path);
                Console.WriteLine("\tEnd: Enable tracing.\n");

                Console.WriteLine("\tStart: Generate some events.");
                for(int i=0; i<InducedGCIterations; i++)
                {
                    GC.Collect();
                }
                Console.WriteLine("\tEnd: Generate some events.\n");

                Console.WriteLine("\tStart: Disable tracing.");
                TraceControl.Disable();
                Console.WriteLine("\tEnd: Disable tracing.\n");

                Console.WriteLine("\tStart: Process the trace file.");
                int matchingEventCount = 0;
                int nonMatchingEventCount = 0;
                using (var trace = TraceEventDispatcher.GetDispatcherFromFileName(netPerfFile.Path))
                {
                    string gcReasonInduced = GCReason.Induced.ToString();
                    string providerName = "Microsoft-Windows-DotNETRuntime";
                    string gcTriggeredEventName = "GC/Triggered";

                    trace.Clr.GCTriggered += delegate(GCTriggeredTraceData data)
                    {
                        if(gcReasonInduced.Equals(data.Reason.ToString()) &&
                           providerName.Equals(data.ProviderName) &&
                           gcTriggeredEventName.Equals(data.EventName))
                        {
                            matchingEventCount++;
                        }
                        else
                        {
                            nonMatchingEventCount++;
                        }
                    };

                    trace.Process();
                }
                Console.WriteLine("\tEnd: Processing events from file.\n");

                Assert.Equal(nameof(matchingEventCount), InducedGCIterations, matchingEventCount);
                Assert.Equal(nameof(nonMatchingEventCount), nonMatchingEventCount, 0);
            }

            return 100;
        }
    }
}
