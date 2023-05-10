// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Threading
{
    public struct LockHolder : IDisposable
    {
        private Lock _lock;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LockHolder Hold(Lock l)
        {
            LockHolder h;
            l.Acquire();
            h._lock = l;
            return h;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            _lock.Release();
        }
    }
}
