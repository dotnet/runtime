// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WebAssembly.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Threading;
using Xunit;
using System.Collections.Generic;

namespace DebuggerTests
{

    public class CustomViewTests : DebuggerTests
    {
        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task UsingDebuggerDisplay()
        {
            var bp = await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.DebuggerCustomViewTest", "run", 15);
            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.DebuggerCustomViewTest:run'); }, 1);",
                "dotnet://debugger-test.dll/debugger-custom-view-test.cs",
                bp.Value["locations"][0]["lineNumber"].Value<int>(),
                bp.Value["locations"][0]["columnNumber"].Value<int>(),
                "run");

            var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            await CheckObject(locals, "a", "DebuggerTests.WithDisplayString", description:"Some one Value 2 End");
            await CheckObject(locals, "c", "DebuggerTests.DebuggerDisplayMethodTest", description: "First Int:32 Second Int:43");
            await CheckObject(locals, "myList", "System.Collections.Generic.List<int>", description: "Count = 4");
            await CheckObject(locals, "person1", "DebuggerTests.Person", description: "FirstName: Anton, SurName: Mueller, Age: 44");
            await CheckObject(locals, "person2", "DebuggerTests.Person", description: "FirstName: Lisa, SurName: Müller, Age: 41");
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task UsingDebuggerTypeProxy()
        {
            var bp = await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.DebuggerCustomViewTest", "run", 15);
            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.DebuggerCustomViewTest:run'); }, 1);",
                "dotnet://debugger-test.dll/debugger-custom-view-test.cs",
                bp.Value["locations"][0]["lineNumber"].Value<int>(),
                bp.Value["locations"][0]["columnNumber"].Value<int>(),
                "run");

            var frame = pause_location["callFrames"][0];
            var locals = await GetProperties(frame["callFrameId"].Value<string>());
            await CheckObject(locals, "myList", "System.Collections.Generic.List<int>", description: "Count = 4");
            var props = await GetObjectOnFrame(frame, "myList");
            Assert.Equal(1, props.Count());

            await CheckArray(props, "Items", "int[]", "int[4]");

            await CheckObject(locals, "b", "DebuggerTests.WithProxy", description:"DebuggerTests.WithProxy");
            props = await GetObjectOnFrame(frame, "b");
            await CheckString(props, "Val2", "one");

            await CheckObject(locals, "openWith", "System.Collections.Generic.Dictionary<string, string>", description: "Count = 3");
            props = await GetObjectOnFrame(frame, "openWith");
            Assert.Equal(1, props.Count());

            await EvaluateOnCallFrameAndCheck(frame["callFrameId"].Value<string>(),
                ("listToTestToList.ToList()", TObject("System.Collections.Generic.List<int>", description: "Count = 11")));

        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task UsingDebuggerDisplayConcurrent()
        {
            async Task<bool> CheckProperties(JObject pause_location)
            {
                var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
                var l = GetAndAssertObjectWithName(locals, "myList");
                var val = l["value"];
                if (val["description"].Value<string>() != "Count = 0")
                    return false;
                return true;
            }

            var bp = await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.DebuggerCustomViewTest2", "run", 2);
            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.DebuggerCustomViewTest2:run'); }, 1);",
                "dotnet://debugger-test.dll/debugger-custom-view-test.cs",
                bp.Value["locations"][0]["lineNumber"].Value<int>(),
                bp.Value["locations"][0]["columnNumber"].Value<int>(),
                "run");

            pause_location = await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-custom-view-test.cs", bp.Value["locations"][0]["lineNumber"].Value<int>()+2, bp.Value["locations"][0]["columnNumber"].Value<int>(),  "run");

            List<Task<bool>> tasks = new();
            for (int i = 0 ; i < 10; i++)
            {
                var task = CheckProperties(pause_location);
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
            foreach(Task<bool> task in tasks)
            {
                //FIXME: blocks
                Assert.True(task.Result);
            }
        }
    }
}
