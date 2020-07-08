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
        private static readonly Lazy<bool> s_isElevated = new Lazy<bool>(AdminHelpers.IsProcessElevated);
        private static bool IsProcessElevated => s_isElevated.Value;
        private static bool IsProcessElevatedAndNotWindowsNanoServerAndRemoteExecutorSupported =>
            IsProcessElevated && PlatformDetection.IsNotWindowsNanoServer && RemoteExecutor.IsSupported;

        /// ETW only works with elevated process
        [ConditionalFact(nameof(IsProcessElevatedAndNotWindowsNanoServerAndRemoteExecutorSupported))]
        public void Test_EventSource_EtwManifestGeneration()
        {
            RemoteExecutor.Invoke(() =>
            {
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
                    var etlFileName = @"file.etl";
                    var tracesession = new TraceEventSession("testname", etlFileName);

                    tracesession.EnableProvider("SimpleEventSource");

                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    tracesession.Flush();
                    tracesession.DisableProvider("SimpleEventSource");
                    tracesession.Dispose();

                    Assert.True(VerifyManifestAndRemoveFile(etlFileName));
                }
            }).Dispose();
        }

        [ConditionalFact(nameof(IsProcessElevatedAndNotWindowsNanoServerAndRemoteExecutorSupported))]
        public void Test_EventSource_EtwManifestGenerationRollover()
        {
            RemoteExecutor.Invoke(() =>
            {
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
                    var initialFileName = @"initialFile.etl";
                    var rolloverFile = @"rolloverFile.etl";
                    var tracesession = new TraceEventSession("testname", initialFileName);

                    tracesession.EnableProvider("SimpleEventSource");

                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    tracesession.Flush();

                    tracesession.SetFileName(rolloverFile);

                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    tracesession.Flush();

                    tracesession.DisableProvider("SimpleEventSource");
                    tracesession.Dispose();

                    Assert.True(VerifyManifestAndRemoveFile(initialFileName));
                    Assert.True(VerifyManifestAndRemoveFile(rolloverFile));
                }
            }).Dispose();
        }

        private bool VerifyManifestAndRemoveFile(string fileName)
        {
            Assert.True(File.Exists(fileName));

            ETWTraceEventSource source = new ETWTraceEventSource(fileName);

            var sawManifestData = false;
            source.Dynamic.All += (eventData) =>
            {
                if (eventData.ProviderName.Equals("SimpleEventSource") && eventData.EventName.Equals("ManifestData"))
                {
                    sawManifestData = true;
                }
            };
            source.Process();
            //File.Delete(fileName);
            return sawManifestData;
        }
    }
}
