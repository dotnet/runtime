// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    /// <summary>
    /// A LIFO semaphore implemented using the PAL's semaphore with uninterruptible waits.
    /// </summary>
    internal sealed partial class LowLevelLifoSemaphore : LowLevelLifoSemaphoreBase, IDisposable
    {
        private Semaphore? _semaphore;

        private void Create(int maximumSignalCount)
        {
            Debug.Assert(maximumSignalCount > 0);
            _semaphore = new Semaphore(0, maximumSignalCount);
        }

        public bool WaitCore(int timeoutMs)
        {
            Debug.Assert(_semaphore != null);
            Debug.Assert(timeoutMs >= -1);

            int waitResult = WaitNative(_semaphore!.SafeWaitHandle, timeoutMs);
            Debug.Assert(waitResult == WaitHandle.WaitSuccess || waitResult == WaitHandle.WaitTimeout);
            return waitResult == WaitHandle.WaitSuccess;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "WaitHandle_CorWaitOnePrioritizedNative")]
        private static partial int WaitNative(SafeWaitHandle handle, int timeoutMs);

        protected override void ReleaseCore(int count)
        {
            Debug.Assert(_semaphore != null);
            Debug.Assert(count > 0);

            _semaphore!.Release(count);
        }

        public void Dispose()
        {
            Debug.Assert(_semaphore != null);
            _semaphore!.Dispose();
        }
    }
}
