// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Provides extension methods to operate with GC handles.
    /// </summary>
    public static class GCHandleExtensions
    {
        // The following methods are strongly typed generic specifications only on
        // PinnedGCHandles with correct type.

        /// <summary>
        /// Retrieves the address of array data in <paramref name="handle"/>.
        /// </summary>
        /// <param name="handle">The handle to retrieve pointer from.</param>
        /// <returns>
        /// The address of 0th array element the pinned array,
        /// or <see langword="null"/> if the handle doesn't point to any object.
        /// </returns>
        /// <exception cref="NullReferenceException">If the handle is not initialized or already disposed.</exception>
        [CLSCompliant(false)]
        public static unsafe T* GetAddressOfArrayData<T>(
#nullable disable // Nullable oblivious because no covariance between PinnedGCHandle<T> and PinnedGCHandle<T?>
            this PinnedGCHandle<T[]> handle)
#nullable restore
        {
            T[]? array = handle.Target;
            if (array is null)
                return null;

            // Unsafe.AsPointer call is safe since object is pinned.
            return (T*)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(array));
        }

        /// <summary>
        /// Retrieves the address string data in <paramref name="handle"/>.
        /// </summary>
        /// <param name="handle">The handle to retrieve pointer from.</param>
        /// <returns>
        /// The address of 0th character of the pinned <see cref="string"/>,
        /// or <see langword="null"/> if the handle doesn't point to any object.
        /// </returns>
        /// <exception cref="NullReferenceException">If the handle is not initialized or already disposed.</exception>
        [CLSCompliant(false)]
        public static unsafe char* GetAddressOfStringData(
#nullable disable // Nullable oblivious because no covariance between PinnedGCHandle<T> and PinnedGCHandle<T?>
            this PinnedGCHandle<string> handle)
#nullable restore
        {
            string? str = handle.Target;
            if (str is null)
                return null;

            // Unsafe.AsPointer call is safe since object is pinned.
            return (char*)Unsafe.AsPointer(ref str.GetRawStringData());
        }
    }
}
