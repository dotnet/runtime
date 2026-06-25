// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

string executableExtension = OperatingSystem.IsWindows() ? ".exe" : "";
string helloExePath = Path.Combine(AppContext.BaseDirectory, $"HelloExe{executableExtension}");

using Process? helloExe = Process.Start(new ProcessStartInfo(helloExePath)
{
    RedirectStandardError = true,
    RedirectStandardOutput = true,
    UseShellExecute = false,
});

if (helloExe is null)
{
    Console.Error.WriteLine($"Failed to start {helloExePath}");
    return 1;
}

Task<string> outputTask = helloExe.StandardOutput.ReadToEndAsync();
Task<string> errorTask = helloExe.StandardError.ReadToEndAsync();
helloExe.WaitForExit();
string output = (await outputTask).Trim();
string error = (await errorTask).Trim();
if (helloExe.ExitCode != 0 || output != "Hello from HelloExe")
{
    Console.Error.WriteLine($"Unexpected HelloExe result. Exit code: {helloExe.ExitCode}; output: '{output}'; error: '{error}'");
    return 1;
}

return 100;
