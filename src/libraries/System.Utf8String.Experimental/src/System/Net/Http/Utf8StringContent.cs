// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public sealed class Utf8StringContent : HttpContent
    {
        private const string DefaultMediaType = "text/plain";

        private readonly Utf8String _content;

        public Utf8StringContent(Utf8String content)
            : this(content, mediaType: null)
        {
        }

        public Utf8StringContent(Utf8String content, string mediaType)
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

        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            return Task.FromResult<Stream>(new Utf8StringStream(_content));
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            return stream.WriteAsync(_content.AsMemoryBytes()).AsTask();
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _content.Length;
            return true;
        }
    }
}
