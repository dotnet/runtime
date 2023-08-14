// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

public class JittedMethodsCountingTest
{
    private const int MAX_JITTED_METHODS_ACCEPTED = 50;

    [Fact]
    public static int TestEntryPoint()
    {
        string appName = "HelloWorld.dll";
        string jitOutputFile = "jits.txt";

        int appResult = RunHelloWorldApp(appName, jitOutputFile);
        int jits = GetNumberOfJittedMethods(jitOutputFile);

        return jits < MAX_JITTED_METHODS_ACCEPTED ? 100 : 101;
    }

    private static int RunHelloWorldApp(string appName, string jitOutputFile)
    {
        int appExitCode = -1;

        using (Process app = new Process())
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = appName,
                CreateNoWindow = true,
                UseShellExecute = false,
            };

            startInfo.EnvironmentVariables.Add("DOTNET_JitDisasmSummary", 1);
            startInfo.EnvironmentVariables.Add("DOTNET_TieredCompilation", 0);
            startInfo.EnvironmentVariables.Add("DOTNET_ReadyToRun", 1);
            startInfo.EnvironmentVariables.Add("DOTNET_JitStdOutFile", jitOutputFile);

            app.StartInfo = startInfo;
            app.Start();
            app.WaitForExit();

            appExitCode = app.ExitCode;
        }

        return appExitCode;
    }

    private static int GetNumberOfJittedMethods(string jitOutputFile)
    {
        string[] tokens = File.ReadLines(jitOutputFile)
                              .Last()
                              .Split(":");

        int numJittedMethods = Int32.Parse(tokens[0]);
        return numJittedMethods;
    }
}

