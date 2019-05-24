// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

#if !BUILDING_INBOX_LIBRARY
using System.Runtime.InteropServices;
#endif

namespace System.Text.Json
{
    /// <summary>
    /// Provides a high-performance API for forward-only, non-cached writing of UTF-8 encoded JSON text.
    /// </summary>
    /// <remarks>
    /// It writes the text sequentially with no caching and adheres to the JSON RFC
    /// by default (https://tools.ietf.org/html/rfc8259), with the exception of writing comments.
    /// </remarks>
    /// <remarks>
    /// When the user attempts to write invalid JSON and validation is enabled, it throws
    /// an <see cref="InvalidOperationException"/> with a context specific error message.
    /// </remarks>
    /// <remarks>
    /// To be able to format the output with indentation and whitespace OR to skip validation, create an instance of 
    /// <see cref="JsonWriterOptions"/> and pass that in to the writer.
    /// </remarks>
    public sealed partial class Utf8JsonWriter : IDisposable
#if BUILDING_INBOX_LIBRARY
        , IAsyncDisposable
#endif
    {
        // Depending on OS, either '\r\n' OR '\n'
        private static readonly int s_newLineLength = Environment.NewLine.Length;

        private const int DefaultGrowthSize = 4096;
        private const int InitialGrowthSize = 256;

        private IBufferWriter<byte> _output;
        private Stream _stream;
        private ArrayBufferWriter<byte> _arrayBufferWriter;

        private Memory<byte> _memory;

        private bool _inObject;
        private bool _isNotPrimitive;
        private JsonTokenType _tokenType;
        private BitStack _bitStack;

        // The highest order bit of _currentDepth is used to discern whether we are writing the first item in a list or not.
        // if (_currentDepth >> 31) == 1, add a list separator before writing the item
        // else, no list separator is needed since we are writing the first item.
        private int _currentDepth;

        /// <summary>
        /// Returns the amount of bytes written by the <see cref="Utf8JsonWriter"/> so far
        /// that have not yet been flushed to the output and committed.
        /// </summary>
        public int BytesPending { get; private set; }

        /// <summary>
        /// Returns the amount of bytes committed to the output by the <see cref="Utf8JsonWriter"/> so far.
        /// </summary>
        /// <remarks>
        /// In the case of IBufferwriter, this is how much the IBufferWriter has advanced.
        /// In the case of Stream, this is how much data has been written to the stream.
        /// </remarks>
        public long BytesCommitted { get; private set; }

        /// <summary>
        /// Gets the custom behavior when writing JSON using
        /// the <see cref="Utf8JsonWriter"/> which indicates whether to format the output
        /// while writing and whether to skip structural JSON validation or not.
        /// </summary>
        public JsonWriterOptions Options { get; }

        private int Indentation => CurrentDepth * JsonConstants.SpacesPerIndent;

        /// <summary>
        /// Tracks the recursive depth of the nested objects / arrays within the JSON text
        /// written so far. This provides the depth of the current token.
        /// </summary>
        public int CurrentDepth => _currentDepth & JsonConstants.RemoveFlagsBitMask;

        /// <summary>
        /// Constructs a new <see cref="Utf8JsonWriter"/> instance with a specified <paramref name="bufferWriter"/>.
        /// </summary>
        /// <param name="bufferWriter">An instance of <see cref="IBufferWriter{Byte}" /> used as a destination for writing JSON text into.</param>
        /// <param name="options">Defines the customized behavior of the <see cref="Utf8JsonWriter"/>
        /// By default, the <see cref="Utf8JsonWriter"/> writes JSON minimized (i.e. with no extra whitespace)
        /// and validates that the JSON being written is structurally valid according to JSON RFC.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the instance of <see cref="IBufferWriter{Byte}" /> that is passed in is null.
        /// </exception>
        public Utf8JsonWriter(IBufferWriter<byte> bufferWriter, JsonWriterOptions options = default)
        {
            _output = bufferWriter ?? throw new ArgumentNullException(nameof(bufferWriter));
            _stream = default;
            _arrayBufferWriter = default;

            BytesPending = default;
            BytesCommitted = default;
            _memory = default;

            _inObject = default;
            _isNotPrimitive = default;
            _tokenType = default;
            _currentDepth = default;
            Options = options;

            // Only allocate if the user writes a JSON payload beyond the depth that the _allocationFreeContainer can handle.
            // This way we avoid allocations in the common, default cases, and allocate lazily.
            _bitStack = default;
        }

        /// <summary>
        /// Constructs a new <see cref="Utf8JsonWriter"/> instance with a specified <paramref name="utf8Json"/>.
        /// </summary>
        /// <param name="utf8Json">An instance of <see cref="Stream" /> used as a destination for writing JSON text into.</param>
        /// <param name="options">Defines the customized behavior of the <see cref="Utf8JsonWriter"/>
        /// By default, the <see cref="Utf8JsonWriter"/> writes JSON minimized (i.e. with no extra whitespace)
        /// and validates that the JSON being written is structurally valid according to JSON RFC.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the instance of <see cref="Stream" /> that is passed in is null.
        /// </exception>
        public Utf8JsonWriter(Stream utf8Json, JsonWriterOptions options = default)
        {
            if (utf8Json == null)
                throw new ArgumentNullException(nameof(utf8Json));
            if (!utf8Json.CanWrite)
                throw new ArgumentException(SR.StreamNotWritable);

            _stream = utf8Json;
            _arrayBufferWriter = new ArrayBufferWriter<byte>();
            _output = default;

            BytesPending = default;
            BytesCommitted = default;
            _memory = default;

            _inObject = default;
            _isNotPrimitive = default;
            _tokenType = default;
            _currentDepth = default;
            Options = options;

            // Only allocate if the user writes a JSON payload beyond the depth that the _allocationFreeContainer can handle.
            // This way we avoid allocations in the common, default cases, and allocate lazily.
            _bitStack = default;
        }

        /// <summary>
        /// Resets the <see cref="Utf8JsonWriter"/> internal state so that it can be re-used.
        /// </summary>
        /// <remarks>
        /// The <see cref="Utf8JsonWriter"/> will continue to use the original writer options
        /// and the original output as the destination (either <see cref="IBufferWriter{Byte}" /> or <see cref="Stream" />).
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        ///   The instance of <see cref="Utf8JsonWriter"/> has been disposed.
        /// </exception>
        public void Reset()
        {
            CheckNotDisposed();

            _arrayBufferWriter?.Clear();
            ResetHelper();
        }

        /// <summary>
        /// Resets the <see cref="Utf8JsonWriter"/> internal state so that it can be re-used with the new instance of <see cref="Stream" />.
        /// </summary>
        /// <param name="utf8Json">An instance of <see cref="Stream" /> used as a destination for writing JSON text into.</param>
        /// <remarks>
        /// The <see cref="Utf8JsonWriter"/> will continue to use the original writer options
        /// but now write to the passed in <see cref="Stream" /> as the new destination.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the instance of <see cref="Stream" /> that is passed in is null.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The instance of <see cref="Utf8JsonWriter"/> has been disposed.
        /// </exception>
        public void Reset(Stream utf8Json)
        {
            CheckNotDisposed();

            if (utf8Json == null)
                throw new ArgumentNullException(nameof(utf8Json));
            if (!utf8Json.CanWrite)
                throw new ArgumentException(SR.StreamNotWritable);

            _stream = utf8Json;
            if (_arrayBufferWriter == null)
            {
                _arrayBufferWriter = new ArrayBufferWriter<byte>();
            }
            else
            {
                _arrayBufferWriter.Clear();
            }
            _output = null;

            ResetHelper();
        }

        /// <summary>
        /// Resets the <see cref="Utf8JsonWriter"/> internal state so that it can be re-used with the new instance of <see cref="IBufferWriter{Byte}" />.
        /// </summary>
        /// <param name="bufferWriter">An instance of <see cref="IBufferWriter{Byte}" /> used as a destination for writing JSON text into.</param>
        /// <remarks>
        /// The <see cref="Utf8JsonWriter"/> will continue to use the original writer options
        /// but now write to the passed in <see cref="IBufferWriter{Byte}" /> as the new destination.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the instance of <see cref="IBufferWriter{Byte}" /> that is passed in is null.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The instance of <see cref="Utf8JsonWriter"/> has been disposed.
        /// </exception>
        public void Reset(IBufferWriter<byte> bufferWriter)
        {
            CheckNotDisposed();

            _output = bufferWriter ?? throw new ArgumentNullException(nameof(bufferWriter));
            _stream = null;
            _arrayBufferWriter = null;

            ResetHelper();
        }

        private void ResetHelper()
        {
            BytesPending = default;
            BytesCommitted = default;
            _memory = default;

            _inObject = default;
            _isNotPrimitive = default;
            _tokenType = default;
            _currentDepth = default;

            // Only allocate if the user writes a JSON payload beyond the depth that the _allocationFreeContainer can handle.
            // This way we avoid allocations in the common, default cases, and allocate lazily.
            _bitStack = default;
        }

        private void CheckNotDisposed()
        {
            if (_stream == null)
            {
                // The conditions are ordered with stream first as that would be the most common mode
                if (_output == null)
                {
                    throw new ObjectDisposedException(nameof(Utf8JsonWriter));
                }
            }
        }

        /// <summary>
        /// Commits the JSON text written so far which makes it visible to the output destination.
        /// </summary>
        /// <remarks>
        /// In the case of IBufferWriter, this advances the underlying <see cref="IBufferWriter{Byte}" /> based on what has been written so far.
        /// In the case of Stream, this writes the data to the stream and flushes it.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        ///   The instance of <see cref="Utf8JsonWriter"/> has been disposed.
        /// </exception>
        public void Flush()
        {
            CheckNotDisposed();

            if (_stream != null)
            {
                Debug.Assert(_arrayBufferWriter != null);
                if (BytesPending != 0)
                {
                    _arrayBufferWriter.Advance(BytesPending);
                    Debug.Assert(BytesPending == _arrayBufferWriter.WrittenCount);
#if BUILDING_INBOX_LIBRARY
                    _stream.Write(_arrayBufferWriter.WrittenSpan);
#else
                    Debug.Assert(_arrayBufferWriter.WrittenMemory.Length == _arrayBufferWriter.WrittenCount);
                    bool result = MemoryMarshal.TryGetArray(_arrayBufferWriter.WrittenMemory, out ArraySegment<byte> underlyingBuffer);
                    Debug.Assert(underlyingBuffer.Offset == 0);
                    Debug.Assert(_arrayBufferWriter.WrittenCount == underlyingBuffer.Count);
                    _stream.Write(underlyingBuffer.Array, underlyingBuffer.Offset, underlyingBuffer.Count);
#endif
                    _arrayBufferWriter.Clear();
                }
                _stream.Flush();
            }
            else
            {
                Debug.Assert(_output != null);
                if (BytesPending != 0)
                {
                    _output.Advance(BytesPending);
                }
            }

            _memory = default;
            BytesCommitted += BytesPending;
            BytesPending = 0;
        }

        /// <summary>
        /// Commits any left over JSON text that has not yet been flushed and releases all resources used by the current instance.
        /// </summary>
        /// <remarks>
        /// In the case of IBufferWriter, this advances the underlying <see cref="IBufferWriter{Byte}" /> based on what has been written so far.
        /// In the case of Stream, this writes the data to the stream and flushes it.
        /// </remarks>
        /// <remarks>
        /// The <see cref="Utf8JsonWriter"/> instance cannot be re-used after disposing.
        /// </remarks>
        public void Dispose()
        {
            if (_stream == null)
            {
                // The conditions are ordered with stream first as that would be the most common mode
                if (_output == null)
                {
                    return;
                }
            }

            Flush();
            ResetHelper();

            _stream = null;
            _arrayBufferWriter = null;
            _output = null;
        }

#if BUILDING_INBOX_LIBRARY
        /// <summary>
        /// Asynchronously commits any left over JSON text that has not yet been flushed and releases all resources used by the current instance.
        /// </summary>
        /// <remarks>
        /// In the case of IBufferWriter, this advances the underlying <see cref="IBufferWriter{Byte}" /> based on what has been written so far.
        /// In the case of Stream, this writes the data to the stream and flushes it.
        /// </remarks>
        /// <remarks>
        /// The <see cref="Utf8JsonWriter"/> instance cannot be re-used after disposing.
        /// </remarks>
        public async ValueTask DisposeAsync()
        {
            if (_stream == null)
            {
                // The conditions are ordered with stream first as that would be the most common mode
                if (_output == null)
                {
                    return;
                }
            }

            await FlushAsync().ConfigureAwait(false);
            ResetHelper();

            _stream = null;
            _arrayBufferWriter = null;
            _output = null;
        }
#endif

        /// <summary>
        /// Asynchronously commits the JSON text written so far which makes it visible to the output destination.
        /// </summary>
        /// <remarks>
        /// In the case of IBufferWriter, this advances the underlying <see cref="IBufferWriter{Byte}" /> based on what has been written so far.
        /// In the case of Stream, this writes the data to the stream and flushes it asynchronously, while monitoring cancellation requests.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        ///   The instance of <see cref="Utf8JsonWriter"/> has been disposed.
        /// </exception>
        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            CheckNotDisposed();

            if (_stream != null)
            {
                Debug.Assert(_arrayBufferWriter != null);
                if (BytesPending != 0)
                {
                    _arrayBufferWriter.Advance(BytesPending);
                    Debug.Assert(BytesPending == _arrayBufferWriter.WrittenCount);
#if BUILDING_INBOX_LIBRARY
                    await _stream.WriteAsync(_arrayBufferWriter.WrittenMemory, cancellationToken).ConfigureAwait(false);
#else
                    Debug.Assert(_arrayBufferWriter.WrittenMemory.Length == _arrayBufferWriter.WrittenCount);
                    bool result = MemoryMarshal.TryGetArray(_arrayBufferWriter.WrittenMemory, out ArraySegment<byte> underlyingBuffer);
                    Debug.Assert(underlyingBuffer.Offset == 0);
                    Debug.Assert(_arrayBufferWriter.WrittenCount == underlyingBuffer.Count);
                    await _stream.WriteAsync(underlyingBuffer.Array, underlyingBuffer.Offset, underlyingBuffer.Count, cancellationToken).ConfigureAwait(false);
#endif
                    _arrayBufferWriter.Clear();
                }
                await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                Debug.Assert(_output != null);
                if (BytesPending != 0)
                {
                    _output.Advance(BytesPending);
                }
            }

            _memory = default;
            BytesCommitted += BytesPending;
            BytesPending = 0;
        }

        /// <summary>
        /// Writes the beginning of a JSON array.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the depth of the JSON has exceeded the maximum depth of 1000 
        /// OR if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteStartArray()
        {
            WriteStart(JsonConstants.OpenBracket);
            _tokenType = JsonTokenType.StartArray;
        }

        /// <summary>
        /// Writes the beginning of a JSON object.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the depth of the JSON has exceeded the maximum depth of 1000 
        /// OR if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteStartObject()
        {
            WriteStart(JsonConstants.OpenBrace);
            _tokenType = JsonTokenType.StartObject;
        }

        private void WriteStart(byte token)
        {
            if (CurrentDepth >= JsonConstants.MaxWriterDepth)
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.DepthTooLarge, _currentDepth, token: default, tokenType: default);

            if (Options.IndentedOrNotSkipValidation)
            {
                WriteStartSlow(token);
            }
            else
            {
                WriteStartMinimized(token);
            }

            _currentDepth &= JsonConstants.RemoveFlagsBitMask;
            _currentDepth++;
            _isNotPrimitive = true;
        }

        private void WriteStartMinimized(byte token)
        {
            if (_memory.Length - BytesPending < 2)  // 1 start token, and optionally, 1 list separator
            {
                Grow(2);
            }

            Span<byte> output = _memory.Span;
            if (_currentDepth < 0)
            {
                output[BytesPending++] = JsonConstants.ListSeparator;
            }
            output[BytesPending++] = token;
        }

        private void WriteStartSlow(byte token)
        {
            Debug.Assert(Options.Indented || !Options.SkipValidation);

            if (Options.Indented)
            {
                if (!Options.SkipValidation)
                {
                    ValidateStart();
                    UpdateBitStackOnStart(token);
                }
                WriteStartIndented(token);
            }
            else
            {
                Debug.Assert(!Options.SkipValidation);
                ValidateStart();
                UpdateBitStackOnStart(token);
                WriteStartMinimized(token);
            }
        }

        private void ValidateStart()
        {
            if (_inObject)
            {
                Debug.Assert(_tokenType != JsonTokenType.None && _tokenType != JsonTokenType.StartArray);
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.CannotStartObjectArrayWithoutProperty, currentDepth: default, token: default, _tokenType);
            }
            else
            {
                Debug.Assert(_tokenType != JsonTokenType.StartObject);
                if (_tokenType != JsonTokenType.None && (!_isNotPrimitive || CurrentDepth == 0))
                {
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.CannotStartObjectArrayAfterPrimitiveOrClose, currentDepth: default, token: default, _tokenType);
                }
            }
        }

        private void WriteStartIndented(byte token)
        {
            int indent = Indentation;
            Debug.Assert(indent <= 2 * JsonConstants.MaxWriterDepth);

            int minRequired = indent + 1;   // 1 start token
            int maxRequired = minRequired + 3; // Optionally, 1 list separator and 1-2 bytes for new line

            if (_memory.Length - BytesPending < maxRequired)
            {
                Grow(maxRequired);
            }

            Span<byte> output = _memory.Span;

            if (_currentDepth < 0)
            {
                output[BytesPending++] = JsonConstants.ListSeparator;
            }

            if (_tokenType != JsonTokenType.None)
            {
                WriteNewLine(output);
            }

            JsonWriterHelper.WriteIndentation(output.Slice(BytesPending), indent);
            BytesPending += indent;

            output[BytesPending++] = token;
        }

        /// <summary>
        /// Writes the beginning of a JSON array with a pre-encoded property name as the key.
        /// </summary>
        /// <param name="propertyName">The JSON encoded property name of the JSON array to be transcoded and written as UTF-8.</param>
        /// <remarks>
        /// The property name should already be escaped when the instance of <see cref="JsonEncodedText"/> was created.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the depth of the JSON has exceeded the maximum depth of 1000 
        /// OR if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteStartArray(JsonEncodedText propertyName)
        {
            WriteStartHelper(propertyName.EncodedUtf8Bytes, JsonConstants.OpenBracket);
            _tokenType = JsonTokenType.StartArray;
        }

        /// <summary>
        /// Writes the beginning of a JSON object with a pre-encoded property name as the key.
        /// </summary>
        /// <param name="propertyName">The JSON encoded property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <remarks>
        /// The property name should already be escaped when the instance of <see cref="JsonEncodedText"/> was created.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the depth of the JSON has exceeded the maximum depth of 1000 
        /// OR if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteStartObject(JsonEncodedText propertyName)
        {
            WriteStartHelper(propertyName.EncodedUtf8Bytes, JsonConstants.OpenBrace);
            _tokenType = JsonTokenType.StartObject;
        }

        private void WriteStartHelper(ReadOnlySpan<byte> utf8PropertyName, byte token)
        {
            Debug.Assert(utf8PropertyName.Length <= JsonConstants.MaxTokenSize);

            ValidateDepth();

            WriteStartByOptions(utf8PropertyName, token);

            _currentDepth &= JsonConstants.RemoveFlagsBitMask;
            _currentDepth++;
            _isNotPrimitive = true;
        }

        /// <summary>
        /// Writes the beginning of a JSON array with a property name as the key.
        /// </summary>
        /// <param name="utf8PropertyName">The UTF-8 encoded property name of the JSON array to be written.</param>
        /// <remarks>
        /// The property name is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the depth of the JSON has exceeded the maximum depth of 1000 
        /// OR if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteStartArray(ReadOnlySpan<byte> utf8PropertyName)
        {
            ValidatePropertyNameAndDepth(utf8PropertyName);

            WriteStartEscape(utf8PropertyName, JsonConstants.OpenBracket);

            _currentDepth &= JsonConstants.RemoveFlagsBitMask;
            _currentDepth++;
            _isNotPrimitive = true;
            _tokenType = JsonTokenType.StartArray;
        }

        /// <summary>
        /// Writes the beginning of a JSON object with a property name as the key.
        /// </summary>
        /// <param name="utf8PropertyName">The UTF-8 encoded property name of the JSON object to be written.</param>
        /// <remarks>
        /// The property name is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the depth of the JSON has exceeded the maximum depth of 1000 
        /// OR if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteStartObject(ReadOnlySpan<byte> utf8PropertyName)
        {
            ValidatePropertyNameAndDepth(utf8PropertyName);

            WriteStartEscape(utf8PropertyName, JsonConstants.OpenBrace);

            _currentDepth &= JsonConstants.RemoveFlagsBitMask;
            _currentDepth++;
            _isNotPrimitive = true;
            _tokenType = JsonTokenType.StartObject;
        }

        private void WriteStartEscape(ReadOnlySpan<byte> utf8PropertyName, byte token)
        {
            int propertyIdx = JsonWriterHelper.NeedsEscaping(utf8PropertyName);

            Debug.Assert(propertyIdx >= -1 && propertyIdx < utf8PropertyName.Length);

            if (propertyIdx != -1)
            {
                WriteStartEscapeProperty(utf8PropertyName, token, propertyIdx);
            }
            else
            {
                WriteStartByOptions(utf8PropertyName, token);
            }
        }

        private void WriteStartByOptions(ReadOnlySpan<byte> utf8PropertyName, byte token)
        {
            ValidateWritingProperty(token);

            if (Options.Indented)
            {
                WritePropertyNameIndented(utf8PropertyName, token);
            }
            else
            {
                WritePropertyNameMinimized(utf8PropertyName, token);
            }
        }

        private void WriteStartEscapeProperty(ReadOnlySpan<byte> utf8PropertyName, byte token, int firstEscapeIndexProp)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8PropertyName.Length);
            Debug.Assert(firstEscapeIndexProp >= 0 && firstEscapeIndexProp < utf8PropertyName.Length);

            byte[] propertyArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndexProp);

            Span<byte> escapedPropertyName = length <= JsonConstants.StackallocThreshold ?
                stackalloc byte[length] :
                (propertyArray = ArrayPool<byte>.Shared.Rent(length));

            JsonWriterHelper.EscapeString(utf8PropertyName, escapedPropertyName, firstEscapeIndexProp, out int written);

            WriteStartByOptions(escapedPropertyName.Slice(0, written), token);

            if (propertyArray != null)
            {
                ArrayPool<byte>.Shared.Return(propertyArray);
            }
        }

        /// <summary>
        /// Writes the beginning of a JSON array with a property name as the key.
        /// </summary>
        /// <param name="propertyName">The property name of the JSON array to be transcoded and written as UTF-8.</param>
        /// <remarks>
        /// The property name is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the depth of the JSON has exceeded the maximum depth of 1000 
        /// OR if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteStartArray(string propertyName)
            => WriteStartArray(propertyName.AsSpan());

        /// <summary>
        /// Writes the beginning of a JSON object with a property name as the key.
        /// </summary>
        /// <param name="propertyName">The property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <remarks>
        /// The property name is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the depth of the JSON has exceeded the maximum depth of 1000 
        /// OR if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteStartObject(string propertyName)
            => WriteStartObject(propertyName.AsSpan());

        /// <summary>
        /// Writes the beginning of a JSON array with a property name as the key.
        /// </summary>
        /// <param name="propertyName">The property name of the JSON array to be transcoded and written as UTF-8.</param>
        /// <remarks>
        /// The property name is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the depth of the JSON has exceeded the maximum depth of 1000 
        /// OR if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteStartArray(ReadOnlySpan<char> propertyName)
        {
            ValidatePropertyNameAndDepth(propertyName);

            WriteStartEscape(propertyName, JsonConstants.OpenBracket);

            _currentDepth &= JsonConstants.RemoveFlagsBitMask;
            _currentDepth++;
            _isNotPrimitive = true;
            _tokenType = JsonTokenType.StartArray;
        }

        /// <summary>
        /// Writes the beginning of a JSON object with a property name as the key.
        /// </summary>
        /// <param name="propertyName">The property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <remarks>
        /// The property name is escaped before writing.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the depth of the JSON has exceeded the maximum depth of 1000 
        /// OR if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteStartObject(ReadOnlySpan<char> propertyName)
        {
            ValidatePropertyNameAndDepth(propertyName);

            WriteStartEscape(propertyName, JsonConstants.OpenBrace);

            _currentDepth &= JsonConstants.RemoveFlagsBitMask;
            _currentDepth++;
            _isNotPrimitive = true;
            _tokenType = JsonTokenType.StartObject;
        }

        private void WriteStartEscape(ReadOnlySpan<char> propertyName, byte token)
        {
            int propertyIdx = JsonWriterHelper.NeedsEscaping(propertyName);

            Debug.Assert(propertyIdx >= -1 && propertyIdx < propertyName.Length);

            if (propertyIdx != -1)
            {
                WriteStartEscapeProperty(propertyName, token, propertyIdx);
            }
            else
            {
                WriteStartByOptions(propertyName, token);
            }
        }

        private void WriteStartByOptions(ReadOnlySpan<char> propertyName, byte token)
        {
            ValidateWritingProperty(token);

            if (Options.Indented)
            {
                WritePropertyNameIndented(propertyName, token);
            }
            else
            {
                WritePropertyNameMinimized(propertyName, token);
            }
        }

        private void WriteStartEscapeProperty(ReadOnlySpan<char> propertyName, byte token, int firstEscapeIndexProp)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= propertyName.Length);
            Debug.Assert(firstEscapeIndexProp >= 0 && firstEscapeIndexProp < propertyName.Length);

            char[] propertyArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(propertyName.Length, firstEscapeIndexProp);

            Span<char> escapedPropertyName = length <= JsonConstants.StackallocThreshold ?
                stackalloc char[length] :
                (propertyArray = ArrayPool<char>.Shared.Rent(length));

            JsonWriterHelper.EscapeString(propertyName, escapedPropertyName, firstEscapeIndexProp, out int written);

            WriteStartByOptions(escapedPropertyName.Slice(0, written), token);

            if (propertyArray != null)
            {
                ArrayPool<char>.Shared.Return(propertyArray);
            }
        }

        /// <summary>
        /// Writes the end of a JSON array.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteEndArray()
        {
            WriteEnd(JsonConstants.CloseBracket);
            _tokenType = JsonTokenType.EndArray;
        }

        /// <summary>
        /// Writes the end of a JSON object.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteEndObject()
        {
            WriteEnd(JsonConstants.CloseBrace);
            _tokenType = JsonTokenType.EndObject;
        }

        private void WriteEnd(byte token)
        {
            if (Options.IndentedOrNotSkipValidation)
            {
                WriteEndSlow(token);
            }
            else
            {
                WriteEndMinimized(token);
            }

            SetFlagToAddListSeparatorBeforeNextItem();
            // Necessary if WriteEndX is called without a corresponding WriteStartX first.
            if (CurrentDepth != 0)
            {
                _currentDepth--;
            }
        }

        private void WriteEndMinimized(byte token)
        {
            if (_memory.Length - BytesPending < 1) // 1 end token
            {
                Grow(1);
            }

            Span<byte> output = _memory.Span;
            output[BytesPending++] = token;
        }

        private void WriteEndSlow(byte token)
        {
            Debug.Assert(Options.Indented || !Options.SkipValidation);

            if (Options.Indented)
            {
                if (!Options.SkipValidation)
                {
                    ValidateEnd(token);
                }
                WriteEndIndented(token);
            }
            else
            {
                Debug.Assert(!Options.SkipValidation);
                ValidateEnd(token);
                WriteEndMinimized(token);
            }
        }

        private void ValidateEnd(byte token)
        {
            if (_bitStack.CurrentDepth <= 0)
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.MismatchedObjectArray, currentDepth: default, token, tokenType: default);

            if (token == JsonConstants.CloseBracket)
            {
                if (_inObject)
                {
                    Debug.Assert(_tokenType != JsonTokenType.None);
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.MismatchedObjectArray, currentDepth: default, token, tokenType: default);
                }
            }
            else
            {
                Debug.Assert(token == JsonConstants.CloseBrace);

                if (!_inObject)
                {
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.MismatchedObjectArray, currentDepth: default, token, tokenType: default);
                }
            }

            _inObject = _bitStack.Pop();
        }

        private void WriteEndIndented(byte token)
        {
            // Do not format/indent empty JSON object/array.
            if (_tokenType == JsonTokenType.StartObject || _tokenType == JsonTokenType.StartArray)
            {
                WriteEndMinimized(token);
            }
            else
            {
                int indent = Indentation;

                // Necessary if WriteEndX is called without a corresponding WriteStartX first.
                if (indent != 0)
                {
                    // The end token should be at an outer indent and since we haven't updated
                    // current depth yet, explicitly subtract here.
                    indent -= JsonConstants.SpacesPerIndent;
                }

                Debug.Assert(indent <= 2 * JsonConstants.MaxWriterDepth);
                Debug.Assert(Options.SkipValidation || _tokenType != JsonTokenType.None);

                int maxRequired = indent + 3; // 1 end token, 1-2 bytes for new line

                if (_memory.Length - BytesPending < maxRequired)
                {
                    Grow(maxRequired);
                }

                Span<byte> output = _memory.Span;

                WriteNewLine(output);

                JsonWriterHelper.WriteIndentation(output.Slice(BytesPending), indent);
                BytesPending += indent;

                output[BytesPending++] = token;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteNewLine(Span<byte> output)
        {
            // Write '\r\n' OR '\n', depending on OS
            if (s_newLineLength == 2)
            {
                output[BytesPending++] = JsonConstants.CarriageReturn;
            }
            output[BytesPending++] = JsonConstants.LineFeed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateBitStackOnStart(byte token)
        {
            if (token == JsonConstants.OpenBracket)
            {
                _bitStack.PushFalse();
                _inObject = false;
            }
            else
            {
                Debug.Assert(token == JsonConstants.OpenBrace);
                _bitStack.PushTrue();
                _inObject = true;
            }
        }

        private void Grow(int requiredSize)
        {
            Debug.Assert(requiredSize > 0);

            if (_memory.Length == 0)
            {
                FirstCallToGetMemory(requiredSize);
                return;
            }

            int sizeHint = Math.Max(DefaultGrowthSize, requiredSize);

            Debug.Assert(BytesPending != 0);

            if (_stream != null)
            {
                Debug.Assert(_arrayBufferWriter != null);

                _arrayBufferWriter.Advance(BytesPending);

                Debug.Assert(BytesPending == _arrayBufferWriter.WrittenCount);

#if BUILDING_INBOX_LIBRARY
                _stream.Write(_arrayBufferWriter.WrittenSpan);
#else
                Debug.Assert(_arrayBufferWriter.WrittenMemory.Length == _arrayBufferWriter.WrittenCount);
                bool result = MemoryMarshal.TryGetArray(_arrayBufferWriter.WrittenMemory, out ArraySegment<byte> underlyingBuffer);
                Debug.Assert(underlyingBuffer.Offset == 0);
                Debug.Assert(_arrayBufferWriter.WrittenCount == underlyingBuffer.Count);
                _stream.Write(underlyingBuffer.Array, underlyingBuffer.Offset, underlyingBuffer.Count);
#endif
                _arrayBufferWriter.Clear();

                _memory = _arrayBufferWriter.GetMemory(sizeHint);

                Debug.Assert(_memory.Length >= sizeHint);
            }
            else
            {
                Debug.Assert(_output != null);

                _output.Advance(BytesPending);

                _memory = _output.GetMemory(sizeHint);

                if (_memory.Length < sizeHint)
                {
                    ThrowHelper.ThrowInvalidOperationException_NeedLargerSpan();
                }
            }

            BytesCommitted += BytesPending;
            BytesPending = 0;
        }

        private void FirstCallToGetMemory(int requiredSize)
        {
            Debug.Assert(_memory.Length == 0);
            Debug.Assert(BytesPending == 0);

            int sizeHint = Math.Max(InitialGrowthSize, requiredSize);

            if (_stream != null)
            {
                Debug.Assert(_arrayBufferWriter != null);
                Debug.Assert(BytesPending == _arrayBufferWriter.WrittenCount);
                _memory = _arrayBufferWriter.GetMemory(sizeHint);
                Debug.Assert(_memory.Length >= sizeHint);
            }
            else
            {
                Debug.Assert(_output != null);
                _memory = _output.GetMemory(sizeHint);

                if (_memory.Length < sizeHint)
                {
                    ThrowHelper.ThrowInvalidOperationException_NeedLargerSpan();
                }
            }
        }

        private void SetFlagToAddListSeparatorBeforeNextItem()
        {
            _currentDepth |= 1 << 31;
        }
    }
}
