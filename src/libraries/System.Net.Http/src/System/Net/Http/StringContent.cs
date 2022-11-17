// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    /// <summary>Provides HTTP content based on a string.</summary>
    public class StringContent : ByteArrayContent
    {
        /// <summary>The media type to use when none is specified.</summary>
        private const string DefaultMediaType = "text/plain";

        /// <summary>Creates a new instance of the <see cref="StringContent"/> class.</summary>
        /// <param name="content">The content used to initialize the <see cref="StringContent"/>.</param>
        /// <remarks>The media type for the <see cref="StringContent"/> created defaults to text/plain.</remarks>
        public StringContent(string content)
            : this(content, DefaultStringEncoding, DefaultMediaType)
        {
        }

        /// <summary>Creates a new instance of the <see cref="StringContent"/> class.</summary>
        /// <param name="content">The content used to initialize the <see cref="StringContent"/>.</param>
        /// <param name="mediaType">The media type to use for the content.</param>
        public StringContent(string content, MediaTypeHeaderValue mediaType)
            : this(content, DefaultStringEncoding, mediaType)
        {
        }

        /// <summary>Creates a new instance of the <see cref="StringContent"/> class.</summary>
        /// <param name="content">The content used to initialize the <see cref="StringContent"/>.</param>
        /// <param name="encoding">The encoding to use for the content.</param>
        /// <remarks>The media type for the <see cref="StringContent"/> created defaults to text/plain.</remarks>
        public StringContent(string content, Encoding? encoding)
            : this(content, encoding, DefaultMediaType)
        {
        }

        /// <summary>Creates a new instance of the <see cref="StringContent"/> class.</summary>
        /// <param name="content">The content used to initialize the <see cref="StringContent"/>.</param>
        /// <param name="encoding">The encoding to use for the content.</param>
        /// <param name="mediaType">The media type to use for the content.</param>
        public StringContent(string content, Encoding? encoding, string mediaType)
            : this(content, encoding, new MediaTypeHeaderValue(mediaType, (encoding ?? DefaultStringEncoding).WebName))
        {
        }

        /// <summary>Creates a new instance of the <see cref="StringContent"/> class.</summary>
        /// <param name="content">The content used to initialize the <see cref="StringContent"/>.</param>
        /// <param name="encoding">The encoding to use for the content.</param>
        /// <param name="mediaType">The media type to use for the content.</param>
        public StringContent(string content, Encoding? encoding, MediaTypeHeaderValue mediaType)
            : base(GetContentByteArray(content, encoding))
        {
            Headers.ContentType = mediaType;
        }

        /// <summary>Serialize the string into a byte-array using encoding information provided by the caller (if any).</summary>
        /// <param name="content">The content used to initialize the <see cref="StringContent"/>.</param>
        /// <param name="encoding">The encoding to use for the content.</param>
        /// <returns>The serialized byte array.</returns>
        private static byte[] GetContentByteArray(string content, Encoding? encoding)
        {
            ArgumentNullException.ThrowIfNull(content);

            // In this case we treat 'null' strings differently from string.Empty in order to be consistent with our
            // other *Content constructors: 'null' throws, empty values are allowed.

            return (encoding ?? DefaultStringEncoding).GetBytes(content);
        }

        /// <summary>Serialize and write the byte array provided in the constructor to an HTTP content stream as an asynchronous operation.</summary>
        /// <param name="stream">The target stream.</param>
        /// <param name="context">Information about the transport, like channel binding token. This parameter may be <see langword="null" />.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken) =>
            // Only skip the original protected virtual SerializeToStreamAsync if this
            // isn't a derived type that may have overridden the behavior.
            GetType() == typeof(StringContent) ? SerializeToStreamAsyncCore(stream, cancellationToken) :
            base.SerializeToStreamAsync(stream, context, cancellationToken);

        internal override Stream? TryCreateContentReadStream() =>
            GetType() == typeof(StringContent) ? CreateMemoryStreamForByteArray() : // type check ensures we use possible derived type's CreateContentReadStreamAsync override
            null;
    }
}
