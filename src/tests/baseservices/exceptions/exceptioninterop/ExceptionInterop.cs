// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;
using static ExceptionInteropNative;

internal unsafe static class ExceptionInteropNative
{
    [DllImport(nameof(ExceptionInteropNative))]
    public static extern void ThrowException();

    [DllImport(nameof(ExceptionInteropNative))]
    public static extern void NativeFunction();

    [DllImport(nameof(ExceptionInteropNative))]
    public static extern void CallCallback(delegate* unmanaged<void> cb);
}

public unsafe static class ExceptionInterop
{
    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    [SkipOnMono("Exception interop not supported on Mono.")]
    public static void ThrowNativeExceptionAndCatchInFrame()
    {
        bool caughtException = false;
        try
        {
            ThrowException();
        }
        catch
        {
            caughtException = true;
            // Try calling another P/Invoke in the catch block to make sure we have everything set up
            // to recover from the exceptional control flow.
            NativeFunction();
        }
        Assert.True(caughtException);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    [SkipOnMono("Exception interop not supported on Mono.")]
    public static void ThrowManagedExceptionThroughNativeAndCatchInFrame()
    {
        bool caughtException = false;
        try
        {
            CallCallback(&ThrowManagedException);
        }
        catch
        {
            caughtException = true;
            // Try calling another P/Invoke in the catch block to make sure we have everything set up
            // to recover from the exceptional control flow.
            NativeFunction();
        }
        Assert.True(caughtException);

        [UnmanagedCallersOnly]
        static void ThrowManagedException()
        {
            throw new Exception();
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    [SkipOnMono("Exception interop not supported on Mono.")]
    public static void ThrowNativeExceptionAndCatchInFrameWithFilter()
    {
        bool caughtException = false;
        try
        {
            ThrowException();
        }
        catch (Exception) when (Filter())
        {
            caughtException = true;
            // Try calling another P/Invoke in the catch block to make sure we have everything set up
            // to recover from the exceptional control flow.
            NativeFunction();
        }
        Assert.True(caughtException);

        // Aggressively inline to make sure the call to NativeFunction is in the filter clause
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool Filter()
        {
            NativeFunction();
            return true;
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    [SkipOnMono("Exception interop not supported on Mono.")]
    public static void ThrowNativeExceptionAndCatchInFrameWithFinally()
    {
        bool caughtException = false;
        try
        {
            try
            {
                ThrowException();
            }
            finally
            {
                // Try calling another P/Invoke in the finally block before the catch
                // to make sure we have everything set up
                // to recover from the exceptional control flow.
                NativeFunction();
            }
        }
        catch
        {
            caughtException = true;
        }

        Assert.True(caughtException);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    [SkipOnMono("Exception interop not supported on Mono.")]
    public static void ThrowNativeExceptionInFrameWithFinallyCatchInOuterFrame()
    {
        bool caughtException = false;
        try
        {
            ThrowInFrameWithFinally();
        }
        catch
        {
            caughtException = true;
        }

        Assert.True(caughtException);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ThrowInFrameWithFinally()
        {
            try
            {
                ThrowException();
            }
            finally
            {
                // Try calling another P/Invoke in the finally block before the catch
                // to make sure we have everything set up
                // to recover from the exceptional control flow.
                NativeFunction();
            }
        }
    }

    [DllImport(nameof(ExceptionInteropNative))]
    public static extern void InvokeCallbackCatchCallbackAndRethrow(delegate* unmanaged<void> callBack1, delegate* unmanaged<void> callBack2);

    [UnmanagedCallersOnly]
    static void CallPInvoke()
    {
        ThrowException();
    }

    [UnmanagedCallersOnly]
    static void ThrowAndCatchException()
    {
        try
        {
            throw new Exception("This one is handled");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Caught {ex}");
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    [SkipOnMono("Exception interop not supported on Mono.")]
    public static void PropagateAndRethrowCppException()
    {
        try
        {
            InvokeCallbackCatchCallbackAndRethrow(&CallPInvoke, &ThrowAndCatchException);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Caught {ex}");
        }
    }
    
    [DllImport(nameof(ExceptionInteropNative))]
    public static extern void InvokeCallbackOnNewThread(delegate*unmanaged<void> callBack);

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    [SkipOnMono("Exception interop not supported on Mono.")]
    public static void PropagateAndCatchCppException()
    {
        bool reportedUnhandledException = false;
        UnhandledExceptionEventHandler handler = (sender, e) =>
        {
            Console.WriteLine($"Exception reported as unhandled: {e.ExceptionObject}");
            reportedUnhandledException = true;
        };

        AppDomain.CurrentDomain.UnhandledException += handler;
        InvokeCallbackOnNewThread(&CallPInvoke);
        AppDomain.CurrentDomain.UnhandledException -= handler;
        Assert.False(reportedUnhandledException, "Exception should not be reported as unhandled");
    }
}
