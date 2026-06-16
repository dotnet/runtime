// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

unsafe class FatalErrorHandlerTest
{
    // Marker string written to stderr by the handler to prove it was invoked.
    const string HandlerInvokedMarker = "FATAL_HANDLER_INVOKED";

    // Marker string written to stderr by the pfnGetFatalErrorLog callback.
    const string LogReceivedMarker = "FATAL_LOG_RECEIVED:";

    // Mirror of the native FatalErrorInfo struct from FatalErrorHandling.h.
    [StructLayout(LayoutKind.Sequential)]
    struct FatalErrorInfo
    {
        public nuint Size;
        public void* Address;
        public void* Info;
        public void* Context;
        public delegate* unmanaged<FatalErrorInfo*, delegate* unmanaged<byte*, void*, void>, void*, void> PfnGetFatalErrorLog;
    }

    //
    // Handlers — each scenario gets its own [UnmanagedCallersOnly] handler.
    //

    [UnmanagedCallersOnly]
    static int HandlerSkipDefault(int hresult, void* pErrorInfo)
    {
        WriteToStdErr(HandlerInvokedMarker);
        return 1; // SkipDefaultHandler
    }

    [UnmanagedCallersOnly]
    static int HandlerRunDefault(int hresult, void* pErrorInfo)
    {
        WriteToStdErr(HandlerInvokedMarker);
        return 0; // RunDefaultHandler
    }

    [UnmanagedCallersOnly]
    static int HandlerWithLog(int hresult, void* pErrorInfo)
    {
        WriteToStdErr(HandlerInvokedMarker);

        FatalErrorInfo* info = (FatalErrorInfo*)pErrorInfo;
        if (info->PfnGetFatalErrorLog != null)
        {
            info->PfnGetFatalErrorLog(info, &LogAction, null);
        }

        return 1; // SkipDefaultHandler
    }

    [UnmanagedCallersOnly]
    static void LogAction(byte* logString, void* userContext)
    {
        string? log = Utf8StringMarshaller.ConvertToManaged(logString);
        WriteToStdErr(LogReceivedMarker + log);
    }

    //
    // Helper to write to stderr from unmanaged context.
    // Console.Error may not be safe during a fatal error, so
    // use low-level interop.
    //
    static void WriteToStdErr(string message)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(message + "\n");
        fixed (byte* p = utf8)
        {
#if TARGET_WINDOWS
            IntPtr hStdErr = GetStdHandle(STD_ERROR_HANDLE);
            WriteFile(hStdErr, p, (uint)utf8.Length, out _, IntPtr.Zero);
#else
            write(2, p, (nuint)utf8.Length);
#endif
        }
    }

#if TARGET_WINDOWS
    private const int STD_ERROR_HANDLE = -12;

    [DllImport("kernel32", ExactSpelling = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32", ExactSpelling = true)]
    private static extern bool WriteFile(IntPtr hFile, byte* lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, IntPtr lpOverlapped);
#else
    [DllImport("libc", ExactSpelling = true)]
    private static extern nint write(int fd, byte* buf, nuint count);
#endif

    //
    // Child process entry points — register handler, trigger FailFast.
    //

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void RunChildSkipHandler()
    {
        ExceptionHandling.SetFatalErrorHandler(&HandlerSkipDefault);
        Environment.FailFast("test fatal error");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void RunChildRunHandler()
    {
        ExceptionHandling.SetFatalErrorHandler(&HandlerRunDefault);
        Environment.FailFast("test fatal error");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void RunChildLogHandler()
    {
        ExceptionHandling.SetFatalErrorHandler(&HandlerWithLog);
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
        ExceptionHandling.SetFatalErrorHandler(&HandlerSkipDefault);
        try
        {
            ExceptionHandling.SetFatalErrorHandler(&HandlerRunDefault);
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
        StringBuilder stderrBuilder = new();

        Process child = new();
        child.StartInfo.FileName = Environment.ProcessPath;
        child.StartInfo.Arguments = scenario;
        child.StartInfo.RedirectStandardError = true;
        child.StartInfo.Environment.Remove("DOTNET_DbgEnableMiniDump");
        child.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                stderrBuilder.AppendLine(e.Data);
        };

        child.Start();
        child.BeginErrorReadLine();
        child.WaitForExit();
        child.CancelErrorRead();

        return (child.ExitCode, stderrBuilder.ToString());
    }

    static bool TestSkipHandler()
    {
        Console.WriteLine("=== TestSkipHandler ===");
        var (exitCode, stderr) = LaunchChild("skip-handler");

        bool handlerInvoked = stderr.Contains(HandlerInvokedMarker);
        // SkipDefaultHandler should result in a clean exit, not a crash code.
        bool exitedCleanly = exitCode != 0 && !IsCrashExitCode(exitCode);

        Console.WriteLine($"  Exit code: 0x{exitCode:X8}, handler invoked: {handlerInvoked}, exited cleanly: {exitedCleanly}");
        if (!handlerInvoked)
            Console.WriteLine("  FAIL: Handler was not invoked");
        if (!exitedCleanly)
            Console.WriteLine($"  FAIL: Expected non-crash exit code, got 0x{exitCode:X8}");

        return handlerInvoked && exitedCleanly;
    }

    static bool TestRunHandler()
    {
        Console.WriteLine("=== TestRunHandler ===");
        var (exitCode, stderr) = LaunchChild("run-handler");

        bool handlerInvoked = stderr.Contains(HandlerInvokedMarker);
        bool crashed = IsCrashExitCode(exitCode);

        Console.WriteLine($"  Exit code: 0x{exitCode:X8}, handler invoked: {handlerInvoked}, crashed: {crashed}");
        if (!handlerInvoked)
            Console.WriteLine("  FAIL: Handler was not invoked");
        if (!crashed)
            Console.WriteLine($"  FAIL: Expected crash exit code, got 0x{exitCode:X8}");

        return handlerInvoked && crashed;
    }

    static bool TestLogHandler()
    {
        Console.WriteLine("=== TestLogHandler ===");
        var (exitCode, stderr) = LaunchChild("log-handler");

        bool handlerInvoked = stderr.Contains(HandlerInvokedMarker);
        bool logReceived = stderr.Contains(LogReceivedMarker);
        bool logContainsMessage = stderr.Contains("test fatal error");
        bool exitedCleanly = exitCode != 0 && !IsCrashExitCode(exitCode);

        Console.WriteLine($"  Exit code: 0x{exitCode:X8}, handler invoked: {handlerInvoked}, log received: {logReceived}, log has message: {logContainsMessage}");
        if (!handlerInvoked)
            Console.WriteLine("  FAIL: Handler was not invoked");
        if (!logReceived)
            Console.WriteLine("  FAIL: pfnGetFatalErrorLog callback was not invoked");
        if (!logContainsMessage)
            Console.WriteLine("  FAIL: Log did not contain the FailFast message");

        return handlerInvoked && logReceived && logContainsMessage && exitedCleanly;
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

    static bool IsCrashExitCode(int exitCode)
    {
        if (OperatingSystem.IsWindows())
        {
            // STATUS_STACK_BUFFER_OVERRUN (used by NativeAOT fast-fail)
            return exitCode == unchecked((int)0xC0000409);
        }
        else
        {
            // SIGABRT = 128 + 6 = 134
            return exitCode == 134;
        }
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
