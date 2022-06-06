// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Represents an opaque, GC handle to a managed object. A GC handle is used when an
    /// object reference must be reachable from unmanaged memory.
    /// </summary>
    /// <remarks>
    /// There are 4 kinds of roots:
    /// Normal: Keeps the object from being collected.
    /// Weak: Allows object to be collected and handle contents will be zeroed.
    /// Weak references are zeroed before the finalizer runs, so if the
    /// object is resurrected in the finalizer the weak reference is still zeroed.
    /// WeakTrackResurrection: Same as Weak, but stays until after object is really gone.
    /// Pinned - same as Normal, but allows the address of the actual object to be taken.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public partial struct GCHandle : IEquatable<GCHandle>
    {
        // The actual integer handle value that the EE uses internally.
        private nint _handle;

        // Allocate a handle storing the object and the type.
        private GCHandle(object? value, GCHandleType type)
        {
            // Make sure the type parameter is within the valid range for the enum.
            if ((uint)type > (uint)GCHandleType.Pinned) // IMPORTANT: This must be kept in sync with the GCHandleType enum.
            {
                throw new ArgumentOutOfRangeException(nameof(type), SR.ArgumentOutOfRange_Enum);
            }

            if (type == GCHandleType.Pinned && !Marshal.IsPinnable(value))
            {
                throw new ArgumentException(SR.ArgumentException_NotIsomorphic, nameof(value));
            }

            nint handle = InternalAlloc(value, type);

            if (type == GCHandleType.Pinned)
            {
                // Record if the handle is pinned.
                handle = (nint)((nint)handle | 1);
            }

            _handle = handle;
        }

        // Used in the conversion functions below.
        private GCHandle(nint handle) => _handle = handle;

        /// <summary>Creates a new GC handle for an object.</summary>
        /// <param name="value">The object that the GC handle is created for.</param>
        /// <returns>A new GC handle that protects the object.</returns>
        public static GCHandle Alloc(object? value) => new GCHandle(value, GCHandleType.Normal);

        /// <summary>Creates a new GC handle for an object.</summary>
        /// <param name="value">The object that the GC handle is created for.</param>
        /// <param name="type">The type of GC handle to create.</param>
        /// <returns>A new GC handle that protects the object.</returns>
        public static GCHandle Alloc(object? value, GCHandleType type) => new GCHandle(value, type);

        /// <summary>Frees a GC handle.</summary>
        public void Free()
        {
            // Free the handle if it hasn't already been freed.
            nint handle = Interlocked.Exchange(ref _handle, 0);
            ThrowIfInvalid(handle);
            InternalFree(GetHandleValue(handle));
        }

        // Target property - allows getting / updating of the handle's referent.
        public object? Target
        {
            get
            {
                nint handle = _handle;
                ThrowIfInvalid(handle);

                return InternalGet(GetHandleValue(handle));
            }
            set
            {
                nint handle = _handle;
                ThrowIfInvalid(handle);

                if (IsPinned(handle) && !Marshal.IsPinnable(value))
                {
                    throw new ArgumentException(SR.ArgumentException_NotIsomorphic, nameof(value));
                }

                InternalSet(GetHandleValue(handle), value);
            }
        }

        /// <summary>
        /// Retrieve the address of an object in a Pinned handle.  This throws
        /// an exception if the handle is any type other than Pinned.
        /// </summary>
        public nint AddrOfPinnedObject()
        {
            // Check if the handle was not a pinned handle.
            // You can only get the address of pinned handles.
            nint handle = _handle;
            ThrowIfInvalid(handle);

            if (!IsPinned(handle))
            {
                ThrowHelper.ThrowInvalidOperationException_HandleIsNotPinned();
            }

            // Get the address.

            object? target = InternalGet(GetHandleValue(handle));
            if (target is null)
            {
                return default;
            }

            unsafe
            {
                if (RuntimeHelpers.ObjectHasComponentSize(target))
                {
                    if (target.GetType() == typeof(string))
                    {
                        return (nint)Unsafe.AsPointer(ref Unsafe.As<string>(target).GetRawStringData());
                    }

                    Debug.Assert(target is Array);
                    return (nint)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(Unsafe.As<Array>(target)));
                }

                return (nint)Unsafe.AsPointer(ref target.GetRawData());
            }
        }

        /// <summary>Determine whether this handle has been allocated or not.</summary>
        public bool IsAllocated => (nint)_handle != 0;

        /// <summary>
        /// Used to create a GCHandle from an int.  This is intended to
        /// be used with the reverse conversion.
        /// </summary>
        public static explicit operator GCHandle(nint value) => FromIntPtr(value);

        public static GCHandle FromIntPtr(nint value)
        {
            ThrowIfInvalid(value);
            return new GCHandle(value);
        }

        /// <summary>Used to get the internal integer representation of the handle out.</summary>
        public static explicit operator nint(GCHandle value) => ToIntPtr(value);

        public static nint ToIntPtr(GCHandle value) => value._handle;

        public override int GetHashCode() => _handle.GetHashCode();

        public override bool Equals([NotNullWhen(true)] object? o) => o is GCHandle other && Equals(other);

        /// <summary>Indicates whether the current instance is equal to another instance of the same type.</summary>
        /// <param name="other">An instance to compare with this instance.</param>
        /// <returns>true if the current instance is equal to the other instance; otherwise, false.</returns>
        public bool Equals(GCHandle other) => _handle == other._handle;

        public static bool operator ==(GCHandle a, GCHandle b) => (nint)a._handle == (nint)b._handle;

        public static bool operator !=(GCHandle a, GCHandle b) => (nint)a._handle != (nint)b._handle;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nint GetHandleValue(nint handle) => handle & ~(nint)1; // Remove Pin flag

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsPinned(nint handle) => (handle & 1) != 0; // Check Pin flag

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ThrowIfInvalid(nint handle)
        {
            // Check if the handle was never initialized or was freed.
            if ((nint)handle == 0)
            {
                ThrowHelper.ThrowInvalidOperationException_HandleIsNotInitialized();
            }
        }
    }
}
