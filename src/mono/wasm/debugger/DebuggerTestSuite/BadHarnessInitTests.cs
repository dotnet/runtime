// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WebAssembly.Diagnostics;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace DebuggerTests
{
    public class BadHarnessInitTests : DebuggerTests
    {
        public BadHarnessInitTests(ITestOutputHelper testOutput) : base(testOutput)
        {}

        public override async Task InitializeAsync() => await Task.CompletedTask;

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task InvalidInitCommands()
        {
            var bad_cmd_name = "non-existent.command";

            var init_cmds = new List<(string, JObject?)>
            {
                ("Profiler.enable", null),
                (bad_cmd_name, null)
            };

            await Ready();

            var ae = await Assert.ThrowsAsync<ArgumentException>(async () => await insp.OpenSessionAsync(init_cmds, "", TestTimeout));
            Assert.Contains(bad_cmd_name, ae.Message);
        }
    }
}
