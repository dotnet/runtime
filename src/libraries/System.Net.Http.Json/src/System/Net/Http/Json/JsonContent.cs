// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    public partial class JsonContent : HttpContent
    {
        internal const string JsonMediaType = "application/json";
        private readonly JsonSerializerOptions? _jsonSerializerOptions;

        private static MediaTypeHeaderValue CreateMediaTypeFromString(string mediaTypeAsString)
        {
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue(mediaTypeAsString);
            Debug.Assert(mediaType.CharSet == null);
            mediaType.CharSet = Encoding.UTF8.WebName;

            return mediaType;
        }

        public Type ObjectType { get; }

        public object? Value { get; }

        public JsonContent(Type type, object? value, JsonSerializerOptions? options = null)
            : this(type, value, CreateMediaTypeFromString(JsonMediaType), options) { }

        public JsonContent(Type type, object? value, string mediaType, JsonSerializerOptions? options = null)
            : this(type, value, CreateMediaTypeFromString(mediaType ?? throw new ArgumentNullException(nameof(mediaType))), options) { }

        public JsonContent(Type type, object? value, MediaTypeHeaderValue mediaType, JsonSerializerOptions? options = null)
        {
            if (mediaType == null)
            {
                throw new ArgumentNullException(nameof(mediaType));
            }

            // TODO: Support other charsets once https://github.com/dotnet/runtime/issues/30260 is done.
            if (mediaType.CharSet != Encoding.UTF8.WebName)
            {
                throw new NotSupportedException(SR.CharSetInvalid);
            }

            Value = value;
            ObjectType = type;
            Headers.ContentType = mediaType;
            // TODO: Set DefaultWebOptions if no options were provided.
            _jsonSerializerOptions = options;
        }

        public static JsonContent Create<T>(T value, JsonSerializerOptions? options = null)
            => Create(value, CreateMediaTypeFromString(JsonMediaType), options);

        public static JsonContent Create<T>(T value, string mediaType, JsonSerializerOptions? options = null)
            => Create(value, CreateMediaTypeFromString(mediaType ?? throw new ArgumentNullException(nameof(mediaType))), options);

        public static JsonContent Create<T>(T value, MediaTypeHeaderValue mediaType, JsonSerializerOptions? options = null)
            => new JsonContent(typeof(T), value, mediaType);

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
#if !NETSTANDARD2_0
            => SerializeToStreamAsync(stream, context, CancellationToken.None);
#else
            => JsonSerializer.SerializeAsync(stream, Value, ObjectType, _jsonSerializerOptions);
#endif

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
