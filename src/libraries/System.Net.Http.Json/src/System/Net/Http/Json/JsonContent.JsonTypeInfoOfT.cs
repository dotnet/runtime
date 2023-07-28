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
        private sealed class JsonContentSerializer<TValue> : JsonContentSerializer
        {
            private readonly JsonTypeInfo<TValue> _typeInfo;
            private readonly TValue _typedValue;

            public override Type ObjectType => _typeInfo.Type;
            public override object? Value => _typedValue;

            public JsonContentSerializer(TValue inputValue, JsonTypeInfo<TValue> jsonTypeInfo)
            {
                ThrowHelper.ThrowIfNull(jsonTypeInfo);

                _typeInfo = jsonTypeInfo;
                _typedValue = inputValue;
            }

            public override Task SerializeToStreamAsync(Stream targetStream,
                CancellationToken cancellationToken)
                => JsonSerializer.SerializeAsync(targetStream, _typedValue, _typeInfo, cancellationToken);

            public override void SerializeToStream(Stream targetStream)
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
