// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.System.Text.Json.Serialization
{
    /// <summary>
    /// Converter to convert boolean to and from strings.
    /// </summary>
    public sealed class JsonStringBooleanConverter : JsonConverterFactory
    {
        private readonly bool _convertBooleanToString;

        /// <summary>
        /// Constructor. Creates the <see cref="JsonStringEnumConverter"/>
        /// </summary>
        /// <param name="convertBooleanToString">Flag to allow boolean to be written into string</param>
        public JsonStringBooleanConverter(bool convertBooleanToString)
        {
            _convertBooleanToString = convertBooleanToString;
        }

        /// <summary>
        /// Constructor. Creates the <see cref="JsonStringEnumConverter"/> allowing boolean to be written into string
        /// </summary>
        public JsonStringBooleanConverter() : this(true) { }

        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(bool);
        }

        /// <inheritdoc />
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                typeof(StringBooleanConverter),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                new object[] { _convertBooleanToString },
                culture: null)!;

            return converter;
        }
    }
}
