// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.WebAssembly.Diagnostics
{

    // This type is the public entrypoint that allows external code to attach the debugger proxy
    // to a given websocket listener. Everything else in this package can be internal.

    public class DebuggerProxy
    {
        private readonly MonoProxy proxy;

        public DebuggerProxy(ILoggerFactory loggerFactory, IList<string> urlSymbolServerList, int runtimeId = 0)
        {
            proxy = new MonoProxy(loggerFactory, urlSymbolServerList, runtimeId);
        }

        public Task Run(Uri browserUri, WebSocket ideSocket)
        {
            return proxy.Run(browserUri, ideSocket);
        }
    }
}
