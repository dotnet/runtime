// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal abstract class SafeInteriorHandle : SafeHandle
    {
        private SafeHandle _parent;

        protected SafeInteriorHandle(IntPtr invalidHandleValue, bool ownsHandle)
            : base(invalidHandleValue, ownsHandle)
        {
        }

        protected override bool ReleaseHandle()
        {
            SafeHandle parent = _parent;

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
    }
}
