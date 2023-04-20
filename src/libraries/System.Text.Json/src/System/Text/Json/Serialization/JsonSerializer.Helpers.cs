// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        internal const string SerializationUnreferencedCodeMessage = "JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.";
        internal const string SerializationRequiresDynamicCodeMessage = "JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.";

        /// <summary>
        /// Indicates whether unconfigured <see cref="JsonSerializerOptions"/> instances
        /// should be set to use the reflection-based <see cref="DefaultJsonTypeInfoResolver"/>.
        /// </summary>
        /// <remarks>
        /// The value of the property is backed by the "System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault"
        /// <see cref="AppContext"/> setting and defaults to <see langword="true"/> if unset.
        /// </remarks>
        public static bool IsReflectionEnabledByDefault { get; } =
            AppContext.TryGetSwitch(
                switchName: "System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault",
                isEnabled: out bool value)
            ? value : true;

        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationRequiresDynamicCodeMessage)]
        private static JsonTypeInfo GetTypeInfo(JsonSerializerOptions? options, Type inputType)
        {
            Debug.Assert(inputType != null);

            options ??= JsonSerializerOptions.Default;

            if (!options.IsConfiguredForJsonSerializer)
            {
                options.ConfigureForJsonSerializer();
            }

            // In order to improve performance of polymorphic root-level object serialization,
            // we bypass GetTypeInfoForRootType and cache JsonTypeInfo<object> in a dedicated property.
            // This lets any derived types take advantage of the cache in GetTypeInfoForRootType themselves.
            return inputType == JsonTypeInfo.ObjectType
                ? options.ObjectTypeInfo
                : options.GetTypeInfoForRootType(inputType);
        }

        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationRequiresDynamicCodeMessage)]
        private static JsonTypeInfo<T> GetTypeInfo<T>(JsonSerializerOptions? options)
            => (JsonTypeInfo<T>)GetTypeInfo(options, typeof(T));

        private static JsonTypeInfo GetTypeInfo(JsonSerializerContext context, Type inputType)
        {
            Debug.Assert(context != null);
            Debug.Assert(inputType != null);

            JsonTypeInfo? info = context.GetTypeInfo(inputType);
            if (info is null)
            {
                ThrowHelper.ThrowInvalidOperationException_NoMetadataForType(inputType, context);
            }

            info.EnsureConfigured();
            return info;
        }

        private static void ValidateInputType(object? value, Type inputType)
        {
            if (inputType is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(inputType));
            }

            if (value is not null)
            {
                Type runtimeType = value.GetType();
                if (!inputType.IsAssignableFrom(runtimeType))
                {
                    ThrowHelper.ThrowArgumentException_DeserializeWrongType(inputType, value);
                }
            }
        }

        internal static bool IsValidNumberHandlingValue(JsonNumberHandling handling) =>
            JsonHelpers.IsInRangeInclusive((int)handling, 0,
                (int)(
                JsonNumberHandling.Strict |
                JsonNumberHandling.AllowReadingFromString |
                JsonNumberHandling.WriteAsString |
                JsonNumberHandling.AllowNamedFloatingPointLiterals));

        internal static bool IsValidUnmappedMemberHandlingValue(JsonUnmappedMemberHandling handling) =>
            handling is JsonUnmappedMemberHandling.Skip or JsonUnmappedMemberHandling.Disallow;

        [return: NotNullIfNotNull(nameof(value))]
        internal static T? UnboxOnRead<T>(object? value)
        {
            if (value is null)
            {
                if (default(T) is not null)
                {
                    // Casting null values to a non-nullable struct throws NullReferenceException.
                    ThrowUnableToCastValue(value);
                }

                return default;
            }

            if (value is T typedValue)
            {
                return typedValue;
            }

            ThrowUnableToCastValue(value);
            return default!;

            static void ThrowUnableToCastValue(object? value)
            {
                if (value is null)
                {
                    ThrowHelper.ThrowInvalidOperationException_DeserializeUnableToAssignNull(declaredType: typeof(T));
                }
                else
                {
                    ThrowHelper.ThrowInvalidCastException_DeserializeUnableToAssignValue(typeOfValue: value.GetType(), declaredType: typeof(T));
                }
            }
        }

        [return: NotNullIfNotNull(nameof(value))]
        internal static T? UnboxOnWrite<T>(object? value)
        {
            if (default(T) is not null && value is null)
            {
                // Casting null values to a non-nullable struct throws NullReferenceException.
                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(typeof(T));
            }

            return (T?)value;
        }
    }
}
