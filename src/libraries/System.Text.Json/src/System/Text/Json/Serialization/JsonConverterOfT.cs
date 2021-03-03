// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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
            // Today only typeof(object) can have polymorphic writes.
            // In the future, this will be check for !IsSealed (and excluding value types).
            CanBePolymorphic = TypeToConvert == JsonClassInfo.ObjectType;
            IsValueType = TypeToConvert.IsValueType;
            CanBeNull = !IsValueType || TypeToConvert.IsNullableOfT();
            IsInternalConverter = GetType().Assembly == typeof(JsonConverter).Assembly;

            if (HandleNull)
            {
                HandleNullOnRead = true;
                HandleNullOnWrite = true;
            }

            // For the HandleNull == false case, either:
            // 1) The default values are assigned in this type's virtual HandleNull property
            // or
            // 2) A converter overroad HandleNull and returned false so HandleNullOnRead and HandleNullOnWrite
            // will be their default values of false.

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

        internal sealed override JsonPropertyInfo CreateJsonPropertyInfo()
        {
            return new JsonPropertyInfo<T>();
        }

        internal override sealed JsonParameterInfo CreateJsonParameterInfo()
        {
            return new JsonParameterInfo<T>();
        }

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
                HandleNullOnRead = !CanBeNull;

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

        /// <summary>
        /// Can <see langword="null"/> be assigned to <see cref="TypeToConvert"/>?
        /// </summary>
        internal bool CanBeNull { get; }

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
            if (ClassType == ClassType.Value)
            {
                // A value converter should never be within a continuation.
                Debug.Assert(!state.IsContinuation);

                // For perf and converter simplicity, handle null here instead of forwarding to the converter.
                if (reader.TokenType == JsonTokenType.Null && !HandleNullOnRead)
                {
                    if (!CanBeNull)
                    {
                        ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                    }

                    value = default;
                    return true;
                }

#if !DEBUG
                // For performance, only perform validation on internal converters on debug builds.
                if (IsInternalConverter)
                {
                    if (state.Current.NumberHandling != null)
                    {
                        value = ReadNumberWithCustomHandling(ref reader, state.Current.NumberHandling.Value);
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
                        value = ReadNumberWithCustomHandling(ref reader, state.Current.NumberHandling.Value);
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
                    CanBePolymorphic && value is JsonElement element)
                {
                    // Edge case where we want to lookup for a reference when parsing into typeof(object)
                    // instead of return `value` as a JsonElement.
                    Debug.Assert(TypeToConvert == typeof(object));

                    if (JsonSerializer.TryGetReferenceFromJsonElement(ref state, element, out object? referenceValue))
                    {
                        value = (T?)referenceValue;
                    }
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
                if (reader.TokenType == JsonTokenType.Null && !HandleNullOnRead && !wasContinuation)
                {
                    if (!CanBeNull)
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
                        if (!CanBeNull)
                        {
                            ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypeToConvert);
                        }

                        value = default;
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
                        bytesConsumed: 0,
                        isValueConverter: false,
                        ref reader);

                    // No need to clear state.Current.* since a stack pop will occur.
                }
            }

            state.Pop(success);
            return success;
        }

        internal override sealed bool TryReadAsObject(ref Utf8JsonReader reader, JsonSerializerOptions options, ref ReadStack state, out object? value)
        {
            bool success = TryRead(ref reader, TypeToConvert, options, ref state, out T? typedValue);
            value = typedValue;
            return success;
        }

        internal virtual bool IsNull(in T value) => value == null;

        internal bool TryWrite(Utf8JsonWriter writer, in T value, JsonSerializerOptions options, ref WriteStack state)
        {
            if (writer.CurrentDepth >= options.EffectiveMaxDepth)
            {
                ThrowHelper.ThrowJsonException_SerializerCycleDetected(options.EffectiveMaxDepth);
            }

            if (CanBeNull && !HandleNullOnWrite && IsNull(value))
            {
                // We do not pass null values to converters unless HandleNullOnWrite is true. Null values for properties were
                // already handled in GetMemberAndWriteJson() so we don't need to check for IgnoreNullValues here.
                writer.WriteNullValue();
                return true;
            }

            bool ignoreCyclesPopReference = false;
            if (options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.IgnoreCycles &&
                !IsValueType && !IsNull(value))
            {
                Debug.Assert(value != null);
                ReferenceResolver resolver = state.ReferenceResolver;

                // Write null to break reference cycles.
                if (resolver.ContainsReferenceForCycleDetection(value))
                {
                    writer.WriteNullValue();
                    return true;
                }

                // For boxed reference types: do not push when boxed in order to avoid false positives
                //   when we run the ContainsReferenceForCycleDetection check for the converter of the unboxed value.
                if (!CanBePolymorphic)
                {
                    resolver.PushReferenceForCycleDetection(value);
                    ignoreCyclesPopReference = true;
                }
            }

            if (CanBePolymorphic)
            {
                if (value == null)
                {
                    Debug.Assert(ClassType == ClassType.Value);
                    Debug.Assert(!state.IsContinuation);
                    Debug.Assert(HandleNullOnWrite);

                    int originalPropertyDepth = writer.CurrentDepth;
                    Write(writer, value, options);
                    VerifyWrite(originalPropertyDepth, writer);

                    return true;
                }

                Type type = value.GetType();
                if (type == JsonClassInfo.ObjectType)
                {
                    writer.WriteStartObject();
                    writer.WriteEndObject();
                    return true;
                }

                if (type != TypeToConvert && IsInternalConverter)
                {
                    // For internal converter only: Handle polymorphic case and get the new converter.
                    // Custom converter, even though polymorphic converter, get called for reading AND writing.
                    JsonConverter jsonConverter = state.Current.InitializeReEntry(type, options);
                    if (jsonConverter != this)
                    {
                        if (options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.IgnoreCycles &&
                            jsonConverter.IsValueType)
                        {
                            // For boxed value types: push the value before it gets unboxed on TryWriteAsObject.
                            state.ReferenceResolver.PushReferenceForCycleDetection(value);
                            ignoreCyclesPopReference = true;
                        }

                        // We found a different converter; forward to that.
                        bool success2 = jsonConverter.TryWriteAsObject(writer, value, options, ref state);

                        if (ignoreCyclesPopReference)
                        {
                            state.ReferenceResolver.PopReferenceForCycleDetection();
                        }

                        return success2;
                    }
                }
            }

            if (ClassType == ClassType.Value)
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

            if (ignoreCyclesPopReference)
            {
                state.ReferenceResolver.PopReferenceForCycleDetection();
            }

            return success;
        }

        internal bool TryWriteDataExtensionProperty(Utf8JsonWriter writer, T value, JsonSerializerOptions options, ref WriteStack state)
        {
            Debug.Assert(value != null);

            if (!IsInternalConverter)
            {
                return TryWrite(writer, value, options, ref state);
            }

            Debug.Assert(this is JsonDictionaryConverter<T>);

            if (writer.CurrentDepth >= options.EffectiveMaxDepth)
            {
                ThrowHelper.ThrowJsonException_SerializerCycleDetected(options.EffectiveMaxDepth);
            }

            JsonDictionaryConverter<T> dictionaryConverter = (JsonDictionaryConverter<T>)this;

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

            success = dictionaryConverter.OnWriteResume(writer, value, options, ref state);
            if (success)
            {
                VerifyWrite(state.Current.OriginalDepth, writer);
            }

            state.Pop(success);

            return success;
        }

        internal sealed override Type TypeToConvert => typeof(T);

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
            => throw new InvalidOperationException();

        internal virtual void WriteWithQuotes(Utf8JsonWriter writer, [DisallowNull] T value, JsonSerializerOptions options, ref WriteStack state)
            => throw new InvalidOperationException();

        internal sealed override void WriteWithQuotesAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options, ref WriteStack state)
            => WriteWithQuotes(writer, (T)value, options, ref state);

        internal virtual T ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling handling)
            => throw new InvalidOperationException();

        internal virtual void WriteNumberWithCustomHandling(Utf8JsonWriter writer, T value, JsonNumberHandling handling)
            => throw new InvalidOperationException();
    }
}
