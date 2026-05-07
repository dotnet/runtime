// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    public static partial class HttpContentJsonExtensions
    {
        private static async Task<Stream> ReadHttpContentStreamAsync(HttpContent content, CancellationToken cancellationToken)
        {
            return await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        }

        private static Stream GetTranscodingStream(Stream contentStream, Encoding sourceEncoding)
        {
            return Encoding.CreateTranscodingStream(contentStream, innerStreamEncoding: sourceEncoding, outerStreamEncoding: Encoding.UTF8);
        }
    }
}
