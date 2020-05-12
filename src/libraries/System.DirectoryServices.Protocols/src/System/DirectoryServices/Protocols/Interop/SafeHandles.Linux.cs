// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;

namespace System.DirectoryServices.Protocols
{
    internal sealed class ConnectionHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal bool _needDispose = false;

        internal ConnectionHandle()
            :base(true)
        {
            Interop.ldap_initialize(out handle, null);
            _needDispose = true;
        }

        internal ConnectionHandle(IntPtr value, bool disposeHandle) : base(true)
        {
            _needDispose = disposeHandle;
            if (value == IntPtr.Zero)
            {
                throw new LdapException(SR.LDAP_CONNECT_ERROR);
            }
            else
            {
                SetHandle(value);
            }
        }

        protected override bool ReleaseHandle()
        {
            if (_needDispose)
            {
                IntPtr nullPointer = IntPtr.Zero;
                Interop.ldap_unbind_ext_s(handle, ref nullPointer, ref nullPointer);
            }

            handle = IntPtr.Zero;
            return true;
        }
    }

    internal sealed class SafeBerHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeBerHandle() : base(true)
        {
            SetHandle(Interop.ber_alloc(1));
            if (handle == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }
        }

        internal SafeBerHandle(berval value) : base(true)
        {
            // In Linux if bv_val is null ber_init will segFault instead of returning IntPtr.Zero.
            // In Linux if bv_len is 0 ber_init returns a valid pointer which will then fail when trying to use it,
            // so we fail early by throwing exception if this is the case.
            if (value.bv_val == IntPtr.Zero || value.bv_len == 0)
            {
                throw new BerConversionException();
            }
            SetHandle(Interop.ber_init(value));
            if (handle == IntPtr.Zero)
            {
                throw new BerConversionException();
            }
        }

        protected override bool ReleaseHandle()
        {
            Interop.ber_free(handle, 1);
            return true;
        }
    }
}
