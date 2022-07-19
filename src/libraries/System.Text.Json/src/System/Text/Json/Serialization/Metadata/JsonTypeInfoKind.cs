// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Determines the kind of contract metadata a given JsonTypeInfo instance is customizing
    /// </summary>
    public enum JsonTypeInfoKind
    {
        /// <summary>
        /// Type is either a primitive value or uses a custom converter. JsonTypeInfo metadata does not apply here.
        /// </summary>
        None = 0,
        /// <summary>
        /// Type is serialized as object with properties
        /// </summary>
        Object = 1,
        /// <summary>
        /// Type is serialized as a collection with elements
        /// </summary>
        Enumerable = 2,
        /// <summary>
        /// Type is serialized as a dictionary with key/value pair entries
        /// </summary>
        Dictionary = 3
    }
}
