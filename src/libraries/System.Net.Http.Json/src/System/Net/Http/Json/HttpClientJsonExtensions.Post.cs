// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    public static partial class HttpClientJsonExtensions
    {
        public static Task<HttpResponseMessage> PostAsJsonAsync(
            this HttpClient client,
            string requestUri,
            Type type,
            object? value,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            throw null!;
        }
    }
}
