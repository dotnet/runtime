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
        [Fact]
        public async Task SetVariableValuesAtBreakpointSite(){
            await SetBreakpointInMethod("debugger-test.dll", "Math", "IntAdd", 1);
            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_add(); }, 1);",
                "dotnet://debugger-test.dll/debugger-test.cs", 9, 8, "IntAdd",
                locals_fn: (locals) =>
                {
                    CheckNumber(locals, "a", 10);
                    CheckNumber(locals, "b", 20);
                    CheckNumber(locals, "c", 0);
                    CheckNumber(locals, "d", 0);
                    CheckNumber(locals, "e", 0);
                }
            );
            var callFrameId = pause_location["callFrames"][0]["callFrameId"].Value<string>();

            await SetVariableValueOnCallFrame( JObject.FromObject(new {callFrameId, variableName="a", newValue=JObject.FromObject(new {value=30}) }));

            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 10, 8, "IntAdd",
                locals_fn: (locals) =>
                {
                    CheckNumber(locals, "a", 30);
                    CheckNumber(locals, "b", 20);
                    CheckNumber(locals, "c", 50);
                    CheckNumber(locals, "d", 0);
                    CheckNumber(locals, "e", 0);
                }
            );
        }
    }

}
