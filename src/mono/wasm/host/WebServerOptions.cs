// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.WebAssembly.AppHost;

internal sealed record WebServerOptions
(
    string? ContentRootPath,
    bool WebServerUseCors,
    bool WebServerUseCrossOriginPolicy,
    string [] Urls,
    string DefaultFileName = "index.html"
);
