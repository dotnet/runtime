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
        /// Retrieves the address of string data in a <see cref="PinnedGCHandle{T}"/> of array.
        /// </summary>
        /// <returns>The address of the pinned array.</returns>
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
        /// Retrieves the address of string data in a <see cref="PinnedGCHandle{T}"/> of <see cref="string"/>.
        /// </summary>
        /// <returns>The address of the pinned <see cref="string"/>.</returns>
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
