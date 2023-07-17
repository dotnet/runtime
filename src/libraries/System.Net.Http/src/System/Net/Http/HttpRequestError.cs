// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http
{
    /// <summary>
    /// Defines error categories representing the reason for <see cref="HttpRequestException"/> or <see cref="HttpIOException"/>.
    /// </summary>
    public enum HttpRequestError
    {
        /// <summary>
        /// A generic or unknown error occured.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The DNS name resolution failed.
        /// </summary>
        NameResolutionError,                    // DNS request failed

        /// <summary>
        /// A transport-level failure occured while connecting to the remote endpoint.
        /// </summary>
        ConnectionError,

        /// <summary>
        /// An error occured during the TLS handshake.
        /// </summary>
        SecureConnectionError,

        /// <summary>
        /// An HTTP/2 or HTTP/3 protocol error occured.
        /// </summary>
        HttpProtocolError,                      // HTTP 2.0/3.0 protocol error occurred

        /// <summary>
        /// Extended CONNECT for WebSockets over HTTP/2 is not supported by the peer.
        /// </summary>
        ExtendedConnectNotSupported,

        /// <summary>
        /// Cannot negotiate the HTTP Version requested.
        /// </summary>
        VersionNegotiationError,

        /// <summary>
        /// The authentication failed with the provided credentials.
        /// </summary>
        UserAuthenticationError,

        /// <summary>
        /// An error occured while establishing a connection to the proxy tunnel.
        /// </summary>
        ProxyTunnelError,

        /// <summary>
        /// An invalid or malformed response has been received.
        /// </summary>
        InvalidResponse,                        // General error in response/malformed response

        /// <summary>
        /// The response ended prematurely.
        /// </summary>
        ResponseEnded,

        /// <summary>
        /// The response exceeded a pre-configured limit such as <see cref="HttpClient.MaxResponseContentBufferSize"/> or <see cref="HttpClientHandler.MaxResponseHeadersLength"/>.
        /// </summary>
        ConfigurationLimitExceeded,
    }
}
