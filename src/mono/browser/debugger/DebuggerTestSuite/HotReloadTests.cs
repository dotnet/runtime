// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace DebuggerTests
{
    public class HotReloadTests : DebuggerTests
    {
        public HotReloadTests(ITestOutputHelper testOutput) : base(testOutput)
        {}

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task DebugHotReloadMethodChangedUserBreak()
        {
            var pause_location = await LoadAssemblyAndTestHotReload(
                    Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.dll"),
                    Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.pdb"),
                    Path.Combine(DebuggerTestAppPath, "../wasm/ApplyUpdateReferencedAssembly.dll"),
                    "MethodBody1", "StaticMethod1", expectBpResolvedEvent: false, sourcesToWait: new string [] { "MethodBody0.cs", "MethodBody1.cs" });
            var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "a", 10);
            pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 12, 16, "ApplyUpdateReferencedAssembly.MethodBody1.StaticMethod1");
            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "b", 15);
            pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 12, 12, "ApplyUpdateReferencedAssembly.MethodBody1.StaticMethod1");
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
                    "MethodBody2", "StaticMethod1", expectBpResolvedEvent: false, sourcesToWait: new string [] { "MethodBody0.cs", "MethodBody1.cs" });
            var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "a", 10);
            pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 21, 12, "ApplyUpdateReferencedAssembly.MethodBody2.StaticMethod1");
            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "a", 10);
            pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 21, 12, "ApplyUpdateReferencedAssembly.MethodBody2.StaticMethod1");
            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "a", 10);
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData("ApplyUpdateReferencedAssembly")]
        [InlineData("ApplyUpdateReferencedAssemblyChineseCharInPath\u3128")]
        public async Task DebugHotReloadMethodAddBreakpoint(string assembly_name)
        {
            int line = 30;
            await SetBreakpoint(".*/MethodBody1.cs$", line, 12, use_regex: true);
            var pause_location = await LoadAssemblyAndTestHotReload(
                    Path.Combine(DebuggerTestAppPath, $"{assembly_name}.dll"),
                    Path.Combine(DebuggerTestAppPath, $"{assembly_name}.pdb"),
                    Path.Combine(DebuggerTestAppPath, $"../wasm/{assembly_name}.dll"),
                    "MethodBody3", "StaticMethod3", expectBpResolvedEvent: true, sourcesToWait: new string [] { "MethodBody0.cs", "MethodBody1.cs" });

            var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "a", 10);
            pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", $"dotnet://{assembly_name}.dll/MethodBody1.cs", 30, 12, "ApplyUpdateReferencedAssembly.MethodBody3.StaticMethod3");
            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "b", 15);

            pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", $"dotnet://{assembly_name}.dll/MethodBody1.cs", 30, 12, "ApplyUpdateReferencedAssembly.MethodBody3.StaticMethod3");
            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            await CheckBool(locals, "c", true);

            await StepAndCheck(StepKind.Over, $"dotnet://{assembly_name}.dll/MethodBody1.cs", 31, 12, "ApplyUpdateReferencedAssembly.MethodBody3.StaticMethod3",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "d", 10);
                    await Task.CompletedTask;
                }
            );
            await StepAndCheck(StepKind.Over, $"dotnet://{assembly_name}.dll/MethodBody1.cs", 32, 12, "ApplyUpdateReferencedAssembly.MethodBody3.StaticMethod3",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "d", 10);
                    CheckNumber(locals, "e", 20);
                    await Task.CompletedTask;
                }
            );
            await StepAndCheck(StepKind.Over, $"dotnet://{assembly_name}.dll/MethodBody1.cs", 33, 8, "ApplyUpdateReferencedAssembly.MethodBody3.StaticMethod3",
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
                    "MethodBody4", "StaticMethod4", expectBpResolvedEvent: true, sourcesToWait: new string [] { "MethodBody0.cs", "MethodBody1.cs" });

            var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 38, 12, "ApplyUpdateReferencedAssembly.MethodBody4.StaticMethod4");
            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 39, 12, "ApplyUpdateReferencedAssembly.MethodBody4.StaticMethod4",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "a", 10);
                    await Task.CompletedTask;
                }
            );
            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 40, 12, "ApplyUpdateReferencedAssembly.MethodBody4.StaticMethod4",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "a", 10);
                    CheckNumber(locals, "b", 20);
                    await Task.CompletedTask;
                }
            );
            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 41, 12, "ApplyUpdateReferencedAssembly.MethodBody4.StaticMethod4",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "a", 10);
                    CheckNumber(locals, "b", 20);
                    await Task.CompletedTask;
                }
            );
            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 42, 12, "ApplyUpdateReferencedAssembly.MethodBody4.StaticMethod4",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "a", 10);
                    CheckNumber(locals, "b", 20);
                    await Task.CompletedTask;
                }
            );
            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 43, 8, "ApplyUpdateReferencedAssembly.MethodBody4.StaticMethod4",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "a", 10);
                    CheckNumber(locals, "b", 20);
                    await Task.CompletedTask;
                }
            );
            pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 38, 8, "ApplyUpdateReferencedAssembly.MethodBody4.StaticMethod4");
            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task DebugHotReloadMethodChangedUserBreakUsingSDB()
        {
            string asm_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.dll");
            string pdb_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.pdb");
            string asm_file_hot_reload = Path.Combine(DebuggerTestAppPath, "../wasm/ApplyUpdateReferencedAssembly.dll");

            var pause_location = await LoadAssemblyAndTestHotReloadUsingSDBWithoutChanges(
                    asm_file, pdb_file, "MethodBody1", "StaticMethod1", expectBpResolvedEvent: false, sourcesToWait: new string [] { "MethodBody0.cs", "MethodBody1.cs" });

            var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "a", 10);
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "MethodBody1", "StaticMethod1", 1);

            JToken top_frame = pause_location["callFrames"]?[0];
            AssertEqual("ApplyUpdateReferencedAssembly.MethodBody1.StaticMethod1", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 12, 16, scripts, top_frame["location"]);

            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "b", 15);
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "MethodBody1", "StaticMethod1", 2);

            top_frame = pause_location["callFrames"]?[0];
            AssertEqual("ApplyUpdateReferencedAssembly.MethodBody1.StaticMethod1", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
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
                    asm_file, pdb_file, "MethodBody2", "StaticMethod1", expectBpResolvedEvent: false, sourcesToWait: new string [] { "MethodBody0.cs", "MethodBody1.cs" });

            var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "a", 10);
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "MethodBody2", "StaticMethod1", 1);

            JToken top_frame = pause_location["callFrames"]?[0];
            AssertEqual("ApplyUpdateReferencedAssembly.MethodBody2.StaticMethod1", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 21, 12, scripts, top_frame["location"]);

            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "a", 10);
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "MethodBody2", "StaticMethod1", 2);

            top_frame = pause_location["callFrames"]?[0];
            AssertEqual("ApplyUpdateReferencedAssembly.MethodBody2.StaticMethod1", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 21, 12, scripts, top_frame["location"]);
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData("ApplyUpdateReferencedAssembly")]
        [InlineData("ApplyUpdateReferencedAssemblyChineseCharInPath\u3128")]
        public async Task DebugHotReloadMethodAddBreakpointUsingSDB(string assembly_name)
        {
            string asm_file = Path.Combine(DebuggerTestAppPath, $"{assembly_name}.dll");
            string pdb_file = Path.Combine(DebuggerTestAppPath, $"{assembly_name}.pdb");
            string asm_file_hot_reload = Path.Combine(DebuggerTestAppPath, $"../wasm/{assembly_name}.dll");

            int line = 30;
            await SetBreakpoint(".*/MethodBody1.cs$", line, 12, use_regex: true);
            var pause_location = await LoadAssemblyAndTestHotReloadUsingSDBWithoutChanges(
                    asm_file, pdb_file, "MethodBody3", "StaticMethod3", expectBpResolvedEvent: true, sourcesToWait: new string [] { "MethodBody0.cs", "MethodBody1.cs" });

            var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "a", 10);

            //apply first update
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "MethodBody3", "StaticMethod3", 1);

            JToken top_frame = pause_location["callFrames"]?[0];
            AssertEqual("ApplyUpdateReferencedAssembly.MethodBody3.StaticMethod3", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
            CheckLocation($"dotnet://{assembly_name}.dll/MethodBody1.cs", 30, 12, scripts, top_frame["location"]);

            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckNumber(locals, "b", 15);

            //apply second update
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "MethodBody3", "StaticMethod3", 2);

            top_frame = pause_location["callFrames"]?[0];
            AssertEqual("ApplyUpdateReferencedAssembly.MethodBody3.StaticMethod3", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
            CheckLocation($"dotnet://{assembly_name}.dll/MethodBody1.cs", 30, 12, scripts, top_frame["location"]);

            locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            await CheckBool(locals, "c", true);

            await StepAndCheck(StepKind.Over, $"dotnet://{assembly_name}.dll/MethodBody1.cs", 31, 12, "ApplyUpdateReferencedAssembly.MethodBody3.StaticMethod3",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "d", 10);
                    await Task.CompletedTask;
                }
            );
            await StepAndCheck(StepKind.Over, $"dotnet://{assembly_name}.dll/MethodBody1.cs", 32, 12, "ApplyUpdateReferencedAssembly.MethodBody3.StaticMethod3",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "d", 10);
                    CheckNumber(locals, "e", 20);
                    await Task.CompletedTask;
                }
            );
            await StepAndCheck(StepKind.Over, $"dotnet://{assembly_name}.dll/MethodBody1.cs", 33, 8, "ApplyUpdateReferencedAssembly.MethodBody3.StaticMethod3",
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
                    asm_file, pdb_file, "MethodBody4", "StaticMethod4", expectBpResolvedEvent: true, sourcesToWait: new string [] { "MethodBody0.cs", "MethodBody1.cs" });

            //apply first update
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "MethodBody4", "StaticMethod4", 1);

            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 39, 12, "ApplyUpdateReferencedAssembly.MethodBody4.StaticMethod4",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "a", 10);
                    await Task.CompletedTask;
                }
            );
            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 40, 12, "ApplyUpdateReferencedAssembly.MethodBody4.StaticMethod4",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "a", 10);
                    CheckNumber(locals, "b", 20);
                    await Task.CompletedTask;
                }
            );
            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 41, 12, "ApplyUpdateReferencedAssembly.MethodBody4.StaticMethod4",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "a", 10);
                    CheckNumber(locals, "b", 20);
                    await Task.CompletedTask;
                }
            );
            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 42, 12, "ApplyUpdateReferencedAssembly.MethodBody4.StaticMethod4",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "a", 10);
                    CheckNumber(locals, "b", 20);
                    await Task.CompletedTask;
                }
            );
            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 43, 8, "ApplyUpdateReferencedAssembly.MethodBody4.StaticMethod4",
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
                    asm_file, pdb_file, "MethodBody5", "StaticMethod1", expectBpResolvedEvent: true, sourcesToWait: new string [] { "MethodBody0.cs", "MethodBody1.cs" });

            //apply first update
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "MethodBody5", "StaticMethod1", 1,
                    rebindBreakpoint : async () =>
                    {
                        await RemoveBreakpoint(bp.Value["breakpointId"].Value<string>());
                        await SetBreakpoint(".*/MethodBody1.cs$", 49, 12, use_regex: true);
                    });

            JToken top_frame = pause_location["callFrames"]?[0];
            AssertEqual("ApplyUpdateReferencedAssembly.MethodBody5.StaticMethod1", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
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
                    asm_file, pdb_file, "MethodBody5", "StaticMethod1", expectBpResolvedEvent: true, sourcesToWait: new string [] { "MethodBody0.cs", "MethodBody1.cs" });

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
            AssertEqual("ApplyUpdateReferencedAssembly.MethodBody5.StaticMethod1", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
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
                    asm_file, pdb_file, "MethodBody5", "StaticMethod1", expectBpResolvedEvent: true, sourcesToWait: new string [] { "MethodBody0.cs", "MethodBody1.cs" });

            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 48, 12, scripts, pause_location["callFrames"]?[0]["location"]);
            //apply first update
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "MethodBody5", "StaticMethod1", 1);

            JToken top_frame = pause_location["callFrames"]?[0];
            AssertEqual("ApplyUpdateReferencedAssembly.MethodBody5.StaticMethod1", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
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
                    asm_file, pdb_file, "MethodBody6", "StaticMethod1", expectBpResolvedEvent: true, sourcesToWait: new string [] { "MethodBody0.cs", "MethodBody1.cs" });

            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 55, 12, scripts, pause_location["callFrames"]?[0]["location"]);
            //apply first update
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "MethodBody6", "NewMethodStatic", 1);

            JToken top_frame = pause_location["callFrames"]?[0];
            AssertEqual("ApplyUpdateReferencedAssembly.MethodBody6.NewMethodStatic", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 59, 12, scripts, top_frame["location"]);
            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 60, 12, "ApplyUpdateReferencedAssembly.MethodBody6.NewMethodStatic",
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
                    asm_file, pdb_file, "MethodBody6", "StaticMethod1", expectBpResolvedEvent: true, sourcesToWait: new string [] { "MethodBody0.cs", "MethodBody1.cs" });

            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 55, 12, scripts, pause_location["callFrames"]?[0]["location"]);
            //apply first update
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "MethodBody6", "NewMethodStatic", 1);

            JToken top_frame = pause_location["callFrames"]?[0];
            AssertEqual("ApplyUpdateReferencedAssembly.MethodBody6.NewMethodStatic", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 59, 12, scripts, top_frame["location"]);

            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 61, 12, "ApplyUpdateReferencedAssembly.MethodBody6.NewMethodStatic",
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
                    asm_file, pdb_file, "MethodBody6", "StaticMethod1", expectBpResolvedEvent: true, sourcesToWait: new string [] { "MethodBody0.cs", "MethodBody1.cs" });

            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 55, 12, scripts, pause_location["callFrames"]?[0]["location"]);
            //apply first update
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "MethodBody7", "StaticMethod1", 1);

            JToken top_frame = pause_location["callFrames"]?[0];
            AssertEqual("ApplyUpdateReferencedAssembly.MethodBody7.StaticMethod1", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 73, 12, scripts, top_frame["location"]);

            pause_location = await StepAndCheck(StepKind.Resume, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 83, 12, "ApplyUpdateReferencedAssembly.MethodBody7.InstanceMethod",
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
            AssertEqual("ApplyUpdateReferencedAssembly.MethodBody8.InstanceMethod", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 102, 12, scripts, top_frame["location"]);

            await EvaluateOnCallFrameAndCheck(pause_location["callFrames"]?[0]["callFrameId"].Value<string>(),
            ("ApplyUpdateReferencedAssembly.MethodBody8.staticField", TNumber(80)));
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task DebugHotReloadMethod_AddingNewMethodWithoutAnyOtherChange()
        {
            string asm_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly2.dll");
            string pdb_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly2.pdb");
            string asm_file_hot_reload = Path.Combine(DebuggerTestAppPath, "../wasm/ApplyUpdateReferencedAssembly2.dll");

            var pause_location = await LoadAssemblyAndTestHotReloadUsingSDBWithoutChanges(
                    asm_file, pdb_file, "AddMethod", "StaticMethod1", expectBpResolvedEvent: false, sourcesToWait: new string [] { "MethodBody2.cs" });
            CheckLocation("dotnet://ApplyUpdateReferencedAssembly2.dll/MethodBody2.cs", 12, 12, scripts, pause_location["callFrames"]?[0]["location"]);
            //apply first update
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "AddMethod", "StaticMethod2", 1);

            JToken top_frame = pause_location["callFrames"]?[0];
            AssertEqual("ApplyUpdateReferencedAssembly.AddMethod.StaticMethod2", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
            CheckLocation("dotnet://ApplyUpdateReferencedAssembly2.dll/MethodBody2.cs", 18, 12, scripts, top_frame["location"]);
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task DebugHotReloadMethod_AddingNewMethodWithoutAnyOtherChange_WithoutSDB()
        {
            var pause_location = await LoadAssemblyAndTestHotReload(
                    Path.Combine(DebuggerTestAppPath, $"ApplyUpdateReferencedAssembly2.dll"),
                    Path.Combine(DebuggerTestAppPath, $"ApplyUpdateReferencedAssembly2.pdb"),
                    Path.Combine(DebuggerTestAppPath, $"../wasm/ApplyUpdateReferencedAssembly2.dll"),
                    "AddMethod", "StaticMethod1", expectBpResolvedEvent: false, sourcesToWait: new string [] { "MethodBody2.cs" }, "StaticMethod2");
            CheckLocation("dotnet://ApplyUpdateReferencedAssembly2.dll/MethodBody2.cs", 12, 12, scripts, pause_location["callFrames"]?[0]["location"]);
            await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", $"dotnet://ApplyUpdateReferencedAssembly2.dll/MethodBody2.cs", 18, 12, "ApplyUpdateReferencedAssembly.AddMethod.StaticMethod2");
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task DebugHotReload_NewInstanceFields()
        {
            string asm_name = "ApplyUpdateReferencedAssembly3";
            string asm_file = Path.Combine (DebuggerTestAppPath, asm_name + ".dll");
            string pdb_file = Path.Combine (DebuggerTestAppPath, asm_name + ".pdb");
            string asm_file_hot_reload = Path.Combine (DebuggerTestAppPath, "..", "wasm", asm_name+ ".dll");
            var pause_location = await LoadAssemblyAndTestHotReload(asm_file, pdb_file, asm_file_hot_reload, "AddInstanceFields", "StaticMethod1",
                                                                    expectBpResolvedEvent: false, sourcesToWait: new string [] { "MethodBody2.cs" });
            var frame = pause_location["callFrames"][0];
            var locals = await GetProperties(frame["callFrameId"].Value<string>());
            await CheckObject(locals, "c", "ApplyUpdateReferencedAssembly.AddInstanceFields.C");
            await SendCommandAndCheck (JObject.FromObject(new { }), "Debugger.resume", script_loc: null, line: -1, column: -1, function_name: null,
                                       locals_fn: async (locals) => {
                                           await CheckObject(locals, "c", "ApplyUpdateReferencedAssembly.AddInstanceFields.C");
                                           var c = await GetObjectOnLocals(locals, "c");
                                           await CheckProps (c, new {
                                                           Field1 = TNumber(123),
                                                   }, "c", num_fields: 1);
                                           var cObj = GetAndAssertObjectWithName (locals, "c");
                                           await SetValueOnObject (cObj, "Field1", "456.5");

                                           c = await GetObjectOnLocals(locals, "c");
                                           await CheckProps (c, new {
                                                           Field1 = TNumber("456.5", isDecimal: true),
                                                   }, "c", num_fields: 1);
                                       });
            await SendCommandAndCheck (JObject.FromObject(new { }), "Debugger.resume", script_loc: null, line: -1, column: -1, function_name: null,
                                       locals_fn: async (locals) => {
                                           await CheckObject(locals, "c", "ApplyUpdateReferencedAssembly.AddInstanceFields.C");
                                           var c = await GetObjectOnLocals(locals, "c");
                                           await CheckProps (c, new {
                                                           Field1 = TNumber(123),
                                                           Field2 = TString("spqr"),
                                                           Field3Unused = TString(null),
                                                   }, "c", num_fields: 3);
                                       });
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task DebugHotReloadMethod_AddingNewClassUsingDebugAttribute()
        {
            await SetJustMyCode(true);
            string asm_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.dll");
            string pdb_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.pdb");
            string asm_file_hot_reload = Path.Combine(DebuggerTestAppPath, "../wasm/ApplyUpdateReferencedAssembly.dll");

            await SetBreakpoint(".*/MethodBody1.cs$", 55, 12, use_regex: true);

            var pause_location = await LoadAssemblyAndTestHotReloadUsingSDBWithoutChanges(
                    asm_file, pdb_file, "MethodBody6", "StaticMethod1", expectBpResolvedEvent: true, sourcesToWait: new string [] { "MethodBody0.cs", "MethodBody1.cs" });

            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 55, 12, scripts, pause_location["callFrames"]?[0]["location"]);
            await SetBreakpoint(".*/MethodBody1.cs$", 118, 12, use_regex: true);            
            //apply first update
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "MethodBody10", "StaticMethod1", 1);

            JToken top_frame = pause_location["callFrames"]?[0];
            AssertEqual("ApplyUpdateReferencedAssembly.MethodBody10.StaticMethod1", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 118, 12, scripts, top_frame["location"]);

            await StepAndCheck(StepKind.Into, $"dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 119, 12, "ApplyUpdateReferencedAssembly.MethodBody10.StaticMethod1");
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task DebugHotReloadMethod_AddingNewMethodAndThrowException()
        {
            //await SetPauseOnException("all");
            string asm_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.dll");
            string pdb_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.pdb");
            string asm_file_hot_reload = Path.Combine(DebuggerTestAppPath, "../wasm/ApplyUpdateReferencedAssembly.dll");

            var bp_notchanged = await SetBreakpoint(".*/MethodBody1.cs$", 129, 12, use_regex: true);
            var bp_invalid = await SetBreakpoint(".*/MethodBody1.cs$", 133, 12, use_regex: true);

            var pause_location = await LoadAssemblyAndTestHotReloadUsingSDBWithoutChanges(
                    asm_file, pdb_file, "MethodBody11", "StaticMethod1", expectBpResolvedEvent: true, sourcesToWait: new string [] { "MethodBody0.cs", "MethodBody1.cs" });

            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 129, 12, scripts, pause_location["callFrames"]?[0]["location"]);
            //apply first update
            pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
                    asm_file_hot_reload, "MethodBody11", "NewMethodStaticWithThrow", 1);

            JToken top_frame = pause_location["callFrames"]?[0];
            AssertEqual("ApplyUpdateReferencedAssembly.MethodBody11.NewMethodStaticWithThrow", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
            CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 133, 12, scripts, top_frame["location"]);
            await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 134, 12, "ApplyUpdateReferencedAssembly.MethodBody11.NewMethodStaticWithThrow",
            locals_fn: async (locals) =>
                {
                    CheckNumber(locals, "i", 20);
                    await Task.CompletedTask;
                }
            );

            await SetPauseOnException("all");

            pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", null, 0, 0, null);

            await CheckValue(pause_location["data"], JObject.FromObject(new
            {
                type = "object",
                subtype = "error",
                className = "System.Exception",
                uncaught = false
            }), "exception0.data");

            pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", null, 0, 0, null);
            try
            {
                pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", null, 0, 0, null);
            }
            catch (System.Exception ae)
            {
                System.Console.WriteLine(ae);
                var eo = JObject.Parse(ae.Message);

                AssertEqual("Uncaught", eo["exceptionDetails"]?["text"]?.Value<string>(), "text");

                await CheckValue(eo["exceptionDetails"]?["exception"], JObject.FromObject(new
                {
                    type = "object",
                    subtype = "error",
                    className = "Error"
                }), "exception");
            }
        }
        // Enable this test when https://github.com/dotnet/hotreload-utils/pull/264 flows into dotnet/runtime repo
        // [ConditionalFact(nameof(RunningOnChrome))]
        // public async Task DebugHotReloadMethod_ChangeParameterName()
        // {
        //     string asm_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.dll");
        //     string pdb_file = Path.Combine(DebuggerTestAppPath, "ApplyUpdateReferencedAssembly.pdb");
        //     string asm_file_hot_reload = Path.Combine(DebuggerTestAppPath, "../wasm/ApplyUpdateReferencedAssembly.dll");

        //     var bp_notchanged = await SetBreakpoint(".*/MethodBody1.cs$", 89, 12, use_regex: true);
        //     // var bp_invalid = await SetBreakpoint(".*/MethodBody1.cs$", 59, 12, use_regex: true);

        //     var pause_location = await LoadAssemblyAndTestHotReloadUsingSDBWithoutChanges(
        //             asm_file, pdb_file, "MethodBody9", "test", expectBpResolvedEvent: true, sourcesToWait: new string [] { "MethodBody0.cs", "MethodBody1.cs" });

        //     CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 89, 12, scripts, pause_location["callFrames"]?[0]["location"]);
        //     await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 90, 12, "ApplyUpdateReferencedAssembly.MethodBody9.M1",
        //     locals_fn: async (locals) =>
        //         {
        //             CheckNumber(locals, "a", 1);
        //             await Task.CompletedTask;
        //         }
        //     );
        //     //apply first update
        //     pause_location = await LoadAssemblyAndTestHotReloadUsingSDB(
        //             asm_file_hot_reload, "MethodBody9", "test", 1);

        //     JToken top_frame = pause_location["callFrames"]?[0];
        //     AssertEqual("ApplyUpdateReferencedAssembly.MethodBody9.M1", top_frame?["functionName"]?.Value<string>(), top_frame?.ToString());
        //     CheckLocation("dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 89, 12, scripts, top_frame["location"]);
        //     await StepAndCheck(StepKind.Over, "dotnet://ApplyUpdateReferencedAssembly.dll/MethodBody1.cs", 90, 12, "ApplyUpdateReferencedAssembly.MethodBody9.M1",
        //     locals_fn: async (locals) =>
        //         {
        //             CheckNumber(locals, "x", 1);
        //             await Task.CompletedTask;
        //         }
        //     );
        // }
    }
}
