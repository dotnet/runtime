// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
#if USE_MDT_EVENTSOURCE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif
using Xunit;

using SdtEventSources;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Diagnostics.Tracing;

namespace BasicEventSourceTests
{
    public partial class TestsManifestGeneration
    {
        // Specifies whether the process is elevated or not.
        private static bool IsProcessElevatedAndNotWindowsNanoServerAndRemoteExecutorSupported =>
            PlatformDetection.IsPrivilegedProcess && PlatformDetection.IsNotWindowsNanoServer && RemoteExecutor.IsSupported;

        /// ETW only works with elevated process
        [ConditionalFact(nameof(IsProcessElevatedAndNotWindowsNanoServerAndRemoteExecutorSupported))]
        [SkipOnCoreClr("Test should only be run in non-stress modes", ~RuntimeTestModes.RegularRun)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/97255", typeof(PlatformDetection), nameof(PlatformDetection.IsX86Process))]
        public void Test_EventSource_EtwManifestGeneration()
        {
            var pid = Process.GetCurrentProcess().Id;
            var etlFileName = $"file.{pid}.etl";

            // Start the trace session
            using (var traceSession = new TraceEventSession(nameof(Test_EventSource_EtwManifestGeneration), etlFileName))
            {
                // Enable the provider of interest.
                traceSession.EnableProvider(nameof(SimpleEventSource));

                // Launch the target process to collect data
                using (RemoteInvokeHandle handle = RemoteExecutor.Invoke(() =>
                {
                    using var es = new SimpleEventSource();

                    // 50 * 100 = 5 seconds
                    for (var i = 0; i < 50; i++)
                    {
                        es.WriteSimpleInt(i);
                        Thread.Sleep(100);
                    }
                }))
                {
                    handle.Process.WaitForExit();
                }

                // Flush session and disable the provider.
                traceSession.Flush();
                traceSession.DisableProvider(nameof(SimpleEventSource));
            }

            // Wait for the ETL file to flush to disk
            Thread.Sleep(TimeSpan.FromSeconds(2));

            Assert.True(VerifyManifestAndRemoveFile(etlFileName));
        }

        [ConditionalFact(nameof(IsProcessElevatedAndNotWindowsNanoServerAndRemoteExecutorSupported))]
        [SkipOnCoreClr("Test should only be run in non-stress modes", ~RuntimeTestModes.RegularRun)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/97255", typeof(PlatformDetection), nameof(PlatformDetection.IsX86Process))]
        public void Test_EventSource_EtwManifestGenerationRollover()
        {
            var pid = Process.GetCurrentProcess().Id;
            var initialFileName = $"initialFile.{pid}.etl";
            var rolloverFileName = $"rolloverFile.{pid}.etl";

            // Start the trace session
            using (var traceSession = new TraceEventSession(nameof(Test_EventSource_EtwManifestGenerationRollover), initialFileName))
            {
                // Enable the provider of interest.
                traceSession.EnableProvider(nameof(SimpleEventSource));

                // Launch the target process to collect data
                using (RemoteInvokeHandle handle = RemoteExecutor.Invoke(() =>
                {
                    using var es = new SimpleEventSource();

                    // 100 * 100 = 10 seconds
                    for (var i = 0; i < 100; i++)
                    {
                        es.WriteSimpleInt(i);
                        Thread.Sleep(100);
                    }
                }))
                {
                    // Wait for some time to collect events
                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    traceSession.Flush();

                    traceSession.SetFileName(rolloverFileName);

                    // Wait for some time to collect events
                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    // Wait for the target process to exit.
                    handle.Process.WaitForExit();

                    // Flush session and disable the provider.
                    traceSession.Flush();
                    traceSession.DisableProvider(nameof(SimpleEventSource));
                }
            }

            // Wait for the ETL files to flush to disk
            Thread.Sleep(TimeSpan.FromSeconds(2));

            Assert.True(VerifyManifestAndRemoveFile(initialFileName));
            Assert.True(VerifyManifestAndRemoveFile(rolloverFileName));
        }

        private bool VerifyManifestAndRemoveFile(string fileName)
        {
            Assert.True(File.Exists(fileName));

            Dictionary<string, int> providers = new Dictionary<string, int>();
            int eventCount = 0;
            var sawManifestData = false;

            using (var source = new ETWTraceEventSource(fileName))
            {
                source.Dynamic.All += (eventData) =>
                {
                    eventCount++;
                    if (!providers.ContainsKey(eventData.ProviderName))
                    {
                        providers[eventData.ProviderName] = 0;
                    }
                    providers[eventData.ProviderName]++;

                    if (eventData.ProviderName.Equals(nameof(SimpleEventSource)) && eventData.EventName.Equals("ManifestData"))
                    {
                        sawManifestData = true;
                    }
                };
                source.Process();
            }

            if (sawManifestData)
            {
                // Delete file if successfully processed.
                File.Delete(fileName);
            }
            else
            {
                Console.WriteLine($"Did not see ManifestData event from {nameof(SimpleEventSource)}, test will fail. Additional info:");
                Console.WriteLine($"    file name {fileName}");
                Console.WriteLine($"    total event count {eventCount}");
                Console.WriteLine($"    total providers {providers.Count}");
                foreach (var provider in providers.Keys)
                {
                    Console.WriteLine($"        Provider name {provider} event count {providers[provider]}");
                }
            }
            return sawManifestData;
        }
    }
}
