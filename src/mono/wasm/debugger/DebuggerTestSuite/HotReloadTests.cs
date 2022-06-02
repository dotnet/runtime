// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WebAssembly.Diagnostics;
using Newtonsoft.Json.Linq;
using System.IO;
using Xunit;
using System.Threading;

namespace DebuggerTests
{
    public class HotReloadTests : DebuggerTests
    {
        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task DebugHotReloadMethodChangedUserBreak()
        {
            var pause_location = await LoadAssemblyAndTestHotReload(
                    Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.dll"),
                    Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.pdb"),
                    Path.Combine(DebuggerTestAppPath, "../wasm/ApplyUpdateReferencedAssembly.dll"),
                    "MethodBody1", "StaticMethod1");
            var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "a", 10);
            pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 12, 16, "StaticMethod1");
            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "b", 15);
            pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 12, 12, "StaticMethod1");
            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            await CheckBool(locals, "c", true);
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task DebugHotReloadMethodUnchanged()
        {
            var pause_location = await LoadAssemblyAndTestHotReload(
                    Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.dll"),
                    Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.pdb"),
                    Path.Combine(DebuggerTestAppPath, "../wasm/ApplyUpdateReferencedAssembly.dll"),
                    "MethodBody2", "StaticMethod1");
            var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "a", 10);
            pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 21, 12, "StaticMethod1");
            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "a", 10);
            pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 21, 12, "StaticMethod1");
            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "a", 10);
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task DebugHotReloadMethodAddBreakpoint()
        {
            int line = 30;
            await SetBreakpoint(".*/MethodBody1.cs$", line, 12, use_regex: true);
            var pause_location = await LoadAssemblyAndTestHotReload(
                    Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.dll"),
                    Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.pdb"),
                    Path.Combine(DebuggerTestAppPath, "../wasm/ApplyUpdateReferencedAssembly.dll"),
                    "MethodBody3", "StaticMethod3");

            var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "a", 10);
            pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 30, 12, "StaticMethod3");
            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "b", 15);

            pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 30, 12, "StaticMethod3");
            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            await CheckBool(locals, "c", true);

            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 31, 12, "StaticMethod3",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "d", 10);
                    await Task.CompletedTask;
                }
            );
            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 32, 12, "StaticMethod3",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "d", 10);
                    CheckNumber(locals, "e", 20);
                    await Task.CompletedTask;
                }
            );
            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 33, 8, "StaticMethod3",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "d", 10);
                    CheckNumber(locals, "e", 20);
                    CheckNumber(locals, "f", 50);
                    await Task.CompletedTask;
                }
            );
        }


        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task DebugHotReloadMethodEmpty()
        {
            int line = 38;
            await SetBreakpoint(".*/MethodBody1.cs$", line, 0, use_regex: true);
            var pause_location = await LoadAssemblyAndTestHotReload(
                    Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.dll"),
                    Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.pdb"),
                    Path.Combine(DebuggerTestAppPath, "../wasm/ApplyUpdateReferencedAssembly.dll"),
                    "MethodBody4", "StaticMethod4");

            var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 38, 12, "StaticMethod4");
            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 39, 12, "StaticMethod4",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "a", 10);
                    await Task.CompletedTask;
                }
            );
            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 40, 12, "StaticMethod4",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "a", 10);
                    CheckNumber(locals, "b", 20);
                    await Task.CompletedTask;
                }
            );
            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 41, 12, "StaticMethod4",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "a", 10);
                    CheckNumber(locals, "b", 20);
                    await Task.CompletedTask;
                }
            );
            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 42, 12, "StaticMethod4",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "a", 10);
                    CheckNumber(locals, "b", 20);
                    await Task.CompletedTask;
                }
            );
            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 43, 8, "StaticMethod4",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "a", 10);
                    CheckNumber(locals, "b", 20);
                    await Task.CompletedTask;
                }
            );
            pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 38, 8, "StaticMethod4");
            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task DebugHotReloadMethodChangedUserBreakUsingSDB()
        {
            string asm_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.dll");
            string pdb_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.pdb");
            string asm_file_hot_reload = Path.Combine(DebuggerTestAppPath, "../wasm/ApplyUpdateReferencedAssembly.dll");

            var pause_location = await LoadAssemblyAndTestHotReloadUsingSDBWithoutChanges(
                    asm_file, pdb_file, "MethodBody1", "StaticMethod1");

            var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "a", 10);
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "MethodBody1", "StaticMethod1", 1);

            JToken top_frame = pause_location["callFrames"]?[0];
            AssertEqual("StaticMethod1", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 12, 16, scripts, top_frame["location"]);

            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "b", 15);
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "MethodBody1", "StaticMethod1", 2);

            top_frame = pause_location["callFrames"]?[0];
            AssertEqual("StaticMethod1", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 12, 12, scripts, top_frame["location"]);

            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            await CheckBool(locals, "c", true);
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task DebugHotReloadMethodUnchangedUsingSDB()
        {
            string asm_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.dll");
            string pdb_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.pdb");
            string asm_file_hot_reload = Path.Combine(DebuggerTestAppPath, "../wasm/ApplyUpdateReferencedAssembly.dll");

            var pause_location = await LoadAssemblyAndTestHotReloadUsingSDBWithoutChanges(
                    asm_file, pdb_file, "MethodBody2", "StaticMethod1");

            var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "a", 10);
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "MethodBody2", "StaticMethod1", 1);

            JToken top_frame = pause_location["callFrames"]?[0];
            AssertEqual("StaticMethod1", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 21, 12, scripts, top_frame["location"]);

            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "a", 10);
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "MethodBody2", "StaticMethod1", 2);

            top_frame = pause_location["callFrames"]?[0];
            AssertEqual("StaticMethod1", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 21, 12, scripts, top_frame["location"]);
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task DebugHotReloadMethodAddBreakpointUsingSDB()
        {
            string asm_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.dll");
            string pdb_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.pdb");
            string asm_file_hot_reload = Path.Combine(DebuggerTestAppPath, "../wasm/ApplyUpdateReferencedAssembly.dll");

            int line = 30;
            await SetBreakpoint(".*/MethodBody1.cs$", line, 12, use_regex: true);
            var pause_location = await LoadAssemblyAndTestHotReloadUsingSDBWithoutChanges(
                    asm_file, pdb_file, "MethodBody3", "StaticMethod3");

            var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "a", 10);

            //apply first update
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "MethodBody3", "StaticMethod3", 1);

            JToken top_frame = pause_location["callFrames"]?[0];
            AssertEqual("StaticMethod3", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 30, 12, scripts, top_frame["location"]);

            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "b", 15);

            //apply second update
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "MethodBody3", "StaticMethod3", 2);

            top_frame = pause_location["callFrames"]?[0];
            AssertEqual("StaticMethod3", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 30, 12, scripts, top_frame["location"]);

            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            await CheckBool(locals, "c", true);

            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 31, 12, "StaticMethod3",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "d", 10);
                    await Task.CompletedTask;
                }
            );
            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 32, 12, "StaticMethod3",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "d", 10);
                    CheckNumber(locals, "e", 20);
                    await Task.CompletedTask;
                }
            );
            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 33, 8, "StaticMethod3",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "d", 10);
                    CheckNumber(locals, "e", 20);
                    CheckNumber(locals, "f", 50);
                    await Task.CompletedTask;
                }
            );
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task DebugHotReloadMethodEmptyUsingSDB()
        {
            string asm_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.dll");
            string pdb_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.pdb");
            string asm_file_hot_reload = Path.Combine(DebuggerTestAppPath, "../wasm/ApplyUpdateReferencedAssembly.dll");

            int line = 38;
            await SetBreakpoint(".*/MethodBody1.cs$", line, 0, use_regex: true);
            var pause_location = await LoadAssemblyAndTestHotReloadUsingSDBWithoutChanges(
                    asm_file, pdb_file, "MethodBody4", "StaticMethod4");

            //apply first update
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "MethodBody4", "StaticMethod4", 1);

            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 39, 12, "StaticMethod4",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "a", 10);
                    await Task.CompletedTask;
                }
            );
            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 40, 12, "StaticMethod4",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "a", 10);
                    CheckNumber(locals, "b", 20);
                    await Task.CompletedTask;
                }
            );
            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 41, 12, "StaticMethod4",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "a", 10);
                    CheckNumber(locals, "b", 20);
                    await Task.CompletedTask;
                }
            );
            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 42, 12, "StaticMethod4",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "a", 10);
                    CheckNumber(locals, "b", 20);
                    await Task.CompletedTask;
                }
            );
            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 43, 8, "StaticMethod4",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "a", 10);
                    CheckNumber(locals, "b", 20);
                    await Task.CompletedTask;
                }
            );
            //pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 38, 8, "StaticMethod4");
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task DebugHotReloadMethod_CheckBreakpointLineUpdated_ByVS_Simulated()
        {
            string asm_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.dll");
            string pdb_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.pdb");
            string asm_file_hot_reload = Path.Combine(DebuggerTestAppPath, "../wasm/ApplyUpdateReferencedAssembly.dll");

            var bp = await SetBreakpoint(".*/MethodBody1.cs$", 48, 12, use_regex: true);
            var pause_location = await LoadAssemblyAndTestHotReloadUsingSDBWithoutChanges(
                    asm_file, pdb_file, "MethodBody5", "StaticMethod1");

            //apply first update
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "MethodBody5", "StaticMethod1", 1, 
                    rebindBreakpoint : async () =>
                    {
                        await RemoveBreakpoint(bp.Value["breakpointId"].Value<string>());
                        await SetBreakpoint(".*/MethodBody1.cs$", 49, 12, use_regex: true);
                    });

            JToken top_frame = pause_location["callFrames"]?[0];
            AssertEqual("StaticMethod1", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 49, 12, scripts, top_frame["location"]);
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task DebugHotReloadMethod_CheckBreakpointLineUpdated_ByVS_Simulated_ReceivingBreakpointBeforeUpdate2()
        {
            string asm_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.dll");
            string pdb_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.pdb");
            string asm_file_hot_reload = Path.Combine(DebuggerTestAppPath, "../wasm/ApplyUpdateReferencedAssembly.dll");

            var bp = await SetBreakpoint(".*/MethodBody1.cs$", 49, 12, use_regex: true);
            var pause_location = await LoadAssemblyAndTestHotReloadUsingSDBWithoutChanges(
                    asm_file, pdb_file, "MethodBody5", "StaticMethod1");

            //apply first update
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "MethodBody5", "StaticMethod1", 1, 
                    rebindBreakpoint : async () =>
                    {
                        await RemoveBreakpoint(bp.Value["breakpointId"].Value<string>());
                        await SetBreakpoint(".*/MethodBody1.cs$", 50, 12, use_regex: true);
                    },
                    rebindBeforeUpdates : true);

            JToken top_frame = pause_location["callFrames"]?[0];
            AssertEqual("StaticMethod1", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 50, 12, scripts, top_frame["location"]);
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task DebugHotReloadMethod_CheckBreakpointLineUpdated_ByVS_Simulated_ReceivingBreakpointBeforeUpdate_BPNotChanged()
        {
            string asm_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.dll");
            string pdb_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.pdb");
            string asm_file_hot_reload = Path.Combine(DebuggerTestAppPath, "../wasm/ApplyUpdateReferencedAssembly.dll");

            var bp_notchanged = await SetBreakpoint(".*/MethodBody1.cs$", 48, 12, use_regex: true);

            var pause_location = await LoadAssemblyAndTestHotReloadUsingSDBWithoutChanges(
                    asm_file, pdb_file, "MethodBody5", "StaticMethod1");

            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 48, 12, scripts, pause_location["callFrames"]?[0]["location"]);
            //apply first update
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "MethodBody5", "StaticMethod1", 1);

            JToken top_frame = pause_location["callFrames"]?[0];
            AssertEqual("StaticMethod1", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 48, 12, scripts, top_frame["location"]);
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task DebugHotReloadMethod_AddingNewMethod()
        {
            string asm_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.dll");
            string pdb_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.pdb");
            string asm_file_hot_reload = Path.Combine(DebuggerTestAppPath, "../wasm/ApplyUpdateReferencedAssembly.dll");

            var bp_notchanged = await SetBreakpoint(".*/MethodBody1.cs$", 55, 12, use_regex: true);
            var bp_invalid = await SetBreakpoint(".*/MethodBody1.cs$", 59, 12, use_regex: true);

            var pause_location = await LoadAssemblyAndTestHotReloadUsingSDBWithoutChanges(
                    asm_file, pdb_file, "MethodBody6", "StaticMethod1");

            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 55, 12, scripts, pause_location["callFrames"]?[0]["location"]);
            //apply first update
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "MethodBody6", "NewMethodStatic", 1);

            JToken top_frame = pause_location["callFrames"]?[0];
            AssertEqual("NewMethodStatic", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 59, 12, scripts, top_frame["location"]);
            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 60, 12, "NewMethodStatic",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "i", 20);
                    await Task.CompletedTask;
                }
            );
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task DebugHotReloadMethod_AddingNewStaticField()
        {
            string asm_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.dll");
            string pdb_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.pdb");
            string asm_file_hot_reload = Path.Combine(DebuggerTestAppPath, "../wasm/ApplyUpdateReferencedAssembly.dll");

            var bp_notchanged = await SetBreakpoint(".*/MethodBody1.cs$", 55, 12, use_regex: true);
            var bp_invalid = await SetBreakpoint(".*/MethodBody1.cs$", 59, 12, use_regex: true);

            var pause_location = await LoadAssemblyAndTestHotReloadUsingSDBWithoutChanges(
                    asm_file, pdb_file, "MethodBody6", "StaticMethod1");

            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 55, 12, scripts, pause_location["callFrames"]?[0]["location"]);
            //apply first update
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "MethodBody6", "NewMethodStatic", 1);

            JToken top_frame = pause_location["callFrames"]?[0];
            AssertEqual("NewMethodStatic", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 59, 12, scripts, top_frame["location"]);

            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 61, 12, "NewMethodStatic",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "i", 20);
                    await Task.CompletedTask;
                }, times: 2
            );
            await EvaluateOnCallFrameAndCheck(top_frame["callFrameId"].Value<string>(),
                   ("ApplyUpdateReferencedAssembly.MethodBody6.newStaticField", TNumber(10)));

        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task DebugHotReloadMethod_AddingNewClass()
        {
            string asm_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.dll");
            string pdb_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.pdb");
            string asm_file_hot_reload = Path.Combine(DebuggerTestAppPath, "../wasm/ApplyUpdateReferencedAssembly.dll");

            var bp_notchanged = await SetBreakpoint(".*/MethodBody1.cs$", 55, 12, use_regex: true);
            var bp_invalid = await SetBreakpoint(".*/MethodBody1.cs$", 73, 12, use_regex: true);
            var bp_invalid1 = await SetBreakpoint(".*/MethodBody1.cs$", 83, 12, use_regex: true);
            var bp_invalid2 = await SetBreakpoint(".*/MethodBody1.cs$", 102, 12, use_regex: true);

            var pause_location = await LoadAssemblyAndTestHotReloadUsingSDBWithoutChanges(
                    asm_file, pdb_file, "MethodBody6", "StaticMethod1");

            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 55, 12, scripts, pause_location["callFrames"]?[0]["location"]);
            //apply first update
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "MethodBody7", "StaticMethod1", 1);

            JToken top_frame = pause_location["callFrames"]?[0];
            AssertEqual("StaticMethod1", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 73, 12, scripts, top_frame["location"]);

            pause_location = await StepAndCheck(StepKind.Resume, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 83, 12, "InstanceMethod",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "aLocal", 50);
                    await Task.CompletedTask;
                }
            );

            var props = await GetObjectOnFrame(pause_location["callFrames"][0], "this");
            Assert.Equal(3, props.Count());
            CheckNumber(props, "attr1", 15);
            await CheckString(props, "attr2", "20");
            
            await EvaluateOnCallFrameAndCheck(pause_location["callFrames"]?[0]["callFrameId"].Value<string>(),
            ("ApplyUpdateReferencedAssembly.MethodBody7.staticField", TNumber(80)));

            //apply second update
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "MethodBody8", "StaticMethod1", 2);

            top_frame = pause_location["callFrames"]?[0];
            AssertEqual("InstanceMethod", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 102, 12, scripts, top_frame["location"]);

            await EvaluateOnCallFrameAndCheck(pause_location["callFrames"]?[0]["callFrameId"].Value<string>(),
            ("ApplyUpdateReferencedAssembly.MethodBody8.staticField", TNumber(80)));
        }
    }
}
