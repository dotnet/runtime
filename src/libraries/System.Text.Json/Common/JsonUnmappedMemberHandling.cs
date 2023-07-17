// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Determines how <see cref="JsonSerializer"/> handles JSON properties that
    /// cannot be mapped to a specific .NET member when deserializing object types.
    /// </summary>
    public enum JsonUnmappedMemberHandling
    {
        /// <summary>
        /// Silently skips any unmapped properties. This is the default behavior.
        /// </summary>
        Skip = 0,

        /// <summary>
        /// Throws an exception when an unmapped property is encountered.
        /// </summary>
        Disallow = 1,
    }
}
