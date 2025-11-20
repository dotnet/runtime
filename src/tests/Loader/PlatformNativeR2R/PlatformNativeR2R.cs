// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

public class PlatformNativeR2R
{
    [Fact]
    public static void EntryPoint()
    {
        Console.WriteLine("Testing platform-native R2R composite image loading...");

        // Run R2RDump to validate header information
        string coreRoot = Environment.GetEnvironmentVariable("CORE_ROOT");
        coreRoot = string.IsNullOrEmpty(coreRoot) ? Path.GetDirectoryName(Environment.ProcessPath) : coreRoot;
        string r2rDumpPath = Path.Join(coreRoot, "R2RDump", "R2RDump.dll");

        string testDotNetCmd = Environment.GetEnvironmentVariable("__TestDotNetCmd");
        ProcessStartInfo psi = new ProcessStartInfo
        {
            UseShellExecute = false,
            FileName = string.IsNullOrEmpty(testDotNetCmd) ? "dotnet" : testDotNetCmd,
            Arguments = $"\"{r2rDumpPath}\" --in \"{Assembly.GetExecutingAssembly().Location}\" --header",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using (Process process = Process.Start(psi))
        {
            process.WaitForExit();

            string output = process.StandardOutput.ReadToEnd();
            if (process.ExitCode != 0)
            {
                string stderr = process.StandardError.ReadToEnd();
                Console.WriteLine($"R2RDump failed with exit code {process.ExitCode}");
                Console.WriteLine($"stdout: {output}");
                Console.WriteLine($"stderr: {stderr}");
                Assert.Fail("R2RDump failed to execute");
            }

            Assert.True(output.Contains("READYTORUN_FLAG_Component"), "Component assembly should be associated with a platform-native composite image");
            // TODO: Uncomment assert when crossgen2 adds support for the flag
            // Assert.True(output.Contains("READYTORUN_FLAG_PlatformNativeImage"), "Component assembly should be associated with a platform-native composite image");
        }
    }
}
