// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tracing.Tests.Common;

using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace Tracing.Tests
{
    [EventSource(Name = "My-Simple-Event-Source")]
    public sealed class MySimpleEventSource : EventSource
    {
        private AbstractTraceTest abstractTraceTest;

        [NonEvent]
        private void OnEventCommand(object sender, EventCommandEventArgs command)
        {
            this.abstractTraceTest.OnEventCommand(sender, command);
        }

        public MySimpleEventSource(AbstractTraceTest abstractTraceTest)
        {
            this.abstractTraceTest = abstractTraceTest;
            this.EventCommandExecuted += this.OnEventCommand;
        }

        public void Request(string message)
        {
            WriteEvent(1, message);
        }
    }

    public abstract class AbstractTraceTest
    {
        protected abstract string GetConfigFileContents();

        public virtual void OnEventCommand(object sender, EventCommandEventArgs command)
        {
        }

        protected virtual void InstallValidationCallbacks(TraceEventDispatcher trace)
        {

        }

        protected virtual bool Pass()
        {
            return true;
        }

        private static readonly TimeSpan TimeIntervalToReadConfigFile = new TimeSpan(0, 0, 25);

        private const int BytesInOneMB = 1024 * 1024;

        /// <summary>
        /// This test collects a trace of itself and then performs some basic validation on the trace.
        /// </summary>
        public int Execute()
        {
            MySimpleEventSource MySimpleEventSource = new MySimpleEventSource(this);

            // Logging before tracing is enable - this should be ignored 
            MySimpleEventSource.Request("Test 1");

            // Calculate the path to the config file.
            string configFileName = Assembly.GetEntryAssembly().GetName().Name + ".eventpipeconfig";
            string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configFileName);
            Console.WriteLine("Calculated config file path: " + configFilePath);

            // Write the config file to disk.
            File.WriteAllText(configFilePath, GetConfigFileContents());
            Console.WriteLine("Wrote contents of config file.");

            // Wait few seconds to ensure that tracing has started.
            Console.WriteLine($"Waiting {TimeIntervalToReadConfigFile.TotalSeconds} seconds for the config file to be picked up by the next poll operation.");
            Thread.Sleep(TimeIntervalToReadConfigFile);

            // Do some work that we can look for in the trace.
            Console.WriteLine("Do some work that will be captured by the trace.");
            GC.Collect(2, GCCollectionMode.Forced);

            // Logging while tracing is enabled - this should NOT be ignored 
            MySimpleEventSource.Request("Test 2");

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

            // Logging after tracing is disabled - this should be ignored 
            MySimpleEventSource.Request("Test 3");

            // Poll for file size changes to the trace file itself.
            // When the size of the trace file hasn't changed for few seconds, consider it fully written out.
            Console.WriteLine($"Waiting for the trace file to be written. Poll every second to watch for {TimeIntervalToReadConfigFile.TotalSeconds} seconds of no file size changes.");
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

            } while (DateTime.UtcNow.Subtract(timeOfLastChangeUTC) < TimeIntervalToReadConfigFile);

            // Use TraceEvent to consume the trace file and look for the work that we did.
            Console.WriteLine("Using TraceEvent to parse the file to find the work that was done during trace capture.");
            using (TraceEventDispatcher trace = TraceEventDispatcher.GetDispatcherFromFileName(traceFilePath))
            {
                InstallValidationCallbacks(trace);
                trace.Process();
            }

            // Clean-up the resulting trace file.
            File.Delete(traceFilePath);

            return this.Pass() ? 100 : 10086;
        }
    }
}