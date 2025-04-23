// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    public sealed class PreAllocatedOverlapped : System.IDisposable
    {
        [CLSCompliantAttribute(false)]
        public PreAllocatedOverlapped(IOCompletionCallback callback, object? state, object? pinData) { }
#pragma warning disable CA1822 // Mark members as static
        internal bool AddRef() => false;
        internal void Release() { }
#pragma warning restore CA1822 // Mark members as static
        [CLSCompliantAttribute(false)]
        public static PreAllocatedOverlapped UnsafeCreate(IOCompletionCallback callback, object? state, object? pinData) => new PreAllocatedOverlapped(callback, state, pinData);
        public void Dispose() { }
        internal ThreadPoolBoundHandleOverlapped? _overlappedPortableCore;
    }
}
