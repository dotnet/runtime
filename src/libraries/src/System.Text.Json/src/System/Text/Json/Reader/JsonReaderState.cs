// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        internal long _lineNumber;
        internal long _bytePositionInLine;
        internal long _bytesConsumed;
        internal bool _inObject;
        internal bool _isNotPrimitive;
        internal char _numberFormat;
        internal bool _stringHasEscaping;
        internal bool _trailingCommaBeforeComment;
        internal JsonTokenType _tokenType;
        internal JsonTokenType _previousTokenType;
        internal JsonReaderOptions _readerOptions;
        internal BitStack _bitStack;
        internal SequencePosition _sequencePosition;

        /// <summary>
        /// Returns the total amount of bytes consumed by the <see cref="Utf8JsonReader"/> so far
        /// for the given UTF-8 encoded input text.
        /// </summary>
        public long BytesConsumed => _bytesConsumed;

        /// <summary>
        /// Returns the current <see cref="SequencePosition"/> within the provided UTF-8 encoded
        /// input ReadOnlySequence&lt;byte&gt;. If the <see cref="Utf8JsonReader"/> was constructed
        /// with a ReadOnlySpan&lt;byte&gt; instead, this will always return a default <see cref="SequencePosition"/>.
        /// </summary>
        public SequencePosition Position => _sequencePosition;

        /// <summary>
        /// Constructs a new <see cref="JsonReaderState"/> instance.
        /// </summary>
        /// <param name="options">Defines the customized behavior of the <see cref="Utf8JsonReader"/>
        /// that is different from the JSON RFC (for example how to handle comments or maximum depth allowed when reading).
        /// By default, the <see cref="Utf8JsonReader"/> follows the JSON RFC strictly (i.e. comments within the JSON are invalid) and reads up to a maximum depth of 64.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the max depth is set to a non-positive value (&lt; 0)
        /// </exception>
        /// <remarks>
        /// An instance of this state must be passed to the <see cref="Utf8JsonReader"/> ctor with the JSON data.
        /// Unlike the <see cref="Utf8JsonReader"/>, which is a ref struct, the state can survive
        /// across async/await boundaries and hence this type is required to provide support for reading
        /// in more data asynchronously before continuing with a new instance of the <see cref="Utf8JsonReader"/>.
        /// </remarks>
        public JsonReaderState(JsonReaderOptions options = default)
        {
            _lineNumber = default;
            _bytePositionInLine = default;
            _bytesConsumed = default;
            _inObject = default;
            _isNotPrimitive = default;
            _numberFormat = default;
            _stringHasEscaping = default;
            _trailingCommaBeforeComment = default;
            _tokenType = default;
            _previousTokenType = default;
            _readerOptions = options;

            // Only allocate if the user reads a JSON payload beyond the depth that the _allocationFreeContainer can handle.
            // This way we avoid allocations in the common, default cases, and allocate lazily.
            _bitStack = default;

            _sequencePosition = default;
        }

        /// <summary>
        /// Gets the custom behavior when reading JSON using
        /// the <see cref="Utf8JsonReader"/> that may deviate from strict adherence
        /// to the JSON specification, which is the default behavior.
        /// </summary>
        public JsonReaderOptions Options => _readerOptions;
    }
}
