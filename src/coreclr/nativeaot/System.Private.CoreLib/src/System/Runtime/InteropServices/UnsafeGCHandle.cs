// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

using Internal.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// The unsafe version of the GCHandle structure.
    /// </summary>
    /// <remarks>
    /// Differences from the <c>GCHandle</c> structure:
    /// <list type="bullet">
    /// <item>The constructor assumes the handle type is valid; no range check is performed.</item>
    /// <item>The pinned flag is not stored in the <c>_handle</c> field.</item>
    /// <item>The <c>Target</c> getter and setter assume the <c>UnsafeGCHandle</c> has been allocated.</item>
    /// <item>No blittable check is performed when allocating a pinned <c>UnsafeGCHandle</c> or setting its target.</item>
    /// <item>The <c>GetRawTargetAddress</c> method returns the raw address of the target (the pointer to
    /// its <c>m_pEEType</c> field).</item>
    /// <item>The <c>Free</c> method is not thread-safe and does not throw if the <c>UnsafeGCHandle</c>
    /// has not been allocated or has been already freed.</item>
    /// </list>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal struct UnsafeGCHandle
    {
        // IMPORTANT: This must be kept in sync with the GCHandleType enum.
        private const GCHandleType MaxHandleType = GCHandleType.Pinned;

        // The actual integer handle value that the EE uses internally.
        private IntPtr _handle;

        // Allocate a handle storing the object and the type.
        private UnsafeGCHandle(object value, GCHandleType type)
        {
            Debug.Assert((uint)type <= (uint)MaxHandleType, "Unexpected handle type");
            _handle = RuntimeImports.RhHandleAlloc(value, type);
        }

        public static UnsafeGCHandle Alloc(object value, GCHandleType type = GCHandleType.Normal)
        {
            return new UnsafeGCHandle(value, type);
        }

        // Target property - allows getting / updating of the handle's referent.
        public object Target
        {
            get
            {
                Debug.Assert(IsAllocated, "Handle is not initialized");
                return RuntimeImports.RhHandleGet(_handle);
            }

            set
            {
                Debug.Assert(IsAllocated, "Handle is not initialized");
                RuntimeImports.RhHandleSet(_handle, value);
            }
        }

        // Frees a GC handle. This method is not thread-safe!
        public void Free()
        {
            if (_handle != default(IntPtr))
            {
                RuntimeImports.RhHandleFree(_handle);
            }
        }

        // Returns the raw address of the target assuming it is pinned.
        public unsafe IntPtr GetRawTargetAddress()
        {
            return *(IntPtr*)_handle;
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
