// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace DebuggerTests
{
    public class MonoJsTests : DebuggerTests
    {
        public MonoJsTests(ITestOutputHelper testOutput) : base(testOutput)
        {}

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task BadRaiseDebugEventsTest()
        {
            var bad_expressions = new[]
            {
                    "getDotnetRuntime(0).INTERNAL.mono_wasm_raise_debug_event('')",
                    "getDotnetRuntime(0).INTERNAL.mono_wasm_raise_debug_event(undefined)",
                    "getDotnetRuntime(0).INTERNAL.mono_wasm_raise_debug_event({})",

                    "getDotnetRuntime(0).INTERNAL.mono_wasm_raise_debug_event({eventName:'foo'}, '')",
                    "getDotnetRuntime(0).INTERNAL.mono_wasm_raise_debug_event({eventName:'foo'}, 12)"
                };

            foreach (var expression in bad_expressions)
            {
                var res = await cli.SendCommand($"Runtime.evaluate",
                            JObject.FromObject(new
                            {
                                expression,
                                returnByValue = true
                            }), token);
                Assert.False(res.IsOk, $"Expected to fail for {expression}");
            }
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(null)]
        public async Task RaiseDebugEventTraceTest(bool? trace)
        {
            var tcs = new TaskCompletionSource<bool>();
            insp.On("Runtime.consoleAPICalled", async (args, token) =>
            {
                if (args?["type"]?.Value<string>() == "debug" &&
                   args?["args"]?.Type == JTokenType.Array &&
                   args?["args"]?[0]?["value"]?.Value<string>()?.StartsWith("mono_wasm_debug_event_raised:") == true)
                {
                    tcs.SetResult(true);
                }

                return tcs.Task.IsCompleted
                            ?  await Task.FromResult(ProtocolEventHandlerReturn.RemoveHandler)
                            :  await Task.FromResult(ProtocolEventHandlerReturn.KeepHandler);
            });

            var trace_str = trace.HasValue ? $"trace: {trace.ToString().ToLower()}" : String.Empty;
            var expression = $"getDotnetRuntime(0).INTERNAL.mono_wasm_raise_debug_event({{ eventName:'qwe' }}, {{ {trace_str} }})";
            var res = await cli.SendCommand($"Runtime.evaluate", JObject.FromObject(new { expression }), token);
            Assert.True(res.IsOk, $"Expected to pass for {expression}");

            var t = await Task.WhenAny(tcs.Task, Task.Delay(2000));

            if (trace == true)
                Assert.True(tcs.Task == t, "Timed out waiting for the event to be logged");
            else
                Assert.False(tcs.Task == t, "Event should not have been logged");
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData(true, 1)]
        [InlineData(false, 0)]
        public async Task DuplicateAssemblyLoadedEventNotLoadedFromBundle(bool load_pdb, int expected_count)
            => await AssemblyLoadedEventTest(
                "lazy-debugger-test",
                Path.Combine(DebuggerTestAppPath, "lazy-debugger-test.dll"),
                load_pdb ? Path.Combine(DebuggerTestAppPath, "lazy-debugger-test.pdb") : null,
                "/lazy-debugger-test.cs",
                expected_count
            );

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData(true, 1)]
        [InlineData(false, 1)] // Since it's being loaded from the bundle, it will have the pdb even if we don't provide one
        public async Task DuplicateAssemblyLoadedEventForAssemblyFromBundle(bool load_pdb, int expected_count)
            => await AssemblyLoadedEventTest(
                "debugger-test",
                Path.Combine(DebuggerTestAppPath, "_framework/debugger-test.dll"),
                load_pdb ? Path.Combine(DebuggerTestAppPath, "_framework/debugger-test.pdb") : null,
                "/debugger-test.cs",
                expected_count
            );

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task DuplicateAssemblyLoadedEventWithEmbeddedPdbNotLoadedFromBundle()
            => await AssemblyLoadedEventTest(
                "lazy-debugger-test-embedded",
                Path.Combine(DebuggerTestAppPath, "lazy-debugger-test-embedded.dll"),
                null,
                "/lazy-debugger-test-embedded.cs",
                expected_count: 1
            );

        async Task AssemblyLoadedEventTest(string asm_name, string asm_path, string pdb_path, string source_file, int expected_count)
        {
            int event_count = 0;
            var tcs = new TaskCompletionSource<bool>();
            insp.On("Debugger.scriptParsed", async (args, c) =>
            {
                try
                {
                    var url = args["url"]?.Value<string>();
                    if (url?.EndsWith(source_file) == true)
                    {
                        event_count++;
                        if (event_count > expected_count)
                            tcs.SetResult(false);
                    }
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }

                return tcs.Task.IsCompleted
                            ?  await Task.FromResult(ProtocolEventHandlerReturn.RemoveHandler)
                            :  await Task.FromResult(ProtocolEventHandlerReturn.KeepHandler);
            });

            byte[] bytes = File.Exists(asm_path) ? File.ReadAllBytes(asm_path) : File.ReadAllBytes(Path.ChangeExtension(asm_path, WebcilInWasmExtension)); // hack!
            string asm_base64 = Convert.ToBase64String(bytes);

            string pdb_base64 = String.Empty;
            if (pdb_path != null)
            {
                bytes = File.ReadAllBytes(pdb_path);
                pdb_base64 = Convert.ToBase64String(bytes);
            }

            var expression = $@"getDotnetRuntime(0).INTERNAL.mono_wasm_raise_debug_event({{
                    eventName: 'AssemblyLoaded',
                    assembly_name: '{asm_name}',
                    assembly_b64: '{asm_base64}',
                    pdb_b64: '{pdb_base64}'
                }});";

            var res = await cli.SendCommand($"Runtime.evaluate", JObject.FromObject(new { expression }), token);
            Assert.True(res.IsOk, $"Expected to pass for {expression}");

            res = await cli.SendCommand($"Runtime.evaluate", JObject.FromObject(new { expression }), token);
            Assert.True(res.IsOk, $"Expected to pass for {expression}");

            var t = await Task.WhenAny(tcs.Task, Task.Delay(2000));
            if (t.IsFaulted)
                throw t.Exception;

            Assert.True(event_count <= expected_count, $"number of scriptParsed events received. Expected: {expected_count}, Actual: {event_count}");
        }
    }
}
