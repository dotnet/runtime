// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http
{
    /// <summary>
    /// Determines behavior when selecting and negotiating HTTP version for a request.
    /// </summary>
    public enum HttpVersionPolicy
    {
        /// <summary>
        /// Default behavior, either uses requested version or downgrades to a lower one.
        /// </summary>
        /// <remarks>
        /// If the server supports the requested version, either negotiated via ALPN (H2) or advertised via Alt-Svc (H3),
        /// as well as a secure connection is being requested, the result is the <see cref="HttpRequestMessage.Version" />.
        /// Otherwise, downgrades to HTTP/1.1.
        /// Note that this option does not allow use of prenegotiated clear text connection, e.g. H2C.
        /// </remarks>
        RequestVersionOrLower,

        /// <summary>
        /// Tries to uses highest available version, downgrading only to the requested version, not bellow.
        /// Throwing <see cref="HttpRequestException" /> if a connection with higher or equal version cannot be established.
        /// </summary>
        /// <remarks>
        /// If the server supports higher than requested version, either negotiated via ALPN (H2) or advertised via Alt-Svc (H3),
        /// as well as secure connection is being requested, the result is the highest available one.
        /// Otherwise, downgrades to the <see cref="HttpRequestMessage.Version" />.
        /// Note that this option allows to use prenegotiated clear text connection for the requested version but not for anything higher.
        /// </remarks>
        RequestVersionOrHigher,

        /// <summary>
        /// Uses only the requested version.
        /// Throwing <see cref="HttpRequestException" /> if a connection with the exact version cannot be established.
        /// </summary>
        /// <remarks>
        /// Note that this option allows to use prenegotiated clear text connection for the requested version.
        /// </remarks>
        RequestVersionExact
    }
}
