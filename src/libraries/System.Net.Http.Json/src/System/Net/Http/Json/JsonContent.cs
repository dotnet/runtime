// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    public sealed partial class JsonContent : HttpContent
    {
        private readonly JsonTypeInfo _typeInfo;
        public Type ObjectType => _typeInfo.Type;
        public object? Value { get; }

        private JsonContent(
            object? inputValue,
            JsonTypeInfo jsonTypeInfo,
            MediaTypeHeaderValue? mediaType)
        {
            Debug.Assert(jsonTypeInfo is not null);
            Debug.Assert(inputValue is null || jsonTypeInfo.Type.IsAssignableFrom(inputValue.GetType()));

            Value = inputValue;
            _typeInfo = jsonTypeInfo;
            Headers.ContentType = mediaType ?? JsonHelpers.GetDefaultMediaType();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="JsonContent"/> class that will contain the <paramref name="inputValue"/> serialized as JSON.
        /// </summary>
        /// <typeparam name="T">The type of the value to serialize.</typeparam>
        /// <param name="inputValue">The value to serialize.</param>
        /// <param name="mediaType">The media type to use for the content.</param>
        /// <param name="options">Options to control the behavior during serialization, the default options are <see cref="JsonSerializerDefaults.Web"/>.</param>
        /// <returns>A <see cref="JsonContent"/> instance.</returns>
        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static JsonContent Create<T>(T inputValue, MediaTypeHeaderValue? mediaType = null, JsonSerializerOptions? options = null)
            => Create(inputValue, JsonHelpers.GetJsonTypeInfo(typeof(T), options), mediaType);

        /// <summary>
        /// Creates a new instance of the <see cref="JsonContent"/> class that will contain the <paramref name="inputValue"/> serialized as JSON.
        /// </summary>
        /// <param name="inputValue">The value to serialize.</param>
        /// <param name="inputType">The type of the value to serialize.</param>
        /// <param name="mediaType">The media type to use for the content.</param>
        /// <param name="options">Options to control the behavior during serialization, the default options are <see cref="JsonSerializerDefaults.Web"/>.</param>
        /// <returns>A <see cref="JsonContent"/> instance.</returns>
        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static JsonContent Create(object? inputValue, Type inputType, MediaTypeHeaderValue? mediaType = null, JsonSerializerOptions? options = null)
        {
            ThrowHelper.ThrowIfNull(inputType);
            EnsureTypeCompatibility(inputValue, inputType);

            return new JsonContent(inputValue, JsonHelpers.GetJsonTypeInfo(inputType, options), mediaType);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="JsonContent"/> class that will contain the <paramref name="inputValue"/> serialized as JSON.
        /// </summary>
        /// <typeparam name="T">The type of the value to serialize.</typeparam>
        /// <param name="inputValue">The value to serialize.</param>
        /// <param name="jsonTypeInfo">The JsonTypeInfo used to control the serialization behavior.</param>
        /// <param name="mediaType">The media type to use for the content.</param>
        /// <returns>A <see cref="JsonContent"/> instance.</returns>
        public static JsonContent Create<T>(T? inputValue, JsonTypeInfo<T> jsonTypeInfo, MediaTypeHeaderValue? mediaType = null)
        {
            ThrowHelper.ThrowIfNull(jsonTypeInfo);

            return new JsonContent(inputValue, jsonTypeInfo, mediaType);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="JsonContent"/> class that will contain the <paramref name="inputValue"/> serialized as JSON.
        /// </summary>
        /// <param name="inputValue">The value to serialize.</param>
        /// <param name="jsonTypeInfo">The JsonTypeInfo used to control the serialization behavior.</param>
        /// <param name="mediaType">The media type to use for the content.</param>
        /// <returns>A <see cref="JsonContent"/> instance.</returns>
        public static JsonContent Create(object? inputValue, JsonTypeInfo jsonTypeInfo, MediaTypeHeaderValue? mediaType = null)
        {
            ThrowHelper.ThrowIfNull(jsonTypeInfo);
            EnsureTypeCompatibility(inputValue, jsonTypeInfo.Type);

            return new JsonContent(inputValue, jsonTypeInfo, mediaType);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            => SerializeToStreamAsyncCore(stream, CancellationToken.None);

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        private Task SerializeToStreamAsyncCore(Stream targetStream, CancellationToken cancellationToken)
        {
            Encoding? targetEncoding = JsonHelpers.GetEncoding(this);

            return targetEncoding != null && targetEncoding != Encoding.UTF8
                ? SerializeToStreamAsyncTranscoding(targetStream, async: true, targetEncoding, cancellationToken)
                : JsonSerializer.SerializeAsync(targetStream, Value, _typeInfo, cancellationToken);
        }

        private async Task SerializeToStreamAsyncTranscoding(Stream targetStream, bool async, Encoding targetEncoding, CancellationToken cancellationToken)
        {
            // Wrap provided stream into a transcoding stream that buffers the data transcoded from utf-8 to the targetEncoding.
#if NETCOREAPP
            Stream transcodingStream = Encoding.CreateTranscodingStream(targetStream, targetEncoding, Encoding.UTF8, leaveOpen: true);
            try
            {
                if (async)
                {
                    await JsonSerializer.SerializeAsync(transcodingStream, Value, _typeInfo, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    JsonSerializer.Serialize(transcodingStream, Value, _typeInfo);
                }
            }
            finally
            {
                // Dispose/DisposeAsync will flush any partial write buffers. In practice our partial write
                // buffers should be empty as we expect JsonSerializer to emit only well-formed UTF-8 data.
                if (async)
                {
                    await transcodingStream.DisposeAsync().ConfigureAwait(false);
                }
                else
                {
                    transcodingStream.Dispose();
                }
            }
#else
            Debug.Assert(async, "HttpContent synchronous serialization is only supported since .NET 5.0");

            using (TranscodingWriteStream transcodingStream = new TranscodingWriteStream(targetStream, targetEncoding))
            {
                await JsonSerializer.SerializeAsync(transcodingStream, Value, _typeInfo, cancellationToken).ConfigureAwait(false);
                // The transcoding streams use Encoders and Decoders that have internal buffers. We need to flush these
                // when there is no more data to be written. Stream.FlushAsync isn't suitable since it's
                // acceptable to Flush a Stream (multiple times) prior to completion.
                await transcodingStream.FinalWriteAsync(cancellationToken).ConfigureAwait(false);
            }
#endif
        }

        private static void EnsureTypeCompatibility(object? inputValue, Type inputType)
        {
            if (inputValue is not null && !inputType.IsAssignableFrom(inputValue.GetType()))
            {
                throw new ArgumentException(SR.Format(SR.SerializeWrongType, inputType, inputValue.GetType()));
            }
        }
    }
}
