// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Text.Json.Serialization.Converters
{
    [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
    internal sealed class MemoryConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            if (!typeToConvert.IsGenericType || !typeToConvert.IsValueType)
            {
                return false;
            }

            Type typeDef = typeToConvert.GetGenericTypeDefinition();
            return typeDef == typeof(Memory<>) || typeDef == typeof(ReadOnlyMemory<>);
        }

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(CanConvert(typeToConvert));

            Type converterType = typeToConvert.GetGenericTypeDefinition() == typeof(Memory<>) ?
                typeof(MemoryConverter<>) : typeof(ReadOnlyMemoryConverter<>);

            Type elementType = typeToConvert.GetGenericArguments()[0];

            return (JsonConverter)Activator.CreateInstance(
                converterType.MakeGenericType(elementType))!;
        }
    }
}
