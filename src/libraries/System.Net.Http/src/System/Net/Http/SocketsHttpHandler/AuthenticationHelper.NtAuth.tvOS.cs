// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal static partial class AuthenticationHelper
    {
        private static Task<HttpResponseMessage> InnerSendAsync(HttpRequestMessage request, Uri authUri, bool async, ICredentials credentials, bool isProxyAuth, HttpConnection connection, HttpConnectionPool connectionPool, CancellationToken cancellationToken)
        {
            return isProxyAuth ?
                SendWithProxyAuthAsync(request, authUri, async, credentials, false, connectionPool, cancellationToken).AsTask() :
                SendWithRequestAuthAsync(request, async, credentials, false, connectionPool, cancellationToken).AsTask();
        }

        public static Task<HttpResponseMessage> SendWithNtProxyAuthAsync(HttpRequestMessage request, Uri proxyUri, bool async, ICredentials proxyCredentials, HttpConnection connection, HttpConnectionPool connectionPool, CancellationToken cancellationToken)
        {
            return InnerSendAsync(request, proxyUri, async, proxyCredentials, isProxyAuth: true, connection, connectionPool, cancellationToken);
        }

        public static Task<HttpResponseMessage> SendWithNtConnectionAuthAsync(HttpRequestMessage request, bool async, ICredentials credentials, HttpConnection connection, HttpConnectionPool connectionPool, CancellationToken cancellationToken)
        {
            Debug.Assert(request.RequestUri != null);
            return InnerSendAsync(request, request.RequestUri, async, credentials, isProxyAuth: false, connection, connectionPool, cancellationToken);
        }
    }
}