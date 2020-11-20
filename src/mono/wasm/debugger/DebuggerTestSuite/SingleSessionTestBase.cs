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
    public abstract class SingleSessionTestBase : DebuggerTestBase, IAsyncLifetime
    {
        internal Inspector insp;
        protected Dictionary<string, string> scripts;

        public SingleSessionTestBase(string driver = "debugger-driver.html") : base(driver)
        {
            insp = new Inspector();
            scripts = SubscribeToScripts(insp);
        }

        public virtual async Task InitializeAsync()
        {
            Func<InspectorClient, CancellationToken, List<(string, Task<Result>)>> fn = (client, token) =>
            {
                Func<string, (string, Task<Result>)> getInitCmdFn = (cmd) => (cmd, client.SendCommand(cmd, null, token));
                var init_cmds = new List<(string, Task<Result>)>
                {
                    getInitCmdFn("Profiler.enable"),
                    getInitCmdFn("Runtime.enable"),
                    getInitCmdFn("Debugger.enable"),
                    getInitCmdFn("Runtime.runIfWaitingForDebugger")
                };

                return init_cmds;
            };

            await Ready();
            await insp.OpenSessionAsync(fn);
            ctx = new DebugTestContext(insp.Client, insp, insp.Token, scripts);
        }

        public virtual async Task DisposeAsync() => await insp.ShutdownAsync().ConfigureAwait(false);
    }
}
