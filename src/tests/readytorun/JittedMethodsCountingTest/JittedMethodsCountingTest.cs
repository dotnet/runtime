// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

using Reflection = System.Reflection;
using InteropServices = System.Runtime.InteropServices;

public class JittedMethodsCountingTest
{
    private const int MAX_JITTED_METHODS_ACCEPTED = 50;

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

        if (IsRunCrossgen2Set() && IsRunningOnARM64())
        {
            Console.WriteLine("\nThis test is currently unsupported on ARM64 when"
                              + " RunCrossGen2 is enabled. Skipping...\n");
            return 100;
        }

        string testAppLocation = Path.GetDirectoryName(Reflection
                                                      .Assembly
                                                      .GetExecutingAssembly()
                                                      .Location);
        string appName = Path.Combine(testAppLocation, "HelloWorld.dll");

        // For adding any new apps for this test, make sure they return a negative
        // number when failing.
        int appResult = RunHelloWorldApp(appName);

        if (appResult < 0)
        {
            Console.WriteLine("App failed somewhere and so we can't proceed with"
                              + " the Jitted Methods analysis.");

            Console.WriteLine("App Exit Code: {0}", appResult);
            Console.WriteLine("Exiting...");

            return 101;
        }

        // App finished successfully. We can now take a look at how many methods
        // got jitted at runtime.
        Console.WriteLine("Number of Jitted Methods: {0}\n", appResult);
        return appResult > 0 && appResult <= MAX_JITTED_METHODS_ACCEPTED ? 100 : 101;
    }

    private static bool IsReadyToRunEnvSet()
    {
        string? dotnetR2R = Environment.GetEnvironmentVariable("DOTNET_ReadyToRun");
        return (string.IsNullOrEmpty(dotnetR2R) || dotnetR2R == "1");
    }

    private static bool IsRunCrossgen2Set()
    {
        string? runCrossgen2 = Environment.GetEnvironmentVariable("RunCrossGen2");
        return (runCrossgen2 == "1");
    }

    private static bool IsRunningOnARM64()
    {
        InteropServices.Architecture thisMachineArch = InteropServices
                                                      .RuntimeInformation
                                                      .OSArchitecture;

        return (thisMachineArch == InteropServices.Architecture.Arm64);
    }

    private static int RunHelloWorldApp(string appName)
    {
        // The app's exit code is the number of jitted methods it got.
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

            Console.WriteLine("\nLaunching Test App: {0} {1}", startInfo.FileName,
                                                               startInfo.Arguments);

            app.StartInfo = startInfo;
            app.Start();
            app.WaitForExit();
            appExitCode = app.ExitCode;
        }

        return appExitCode;
    }
}
