// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Converter to convert enums to and from numeric values.
    /// </summary>
    /// <typeparam name="TEnum">The enum type that this converter targets.</typeparam>
    /// <remarks>
    /// This is the default converter for enums and can be used to override
    /// <see cref="JsonSourceGenerationOptionsAttribute.UseStringEnumConverter"/>
    /// on individual types or properties.
    /// </remarks>
    public sealed class JsonNumberEnumConverter<TEnum> : JsonConverterFactory
        where TEnum : struct, Enum
    {
        /// <summary>
        /// Initializes a new instance of <see cref="JsonNumberEnumConverter{TEnum}"/>.
        /// </summary>
        public JsonNumberEnumConverter() { }

        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(TEnum);

        /// <inheritdoc />
        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert != typeof(TEnum))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_JsonConverterFactory_TypeNotSupported(typeToConvert);
            }

            return new EnumConverter<TEnum>(EnumConverterOptions.AllowNumbers, options);
        }
    }
}
