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
            bool pass = true;
            bool keepOutput = false;

            // Use the first arg as an output filename if there is one.
            string outputFilename = null;
            if (args.Length >= 1)
            {
                outputFilename = args[0];
                keepOutput = true;
            }
            else
            {
                outputFilename = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".netperf";
            }

            try
            {
                Console.WriteLine("\tStart: Enable tracing.");
                TraceControl.EnableDefault(outputFilename);
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
                using (var trace = TraceEventDispatcher.GetDispatcherFromFileName(outputFilename))
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
            finally {
                if (keepOutput)
                {
                    Console.WriteLine("\n\tOutput file: {0}", outputFilename);
                }
                else
                {
                    System.IO.File.Delete(outputFilename);
                }
            }

            return 100;
        }
    }
}
