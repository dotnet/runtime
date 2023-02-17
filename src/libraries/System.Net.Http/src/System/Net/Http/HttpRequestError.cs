// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http
{
    public enum HttpRequestError
    {
        NameResolutionError,                    // DNS request failed
        ConnectionError,                        // Transport-level errors during connection
        TransportError,                         // Transport-level errors after connection
        SecureConnectionError,                  // SSL/TLS errors
        HttpProtocolError,                      // HTTP 2.0/3.0 protocol error occured

        ResponseEnded,                          // Received EOF
        InvalidResponse,                        // General error in response/malformed response
        InvalidResponseHeader,                  // Error with response headers
        ContentBufferSizeExceeded,              // Response Content size exceeded MaxResponseContentBufferSize
        ResponseHeaderExceededLengthLimit,      // Response Header length exceeded MaxResponseHeadersLength
        UnsupportedExtendedConnect,             // Extended CONNECT for WebSockets over HTTP/2 is not supported. (SETTINGS_ENABLE_CONNECT_PROTOCOL has not been sent).
        VersionNegotiationError,                // Cannot negotiate the HTTP Version requested
        AuthenticationError,
        SocksTunnelError,
    }
}
