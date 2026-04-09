// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Wasm.Build.Tests;

public record RunResult(
    int ExitCode,
    IReadOnlyCollection<string> TestOutput,
    IReadOnlyCollection<string> ConsoleOutput,
    IReadOnlyCollection<string> ServerOutput
);
