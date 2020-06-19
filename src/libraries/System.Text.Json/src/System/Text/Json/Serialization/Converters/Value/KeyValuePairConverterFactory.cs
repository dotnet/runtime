// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class KeyValuePairConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            if (!typeToConvert.IsGenericType)
                return false;

            Type generic = typeToConvert.GetGenericTypeDefinition();
            return (generic == typeof(KeyValuePair<,>));
        }

        [DynamicDependency("#ctor()", typeof(KeyValuePairConverter<,>))]
        public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
        {
            Debug.Assert(CanConvert(type));

            Type keyType = type.GetGenericArguments()[0];
            Type valueType = type.GetGenericArguments()[1];

            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                typeof(KeyValuePairConverter<,>).MakeGenericType(new Type[] { keyType, valueType }),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: null,
                culture: null)!;

            converter.Initialize(options);

            return converter;
        }
    }
}
