// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json
{
    /// <summary>
    /// Provides the ability for the user to define custom behavior when reading JSON.
    /// </summary>
    public struct JsonReaderOptions
    {
        internal const int DefaultMaxDepth = 64;

        private int _maxDepth;

        /// <summary>
        /// Defines how the <see cref="Utf8JsonReader"/> should handle comments when reading through the JSON.
        /// By default <exception cref="JsonException"/> is thrown if a comment is encountered.
        /// </summary>
        public JsonCommentHandling CommentHandling { get; set; }

        /// <summary>
        /// Gets or sets the maximum depth allowed when reading JSON, with the default (i.e. 0) indicating a max depth of 64.
        /// Reading past this depth will throw a <exception cref="JsonException"/>.
        /// </summary>
        public int MaxDepth
        {
            get => _maxDepth;
            set
            {
                if (value < 0)
                    throw ThrowHelper.GetArgumentException_MaxDepthMustBePositive();

                _maxDepth = value;
            }
        }

        /// <summary>
        /// Defines whether an extra comma at the end of a list of JSON values in an object or array
        /// is allowed (and ignored) within the JSON payload being read.
        /// By default, it's set to false, and <exception cref="JsonException"/> is thrown if a trailing comma is encountered.
        /// </summary>
        public bool AllowTrailingCommas { get; set; }
    }
}
