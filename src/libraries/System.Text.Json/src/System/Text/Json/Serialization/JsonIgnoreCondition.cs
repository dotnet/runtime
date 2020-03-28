// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Controls how the <see cref="JsonIgnoreAttribute"/> ignores properties on serialization and deserialization.
    /// </summary>
    public enum JsonIgnoreCondition
    {
        /// <summary>
        /// Property will always be ignored.
        /// </summary>
        Always = 0,
        /// <summary>
        /// Property will only be ignored if it is null.
        /// </summary>
        WhenNull = 1,
        /// <summary>
        /// Property will always be serialized and deserialized, regardless of <see cref="JsonSerializerOptions.IgnoreNullValues"/> configuration.
        /// </summary>
        Never = 2
    }
}
