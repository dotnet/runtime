// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// When specified on <see cref="JsonSerializerOptions.DefaultIgnoreCondition"/>,
    /// determines when properties and fields across the type graph are ignored.
    /// When specified on <see cref="JsonIgnoreAttribute.Condition"/>, controls whether
    /// a property or field is ignored during serialization and deserialization. This option
    /// overrides the setting on <see cref="JsonSerializerOptions.DefaultIgnoreCondition"/>.
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
        /// This is applied to both reference and value-type properties and fields.
        /// </summary>
        WhenWritingDefault = 2,
        /// <summary>
        /// If the value is <see langword="null"/>, the property is ignored during serialization.
        /// This is applied only to reference-type properties and fields.
        /// </summary>
        WhenWritingNull = 3,
    }
}
