// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;

string executableExtension = OperatingSystem.IsWindows() ? ".exe" : "";
string helloExePath = Path.Combine(AppContext.BaseDirectory, $"HelloExe{executableExtension}");

ProcessTextOutput result = Process.RunAndCaptureText(new ProcessStartInfo(helloExePath)
{
    RedirectStandardError = true,
    RedirectStandardOutput = true,
    UseShellExecute = false,
});

string output = result.StandardOutput.Trim();
if (result.ExitStatus.ExitCode != 0 || output != "Hello from HelloExe")
{
    string error = result.StandardError.Trim();
    Console.Error.WriteLine($"Unexpected HelloExe result. Exit code: {result.ExitStatus.ExitCode}; output: '{output}'; error: '{error}'");
    return 1;
}

return 100;
