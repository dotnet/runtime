// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Specifies the enum member that is present in the JSON when serializing and deserializing.
    /// This overrides any naming policy specified by <see cref="JsonNamingPolicy"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class JsonStringEnumMemberAttribute : JsonAttribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="JsonStringEnumMemberAttribute"/> with the specified enum member name.
        /// </summary>
        /// <param name="name">The name of the enum member.</param>
        public JsonStringEnumMemberAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// The name of the enum member.
        /// </summary>
        public string Name { get; }
    }
}
