// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tracing.Tests.Common;

using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace Tracing.Tests
{
    public static class TraceControlTest
    {
        private static string ConfigFileContents = @"
OutputPath=.
CircularMB=2048
Providers=*:0xFFFFFFFFFFFFFFFF:5
";

        private const int BytesInOneMB = 1024 * 1024;

        /// <summary>
        /// This test collects a trace of itself and then performs some basic validation on the trace.
        /// </summary>
        public static int Main(string[] args)
        {
            // Calculate the path to the config file.
            string configFileName = Assembly.GetEntryAssembly().GetName().Name + ".eventpipeconfig";
            string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configFileName);
            Console.WriteLine("Calculated config file path: " + configFilePath);

            // Write the config file to disk.
            File.WriteAllText(configFilePath, ConfigFileContents);
            Console.WriteLine("Wrote contents of config file.");

            // Wait 5 seconds to ensure that tracing has started.
            Console.WriteLine("Waiting 5 seconds for the config file to be picked up by the next poll operation.");
            Thread.Sleep(TimeSpan.FromSeconds(5));

            // Do some work that we can look for in the trace.
            Console.WriteLine("Do some work that will be captured by the trace.");
            GC.Collect(2, GCCollectionMode.Forced);
            Console.WriteLine("Done with the work.");

            // Delete the config file to start tracing.
            File.Delete(configFilePath);
            Console.WriteLine("Deleted the config file.");

            // Build the full path to the trace file.
            string[] traceFiles = Directory.GetFiles(".", "*.netperf", SearchOption.TopDirectoryOnly);
            Assert.Equal("traceFiles.Length == 1", traceFiles.Length, 1);
            string traceFilePath = traceFiles[0];

            // Poll the file system and wait for the trace file to be written.
            Console.WriteLine("Wait for the config file deletion to be picked up and for the trace file to be written.");

            // Wait for 1 second, which is the poll time when tracing is enabled.
            Thread.Sleep(TimeSpan.FromSeconds(1));

            // Poll for file size changes to the trace file itself.  When the size of the trace file hasn't changed for 5 seconds, consider it fully written out.
            Console.WriteLine("Waiting for the trace file to be written.  Poll every second to watch for 5 seconds of no file size changes.");
            long lastSizeInBytes = 0;
            DateTime timeOfLastChangeUTC = DateTime.UtcNow;
            do
            {
                FileInfo traceFileInfo = new FileInfo(traceFilePath);
                long currentSizeInBytes = traceFileInfo.Length;
                Console.WriteLine("Trace file size: " + ((double)currentSizeInBytes / BytesInOneMB));

                if (currentSizeInBytes > lastSizeInBytes)
                {
                    lastSizeInBytes = currentSizeInBytes;
                    timeOfLastChangeUTC = DateTime.UtcNow;
                }

                Thread.Sleep(TimeSpan.FromSeconds(1));

            } while (DateTime.UtcNow.Subtract(timeOfLastChangeUTC) < TimeSpan.FromSeconds(5));

            int retVal = 0;

            // Use TraceEvent to consume the trace file and look for the work that we did.
            Console.WriteLine("Using TraceEvent to parse the file to find the work that was done during trace capture.");
            using (var trace = TraceEventDispatcher.GetDispatcherFromFileName(traceFilePath))
            {
                string gcReasonInduced = GCReason.Induced.ToString();
                string providerName = "Microsoft-Windows-DotNETRuntime";
                string gcTriggeredEventName = "GC/Triggered";

                trace.Clr.GCTriggered += delegate (GCTriggeredTraceData data)
                {
                    if (gcReasonInduced.Equals(data.Reason.ToString()))
                    {
                        Console.WriteLine("Detected an induced GC");
                        retVal = 100;
                    }
                };

                trace.Process();
            }

            // Clean-up the resulting trace file.
            File.Delete(traceFilePath);

            return retVal;
        }
    }
}
