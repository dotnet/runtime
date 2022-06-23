// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WebAssembly.Diagnostics;
using Newtonsoft.Json.Linq;
using Xunit;

namespace DebuggerTests
{
    public class SetVariableValueTests : DebuggerTests
    {
        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData("a", 1, 30, 130)]
        [InlineData("a", 1, -30, -130)]
        [InlineData("a1", 1, 20, -1)]
        [InlineData("a1", 1, 20, 256)]
        [InlineData("b", 2, 200, -32769)]
        [InlineData("b", 2, -500, 32769)]
        [InlineData("b2", 3, 30000, -500)]
        [InlineData("b2", 3, 30000, 65550)]
        [InlineData("d", 5, 70, -2147483649)]
        [InlineData("d", 5, 70, 2147483648)]
        [InlineData("d2", 6, 70, -50)]
        [InlineData("d2", 6, 70, 4294967296)]
        public async Task SetLocalPrimitiveTypeVariableOutOfRange(string variableName, long originalValue, long newValue, long overflowValue) {
            await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.SetVariableLocals", "run", 12);
            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() {{ invoke_static_method_async('[debugger-test] DebuggerTests.SetVariableLocals:run');}}, 1);",
                "dotnet://debugger-test.dll/debugger-set-variable-value-test.cs", 22, 12, "run",
                locals_fn: async (locals) =>
                {
                    CheckNumber(locals, variableName, originalValue);
                    await Task.CompletedTask;
                }
            );
            var callFrameId = pause_location["callFrames"][0]["callFrameId"].Value<string>();

            await SetVariableValueOnCallFrame( JObject.FromObject(new {callFrameId, variableName, newValue=JObject.FromObject(new {value=newValue}) }));

            pause_location = await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-set-variable-value-test.cs", 23, 12, "run",
                locals_fn: async (locals) =>
                {
                    CheckNumber(locals, variableName, newValue);
                    await Task.CompletedTask;
                }
            );

            callFrameId = pause_location["callFrames"][0]["callFrameId"].Value<string>();

            await SetVariableValueOnCallFrame( JObject.FromObject(new {callFrameId, variableName, newValue=JObject.FromObject(new {value=overflowValue}) }), false);

            pause_location = await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-set-variable-value-test.cs", 24, 8, "run",
                locals_fn: async (locals) =>
                {
                    CheckNumber(locals, variableName, newValue);
                    await Task.CompletedTask;
                }
            );
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData("f", 9, 150.15616, 0.4564)]
        [InlineData("f", 9, -454.54654, -0.5648)]
        public async Task SetLocalFloatVariable(string variableName, float originalValue, float newValue, float newValue2) {
            await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.SetVariableLocals", "run", 12);
            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() {{ invoke_static_method_async('[debugger-test] DebuggerTests.SetVariableLocals:run');}}, 1);",
                "dotnet://debugger-test.dll/debugger-set-variable-value-test.cs", 22, 12, "run",
                locals_fn: async (locals) =>
                {
                    CheckNumber(locals, variableName, originalValue);
                    await Task.CompletedTask;
                }
            );
            var callFrameId = pause_location["callFrames"][0]["callFrameId"].Value<string>();

            await SetVariableValueOnCallFrame( JObject.FromObject(new {callFrameId, variableName, newValue=JObject.FromObject(new {value=newValue}) }));

            pause_location = await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-set-variable-value-test.cs", 23, 12, "run",
                locals_fn: async (locals) =>
                {
                    CheckNumber(locals, variableName, newValue);
                    await Task.CompletedTask;
                }
            );

            callFrameId = pause_location["callFrames"][0]["callFrameId"].Value<string>();

            await SetVariableValueOnCallFrame( JObject.FromObject(new {callFrameId, variableName, newValue=JObject.FromObject(new {value=newValue2}) }));

            pause_location = await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-set-variable-value-test.cs", 24, 8, "run",
                locals_fn: async (locals) =>
                {
                    CheckNumber(locals, variableName, newValue2);
                    await Task.CompletedTask;
                }
            );
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData("g", 10, 150.15615844726562, 0.4564000070095062)]
        [InlineData("g", 10, -454.5465393066406, -0.5648000240325928)]
        public async Task SetLocalDoubleVariable(string variableName, double originalValue, double newValue, double newValue2) {
            await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.SetVariableLocals", "run", 12);
            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() {{ invoke_static_method_async('[debugger-test] DebuggerTests.SetVariableLocals:run');}}, 1);",
                "dotnet://debugger-test.dll/debugger-set-variable-value-test.cs", 22, 12, "run",
                locals_fn: async (locals) =>
                {
                    CheckNumber(locals, variableName, originalValue);
                    await Task.CompletedTask;
                }
            );
            var callFrameId = pause_location["callFrames"][0]["callFrameId"].Value<string>();

            await SetVariableValueOnCallFrame( JObject.FromObject(new {callFrameId, variableName, newValue=JObject.FromObject(new {value=newValue}) }));

            pause_location = await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-set-variable-value-test.cs", 23, 12, "run",
                locals_fn: async (locals) =>
                {
                    CheckNumber(locals, variableName, newValue);
                    await Task.CompletedTask;
                }
            );

            callFrameId = pause_location["callFrames"][0]["callFrameId"].Value<string>();

            await SetVariableValueOnCallFrame( JObject.FromObject(new {callFrameId, variableName, newValue=JObject.FromObject(new {value=newValue2}) }));

            pause_location = await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-set-variable-value-test.cs", 24, 8, "run",
                locals_fn: async (locals) =>
                {
                    CheckNumber(locals, variableName, newValue2);
                    await Task.CompletedTask;
                }
            );
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData("a", "1", "30", "127")]
        [InlineData("a", "1", "-30", "-128")]
        [InlineData("a1", "1", "20", "0")]
        [InlineData("a1", "1", "20", "255")]
        [InlineData("b", "2", "200", "-32768")]
        [InlineData("b", "2", "-500", "32767")]
        [InlineData("b2", "3", "30000", "65535")]
        [InlineData("d", "5", "70", "-2147483648")]
        [InlineData("d", "5", "70", "2147483647")]
        [InlineData("d2", "6", "70", "4294967295")]
        [InlineData("e", "7", "70", "-9223372036854775808")]
        [InlineData("e", "7", "70", "9254456")]
        [InlineData("e2", "8", "70", "184467")]
        public async Task SetLocalPrimitiveTypeVariableValid(string variableName, string originalValue, string newValue, string newValue2) {
            await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.SetVariableLocals", "run", 12);
            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() {{ invoke_static_method_async('[debugger-test] DebuggerTests.SetVariableLocals:run');}}, 1);",
                "dotnet://debugger-test.dll/debugger-set-variable-value-test.cs", 22, 12, "run",
                locals_fn: async (locals) =>
                {
                    CheckNumberAsString(locals, variableName, originalValue.ToString());
                    await Task.CompletedTask;
                }
            );
            var callFrameId = pause_location["callFrames"][0]["callFrameId"].Value<string>();

            await SetVariableValueOnCallFrame( JObject.FromObject(new {callFrameId, variableName, newValue=JObject.FromObject(new {value=newValue}) }));

            pause_location = await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-set-variable-value-test.cs", 23, 12, "run",
                locals_fn: async (locals) =>
                {
                    CheckNumberAsString(locals, variableName, newValue.ToString());
                    await Task.CompletedTask;
                }
            );

            callFrameId = pause_location["callFrames"][0]["callFrameId"].Value<string>();

            await SetVariableValueOnCallFrame( JObject.FromObject(new {callFrameId, variableName, newValue=JObject.FromObject(new {value=newValue2}) }));

            pause_location = await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-set-variable-value-test.cs", 24, 8, "run",
                locals_fn: async (locals) =>
                {
                    CheckNumberAsString(locals, variableName, newValue2.ToString());
                    await Task.CompletedTask;
                }
            );
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData(1, "a", 10, 30)]
        [InlineData(1, "a", 10, -1)]
        [InlineData(1, "b", 20, 30)]
        [InlineData(2, "c", 30, 60)]
        [InlineData(3, "d", 50, 70)]
        public async Task SetLocalPrimitiveTypeVariable(int offset, string variableName, int originalValue, int newValue){
            await SetBreakpointInMethod("debugger-test.dll", "Math", "IntAdd", offset);
            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_add(); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 8+offset, 8, "IntAdd",
                locals_fn: async (locals) =>
                {
                    CheckNumber(locals, variableName, originalValue);
                    await Task.CompletedTask;
                }
            );
            var callFrameId = pause_location["callFrames"][0]["callFrameId"].Value<string>();

            await SetVariableValueOnCallFrame( JObject.FromObject(new {callFrameId, variableName, newValue=JObject.FromObject(new {value=newValue}) }));

            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 9+offset, 8, "IntAdd",
                locals_fn: async (locals) =>
                {
                    CheckNumber(locals, variableName, newValue);
                    await Task.CompletedTask;
                }
            );
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData(1, "a", 10, "wrongValue")]
        [InlineData(1, "b", 20, "wrongValue")]
        [InlineData(2, "c", 30, "wrongValue")]
        [InlineData(3, "d", 50, "wrongValue")]
        [InlineData(3, "d", 50, "123wrongValue")]
        public async Task SetVariableValuesAtBreakpointSiteFail(int offset, string variableName, int originalValue, string invalidValue){
            await SetBreakpointInMethod("debugger-test.dll", "Math", "IntAdd", offset);
            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_add(); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 8+offset, 8, "IntAdd",
                locals_fn: async (locals) =>
                {
                    CheckNumber(locals, variableName, originalValue);
                    await Task.CompletedTask;
                }
            );
            var callFrameId = pause_location["callFrames"][0]["callFrameId"].Value<string>();

            await SetVariableValueOnCallFrame( JObject.FromObject(new {callFrameId, variableName, newValue=JObject.FromObject(new {value=invalidValue}) }), false);

            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 9+offset, 8, "IntAdd",
                locals_fn: async (locals) =>
                {
                    CheckNumber(locals, variableName, originalValue);
                    await Task.CompletedTask;
                }
            );
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData(5, "f", true, false)]
        [InlineData(5, "f", true, true)]
        public async Task SetLocalBoolTypeVariable(int offset, string variableName, bool originalValue, bool newValue){
            await SetBreakpointInMethod("debugger-test.dll", "Math", "IntAdd", offset);
            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_add(); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 8+offset, 8, "IntAdd",
                locals_fn: async (locals) =>
                {
                    await CheckBool(locals, variableName, originalValue);
                }
            );
            var callFrameId = pause_location["callFrames"][0]["callFrameId"].Value<string>();

            await SetVariableValueOnCallFrame( JObject.FromObject(new {callFrameId, variableName, newValue=JObject.FromObject(new {value=newValue}) }));

            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 9+offset, 4, "IntAdd",
                locals_fn: async (locals) =>
                {
                    await CheckBool(locals, variableName, newValue);
                }
            );
        }
        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData("A", 10, "20", true)]
        [InlineData("A", 10, "error", false)]
        [InlineData("d", 15, "20", true)]
        [InlineData("d", 15, "error", false)]
        [InlineData("d", 15, "123error", false)]
        public async Task TestSetValueOnObject(string prop_name, int prop_value, string prop_new_value, bool expect_ok)
        {
            var bp = await SetBreakpointInMethod("debugger-test.dll", "Math", "UseComplex", 5);
            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_use_complex(); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs",
                bp.Value["locations"][0]["lineNumber"].Value<int>(),
                bp.Value["locations"][0]["columnNumber"].Value<int>(),
                "UseComplex");


            var frame = pause_location["callFrames"][0];
            var props = await GetObjectOnFrame(frame, "complex");
            var locals = await GetProperties(frame["callFrameId"].Value<string>());
            var obj = GetAndAssertObjectWithName(locals, "complex");
            Assert.Equal(4, props.Count());
            CheckNumber(props, prop_name, prop_value);
            await CheckString(props, "B", "xx");

            await SetValueOnObject(obj, prop_name, prop_new_value, expect_ok: expect_ok);

            pause_location = await StepAndCheck(
                StepKind.Over,
                "dotnet://debugger-test.dll/debugger-test.cs",
                bp.Value["locations"][0]["lineNumber"].Value<int>()+1,
                bp.Value["locations"][0]["columnNumber"].Value<int>(),
                "UseComplex");

            frame = pause_location["callFrames"][0];
            props = await GetObjectOnFrame(frame, "complex");
            locals = await GetProperties(frame["callFrameId"].Value<string>());
            Assert.Equal(4, props.Count());
            CheckNumber(props, prop_name, expect_ok ? Int32.Parse(prop_new_value) : prop_value);
            await CheckString(props, "B", "xx");
        }
    }

}
