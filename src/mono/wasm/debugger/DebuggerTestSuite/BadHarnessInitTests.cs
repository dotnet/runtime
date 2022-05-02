// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WebAssembly.Diagnostics;
using Xunit;

#nullable enable

namespace DebuggerTests
{
    public class BadHarnessInitTests : DebuggerTests
    {
        public override async Task InitializeAsync() => await Task.CompletedTask;

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task InvalidInitCommands()
        {
            var bad_cmd_name = "non-existant.command";

            Func<InspectorClient, CancellationToken, List<(string, Task<Result>)>> fn = (client, token) =>
                new List<(string, Task<Result>)>
                {
                    ("Profiler.enable", client.SendCommand("Profiler.enable", null, token)),
                    (bad_cmd_name, client.SendCommand(bad_cmd_name, null, token))
                };

            await Ready();

            var ae = await Assert.ThrowsAsync<ArgumentException>(async () => await insp.OpenSessionAsync(fn, TestTimeout));
            Assert.Contains(bad_cmd_name, ae.Message);
        }
    }
}
