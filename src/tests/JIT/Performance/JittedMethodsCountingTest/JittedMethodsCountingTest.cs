// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

public class JittedMethodsCountingTest
{
    private const int MAX_JITTED_METHODS_ACCEPTED = 120;

    [Fact]
    public static int TestEntryPoint()
    {
        // If DOTNET_ReadyToRun is disabled, then this test ought to be skipped.
        if (!IsReadyToRunEnvSet())
        {
            Console.WriteLine("\nThis test is only supported in ReadyToRun scenarios."
                              + " Skipping...\n");
            return 100;
        }

        string appName = "HelloWorld.dll";
        string jitOutputFile = "jits.txt";

        // DOTNET_JitStdOutFile appends to the file in question if it exists.
        // This can potentially cause issues, so we have to make sure it is
        // created exclusively for this test's purpose.
        if (File.Exists(jitOutputFile))
        {
            File.Delete(jitOutputFile);
        }

        // For adding any new apps for this test, make sure their success return
        // code is 0, so we can universally handle when they fail.
        int appResult = RunHelloWorldApp(appName, jitOutputFile);

        if (appResult != 0)
        {
            Console.WriteLine("App failed somewhere and so we can't proceed with"
                              + " the Jitted Methods analysis. Exiting...");
            return 101;
        }

        // App finished successfully. We can now take a look at the methods that
        // got jitted at runtime.
        int jits = GetNumberOfJittedMethods(jitOutputFile);
        return jits > 0 && jits <= MAX_JITTED_METHODS_ACCEPTED ? 100 : 101;
    }

    private static bool IsReadyToRunEnvSet()
    {
        string? dotnetR2R = Environment.GetEnvironmentVariable("DOTNET_ReadyToRun");
        return (dotnetR2R is null || dotnetR2R == "1") ? true : false;
    }

    private static int RunHelloWorldApp(string appName, string jitOutputFile)
    {
        int appExitCode = -1;

        // Set up our CoreRun call.
        string coreRoot = Environment.GetEnvironmentVariable("CORE_ROOT");
        string exeSuffix = OperatingSystem.IsWindows() ? ".exe" : "";
        string coreRun = Path.Combine(coreRoot, $"corerun{exeSuffix}");

        using (Process app = new Process())
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = coreRun,
                Arguments = appName,
                CreateNoWindow = true,
                UseShellExecute = false,
            };

            // Set up the environment for running the test app. We are looking
            // to seeing how many methods were jitted at runtime. So, we ask
            // the runtime to print them out with DOTNET_JitDisasmSummary, and
            // write them out to a file we can parse and investigate later
            // with DOTNET_JitStdOutFile.
            startInfo.EnvironmentVariables.Add("DOTNET_JitDisasmSummary", "1");
            startInfo.EnvironmentVariables.Add("DOTNET_JitStdOutFile", jitOutputFile);

            Console.WriteLine("\nLaunching Test App: {0} {1}", startInfo.FileName,
                                                               startInfo.Arguments);

            app.StartInfo = startInfo;
            app.Start();
            app.WaitForExit();

            appExitCode = app.ExitCode;
            Console.WriteLine("App Exit Code: {0}", appExitCode);
            Console.WriteLine("Jitted Methods Generated File: {0}", jitOutputFile);
        }

        return appExitCode;
    }

    private static int GetNumberOfJittedMethods(string jitOutputFile)
    {
        string[] lines = File.ReadAllLines(jitOutputFile);

        // Print out the jitted methods from the app run previously. This is
        // mostly done as additional logging to simplify potential bug investigations
        // in the future.

        Console.WriteLine("\n========== App Jitted Methods Start ==========");
        foreach (string line in lines)
        {
            Console.WriteLine(line);
        }
        Console.WriteLine("========== App Jitted Methods End ==========\n");

        // The jitted methods are printed in the following format:
        //
        //    method number: method description
        //
        // This is why we split by ':' and parse the left side as a number.

        string[] tokens = lines.Last().Split(":");
        int numJittedMethods = Int32.Parse(tokens[0]);
        Console.WriteLine("Total Jitted Methods: {0}\n", numJittedMethods);

        return numJittedMethods;
    }
}

