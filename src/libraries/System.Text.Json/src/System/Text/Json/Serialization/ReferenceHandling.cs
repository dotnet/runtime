// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// This class defines the ways the <see cref="JsonSerializer"/> can deal with references on serialization and deserialization.
    /// </summary>
    public sealed class ReferenceHandling
    {
        /// <summary>
        /// Serialization does not support objects with cycles and does not preserve duplicate references. Metadata properties will not be written when serializing reference types and will be treated as regular properties on deserialize.
        /// </summary>
        /// <remarks>
        /// * On Serialize:
        /// Treats duplicate object references as if they were unique and writes all their properties.
        /// The serializer throws a `JsonException` if an object contains a cycle.
        /// * On Deserialize:
        /// Metadata properties (`$id`, `$values`, and `$ref`) will not be consumed and therefore will be treated as regular JSON properties.
        /// The metadata properties can map to a real property on the returned object if the property names match, or will be added to the `JsonExtensionData` overflow dictionary, if one exists; otherwise, they are ignored.
        /// </remarks>
        public static ReferenceHandling Default { get; } = new ReferenceHandling(PreserveReferencesHandling.None);

        /// <summary>
        /// Metadata properties will be honored when deserializing JSON objects and arrays into reference types and written when serializing reference types. This is necessary to create round-trippable JSON from objects that contain cycles or duplicate references.
        /// </summary>
        /// <remarks>
        /// * On Serialize:
        /// When writing complex reference types, the serializer also writes metadata properties (`$id`, `$values`, and `$ref`) within them.
        /// The output JSON will contain an extra `$id` property for every object, and for every enumerable type the JSON array emitted will be nested within a JSON object containing an `$id` and `$values` property.
        /// `RefererenceEquals` is used to determine whether objects are identical.
        /// When an object is identical to a previously serialized one, a pointer(`$ref`) to the identifier(`$id`) of such object is written instead.
        /// No metadata properties are written for value types.
        /// * On Deserialize:
        /// The metadata properties within the JSON that are used to preserve duplicated references and cycles will be honored as long as they are well-formed*.
        /// For JSON objects that don't contain any metadata properties, the deserialization behavior is identical to `ReferenceHandling.Default`.
        /// For value types:
        ///   * the `$id` metadata property is ignored.
        ///   * A `JsonException` is thrown if a `$ref` metadata property is found within the JSON object.
        ///   * For enumerable value types, the `$values` metadata property is ignored.
        /// For the metadata properties within the JSON to be considered well-formed*, they must follow these rules:
        ///   1) The `$id` metadata property must be the first property in the JSON object.
        ///   2) A JSON object that contains a `$ref` metadata property must not contain any other properties.
        ///   3) The value of the `$ref` metadata property must refer to an `$id` that has appeared earlier in the JSON.
        ///   4) The value of the `$id` and `$ref` metadata properties must be a JSON string.
        ///   5) For enumerable types,  such as `List{T}`, the JSON array must be nested within a JSON object containing an `$id` and `$values` metadata property, in that order.
        ///   6) For enumerable types, the `$values` metadata property must be a JSON array.
        ///   7) The `$values` metadata property is only valid when referring to enumerable types.
        /// If the JSON is not well-formed, a `JsonException` is thrown.
        /// </remarks>
        public static ReferenceHandling Preserve { get; } = new ReferenceHandling(PreserveReferencesHandling.All);

        private PreserveReferencesHandling _preserveHandlingOnSerialize;
        private PreserveReferencesHandling _preserveHandlingOnDeserialize;

        /// <summary>
        /// Creates a new instance of <see cref="ReferenceHandling"/> using the specified <paramref name="handling"/>
        /// </summary>
        /// <param name="handling">The specified behavior for write/read preserved references.</param>
        private ReferenceHandling(PreserveReferencesHandling handling) : this(handling, handling) { }

        // For future, someone may want to define their own custom Handler with different behaviors of PreserveReferenceHandling on Serialize vs Deserialize.
        private ReferenceHandling(PreserveReferencesHandling preserveHandlingOnSerialize, PreserveReferencesHandling preserveHandlingOnDeserialize)
        {
            _preserveHandlingOnSerialize = preserveHandlingOnSerialize;
            _preserveHandlingOnDeserialize = preserveHandlingOnDeserialize;
        }

        internal bool ShouldReadPreservedReferences()
        {
            return _preserveHandlingOnDeserialize == PreserveReferencesHandling.All;
        }

        internal bool ShouldWritePreservedReferences()
        {
            return _preserveHandlingOnSerialize == PreserveReferencesHandling.All;
        }
    }


    /// <summary>
    /// Defines behaviors to preserve references of JSON complex types.
    /// </summary>
    internal enum PreserveReferencesHandling
    {
        /// <summary>
        /// Preserved objects and arrays will not be written/read.
        /// </summary>
        None = 0,
        /// <summary>
        /// Preserved objects and arrays will be written/read.
        /// </summary>
        All = 1,
    }
}
