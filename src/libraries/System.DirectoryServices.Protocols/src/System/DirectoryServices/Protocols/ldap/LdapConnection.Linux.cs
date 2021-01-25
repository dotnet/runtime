// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;

namespace System.DirectoryServices.Protocols
{
    public partial class LdapConnection
    {
        // Linux doesn't support setting FQDN so we mark the flag as if it is already set so we don't make a call to set it again.
        private bool _setFQDNDone = true;

        private void InternalInitConnectionHandle(string hostname) => _ldapHandle = new ConnectionHandle(Interop.Ldap.ldap_init(hostname, ((LdapDirectoryIdentifier)_directoryIdentifier).PortNumber), _needDispose);

        private int InternalConnectToServer()
        {
            Debug.Assert(!_ldapHandle.IsInvalid);
            // In Linux you don't have to call Connect after calling init. You directly call bind.
            return 0;
        }

        private int InternalBind(NetworkCredential tempCredential, SEC_WINNT_AUTH_IDENTITY_EX cred, BindMethod method)
        {
            int error;
            if (tempCredential == null && (AuthType == AuthType.External || AuthType == AuthType.Kerberos))
            {
                error = BindSasl();
            }
            else
            {
                error = Interop.Ldap.ldap_simple_bind(_ldapHandle, cred.user, cred.password);
            }

            return error;
        }

        private int BindSasl()
        {
            SaslDefaultCredentials defaults = GetSaslDefaults();
            IntPtr ptrToDefaults = Marshal.AllocHGlobal(Marshal.SizeOf(defaults));
            Marshal.StructureToPtr(defaults, ptrToDefaults, false);
            try
            {
                return Interop.Ldap.ldap_sasl_interactive_bind(_ldapHandle, null, Interop.KerberosDefaultMechanism, IntPtr.Zero, IntPtr.Zero, Interop.LDAP_SASL_QUIET, LdapPal.SaslInteractionProcedure, ptrToDefaults);
            }
            finally
            {
                GC.KeepAlive(defaults); //Making sure we keep it in scope as we will still use ptrToDefaults
                Marshal.FreeHGlobal(ptrToDefaults);
            }
        }

        private SaslDefaultCredentials GetSaslDefaults()
        {
            var defaults = new SaslDefaultCredentials { mech = Interop.KerberosDefaultMechanism };
            IntPtr outValue = IntPtr.Zero;
            int error = Interop.Ldap.ldap_get_option_ptr(_ldapHandle, LdapOption.LDAP_OPT_X_SASL_REALM, ref outValue);
            if (error == 0 && outValue != IntPtr.Zero)
            {
                defaults.realm = Marshal.PtrToStringAnsi(outValue);
            }
            error = Interop.Ldap.ldap_get_option_ptr(_ldapHandle, LdapOption.LDAP_OPT_X_SASL_AUTHCID, ref outValue);
            if (error == 0 && outValue != IntPtr.Zero)
            {
                defaults.authcid = Marshal.PtrToStringAnsi(outValue);
            }
            error = Interop.Ldap.ldap_get_option_ptr(_ldapHandle, LdapOption.LDAP_OPT_X_SASL_AUTHZID, ref outValue);
            if (error == 0 && outValue != IntPtr.Zero)
            {
                defaults.authzid = Marshal.PtrToStringAnsi(outValue);
            }
            return defaults;
        }
    }
}
