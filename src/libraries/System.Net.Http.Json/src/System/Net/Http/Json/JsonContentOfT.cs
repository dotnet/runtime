// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NETCOREAPP
using System.Diagnostics;
#endif
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    internal sealed partial class JsonContent<TValue> : HttpContent
    {
        private readonly JsonTypeInfo<TValue> _typeInfo;

        private readonly TValue _typedValue;

        public JsonContent(TValue inputValue, JsonTypeInfo<TValue> jsonTypeInfo)
        {
            if (jsonTypeInfo is null)
            {
                throw new ArgumentNullException(nameof(jsonTypeInfo));
            }

            _typeInfo = jsonTypeInfo;
            _typedValue = inputValue;
            Headers.ContentType = JsonHelpers.GetDefaultMediaType();
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            => SerializeToStreamAsyncCore(stream, async: true, CancellationToken.None);

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        /// <summary>
        /// Based on <see cref="JsonContent.SerializeToStreamAsyncCore(Stream, bool, CancellationToken)"/>.
        /// The difference is that this implementation calls overloads of <see cref="JsonSerializer"/> that take type metadata directly.
        /// This is done to avoid rooting unused, built-in <see cref="System.Text.Json.Serialization.JsonConverter"/>s and reflection-based
        /// warm-up logic (to reduce app size and be trim-friendly), post trimming.
        /// </summary>
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
                        await JsonSerializer.SerializeAsync(transcodingStream, _typedValue, _typeInfo, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        JsonSerializer.Serialize(transcodingStream, _typedValue, _typeInfo);
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
                    await JsonSerializer.SerializeAsync(transcodingStream, _typedValue, _typeInfo, cancellationToken).ConfigureAwait(false);
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
                    await JsonSerializer.SerializeAsync(targetStream, _typedValue, _typeInfo, cancellationToken).ConfigureAwait(false);
                }
                else
                {
#if NETCOREAPP
                    JsonSerializer.Serialize(targetStream, _typedValue, _typeInfo);
#else
                    Debug.Fail("Synchronous serialization is only supported since .NET 5.0");
#endif
                }
            }
        }
    }
}
