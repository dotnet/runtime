// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
            Interop.NativeCrypto.RecursiveFreeX509Stack(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }
    }
}
