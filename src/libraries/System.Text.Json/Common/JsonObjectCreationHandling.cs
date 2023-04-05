// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{

    /// <summary>
    /// Indicates how .NET properties or fields should be populated during deserialization.
    /// </summary>
#if BUILDING_SOURCE_GENERATOR
    internal
#else
    public
#endif
    enum JsonObjectCreationHandling
    {
        /// <summary>
        /// Member is replaced during deserialization.
        /// </summary>
        Replace = 0,

        /// <summary>
        /// Member is populated during deserialization.
        /// </summary>
        Populate = 1,
    }
}
