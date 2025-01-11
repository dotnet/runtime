// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Provides extension methods to operate with GC handles.
    /// </summary>
    public static class GCHandleExtensions
    {
        // The following methods are strongly typed generic specifications only on
        // PinnedGCHandles with correct type.

#nullable disable // Nullable oblivious because no covariance between PinnedGCHandle<T> and PinnedGCHandle<T?>
        /// <summary>
        /// Retrieves the address of string data in a <see cref="PinnedGCHandle{T}"/> of array.
        /// </summary>
        /// <returns>The address of the pinned array.</returns>
        [CLSCompliant(false)]
        public static unsafe T* GetAddressOfArrayData<T>(this PinnedGCHandle<T[]> handle)
            => (T*)handle.GetAddressOfObjectData();

        /// <summary>
        /// Retrieves the address of string data in a <see cref="PinnedGCHandle{T}"/> of <see cref="string"/>.
        /// </summary>
        /// <returns>The address of the pinned <see cref="string"/>.</returns>
        [CLSCompliant(false)]
        public static unsafe char* GetAddressOfStringData(this PinnedGCHandle<string> handle)
            => (char*)handle.GetAddressOfObjectData();
#nullable restore
    }
}
