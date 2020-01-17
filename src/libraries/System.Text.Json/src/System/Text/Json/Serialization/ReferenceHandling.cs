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
        /// Reference handling semantics will not be applied to JSON objects and arrays when serializing or deserializing.
        /// </summary>
        /// <remarks>
        /// * On Serialize: Throw a `JsonException` when `MaxDepth` is exceeded. This may occur by either a reference loop or by passing a very deep object. This option will not affect the performance of the serializer.
        /// * On Deserialize: Metadata properties will not be consumed, therefore metadata properties(`$id`, `$values`, and `$ref`) will be treated as regular properties that can map to a real property using `JsonPropertyName` or be added to the `JsonExtensionData` overflow dictionary.
        /// </remarks>
        public static ReferenceHandling Default { get; } = new ReferenceHandling(PreserveReferencesHandling.None);
        /// <summary>
        /// Reference metadata will be written and honored when deserializing and serializing JSON objects and arrays.
        /// </summary>
        /// <remarks>
        /// * On Serialize: When writing complex CLR types (e.g. POCOs/non-primitive types), the serializer also writes metadata (`$id`, `$values`, and `$ref`) properties within them in order to reference them later by writing a pointer to the previously written JSON object or array on the CLR objects that are identical; this is very useful to prevent serialization cycles and preserve reference equality through serialization.
        /// * On Deserialize: The metadata properties emitted on serialization will be expected (although they are not mandatory) and the deserializer will try to understand them.
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
