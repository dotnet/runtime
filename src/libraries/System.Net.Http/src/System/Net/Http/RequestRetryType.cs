// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Http
{
    /// <summary>
    /// Used with <see cref="HttpRequestException"/> to indicate if a request is safe to retry.
    /// </summary>
    internal enum RequestRetryType
    {
        /// <summary>
        /// The request must not be retried; this indicates we aren't certain the server hasn't processed the request.
        /// </summary>
        NoRetry,

        /// <summary>
        /// The request failed on the current HTTP version, and the server requested it be retried on a lower version.
        /// </summary>
        RetryOnLowerHttpVersion,

        /// <summary>
        /// The request failed due to e.g. server shutting down (GOAWAY) and should be retried on a new connection.
        /// </summary>
        RetryOnSameOrNextProxy,

        /// <summary>
        /// The proxy failed, so the request should be retried on the next proxy.
        /// </summary>
        RetryOnNextProxy
    }
}
