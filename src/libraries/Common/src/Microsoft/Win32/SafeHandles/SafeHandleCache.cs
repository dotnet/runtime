// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Win32.SafeHandles
{
    /// <summary>Provides a cache for special instances of SafeHandles.</summary>
    /// <typeparam name="T">Specifies the type of SafeHandle.</typeparam>
    internal static class SafeHandleCache<T> where T : SafeHandle
    {
        private static T s_invalidHandle;

        /// <summary>
        /// Gets a cached, invalid handle.  As the instance is cached, it should either never be Disposed
        /// or it should override <see cref="SafeHandle.Dispose(bool)"/> to prevent disposal when the
        /// instance is the <see cref="InvalidHandle"/>.
        /// </summary>
        internal static T GetInvalidHandle(Func<T> invalidHandleFactory)
        {
            T currentHandle = Volatile.Read(ref s_invalidHandle);
            if (currentHandle == null)
            {
                T newHandle = invalidHandleFactory();
                currentHandle = Interlocked.CompareExchange(ref s_invalidHandle, newHandle, null);
                if (currentHandle == null)
                {
                    GC.SuppressFinalize(newHandle);
                    currentHandle = newHandle;
                }
                else
                {
                    newHandle.Dispose();
                }
            }
            Debug.Assert(currentHandle.IsInvalid);
            return currentHandle;
        }

        /// <summary>Gets whether the specified handle is <see cref="InvalidHandle"/>.</summary>
        /// <param name="handle">The handle to compare.</param>
        /// <returns>true if <paramref name="handle"/> is <see cref="InvalidHandle"/>; otherwise, false.</returns>
        internal static bool IsCachedInvalidHandle(SafeHandle handle)
        {
            Debug.Assert(handle != null);
            bool isCachedInvalidHandle = ReferenceEquals(handle, Volatile.Read(ref s_invalidHandle));
            Debug.Assert(!isCachedInvalidHandle || handle.IsInvalid, "The cached invalid handle must still be invalid.");
            return isCachedInvalidHandle;
        }
    }
}
