// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Creates and initializes serialization metadata for a type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class CustomJsonTypeInfo<T> : JsonTypeInfo<T>
    {
        /// <summary>
        /// Creates serialization metadata for a type using a simple converter.
        /// </summary>
        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        internal CustomJsonTypeInfo(JsonSerializerOptions options)
            : base(GetConverter(options),
                  options)
        {
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        private static JsonConverter GetConverter(JsonSerializerOptions options)
        {
            DefaultJsonTypeInfoResolver.RootDefaultInstance();
            return GetEffectiveConverter(
                    typeof(T),
                    parentClassType: null, // A TypeInfo never has a "parent" class.
                    memberInfo: null, // A TypeInfo never has a "parent" property.
                    options);
        }

        internal override JsonParameterInfoValues[] GetParameterInfoValues()
        {
            // Parametrized constructors not supported yet for custom types
            return Array.Empty<JsonParameterInfoValues>();
        }
    }
}
