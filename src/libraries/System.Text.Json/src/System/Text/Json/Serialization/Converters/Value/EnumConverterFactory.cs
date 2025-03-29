// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Reflection;

namespace System.Text.Json.Serialization.Converters
{
    [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
    internal sealed class EnumConverterFactory : JsonConverterFactory
    {
        public EnumConverterFactory()
        {
        }

        public override bool CanConvert(Type type)
        {
            return type.IsEnum;
        }

        public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
        {
            Debug.Assert(CanConvert(type));
            return Create(type, EnumConverterOptions.AllowNumbers, namingPolicy: null, options);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2071:UnrecognizedReflectionPattern",
            Justification = "'EnumConverter<T> where T : struct' implies 'T : new()', so the trimmer is warning calling MakeGenericType here because enumType's constructors are not annotated. " +
            "But EnumConverter doesn't call new T(), so this is safe.")]
        public static JsonConverter Create(Type enumType, EnumConverterOptions converterOptions, JsonNamingPolicy? namingPolicy, JsonSerializerOptions options)
        {
            if (!Helpers.IsSupportedTypeCode(Type.GetTypeCode(enumType)))
            {
                // Char-backed enums are valid in IL and F# but are not supported by System.Text.Json.
                return UnsupportedTypeConverterFactory.CreateUnsupportedConverterForType(enumType);
            }

            Type converterType = typeof(EnumConverter<>).MakeGenericType(enumType);
            return (JsonConverter)converterType.CreateInstanceNoWrapExceptions(
                parameterTypes: [typeof(EnumConverterOptions), typeof(JsonNamingPolicy), typeof(JsonSerializerOptions)],
                parameters: [converterOptions, namingPolicy, options])!;
        }

        // Some of the static methods are in a separate class so that the
        // RequiresDynamicCode annotation on EnumConverterFactory doesn't apply
        // to them.
        internal static class Helpers
        {
            public static bool IsSupportedTypeCode(TypeCode typeCode)
            {
                return typeCode is TypeCode.SByte or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64
                                or TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64;
            }

            public static JsonConverter<T> Create<T>(EnumConverterOptions converterOptions, JsonSerializerOptions options, JsonNamingPolicy? namingPolicy = null)
                where T : struct, Enum
            {
                if (!IsSupportedTypeCode(Type.GetTypeCode(typeof(T))))
                {
                    // Char-backed enums are valid in IL and F# but are not supported by System.Text.Json.
                    return new UnsupportedTypeConverter<T>();
                }

                return new EnumConverter<T>(converterOptions, namingPolicy, options);
            }
        }
    }
}
