// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Represents an strongly-typed opaque, GC handle to a managed object.
    /// A GC handle is used when an object reference must be reachable from
    /// unmanaged memory.
    /// </summary>
    /// <remarks>
    /// <see cref="GCHandle{T}"/> corresponds to Normal roots.
    /// For Weak and WeakTrackResurrection, see WeakGCHandle.
    /// For Pinned, see PinnedGCHandle.
    /// </remarks>
    /// <seealso cref="GCHandle" />
    /// <typeparam name="T">The type of the object this <see cref="GCHandle{T}"/> tracks to.</typeparam>
    public struct GCHandle<T>
        where T : class?
    {
        // The actual integer handle value that the EE uses internally.
        private IntPtr _handle;
    }
}
