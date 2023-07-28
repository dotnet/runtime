// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NETCOREAPP
using System.Diagnostics;
#endif
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    public sealed partial class JsonContent
    {
        private sealed class JsonContentTypeInfoSerializer : JsonContentSerializer
        {
            private readonly JsonTypeInfo _typeInfo;

            public override Type ObjectType => _typeInfo.Type;
            public override object? Value { get; }

            public JsonContentTypeInfoSerializer(object? inputValue, JsonTypeInfo jsonTypeInfo)
            {
                ThrowHelper.ThrowIfNull(jsonTypeInfo);

                _typeInfo = jsonTypeInfo;
                Value = inputValue;
            }

            public override Task SerializeToStreamAsync(Stream targetStream,
                CancellationToken cancellationToken)
                => JsonSerializer.SerializeAsync(targetStream, Value, _typeInfo, cancellationToken);

            public override void SerializeToStream(Stream targetStream)
            {
#if NETCOREAPP
                JsonSerializer.Serialize(targetStream, Value, _typeInfo);
#else
                Debug.Fail("Synchronous serialization is only supported since .NET 5.0");
#endif
            }
        }
    }
}
