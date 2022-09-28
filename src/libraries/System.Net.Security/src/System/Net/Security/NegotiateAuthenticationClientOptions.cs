// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;

namespace System.Net.Security
{
    /// <summary>
    /// Represents a property bag for client-side of an authentication exchange.
    /// </summary>
    /// <remarks>
    /// This property bag is used as argument for <see cref="NegotiateAuthentication" />
    /// constructor for initializing a client-side authentication.
    ///
    /// Initial values of the properties are set for an authentication using
    /// default network credentials. If you want to explicitly authenticate using a user
    /// name, password and domain combination then set the <see cref="Credential" />
    /// property appropriately.
    ///
    /// Typical usage of the client-side authentication will also require specifying the
    /// the <see cref="TargetName" /> property. While it may be omitted in some scenarios
    /// it is usually required to be set to a valid value like <c>HOST/contoso.com</c> or
    /// <c>HTTP/www.contoso.com</c>.
    ///
    /// When the authentication is wrapped in a secure channel, like TLS, the channel
    /// binding can provide additional protection by strongly binding the authentication
    /// to a given transport channel. This is handled by setting the <see cref="Binding" />
    /// property. For <see cref="SslStream" /> the channel binding could be obtained
    /// through the <see cref="SslStream.TransportContext" /> property and calling the
    /// <see cref="TransportContext.GetChannelBinding" /> method.
    /// </remarks>
    public class NegotiateAuthenticationClientOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NegotiateAuthenticationClientOptions" /> class.
        /// </summary>
        public NegotiateAuthenticationClientOptions()
        {
        }

        /// <summary>
        /// Specifies the GSSAPI authentication package used for the authentication.
        /// Common values are Negotiate, NTLM or Kerberos. Default value is Negotiate.
        /// </summary>
        public string Package { get; set; } = NegotiationInfoClass.Negotiate;

        /// <summary>
        /// The NetworkCredential that is used to establish the identity of the client.
        /// Default value is <see cref="CredentialCache.DefaultNetworkCredentials" />.
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
