// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Nodes
{
    /// <summary>
    /// Represents a mutable JSON value.
    /// </summary>
    public abstract partial class JsonValue : JsonNode
    {
        internal const string CreateUnreferencedCodeMessage = "Creating JsonValue instances with non-primitive types is not compatible with trimming. It can result in non-primitive types being serialized, which may have their members trimmed.";
        internal const string CreateDynamicCodeMessage = "Creating JsonValue instances with non-primitive types requires generating code at runtime.";

        private protected JsonValue(JsonNodeOptions? options) : base(options) { }

        /// <summary>
        ///   Tries to obtain the current JSON value and returns a value that indicates whether the operation succeeded.
        /// </summary>
        /// <remarks>
        ///   {T} can be the type or base type of the underlying value.
        ///   If the underlying value is a <see cref="JsonElement"/> then {T} can also be the type of any primitive
        ///   value supported by current <see cref="JsonElement"/>.
        ///   Specifying the <see cref="object"/> type for {T} will always succeed and return the underlying value as <see cref="object"/>.<br />
        ///   The underlying value of a <see cref="JsonValue"/> after deserialization is an instance of <see cref="JsonElement"/>,
        ///   otherwise it's the value specified when the <see cref="JsonValue"/> was created.
        /// </remarks>
        /// <seealso cref="JsonNode.GetValue{T}"></seealso>
        /// <typeparam name="T">The type of value to obtain.</typeparam>
        /// <param name="value">When this method returns, contains the parsed value.</param>
        /// <returns><see langword="true"/> if the value can be successfully obtained; otherwise, <see langword="false"/>.</returns>
        public abstract bool TryGetValue<T>([NotNullWhen(true)] out T? value);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <returns>
        ///   The new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </returns>
        /// <typeparam name="T">The type of value to create.</typeparam>
        /// <param name="value">The value to create.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        [RequiresUnreferencedCode(CreateUnreferencedCodeMessage + " Use the overload that takes a JsonTypeInfo, or make sure all of the required types are preserved.")]
        [RequiresDynamicCode(CreateDynamicCodeMessage)]
        public static JsonValue? Create<T>(T? value, JsonNodeOptions? options = null)
        {
            if (value is null)
            {
                return null;
            }

            if (value is JsonNode)
            {
                ThrowHelper.ThrowArgumentException_NodeValueNotAllowed(nameof(value));
            }

            if (value is JsonElement element)
            {
                return CreateFromElement(ref element, options);
            }

            var jsonTypeInfo = (JsonTypeInfo<T>)JsonSerializerOptions.Default.GetTypeInfo(typeof(T));
            return CreateFromTypeInfo(value, jsonTypeInfo, options);
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <returns>
        ///   The new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </returns>
        /// <typeparam name="T">The type of value to create.</typeparam>
        /// <param name="value">The value to create.</param>
        /// <param name="jsonTypeInfo">The <see cref="JsonTypeInfo"/> that will be used to serialize the value.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create<T>(T? value, JsonTypeInfo<T> jsonTypeInfo, JsonNodeOptions? options = null)
        {
            if (jsonTypeInfo is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(jsonTypeInfo));
            }

            if (value is null)
            {
                return null;
            }

            if (value is JsonNode)
            {
                ThrowHelper.ThrowArgumentException_NodeValueNotAllowed(nameof(value));
            }

            jsonTypeInfo.EnsureConfigured();

            if (value is JsonElement element && jsonTypeInfo.EffectiveConverter.IsInternalConverter)
            {
                return CreateFromElement(ref element, options);
            }

            return CreateFromTypeInfo(value, jsonTypeInfo, options);
        }

        internal override bool DeepEqualsCore(JsonNode otherNode)
        {
            if (GetValueKind() != otherNode.GetValueKind())
            {
                return false;
            }

            // Fall back to slow path that converts the nodes to JsonElement.
            JsonElement thisElement = ToJsonElement(this, out JsonDocument? thisDocument);
            JsonElement otherElement = ToJsonElement(otherNode, out JsonDocument? otherDocument);
            try
            {
                return JsonElement.DeepEquals(thisElement, otherElement);
            }
            finally
            {
                thisDocument?.Dispose();
                otherDocument?.Dispose();
            }

            static JsonElement ToJsonElement(JsonNode node, out JsonDocument? backingDocument)
            {
                if (node.UnderlyingElement is { } element)
                {
                    backingDocument = null;
                    return element;
                }

                Utf8JsonWriter writer = Utf8JsonWriterCache.RentWriterAndBuffer(
                    options: default,
                    JsonSerializerOptions.BufferSizeDefault,
                    out PooledByteBufferWriter output);

                try
                {
                    node.WriteTo(writer);
                    writer.Flush();
                    Utf8JsonReader reader = new(output.WrittenMemory.Span);
                    backingDocument = JsonDocument.ParseValue(ref reader);
                    return backingDocument.RootElement;
                }
                finally
                {
                    Utf8JsonWriterCache.ReturnWriterAndBuffer(writer, output);
                }
            }
        }

        internal sealed override void GetPath(ref ValueStringBuilder path, JsonNode? child)
        {
            Debug.Assert(child == null);

            Parent?.GetPath(ref path, this);
        }

        internal static JsonValue CreateFromTypeInfo<T>(T value, JsonTypeInfo<T> jsonTypeInfo, JsonNodeOptions? options = null)
        {
            Debug.Assert(jsonTypeInfo.IsConfigured);
            Debug.Assert(value != null);

            if (JsonValue<T>.TypeIsSupportedPrimitive &&
                jsonTypeInfo is { EffectiveConverter.IsInternalConverter: true } &&
                (jsonTypeInfo.EffectiveNumberHandling & JsonNumberHandling.WriteAsString) is 0)
            {
                // If the type is using the built-in converter for a known primitive,
                // switch to the more efficient JsonValuePrimitive<T> implementation.
                return new JsonValuePrimitive<T>(value, jsonTypeInfo.EffectiveConverter, options);
            }

            return new JsonValueCustomized<T>(value, jsonTypeInfo, options);
        }

        internal static JsonValue? CreateFromElement(ref readonly JsonElement element, JsonNodeOptions? options = null)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Null:
                    return null;

                case JsonValueKind.Object:
                case JsonValueKind.Array:
                    // Force usage of JsonArray and JsonObject instead of supporting those in an JsonValue.
                    ThrowHelper.ThrowInvalidOperationException_NodeElementCannotBeObjectOrArray();
                    return null;

                default:
                    return new JsonValueOfElement(element, options);
            }
        }
    }
}
