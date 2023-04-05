// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading
{
    public abstract partial class WaitHandle
    {
        internal static unsafe int WaitMultipleIgnoringSyncContext(Span<IntPtr> handles, bool waitAll, int millisecondsTimeout) =>
            ThreadPool.UseWindowsThreadPool ?
            WaitMultipleIgnoringSyncContextCore(handles, waitAll, millisecondsTimeout) :
            WaitMultipleIgnoringSyncContextPortableCore(handles, waitAll, millisecondsTimeout);

        internal static unsafe int WaitOneCore(IntPtr handle, int millisecondsTimeout) =>
            ThreadPool.UseWindowsThreadPool ?
            WaitOneCoreCore(handle, millisecondsTimeout) :
            WaitOnePortableCore(handle, millisecondsTimeout);

        internal static Exception ExceptionFromCreationError(int errorCode, string path) =>
            ExceptionFromCreationErrorCore(errorCode, path);

        private static int SignalAndWaitCore(IntPtr handleToSignal, IntPtr handleToWaitOn, int millisecondsTimeout) =>
            ThreadPool.UseWindowsThreadPool ?
            SignalAndWaitCoreCore(handleToSignal, handleToWaitOn, millisecondsTimeout) :
            SignalAndWaitPortableCore(handleToSignal, handleToWaitOn, millisecondsTimeout);
    }
}
