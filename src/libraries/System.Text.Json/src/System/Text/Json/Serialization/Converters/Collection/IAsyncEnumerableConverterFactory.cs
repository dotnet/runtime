// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter factory for all IEnumerable types.
    /// </summary>
    internal class IAsyncEnumerableConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
            => GetConverterType(typeToConvert, out _, out _);

        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.IAsyncEnumerableOfTConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.IAsyncEnumerableOfTObjectConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.IAsyncEnumerableOfTValueConverter`2")]
        private static bool GetConverterType(Type typeToConvert, out Type converterType, out Type[] genericArguments)
        {
            // Try the supplied type directly.
            if (typeToConvert.IsGenericType
             && typeToConvert.IsInterface
             && (typeToConvert.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>)))
            {
                var elementType = typeToConvert.GetGenericArguments()[0];

                genericArguments = new[] { typeToConvert, elementType };

                converterType = elementType.IsValueType
                    ? typeof(IAsyncEnumerableOfTValueConverter<,>)
                    : typeof(IAsyncEnumerableOfTObjectConverter<,>);

                return true;
            }

            // Check all interfaces implemented by the supplied type.
            foreach (var interfaceType in typeToConvert.GetInterfaces())
            {
                if (GetConverterType(interfaceType, out converterType, out genericArguments))
                {
                    genericArguments[0] = typeToConvert;
                    return true;
                }
            }

            // No match.
            converterType = default!;
            genericArguments = default!;

            return false;
        }

        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.IAsyncEnumerableOfTConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.IAsyncEnumerableOfTObjectConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.IAsyncEnumerableOfTValueConverter`2")]
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            GetConverterType(
                typeToConvert,
                out Type converterType,
                out Type[] genericArguments);

            Debug.Assert(converterType != null);
            Debug.Assert(genericArguments != null);

            Type genericType = converterType.MakeGenericType(genericArguments);

            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                genericType,
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: null,
                culture: null)!;

            return converter!;
        }
    }
}
