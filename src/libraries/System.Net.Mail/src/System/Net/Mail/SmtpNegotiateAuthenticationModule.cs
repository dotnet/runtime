// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Security;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net.Mail
{
    internal sealed class SmtpNegotiateAuthenticationModule : ISmtpAuthenticationModule
    {
        private static byte[] _saslNoSecurtyLayerToken = new byte[] { 1, 0, 0, 0 };
        private readonly Dictionary<object, NegotiateAuthentication> _sessions = new Dictionary<object, NegotiateAuthentication>();

        internal SmtpNegotiateAuthenticationModule()
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

                    ProtectionLevel protectionLevel = ProtectionLevel.Sign;
                    // Workaround for https://github.com/gssapi/gss-ntlmssp/issues/77
                    // GSSAPI NTLM SSP does not support gss_wrap/gss_unwrap unless confidentiality
                    // is negotiated.
                    if (OperatingSystem.IsLinux())
                    {
                        protectionLevel = ProtectionLevel.EncryptAndSign;
                    }

                    _sessions[sessionCookie] = clientContext =
                        new NegotiateAuthentication(
                            new NegotiateAuthenticationClientOptions
                            {
                                Credential = credential,
                                TargetName = spn,
                                RequiredProtectionLevel = protectionLevel,
                                Binding = channelBindingToken
                            });
                }

                string? resp = null;
                NegotiateAuthenticationStatusCode statusCode;

                if (!clientContext.IsAuthenticated)
                {
                    // If auth is not yet completed keep producing
                    // challenge responses with GetOutgoingBlob
                    resp = clientContext.GetOutgoingBlob(challenge, out statusCode);
                    if (statusCode != NegotiateAuthenticationStatusCode.Completed &&
                        statusCode != NegotiateAuthenticationStatusCode.ContinueNeeded)
                    {
                        return null;
                    }
                    if (clientContext.IsAuthenticated && resp == null)
                    {
                        resp = "\r\n";
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

                return new Authorization(resp, clientContext.IsAuthenticated);
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
            NegotiateAuthentication? clientContext = null;
            lock (_sessions)
            {
                if (_sessions.TryGetValue(sessionCookie, out clientContext))
                {
                    _sessions.Remove(sessionCookie);
                }
            }
            clientContext?.Dispose();
        }

        // Function for SASL security layer negotiation after
        // authorization completes.
        //
        // Returns null for failure, Base64 encoded string on
        // success.
        private static string? GetSecurityLayerOutgoingBlob(string? challenge, NegotiateAuthentication clientContext)
        {
            // must have a security layer challenge

            if (challenge == null)
                return null;

            // "unwrap" challenge

            byte[] input = Convert.FromBase64String(challenge);

            Span<byte> unwrappedChallenge;
            NegotiateAuthenticationStatusCode statusCode;

            statusCode = clientContext.UnwrapInPlace(input, out int newOffset, out int newLength, out _);
            if (statusCode != NegotiateAuthenticationStatusCode.Completed)
            {
                return null;
            }
            unwrappedChallenge = input.AsSpan(newOffset, newLength);

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
            // So now this constructs the "wrapped" response.

            // let MakeSignature figure out length of output
            ArrayBufferWriter<byte> outputWriter = new ArrayBufferWriter<byte>();
            statusCode = clientContext.Wrap(_saslNoSecurtyLayerToken, outputWriter, false, out _);
            if (statusCode != NegotiateAuthenticationStatusCode.Completed)
            {
                return null;
            }

            // return Base64 encoded string of signed payload
            return Convert.ToBase64String(outputWriter.WrittenSpan);
        }
    }
}
