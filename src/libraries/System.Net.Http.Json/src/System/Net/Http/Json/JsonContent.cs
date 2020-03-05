using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
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

        // Is this the declared or the runtime type?
        // if it is the declared type, then is weird that this does not honor the type passed-in to the constructor.
        // if it is the runtime type, then is weird that this does not honor the T type in the Create method.
        public Type ObjectType { get; }

        public object? Value { get; }

        public JsonContent(Type type, object? value, JsonSerializerOptions? options = null)
            : this(type, value, new MediaTypeHeaderValue(JsonMediaType), options) { }

        public JsonContent(Type type, object? value, string mediaType, JsonSerializerOptions? options = null)
            : this(type, value, new MediaTypeHeaderValue(mediaType), options) { }

        // What if someone passes a weird Content-Type?
        // Should we set mediaType.CharSet = UTF-8?
        public JsonContent(Type type, object? value, MediaTypeHeaderValue mediaType, JsonSerializerOptions? options = null)
            : this(JsonSerializer.SerializeToUtf8Bytes(value, type, options), type, value, mediaType) { }

        private JsonContent(byte[] content, Type type, object? value, MediaTypeHeaderValue mediaType)
        {
            _content = content;
            _offset = 0;
            _count = content.Length;

            Value = value;
            ObjectType = type;
            Headers.ContentType = mediaType;
        }

        public static JsonContent Create<T>(T value, JsonSerializerOptions? options = null)
            => Create(value, new MediaTypeHeaderValue(JsonMediaType), options);

        public static JsonContent Create<T>(T value, string mediaType, JsonSerializerOptions? options = null)
            => Create(value, new MediaTypeHeaderValue(mediaType), options);

        public static JsonContent Create<T>(T value, MediaTypeHeaderValue mediaType, JsonSerializerOptions? options = null)
            => new JsonContent(JsonSerializer.SerializeToUtf8Bytes(value, options), typeof(T), value, new MediaTypeHeaderValue(JsonMediaType));

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
            throw new NotImplementedException();
        }
    }
}
