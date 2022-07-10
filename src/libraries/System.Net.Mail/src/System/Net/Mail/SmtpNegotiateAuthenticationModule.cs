// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net.Mail
{
    internal sealed class SmtpNegotiateAuthenticationModule : ISmtpAuthenticationModule
    {
        private static byte[] _saslNoSecurtyLayerToken = new byte[] { 1, 0, 0, 0 };
        private readonly Dictionary<object, NTAuthentication> _sessions = new Dictionary<object, NTAuthentication>();

        internal SmtpNegotiateAuthenticationModule()
        {
        }

        public Authorization? Authenticate(string? challenge, NetworkCredential? credential, object sessionCookie, string? spn, ChannelBinding? channelBindingToken)
        {
            try
            {
                lock (_sessions)
                {
                    NTAuthentication? clientContext;
                    if (!_sessions.TryGetValue(sessionCookie, out clientContext))
                    {
                        if (credential == null)
                        {
                            return null;
                        }

                        ContextFlagsPal contextFlags = ContextFlagsPal.Connection | ContextFlagsPal.InitIntegrity;
                        // Workaround for https://github.com/gssapi/gss-ntlmssp/issues/77
                        // GSSAPI NTLM SSP does not support gss_wrap/gss_unwrap unless confidentiality
                        // is negotiated.
                        if (OperatingSystem.IsLinux())
                        {
                            contextFlags |= ContextFlagsPal.Confidentiality;
                        }

                        _sessions[sessionCookie] =
                            clientContext =
                            new NTAuthentication(false, "Negotiate", credential, spn,
                                                 contextFlags, channelBindingToken);
                    }

                    byte[]? byteResp;
                    string? resp = null;

                    if (!clientContext.IsCompleted)
                    {

                        // If auth is not yet completed keep producing
                        // challenge responses with GetOutgoingBlob

                        byte[]? decodedChallenge = null;
                        if (challenge != null)
                        {
                            decodedChallenge =
                                Convert.FromBase64String(challenge);
                        }
                        byteResp = clientContext.GetOutgoingBlob(decodedChallenge, false);
                        if (clientContext.IsCompleted && byteResp == null)
                        {
                            resp = "\r\n";
                        }
                        if (byteResp != null)
                        {
                            resp = Convert.ToBase64String(byteResp);
                        }
                    }
                    else
                    {
                        // If auth completed and still have a challenge then
                        // server may be doing "correct" form of GSSAPI SASL.
                        // Validate incoming and produce outgoing SASL security
                        // layer negotiate message.

                        resp = GetSecurityLayerOutgoingBlob(challenge, clientContext);
                    }

                    return new Authorization(resp, clientContext.IsCompleted);
                }
            }
            // From reflected type NTAuthentication in System.Net.Security.
            catch (NullReferenceException)
            {
                return null;
            }
        }

        public string AuthenticationType
        {
            get
            {
                return "gssapi";
            }
        }

        public void CloseContext(object sessionCookie)
        {
            NTAuthentication? clientContext = null;
            lock (_sessions)
            {
                if (_sessions.TryGetValue(sessionCookie, out clientContext))
                {
                    _sessions.Remove(sessionCookie);
                }
            }
            clientContext?.CloseContext();
        }

        // Function for SASL security layer negotiation after
        // authorization completes.
        //
        // Returns null for failure, Base64 encoded string on
        // success.
        private static string? GetSecurityLayerOutgoingBlob(string? challenge, NTAuthentication clientContext)
        {
            // must have a security layer challenge

            if (challenge == null)
                return null;

            // "unwrap" challenge

            byte[] input = Convert.FromBase64String(challenge);

            int len;
            int newOffset;
            Span<byte> unwrappedChallenge;

            try
            {
                len = clientContext.Unwrap(input, out newOffset, out _);
                unwrappedChallenge = input.AsSpan(newOffset, len);
            }
            catch (Win32Exception)
            {
                // any decrypt failure is an auth failure
                return null;
            }

            // Per RFC 2222 Section 7.2.2:
            //   the client should then expect the server to issue a
            //   token in a subsequent challenge.  The client passes
            //   this token to GSS_Unwrap and interprets the first
            //   octet of cleartext as a bit-mask specifying the
            //   security layers supported by the server and the
            //   second through fourth octets as the maximum size
            //   output_message to send to the server.
            // Section 7.2.3
            //   The security layer and their corresponding bit-masks
            //   are as follows:
            //     1 No security layer
            //     2 Integrity protection
            //       Sender calls GSS_Wrap with conf_flag set to FALSE
            //     4 Privacy protection
            //       Sender calls GSS_Wrap with conf_flag set to TRUE
            //
            // Exchange 2007 and our client only support
            // "No security layer". We verify that the server offers
            // option to use no security layer and negotiate that if
            // possible.

            if (unwrappedChallenge.Length != 4 || (unwrappedChallenge[0] & 1) != 1)
            {
                return null;
            }

            // Continuing with RFC 2222 section 7.2.2:
            //   The client then constructs data, with the first octet
            //   containing the bit-mask specifying the selected security
            //   layer, the second through fourth octets containing in
            //   network byte order the maximum size output_message the client
            //   is able to receive, and the remaining octets containing the
            //   authorization identity.
            //
            // So now this contructs the "wrapped" response.

            // let MakeSignature figure out length of output
            byte[]? output = null;
            try
            {
                len = clientContext.Wrap(_saslNoSecurtyLayerToken, ref output, false);
            }
            catch (Win32Exception)
            {
                // any encrypt failure is an auth failure
                return null;
            }

            // return Base64 encoded string of signed payload
            return Convert.ToBase64String(output, 0, len);
        }
    }
}
