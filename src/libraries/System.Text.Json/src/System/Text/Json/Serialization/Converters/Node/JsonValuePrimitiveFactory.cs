// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Reflection;
using System.Text.Json.Serialization.Converters.Node;

namespace System.Text.Json.Serialization.Converters
{
    [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
    internal sealed class JsonValuePrimitiveFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsJsonValuePrimitiveOfT();
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(typeToConvert.IsJsonValuePrimitiveOfT());

            Type valueTypeToConvert = typeToConvert.GetGenericArguments()[0];
            JsonConverter valueConverter = options.GetConverterInternal(valueTypeToConvert);

            return CreateValueConverter(valueTypeToConvert, valueConverter);
        }

        public static JsonConverter CreateValueConverter(Type valueTypeToConvert, JsonConverter valueConverter)
        {
            return (JsonConverter)Activator.CreateInstance(
                GetJsonValuePrimitiveConverterType(valueTypeToConvert),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: new object[] { valueConverter },
                culture: null)!;
        }

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        private static Type GetJsonValuePrimitiveConverterType(Type valueTypeToConvert) => typeof(JsonValuePrimitiveConverter<>).MakeGenericType(valueTypeToConvert);
    }
}
