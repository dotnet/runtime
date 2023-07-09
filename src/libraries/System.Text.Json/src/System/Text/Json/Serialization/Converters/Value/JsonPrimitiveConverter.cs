// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Inherited by built-in converters serializing types as JSON primitives that support property name serialization.
    /// </summary>
    internal abstract class JsonPrimitiveConverter<T> : JsonConverter<T>
    {
        public sealed override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] T value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(value));
            }

            WriteAsPropertyNameCore(writer, value, options, isWritingExtensionDataProperty: false);
        }

        public sealed override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedPropertyName(reader.TokenType);
            }

            return ReadAsPropertyNameCore(ref reader, typeToConvert, options);
        }
    }
}
