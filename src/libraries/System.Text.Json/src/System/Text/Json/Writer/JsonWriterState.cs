// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json
{
    /// <summary>
    /// Defines an opaque type that holds and saves all the relevant state information which must be provided
    /// to the <see cref="Utf8JsonWriter"/> to continue writing after completing a partial write.
    /// </summary>
    /// <remarks>
    /// This type is required to support reentrancy when writing incomplete data, and to continue
    /// writing in chunks. Unlike the <see cref="Utf8JsonWriter"/>, which is a ref struct,
    /// this type can survive across async/await boundaries and hence this type is required to provide
    /// support for writing more JSON text asynchronously before continuing with a new instance of the <see cref="Utf8JsonWriter"/>.
    /// </remarks>
    public struct JsonWriterState
    {
        internal long _bytesWritten;
        internal long _bytesCommitted;
        internal bool _inObject;
        internal bool _isNotPrimitive;
        internal JsonTokenType _tokenType;
        internal int _currentDepth;
        internal JsonWriterOptions _writerOptions;
        internal BitStack _bitStack;

        /// <summary>
        /// Returns the total amount of bytes written by the <see cref="Utf8JsonWriter"/> so far.
        /// This includes data that has been written beyond what has already been committed.
        /// </summary>
        public long BytesWritten => _bytesWritten;

        /// <summary>
        /// Returns the total amount of bytes committed to the output by the <see cref="Utf8JsonWriter"/> so far.
        /// This is how much the IBufferWriter has advanced.
        /// </summary>
        public long BytesCommitted => _bytesCommitted;

        /// <summary>
        /// Constructs a new <see cref="JsonWriterState"/> instance.
        /// </summary>
        /// <param name="options">Defines the customized behavior of the <see cref="Utf8JsonWriter"/>
        /// By default, the <see cref="Utf8JsonWriter"/> writes JSON minimized (i.e. with no extra whitespace)
        /// and validates that the JSON being written is structurally valid according to JSON RFC.</param>
        /// <remarks>
        /// An instance of this state must be passed to the <see cref="Utf8JsonWriter"/> ctor with the output destination.
        /// Unlike the <see cref="Utf8JsonWriter"/>, which is a ref struct, the state can survive
        /// across async/await boundaries and hence this type is required to provide support for reading
        /// in more data asynchronously before continuing with a new instance of the <see cref="Utf8JsonWriter"/>.
        /// </remarks>
        public JsonWriterState(JsonWriterOptions options = default)
        {
            _bytesWritten = default;
            _bytesCommitted = default;
            _inObject = default;
            _isNotPrimitive = default;
            _tokenType = default;
            _currentDepth = default;
            _writerOptions = options;

            // Only allocate if the user writes a JSON payload beyond the depth that the _allocationFreeContainer can handle.
            // This way we avoid allocations in the common, default cases, and allocate lazily.
            _bitStack = default;
        }

        /// <summary>
        /// Gets the custom behavior when writing JSON using
        /// the <see cref="Utf8JsonWriter"/> which indicates whether to format the output
        /// while writing and whether to skip structural JSON validation or not.
        /// </summary>
        public JsonWriterOptions Options => _writerOptions;
    }
}
