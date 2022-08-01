// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        private static TValue? ReadCore<TValue>(ref Utf8JsonReader reader, JsonTypeInfo jsonTypeInfo, ref ReadStack state)
        {
            if (jsonTypeInfo is JsonTypeInfo<TValue> typedInfo)
            {
                // Call the strongly-typed ReadCore that will not box structs.
                return typedInfo.EffectiveConverter.ReadCore(ref reader, typedInfo.Options, ref state);
            }

            // The non-generic API was called.
            object? value = jsonTypeInfo.Converter.ReadCoreAsObject(ref reader, jsonTypeInfo.Options, ref state);
            Debug.Assert(value is null or TValue);
            return (TValue?)value;
        }

        private static TValue? ReadFromSpan<TValue>(ReadOnlySpan<byte> utf8Json, JsonTypeInfo jsonTypeInfo, int? actualByteCount = null)
        {
            Debug.Assert(jsonTypeInfo.IsConfigured);

            JsonSerializerOptions options = jsonTypeInfo.Options;

            var readerState = new JsonReaderState(options.GetReaderOptions());
            var reader = new Utf8JsonReader(utf8Json, isFinalBlock: true, readerState);

            ReadStack state = default;
            state.Initialize(jsonTypeInfo);

            TValue? value;

            // For performance, the code below is a lifted ReadCore() above.
            if (jsonTypeInfo is JsonTypeInfo<TValue> typedInfo)
            {
                // Call the strongly-typed ReadCore that will not box structs.
                value = typedInfo.EffectiveConverter.ReadCore(ref reader, options, ref state);
            }
            else
            {
                // The non-generic API was called.
                object? objValue = jsonTypeInfo.Converter.ReadCoreAsObject(ref reader, options, ref state);
                Debug.Assert(objValue is null or TValue);
                value = (TValue?)objValue;
            }

            // The reader should have thrown if we have remaining bytes.
            Debug.Assert(reader.BytesConsumed == (actualByteCount ?? utf8Json.Length));
            return value;
        }
    }
}
