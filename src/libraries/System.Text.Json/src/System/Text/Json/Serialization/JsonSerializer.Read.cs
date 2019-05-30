// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections;
using System.Diagnostics;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Provides functionality to serialize objects or value types to JSON and
    /// deserialize JSON into objects or value types.
    /// </summary>
    public static partial class JsonSerializer
    {
        private static void ReadCore(
            JsonSerializerOptions options,
            ref Utf8JsonReader reader,
            ref ReadStack state)
        {
            try
            {
                while (reader.Read())
                {
                    JsonTokenType tokenType = reader.TokenType;

                    if (JsonHelpers.IsInRangeInclusive(tokenType, JsonTokenType.String, JsonTokenType.False))
                    {
                        Debug.Assert(tokenType == JsonTokenType.String || tokenType == JsonTokenType.Number || tokenType == JsonTokenType.True || tokenType == JsonTokenType.False);

                        if (HandleValue(tokenType, options, ref reader, ref state))
                        {
                            continue;
                        }
                    }
                    else if (tokenType == JsonTokenType.PropertyName)
                    {
                        HandlePropertyName(options, ref reader, ref state);
                    }
                    else if (tokenType == JsonTokenType.StartObject)
                    {
                        if (state.Current.SkipProperty)
                        {
                            state.Push();
                            state.Current.Drain = true;
                        }
                        else if (state.Current.IsProcessingValue)
                        {
                            if (HandleValue(tokenType, options, ref reader, ref state))
                            {
                                continue;
                            }
                        }
                        else if (state.Current.IsProcessingDictionary)
                        {
                            HandleStartDictionary(options, ref reader, ref state);
                        }
                        else
                        {
                            HandleStartObject(options, ref reader, ref state);
                        }
                    }
                    else if (tokenType == JsonTokenType.EndObject)
                    {
                        if (state.Current.Drain)
                        {
                            state.Pop();
                        }
                        else if (state.Current.IsProcessingDictionary)
                        {
                            HandleEndDictionary(options, ref reader, ref state);
                        }
                        else
                        {
                            HandleEndObject(options, ref reader, ref state);
                        }
                    }
                    else if (tokenType == JsonTokenType.StartArray)
                    {
                        if (!state.Current.IsProcessingValue)
                        {
                            HandleStartArray(options, ref reader, ref state);
                        }
                        else if (HandleValue(tokenType, options, ref reader, ref state))
                        {
                            continue;
                        }
                    }
                    else if (tokenType == JsonTokenType.EndArray)
                    {
                        if (HandleEndArray(options, ref reader, ref state))
                        {
                            continue;
                        }
                    }
                    else if (tokenType == JsonTokenType.Null)
                    {
                        if (HandleNull(ref reader, ref state, options))
                        {
                            continue;
                        }
                    }
                }
            }
            catch (JsonReaderException e)
            {
                // Re-throw with Path information.
                ThrowHelper.ReThrowWithPath(e, state.JsonPath);
            }

            return;
        }

        private static ReadOnlySpan<byte> GetUnescapedString(ReadOnlySpan<byte> utf8Source, int idx)
        {
            // The escaped name is always longer than the unescaped, so it is safe to use escaped name for the buffer length.
            int length = utf8Source.Length;
            byte[] pooledName = null;

            Span<byte> unescapedName = length <= JsonConstants.StackallocThreshold ?
                stackalloc byte[length] :
                (pooledName = ArrayPool<byte>.Shared.Rent(length));

            JsonReaderHelper.Unescape(utf8Source, unescapedName, idx, out int written);
            ReadOnlySpan<byte> propertyName = unescapedName.Slice(0, written).ToArray();

            if (pooledName != null)
            {
                // We clear the array because it is "user data" (although a property name).
                new Span<byte>(pooledName, 0, written).Clear();
                ArrayPool<byte>.Shared.Return(pooledName);
            }

            return propertyName;
        }
    }
}
