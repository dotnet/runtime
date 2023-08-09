// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Wasm.Build.Tests;

public class NodeJSHostRunner : IHostRunner
{
    public string GetTestCommand() => "wasm test";
    public string GetXharnessArgsWindowsOS(XHarnessArgsOptions options) => $"--js-file={options.jsRelativePath} --engine=NodeJS -v trace --engine-arg=--experimental-wasm-simd --engine-arg=--experimental-wasm-eh";
    public string GetXharnessArgsOtherOS(XHarnessArgsOptions options) => $"--js-file={options.jsRelativePath} --engine=NodeJS -v trace --locale={options.environmentLocale} --engine-arg=--experimental-wasm-simd --engine-arg=--experimental-wasm-eh";
    public bool UseWasmConsoleOutput() => true;
    public bool CanRunWBT() => true;
}
