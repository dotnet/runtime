// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        internal static readonly MediaTypeHeaderValue DefaultMediaType = MediaTypeHeaderValue.Parse(string.Format("{0} {1}", JsonMediaType, Encoding.UTF8.WebName));

        private readonly JsonSerializerOptions? _jsonSerializerOptions;
        public Type ObjectType { get; }
        public object? Value { get; }

        private JsonContent(object? value, Type inputType, MediaTypeHeaderValue mediaType, JsonSerializerOptions? options)
        {
            if (mediaType == null)
            {
                throw new ArgumentNullException(nameof(mediaType));
            }

            if (inputType == null)
            {
                throw new ArgumentNullException(nameof(inputType));
            }

            Value = value;
            ObjectType = inputType;
            Headers.ContentType = mediaType;

            //    // TODO: Support other charsets once https://github.com/dotnet/runtime/issues/30260 is done.
            //    string charset = mediaType.CharSet;
            //    if (charset != null && charset != Encoding.UTF8.WebName)
            //    {
            //        // Add validations for uppercase, quoted and invalid charsets.
            //        _encoding = Encoding.GetEncoding(charset);
            //        //throw new NotSupportedException(SR.CharSetInvalid);
            //    }

            _jsonSerializerOptions = options;
        }

        public static JsonContent Create<T>(T value, MediaTypeHeaderValue mediaType, JsonSerializerOptions? options = null)
        {
            return Create(value, typeof(T), mediaType, options);
        }

        public static JsonContent Create(object? inputValue, Type inputType, MediaTypeHeaderValue mediaType, JsonSerializerOptions? options = null)
        {
            if (mediaType == null)
            {
                throw new ArgumentNullException(nameof(mediaType));
            }

            return new JsonContent(inputValue, inputType, mediaType, options);
        }

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
