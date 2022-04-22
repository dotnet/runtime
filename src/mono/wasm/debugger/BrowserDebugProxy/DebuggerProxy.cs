// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.WebAssembly.Diagnostics
{

    // This type is the public entrypoint that allows external code to attach the debugger proxy
    // to a given websocket listener. Everything else in this package can be internal.

    public class DebuggerProxy : DebuggerProxyBase
    {
        internal MonoProxy MonoProxy { get; }

        public DebuggerProxy(ILoggerFactory loggerFactory, IList<string> urlSymbolServerList, int runtimeId = 0, string loggerId = "")
        {
            MonoProxy = new MonoProxy(loggerFactory, urlSymbolServerList, runtimeId, loggerId);
        }

        public Task Run(Uri browserUri, WebSocket ideSocket, CancellationTokenSource cts)
        {
            return MonoProxy.RunForDevTools(browserUri, ideSocket, cts);
        }

        public override void Shutdown() => MonoProxy.Shutdown();
    }
}
