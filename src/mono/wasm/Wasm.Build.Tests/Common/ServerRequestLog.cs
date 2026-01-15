// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Wasm.Build.Tests;

using System;

public record ServerRequestLog(
    DateTime Timestamp,
    string Method,
    string Path,
    int StatusCode
);