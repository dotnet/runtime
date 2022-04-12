// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        private static bool WriteCore<TValue>(
            JsonConverter jsonConverter,
            Utf8JsonWriter writer,
            in TValue value,
            JsonSerializerOptions options,
            ref WriteStack state)
        {
            Debug.Assert(writer != null);

            bool success;

            if (jsonConverter is JsonConverter<TValue> converter)
            {
                // Call the strongly-typed WriteCore that will not box structs.
                success = converter.WriteCore(writer, value, options, ref state);
            }
            else
            {
                // The non-generic API was called or we have a polymorphic case where TValue is not equal to the T in JsonConverter<T>.
                success = jsonConverter.WriteCoreAsObject(writer, value, options, ref state);
            }

            writer.Flush();
            return success;
        }

        private static void WriteUsingGeneratedSerializer<TValue>(Utf8JsonWriter writer, in TValue value, JsonTypeInfo jsonTypeInfo)
        {
            Debug.Assert(writer != null);

            if (jsonTypeInfo.HasSerialize &&
                jsonTypeInfo is JsonTypeInfo<TValue> typedInfo &&
                typedInfo.Options.JsonSerializerContext?.CanUseSerializationLogic == true)
            {
                Debug.Assert(typedInfo.SerializeHandler != null);
                typedInfo.SerializeHandler(writer, value);
                writer.Flush();
            }
            else
            {
                WriteUsingSerializer(writer, value, jsonTypeInfo);
            }
        }

        private static void WriteUsingSerializer<TValue>(Utf8JsonWriter writer, in TValue value, JsonTypeInfo jsonTypeInfo)
        {
            Debug.Assert(writer != null);

            Debug.Assert(!jsonTypeInfo.HasSerialize ||
                jsonTypeInfo is not JsonTypeInfo<TValue> ||
                jsonTypeInfo.Options.JsonSerializerContext == null ||
                !jsonTypeInfo.Options.JsonSerializerContext.CanUseSerializationLogic,
                "Incorrect method called. WriteUsingGeneratedSerializer() should have been called instead.");

            WriteStack state = default;
            jsonTypeInfo.EnsureConfigured();
            state.Initialize(jsonTypeInfo, supportContinuation: false, supportAsync: false);

            JsonConverter converter = jsonTypeInfo.PropertyInfoForTypeInfo.ConverterBase;
            Debug.Assert(converter != null);
            Debug.Assert(jsonTypeInfo.Options != null);

            // For performance, the code below is a lifted WriteCore() above.
            if (converter is JsonConverter<TValue> typedConverter)
            {
                // Call the strongly-typed WriteCore that will not box structs.
                typedConverter.WriteCore(writer, value, jsonTypeInfo.Options, ref state);
            }
            else
            {
                // The non-generic API was called or we have a polymorphic case where TValue is not equal to the T in JsonConverter<T>.
                converter.WriteCoreAsObject(writer, value, jsonTypeInfo.Options, ref state);
            }

            writer.Flush();
        }

        private static Type GetRuntimeType<TValue>(in TValue value)
        {
            Type type = typeof(TValue);
            if (type == JsonTypeInfo.ObjectType && value is not null)
            {
                type = value.GetType();
            }

            return type;
        }

        private static Type GetRuntimeTypeAndValidateInputType(object? value, Type inputType!!)
        {
            if (value is not null)
            {
                Type runtimeType = value.GetType();
                if (!inputType.IsAssignableFrom(runtimeType))
                {
                    ThrowHelper.ThrowArgumentException_DeserializeWrongType(inputType, value);
                }

                if (inputType == JsonTypeInfo.ObjectType)
                {
                    return runtimeType;
                }
            }

            return inputType;
        }
    }
}
