// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

unsafe class FatalErrorHandlerTest
{
    // Marker strings written to stderr by the native handlers.
    const string HandlerInvokedMarker = "FATAL_HANDLER_INVOKED";
    const string LogReceivedMarker = "FATAL_LOG_RECEIVED:";

    //
    // P/Invoke declarations for the native handler library.
    //

    [DllImport("FatalErrorHandlerNative")]
    private static extern delegate* unmanaged<int, void*, int> GetHandlerSkipDefault();

    [DllImport("FatalErrorHandlerNative")]
    private static extern delegate* unmanaged<int, void*, int> GetHandlerRunDefault();

    [DllImport("FatalErrorHandlerNative")]
    private static extern delegate* unmanaged<int, void*, int> GetHandlerWithLog();

    //
    // Child process entry points — register handler, trigger FailFast.
    //

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void RunChildSkipHandler()
    {
        ExceptionHandling.SetFatalErrorHandler(GetHandlerSkipDefault());
        Environment.FailFast("test fatal error");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void RunChildRunHandler()
    {
        ExceptionHandling.SetFatalErrorHandler(GetHandlerRunDefault());
        Environment.FailFast("test fatal error");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void RunChildLogHandler()
    {
        ExceptionHandling.SetFatalErrorHandler(GetHandlerWithLog());
        Environment.FailFast("test fatal error");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RunChildSetNull()
    {
        try
        {
            ExceptionHandling.SetFatalErrorHandler(null);
            return 1;
        }
        catch (ArgumentNullException)
        {
            return 100;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RunChildSetTwice()
    {
        ExceptionHandling.SetFatalErrorHandler(GetHandlerSkipDefault());
        try
        {
            ExceptionHandling.SetFatalErrorHandler(GetHandlerRunDefault());
            return 1;
        }
        catch (InvalidOperationException)
        {
            return 100;
        }
    }

    //
    // Parent — launches child processes and validates results.
    //

    static (int exitCode, string stderr) LaunchChild(string scenario)
    {
        // For NativeAOT, Assembly.Location is empty — the process IS the test binary.
        // For CoreCLR, we need to pass the DLL path as the first argument to corerun.
        string arguments = TestLibrary.Utilities.IsNativeAot
            ? scenario
            : $"\"{Assembly.GetExecutingAssembly().Location}\" {scenario}";

        ProcessStartInfo startInfo = new(Environment.ProcessPath!, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.Environment["DOTNET_DbgEnableMiniDump"] = "0";

        ProcessTextOutput result = Process.RunAndCaptureText(startInfo);

        return (result.ExitStatus.ExitCode, result.StandardError);
    }

    static bool TestSkipHandler()
    {
        Console.WriteLine("=== TestSkipHandler ===");
        var (exitCode, stderr) = LaunchChild("skip-handler");

        bool handlerInvoked = stderr.Contains(HandlerInvokedMarker);
        // SkipDefaultHandler suppresses the runtime's crash log output but the
        // process still terminates via the normal fatal path (Abort/RaiseFailFastException).
        bool noRuntimeOutput = !stderr.Contains("Process terminated.");
        bool exited = exitCode != 0;

        Console.WriteLine($"  Exit code: 0x{exitCode:X8}, handler invoked: {handlerInvoked}, no runtime output: {noRuntimeOutput}");
        if (!handlerInvoked)
            Console.WriteLine("  FAIL: Handler was not invoked");
        if (!noRuntimeOutput)
            Console.WriteLine("  FAIL: Runtime crash log should be suppressed by SkipDefaultHandler");
        if (!exited)
            Console.WriteLine("  FAIL: Expected non-zero exit code");

        return handlerInvoked && noRuntimeOutput && exited;
    }

    static bool TestRunHandler()
    {
        Console.WriteLine("=== TestRunHandler ===");
        var (exitCode, stderr) = LaunchChild("run-handler");

        bool handlerInvoked = stderr.Contains(HandlerInvokedMarker);
        // RunDefaultHandler lets the runtime proceed with its default fatal handling.
        // The exit code varies by runtime and platform, so only verify the handler ran.
        bool exited = exitCode != 0;

        Console.WriteLine($"  Exit code: 0x{exitCode:X8}, handler invoked: {handlerInvoked}, exited: {exited}");
        if (!handlerInvoked)
            Console.WriteLine("  FAIL: Handler was not invoked");
        if (!exited)
            Console.WriteLine("  FAIL: Expected non-zero exit code");

        return handlerInvoked && exited;
    }

    static bool TestLogHandler()
    {
        Console.WriteLine("=== TestLogHandler ===");
        var (exitCode, stderr) = LaunchChild("log-handler");

        bool handlerInvoked = stderr.Contains(HandlerInvokedMarker);
        bool logReceived = stderr.Contains(LogReceivedMarker);
        bool logContainsMessage = stderr.Contains("test fatal error");
        bool exited = exitCode != 0;

        Console.WriteLine($"  Exit code: 0x{exitCode:X8}, handler invoked: {handlerInvoked}, log received: {logReceived}, log has message: {logContainsMessage}");
        if (!handlerInvoked)
            Console.WriteLine("  FAIL: Handler was not invoked");
        if (!logReceived)
            Console.WriteLine("  FAIL: pfnGetFatalErrorLog callback was not invoked");
        if (!logContainsMessage)
            Console.WriteLine("  FAIL: Log did not contain the FailFast message");

        return handlerInvoked && logReceived && logContainsMessage && exited;
    }

    static bool TestSetNull()
    {
        Console.WriteLine("=== TestSetNull ===");
        var (exitCode, _) = LaunchChild("set-null");

        Console.WriteLine($"  Exit code: {exitCode}");
        if (exitCode != 100)
            Console.WriteLine("  FAIL: Expected exit code 100 (ArgumentNullException caught)");

        return exitCode == 100;
    }

    static bool TestSetTwice()
    {
        Console.WriteLine("=== TestSetTwice ===");
        var (exitCode, _) = LaunchChild("set-twice");

        Console.WriteLine($"  Exit code: {exitCode}");
        if (exitCode != 100)
            Console.WriteLine("  FAIL: Expected exit code 100 (InvalidOperationException caught)");

        return exitCode == 100;
    }

    //
    // Main entry point — parent or child depending on args.
    //

    static int Main(string[] args)
    {
        if (args.Length > 0)
        {
            switch (args[0])
            {
                case "skip-handler": RunChildSkipHandler(); return 1;
                case "run-handler":  RunChildRunHandler();  return 1;
                case "log-handler":  RunChildLogHandler();  return 1;
                case "set-null":     return RunChildSetNull();
                case "set-twice":    return RunChildSetTwice();
                default:
                    Console.Error.WriteLine($"Unknown scenario: {args[0]}");
                    return 1;
            }
        }

        bool allPassed = true;
        allPassed &= TestSkipHandler();
        allPassed &= TestRunHandler();
        allPassed &= TestLogHandler();
        allPassed &= TestSetNull();
        allPassed &= TestSetTwice();

        Console.WriteLine();
        Console.WriteLine(allPassed ? "ALL TESTS PASSED" : "SOME TESTS FAILED");

        return allPassed ? 100 : 1;
    }
}
