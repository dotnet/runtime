// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Converts an object or value to or from JSON.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> to convert.</typeparam>
    public abstract partial class JsonConverter<T> : JsonConverter
    {
        /// <summary>
        /// When overidden, constructs a new <see cref="JsonConverter{T}"/> instance.
        /// </summary>
        protected internal JsonConverter()
        {
            IsValueType = TypeToConvert.IsValueType;
            IsInternalConverter = GetType().Assembly == typeof(JsonConverter).Assembly;

            if (HandleNull)
            {
                HandleNullOnRead = true;
                HandleNullOnWrite = true;
            }
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

        internal override ConverterStrategy ConverterStrategy => ConverterStrategy.Value;

        internal sealed override JsonPropertyInfo CreateJsonPropertyInfo()
        {
            return new JsonPropertyInfo<T>();
        }

        internal override sealed JsonParameterInfo CreateJsonParameterInfo()
        {
            return new JsonParameterInfo<T>();
        }

        internal override Type? KeyType => null;

        internal override Type? ElementType => null;

        /// <summary>
        /// Indicates whether <see langword="null"/> should be passed to the converter on serialization,
        /// and whether <see cref="JsonTokenType.Null"/> should be passed on deserialization.
        /// </summary>
        /// <remarks>
        /// The default value is <see langword="true"/> for converters for value types, and <see langword="false"/> for converters for reference types.
        /// </remarks>
        public virtual bool HandleNull
        {
            get
            {
                // HandleNull is only called by the framework once during initialization and any
                // subsequent calls elsewhere would just re-initialize to the same values (we don't
                // track a "hasInitialized" flag since that isn't necessary).

                // If the type doesn't support null, allow the converter a chance to modify.
                // These semantics are backwards compatible with 3.0.
                HandleNullOnRead = default(T) is not null;

                // The framework handles null automatically on writes.
                HandleNullOnWrite = false;

                return false;
            }
        }

        /// <summary>
        /// Does the converter want to be called when reading null tokens.
        /// </summary>
        internal bool HandleNullOnRead { get; private set; }

        /// <summary>
        /// Does the converter want to be called for null values.
        /// </summary>
        internal bool HandleNullOnWrite { get; private set; }

        // This non-generic API is sealed as it just forwards to the generic version.
        internal sealed override bool TryWriteAsObject(Utf8JsonWriter writer, object? value, JsonSerializerOptions options, ref WriteStack state)
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

        // Provide a default implementation for value converters.
        internal virtual bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out T? value)
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
        public abstract T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options);

        internal bool TryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out T? value)
        {
            // Handle polymorphic deserialization
            if (
#if NET5_0_OR_GREATER
                !typeof(T).IsValueType && // treated as a constant by recent versions of the JIT.
#else
                !IsValueType &&
#endif
                ConverterStrategy != ConverterStrategy.Value)
            {
                JsonTypeInfo jsonTypeInfo = state.PeekNextJsonTypeInfo();
                Debug.Assert(jsonTypeInfo.PropertyInfoForTypeInfo.ConverterBase.TypeToConvert == TypeToConvert);

                if (jsonTypeInfo.TypeDiscriminatorResolver is not null)
                {
                    // The current converter supports polymorphic deserialization
                    JsonConverter? polymorphicConverter = null;

                    switch (state.Current.PolymorphicSerializationState)
                    {
                        case PolymorphicSerializationState.PolymorphicReEntryStarted:
                            // Current frame is already dispatched to a polymorphic converter, abort.
                            break;

                        case PolymorphicSerializationState.PolymorphicReEntrySuspended:
                            // Resuming a polymorphic converter.
                            Debug.Assert(state.IsContinuation);
                            polymorphicConverter = state.Current.ResumePolymorphicReEntry();
                            Debug.Assert(typeToConvert.IsAssignableFrom(polymorphicConverter.TypeToConvert));
                            break;

                        default:
                            Debug.Assert(state.Current.PolymorphicSerializationState == PolymorphicSerializationState.None);

                            // Need to read ahead for the type discriminator before dispatching to the relevant polymorphic converter
                            // Use a copy of the reader to avoid advancing the buffer.
                            Utf8JsonReader readerCopy = reader;
                            if (!JsonSerializer.TryReadTypeDiscriminator(ref readerCopy, out string? typeId))
                            {
                                // Insufficient data in the buffer to read the type discriminator.
                                // Signal to the state that only the read-ahead operation requires more data
                                // and that the Utf8JsonReader state should not be advanced.
                                state.NoReaderAdvanceOnContinuation = true;
                                value = default;
                                return false;
                            }

                            if (state.NoReaderAdvanceOnContinuation)
                            {
                                // the converter was suspended while attempting to read ahead the type discrimator.
                                // Unset the continuation the flag since for all intents and purposes this is the first run of the converter.
                                state.NoReaderAdvanceOnContinuation = false;
                            }

                            if (typeId is not null &&
                                jsonTypeInfo.TypeDiscriminatorResolver.TryResolveTypeByTypeId(typeId, out Type? type) &&
                                type != typeToConvert)
                            {
                                polymorphicConverter = state.Current.InitializePolymorphicReEntry(type, options);
                                Debug.Assert(polymorphicConverter.TypeToConvert == type);
                            }

                            break;
                    }

                    if (polymorphicConverter is not null)
                    {
                        bool success2 = polymorphicConverter.TryReadAsObject(ref reader, options, ref state, out object? objectValue);
                        value = (T?)objectValue;

                        state.Current.PolymorphicSerializationState = success2
                            ? PolymorphicSerializationState.None
                            : PolymorphicSerializationState.PolymorphicReEntrySuspended;

                        return success2;
                    }
                }
            }

            if (ConverterStrategy == ConverterStrategy.Value)
            {
                // A value converter should never be within a continuation.
                Debug.Assert(!state.IsContinuation);

                // For perf and converter simplicity, handle null here instead of forwarding to the converter.
                if (reader.TokenType == JsonTokenType.Null && !HandleNullOnRead)
                {
                    if (default(T) is not null)
                    {
                        ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                    }
                    else
                    {
                        value = default;
                        return true;
                    }
                }

#if !DEBUG
                // For performance, only perform validation on internal converters on debug builds.
                if (IsInternalConverter)
                {
                    if (state.Current.NumberHandling != null)
                    {
                        value = ReadNumberWithCustomHandling(ref reader, state.Current.NumberHandling.Value, options);
                    }
                    else
                    {
                        value = Read(ref reader, typeToConvert, options);
                    }
                }
                else
#endif
                {
                    JsonTokenType originalPropertyTokenType = reader.TokenType;
                    int originalPropertyDepth = reader.CurrentDepth;
                    long originalPropertyBytesConsumed = reader.BytesConsumed;

                    if (state.Current.NumberHandling != null)
                    {
                        value = ReadNumberWithCustomHandling(ref reader, state.Current.NumberHandling.Value, options);
                    }
                    else
                    {
                        value = Read(ref reader, typeToConvert, options);
                    }

                    VerifyRead(
                        originalPropertyTokenType,
                        originalPropertyDepth,
                        originalPropertyBytesConsumed,
                        isValueConverter: true,
                        ref reader);
                }

                if (options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve &&
                    TypeToConvert == JsonTypeInfo.ObjectType && value is JsonElement element)
                {
                    // Edge case where we want to lookup for a reference when parsing into typeof(object)
                    // instead of return `value` as a JsonElement.
                    if (JsonSerializer.TryGetReferenceFromJsonElement(ref state, element, out object? referenceValue))
                    {
                        value = (T?)referenceValue;
                    }
                }

                return true;
            }

            // Remember if we were a continuation here since Push() may affect IsContinuation.
            bool wasContinuation = state.IsContinuation;

#if DEBUG
            // DEBUG: ensure push/pop operations preserve stack integrity
            JsonTypeInfo originalJsonTypeInfo = state.Current.JsonTypeInfo;
#endif
            state.Push();
            Debug.Assert(state.Current.JsonTypeInfo.Type == TypeToConvert);

            bool success;
#if !DEBUG
            // For performance, only perform validation on internal converters on debug builds.
            if (IsInternalConverter)
            {
                if (reader.TokenType == JsonTokenType.Null && !HandleNullOnRead && !wasContinuation)
                {
                    if (default(T) is not null)
                    {
                        ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                    }

                    // For perf and converter simplicity, handle null here instead of forwarding to the converter.
                    value = default;
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
                    if (reader.TokenType == JsonTokenType.Null && !HandleNullOnRead)
                    {
                        if (default(T) is not null)
                        {
                            ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                        }
                        else
                        {
                            value = default;
                            state.Pop(true);
                            return true;
                        }
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
                        bytesConsumed: 0,
                        isValueConverter: false,
                        ref reader);

                    // No need to clear state.Current.* since a stack pop will occur.
                }
            }

            state.Pop(success);
#if DEBUG
            Debug.Assert(ReferenceEquals(originalJsonTypeInfo, state.Current.JsonTypeInfo));
#endif
            return success;
        }

        internal override sealed bool TryReadAsObject(ref Utf8JsonReader reader, JsonSerializerOptions options, ref ReadStack state, out object? value)
        {
            bool success = TryRead(ref reader, TypeToConvert, options, ref state, out T? typedValue);
            value = typedValue;
            return success;
        }

        internal bool IsNull(T value) => value is null;

        internal bool TryWrite(Utf8JsonWriter writer, in T value, JsonSerializerOptions options, ref WriteStack state)
        {
            if (writer.CurrentDepth >= options.EffectiveMaxDepth)
            {
                ThrowHelper.ThrowJsonException_SerializerCycleDetected(options.EffectiveMaxDepth);
            }

            if (default(T) is null && IsNull(value) && !HandleNullOnWrite)
            {
                // We do not pass null values to converters unless HandleNullOnWrite is true. Null values for properties were
                // already handled in GetMemberAndWriteJson() so we don't need to check for IgnoreNullValues here.
                writer.WriteNullValue();
                return true;
            }

            bool isCycleDetectionReferencePushed = false;

            // Check if converter is eligible for cycle detection or polymorphism.
            // We do not support custom converters, with the exception of System.Object converters.
            // TODO: make ObjectConverter use ConverterStrategy.Object.
            if (
#if NET5_0_OR_GREATER
                !typeof(T).IsValueType && // treated as a constant by recent versions of the JIT.
#else
                !IsValueType &&
#endif
                value is not null &&
                (ConverterStrategy != ConverterStrategy.Value || TypeToConvert == JsonTypeInfo.ObjectType))
            {
                // Only enter reference handling section value is first picked up by a converter:
                // do not run if continuation or if dispatched to a polymorphic converter.
                if (options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.IgnoreCycles &&
                    !state.IsContinuation && state.Current.PolymorphicSerializationState == PolymorphicSerializationState.None)
                {
                    ReferenceResolver resolver = state.ReferenceResolver;

                    // Write null to break reference cycles.
                    if (resolver.ContainsReferenceForCycleDetection(value))
                    {
                        writer.WriteNullValue();
                        return true;
                    }

                    resolver.PushReferenceForCycleDetection(value);
                    isCycleDetectionReferencePushed = true;
                }

                JsonTypeInfo jsonTypeInfo = state.PeekNextJsonTypeInfo();
                Debug.Assert(jsonTypeInfo.PropertyInfoForTypeInfo.ConverterBase.TypeToConvert == TypeToConvert || !IsInternalConverter);

                // TODO: refactor into a standalone TryGetPolymorphicConverter method
                if (jsonTypeInfo.CanBePolymorphic)
                {
                    JsonConverter? polymorphicConverter = null;

                    switch (state.Current.PolymorphicSerializationState)
                    {
                        case PolymorphicSerializationState.PolymorphicReEntryStarted:
                            // Current frame is already dispatched to a polymorphic converter, abort.
                            break;

                        case PolymorphicSerializationState.PolymorphicReEntrySuspended:
                            // Resuming a polymorphic converter.
                            Debug.Assert(state.IsContinuation);
                            polymorphicConverter = state.Current.ResumePolymorphicReEntry();
                            Debug.Assert(value is not null && polymorphicConverter.TypeToConvert.IsAssignableFrom(value.GetType()));
                            break;

                        default:
                            Debug.Assert(state.Current.PolymorphicSerializationState == PolymorphicSerializationState.None);

                            Type type = value.GetType();

                            if (jsonTypeInfo.TypeDiscriminatorResolver is not null)
                            {
                                // Prepare serialization for type discriminator polymorphism:
                                // if the resolver yields a valid typeId dispatch to the converter for the resolved type,
                                // otherwise revert back to using the current converter type and do not serialize polymorphically.
                                Debug.Assert(state.PolymorphicTypeDiscriminator is null);

                                if (jsonTypeInfo.TypeDiscriminatorResolver.TryResolvePolymorphicSubtype(type, out Type? resolvedType, out string? typeId))
                                {
                                    Debug.Assert(resolvedType.IsAssignableFrom(type));

                                    type = resolvedType;
                                    state.PolymorphicTypeDiscriminator = typeId;
                                }
                                else
                                {
                                    type = TypeToConvert;
                                }
                            }

                            // Special handling for System.Object instance
                            if (type == JsonTypeInfo.ObjectType)
                            {
                                writer.WriteStartObject();
                                writer.WriteEndObject();

                                if (isCycleDetectionReferencePushed)
                                {
                                    state.ReferenceResolver.PopReferenceForCycleDetection();
                                }

                                return true;
                            }

                            if (type != TypeToConvert && IsInternalConverter) // TODO: IsInternalConverter should be moved to outer check
                                                                              // Currently used here so that user converters include handling for System.Object instances
                            {
                                // For internal converter only: Handle polymorphic case and get the new converter.
                                // Custom converter, even though polymorphic converter, get called for reading AND writing.
                                polymorphicConverter = state.Current.InitializePolymorphicReEntry(type, options);
                                Debug.Assert(polymorphicConverter.TypeToConvert == type || !polymorphicConverter.IsInternalConverter);

                                // Store the cycle detection in case a continuation is required.
                                state.Current.IsCycleDetectionReferencePushed = isCycleDetectionReferencePushed;
                            }

                            break;
                    }

                    if (polymorphicConverter is not null)
                    {
                        // We found a different converter; forward to that.
                        bool success2 = polymorphicConverter.TryWriteAsObject(writer, value, options, ref state);

                        if (success2)
                        {
                            state.Current.PolymorphicSerializationState = PolymorphicSerializationState.None;
                            state.PolymorphicTypeDiscriminator = null;

                            if (state.Current.IsCycleDetectionReferencePushed)
                            {
                                state.ReferenceResolver.PopReferenceForCycleDetection();
                                state.Current.IsCycleDetectionReferencePushed = false;
                            }
                        }
                        else
                        {
                            state.Current.PolymorphicSerializationState = PolymorphicSerializationState.PolymorphicReEntrySuspended;
                        }

                        return success2;
                    }
                }
            }

            if (ConverterStrategy == ConverterStrategy.Value)
            {
                Debug.Assert(!state.IsContinuation);

                int originalPropertyDepth = writer.CurrentDepth;

                if (state.Current.NumberHandling != null && IsInternalConverterForNumberType)
                {
                    WriteNumberWithCustomHandling(writer, value, state.Current.NumberHandling.Value);
                }
                else
                {
                    Write(writer, value, options);
                }

                VerifyWrite(originalPropertyDepth, writer);
                return true;
            }

            bool isContinuation = state.IsContinuation;

#if DEBUG
            // DEBUG: ensure push/pop operations preserve stack integrity
            JsonTypeInfo originalJsonTypeInfo = state.Current.JsonTypeInfo;
#endif
            state.Push();
            Debug.Assert(state.Current.JsonTypeInfo.Type == TypeToConvert);

            if (!isContinuation)
            {
                Debug.Assert(state.Current.OriginalDepth == 0);
                state.Current.OriginalDepth = writer.CurrentDepth;
                Debug.Assert(!state.Current.IsCycleDetectionReferencePushed);
                state.Current.IsCycleDetectionReferencePushed = isCycleDetectionReferencePushed;
            }

            bool success = OnTryWrite(writer, value, options, ref state);
            if (success)
            {
                VerifyWrite(state.Current.OriginalDepth, writer);

                if (state.Current.IsCycleDetectionReferencePushed)
                {
                    state.ReferenceResolver.PopReferenceForCycleDetection();
                    state.Current.IsCycleDetectionReferencePushed = false;
                }

                // No need to clear state.Current.OriginalDepth since a stack pop will occur.
            }

            state.Pop(success);
#if DEBUG
            Debug.Assert(ReferenceEquals(originalJsonTypeInfo, state.Current.JsonTypeInfo));
#endif
            return success;
        }

        internal bool TryWriteDataExtensionProperty(Utf8JsonWriter writer, T value, JsonSerializerOptions options, ref WriteStack state)
        {
            Debug.Assert(value != null);

            if (!IsInternalConverter)
            {
                return TryWrite(writer, value, options, ref state);
            }

            if (!(this is JsonDictionaryConverter<T> dictionaryConverter))
            {
                // If not JsonDictionaryConverter<T> then we are JsonObject.
                // Avoid a type reference to JsonObject and its converter to support trimming.
                Debug.Assert(TypeToConvert == typeof(Nodes.JsonObject));
                return TryWrite(writer, value, options, ref state);
            }

            if (writer.CurrentDepth >= options.EffectiveMaxDepth)
            {
                ThrowHelper.ThrowJsonException_SerializerCycleDetected(options.EffectiveMaxDepth);
            }

            bool isContinuation = state.IsContinuation;
            bool success;

            state.Push();

            if (!isContinuation)
            {
                Debug.Assert(state.Current.OriginalDepth == 0);
                state.Current.OriginalDepth = writer.CurrentDepth;
            }

            // Ignore the naming policy for extension data.
            state.Current.IgnoreDictionaryKeyPolicy = true;
            state.Current.DeclaredJsonPropertyInfo = state.Current.JsonTypeInfo.ElementTypeInfo!.PropertyInfoForTypeInfo;

            success = dictionaryConverter.OnWriteResume(writer, value, options, ref state);
            if (success)
            {
                VerifyWrite(state.Current.OriginalDepth, writer);
            }

            state.Pop(success);

            return success;
        }

        internal sealed override Type TypeToConvert { get; } = typeof(T);

        internal void VerifyRead(JsonTokenType tokenType, int depth, long bytesConsumed, bool isValueConverter, ref Utf8JsonReader reader)
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
                    // A non-value converter (object or collection) should always have Start and End tokens.
                    // A value converter should not make any reads.
                    if (!isValueConverter || reader.BytesConsumed != bytesConsumed)
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

        internal virtual T ReadWithQuotes(ref Utf8JsonReader reader)
        {
            ThrowHelper.ThrowNotSupportedException_DictionaryKeyTypeNotSupported(TypeToConvert, this);
            return default;
        }

        internal virtual void WriteWithQuotes(Utf8JsonWriter writer, [DisallowNull] T value, JsonSerializerOptions options, ref WriteStack state)
            => ThrowHelper.ThrowNotSupportedException_DictionaryKeyTypeNotSupported(TypeToConvert, this);

        internal sealed override void WriteWithQuotesAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options, ref WriteStack state)
            => WriteWithQuotes(writer, (T)value, options, ref state);

        internal virtual T ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options)
            => throw new InvalidOperationException();

        internal virtual void WriteNumberWithCustomHandling(Utf8JsonWriter writer, T value, JsonNumberHandling handling)
            => throw new InvalidOperationException();
    }
}
