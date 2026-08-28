// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

public class Program
{
    private const string ChildEnvironmentVariable = "DOTNET_TEST_132581_CHILD";

    [DllImport("nativetest132581")]
    private static extern int InstallSignalHandlerAndExec(string executable, string managedAssembly);

    [DllImport("nativetest132581")]
    private static extern int SendSignalFromChildProcess();

    [Fact]
    public static void TestEntryPoint()
    {
        if (Environment.GetEnvironmentVariable(ChildEnvironmentVariable) is null)
        {
            string executable = Environment.ProcessPath ?? throw new InvalidOperationException("The process path is unavailable.");
            string command = Environment.GetCommandLineArgs()[0];
            string managedAssembly = string.Equals(executable, command, StringComparison.Ordinal) ? string.Empty : command;

            int error = InstallSignalHandlerAndExec(executable, managedAssembly);
            throw new InvalidOperationException($"execv failed with error {error}.");
        }

        Assert.Equal(0, SendSignalFromChildProcess());
    }
}
