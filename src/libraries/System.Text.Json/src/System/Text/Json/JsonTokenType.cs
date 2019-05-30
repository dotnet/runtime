// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json
{
    /// <summary>
    /// This enum defines the various JSON tokens that make up a JSON text and is used by
    /// the <see cref="Utf8JsonReader"/> when moving from one token to the next.
    /// The <see cref="Utf8JsonReader"/> starts at 'None' by default. The 'Comment' enum value
    /// is only ever reached in a specific <see cref="Utf8JsonReader"/> mode and is not
    /// reachable by default.
    /// </summary>
    public enum JsonTokenType : byte
    {
        // Do not re-order.
        // We rely on the ordering to quickly check things like IsTokenTypePrimitive

        /// <summary>
        ///   Indicates that there is no value (as distinct from <see cref="Null"/>).
        /// </summary>
        /// <remarks>
        ///   This is the default token type if no data has been read by the <see cref="Utf8JsonReader"/>.
        /// </remarks>
        None,

        /// <summary>
        ///   Indicates that the token type is the start of a JSON object.
        /// </summary>
        StartObject,

        /// <summary>
        ///   Indicates that the token type is the end of a JSON object.
        /// </summary>
        EndObject,

        /// <summary>
        ///   Indicates that the token type is the start of a JSON array.
        /// </summary>
        StartArray,

        /// <summary>
        ///   Indicates that the token type is the end of a JSON array.
        /// </summary>
        EndArray,

        /// <summary>
        ///   Indicates that the token type is a JSON property name.
        /// </summary>
        PropertyName,

        /// <summary>
        ///   Indicates that the token type is a JSON string.
        /// </summary>
        String,

        /// <summary>
        ///   Indicates that the token type is a JSON number.
        /// </summary>
        Number,

        /// <summary>
        ///   Indicates that the token type is the JSON literal <c>true</c>.
        /// </summary>
        True,

        /// <summary>
        ///   Indicates that the token type is the JSON literal <c>false</c>.
        /// </summary>
        False,

        /// <summary>
        ///   Indicates that the token type is the JSON literal <c>null</c>.
        /// </summary>
        Null,

        /// <summary>
        ///   Indicates that the token type is the comment string.
        /// </summary>
        Comment,
    }
}
