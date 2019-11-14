// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Prevents a property from being serialized or deserialized.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class JsonIgnoreAttribute : JsonAttribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="JsonIgnoreAttribute"/>.
        /// </summary>
        public JsonIgnoreAttribute() { }
    }
}
