// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net;

namespace System.DirectoryServices.Protocols
{
    public partial class LdapConnection
    {
        // Linux doesn't support setting FQDN so we mark the flag as if it is already set so we don't make a call to set it again.
        private bool _setFQDNDone = true;

        private void InternalInitConnectionHandle(string hostname) => _ldapHandle = new ConnectionHandle(Interop.ldap_init(hostname, ((LdapDirectoryIdentifier)_directoryIdentifier).PortNumber), _needDispose);

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
                error = Interop.ldap_simple_bind(_ldapHandle, null, null);
            }
            else
            {
                error = Interop.ldap_simple_bind(_ldapHandle, cred.user, cred.password);
            }

            return error;
        }
    }
}
