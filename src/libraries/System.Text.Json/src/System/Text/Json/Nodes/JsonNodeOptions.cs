// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Nodes
{
    /// <summary>
    ///   Options to control <see cref="JsonNode"/> behavior.
    /// </summary>
    public struct JsonNodeOptions
    {
        /// <summary>
        ///   Specifies whether property names on <see cref="JsonObject"/> are case insensitive.
        /// </summary>
        public bool PropertyNameCaseInsensitive { get; set; }
    }
}
