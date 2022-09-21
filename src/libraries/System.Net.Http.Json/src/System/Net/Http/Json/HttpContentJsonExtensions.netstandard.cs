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
        private static Task<Stream> ReadHttpContentStreamAsync(HttpContent content, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<Stream>(cancellationToken);
            }

            // The ReadAsStreamAsync overload that takes a cancellationToken is not available in .NET Standard
            return content.ReadAsStreamAsync();
        }

        private static Stream GetTranscodingStream(Stream contentStream, Encoding sourceEncoding)
        {
            return new TranscodingReadStream(contentStream, sourceEncoding);
        }
    }
}
