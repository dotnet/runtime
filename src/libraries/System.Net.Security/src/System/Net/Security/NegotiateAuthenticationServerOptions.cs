// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;

namespace System.Net.Security
{
    /// <summary>
    /// Represents a property bag for server-side of an authentication exchange.
    /// </summary>
    /// <remarks>
    /// This property bag is used as argument for <see cref="NegotiateAuthentication" />
    /// constructor for initializing a server-side authentication.
    /// </remarks>
    public class NegotiateAuthenticationServerOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NegotiateAuthenticationServerOptions" /> class.
        /// </summary>
        public NegotiateAuthenticationServerOptions()
        {
        }

        /// <summary>
        /// Specifies the GSSAPI authentication package used for the authentication.
        /// Common values are Negotiate, NTLM or Kerberos. Default value is Negotiate.
        /// </summary>
        /// <remarks>
        /// Specifying Kerberos or NTLM restricts authentication to that protocol.
        /// NTLM is a legacy authentication protocol and should be used only when required for
        /// compatibility with existing systems. Negotiate prefers Kerberos when available but
        /// can fall back to NTLM. To prevent NTLM, request Kerberos explicitly when supported
        /// or use another modern authentication protocol.
        /// </remarks>
        public string Package { get; set; } = NegotiationInfoClass.Negotiate;

        /// <summary>
        /// The NetworkCredential that is used to establish the identity of the client.
        /// Default value is CredentialCache.DefaultNetworkCredentials.
        /// </summary>
        public NetworkCredential Credential { get; set; } = CredentialCache.DefaultNetworkCredentials;

        /// <summary>
        /// Gets or sets the channel binding used for extended protection.
        /// </summary>
        /// <remarks>
        /// On Windows, if this property is <see langword="null" />, the
        /// <see cref="ExtendedProtectionPolicy.CustomChannelBinding" /> specified by <see cref="Policy" /> is used.
        /// </remarks>
        public ChannelBinding? Binding { get; set; }

        /// <summary>
        /// Indicates the required level of protection of the authentication exchange
        /// and any further data exchange. Default value is None.
        /// </summary>
        public ProtectionLevel RequiredProtectionLevel { get; set; } = ProtectionLevel.None;

        /// <summary>
        /// Indicates extended security and validation policies.
        /// </summary>
        /// <remarks>
        /// On Linux, setting <see cref="ExtendedProtectionPolicy.PolicyEnforcement" /> to
        /// <see cref="PolicyEnforcement.Always" /> with a non-null <see cref="Binding" /> requires
        /// the GSSAPI provider to report <c>GSS_C_CHANNEL_BOUND_FLAG</c> after authentication.
        /// MIT Kerberos supports this flag starting with version 1.19; other providers must also
        /// support reporting it. Authentication fails with <see cref="NegotiateAuthenticationStatusCode.BadBinding" />
        /// when matching channel bindings cannot be confirmed, including when an older provider
        /// does not report the flag even for matching bindings.
        /// </remarks>
        public ExtendedProtectionPolicy? Policy { get; set; }

        /// <summary>
        /// One of the <see cref="TokenImpersonationLevel" /> values, indicating how the server
        /// can use the client's credentials to access resources.
        /// </summary>
        public TokenImpersonationLevel RequiredImpersonationLevel { get; set; } = TokenImpersonationLevel.None;
    }
}
