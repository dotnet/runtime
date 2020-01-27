// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Reflection;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter factory for all object-based types (non-enumerable and non-primitive).
    /// </summary>
    internal class JsonObjectFactoryConverter : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            // If the IEnumerableConverterFactory doesn't support the collection, ObjectConverterFactory doesn't either.
            return !typeof(IEnumerable).IsAssignableFrom(typeToConvert);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                typeof(JsonObjectDefaultConverter<>).MakeGenericType(typeToConvert),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: null,
                culture: null)!;

            return converter;
        }
    }
}
