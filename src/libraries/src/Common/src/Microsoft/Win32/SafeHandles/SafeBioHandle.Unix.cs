// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Microsoft.Win32.SafeHandles
{
    [SecurityCritical]
    internal sealed class SafeBioHandle : SafeHandle
    {
        private SafeHandle _parent;

        private SafeBioHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            if (_parent != null)
            {
                _parent.DangerousRelease();
                _parent = null;
            }
            else
            {
                Interop.Crypto.BioDestroy(handle);
            }
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

        internal void TransferOwnershipToParent(SafeHandle parent)
        {
            Debug.Assert(_parent == null, "Expected no existing parent");

            bool addedRef = false;
            parent.DangerousAddRef(ref addedRef);

            _parent = parent;
        }
    }
}
