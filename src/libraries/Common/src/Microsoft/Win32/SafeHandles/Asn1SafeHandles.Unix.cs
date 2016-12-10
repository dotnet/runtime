// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeAsn1ObjectHandle : SafeHandle
    {
        private SafeAsn1ObjectHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.Crypto.Asn1ObjectFree(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }
    }

    internal sealed class SafeAsn1BitStringHandle : SafeHandle
    {
        private SafeAsn1BitStringHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.Crypto.Asn1BitStringFree(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }
    }

    internal sealed class SafeAsn1OctetStringHandle : SafeHandle
    {
        private SafeAsn1OctetStringHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.Crypto.Asn1OctetStringFree(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }
    }

    internal sealed class SafeAsn1StringHandle : SafeHandle
    {
        private SafeAsn1StringHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.Crypto.Asn1StringFree(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }
    }

    internal sealed class SafeSharedAsn1StringHandle : SafeInteriorHandle
    {
        private SafeSharedAsn1StringHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }
    }

    internal sealed class SafeSharedAsn1IntegerHandle : SafeInteriorHandle
    {
        private SafeSharedAsn1IntegerHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }
    }
}
