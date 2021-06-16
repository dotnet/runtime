// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    /// <summary>
    /// SafeHandle representing HCRYPTHASH handle
    /// </summary>
    internal sealed class SafeHashHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeProvHandle? _parent;

        public SafeHashHandle() : base(true)
        {
            SetHandle(IntPtr.Zero);
        }

        internal void SetParent(SafeProvHandle parent)
        {
            if (IsInvalid || IsClosed)
            {
                return;
            }

            Debug.Assert(_parent == null);
            Debug.Assert(!parent.IsClosed);
            Debug.Assert(!parent.IsInvalid);

            _parent = parent;

            bool ignored = false;
            _parent.DangerousAddRef(ref ignored);
        }

        internal static SafeHashHandle InvalidHandle
        {
            get { return SafeHandleCache<SafeHashHandle>.GetInvalidHandle(() => new SafeHashHandle()); }
        }

        protected override void Dispose(bool disposing)
        {
            if (!SafeHandleCache<SafeHashHandle>.IsCachedInvalidHandle(this))
            {
                base.Dispose(disposing);
            }
        }

        protected override bool ReleaseHandle()
        {
            bool successfullyFreed = Interop.Advapi32.CryptDestroyHash(handle);
            Debug.Assert(successfullyFreed);

            SafeProvHandle? parent = _parent;
            _parent = null;
            parent?.DangerousRelease();

            return successfullyFreed;
        }
    }
}
