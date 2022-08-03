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
        /// <typeparam name="TValue">The type to deserialize the JSON value into.</typeparam>
        /// <returns>A <typeparamref name="TValue"/> representation of the JSON value.</returns>
        /// <param name="reader">The reader to read.</param>
        /// <param name="options">Options to control the serializer behavior during reading.</param>
        /// <exception cref="JsonException">
        /// The JSON is invalid,
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
        ///     Upon completion of this method, <paramref name="reader"/> will be positioned at the
        ///     final token in the JSON value. If an exception is thrown, the reader is reset to
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
        [RequiresDynamicCode(SerializationRequiresDynamicCodeMessage)]
        public static TValue? Deserialize<TValue>(ref Utf8JsonReader reader, JsonSerializerOptions? options = null)
        {
            JsonTypeInfo jsonTypeInfo = GetTypeInfo(options, typeof(TValue));
            return Read<TValue>(ref reader, jsonTypeInfo);
        }

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
        /// The JSON is invalid,
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
        ///     Upon completion of this method, <paramref name="reader"/> will be positioned at the
        ///     final token in the JSON value. If an exception is thrown, the reader is reset to
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
        [RequiresDynamicCode(SerializationRequiresDynamicCodeMessage)]
        public static object? Deserialize(ref Utf8JsonReader reader, Type returnType, JsonSerializerOptions? options = null)
        {
            if (returnType is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(returnType));
            }

            JsonTypeInfo jsonTypeInfo = GetTypeInfo(options, returnType);
            return Read<object?>(ref reader, jsonTypeInfo);
        }

        /// <summary>
        /// Reads one JSON value (including objects or arrays) from the provided reader into a <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The type to deserialize the JSON value into.</typeparam>
        /// <returns>A <typeparamref name="TValue"/> representation of the JSON value.</returns>
        /// <param name="reader">The reader to read.</param>
        /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
        /// <exception cref="JsonException">
        /// The JSON is invalid,
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
        ///     Upon completion of this method, <paramref name="reader"/> will be positioned at the
        ///     final token in the JSON value. If an exception is thrown, the reader is reset to
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
            if (jsonTypeInfo is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(jsonTypeInfo));
            }

            jsonTypeInfo.EnsureConfigured();
            return Read<TValue>(ref reader, jsonTypeInfo);
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
        /// The JSON is invalid,
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
        ///     Upon completion of this method, <paramref name="reader"/> will be positioned at the
        ///     final token in the JSON value. If an exception is thrown, the reader is reset to
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
            if (returnType is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(returnType));
            }
            if (context is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(context));
            }

            return Read<object>(ref reader, GetTypeInfo(context, returnType));
        }

        private static TValue? Read<TValue>(ref Utf8JsonReader reader, JsonTypeInfo jsonTypeInfo)
        {
            Debug.Assert(jsonTypeInfo.IsConfigured);

            if (reader.CurrentState.Options.CommentHandling == JsonCommentHandling.Allow)
            {
                ThrowHelper.ThrowArgumentException_SerializerDoesNotSupportComments(nameof(reader));
            }

            ReadStack state = default;
            state.Initialize(jsonTypeInfo);
            Utf8JsonReader restore = reader;

            try
            {
                Utf8JsonReader scopedReader = GetReaderScopedToNextValue(ref reader, ref state);
                return ReadCore<TValue>(ref scopedReader, jsonTypeInfo, ref state);
            }
            catch (JsonException)
            {
                reader = restore;
                throw;
            }
        }

        private static Utf8JsonReader GetReaderScopedToNextValue(ref Utf8JsonReader reader, ref ReadStack state)
        {
            // Advances the provided reader, validating that it is pointing to a complete JSON value.
            // If successful, returns a new Utf8JsonReader that is scoped to the next value, reusing existing buffers.

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

                        Debug.Assert(reader.TokenType is JsonTokenType.EndObject or JsonTokenType.EndArray);
                        break;

                    // Single-token values
                    case JsonTokenType.Number:
                    case JsonTokenType.True:
                    case JsonTokenType.False:
                    case JsonTokenType.Null:
                        if (reader.HasValueSequence)
                        {
                            valueSequence = reader.ValueSequence;
                        }
                        else
                        {
                            valueSpan = reader.ValueSpan;
                        }

                        break;

                    // String's ValueSequence/ValueSpan omits the quotes, we need them back.
                    case JsonTokenType.String:
                        ReadOnlySequence<byte> originalSequence = reader.OriginalSequence;

                        if (originalSequence.IsEmpty)
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
                            long payloadLength = reader.HasValueSequence
                                ? reader.ValueSequence.Length + 2
                                : reader.ValueSpan.Length + 2;

                            valueSequence = originalSequence.Slice(reader.TokenStartIndex, payloadLength);
                            Debug.Assert(
                                valueSequence.First.Span[0] == (byte)'"',
                                $"Calculated sequence starts with {valueSequence.First.Span[0]}");

                            Debug.Assert(
                                valueSequence.ToArray()[payloadLength - 1] == (byte)'"',
                                $"Calculated sequence ends with {valueSequence.ToArray()[payloadLength - 1]}");
                        }

                        break;

                    default:
                        byte displayByte = reader.HasValueSequence
                            ? reader.ValueSequence.First.Span[0]
                            : reader.ValueSpan[0];

                        ThrowHelper.ThrowJsonReaderException(
                            ref reader,
                            ExceptionResource.ExpectedStartOfValueNotFound,
                            displayByte);

                        break;
                }
            }
            catch (JsonReaderException ex)
            {
                // Re-throw with Path information.
                ThrowHelper.ReThrowWithPath(ref state, ex);
            }

            Debug.Assert(!valueSpan.IsEmpty ^ !valueSequence.IsEmpty);

            return valueSpan.IsEmpty
                ? new Utf8JsonReader(valueSequence, reader.CurrentState.Options)
                : new Utf8JsonReader(valueSpan, reader.CurrentState.Options);
        }
    }
}
