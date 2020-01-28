// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        // Whether the converter should handle the null value.
        internal virtual bool HandleNullValue
        {
            get
            {
                // Allow a converter that can't be null to return a null value representation, such as JsonElement or Nullable<>.
                // In other cases, this will likely cause an JsonException in the converter.
                return TypeToConvert.IsValueType;
            }
        }

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

        internal abstract Type? ElementType { get; }

        // For polymorphic cases, the concrete type to create.
        internal virtual Type RuntimeType => TypeToConvert;

        internal bool ShouldFlush(Utf8JsonWriter writer, ref WriteStack state)
        {
            // If surpassed flush threshold then return false which will flush stream.
            return (state.FlushThreshold > 0 && writer.BytesPending > state.FlushThreshold);
        }

        // This is used internally to quickly determine the type being converted for JsonConverter<T>.
        internal abstract Type TypeToConvert { get; }

        internal abstract bool TryReadAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out object? value);
        internal abstract bool TryWriteAsObject(Utf8JsonWriter writer, object? value, JsonSerializerOptions options, ref WriteStack state);
    }
}
