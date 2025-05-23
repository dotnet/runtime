// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests.Blazor
{
    public class EventPipeDiagnosticsTests : BlazorWasmTestBase
    {
        public EventPipeDiagnosticsTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
            _enablePerTestCleanup = true;
        }

        [Theory]
        [InlineData(Configuration.Debug)]
        [InlineData(Configuration.Release)]
        public async Task BlazorEventPipeTestWithCpuSamples(Configuration config)
        {            const string tracesPath = "traces";
            Directory.CreateDirectory(tracesPath);

            string extraProperties = @"
                <WasmPerfInstrumentation>all,interval=0</WasmPerfInstrumentation>
                <WasmPerfTracing>true</WasmPerfTracing>
            ";

            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.BlazorBasicTestApp, "blazor_eventpipe", extraProperties: extraProperties);

            // Build the project
            BuildProject(info, config, new BuildOptions(AssertAppBundle: false));

            // Setup the environment for file uploads
            string traceFilePath = Path.Combine(tracesPath, "cpuprofile.nettrace");
            var serverEnv = new Dictionary<string, string>
            {
                ["FILE_UPLOAD_PATH"] = tracesPath,
                ["TRACE_FILE_PATH"] = traceFilePath
            };

            // Create a custom test handler that will navigate to Counter page, collect CPU samples,
            // click the button, and upload the trace
            async Task CpuProfileTest(IPage page)
            {
                // Navigate to the Counter page
                await page.Locator("text=Counter").ClickAsync();
                
                // Verify we're on the Counter page
                var txt = await page.Locator("p[role='status']").InnerHTMLAsync();
                Assert.Equal("Current count: 0", txt);

                // Collect CPU samples for 5 seconds
                await page.EvaluateAsync(@"
                    window.cpuSamplesPromise = globalThis.getDotnetRuntime(0).collectCpuSamples({durationSeconds: 5});
                ");

                // Click the button a few times
                for (int i = 0; i < 5; i++)
                {
                    await page.Locator("text=\"Click me\"").ClickAsync();
                    await Task.Delay(300);
                }                // Wait for the CPU samples promise to complete
                await page.EvaluateAsync(@"
                    window.cpuSamplesPromise.then(trace => {
                        return fetch('/upload', {
                            method: 'POST',
                            headers: {
                                'File-Name': 'cpuprofile.nettrace'
                            },
                            body: trace
                        });
                    });
                ");

                // Give time for the upload to complete
                await Task.Delay(1000);
            }

            // Run the test using the custom handler
            await RunForBuildWithDotnetRun(new BlazorRunOptions(
                Configuration: config, 
                Test: CpuProfileTest, 
                CheckCounter: false,
                ServerEnvironment: serverEnv
            ));

            // Verify the trace file was created
            Assert.True(File.Exists(traceFilePath), $"Trace file {traceFilePath} was not created");

            // Analyze the trace file
            using (var source = new ETWTraceEventSource(traceFilePath))
            {
                var methodFound = false;
                var sampledMethodNames = new List<string>();

                // Get all sample profile events
                source.Clr.All += (TraceEvent data) => 
                {
                    if (data.EventName == "Sample" || data.EventName == "GC/SampledObjectAllocation")
                    {
                        var stackEvent = data as ClrStackTraceTraceData;
                        if (stackEvent != null && stackEvent.CallStack != null)
                        {
                            var stack = stackEvent.CallStack;
                            while (stack != null)
                            {
                                var methodName = stack.CodeAddress.FullMethodName;
                                sampledMethodNames.Add(methodName);
                                
                                // Check if IncrementCount method is in the stack
                                if (methodName.Contains("IncrementCount"))
                                {
                                    methodFound = true;
                                }
                                stack = stack.Caller;
                            }
                        }
                    }
                };

                source.Process();

                // Output all sampled method names for diagnostic purposes
                foreach (var methodName in sampledMethodNames.Distinct().Take(20))
                {
                    _testOutput.WriteLine($"Sampled method: {methodName}");
                }

                Assert.True(methodFound, "The trace should contain stack frames for the 'IncrementCount' method");
            }
        }
    }
}
