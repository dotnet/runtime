// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// This class defines how the <see cref="JsonSerializer"/> deals with references on serialization and deserialization.
    /// </summary>
    public abstract class ReferenceHandler
    {
        /// <summary>
        /// Indicates whether this ReferenceHandler implementation should use <see cref="ReferenceHandlingStrategy.Preserve"/> semantics or <see cref="ReferenceHandlingStrategy.IgnoreCycles"/> semantics.
        /// The defualt is Preserve.
        /// </summary>
        internal ReferenceHandlingStrategy HandlingStrategy = ReferenceHandlingStrategy.Preserve;

        /// <summary>
        /// Metadata properties will be honored when deserializing JSON objects and arrays into reference types and written when serializing reference types. This is necessary to create round-trippable JSON from objects that contain cycles or duplicate references.
        /// </summary>
        /// <remarks>
        /// * On Serialize:
        /// When writing complex reference types, the serializer also writes metadata properties (`$id`, `$values`, and `$ref`) within them.
        /// The output JSON will contain an extra `$id` property for every object, and for every enumerable type the JSON array emitted will be nested within a JSON object containing an `$id` and `$values` property.
        /// <see cref="object.ReferenceEquals(object?, object?)"/> is used to determine whether objects are identical.
        /// When an object is identical to a previously serialized one, a pointer (`$ref`) to the identifier (`$id`) of such object is written instead.
        /// No metadata properties are written for value types.
        /// * On Deserialize:
        /// The metadata properties within the JSON that are used to preserve duplicated references and cycles will be honored as long as they are well-formed**.
        /// For JSON objects that don't contain any metadata properties, the deserialization behavior is identical to <see langword="null"/>.
        /// For value types:
        ///   * The `$id` metadata property is ignored.
        ///   * A <see cref="JsonException"/> is thrown if a `$ref` metadata property is found within the JSON object.
        ///   * For enumerable value types, the `$values` metadata property is ignored.
        /// ** For the metadata properties within the JSON to be considered well-formed, they must follow these rules:
        ///   1) The `$id` metadata property must be the first property in the JSON object.
        ///   2) A JSON object that contains a `$ref` metadata property must not contain any other properties.
        ///   3) The value of the `$ref` metadata property must refer to an `$id` that has appeared earlier in the JSON.
        ///   4) The value of the `$id` and `$ref` metadata properties must be a JSON string.
        ///   5) For enumerable types, such as <see cref="List{T}"/>, the JSON array must be nested within a JSON object containing an `$id` and `$values` metadata property, in that order.
        ///   6) For enumerable types, the `$values` metadata property must be a JSON array.
        ///   7) The `$values` metadata property is only valid when referring to enumerable types.
        /// If the JSON is not well-formed, a <see cref="JsonException"/> is thrown.
        /// </remarks>
        public static ReferenceHandler Preserve { get; } = new PreserveReferenceHandler();

        /// <summary>
        /// Ignores an object when a reference cycle is detected during serialization.
        /// </summary>
        public static ReferenceHandler IgnoreCycles { get; } = new IgnoreReferenceHandler();

        /// <summary>
        /// Returns the <see cref="ReferenceResolver "/> used for each serialization call.
        /// </summary>
        /// <returns>The resolver to use for serialization and deserialization.</returns>
        public abstract ReferenceResolver CreateResolver();

        /// <summary>
        /// Optimization for the resolver used when <see cref="Preserve"/> is set in <see cref="JsonSerializerOptions.ReferenceHandler"/>;
        /// we pass a flag signaling whether this is called from serialization or deserialization to save one dictionary instantiation.
        /// </summary>
        internal virtual ReferenceResolver CreateResolver(bool writing) => CreateResolver();
    }
}
