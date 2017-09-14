using System;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics.Tracing;

using Tracing.Tests.Common;

namespace Tracing.Tests
{
    [EventSource(Name = "SimpleEventSource")]
    class SimpleEventSource : EventSource
    {
        public SimpleEventSource() : base(true) { }

        [Event(1)]
        internal void MathResult(int x, int y, int z, string formula) { this.WriteEvent(1, x, y, z, formula); }
    }

    class EventPipeSmoke
    {
        private static int messageIterations = 10000;
        private static int trivialSize = 0x100000;

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

            SimpleEventSource eventSource = new SimpleEventSource();

            Console.WriteLine("\tStart: Enable tracing.");
            TraceControl.Enable(GetConfig(eventSource, outputFilename));
            Console.WriteLine("\tEnd: Enable tracing.\n");

            Console.WriteLine("\tStart: Messaging.");
            // Send messages
            // Use random numbers and addition as a simple, human readble checksum
            Random generator = new Random();
            for(int i=0; i<messageIterations; i++)
            {
                int x = generator.Next(1,1000);
                int y = generator.Next(1,1000);
                string formula = String.Format("{0} + {1} = {2}", x, y, x+y);
                
                eventSource.MathResult(x, y, x+y, formula);
            }
            Console.WriteLine("\tEnd: Messaging.\n");

            Console.WriteLine("\tStart: Disable tracing.");
            TraceControl.Disable();
            Console.WriteLine("\tEnd: Disable tracing.\n");

            FileInfo outputMeta = new FileInfo(outputFilename);
            Console.WriteLine("\tCreated {0} bytes of data", outputMeta.Length);

            bool pass = false;
            if (outputMeta.Length > trivialSize){
                pass = true;
            }

            if (keepOutput)
            {
                Console.WriteLine(String.Format("\tOutput file: {0}", outputFilename));
            }
            else
            {
                System.IO.File.Delete(outputFilename);
            }

            return pass ? 100 : -1;
        }
    }
}
