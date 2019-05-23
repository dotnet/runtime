// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections;
using System.Diagnostics;

namespace System.Text.Json.Serialization
{
    public static partial class JsonSerializer
    {
        private static void HandlePropertyName(
            JsonSerializerOptions options,
            ref Utf8JsonReader reader,
            ref ReadStack state)
        {
            if (state.Current.Drain)
            {
                return;
            }

            Debug.Assert(state.Current.ReturnValue != default);
            Debug.Assert(state.Current.JsonClassInfo != default);

            if (state.Current.IsProcessingDictionary)
            {
                if (ReferenceEquals(state.Current.JsonClassInfo.DataExtensionProperty, state.Current.JsonPropertyInfo))
                {
                    ReadOnlySpan<byte> propertyName = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;

                    // todo: use a cleaner call to get the unescaped string once https://github.com/dotnet/corefx/issues/35386 is implemented.
                    if (reader._stringHasEscaping)
                    {
                        int idx = propertyName.IndexOf(JsonConstants.BackSlash);
                        Debug.Assert(idx != -1);
                        propertyName = GetUnescapedString(propertyName, idx);
                    }

                    ProcessMissingProperty(propertyName, options, ref reader, ref state);
                }
                else
                {
                    string keyName = reader.GetString();
                    if (options.DictionaryKeyPolicy != null)
                    {
                        keyName = options.DictionaryKeyPolicy.ConvertName(keyName);
                    }

                    if (state.Current.IsDictionary)
                    {
                        state.Current.JsonPropertyInfo = state.Current.JsonClassInfo.GetPolicyProperty();
                    }

                    Debug.Assert(state.Current.IsDictionary ||
                        (state.Current.IsDictionaryProperty && state.Current.JsonPropertyInfo != null));

                    state.Current.KeyName = keyName;
                }
            }
            else
            {
                state.Current.ResetProperty();

                ReadOnlySpan<byte> propertyName = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
                if (reader._stringHasEscaping)
                {
                    int idx = propertyName.IndexOf(JsonConstants.BackSlash);
                    Debug.Assert(idx != -1);
                    propertyName = GetUnescapedString(propertyName, idx);
                }

                state.Current.JsonPropertyInfo = state.Current.JsonClassInfo.GetProperty(options, propertyName, ref state.Current);
                if (state.Current.JsonPropertyInfo == null)
                {
                    if (state.Current.JsonClassInfo.DataExtensionProperty == null)
                    {
                        state.Current.JsonPropertyInfo = JsonPropertyInfo.s_missingProperty;
                    }
                    else
                    {
                        ProcessMissingProperty(propertyName, options, ref reader, ref state);
                    }
                }
                else
                {
                    state.Current.PropertyIndex++;
                }
            }
        }

        private static void ProcessMissingProperty(
            ReadOnlySpan<byte> unescapedPropertyName,
            JsonSerializerOptions options,
            ref Utf8JsonReader reader,
            ref ReadStack state)
        {
            JsonPropertyInfo jsonPropertyInfo = state.Current.JsonClassInfo.DataExtensionProperty;

            Debug.Assert(jsonPropertyInfo != null);
            Debug.Assert(state.Current.ReturnValue != null);

            IDictionary extensionData = (IDictionary)jsonPropertyInfo.GetValueAsObject(state.Current.ReturnValue);
            if (extensionData == null)
            {
                // Create the appropriate dictionary type. We already verified the types.
                Debug.Assert(jsonPropertyInfo.DeclaredPropertyType.IsGenericType);
                Debug.Assert(jsonPropertyInfo.DeclaredPropertyType.GetGenericArguments().Length == 2);
                Debug.Assert(jsonPropertyInfo.DeclaredPropertyType.GetGenericArguments()[0].UnderlyingSystemType == typeof(string));
                Debug.Assert(
                    jsonPropertyInfo.DeclaredPropertyType.GetGenericArguments()[1].UnderlyingSystemType == typeof(object) ||
                    jsonPropertyInfo.DeclaredPropertyType.GetGenericArguments()[1].UnderlyingSystemType == typeof(JsonElement));

                extensionData = (IDictionary)jsonPropertyInfo.RuntimeClassInfo.CreateObject();
                jsonPropertyInfo.SetValueAsObject(state.Current.ReturnValue, extensionData);
            }

            JsonElement jsonElement;
            using (JsonDocument jsonDocument = JsonDocument.ParseValue(ref reader))
            {
                jsonElement = jsonDocument.RootElement.Clone();
            }

            string keyName = JsonHelpers.Utf8GetString(unescapedPropertyName);

            // Currently we don't apply any naming policy. If we do, we'd have to pass it onto the JsonDocument.

            extensionData.Add(keyName, jsonElement);
        }
    }
}
