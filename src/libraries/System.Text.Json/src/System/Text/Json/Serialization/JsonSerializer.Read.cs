// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Json
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
            ref ReadStack readStack)
        {
            try
            {
                JsonReaderState initialState = default;

                while (true)
                {
                    if (readStack.ReadAhead)
                    {
                        // When we're reading ahead we always have to save the state
                        // as we don't know if the next token is an opening object or
                        // array brace.
                        initialState = reader.CurrentState;
                    }

                    if (!reader.Read())
                    {
                        // Need more data
                        break;
                    }

                    JsonTokenType tokenType = reader.TokenType;

                    if (JsonHelpers.IsInRangeInclusive(tokenType, JsonTokenType.String, JsonTokenType.False))
                    {
                        Debug.Assert(tokenType == JsonTokenType.String || tokenType == JsonTokenType.Number || tokenType == JsonTokenType.True || tokenType == JsonTokenType.False);

                        HandleValue(tokenType, options, ref reader, ref readStack);
                    }
                    else if (tokenType == JsonTokenType.PropertyName)
                    {
                        HandlePropertyName(options, ref reader, ref readStack);
                    }
                    else if (tokenType == JsonTokenType.StartObject)
                    {
                        if (readStack.Current.SkipProperty)
                        {
                            readStack.Push();
                            readStack.Current.Drain = true;
                        }
                        else if (readStack.Current.IsProcessingValue)
                        {
                            if (!HandleObjectAsValue(tokenType, options, ref reader, ref readStack, ref initialState))
                            {
                                // Need more data
                                break;
                            }
                        }
                        else if (readStack.Current.IsProcessingDictionary || readStack.Current.IsProcessingImmutableDictionary)
                        {
                            HandleStartDictionary(options, ref reader, ref readStack);
                        }
                        else
                        {
                            HandleStartObject(options, ref reader, ref readStack);
                        }
                    }
                    else if (tokenType == JsonTokenType.EndObject)
                    {
                        if (readStack.Current.Drain)
                        {
                            readStack.Pop();
                        }
                        else if (readStack.Current.IsProcessingDictionary || readStack.Current.IsProcessingImmutableDictionary)
                        {
                            HandleEndDictionary(options, ref reader, ref readStack);
                        }
                        else
                        {
                            HandleEndObject(options, ref reader, ref readStack);
                        }
                    }
                    else if (tokenType == JsonTokenType.StartArray)
                    {
                        if (!readStack.Current.IsProcessingValue)
                        {
                            HandleStartArray(options, ref reader, ref readStack);
                        }
                        else if (!HandleObjectAsValue(tokenType, options, ref reader, ref readStack, ref initialState))
                        {
                            // Need more data
                            break;
                        }
                    }
                    else if (tokenType == JsonTokenType.EndArray)
                    {
                        HandleEndArray(options, ref reader, ref readStack);
                    }
                    else if (tokenType == JsonTokenType.Null)
                    {
                        HandleNull(ref reader, ref readStack, options);
                    }
                }
            }
            catch (JsonReaderException e)
            {
                // Re-throw with Path information.
                ThrowHelper.ReThrowWithPath(e, readStack.JsonPath);
            }

            readStack.BytesConsumed += reader.BytesConsumed;
            return;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HandleObjectAsValue(
            JsonTokenType tokenType,
            JsonSerializerOptions options,
            ref Utf8JsonReader reader,
            ref ReadStack readStack,
            ref JsonReaderState initialState)
        {
            if (readStack.ReadAhead)
            {
                // Attempt to skip to make sure we have all the data we need.
                bool complete = reader.TrySkip();

                // We need to restore the state in all cases as we need to be positioned back before
                // the current token to either attempt to skip again or to actually read the value in
                // HandleValue below.

                reader = new Utf8JsonReader(
                    reader.OriginalSpan.Slice(checked((int)initialState.BytesConsumed)),
                    isFinalBlock: reader.IsFinalBlock,
                    state: initialState);
                Debug.Assert(reader.BytesConsumed == 0);
                readStack.BytesConsumed += initialState.BytesConsumed;

                if (!complete)
                {
                    // Couldn't read to the end of the object, exit out to get more data in the buffer.
                    return false;
                }

                // Success, requeue the reader to the token for HandleValue.
                reader.Read();
                Debug.Assert(tokenType == reader.TokenType);
            }

            HandleValue(tokenType, options, ref reader, ref readStack);
            return true;
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
