// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Text.Json
{
    /// <summary>
    /// Provides the ability for the user to define custom behavior when reading JSON.
    /// </summary>
    public struct JsonReaderOptions
    {
        internal const int DefaultMaxDepth = 64;

        private int _maxDepth;
        private JsonCommentHandling _commentHandling;

        /// <summary>
        /// Defines how the <see cref="Utf8JsonReader"/> should handle comments when reading through the JSON.
        /// By default <exception cref="JsonException"/> is thrown if a comment is encountered.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the comment handling enum is set to a value that is not supported (i.e. not within the <see cref="JsonCommentHandling"/> enum range).
        /// </exception>
        public JsonCommentHandling CommentHandling
        {
            get => _commentHandling;
            set
            {
                Debug.Assert(value >= 0);
                if (value > JsonCommentHandling.Allow)
                    throw ThrowHelper.GetArgumentOutOfRangeException_CommentEnumMustBeInRange(nameof(value));

                _commentHandling = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum depth allowed when reading JSON, with the default (i.e. 0) indicating a max depth of 64.
        /// Reading past this depth will throw a <exception cref="JsonException"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the max depth is set to a negative value.
        /// </exception>
        public int MaxDepth
        {
            get => _maxDepth;
            set
            {
                if (value < 0)
                    throw ThrowHelper.GetArgumentOutOfRangeException_MaxDepthMustBePositive(nameof(value));

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
