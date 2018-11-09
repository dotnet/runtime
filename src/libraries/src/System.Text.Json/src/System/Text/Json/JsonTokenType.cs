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
        None,
        StartObject,
        EndObject,
        StartArray,
        EndArray,
        PropertyName,
        String,
        Number,
        True,
        False,
        Null,
        Comment,
    }
}
