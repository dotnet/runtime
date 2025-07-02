// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Represents a strongly-typed GC handle to a managed object.
    /// The object is allowed to be collected. When the object is collected, handle target is set to null.
    /// </summary>
    /// <remarks>
    /// <para>This type corresponds to <see cref="GCHandleType.Weak"/> or <see cref="GCHandleType.WeakTrackResurrection"/>.</para>
    /// <para>
    /// This type is unsafe if used incorrectly. Incorrect usage like mismanagement
    /// of lifetime, providing invalid handle value or concurrent disposal can result
    /// in hard to diagnose crashes or data corruptions.
    /// </para>
    /// </remarks>
    /// <seealso cref="GCHandle" />
    /// <typeparam name="T">The type of the object this <see cref="WeakGCHandle{T}"/> tracks to.</typeparam>
    public struct WeakGCHandle<T> : IEquatable<WeakGCHandle<T>>, IDisposable
        where T : class?
    {
        // The actual integer handle value that the EE uses internally.
        private IntPtr _handle;

        /// <summary>
        /// Allocates a handle for the specified object.
        /// </summary>
        /// <param name="target">The object that uses the <see cref="WeakGCHandle{T}"/>.</param>
        /// <param name="trackResurrection">Whether to track the object when it's resurrected in the finalizer.</param>
        public WeakGCHandle(T target, bool trackResurrection = false)
        {
            _handle = GCHandle.InternalAlloc(target, trackResurrection ? GCHandleType.WeakTrackResurrection : GCHandleType.Weak);
        }

        private WeakGCHandle(IntPtr handle) => _handle = handle;

        /// <summary>Determine whether this handle has been allocated or not.</summary>
        public readonly bool IsAllocated => _handle != IntPtr.Zero;

        /// <summary>
        /// Tries to retrieve the target object that is referenced by the current <see cref="WeakGCHandle{T}"/> object.
        /// </summary>
        /// <param name="target">When this method returns, contains the target object, if it is available.</param>
        /// <returns><see langword="true"/> if the target was retrieved; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="NullReferenceException">If the handle is not initialized or already disposed.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryGetTarget([NotNullWhen(true)] out T? target)
        {
            IntPtr handle = _handle;
            GCHandle.CheckUninitialized(handle);
            // Skip the type check to provide lowest overhead.
            T? obj = Unsafe.As<T?>(GCHandle.InternalGet(handle));
            target = obj;
            return obj != null;
        }

        /// <summary>Sets the object this handle represents.</summary>
        /// <exception cref="NullReferenceException">If the handle is not initialized or already disposed.</exception>
        public readonly void SetTarget(T target)
        {
            IntPtr handle = _handle;
            GCHandle.CheckUninitialized(handle);
            GCHandle.InternalSet(handle, target);
        }

        /// <summary>
        /// Returns a new <see cref="WeakGCHandle{T}"/> object created from a handle to a managed object.
        /// </summary>
        /// <param name="value">An <see cref="IntPtr"/> handle to a managed object to create a <see cref="WeakGCHandle{T}"/> object from.</param>
        /// <returns>A new <see cref="WeakGCHandle{T}"/> object that corresponds to the value parameter.</returns>
        /// <remarks>
        /// <para>This method doesn't validate the provided handle value. The caller must ensure the validity of the handle.</para>
        /// <para>
        /// The <see cref="IntPtr"/> representation of <see cref="WeakGCHandle{T}"/> is not
        /// interchangable with <see cref="GCHandle"/>.
        /// </para>
        /// </remarks>
        public static WeakGCHandle<T> FromIntPtr(IntPtr value) => new WeakGCHandle<T>(value);

        /// <summary>
        /// Returns the internal integer representation of a <see cref="WeakGCHandle{T}"/> object.
        /// </summary>
        /// <param name="value">A <see cref="WeakGCHandle{T}"/> object to retrieve an internal integer representation from.</param>
        /// <returns>An <see cref="IntPtr"/> object that represents a <see cref="WeakGCHandle{T}"/> object.</returns>
        /// <remarks>
        /// The <see cref="IntPtr"/> representation of <see cref="WeakGCHandle{T}"/> is not
        /// interchangable with <see cref="GCHandle"/>.
        /// </remarks>
        public static IntPtr ToIntPtr(WeakGCHandle<T> value) => value._handle;

        /// <summary>Releases this <see cref="WeakGCHandle{T}"/>.</summary>
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
        public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is WeakGCHandle<T> handle && Equals(handle);

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        public readonly bool Equals(WeakGCHandle<T> other) => _handle == other._handle;

        /// <summary>
        /// Returns the hash code for the current instance.
        /// </summary>
        /// <returns>A hash code for the current instance.</returns>
        public override readonly int GetHashCode() => _handle.GetHashCode();
    }
}
