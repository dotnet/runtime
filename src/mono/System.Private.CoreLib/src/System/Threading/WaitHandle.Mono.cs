// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading
{
    public partial class WaitHandle
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern unsafe int Wait_internal(IntPtr* handles, int numHandles, bool waitAll, int ms);

        private static int WaitOneCore(IntPtr waitHandle, int millisecondsTimeout)
        {
            unsafe
            {
                return Wait_internal(&waitHandle, 1, false, millisecondsTimeout);
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int SignalAndWait_Internal(IntPtr waitHandleToSignal, IntPtr waitHandleToWaitOn, int millisecondsTimeout);

        private const int ERROR_TOO_MANY_POSTS = 0x12A;
        private const int ERROR_NOT_OWNED_BY_CALLER = 0x12B;

        private static int SignalAndWaitCore(IntPtr waitHandleToSignal, IntPtr waitHandleToWaitOn, int millisecondsTimeout)
        {
            int ret = SignalAndWait_Internal(waitHandleToSignal, waitHandleToWaitOn, millisecondsTimeout);
            if (ret == ERROR_TOO_MANY_POSTS)
                throw new InvalidOperationException(SR.Threading_WaitHandleTooManyPosts);
            if (ret == ERROR_NOT_OWNED_BY_CALLER)
                throw new ApplicationException("Attempt to release mutex not owned by caller");
            return ret;
        }

        internal static int WaitMultipleIgnoringSyncContext(Span<IntPtr> waitHandles, bool waitAll, int millisecondsTimeout)
        {
            unsafe
            {
                fixed (IntPtr* handles = &MemoryMarshal.GetReference(waitHandles))
                {
                    return Wait_internal(handles, waitHandles.Length, waitAll, millisecondsTimeout);
                }
            }
        }

        // FIXME: Move to shared
        internal static int WaitAny(ReadOnlySpan<WaitHandle> waitHandles, int millisecondsTimeout) =>
            WaitMultiple(waitHandles, false, millisecondsTimeout);
    }
}
