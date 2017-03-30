// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    using System;
    using Microsoft.Win32.SafeHandles;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Diagnostics;
    using System.Security;

    [SuppressUnmanagedCodeSecurityAttribute()]
    internal sealed class BerSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal BerSafeHandle() : base(true)
        {
            SetHandle(Wldap32.ber_alloc(1));
            if (handle == (IntPtr)0)
                throw new OutOfMemoryException();
        }

        internal BerSafeHandle(berval value) : base(true)
        {
            SetHandle(Wldap32.ber_init(value));
            if (handle == (IntPtr)0)
                throw new BerConversionException();
        }

        override protected bool ReleaseHandle()
        {
            Wldap32.ber_free(handle, 1);
            return true;
        }
    }

    [SuppressUnmanagedCodeSecurityAttribute()]
    sealed internal class HGlobalMemHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal HGlobalMemHandle(IntPtr value) : base(true)
        {
            SetHandle(value);
        }

        override protected bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(handle);
            return true;
        }
    }

    [SuppressUnmanagedCodeSecurityAttribute()]
    sealed internal class ConnectionHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal bool needDispose = false;
        internal ConnectionHandle() : base(true)
        {
            SetHandle(Wldap32.ldap_init(null, 389));

            if (handle == (IntPtr)0)
            {
                int error = Wldap32.LdapGetLastError();
                if (Utility.IsLdapError((LdapError)error))
                {
                    string errorMessage = LdapErrorMappings.MapResultCode(error);
                    throw new LdapException(error, errorMessage);
                }
                else
                    throw new LdapException(error);
            }
        }

        internal ConnectionHandle(IntPtr value, bool disposeHandle)
            : base(true)
        {
            needDispose = disposeHandle;
            if (value == (IntPtr)0)
            {
                int error = Wldap32.LdapGetLastError();
                if (Utility.IsLdapError((LdapError)error))
                {
                    string errorMessage = LdapErrorMappings.MapResultCode(error);
                    throw new LdapException(error, errorMessage);
                }
                else
                    throw new LdapException(error);
            }
            else
            {
                SetHandle(value);
            }
        }
        override protected bool ReleaseHandle()
        {
            if (handle != (IntPtr)0)
            {
                if (needDispose)
                {
                    Wldap32.ldap_unbind(handle);
                }
                handle = (IntPtr)0;
            }
            return true;
        }
    }
}
