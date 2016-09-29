// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Security.Permissions;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net.Mail
{
    internal class SmtpLoginAuthenticationModule : ISmtpAuthenticationModule
    {
        private Hashtable _sessions = new Hashtable();

        internal SmtpLoginAuthenticationModule()
        {
        }

        #region ISmtpAuthenticationModule Members

        // Security this method will access NetworkCredential properties that demand UnmanagedCode and Environment Permission
        [EnvironmentPermission(SecurityAction.Assert, Unrestricted = true)]
        [SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public Authorization Authenticate(string challenge, NetworkCredential credential, object sessionCookie, string spn, ChannelBinding channelBindingToken)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.Web, this, "Authenticate", null);
            try
            {
                lock (_sessions)
                {
                    NetworkCredential cachedCredential = _sessions[sessionCookie] as NetworkCredential;
                    if (cachedCredential == null)
                    {
                        if (credential == null || credential is SystemNetworkCredential)
                        {
                            return null;
                        }

                        _sessions[sessionCookie] = credential;

                        string userName = credential.UserName;
                        string domain = credential.Domain;

                        if (domain != null && domain.Length > 0)
                        {
                            userName = domain + "\\" + userName;
                        }

                        return new Authorization(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(userName)), false);
                    }
                    else
                    {
                        _sessions.Remove(sessionCookie);

                        return new Authorization(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(cachedCredential.Password)), true);
                    }
                }
            }
            finally
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.Web, this, "Authenticate", null);
            }
        }

        public string AuthenticationType
        {
            get
            {
                return "login";
            }
        }

        public void CloseContext(object sessionCookie)
        {
            // This is a no-op since the context is not
            // kept open by this module beyond auth completion.
        }

        #endregion
    }
}
