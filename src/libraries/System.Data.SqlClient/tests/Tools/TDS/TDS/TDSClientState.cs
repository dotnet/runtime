// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS
{
    /// <summary>
    /// State of the client TDS parser
    /// </summary>
    public enum TDSClientState
    {
        /// <summary>
        /// Sent Initial PRELOGIN Packet State
        /// </summary>
        PreLoginSent,

        /// <summary>
        /// Sent TLS/SSL Negotiation Packet State
        /// </summary>
        SSLNegotiationSent,

        /// <summary>
        /// Sent LOGIN7 Record with Complete Authentication Token state
        /// </summary>
        CompleteLogin7Sent,

        /// <summary>
        /// Sent LOGIN7 Record with SPNEGO Packet State
        /// </summary>
        Login7SPNEGOSent,

        /// <summary>
        /// Sent LOGIN7 Record with Authentication information request.
        /// </summary>
        Login7FederatedAuthenticationInformationRequestSent,

        /// <summary>
        /// Logged In State
        /// </summary>
        LoggedIn,

        /// <summary>
        /// Sent Client Request State
        /// </summary>
        RequestSent,

        /// <summary>
        /// Sent Attention State
        /// </summary>
        AttentionSent,

        /// <summary>
        /// Indicates that a connection was re-routed to a different SQL Server and transport needs to be re-established
        /// </summary>
        ReConnect,

        /// <summary>
        /// Sent a final notification to the TDS Server
        /// </summary>
        LogoutSent,

        /// <summary>
        /// Final State
        /// </summary>
        Final
    }
}
