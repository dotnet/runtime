// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Permissions;

namespace System.Net.Mail
{
    internal class SmtpNtlmAuthenticationModule : ISmtpAuthenticationModule
    {
        private Dictionary<object, NTAuthentication> _sessions = new Dictionary<object, NTAuthentication>();

        internal SmtpNtlmAuthenticationModule()
        {
        }

        // Security this method will access NetworkCredential properties that demand UnmanagedCode and Environment Permission
        [EnvironmentPermission(SecurityAction.Assert, Unrestricted = true)]
        [SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public Authorization Authenticate(string challenge, NetworkCredential credential, object sessionCookie, string spn, ChannelBinding channelBindingToken)
        {
            NetEventSource.Enter(this, "Authenticate");
            try
            {
                lock (_sessions)
                {
                    NTAuthentication clientContext;
                    if (!_sessions.TryGetValue(sessionCookie, out clientContext))
                    {
                        if (credential == null)
                        {
                            return null;
                        }

                        _sessions[sessionCookie] =
                            clientContext =
                            new NTAuthentication(false, "Ntlm", credential, spn, ContextFlags.Connection, channelBindingToken);

                    }

                    string resp = clientContext.GetOutgoingBlob(challenge);

                    if (!clientContext.IsCompleted)
                    {
                        return new Authorization(resp, false);
                    }
                    else
                    {
                        _sessions.Remove(sessionCookie);
                        return new Authorization(resp, true);
                    }
                }
            }
            finally
            {
                NetEventSource.Exit(this, "Authenticate");
            }
        }

        public string AuthenticationType
        {
            get
            {
                return "ntlm";
            }
        }

        public void CloseContext(object sessionCookie)
        {
            // This is a no-op since the context is not
            // kept open by this module beyond auth completion.
        }
    }
}
