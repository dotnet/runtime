// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Wasm.Build.Tests;

using System;
using System.IO;

public class BrowserHostRunner : IHostRunner
{
    private static string? s_binaryPathArg;
    private static string BinaryPathArg
    {
        get
        {
            if (s_binaryPathArg is null)
            {
                if (!string.IsNullOrEmpty(EnvironmentVariables.BrowserPathForTests))
                {
                    if (!File.Exists(EnvironmentVariables.BrowserPathForTests))
                        throw new Exception($"Cannot find BROWSER_PATH_FOR_TESTS={EnvironmentVariables.BrowserPathForTests}");
                    s_binaryPathArg = $" --browser-path=\"{EnvironmentVariables.BrowserPathForTests}\"";
                }
                else
                {
                    s_binaryPathArg = "";
                }
            }
            return s_binaryPathArg;
        }
    }


    public string GetTestCommand() => "wasm test-browser";
    public string GetXharnessArgsWindowsOS(XHarnessArgsOptions options) => $"-v trace -b {options.host} --browser-arg=--lang={options.environmentLocale} --web-server-use-cop {BinaryPathArg}";  // Windows: chrome.exe --lang=locale
    public string GetXharnessArgsOtherOS(XHarnessArgsOptions options) => $"-v trace -b {options.host} --locale={options.environmentLocale} --web-server-use-cop {BinaryPathArg}";                // Linux: LANGUAGE=locale ./chrome
    public bool UseWasmConsoleOutput() => false;
    public bool CanRunWBT() => true;
}
