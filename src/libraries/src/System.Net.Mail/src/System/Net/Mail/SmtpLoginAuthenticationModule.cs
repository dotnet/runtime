// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Permissions;

namespace System.Net.Mail
{
    internal class SmtpLoginAuthenticationModule : ISmtpAuthenticationModule
    {
        private Dictionary<object, NetworkCredential> _sessions = new Dictionary<object, NetworkCredential>();

        internal SmtpLoginAuthenticationModule()
        {
        }

        // Security this method will access NetworkCredential properties that demand UnmanagedCode and Environment Permission
        [EnvironmentPermission(SecurityAction.Assert, Unrestricted = true)]
        [SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public Authorization Authenticate(string challenge, NetworkCredential credential, object sessionCookie, string spn, ChannelBinding channelBindingToken)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            try
            {
                lock (_sessions)
                {
                    NetworkCredential cachedCredential;
                    if (!_sessions.TryGetValue(sessionCookie, out cachedCredential))
                    {
                        if (credential == null || ReferenceEquals(credential, CredentialCache.DefaultNetworkCredentials))
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
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
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
    }
}
