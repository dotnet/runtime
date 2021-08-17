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
            IsInternalConverter = GetType().Assembly == typeof(JsonConverter).Assembly;
            // Today only the internal JsonConverter<object> can have polymorphic writes.
            CanBePolymorphic = IsInternalConverter && TypeToConvert == JsonTypeInfo.ObjectType;
            IsValueType = TypeToConvert.IsValueType;
            CanBeNull = default(T) is null;

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

            CanUseDirectReadOrWrite = !CanBePolymorphic && IsInternalConverter && ConverterStrategy == ConverterStrategy.Value;
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
        /// The default value is <see langword="true"/> for converters based on value types, and <see langword="false"/> for converters based on reference types.
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
        /// <remarks>Note that the value of <seealso cref="HandleNull"/> determines if the converter handles null JSON tokens.</remarks>
        public abstract T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options);

#if NET6_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
        internal bool TryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out T? value)
        {
            if (ConverterStrategy == ConverterStrategy.Value)
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

#if DEBUG
            // DEBUG: ensure push/pop operations preserve stack integrity
            JsonTypeInfo originalJsonTypeInfo = state.Current.JsonTypeInfo;
#endif
            state.Push();
            Debug.Assert(TypeToConvert.IsAssignableFrom(state.Current.JsonTypeInfo.Type));

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
#if DEBUG
                        Debug.Assert(ReferenceEquals(originalJsonTypeInfo, state.Current.JsonTypeInfo));
#endif
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

        /// <summary>
        /// Performance optimization.
        /// The 'in' modifier in 'TryWrite(in T Value)' causes boxing for Nullable{T}, so this helper avoids that.
        /// TODO: Remove this work-around once #50915 is addressed.
        /// </summary>
        private static bool IsNull(T value) => value is null;

#if NET6_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
        internal bool TryWrite(Utf8JsonWriter writer, in T value, JsonSerializerOptions options, ref WriteStack state)
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

            bool ignoreCyclesPopReference = false;

            if (
#if NET5_0_OR_GREATER
                // Short-circuit the check against "is not null"; treated as a constant by recent versions of the JIT.
                !typeof(T).IsValueType &&
#else
                !IsValueType &&
#endif
                // Since we may have checked for a null value above we may have a redundant check here,
                // but this seems to be better than trying to cache that value when considering all permutations:
                // int?, int?(null value), int, object, object(null value)
                value is not null)
            {

                if (options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.IgnoreCycles &&
                    // .NET types that are serialized as JSON primitive values don't need to be tracked for cycle detection e.g: string.
                    ConverterStrategy != ConverterStrategy.Value)
                {
                    // Custom (user) converters shall not track references
                    //  it is responsibility of the user to break cycles in case there's any
                    //  if we compare against Preserve, objects don't get preserved when a custom converter exists
                    //  given that the custom converter executes prior to the preserve logic.
                    Debug.Assert(IsInternalConverter);

                    ReferenceResolver resolver = state.ReferenceResolver;

                    // Write null to break reference cycles.
                    if (resolver.ContainsReferenceForCycleDetection(value))
                    {
                        writer.WriteNullValue();
                        return true;
                    }

                    // For boxed reference types: do not push when boxed in order to avoid false positives
                    //   when we run the ContainsReferenceForCycleDetection check for the converter of the unboxed value.
                    Debug.Assert(!CanBePolymorphic);
                    resolver.PushReferenceForCycleDetection(value);
                    ignoreCyclesPopReference = true;
                }

                if (CanBePolymorphic)
                {
                    Debug.Assert(IsInternalConverter);

                    Type type = value.GetType();

                    if (type != TypeToConvert)
                    {
                        // For internal converter only: Handle polymorphic case and get the new converter.
                        // Custom converter, even though polymorphic converter, get called for reading AND writing.
                        JsonConverter jsonConverter = state.Current.InitializeReEntry(type, options);
                        Debug.Assert(jsonConverter != this);

                        // For boxed value types: invoke the reference handler
                        // before the instance gets unboxed by the subtype converter.
                        if (jsonConverter.IsValueType)
                        {
                            switch (options.ReferenceHandlingStrategy)
                            {
                                case ReferenceHandlingStrategy.Preserve when (jsonConverter.CanHaveIdMetadata && !state.IsContinuation):
                                    if (JsonSerializer.TryWriteReferenceForBoxedStruct(value, ref state, writer))
                                    {
                                        return true;
                                    }
                                    break;

                                case ReferenceHandlingStrategy.IgnoreCycles:
                                    state.ReferenceResolver.PushReferenceForCycleDetection(value);
                                    ignoreCyclesPopReference = true;
                                    break;
                                default:
                                    break;
                            }
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

                if (
#if NET5_0_OR_GREATER
                    // Short-circuit the check against ignoreCyclesPopReference; treated as a constant by recent versions of the JIT.
                    !typeof(T).IsValueType &&
#endif
                    ignoreCyclesPopReference)
                {
                    // Should only be entered if we're serializing instances
                    // of type object using the internal object converter.
                    Debug.Assert(value?.GetType() == typeof(object) && IsInternalConverter);
                    state.ReferenceResolver.PopReferenceForCycleDetection();
                }

                return true;
            }

            bool isContinuation = state.IsContinuation;

#if DEBUG
            // DEBUG: ensure push/pop operations preserve stack integrity
            JsonTypeInfo originalJsonTypeInfo = state.Current.JsonTypeInfo;
#endif
            state.Push();
            Debug.Assert(TypeToConvert.IsAssignableFrom(state.Current.JsonTypeInfo.Type));

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
#if DEBUG
            Debug.Assert(ReferenceEquals(originalJsonTypeInfo, state.Current.JsonTypeInfo));
#endif

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

            if (this is not JsonDictionaryConverter<T> dictionaryConverter)
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

            // Extension data properties change how dictionary key naming policies are applied.
            state.Current.IsWritingExtensionDataProperty = true;
            state.Current.DeclaredJsonPropertyInfo = state.Current.JsonTypeInfo.ElementTypeInfo!.PropertyInfoForTypeInfo;

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
                    // A non-value converter (object or collection) should always have Start and End tokens
                    if (!isValueConverter)
                    {
                        // with the exception of converters that support null value reads
                        if (!HandleNullOnRead || tokenType != JsonTokenType.Null)
                        {
                            ThrowHelper.ThrowJsonException_SerializationConverterRead(this);
                        }
                    }
                    // A value converter should not make any reads.
                    else if (reader.BytesConsumed != bytesConsumed)
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
        /// <param name="value">The value to convert. Note that the value of <seealso cref="HandleNull"/> determines if the converter handles <see langword="null" /> values.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> being used.</param>
        public abstract void Write(
            Utf8JsonWriter writer,
#nullable disable // T may or may not be nullable depending on the derived type's overload.
            T value,
#nullable restore
            JsonSerializerOptions options);

        /// <summary>
        /// Reads a dictionary key from a JSON property name.
        /// </summary>
        /// <param name="reader">The <see cref="Utf8JsonReader"/> to read from.</param>
        /// <param name="typeToConvert">The <see cref="Type"/> being converted.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> being used.</param>
        /// <returns>The value that was converted.</returns>
        /// <remarks>Method should be overridden in custom converters of types used in deserialized dictionary keys.</remarks>
        public virtual T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (!IsInternalConverter && options.TryGetDefaultSimpleConverter(TypeToConvert, out JsonConverter? defaultConverter))
            {
                // .NET 5 backward compatibility: hardcode the default converter for primitive key serialization.
                Debug.Assert(defaultConverter.IsInternalConverter && defaultConverter is JsonConverter<T>);
                return ((JsonConverter<T>)defaultConverter).ReadAsPropertyNameCore(ref reader, TypeToConvert, options);
            }

            ThrowHelper.ThrowNotSupportedException_DictionaryKeyTypeNotSupported(TypeToConvert, this);
            return default;
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
        public virtual void WriteAsPropertyName(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (!IsInternalConverter && options.TryGetDefaultSimpleConverter(TypeToConvert, out JsonConverter? defaultConverter))
            {
                // .NET 5 backward compatibility: hardcode the default converter for primitive key serialization.
                Debug.Assert(defaultConverter.IsInternalConverter && defaultConverter is JsonConverter<T>);
                ((JsonConverter<T>)defaultConverter).WriteAsPropertyNameCore(writer, value, options, isWritingExtensionDataProperty: false);
                return;
            }

            ThrowHelper.ThrowNotSupportedException_DictionaryKeyTypeNotSupported(TypeToConvert, this);
        }

        internal virtual void WriteAsPropertyNameCore(Utf8JsonWriter writer, T value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            if (isWritingExtensionDataProperty)
            {
                // Extension data is meant as mechanism to gather unused JSON properties;
                // do not apply any custom key conversions and hardcode the default behavior.
                Debug.Assert(!IsInternalConverter && TypeToConvert == typeof(string));
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

        internal sealed override void WriteAsPropertyNameCoreAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
            => WriteAsPropertyNameCore(writer, (T)value, options, isWritingExtensionDataProperty);

        internal virtual T ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options)
            => throw new InvalidOperationException();

        internal virtual void WriteNumberWithCustomHandling(Utf8JsonWriter writer, T value, JsonNumberHandling handling)
            => throw new InvalidOperationException();
    }
}
