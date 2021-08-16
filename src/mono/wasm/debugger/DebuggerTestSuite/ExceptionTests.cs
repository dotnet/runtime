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

    public class ExceptionTests : DebuggerTestBase
    {
        [Fact]
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
            CheckString(exception_members, "message", "not implemented caught");

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
            CheckString(exception_members, "message", "not implemented uncaught");
        }

        [Fact]
        public async Task JSExceptionTestAll()
        {
            await SetPauseOnException("all");

            var eval_expr = "window.setTimeout(function () { exceptions_test (); }, 1)";
            var pause_location = await EvaluateAndCheck(eval_expr, null, 0, 0, "exception_caught_test", null, null);

            Assert.Equal("exception", pause_location["reason"]);
            await CheckValue(pause_location["data"], JObject.FromObject(new
            {
                type = "object",
                subtype = "error",
                className = "TypeError",
                uncaught = false
            }), "exception0.data");

            var exception_members = await GetProperties(pause_location["data"]["objectId"]?.Value<string>());
            CheckString(exception_members, "message", "exception caught");

            pause_location = await SendCommandAndCheck(null, "Debugger.resume", null, 0, 0, "exception_uncaught_test");

            Assert.Equal("exception", pause_location["reason"]);
            await CheckValue(pause_location["data"], JObject.FromObject(new
            {
                type = "object",
                subtype = "error",
                className = "RangeError",
                uncaught = true
            }), "exception1.data");

            exception_members = await GetProperties(pause_location["data"]["objectId"]?.Value<string>());
            CheckString(exception_members, "message", "exception uncaught");
        }

        // FIXME? BUG? We seem to get the stack trace for Runtime.exceptionThrown at `call_method`,
        // but JS shows the original error type, and original trace
        [Fact]
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

        [Fact]
        public async Task JSExceptionTestNone()
        {
            await SetPauseOnException("none");

            var eval_expr = "window.setTimeout(function () { exceptions_test (); }, 1)";

            int line = 44;
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

        [Theory]
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
            CheckString(exception_members, "message", exception_message);
        }

        [Fact]
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
            CheckString(exception_members, "message", "not implemented uncaught");
        }

        [Fact]
        public async Task ExceptionTestAllWithReload()
        {
            string entry_method_name = "[debugger-test] DebuggerTests.ExceptionTestsClass:TestExceptions";
            var debugger_test_loc = "dotnet://debugger-test.dll/debugger-exception-test.cs";

            await SetPauseOnException("all");

            await SendCommand("Page.enable", null);
            var pause_location = await SendCommandAndCheck(JObject.FromObject(new
                                    {
                                        ignoreCache = true
                                    }), "Page.reload",null, 0, 0, null);
            Thread.Sleep(1000);

            //send a lot of resumes to "skip" all the pauses on caught exception and completely reload the page
            int i = 0;
            while (i < 100)
            {
                Result res = await cli.SendCommand("Debugger.resume", null, token);
                i++;
            }

            
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
                className = "DebuggerTests.CustomException",
                uncaught = false
            }), "exception0.data");

            var exception_members = await GetProperties(pause_location["data"]["objectId"]?.Value<string>());
            CheckString(exception_members, "message", "not implemented caught");

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
            CheckString(exception_members, "message", "not implemented uncaught");
        }


        async Task<JObject> WaitForManagedException(JObject pause_location)
        {
            while (true)
            {
                if (pause_location != null)
                {
                    AssertEqual("exception", pause_location["reason"]?.Value<string>(), $"Expected to only pause because of an exception. {pause_location}");

                    // return in case of a managed exception, and ignore JS ones
                    if (pause_location["data"]?["objectId"]?.Value<string>()?.StartsWith("dotnet:object:") == true)
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
