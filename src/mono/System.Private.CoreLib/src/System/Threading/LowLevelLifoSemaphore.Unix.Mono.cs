// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Threading
{
    internal sealed unsafe partial class LowLevelLifoSemaphore : IDisposable
    {
        private IntPtr lifo_semaphore;
#if FEATURE_WASM_THREADS
        private LifoSemaphoreKind _kind;

        // Keep in sync with lifo-semaphore.h
        private enum LifoSemaphoreKind : int {
            Normal = 1,
            AsyncJS = 2,
        }
#endif

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern IntPtr InitInternal(int kind);

#pragma warning disable IDE0060
        private void Create(int maximumSignalCount)
        {
            _kind = LifoSemaphoreKind.Normal;
            lifo_semaphore = InitInternal((int)_kind);
        }
#pragma warning restore IDE0060

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void DeleteInternal(IntPtr semaphore);

        public void Dispose()
        {
            DeleteInternal(lifo_semaphore);
            lifo_semaphore = IntPtr.Zero;
            _kind = (LifoSemaphoreKind)0;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int TimedWaitInternal(IntPtr semaphore, int timeoutMs);

        private bool WaitCore(int timeoutMs)
        {
            ThrowIfInvalidSemaphoreKind(LifoSemaphoreKind.Normal);
            return TimedWaitInternal(lifo_semaphore, timeoutMs) != 0;
        }

        private void ThrowIfInvalidSemaphoreKind(LifoSemaphoreKind expected)
        {
            if (_kind != expected)
                throw new InvalidOperationException ($"Unexpected LowLevelLifoSemaphore kind {_kind} expected {expected}");
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void ReleaseInternal(IntPtr semaphore, int count);

        private void ReleaseCore(int count)
        {
            ReleaseInternal(lifo_semaphore, count);
        }
    }
}
