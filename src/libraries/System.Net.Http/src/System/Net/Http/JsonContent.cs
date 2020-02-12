// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public class JsonContent : ByteArrayContent
    {
        private const string DefaultMediaType = "application/json";

        public JsonContent(object content)
            : this(content, null, null)
        {
        }

        public JsonContent(object content, string mediaType)
            : this(content, mediaType, null)
        {
        }

        public JsonContent(object content, JsonSerializerOptions options)
            : this(content, null, options)
        {
        }

        public JsonContent(object content, string mediaType, JsonSerializerOptions options)
            : base(GetContentByteArray(content, options))
        {
            // Initialize the 'Content-Type' header with information provided by parameters.
            MediaTypeHeaderValue headerValue = new MediaTypeHeaderValue((mediaType == null) ? DefaultMediaType : mediaType);
            headerValue.CharSet = Encoding.UTF8.WebName;

            Headers.ContentType = headerValue;
        }

        // A JsonContent is essentially a ByteArrayContent. We serialize the object into a byte-array in the
        // constructor using utf-8. When this content is sent, the Content-Length can be retrieved easily (length of the array).
        private static byte[] GetContentByteArray(object content, JsonSerializerOptions options)
        {
            return JsonSerializer.SerializeToUtf8Bytes(content, options);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context, CancellationToken cancellationToken) =>
            // Only skip the original protected virtual SerializeToStreamAsync if this
            // isn't a derived type that may have overridden the behavior.
            GetType() == typeof(JsonContent) ? SerializeToStreamAsyncCore(stream, cancellationToken) :
            base.SerializeToStreamAsync(stream, context, cancellationToken);

        internal override Stream TryCreateContentReadStream() =>
            GetType() == typeof(JsonContent) ? CreateMemoryStreamForByteArray() : // type check ensures we use possible derived type's CreateContentReadStreamAsync override
            null;
    }
}
