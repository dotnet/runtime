// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    [SecurityCritical]
    internal sealed class SafeX509ExtensionHandle : SafeHandle
    {
        private SafeX509ExtensionHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.Crypto.X509ExtensionDestroy(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }
    }

    [SecurityCritical]
    internal sealed class SafeEkuExtensionHandle : SafeHandle
    {
        private SafeEkuExtensionHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.Crypto.ExtendedKeyUsageDestory(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }
    }
}