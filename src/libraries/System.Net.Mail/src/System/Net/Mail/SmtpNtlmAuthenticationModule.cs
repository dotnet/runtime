// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Security;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net.Mail
{
    internal sealed class SmtpNtlmAuthenticationModule : ISmtpAuthenticationModule
    {
        private readonly Dictionary<object, NegotiateAuthentication> _sessions = new Dictionary<object, NegotiateAuthentication>();

        internal SmtpNtlmAuthenticationModule()
        {
        }

        public Authorization? Authenticate(string? challenge, NetworkCredential? credential, object sessionCookie, string? spn, ChannelBinding? channelBindingToken)
        {
            lock (_sessions)
            {
                NegotiateAuthentication? clientContext;
                if (!_sessions.TryGetValue(sessionCookie, out clientContext))
                {
                    if (credential == null)
                    {
                        return null;
                    }

                    _sessions[sessionCookie] = clientContext =
                        new NegotiateAuthentication(
                            new NegotiateAuthenticationClientOptions
                            {
                                Credential = credential,
                                TargetName = spn,
                                Binding = channelBindingToken
                            });
                }

                NegotiateAuthenticationStatusCode statusCode;
                string? resp = clientContext.GetOutgoingBlob(challenge, out statusCode);

                if (statusCode != NegotiateAuthenticationStatusCode.Completed &&
                    statusCode != NegotiateAuthenticationStatusCode.ContinueNeeded)
                {
                    return null;
                }

                if (!clientContext.IsAuthenticated)
                {
                    return new Authorization(resp, false);
                }
                else
                {
                    _sessions.Remove(sessionCookie);
                    clientContext.Dispose();
                    return new Authorization(resp, true);
                }
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
