// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TestLibrary;

public class TailCallImportException
{
    private const string ChildArgument = "--unhandled";

    public static int Main(string[] args)
    {
        if (args.Length != 0 && args[0] == ChildArgument)
        {
            Utilities.DisableOSCoreDump();
            return ThrowFromTailCall();
        }

        ProcessStartInfo startInfo = new(Environment.ProcessPath, [typeof(TailCallImportException).Assembly.Location, ChildArgument])
        {
            InheritedHandles = [],
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.Environment["DOTNET_DbgEnableMiniDump"] = "0";
        startInfo.Environment["DOTNET_EnableCrashReport"] = "0";
        startInfo.Environment["DOTNET_ReadyToRun"] = "1";
        // InterpMode 2 and 3 disable ReadyToRun, so select only this assembly instead.
        startInfo.Environment["DOTNET_InterpMode"] = "0";
        startInfo.Environment["DOTNET_Interpreter"] = "TailCallImportException!*";
        startInfo.Environment["DOTNET_TieredCompilation"] = "0";

        ProcessTextOutput output = Process.RunAndCaptureText(startInfo, TimeSpan.FromMinutes(2));
        int expectedExitCode = OperatingSystem.IsWindows() ? unchecked((int)0xE0434352) : 128 + 6;
        Console.WriteLine($"Child process exit code: 0x{output.ExitStatus.ExitCode:X8}, expected: 0x{expectedExitCode:X8}");
        string diagnostics = output.StandardOutput + output.StandardError;
        Console.Write(diagnostics);

        if (output.ExitStatus.ExitCode != expectedExitCode ||
            !output.StandardError.Contains("Unhandled exception. System.InvalidProgramException") ||
            !output.StandardError.Contains($"at {nameof(TailCallImportException)}.{nameof(ThrowFromTailCall)}") ||
            diagnostics.Contains("Assert failure") ||
            diagnostics.Contains("Fatal error"))
        {
            return 101;
        }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe int ThrowFromTailCall()
    {
        delegate* managed<int> target = &TailCallHelper.TailCall;
        return target();
    }
}
