// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Text.Json
{
    /// <summary>
    ///   Provides the ability for the user to define custom behavior when parsing JSON to create a <see cref="JsonNode"/>.
    /// </summary>
    public struct JsonNodeOptions
    {
        internal const int DefaultMaxDepth = 64;

        private int _maxDepth;
        private JsonCommentHandling _commentHandling;

        /// <summary>
        ///   Gets or sets a value that determines how the <see cref="JsonNode"/> handles comments when reading through the JSON data.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The comment handling enum is set to a value that is not supported (or not within the <see cref="JsonCommentHandling"/> enum range).
        /// </exception>
        /// <remarks>
        ///   By default <exception cref="JsonException"/> is thrown if a comment is encountered.
        /// </remarks>
        public JsonCommentHandling CommentHandling
        {
            readonly get => _commentHandling;
            set
            {
                Debug.Assert(value >= 0);
                if (value > JsonCommentHandling.Skip)
                    throw new ArgumentOutOfRangeException(nameof(value), SR.JsonDocumentDoesNotSupportComments);

                _commentHandling = value;
            }
        }

        /// <summary>
        ///   Gets or sets the maximum depth allowed when parsing JSON data,
        ///   with the default (that is, 0) indicating a maximum depth of 64.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The max depth is set to a negative value.
        /// </exception>
        /// <value>
        ///   The maximum depth allowed when parsing JSON data.
        /// </value>
        /// <remarks>
        ///   Parsing past this depth will throw a <exception cref="JsonException"/>.
        /// </remarks>
        public int MaxDepth
        {
            readonly get => _maxDepth;
            set
            {
                if (value < 0)
                    throw ThrowHelper.GetArgumentOutOfRangeException_MaxDepthMustBePositive(nameof(value));

                _maxDepth = value;
            }
        }

        /// <summary>
        ///   Gets or sets a value that indicates whether an extra comma at the end of a list of JSON values
        ///   in an object or array is allowed (and ignored) within the JSON payload being read.
        /// </summary>
        /// <returns>
        ///   <see langword="true"/> if an extra comma at the end of a list of JSON values in an object or array is allowed;
        ///   otherwise, <see langword="false"/>. Default is <see langword="false"/>.
        /// </returns>
        /// <remarks>
        ///   By default, it's set to false, and <exception cref="JsonException"/> is thrown if a trailing comma is encountered.
        /// </remarks>
        public bool AllowTrailingCommas { get; set; }

        private DuplicatePropertyNameHandlingStrategy _duplicatePropertyNameHandlingStrategy;

        /// <summary>
        ///   Gets or sets the duplicate property name handling strategy.
        /// </summary>
        /// <remarks>
        ///   By default, it's set to <exception cref="DuplicatePropertyNameHandlingStrategy.Replace"/>.
        /// </remarks>
        public DuplicatePropertyNameHandlingStrategy DuplicatePropertyNameHandling
        {
            readonly get => _duplicatePropertyNameHandlingStrategy;
            set
            {
                if ((uint)value > (uint)DuplicatePropertyNameHandlingStrategy.Error)
                    throw new ArgumentOutOfRangeException(SR.InvalidDuplicatePropertyNameHandling);

                _duplicatePropertyNameHandlingStrategy = value;
            }
        }

        internal JsonReaderOptions GetReaderOptions()
        {
            return new JsonReaderOptions
            {
                AllowTrailingCommas = AllowTrailingCommas,
                CommentHandling = CommentHandling,
                MaxDepth = MaxDepth
            };
        }
    }
}
