// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading
{
    public abstract partial class WaitHandle
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int WaitOneCore(nint waitHandle, int millisecondsTimeout);

        internal static unsafe int WaitMultipleIgnoringSyncContext(Span<nint> waitHandles, bool waitAll, int millisecondsTimeout)
        {
            fixed (nint* pWaitHandles = &MemoryMarshal.GetReference(waitHandles))
            {
                return WaitMultipleIgnoringSyncContext(pWaitHandles, waitHandles.Length, waitAll, millisecondsTimeout);
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe int WaitMultipleIgnoringSyncContext(nint* waitHandles, int numHandles, bool waitAll, int millisecondsTimeout);

        private static int SignalAndWaitCore(nint waitHandleToSignal, nint waitHandleToWaitOn, int millisecondsTimeout)
        {
            int ret = SignalAndWaitNative(waitHandleToSignal, waitHandleToWaitOn, millisecondsTimeout);

            if (ret == Interop.Errors.ERROR_TOO_MANY_POSTS)
            {
                throw new InvalidOperationException(SR.Threading_WaitHandleTooManyPosts);
            }

            return ret;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int SignalAndWaitNative(nint waitHandleToSignal, nint waitHandleToWaitOn, int millisecondsTimeout);
    }
}
