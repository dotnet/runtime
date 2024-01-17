// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Converters;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Converts an object or value to or from JSON.
    /// </summary>
    /// <typeparam name="T">The <see cref="System.Type"/> to convert.</typeparam>
    public abstract partial class JsonConverter<T> : JsonConverter
    {
        /// <summary>
        /// When overridden, constructs a new <see cref="JsonConverter{T}"/> instance.
        /// </summary>
        protected internal JsonConverter()
        {
            IsValueType = typeof(T).IsValueType;

            if (HandleNull)
            {
                HandleNullOnRead = true;
                HandleNullOnWrite = true;
            }
            else if (UsesDefaultHandleNull)
            {
                // If the type doesn't support null, allow the converter a chance to modify.
                // These semantics are backwards compatible with 3.0.
                HandleNullOnRead = default(T) is not null;

                // The framework handles null automatically on writes.
                HandleNullOnWrite = false;
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

        private protected override ConverterStrategy GetDefaultConverterStrategy() => ConverterStrategy.Value;

        internal sealed override JsonTypeInfo CreateJsonTypeInfo(JsonSerializerOptions options)
        {
            return new JsonTypeInfo<T>(this, options);
        }

        internal override Type? KeyType => null;

        internal override Type? ElementType => null;

        /// <summary>
        /// Indicates whether <see langword="null"/> should be passed to the converter on serialization,
        /// and whether <see cref="JsonTokenType.Null"/> should be passed on deserialization.
        /// </summary>
        /// <remarks>
        /// The default value is <see langword="true"/> for converters based on value types, and <see langword="false"/> for converters based on reference types.
        /// </remarks>
        public virtual bool HandleNull
        {
            get
            {
                UsesDefaultHandleNull = true;
                return false;
            }
        }

        // This non-generic API is sealed as it just forwards to the generic version.
        internal sealed override void WriteAsObject(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            T valueOfT = JsonSerializer.UnboxOnWrite<T>(value)!;
            Write(writer, valueOfT, options);
        }

        // This non-generic API is sealed as it just forwards to the generic version.
        internal sealed override bool OnTryWriteAsObject(Utf8JsonWriter writer, object? value, JsonSerializerOptions options, ref WriteStack state)
        {
            T valueOfT = JsonSerializer.UnboxOnWrite<T>(value)!;
            return OnTryWrite(writer, valueOfT, options, ref state);
        }

        // This non-generic API is sealed as it just forwards to the generic version.
        internal sealed override void WriteAsPropertyNameAsObject(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            T valueOfT = JsonSerializer.UnboxOnWrite<T>(value)!;
            WriteAsPropertyName(writer, valueOfT, options);
        }

        internal sealed override void WriteAsPropertyNameCoreAsObject(Utf8JsonWriter writer, object? value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            T valueOfT = JsonSerializer.UnboxOnWrite<T>(value)!;
            WriteAsPropertyNameCore(writer, valueOfT, options, isWritingExtensionDataProperty);
        }

        internal sealed override void WriteNumberWithCustomHandlingAsObject(Utf8JsonWriter writer, object? value, JsonNumberHandling handling)
        {
            T valueOfT = JsonSerializer.UnboxOnWrite<T>(value)!;
            WriteNumberWithCustomHandling(writer, valueOfT, handling);
        }

        // This non-generic API is sealed as it just forwards to the generic version.
        internal sealed override bool TryWriteAsObject(Utf8JsonWriter writer, object? value, JsonSerializerOptions options, ref WriteStack state)
        {
            T valueOfT = JsonSerializer.UnboxOnWrite<T>(value)!;
            return TryWrite(writer, valueOfT, options, ref state);
        }

        // Provide a default implementation for value converters.
        internal virtual bool OnTryWrite(Utf8JsonWriter writer,
#nullable disable // T may or may not be nullable depending on the derived converter's HandleNull override.
            T value,
#nullable enable
            JsonSerializerOptions options,
            ref WriteStack state)
        {
            Write(writer, value, options);
            return true;
        }

        // Provide a default implementation for value converters.
        internal virtual bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out T? value)
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
        /// <param name="typeToConvert">The <see cref="System.Type"/> being converted.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> being used.</param>
        /// <returns>The value that was converted.</returns>
        /// <remarks>Note that the value of <seealso cref="HandleNull"/> determines if the converter handles null JSON tokens.</remarks>
        public abstract T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options);

        internal bool TryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out T? value, out bool isPopulatedValue)
        {
            // For perf and converter simplicity, handle null here instead of forwarding to the converter.
            if (reader.TokenType == JsonTokenType.Null && !HandleNullOnRead && !state.IsContinuation)
            {
                if (default(T) is not null)
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(Type);
                }

                value = default;
                isPopulatedValue = false;
                return true;
            }

            if (ConverterStrategy == ConverterStrategy.Value)
            {
                // A value converter should never be within a continuation.
                Debug.Assert(!state.IsContinuation);
#if !DEBUG
                // For performance, only perform validation on internal converters on debug builds.
                if (IsInternalConverter)
                {
                    if (state.Current.NumberHandling != null && IsInternalConverterForNumberType)
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

                    if (state.Current.NumberHandling != null && IsInternalConverterForNumberType)
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

                isPopulatedValue = false;
                return true;
            }

            Debug.Assert(IsInternalConverter);
            bool isContinuation = state.IsContinuation;
            bool success;

            if (
#if NETCOREAPP
                !typeof(T).IsValueType &&
#endif
                CanBePolymorphic)
            {
                // Special case object converters since they don't
                // require the expensive ReadStack.Push()/Pop() operations.
                Debug.Assert(this is ObjectConverter);
                success = OnTryRead(ref reader, typeToConvert, options, ref state, out value);
                Debug.Assert(success);
                isPopulatedValue = false;
                return true;
            }

            JsonPropertyInfo? propertyInfo = state.Current.JsonPropertyInfo;
            object? parentObj = state.Current.ReturnValue;

#if DEBUG
            // DEBUG: ensure push/pop operations preserve stack integrity
            JsonTypeInfo originalJsonTypeInfo = state.Current.JsonTypeInfo;
#endif
            state.Push();
            Debug.Assert(Type == state.Current.JsonTypeInfo.Type);

            if (!isContinuation)
            {
#if DEBUG
                // For performance, only perform token type validation of converters on debug builds.
                Debug.Assert(state.Current.OriginalTokenType == JsonTokenType.None);
                state.Current.OriginalTokenType = reader.TokenType;
#endif
                Debug.Assert(state.Current.OriginalDepth == 0);
                state.Current.OriginalDepth = reader.CurrentDepth;
            }

            if (parentObj != null && propertyInfo != null && !propertyInfo.IsForTypeInfo)
            {
                state.Current.HasParentObject = true;
            }

            success = OnTryRead(ref reader, typeToConvert, options, ref state, out value);
#if DEBUG
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
#endif

            isPopulatedValue = state.Current.IsPopulating;
            state.Pop(success);
#if DEBUG
            Debug.Assert(ReferenceEquals(originalJsonTypeInfo, state.Current.JsonTypeInfo));
#endif
            return success;
        }

        internal sealed override bool OnTryReadAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out object? value)
        {
            bool success = OnTryRead(ref reader, typeToConvert, options, ref state, out T? typedValue);
            value = typedValue;
            return success;
        }

        internal sealed override bool TryReadAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out object? value)
        {
            bool success = TryRead(ref reader, typeToConvert, options, ref state, out T? typedValue, out _);
            value = typedValue;
            return success;
        }

        internal sealed override object? ReadAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            T? typedValue = Read(ref reader, typeToConvert, options);
            return typedValue;
        }

        internal sealed override object? ReadAsPropertyNameAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            T typedValue = ReadAsPropertyName(ref reader, typeToConvert, options);
            return typedValue;
        }

        internal sealed override object? ReadAsPropertyNameCoreAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            T typedValue = ReadAsPropertyNameCore(ref reader, typeToConvert, options);
            return typedValue;
        }

        internal sealed override object? ReadNumberWithCustomHandlingAsObject(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options)
        {
            T typedValue = ReadNumberWithCustomHandling(ref reader, handling, options);
            return typedValue;
        }

        /// <summary>
        /// Performance optimization.
        /// The 'in' modifier in 'TryWrite(in T Value)' causes boxing for Nullable{T}, so this helper avoids that.
        /// TODO: Remove this work-around once https://github.com/dotnet/runtime/issues/50915 is addressed.
        /// </summary>
        private static bool IsNull(T? value) => value is null;

        internal bool TryWrite(Utf8JsonWriter writer, in T? value, JsonSerializerOptions options, ref WriteStack state)
        {
            if (writer.CurrentDepth >= options.EffectiveMaxDepth)
            {
                ThrowHelper.ThrowJsonException_SerializerCycleDetected(options.EffectiveMaxDepth);
            }

            if (default(T) is null && !HandleNullOnWrite && IsNull(value))
            {
                // We do not pass null values to converters unless HandleNullOnWrite is true. Null values for properties were
                // already handled in GetMemberAndWriteJson() so we don't need to check for IgnoreNullValues here.
                writer.WriteNullValue();
                return true;
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

            Debug.Assert(IsInternalConverter);
            bool isContinuation = state.IsContinuation;
            bool success;

            if (
#if NETCOREAPP
                // Short-circuit the check against "is not null"; treated as a constant by recent versions of the JIT.
                !typeof(T).IsValueType &&
#else
                !IsValueType &&
#endif
                value is not null &&
                // Do not handle objects that have already been
                // handled by a polymorphic converter for a base type.
                state.Current.PolymorphicSerializationState != PolymorphicSerializationState.PolymorphicReEntryStarted)
            {
                JsonTypeInfo jsonTypeInfo = state.PeekNestedJsonTypeInfo();
                Debug.Assert(jsonTypeInfo.Converter.Type == Type);

                bool canBePolymorphic = CanBePolymorphic || jsonTypeInfo.PolymorphicTypeResolver is not null;
                JsonConverter? polymorphicConverter = canBePolymorphic ?
                    ResolvePolymorphicConverter(value, jsonTypeInfo, options, ref state) :
                    null;

                if (!isContinuation && options.ReferenceHandlingStrategy != ReferenceHandlingStrategy.None &&
                    TryHandleSerializedObjectReference(writer, value, options, polymorphicConverter, ref state))
                {
                    // The reference handler wrote reference metadata, serialization complete.
                    return true;
                }

                if (polymorphicConverter is not null)
                {
                    success = polymorphicConverter.TryWriteAsObject(writer, value, options, ref state);
                    state.Current.ExitPolymorphicConverter(success);

                    if (success)
                    {
                        if (state.Current.IsPushedReferenceForCycleDetection)
                        {
                            state.ReferenceResolver.PopReferenceForCycleDetection();
                            state.Current.IsPushedReferenceForCycleDetection = false;
                        }
                    }

                    return success;
                }
            }

#if DEBUG
            // DEBUG: ensure push/pop operations preserve stack integrity
            JsonTypeInfo originalJsonTypeInfo = state.Current.JsonTypeInfo;
#endif
            state.Push();
            Debug.Assert(Type == state.Current.JsonTypeInfo.Type);

#if DEBUG
            // For performance, only perform validation on internal converters on debug builds.
            if (!isContinuation)
            {
                Debug.Assert(state.Current.OriginalDepth == 0);
                state.Current.OriginalDepth = writer.CurrentDepth;
            }
#endif
            success = OnTryWrite(writer, value, options, ref state);
#if DEBUG
            if (success)
            {
                VerifyWrite(state.Current.OriginalDepth, writer);
            }
#endif
            state.Pop(success);

            if (success && state.Current.IsPushedReferenceForCycleDetection)
            {
                state.ReferenceResolver.PopReferenceForCycleDetection();
                state.Current.IsPushedReferenceForCycleDetection = false;
            }
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

            JsonDictionaryConverter<T>? dictionaryConverter = this as JsonDictionaryConverter<T>
                ?? (this as JsonMetadataServicesConverter<T>)?.Converter as JsonDictionaryConverter<T>;

            if (dictionaryConverter == null)
            {
                // If not JsonDictionaryConverter<T> then we are JsonObject.
                // Avoid a type reference to JsonObject and its converter to support trimming.
                Debug.Assert(Type == typeof(Nodes.JsonObject));
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

            // Extension data properties change how dictionary key naming policies are applied.
            state.Current.IsWritingExtensionDataProperty = true;
            state.Current.JsonPropertyInfo = state.Current.JsonTypeInfo.ElementTypeInfo!.PropertyInfoForTypeInfo;

            success = dictionaryConverter.OnWriteResume(writer, value, options, ref state);
            if (success)
            {
                VerifyWrite(state.Current.OriginalDepth, writer);
            }

            state.Pop(success);

            return success;
        }

        /// <inheritdoc/>
        public sealed override Type Type { get; } = typeof(T);

        internal void VerifyRead(JsonTokenType tokenType, int depth, long bytesConsumed, bool isValueConverter, ref Utf8JsonReader reader)
        {
            Debug.Assert(isValueConverter == (ConverterStrategy == ConverterStrategy.Value));

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
                    if (isValueConverter)
                    {
                        // A value converter should not make any reads.
                        if (reader.BytesConsumed != bytesConsumed)
                        {
                            ThrowHelper.ThrowJsonException_SerializationConverterRead(this);
                        }
                    }
                    else
                    {
                        // A non-value converter (object or collection) should always have Start and End tokens
                        // unless it is polymorphic or supports null value reads.
                        if (!CanBePolymorphic && !(HandleNullOnRead && tokenType == JsonTokenType.Null))
                        {
                            ThrowHelper.ThrowJsonException_SerializationConverterRead(this);
                        }
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
        /// <param name="value">The value to convert. Note that the value of <seealso cref="HandleNull"/> determines if the converter handles <see langword="null" /> values.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> being used.</param>
        public abstract void Write(
            Utf8JsonWriter writer,
#nullable disable // T may or may not be nullable depending on the derived converter's HandleNull override.
            T value,
#nullable restore
            JsonSerializerOptions options);

        /// <summary>
        /// Reads a dictionary key from a JSON property name.
        /// </summary>
        /// <param name="reader">The <see cref="Utf8JsonReader"/> to read from.</param>
        /// <param name="typeToConvert">The <see cref="System.Type"/> being converted.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> being used.</param>
        /// <returns>The value that was converted.</returns>
        /// <remarks>Method should be overridden in custom converters of types used in deserialized dictionary keys.</remarks>
        public virtual T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // .NET 5 backward compatibility: hardcode the default converter for primitive key serialization.
            JsonConverter<T>? fallbackConverter = GetFallbackConverterForPropertyNameSerialization(options);
            if (fallbackConverter is null)
            {
                ThrowHelper.ThrowNotSupportedException_DictionaryKeyTypeNotSupported(Type, this);
            }

            return fallbackConverter.ReadAsPropertyNameCore(ref reader, typeToConvert, options);
        }

        internal virtual T ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);

            long originalBytesConsumed = reader.BytesConsumed;
            T result = ReadAsPropertyName(ref reader, typeToConvert, options);
            if (reader.BytesConsumed != originalBytesConsumed)
            {
                ThrowHelper.ThrowJsonException_SerializationConverterRead(this);
            }

            return result;
        }

        /// <summary>
        /// Writes a dictionary key as a JSON property name.
        /// </summary>
        /// <param name="writer">The <see cref="Utf8JsonWriter"/> to write to.</param>
        /// <param name="value">The value to convert. Note that the value of <seealso cref="HandleNull"/> determines if the converter handles <see langword="null" /> values.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> being used.</param>
        /// <remarks>Method should be overridden in custom converters of types used in serialized dictionary keys.</remarks>
        public virtual void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] T value, JsonSerializerOptions options)
        {
            // .NET 5 backward compatibility: hardcode the default converter for primitive key serialization.
            JsonConverter<T>? fallbackConverter = GetFallbackConverterForPropertyNameSerialization(options);
            if (fallbackConverter is null)
            {
                ThrowHelper.ThrowNotSupportedException_DictionaryKeyTypeNotSupported(Type, this);
            }

            fallbackConverter.WriteAsPropertyNameCore(writer, value, options, isWritingExtensionDataProperty: false);
        }

        internal virtual void WriteAsPropertyNameCore(Utf8JsonWriter writer, [DisallowNull] T value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(value));
            }

            if (isWritingExtensionDataProperty)
            {
                // Extension data is meant as mechanism to gather unused JSON properties;
                // do not apply any custom key conversions and hardcode the default behavior.
                Debug.Assert(!IsInternalConverter && Type == typeof(string));
                writer.WritePropertyName((string)(object)value!);
                return;
            }

            int originalDepth = writer.CurrentDepth;
            WriteAsPropertyName(writer, value, options);
            if (originalDepth != writer.CurrentDepth || writer.TokenType != JsonTokenType.PropertyName)
            {
                ThrowHelper.ThrowJsonException_SerializationConverterWrite(this);
            }
        }

        // .NET 5 backward compatibility: hardcode the default converter for primitive key serialization.
        private JsonConverter<T>? GetFallbackConverterForPropertyNameSerialization(JsonSerializerOptions options)
        {
            JsonConverter<T>? result = null;

            // For consistency do not return any default converters for options instances linked to a
            // JsonSerializerContext, even if the default converters might have been rooted.
            if (!IsInternalConverter && options.TypeInfoResolver is not JsonSerializerContext)
            {
                result = _fallbackConverterForPropertyNameSerialization;

                if (result is null && DefaultJsonTypeInfoResolver.TryGetDefaultSimpleConverter(Type, out JsonConverter? defaultConverter))
                {
                    Debug.Assert(defaultConverter != this);
                    _fallbackConverterForPropertyNameSerialization = result = (JsonConverter<T>)defaultConverter;
                }
            }

            return result;
        }

        private JsonConverter<T>? _fallbackConverterForPropertyNameSerialization;

        internal virtual T ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options)
            => throw new InvalidOperationException();

        internal virtual void WriteNumberWithCustomHandling(Utf8JsonWriter writer, T? value, JsonNumberHandling handling)
            => throw new InvalidOperationException();
    }
}
