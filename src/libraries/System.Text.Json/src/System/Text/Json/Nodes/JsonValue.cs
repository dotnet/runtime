// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

        private protected JsonValue(JsonNodeOptions? options = null) : base(options) { }

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

            if (value is JsonElement element)
            {
                if (element.ValueKind is JsonValueKind.Null)
                {
                    return null;
                }

                VerifyJsonElementIsNotArrayOrObject(ref element);

                return new JsonValuePrimitive<JsonElement>(element, JsonMetadataServices.JsonElementConverter, options);
            }

            var jsonTypeInfo = (JsonTypeInfo<T>)JsonSerializerOptions.Default.GetTypeInfo(typeof(T));
            return new JsonValueCustomized<T>(value, jsonTypeInfo, options);
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

            if (value is JsonElement element)
            {
                if (element.ValueKind is JsonValueKind.Null)
                {
                    return null;
                }

                VerifyJsonElementIsNotArrayOrObject(ref element);
            }

            jsonTypeInfo.EnsureConfigured();
            return new JsonValueCustomized<T>(value, jsonTypeInfo, options);
        }

        internal override void GetPath(ref ValueStringBuilder path, JsonNode? child)
        {
            Debug.Assert(child == null);

            Parent?.GetPath(ref path, this);
        }

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

        private static void VerifyJsonElementIsNotArrayOrObject(ref JsonElement element)
        {
            // Force usage of JsonArray and JsonObject instead of supporting those in an JsonValue.
            if (element.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
            {
                ThrowHelper.ThrowInvalidOperationException_NodeElementCannotBeObjectOrArray();
            }
        }
    }
}
