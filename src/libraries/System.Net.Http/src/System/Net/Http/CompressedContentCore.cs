// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    /// <summary>
    /// Shared helpers for the per-algorithm compressed content types. Provides header initialization,
    /// argument validation, and serialization that copies the inner content through a compression stream.
    /// </summary>
    internal static class CompressedContentCore
    {
        public static void ValidateCompressionLevel(CompressionLevel compressionLevel, string paramName)
        {
            // Validate up front so an invalid enum value fails fast at construction time rather than
            // deferring the exception to the serialization path when the request is being sent.
            if (compressionLevel is < CompressionLevel.Optimal or > CompressionLevel.SmallestSize)
            {
                throw new ArgumentOutOfRangeException(paramName);
            }
        }

        public static void InitializeHeaders(HttpContent target, HttpContent source, string encoding)
        {
            // Copy headers from the original content.
            target.Headers.AddHeaders(source.Headers);

            // Append our encoding to the Content-Encoding header (supports stacking per HTTP spec).
            // Always use TryAddWithoutValidation so we append the new encoding without re-parsing or
            // validating any existing Content-Encoding values the inner content may have added.
            target.Headers.TryAddWithoutValidation(KnownHeaders.ContentEncoding.Descriptor, encoding);

            // Remove Content-Length since we don't know the compressed size upfront.
            target.Headers.ContentLength = null;
        }

        public static void SerializeToStream(HttpContent originalContent, Stream compressionStream, TransportContext? context, CancellationToken cancellationToken)
        {
            using (compressionStream)
            {
                originalContent.CopyTo(compressionStream, context, cancellationToken);
            }
        }

        public static async Task SerializeToStreamAsync(HttpContent originalContent, Stream compressionStream, TransportContext? context, CancellationToken cancellationToken)
        {
            await using (compressionStream.ConfigureAwait(false))
            {
                await originalContent.CopyToAsync(compressionStream, context, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
