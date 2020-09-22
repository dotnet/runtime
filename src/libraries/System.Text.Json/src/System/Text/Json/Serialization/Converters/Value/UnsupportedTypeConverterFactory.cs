// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.Serialization;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class UnsupportedTypeConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type type)
        {
            // If a value type is added, also add a test that
            // shows NSE is thrown when Nullable<T> is deserialized.
            return type == typeof(Type) || type == typeof(SerializationInfo);
        }

        public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
        {
            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                typeof(UnsupportedTypeConverter<>).MakeGenericType(type),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: null,
                culture: null)!;

            return converter;
        }
    }
}
