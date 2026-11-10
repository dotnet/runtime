// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Text.Json.Serialization.Converters
{
    [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
    internal sealed class JsonUnionConverterFactory : JsonConverterFactory
    {
        private const string UnionAttributeName = "UnionAttribute";
        private const string UnionAttributeFullName = "System.Runtime.CompilerServices.UnionAttribute";

        // A type IS a union iff it carries [System.Runtime.CompilerServices.UnionAttribute].
        // The marker is detected by full name (rather than via a compile-time reference)
        // because the C# compiler may polyfill it into the user's assembly when targeting a
        // downlevel runtime. [JsonUnion] alone is a customization attribute that has no meaning
        // on non-union types.
        public override bool CanConvert(Type typeToConvert) => IsUnionType(typeToConvert);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            Type converterType = typeof(JsonUnionConverter<>).MakeGenericType(typeToConvert);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }

        private static bool IsUnionType(Type type)
        {
            foreach (CustomAttributeData data in type.GetCustomAttributesData())
            {
                Type attributeType = data.AttributeType;
                if (attributeType.Name == UnionAttributeName &&
                    attributeType.FullName == UnionAttributeFullName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
