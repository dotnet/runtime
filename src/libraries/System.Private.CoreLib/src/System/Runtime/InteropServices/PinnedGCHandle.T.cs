// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Represents an strongly-typed opaque, GC handle to a managed object.
    /// A GC handle is used when an object reference must be reachable from
    /// unmanaged memory.
    /// The object is pinned at fixed location in GC heap and allows its
    /// address to be taken.
    /// </summary>
    /// <remarks>
    /// <see cref="PinnedGCHandle{T}"/> corresponds to Pinned roots.
    /// For Normal, see <see cref="GCHandle{T}"/>.
    /// For Weak and WeakTrackResurrection, see <see cref="WeakGCHandle{T}"/>.
    /// </remarks>
    /// <seealso cref="GCHandle" />
    /// <typeparam name="T">The type of the object this <see cref="GCHandle{T}"/> tracks to.</typeparam>
    public struct PinnedGCHandle<T> : IEquatable<PinnedGCHandle<T>>, IDisposable
        where T : class?
    {
        // The actual integer handle value that the EE uses internally.
        private IntPtr _handle;

        /// <summary>
        /// Allocates a handle for the specified object.
        /// </summary>
        /// <param name="target">The object that uses the <see cref="GCHandle{T}"/>.</param>
        public PinnedGCHandle(T target)
        {
            // Unlike GCHandle, pinning any object is allowed
            _handle = GCHandle.InternalAlloc(target, GCHandleType.Pinned);
        }

        private PinnedGCHandle(IntPtr handle) => _handle = handle;

        /// <summary>Determine whether this handle has been allocated or not.</summary>
        public readonly bool IsAllocated => _handle != IntPtr.Zero;

        /// <summary>Gets or sets the object this handle represents.</summary>
        public readonly T Target
        {
            get
            {
                IntPtr handle = _handle;
                GCHandle.CheckUninitialized(handle);
                // Skip the type check to provide lowest overhead.
                return Unsafe.As<T>(GCHandle.InternalGet(handle));
            }
            set
            {
                IntPtr handle = _handle;
                GCHandle.CheckUninitialized(handle);
                // Unlike GCHandle, pinning any object is allowed
                GCHandle.InternalSet(handle, value);
            }
        }

        /// <summary>
        /// Retrieves the address of object data in a <see cref="PinnedGCHandle{T}"/>.
        /// </summary>
        /// <returns>The address of the pinned data object.</returns>
        [CLSCompliant(false)]
        public readonly unsafe void* GetAddressOfObjectData()
        {
            IntPtr handle = _handle;
            GCHandle.CheckUninitialized(handle);
            return GCHandle.AddrOfPinnedObjectFromHandle(handle);
        }

        /// <summary>
        /// Returns a new <see cref="PinnedGCHandle{T}"/> object created from a handle to a managed object.
        /// </summary>
        /// <param name="value">An <see cref="IntPtr"/> handle to a managed object to create a <see cref="PinnedGCHandle{T}"/> object from.</param>
        /// <returns>A new <see cref="PinnedGCHandle{T}"/> object that corresponds to the value parameter.</returns>
        /// <remarks>
        /// The <see cref="IntPtr"/> representation of <see cref="PinnedGCHandle{T}"/> is not
        /// interchangable with <see cref="GCHandle"/>.
        /// </remarks>
        public static PinnedGCHandle<T> FromIntPtr(IntPtr value) => new PinnedGCHandle<T>(value);

        /// <summary>
        /// Returns the internal integer representation of a <see cref="PinnedGCHandle{T}"/> object.
        /// </summary>
        /// <param name="value">A <see cref="PinnedGCHandle{T}"/> object to retrieve an internal integer representation from.</param>
        /// <returns>An <see cref="IntPtr"/> object that represents a <see cref="PinnedGCHandle{T}"/> object.</returns>
        /// <remarks>
        /// The <see cref="IntPtr"/> representation of <see cref="PinnedGCHandle{T}"/> is not
        /// interchangable with <see cref="GCHandle"/>.
        /// </remarks>
        public static IntPtr ToIntPtr(PinnedGCHandle<T> value) => value._handle;

        /// <summary>
        /// Releases this <see cref="PinnedGCHandle{T}"/>.
        /// </summary>
        public void Dispose()
        {
            // Free the handle if it hasn't already been freed.
            // Unlike GCHandle.Free, no thread safety is provided.
            IntPtr handle = _handle;
            if (handle != IntPtr.Zero)
            {
                _handle = IntPtr.Zero;
                GCHandle.InternalFree(handle);
            }
        }

        /// <summary>Indicates whether the current instance is equal to another instance of the same type.</summary>
        /// <param name="obj">An instance to compare with this instance.</param>
        /// <returns>true if the current instance is equal to the other instance; otherwise, false.</returns>
        public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is PinnedGCHandle<T> handle && Equals(handle);

        /// <summary>Indicates whether the current instance is equal to another instance of the same type.</summary>
        /// <param name="other">An instance to compare with this instance.</param>
        /// <returns>true if the current instance is equal to the other instance; otherwise, false.</returns>
        public readonly bool Equals(PinnedGCHandle<T> other) => _handle == other._handle;

        /// <summary>
        /// Returns an identifier for the current <see cref="PinnedGCHandle{T}"/> object.
        /// </summary>
        /// <returns>An identifier for the current <see cref="PinnedGCHandle{T}"/> object.</returns>
        public override readonly int GetHashCode() => _handle.GetHashCode();
    }
}
