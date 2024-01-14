// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
#if USE_MDT_EVENTSOURCE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif
using Xunit;

using SdtEventSources;
using System.Diagnostics;
using System.Threading;
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
        public void Test_EventSource_EtwManifestGeneration()
        {
            var pid = Process.GetCurrentProcess().Id;
            var etlFileName = $"file.{pid}.etl";
            var tracesession = new TraceEventSession("testname", etlFileName);

            tracesession.EnableProvider("SimpleEventSource");

            RemoteInvokeOptions localOptions = new RemoteInvokeOptions { TimeOut = 300_000 /* ms */ };
            using (RemoteInvokeHandle handle = RemoteExecutor.Invoke(() =>
            {
                var es = new SimpleEventSource();
                for (var i = 0; i < 100; i++)
                {
                    es.WriteSimpleInt(i);
                    Thread.Sleep(100);
                }
            }, localOptions))
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));

                tracesession.Flush();

                tracesession.DisableProvider("SimpleEventSource");
                tracesession.Dispose();

                var manifestExists = false;
                var max_retries = 50;

                for (int i = 0; i < max_retries; i++)
                {
                    if (VerifyManifestAndRemoveFile(etlFileName))
                    {
                        manifestExists = true;
                        break;
                    }
                    Thread.Sleep(1000);
                }
                Assert.True(manifestExists);
            }
        }

        [ConditionalFact(nameof(IsProcessElevatedAndNotWindowsNanoServerAndRemoteExecutorSupported))]
        [SkipOnCoreClr("Test should only be run in non-stress modes", ~RuntimeTestModes.RegularRun)]
        public void Test_EventSource_EtwManifestGenerationRollover()
        {
            var pid = Process.GetCurrentProcess().Id;
            var initialFileName = $"initialFile.{pid}.etl";
            var rolloverFileName = $"rolloverFile.{pid}.etl";
            var tracesession = new TraceEventSession("testname", initialFileName);

            tracesession.EnableProvider("SimpleEventSource");

            using (RemoteInvokeHandle handle = RemoteExecutor.Invoke(() =>
            {
                var es = new SimpleEventSource();
                for (var i = 0; i < 100; i++)
                {
                    es.WriteSimpleInt(i);
                    Thread.Sleep(100);
                }
            }))
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));

                tracesession.Flush();

                tracesession.SetFileName(rolloverFileName);

                Thread.Sleep(TimeSpan.FromSeconds(5));

                tracesession.Flush();

                tracesession.DisableProvider("SimpleEventSource");
                tracesession.Dispose();

                bool initialFileHasManifest = false;
                bool rollOverFileHasManifest = false;

                var max_retries = 50;
                for (int i = 0; i < max_retries; i++)
                {
                    if (VerifyManifestAndRemoveFile(initialFileName))
                    {
                        initialFileHasManifest = true;
                        break;
                    }
                    Thread.Sleep(1000);
                }
                for (int i = 0; i < max_retries; i++)
                {
                    if (VerifyManifestAndRemoveFile(rolloverFileName))
                    {
                        rollOverFileHasManifest = true;
                        break;
                    }
                    Thread.Sleep(1000);
                }
                Assert.True(initialFileHasManifest);
                Assert.True(rollOverFileHasManifest);
            }
        }

        private bool VerifyManifestAndRemoveFile(string fileName)
        {
            Assert.True(File.Exists(fileName));

            using var source = new ETWTraceEventSource(fileName);

            Dictionary<string, int> providers = new Dictionary<string, int>();
            int eventCount = 0;
            var sawManifestData = false;
            source.Dynamic.All += (eventData) =>
            {
                eventCount++;
                if (!providers.ContainsKey(eventData.ProviderName))
                {
                    providers[eventData.ProviderName] = 0;
                }
                providers[eventData.ProviderName]++;

                if (eventData.ProviderName.Equals("SimpleEventSource") && eventData.EventName.Equals("ManifestData"))
                {
                    sawManifestData = true;
                }
            };
            source.Process();
            //File.Delete(fileName);

            if (!sawManifestData)
            {
                Console.WriteLine("Did not see ManifestData event from SimpleEventSource, test will fail. Additional info:");
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
