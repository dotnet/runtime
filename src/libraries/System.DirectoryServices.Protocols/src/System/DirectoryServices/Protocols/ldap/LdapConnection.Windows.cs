// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;

namespace System.DirectoryServices.Protocols
{
    public partial class LdapConnection
    {
        private bool _setFQDNDone;

        private void InternalInitConnectionHandle(string hostname)
        {
            LdapDirectoryIdentifier directoryIdentifier = _directoryIdentifier as LdapDirectoryIdentifier;

            // User wants to setup a connectionless session with server.
            if (directoryIdentifier.Connectionless)
            {
                _ldapHandle = new ConnectionHandle(Interop.Ldap.cldap_open(hostname, directoryIdentifier.PortNumber), _needDispose);
            }
            else
            {
                _ldapHandle = new ConnectionHandle(Interop.Ldap.ldap_init(hostname, directoryIdentifier.PortNumber), _needDispose);
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
            return Interop.Ldap.ldap_connect(_ldapHandle, timeout);
        }

        private int InternalBind(NetworkCredential tempCredential, SEC_WINNT_AUTH_IDENTITY_EX cred, BindMethod method)
            => tempCredential == null && AuthType == AuthType.External ? Interop.Ldap.ldap_bind_s(_ldapHandle, null, null, method) : Interop.Ldap.ldap_bind_s(_ldapHandle, null, cred, method);
    }
}
