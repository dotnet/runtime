// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    public abstract partial class WaitHandle
    {
#if FEATURE_SINGLE_THREADED
        private static int WaitOneCore(IntPtr handle, int millisecondsTimeout, bool useTrivialWaits) =>
            throw new PlatformNotSupportedException();

        private static int WaitMultipleIgnoringSyncContextCore(ReadOnlySpan<IntPtr> handles, bool waitAll, int millisecondsTimeout) =>
            throw new PlatformNotSupportedException();

        private static int SignalAndWaitCore(IntPtr handleToSignal, IntPtr handleToWaitOn, int millisecondsTimeout) =>
            throw new PlatformNotSupportedException();
#else
        private static int WaitOneCore(IntPtr handle, int millisecondsTimeout, bool useTrivialWaits) =>
            WaitSubsystem.Wait(handle, millisecondsTimeout, interruptible: !useTrivialWaits);

        private static int WaitMultipleIgnoringSyncContextCore(ReadOnlySpan<IntPtr> handles, bool waitAll, int millisecondsTimeout) =>
            WaitSubsystem.Wait(handles, waitAll, millisecondsTimeout);

        private static int SignalAndWaitCore(IntPtr handleToSignal, IntPtr handleToWaitOn, int millisecondsTimeout) =>
            WaitSubsystem.SignalAndWait(handleToSignal, handleToWaitOn, millisecondsTimeout);
#endif
    }
}
