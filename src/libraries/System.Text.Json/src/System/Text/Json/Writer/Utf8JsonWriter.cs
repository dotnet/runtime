// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    /// <summary>
    /// Provides a high-performance API for forward-only, non-cached writing of UTF-8 encoded JSON text.
    /// It writes the text sequentially with no caching and adheres to the JSON RFC
    /// by default (https://tools.ietf.org/html/rfc8259), with the exception of writing comments.
    /// </summary>
    /// <remarks>
    /// When the user attempts to write invalid JSON and validation is enabled, it throws
    /// a <see cref="InvalidOperationException"/> with a context specific error message.
    /// Since this type is a ref struct, it does not directly support async. However, it does provide
    /// support for reentrancy to write partial data, and continue writing in chunks.
    /// To be able to format the output with indentation and whitespace OR to skip validation, create an instance of 
    /// <see cref="JsonWriterState"/> and pass that in to the writer.
    /// </remarks>
    public ref partial struct Utf8JsonWriter
    {
        private const int StackallocThreshold = 256;
        private const int DefaultGrowthSize = 4096;

        private readonly IBufferWriter<byte> _output;
        private int _buffered;
        private Span<byte> _buffer;

        /// <summary>
        /// Returns the total amount of bytes written by the <see cref="Utf8JsonWriter"/> so far
        /// for the current instance of the <see cref="Utf8JsonWriter"/>.
        /// This includes data that has been written beyond what has already been committed.
        /// </summary>
        public long BytesWritten
        {
            get
            {
                Debug.Assert(BytesCommitted <= long.MaxValue - _buffered);
                return BytesCommitted + _buffered;
            }
        }

        /// <summary>
        /// Returns the total amount of bytes committed to the output by the <see cref="Utf8JsonWriter"/> so far
        /// for the current instance of the <see cref="Utf8JsonWriter"/>.
        /// This is how much the IBufferWriter has advanced.
        /// </summary>
        public long BytesCommitted { get; private set; }

        private bool _inObject;
        private bool _isNotPrimitive;
        private JsonTokenType _tokenType;
        private readonly JsonWriterOptions _writerOptions;
        private BitStack _bitStack;

        // The highest order bit of _currentDepth is used to discern whether we are writing the first item in a list or not.
        // if (_currentDepth >> 31) == 1, add a list separator before writing the item
        // else, no list separator is needed since we are writing the first item.
        private int _currentDepth;

        private int Indentation => CurrentDepth * JsonConstants.SpacesPerIndent;

        /// <summary>
        /// Tracks the recursive depth of the nested objects / arrays within the JSON text
        /// written so far. This provides the depth of the current token.
        /// </summary>
        public int CurrentDepth => _currentDepth & JsonConstants.RemoveFlagsBitMask;

        /// <summary>
        /// Returns the current snapshot of the <see cref="Utf8JsonWriter"/> state which must
        /// be captured by the caller and passed back in to the <see cref="Utf8JsonWriter"/> ctor with more data.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when there is JSON data that has been written and buffered but not yet flushed to the <see cref="IBufferWriter{Byte}" />.	
        /// Getting the state for creating a new <see cref="Utf8JsonWriter"/> without first committing the data that has been written	
        /// would result in an inconsistent state. Call Flush before getting the current state.	
        /// </exception>
        /// <remarks>
        /// Unlike the <see cref="Utf8JsonWriter"/>, which is a ref struct, the state can survive
        /// across async/await boundaries and hence this type is required to provide support for reading
        /// in more data asynchronously before continuing with a new instance of the <see cref="Utf8JsonWriter"/>.
        /// </remarks>
        public JsonWriterState GetCurrentState()
        {
            if (_buffered != 0)
            {
                throw ThrowHelper.GetInvalidOperationException_CallFlushFirst(_buffered);
            }
            return new JsonWriterState
            {
                _bytesWritten = BytesWritten,
                _bytesCommitted = BytesCommitted,
                _inObject = _inObject,
                _isNotPrimitive = _isNotPrimitive,
                _tokenType = _tokenType,
                _currentDepth = _currentDepth,
                _writerOptions = _writerOptions,
                _bitStack = _bitStack,
            };
        }

        /// <summary>
        /// Constructs a new <see cref="Utf8JsonWriter"/> instance with a specified <paramref name="bufferWriter"/>.
        /// </summary>
        /// <param name="bufferWriter">An instance of <see cref="IBufferWriter{Byte}" /> used as a destination for writing JSON text into.</param>
        /// <param name="state">If this is the first call to the ctor, pass in a default state. Otherwise,
        /// capture the state from the previous instance of the <see cref="Utf8JsonWriter"/> and pass that back.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the instance of <see cref="IBufferWriter{Byte}" /> that is passed in is null.
        /// </exception>
        /// <remarks>
        /// Since this type is a ref struct, it is a stack-only type and all the limitations of ref structs apply to it.
        /// This is the reason why the ctor accepts a <see cref="JsonWriterState"/>.
        /// </remarks>
        public Utf8JsonWriter(IBufferWriter<byte> bufferWriter, JsonWriterState state = default)
        {
            _output = bufferWriter ?? throw new ArgumentNullException(nameof(bufferWriter));
            _buffered = 0;
            BytesCommitted = 0;
            _buffer = _output.GetSpan();

            _inObject = state._inObject;
            _isNotPrimitive = state._isNotPrimitive;
            _tokenType = state._tokenType;
            _writerOptions = state._writerOptions;
            _bitStack = state._bitStack;

            _currentDepth = state._currentDepth;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Advance(int count)
        {
            Debug.Assert(count >= 0 && _buffered <= int.MaxValue - count);

            _buffered += count;
            _buffer = _buffer.Slice(count);
        }

        /// <summary>
        /// Advances the underlying <see cref="IBufferWriter{Byte}" /> based on what has been written so far.
        /// </summary>
        /// <param name="isFinalBlock">Let's the writer know whether more data will be written. This is used to validate
        /// that the JSON written so far is structurally valid if no more data is to follow.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when incomplete JSON has been written and <paramref name="isFinalBlock"/> is true.
        /// (for example when an open object or array needs to be closed).
        /// </exception>
        public void Flush(bool isFinalBlock = true)
        {
            if (isFinalBlock && !_writerOptions.SkipValidation && (CurrentDepth != 0 || _tokenType == JsonTokenType.None))
                ThrowHelper.ThrowInvalidOperationException_DepthNonZeroOrEmptyJson(_currentDepth);

            Flush();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Flush()
        {
            _output.Advance(_buffered);
            BytesCommitted += _buffered;
            _buffered = 0;
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
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.DepthTooLarge, _currentDepth);

            if (_writerOptions.IndentedOrNotSkipValidation)
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
            int idx = 0;
            if (_currentDepth < 0)
            {
                if (_buffer.Length <= idx)
                {
                    GrowAndEnsure();
                }
                _buffer[idx++] = JsonConstants.ListSeparator;
            }

            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = token;

            Advance(idx);
        }

        private void WriteStartSlow(byte token)
        {
            Debug.Assert(_writerOptions.Indented || !_writerOptions.SkipValidation);

            if (_writerOptions.Indented)
            {
                if (!_writerOptions.SkipValidation)
                {
                    ValidateStart();
                    UpdateBitStackOnStart(token);
                }
                WriteStartIndented(token);
            }
            else
            {
                Debug.Assert(!_writerOptions.SkipValidation);
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
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.CannotStartObjectArrayWithoutProperty, tokenType: _tokenType);
            }
            else
            {
                Debug.Assert(_tokenType != JsonTokenType.StartObject);
                if (_tokenType != JsonTokenType.None && (!_isNotPrimitive || CurrentDepth == 0))
                {
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.CannotStartObjectArrayAfterPrimitiveOrClose, tokenType: _tokenType);
                }
            }
        }

        private void WriteStartIndented(byte token)
        {
            int idx = 0;
            if (_currentDepth < 0)
            {
                if (_buffer.Length <= idx)
                {
                    GrowAndEnsure();
                }
                _buffer[idx++] = JsonConstants.ListSeparator;
            }

            if (_tokenType != JsonTokenType.None)
                WriteNewLine(ref idx);

            int indent = Indentation;
            while (true)
            {
                bool result = JsonWriterHelper.TryWriteIndentation(_buffer.Slice(idx), indent, out int bytesWritten);
                idx += bytesWritten;
                if (result)
                {
                    break;
                }
                indent -= bytesWritten;
                AdvanceAndGrow(ref idx);
            }

            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = token;

            Advance(idx);
        }

        /// <summary>
        /// Writes the beginning of a JSON array with a property name as the key.
        /// </summary>
        /// <param name="utf8PropertyName">The UTF-8 encoded property name of the JSON array to be written.</param>
        /// <param name="escape">If this is set to false, the writer assumes the property name is properly escaped and skips the escaping step.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the depth of the JSON has exceeded the maximum depth of 1000 
        /// OR if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteStartArray(ReadOnlySpan<byte> utf8PropertyName, bool escape = true)
        {
            ValidatePropertyNameAndDepth(utf8PropertyName);

            if (escape)
            {
                WriteStartEscape(utf8PropertyName, JsonConstants.OpenBracket);
            }
            else
            {
                WriteStartByOptions(utf8PropertyName, JsonConstants.OpenBracket);
            }

            _currentDepth &= JsonConstants.RemoveFlagsBitMask;
            _currentDepth++;
            _isNotPrimitive = true;
            _tokenType = JsonTokenType.StartArray;
        }

        /// <summary>
        /// Writes the beginning of a JSON object with a property name as the key.
        /// </summary>
        /// <param name="utf8PropertyName">The UTF-8 encoded property name of the JSON object to be written.</param>
        /// <param name="escape">If this is set to false, the writer assumes the property name is properly escaped and skips the escaping step.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the depth of the JSON has exceeded the maximum depth of 1000 
        /// OR if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteStartObject(ReadOnlySpan<byte> utf8PropertyName, bool escape = true)
        {
            ValidatePropertyNameAndDepth(utf8PropertyName);

            if (escape)
            {
                WriteStartEscape(utf8PropertyName, JsonConstants.OpenBrace);
            }
            else
            {
                WriteStartByOptions(utf8PropertyName, JsonConstants.OpenBrace);
            }

            _currentDepth &= JsonConstants.RemoveFlagsBitMask;
            _currentDepth++;
            _isNotPrimitive = true;
            _tokenType = JsonTokenType.StartObject;
        }

        private void WriteStartEscape(ReadOnlySpan<byte> utf8PropertyName, byte token)
        {
            int propertyIdx = JsonWriterHelper.NeedsEscaping(utf8PropertyName);

            Debug.Assert(propertyIdx >= -1 && propertyIdx < int.MaxValue / 2);

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
            int idx;
            if (_writerOptions.Indented)
            {
                idx = WritePropertyNameIndented(utf8PropertyName);
            }
            else
            {
                idx = WritePropertyNameMinimized(utf8PropertyName);
            }

            if (1 > _buffer.Length - idx)
            {
                AdvanceAndGrow(ref idx, 1);
            }

            _buffer[idx++] = token;

            Advance(idx);
        }

        private void WriteStartEscapeProperty(ReadOnlySpan<byte> utf8PropertyName, byte token, int firstEscapeIndexProp)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8PropertyName.Length);
            Debug.Assert(firstEscapeIndexProp >= 0 && firstEscapeIndexProp < utf8PropertyName.Length);

            byte[] propertyArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndexProp);
            Span<byte> escapedPropertyName;
            if (length > StackallocThreshold)
            {
                propertyArray = ArrayPool<byte>.Shared.Rent(length);
                escapedPropertyName = propertyArray;
            }
            else
            {
                // Cannot create a span directly since it gets passed to instance methods on a ref struct.
                unsafe
                {
                    byte* ptr = stackalloc byte[length];
                    escapedPropertyName = new Span<byte>(ptr, length);
                }
            }

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
        /// <param name="propertyName">The UTF-16 encoded property name of the JSON array to be transcoded and written as UTF-8.</param>
        /// <param name="escape">If this is set to false, the writer assumes the property name is properly escaped and skips the escaping step.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the depth of the JSON has exceeded the maximum depth of 1000 
        /// OR if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteStartArray(string propertyName, bool escape = true)
            => WriteStartArray(propertyName.AsSpan(), escape);

        /// <summary>
        /// Writes the beginning of a JSON object with a property name as the key.
        /// </summary>
        /// <param name="propertyName">The UTF-16 encoded property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="escape">If this is set to false, the writer assumes the property name is properly escaped and skips the escaping step.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the depth of the JSON has exceeded the maximum depth of 1000 
        /// OR if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteStartObject(string propertyName, bool escape = true)
            => WriteStartObject(propertyName.AsSpan(), escape);

        /// <summary>
        /// Writes the beginning of a JSON array with a property name as the key.
        /// </summary>
        /// <param name="propertyName">The UTF-16 encoded property name of the JSON array to be transcoded and written as UTF-8.</param>
        /// <param name="escape">If this is set to false, the writer assumes the property name is properly escaped and skips the escaping step.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the depth of the JSON has exceeded the maximum depth of 1000 
        /// OR if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteStartArray(ReadOnlySpan<char> propertyName, bool escape = true)
        {
            ValidatePropertyNameAndDepth(propertyName);

            if (escape)
            {
                WriteStartEscape(propertyName, JsonConstants.OpenBracket);
            }
            else
            {
                WriteStartByOptions(propertyName, JsonConstants.OpenBracket);
            }

            _currentDepth &= JsonConstants.RemoveFlagsBitMask;
            _currentDepth++;
            _isNotPrimitive = true;
            _tokenType = JsonTokenType.StartArray;
        }

        /// <summary>
        /// Writes the beginning of a JSON object with a property name as the key.
        /// </summary>
        /// <param name="propertyName">The UTF-16 encoded property name of the JSON object to be transcoded and written as UTF-8.</param>
        /// <param name="escape">If this is set to false, the writer assumes the property name is properly escaped and skips the escaping step.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the depth of the JSON has exceeded the maximum depth of 1000 
        /// OR if this would result in an invalid JSON to be written (while validation is enabled).
        /// </exception>
        public void WriteStartObject(ReadOnlySpan<char> propertyName, bool escape = true)
        {
            ValidatePropertyNameAndDepth(propertyName);

            if (escape)
            {
                WriteStartEscape(propertyName, JsonConstants.OpenBrace);
            }
            else
            {
                WriteStartByOptions(propertyName, JsonConstants.OpenBrace);
            }

            _currentDepth &= JsonConstants.RemoveFlagsBitMask;
            _currentDepth++;
            _isNotPrimitive = true;
            _tokenType = JsonTokenType.StartObject;
        }

        private void WriteStartEscape(ReadOnlySpan<char> propertyName, byte token)
        {
            int propertyIdx = JsonWriterHelper.NeedsEscaping(propertyName);

            Debug.Assert(propertyIdx >= -1 && propertyIdx < int.MaxValue / 2);

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
            int idx;
            if (_writerOptions.Indented)
            {
                idx = WritePropertyNameIndented(propertyName);
            }
            else
            {
                idx = WritePropertyNameMinimized(propertyName);
            }

            if (1 > _buffer.Length - idx)
            {
                AdvanceAndGrow(ref idx, 1);
            }

            _buffer[idx++] = token;

            Advance(idx);
        }

        private void WriteStartEscapeProperty(ReadOnlySpan<char> propertyName, byte token, int firstEscapeIndexProp)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= propertyName.Length);
            Debug.Assert(firstEscapeIndexProp >= 0 && firstEscapeIndexProp < propertyName.Length);

            char[] propertyArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(propertyName.Length, firstEscapeIndexProp);
            Span<char> escapedPropertyName;
            if (length > StackallocThreshold)
            {
                propertyArray = ArrayPool<char>.Shared.Rent(length);
                escapedPropertyName = propertyArray;
            }
            else
            {
                // Cannot create a span directly since it gets passed to instance methods on a ref struct.
                unsafe
                {
                    char* ptr = stackalloc char[length];
                    escapedPropertyName = new Span<char>(ptr, length);
                }
            }
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
            if (_writerOptions.IndentedOrNotSkipValidation)
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
            if (_buffer.Length < 1)
            {
                GrowAndEnsure();
            }

            _buffer[0] = token;
            Advance(1);
        }

        private void WriteEndSlow(byte token)
        {
            Debug.Assert(_writerOptions.Indented || !_writerOptions.SkipValidation);

            if (_writerOptions.Indented)
            {
                if (!_writerOptions.SkipValidation)
                {
                    ValidateEnd(token);
                }
                WriteEndIndented(token);
            }
            else
            {
                Debug.Assert(!_writerOptions.SkipValidation);
                ValidateEnd(token);
                WriteEndMinimized(token);
            }
        }

        private void ValidateEnd(byte token)
        {
            if (_bitStack.CurrentDepth <= 0)
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.MismatchedObjectArray, token);

            if (token == JsonConstants.CloseBracket)
            {
                if (_inObject)
                {
                    Debug.Assert(_tokenType != JsonTokenType.None);
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.MismatchedObjectArray, token);
                }
            }
            else
            {
                Debug.Assert(token == JsonConstants.CloseBrace);

                if (!_inObject)
                {
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.MismatchedObjectArray, token);
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
                int idx = 0;
                WriteNewLine(ref idx);

                int indent = Indentation;
                // Necessary if WriteEndX is called without a corresponding WriteStartX first.
                if (indent != 0)
                {
                    // The end token should be at an outer indent and since we haven't updated
                    // current depth yet, explicitly subtract here.
                    indent -= JsonConstants.SpacesPerIndent;
                }
                while (true)
                {
                    bool result = JsonWriterHelper.TryWriteIndentation(_buffer.Slice(idx), indent, out int bytesWritten);
                    idx += bytesWritten;
                    if (result)
                    {
                        break;
                    }
                    indent -= bytesWritten;
                    AdvanceAndGrow(ref idx);
                }

                if (_buffer.Length <= idx)
                {
                    AdvanceAndGrow(ref idx);
                }
                _buffer[idx++] = token;

                Advance(idx);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteNewLine(ref int idx)
        {
            // Write '\r\n' OR '\n', depending on OS
            if (Environment.NewLine.Length == 2)
            {
                if (_buffer.Length <= idx)
                {
                    AdvanceAndGrow(ref idx);
                }
                _buffer[idx++] = JsonConstants.CarriageReturn;
            }

            if (_buffer.Length <= idx)
            {
                AdvanceAndGrow(ref idx);
            }
            _buffer[idx++] = JsonConstants.LineFeed;
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

        private void GrowAndEnsure()
        {
            Flush();
            int previousSpanLength = _buffer.Length;
            Debug.Assert(previousSpanLength < DefaultGrowthSize);
            _buffer = _output.GetSpan(DefaultGrowthSize);
            if (_buffer.Length <= previousSpanLength)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.FailedToGetLargerSpan);
            }
        }

        private void GrowAndEnsure(int minimumSize)
        {
            Flush();
            Debug.Assert(minimumSize < DefaultGrowthSize);
            _buffer = _output.GetSpan(DefaultGrowthSize);
            if (_buffer.Length < minimumSize)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.FailedToGetMinimumSizeSpan, minimumSize);
            }
        }

        private void AdvanceAndGrow(ref int alreadyWritten)
        {
            Debug.Assert(alreadyWritten >= 0);
            Advance(alreadyWritten);
            GrowAndEnsure();
            alreadyWritten = 0;
        }

        private void AdvanceAndGrow(ref int alreadyWritten, int minimumSize)
        {
            Debug.Assert(minimumSize >= 1 && minimumSize <= 128);
            Advance(alreadyWritten);
            GrowAndEnsure(minimumSize);
            alreadyWritten = 0;
        }

        private void CopyLoop(ReadOnlySpan<byte> span, ref int idx)
        {
            while (true)
            {
                if (span.Length <= _buffer.Length - idx)
                {
                    span.CopyTo(_buffer.Slice(idx));
                    idx += span.Length;
                    break;
                }

                span.Slice(0, _buffer.Length - idx).CopyTo(_buffer.Slice(idx));
                span = span.Slice(_buffer.Length - idx);
                idx = _buffer.Length;
                AdvanceAndGrow(ref idx);
            }
        }

        private void SetFlagToAddListSeparatorBeforeNextItem()
        {
            _currentDepth |= 1 << 31;
        }
    }
}
