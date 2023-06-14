// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;

namespace System.Text.Json.Serialization.Converters
{
    [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
    internal sealed class UnsupportedTypeConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type type)
        {
            // If a type is added, also add to the SourceGeneration project.

            return
                // There's no safe way to construct a Type/MemberInfo from untrusted user input.
                typeof(MemberInfo).IsAssignableFrom(type) ||
                // (De)serialization of SerializationInfo is already disallowed due to Type being disallowed
                // (the two ctors on SerializationInfo take a Type, and a Type member is present when serializing).
                // Explicitly disallowing this type provides a clear exception when ctors with
                // .ctor(SerializationInfo, StreamingContext) signatures are attempted to be used for deserialization.
                // Invoking such ctors is not safe when used with untrusted user input.
                type == typeof(SerializationInfo) ||
                type == typeof(IntPtr) ||
                type == typeof(UIntPtr) ||
                // Exclude Memory<T> and ReadOnlyMemory<T> types.
                IsMemoryType(type) ||
                // Exclude delegates.
                typeof(Delegate).IsAssignableFrom(type);

            static bool IsMemoryType(Type type)
            {
                if (!type.IsGenericType || !type.IsValueType)
                {
                    return false;
                }

                Type typeDef = type.GetGenericTypeDefinition();
                return typeDef == typeof(Memory<>) || typeDef == typeof(ReadOnlyMemory<>);
            }
        }

        public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
        {
            Debug.Assert(CanConvert(type));
            return CreateUnsupportedConverterForType(type);
        }

        internal static JsonConverter CreateUnsupportedConverterForType(Type type, string? errorMessage = null)
        {
            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                typeof(UnsupportedTypeConverter<>).MakeGenericType(type),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: new object?[] { errorMessage },
                culture: null)!;

            return converter;
        }
    }
}
