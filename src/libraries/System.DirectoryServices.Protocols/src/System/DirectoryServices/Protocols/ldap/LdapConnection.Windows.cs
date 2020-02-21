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
            // User wants to setup a connectionless session with server.
            if (((LdapDirectoryIdentifier)_directoryIdentifier).Connectionless == true)
            {
                _ldapHandle = new ConnectionHandle(Wldap32.cldap_open(hostname, ((LdapDirectoryIdentifier)_directoryIdentifier).PortNumber), _needDispose);
            }
            else
            {
                _ldapHandle = new ConnectionHandle(Wldap32.ldap_init(hostname, ((LdapDirectoryIdentifier)_directoryIdentifier).PortNumber), _needDispose);
            }
        }

        private int InternalConnectToServer()
        {
            // Connect explicitly to the server.
            var timeout = new LDAP_TIMEVAL()
            {
                tv_sec = (int)(_connectionTimeOut.Ticks / TimeSpan.TicksPerSecond)
            };
            Debug.Assert(!_ldapHandle.IsInvalid);
            int error = Wldap32.ldap_connect(_ldapHandle, timeout);
            return error;
        }

        private int InternalBind(NetworkCredential tempCredential, SEC_WINNT_AUTH_IDENTITY_EX cred, BindMethod method)
        {
            int error;
            if (tempCredential == null && AuthType == AuthType.External)
            {
                error = Wldap32.ldap_bind_s(_ldapHandle, null, null, method);
            }
            else
            {
                error = Wldap32.ldap_bind_s(_ldapHandle, null, cred, method);
            }

            return error;
        }

        private static string PtrToString(IntPtr requestName) => Marshal.PtrToStringUni(requestName);

        private IntPtr StringToPtr(string s) => Marshal.StringToHGlobalUni(s);
    }
}
