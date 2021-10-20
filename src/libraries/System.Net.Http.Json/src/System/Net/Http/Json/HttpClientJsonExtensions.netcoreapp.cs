// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    public static partial class HttpClientJsonExtensions
    {
        private static Task<HttpResponseMessage> PatchAsync(HttpClient client, Uri? requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            return client.PatchAsync(requestUri, content, cancellationToken);
        }

        private static Task<HttpResponseMessage> PatchAsync(HttpClient client, string? requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            return client.PatchAsync(requestUri, content, cancellationToken);
        }
    }
}
