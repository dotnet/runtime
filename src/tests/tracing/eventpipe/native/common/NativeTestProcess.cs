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
        string baseDirectory = AppContext.BaseDirectory;
        if (string.IsNullOrEmpty(baseDirectory))
        {
            baseDirectory = Environment.CurrentDirectory;
        }
        string directory = TestLibrary.Utilities.IsNativeAot
            ? Path.Combine(baseDirectory, "..")
            : baseDirectory;
        string fileName = executable + (OperatingSystem.IsWindows() ? ".exe" : "");
        var startInfo = new ProcessStartInfo(Path.GetFullPath(Path.Combine(directory, fileName)))
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        DirectoryInfo workingDirectory = Directory.CreateTempSubdirectory(nameof(NativeTestProcess) + "-");
        try
        {
            startInfo.WorkingDirectory = workingDirectory.FullName;
            using Process process = Process.Start(startInfo);
            try
            {
                Task<string> output = process.StandardOutput.ReadToEndAsync();
                Task<string> error = process.StandardError.ReadToEndAsync();

                if (!process.WaitForExit(60_000))
                {
                    throw new TimeoutException($"Native test executable '{fileName}' did not exit within 60 seconds.");
                }

                return (process.ExitCode, output.GetAwaiter().GetResult(), error.GetAwaiter().GetResult());
            }
            finally
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit();
                }
            }
        }
        finally
        {
            workingDirectory.Delete(recursive: true);
        }
    }
}
