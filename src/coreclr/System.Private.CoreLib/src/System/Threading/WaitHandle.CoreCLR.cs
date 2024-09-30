// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading
{
    public abstract partial class WaitHandle
    {
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "WaitHandle_WaitOneCore")]
        private static partial int WaitOneCore(IntPtr waitHandle, int millisecondsTimeout, [MarshalAs(UnmanagedType.Bool)] bool useTrivialWaits);

        private static unsafe int WaitMultipleIgnoringSyncContextCore(ReadOnlySpan<IntPtr> waitHandles, bool waitAll, int millisecondsTimeout)
            => WaitMultipleIgnoringSyncContext(waitHandles, waitHandles.Length, waitAll, millisecondsTimeout);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "WaitHandle_WaitMultipleIgnoringSyncContext")]
        private static partial int WaitMultipleIgnoringSyncContext(ReadOnlySpan<IntPtr> waitHandles, int numHandles, [MarshalAs(UnmanagedType.Bool)] bool waitAll, int millisecondsTimeout);

        private static int SignalAndWaitCore(IntPtr waitHandleToSignal, IntPtr waitHandleToWaitOn, int millisecondsTimeout)
        {
            int ret = SignalAndWait(waitHandleToSignal, waitHandleToWaitOn, millisecondsTimeout);

            if (ret == Interop.Errors.ERROR_TOO_MANY_POSTS)
            {
                throw new InvalidOperationException(SR.Threading_WaitHandleTooManyPosts);
            }

            return ret;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "WaitHandle_SignalAndWait")]
        private static partial int SignalAndWait(IntPtr waitHandleToSignal, IntPtr waitHandleToWaitOn, int millisecondsTimeout);
    }
}
