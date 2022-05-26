// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.WebSockets;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.WebAssembly.AppHost;

internal sealed record WebServerOptions
(
    Func<WebSocket, Task>? OnConsoleConnected,
    string? ContentRootPath,
    bool WebServerUseCors,
    bool WebServerUseCrossOriginPolicy,
    int Port,
    string DefaultFileName = "index.html"
);
