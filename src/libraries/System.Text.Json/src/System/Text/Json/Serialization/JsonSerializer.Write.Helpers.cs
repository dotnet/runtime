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
        // Members accessed by the serializer when serializing.
        private const DynamicallyAccessedMemberTypes MembersAccessedOnWrite = DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields;

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

        private static void WriteUsingMetadata<TValue>(Utf8JsonWriter writer, in TValue value, JsonTypeInfo jsonTypeInfo)
        {
            if (jsonTypeInfo is JsonTypeInfo<TValue> typedInfo &&
                typedInfo.Serialize != null &&
                typedInfo.Options._context?.CanUseSerializationLogic == true)
            {
                typedInfo.Serialize(writer, value);
            }
            else
            {
                WriteStack state = default;
                state.Initialize(jsonTypeInfo, supportContinuation: false);

                JsonConverter converter = jsonTypeInfo.PropertyInfoForTypeInfo.ConverterBase;
                Debug.Assert(converter != null);

                Debug.Assert(jsonTypeInfo.Options != null);

                WriteCore(converter, writer, value, jsonTypeInfo.Options, ref state);
            }
        }

        private static Type GetRuntimeType<TValue>(in TValue value)
        {
            if (typeof(TValue) == typeof(object) && value != null)
            {
                return value.GetType();
            }

            return typeof(TValue);
        }

        private static Type GetRuntimeTypeAndValidateInputType(object? value, Type inputType)
        {
            if (inputType == null)
            {
                throw new ArgumentNullException(nameof(inputType));
            }

            if (value != null)
            {
                Type runtimeType = value.GetType();
                if (!inputType.IsAssignableFrom(runtimeType))
                {
                    ThrowHelper.ThrowArgumentException_DeserializeWrongType(inputType, value);
                }

                if (inputType == typeof(object))
                {
                    return runtimeType;
                }
            }

            return inputType;
        }
    }
}
