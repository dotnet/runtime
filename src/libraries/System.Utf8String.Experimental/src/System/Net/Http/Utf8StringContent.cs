// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public sealed partial class Utf8StringContent : HttpContent
    {
        private const string DefaultMediaType = "text/plain";

        private readonly Utf8String _content;

        public Utf8StringContent(Utf8String content)
            : this(content, mediaType: null)
        {
        }

        public Utf8StringContent(Utf8String content, string? mediaType)
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            _content = content;

            // Initialize the 'Content-Type' header with information provided by parameters.

            Headers.ContentType = new MediaTypeHeaderValue(mediaType ?? DefaultMediaType)
            {
                CharSet = "utf-8" // Encoding.UTF8.WebName
            };
        }

        protected override Task<Stream> CreateContentReadStreamAsync() =>
            Task.FromResult<Stream>(new Utf8StringStream(_content));

#if (NETSTANDARD2_0 || NETFRAMEWORK)
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            ReadOnlyMemory<byte> buffer = _content.AsMemoryBytes();
            if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> array))
            {
                await stream.WriteAsync(array.Array, array.Offset, array.Count).ConfigureAwait(false);
            }
            else
            {
                byte[] localBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
                buffer.Span.CopyTo(localBuffer);

                await stream.WriteAsync(localBuffer, 0, buffer.Length).ConfigureAwait(false);

                ArrayPool<byte>.Shared.Return(localBuffer);
            }
        }
#elif NETSTANDARD2_1 || NETCOREAPP3_0
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            stream.WriteAsync(_content.AsMemoryBytes()).AsTask();
#else
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            SerializeToStreamAsync(stream, context, default);

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken) =>
            stream.WriteAsync(_content.AsMemoryBytes(), cancellationToken).AsTask();
#endif

        protected override bool TryComputeLength(out long length)
        {
            length = _content.Length;
            return true;
        }
    }
}
