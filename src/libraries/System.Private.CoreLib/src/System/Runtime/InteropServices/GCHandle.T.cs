// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Represents a strongly-typed GC handle to a managed object.
    /// A GC handle is used to work with object references in unmanaged code.
    /// </summary>
    /// <remarks>
    /// <para>This type corresponds to <see cref="GCHandleType.Normal"/>.</para>
    /// <para>
    /// This type is unsafe if used incorrectly. Incorrect usage like mismanagement
    /// of lifetime, providing invalid handle value or concurrent disposal can result
    /// in hard to diagnose crashes or data corruptions.
    /// </para>
    /// </remarks>
    /// <seealso cref="GCHandle" />
    /// <typeparam name="T">The type of the object this <see cref="GCHandle{T}"/> tracks to.</typeparam>
    public struct GCHandle<T> : IEquatable<GCHandle<T>>, IDisposable
        where T : class?
    {
        // The actual integer handle value that the EE uses internally.
        private IntPtr _handle;

        /// <summary>
        /// Allocates a handle for the specified object.
        /// </summary>
        /// <param name="target">The object that uses the <see cref="GCHandle{T}"/>.</param>
        public GCHandle(T target)
        {
            _handle = GCHandle.InternalAlloc(target, GCHandleType.Normal);
        }

        private GCHandle(IntPtr handle) => _handle = handle;

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
                GCHandle.InternalSet(handle, value);
            }
        }

        /// <summary>
        /// Returns a new <see cref="GCHandle{T}"/> object created from a handle to a managed object.
        /// </summary>
        /// <param name="value">An <see cref="IntPtr"/> handle to a managed object to create a <see cref="GCHandle{T}"/> object from.</param>
        /// <returns>A new <see cref="GCHandle{T}"/> object that corresponds to the value parameter.</returns>
        /// <remarks>
        /// <para>This method doesn't validate the provided handle value. The caller must ensure the validity of the handle.</para>
        /// <para>
        /// The <see cref="IntPtr"/> representation of <see cref="GCHandle{T}"/> is not
        /// interchangable with <see cref="GCHandle"/>.
        /// </para>
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

        /// <summary>Releases this <see cref="GCHandle{T}"/>.</summary>
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
        public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is GCHandle<T> handle && Equals(handle);

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        public readonly bool Equals(GCHandle<T> other) => _handle == other._handle;

        /// <summary>
        /// Returns the hash code for the current instance.
        /// </summary>
        /// <returns>A hash code for the current instance.</returns>
        public override readonly int GetHashCode() => _handle.GetHashCode();
    }
}
