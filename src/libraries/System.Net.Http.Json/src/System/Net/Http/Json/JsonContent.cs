// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    /// <summary>
    /// TODO
    /// </summary>
    public class JsonContent : HttpContent
    {
        private const string JsonMediaType = "application/json";

        private readonly byte[] _content;
        private readonly int _offset;
        private readonly int _count;

        private static MediaTypeHeaderValue CreateMediaType(string mediaTypeAsString)
        {
            //MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue(mediaTypeAsString); // this one is used by the Formatting API.
            MediaTypeHeaderValue mediaType = MediaTypeHeaderValue.Parse(mediaTypeAsString);

            // If the instantiated mediaType does not specify its CharSet, set UTF-8 by default.
            if (mediaType.CharSet == null)
            {
                mediaType.CharSet = Encoding.UTF8.WebName;
            }

            return mediaType;
        }

        // When Create<T> is callled, this is the typeof(T).
        // When .ctor is called, this is the specified type argument.
        // As per Formatting, this is always the declared type.
        public Type ObjectType { get; }

        public object? Value { get; }

        public JsonContent(Type type, object? value, JsonSerializerOptions? options = null)
            : this(type, value, CreateMediaType(JsonMediaType), options) { }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="mediaType">The authoritative value of the request's content's Content-Type header. Can be <c>null</c> in which case the default content type will be used.</param>
        /// <param name="options"></param>
        public JsonContent(Type type, object? value, string mediaType, JsonSerializerOptions? options = null)
            : this(type, value, CreateMediaType(mediaType?? throw new ArgumentNullException(nameof(mediaType))), options) { }

        // What if someone passes a weird Content-Type?
        // Should we set mediaType.CharSet = UTF-8?
        // Formatting allows it.
        public JsonContent(Type type, object? value, MediaTypeHeaderValue mediaType, JsonSerializerOptions? options = null)
            : this(JsonSerializer.SerializeToUtf8Bytes(value, type, options), type, value, mediaType ?? throw new ArgumentNullException(nameof(mediaType))) { }

        private JsonContent(byte[] content, Type type, object? value, MediaTypeHeaderValue mediaType)
        {
            if (mediaType == null)
            {
                throw new ArgumentNullException(nameof(mediaType));
            }

            _content = content;
            _offset = 0;
            _count = content.Length;

            Value = value;
            ObjectType = type;
            Headers.ContentType = mediaType;
        }

        public static JsonContent Create<T>(T value, JsonSerializerOptions? options = null)
            => Create(value, CreateMediaType(JsonMediaType), options);

        public static JsonContent Create<T>(T value, string mediaType, JsonSerializerOptions? options = null)
            => Create(value, CreateMediaType(mediaType ?? throw new ArgumentNullException(nameof(mediaType))), options);

        public static JsonContent Create<T>(T value, MediaTypeHeaderValue mediaType, JsonSerializerOptions? options = null)
            => new JsonContent(JsonSerializer.SerializeToUtf8Bytes(value, options), typeof(T), value, mediaType ?? throw new ArgumentNullException(nameof(mediaType)));

        /// <summary>
        /// Serialize the HTTP content to a stream as an asynchronous operation.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            => stream.WriteAsync(_content, _offset, _count);

        // Should this method even exist? or we just call WriteAsync from above method without cancellationToken?
        // UPDATE: This does not exists on netstandard
        // protected override Task SerializeToStreamAsync(Stream stream, TransportContext context, CancellationToken cancellationToken)
        //    => stream.WriteAsync(_content, _offset, _count, cancellationToken);


        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        protected override bool TryComputeLength(out long length)
        {
            length = _count;
            return true;
        }
    }
}
