// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Converts an object or value to or from JSON.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> to convert.</typeparam>
    public abstract class JsonConverter<T> : JsonConverter
    {
        private Type _typeToConvert = typeof(T);

        /// <summary>
        /// When overidden, constructs a new <see cref="JsonConverter{T}"/> instance.
        /// </summary>
        protected internal JsonConverter()
        {
            // Today only typeof(object) can have polymorphic writes.
            // In the future, this will be check for !IsSealed (and excluding value types).
            CanBePolymorphic = TypeToConvert == typeof(object);

            IsInternalConverter = GetType().Assembly == typeof(JsonConverter).Assembly;
            CanUseDirectReadOrWrite = !CanBePolymorphic && IsInternalConverter && ClassType == ClassType.Value;
        }

        /// <summary>
        /// Determines whether the type can be converted.
        /// </summary>
        /// <remarks>
        /// The default implementation is to return True when <paramref name="typeToConvert"/> equals typeof(T).
        /// </remarks>
        /// <param name="typeToConvert"></param>
        /// <returns>True if the type can be converted, False otherwise.</returns>
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(T);
        }

        internal override ClassType ClassType => ClassType.Value;

        internal override sealed JsonPropertyInfo CreateJsonPropertyInfo()
        {
            return new JsonPropertyInfo<T>();
        }

        internal override Type? ElementType => null;

        /// <summary>
        /// Is the converter built-in.
        /// </summary>
        internal bool IsInternalConverter;

        // This non-generic API is sealed as it just forwards to the generic version.
        internal override sealed bool TryWriteAsObject(Utf8JsonWriter writer, object? value, JsonSerializerOptions options, ref WriteStack state)
        {
            T valueOfT = (T)value!;
            return TryWrite(writer, valueOfT, options, ref state);
        }

        // Provide a default implementation for value converters.
        internal virtual bool OnTryWrite(Utf8JsonWriter writer, T value, JsonSerializerOptions options, ref WriteStack state)
        {
            Write(writer, value, options);
            return true;
        }

        // This non-generic API is sealed as it just forwards to the generic version.
        internal override sealed bool TryReadAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out object? value)
        {
            bool success = TryRead(ref reader, typeToConvert, options, ref state, out T valueOfT);
            if (success)
            {
                value = valueOfT;
            }
            else
            {
                value = default;
            }

            return success;
        }

        // Provide a default implementation for value converters.
        internal virtual bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out T value)
        {
            value = Read(ref reader, typeToConvert, options);
            return true;
        }

        /// <summary>
        /// Read and convert the JSON to T.
        /// </summary>
        /// <remarks>
        /// A converter may throw any Exception, but should throw <cref>JsonException</cref> when the JSON is invalid.
        /// </remarks>
        /// <param name="reader">The <see cref="Utf8JsonReader"/> to read from.</param>
        /// <param name="typeToConvert">The <see cref="Type"/> being converted.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> being used.</param>
        /// <returns>The value that was converted.</returns>
        public abstract T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options);

        internal bool TryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out T value)
        {
            if (ClassType == ClassType.Value)
            {
                // A value converter should never be within a continuation.
                Debug.Assert(!state.IsContinuation);

                // For perf and converter simplicity, handle null here instead of forwarding to the converter.
                if (reader.TokenType == JsonTokenType.Null && !HandleNullValue)
                {
                    value = default!;
                    return true;
                }

#if !DEBUG
                // For performance, only perform validation on internal converters on debug builds.
                if (IsInternalConverter)
                {
                    value = Read(ref reader, typeToConvert, options);
                }
                else
#endif
                {
                    state.Current.OriginalPropertyTokenType = reader.TokenType;
                    state.Current.OriginalPropertyDepth = reader.CurrentDepth;
                    state.Current.OriginalPropertyBytesConsumed = reader.BytesConsumed;

                    value = Read(ref reader, typeToConvert, options);
                    VerifyRead(
                        state.Current.OriginalPropertyTokenType,
                        state.Current.OriginalPropertyDepth,
                        state.Current.OriginalPropertyBytesConsumed != reader.BytesConsumed,
                        ref reader);
                }

                return true;
            }

            bool success;

            // Remember if we were a continuation here since Push() may affect IsContinuation.
            bool wasContinuation = state.IsContinuation;

            state.Push();

#if !DEBUG
            // For performance, only perform validation on internal converters on debug builds.
            if (IsInternalConverter)
            {
                if (reader.TokenType == JsonTokenType.Null && !HandleNullValue && !wasContinuation)
                {
                    // For perf and converter simplicity, handle null here instead of forwarding to the converter.
                    value = default!;
                    success = true;
                }
                else
                {
                    success = OnTryRead(ref reader, typeToConvert, options, ref state, out value);
                }
            }
            else
#endif
            {
                if (!wasContinuation)
                {
                    // For perf and converter simplicity, handle null here instead of forwarding to the converter.
                    if (reader.TokenType == JsonTokenType.Null && !HandleNullValue)
                    {
                        value = default!;
                        state.Pop(true);
                        return true;
                    }

                    Debug.Assert(state.Current.OriginalTokenType == JsonTokenType.None);
                    state.Current.OriginalTokenType = reader.TokenType;

                    Debug.Assert(state.Current.OriginalDepth == 0);
                    state.Current.OriginalDepth = reader.CurrentDepth;
                }

                success = OnTryRead(ref reader, typeToConvert, options, ref state, out value);
                if (success)
                {
                    if (state.IsContinuation)
                    {
                        // The resumable converter did not forward to the next converter that previously returned false.
                        ThrowHelper.ThrowJsonException_SerializationConverterRead(this);
                    }

                    VerifyRead(
                        state.Current.OriginalTokenType,
                        state.Current.OriginalDepth,
                        hasConsumedAnyBytes: true,
                        ref reader);

                    // No need to clear state.Current.* since a stack pop will occur.
                }
            }

            state.Pop(success);
            return success;
        }

        internal bool TryWrite(Utf8JsonWriter writer, T value, JsonSerializerOptions options, ref WriteStack state)
        {
            if (writer.CurrentDepth >= options.EffectiveMaxDepth)
            {
                ThrowHelper.ThrowInvalidOperationException_SerializerCycleDetected(options.MaxDepth);
            }

            if (CanBePolymorphic)
            {
                if (value == null)
                {
                    writer.WriteNullValue();
                    return true;
                }

                Type type = value.GetType();
                if (type == typeof(object))
                {
                    writer.WriteStartObject();
                    writer.WriteEndObject();
                    return true;
                }

                if (type != TypeToConvert)
                {
                    JsonConverter jsonConverter = state.Current.InitializeReEntry(type, options);
                    if (jsonConverter != this)
                    {
                        // We found a different converter; forward to that.
                        return jsonConverter.TryWriteAsObject(writer, value, options, ref state);
                    }
                }
            }

            if (ClassType == ClassType.Value)
            {
                if (!state.IsContinuation)
                {
                    state.Current.OriginalPropertyDepth = writer.CurrentDepth;
                }

                Write(writer, value, options);
                VerifyWrite(state.Current.OriginalPropertyDepth, writer);

                return true;
            }

            bool isContinuation = state.IsContinuation;

            state.Push();

            if (!isContinuation)
            {
                Debug.Assert(state.Current.OriginalDepth == 0);
                state.Current.OriginalDepth = writer.CurrentDepth;
            }

            bool success = OnTryWrite(writer, value, options, ref state);
            if (success)
            {
                VerifyWrite(state.Current.OriginalDepth, writer);
                // No need to clear state.Current.OriginalDepth since a stack pop will occur.
            }

            state.Pop(success);

            return success;
        }

        internal bool TryWriteDataExtensionProperty(Utf8JsonWriter writer, T value, JsonSerializerOptions options, ref WriteStack state)
        {
            Debug.Assert(this is JsonDictionaryConverter<T>);

            if (writer.CurrentDepth >= options.EffectiveMaxDepth)
            {
                ThrowHelper.ThrowInvalidOperationException_SerializerCycleDetected(options.MaxDepth);
            }

            bool success;
            JsonDictionaryConverter<T> dictionaryConverter = (JsonDictionaryConverter<T>)this;

            if (ClassType == ClassType.Value)
            {
                if (!state.IsContinuation)
                {
                    state.Current.OriginalPropertyDepth = writer.CurrentDepth;
                }

                // Ignore the naming policy for extension data.
                state.Current.IgnoreDictionaryKeyPolicy = true;

                success = dictionaryConverter.OnWriteResume(writer, value, options, ref state);
                if (success)
                {
                    VerifyWrite(state.Current.OriginalPropertyDepth, writer);
                }
            }
            else
            {
                bool isContinuation = state.IsContinuation;

                state.Push();

                if (!isContinuation)
                {
                    Debug.Assert(state.Current.OriginalDepth == 0);
                    state.Current.OriginalDepth = writer.CurrentDepth;
                }

                // Ignore the naming policy for extension data.
                state.Current.IgnoreDictionaryKeyPolicy = true;

                success = dictionaryConverter.OnWriteResume(writer, value, options, ref state);
                if (success)
                {
                    VerifyWrite(state.Current.OriginalDepth, writer);
                }

                state.Pop(success);
            }

            return success;
        }

        internal override sealed Type TypeToConvert => _typeToConvert;

        internal void VerifyRead(JsonTokenType tokenType, int depth, bool hasConsumedAnyBytes, ref Utf8JsonReader reader)
        {
            switch (tokenType)
            {
                case JsonTokenType.StartArray:
                    if (reader.TokenType != JsonTokenType.EndArray)
                    {
                        ThrowHelper.ThrowJsonException_SerializationConverterRead(this);
                    }
                    else if (depth != reader.CurrentDepth)
                    {
                        ThrowHelper.ThrowJsonException_SerializationConverterRead(this);
                    }

                    break;

                case JsonTokenType.StartObject:
                    if (reader.TokenType != JsonTokenType.EndObject)
                    {
                        ThrowHelper.ThrowJsonException_SerializationConverterRead(this);
                    }
                    else if (depth != reader.CurrentDepth)
                    {
                        ThrowHelper.ThrowJsonException_SerializationConverterRead(this);
                    }

                    break;

                default:
                    // A non-array or non-object should not make any additional reads.
                    if (hasConsumedAnyBytes)
                    {
                        ThrowHelper.ThrowJsonException_SerializationConverterRead(this);
                    }

                    // Should not be possible to change token type.
                    Debug.Assert(reader.TokenType == tokenType);

                    break;
            }
        }

        internal void VerifyWrite(int originalDepth, Utf8JsonWriter writer)
        {
            if (originalDepth != writer.CurrentDepth)
            {
                ThrowHelper.ThrowJsonException_SerializationConverterWrite(this);
            }
        }

        /// <summary>
        /// Write the value as JSON.
        /// </summary>
        /// <remarks>
        /// A converter may throw any Exception, but should throw <cref>JsonException</cref> when the JSON
        /// cannot be created.
        /// </remarks>
        /// <param name="writer">The <see cref="Utf8JsonWriter"/> to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> being used.</param>
        public abstract void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options);
    }
}
