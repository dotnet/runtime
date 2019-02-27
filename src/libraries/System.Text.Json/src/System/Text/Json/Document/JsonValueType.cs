// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json
{
    /// <summary>
    ///   Specifies the data type of a JSON value.
    /// </summary>
    public enum JsonValueType : byte
    {
        /// <summary>
        ///   Indicates that there is no value (as distinct from <see cref="Null"/>).
        /// </summary>
        Undefined,

        /// <summary>
        ///   Indicates that a value is a JSON object.
        /// </summary>
        Object,

        /// <summary>
        ///   Indicates that a value is a JSON array.
        /// </summary>
        Array,

        /// <summary>
        ///   Indicates that a value is a JSON string.
        /// </summary>
        String,

        /// <summary>
        ///   Indicates that a value is a JSON number.
        /// </summary>
        Number,

        /// <summary>
        ///   Indicates that a value is the JSON value <c>true</c>.
        /// </summary>
        True,

        /// <summary>
        ///   Indicates that a value is the JSON value <c>false</c>.
        /// </summary>
        False,

        /// <summary>
        ///   Indicates that a value is the JSON value <c>null</c>.
        /// </summary>
        Null,
    }
}
