// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// This class defines how the <see cref="JsonSerializer"/> deals with references on serialization and deserialization.
    /// </summary>
    public abstract class ReferenceHandler
    {
        /// <summary>
        /// Serialization does not support objects with cycles and does not preserve duplicate references. Metadata properties will not be written when serializing reference types and will be treated as regular properties on deserialize.
        /// </summary>
        /// <remarks>
        /// * On Serialize:
        /// Treats duplicate object references as if they were unique and writes all their properties.
        /// The serializer throws a <see cref="JsonException"/> if an object contains a cycle.
        /// * On Deserialize:
        /// Metadata properties (`$id`, `$values`, and `$ref`) will not be consumed and therefore will be treated as regular JSON properties.
        /// The metadata properties can map to a real property on the returned object if the property names match, or will be added to the <see cref="JsonExtensionDataAttribute"/> overflow dictionary, if one exists; otherwise, they are ignored.
        /// </remarks>
        public static ReferenceHandler? Default { get; } = null;

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
        /// For JSON objects that don't contain any metadata properties, the deserialization behavior is identical to <see cref="Default"/>.
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
        public static ReferenceHandler Preserve { get; } = new ReferenceHandler<DefaultReferenceResolver>();

        /// <summary>
        /// Returns the <see cref="ReferenceResolver "/> used for each serialization call.
        /// </summary>
        /// <returns>The resolver to use for serialization and deserialization.</returns>
        public abstract ReferenceResolver CreateResolver();
    }
}
