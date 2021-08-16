// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WebAssembly.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Threading;
using Xunit;

namespace DebuggerTests
{

    public class CustomViewTests : DebuggerTestBase
    {
        [Fact]
        public async Task CustomView()
        {
            var bp = await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.DebuggerCustomViewTest", "run", 5);
            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.DebuggerCustomViewTest:run'); }, 1);",
                "dotnet://debugger-test.dll/debugger-custom-view-test.cs",
                bp.Value["locations"][0]["lineNumber"].Value<int>(),
                bp.Value["locations"][0]["columnNumber"].Value<int>(),
                "run");

            var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            CheckObject(locals, "a", "DebuggerTests.WithDisplayString", description:"Some one Value 2 End");
			  CheckObject(locals, "c", "DebuggerTests.DebuggerDisplayMethodTest", description: "First Int:32 Second Int:43");
        }
    }
}
