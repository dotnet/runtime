// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;

namespace System.Text.Json.Serialization
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

            JsonConverter? valueConverter = options.GetConverter(valueTypeToConvert);
            if (valueConverter == null)
            {
                ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(valueTypeToConvert);
            }

            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                typeof(NullableConverter<>).MakeGenericType(valueTypeToConvert),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: new object[] { valueConverter },
                culture: null)!;

            return converter;
        }
    }
}
