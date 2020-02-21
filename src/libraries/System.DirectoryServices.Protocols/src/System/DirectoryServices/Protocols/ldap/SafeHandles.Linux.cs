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
            OpenLDAP.ldap_initialize(out IntPtr ldap, null);
            SetHandle(ldap);
        }

        internal ConnectionHandle(IntPtr value, bool disposeHandle) : base(true)
        {
            _needDispose = disposeHandle;
            if (value == IntPtr.Zero)
            {
                throw new LdapException("There was an error when attempting to initialize a connection with the LDAP server");
            }
            else
            {
                SetHandle(value);
            }
        }
        protected override bool ReleaseHandle()
        {
            if (handle != IntPtr.Zero)
            {
                if (_needDispose)
                {
                    IntPtr nullPointer = IntPtr.Zero;
                    OpenLDAP.ldap_unbind_ext_s(handle, ref nullPointer, ref nullPointer);
                }

                handle = IntPtr.Zero;
            }
            return true;
        }
    }

    internal sealed class BerSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal BerSafeHandle() : base(true)
        {
            SetHandle(OpenLDAP.ber_alloc(1));
            if (handle == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }
        }

        internal BerSafeHandle(berval value) : base(true)
        {
            SetHandle(OpenLDAP.ber_init(value));
            if (handle == IntPtr.Zero)
            {
                throw new BerConversionException();
            }
        }

        protected override bool ReleaseHandle()
        {
            OpenLDAP.ber_free(handle, 1);
            return true;
        }
    }
}
