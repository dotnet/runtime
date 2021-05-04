// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Implementation of <cref>JsonObjectConverter{T}</cref> for source-generated converters.
    /// </summary>
    internal sealed class ObjectSourceGenConverter<T> : ObjectDefaultConverter<T> where T : notnull
    {
        internal override bool OnTryRead(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options,
            ref ReadStack state,
            [MaybeNullWhen(false)] out T value)
        {
            JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;
            if (jsonTypeInfo.PropertyCache == null)
            {
                jsonTypeInfo.InitializeDeserializePropCache();
            }

            return base.OnTryRead(ref reader, typeToConvert, options, ref state, out value);
        }

        internal override bool OnTryWrite(
            Utf8JsonWriter writer,
            T value,
            JsonSerializerOptions options,
            ref WriteStack state)
        {
            JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;
            if (jsonTypeInfo.PropertyCacheArray == null)
            {
                jsonTypeInfo.InitializeSerializePropCache();
            }

            return base.OnTryWrite(writer, value, options, ref state);
        }
    }
}
