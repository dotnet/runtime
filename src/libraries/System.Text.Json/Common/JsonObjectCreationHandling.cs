// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{

    /// <summary>
    /// Determines how deserialization will handle object creation for fields or properties.
    /// </summary>
#if BUILDING_SOURCE_GENERATOR
    internal
#else
    public
#endif
    enum JsonObjectCreationHandling
    {
        /// <summary>
        /// A new instance will always be created when deserializing a field or property.
        /// </summary>
        Replace = 0,

        /// <summary>
        /// Attempt to populate any instances already found on a deserialized field or property.
        /// </summary>
        Populate = 1,
    }
}
