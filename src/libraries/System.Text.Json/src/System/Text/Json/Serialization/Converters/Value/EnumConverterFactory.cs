// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class EnumConverterFactory : JsonConverterFactory
    {
        public EnumConverterFactory()
        {
        }

        public override bool CanConvert(Type type)
        {
            return type.IsEnum;
        }

        [PreserveDependency(
            ".ctor(System.Text.Json.Serialization.Converters.EnumConverterOptions)",
            "System.Text.Json.Serialization.Converters.EnumConverter`1")]
        public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
        {
            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                typeof(EnumConverter<>).MakeGenericType(type),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                new object[] { EnumConverterOptions.AllowNumbers },
                culture: null)!;

            return converter;
        }
    }
}
