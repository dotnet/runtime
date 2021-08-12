// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.Serialization;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class DisallowedTypeConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type type)
        {
            // If a value type is added, also add a test that
            // shows NSE is thrown when Nullable<T> is (de)serialized.

            return
                // There's no safe way to construct a Type from untrusted user input.
                type == typeof(Type) ||
                // (De)serialization of SerializationInfo is already disallowed due to Type being disallowed
                // (the two ctors on SerializationInfo take a Type, and a Type member is present when serializing).
                // Explicitly disallowing this type provides a clear exception when ctors with
                // .ctor(SerializationInfo, StreamingContext) signatures are attempted to be used for deserialization.
                // Invoking such ctors is not safe when used with untrusted user input.
                type == typeof(SerializationInfo) ||
                type == typeof(IntPtr) ||
                type == typeof(UIntPtr) ||
                // To be added in future releases; guard against invalid object-based serializations.
                // https://github.com/dotnet/runtime/issues/53539
                IsDateOnlyOrTimeOnly(type);

            static bool IsDateOnlyOrTimeOnly(Type type)
                => type.Assembly == typeof(int).Assembly && type.FullName is "System.DateOnly" or "System.TimeOnly";
        }

        public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
        {
            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                typeof(DisallowedTypeConverter<>).MakeGenericType(type),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: null,
                culture: null)!;

            return converter;
        }
    }
}
