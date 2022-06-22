// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Exposes method for resolving Type into JsonTypeInfo for given options.
    /// </summary>
    public interface IJsonTypeInfoResolver
    {
        /// <summary>
        /// Resolves Type into JsonTypeInfo which defines serialization and deserialization logic.
        /// </summary>
        /// <param name="type">Type to be resolved.</param>
        /// <param name="options">JsonSerializerOptions instance defining resolution parameters.</param>
        /// <returns>Returns JsonTypeInfo instance or null if the resolver cannot produce metadata for this type.</returns>
        JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options);
    }
}
