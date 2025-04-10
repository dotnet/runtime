// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Text;

using Xunit;

public class BridgeTester
{

    public static void RunBridgeTests(string monoGCDebug, string monoGCParams)
    {
        string corerun = TestLibrary.Utilities.IsWindows ? "corerun.exe" : "corerun";
        string coreRoot = Environment.GetEnvironmentVariable("CORE_ROOT");
        string bridgeTestApp = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Bridge.dll");

        var startInfo = new ProcessStartInfo(Path.Combine(coreRoot, corerun), bridgeTestApp);
        startInfo.EnvironmentVariables["MONO_GC_DEBUG"] = monoGCDebug;
        startInfo.EnvironmentVariables["MONO_GC_PARAMS"] = monoGCParams;

        using (Process p = Process.Start(startInfo))
        {
            p.WaitForExit();
            Console.WriteLine ("Bridge Test App returned {0}", p.ExitCode);
            if (p.ExitCode != 100)
                throw new Exception("Bridge Test App failed execution");
        }
    }

    [Fact]
    public static void RunBridgeCompare()
    {
        RunBridgeTests("bridge=BridgeBase,bridge-compare-to=new", "bridge-implementation=tarjan,bridge-require-precise-merge");
    }

    [Fact]
    public static void RunTarjan()
    {
        RunBridgeTests("bridge=BridgeBase", "bridge-implementation=tarjan");
    }
}
