// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.WebAssembly.AppHost.DevServer;

internal sealed record DevServerOptions
(
    string? StaticWebAssetsPath,
    string? StaticWebAssetsEndpointsPath,
    bool WebServerUseCors,
    bool WebServerUseCrossOriginPolicy,
    string[] Urls,
    string DefaultFileName = "index.html"
);
