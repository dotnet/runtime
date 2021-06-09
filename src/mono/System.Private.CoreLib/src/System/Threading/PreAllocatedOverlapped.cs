// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    public sealed class PreAllocatedOverlapped : System.IDisposable
    {
        [CLSCompliantAttribute(false)]
        public PreAllocatedOverlapped(IOCompletionCallback callback, object? state, object? pinData) { }
        [CLSCompliantAttribute(false)]
        public static PreAllocatedOverlapped UnsafeCreate(IOCompletionCallback callback, object? state, object? pinData) => new PreAllocatedOverlapped(callback, state, pinData);
        public void Dispose() { }
        internal bool IsUserObject(byte[]? buffer) => false;
    }
}
