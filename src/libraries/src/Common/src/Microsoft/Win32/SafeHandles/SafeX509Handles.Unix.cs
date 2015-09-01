// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Security;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    [SecurityCritical]
    internal sealed class SafeX509Handle : SafeHandle
    {
        internal static readonly SafeX509Handle InvalidHandle = new SafeX509Handle();

        private SafeX509Handle() : 
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        [SecurityCritical]
        protected override bool ReleaseHandle()
        {
            Interop.libcrypto.X509_free(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid
        {
            [SecurityCritical]
            get { return handle == IntPtr.Zero; }
        }
    }

    internal sealed class SafeX509CrlHandle : SafeHandle
    {
        private SafeX509CrlHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.libcrypto.X509_CRL_free(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }
    }

    [SecurityCritical]
    internal sealed class SafeX509StoreHandle : SafeHandle
    {
        private SafeX509StoreHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.libcrypto.X509_STORE_free(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }
    }
    [SecurityCritical]
    internal sealed class SafeX509StoreCtxHandle : SafeHandle
    {
        private SafeX509StoreCtxHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.libcrypto.X509_STORE_CTX_free(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }
    }

    [SecurityCritical]
    internal sealed class SafeX509StackHandle : SafeHandle
    {
        private SafeX509StackHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.Crypto.RecursiveFreeX509Stack(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }
    }

    /// <summary>
    /// Represents access to a STACK_OF(X509)* which is a member of a structure tracked
    /// by another SafeHandle.
    /// </summary>
    [SecurityCritical]
    internal sealed class SafeSharedX509StackHandle : SafeHandle
    {
        internal static readonly SafeSharedX509StackHandle InvalidHandle = new SafeSharedX509StackHandle();
        private SafeHandle _parent;

        private SafeSharedX509StackHandle() :
            base(IntPtr.Zero, ownsHandle: true)
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
