// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Threading;

[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
public class CallbackStressTest
{
    static int s_LoopCounter = 10;
    static int s_FinallyCalled = 0;
    static int s_CatchCalled = 0;
    static int s_OtherExceptionCatchCalled = 0;
    static int s_SEHExceptionCatchCalled = 0;
    static int s_WrongPInvokesExecuted = 0;
    static int s_PInvokesExecuted = 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SetResolve()
    {
        Console.WriteLine("Setting PInvoke Resolver");

        DllImportResolver resolver =
            (string libraryName, Assembly asm, DllImportSearchPath? dllImportSearchPath) =>
            {
                if (string.Equals(libraryName, NativeLibraryToLoad.InvalidName))
                {
                    if (dllImportSearchPath != DllImportSearchPath.System32)
                    {
                        Console.WriteLine($"Unexpected dllImportSearchPath: {dllImportSearchPath.ToString()}");
                        throw new ArgumentException();
                    }

                    return NativeLibrary.Load(NativeLibraryToLoad.Name, asm, null);
                }

                return IntPtr.Zero;
            };

        NativeLibrary.SetDllImportResolver(
            Assembly.GetExecutingAssembly(),
            resolver);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void DoCall()
    {
        NativeSum(10, 10);
        s_WrongPInvokesExecuted++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void DoCallTryCatch(bool shouldThrow)
    {
        try
        {
            var a = NativeSum(10, 10);
            if (shouldThrow)
                s_WrongPInvokesExecuted++;
            else
                s_PInvokesExecuted += (a == 20 ? 1 : 0);
        }
        catch (DllNotFoundException) { s_CatchCalled++; }

        throw new ArgumentException();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void DoCallTryRethrowInCatch()
    {
        try
        {
            var a = NativeSum(10, 10);
            s_WrongPInvokesExecuted++;
        }
        catch (DllNotFoundException) { s_CatchCalled++; throw; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void DoCallTryRethrowDifferentExceptionInCatch()
    {
        try
        {
            var a = NativeSum(10, 10);
            s_WrongPInvokesExecuted++;
        }
        catch (DllNotFoundException) { s_CatchCalled++; throw new InvalidOperationException(); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void DoCallTryFinally()
    {
        try
        {
            NativeSum(10, 10);
            s_WrongPInvokesExecuted++;
        }
        finally { s_FinallyCalled++; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ManualRaiseException()
    {
#if WINDOWS
        if (!TestLibrary.Utilities.IsMonoRuntime)
        {
            try
            {
                RaiseException(5, 0, 0, IntPtr.Zero);
            }
            catch(SEHException ex) { GC.Collect(); s_SEHExceptionCatchCalled++; }
        }
        else
        {
            // SEH exception handling not supported on Mono.
            s_SEHExceptionCatchCalled++;
        }
#else
        // TODO: test on Unix when implementing pinvoke inlining
        s_SEHExceptionCatchCalled++;
#endif
    }

    public static int Main()
    {
        for(int i = 0; i < s_LoopCounter; i++)
        {
            try
            {
                NativeSum(10, 10);
                s_WrongPInvokesExecuted++;
            }
            catch (DllNotFoundException) { GC.Collect(); s_CatchCalled++; }

            try { DoCall(); }
            catch (DllNotFoundException) { GC.Collect(); s_CatchCalled++; }

            try { DoCallTryFinally(); }
            catch (DllNotFoundException) { GC.Collect(); s_CatchCalled++; }

            try { DoCallTryCatch(true); }
            catch (ArgumentException) { GC.Collect(); s_OtherExceptionCatchCalled++; }

            try { DoCallTryRethrowInCatch(); }
            catch (DllNotFoundException) { GC.Collect(); s_CatchCalled++; }

            try { DoCallTryRethrowDifferentExceptionInCatch(); }
            catch (InvalidOperationException) { GC.Collect(); s_OtherExceptionCatchCalled++; }

            ManualRaiseException();
        }

        SetResolve();

        for(int i = 0; i < s_LoopCounter; i++)
        {
            var a = NativeSum(10, 10);
            var b = NativeSum(10, 10);
            s_PInvokesExecuted += (a == b && a == 20)? 2 : 0;

            try { DoCallTryCatch(false); }
            catch (ArgumentException) { GC.Collect(); s_OtherExceptionCatchCalled++; }

            ManualRaiseException();
        }

        if (s_FinallyCalled == s_LoopCounter &&
            s_CatchCalled == (s_LoopCounter * 7) &&
            s_OtherExceptionCatchCalled == (s_LoopCounter * 3) &&
            s_WrongPInvokesExecuted == 0 &&
            s_PInvokesExecuted == (s_LoopCounter * 3) &&
            s_SEHExceptionCatchCalled == (s_LoopCounter * 2))
        {
            Console.WriteLine("PASS");
            return 100;
        }

        Console.WriteLine("s_FinallyCalled = " + s_FinallyCalled);
        Console.WriteLine("s_CatchCalled = " + s_CatchCalled);
        Console.WriteLine("s_OtherExceptionCatchCalled = " + s_OtherExceptionCatchCalled);
        Console.WriteLine("s_SEHExceptionCatchCalled = " + s_SEHExceptionCatchCalled);
        Console.WriteLine("s_WrongPInvokesExecuted = " + s_WrongPInvokesExecuted);
        Console.WriteLine("s_PInvokesExecuted = " + s_PInvokesExecuted);
        return -1;
    }

    [DllImport(NativeLibraryToLoad.InvalidName)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    static extern int NativeSum(int arg1, int arg2);

#if WINDOWS
    [DllImport("kernel32")]
    static extern void RaiseException(uint dwExceptionCode, uint dwExceptionFlags, uint nNumberOfArguments, IntPtr lpArguments);
#endif
}
