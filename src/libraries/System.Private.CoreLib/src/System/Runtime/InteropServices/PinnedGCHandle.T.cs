// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Represents a strongly-typed GC handle to a managed object.
    /// The object is pinned at fixed location in GC heap and allows its
    /// address to be taken.
    /// </summary>
    /// <remarks>
    /// <para>This type corresponds to <see cref="GCHandleType.Pinned"/>.</para>
    /// <para>
    /// This type is unsafe if used incorrectly. Incorrect usage like mismanagement
    /// of lifetime, providing invalid handle value or concurrent disposal can result
    /// in hard to diagnose crashes or data corruptions.
    /// </para>
    /// </remarks>
    /// <seealso cref="GCHandle" />
    /// <typeparam name="T">The type of the object this <see cref="PinnedGCHandle{T}"/> tracks to.</typeparam>
    public struct PinnedGCHandle<T> : IEquatable<PinnedGCHandle<T>>, IDisposable
        where T : class?
    {
        // The actual integer handle value that the EE uses internally.
        private IntPtr _handle;

        /// <summary>
        /// Allocates a handle for the specified object.
        /// </summary>
        /// <param name="target">The object that uses the <see cref="PinnedGCHandle{T}"/>.</param>
        public PinnedGCHandle(T target)
        {
            // Unlike GCHandle, pinning any object is allowed
            _handle = GCHandle.InternalAlloc(target, GCHandleType.Pinned);
        }

        private PinnedGCHandle(IntPtr handle) => _handle = handle;

        /// <summary>Determine whether this handle has been allocated or not.</summary>
        public readonly bool IsAllocated => _handle != IntPtr.Zero;

        /// <summary>Gets or sets the object this handle represents.</summary>
        /// <exception cref="NullReferenceException">If the handle is not initialized or already disposed.</exception>
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
        /// <returns>
        /// The address of first instance field of the pinned object,
        /// or <see langword="null"/> if the handle doesn't point to any object.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method is intended to be used with types other than array or <see cref="string"/>.
        /// For array or <see cref="string"/>, use <see cref="GCHandleExtensions.GetAddressOfArrayData{T}(PinnedGCHandle{T[]})"/>
        /// or <see cref="GCHandleExtensions.GetAddressOfStringData(PinnedGCHandle{string})"/> instead.
        /// </para>
        /// <para>
        /// This method should only be used for blittable types.
        /// </para>
        /// </remarks>
        /// <exception cref="NullReferenceException">If the handle is not initialized or already disposed.</exception>
        [CLSCompliant(false)]
        public readonly unsafe void* GetAddressOfObjectData()
        {
            object? target = Target;
            if (target is null)
            {
                return null;
            }

            // Unsafe.AsPointer is safe since object is pinned.
            return Unsafe.AsPointer(ref target.GetRawData());
        }

        /// <summary>
        /// Returns a new <see cref="PinnedGCHandle{T}"/> object created from a handle to a managed object.
        /// </summary>
        /// <param name="value">An <see cref="IntPtr"/> handle to a managed object to create a <see cref="PinnedGCHandle{T}"/> object from.</param>
        /// <returns>A new <see cref="PinnedGCHandle{T}"/> object that corresponds to the value parameter.</returns>
        /// <remarks>
        /// <para>This method doesn't validate the provided handle value. The caller must ensure the validity of the handle.</para>
        /// <para>
        /// The <see cref="IntPtr"/> representation of <see cref="PinnedGCHandle{T}"/> is not
        /// interchangable with <see cref="GCHandle"/>.
        /// </para>
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

        /// <summary>Releases this <see cref="PinnedGCHandle{T}"/>.</summary>
        /// <remarks>This method is not thread safe.</remarks>
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

        /// <inheritdoc/>
        public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is PinnedGCHandle<T> handle && Equals(handle);

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        public readonly bool Equals(PinnedGCHandle<T> other) => _handle == other._handle;

        /// <summary>
        /// Returns the hash code for the current instance.
        /// </summary>
        /// <returns>A hash code for the current instance.</returns>
        public override readonly int GetHashCode() => _handle.GetHashCode();
    }
}
