// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Specifies the property name that is present in the JSON when serializing and deserializing.
    /// This overrides any naming policy specified by <see cref="JsonNamingPolicy"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class JsonPropertyNameAttribute : JsonAttribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="JsonPropertyNameAttribute"/> with the specified property name.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        public JsonPropertyNameAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// The name of the property.
        /// </summary>
        public string Name { get; }
    }
}
