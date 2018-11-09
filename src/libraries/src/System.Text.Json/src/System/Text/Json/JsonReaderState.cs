// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Text.Json
{
    /// <summary>
    /// Defines an opaque type that holds and saves all the relevant state information which must be provided
    /// to the <see cref="Utf8JsonReader"/> to continue reading after processing incomplete data.
    /// This type is required to support reentrancy when reading incomplete data, and to continue
    /// reading once more data is available. Unlike the <see cref="Utf8JsonReader"/>, which is a ref struct,
    /// this type can survive across async/await boundaries and hence this type is required to provide
    /// support for reading in more data asynchronously before continuing with a new instance of the <see cref="Utf8JsonReader"/>.
    /// </summary>
    public struct JsonReaderState
    {
        // We are using a ulong to represent our nested state, so we can only
        // go 64 levels deep without having to allocate a stack.
        internal const int StackFreeMaxDepth = sizeof(ulong) * 8;

        // This ulong container represents a tiny stack to track the state during nested transitions.
        // The first bit represents the state of the current depth (1 == object, 0 == array).
        // Each subsequent bit is the parent / containing type (object or array). Since this
        // reader does a linear scan, we only need to keep a single path as we go through the data.
        // This is primarily used as an optimization to avoid having to allocate a stack object for
        // depths up to 64 (which is the default max depth).
        internal ulong _stackFreeContainer;
        internal long _lineNumber;
        internal long _bytePositionInLine;
        internal long _bytesConsumed;
        internal int _currentDepth;
        internal int _maxDepth;
        internal bool _inObject;
        internal bool _isNotPrimitive;
        internal JsonTokenType _tokenType;
        internal JsonTokenType _previousTokenType;
        internal JsonReaderOptions _readerOptions;
        internal Stack<JsonTokenType> _stack;

        /// <summary>
        /// Returns the total amount of bytes consumed by the <see cref="Utf8JsonReader"/> so far
        /// for the given UTF-8 encoded input text.
        /// </summary>
        public long BytesConsumed => _bytesConsumed;

        /// <summary>
        /// Constructs a new <see cref="JsonReaderState"/> instance.
        /// </summary>
        /// <param name="maxDepth">Sets the maximum depth allowed when reading JSON, with the default set as 64.
        /// Reading past this depth will throw a <exception cref="JsonReaderException"/>.</param>
        /// <param name="options">Defines the customized behavior of the <see cref="Utf8JsonReader"/>
        /// that is different from the JSON RFC (for example how to handle comments).
        /// By default, the <see cref="Utf8JsonReader"/> follows the JSON RFC strictly (i.e. comments within the JSON are invalid).</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the max depth is set to a non-positive value (&lt;= 0)
        /// </exception>
        /// <remarks>
        /// An instance of this state must be passed to the <see cref="Utf8JsonReader"/> ctor with the JSON data.
        /// Unlike the <see cref="Utf8JsonReader"/>, which is a ref struct, the state can survive
        /// across async/await boundaries and hence this type is required to provide support for reading
        /// in more data asynchronously before continuing with a new instance of the <see cref="Utf8JsonReader"/>.
        /// </remarks>
        public JsonReaderState(int maxDepth = StackFreeMaxDepth, JsonReaderOptions options = default)
        {
            if (maxDepth <= 0)
                throw ThrowHelper.GetArgumentException_MaxDepthMustBePositive();

            _stackFreeContainer = default;
            _lineNumber = default;
            _bytePositionInLine = default;
            _bytesConsumed = default;
            _currentDepth = default;
            _maxDepth = maxDepth;
            _inObject = default;
            _isNotPrimitive = default;
            _tokenType = default;
            _previousTokenType = default;
            _readerOptions = options;

            // Only allocate the stack if the user reads a JSON payload beyond the depth that the _stackFreeContainer can handle.
            // This way we avoid allocations in the common, default cases, and allocate lazily.
            _stack = null;
        }

        /// <summary>
        /// Gets the custom behavior when reading JSON using
        /// the <see cref="Utf8JsonReader"/> that may deviate from strict adherence
        /// to the JSON specification, which is the default behavior.
        /// </summary>
        public JsonReaderOptions Options => _readerOptions;

        /// <summary>
        /// Gets or sets the maximum depth allowed when reading JSON.
        /// Reading past this depth will throw a <exception cref="JsonReaderException"/>.
        /// </summary>
        public int MaxDepth => _maxDepth;
    }
}
