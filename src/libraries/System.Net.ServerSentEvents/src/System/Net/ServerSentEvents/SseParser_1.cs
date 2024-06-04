// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.ServerSentEvents
{
    /// <summary>Provides a parser for server-sent events information.</summary>
    /// <typeparam name="T">Specifies the type of data parsed from an event.</typeparam>
    public sealed class SseParser<T>
    {
        // For reference:
        // Specification: https://html.spec.whatwg.org/multipage/server-sent-events.html#server-sent-events

        /// <summary>Carriage Return.</summary>
        private const byte CR = (byte)'\r';
        /// <summary>Line Feed.</summary>
        private const byte LF = (byte)'\n';
        /// <summary>Carriage Return Line Feed.</summary>
        private static ReadOnlySpan<byte> CRLF => "\r\n"u8;

        /// <summary>The default size of an ArrayPool buffer to rent.</summary>
        /// <remarks>Larger size used by default to minimize number of reads. Smaller size used in debug to stress growth/shifting logic.</remarks>
        private const int DefaultArrayPoolRentSize =
#if DEBUG
            16;
#else
            1024;
#endif

        /// <summary>The stream to be parsed.</summary>
        private readonly Stream _stream;
        /// <summary>The parser delegate used to transform bytes into a <typeparamref name="T"/>.</summary>
        private readonly SseItemParser<T> _itemParser;

        /// <summary>Indicates whether the enumerable has already been used for enumeration.</summary>
        private int _used;

        /// <summary>Buffer, either empty or rented, containing the data being read from the stream while looking for the next line.</summary>
        private byte[] _lineBuffer = [];
        /// <summary>The starting offset of valid data in <see cref="_lineBuffer"/>.</summary>
        private int _lineOffset;
        /// <summary>The length of valid data in <see cref="_lineBuffer"/>, starting from <see cref="_lineOffset"/>.</summary>
        private int _lineLength;
        /// <summary>The index in <see cref="_lineBuffer"/> where a newline ('\r', '\n', or "\r\n") was found.</summary>
        private int _newlineIndex;
        /// <summary>The index in <see cref="_lineBuffer"/> of characters already checked for newlines.</summary>
        /// <remarks>
        /// This is to avoid O(LineLength^2) behavior in the rare case where we have long lines that are built-up over multiple reads.
        /// We want to avoid re-checking the same characters we've already checked over and over again.
        /// </remarks>
        private int _lastSearchedForNewline;
        /// <summary>Set when eof has been reached in the stream.</summary>
        private bool _eof;

        /// <summary>Rented buffer containing buffered data for the next event.</summary>
        private byte[]? _dataBuffer;
        /// <summary>The length of valid data in <see cref="_dataBuffer"/>, starting from index 0.</summary>
        private int _dataLength;
        /// <summary>Whether data has been appended to <see cref="_dataBuffer"/>.</summary>
        /// <remarks>This can be different than <see cref="_dataLength"/> != 0 if empty data was appended.</remarks>
        private bool _dataAppended;

        /// <summary>The event type for the next event.</summary>
        private string _eventType = SseParser.EventTypeDefault;

        /// <summary>Initialize the enumerable.</summary>
        /// <param name="stream">The stream to parse.</param>
        /// <param name="itemParser">The function to use to parse payload bytes into a <typeparamref name="T"/>.</param>
        internal SseParser(Stream stream, SseItemParser<T> itemParser)
        {
            _stream = stream;
            _itemParser = itemParser;
        }

        /// <summary>Gets an enumerable of the server-sent events from this parser.</summary>
        /// <exception cref="InvalidOperationException">The parser has already been enumerated. Such an exception may propagate out of a call to <see cref="IEnumerator.MoveNext"/>.</exception>
        public IEnumerable<SseItem<T>> Enumerate()
        {
            // Validate that the parser is only used for one enumeration.
            ThrowIfNotFirstEnumeration();

            // Rent a line buffer. This will grow as needed. The line buffer is what's passed to the stream,
            // so we want it to be large enough to reduce the number of reads we need to do when data is
            // arriving quickly. (In debug, we use a smaller buffer to stress the growth and shifting logic.)
            _lineBuffer = ArrayPool<byte>.Shared.Rent(DefaultArrayPoolRentSize);
            try
            {
                // Spec: "Event streams in this format must always be encoded as UTF-8".
                // Skip a UTF8 BOM if it exists at the beginning of the stream. (The BOM is defined as optional in the SSE grammar.)
                while (FillLineBuffer() != 0 && _lineLength < Utf8Bom.Length) ;
                SkipBomIfPresent();

                // Process all events in the stream.
                while (true)
                {
                    // See if there's a complete line in data already read from the stream. Lines are permitted to
                    // end with CR, LF, or CRLF. Look for all of them and if we find one, process the line. However,
                    // if we only find a CR and it's at the end of the read data, don't process it now, as we want
                    // to process it together with an LF that might immediately follow, rather than treating them
                    // as two separate characters, in which case we'd incorrectly process the CR as a line by itself.
                    GetNextSearchOffsetAndLength(out int searchOffset, out int searchLength);
                    _newlineIndex = _lineBuffer.AsSpan(searchOffset, searchLength).IndexOfAny(CR, LF);
                    if (_newlineIndex >= 0)
                    {
                        _lastSearchedForNewline = -1;
                        _newlineIndex += searchOffset;
                        if (_lineBuffer[_newlineIndex] is LF || // the newline is LF
                            _newlineIndex - _lineOffset + 1 < _lineLength || // we must have CR and we have whatever comes after it
                            _eof) // if we get here, we know we have a CR at the end of the buffer, so it's definitely the whole newline if we've hit EOF
                        {
                            // Process the line.
                            if (ProcessLine(out SseItem<T> sseItem, out int advance))
                            {
                                yield return sseItem;
                            }

                            // Move past the line.
                            _lineOffset += advance;
                            _lineLength -= advance;
                            continue;
                        }
                    }
                    else
                    {
                        // Record the last position searched for a newline. The next time we search,
                        // we'll search from here rather than from _lineOffset, in order to avoid searching
                        // the same characters again.
                        _lastSearchedForNewline = _lineOffset + _lineLength;
                    }

                    // We've processed everything in the buffer we currently can, so if we've already read EOF, we're done.
                    if (_eof)
                    {
                        // Spec: "Once the end of the file is reached, any pending data must be discarded. (If the file ends in the middle of an
                        // event, before the final empty line, the incomplete event is not dispatched.)"
                        break;
                    }

                    // Read more data into the buffer.
                    FillLineBuffer();
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(_lineBuffer);
                if (_dataBuffer is not null)
                {
                    ArrayPool<byte>.Shared.Return(_dataBuffer);
                }
            }
        }

        /// <summary>Gets an asynchronous enumerable of the server-sent events from this parser.</summary>
        /// <param name="cancellationToken">The cancellation token to use to cancel the enumeration.</param>
        /// <exception cref="InvalidOperationException">The parser has already been enumerated. Such an exception may propagate out of a call to <see cref="IAsyncEnumerator{T}.MoveNextAsync"/>.</exception>
        /// <exception cref="OperationCanceledException">The enumeration was canceled. Such an exception may propagate out of a call to <see cref="IAsyncEnumerator{T}.MoveNextAsync"/>.</exception>
        public async IAsyncEnumerable<SseItem<T>> EnumerateAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Validate that the parser is only used for one enumeration.
            ThrowIfNotFirstEnumeration();

            // Rent a line buffer. This will grow as needed. The line buffer is what's passed to the stream,
            // so we want it to be large enough to reduce the number of reads we need to do when data is
            // arriving quickly. (In debug, we use a smaller buffer to stress the growth and shifting logic.)
            _lineBuffer = ArrayPool<byte>.Shared.Rent(DefaultArrayPoolRentSize);
            try
            {
                // Spec: "Event streams in this format must always be encoded as UTF-8".
                // Skip a UTF8 BOM if it exists at the beginning of the stream. (The BOM is defined as optional in the SSE grammar.)
                while (await FillLineBufferAsync(cancellationToken).ConfigureAwait(false) != 0 && _lineLength < Utf8Bom.Length) ;
                SkipBomIfPresent();

                // Process all events in the stream.
                while (true)
                {
                    // See if there's a complete line in data already read from the stream. Lines are permitted to
                    // end with CR, LF, or CRLF. Look for all of them and if we find one, process the line. However,
                    // if we only find a CR and it's at the end of the read data, don't process it now, as we want
                    // to process it together with an LF that might immediately follow, rather than treating them
                    // as two separate characters, in which case we'd incorrectly process the CR as a line by itself.
                    GetNextSearchOffsetAndLength(out int searchOffset, out int searchLength);
                    _newlineIndex = _lineBuffer.AsSpan(searchOffset, searchLength).IndexOfAny(CR, LF);
                    if (_newlineIndex >= 0)
                    {
                        _lastSearchedForNewline = -1;
                        _newlineIndex += searchOffset;
                        if (_lineBuffer[_newlineIndex] is LF || // newline is LF
                            _newlineIndex - _lineOffset + 1 < _lineLength || // newline is CR, and we have whatever comes after it
                            _eof) // if we get here, we know we have a CR at the end of the buffer, so it's definitely the whole newline if we've hit EOF
                        {
                            // Process the line.
                            if (ProcessLine(out SseItem<T> sseItem, out int advance))
                            {
                                yield return sseItem;
                            }

                            // Move past the line.
                            _lineOffset += advance;
                            _lineLength -= advance;
                            continue;
                        }
                    }
                    else
                    {
                        // Record the last position searched for a newline. The next time we search,
                        // we'll search from here rather than from _lineOffset, in order to avoid searching
                        // the same characters again.
                        _lastSearchedForNewline = searchOffset + searchLength;
                    }

                    // We've processed everything in the buffer we currently can, so if we've already read EOF, we're done.
                    if (_eof)
                    {
                        // Spec: "Once the end of the file is reached, any pending data must be discarded. (If the file ends in the middle of an
                        // event, before the final empty line, the incomplete event is not dispatched.)"
                        break;
                    }

                    // Read more data into the buffer.
                    await FillLineBufferAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(_lineBuffer);
                if (_dataBuffer is not null)
                {
                    ArrayPool<byte>.Shared.Return(_dataBuffer);
                }
            }
        }

        /// <summary>Gets the next index and length with which to perform a newline search.</summary>
        private void GetNextSearchOffsetAndLength(out int searchOffset, out int searchLength)
        {
            if (_lastSearchedForNewline > _lineOffset)
            {
                searchOffset = _lastSearchedForNewline;
                searchLength = _lineLength - (_lastSearchedForNewline - _lineOffset);
            }
            else
            {
                searchOffset = _lineOffset;
                searchLength = _lineLength;
            }

            Debug.Assert(searchOffset >= _lineOffset, $"{searchOffset}, {_lineLength}");
            Debug.Assert(searchOffset <= _lineOffset + _lineLength, $"{searchOffset}, {_lineOffset}, {_lineLength}");
            Debug.Assert(searchOffset <= _lineBuffer.Length, $"{searchOffset}, {_lineBuffer.Length}");

            Debug.Assert(searchLength >= 0, $"{searchLength}");
            Debug.Assert(searchLength <= _lineLength, $"{searchLength}, {_lineLength}");
        }

        private int GetNewLineLength()
        {
            Debug.Assert(_newlineIndex - _lineOffset < _lineLength, "Expected to be positioned at a non-empty newline");
            return _lineBuffer.AsSpan(_newlineIndex, _lineLength - (_newlineIndex - _lineOffset)).StartsWith(CRLF) ? 2 : 1;
        }

        /// <summary>
        /// If there's no room remaining in the line buffer, either shifts the contents
        /// left or grows the buffer in order to make room for the next read.
        /// </summary>
        private void ShiftOrGrowLineBufferIfNecessary()
        {
            // If data we've read is butting up against the end of the buffer and
            // it's not taking up the entire buffer, slide what's there down to
            // the beginning, making room to read more data into the buffer (since
            // there's no newline in the data that's there). Otherwise, if the whole
            // buffer is full, grow the buffer to accommodate more data, since, again,
            // what's there doesn't contain a newline and thus a line is longer than
            // the current buffer accommodates.
            if (_lineOffset + _lineLength == _lineBuffer.Length)
            {
                if (_lineOffset != 0)
                {
                    _lineBuffer.AsSpan(_lineOffset, _lineLength).CopyTo(_lineBuffer);
                    if (_lastSearchedForNewline >= 0)
                    {
                        _lastSearchedForNewline -= _lineOffset;
                    }
                    _lineOffset = 0;
                }
                else if (_lineLength == _lineBuffer.Length)
                {
                    GrowBuffer(ref _lineBuffer, _lineBuffer.Length * 2);
                }
            }
        }

        /// <summary>Processes a complete line from the SSE stream.</summary>
        /// <param name="sseItem">The parsed item if the method returns true.</param>
        /// <param name="advance">How many characters to advance in the line buffer.</param>
        /// <returns>true if an SSE item was successfully parsed; otherwise, false.</returns>
        private bool ProcessLine(out SseItem<T> sseItem, out int advance)
        {
            ReadOnlySpan<byte> line = _lineBuffer.AsSpan(_lineOffset, _newlineIndex - _lineOffset);

            // Spec: "If the line is empty (a blank line) Dispatch the event"
            if (line.IsEmpty)
            {
                advance = GetNewLineLength();

                if (_dataAppended)
                {
                    sseItem = new SseItem<T>(_itemParser(_eventType, _dataBuffer.AsSpan(0, _dataLength)), _eventType);
                    _eventType = SseParser.EventTypeDefault;
                    _dataLength = 0;
                    _dataAppended = false;
                    return true;
                }

                sseItem = default;
                return false;
            }

            // Find the colon separating the field name and value.
            int colonPos = line.IndexOf((byte)':');
            ReadOnlySpan<byte> fieldName;
            ReadOnlySpan<byte> fieldValue;
            if (colonPos >= 0)
            {
                // Spec: "Collect the characters on the line before the first U+003A COLON character (:), and let field be that string."
                fieldName = line.Slice(0, colonPos);

                // Spec: "Collect the characters on the line after the first U+003A COLON character (:), and let value be that string.
                // If value starts with a U+0020 SPACE character, remove it from value."
                fieldValue = line.Slice(colonPos + 1);
                if (!fieldValue.IsEmpty && fieldValue[0] == (byte)' ')
                {
                    fieldValue = fieldValue.Slice(1);
                }
            }
            else
            {
                // Spec: "using the whole line as the field name, and the empty string as the field value."
                fieldName = line;
                fieldValue = [];
            }

            if (fieldName.SequenceEqual("data"u8))
            {
                // Spec: "Append the field value to the data buffer, then append a single U+000A LINE FEED (LF) character to the data buffer."
                // Spec: "If the data buffer's last character is a U+000A LINE FEED (LF) character, then remove the last character from the data buffer."

                // If there's nothing currently in the data buffer and we can easily detect that this line is immediately followed by
                // an empty line, we can optimize it to just handle the data directly from the line buffer, rather than first copying
                // into the data buffer and dispatching from there.
                if (!_dataAppended)
                {
                    int newlineLength = GetNewLineLength();
                    ReadOnlySpan<byte> remainder = _lineBuffer.AsSpan(_newlineIndex + newlineLength, _lineLength - line.Length - newlineLength);
                    if (!remainder.IsEmpty &&
                        (remainder[0] is LF || (remainder[0] is CR && remainder.Length > 1)))
                    {
                        advance = line.Length + newlineLength + (remainder.StartsWith(CRLF) ? 2 : 1);
                        sseItem = new SseItem<T>(_itemParser(_eventType, fieldValue), _eventType);
                        _eventType = SseParser.EventTypeDefault;
                        return true;
                    }
                }

                // We need to copy the data from the data buffer to the line buffer. Make sure there's enough room.
                if (_dataBuffer is null || _dataLength + _lineLength + 1 > _dataBuffer.Length)
                {
                    GrowBuffer(ref _dataBuffer, _dataLength + _lineLength + 1);
                }

                // Append a newline if there's already content in the buffer.
                // Then copy the field value to the data buffer
                if (_dataAppended)
                {
                    _dataBuffer[_dataLength++] = LF;
                }
                fieldValue.CopyTo(_dataBuffer.AsSpan(_dataLength));
                _dataLength += fieldValue.Length;
                _dataAppended = true;
            }
            else if (fieldName.SequenceEqual("event"u8))
            {
                // Spec: "Set the event type buffer to field value."
                _eventType = SseParser.Utf8GetString(fieldValue);
            }
            else if (fieldName.SequenceEqual("id"u8))
            {
                // Spec: "If the field value does not contain U+0000 NULL, then set the last event ID buffer to the field value. Otherwise, ignore the field."
                if (fieldValue.IndexOf((byte)'\0') < 0)
                {
                    // Note that fieldValue might be empty, in which case LastEventId will naturally be reset to the empty string. This is per spec.
                    LastEventId = SseParser.Utf8GetString(fieldValue);
                }
            }
            else if (fieldName.SequenceEqual("retry"u8))
            {
                // Spec: "If the field value consists of only ASCII digits, then interpret the field value as an integer in base ten,
                // and set the event stream's reconnection time to that integer. Otherwise, ignore the field."
                if (long.TryParse(
#if NET
                    fieldValue,
#else
                    SseParser.Utf8GetString(fieldValue),
#endif
                    NumberStyles.None, CultureInfo.InvariantCulture, out long milliseconds))
                {
                    ReconnectionInterval = TimeSpan.FromMilliseconds(milliseconds);
                }
            }
            else
            {
                // We'll end up here if the line starts with a colon, producing an empty field name, or if the field name is otherwise unrecognized.
                // Spec: "If the line starts with a U+003A COLON character (:) Ignore the line."
                // Spec: "Otherwise, The field is ignored"
            }

            advance = line.Length + GetNewLineLength();
            sseItem = default;
            return false;
        }

        /// <summary>Gets the last event ID.</summary>
        /// <remarks>This value is updated any time a new last event ID is parsed. It is not reset between SSE items.</remarks>
        public string LastEventId { get; private set; } = string.Empty; // Spec: "must be initialized to the empty string"

        /// <summary>Gets the reconnection interval.</summary>
        /// <remarks>
        /// If no retry event was received, this defaults to <see cref="Timeout.InfiniteTimeSpan"/>, and it will only
        /// ever be <see cref="Timeout.InfiniteTimeSpan"/> in that situation. If a client wishes to retry, the server-sent
        /// events specification states that the interval may then be decided by the client implementation and should be a
        /// few seconds.
        /// </remarks>
        public TimeSpan ReconnectionInterval { get; private set; } = Timeout.InfiniteTimeSpan;

        /// <summary>Transitions the object to a used state, throwing if it's already been used.</summary>
        private void ThrowIfNotFirstEnumeration()
        {
            if (Interlocked.Exchange(ref _used, 1) != 0)
            {
                throw new InvalidOperationException(SR.InvalidOperation_EnumerateOnlyOnce);
            }
        }

        /// <summary>Reads data from the stream into the line buffer.</summary>
        private int FillLineBuffer()
        {
            ShiftOrGrowLineBufferIfNecessary();

            int offset = _lineOffset + _lineLength;
            int bytesRead = _stream.Read(
#if NET
                _lineBuffer.AsSpan(offset));
#else
                _lineBuffer, offset, _lineBuffer.Length - offset);
#endif

            if (bytesRead > 0)
            {
                _lineLength += bytesRead;
            }
            else
            {
                _eof = true;
                bytesRead = 0;
            }

            return bytesRead;
        }

        /// <summary>Reads data asynchronously from the stream into the line buffer.</summary>
        private async ValueTask<int> FillLineBufferAsync(CancellationToken cancellationToken)
        {
            ShiftOrGrowLineBufferIfNecessary();

            int offset = _lineOffset + _lineLength;
            int bytesRead = await
#if NET
                _stream.ReadAsync(_lineBuffer.AsMemory(offset), cancellationToken)
#else
                new ValueTask<int>(_stream.ReadAsync(_lineBuffer, offset, _lineBuffer.Length - offset, cancellationToken))
#endif
                .ConfigureAwait(false);

            if (bytesRead > 0)
            {
                _lineLength += bytesRead;
            }
            else
            {
                _eof = true;
                bytesRead = 0;
            }

            return bytesRead;
        }

        /// <summary>Gets the UTF8 BOM.</summary>
        private static ReadOnlySpan<byte> Utf8Bom => [0xEF, 0xBB, 0xBF];

        /// <summary>Called at the beginning of processing to skip over an optional UTF8 byte order mark.</summary>
        private void SkipBomIfPresent()
        {
            Debug.Assert(_lineOffset == 0, $"Expected _lineOffset == 0, got {_lineOffset}");

            if (_lineBuffer.AsSpan(0, _lineLength).StartsWith(Utf8Bom))
            {
                _lineOffset += 3;
                _lineLength -= 3;
            }
        }

        /// <summary>Grows the buffer, returning the existing one to the ArrayPool and renting an ArrayPool replacement.</summary>
        private static void GrowBuffer([NotNull] ref byte[]? buffer, int minimumLength)
        {
            byte[]? toReturn = buffer;
            buffer = ArrayPool<byte>.Shared.Rent(Math.Max(minimumLength, DefaultArrayPoolRentSize));
            if (toReturn is not null)
            {
                Array.Copy(toReturn, buffer, toReturn.Length);
                ArrayPool<byte>.Shared.Return(toReturn);
            }
        }
    }
}
