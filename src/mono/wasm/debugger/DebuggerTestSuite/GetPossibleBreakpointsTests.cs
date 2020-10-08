// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WebAssembly.Diagnostics;
using Newtonsoft.Json.Linq;
using Xunit;

namespace DebuggerTests
{
    public class GetPossibleBreakpointsTests : DebuggerTestBase, IAsyncLifetime
    {
        Inspector insp;
        Dictionary<string, string> scripts;

        public async Task InitializeAsync()
        {
            insp = new Inspector();

            //Collect events
            scripts = SubscribeToScripts(insp);
            await Ready();
        }

        public async Task DisposeAsync()
        {
            await Task.CompletedTask;
        }

        [Fact]
        public async Task DoesNotReturnHiddenLines() => await insp.Ready(async (cli, token) =>
        {
            ctx = new DebugTestContext(cli, insp, token, scripts);

            var source_file = "dotnet://debugger-test.dll/debugger-test.cs";
            string scriptId = scripts.Where(kvp => kvp.Value == source_file).Single().Key;
            var args = JObject.FromObject(new
            {
                start = new
                {
                    scriptId,
                    lineNumber = 539,
                    columnNumber = 8
                },
                end = new
                {
                    scriptId,
                    lineNumber = 544,
                    columnNumber = 4
                }
            });

            Result res = await SendCommand("Debugger.getPossibleBreakpoints", args);
            Assert.True(res.IsOk, $"getPossibleBreakpoints failed with {res}");

            JToken.DeepEquals(res.Value["locations"], new JArray(new[]
            {
                TLocation(542, 8),
                TLocation(543, 8),
                TLocation(544, 4)
            }));

            JObject TLocation(int lineNumber, int columnNumber) => JObject.FromObject(new
            {
                scriptId,
                lineNumber,
                columnNumber
            });
        });

        [Fact]
        public async Task InvalidScriptId() => await insp.Ready(async (cli, token) =>
        {
            ctx = new DebugTestContext(cli, insp, token, scripts);

            var args = JObject.FromObject(new
            {
                start = new
                {
                    scriptId = "dotnet://123_123",
                    lineNumber = 539,
                    columnNumber = 8
                },
                end = new
                {
                    scriptId = "dotnet://123_123",
                    lineNumber = 539,
                    columnNumber = 8
                }
            });

            Result res = await SendCommand("Debugger.getPossibleBreakpoints", args, expect_ok: false);
            Assert.False(res.IsOk, $"getPossibleBreakpoints should have failed, but we got {res}");
        });

        [Fact]
        public async Task MissingEndArgument() => await insp.Ready(async (cli, token) =>
        {
            ctx = new DebugTestContext(cli, insp, token, scripts);

            var source_file = "dotnet://debugger-test.dll/debugger-test.cs";
            string scriptId = scripts.Where(kvp => kvp.Value == source_file).Single().Key;
            var args = JObject.FromObject(new
            {
                start = new
                {
                    scriptId,
                    lineNumber = 539,
                    columnNumber = 8
                },
            });

            Result res = await SendCommand("Debugger.getPossibleBreakpoints", args, expect_ok: false);
            Assert.False(res.IsOk, $"getPossibleBreakpoints should have failed, but we got {res}");
        });

        [Fact]
        public async Task MissingStartArgument() => await insp.Ready(async (cli, token) =>
        {
            ctx = new DebugTestContext(cli, insp, token, scripts);

            var source_file = "dotnet://debugger-test.dll/debugger-test.cs";
            string scriptId = scripts.Where(kvp => kvp.Value == source_file).Single().Key;
            var args = JObject.FromObject(new
            {
                end = new
                {
                    scriptId,
                    lineNumber = 539,
                    columnNumber = 8
                }
            });

            Result res = await SendCommand("Debugger.getPossibleBreakpoints", args, expect_ok: false);
            Assert.False(res.IsOk, $"getPossibleBreakpoints should have failed, but we got {res}");
        });

        [Fact]
        public async Task StartAndEndWithDifferentScriptIds() => await insp.Ready(async (cli, token) =>
        {
            ctx = new DebugTestContext(cli, insp, token, scripts);

            string scriptId0 = scripts.Where(kvp => kvp.Value.Contains("/debugger-test.cs")).Single().Key;
            string scriptId1 = scripts.Where(kvp => kvp.Value.Contains("/debugger-valuetypes-test.cs")).Single().Key;
            var args = JObject.FromObject(new
            {
                start = new
                {
                    scriptId = scriptId0,
                    lineNumber = 539,
                    columnNumber = 8
                },
                end = new
                {
                    scriptId = scriptId1,
                    lineNumber = 14,
                    columnNumber = 12
                }
            });

            Result res = await SendCommand("Debugger.getPossibleBreakpoints", args, expect_ok: false);
            Assert.False(res.IsOk, $"getPossibleBreakpoints should have failed, but we got {res}");
        });
    }
}
