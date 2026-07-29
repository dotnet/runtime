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
    const string AddressMarker = "FATAL_ADDRESS:";
    const string DiagUnsupportedMarker = "FATAL_DIAG:unsupported";
    const string DiagNegOkMarker = "FATAL_DIAG_NEG:ok";
    const string DiagJsonOkMarker = "FATAL_DIAG_JSON:ok";
    const string DiagLogOkMarker = "FATAL_DIAG_LOG:ok";
    const string FaultCodeMarker = "FATAL_FAULTCODE:";

    // Win32 exception codes (produced by CONTEXTGetExceptionCodeForSignal) surfaced to
    // the handler on the fatal-signal path. A native access violation maps to
    // EXCEPTION_ACCESS_VIOLATION; SIGABRT has no dedicated mapping and falls back to
    // EXCEPTION_ILLEGAL_INSTRUCTION.
    const string AccessViolationFaultCode = "0xC0000005";
    const string IllegalInstructionFaultCode = "0xC000001D";

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

    [DllImport("FatalErrorHandlerNative")]
    private static extern delegate* unmanaged<int, void*, int> GetHandlerCheckNativeInfo();

    [DllImport("FatalErrorHandlerNative")]
    private static extern delegate* unmanaged<int, void*, int> GetHandlerCheckDiagnosticData();

    [DllImport("FatalErrorHandlerNative")]
    private static extern delegate* unmanaged<int, void*, int> GetHandlerCheckSignalInfo();

    [DllImport("FatalErrorHandlerNative")]
    private static extern void TriggerNativeAccessViolation();

    [DllImport("FatalErrorHandlerNative")]
    private static extern void TriggerNativeAbort();

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
        // corrupted-state exception that reaches the handler with the faulting
        // instruction pointer (IP) populated.
        *(int*)s_badAddress = 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void RunChildNativeCodeException()
    {
        ExceptionHandling.SetFatalErrorHandler(GetHandlerCheckNativeInfo());
        // Trigger an access violation from *native* code (inside the P/Invoked
        // TriggerNativeAccessViolation).
        TriggerNativeAccessViolation();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void RunChildNativeSignalAccessViolation()
    {
        ExceptionHandling.SetFatalErrorHandler(GetHandlerCheckSignalInfo());
        // A genuinely-unmanaged access violation. On CoreCLR/Linux this reaches the
        // handler through the PAL fatal-signal path as SIGSEGV,
        // not through the managed fatal path.
        TriggerNativeAccessViolation();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void RunChildNativeSignalAbort()
    {
        ExceptionHandling.SetFatalErrorHandler(GetHandlerCheckSignalInfo());
        // A native abort() raises SIGABRT, which reaches the handler through the PAL
        // fatal-signal path on Unix without flowing through the managed fatal path.
        TriggerNativeAbort();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void RunChildDiagnosticData()
    {
        ExceptionHandling.SetFatalErrorHandler(GetHandlerCheckDiagnosticData());
        Environment.FailFast("test fatal error");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void RunChildDiagnosticDataAccessViolation()
    {
        ExceptionHandling.SetFatalErrorHandler(GetHandlerCheckDiagnosticData());
        // Reach the handler through the managed access-violation (corrupted-state) fatal
        // path instead of FailFast, so the on-demand report is generated while the runtime
        // is in the fatal-hardware-exception state rather than a clean FailFast.
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
        // prevent the still-in-flight outer fault A from reaching the fatal error
        // handler with its own faulting instruction pointer.
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
            // Swallow B. Handling a nested hardware fault must not disrupt the
            // still-in-flight outer fault's path to the fatal error handler.
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
        // SkipDefaultHandler suppresses the runtime's crash log output, but the
        // process still terminates.
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

        bool handlerInvoked = stderr.Contains(HandlerInvokedMarker);
        bool addressReported = stderr.Contains(AddressMarker);

        // The managed fatal path surfaces only the faulting instruction pointer (IP).
        // It is available on every platform and both runtimes.
        bool addressPopulated = stderr.Contains("addr=true");
        bool exited = exitCode != 0;

        Console.WriteLine($"  Exit code: 0x{exitCode:X8}, handler invoked: {handlerInvoked}, address ok: {addressPopulated}, exited: {exited}");
        if (!handlerInvoked)
            Console.WriteLine("  FAIL: Handler was not invoked");
        if (!addressReported)
            Console.WriteLine("  FAIL: Handler did not report the crash address");
        if (!addressPopulated)
            Console.WriteLine("  FAIL: crash address (IP) was not populated for a native exception");
        if (!exited)
            Console.WriteLine("  FAIL: Expected non-zero exit code");

        return handlerInvoked && addressReported && addressPopulated && exited;
    }

    static bool TestNativeCodeException()
    {
        Console.WriteLine("=== TestNativeCodeException ===");

        // A genuinely unmanaged fatal fault (an access violation whose faulting
        // instruction pointer is inside native code). It reaches the fatal error handler
        // through the native fatal-signal/SEH path: NativeAOT routes it on all platforms,
        // CoreCLR routes it on Windows (SEH) and on Linux/other signal-based Unix.
        // CoreCLR/macOS is the one gap: SIGSEGV is delivered as a Mach exception there,
        // bypassing the signal path.
        if (!TestLibrary.Utilities.IsNativeAot && OperatingSystem.IsMacOS())
        {
            Console.WriteLine("  SKIP: CoreCLR/macOS delivers SIGSEGV via Mach exceptions, bypassing the fatal-signal path");
            return true;
        }

        var (exitCode, stderr) = LaunchChild("native-code-exception");

        bool handlerInvoked = stderr.Contains(HandlerInvokedMarker);
        // For the unmanaged fatal path the runtime forwards the live platform-native fault
        // structures.
        bool addressPopulated = stderr.Contains("addr=true");
        bool exited = exitCode != 0;

        bool firstStructPopulated;
        bool secondStructPopulated;
        string firstStructName;
        string secondStructName;
        if (OperatingSystem.IsWindows())
        {
            firstStructPopulated = stderr.Contains("excrec=true");
            secondStructPopulated = stderr.Contains("ctxrec=true");
            firstStructName = "EXCEPTION_RECORD";
            secondStructName = "CONTEXT";
        }
        else
        {
            firstStructPopulated = stderr.Contains("siginfo=true");
            secondStructPopulated = stderr.Contains("ucontext=true");
            firstStructName = "siginfo_t";
            secondStructName = "ucontext_t";
        }

        Console.WriteLine($"  Exit code: 0x{exitCode:X8}, handler invoked: {handlerInvoked}, address ok: {addressPopulated}, {firstStructName} ok: {firstStructPopulated}, {secondStructName} ok: {secondStructPopulated}, exited: {exited}");
        if (!handlerInvoked)
            Console.WriteLine("  FAIL: Handler was not invoked");
        if (!addressPopulated)
            Console.WriteLine("  FAIL: crash address (IP) was not populated for a native-code exception");
        if (!firstStructPopulated)
            Console.WriteLine($"  FAIL: {firstStructName} was not surfaced for a native-code exception");
        if (!secondStructPopulated)
            Console.WriteLine($"  FAIL: {secondStructName} was not surfaced for a native-code exception");
        if (!exited)
            Console.WriteLine("  FAIL: Expected non-zero exit code");

        return handlerInvoked && addressPopulated && firstStructPopulated && secondStructPopulated && exited;
    }

    static bool TestDiagnosticData(string scenario, string crashDescription)
    {
        Console.WriteLine($"=== TestDiagnosticData ({crashDescription}) ===");

        // The in-proc crash reporter that backs FEP_DiagnosticDataFunc is compiled for
        // CoreCLR on desktop Linux and macOS (and mobile). It is not present for CoreCLR on
        // Windows or other Unix platforms, nor for NativeAOT, where the property is reported
        // as unavailable. Mirror the clrfeatures.cmake gate rather than "any non-Windows".
        bool expectSupported = !TestLibrary.Utilities.IsNativeAot &&
                               (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS());

        var (exitCode, stderr) = LaunchChild(scenario);

        bool handlerInvoked = stderr.Contains(HandlerInvokedMarker);
        bool reportedUnsupported = stderr.Contains(DiagUnsupportedMarker);
        bool exited = exitCode != 0;

        Console.WriteLine($"  Exit code: 0x{exitCode:X8}, handler invoked: {handlerInvoked}, expect supported: {expectSupported}, reported unsupported: {reportedUnsupported}, exited: {exited}");
        if (!handlerInvoked)
            Console.WriteLine("  FAIL: Handler was not invoked");
        if (!exited)
            Console.WriteLine("  FAIL: Expected non-zero exit code");

        if (!expectSupported)
        {
            // Where the reporter is not compiled the property must be reported as
            // unavailable rather than misbehave.
            if (!reportedUnsupported)
                Console.WriteLine("  FAIL: FEP_DiagnosticDataFunc should be unavailable on this platform");
            return handlerInvoked && exited && reportedUnsupported;
        }

        bool negOk = stderr.Contains(DiagNegOkMarker);
        bool jsonOk = stderr.Contains(DiagJsonOkMarker);
        bool logOk = stderr.Contains(DiagLogOkMarker);

        if (reportedUnsupported)
            Console.WriteLine("  FAIL: FEP_DiagnosticDataFunc was unavailable on a platform expected to support it");
        if (!negOk)
            Console.WriteLine("  FAIL: malformed on-demand requests were not rejected as documented");
        if (!jsonOk)
            Console.WriteLine("  FAIL: JSON crash report was not produced on demand");
        if (!logOk)
            Console.WriteLine("  FAIL: Log crash report was not produced on demand");

        return handlerInvoked && exited && !reportedUnsupported && negOk && jsonOk && logOk;
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
        bool addressReported = stderr.Contains(AddressMarker);

        // Even though a nested hardware fault (B) was handled while the outer fault (A)
        // was still in flight, A must still reach the fatal error handler with a valid
        // faulting instruction pointer.
        bool addressPopulated = stderr.Contains("addr=true");
        bool exited = exitCode != 0;

        Console.WriteLine($"  Exit code: 0x{exitCode:X8}, handler invoked: {handlerInvoked}, address ok: {addressPopulated}, exited: {exited}");
        if (!handlerInvoked)
            Console.WriteLine("  FAIL: Handler was not invoked");
        if (!addressReported)
            Console.WriteLine("  FAIL: Handler did not report the crash address");
        if (!addressPopulated)
            Console.WriteLine("  FAIL: crash address (IP) was not populated for the outer fault after a nested fault was handled");
        if (!exited)
            Console.WriteLine("  FAIL: Expected non-zero exit code");

        return handlerInvoked && addressReported && addressPopulated && exited;
    }

    // Verifies that a genuine native crash reaches the user handler through the native
    // fatal-signal path on Unix, carrying the POSIX crash context (fault code, siginfo_t,
    // ucontext_t, and previous struct sigaction). Both CoreCLR and NativeAOT route
    // hardware faults and surface the same properties, but they hook different signals:
    // CoreCLR handles SIGSEGV/SIGILL/SIGFPE/SIGBUS/SIGABRT, while NativeAOT hooks only
    // SIGSEGV/SIGFPE. So the SIGSEGV (access violation) scenario runs on both runtimes,
    // whereas the abort()/SIGABRT scenario is CoreCLR-only. Windows uses SEH, not signals.
    // Hardware faults (SIGSEGV) are delivered as Mach exceptions on macOS.
    static bool TestNativeSignal(string scenario, string expectedFaultCode, bool linuxOnly, bool runsOnNativeAot)
    {
        Console.WriteLine($"=== TestNativeSignal ({scenario}) ===");

        bool isNativeAot = TestLibrary.Utilities.IsNativeAot;
        bool applicable = !OperatingSystem.IsWindows() &&
                          (runsOnNativeAot || !isNativeAot) &&
                          (!linuxOnly || OperatingSystem.IsLinux());
        if (!applicable)
        {
            string platforms = linuxOnly ? "Linux" : "Unix";
            string runtimes = runsOnNativeAot ? "CoreCLR/NativeAOT" : "CoreCLR";
            Console.WriteLine($"  SKIP: fatal-signal-path handler is only wired for {runtimes} on {platforms}");
            return true;
        }

        var (exitCode, stderr) = LaunchChild(scenario);

        bool handlerInvoked = stderr.Contains(HandlerInvokedMarker);
        bool faultCodeReported = stderr.Contains($"{FaultCodeMarker}{expectedFaultCode}");
        bool sigInfoOk = stderr.Contains("siginfo=true");
        bool ucontextOk = stderr.Contains("ucontext=true");
        bool prevActionOk = stderr.Contains("prevaction=true");
        bool exited = exitCode != 0;

        Console.WriteLine($"  Exit code: 0x{exitCode:X8}, handler invoked: {handlerInvoked}, fault code {expectedFaultCode} reported: {faultCodeReported}, siginfo ok: {sigInfoOk}, ucontext ok: {ucontextOk}, prevaction ok: {prevActionOk}, exited: {exited}");
        if (!handlerInvoked)
            Console.WriteLine("  FAIL: Handler was not invoked from the fatal-signal path");
        if (!faultCodeReported)
            Console.WriteLine($"  FAIL: Handler did not receive the expected fault code ({expectedFaultCode})");
        if (!sigInfoOk)
            Console.WriteLine("  FAIL: siginfo_t was not surfaced on the fatal-signal path");
        if (!ucontextOk)
            Console.WriteLine("  FAIL: ucontext_t was not surfaced on the fatal-signal path");
        if (!prevActionOk)
            Console.WriteLine("  FAIL: previous struct sigaction was not surfaced on the fatal-signal path");
        if (!exited)
            Console.WriteLine("  FAIL: Expected non-zero exit code");

        return handlerInvoked && faultCodeReported && sigInfoOk && ucontextOk && prevActionOk && exited;
    }

    static bool TestFailFastInvokesHandlerOnce()
    {
        Console.WriteLine("=== TestFailFastInvokesHandlerOnce ===");

        // FailFast with a RunDefaultHandler handler lets the runtime proceed with its
        // default fatal handling, which on Unix terminates the process via abort() ->
        // SIGABRT. That re-enters the PAL fatal-signal path, which must NOT invoke the
        // user handler a second time for the same fatal event: the managed fatal path
        // latches a one-shot flag before it aborts. Verify the handler ran exactly
        // once.
        var (exitCode, stderr) = LaunchChild("run-handler");

        int invocationCount = CountOccurrences(stderr, HandlerInvokedMarker);
        bool exited = exitCode != 0;

        Console.WriteLine($"  Exit code: 0x{exitCode:X8}, handler invocation count: {invocationCount}, exited: {exited}");
        if (invocationCount != 1)
            Console.WriteLine($"  FAIL: Expected the handler to be invoked exactly once, but it ran {invocationCount} time(s)");
        if (!exited)
            Console.WriteLine("  FAIL: Expected non-zero exit code");

        return invocationCount == 1 && exited;
    }

    static int CountOccurrences(string haystack, string needle)
    {
        int count = 0;
        int index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
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
                case "native-code-exception":   RunChildNativeCodeException();      return 1;
                case "native-signal-av":        RunChildNativeSignalAccessViolation(); return 1;
                case "native-signal-abort":     RunChildNativeSignalAbort();       return 1;
                case "diagnostic-data":         RunChildDiagnosticData();          return 1;
                case "diagnostic-data-av":      RunChildDiagnosticDataAccessViolation(); return 1;
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
        allPassed &= TestNativeCodeException();
        allPassed &= TestNativeSignal("native-signal-av", AccessViolationFaultCode, linuxOnly: true, runsOnNativeAot: true);
        allPassed &= TestNativeSignal("native-signal-abort", IllegalInstructionFaultCode, linuxOnly: false, runsOnNativeAot: false);
        allPassed &= TestDiagnosticData("diagnostic-data", "FailFast");
        allPassed &= TestDiagnosticData("diagnostic-data-av", "managed access violation");
        allPassed &= TestNestedHardwareFault();
        allPassed &= TestSetNull();
        allPassed &= TestSetTwice();
        allPassed &= TestFailFastInvokesHandlerOnce();

        Console.WriteLine();
        Console.WriteLine(allPassed ? "ALL TESTS PASSED" : "SOME TESTS FAILED");

        return allPassed ? 100 : 1;
    }
}
