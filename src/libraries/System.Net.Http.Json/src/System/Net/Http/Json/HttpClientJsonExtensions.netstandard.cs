// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    public static partial class HttpClientJsonExtensions
    {
        private static HttpMethod HttpPatch => s_httpPatch ??= new HttpMethod("PATCH");
        private static HttpMethod? s_httpPatch;

        private static Task<HttpResponseMessage> PatchAsync(this HttpClient client, string? requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            Uri? uri = string.IsNullOrEmpty(requestUri) ? null : new Uri(requestUri, UriKind.RelativeOrAbsolute);
            return client.PatchAsync(uri, content, cancellationToken);
        }

        private static Task<HttpResponseMessage> PatchAsync(this HttpClient client, Uri? requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            // HttpClient.PatchAsync is not available in .NET standard and NET462
            HttpRequestMessage request = new HttpRequestMessage(HttpPatch, requestUri) { Content = content };
            return client.SendAsync(request, cancellationToken);
        }
    }
}
