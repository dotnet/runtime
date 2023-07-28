// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    internal sealed class JsonContent<TValue> : JsonContent
    {
        private readonly JsonTypeInfo<TValue> _typeInfo;
        private readonly TValue _typedValue;

        private protected override JsonTypeInfo JsonTypeInfo => _typeInfo;
        private protected override object? ValueCore => _typedValue;

        public JsonContent(TValue inputValue, JsonTypeInfo<TValue> jsonTypeInfo, MediaTypeHeaderValue? mediaType)
            : base(mediaType)
        {
            ThrowHelper.ThrowIfNull(jsonTypeInfo);

            _typeInfo = jsonTypeInfo;
            _typedValue = inputValue;
        }

        private protected override Task SerializeToUtf8StreamAsync(Stream targetStream,
            CancellationToken cancellationToken)
            => JsonSerializer.SerializeAsync(targetStream, _typedValue, _typeInfo, cancellationToken);

#if NETCOREAPP
        private protected override void SerializeToUtf8Stream(Stream targetStream)
        {
            JsonSerializer.Serialize(targetStream, _typedValue, _typeInfo);
        }
#endif
    }
}
