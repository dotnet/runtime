// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;

namespace System.Net.Security
{
    /// <summary>
    /// Represents a property bag for client-side of an authentication exchange.
    /// </summary>
    public class NegotiateAuthenticationClientOptions
    {
        /// <summary>
        /// Specifies the GSSAPI authentication package used for the authentication.
        /// Common values are Negotiate, NTLM or Kerberos. Default value is Negotiate.
        /// </summary>
        public string Package { get; set; } = NegotiationInfoClass.Negotiate;

        /// <summary>
        /// The NetworkCredential that is used to establish the identity of the client.
        /// Default value is CredentialCache.DefaultNetworkCredentials.
        /// </summary>
        public NetworkCredential Credential { get; set; } = CredentialCache.DefaultNetworkCredentials;

        /// <summary>
        /// The Service Principal Name (SPN) that uniquely identifies the server to authenticate.
        /// </summary>
        public string? TargetName { get; set; }

        /// <summary>
        /// Channel binding that is used for extended protection.
        /// </summary>
        public ChannelBinding? Binding { get; set; }

        /// <summary>
        /// Indicates the required level of protection of the authentication exchange
        /// and any further data exchange. Default value is None.
        /// </summary>
        public ProtectionLevel RequiredProtectionLevel { get; set; } = ProtectionLevel.None;

        /// <summary>
        /// Indicates that mutual authentication is required between the client and server.
        /// </summary>
        public bool RequireMutualAuthentication { get; set; }

        /// <summary>
        /// One of the <see cref="TokenImpersonationLevel" /> values, indicating how the server
        /// can use the client's credentials to access resources.
        /// </summary>
        public TokenImpersonationLevel AllowedImpersonationLevel { get; set; } = TokenImpersonationLevel.None;
    }
}
