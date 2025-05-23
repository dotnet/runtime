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
            const string tracesPath = "traces";
            Directory.CreateDirectory(tracesPath);

            string extraProperties = @"
                <WasmPerfInstrumentation>all,interval=0</WasmPerfInstrumentation>
                <WasmPerfTracing>true</WasmPerfTracing>
            ";

            ProjectInfo info = CopyTestAsset(Configuration.Release, aot: false, TestAsset.BlazorBasicTestApp, "blazor_eventpipe", extraProperties: extraProperties);

            UpdateFile(Path.Combine("Pages", "Counter.razor"), new Dictionary<string, string> {
                    {
                        @"currentCount++;",
                        """
                        for(int i = 0; i < 1000; i++)
                        {
                            Console.WriteLine($"Incrementing count: {i}");
                        }
                        currentCount++;
                        """
                    }
                });


            // Build the project
            BuildProject(info, Configuration.Release, new BuildOptions(AssertAppBundle: false));

            // Setup the environment for file uploads
            var serverEnv = new Dictionary<string, string>
            {
                ["DEVSERVER_UPLOAD_PATH"] = tracesPath
            };

            // Create a custom test handler that will navigate to Counter page, collect CPU samples,
            // click the button, and upload the trace
            async Task CpuProfileTest(IPage page)
            {
                await Task.Delay(1000);
                _testOutput.WriteLine("XXXXXXX 1");
                // Navigate to the Counter page
                await page.Locator("text=Counter").ClickAsync();
                
                // Verify we're on the Counter page
                var txt = await page.Locator("p[role='status']").InnerHTMLAsync();
                Assert.Equal("Current count: 0", txt);
                _testOutput.WriteLine("XXXXXXX 2");

                // Collect CPU samples for 5 seconds
                await page.EvaluateAsync(@"
                    console.log(`AAAAAAAAAAAAAAAAAAAAAA`);
                    globalThis.getDotnetRuntime(0)
                    .collectCpuSamples({durationSeconds: 2, skipDownload:true}).then(traces => {
                        console.log(`DDDDDDDDDDDDDDDDDDDDDDD`);
                        // concatenate the buffers into a single Uint8Array
                        const concatenated = new Uint8Array(traces.reduce((acc, curr) => acc + curr.byteLength, 0));
                        let offset = 0;
                        for (const trace of traces) {
                            concatenated.set(new Uint8Array(trace), offset);
                            offset += trace.byteLength;
                        }
                        console.log(`EEEEEEEEEEEEEEEEEEEEEEEEE`);

                        return fetch('/upload/cpuprofile.nettrace', {
                            headers: {
                                'Content-Type': 'application/octet-stream'
                            },
                            method: 'POST',
                            body: concatenated
                        });
                    }).then(() => {
                        console.log(`XXXXXXXXXXXXXXX`);
                    }).catch(err => {
                        console.log(`ERROR: ${err}`);
                    });
                    console.log(`BBBBBBBBBBBBBBBBBBBBBBBB`);
                ");
                _testOutput.WriteLine("XXXXXXX 2a");

                // Click the button a few times
                for (int i = 0; i < 5; i++)
                {
                    _testOutput.WriteLine("XXXXXXX 4 " + i);
                    await page.Locator("text=\"Click me\"").ClickAsync();
                    await Task.Delay(300);
                }                
                _testOutput.WriteLine("XXXXXXX 3");

                var txt2 = await page.Locator("p[role='status']").InnerHTMLAsync();
                _testOutput.WriteLine("XXXXXXX T " + txt2);
                Assert.NotEqual("Current count: 0", txt2);

                _testOutput.WriteLine("XXXXXXX 6");
                // Give time for the upload to complete
                await Task.Delay(5000);
            }

            string extraArgs = " --web-server-use-cors --web-server-use-https";

            _testOutput.WriteLine("XXXXXXX 7");
            // Run the test using the custom handler
            await RunForBuildWithDotnetRun(new BlazorRunOptions(
                ExtraArgs: extraArgs,
                Configuration: Configuration.Release, 
                Test: CpuProfileTest, 
                CheckCounter: false,
                ServerEnvironment: serverEnv
            ));

            _testOutput.WriteLine("XXXXXXX 8");

            // Verify the trace file was created
            var traceFilePath = Path.Combine(tracesPath, "cpuprofile.nettrace");
            Assert.True(File.Exists(traceFilePath), $"Trace file {traceFilePath} was not created");
            var converted = TraceLog.CreateFromEventTraceLogFile(traceFilePath);
            Assert.True(File.Exists(converted), $"Trace file {converted} was not created");
            _testOutput.WriteLine("XXXXXXX 9");

            var methodFound = false;
            using (var source = TraceLog.OpenOrConvert(converted))
            {
                methodFound = source.CallStacks.Any(stack => stack.CodeAddress.FullMethodName=="BlazorBasicTestApp.Counter.IncrementCount()");
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
