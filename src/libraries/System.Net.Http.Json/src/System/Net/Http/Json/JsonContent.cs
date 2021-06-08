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
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    public sealed partial class JsonContent : HttpContent
    {
        private readonly JsonSerializerOptions? _jsonSerializerOptions;
        public Type ObjectType { get; }
        public object? Value { get; }

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        private JsonContent(
            object? inputValue,
            Type inputType,
            MediaTypeHeaderValue? mediaType,
            JsonSerializerOptions? options)
        {
            if (inputType == null)
            {
                throw new ArgumentNullException(nameof(inputType));
            }

            if (inputValue != null && !inputType.IsAssignableFrom(inputValue.GetType()))
            {
                throw new ArgumentException(SR.Format(SR.SerializeWrongType, inputType, inputValue.GetType()));
            }

            Value = inputValue;
            ObjectType = inputType;
            Headers.ContentType = mediaType ?? JsonHelpers.GetDefaultMediaType();
            _jsonSerializerOptions = options ?? JsonHelpers.s_defaultSerializerOptions;
        }

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        public static JsonContent Create<T>(T inputValue, MediaTypeHeaderValue? mediaType = null, JsonSerializerOptions? options = null)
            => Create(inputValue, typeof(T), mediaType, options);

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        public static JsonContent Create(object? inputValue, Type inputType, MediaTypeHeaderValue? mediaType = null, JsonSerializerOptions? options = null)
            => new JsonContent(inputValue, inputType, mediaType, options);

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            => SerializeToStreamAsyncCore(stream, async: true, CancellationToken.None);

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The ctor is annotated with RequiresUnreferencedCode.")]
        private async Task SerializeToStreamAsyncCore(Stream targetStream, bool async, CancellationToken cancellationToken)
        {
            Encoding? targetEncoding = JsonHelpers.GetEncoding(Headers.ContentType?.CharSet);

            // Wrap provided stream into a transcoding stream that buffers the data transcoded from utf-8 to the targetEncoding.
            if (targetEncoding != null && targetEncoding != Encoding.UTF8)
            {
#if NETCOREAPP
                Stream transcodingStream = Encoding.CreateTranscodingStream(targetStream, targetEncoding, Encoding.UTF8, leaveOpen: true);
                try
                {
                    if (async)
                    {
                        await SerializeAsyncHelper(transcodingStream, Value, ObjectType, _jsonSerializerOptions, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // Have to use Utf8JsonWriter because JsonSerializer doesn't support sync serialization into stream directly.
                        // ToDo: Remove Utf8JsonWriter usage after https://github.com/dotnet/runtime/issues/1574
                        using var writer = new Utf8JsonWriter(transcodingStream);
                        SerializeHelper(writer, Value, ObjectType, _jsonSerializerOptions);
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
                    await SerializeAsyncHelper(transcodingStream, Value, ObjectType, _jsonSerializerOptions, cancellationToken).ConfigureAwait(false);
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
                    await SerializeAsyncHelper(targetStream, Value, ObjectType, _jsonSerializerOptions, cancellationToken).ConfigureAwait(false);
                }
                else
                {
#if NETCOREAPP
                    // Have to use Utf8JsonWriter because JsonSerializer doesn't support sync serialization into stream directly.
                    // ToDo: Remove Utf8JsonWriter usage after https://github.com/dotnet/runtime/issues/1574
                    using var writer = new Utf8JsonWriter(targetStream);
                    SerializeHelper(writer, Value, ObjectType, _jsonSerializerOptions);
#else
                    Debug.Fail("Synchronous serialization is only supported since .NET 5.0");
#endif
                }
            }

#if NETCOREAPP
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "Workaround for https://github.com/mono/linker/issues/1416. The outer method is marked as RequiresUnreferencedCode.")]
            static void SerializeHelper(Utf8JsonWriter writer, object? value, Type inputType, JsonSerializerOptions? options)
                => JsonSerializer.Serialize(writer, value, inputType, options);
#endif

            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "Workaround for https://github.com/mono/linker/issues/1416. The outer method is marked as RequiresUnreferencedCode.")]
            static Task SerializeAsyncHelper(Stream utf8Json, object? value, Type inputType, JsonSerializerOptions? options, CancellationToken cancellationToken)
                => JsonSerializer.SerializeAsync(utf8Json, value, inputType, options, cancellationToken);
        }
    }
}
