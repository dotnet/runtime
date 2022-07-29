// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Reflection;

namespace System.Text.Json.Serialization.Converters
{
    [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
    internal sealed class NullableConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsNullableOfT();
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(typeToConvert.GetGenericArguments().Length > 0);

            Type valueTypeToConvert = typeToConvert.GetGenericArguments()[0];

            JsonConverter valueConverter = options.GetConverterInternal(valueTypeToConvert);
            Debug.Assert(valueConverter != null);

            // If the value type has an interface or object converter, just return that converter directly.
            if (!valueConverter.TypeToConvert.IsValueType && valueTypeToConvert.IsValueType)
            {
                return valueConverter;
            }

            return CreateValueConverter(valueTypeToConvert, valueConverter);
        }

        public static JsonConverter CreateValueConverter(Type valueTypeToConvert, JsonConverter valueConverter)
        {
            return (JsonConverter)Activator.CreateInstance(
                GetNullableConverterType(valueTypeToConvert),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: new object[] { valueConverter },
                culture: null)!;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:MakeGenericType",
            Justification = "'NullableConverter<T> where T : struct' implies 'T : new()', so the trimmer is warning calling MakeGenericType here because valueTypeToConvert's constructors are not annotated. " +
            "But NullableConverter doesn't call new T(), so this is safe.")]
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        private static Type GetNullableConverterType(Type valueTypeToConvert) => typeof(NullableConverter<>).MakeGenericType(valueTypeToConvert);
    }
}
