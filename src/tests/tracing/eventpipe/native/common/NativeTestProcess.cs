// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

internal static class NativeTestProcess
{
    internal static (int ExitCode, string Output, string Error) Run(string executable, params string[] arguments)
    {
        string directory = TestLibrary.Utilities.IsNativeAot
            ? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, ".."))
            : AppContext.BaseDirectory;
        string fileName = executable + (OperatingSystem.IsWindows() ? ".exe" : "");
        var startInfo = new ProcessStartInfo(Path.Combine(directory, fileName))
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = directory
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo);
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(60_000))
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
            throw new TimeoutException($"Native test executable '{fileName}' did not exit within 60 seconds.");
        }

        return (process.ExitCode, output.GetAwaiter().GetResult(), error.GetAwaiter().GetResult());
    }
}
