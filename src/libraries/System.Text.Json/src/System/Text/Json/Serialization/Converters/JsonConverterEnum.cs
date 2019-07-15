// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonConverterEnum : JsonConverterFactory
    {
        public JsonConverterEnum()
        {
        }

        public override bool CanConvert(Type type)
        {
            return type.IsEnum;
        }

        public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
        {
            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                typeof(JsonConverterEnum<>).MakeGenericType(type),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                new object[] { EnumConverterOptions.AllowNumbers },
                culture: null);

            return converter;
        }
    }
}
