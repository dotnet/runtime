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

        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationRequiresDynamicCodeMessage)]
        private static JsonTypeInfo GetTypeInfo(JsonSerializerOptions? options, Type inputType)
        {
            Debug.Assert(inputType != null);

            options ??= JsonSerializerOptions.Default;

            if (!options.IsInitializedForReflectionSerializer)
            {
                options.InitializeForReflectionSerializer();
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

        internal static bool IsValidNumberHandlingValue(JsonNumberHandling handling) =>
            JsonHelpers.IsInRangeInclusive((int)handling, 0,
                (int)(
                JsonNumberHandling.Strict |
                JsonNumberHandling.AllowReadingFromString |
                JsonNumberHandling.WriteAsString |
                JsonNumberHandling.AllowNamedFloatingPointLiterals));
    }
}
