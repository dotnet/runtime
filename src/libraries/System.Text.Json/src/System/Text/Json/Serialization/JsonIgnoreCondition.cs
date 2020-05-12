// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// When specified on <see cref="JsonSerializerOptions.DefaultIgnoreCondition"/>, controls whether properties with default values are ignored during serialization.
    /// When specified on <see cref="JsonIgnoreAttribute.Condition"/>, controls whether a property is ignored during serialization and deserialization.
    /// </summary>
    public enum JsonIgnoreCondition
    {
        /// <summary>
        /// Property will always be serialized and deserialized.
        /// </summary>
        Never = 0,
        /// <summary>
        /// Property will never be serialized or deserialized.
        /// </summary>
        Always = 1,
        /// <summary>
        /// Property will not be serialized if it is the default value.
        /// </summary>
        WhenWritingDefault = 2,
    }
}
