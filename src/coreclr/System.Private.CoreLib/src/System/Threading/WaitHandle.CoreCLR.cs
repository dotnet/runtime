// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading
{
    public abstract partial class WaitHandle
    {
        private static int WaitOneCore(IntPtr waitHandle, int millisecondsTimeout) => WaitOnePortableCore(waitHandle, millisecondsTimeout);

        internal static unsafe int WaitMultipleIgnoringSyncContext(Span<IntPtr> waitHandles, bool waitAll, int millisecondsTimeout) =>
            WaitMultipleIgnoringSyncContextPortableCore(waitHandles, waitAll, millisecondsTimeout);

        private static int SignalAndWaitCore(IntPtr waitHandleToSignal, IntPtr waitHandleToWaitOn, int millisecondsTimeout) =>
            SignalAndWaitPortableCore(waitHandleToSignal, waitHandleToWaitOn, millisecondsTimeout);

        private static int SignalAndWaitNative(IntPtr waitHandleToSignal, IntPtr waitHandleToWaitOn, int millisecondsTimeout) =>
            SignalAndWaitNativePortableCore(waitHandleToSignal, waitHandleToWaitOn, millisecondsTimeout);
    }
}
