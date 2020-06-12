// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Indicates that the member should be included for serialization and deserialization.
    /// </summary>
    /// <remarks>
    /// When applied to a property, indicates that non-public getters and setters can be used for serialization and deserialization.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class JsonIncludeAttribute : JsonAttribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="JsonIncludeAttribute"/>.
        /// </summary>
        public JsonIncludeAttribute() { }
    }
}
