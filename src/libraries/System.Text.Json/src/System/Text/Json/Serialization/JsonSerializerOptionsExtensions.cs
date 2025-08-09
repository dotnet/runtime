// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json;

/// <summary>
/// Extension methods for <see cref="JsonSerializerOptions"/>.
/// </summary>
public static class JsonSerializerOptionsExtensions
{
    /// <summary>
    /// Gets the <see cref="JsonTypeInfo"/> contract metadata resolved by the current <see cref="JsonSerializerOptions"/> instance.
    /// </summary>
    /// <returns>The contract metadata resolved for <typeparamref name="T"/>.</returns>
    /// <remarks>
    /// If the <see cref="JsonSerializerOptions"/> instance is locked for modification, the method will return a cached instance for the metadata.
    /// </remarks>
    public static JsonTypeInfo<T> GetTypeInfo<T>(this JsonSerializerOptions options)
        => (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
}
