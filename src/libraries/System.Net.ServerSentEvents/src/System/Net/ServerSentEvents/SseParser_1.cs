// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
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

        /// <summary>The maximum number of milliseconds representible by <see cref="System.TimeSpan"/>.</summary>
        private readonly long TimeSpan_MaxValueMilliseconds = (long)TimeSpan.MaxValue.TotalMilliseconds;

        /// <summary>The default size of an ArrayPool buffer to rent.</summary>
        /// <remarks>
        /// Larger size used by default to minimize number of reads. Smaller size used in debug to stress growth/shifting logic.
        /// Also serves as the smallest configurable maximum buffer size; buffers smaller than this don't meaningfully reduce
        /// memory usage but can cause excessive I/O and line-buffer churn.
        /// </remarks>
        private const int DefaultArrayPoolRentSize =
#if DEBUG
            16;
#else
            1024;
#endif

        /// <summary>The maximum amount of data buffered by default.</summary>
        private const int DefaultMaxBufferSize = 1024 * 1024 * 1024;

        /// <summary>The stream to be parsed.</summary>
        private readonly Stream _stream;
        /// <summary>The parser delegate used to transform bytes into a <typeparamref name="T"/>.</summary>
        private readonly SseItemParser<T> _itemParser;

        /// <summary>Indicates whether the enumerable has already been used for enumeration.</summary>
        private int _used;

        /// <summary>Buffer containing the data being read from the stream while looking for the next line.</summary>
        private ArrayBuffer _lineBuffer = new(initialSize: 0, usePool: true);
        /// <summary>The index relative to the start of the line buffer's active region where a newline ('\r', '\n', or "\r\n") was found.</summary>
        private int _newlineIndex;
        /// <summary>The index relative to the start of the line buffer's active region of characters already checked for newlines.</summary>
        /// <remarks>
        /// This is to avoid O(LineLength^2) behavior in the rare case where we have long lines that are built-up over multiple reads.
        /// We want to avoid re-checking the same characters we've already checked over and over again.
        /// </remarks>
        private int _lastSearchedForNewline;
        /// <summary>Set when eof has been reached in the stream.</summary>
        private bool _eof;

        /// <summary>Buffer containing buffered data for the next event.</summary>
        private ArrayBuffer _dataBuffer = new(initialSize: 0, usePool: true);
        /// <summary>Whether data has been appended to <see cref="_dataBuffer"/>.</summary>
        /// <remarks>This can be different than <see cref="ArrayBuffer.ActiveLength"/> != 0 if empty data was appended.</remarks>
        private bool _dataAppended;

        private readonly int _maxBufferSize;

        /// <summary>The event type for the next event.</summary>
        private string? _eventType;

        /// <summary>The event id for the next event.</summary>
        private string? _eventId;

        /// <summary>The reconnection interval for the next event.</summary>
        private TimeSpan? _nextReconnectionInterval;

        /// <summary>Initialize the enumerable.</summary>
        /// <param name="stream">The stream to parse.</param>
        /// <param name="options">The options to use to parse the stream.</param>
        internal SseParser(Stream stream, SseParserOptions<T> options)
        {
            _stream = stream;
            _itemParser = options.ItemParser;
            _maxBufferSize = options.MaxBufferSize == -1 ? DefaultMaxBufferSize : Math.Max(options.MaxBufferSize, DefaultArrayPoolRentSize);

#if NET
            _maxBufferSize = Math.Min(_maxBufferSize, Array.MaxLength);
#else
            _maxBufferSize = Math.Min(_maxBufferSize, 0x7FFFFFC7);
#endif
        }

        /// <summary>Gets an enumerable of the server-sent events from this parser.</summary>
        /// <exception cref="InvalidOperationException">The parser has already been enumerated. Such an exception may propagate out of a call to <see cref="IEnumerator.MoveNext"/>.</exception>
        public IEnumerable<SseItem<T>> Enumerate()
        {
            // Validate that the parser is only used for one enumeration.
            ThrowIfNotFirstEnumeration();

            try
            {
                // Spec: "Event streams in this format must always be encoded as UTF-8".
                // Skip a UTF8 BOM if it exists at the beginning of the stream. (The BOM is defined as optional in the SSE grammar.)
                while (FillLineBuffer() != 0 && _lineBuffer.ActiveLength < Utf8Bom.Length) ;
                SkipBomIfPresent();

                // Process all events in the stream.
                while (true)
                {
                    if (TryProcessLine(out SseItem<T>? sseItem))
                    {
                        if (sseItem.HasValue)
                        {
                            yield return sseItem.GetValueOrDefault();
                        }

                        continue;
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
                _lineBuffer.Dispose();
                _dataBuffer.Dispose();
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

            try
            {
                // Spec: "Event streams in this format must always be encoded as UTF-8".
                // Skip a UTF8 BOM if it exists at the beginning of the stream. (The BOM is defined as optional in the SSE grammar.)
                while (await FillLineBufferAsync(cancellationToken).ConfigureAwait(false) != 0 && _lineBuffer.ActiveLength < Utf8Bom.Length) ;
                SkipBomIfPresent();

                // Process all events in the stream.
                while (true)
                {
                    if (TryProcessLine(out SseItem<T>? sseItem))
                    {
                        if (sseItem.HasValue)
                        {
                            yield return sseItem.GetValueOrDefault();
                        }

                        continue;
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
                _lineBuffer.Dispose();
                _dataBuffer.Dispose();
            }
        }

        /// <summary>Tries to process a complete line from data already read from the stream.</summary>
        /// <param name="sseItem">The parsed item if processing the line dispatched an event; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if a complete line was processed; otherwise, <see langword="false"/>.</returns>
        private bool TryProcessLine(out SseItem<T>? sseItem)
        {
            // See if there's a complete line in data already read from the stream. Lines are permitted to
            // end with CR, LF, or CRLF. Look for all of them and if we find one, process the line. However,
            // if we only find a CR and it's at the end of the read data, don't process it now, as we want
            // to process it together with an LF that might immediately follow, rather than treating them
            // as two separate characters, in which case we'd incorrectly process the CR as a line by itself.
            ReadOnlySpan<byte> lineBuffer = _lineBuffer.ActiveReadOnlySpan;
            int searchOffset = Math.Max(_lastSearchedForNewline, 0);
            _newlineIndex = lineBuffer.Slice(searchOffset).IndexOfAny(CR, LF);
            if (_newlineIndex >= 0)
            {
                _lastSearchedForNewline = -1;
                _newlineIndex += searchOffset;
                if (lineBuffer[_newlineIndex] is LF || // the newline is LF
                    _newlineIndex + 1 < lineBuffer.Length || // we must have CR and we have whatever comes after it
                    _eof) // if we get here, we know we have a CR at the end of the buffer, so it's definitely the whole newline if we've hit EOF
                {
                    // Process the line.
                    sseItem = ProcessLine(out SseItem<T> item) ? item : null;
                    return true;
                }
            }
            else
            {
                // Record the last position searched for a newline. The next time we search,
                // we'll search from here rather than from the beginning, in order to avoid searching
                // the same characters again.
                _lastSearchedForNewline = lineBuffer.Length;
            }

            sseItem = null;
            return false;
        }

        private int GetNewLineLength(ReadOnlySpan<byte> lineBuffer)
        {
            Debug.Assert(_newlineIndex < lineBuffer.Length, "Expected to be positioned at a non-empty newline");
            return lineBuffer.Slice(_newlineIndex).StartsWith(CRLF) ? 2 : 1;
        }

        /// <summary>Processes a complete line from the SSE stream.</summary>
        /// <param name="sseItem">The parsed item if the method returns true.</param>
        /// <returns>true if an SSE item was successfully parsed; otherwise, false.</returns>
        private bool ProcessLine(out SseItem<T> sseItem)
        {
            ReadOnlySpan<byte> lineBuffer = _lineBuffer.ActiveReadOnlySpan;
            ReadOnlySpan<byte> line = lineBuffer.Slice(0, _newlineIndex);

            // Spec: "If the line is empty (a blank line) Dispatch the event"
            if (line.IsEmpty)
            {
                int advance = GetNewLineLength(lineBuffer);

                if (_dataAppended)
                {
                    T data = _itemParser(_eventType ?? SseParser.EventTypeDefault, _dataBuffer.ActiveReadOnlySpan);
                    sseItem = new SseItem<T>(data, _eventType) { EventId = _eventId, ReconnectionInterval = _nextReconnectionInterval };
                    _eventType = null;
                    _eventId = null;
                    _nextReconnectionInterval = null;
                    _dataBuffer.DiscardAll();
                    _dataAppended = false;

                    _lineBuffer.Discard(advance);
                    return true;
                }

                _lineBuffer.Discard(advance);
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
                    int newlineLength = GetNewLineLength(lineBuffer);
                    ReadOnlySpan<byte> remainder = lineBuffer.Slice(_newlineIndex + newlineLength);
                    if (!remainder.IsEmpty &&
                        (remainder[0] is LF || (remainder[0] is CR && remainder.Length > 1)))
                    {
                        T data = _itemParser(_eventType ?? SseParser.EventTypeDefault, fieldValue);
                        sseItem = new SseItem<T>(data, _eventType) { EventId = _eventId, ReconnectionInterval = _nextReconnectionInterval };
                        _eventType = null;
                        _eventId = null;
                        _nextReconnectionInterval = null;

                        _lineBuffer.Discard(line.Length + newlineLength + (remainder.StartsWith(CRLF) ? 2 : 1));
                        return true;
                    }
                }

                // We need to copy the data from the line buffer to the data buffer. Make sure there's enough room.
                int requiredAvailableSpace = lineBuffer.Length + 1;
                if (_dataBuffer.AvailableLength < requiredAvailableSpace)
                {
                    if (requiredAvailableSpace > _maxBufferSize - _dataBuffer.ActiveLength)
                    {
                        throw new InvalidDataException(SR.InvalidDataException_SseExceededMaxLength);
                    }

                    _dataBuffer.EnsureAvailableSpace(
                        _dataBuffer.Capacity == 0 ? Math.Max(requiredAvailableSpace, DefaultArrayPoolRentSize) : requiredAvailableSpace);
                }

                // Append a newline if there's already content in the buffer.
                // Then copy the field value to the data buffer
                Span<byte> destination = _dataBuffer.AvailableSpan;
                int bytesWritten = 0;
                if (_dataAppended)
                {
                    destination[bytesWritten++] = LF;
                }
                fieldValue.CopyTo(destination.Slice(bytesWritten));
                _dataBuffer.Commit(bytesWritten + fieldValue.Length);
                _dataAppended = true;
            }
            else if (fieldName.SequenceEqual("event"u8))
            {
                // Spec: "Set the event type buffer to field value."
                _eventType = Encoding.UTF8.GetString(fieldValue);
            }
            else if (fieldName.SequenceEqual("id"u8))
            {
                // Spec: "If the field value does not contain U+0000 NULL, then set the last event ID buffer to the field value. Otherwise, ignore the field."
                if (!fieldValue.Contains((byte)'\0'))
                {
                    // Note that fieldValue might be empty, in which case LastEventId will naturally be reset to the empty string. This is per spec.
                    LastEventId = _eventId = Encoding.UTF8.GetString(fieldValue);
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
                    Encoding.UTF8.GetString(fieldValue),
#endif
                    NumberStyles.None, CultureInfo.InvariantCulture, out long milliseconds) &&
                    0 <= milliseconds && milliseconds <= TimeSpan_MaxValueMilliseconds)
                {
                    // Workaround for TimeSpan.FromMilliseconds not being able to roundtrip TimeSpan.MaxValue
                    TimeSpan timeSpan = milliseconds == TimeSpan_MaxValueMilliseconds ? TimeSpan.MaxValue : TimeSpan.FromMilliseconds(milliseconds);
                    _nextReconnectionInterval = ReconnectionInterval = timeSpan;
                }
            }
            else
            {
                // We'll end up here if the line starts with a colon, producing an empty field name, or if the field name is otherwise unrecognized.
                // Spec: "If the line starts with a U+003A COLON character (:) Ignore the line."
                // Spec: "Otherwise, The field is ignored"
            }

            _lineBuffer.Discard(line.Length + GetNewLineLength(lineBuffer));
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
            EnsureLineBufferAvailableSpace();
            int bytesRead = _stream.Read(
#if NET
                _lineBuffer.AvailableSpan);
#else
                _lineBuffer.DangerousGetUnderlyingBuffer(),
                _lineBuffer.ActiveStartOffset + _lineBuffer.ActiveLength,
                _lineBuffer.AvailableLength);
#endif

            if (bytesRead > 0)
            {
                _lineBuffer.Commit(bytesRead);
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
            EnsureLineBufferAvailableSpace();
            int bytesRead = await _stream.ReadAsync(_lineBuffer.AvailableMemory, cancellationToken).ConfigureAwait(false);

            if (bytesRead > 0)
            {
                _lineBuffer.Commit(bytesRead);
            }
            else
            {
                _eof = true;
                bytesRead = 0;
            }

            return bytesRead;
        }

        private void EnsureLineBufferAvailableSpace()
        {
            if (_lineBuffer.AvailableLength == 0)
            {
                if (_lineBuffer.ActiveLength >= _maxBufferSize)
                {
                    throw new InvalidDataException(SR.InvalidDataException_SseExceededMaxLength);
                }

                _lineBuffer.EnsureAvailableSpace(_lineBuffer.Capacity == 0 ? DefaultArrayPoolRentSize : 1);
            }
        }

        /// <summary>Gets the UTF8 BOM.</summary>
        private static ReadOnlySpan<byte> Utf8Bom => [0xEF, 0xBB, 0xBF];

        /// <summary>Called at the beginning of processing to skip over an optional UTF8 byte order mark.</summary>
        private void SkipBomIfPresent()
        {
            if (_lineBuffer.ActiveReadOnlySpan.StartsWith(Utf8Bom))
            {
                _lineBuffer.Discard(Utf8Bom.Length);
            }
        }
    }
}
