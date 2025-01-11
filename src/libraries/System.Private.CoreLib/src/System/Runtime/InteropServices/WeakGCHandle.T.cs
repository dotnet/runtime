// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Represents an strongly-typed opaque, GC handle to a managed object.
    /// A GC handle is used when an object reference must be reachable from
    /// unmanaged memory.
    /// The object is allowed to be collected and handle contents will be zeroed.
    /// </summary>
    /// <remarks>
    /// <see cref="WeakGCHandle{T}"/> corresponds to Weak or WeakTrackResurrection roots.
    /// For Normal, see <see cref="GCHandle{T}"/>.
    /// For Pinned, see <see cref="PinnedGCHandle{T}"/>.
    /// </remarks>
    /// <seealso cref="GCHandle" />
    /// <typeparam name="T">The type of the object this <see cref="GCHandle{T}"/> tracks to.</typeparam>
    public struct WeakGCHandle<T> : IDisposable
        where T : class?
    {
        // The actual integer handle value that the EE uses internally.
        private IntPtr _handle;

        /// <summary>
        /// Allocates a handle for the specified object.
        /// </summary>
        /// <param name="target">The object that uses the <see cref="GCHandle{T}"/>.</param>
        /// <param name="trackResurrection">Whether track the object when it's resurrected in the finalizer.</param>
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryGetTarget([NotNullWhen(true)] out T? target)
        {
            IntPtr handle = _handle;
            GCHandle.ThrowIfInvalid(_handle);

            var obj = (T?)GCHandle.InternalGet(handle);
            target = obj;
            return obj != null;
        }

        /// <summary>Sets the object this handle represents.</summary>
        public readonly void SetTarget(T target)
        {
            IntPtr handle = _handle;
            GCHandle.ThrowIfInvalid(handle);

            GCHandle.InternalSet(handle, target);
        }

        /// <summary>
        /// Returns a new <see cref="WeakGCHandle{T}"/> object created from a handle to a managed object.
        /// </summary>
        /// <param name="value">An <see cref="IntPtr"/> handle to a managed object to create a <see cref="WeakGCHandle{T}"/> object from.</param>
        /// <returns>A new <see cref="WeakGCHandle{T}"/> object that corresponds to the value parameter.</returns>
        /// <remarks>
        /// The <see cref="IntPtr"/> representation of <see cref="WeakGCHandle{T}"/> is not
        /// interchangable with <see cref="GCHandle"/>.
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

        /// <summary>
        /// Releases this <see cref="WeakGCHandle{T}"/>.
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
