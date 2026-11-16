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

public class InterpreterTester
{
    [SkipOnCoreClr("Temporarily disabled due to problems generating interpreter bytecode for methods with an existing prestub.", RuntimeTestModes.AnyJitOptimizationStress)]
    [SkipOnCoreClr("Temporarily disabled due to https://github.com/dotnet/runtime/issues/112827.", RuntimeTestModes.AnyGCStress)]
    [Fact]
    public static void RunTests()
    {
        string coreRoot = Environment.GetEnvironmentVariable("CORE_ROOT");
        string interpreterApp = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Interpreter.dll");

        ProcessStartInfo startInfo = new ProcessStartInfo(Path.Combine(coreRoot, "corerun"), [interpreterApp]);
        startInfo.InheritedHandles = [];
        startInfo.EnvironmentVariables["DOTNET_Interpreter"] = "RunInterpreterTests";

        ProcessExitStatus status = Process.Run(startInfo);
        Console.WriteLine ("Interpreted App returned {0}", status.ExitCode);
        if (status.ExitCode != 100)
            throw new Exception("Interpreted App failed execution");
    }
}
