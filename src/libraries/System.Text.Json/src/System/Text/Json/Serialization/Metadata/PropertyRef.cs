// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Represents a UTF-8 encoded JSON property name and its associated <see cref="JsonPropertyInfo"/>, if available.
    /// PropertyRefs use byte sequence equality, so equal JSON strings with alternate encodings or casings are not equal.
    /// Used as a first-level cache for property lookups before falling back to UTF decoding and string comparison.
    /// </summary>
    internal readonly struct PropertyRef(ulong key, JsonPropertyInfo? info, byte[] utf8PropertyName)
    {
        /// <summary>
        /// A custom hashcode produced from the UTF-8 encoded property name.
        /// </summary>
        public readonly ulong Key = key;

        /// <summary>
        /// The <see cref="JsonPropertyInfo"/> associated with the property name, if available.
        /// </summary>
        public readonly JsonPropertyInfo? Info = info;

        /// <summary>
        /// Caches a heap allocated copy of the UTF-8 encoded property name.
        /// </summary>
        public readonly byte[] Utf8PropertyName = utf8PropertyName;
    }

    /// <summary>
    /// Defines a list of <see cref="PropertyRef"/> instances used for tracking optimistic cache update operations.
    /// </summary>
    internal sealed class PropertyRefList(PropertyRef[]? originalCache) : List<PropertyRef>
    {
        /// <summary>
        /// Stores a reference to the original cache off which the current list is being built.
        /// </summary>
        public readonly PropertyRef[]? OriginalCache = originalCache;
    }
}
