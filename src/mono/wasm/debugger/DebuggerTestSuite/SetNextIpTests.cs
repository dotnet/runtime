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

namespace DebuggerTests;

public class SetNextIpTests : DebuggerTests
{
    [ConditionalFact(nameof(RunningOnChrome))]
    public async Task SetAndCheck()
    {
        async Task CheckLocalsAsync(JToken locals, int c, int d, int e, bool f)
        {
            CheckNumber(locals, "c", c);
            CheckNumber(locals, "d", d);
            CheckNumber(locals, "e", e);
            await CheckBool(locals, "f", f);
        }
        var bp = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 9, 8);

        //calling invoke_add twice to check if the breakpoint continue working after calling SetNextIP
        var pause_location = await EvaluateAndCheck(
            "window.setTimeout(function() { invoke_add(); invoke_add(); }, 1);",
            "dotnet://debugger-test.dll/debugger-test.cs", 9, 8,
            "IntAdd"
        );
        var top_frame = pause_location["callFrames"][0]["functionLocation"];
        await SetNextIPAndCheck(top_frame["scriptId"].Value<string>(), "dotnet://debugger-test.dll/debugger-test.cs", 12, 8, "IntAdd",
            locals_fn: async (locals) => await CheckLocalsAsync(locals, 0, 0, 0, false));
        await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 13, 8, "IntAdd",
            locals_fn: async (locals) => await CheckLocalsAsync(locals, 0, 0, 0, true));
        await SetNextIPAndCheck(top_frame["scriptId"].Value<string>(), "dotnet://debugger-test.dll/debugger-test.cs", 9, 8, "IntAdd",
            locals_fn: async (locals) => await CheckLocalsAsync(locals, 0, 0, 0, true));
        await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 10, 8, "IntAdd",
            locals_fn: async (locals) => await CheckLocalsAsync(locals, 30, 0, 0, true));
        await SetNextIPAndCheck(top_frame["scriptId"].Value<string>(), "dotnet://debugger-test.dll/debugger-test.cs", 11, 8, "IntAdd",
            locals_fn: async (locals) => await CheckLocalsAsync(locals, 30, 0, 0, true));
        await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 12, 8, "IntAdd",
            locals_fn: async (locals) => await CheckLocalsAsync(locals, 30, 0, 10, true));

        //to check that after moving the execution pointer to the same line that there is already
        //a breakpoint, the breakpoint continue working
        pause_location = await StepAndCheck(StepKind.Resume,
            "dotnet://debugger-test.dll/debugger-test.cs", 9, 8,
            "IntAdd");
    }

    [ConditionalFact(nameof(RunningOnChrome))]
    public async Task OutsideTheCurrentMethod()
    {
        var bp = await SetBreakpoint("dotnet://debugger-test.dll/debugger-test.cs", 9, 8);

        var pause_location = await EvaluateAndCheck(
            "window.setTimeout(function() { invoke_add(); }, 1);",
            "dotnet://debugger-test.dll/debugger-test.cs", 9, 8,
            "IntAdd");
        var top_frame = pause_location["callFrames"][0]["functionLocation"];
        await SetNextIPAndCheck(top_frame["scriptId"].Value<string>(), "dotnet://debugger-test.dll/debugger-test.cs", 20, 8, "IntAdd",
        expected_error: true);
        await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 10, 8, "IntAdd",
        locals_fn: async (locals) =>
            {
                CheckNumber(locals, "c", 30);
                CheckNumber(locals, "d", 0);
                CheckNumber(locals, "e", 0);
                await CheckBool(locals, "f", false);
            });
    }

    [ConditionalFact(nameof(RunningOnChrome))]
    public async Task AsyncMethod()
    {
        var debugger_test_loc = "dotnet://debugger-test.dll/debugger-test.cs";

        await SetBreakpoint(debugger_test_loc, 140, 12);

        var pause_location = await EvaluateAndCheck(
            "window.setTimeout(function() { invoke_async_method_with_await(); }, 1);",
            debugger_test_loc, 140, 12, "MoveNext",
            locals_fn: async (locals) =>
            {
                CheckNumber(locals, "li", 0);
                CheckNumber(locals, "i", 42);
                await CheckString(locals, "ls", null);
            }
        );
        var top_frame = pause_location["callFrames"][0]["functionLocation"];
        await SetNextIPAndCheck(top_frame["scriptId"].Value<string>(), "dotnet://debugger-test.dll/debugger-test.cs", 141, 12, "MoveNext",
        locals_fn: async (locals) =>
            {
                CheckNumber(locals, "li", 0);
                CheckNumber(locals, "i", 42);
                await CheckString(locals, "ls", null);
            });
        await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 142, 12, "MoveNext",
        locals_fn: async (locals) =>
            {
                CheckNumber(locals, "li", 0);
                CheckNumber(locals, "i", 42);
                await CheckString(locals, "ls", "string from jstest");
            });
    }

    [ConditionalFact(nameof(RunningOnChrome))]
    public async Task Lambda()
    {
        var debugger_test_loc = "dotnet://debugger-test.dll/debugger-async-test.cs";

        await SetBreakpoint(debugger_test_loc, 77, 12);
        var pause_location = await EvaluateAndCheck(
        "window.setTimeout(function() { invoke_static_method('[debugger-test] DebuggerTests.AsyncTests.ContinueWithTests:RunAsync'); })",
        debugger_test_loc, 77, 12, "MoveNext",
        locals_fn: async (locals) =>
        {
            await CheckString(locals, "str", "foobar");
            await CheckValueType(locals, "code", "System.Threading.Tasks.TaskStatus", description: "Created");
            await CheckValueType(locals, "dt0", "System.DateTime", description: "1/1/0001 12:00:00 AM");
        });
        var top_frame = pause_location["callFrames"][0]["functionLocation"];
        await SetNextIPAndCheck(top_frame["scriptId"].Value<string>(), "dotnet://debugger-test.dll/debugger-async-test.cs", 79, 16, "MoveNext",
        locals_fn: async (locals) =>
            {
                await CheckString(locals, "str", "foobar");
                await CheckValueType(locals, "code", "System.Threading.Tasks.TaskStatus", description: "Created");
                await CheckValueType(locals, "dt0", "System.DateTime", description: "1/1/0001 12:00:00 AM");
            });
        await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-async-test.cs", 80, 16, "MoveNext",
        locals_fn: async (locals) =>
            {
                await CheckString(locals, "str", "foobar");
                await CheckValueType(locals, "code", "System.Threading.Tasks.TaskStatus", description: "Created");
                await CheckDateTime(locals, "dt0", new DateTime(3412, 4, 6, 8, 0, 2));
            });
        await SetNextIPAndCheck(top_frame["scriptId"].Value<string>(), "dotnet://debugger-test.dll/debugger-async-test.cs", 91, 16, "MoveNext",
        locals_fn: async (locals) =>
            {
                await CheckString(locals, "str", "foobar");
                await CheckValueType(locals, "code", "System.Threading.Tasks.TaskStatus", description: "Created");
                await CheckDateTime(locals, "dt0", new DateTime(3412, 4, 6, 8, 0, 2));
            });
        await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-async-test.cs", 92, 12, "MoveNext",
        locals_fn: async (locals) =>
            {
                await CheckString(locals, "str", "foobar");
                await CheckValueType(locals, "code", "System.Threading.Tasks.TaskStatus", description: "Created");
                await CheckDateTime(locals, "dt0", new DateTime(3412, 4, 6, 8, 0, 2));
            });
        }

    [ConditionalFact(nameof(RunningOnChrome))]
    public async Task Lambda_InvalidLocation()
    {
        var debugger_test_loc = "dotnet://debugger-test.dll/debugger-async-test.cs";

        await SetBreakpoint(debugger_test_loc, 77, 12);
        var pause_location = await EvaluateAndCheck(
        "window.setTimeout(function() { invoke_static_method('[debugger-test] DebuggerTests.AsyncTests.ContinueWithTests:RunAsync'); })",
        debugger_test_loc, 77, 12, "MoveNext",
        locals_fn: async (locals) =>
        {
            await CheckString(locals, "str", "foobar");
            await CheckValueType(locals, "code", "System.Threading.Tasks.TaskStatus", description: "Created");
            await CheckValueType(locals, "dt0", "System.DateTime", description: "1/1/0001 12:00:00 AM");
        });
        var top_frame = pause_location["callFrames"][0]["functionLocation"];
        await SetNextIPAndCheck(top_frame["scriptId"].Value<string>(), "dotnet://debugger-test.dll/debugger-async-test.cs", 92, 8, "MoveNext",
        expected_error: true);

        await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-async-test.cs", 79, 16, "MoveNext",
        locals_fn: async (locals) =>
            {
                await CheckString(locals, "str", "foobar");
                await CheckValueType(locals, "code", "System.Threading.Tasks.TaskStatus", description: "RanToCompletion");
                await CheckValueType(locals, "dt0", "System.DateTime", description: "1/1/0001 12:00:00 AM");
            },
        times: 2);
    }

    [ConditionalFact(nameof(RunningOnChrome))]
    public async Task Lambda_ToNestedLambda()
    {
        var debugger_test_loc = "dotnet://debugger-test.dll/debugger-async-test.cs";

        await SetBreakpoint(debugger_test_loc, 77, 12);
        var pause_location = await EvaluateAndCheck(
        "window.setTimeout(function() { invoke_static_method('[debugger-test] DebuggerTests.AsyncTests.ContinueWithTests:RunAsync'); })",
        debugger_test_loc, 77, 12, "MoveNext",
        locals_fn: async (locals) =>
        {
            await CheckString(locals, "str", "foobar");
            await CheckValueType(locals, "code", "System.Threading.Tasks.TaskStatus", description: "Created");
            await CheckValueType(locals, "dt0", "System.DateTime", description: "1/1/0001 12:00:00 AM");
        });
        var top_frame = pause_location["callFrames"][0]["functionLocation"];

        await SetNextIPAndCheck(top_frame["scriptId"].Value<string>(), "dotnet://debugger-test.dll/debugger-async-test.cs", 88, 20, "MoveNext",
        expected_error: true);
        
        await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-async-test.cs", 79, 16, "MoveNext",
        locals_fn: async (locals) =>
            {
                await CheckString(locals, "str", "foobar");
                await CheckValueType(locals, "code", "System.Threading.Tasks.TaskStatus", description: "RanToCompletion");
                await CheckValueType(locals, "dt0", "System.DateTime", description: "1/1/0001 12:00:00 AM");
            },
        times: 2);
        }

    [ConditionalFact(nameof(RunningOnChrome))]
    public async Task Lambda_ToNestedSingleLineLambda_Invalid()
    {
        var debugger_test_loc = "dotnet://debugger-test.dll/debugger-async-test.cs";

        await SetBreakpoint(debugger_test_loc, 77, 12);
        var pause_location = await EvaluateAndCheck(
        "window.setTimeout(function() { invoke_static_method('[debugger-test] DebuggerTests.AsyncTests.ContinueWithTests:RunAsync'); })",
        debugger_test_loc, 77, 12, "MoveNext",
        locals_fn: async (locals) =>
        {
            await CheckString(locals, "str", "foobar");
            await CheckValueType(locals, "code", "System.Threading.Tasks.TaskStatus", description: "Created");
            await CheckValueType(locals, "dt0", "System.DateTime", description: "1/1/0001 12:00:00 AM");
        });
        var top_frame = pause_location["callFrames"][0]["functionLocation"];

        await SetNextIPAndCheck(top_frame["scriptId"].Value<string>(), "dotnet://debugger-test.dll/debugger-async-test.cs", 91, 58, "MoveNext",
        expected_error: true);
        
        await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-async-test.cs", 79, 16, "MoveNext",
        locals_fn: async (locals) =>
            {
                await CheckString(locals, "str", "foobar");
                await CheckValueType(locals, "code", "System.Threading.Tasks.TaskStatus", description: "RanToCompletion");
                await CheckValueType(locals, "dt0", "System.DateTime", description: "1/1/0001 12:00:00 AM");
            },
        times: 2);
    }

    [ConditionalFact(nameof(RunningOnChrome))]
    public async Task Lambda_ToNestedSingleLineLambda_Valid()
    {
        var debugger_test_loc = "dotnet://debugger-test.dll/debugger-async-test.cs";

        await SetBreakpoint(debugger_test_loc, 77, 12);
        var pause_location = await EvaluateAndCheck(
        "window.setTimeout(function() { invoke_static_method('[debugger-test] DebuggerTests.AsyncTests.ContinueWithTests:RunAsync'); })",
        debugger_test_loc, 77, 12, "MoveNext",
        locals_fn: async (locals) =>
        {
            await CheckString(locals, "str", "foobar");
            await CheckValueType(locals, "code", "System.Threading.Tasks.TaskStatus", description: "Created");
            await CheckValueType(locals, "dt0", "System.DateTime", description: "1/1/0001 12:00:00 AM");
        });
        var top_frame = pause_location["callFrames"][0]["functionLocation"];

        await SetNextIPAndCheck(top_frame["scriptId"].Value<string>(), "dotnet://debugger-test.dll/debugger-async-test.cs", 91, 16, "MoveNext",
        locals_fn: async (locals) =>
            {
                await CheckString(locals, "str", "foobar");
                await CheckValueType(locals, "code", "System.Threading.Tasks.TaskStatus", description: "Created");
                await CheckValueType(locals, "dt0", "System.DateTime", description: "1/1/0001 12:00:00 AM");
            });
        
        await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-async-test.cs", 92, 12, "MoveNext",
        locals_fn: async (locals) =>
            {
                await CheckString(locals, "str", "foobar");
                await CheckValueType(locals, "code", "System.Threading.Tasks.TaskStatus", description: "Created");
                await CheckValueType(locals, "dt0", "System.DateTime", description: "1/1/0001 12:00:00 AM");
            });
    }
}