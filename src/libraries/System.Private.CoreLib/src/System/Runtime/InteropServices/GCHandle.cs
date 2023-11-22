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
        private IntPtr _handle;

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

            _handle = type == GCHandleType.Pinned ?
                AllocPinnedWithPooling(value) :
                InternalAlloc(value, type);
        }

        /// <summary>Per-thread cache of pinning handles.</summary>
        /// <remarks>Every handle in the cache is a GCHandleType.Pinned handle with a null target.</remarks>
        [ThreadStatic]
        private static FixedCapacityIntPtrStack? s_pooledPinningHandles;

        /// <summary>Allocates a pinning handle whose target is the specified object.</summary>
        /// <param name="value">The object to which the allocated handle should reference.</param>
        /// <returns>The allocated handle.</returns>
        private static IntPtr AllocPinnedWithPooling(object? value)
        {
            // If we have a cache and we can take a handle from it, do so.
            if (s_pooledPinningHandles is FixedCapacityIntPtrStack handles && handles.TryPop(out IntPtr handle))
            {
                // We successfully got a handle from the cache.  Update it to point to the new target.
                InternalSet(GetHandleValue(handle), value);
            }
            else
            {
                // We could not get a handle from the cache.  Allocate a new one.
                handle = MarkPinned(InternalAlloc(value, GCHandleType.Pinned));
            }

            Debug.Assert(IsPinned(handle));
            return handle;
        }

        /// <summary>"Frees" a pinning handle, which might mean actually freeing it but might also mean storing it into a cache.</summary>
        /// <param name="handle">The handle.</param>
        private static void FreePinnedWithPooling(IntPtr handle)
        {
            Debug.Assert(IsPinned(handle));

            // Try to store the handle into the cache rather than actually freeing it.
            FixedCapacityIntPtrStack handles = s_pooledPinningHandles ??= new();
            if (handles.TryPush(handle))
            {
                // Clear the handle's target so that it doesn't keep the object pinned.
                // If this were a cache accessible to multiple threads, this clearing would
                // need to be done before calling TryAdd, as if TryAdd returns true, another
                // thread could take ownership of the handle the moment it hits the cache.
                // But, as the cache is only accessible to this thread, we can do it here
                // after the TryAdd and thus avoid the additional overhead in the case where
                // the cache is full and we have to free.
                InternalSet(GetHandleValue(handle), null);
            }
            else
            {
                // The cache was full: actually free the handle.
                InternalFree(GetHandleValue(handle));
            }
        }

        /// <summary>Provides a simple stack of IntPtrs with a fixed capacity.</summary>
        /// <param name="capacity">The maximum number of items the stack can hold.</param>
        private sealed class FixedCapacityIntPtrStack(int capacity = 8) // 8 == arbitrary limit that can be tuned over time
        {
            /// <summary>Array of values.</summary>
            private readonly IntPtr[] _values = new IntPtr[capacity];
            /// <summary>Number of valid cached handles in <see cref="_values"/>.</summary>
            private int _count;

            /// <summary>Tries to push a value onto the stack.</summary>
            /// <returns>true if the value was stored; otherwise, false.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryPush(IntPtr value)
            {
                bool pushed = false;
                IntPtr[] array = _values;
                int i = _count;
                if ((uint)i < (uint)array.Length)
                {
                    array[i] = value;
                    _count++;
                    pushed = true;
                }

                return pushed;
            }

            /// <summary>Tries to pop a value from the stack.</summary>
            /// <param name="value">The popped value.</param>
            /// <returns>true if a value could be popped; otherwise, false.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryPop(out IntPtr value)
            {
                IntPtr[] array = _values;
                int i = _count - 1;
                if ((uint)i < (uint)array.Length)
                {
                    value = array[i];
                    _count = i;
                    return true;
                }

                value = default;
                return false;
            }
        }

        // Used in the conversion functions below.
        private GCHandle(IntPtr handle) => _handle = handle;

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
            IntPtr handle = Interlocked.Exchange(ref _handle, IntPtr.Zero);
            ThrowIfInvalid(handle);
            if (IsPinned(handle))
            {
                FreePinnedWithPooling(handle);
            }
            else
            {
                InternalFree(GetHandleValue(handle));
            }
        }

        // Target property - allows getting / updating of the handle's referent.
        public object? Target
        {
            readonly get
            {
                IntPtr handle = _handle;
                ThrowIfInvalid(handle);

                return InternalGet(GetHandleValue(handle));
            }
            set
            {
                IntPtr handle = _handle;
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
        public readonly IntPtr AddrOfPinnedObject()
        {
            // Check if the handle was not a pinned handle.
            // You can only get the address of pinned handles.
            IntPtr handle = _handle;
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
                        return (IntPtr)Unsafe.AsPointer(ref Unsafe.As<string>(target).GetRawStringData());
                    }

                    Debug.Assert(target is Array);
                    return (IntPtr)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(Unsafe.As<Array>(target)));
                }

                return (IntPtr)Unsafe.AsPointer(ref target.GetRawData());
            }
        }

        /// <summary>Determine whether this handle has been allocated or not.</summary>
        public readonly bool IsAllocated => (nint)_handle != 0;

        /// <summary>
        /// Used to create a GCHandle from an int.  This is intended to
        /// be used with the reverse conversion.
        /// </summary>
        public static explicit operator GCHandle(IntPtr value) => FromIntPtr(value);

        public static GCHandle FromIntPtr(IntPtr value)
        {
            ThrowIfInvalid(value);
            return new GCHandle(value);
        }

        /// <summary>Used to get the internal integer representation of the handle out.</summary>
        public static explicit operator IntPtr(GCHandle value) => ToIntPtr(value);

        public static IntPtr ToIntPtr(GCHandle value) => value._handle;

        public override readonly int GetHashCode() => _handle.GetHashCode();

        public override readonly bool Equals([NotNullWhen(true)] object? o) => o is GCHandle other && Equals(other);

        /// <summary>Indicates whether the current instance is equal to another instance of the same type.</summary>
        /// <param name="other">An instance to compare with this instance.</param>
        /// <returns>true if the current instance is equal to the other instance; otherwise, false.</returns>
        public readonly bool Equals(GCHandle other) => _handle == other._handle;

        public static bool operator ==(GCHandle a, GCHandle b) => (nint)a._handle == (nint)b._handle;

        public static bool operator !=(GCHandle a, GCHandle b) => (nint)a._handle != (nint)b._handle;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IntPtr GetHandleValue(IntPtr handle) => new IntPtr((nint)handle & ~(nint)1); // Remove Pin flag

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsPinned(IntPtr handle) => ((nint)handle & 1) != 0; // Check Pin flag

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IntPtr MarkPinned(IntPtr handle) => handle | 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ThrowIfInvalid(IntPtr handle)
        {
            // Check if the handle was never initialized or was freed.
            if ((nint)handle == 0)
            {
                ThrowHelper.ThrowInvalidOperationException_HandleIsNotInitialized();
            }
        }
    }
}
