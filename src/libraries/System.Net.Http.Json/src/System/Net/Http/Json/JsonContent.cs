// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NETCOREAPP
using System.Diagnostics;
#endif
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
        private readonly JsonContentSerializer _serializer;

        public Type ObjectType => _serializer.ObjectType;
        public object? Value => _serializer.Value;

        private JsonContent(JsonContentSerializer serializer, MediaTypeHeaderValue? mediaType)
        {
            ThrowHelper.ThrowIfNull(serializer);

            _serializer = serializer;
            Headers.ContentType = mediaType ?? JsonHelpers.GetDefaultMediaType();
        }

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static JsonContent Create<T>(T inputValue, MediaTypeHeaderValue? mediaType = null, JsonSerializerOptions? options = null)
            => Create(inputValue, typeof(T), mediaType, options);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public static JsonContent Create(object? inputValue, Type inputType, MediaTypeHeaderValue? mediaType = null, JsonSerializerOptions? options = null)
            => new JsonContent(new JsonContentObjectSerializer(inputValue, inputType, options), mediaType);

        public static JsonContent Create<T>(T? inputValue, JsonTypeInfo<T> jsonTypeInfo,
            MediaTypeHeaderValue? mediaType = null)
        {
            JsonContentSerializer serializer = inputValue is not null
                ? new JsonContentSerializer<T>(inputValue, jsonTypeInfo)
                : new JsonContentTypeInfoSerializer(null, jsonTypeInfo);

            return new JsonContent(serializer, mediaType);
        }

        public static JsonContent Create(object? inputValue, JsonTypeInfo jsonTypeInfo, MediaTypeHeaderValue? mediaType = null)
            => new JsonContent(new JsonContentTypeInfoSerializer(inputValue, jsonTypeInfo), mediaType);

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            => SerializeToStreamAsyncCore(stream, async: true, CancellationToken.None);

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        private async Task SerializeToStreamAsyncCore(Stream targetStream, bool async, CancellationToken cancellationToken)
        {
            Encoding? targetEncoding = JsonHelpers.GetEncoding(this);

            // Wrap provided stream into a transcoding stream that buffers the data transcoded from utf-8 to the targetEncoding.
            if (targetEncoding != null && targetEncoding != Encoding.UTF8)
            {
#if NETCOREAPP
                Stream transcodingStream = Encoding.CreateTranscodingStream(targetStream, targetEncoding, Encoding.UTF8, leaveOpen: true);
                try
                {
                    if (async)
                    {
                        await _serializer.SerializeToStreamAsync(transcodingStream, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        _serializer.SerializeToStream(transcodingStream);
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
                Debug.Assert(async);

                using (TranscodingWriteStream transcodingStream = new TranscodingWriteStream(targetStream, targetEncoding))
                {
                    await _serializer.SerializeToStreamAsync(transcodingStream, cancellationToken).ConfigureAwait(false);
                    // The transcoding streams use Encoders and Decoders that have internal buffers. We need to flush these
                    // when there is no more data to be written. Stream.FlushAsync isn't suitable since it's
                    // acceptable to Flush a Stream (multiple times) prior to completion.
                    await transcodingStream.FinalWriteAsync(cancellationToken).ConfigureAwait(false);
                }
#endif
            }
            else
            {
                if (async)
                {
                    await _serializer.SerializeToStreamAsync(targetStream, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _serializer.SerializeToStream(targetStream);
                }
            }
        }

        private abstract class JsonContentSerializer
        {
            public abstract Type ObjectType { get; }
            public abstract object? Value { get; }

            public abstract Task SerializeToStreamAsync(Stream targetStream, CancellationToken cancellationToken);

            public abstract void SerializeToStream(Stream targetStream);
        }
    }
}
