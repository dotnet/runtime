// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Defines how deserializing a type declared as an <see cref="object"/> is handled during deserialization.
    /// </summary>
    public enum JsonUnknownTypeHandling
    {
        /// <summary>
        /// A type declared as <see cref="object"/> is deserialized as a <see cref="JsonElement"/>.
        /// </summary>
        JsonElement = 0,
        /// <summary>
        /// A type declared as <see cref="object"/> is deserialized as a <see cref="JsonNode"/>.
        /// </summary>
        JsonNode = 1
    }
}
