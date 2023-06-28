// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Wasm.Build.Tests;

using System.Runtime.InteropServices;

public class V8HostRunner : IHostRunner
{
    private string GetXharnessArgs(string jsRelativePath) => $"--js-file={jsRelativePath} --engine=V8 -v trace --engine-arg=--experimental-wasm-simd --engine-arg=--module";

    public string GetTestCommand() => "wasm test";
    public string GetXharnessArgsWindowsOS(XHarnessArgsOptions options) => GetXharnessArgs(options.jsRelativePath);
    public string GetXharnessArgsOtherOS(XHarnessArgsOptions options) => GetXharnessArgs(options.jsRelativePath);
    public bool UseWasmConsoleOutput() => true;
    public bool CanRunWBT() => !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
}
