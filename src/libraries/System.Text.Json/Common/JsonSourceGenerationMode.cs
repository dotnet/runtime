// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// The generation mode for the System.Text.Json source generator.
    /// </summary>
    [Flags]
#if BUILDING_SOURCE_GENERATOR
    internal
#else
    public
#endif
    enum JsonSourceGenerationMode
    {
        /// <summary>
        /// Instructs the JSON source generator to generate serialization logic and type metadata to fallback to
        /// when the run-time options are not compatible with the indicated <see cref="JsonSerializerOptionsAttribute"/>.
        /// </summary>
        /// <remarks>
        /// This mode supports all <see cref="JsonSerializer"/> features.
        /// </remarks>
        MetadataAndSerialization = 0,

        /// <summary>
        /// Instructs the JSON source generator to generate type-metadata initialization logic.
        /// </summary>
        /// <remarks>
        /// This mode supports all <see cref="JsonSerializer"/> features.
        /// </remarks>
        Metadata = 1,

        /// <summary>
        /// Instructs the JSON source generator to generate serialization logic.
        /// </summary>
        /// <remarks>
        /// This mode supports only a subset of <see cref="JsonSerializer"/> features.
        /// </remarks>
        Serialization = 2
    }
}
