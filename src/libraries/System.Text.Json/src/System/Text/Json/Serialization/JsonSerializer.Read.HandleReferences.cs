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
        private static void ReadCoreRef(
            JsonSerializerOptions options,
            ref Utf8JsonReader reader,
            ref ReadStack readStack)
        {
            try
            {
                JsonReaderState initialState = default;
                long initialBytesConsumed = default;

                while (true)
                {
                    if (readStack.ReadAhead)
                    {
                        // When we're reading ahead we always have to save the state
                        // as we don't know if the next token is an opening object or
                        // array brace.
                        initialState = reader.CurrentState;
                        initialBytesConsumed = reader.BytesConsumed;
                    }

                    if (!reader.Read())
                    {
                        // Need more data
                        break;
                    }

                    JsonTokenType tokenType = reader.TokenType;

                    if (readStack.Current.MetadataProperty == MetadataPropertyName.Values)
                    {
                        if (tokenType != JsonTokenType.StartArray)
                        {
                            throw new JsonException("Invalid array for $values property.");
                        }
                        else
                        {
                            readStack.Current.MetadataProperty = MetadataPropertyName.NoMetadata;
                        }
                    }

                    if (JsonHelpers.IsInRangeInclusive(tokenType, JsonTokenType.String, JsonTokenType.False))
                    {
                        Debug.Assert(tokenType == JsonTokenType.String || tokenType == JsonTokenType.Number || tokenType == JsonTokenType.True || tokenType == JsonTokenType.False);

                        //HandleValue(tokenType, options, ref reader, ref readStack);
                        HandleValueRef(tokenType, options, ref reader, ref readStack);
                    }
                    else if (tokenType == JsonTokenType.PropertyName)
                    {
                        HandlePropertyNameRef(options, ref reader, ref readStack);
                    }
                    else if (tokenType == JsonTokenType.StartObject)
                    {
                        if (readStack.Current.SkipProperty)
                        {
                            readStack.Push();
                            readStack.Current.Drain = true;
                        }
                        else if (readStack.Current.IsProcessingValue())
                        {
                            if (!HandleObjectAsValue(tokenType, options, ref reader, ref readStack, ref initialState, initialBytesConsumed))
                            {
                                // Need more data
                                break;
                            }
                        }
                        else if (readStack.Current.IsProcessingDictionary())
                        {
                            HandleStartDictionaryRef(options, ref readStack);
                        }
                        else
                        {
                            HandleStartObjectRef(options, ref readStack);
                        }
                    }
                    else if (tokenType == JsonTokenType.EndObject)
                    {
                        if (readStack.Current.ShouldHandleReference)
                        {
                            HandleReference(options, ref readStack, ref reader);
                        }
                        else if (readStack.Current.Drain)
                        {
                            readStack.Pop();

                            // Clear the current property in case it is a dictionary, since dictionaries must have EndProperty() called when completed.
                            // A non-dictionary property can also have EndProperty() called when completed, although it is redundant.
                            readStack.Current.EndProperty();
                        }
                        else if (readStack.Current.IsProcessingDictionary())
                        {
                            HandleEndDictionaryRef(options, ref readStack);
                        }
                        else
                        {
                            HandleEndObjectRef(ref readStack);
                        }
                    }
                    else if (tokenType == JsonTokenType.StartArray)
                    {
                        if (!readStack.Current.IsProcessingValue())
                        {
                            HandleStartArray(options, ref readStack);
                        }
                        else if (!HandleObjectAsValue(tokenType, options, ref reader, ref readStack, ref initialState, initialBytesConsumed))
                        {
                            // Need more data
                            break;
                        }
                    }
                    else if (tokenType == JsonTokenType.EndArray)
                    {
                        HandleEndArray(options, ref readStack);
                    }
                    else if (tokenType == JsonTokenType.Null)
                    {
                        HandleNull(options, ref reader, ref readStack);
                    }
                }
            }
            catch (JsonReaderException ex)
            {
                // Re-throw with Path information.
                ThrowHelper.ReThrowWithPath(readStack, ex);
            }
            catch (FormatException ex) when (ex.Source == ThrowHelper.ExceptionSourceValueToRethrowAsJsonException)
            {
                ThrowHelper.ReThrowWithPath(readStack, reader, ex);
            }
            catch (InvalidOperationException ex) when (ex.Source == ThrowHelper.ExceptionSourceValueToRethrowAsJsonException)
            {
                ThrowHelper.ReThrowWithPath(readStack, reader, ex);
            }
            catch (JsonException ex)
            {
                ThrowHelper.AddExceptionInformation(readStack, reader, ex);
                throw;
            }

            readStack.BytesConsumed += reader.BytesConsumed;
        }
    }
}
