// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Converters;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Converts an object or value to or from JSON.
    /// </summary>
    public abstract partial class JsonConverter
    {
        internal JsonConverter()
        {
            IsInternalConverter = GetType().Assembly == typeof(JsonConverter).Assembly;
            ConverterStrategy = GetDefaultConverterStrategy();
        }

        /// <summary>
        /// Gets the type being converted by the current converter instance.
        /// </summary>
        /// <remarks>
        /// For instances of type <see cref="JsonConverter{T}"/> returns typeof(T),
        /// and for instances of type <see cref="JsonConverterFactory"/> returns <see langword="null" />.
        /// </remarks>
        public abstract Type? Type { get; }

        /// <summary>
        /// Determines whether the type can be converted.
        /// </summary>
        /// <param name="typeToConvert">The type is checked as to whether it can be converted.</param>
        /// <returns>True if the type can be converted, false otherwise.</returns>
        public abstract bool CanConvert(Type typeToConvert);

        internal ConverterStrategy ConverterStrategy
        {
            get => _converterStrategy;
            init
            {
                CanUseDirectReadOrWrite = value == ConverterStrategy.Value && IsInternalConverter;
                RequiresReadAhead = value == ConverterStrategy.Value;
                _converterStrategy = value;
            }
        }

        private ConverterStrategy _converterStrategy;

        /// <summary>
        /// Invoked by the base contructor to populate the initial value of the <see cref="ConverterStrategy"/> property.
        /// Used for declaring the default strategy for specific converter hierarchies without explicitly setting in a constructor.
        /// </summary>
        private protected abstract ConverterStrategy GetDefaultConverterStrategy();

        /// <summary>
        /// Indicates that the converter can consume the <see cref="JsonTypeInfo.CreateObject"/> delegate.
        /// Needed because certain collection converters cannot support arbitrary delegates.
        /// TODO remove once https://github.com/dotnet/runtime/pull/73395/ and
        /// https://github.com/dotnet/runtime/issues/71944 have been addressed.
        /// </summary>
        internal virtual bool SupportsCreateObjectDelegate => false;

        /// <summary>
        /// Indicates that the converter is compatible with <see cref="JsonObjectCreationHandling.Populate"/>.
        /// </summary>
        internal virtual bool CanPopulate => false;

        /// <summary>
        /// Can direct Read or Write methods be called (for performance).
        /// </summary>
        internal bool CanUseDirectReadOrWrite { get; set; }

        /// <summary>
        /// The converter supports writing and reading metadata.
        /// </summary>
        internal virtual bool CanHaveMetadata => false;

        /// <summary>
        /// The converter supports polymorphic writes; only reserved for System.Object types.
        /// </summary>
        internal bool CanBePolymorphic { get; set; }

        /// <summary>
        /// The serializer must read ahead all contents of the next JSON value
        /// before calling into the converter for deserialization.
        /// </summary>
        internal bool RequiresReadAhead { get; set; }

        /// <summary>
        /// Used to support JsonObject as an extension property in a loosely-typed, trimmable manner.
        /// </summary>
        internal virtual void ReadElementAndSetProperty(
            object obj,
            string propertyName,
            ref Utf8JsonReader reader,
            JsonSerializerOptions options,
            scoped ref ReadStack state)
        {
            Debug.Fail("Should not be reachable.");

            throw new InvalidOperationException();
        }

        internal virtual JsonTypeInfo CreateJsonTypeInfo(JsonSerializerOptions options)
        {
            Debug.Fail("Should not be reachable.");

            throw new InvalidOperationException();
        }

        internal JsonConverter<TTarget> CreateCastingConverter<TTarget>()
        {
            Debug.Assert(this is not JsonConverterFactory);

            if (this is JsonConverter<TTarget> conv)
            {
                return conv;
            }
            else
            {
                JsonSerializerOptions.CheckConverterNullabilityIsSameAsPropertyType(this, typeof(TTarget));

                // Avoid layering casting converters by consulting any source converters directly.
                return
                    SourceConverterForCastingConverter?.CreateCastingConverter<TTarget>()
                    ?? new CastingConverter<TTarget>(this);
            }
        }

        /// <summary>
        /// Tracks whether the JsonConverter&lt;T&gt;.HandleNull property has been overridden by a derived converter.
        /// </summary>
        internal bool UsesDefaultHandleNull { get; private protected set; }

        /// <summary>
        /// Does the converter want to be called when reading null tokens.
        /// When JsonConverter&lt;T&gt;.HandleNull isn't overridden this can still be true for non-nullable structs.
        /// </summary>
        internal bool HandleNullOnRead { get; private protected init; }

        /// <summary>
        /// Does the converter want to be called for null values.
        /// Should always match the precise value of the JsonConverter&lt;T&gt;.HandleNull virtual property.
        /// </summary>
        internal bool HandleNullOnWrite { get; private protected init; }

        /// <summary>
        /// Set if this converter is itself a casting converter.
        /// </summary>
        internal virtual JsonConverter? SourceConverterForCastingConverter => null;

        internal virtual Type? ElementType => null;

        internal virtual Type? KeyType => null;

        internal virtual JsonConverter? NullableElementConverter => null;

        /// <summary>
        /// Cached value of TypeToConvert.IsValueType, which is an expensive call.
        /// </summary>
        internal bool IsValueType { get; init; }

        /// <summary>
        /// Whether the converter is built-in.
        /// </summary>
        internal bool IsInternalConverter { get; init; }

        /// <summary>
        /// Whether the converter is built-in and handles a number type.
        /// </summary>
        internal bool IsInternalConverterForNumberType { get; init; }

        internal static bool ShouldFlush(ref WriteStack state, Utf8JsonWriter writer)
        {
            Debug.Assert(state.FlushThreshold == 0 || (state.PipeWriter is { CanGetUnflushedBytes: true }),
                "ShouldFlush should only be called by resumable serializers, all of which use the PipeWriter abstraction with CanGetUnflushedBytes == true.");
            // If surpassed flush threshold then return true which will flush stream.
            if (state.PipeWriter is { } pipeWriter)
            {
                return state.FlushThreshold > 0 && pipeWriter.UnflushedBytes > state.FlushThreshold - writer.BytesPending;
            }

            return false;
        }

        internal abstract object? ReadAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options);
        internal abstract bool OnTryReadAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out object? value);
        internal abstract bool TryReadAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out object? value);
        internal abstract object? ReadAsPropertyNameAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options);
        internal abstract object? ReadAsPropertyNameCoreAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options);
        internal abstract object? ReadNumberWithCustomHandlingAsObject(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options);

        internal abstract void WriteAsObject(Utf8JsonWriter writer, object? value, JsonSerializerOptions options);
        internal abstract bool OnTryWriteAsObject(Utf8JsonWriter writer, object? value, JsonSerializerOptions options, ref WriteStack state);
        internal abstract bool TryWriteAsObject(Utf8JsonWriter writer, object? value, JsonSerializerOptions options, ref WriteStack state);
        internal abstract void WriteAsPropertyNameAsObject(Utf8JsonWriter writer, object? value, JsonSerializerOptions options);
        internal abstract void WriteAsPropertyNameCoreAsObject(Utf8JsonWriter writer, object? value, JsonSerializerOptions options, bool isWritingExtensionDataProperty);
        internal abstract void WriteNumberWithCustomHandlingAsObject(Utf8JsonWriter writer, object? value, JsonNumberHandling handling);

        /// <summary>
        /// Gets a schema from the type being converted
        /// </summary>
        internal virtual JsonSchema? GetSchema(JsonNumberHandling numberHandling) => null;

        // Whether a type (ConverterStrategy.Object) is deserialized using a parameterized constructor.
        internal virtual bool ConstructorIsParameterized { get; }

        internal ConstructorInfo? ConstructorInfo { get; set; }

        /// <summary>
        /// Used for hooking custom configuration to a newly created associated JsonTypeInfo instance.
        /// </summary>
        internal virtual void ConfigureJsonTypeInfo(JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options) { }

        /// <summary>
        /// Additional reflection-specific configuration required by certain collection converters.
        /// </summary>
        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        internal virtual void ConfigureJsonTypeInfoUsingReflection(JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options) { }

        /// <summary>
        /// Whether the converter handles an immutable collection or dictionary type.
        /// </summary>
        internal virtual bool IsImmutableCollectionType => false;
    }
}
