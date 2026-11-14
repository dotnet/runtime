// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using TestLibrary;

namespace System.Runtime
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class BypassReadyToRunAttribute : Attribute
    {
    }
}

public class TailCallImportException
{
    private const string ChildArgument = "--unhandled";
    private const string ImportScenario = "import";
    private const string DispatchScenario = "dispatch";

    public static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == ChildArgument)
        {
            Utilities.DisableOSCoreDump();
            return args[1] switch
            {
                ImportScenario => ThrowFromTailCall(),
                DispatchScenario => ThrowFromInterfaceTailCall(),
                _ => 102,
            };
        }

        if (!RunChild(ImportScenario, "System.InvalidProgramException", nameof(ThrowFromTailCall)))
        {
            return 101;
        }

        if (!RunChild(DispatchScenario, "System.Runtime.AmbiguousImplementationException", nameof(ThrowFromInterfaceTailCall)))
        {
            return 101;
        }

        return 100;
    }

    private static bool RunChild(string scenario, string exceptionName, string callerName)
    {
        ProcessStartInfo startInfo = new(Environment.ProcessPath, [typeof(TailCallImportException).Assembly.Location, ChildArgument, scenario])
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
        Console.WriteLine($"Scenario '{scenario}': child process exit code: 0x{output.ExitStatus.ExitCode:X8}, expected: 0x{expectedExitCode:X8}");
        string diagnostics = output.StandardOutput + output.StandardError;
        Console.Write(diagnostics);

        return output.ExitStatus.ExitCode == expectedExitCode &&
            output.StandardError.Contains($"Unhandled exception. {exceptionName}") &&
            output.StandardError.Contains($"at {nameof(TailCallImportException)}.{callerName}") &&
            !diagnostics.Contains("Assert failure") &&
            !diagnostics.Contains("Fatal error");
    }

    // The caller must be interpreted while the helper runs as compiled code, so that the
    // exception unwinds through the native frame (ExternalMethodFrame or StubDispatchFrame)
    // pushed by the resolver the helper tail calls into.
    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe int ThrowFromTailCall()
    {
        delegate* managed<int> target = &TailCallHelper.TailCall;
        return target();
    }

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe int ThrowFromInterfaceTailCall()
    {
        delegate* managed<IAmbiguous, int> target = &TailCallHelper.InterfaceTailCall;
        return target(TailCallHelper.CreateAmbiguous());
    }
}