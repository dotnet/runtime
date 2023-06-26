// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization;

/// <summary>
/// Determines what JSON keys should be ignored on dictionary deserialization.
/// </summary>
public abstract class JsonDictionaryKeyFilter
{
    /// <summary>
    /// Initializes a new instance of <see cref="JsonDictionaryKeyFilter"/>.
    /// </summary>
    protected JsonDictionaryKeyFilter() { }

    /// <summary>
    /// Returns the key filter that ignores any metadata keys starting with $, such as `$schema`.
    /// </summary>
    public static JsonDictionaryKeyFilter IgnoreMetadataNames { get; } = new JsonIgnoreMetadataNamesDictionaryKeyFilter();

    /// <summary>
    /// When overridden in a derived class, ignore keys according to filter.
    /// </summary>
    /// <param name="utf8Key">The UTF8 string with key name to filter.</param>
    /// <returns>true to ignore that key.</returns>
    public abstract bool IgnoreKey(ReadOnlySpan<byte> utf8Key);
}
