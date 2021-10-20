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
            // The PatchAsync overload is not available in .NET standard and NET462
            HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("PATCH"), requestUri) { Content = content };
            return client.SendAsync(request, cancellationToken);
        }

        private static Task<HttpResponseMessage> PatchAsync(HttpClient client, string? requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            Uri? uri = string.IsNullOrEmpty(requestUri) ? null : new Uri(requestUri, UriKind.RelativeOrAbsolute);
            return PatchAsync(client, uri, content, cancellationToken);
        }
    }
}
