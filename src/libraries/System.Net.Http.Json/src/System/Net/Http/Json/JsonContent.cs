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
        private static MediaTypeHeaderValue s_defaultMediaType => MediaTypeHeaderValue.Parse(string.Format("{0}; charset={1}", JsonMediaType, Encoding.UTF8.WebName));
        internal static JsonSerializerOptions s_defaultSerializerOptions => new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private readonly JsonSerializerOptions? _jsonSerializerOptions;
        public Type ObjectType { get; }
        public object? Value { get; }

        private JsonContent(object? inputValue, Type inputType, MediaTypeHeaderValue? mediaType, JsonSerializerOptions? options)
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
            Headers.ContentType = mediaType ?? s_defaultMediaType;
            _jsonSerializerOptions = options ?? s_defaultSerializerOptions;
        }

        public static JsonContent Create<T>(T inputValue, MediaTypeHeaderValue? mediaType, JsonSerializerOptions? options = null)
            => Create(inputValue, typeof(T), mediaType, options);

        public static JsonContent Create(object? inputValue, Type inputType, MediaTypeHeaderValue? mediaType, JsonSerializerOptions? options = null)
            => new JsonContent(inputValue, inputType, mediaType, options);

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            => SerializeToStreamAsyncCore(stream, CancellationToken.None);

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        private async Task SerializeToStreamAsyncCore(Stream targetStream, CancellationToken cancellationToken)
        {
            Encoding? targetEncoding = GetEncoding(Headers.ContentType!.CharSet);

            // Wrap provided stream into a transcoding stream that buffers the data transcoded from utf-8 to the targetEncoding.
            if (targetEncoding != null && targetEncoding != Encoding.UTF8)
            {
                using (TranscodingWriteStream transcodingStream = new TranscodingWriteStream(targetStream, targetEncoding))
                {
                    await JsonSerializer.SerializeAsync(transcodingStream, Value, ObjectType, _jsonSerializerOptions, cancellationToken).ConfigureAwait(false);
                    await transcodingStream.FinalWriteAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                await JsonSerializer.SerializeAsync(targetStream, Value, ObjectType, _jsonSerializerOptions, cancellationToken).ConfigureAwait(false);
            }
        }

        private static Encoding? GetEncoding(string? charset)
        {
            Encoding? encoding = null;

            if (charset != null)
            {
                try
                {
                    // Remove at most a single set of quotes.
                    if (charset.Length > 2 && charset[0] == '\"' && charset[charset.Length - 1] == '\"')
                    {
                        encoding = Encoding.GetEncoding(charset.Substring(1, charset.Length - 2));
                    }
                    else
                    {
                        encoding = Encoding.GetEncoding(charset);
                    }
                }
                catch (ArgumentException e)
                {
                    throw new InvalidOperationException(SR.CharSetInvalid, e);
                }

                Debug.Assert(encoding != null);
            }

            return encoding;
        }
    }
}
