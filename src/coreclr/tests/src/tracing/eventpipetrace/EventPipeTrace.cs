using System;
using System.IO;
using Tracing.Tests.Common;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace Tracing.Tests
{
    class EventPipeTrace
    {
        private static int allocIterations = 10000;
        private static int gcIterations = 10;

        static void AssertEqual<T>(T left, T right) where T : IEquatable<T>
        {
            if (left.Equals(right) == false)
            {
                throw new Exception(string.Format("Values were not equal! {0} and {1}", left, right));
            }
        }

        static int Main(string[] args)
        {
            bool pass = true;
            bool keepOutput = false;

            // Use the first arg as an output filename if there is one
            string outputFilename = null;
            if (args.Length >= 1) {
                outputFilename = args[0];
                keepOutput = true;
            }
            else {
                outputFilename = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".netperf";
            }

            try
            {
                Console.WriteLine("\tStart: Enable tracing.");
                TraceControl.EnableDefault(outputFilename);
                Console.WriteLine("\tEnd: Enable tracing.\n");

                Console.WriteLine("\tStart: Generating CLR events");
                // Allocate for allocIterations iterations.
                for(int i=0; i<allocIterations; i++)
                {
                    GC.KeepAlive(new object());
                }
                // GC gcIternation times
                for(int i=0; i<gcIterations; i++)
                {
                    GC.Collect();
                }
                Console.WriteLine("\tEnd: Generating CLR Events\n");

                Console.WriteLine("\tStart: Disable tracing.");
                TraceControl.Disable();
                Console.WriteLine("\tEnd: Disable tracing.\n");

                Console.WriteLine("\tStart: Processing events from file.");
                int allocTickCount = 0;
                int gcTriggerCount = 0;
                using (var trace = TraceEventDispatcher.GetDispatcherFromFileName(outputFilename))
                {
                    trace.Clr.GCAllocationTick += delegate(GCAllocationTickTraceData data)
                    {
                        allocTickCount += 1;

                        // Some basic integrity checks
                        AssertEqual(data.TypeName, "System.Object");
                        AssertEqual(data.AllocationKind.ToString(), GCAllocationKind.Small.ToString());
                        AssertEqual(data.ProviderName, "Microsoft-Windows-DotNETRuntime");
                        AssertEqual(data.EventName, "GC/AllocationTick");
                    };
                    trace.Clr.GCTriggered += delegate(GCTriggeredTraceData data)
                    {
                        gcTriggerCount += 1;

                        // Some basic integrity checks
                        AssertEqual(data.Reason.ToString(), GCReason.Induced.ToString());
                        AssertEqual(data.ProviderName, "Microsoft-Windows-DotNETRuntime");
                        AssertEqual(data.EventName, "GC/Triggered");
                    };

                    trace.Process();
                }
                Console.WriteLine("\tEnd: Processing events from file.\n");

                Console.WriteLine("\tProcessed {0} GCAllocationTick events", allocTickCount);
                Console.WriteLine("\tProcessed {0} GCTriggered events", gcTriggerCount);

                pass &= allocTickCount > 0;
                pass &= gcTriggerCount == gcIterations;
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

            return pass ? 100 : 0;
        }
    }
}
