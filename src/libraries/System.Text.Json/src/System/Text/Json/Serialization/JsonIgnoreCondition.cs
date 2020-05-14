// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// When specified on <see cref="JsonSerializerOptions.DefaultIgnoreCondition"/>,
    /// <see cref="WhenWritingDefault"/> specifies that properties with default values are ignored during serialization.
    /// When specified on <see cref="JsonIgnoreAttribute.Condition"/>, controls whether
    /// a property is ignored during serialization and deserialization.
    /// </summary>
    public enum JsonIgnoreCondition
    {
        /// <summary>
        /// Property is never ignored during serialization or deserialization.
        /// </summary>
        Never = 0,
        /// <summary>
        /// Property is always ignored during serialization and deserialization.
        /// </summary>
        Always = 1,
        /// <summary>
        /// If the value is the default, the property is ignored during serialization.
        /// </summary>
        WhenWritingDefault = 2,
    }
}
