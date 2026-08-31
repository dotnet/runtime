// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    public readonly struct JsonReaderState
    {
        internal readonly long _lineNumber;
        internal readonly long _bytePositionInLine;
        internal readonly bool _inObject;
        internal readonly bool _isNotPrimitive;
        internal readonly bool _valueIsEscaped;
        internal readonly bool _trailingCommaBeforeComment;
        internal readonly JsonTokenType _tokenType;
        internal readonly JsonTokenType _previousTokenType;
        internal readonly JsonReaderOptions _readerOptions;
        internal readonly BitStack _bitStack;

        /// <summary>
        /// Constructs a new <see cref="JsonReaderState"/> instance.
        /// </summary>
        /// <param name="options">Defines the customized behavior of the <see cref="Utf8JsonReader"/>
        /// that is different from the JSON RFC (for example how to handle comments or maximum depth allowed when reading).
        /// By default, the <see cref="Utf8JsonReader"/> follows the JSON RFC strictly (i.e. comments within the JSON are invalid) and reads up to a maximum depth of 64.</param>
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
            _inObject = default;
            _isNotPrimitive = default;
            _valueIsEscaped = default;
            _trailingCommaBeforeComment = default;
            _tokenType = default;
            _previousTokenType = default;
            _readerOptions = options;

            // Only allocate if the user reads a JSON payload beyond the depth that the _allocationFreeContainer can handle.
            // This way we avoid allocations in the common, default cases, and allocate lazily.
            _bitStack = default;
        }

        internal JsonReaderState(
            long lineNumber,
            long bytePositionInLine,
            bool inObject,
            bool isNotPrimitive,
            bool valueIsEscaped,
            bool trailingCommaBeforeComment,
            JsonTokenType tokenType,
            JsonTokenType previousTokenType,
            JsonReaderOptions readerOptions,
            BitStack bitStack)
        {
            _lineNumber = lineNumber;
            _bytePositionInLine = bytePositionInLine;
            _inObject = inObject;
            _isNotPrimitive = isNotPrimitive;
            _valueIsEscaped = valueIsEscaped;
            _trailingCommaBeforeComment = trailingCommaBeforeComment;
            _tokenType = tokenType;
            _previousTokenType = previousTokenType;
            _readerOptions = readerOptions;
            _bitStack = bitStack;
        }

        /// <summary>
        /// Gets the custom behavior when reading JSON using
        /// the <see cref="Utf8JsonReader"/> that may deviate from strict adherence
        /// to the JSON specification, which is the default behavior.
        /// </summary>
        public JsonReaderOptions Options => _readerOptions;
    }
}
