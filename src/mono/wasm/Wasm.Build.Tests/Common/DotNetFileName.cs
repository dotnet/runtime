// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Wasm.Build.Tests;

public sealed record DotNetFileName
(
    string ExpectedFilename,
    string? Hash,
    string ActualPath
);
