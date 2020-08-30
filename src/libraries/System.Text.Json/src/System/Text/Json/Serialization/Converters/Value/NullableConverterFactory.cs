// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;

namespace System.Text.Json.Serialization.Converters
{
    internal class NullableConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return Nullable.GetUnderlyingType(typeToConvert) != null;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(typeToConvert.GetGenericArguments().Length > 0);

            Type valueTypeToConvert = typeToConvert.GetGenericArguments()[0];

            JsonConverter valueConverter = options.GetConverter(valueTypeToConvert);
            Debug.Assert(valueConverter != null);

            // If the value type has an interface or object converter, just return that converter directly.
            if (!valueConverter.TypeToConvert.IsValueType && valueTypeToConvert.IsValueType)
            {
                return valueConverter;
            }

            return CreateValueConverter(valueTypeToConvert, valueConverter);
        }

        public static JsonConverter CreateValueConverter(Type valueTypeToConvert, JsonConverter valueConverter)
        {
            return (JsonConverter)Activator.CreateInstance(
                typeof(NullableConverter<>).MakeGenericType(valueTypeToConvert),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: new object[] { valueConverter },
                culture: null)!;
        }
    }
}
