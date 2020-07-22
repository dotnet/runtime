// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    public static partial class HttpContentJsonExtensions
    {
        private static Task<Stream> ReadHttpContentStreamAsync(HttpContent content, CancellationToken cancellationToken)
        {
            return content.ReadAsStreamAsync(cancellationToken);
        }

        private static Stream GetTranscodingStream(Stream contentStream, Encoding sourceEncoding)
        {
            return Encoding.CreateTranscodingStream(contentStream, innerStreamEncoding: sourceEncoding, outerStreamEncoding: Encoding.UTF8);
        }
    }
}
