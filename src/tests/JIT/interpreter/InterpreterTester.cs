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
    [Fact]
    public static void RunTests()
    {
        string corerun = TestLibrary.Utilities.IsWindows ? "corerun.exe" : "corerun";
        string libInterp = TestLibrary.Utilities.IsWindows ? "clrinterpreter.dll" : (TestLibrary.Utilities.IsMacOSX ? "libclrinterpreter.dylib" : "libclrinterpreter.so");
        string coreRoot = Environment.GetEnvironmentVariable("CORE_ROOT");
        string interpreterApp = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Interpreter.dll");

        var startInfo = new ProcessStartInfo(Path.Combine(coreRoot, corerun), interpreterApp);
        startInfo.EnvironmentVariables["DOTNET_AltJitName"] = libInterp;
        startInfo.EnvironmentVariables["DOTNET_AltJitPath"] = Path.Combine(coreRoot, libInterp);
        startInfo.EnvironmentVariables["DOTNET_AltJit"] = "RunInterpreterTests";

        using (Process p = Process.Start(startInfo))
        {
            p.WaitForExit();
            Console.WriteLine ("Interpreted App returned {0}", p.ExitCode);
            if (p.ExitCode != 100)
                throw new Exception("Interpreted App failed execution");
        }
    }
}
