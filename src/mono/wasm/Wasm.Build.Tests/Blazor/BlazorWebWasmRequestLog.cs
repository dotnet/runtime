// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

public record BlazorWebWasmRequestLog(
    DateTime Timestamp,
    string Method,
    string Path,
    int StatusCode
);
