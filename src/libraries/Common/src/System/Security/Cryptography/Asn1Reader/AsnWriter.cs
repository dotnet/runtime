// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Enable CHECK_ACCURATE_ENSURE to ensure that the AsnWriter is not ever
// abusing the normal EnsureWriteCapacity + ArrayPool behaviors of rounding up.
//#define CHECK_ACCURATE_ENSURE

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    /// <summary>
    ///   A writer for BER-, CER-, and DER-encoded ASN.1 data.
    /// </summary>
    internal sealed partial class AsnWriter : IDisposable
    {
        private byte[] _buffer;
        private int _offset;
        private Stack<(Asn1Tag,int,UniversalTagNumber)> _nestingStack;

        /// <summary>
        ///   The <see cref="AsnEncodingRules"/> in use by this writer.
        /// </summary>
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
        ///   Release the resources held by this writer.
        /// </summary>
        public void Dispose()
        {
            _nestingStack = null;

            if (_buffer != null)
            {
                Array.Clear(_buffer, 0, _offset);
#if !CHECK_ACCURATE_ENSURE
                ArrayPool<byte>.Shared.Return(_buffer);
#endif
                _buffer = null;
            }

            _offset = -1;
        }

        /// <summary>
        ///   Reset the writer to have no data, without releasing resources.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        public void Reset()
        {
            CheckDisposed();

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
        ///   The number of bytes that would be written by <see cref="TryEncode"/>, or -1
        ///   if a <see cref="PushSequence()"/> or <see cref="PushSetOf()"/> has not been completed.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        public int GetEncodedLength()
        {
            CheckDisposed();

            if ((_nestingStack?.Count ?? 0) != 0)
            {
                return -1;
            }

            return _offset;
        }

        /// <summary>
        ///   Write the encoded representation of the data to <paramref name="destination"/>.
        /// </summary>
        /// <param name="destination">The buffer in which to write.</param>
        /// <param name="bytesWritten">
        ///   On success, receives the number of bytes written to <paramref name="destination"/>.
        /// </param>
        /// <returns>
        ///   <c>true</c> if the encode succeeded,
        ///   <c>false</c> if <paramref name="destination"/> is too small.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   A <see cref="PushSequence()"/> or <see cref="PushSetOf()"/> has not been closed via
        ///   <see cref="PopSequence()"/> or <see cref="PopSetOf()"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        public bool TryEncode(Span<byte> destination, out int bytesWritten)
        {
            CheckDisposed();

            if ((_nestingStack?.Count ?? 0) != 0)
                throw new InvalidOperationException(SR.Cryptography_AsnWriter_EncodeUnbalancedStack);

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
        ///   Return a new array containing the encoded value.
        /// </summary>
        /// <returns>A precisely-sized array containing the encoded value.</returns>
        /// <exception cref="InvalidOperationException">
        ///   A <see cref="PushSequence()"/> or <see cref="PushSetOf()"/> has not been closed via
        ///   <see cref="PopSequence()"/> or <see cref="PopSetOf()"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        public byte[] Encode()
        {
            CheckDisposed();

            if ((_nestingStack?.Count ?? 0) != 0)
            {
                throw new InvalidOperationException(SR.Cryptography_AsnWriter_EncodeUnbalancedStack);
            }

            if (_offset == 0)
            {
                return Array.Empty<byte>();
            }

            // If the stack is closed out then everything is a definite encoding (BER, DER) or a
            // required indefinite encoding (CER). So we're correctly sized up, and ready to copy.
            return _buffer.AsSpan(0, _offset).ToArray();
        }

        internal ReadOnlySpan<byte> EncodeAsSpan()
        {
            CheckDisposed();

            if ((_nestingStack?.Count ?? 0) != 0)
            {
                throw new InvalidOperationException(SR.Cryptography_AsnWriter_EncodeUnbalancedStack);
            }

            if (_offset == 0)
            {
                return ReadOnlySpan<byte>.Empty;
            }

            // If the stack is closed out then everything is a definite encoding (BER, DER) or a
            // required indefinite encoding (CER). So we're correctly sized up, and ready to copy.
            return new ReadOnlySpan<byte>(_buffer, 0, _offset);
        }

        private void CheckDisposed()
        {
            if (_offset < 0)
            {
                throw new ObjectDisposedException(nameof(AsnWriter));
            }
        }

        private void EnsureWriteCapacity(int pendingCount)
        {
            CheckDisposed();

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
                    Buffer.BlockCopy(_buffer, 0, newBytes, 0, _offset);
                }
#else
                const int BlockSize = 1024;
                // While the ArrayPool may have similar logic, make sure we don't run into a lot of
                // "grow a little" by asking in 1k steps.
                int blocks = checked(_offset + pendingCount + (BlockSize - 1)) / BlockSize;
                var localPool = ArrayPool<byte>.Shared;
                byte[] newBytes = localPool.Rent(BlockSize * blocks);

                if (_buffer != null)
                {
                    Buffer.BlockCopy(_buffer, 0, newBytes, 0, _offset);
                    Array.Clear(_buffer, 0, _offset);
                    localPool.Return(_buffer);
                }
#endif

#if DEBUG
                // Ensure no "implicit 0" is happening
                for (int i = _offset; i < newBytes.Length; i++)
                {
                    newBytes[i] ^= 0xFF;
                }
#endif

                _buffer = newBytes;
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
                throw new CryptographicException();
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
        ///   Write a single value which has already been encoded.
        /// </summary>
        /// <param name="preEncodedValue">The value to write.</param>
        /// <remarks>
        ///   This method only checks that the tag and length are encoded according to the current ruleset,
        ///   and that the end of the value is the end of the input. The contents are not evaluated for
        ///   semantic meaning.
        /// </remarks>
        /// <exception cref="CryptographicException">
        ///   <paramref name="preEncodedValue"/> could not be read under the current encoding rules --OR--
        ///   <paramref name="preEncodedValue"/> has data beyond the end of the first value
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        public unsafe void WriteEncodedValue(ReadOnlySpan<byte> preEncodedValue)
        {
            CheckDisposed();

            fixed (byte* ptr = &MemoryMarshal.GetReference(preEncodedValue))
            {
                using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, preEncodedValue.Length))
                {
                    WriteEncodedValue(manager.Memory);
                }
            }
        }

        private void WriteEncodedValue(ReadOnlyMemory<byte> preEncodedValue)
        {
            AsnReader reader = new AsnReader(preEncodedValue, RuleSet);

            // Is it legal under the current rules?
            ReadOnlyMemory<byte> parsedBack = reader.ReadEncodedValue();

            if (reader.HasData)
            {
                throw new ArgumentException(
                    SR.Cryptography_WriteEncodedValue_OneValueAtATime,
                    nameof(preEncodedValue));
            }

            Debug.Assert(parsedBack.Length == preEncodedValue.Length);

            EnsureWriteCapacity(preEncodedValue.Length);
            preEncodedValue.Span.CopyTo(_buffer.AsSpan(_offset));
            _offset += preEncodedValue.Length;
        }

        // T-REC-X.690-201508 sec 8.1.5
        private void WriteEndOfContents()
        {
            EnsureWriteCapacity(2);
            _buffer[_offset++] = 0;
            _buffer[_offset++] = 0;
        }

        private void PushTag(Asn1Tag tag, UniversalTagNumber tagType)
        {
            CheckDisposed();

            if (_nestingStack == null)
            {
                _nestingStack = new Stack<(Asn1Tag,int,UniversalTagNumber)>();
            }

            Debug.Assert(tag.IsConstructed);
            WriteTag(tag);
            _nestingStack.Push((tag, _offset, tagType));
            // Indicate that the length is indefinite.
            // We'll come back and clean this up (as appropriate) in PopTag.
            WriteLength(-1);
        }

        private void PopTag(Asn1Tag tag, UniversalTagNumber tagType, bool sortContents=false)
        {
            CheckDisposed();

            if (_nestingStack == null || _nestingStack.Count == 0)
            {
                throw new InvalidOperationException(SR.Cryptography_AsnWriter_PopWrongTag);
            }

            (Asn1Tag stackTag, int lenOffset, UniversalTagNumber stackTagType) = _nestingStack.Peek();

            Debug.Assert(tag.IsConstructed);
            if (stackTag != tag || stackTagType != tagType)
            {
                throw new InvalidOperationException(SR.Cryptography_AsnWriter_PopWrongTag);
            }

            _nestingStack.Pop();

            if (sortContents)
            {
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
            if (RuleSet == AsnEncodingRules.CER)
            {
                WriteEndOfContents();
                return;
            }

            int containedLength = _offset - 1 - lenOffset;
            Debug.Assert(containedLength >= 0);

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
            int start = lenOffset + 1;
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

            ArrayPool<byte> localPool = ArrayPool<byte>.Shared;
            byte[] tmp = localPool.Rent(len);

            pos = 0;

            foreach ((int offset, int length) in positions)
            {
                Buffer.BlockCopy(buffer, offset, tmp, pos, length);
                pos += length;
            }

            Debug.Assert(pos == len);

            Buffer.BlockCopy(tmp, 0, buffer, start, len);
            Array.Clear(tmp, 0, len);
            localPool.Return(tmp);
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

        private static void CheckUniversalTag(Asn1Tag tag, UniversalTagNumber universalTagNumber)
        {
            if (tag.TagClass == TagClass.Universal && tag.TagValue != (int)universalTagNumber)
            {
                throw new ArgumentException(
                    SR.Cryptography_Asn_UniversalValueIsFixed,
                    nameof(tag));
            }
        }

        private class ArrayIndexSetOfValueComparer : IComparer<(int, int)>
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
    }
}
