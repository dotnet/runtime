// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Net;
using System.Collections;
using System.ComponentModel;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Xml;
using System.Threading;
using System.Security.Cryptography.X509Certificates;

namespace System.DirectoryServices.Protocols
{
    public partial class LdapConnection
    {
        private void InternalInitConnectionHandle(string hostname)
        {
            OpenLDAP.ldap_initialize(out IntPtr ldapServerHandle, $"ldap://{hostname}:{((LdapDirectoryIdentifier)_directoryIdentifier).PortNumber}/");
            _ldapHandle = new ConnectionHandle(ldapServerHandle, _needDispose);
        }

        private int InternalConnectToServer()
        {
            Debug.Assert(!_ldapHandle.IsInvalid);
            // In Linux you don't have to call Connect after calling init. You directly call bind.
            return 0;
        }

        private int InternalBind(NetworkCredential tempCredential, SEC_WINNT_AUTH_IDENTITY_EX cred, BindMethod method)
        {
            int error;
            if (tempCredential == null && AuthType == AuthType.External)
            {
                error = OpenLDAP.ldap_simple_bind(_ldapHandle, null, null);
            }
            else
            {
                error = OpenLDAP.ldap_simple_bind(_ldapHandle, cred.user, cred.password);
                if (error != 0x00)
                {
                    IntPtr mssg = IntPtr.Zero;
                    OpenLDAP.ldap_get_option_ptr(_ldapHandle, LdapOption.LDAP_OPT_ERROR_STRING, ref mssg);
                    string message = Marshal.PtrToStringAnsi(mssg);
                    throw new LdapException(message);
                }
            }

            return error;
        }

        private static string PtrToString(IntPtr requestName) => Marshal.PtrToStringAnsi(requestName);

        private IntPtr StringToPtr(string s) => Marshal.StringToHGlobalAnsi(s);
    }
}
