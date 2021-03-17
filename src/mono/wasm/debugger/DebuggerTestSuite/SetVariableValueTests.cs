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
    public class SetVariableValueTests : DebuggerTestBase
    {
        [Theory]
        [InlineData(1, "a", 10, 30)]
        [InlineData(1, "a", 10, -1)]
        [InlineData(1, "b", 20, 30)]
        [InlineData(2, "c", 30, 60)]
        [InlineData(3, "d", 50, 70)]
        public async Task SetVariableValuesAtBreakpointSite(int offset, string variableName, int originalValue, int newValue){
            await SetBreakpointInMethod("debugger-test.dll", "Math", "IntAdd", offset);
            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_add(); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 8+offset, 8, "IntAdd",
                locals_fn: (locals) =>
                {
                    CheckNumber(locals, variableName, originalValue);
                }
            );
            var callFrameId = pause_location["callFrames"][0]["callFrameId"].Value<string>();

            await SetVariableValueOnCallFrame( JObject.FromObject(new {callFrameId, variableName, newValue=JObject.FromObject(new {value=newValue}) }));

            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 9+offset, 8, "IntAdd",
                locals_fn: (locals) =>
                {
                    CheckNumber(locals, variableName, newValue);
                }
            );
        }

        [Theory]
        [InlineData(1, "a", 10, "wrongValue")]
        [InlineData(1, "b", 20, "wrongValue")]
        [InlineData(2, "c", 30, "wrongValue")]
        [InlineData(3, "d", 50, "wrongValue")]
        public async Task SetVariableValuesAtBreakpointSiteFail(int offset, string variableName, int originalValue, string invalidValue){
            await SetBreakpointInMethod("debugger-test.dll", "Math", "IntAdd", offset);
            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_add(); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 8+offset, 8, "IntAdd",
                locals_fn: (locals) =>
                {
                    CheckNumber(locals, variableName, originalValue);
                }
            );
            var callFrameId = pause_location["callFrames"][0]["callFrameId"].Value<string>();

            await SetVariableValueOnCallFrame( JObject.FromObject(new {callFrameId, variableName, newValue=JObject.FromObject(new {value=invalidValue}) }), false);

            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 9+offset, 8, "IntAdd",
                locals_fn: (locals) =>
                {
                    CheckNumber(locals, variableName, originalValue);
                }
            );
        }

        [Theory]
        [InlineData(5, "f", true, false)]
        [InlineData(5, "f", true, true)]
        public async Task SetVariableValuesAtBreakpointSiteBool(int offset, string variableName, bool originalValue, bool newValue){
            await SetBreakpointInMethod("debugger-test.dll", "Math", "IntAdd", offset);
            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_add(); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 8+offset, 8, "IntAdd",
                locals_fn: (locals) =>
                {
                    CheckBool(locals, variableName, originalValue);
                }
            );
            var callFrameId = pause_location["callFrames"][0]["callFrameId"].Value<string>();

            await SetVariableValueOnCallFrame( JObject.FromObject(new {callFrameId, variableName, newValue=JObject.FromObject(new {value=newValue}) }));

            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 9+offset, 4, "IntAdd",
                locals_fn: (locals) =>
                {
                    CheckBool(locals, variableName, newValue);
                }
            );
        }
        [Theory]
        [InlineData("A", 10, "20", true)]
        [InlineData("A", 10, "error", false)]
        [InlineData("d", 15, "20", true)]
        [InlineData("d", 15, "error", false)]
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
            CheckString(props, "B", "xx");

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
            CheckString(props, "B", "xx");
        }
    }

}
