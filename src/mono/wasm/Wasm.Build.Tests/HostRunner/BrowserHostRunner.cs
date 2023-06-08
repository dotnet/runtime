// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Wasm.Build.Tests;

public class BrowserHostRunner : IHostRunner
{
    public string GetTestCommand() => "wasm test-browser";
    public string GetXharnessArgsWindowsOS(XHarnessArgsOptions options) => $"-v trace -b {options.host} --browser-arg=--lang={options.environmentLocale} --web-server-use-cop";  // Windows: chrome.exe --lang=locale
    public string GetXharnessArgsOtherOS(XHarnessArgsOptions options) => $"-v trace -b {options.host} --locale={options.environmentLocale} --web-server-use-cop";                // Linux: LANGUAGE=locale ./chrome
    public bool UseWasmConsoleOutput() => false;
    public bool CanRunWBT() => true;
}
