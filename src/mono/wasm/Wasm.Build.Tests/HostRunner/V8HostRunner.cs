// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Wasm.Build.Tests;

using System;
using System.IO;
using System.Runtime.InteropServices;

public class V8HostRunner : IHostRunner
{
    private static string? s_binaryPathArg;
    private static string BinaryPathArg
    {
        get
        {
            if (s_binaryPathArg is null)
            {
                if (!string.IsNullOrEmpty(EnvironmentVariables.V8PathForTests))
                {
                    if (!File.Exists(EnvironmentVariables.V8PathForTests))
                        throw new Exception($"Cannot find V8_PATH_FOR_TESTS={EnvironmentVariables.V8PathForTests}");
                    s_binaryPathArg += $" --js-engine-path=\"{EnvironmentVariables.V8PathForTests}\"";
                }
                else
                {
                    s_binaryPathArg = "";
                }
            }
            return s_binaryPathArg;
        }
    }

    private string GetXharnessArgs(string jsRelativePath) => $"--js-file={jsRelativePath} --engine=V8 -v trace --engine-arg=--module {BinaryPathArg}";

    public string GetTestCommand() => "wasm test";
    public string GetXharnessArgsWindowsOS(XHarnessArgsOptions options) => GetXharnessArgs(options.jsRelativePath);
    public string GetXharnessArgsOtherOS(XHarnessArgsOptions options) => GetXharnessArgs(options.jsRelativePath);
    public bool UseWasmConsoleOutput() => true;
    public bool CanRunWBT() => !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
}
