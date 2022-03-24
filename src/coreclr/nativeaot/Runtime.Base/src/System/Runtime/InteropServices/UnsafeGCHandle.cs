// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Limited implementation of GCHandle.  Only implements as much as is currently used.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct UnsafeGCHandle
    {
        // The actual integer handle value that the EE uses internally.
        private IntPtr _handle;

        // Allocate a handle storing the object and the type.
        private UnsafeGCHandle(object value, GCHandleType type)
        {
            Debug.Assert((uint)type <= (uint)GCHandleType.Normal, "unexpected handle type");

            _handle = InternalCalls.RhpHandleAlloc(value, type);
            if (_handle == IntPtr.Zero)
                throw new OutOfMemoryException();
        }

        public static UnsafeGCHandle Alloc(object value, GCHandleType type)
        {
            return new UnsafeGCHandle(value, type);
        }

        // Target property - allows getting / updating of the handle's referent.
        public unsafe object Target
        {
            get
            {
                Debug.Assert(IsAllocated, "handle isn't initialized");
#if DEBUG
                // The runtime performs additional checks in debug builds
                return InternalCalls.RhHandleGet(_handle);
#else
                return Unsafe.As<IntPtr, object>(ref *(IntPtr*)_handle);
#endif
            }

            set
            {
                Debug.Assert(IsAllocated, "handle isn't initialized");
                InternalCalls.RhHandleSet(_handle, value);
            }
        }

        // Determine whether this handle has been allocated or not.
        public bool IsAllocated
        {
            get
            {
                return _handle != default(IntPtr);
            }
        }
    }
}
