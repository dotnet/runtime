// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace System.DirectoryServices.Protocols
{
    public partial class LdapConnection
    {
        // Linux doesn't support setting FQDN so we mark the flag as if it is already set so we don't make a call to set it again.
        private bool _setFQDNDone = true;

        private void InternalInitConnectionHandle()
        {
            if ((LdapDirectoryIdentifier)_directoryIdentifier == null)
            {
                throw new NullReferenceException();
            }

            _ldapHandle = new ConnectionHandle();
        }

        private int InternalConnectToServer()
        {
            // In Linux you don't have to call Connect after calling init. You
            // directly call bind. However, we set the URI for the connection
            // here instead of during initialization because we need access to
            // the SessionOptions property to properly define it, which is not
            // available during init.
            Debug.Assert(!_ldapHandle.IsInvalid);

            string scheme;
            LdapDirectoryIdentifier directoryIdentifier = (LdapDirectoryIdentifier)_directoryIdentifier;
            if (directoryIdentifier.Connectionless)
            {
                scheme = "cldap://";
            }
            else if (SessionOptions.SecureSocketLayer)
            {
                scheme = "ldaps://";
            }
            else
            {
                scheme = "ldap://";
            }

            string uris = null;
            string[] servers = directoryIdentifier.Servers;
            if (servers != null && servers.Length != 0)
            {
                StringBuilder temp = new StringBuilder(200);
                for (int i = 0; i < servers.Length; i++)
                {
                    if (i != 0)
                    {
                        temp.Append(' ');
                    }
                    temp.Append(scheme);
                    temp.Append(servers[i]);
                    if (!servers[i].Contains(':'))
                    {
                        temp.Append(':');
                        temp.Append(directoryIdentifier.PortNumber);
                    }
                }
                if (temp.Length != 0)
                {
                    uris = temp.ToString();
                }
            }
            else
            {
                uris = $"{scheme}:{directoryIdentifier.PortNumber}";
            }

            return LdapPal.SetStringOption(_ldapHandle, LdapOption.LDAP_OPT_URI, uris);
        }

        private int InternalBind(NetworkCredential tempCredential, SEC_WINNT_AUTH_IDENTITY_EX cred, BindMethod method)
        {
            int error;

            if (LocalAppContextSwitches.UseBasicAuthFallback)
            {
                if (tempCredential == null && (AuthType == AuthType.External || AuthType == AuthType.Kerberos))
                {
                    error = BindSasl();
                }
                else
                {
                    error = LdapPal.BindToDirectory(_ldapHandle, cred.user, cred.password);
                }
            }
            else
            {
                if (method == BindMethod.LDAP_AUTH_NEGOTIATE)
                {
                    if (tempCredential == null)
                    {
                        error = BindSasl();
                    }
                    else
                    {
                        // Explicit credentials were provided.  If we call ldap_bind_s it will
                        // return LDAP_NOT_SUPPORTED, so just skip the P/Invoke.
                        error = (int)LdapError.NotSupported;
                    }
                }
                else
                {
                    // Basic and Anonymous are handled elsewhere.
                    Debug.Assert(AuthType != AuthType.Anonymous && AuthType != AuthType.Basic);
                    error = (int)LdapError.AuthUnknown;
                }
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
