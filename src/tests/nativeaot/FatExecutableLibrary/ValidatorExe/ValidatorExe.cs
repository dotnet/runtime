// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;

string executableExtension = OperatingSystem.IsWindows() ? ".exe" : "";
string helloExePath = Path.Combine(AppContext.BaseDirectory, $"HelloExe{executableExtension}");

using Process? helloExe = Process.Start(new ProcessStartInfo(helloExePath)
{
    RedirectStandardOutput = true,
    UseShellExecute = false,
});

if (helloExe is null)
{
    Console.Error.WriteLine($"Failed to start {helloExePath}");
    return 1;
}

helloExe.WaitForExit();
string output = helloExe.StandardOutput.ReadToEnd().Trim();
if (helloExe.ExitCode != 0 || output != "Hello from HelloExe")
{
    Console.Error.WriteLine($"Unexpected HelloExe result. Exit code: {helloExe.ExitCode}; output: '{output}'");
    return 1;
}

return 100;
