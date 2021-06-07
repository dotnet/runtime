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

        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        private static JsonTypeInfo GetTypeInfo(Type runtimeType, JsonSerializerOptions? options)
        {
            options ??= JsonSerializerOptions.s_defaultOptions;
            options.RootBuiltInConvertersAndTypeInfoCreator();
            return options.GetOrAddClassForRootType(runtimeType);
        }

        private static JsonTypeInfo GetTypeInfo(JsonSerializerContext context, Type type)
        {
            Debug.Assert(context != null);
            Debug.Assert(type != null);

            JsonTypeInfo? info = context.GetTypeInfo(type);
            if (info == null)
            {
                ThrowHelper.ThrowInvalidOperationException_NoMetadataForType(type);
            }

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
