// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Serialization
{
    public class JsonStringEnumConverter<TEnum> : JsonConverterFactory
        where TEnum : struct, Enum
    {
        private readonly JsonNamingPolicy? _namingPolicy;
        private readonly EnumConverterOptions _converterOptions;

        /// <summary>
        /// Constructor. Creates the <see cref="JsonStringEnumConverter"/> with the
        /// default naming policy and allows integer values.
        /// </summary>
        public JsonStringEnumConverter() : this(namingPolicy: null, allowIntegerValues: true)
        {
            // An empty constructor is needed for construction via attributes
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="namingPolicy">
        /// Optional naming policy for writing enum values.
        /// </param>
        /// <param name="allowIntegerValues">
        /// True to allow undefined enum values. When true, if an enum value isn't
        /// defined it will output as a number rather than a string.
        /// </param>
        public JsonStringEnumConverter(JsonNamingPolicy? namingPolicy = null, bool allowIntegerValues = true)
        {
            _namingPolicy = namingPolicy;
            _converterOptions = allowIntegerValues
                ? EnumConverterOptions.AllowNumbers | EnumConverterOptions.AllowStrings
                : EnumConverterOptions.AllowStrings;
        }

        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(TEnum);
        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert != typeof(TEnum))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_ArrayIndexNegative(nameof(typeToConvert));
            }

            return new EnumConverter<TEnum>(_converterOptions, _namingPolicy, options);
        }
    }

    /// <summary>
    /// Converter to convert enums to and from strings.
    /// </summary>
    /// <remarks>
    /// Reading is case insensitive, writing can be customized via a <see cref="JsonNamingPolicy" />.
    /// </remarks>
    [RequiresDynamicCode(
        "JsonStringEnumConverter cannot be statically analyzed and requires runtime code generation. " +
        "Native AOT applications should use the generic JsonStringEnumConverter<TEnum> instead.")]
    public class JsonStringEnumConverter : JsonConverterFactory
    {
        private readonly JsonNamingPolicy? _namingPolicy;
        private readonly EnumConverterOptions _converterOptions;

        /// <summary>
        /// Constructor. Creates the <see cref="JsonStringEnumConverter"/> with the
        /// default naming policy and allows integer values.
        /// </summary>
        public JsonStringEnumConverter() : this(namingPolicy: null, allowIntegerValues: true)
        {
            // An empty constructor is needed for construction via attributes
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="namingPolicy">
        /// Optional naming policy for writing enum values.
        /// </param>
        /// <param name="allowIntegerValues">
        /// True to allow undefined enum values. When true, if an enum value isn't
        /// defined it will output as a number rather than a string.
        /// </param>
        public JsonStringEnumConverter(JsonNamingPolicy? namingPolicy = null, bool allowIntegerValues = true)
        {
            _namingPolicy = namingPolicy;
            _converterOptions = allowIntegerValues
                ? EnumConverterOptions.AllowNumbers | EnumConverterOptions.AllowStrings
                : EnumConverterOptions.AllowStrings;
        }

        /// <inheritdoc />
        public sealed override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsEnum;
        }

        /// <inheritdoc />
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "The ctor is marked RequiresDynamicCode.")]
        public sealed override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            EnumConverterFactory.Create(typeToConvert, _converterOptions, _namingPolicy, options);
    }
}
