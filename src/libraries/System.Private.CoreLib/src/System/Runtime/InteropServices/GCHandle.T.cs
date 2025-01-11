// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

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
    /// For Pinned, see <see cref="PinnedGCHandle{T}"/>.
    /// </remarks>
    /// <seealso cref="GCHandle" />
    /// <typeparam name="T">The type of the object this <see cref="GCHandle{T}"/> tracks to.</typeparam>
    public struct GCHandle<T> : IDisposable
        where T : class?
    {
        // The actual integer handle value that the EE uses internally.
        private IntPtr _handle;

        /// <summary>
        /// Allocates a handle for the specified object.
        /// </summary>
        /// <param name="value">The object that uses the <see cref="GCHandle{T}"/>.</param>
        public GCHandle(T value)
        {
            _handle = GCHandle.InternalAlloc(value, GCHandleType.Normal);
        }

        private GCHandle(IntPtr handle) => _handle = handle;

        /// <summary>Determine whether this handle has been allocated or not.</summary>
        public readonly bool IsAllocated => _handle != IntPtr.Zero;

        /// <summary>Gets the object this handle represents.</summary>
        public readonly T Target
        {
            get
            {
                IntPtr handle = _handle;
                GCHandle.ThrowIfInvalid(_handle);

                return (T)GCHandle.InternalGet(handle)!;
            }
        }

        /// <summary>
        /// Returns a new <see cref="GCHandle{T}"/> object created from a handle to a managed object.
        /// </summary>
        /// <param name="value">An <see cref="IntPtr"/> handle to a managed object to create a <see cref="GCHandle{T}"/> object from.</param>
        /// <returns>A new <see cref="GCHandle{T}"/> object that corresponds to the value parameter.</returns>
        /// <remarks>
        /// The <see cref="IntPtr"/> representation of <see cref="GCHandle{T}"/> is not
        /// interchangable with <see cref="GCHandle"/>.
        /// </remarks>
        public static GCHandle<T> FromIntPtr(IntPtr value) => new GCHandle<T>(value);

        /// <summary>
        /// Returns the internal integer representation of a <see cref="GCHandle{T}"/> object.
        /// </summary>
        /// <param name="value">A <see cref="GCHandle{T}"/> object to retrieve an internal integer representation from.</param>
        /// <returns>An <see cref="IntPtr"/> object that represents a <see cref="GCHandle{T}"/> object.</returns>
        /// <remarks>
        /// The <see cref="IntPtr"/> representation of <see cref="GCHandle{T}"/> is not
        /// interchangable with <see cref="GCHandle"/>.
        /// </remarks>
        public static IntPtr ToIntPtr(GCHandle<T> value) => value._handle;

        /// <summary>
        /// Releases this <see cref="GCHandle{T}"/>.
        /// </summary>
        public void Dispose()
        {
            // Free the handle if it hasn't already been freed.
            IntPtr handle = Interlocked.Exchange(ref _handle, IntPtr.Zero);
            if (handle != IntPtr.Zero)
            {
                GCHandle.InternalFree(handle);
            }
        }
    }
}
