// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        /// <summary>
        /// Reads one JSON value (including objects or arrays) from the provided reader into a <typeparamref name="TValue"/>.
        /// </summary>
        /// <returns>A <typeparamref name="TValue"/> representation of the JSON value.</returns>
        /// <param name="reader">The reader to read.</param>
        /// <param name="options">Options to control the serializer behavior during reading.</param>
        /// <exception cref="JsonException">
        /// Thrown when the JSON is invalid,
        /// <typeparamref name="TValue"/> is not compatible with the JSON,
        /// or a value could not be read from the reader.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="reader"/> is using unsupported options.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <typeparamref name="TValue"/> or its serializable members.
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     If the <see cref="Utf8JsonReader.TokenType"/> property of <paramref name="reader"/>
        ///     is <see cref="JsonTokenType.PropertyName"/> or <see cref="JsonTokenType.None"/>, the
        ///     reader will be advanced by one call to <see cref="Utf8JsonReader.Read"/> to determine
        ///     the start of the value.
        ///   </para>
        ///
        ///   <para>
        ///     Upon completion of this method <paramref name="reader"/> will be positioned at the
        ///     final token in the JSON value.  If an exception is thrown the reader is reset to
        ///     the state it was in when the method was called.
        ///   </para>
        ///
        ///   <para>
        ///     This method makes a copy of the data the reader acted on, so there is no caller
        ///     requirement to maintain data integrity beyond the return of this method.
        ///   </para>
        ///
        ///   <para>
        ///     The <see cref="JsonReaderOptions"/> used to create the instance of the <see cref="Utf8JsonReader"/> take precedence over the <see cref="JsonSerializerOptions"/> when they conflict.
        ///     Hence, <see cref="JsonReaderOptions.AllowTrailingCommas"/>, <see cref="JsonReaderOptions.MaxDepth"/>, and <see cref="JsonReaderOptions.CommentHandling"/> are used while reading.
        ///   </para>
        /// </remarks>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        public static TValue? Deserialize<TValue>(ref Utf8JsonReader reader, JsonSerializerOptions? options = null)
            => ReadUsingOptions<TValue>(ref reader, typeof(TValue), options);

        /// <summary>
        /// Reads one JSON value (including objects or arrays) from the provided reader into a <paramref name="returnType"/>.
        /// </summary>
        /// <returns>A <paramref name="returnType"/> representation of the JSON value.</returns>
        /// <param name="reader">The reader to read.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="options">Options to control the serializer behavior during reading.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="returnType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="JsonException">
        /// Thrown when the JSON is invalid,
        /// <paramref name="returnType"/> is not compatible with the JSON,
        /// or a value could not be read from the reader.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="reader"/> is using unsupported options.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="returnType"/> or its serializable members.
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     If the <see cref="Utf8JsonReader.TokenType"/> property of <paramref name="reader"/>
        ///     is <see cref="JsonTokenType.PropertyName"/> or <see cref="JsonTokenType.None"/>, the
        ///     reader will be advanced by one call to <see cref="Utf8JsonReader.Read"/> to determine
        ///     the start of the value.
        ///   </para>
        ///
        ///   <para>
        ///     Upon completion of this method <paramref name="reader"/> will be positioned at the
        ///     final token in the JSON value.  If an exception is thrown the reader is reset to
        ///     the state it was in when the method was called.
        ///   </para>
        ///
        ///   <para>
        ///     This method makes a copy of the data the reader acted on, so there is no caller
        ///     requirement to maintain data integrity beyond the return of this method.
        ///   </para>
        ///   <para>
        ///     The <see cref="JsonReaderOptions"/> used to create the instance of the <see cref="Utf8JsonReader"/> take precedence over the <see cref="JsonSerializerOptions"/> when they conflict.
        ///     Hence, <see cref="JsonReaderOptions.AllowTrailingCommas"/>, <see cref="JsonReaderOptions.MaxDepth"/>, and <see cref="JsonReaderOptions.CommentHandling"/> are used while reading.
        ///   </para>
        /// </remarks>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        public static object? Deserialize(ref Utf8JsonReader reader, Type returnType, JsonSerializerOptions? options = null)
        {
            if (returnType == null)
            {
                throw new ArgumentNullException(nameof(returnType));
            }

            return ReadUsingOptions<object?>(ref reader, returnType, options);
        }

        /// <summary>
        /// Reads one JSON value (including objects or arrays) from the provided reader into a <typeparamref name="TValue"/>.
        /// </summary>
        /// <returns>A <typeparamref name="TValue"/> representation of the JSON value.</returns>
        /// <param name="reader">The reader to read.</param>
        /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
        /// <exception cref="JsonException">
        /// Thrown when the JSON is invalid,
        /// <typeparamref name="TValue"/> is not compatible with the JSON,
        /// or a value could not be read from the reader.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="reader"/> is using unsupported options.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <typeparamref name="TValue"/> or its serializable members.
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     If the <see cref="Utf8JsonReader.TokenType"/> property of <paramref name="reader"/>
        ///     is <see cref="JsonTokenType.PropertyName"/> or <see cref="JsonTokenType.None"/>, the
        ///     reader will be advanced by one call to <see cref="Utf8JsonReader.Read"/> to determine
        ///     the start of the value.
        ///   </para>
        ///
        ///   <para>
        ///     Upon completion of this method <paramref name="reader"/> will be positioned at the
        ///     final token in the JSON value.  If an exception is thrown the reader is reset to
        ///     the state it was in when the method was called.
        ///   </para>
        ///
        ///   <para>
        ///     This method makes a copy of the data the reader acted on, so there is no caller
        ///     requirement to maintain data integrity beyond the return of this method.
        ///   </para>
        ///
        ///   <para>
        ///     The <see cref="JsonReaderOptions"/> used to create the instance of the <see cref="Utf8JsonReader"/> take precedence over the <see cref="JsonSerializerOptions"/> when they conflict.
        ///     Hence, <see cref="JsonReaderOptions.AllowTrailingCommas"/>, <see cref="JsonReaderOptions.MaxDepth"/>, and <see cref="JsonReaderOptions.CommentHandling"/> are used while reading.
        ///   </para>
        /// </remarks>
        public static TValue? Deserialize<TValue>(ref Utf8JsonReader reader, JsonTypeInfo<TValue> jsonTypeInfo)
        {
            if (jsonTypeInfo == null)
            {
                throw new ArgumentNullException(nameof(jsonTypeInfo));
            }

            return ReadUsingMetadata<TValue>(ref reader, jsonTypeInfo);
        }

        /// <summary>
        /// Reads one JSON value (including objects or arrays) from the provided reader into a <paramref name="returnType"/>.
        /// </summary>
        /// <returns>A <paramref name="returnType"/> representation of the JSON value.</returns>
        /// <param name="reader">The reader to read.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="returnType"/> or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="JsonException">
        /// Thrown when the JSON is invalid,
        /// <paramref name="returnType"/> is not compatible with the JSON,
        /// or a value could not be read from the reader.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="reader"/> is using unsupported options.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="returnType"/> or its serializable members.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="JsonSerializerContext.GetTypeInfo(Type)"/> method on the provided <paramref name="context"/>
        /// did not return a compatible <see cref="JsonTypeInfo"/> for <paramref name="returnType"/>.
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     If the <see cref="Utf8JsonReader.TokenType"/> property of <paramref name="reader"/>
        ///     is <see cref="JsonTokenType.PropertyName"/> or <see cref="JsonTokenType.None"/>, the
        ///     reader will be advanced by one call to <see cref="Utf8JsonReader.Read"/> to determine
        ///     the start of the value.
        ///   </para>
        ///
        ///   <para>
        ///     Upon completion of this method <paramref name="reader"/> will be positioned at the
        ///     final token in the JSON value.  If an exception is thrown the reader is reset to
        ///     the state it was in when the method was called.
        ///   </para>
        ///
        ///   <para>
        ///     This method makes a copy of the data the reader acted on, so there is no caller
        ///     requirement to maintain data integrity beyond the return of this method.
        ///   </para>
        ///   <para>
        ///     The <see cref="JsonReaderOptions"/> used to create the instance of the <see cref="Utf8JsonReader"/> take precedence over the <see cref="JsonSerializerOptions"/> when they conflict.
        ///     Hence, <see cref="JsonReaderOptions.AllowTrailingCommas"/>, <see cref="JsonReaderOptions.MaxDepth"/>, and <see cref="JsonReaderOptions.CommentHandling"/> are used while reading.
        ///   </para>
        /// </remarks>
        public static object? Deserialize(ref Utf8JsonReader reader, Type returnType, JsonSerializerContext context)
        {
            if (returnType == null)
            {
                throw new ArgumentNullException(nameof(returnType));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return ReadUsingMetadata<object>(ref reader, GetTypeInfo(context, returnType));
        }

        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        private static TValue? ReadUsingOptions<TValue>(ref Utf8JsonReader reader, Type returnType, JsonSerializerOptions? options)
        {
            JsonTypeInfo jsonTypeInfo = GetTypeInfo(returnType, options);
            return ReadUsingMetadata<TValue>(ref reader, jsonTypeInfo);
        }

        private static TValue? ReadUsingMetadata<TValue>(ref Utf8JsonReader reader, JsonTypeInfo jsonTypeInfo)
        {
            ReadStack state = default;
            state.Initialize(jsonTypeInfo);

            JsonReaderState readerState = reader.CurrentState;
            if (readerState.Options.CommentHandling == JsonCommentHandling.Allow)
            {
                throw new ArgumentException(SR.JsonSerializerDoesNotSupportComments, nameof(reader));
            }

            // Value copy to overwrite the ref on an exception and undo the destructive reads.
            Utf8JsonReader restore = reader;

            ReadOnlySpan<byte> valueSpan = default;
            ReadOnlySequence<byte> valueSequence = default;

            try
            {
                switch (reader.TokenType)
                {
                    // A new reader was created and has never been read,
                    // so we need to move to the first token.
                    // (or a reader has terminated and we're about to throw)
                    case JsonTokenType.None:
                    // Using a reader loop the caller has identified a property they wish to
                    // hydrate into a JsonDocument. Move to the value first.
                    case JsonTokenType.PropertyName:
                        {
                            if (!reader.Read())
                            {
                                ThrowHelper.ThrowJsonReaderException(ref reader, ExceptionResource.ExpectedOneCompleteToken);
                            }
                            break;
                        }
                }

                switch (reader.TokenType)
                {
                    // Any of the "value start" states are acceptable.
                    case JsonTokenType.StartObject:
                    case JsonTokenType.StartArray:
                        {
                            long startingOffset = reader.TokenStartIndex;

                            if (!reader.TrySkip())
                            {
                                ThrowHelper.ThrowJsonReaderException(ref reader, ExceptionResource.NotEnoughData);
                            }

                            long totalLength = reader.BytesConsumed - startingOffset;
                            ReadOnlySequence<byte> sequence = reader.OriginalSequence;

                            if (sequence.IsEmpty)
                            {
                                valueSpan = reader.OriginalSpan.Slice(
                                    checked((int)startingOffset),
                                    checked((int)totalLength));
                            }
                            else
                            {
                                valueSequence = sequence.Slice(startingOffset, totalLength);
                            }

                            Debug.Assert(
                                reader.TokenType == JsonTokenType.EndObject ||
                                reader.TokenType == JsonTokenType.EndArray);

                            break;
                        }

                    // Single-token values
                    case JsonTokenType.Number:
                    case JsonTokenType.True:
                    case JsonTokenType.False:
                    case JsonTokenType.Null:
                        {
                            if (reader.HasValueSequence)
                            {
                                valueSequence = reader.ValueSequence;
                            }
                            else
                            {
                                valueSpan = reader.ValueSpan;
                            }

                            break;
                        }
                    // String's ValueSequence/ValueSpan omits the quotes, we need them back.
                    case JsonTokenType.String:
                        {
                            ReadOnlySequence<byte> sequence = reader.OriginalSequence;

                            if (sequence.IsEmpty)
                            {
                                // Since the quoted string fit in a ReadOnlySpan originally
                                // the contents length plus the two quotes can't overflow.
                                int payloadLength = reader.ValueSpan.Length + 2;
                                Debug.Assert(payloadLength > 1);

                                ReadOnlySpan<byte> readerSpan = reader.OriginalSpan;

                                Debug.Assert(
                                    readerSpan[(int)reader.TokenStartIndex] == (byte)'"',
                                    $"Calculated span starts with {readerSpan[(int)reader.TokenStartIndex]}");

                                Debug.Assert(
                                    readerSpan[(int)reader.TokenStartIndex + payloadLength - 1] == (byte)'"',
                                    $"Calculated span ends with {readerSpan[(int)reader.TokenStartIndex + payloadLength - 1]}");

                                valueSpan = readerSpan.Slice((int)reader.TokenStartIndex, payloadLength);
                            }
                            else
                            {
                                long payloadLength = 2;

                                if (reader.HasValueSequence)
                                {
                                    payloadLength += reader.ValueSequence.Length;
                                }
                                else
                                {
                                    payloadLength += reader.ValueSpan.Length;
                                }

                                valueSequence = sequence.Slice(reader.TokenStartIndex, payloadLength);
                                Debug.Assert(
                                    valueSequence.First.Span[0] == (byte)'"',
                                    $"Calculated sequence starts with {valueSequence.First.Span[0]}");

                                Debug.Assert(
                                    valueSequence.ToArray()[payloadLength - 1] == (byte)'"',
                                    $"Calculated sequence ends with {valueSequence.ToArray()[payloadLength - 1]}");
                            }

                            break;
                        }
                    default:
                        {
                            byte displayByte;

                            if (reader.HasValueSequence)
                            {
                                displayByte = reader.ValueSequence.First.Span[0];
                            }
                            else
                            {
                                displayByte = reader.ValueSpan[0];
                            }

                            ThrowHelper.ThrowJsonReaderException(
                                ref reader,
                                ExceptionResource.ExpectedStartOfValueNotFound,
                                displayByte);

                            break;
                        }
                }
            }
            catch (JsonReaderException ex)
            {
                reader = restore;
                // Re-throw with Path information.
                ThrowHelper.ReThrowWithPath(state, ex);
            }

            int length = valueSpan.IsEmpty ? checked((int)valueSequence.Length) : valueSpan.Length;
            byte[] rented = ArrayPool<byte>.Shared.Rent(length);
            Span<byte> rentedSpan = rented.AsSpan(0, length);

            try
            {
                if (valueSpan.IsEmpty)
                {
                    valueSequence.CopyTo(rentedSpan);
                }
                else
                {
                    valueSpan.CopyTo(rentedSpan);
                }

                JsonReaderOptions originalReaderOptions = readerState.Options;

                var newReader = new Utf8JsonReader(rentedSpan, originalReaderOptions);

                JsonConverter jsonConverter = state.Current.JsonPropertyInfo!.ConverterBase;
                TValue? value = ReadCore<TValue>(jsonConverter, ref newReader, jsonTypeInfo.Options, ref state);

                // The reader should have thrown if we have remaining bytes.
                Debug.Assert(newReader.BytesConsumed == length);

                return value;
            }
            catch (JsonException)
            {
                reader = restore;
                throw;
            }
            finally
            {
                rentedSpan.Clear();
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }
}
