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
using Microsoft.Diagnostics.Tracing.Etlx;

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

        [Fact]
        public async Task BlazorEventPipeTestWithCpuSamples()
        {
            string extraProperties = @"
                <WasmPerfInstrumentation>all,interval=0</WasmPerfInstrumentation>
                <WasmPerfTracing>true</WasmPerfTracing>
                <WBTDevServer>true</WBTDevServer>
            ";

            ProjectInfo info = CopyTestAsset(Configuration.Release, aot: false, TestAsset.BlazorBasicTestApp, "blazor_eventpipe", extraProperties: extraProperties);

            UpdateFile(Path.Combine("Pages", "Counter.razor"), new Dictionary<string, string> {
                    {
                        @"currentCount++;",
                        """
                        for(int i = 0; i < 50; i++)
                        {
                            if( i % 50 == 0)
                            {
                                Console.WriteLine($"Incrementing count: {i} {DateTime.Now:O}");
                            }
                        }
                        currentCount++;
                        if (currentCount > 4)
                        {
                            Console.WriteLine("WASM EXIT 0");
                        }
                        """
                    }
                });


            // Build the project
            BuildProject(info, Configuration.Release, new BuildOptions(AssertAppBundle: false));

            // Setup the environment for file uploads
            var serverEnv = new Dictionary<string, string>
            {
                ["DEVSERVER_UPLOAD_PATH"] = info.LogPath
            };

            // Create a custom test handler that will navigate to Counter page, collect CPU samples,
            // click the button, and upload the trace
            async Task CpuProfileTest(IPage page)
            {
                await Task.Delay(500);
                // Navigate to the Counter page
                await page.Locator("text=Counter").ClickAsync();
                
                // Verify we're on the Counter page
                var txt = await page.Locator("p[role='status']").InnerHTMLAsync();
                Assert.Equal("Current count: 0", txt);

                // Collect CPU samples for 5 seconds
                await page.EvaluateAsync(@"
                    globalThis.getDotnetRuntime(0)
                    .collectCpuSamples({durationSeconds: 2.0, skipDownload:true}).then(traces => {
                        // concatenate the buffers into a single Uint8Array
                        const concatenated = new Uint8Array(traces.reduce((acc, curr) => acc + curr.byteLength, 0));
                        let offset = 0;
                        for (const trace of traces) {
                            concatenated.set(new Uint8Array(trace), offset);
                            offset += trace.byteLength;
                        }

                        console.log(`File size to upload: ${concatenated.byteLength} bytes ${new Date().toISOString()}`);
                        return fetch('/upload/cpuprofile.nettrace', {
                            headers: {
                                'Content-Type': 'application/octet-stream'
                            },
                            method: 'POST',
                            body: concatenated
                        });
                    }).then(() => {
                        console.log(`File uploaded successfully`);
                    }).catch(err => {
                        console.log(`ERROR: ${err}`);
                    });
                    console.log(`collectCpuSamples started at ${new Date().toISOString()}`);
                ");

                // Click the button a few times
                for (int i = 0; i < 5; i++)
                {
                    await page.Locator("text=\"Click me\"").ClickAsync();
                    await Task.Delay(10);
                }                

                var txt2 = await page.Locator("p[role='status']").InnerHTMLAsync();
                Assert.NotEqual("Current count: 0", txt2);

                await Task.Delay(5000);
            }

            // Run the test using the custom handler
            await RunForBuildWithDotnetRun(new BlazorRunOptions(
                Configuration: Configuration.Release, 
                Test: CpuProfileTest,
                TimeoutSeconds: 60,
                CheckCounter: false,
                ServerEnvironment: serverEnv
            ));


            // Verify the trace file was created
            var traceFilePath = Path.GetFullPath(Path.Combine(info.LogPath, "cpuprofile.nettrace"));
            Assert.True(File.Exists(traceFilePath), $"Trace file {traceFilePath} was not created");
            var conversionLog = new StringBuilder();
            var converted = TraceLog.CreateFromEventTraceLogFile(traceFilePath,null,new TraceLogOptions
            {
                ConversionLog = new StringWriter(conversionLog),
            });
            _testOutput.WriteLine(conversionLog.ToString());
            Assert.True(File.Exists(converted), $"Trace file {converted} was not created");

            var methodFound = false;
            using (var source = TraceLog.OpenOrConvert(converted))
            {
                methodFound = source.CallStacks.Any(stack => stack.CodeAddress.FullMethodName=="BlazorBasicTestApp.Pages.Counter.IncrementCount()");
                if(!methodFound)
                {
                    foreach (var stack in source.CallStacks)
                    {
                        _testOutput.WriteLine($"Stack: {stack.CodeAddress.FullMethodName}");
                    }
                }
            }

            Assert.True(methodFound, "The trace should contain stack frames for the 'IncrementCount' method");
        }
    }
}
