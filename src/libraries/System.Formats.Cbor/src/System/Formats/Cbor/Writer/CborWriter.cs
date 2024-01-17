// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Formats.Cbor
{
    /// <summary>A writer for Concise Binary Object Representation (CBOR) encoded data.</summary>
    public partial class CborWriter
    {
        private const int DefaultCapacitySentinel = -1;
        private static readonly ArrayPool<byte> s_bufferPool = ArrayPool<byte>.Create();

        private byte[] _buffer;
        private int _offset;

        private Stack<StackFrame>? _nestedDataItems;
        private CborMajorType? _currentMajorType; // major type of the current data item context
        private int? _definiteLength; // predetermined definite-length of current data item context
        private int _itemsWritten; // number of items written in the current context
        private int _frameOffset; // buffer offset particular to the current data item context
        private bool _isTagContext; // true if writer is expecting a tagged value

        // Map-specific book-keeping
        private int? _currentKeyOffset; // offset for the current key encoding
        private int? _currentValueOffset; // offset for the current value encoding
        private bool _keysRequireSorting; // tracks whether key/value pair encodings need to be sorted
        private List<KeyValuePairEncodingRange>? _keyValuePairEncodingRanges; // all key/value pair encoding ranges
        private HashSet<(int Offset, int Length)>? _keyEncodingRanges; // all key encoding ranges up to encoding equality

        /// <summary>Gets the conformance mode used by this writer.</summary>
        /// <value>One of the enumeration values that represent the conformance mode used by this writer.</value>
        public CborConformanceMode ConformanceMode { get; }

        /// <summary>Gets a value that indicates whether the writer automatically converts indefinite-length encodings into definite-length equivalents.</summary>
        /// <value><see langword="true" /> if the writer automatically converts indefinite-length encodings into definite-length equivalents; otherwise, <see langword="false" />.</value>
        public bool ConvertIndefiniteLengthEncodings { get; }

        /// <summary>Gets a value that indicates whether this writer allows multiple root-level CBOR data items.</summary>
        /// <value><see langword="true" /> if the writer allows multiple root-level CBOR data items; otherwise, <see langword="false" />.</value>
        public bool AllowMultipleRootLevelValues { get; }

        /// <summary>Gets the writer's current level of nestedness in the CBOR document.</summary>
        /// <value>A number that represents the current level of nestedness in the CBOR document.</value>
        public int CurrentDepth => _nestedDataItems is null ? 0 : _nestedDataItems.Count;

        /// <summary>Gets the total number of bytes that have been written to the buffer.</summary>
        /// <value>The total number of bytes that have been written to the buffer.</value>
        public int BytesWritten => _offset;

        /// <summary>Declares whether the writer has completed writing a complete root-level CBOR document, or sequence of root-level CBOR documents.</summary>
        /// <value><see langword="true" /> if the writer has completed writing a complete root-level CBOR document, or sequence of root-level CBOR documents; <see langword="false" /> otherwise.</value>
        public bool IsWriteCompleted => _currentMajorType is null && _itemsWritten > 0;

        /// <summary>Initializes a new instance of <see cref="CborWriter" /> class using the specified configuration.</summary>
        /// <param name="conformanceMode">One of the enumeration values that specifies the guidance on the conformance checks performed on the encoded data.
        /// Defaults to <see cref="CborConformanceMode.Strict" /> conformance mode.</param>
        /// <param name="convertIndefiniteLengthEncodings"><see langword="true" /> to enable automatically converting indefinite-length encodings into definite-length equivalents and allow use of indefinite-length write APIs in conformance modes that otherwise do not permit it; otherwise, <see langword="false" />.</param>
        /// <param name="allowMultipleRootLevelValues"><see langword="true" /> to allow multiple root-level values to be written by the writer; otherwise, <see langword="false" />.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="conformanceMode" /> is not a defined <see cref="CborConformanceMode" />.</exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public CborWriter(CborConformanceMode conformanceMode, bool convertIndefiniteLengthEncodings, bool allowMultipleRootLevelValues)
            : this(conformanceMode, convertIndefiniteLengthEncodings, allowMultipleRootLevelValues, DefaultCapacitySentinel)
        {
        }

        /// <summary>Initializes a new instance of <see cref="CborWriter" /> class using the specified configuration.</summary>
        /// <param name="conformanceMode">One of the enumeration values that specifies the guidance on the conformance checks performed on the encoded data.
        /// Defaults to <see cref="CborConformanceMode.Strict" /> conformance mode.</param>
        /// <param name="convertIndefiniteLengthEncodings"><see langword="true" /> to enable automatically converting indefinite-length encodings into definite-length equivalents and allow use of indefinite-length write APIs in conformance modes that otherwise do not permit it; otherwise, <see langword="false" />.</param>
        /// <param name="allowMultipleRootLevelValues"><see langword="true" /> to allow multiple root-level values to be written by the writer; otherwise, <see langword="false" />.</param>
        /// <param name="initialCapacity">The initial capacity of the underlying buffer. The value -1 can be used to use the default capacity.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para><paramref name="conformanceMode" /> is not a defined <see cref="CborConformanceMode" />.</para>
        /// <para>-or-</para>
        /// <para><paramref name="initialCapacity"/> is not zero, positive, or the default value indicator -1.</para>
        /// </exception>
        public CborWriter(
            CborConformanceMode conformanceMode = CborConformanceMode.Strict,
            bool convertIndefiniteLengthEncodings = false,
            bool allowMultipleRootLevelValues = false,
            int initialCapacity = DefaultCapacitySentinel)
        {
            CborConformanceModeHelpers.Validate(conformanceMode);

            ConformanceMode = conformanceMode;
            ConvertIndefiniteLengthEncodings = convertIndefiniteLengthEncodings;
            AllowMultipleRootLevelValues = allowMultipleRootLevelValues;
            _definiteLength = allowMultipleRootLevelValues ? null : (int?)1;

            _buffer = initialCapacity switch
            {
                DefaultCapacitySentinel or 0 => Array.Empty<byte>(),
                < -1 => throw new ArgumentOutOfRangeException(nameof(initialCapacity)),
                _ => new byte[initialCapacity],
            };
        }

        /// <summary>Resets the writer to have no data, without releasing resources.</summary>
        public void Reset()
        {
            if (_offset > 0)
            {
                Array.Clear(_buffer, 0, _offset);

                _offset = 0;
                _nestedDataItems?.Clear();
                _currentMajorType = null;
                _definiteLength = null;
                _itemsWritten = 0;
                _frameOffset = 0;
                _isTagContext = false;

                _currentKeyOffset = null;
                _currentValueOffset = null;
                _keysRequireSorting = false;
                _keyValuePairEncodingRanges?.Clear();
                _keyEncodingRanges?.Clear();
            }
        }

        /// <summary>Writes a single CBOR data item which has already been encoded.</summary>
        /// <param name="encodedValue">The encoded value to write.</param>
        /// <exception cref="ArgumentException"><para><paramref name="encodedValue" /> is not a well-formed CBOR encoding.</para>
        /// <para>-or-</para>
        /// <para><paramref name="encodedValue" /> is not valid under the current conformance mode.</para></exception>
        public void WriteEncodedValue(ReadOnlySpan<byte> encodedValue)
        {
            ValidateEncoding(encodedValue, ConformanceMode);
            EnsureWriteCapacity(encodedValue.Length);

            // even though the encoding might be valid CBOR, it might not be valid within the current writer context.
            // E.g. we're at the end of a definite-length collection or writing integers in an indefinite-length string.
            // For this reason we write the initial byte separately and perform the usual validation.
            CborInitialByte initialByte = new CborInitialByte(encodedValue[0]);
            WriteInitialByte(initialByte);

            // now copy any remaining bytes
            encodedValue = encodedValue.Slice(1);

            if (!encodedValue.IsEmpty)
            {
                encodedValue.CopyTo(_buffer.AsSpan(_offset));
                _offset += encodedValue.Length;
            }

            AdvanceDataItemCounters();

            static unsafe void ValidateEncoding(ReadOnlySpan<byte> encodedValue, CborConformanceMode conformanceMode)
            {
                fixed (byte* ptr = &MemoryMarshal.GetReference(encodedValue))
                {
                    using var manager = new PointerMemoryManager<byte>(ptr, encodedValue.Length);
                    var reader = new CborReader(manager.Memory, conformanceMode: conformanceMode, allowMultipleRootLevelValues: false);

                    try
                    {
                        reader.SkipValue(disableConformanceModeChecks: false);
                    }
                    catch (CborContentException e)
                    {
                        throw new ArgumentException(SR.Cbor_Writer_PayloadIsNotValidCbor, e);
                    }

                    if (reader.BytesRemaining > 0)
                    {
                        throw new ArgumentException(SR.Cbor_Writer_PayloadIsNotValidCbor);
                    }
                }

            }
        }

        /// <summary>Returns a new array containing the encoded value.</summary>
        /// <returns>A precisely-sized array containing the encoded value.</returns>
        /// <exception cref="InvalidOperationException">The writer does not contain a complete CBOR value or sequence of root-level values.</exception>
        public byte[] Encode() => GetSpanEncoding().ToArray();

        /// <summary>Writes the encoded representation of the data to <paramref name="destination" />.</summary>
        /// <param name="destination">The buffer in which to write.</param>
        /// <returns>The number of bytes written to <paramref name="destination" />.</returns>
        /// <exception cref="InvalidOperationException">The writer does not contain a complete CBOR value or sequence of root-level values.</exception>
        /// <exception cref="ArgumentException">The destination buffer is not large enough to hold the encoded value.</exception>
        public int Encode(Span<byte> destination)
        {
            ReadOnlySpan<byte> encoding = GetSpanEncoding();

            if (encoding.Length > destination.Length)
            {
                throw new ArgumentException(SR.Argument_EncodeDestinationTooSmall, nameof(destination));
            }

            encoding.CopyTo(destination);
            return encoding.Length;
        }

        /// <summary>Attempts to write the encoded representation of the data to <paramref name="destination" />.</summary>
        /// <param name="destination">The buffer in which to write.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes written to <paramref name="destination" />.</param>
        /// <returns><see langword="true" /> if the encode succeeded, <see langword="false" /> if <paramref name="destination" /> is too small.</returns>
        /// <exception cref="InvalidOperationException">The writer does not contain a complete CBOR value or sequence of root-level values.</exception>
        public bool TryEncode(Span<byte> destination, out int bytesWritten)
        {
            ReadOnlySpan<byte> encoding = GetSpanEncoding();

            if (encoding.Length > destination.Length)
            {
                bytesWritten = 0;
                return false;
            }

            encoding.CopyTo(destination);
            bytesWritten = encoding.Length;
            return true;
        }

        private ReadOnlySpan<byte> GetSpanEncoding()
        {
            if (!IsWriteCompleted)
            {
                throw new InvalidOperationException(SR.Cbor_Writer_IncompleteCborDocument);
            }

            return new ReadOnlySpan<byte>(_buffer, 0, _offset);
        }

        private void EnsureWriteCapacity(int pendingCount)
        {
            if (pendingCount < 0)
            {
                throw new OverflowException();
            }

            int currentCapacity = _buffer.Length;
            int requiredCapacity = _offset + pendingCount;
            if (currentCapacity < requiredCapacity)
            {
                int newCapacity = currentCapacity == 0 ? 1024 : currentCapacity * 2;
                const uint MaxArrayLength = 0x7FFFFFC7; // Array.MaxLength
#if NETCOREAPP
                Debug.Assert(MaxArrayLength == Array.MaxLength);
#endif
                if ((uint)newCapacity > MaxArrayLength || newCapacity < requiredCapacity)
                {
                    newCapacity = requiredCapacity;
                }

                byte[] newBuffer = new byte[newCapacity];
                new ReadOnlySpan<byte>(_buffer, 0, _offset).CopyTo(newBuffer);
                _buffer = newBuffer;
            }
        }

        private void PushDataItem(CborMajorType newMajorType, int? definiteLength)
        {
            _nestedDataItems ??= new Stack<StackFrame>();

            var frame = new StackFrame(
                type: _currentMajorType,
                frameOffset: _frameOffset,
                definiteLength: _definiteLength,
                itemsWritten: _itemsWritten,
                currentKeyOffset: _currentKeyOffset,
                currentValueOffset: _currentValueOffset,
                keysRequireSorting: _keysRequireSorting,
                keyValuePairEncodingRanges: _keyValuePairEncodingRanges,
                keyEncodingRanges: _keyEncodingRanges
            );

            _nestedDataItems.Push(frame);

            _currentMajorType = newMajorType;
            _frameOffset = _offset;
            _definiteLength = definiteLength;
            _itemsWritten = 0;
            _currentKeyOffset = null;
            _currentValueOffset = null;
            _keysRequireSorting = false;
            _keyEncodingRanges = null;
            _keyValuePairEncodingRanges = null;
            _isTagContext = false;
        }

        private void PopDataItem(CborMajorType typeToPop)
        {
            // Validate that the pop operation can be performed
            if (typeToPop != _currentMajorType)
            {
                if (_currentMajorType.HasValue)
                {
                    throw new InvalidOperationException(SR.Format(SR.Cbor_PopMajorTypeMismatch, (int)_currentMajorType));
                }
                else
                {
                    throw new InvalidOperationException(SR.Cbor_Reader_IsAtRootContext);
                }
            }

            Debug.Assert(_nestedDataItems?.Count > 0); // implied by previous check

            if (_isTagContext)
            {
                // writer expecting value after a tag data item, cannot pop the current context
                throw new InvalidOperationException(SR.Format(SR.Cbor_PopMajorTypeMismatch, (int)CborMajorType.Tag));
            }

            if (_definiteLength - _itemsWritten > 0)
            {
                throw new InvalidOperationException(SR.Cbor_NotAtEndOfDefiniteLengthDataItem);
            }

            // Perform encoding fixups that require the current context and must be done before popping
            // NB map key sorting must happen _before_ indefinite-length patching

            if (typeToPop == CborMajorType.Map)
            {
                CompleteMapWrite();
            }

            if (_definiteLength == null)
            {
                CompleteIndefiniteLengthWrite(typeToPop);
            }

            // pop writer state
            StackFrame frame = _nestedDataItems.Pop();
            _currentMajorType = frame.MajorType;
            _frameOffset = frame.FrameOffset;
            _definiteLength = frame.DefiniteLength;
            _itemsWritten = frame.ItemsWritten;
            _currentKeyOffset = frame.CurrentKeyOffset;
            _currentValueOffset = frame.CurrentValueOffset;
            _keysRequireSorting = frame.KeysRequireSorting;
            _keyValuePairEncodingRanges = frame.KeyValuePairEncodingRanges;
            _keyEncodingRanges = frame.KeyEncodingRanges;
        }

        // Advance writer state after a data item has been written to the buffer
        private void AdvanceDataItemCounters()
        {
            if (_currentMajorType == CborMajorType.Map)
            {
                if (_itemsWritten % 2 == 0)
                {
                    HandleMapKeyWritten();
                }
                else
                {
                    HandleMapValueWritten();
                }
            }

            _itemsWritten++;
            _isTagContext = false;
        }

        private void WriteInitialByte(CborInitialByte initialByte)
        {
            if (_definiteLength - _itemsWritten == 0)
            {
                throw new InvalidOperationException(SR.Cbor_Writer_DefiniteLengthExceeded);
            }

            switch (_currentMajorType)
            {
                case CborMajorType.ByteString:
                case CborMajorType.TextString:
                    // Indefinite-length string contexts allow two possible data items:
                    // 1) Definite-length string chunks of the same major type OR
                    // 2) a break byte denoting the end of the indefinite-length string context.
                    // NB the second check is not needed here, as we use a separate mechanism to append the break byte
                    if (initialByte.MajorType != _currentMajorType ||
                        initialByte.AdditionalInfo == CborAdditionalInfo.IndefiniteLength)
                    {
                        throw new InvalidOperationException(SR.Cbor_Writer_CannotNestDataItemsInIndefiniteLengthStrings);
                    }

                    break;
            }

            _buffer[_offset++] = initialByte.InitialByte;
        }

        private void CompleteIndefiniteLengthWrite(CborMajorType type)
        {
            Debug.Assert(_definiteLength == null);

            if (ConvertIndefiniteLengthEncodings)
            {
                // indefinite-length not allowed, convert the encoding into definite-length
                switch (type)
                {
                    case CborMajorType.ByteString:
                    case CborMajorType.TextString:
                        PatchIndefiniteLengthString(type);
                        break;
                    case CborMajorType.Array:
                        PatchIndefiniteLengthCollection(CborMajorType.Array, _itemsWritten);
                        break;
                    case CborMajorType.Map:
                        Debug.Assert(_itemsWritten % 2 == 0);
                        PatchIndefiniteLengthCollection(CborMajorType.Map, _itemsWritten / 2);
                        break;
                    default:
                        Debug.Fail("Invalid CBOR major type pushed to stack.");
                        throw new Exception();
                }
            }
            else
            {
                // using indefinite-length encoding, append a break byte to the existing encoding
                EnsureWriteCapacity(1);
                _buffer[_offset++] = CborInitialByte.IndefiniteLengthBreakByte;
            }
        }

        private readonly struct StackFrame
        {
            public StackFrame(
                CborMajorType? type,
                int frameOffset,
                int? definiteLength,
                int itemsWritten,
                int? currentKeyOffset,
                int? currentValueOffset,
                bool keysRequireSorting,
                List<KeyValuePairEncodingRange>? keyValuePairEncodingRanges,
                HashSet<(int Offset, int Length)>? keyEncodingRanges)
            {
                MajorType = type;
                FrameOffset = frameOffset;
                DefiniteLength = definiteLength;
                ItemsWritten = itemsWritten;
                CurrentKeyOffset = currentKeyOffset;
                CurrentValueOffset = currentValueOffset;
                KeysRequireSorting = keysRequireSorting;
                KeyValuePairEncodingRanges = keyValuePairEncodingRanges;
                KeyEncodingRanges = keyEncodingRanges;
            }

            public CborMajorType? MajorType { get; }
            public int FrameOffset { get; }
            public int? DefiniteLength { get; }
            public int ItemsWritten { get; }

            public int? CurrentKeyOffset { get; }
            public int? CurrentValueOffset { get; }
            public bool KeysRequireSorting { get; }
            public List<KeyValuePairEncodingRange>? KeyValuePairEncodingRanges { get; }
            public HashSet<(int Offset, int Length)>? KeyEncodingRanges { get; }
        }
    }
}
