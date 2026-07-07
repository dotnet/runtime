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
    const string InfoMarker = "FATAL_INFO:";

    //
    // P/Invoke declarations for the native handler library.
    //

    [DllImport("FatalErrorHandlerNative")]
    private static extern delegate* unmanaged<int, void*, int> GetHandlerSkipDefault();

    [DllImport("FatalErrorHandlerNative")]
    private static extern delegate* unmanaged<int, void*, int> GetHandlerRunDefault();

    [DllImport("FatalErrorHandlerNative")]
    private static extern delegate* unmanaged<int, void*, int> GetHandlerWithLog();

    [DllImport("FatalErrorHandlerNative")]
    private static extern delegate* unmanaged<int, void*, int> GetHandlerCheckInfo();

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

    // Held in a volatile static so the JIT cannot fold the dereference into a
    // null-reference check or optimize the faulting store away. The address is
    // well above the null page, so the fault is reported as an access violation
    // (a corrupted-state exception) rather than a NullReferenceException.
    static volatile nint s_badAddress = unchecked((nint)0xDEADBEEF);

    // A null address, held in a volatile static so the JIT emits a real faulting
    // store. A write here faults in the null page and is reported as a catchable
    // NullReferenceException hardware fault (unlike s_badAddress, which yields an
    // uncatchable access violation).
    static volatile nint s_nullAddress = 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void RunChildNativeException()
    {
        ExceptionHandling.SetFatalErrorHandler(GetHandlerCheckInfo());
        // Trigger an access violation from managed code. This is a fatal
        // corrupted-state exception that reaches the handler with the
        // platform-specific exception info and thread context populated.
        *(int*)s_badAddress = 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void RunChildNestedNativeException()
    {
        ExceptionHandling.SetFatalErrorHandler(GetHandlerCheckInfo());

        // Trigger an outer hardware fault (A) whose catch clause runs an exception
        // filter. The filter itself triggers and swallows a *nested* hardware fault
        // (B) during A's first pass — before A is determined to be unhandled — and
        // then returns false so A remains unhandled and reaches FailFast.
        //
        // This is the nested-exception case where "multiple exceptions get handled
        // before the original exception becomes unhandled": handling B must not
        // discard the native signal/exception records captured for the still-in-flight
        // outer fault A. When A finally reaches the fatal error handler, its own
        // platform records must still be surfaced.
        //
        // Uses null (catchable NullReferenceException) hardware faults rather than an
        // access violation so the outer fault dispatches through managed EH (running
        // the filter) instead of being treated as an uncatchable corrupted state. This
        // path is meaningful only on NativeAOT, where every unhandled exception is
        // routed through the classlib FailFast (and thus the fatal error handler).
        try
        {
            *(int*)s_nullAddress = 0; // fault A
        }
        catch (Exception) when (TriggerAndSwallowNestedFault())
        {
            // Unreached: the filter always returns false.
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TriggerAndSwallowNestedFault()
    {
        try
        {
            *(int*)s_nullAddress = 0; // fault B, nested within A's first pass
        }
        catch
        {
            // Swallow B. Catching a hardware fault clears the "current fault"
            // record tracking, which under single-slot storage also loses A's records.
        }

        return false;
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
        // RunDefaultHandler lets the runtime proceed with its default fatal handling,
        // which must print the standard crash log to stderr: the FailFast header
        // ("Process terminated.") followed by the FailFast message.
        bool defaultOutput = stderr.Contains("Process terminated.") && stderr.Contains("test fatal error");
        // Regression guard: the runtime must emit the default crash log exactly once.
        // A second emission re-enters the crashing-thread guard in the fatal error
        // logging path, which prints this marker instead of a second crash log.
        bool singleEmission = !stderr.Contains("Fatal error while logging another fatal error.");
        // The exit code varies by runtime and platform, so only verify the process exited.
        bool exited = exitCode != 0;

        Console.WriteLine($"  Exit code: 0x{exitCode:X8}, handler invoked: {handlerInvoked}, default output: {defaultOutput}, single emission: {singleEmission}, exited: {exited}");
        if (!handlerInvoked)
            Console.WriteLine("  FAIL: Handler was not invoked");
        if (!defaultOutput)
            Console.WriteLine("  FAIL: Default crash log (header + message) was not emitted");
        if (!singleEmission)
            Console.WriteLine("  FAIL: Default crash log was emitted more than once");
        if (!exited)
            Console.WriteLine("  FAIL: Expected non-zero exit code");

        return handlerInvoked && defaultOutput && singleEmission && exited;
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

    static bool TestNativeException(string scenario)
    {
        Console.WriteLine($"=== TestNativeException ({scenario}) ===");
        var (exitCode, stderr) = LaunchChild(scenario);

        bool isNativeAot = TestLibrary.Utilities.IsNativeAot;
        bool handlerInvoked = stderr.Contains(HandlerInvokedMarker);
        bool infoReceived = stderr.Contains(InfoMarker);

        // "info" is the platform's signal/exception record.
        //  - CoreCLR: populated on signal-based Unix and Windows; on Apple platforms the
        //    runtime uses Mach exceptions, so no such record is provided.
        //  - NativeAOT: populated on all platforms (Windows exception record on Windows,
        //    POSIX siginfo everywhere else, including Apple since NativeAOT uses signals).
        bool infoExpected = isNativeAot || !OperatingSystem.IsMacOS();
        bool infoPopulated = stderr.Contains(infoExpected ? "info=true" : "info=false");

        // "context" is the thread context at the point of failure. Both runtimes surface
        // it on every platform (Mach thread state on CoreCLR Apple, ucontext/CONTEXT
        // elsewhere).
        bool contextExpected = true;
        bool contextPopulated = stderr.Contains(contextExpected ? "context=true" : "context=false");
        bool exited = exitCode != 0;

        Console.WriteLine($"  Exit code: 0x{exitCode:X8}, handler invoked: {handlerInvoked}, info expected: {infoExpected}, info ok: {infoPopulated}, context expected: {contextExpected}, context ok: {contextPopulated}");
        if (!handlerInvoked)
            Console.WriteLine("  FAIL: Handler was not invoked");
        if (!infoReceived)
            Console.WriteLine("  FAIL: Handler did not report native exception properties");
        if (!infoPopulated)
            Console.WriteLine($"  FAIL: exception record was {(infoExpected ? "not populated" : "unexpectedly populated")} for a native exception");
        if (!contextPopulated)
            Console.WriteLine($"  FAIL: thread context was {(contextExpected ? "not populated" : "unexpectedly populated")} for a native exception");
        if (!exited)
            Console.WriteLine("  FAIL: Expected non-zero exit code");

        return handlerInvoked && infoReceived && infoPopulated && contextPopulated && exited;
    }

    static bool TestNestedHardwareFault()
    {
        Console.WriteLine("=== TestNestedHardwareFault ===");

        // This scenario only reaches the fatal error handler on NativeAOT, where every
        // unhandled exception is routed through the classlib FailFast.
        if (!TestLibrary.Utilities.IsNativeAot)
        {
            Console.WriteLine("  SKIP: not applicable outside NativeAOT");
            return true;
        }

        var (exitCode, stderr) = LaunchChild("nested-native-exception");

        bool handlerInvoked = stderr.Contains(HandlerInvokedMarker);
        bool infoReceived = stderr.Contains(InfoMarker);
        // The outer fault A is a hardware fault, so its native signal record and thread
        // context must still be surfaced even though a nested fault B was handled while A
        // was in flight. NativeAOT provides both on every platform.
        bool infoPopulated = stderr.Contains("info=true");
        bool contextPopulated = stderr.Contains("context=true");
        bool exited = exitCode != 0;

        Console.WriteLine($"  Exit code: 0x{exitCode:X8}, handler invoked: {handlerInvoked}, info ok: {infoPopulated}, context ok: {contextPopulated}, exited: {exited}");
        if (!handlerInvoked)
            Console.WriteLine("  FAIL: Handler was not invoked");
        if (!infoReceived)
            Console.WriteLine("  FAIL: Handler did not report native exception properties");
        if (!infoPopulated)
            Console.WriteLine("  FAIL: exception record was lost for the outer fault after a nested fault was handled");
        if (!contextPopulated)
            Console.WriteLine("  FAIL: thread context was lost for the outer fault after a nested fault was handled");
        if (!exited)
            Console.WriteLine("  FAIL: Expected non-zero exit code");

        return handlerInvoked && infoReceived && infoPopulated && contextPopulated && exited;
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
                case "native-exception":        RunChildNativeException();         return 1;
                case "nested-native-exception": RunChildNestedNativeException();   return 1;
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
        allPassed &= TestNativeException("native-exception");
        allPassed &= TestNestedHardwareFault();
        allPassed &= TestSetNull();
        allPassed &= TestSetTwice();

        Console.WriteLine();
        Console.WriteLine(allPassed ? "ALL TESTS PASSED" : "SOME TESTS FAILED");

        return allPassed ? 100 : 1;
    }
}
