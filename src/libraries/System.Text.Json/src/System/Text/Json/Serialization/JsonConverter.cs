// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

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

        internal abstract ClassType ClassType { get; }

        /// <summary>
        /// Can direct Read or Write methods be called (for performance).
        /// </summary>
        internal bool CanUseDirectReadOrWrite { get; set; }

        /// <summary>
        /// Can the converter have $id metadata.
        /// </summary>
        internal virtual bool CanHaveIdMetadata => true;

        internal bool CanBePolymorphic { get; set; }

        internal abstract JsonPropertyInfo CreateJsonPropertyInfo();

        internal abstract JsonParameterInfo CreateJsonParameterInfo();

        internal abstract Type? ElementType { get; }

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

        // For polymorphic cases, the concrete type to create.
        internal virtual Type RuntimeType => TypeToConvert;

        internal bool ShouldFlush(Utf8JsonWriter writer, ref WriteStack state)
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
        /// Loosely-typed WriteWithQuotes() that forwards to strongly-typed WriteWithQuotes().
        /// </summary>
        internal abstract void WriteWithQuotesAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options, ref WriteStack state);

        // Whether a type (ClassType.Object) is deserialized using a parameterized constructor.
        internal virtual bool ConstructorIsParameterized { get; }

        internal ConstructorInfo? ConstructorInfo { get; set; }

        internal virtual void Initialize(JsonSerializerOptions options) { }

        /// <summary>
        /// Creates the instance and assigns it to state.Current.ReturnValue.
        /// </summary>
        internal virtual void CreateInstanceForReferenceResolver(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options) { }
    }
}
