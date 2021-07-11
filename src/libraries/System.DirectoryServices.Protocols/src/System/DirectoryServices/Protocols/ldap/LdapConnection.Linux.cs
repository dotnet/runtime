// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Runtime.InteropServices;

namespace System.DirectoryServices.Protocols
{
    public partial class LdapConnection
    {
        // Linux doesn't support setting FQDN so we mark the flag as if it is already set so we don't make a call to set it again.
        private bool _setFQDNDone = true;
        internal bool _startTls;
        internal DirectoryControlCollection _startTlsControls;

        private void InternalInitConnectionHandle(string hostname)
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

            string scheme = null;
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
                    temp.Append(':');
                    temp.Append(directoryIdentifier.PortNumber);
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

            int error = LdapPal.SetStringOption(_ldapHandle, LdapOption.LDAP_OPT_URI, uris);

            // LdapPal.StartTls() must be called after setting the URI.
            if (error == 0 && _startTls)
            {
                error = StartTls();
            }
            return error;
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
                error = LdapPal.BindToDirectory(_ldapHandle, cred.user, cred.password);
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

        private unsafe int StartTls()
        {

            IntPtr serverControlArray = IntPtr.Zero;
            LdapControl[] managedServerControls = null;
            IntPtr clientControlArray = IntPtr.Zero;
            LdapControl[] managedClientControls = null;
            IntPtr referral = IntPtr.Zero;


            try
            {
                IntPtr tempPtr = IntPtr.Zero;

                // build server control
                managedServerControls = BuildControlArray(_startTlsControls, true);
                int structSize = Marshal.SizeOf(typeof(LdapControl));
                if (managedServerControls != null)
                {
                    serverControlArray = Utility.AllocHGlobalIntPtrArray(managedServerControls.Length + 1);
                    for (int i = 0; i < managedServerControls.Length; i++)
                    {
                        IntPtr controlPtr = Marshal.AllocHGlobal(structSize);
                        Marshal.StructureToPtr(managedServerControls[i], controlPtr, false);
                        tempPtr = (IntPtr)((long)serverControlArray + IntPtr.Size * i);
                        Marshal.WriteIntPtr(tempPtr, controlPtr);
                    }

                    tempPtr = (IntPtr)((long)serverControlArray + IntPtr.Size * managedServerControls.Length);
                    Marshal.WriteIntPtr(tempPtr, IntPtr.Zero);
                }

                // Build client control.
                managedClientControls = BuildControlArray(_startTlsControls, false);
                if (managedClientControls != null)
                {
                    clientControlArray = Utility.AllocHGlobalIntPtrArray(managedClientControls.Length + 1);
                    for (int i = 0; i < managedClientControls.Length; i++)
                    {
                        IntPtr controlPtr = Marshal.AllocHGlobal(structSize);
                        Marshal.StructureToPtr(managedClientControls[i], controlPtr, false);
                        tempPtr = (IntPtr)((long)clientControlArray + IntPtr.Size * i);
                        Marshal.WriteIntPtr(tempPtr, controlPtr);
                    }

                    tempPtr = (IntPtr)((long)clientControlArray + IntPtr.Size * managedClientControls.Length);
                    Marshal.WriteIntPtr(tempPtr, IntPtr.Zero);
                }

                int error = LdapPal.StartTls(_ldapHandle, serverControlArray, clientControlArray);
                if (error != (int)ResultCode.Success)
                {
                    if (Utility.IsResultCode((ResultCode)error))
                    {
                        // Parse the referral.
                        Uri[] responseReferral = null;

                        string errorMessage = OperationErrorMappings.MapResultCode(error);
                        ExtendedResponse response = new ExtendedResponse(null, null, (ResultCode)error, errorMessage, responseReferral);
                        response.ResponseName = "1.3.6.1.4.1.1466.20037";
                        throw new TlsOperationException(response);
                    }
                    else if (LdapErrorMappings.IsLdapError(error))
                    {
                        string errorMessage = LdapErrorMappings.MapResultCode(error);
                        throw new LdapException(error, errorMessage);
                    }
                }

                return error;
            }
            finally
            {
                if (serverControlArray != IntPtr.Zero)
                {
                    // Release the memory from the heap.
                    for (int i = 0; i < managedServerControls.Length; i++)
                    {
                        IntPtr tempPtr = Marshal.ReadIntPtr(serverControlArray, IntPtr.Size * i);
                        if (tempPtr != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(tempPtr);
                        }
                    }
                    Marshal.FreeHGlobal(serverControlArray);
                }

                if (managedServerControls != null)
                {
                    for (int i = 0; i < managedServerControls.Length; i++)
                    {
                        if (managedServerControls[i].ldctl_oid != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(managedServerControls[i].ldctl_oid);
                        }

                        if (managedServerControls[i].ldctl_value != null)
                        {
                            if (managedServerControls[i].ldctl_value.bv_val != IntPtr.Zero)
                            {
                                Marshal.FreeHGlobal(managedServerControls[i].ldctl_value.bv_val);
                            }
                        }
                    }
                }

                if (clientControlArray != IntPtr.Zero)
                {
                    // Release the memory from the heap.
                    for (int i = 0; i < managedClientControls.Length; i++)
                    {
                        IntPtr tempPtr = Marshal.ReadIntPtr(clientControlArray, IntPtr.Size * i);
                        if (tempPtr != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(tempPtr);
                        }
                    }

                    Marshal.FreeHGlobal(clientControlArray);
                }

                if (managedClientControls != null)
                {
                    for (int i = 0; i < managedClientControls.Length; i++)
                    {
                        if (managedClientControls[i].ldctl_oid != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(managedClientControls[i].ldctl_oid);
                        }

                        if (managedClientControls[i].ldctl_value != null)
                        {
                            if (managedClientControls[i].ldctl_value.bv_val != IntPtr.Zero)
                                Marshal.FreeHGlobal(managedClientControls[i].ldctl_value.bv_val);
                        }
                    }
                }

                if (referral != IntPtr.Zero)
                {
                    LdapPal.FreeValue(referral);
                }
            }
        }
    }
}
