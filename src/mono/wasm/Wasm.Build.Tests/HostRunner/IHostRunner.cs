// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Wasm.Build.Tests;

public record XHarnessArgsOptions(string jsRelativePath, string environmentLocale, RunHost host);

interface IHostRunner
{
    string GetTestCommand();
    string GetXharnessArgsWindowsOS(XHarnessArgsOptions options);
    string GetXharnessArgsOtherOS(XHarnessArgsOptions options);
    bool UseWasmConsoleOutput();
    bool CanRunWBT();
}
