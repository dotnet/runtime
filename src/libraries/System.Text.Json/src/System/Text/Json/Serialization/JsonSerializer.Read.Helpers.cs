// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        private static TValue? ReadCore<TValue>(ref Utf8JsonReader reader, Type returnType, JsonSerializerOptions options)
        {
            ReadStack state = default;
            state.Initialize(returnType, options, supportContinuation: false);
            JsonConverter jsonConverter = state.Current.JsonPropertyInfo!.ConverterBase;
            return ReadCore<TValue>(jsonConverter, ref reader, options, ref state);
        }

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
            return (TValue)value!;
        }
    }
}
