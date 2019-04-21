// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Provides functionality to serialize objects or value types to JSON and
    /// deserialize JSON into objects or value types.
    /// </summary>
    public static partial class JsonSerializer
    {
        internal static readonly JsonPropertyInfo s_missingProperty = new JsonPropertyInfoNotNullable<object, object, object>();

        // todo: for readability, refactor this method to split by ClassType(Enumerable, Object, or Value) like Write()
        private static void ReadCore(
            JsonSerializerOptions options,
            ref Utf8JsonReader reader,
            ref ReadStack state)
        {
            while (reader.Read())
            {
                JsonTokenType tokenType = reader.TokenType;

                if (JsonHelpers.IsInRangeInclusive(tokenType, JsonTokenType.String, JsonTokenType.False))
                {
                    Debug.Assert(tokenType == JsonTokenType.String || tokenType == JsonTokenType.Number || tokenType == JsonTokenType.True || tokenType == JsonTokenType.False);

                    if (HandleValue(tokenType, options, ref reader, ref state))
                    {
                        return;
                    }
                }
                else if (tokenType == JsonTokenType.PropertyName)
                {
                    if (!state.Current.Drain)
                    {
                        Debug.Assert(state.Current.ReturnValue != default);
                        Debug.Assert(state.Current.JsonClassInfo != default);

                        ReadOnlySpan<byte> propertyName = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;

                        if (state.Current.IsDictionary())
                        {
                            string keyName = reader.GetString();
                            if (options.DictionaryKeyPolicy != null)
                            {
                                keyName = options.DictionaryKeyPolicy.ConvertName(keyName);
                            }

                            state.Current.JsonPropertyInfo = state.Current.JsonClassInfo.GetPolicyProperty();
                            state.Current.KeyName = keyName;
                        }
                        else
                        {
                            state.Current.JsonPropertyInfo = state.Current.JsonClassInfo.GetProperty(options, propertyName, ref state.Current);
                            if (state.Current.JsonPropertyInfo == null)
                            {
                                state.Current.JsonPropertyInfo = s_missingProperty;
                            }

                            state.Current.PropertyIndex++;
                        }
                    }
                }
                else if (tokenType == JsonTokenType.StartObject)
                {
                    HandleStartObject(options, ref state);
                }
                else if (tokenType == JsonTokenType.EndObject)
                {
                    if (HandleEndObject(options, ref state))
                    {
                        return;
                    }
                }
                else if (tokenType == JsonTokenType.StartArray)
                {
                    HandleStartArray(options, ref reader, ref state);
                }
                else if (tokenType == JsonTokenType.EndArray)
                {
                    if (HandleEndArray(options, ref state))
                    {
                        return;
                    }
                }
                else if (tokenType == JsonTokenType.Null)
                {
                    if (HandleNull(ref reader, ref state, options))
                    {
                        return;
                    }
                }
            }

            return;
        }
    }
}
