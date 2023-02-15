// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Used for resolving metadata recursively within a <see cref="JsonTypeInfo"/> graph.
    /// </summary>
    internal interface IMetadataResolutionContext
    {
        JsonTypeInfo Resolve(Type type);
    }
}
