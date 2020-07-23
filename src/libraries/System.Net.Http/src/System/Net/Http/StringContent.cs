// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public class StringContent : ByteArrayContent
    {
        private const string DefaultMediaType = "text/plain";

        public StringContent(string content)
            : this(content, null, null)
        {
        }

        public StringContent(string content, Encoding? encoding)
            : this(content, encoding, null)
        {
        }

        public StringContent(string content, Encoding? encoding, string? mediaType)
            : base(GetContentByteArray(content, encoding))
        {
            // Initialize the 'Content-Type' header with information provided by parameters.
            MediaTypeHeaderValue headerValue = new MediaTypeHeaderValue((mediaType == null) ? DefaultMediaType : mediaType);
            headerValue.CharSet = (encoding == null) ? HttpContent.DefaultStringEncoding.WebName : encoding.WebName;

            Headers.ContentType = headerValue;
        }

        // A StringContent is essentially a ByteArrayContent. We serialize the string into a byte-array in the
        // constructor using encoding information provided by the caller (if any). When this content is sent, the
        // Content-Length can be retrieved easily (length of the array).
        private static byte[] GetContentByteArray(string content, Encoding? encoding)
        {
            // In this case we treat 'null' strings different from string.Empty in order to be consistent with our
            // other *Content constructors: 'null' throws, empty values are allowed.
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (encoding == null)
            {
                encoding = HttpContent.DefaultStringEncoding;
            }

            return encoding.GetBytes(content);
        }

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
