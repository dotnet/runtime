// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Playwright;
using Xunit;
#nullable enable

namespace Wasm.Build.Tests.Blazor;

[TestCategory("mono")]
public class EventPipeDiagnosticsTests : BlazorWasmTestBase
{
    private static readonly string uploadPattern = "^[a-zA-Z0-9_]+\\.nettrace$";

    public EventPipeDiagnosticsTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
        _enablePerTestCleanup = true;
    }

    [Theory]
    [InlineData(Configuration.Debug, false)]
    [InlineData(Configuration.Release, false)]
    [InlineData(Configuration.Release, true)]
    public async Task BlazorEventPipeTestWithCpuSamples(Configuration config, bool aot)
    {
        string extraProperties = @"
                <WasmPerformanceInstrumentation>all,interval=0</WasmPerformanceInstrumentation>
                <EnableDiagnostics>true</EnableDiagnostics>
                <WasmDebugLevel>0</WasmDebugLevel>
                <WBTDevServer>true</WBTDevServer>
            ";

        ProjectInfo info = CopyTestAsset(config, aot, TestAsset.BlazorBasicTestApp, "blazor_cpu_samples", extraProperties: extraProperties);

        UpdateCounterPage();

        BuildProject(info, config, new BuildOptions(AssertAppBundle: false));

        async Task CollectCpuSamplesTest(IPage page)
        {
            await SetupCounterPage(page, "cpuprofile.nettrace", "globalThis.getDotnetRuntime(0).collectCpuSamples({ durationSeconds: 5.0, skipDownload: true })");
            await ClickAndCollect(page);
        }

        // Run the test using the custom handler
        await RunForBuildWithDotnetRun(new BlazorRunOptions(
            Configuration: config,
            Test: CollectCpuSamplesTest,
            TimeoutSeconds: 60,
            CheckCounter: false,
            ServerEnvironment: new Dictionary<string, string>
            {
                ["DEVSERVER_UPLOAD_PATH"] = info.LogPath,
                ["DEVSERVER_UPLOAD_PATTERN"] = uploadPattern
            }
        ));

        var methodFound = false;
        using (var source = TraceLog.OpenOrConvert(ConvertTrace(info, "cpuprofile.nettrace")))
        {
            methodFound = source.CallStacks.Any(stack => stack.CodeAddress.FullMethodName == "BlazorBasicTestApp.Pages.Counter.IncrementCount()");
            if (!methodFound)
            {
                foreach (var stack in source.CallStacks)
                {
                    _testOutput.WriteLine($"Stack: {stack.CodeAddress.FullMethodName}");
                }
            }
        }

        Assert.True(methodFound, "The cpuprofile.nettrace should contain stack frames for the 'Counter.IncrementCount' method");
    }

    [Fact]
    public async Task BlazorEventPipeTestWithMetrics()
    {
        string extraProperties = @"
                <EnableDiagnostics>true</EnableDiagnostics>
                <EventSourceSupport>true</EventSourceSupport>
                <MetricsSupport>true</MetricsSupport>
                <WBTDevServer>true</WBTDevServer>
            ";

        ProjectInfo info = CopyTestAsset(Configuration.Release, aot: false, TestAsset.BlazorBasicTestApp, "blazor_metrics", extraProperties: extraProperties);

        UpdateCounterPage();
        BuildProject(info, Configuration.Release, new BuildOptions(AssertAppBundle: false));

        async Task CollectMetricsTest(IPage page)
        {
            await SetupCounterPage(page, "metrics.nettrace", "globalThis.getDotnetRuntime(0).collectMetrics({ durationSeconds: 5.0, skipDownload: true })");
            await ClickAndCollect(page);
        }

        // Run the test using the custom handler
        await RunForBuildWithDotnetRun(new BlazorRunOptions(
            Configuration: Configuration.Release,
            Test: CollectMetricsTest,
            TimeoutSeconds: 60,
            CheckCounter: false,
            ServerEnvironment: new Dictionary<string, string>
            {
                ["DEVSERVER_UPLOAD_PATH"] = info.LogPath,
                ["DEVSERVER_UPLOAD_PATTERN"] = uploadPattern
            }
        ));

        var actualInstruments = ExtractInstrumentNames(info, "metrics.nettrace");
        var expectedInstruments = new[]
        {
            "System.Diagnostics.Metrics/instrumentName/dotnet.assembly.count",
            "System.Diagnostics.Metrics/instrumentName/dotnet.exceptions",
            "System.Diagnostics.Metrics/instrumentName/dotnet.gc.collections",
            "System.Diagnostics.Metrics/instrumentName/dotnet.gc.heap.total_allocated",
            "System.Diagnostics.Metrics/instrumentName/dotnet.gc.last_collection.heap.fragmentation.size",
            "System.Diagnostics.Metrics/instrumentName/dotnet.gc.last_collection.heap.size",
            "System.Diagnostics.Metrics/instrumentName/dotnet.gc.last_collection.memory.committed_size",
            "System.Diagnostics.Metrics/instrumentName/dotnet.gc.pause.time",
            "System.Diagnostics.Metrics/instrumentName/dotnet.jit.compilation.time",
            "System.Diagnostics.Metrics/instrumentName/dotnet.jit.compiled_il.size",
            "System.Diagnostics.Metrics/instrumentName/dotnet.jit.compiled_methods",
            "System.Diagnostics.Metrics/instrumentName/dotnet.monitor.lock_contentions",
            "System.Diagnostics.Metrics/instrumentName/dotnet.process.cpu.count",
            "System.Diagnostics.Metrics/instrumentName/dotnet.process.memory.working_set",
            "System.Diagnostics.Metrics/instrumentName/dotnet.thread_pool.queue.length",
            "System.Diagnostics.Metrics/instrumentName/dotnet.thread_pool.thread.count",
            "System.Diagnostics.Metrics/instrumentName/dotnet.thread_pool.work_item.count",
            "System.Diagnostics.Metrics/instrumentName/dotnet.timer.count",
        };

        foreach (var expectedInstrument in expectedInstruments)
        {
            Assert.True(actualInstruments.ContainsKey(expectedInstrument), $"The metrics.nettrace should contain instrument: {expectedInstrument}");
        }
    }

    [Fact]
    public async Task BlazorEventPipeTestWithHeapDump()
    {
        string extraProperties = @"
                <EnableDiagnostics>true</EnableDiagnostics>
                <WBTDevServer>true</WBTDevServer>
            ";

        ProjectInfo info = CopyTestAsset(Configuration.Release, aot: false, TestAsset.BlazorBasicTestApp, "blazor_gc_dump", extraProperties: extraProperties);

        UpdateCounterPage();

        // Build the project
        BuildProject(info, Configuration.Release, new BuildOptions(AssertAppBundle: false));

        // Create a custom test handler that will navigate to Counter page, collect CPU samples,
        // click the button, and upload the trace
        async Task CollectGcDumpTest(IPage page)
        {
            await SetupCounterPage(page, "gcdump.nettrace", "globalThis.getDotnetRuntime(0).collectGcDump({ durationSeconds: 5.0, skipDownload: true })");
            await ClickAndCollect(page);
        }

        // Run the test using the custom handler
        await RunForBuildWithDotnetRun(new BlazorRunOptions(
            Configuration: Configuration.Release,
            Test: CollectGcDumpTest,
            TimeoutSeconds: 60,
            CheckCounter: false,
            ServerEnvironment: new Dictionary<string, string>
            {
                ["DEVSERVER_UPLOAD_PATH"] = info.LogPath,
                ["DEVSERVER_UPLOAD_PATTERN"] = uploadPattern
            }
        ));

        var actualEvents = ExtractEventNames(info, "gcdump.nettrace");
        var expectedEvents = new[]
        {
            "Microsoft-Windows-DotNETRuntime/GC/Start",
            "Microsoft-Windows-DotNETRuntime/GC/Stop",
            "Microsoft-Windows-DotNETRuntime/GC/BulkEdge",
            "Microsoft-Windows-DotNETRuntime/GC/BulkNode",
            "Microsoft-Windows-DotNETRuntime/GC/BulkRootEdge",
            "Microsoft-Windows-DotNETRuntime/Type/BulkType"
        };

        foreach (var expectedEvent in expectedEvents)
        {
            Assert.True(actualEvents.ContainsKey(expectedEvent), $"The metrics.nettrace should contain event: {expectedEvent}");
        }
    }

    private string ConvertTrace(ProjectInfo info, string fileName)
    {
        var traceFilePath = Path.GetFullPath(Path.Combine(info.LogPath, fileName));
        Assert.True(File.Exists(traceFilePath), $"Trace file {traceFilePath} was not created");
        var conversionLog = new StringBuilder();
        using var sw = new StringWriter(conversionLog);
        var options = new TraceLogOptions
        {
            ConversionLog = sw,
        };
        var traceConvertedPath = TraceLog.CreateFromEventTraceLogFile(traceFilePath, null, options);
        _testOutput.WriteLine(conversionLog.ToString());
        Assert.True(File.Exists(traceConvertedPath), $"Trace file {traceConvertedPath} was not created");
        return traceConvertedPath;
    }

    private static Dictionary<string, int> ExtractEventNames(ProjectInfo info, string fileName)
    {
        var dictionary = new Dictionary<string, int>();
        var traceFilePath = Path.GetFullPath(Path.Combine(info.LogPath, fileName));
        using (var source = new EventPipeEventSource(traceFilePath))
        {
            source.Clr.All += (data) =>
            {
                var key = $"{data.ProviderName}/{data.EventName}";
                if (dictionary.ContainsKey(key))
                {
                    dictionary[key] = dictionary[key] + 1;
                }
                else
                {
                    dictionary[key] = 1;
                }
            };
            source.Process();
        }

        return dictionary;
    }

    private static Dictionary<string, int> ExtractInstrumentNames(ProjectInfo info, string fileName)
    {
        var dictionary = new Dictionary<string, int>();
        var traceFilePath = Path.GetFullPath(Path.Combine(info.LogPath, fileName));
        using (var source = new EventPipeEventSource(traceFilePath))
        {
            source.Dynamic.All += (data) =>
            {
                foreach (var arg in data.PayloadNames)
                {
                    var key = $"{data.ProviderName}/{arg}/{data.PayloadByName(arg)}";
                    if (dictionary.ContainsKey(key))
                    {
                        dictionary[key] = dictionary[key] + 1;
                    }
                    else
                    {
                        dictionary[key] = 1;
                    }
                }
            };
            source.Process();
        }

        return dictionary;
    }

    private void UpdateCounterPage()
    {
        UpdateFile(Path.Combine("Pages", "Counter.razor"), new Dictionary<string, string> {
                {
                    @"currentCount++;",
                    """
                    for(int i = 0; i < 100; i++)
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.Append("Incrementing count: ");
                        sb.Append(i);
                        sb.Append(" ");
                        sb.Append(DateTime.Now.ToString("O"));
                        if( i % 50 == 0)
                        {
                            Console.WriteLine(sb.ToString());
                        }
                    }
                    currentCount++;
                    """
                }
            });
    }

    private async Task SetupCounterPage(IPage page, string fileName, string traceCommand)
    {
        await Task.Delay(500);
        // Navigate to the Counter page
        await page.Locator("text=Counter").ClickAsync();

        // Verify we're on the Counter page
        var txt = await page.Locator("p[role='status']").InnerHTMLAsync();
        Assert.Equal("Current count: 0", txt);

        var up = """
        globalThis.collectAndUpload = async () => {
            console.log(`Tracing ${filename} started ${new Date().toISOString()}`);
            
            const traces = await ${traceCommand};

            console.log(`Tracing done ${new Date().toISOString()}`);
                    
            // concatenate the buffers into a single Uint8Array
            const concatenated = new Uint8Array(traces.reduce((acc, curr) => acc + curr.byteLength, 0));
            let offset = 0;
            for (const trace of traces) {
                concatenated.set(new Uint8Array(trace), offset);
                offset += trace.byteLength;
            }

            console.log(`File ${filename} size to upload: ${concatenated.byteLength} bytes ${new Date().toISOString()}`);
            await fetch(`/upload/${filename}`, {
                headers: {
                    'Content-Type': 'application/octet-stream'
                },
                method: 'POST',
                body: concatenated
            });
            console.log(`File uploaded successfully: ${new Date().toISOString()}`);
            await new Promise(resolve => setTimeout(resolve, 1000));
            console.log(`Shutting down: ${new Date().toISOString()}`);
            console.log(`WASM EXIT 0`);
        };
        console.log(`globalThis.collectAndUpload method created ${new Date().toISOString()}`);
        """;
        up = up.Replace("${traceCommand}", traceCommand).Replace("${filename}", fileName);
        await page.EvaluateAsync(up);
    }

    private async Task ClickAndCollect(IPage page)
    {
        // Use void to prevent Playwright from awaiting the returned Promise,
        // so tracing runs in parallel with button clicks below.
        await page.EvaluateAsync(@"void (globalThis.donePromise = globalThis.collectAndUpload())");
        _testOutput.WriteLine($"Installed script: {DateTime.Now.ToString("O")}");

        // Click the button a few times while tracing is running
        for (int i = 0; i < 5; i++)
        {
            await page.Locator("text=\"Click me\"").ClickAsync();
            await Task.Delay(10);
        }
        _testOutput.WriteLine($"Done clicking: {DateTime.Now.ToString("O")}");

        var txt2 = await page.Locator("p[role='status']").InnerHTMLAsync();
        Assert.NotEqual("Current count: 0", txt2);

        // Wait for trace collection and upload to complete
        await page.EvaluateAsync(@"globalThis.donePromise");
    }
}
