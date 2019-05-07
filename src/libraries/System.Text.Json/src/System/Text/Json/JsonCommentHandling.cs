// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json
{
    /// <summary>
    /// This enum defines the various ways the <see cref="Utf8JsonReader"/> can deal with comments.
    /// </summary>
    public enum JsonCommentHandling : byte
    {
        /// <summary>
        /// By default, do no allow comments within the JSON input.
        /// Comments are treated as invalid JSON if found and a
        /// <see cref="JsonException"/> is thrown.
        /// </summary>
        Disallow = 0,
        /// <summary>
        /// Allow comments within the JSON input and treat them as a valid token.
        /// While reading, the caller will be able to access the comment values.
        /// </summary>
        Allow = 1,
        /// <summary>
        /// Allow comments within the JSON input and ignore them.
        /// The <see cref="Utf8JsonReader"/> will behave as if no comments were present.
        /// </summary>
        Skip = 2,
    }
}
