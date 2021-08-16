// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal abstract class SafeInteriorHandle : SafeHandle
    {
        private SafeHandle? _parent;

        protected SafeInteriorHandle(IntPtr invalidHandleValue, bool ownsHandle)
            : base(invalidHandleValue, ownsHandle)
        {
        }

        protected override bool ReleaseHandle()
        {
            SafeHandle? parent = _parent;

            if (parent != null)
            {
                parent.DangerousRelease();
            }

            _parent = null;
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid
        {
            get
            {
                // If handle is 0, we're invalid.
                // If we have a _parent and they're invalid, we're invalid.
                return handle == IntPtr.Zero || (_parent != null && _parent.IsInvalid);
            }
        }

        internal void SetParent(SafeHandle parent)
        {
            bool addedRef = false;
            parent.DangerousAddRef(ref addedRef);
            Debug.Assert(addedRef);

            _parent = parent;
        }

        internal static TInteriorHandle OpenInteriorHandle<TInteriorHandle, TExteriorHandle>(
            Func<TExteriorHandle, TInteriorHandle> accessor,
            TExteriorHandle exteriorHandle)
            where TInteriorHandle : SafeInteriorHandle
            where TExteriorHandle : SafeHandle
        {
            TInteriorHandle interiorHandle = accessor(exteriorHandle);

            if (!interiorHandle.IsInvalid)
            {
                interiorHandle.SetParent(exteriorHandle);
            }

            return interiorHandle;
        }

        internal static TInteriorHandle OpenInteriorHandle<TExteriorHandle, TArg1, TInteriorHandle>(
            Func<TExteriorHandle, TArg1, TInteriorHandle> accessor,
            TExteriorHandle exteriorHandle,
            TArg1 arg1)
            where TInteriorHandle : SafeInteriorHandle
            where TExteriorHandle : SafeHandle
        {
            TInteriorHandle interiorHandle = accessor(exteriorHandle, arg1);

            if (!interiorHandle.IsInvalid)
            {
                interiorHandle.SetParent(exteriorHandle);
            }

            return interiorHandle;
        }
    }
}
