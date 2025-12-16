// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

using Internal.Runtime;

namespace System.Threading
{
    /// <summary>
    /// Manipulates the object header located 4 bytes before each object's MethodTable pointer
    /// in the managed heap.
    /// </summary>
    /// <remarks>
    /// Do not store managed pointers (ref int) to the object header in locals or parameters
    /// as they may be incorrectly updated during garbage collection.
    /// </remarks>
    internal static class ObjectHeader
    {
        // These must match the values in syncblk.h
        public enum HeaderLockResult
        {
            Success = 0,
            Failure = 1,
            UseSlowPath = 2
        };

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern HeaderLockResult Acquire(object obj);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern HeaderLockResult Release(object obj);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern HeaderLockResult IsAcquired(object obj);
    }
}
