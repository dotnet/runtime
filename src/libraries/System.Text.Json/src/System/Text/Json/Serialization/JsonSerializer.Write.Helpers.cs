// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization.Converters;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        /// <summary>
        /// Sync, strongly typed root value serialization helper.
        /// </summary>
        private static void WriteCore<TValue>(
            Utf8JsonWriter writer,
            in TValue value,
            JsonTypeInfo<TValue> jsonTypeInfo)
        {
            if (jsonTypeInfo.CanUseSerializeHandler)
            {
                // Short-circuit calls into SerializeHandler, if supported.
                // Even though this is already handled by JsonMetadataServicesConverter,
                // we avoid instantiating a WriteStack and a couple of additional virtual calls.

                Debug.Assert(jsonTypeInfo.SerializeHandler != null);
                Debug.Assert(jsonTypeInfo.Options.SerializerContext?.CanUseSerializationLogic == true);
                Debug.Assert(jsonTypeInfo.Converter is JsonMetadataServicesConverter<TValue>);

                jsonTypeInfo.SerializeHandler(writer, value);
            }
            else
            {
                WriteStack state = default;
                JsonTypeInfo polymorphicTypeInfo = ResolvePolymorphicTypeInfo(value, jsonTypeInfo, out state.IsPolymorphicRootValue);
                state.Initialize(polymorphicTypeInfo);

                bool success =
                    state.IsPolymorphicRootValue
                    ? polymorphicTypeInfo.Converter.WriteCoreAsObject(writer, value, jsonTypeInfo.Options, ref state)
                    : jsonTypeInfo.EffectiveConverter.WriteCore(writer, value, jsonTypeInfo.Options, ref state);

                Debug.Assert(success);
            }

            writer.Flush();
        }

        /// <summary>
        /// Sync, untyped root value serialization helper.
        /// </summary>
        private static void WriteCoreAsObject(
            Utf8JsonWriter writer,
            object? value,
            JsonTypeInfo jsonTypeInfo)
        {
            WriteStack state = default;
            JsonTypeInfo polymorphicTypeInfo = ResolvePolymorphicTypeInfo(value, jsonTypeInfo, out state.IsPolymorphicRootValue);
            state.Initialize(polymorphicTypeInfo);

            bool success = polymorphicTypeInfo.Converter.WriteCoreAsObject(writer, value, jsonTypeInfo.Options, ref state);
            Debug.Assert(success);
            writer.Flush();
        }

        /// <summary>
        /// Streaming root-level serialization helper.
        /// </summary>
        private static bool WriteCore<TValue>(Utf8JsonWriter writer, in TValue value, JsonTypeInfo jsonTypeInfo, ref WriteStack state)
        {
            Debug.Assert(state.SupportContinuation);

            bool isFinalBlock;
            if (jsonTypeInfo is JsonTypeInfo<TValue> typedInfo)
            {
                isFinalBlock = typedInfo.EffectiveConverter.WriteCore(writer, value, jsonTypeInfo.Options, ref state);
            }
            else
            {
                // The non-generic API was called.
                isFinalBlock = jsonTypeInfo.Converter.WriteCoreAsObject(writer, value, jsonTypeInfo.Options, ref state);
            }

            writer.Flush();
            return isFinalBlock;
        }

        private static JsonTypeInfo ResolvePolymorphicTypeInfo<TValue>(in TValue value, JsonTypeInfo jsonTypeInfo, out bool isPolymorphicType)
        {
            if (
#if NETCOREAPP
                !typeof(TValue).IsValueType &&
#endif
                jsonTypeInfo.Converter.CanBePolymorphic && value is not null)
            {
                Debug.Assert(typeof(TValue) == typeof(object));

                Type runtimeType = value.GetType();
                if (runtimeType != jsonTypeInfo.Type)
                {
                    isPolymorphicType = true;
                    return jsonTypeInfo.Options.GetTypeInfoForRootType(runtimeType);
                }
            }

            isPolymorphicType = false;
            return jsonTypeInfo;
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
    }
}
