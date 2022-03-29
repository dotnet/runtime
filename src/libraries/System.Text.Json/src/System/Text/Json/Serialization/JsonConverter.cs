// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Converts an object or value to or from JSON.
    /// </summary>
    public abstract partial class JsonConverter
    {
        internal JsonConverter() { }

        /// <summary>
        /// Determines whether the type can be converted.
        /// </summary>
        /// <param name="typeToConvert">The type is checked as to whether it can be converted.</param>
        /// <returns>True if the type can be converted, false otherwise.</returns>
        public abstract bool CanConvert(Type typeToConvert);

        internal abstract ConverterStrategy ConverterStrategy { get; }

        /// <summary>
        /// Can direct Read or Write methods be called (for performance).
        /// </summary>
        internal bool CanUseDirectReadOrWrite { get; set; }

        /// <summary>
        /// Can the converter have $id metadata.
        /// </summary>
        internal virtual bool CanHaveIdMetadata => false;

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
        internal virtual object CreateObject(JsonSerializerOptions options)
        {
            throw new InvalidOperationException(SR.NodeJsonObjectCustomConverterNotAllowedOnExtensionProperty);
        }

        /// <summary>
        /// Used to support JsonObject as an extension property in a loosely-typed, trimmable manner.
        /// </summary>
        internal virtual void ReadElementAndSetProperty(
            object obj,
            string propertyName,
            ref Utf8JsonReader reader,
            JsonSerializerOptions options,
            ref ReadStack state)
        {
            throw new InvalidOperationException(SR.NodeJsonObjectCustomConverterNotAllowedOnExtensionProperty);
        }

        internal abstract JsonPropertyInfo CreateJsonPropertyInfo();

        internal abstract JsonParameterInfo CreateJsonParameterInfo();

        internal abstract Type? ElementType { get; }

        internal abstract Type? KeyType { get; }

        /// <summary>
        /// Cached value of TypeToConvert.IsValueType, which is an expensive call.
        /// </summary>
        internal bool IsValueType { get; set; }

        /// <summary>
        /// Whether the converter is built-in.
        /// </summary>
        internal bool IsInternalConverter { get; set; }

        /// <summary>
        /// Whether the converter is built-in and handles a number type.
        /// </summary>
        internal bool IsInternalConverterForNumberType;

        /// <summary>
        /// Loosely-typed ReadCore() that forwards to strongly-typed ReadCore().
        /// </summary>
        internal abstract object? ReadCoreAsObject(ref Utf8JsonReader reader, JsonSerializerOptions options, ref ReadStack state);


        internal static bool ShouldFlush(Utf8JsonWriter writer, ref WriteStack state)
        {
            // If surpassed flush threshold then return false which will flush stream.
            return (state.FlushThreshold > 0 && writer.BytesPending > state.FlushThreshold);
        }

        // This is used internally to quickly determine the type being converted for JsonConverter<T>.
        internal abstract Type TypeToConvert { get; }

        internal abstract bool TryReadAsObject(ref Utf8JsonReader reader, JsonSerializerOptions options, ref ReadStack state, out object? value);

        internal abstract bool TryWriteAsObject(Utf8JsonWriter writer, object? value, JsonSerializerOptions options, ref WriteStack state);

        /// <summary>
        /// Loosely-typed WriteCore() that forwards to strongly-typed WriteCore().
        /// </summary>
        internal abstract bool WriteCoreAsObject(Utf8JsonWriter writer, object? value, JsonSerializerOptions options, ref WriteStack state);

        /// <summary>
        /// Loosely-typed WriteToPropertyName() that forwards to strongly-typed WriteToPropertyName().
        /// </summary>
        internal abstract void WriteAsPropertyNameCoreAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options, bool isWritingExtensionDataProperty);

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
        internal virtual void ConfigureJsonTypeInfoUsingReflection(JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options) { }

        /// <summary>
        /// Creates the instance and assigns it to state.Current.ReturnValue.
        /// </summary>
        internal virtual void CreateInstanceForReferenceResolver(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options) { }
    }
}
