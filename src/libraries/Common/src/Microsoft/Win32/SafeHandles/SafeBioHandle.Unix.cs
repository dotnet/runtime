// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeBioHandle : SafeHandle
    {
        private SafeHandle? _parent;

        public SafeBioHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            IntPtr h = handle;
            SetHandle(IntPtr.Zero);

            if (_parent != null)
            {
                SafeHandle parent = _parent;
                _parent = null;
                parent.DangerousRelease();
                return true;
            }
            else
            {
                return Interop.Crypto.BioDestroy(h);
            }
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

        internal void TransferOwnershipToParent(SafeHandle parent)
        {
            Debug.Assert(_parent == null, "Expected no existing parent");
            Debug.Assert(parent != null && !parent.IsInvalid, "Expected new parent to be non-null and valid");

            bool addedRef = false;
            parent.DangerousAddRef(ref addedRef);

            _parent = parent;
        }
    }
}
