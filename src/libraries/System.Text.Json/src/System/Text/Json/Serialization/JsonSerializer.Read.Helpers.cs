// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        private static TValue? ReadCore<TValue>(JsonConverter jsonConverter, ref Utf8JsonReader reader, JsonSerializerOptions options, ref ReadStack state)
        {
            if (jsonConverter is JsonConverter<TValue> converter)
            {
                // Call the strongly-typed ReadCore that will not box structs.
                return converter.ReadCore(ref reader, options, ref state);
            }

            // The non-generic API was called or we have a polymorphic case where TValue is not equal to the T in JsonConverter<T>.
            object? value = jsonConverter.ReadCoreAsObject(ref reader, options, ref state);
            Debug.Assert(value == null || value is TValue);
            return (TValue?)value;
        }

        private static TValue? ReadFromSpan<TValue>(ReadOnlySpan<byte> utf8Json, JsonTypeInfo jsonTypeInfo, int? actualByteCount = null)
        {
            JsonSerializerOptions options = jsonTypeInfo.Options;

            var readerState = new JsonReaderState(options.GetReaderOptions());
            var reader = new Utf8JsonReader(utf8Json, isFinalBlock: true, readerState);

            ReadStack state = default;
            state.Initialize(jsonTypeInfo);

            TValue? value;
            JsonConverter jsonConverter = jsonTypeInfo.PropertyInfoForTypeInfo.ConverterBase;

            // For performance, the code below is a lifted ReadCore() above.
            if (jsonConverter is JsonConverter<TValue> converter)
            {
                // Call the strongly-typed ReadCore that will not box structs.
                value = converter.ReadCore(ref reader, options, ref state);
            }
            else
            {
                // The non-generic API was called or we have a polymorphic case where TValue is not equal to the T in JsonConverter<T>.
                object? objValue = jsonConverter.ReadCoreAsObject(ref reader, options, ref state);
                Debug.Assert(objValue == null || objValue is TValue);
                value = (TValue?)objValue;
            }

            // The reader should have thrown if we have remaining bytes.
            Debug.Assert(reader.BytesConsumed == (actualByteCount ?? utf8Json.Length));
            return value;
        }
    }
}
