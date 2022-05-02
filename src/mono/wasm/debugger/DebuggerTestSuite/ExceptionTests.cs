// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Threading;
using Xunit;
using Xunit.Sdk;

namespace DebuggerTests
{

    public class ExceptionTests : DebuggerTests
    {
        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task ExceptionTestAll()
        {
            string entry_method_name = "[debugger-test] DebuggerTests.ExceptionTestsClass:TestExceptions";
            var debugger_test_loc = "dotnet://debugger-test.dll/debugger-exception-test.cs";

            await SetPauseOnException("all");

            var eval_expr = "window.setTimeout(function() { invoke_static_method (" +
                $"'{entry_method_name}'" +
                "); }, 1);";

            var pause_location = await EvaluateAndCheck(eval_expr, null, 0, 0, null);
            //stop in the managed caught exception
            pause_location = await WaitForManagedException(pause_location);

            AssertEqual("run", pause_location["callFrames"]?[0]?["functionName"]?.Value<string>(), "pause0");

            await CheckValue(pause_location["data"], JObject.FromObject(new
            {
                type = "object",
                subtype = "error",
                className = "DebuggerTests.CustomException",
                uncaught = false
            }), "exception0.data");

            var exception_members = await GetProperties(pause_location["data"]["objectId"]?.Value<string>());
            await CheckString(exception_members, "message", "not implemented caught");

            pause_location = await WaitForManagedException(null);
            AssertEqual("run", pause_location["callFrames"]?[0]?["functionName"]?.Value<string>(), "pause1");

            //stop in the uncaught exception
            CheckLocation(debugger_test_loc, 28, 16, scripts, pause_location["callFrames"][0]["location"]);

            await CheckValue(pause_location["data"], JObject.FromObject(new
            {
                type = "object",
                subtype = "error",
                className = "DebuggerTests.CustomException",
                uncaught = true
            }), "exception1.data");

            exception_members = await GetProperties(pause_location["data"]["objectId"]?.Value<string>());
            await CheckString(exception_members, "message", "not implemented uncaught");
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task JSExceptionTestAll()
        {
            await SetPauseOnException("all");

            var eval_expr = "window.setTimeout(function () { exceptions_test (); }, 1)";
            var pause_location = await EvaluateAndCheck(eval_expr, null, 0, 0, null);
            pause_location = await WaitForJSException(pause_location, "exception_caught_test");

            await CheckValue(pause_location["data"], JObject.FromObject(new
            {
                type = "object",
                subtype = "error",
                className = "TypeError",
                uncaught = false
            }), "exception0.data");

            var exception_members = await GetProperties(pause_location["data"]["objectId"]?.Value<string>());
            await CheckString(exception_members, "message", "exception caught");

            pause_location = await SendCommandAndCheck(null, "Debugger.resume", null, 0, 0, null);
            pause_location = await WaitForJSException(pause_location, "exception_uncaught_test");

            await CheckValue(pause_location["data"], JObject.FromObject(new
            {
                type = "object",
                subtype = "error",
                className = "RangeError",
                uncaught = true
            }), "exception1.data");

            exception_members = await GetProperties(pause_location["data"]["objectId"]?.Value<string>());
            await CheckString(exception_members, "message", "exception uncaught");

            async Task<JObject> WaitForJSException(JObject pause_location, string exp_fn_name)
            {
                while (true)
                {
                    if (pause_location != null)
                    {
                        AssertEqual("exception", pause_location["reason"]?.Value<string>(), $"Expected to only pause because of an exception. {pause_location}");

                        string actual_fn_name = pause_location["callFrames"]?[0]?["functionName"]?.Value<string>();

                        // return if we hit a managed exception, or an uncaught one
                        if (pause_location["data"]?["objectId"]?.Value<string>()?.StartsWith("dotnet:object:", StringComparison.Ordinal) == true)
                        {
                            Console.WriteLine($"Hit an unexpected managed exception, with function name: {actual_fn_name}. {pause_location}");
                            throw new XunitException($"Hit an unexpected managed exception, with function name: {actual_fn_name}");
                        }

                        if (pause_location["data"]?["uncaught"]?.Value<bool>() == true)
                            break;

                        if (actual_fn_name == exp_fn_name)
                            break;
                    }

                    pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", null, 0, 0, null);
                }

                return pause_location;
            }
        }

        // FIXME? BUG? We seem to get the stack trace for Runtime.exceptionThrown at `call_method`,
        // but JS shows the original error type, and original trace
        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task ExceptionTestNone()
        {
            //Collect events
            string entry_method_name = "[debugger-test] DebuggerTests.ExceptionTestsClass:TestExceptions";
            await SetPauseOnException("none");

            var eval_expr = "window.setTimeout(function() { invoke_static_method (" +
                $"'{entry_method_name}'" +
                "); }, 1);";

            try
            {
                await EvaluateAndCheck(eval_expr, null, 0, 0, "", null, null);
            }
            catch (ArgumentException ae)
            {
                var eo = JObject.Parse(ae.Message);

                // AssertEqual (line, eo ["exceptionDetails"]?["lineNumber"]?.Value<int> (), "lineNumber");
                AssertEqual("Uncaught", eo["exceptionDetails"]?["text"]?.Value<string>(), "text");

                await CheckValue(eo["exceptionDetails"]?["exception"], JObject.FromObject(new
                {
                    type = "object",
                    subtype = "error",
                    className = "Error" // BUG?: "DebuggerTests.CustomException"
                }), "exception");

                return;
            }

            Assert.True(false, "Expected to get an ArgumentException from the uncaught user exception");
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task JSExceptionTestNone()
        {
            await SetPauseOnException("none");

            var eval_expr = "window.setTimeout(function () { exceptions_test (); }, 1)";

            int line = 46;
            try
            {
                await EvaluateAndCheck(eval_expr, null, 0, 0, "", null, null);
            }
            catch (ArgumentException ae)
            {
                Console.WriteLine($"{ae}");
                var eo = JObject.Parse(ae.Message);

                AssertEqual(line, eo["exceptionDetails"]?["lineNumber"]?.Value<int>(), "lineNumber");
                AssertEqual("Uncaught", eo["exceptionDetails"]?["text"]?.Value<string>(), "text");

                await CheckValue(eo["exceptionDetails"]?["exception"], JObject.FromObject(new
                {
                    type = "object",
                    subtype = "error",
                    className = "RangeError"
                }), "exception");

                return;
            }

            Assert.True(false, "Expected to get an ArgumentException from the uncaught user exception");
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData("function () { exceptions_test (); }", null, 0, 0, "exception_uncaught_test", "RangeError", "exception uncaught")]
        [InlineData("function () { invoke_static_method ('[debugger-test] DebuggerTests.ExceptionTestsClass:TestExceptions'); }",
            "dotnet://debugger-test.dll/debugger-exception-test.cs", 28, 16, "run",
            "DebuggerTests.CustomException", "not implemented uncaught")]
        public async Task ExceptionTestUncaught(string eval_fn, string loc, int line, int col, string fn_name,
            string exception_type, string exception_message)
        {
            await SetPauseOnException("uncaught");

            var eval_expr = $"window.setTimeout({eval_fn}, 1);";
            var pause_location = await EvaluateAndCheck(eval_expr, loc, line, col, fn_name);

            Assert.Equal("exception", pause_location["reason"]);
            await CheckValue(pause_location["data"], JObject.FromObject(new
            {
                type = "object",
                subtype = "error",
                className = exception_type,
                uncaught = true
            }), "exception.data");

            var exception_members = await GetProperties(pause_location["data"]["objectId"]?.Value<string>());
            await CheckString(exception_members, "message", exception_message);
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task ExceptionTestUncaughtWithReload()
        {
            string entry_method_name = "[debugger-test] DebuggerTests.ExceptionTestsClass:TestExceptions";
            var debugger_test_loc = "dotnet://debugger-test.dll/debugger-exception-test.cs";

            await SetPauseOnException("uncaught");

            await SendCommand("Page.enable", null);
            await SendCommand("Page.reload", JObject.FromObject(new
                                    {
                                        ignoreCache = true
                                    }));
            Thread.Sleep(1000);

            var eval_expr = "window.setTimeout(function() { invoke_static_method (" +
                $"'{entry_method_name}'" +
                "); }, 1);";

            var pause_location = await EvaluateAndCheck(eval_expr, null, 0, 0, null);
            //stop in the managed caught exception
            pause_location = await WaitForManagedException(pause_location);

            AssertEqual("run", pause_location["callFrames"]?[0]?["functionName"]?.Value<string>(), "pause1");

            //stop in the uncaught exception
            CheckLocation(debugger_test_loc, 28, 16, scripts, pause_location["callFrames"][0]["location"]);

            await CheckValue(pause_location["data"], JObject.FromObject(new
            {
                type = "object",
                subtype = "error",
                className = "DebuggerTests.CustomException",
                uncaught = true
            }), "exception1.data");

            var exception_members = await GetProperties(pause_location["data"]["objectId"]?.Value<string>());
            await CheckString(exception_members, "message", "not implemented uncaught");
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData("[debugger-test] DebuggerTests.ExceptionTestsClassDefault:TestExceptions", "System.Exception", 76)]
        [InlineData("[debugger-test] DebuggerTests.ExceptionTestsClass:TestExceptions", "DebuggerTests.CustomException", 28)]
        public async Task ExceptionTestAllWithReload(string entry_method_name, string class_name, int line_number)
        {
            var debugger_test_loc = "dotnet://debugger-test.dll/debugger-exception-test.cs";

            await SetPauseOnException("all");

            await SendCommand("Page.enable", null);
            var pause_location = await SendCommandAndCheck(JObject.FromObject(new
                                    {
                                        ignoreCache = true
                                    }), "Page.reload",null, 0, 0, null);
            Thread.Sleep(1000);

            // Hit resume to skip 
            int count = 0;
            while(true)
            {
                await cli.SendCommand("Debugger.resume", null, token);
                count++;

                try
                {
                    await insp.WaitFor(Inspector.PAUSE)
                                .WaitAsync(TimeSpan.FromSeconds(10));
                }
                catch (TimeoutException)
                {
                    // timed out waiting for a PAUSE
                    insp.ClearWaiterFor(Inspector.PAUSE);
                    break;
                }
            }
            Console.WriteLine ($"* Resumed {count} times");

            var eval_expr = "window.setTimeout(function() { invoke_static_method (" +
                $"'{entry_method_name}'" +
                "); }, 1);";

            pause_location = await EvaluateAndCheck(eval_expr, null, 0, 0, null);
            //stop in the managed caught exception
            pause_location = await WaitForManagedException(pause_location);

            AssertEqual("run", pause_location["callFrames"]?[0]?["functionName"]?.Value<string>(), "pause0");

            await CheckValue(pause_location["data"], JObject.FromObject(new
            {
                type = "object",
                subtype = "error",
                className = class_name,
                uncaught = false,
                description = "not implemented caught"
            }), "exception0.data");

            var exception_members = await GetProperties(pause_location["data"]["objectId"]?.Value<string>());
            await CheckString(exception_members, "_message", "not implemented caught");

            pause_location = await WaitForManagedException(null);
            AssertEqual("run", pause_location["callFrames"]?[0]?["functionName"]?.Value<string>(), "pause1");

            //stop in the uncaught exception
            CheckLocation(debugger_test_loc, line_number, 16, scripts, pause_location["callFrames"][0]["location"]);

            await CheckValue(pause_location["data"], JObject.FromObject(new
            {
                type = "object",
                subtype = "error",
                className = class_name,
                uncaught = true,
                description = "not implemented uncaught"
            }), "exception1.data");

            exception_members = await GetProperties(pause_location["data"]["objectId"]?.Value<string>());
            await CheckString(exception_members, "_message", "not implemented uncaught");
        }


        async Task<JObject> WaitForManagedException(JObject pause_location)
        {
            while (true)
            {
                if (pause_location != null)
                {
                    AssertEqual("exception", pause_location["reason"]?.Value<string>(), $"Expected to only pause because of an exception. {pause_location}");

                    // return in case of a managed exception, and ignore JS ones
                    if (pause_location["data"]?["objectId"]?.Value<string>()?.StartsWith("dotnet:object:", StringComparison.Ordinal) == true ||
                        pause_location["data"]?["uncaught"]?.Value<bool>() == true)
                    {
                        break;
                    }
                }

                pause_location = await SendCommandAndCheck(JObject.FromObject(new { }), "Debugger.resume", null, 0, 0, null);
            }

            return pause_location;
        }
    }
}
