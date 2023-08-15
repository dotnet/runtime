// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

public class JittedMethodsCountingTest
{
    private const int MAX_JITTED_METHODS_ACCEPTED = 50;

    [Fact]
    public static int TestEntryPoint()
    {
        string testAppFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        
        string appName = Path.Combine(testAppFolder, "HelloWorld.dll");
        string jitOutputFile = Path.Combine(testAppFolder, "jits.txt");

        int appResult = RunHelloWorldApp(appName, jitOutputFile);
        int jits = GetNumberOfJittedMethods(jitOutputFile);

        return jits > 0 && jits <= MAX_JITTED_METHODS_ACCEPTED ? 100 : 101;
    }

    private static int RunHelloWorldApp(string appName, string jitOutputFile)
    {
        int appExitCode = -1;
        string osExeSuffix = (OperatingSystem.IsWindows() ? ".exe" : "");
        string coreRootPath = Environment.GetEnvironmentVariable("CORE_ROOT");
        string coreRunPath = Path.Combine(coreRootPath, "corerun" + osExeSuffix);

        using (Process app = new Process())
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = coreRunPath,
                Arguments = appName,
                CreateNoWindow = true,
                UseShellExecute = false,
            };

            Console.WriteLine("Launching: {0} {1}", startInfo.FileName, startInfo.Arguments);

            startInfo.EnvironmentVariables.Add("DOTNET_JitDisasmSummary", "1");
            startInfo.EnvironmentVariables.Add("DOTNET_TieredCompilation", "0");
            startInfo.EnvironmentVariables.Add("DOTNET_ReadyToRun", "1");
            startInfo.EnvironmentVariables.Add("DOTNET_JitStdOutFile", jitOutputFile);

            app.StartInfo = startInfo;
            app.Start();
            app.WaitForExit();

            appExitCode = app.ExitCode;
            Console.WriteLine("Exit code: {0}", appExitCode);
            Console.WriteLine("JIT file: {0} ({1} B)", jitOutputFile, new FileInfo(jitOutputFile).Length);
        }

        return appExitCode;
    }

    private static int GetNumberOfJittedMethods(string jitOutputFile)
    {
        string[] lines = File.ReadAllLines(jitOutputFile);
        
        Console.WriteLine("=========== App standard output ===========");
        foreach (string line in lines)
        {
            Console.WriteLine(line);
        }
        Console.WriteLine("=========== End of app standard output ===========");

        string[] tokens = lines.Last().Split(":");

        int numJittedMethods = Int32.Parse(tokens[0]);
        return numJittedMethods;
    }
}

