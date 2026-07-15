// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.WebAssembly.AppHost;

// FIXME: can be simplified to string[]
public record ServerURLs(string Http, string? Https, string? DebugPath = null);
