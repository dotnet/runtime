// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Enable CHECK_ACCURATE_ENSURE to ensure that the AsnWriter is not ever
// abusing the normal EnsureWriteCapacity + ArrayPool behaviors of rounding up.
//#define CHECK_ACCURATE_ENSURE

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace System.Formats.Asn1
{
    /// <summary>
    ///   A writer for BER-, CER-, and DER-encoded ASN.1 data.
    /// </summary>
    public sealed partial class AsnWriter
    {
        private byte[] _buffer = null!;
        private int _offset;
        private Stack<StackFrame>? _nestingStack;

        /// <summary>
        ///   Gets the encoding rules in use by this writer.
        /// </summary>
        /// <value>
        ///   The encoding rules in use by this writer.
        /// </value>
        public AsnEncodingRules RuleSet { get; }

        /// <summary>
        ///   Create a new <see cref="AsnWriter"/> with a given set of encoding rules.
        /// </summary>
        /// <param name="ruleSet">The encoding constraints for the writer.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="ruleSet"/> is not defined.
        /// </exception>
        public AsnWriter(AsnEncodingRules ruleSet)
        {
            if (ruleSet != AsnEncodingRules.BER &&
                ruleSet != AsnEncodingRules.CER &&
                ruleSet != AsnEncodingRules.DER)
            {
                throw new ArgumentOutOfRangeException(nameof(ruleSet));
            }

            RuleSet = ruleSet;
        }

        /// <summary>
        ///   Reset the writer to have no data, without releasing resources.
        /// </summary>
        public void Reset()
        {
            if (_offset > 0)
            {
                Debug.Assert(_buffer != null);
                Array.Clear(_buffer, 0, _offset);
                _offset = 0;

                _nestingStack?.Clear();
            }
        }

        /// <summary>
        ///   Gets the number of bytes that would be written by <see cref="TryEncode"/>.
        /// </summary>
        /// <returns>
        ///   The number of bytes that would be written by <see cref="TryEncode"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   <see cref="PushSequence"/>, <see cref="PushSetOf"/>, or
        ///   <see cref="PushOctetString"/> was called without the corresponding
        ///   Pop method.
        /// </exception>
        public int GetEncodedLength()
        {
            if ((_nestingStack?.Count ?? 0) != 0)
            {
                throw new InvalidOperationException(SR.AsnWriter_EncodeUnbalancedStack);
            }

            return _offset;
        }

        /// <summary>
        ///   Attempts to write the encoded representation of the data to <paramref name="destination"/>.
        /// </summary>
        /// <param name="destination">The buffer in which to write.</param>
        /// <param name="bytesWritten">
        ///   On success, receives the number of bytes written to <paramref name="destination"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the encode succeeded,
        ///   <see langword="false"/> if <paramref name="destination"/> is too small.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   A <see cref="PushSequence"/> or <see cref="PushSetOf"/> has not been closed via
        ///   <see cref="PopSequence"/> or <see cref="PopSetOf"/>.
        /// </exception>
        public bool TryEncode(Span<byte> destination, out int bytesWritten)
        {
            if ((_nestingStack?.Count ?? 0) != 0)
                throw new InvalidOperationException(SR.AsnWriter_EncodeUnbalancedStack);

            // If the stack is closed out then everything is a definite encoding (BER, DER) or a
            // required indefinite encoding (CER). So we're correctly sized up, and ready to copy.
            if (destination.Length < _offset)
            {
                bytesWritten = 0;
                return false;
            }

            if (_offset == 0)
            {
                bytesWritten = 0;
                return true;
            }

            bytesWritten = _offset;
            _buffer.AsSpan(0, _offset).CopyTo(destination);
            return true;
        }

        /// <summary>
        ///   Writes the encoded representation of the data to <paramref name="destination"/>.
        /// </summary>
        /// <param name="destination">The buffer in which to write.</param>
        /// <returns>
        ///   The number of bytes written to <paramref name="destination" />.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   A <see cref="PushSequence"/> or <see cref="PushSetOf"/> has not been closed via
        ///   <see cref="PopSequence"/> or <see cref="PopSetOf"/>.
        /// </exception>
        public int Encode(Span<byte> destination)
        {
            // Since TryEncode doesn't have any side effects on the return false paths, just
            // call it from here and do argument validation late.
            if (!TryEncode(destination, out int bytesWritten))
            {
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
            }

            Debug.Assert(bytesWritten == _offset);
            return bytesWritten;
        }

        /// <summary>
        ///   Return a new array containing the encoded value.
        /// </summary>
        /// <returns>
        ///   A precisely-sized array containing the encoded value.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   A <see cref="PushSequence"/> or <see cref="PushSetOf"/> has not been closed via
        ///   <see cref="PopSequence"/> or <see cref="PopSetOf"/>.
        /// </exception>
        public byte[] Encode()
        {
            if ((_nestingStack?.Count ?? 0) != 0)
            {
                throw new InvalidOperationException(SR.AsnWriter_EncodeUnbalancedStack);
            }

            if (_offset == 0)
            {
                return Array.Empty<byte>();
            }

            // If the stack is closed out then everything is a definite encoding (BER, DER) or a
            // required indefinite encoding (CER). So we're correctly sized up, and ready to copy.
            return _buffer.AsSpan(0, _offset).ToArray();
        }

        private ReadOnlySpan<byte> EncodeAsSpan()
        {
            if ((_nestingStack?.Count ?? 0) != 0)
            {
                throw new InvalidOperationException(SR.AsnWriter_EncodeUnbalancedStack);
            }

            if (_offset == 0)
            {
                return ReadOnlySpan<byte>.Empty;
            }

            // If the stack is closed out then everything is a definite encoding (BER, DER) or a
            // required indefinite encoding (CER). So we're correctly sized up, and ready to copy.
            return new ReadOnlySpan<byte>(_buffer, 0, _offset);
        }

        /// <summary>
        ///   Determines if <see cref="Encode()"/> would produce an output identical to
        ///   <paramref name="other"/>.
        /// </summary>
        /// <returns>
        ///   <see langword="true"/> if the pending encoded data is identical to <paramref name="other"/>,
        ///   <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   A <see cref="PushSequence"/> or <see cref="PushSetOf"/> has not been closed via
        ///   <see cref="PopSequence"/> or <see cref="PopSetOf"/>.
        /// </exception>
        public bool EncodedValueEquals(ReadOnlySpan<byte> other)
        {
            return EncodeAsSpan().SequenceEqual(other);
        }

        /// <summary>
        ///   Determines if <see cref="Encode()"/> would produce an output identical to
        ///   <paramref name="other"/>.
        /// </summary>
        /// <returns>
        ///   <see langword="true"/> if the pending encoded data is identical to <paramref name="other"/>,
        ///   <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="other"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   A <see cref="PushSequence"/> or <see cref="PushSetOf"/> has not been closed via
        ///   <see cref="PopSequence"/> or <see cref="PopSetOf"/>.
        /// </exception>
        public bool EncodedValueEquals(AsnWriter other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            return EncodeAsSpan().SequenceEqual(other.EncodeAsSpan());
        }

        private void EnsureWriteCapacity(int pendingCount)
        {
            if (pendingCount < 0)
            {
                throw new OverflowException();
            }

            if (_buffer == null || _buffer.Length - _offset < pendingCount)
            {
#if CHECK_ACCURATE_ENSURE
                // A debug paradigm to make sure that throughout the execution nothing ever writes
                // past where the buffer was "allocated".  This causes quite a number of reallocs
                // and copies, so it's a #define opt-in.
                byte[] newBytes = new byte[_offset + pendingCount];

                if (_buffer != null)
                {
                    Span<byte> bufferSpan = _buffer.AsSpan(0, _offset);
                    bufferSpan.CopyTo(newBytes);
                    bufferSpan.Clear();
                }

                _buffer = newBytes;
#else
                const int BlockSize = 1024;
                // Make sure we don't run into a lot of "grow a little" by asking in 1k steps.
                int blocks = checked(_offset + pendingCount + (BlockSize - 1)) / BlockSize;
                byte[]? oldBytes = _buffer;
                Array.Resize(ref _buffer, BlockSize * blocks);

                if (oldBytes != null)
                {
                    oldBytes.AsSpan(0, _offset).Clear();
                }
#endif

#if DEBUG
                // Ensure no "implicit 0" is happening, in case we move to pooling.
                _buffer.AsSpan(_offset).Fill(0xCA);
#endif
            }
        }

        private void WriteTag(Asn1Tag tag)
        {
            int spaceRequired = tag.CalculateEncodedSize();
            EnsureWriteCapacity(spaceRequired);

            if (!tag.TryEncode(_buffer.AsSpan(_offset, spaceRequired), out int written) ||
                written != spaceRequired)
            {
                Debug.Fail($"TryWrite failed or written was wrong value ({written} vs {spaceRequired})");
                throw new InvalidOperationException();
            }

            _offset += spaceRequired;
        }

        // T-REC-X.690-201508 sec 8.1.3
        private void WriteLength(int length)
        {
            const byte MultiByteMarker = 0x80;
            Debug.Assert(length >= -1);

            // If the indefinite form has been requested.
            // T-REC-X.690-201508 sec 8.1.3.6
            if (length == -1)
            {
                EnsureWriteCapacity(1);
                _buffer[_offset] = MultiByteMarker;
                _offset++;
                return;
            }

            Debug.Assert(length >= 0);

            // T-REC-X.690-201508 sec 8.1.3.3, 8.1.3.4
            if (length < MultiByteMarker)
            {
                // Pre-allocate the pending data since we know how much.
                EnsureWriteCapacity(1 + length);
                _buffer[_offset] = (byte)length;
                _offset++;
                return;
            }

            // The rest of the method implements T-REC-X.680-201508 sec 8.1.3.5
            int lengthLength = GetEncodedLengthSubsequentByteCount(length);

            // Pre-allocate the pending data since we know how much.
            EnsureWriteCapacity(lengthLength + 1 + length);
            _buffer[_offset] = (byte)(MultiByteMarker | lengthLength);

            // No minus one because offset didn't get incremented yet.
            int idx = _offset + lengthLength;

            int remaining = length;

            do
            {
                _buffer[idx] = (byte)remaining;
                remaining >>= 8;
                idx--;
            } while (remaining > 0);

            Debug.Assert(idx == _offset);
            _offset += lengthLength + 1;
        }

        // T-REC-X.690-201508 sec 8.1.3.5
        private static int GetEncodedLengthSubsequentByteCount(int length)
        {
            if (length < 0)
                throw new OverflowException();
            if (length <= 0x7F)
                return 0;
            if (length <= byte.MaxValue)
                return 1;
            if (length <= ushort.MaxValue)
                return 2;
            if (length <= 0x00FFFFFF)
                return 3;

            return 4;
        }

        /// <summary>
        ///   Copy the value of this writer into another.
        /// </summary>
        /// <param name="destination">The writer to receive the value.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="destination"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   A <see cref="PushSequence"/> or <see cref="PushSetOf"/> has not been closed via
        ///   <see cref="PopSequence"/> or <see cref="PopSetOf"/>.
        ///
        ///   -or-
        ///
        ///   This writer is empty.
        ///
        ///   -or-
        ///
        ///   This writer represents more than one top-level value.
        ///
        ///   -or-
        ///
        ///   This writer's value is encoded in a manner that is not compatible with the
        ///   ruleset for the destination writer.
        /// </exception>
        public void CopyTo(AsnWriter destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            try
            {
                destination.WriteEncodedValue(EncodeAsSpan());
            }
            catch (ArgumentException e)
            {
                throw new InvalidOperationException(new InvalidOperationException().Message, e);
            }
        }

        /// <summary>
        ///   Write a single value which has already been encoded.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <remarks>
        ///   This method only checks that the tag and length are encoded according to the current ruleset,
        ///   and that the end of the value is the end of the input. The contents are not evaluated for
        ///   semantic meaning.
        /// </remarks>
        /// <exception cref="ArgumentException">
        ///   <paramref name="value"/> could not be read under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   <paramref name="value"/> has data beyond the end of the first value.
        /// </exception>
        public void WriteEncodedValue(ReadOnlySpan<byte> value)
        {
            // Is it legal under the current rules?
            bool read = AsnDecoder.TryReadEncodedValue(
                value,
                RuleSet,
                out _,
                out _,
                out _,
                out int consumed);

            if (!read || consumed != value.Length)
            {
                throw new ArgumentException(
                    SR.Argument_WriteEncodedValue_OneValueAtATime,
                    nameof(value));
            }

            EnsureWriteCapacity(value.Length);
            value.CopyTo(_buffer.AsSpan(_offset));
            _offset += value.Length;
        }

        // T-REC-X.690-201508 sec 8.1.5
        private void WriteEndOfContents()
        {
            EnsureWriteCapacity(2);
            _buffer[_offset++] = 0;
            _buffer[_offset++] = 0;
        }

        private Scope PushTag(Asn1Tag tag, UniversalTagNumber tagType)
        {
            if (_nestingStack == null)
            {
                _nestingStack = new Stack<StackFrame>();
            }

            Debug.Assert(tag.IsConstructed);
            WriteTag(tag);
            _nestingStack.Push(new StackFrame(tag, _offset, tagType));
            // Indicate that the length is indefinite.
            // We'll come back and clean this up (as appropriate) in PopTag.
            WriteLength(-1);
            return new Scope(this);
        }

        private void PopTag(Asn1Tag tag, UniversalTagNumber tagType, bool sortContents = false)
        {
            if (_nestingStack == null || _nestingStack.Count == 0)
            {
                throw new InvalidOperationException(SR.AsnWriter_PopWrongTag);
            }

            (Asn1Tag stackTag, int lenOffset, UniversalTagNumber stackTagType) = _nestingStack.Peek();

            Debug.Assert(tag.IsConstructed);
            if (stackTag != tag || stackTagType != tagType)
            {
                throw new InvalidOperationException(SR.AsnWriter_PopWrongTag);
            }

            _nestingStack.Pop();

            if (sortContents)
            {
                Debug.Assert(tagType == UniversalTagNumber.SetOf);
                SortContents(_buffer, lenOffset + 1, _offset);
            }

            // BER could use the indefinite encoding that CER does.
            // But since the definite encoding form is easier to read (doesn't require a contextual
            // parser to find the end-of-contents marker) some ASN.1 readers (including the previous
            // incarnation of AsnReader) may choose not to support it.
            //
            // So, BER will use the DER rules here, in the interest of broader compatibility.

            // T-REC-X.690-201508 sec 9.1 (constructed CER => indefinite length)
            // T-REC-X.690-201508 sec 8.1.3.6
            if (RuleSet == AsnEncodingRules.CER && tagType != UniversalTagNumber.OctetString)
            {
                WriteEndOfContents();
                return;
            }

            int containedLength = _offset - 1 - lenOffset;
            Debug.Assert(containedLength >= 0);

            int start = lenOffset + 1;

            // T-REC-X.690-201508 sec 9.2
            // T-REC-X.690-201508 sec 10.2
            if (tagType == UniversalTagNumber.OctetString)
            {
                if (RuleSet != AsnEncodingRules.CER || containedLength <= AsnDecoder.MaxCERSegmentSize)
                {
                    // Need to replace the tag with the primitive tag.
                    // Since the P/C bit doesn't affect the length, overwrite the tag.
                    int tagLen = tag.CalculateEncodedSize();
                    tag.AsPrimitive().Encode(_buffer.AsSpan(lenOffset - tagLen, tagLen));
                    // Continue with the regular flow.
                }
                else
                {
                    int fullSegments = Math.DivRem(
                        containedLength,
                        AsnDecoder.MaxCERSegmentSize,
                        out int lastSegmentSize);

                    int requiredPadding =
                        // Each full segment has a header of 048203E8
                        4 * fullSegments +
                        // The last one is 04 plus the encoded length.
                        2 + GetEncodedLengthSubsequentByteCount(lastSegmentSize);

                    // Shift the data forward so we can use right-source-overlapped
                    // copy in the existing method.
                    // Also, ensure the space for the end-of-contents marker.
                    EnsureWriteCapacity(requiredPadding + 2);
                    ReadOnlySpan<byte> src = _buffer.AsSpan(start, containedLength);
                    Span<byte> dest = _buffer.AsSpan(start + requiredPadding, containedLength);
                    src.CopyTo(dest);

                    int expectedEnd = start + containedLength + requiredPadding + 2;
                    _offset = lenOffset - tag.CalculateEncodedSize();
                    WriteConstructedCerOctetString(tag, dest);
                    Debug.Assert(_offset == expectedEnd);
                    return;
                }
            }

            int shiftSize = GetEncodedLengthSubsequentByteCount(containedLength);

            // Best case, length fits in the compact byte
            if (shiftSize == 0)
            {
                _buffer[lenOffset] = (byte)containedLength;
                return;
            }

            // We're currently at the end, so ensure we have room for N more bytes.
            EnsureWriteCapacity(shiftSize);

            // Buffer.BlockCopy correctly does forward-overlapped, so use it.
            Buffer.BlockCopy(_buffer, start, _buffer, start + shiftSize, containedLength);

            int tmp = _offset;
            _offset = lenOffset;
            WriteLength(containedLength);
            Debug.Assert(_offset - lenOffset - 1 == shiftSize);
            _offset = tmp + shiftSize;
        }

        private static void SortContents(byte[] buffer, int start, int end)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(end >= start);

            int len = end - start;

            if (len == 0)
            {
                return;
            }

            // Since BER can read everything and the reader does not mutate data
            // just use a BER reader for identifying the positions of the values
            // within this memory segment.
            //
            // Since it's not mutating, any restrictions imposed by CER or DER will
            // still be maintained.
            var reader = new AsnReader(new ReadOnlyMemory<byte>(buffer, start, len), AsnEncodingRules.BER);

            List<(int, int)> positions = new List<(int, int)>();

            int pos = start;

            while (reader.HasData)
            {
                ReadOnlyMemory<byte> encoded = reader.ReadEncodedValue();
                positions.Add((pos, encoded.Length));
                pos += encoded.Length;
            }

            Debug.Assert(pos == end);

            var comparer = new ArrayIndexSetOfValueComparer(buffer);
            positions.Sort(comparer);

            byte[] tmp = CryptoPool.Rent(len);

            pos = 0;

            foreach ((int offset, int length) in positions)
            {
                Buffer.BlockCopy(buffer, offset, tmp, pos, length);
                pos += length;
            }

            Debug.Assert(pos == len);

            Buffer.BlockCopy(tmp, 0, buffer, start, len);
            CryptoPool.Return(tmp, len);
        }

        internal static void Reverse(Span<byte> span)
        {
            int i = 0;
            int j = span.Length - 1;

            while (i < j)
            {
                byte tmp = span[i];
                span[i] = span[j];
                span[j] = tmp;

                i++;
                j--;
            }
        }

        private static void CheckUniversalTag(Asn1Tag? tag, UniversalTagNumber universalTagNumber)
        {
            if (tag != null)
            {
                Asn1Tag value = tag.Value;

                if (value.TagClass == TagClass.Universal && value.TagValue != (int)universalTagNumber)
                {
                    throw new ArgumentException(
                        SR.Argument_UniversalValueIsFixed,
                        nameof(tag));
                }
            }
        }

        private sealed class ArrayIndexSetOfValueComparer : IComparer<(int, int)>
        {
            private readonly byte[] _data;

            public ArrayIndexSetOfValueComparer(byte[] data)
            {
                _data = data;
            }

            public int Compare((int, int) x, (int, int) y)
            {
                (int xOffset, int xLength) = x;
                (int yOffset, int yLength) = y;

                int value =
                    SetOfValueComparer.Instance.Compare(
                        new ReadOnlyMemory<byte>(_data, xOffset, xLength),
                        new ReadOnlyMemory<byte>(_data, yOffset, yLength));

                if (value == 0)
                {
                    // Whichever had the lowest index wins (once sorted, stay sorted)
                    return xOffset - yOffset;
                }

                return value;
            }
        }

        private readonly struct StackFrame : IEquatable<StackFrame>
        {
            public Asn1Tag Tag { get; }
            public int Offset { get; }
            public UniversalTagNumber ItemType { get; }

            internal StackFrame(Asn1Tag tag, int offset, UniversalTagNumber itemType)
            {
                Tag = tag;
                Offset = offset;
                ItemType = itemType;
            }

            public void Deconstruct(out Asn1Tag tag, out int offset, out UniversalTagNumber itemType)
            {
                tag = Tag;
                offset = Offset;
                itemType = ItemType;
            }

            public bool Equals(StackFrame other)
            {
                return Tag.Equals(other.Tag) && Offset == other.Offset && ItemType == other.ItemType;
            }

            public override bool Equals([NotNullWhen(true)] object? obj) => obj is StackFrame other && Equals(other);

            public override int GetHashCode()
            {
                return (Tag, Offset, ItemType).GetHashCode();
            }

            public static bool operator ==(StackFrame left, StackFrame right) => left.Equals(right);

            public static bool operator !=(StackFrame left, StackFrame right) => !left.Equals(right);
        }

        public readonly struct Scope : IDisposable
        {
            private readonly AsnWriter _writer;
            private readonly StackFrame _frame;
            private readonly int _depth;

            internal Scope(AsnWriter writer)
            {
                Debug.Assert(writer._nestingStack != null);

                _writer = writer;
                _frame = _writer._nestingStack.Peek();
                _depth = _writer._nestingStack.Count;
            }

            public void Dispose()
            {
                Debug.Assert(_writer == null || _writer._nestingStack != null);

                if (_writer == null || _writer._nestingStack!.Count == 0)
                {
                    return;
                }

                if (_writer._nestingStack.Peek() == _frame)
                {
                    switch (_frame.ItemType)
                    {
                        case UniversalTagNumber.SetOf:
                            _writer.PopSetOf(_frame.Tag);
                            break;
                        case UniversalTagNumber.Sequence:
                            _writer.PopSequence(_frame.Tag);
                            break;
                        case UniversalTagNumber.OctetString:
                            _writer.PopOctetString(_frame.Tag);
                            break;
                        default:
                            Debug.Fail($"No handler for {_frame.ItemType}");
                            throw new InvalidOperationException();
                    }
                }
                else if (_writer._nestingStack.Count > _depth &&
                    _writer._nestingStack.Contains(_frame))
                {
                    // Another frame was pushed when we got disposed.
                    // Report the imbalance.
                    throw new InvalidOperationException(SR.AsnWriter_PopWrongTag);
                }
            }
        }
    }
}
