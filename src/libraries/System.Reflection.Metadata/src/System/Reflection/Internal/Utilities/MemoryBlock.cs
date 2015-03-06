// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Reflection.Internal
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal unsafe struct MemoryBlock
    {
        internal readonly byte* Pointer;
        internal readonly int Length;

        internal MemoryBlock(byte* buffer, int length)
        {
            Debug.Assert(length >= 0 && (buffer != null || length == 0));
            this.Pointer = buffer;
            this.Length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckBounds(int offset, int byteCount)
        {
            if (unchecked((ulong)(uint)offset + (uint)byteCount) > (ulong)Length)
            {
                ThrowOutOfBounds();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowOutOfBounds()
        {
            throw new BadImageFormatException(MetadataResources.OutOfBoundsRead);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowReferenceOverflow()
        {
            throw new BadImageFormatException(MetadataResources.RowIdOrHeapOffsetTooLarge);
        }

        internal byte[] ToArray()
        {
            return Pointer == null ? null : PeekBytes(0, Length);
        }

        private string GetDebuggerDisplay()
        {
            if (Pointer == null)
            {
                return "<null>";
            }

            int displayedBytes;
            return GetDebuggerDisplay(out displayedBytes);
        }

        internal string GetDebuggerDisplay(out int displayedBytes)
        {
            displayedBytes = Math.Min(Length, 64);
            string result = BitConverter.ToString(PeekBytes(0, displayedBytes));
            if (displayedBytes < Length)
            {
                result += "-...";
            }

            return result;
        }

        internal MemoryBlock GetMemoryBlockAt(int offset, int length)
        {
            CheckBounds(offset, length);
            return new MemoryBlock(Pointer + offset, length);
        }

        internal Byte PeekByte(int offset)
        {
            CheckBounds(offset, sizeof(Byte));
            return Pointer[offset];
        }

        internal Int32 PeekInt32(int offset)
        {
            CheckBounds(offset, sizeof(Int32));
            return *(Int32*)(Pointer + offset);
        }

        internal UInt32 PeekUInt32(int offset)
        {
            CheckBounds(offset, sizeof(UInt32));
            return *(UInt32*)(Pointer + offset);
        }

        /// <summary>
        /// Decodes a compressed integer value starting at offset. 
        /// See Metadata Specification section II.23.2: Blobs and signatures.
        /// </summary>
        /// <param name="offset">Offset to the start of the compressed data.</param>
        /// <param name="numberOfBytesRead">Bytes actually read.</param>
        /// <returns>
        /// Value between 0 and 0x1fffffff, or <see cref="BlobReader.InvalidCompressedInteger"/> if the value encoding is invalid.
        /// </returns>
        internal int PeekCompressedInteger(int offset, out int numberOfBytesRead)
        {
            CheckBounds(offset, 0);

            byte* ptr = Pointer + offset;
            long limit = Length - offset;

            if (limit == 0)
            {
                numberOfBytesRead = 0;
                return BlobReader.InvalidCompressedInteger;
            }

            byte headerByte = ptr[0];
            if ((headerByte & 0x80) == 0)
            {
                numberOfBytesRead = 1;
                return headerByte;
            }
            else if ((headerByte & 0x40) == 0)
            {
                if (limit >= 2)
                {
                    numberOfBytesRead = 2;
                    return ((headerByte & 0x3f) << 8) | ptr[1];
                }
            }
            else if ((headerByte & 0x20) == 0)
            {
                if (limit >= 4)
                {
                    numberOfBytesRead = 4;
                    return ((headerByte & 0x1f) << 24) | (ptr[1] << 16) | (ptr[2] << 8) | ptr[3];
                }
            }

            numberOfBytesRead = 0;
            return BlobReader.InvalidCompressedInteger;
        }

        internal UInt16 PeekUInt16(int offset)
        {
            CheckBounds(offset, sizeof(UInt16));
            return *(UInt16*)(Pointer + offset);
        }

        // When reference has tag bits.
        internal UInt32 PeekTaggedReference(int offset, bool smallRefSize)
        {
            return smallRefSize ? PeekUInt16(offset) : PeekUInt32(offset);
        }

        // When reference has at most 24 bits.
        internal UInt32 PeekReference(int offset, bool smallRefSize)
        {
            if (smallRefSize)
            {
                return PeekUInt16(offset);
            }

            uint value = PeekUInt32(offset);

            if (!TokenTypeIds.IsValidRowId(value))
            {
                ThrowReferenceOverflow();
            }

            return value;
        }

        internal Guid PeekGuid(int offset)
        {
            CheckBounds(offset, sizeof(Guid));
            return *(Guid*)(Pointer + offset);
        }

        internal string PeekUtf16(int offset, int byteCount)
        {
            CheckBounds(offset, byteCount);

            // doesn't allocate a new string if byteCount == 0
            return new string((char*)(Pointer + offset), 0, byteCount / sizeof(char));
        }

        internal string PeekUtf8(int offset, int byteCount)
        {
            CheckBounds(offset, byteCount);
            return Encoding.UTF8.GetString(Pointer + offset, byteCount);
        }

        /// <summary>
        /// Read UTF8 at the given offset up to the given terminator, null terminator, or end-of-block.
        /// </summary>
        /// <param name="offset">Offset in to the block where the UTF8 bytes start.</param>
        /// <param name="prefix">UTF8 encoded prefix to prepend to the bytes at the offset before decoding.</param>
        /// <param name="utf8Decoder">The UTF8 decoder to use that allows user to adjust fallback and/or reuse existing strings without allocating a new one.</param>
        /// <param name="numberOfBytesRead">The number of bytes read, which includes the terminator if we did not hit the end of the block.</param>
        /// <param name="terminator">A character in the ASCII range that marks the end of the string. 
        /// If a value other than '\0' is passed we still stop at the null terminator if encountered first.</param>
        /// <returns>The decoded string.</returns>
        internal string PeekUtf8NullTerminated(int offset, byte[] prefix, MetadataStringDecoder utf8Decoder, out int numberOfBytesRead, char terminator = '\0')
        {
            Debug.Assert(terminator <= 0x7F);
            CheckBounds(offset, 0);
            int length = GetUtf8NullTerminatedLength(offset, out numberOfBytesRead, terminator);
            return EncodingHelper.DecodeUtf8(Pointer + offset, length, prefix, utf8Decoder);
        }

        /// <summary>
        /// Get number of bytes from offset to given terminator, null terminator, or end-of-block (whichever comes first).
        /// Returned length does not include the terminator, but numberOfBytesRead out parameter does.
        /// </summary>
        /// <param name="offset">Offset in to the block where the UTF8 bytes start.</param>
        /// <param name="terminator">A character in the ASCII range that marks the end of the string. 
        /// If a value other than '\0' is passed we still stop at the null terminator if encountered first.</param>
        /// <param name="numberOfBytesRead">The number of bytes read, which includes the terminator if we did not hit the end of the block.</param>
        /// <returns>Length (byte count) not including terminator.</returns>
        internal int GetUtf8NullTerminatedLength(int offset, out int numberOfBytesRead, char terminator = '\0')
        {
            CheckBounds(offset, 0);

            Debug.Assert(terminator <= 0x7f);

            byte* start = Pointer + offset;
            byte* end = Pointer + Length;
            byte* current = start;

            while (current < end)
            {
                byte b = *current;
                if (b == 0 || b == terminator)
                {
                    break;
                }

                current++;
            }

            int length = (int)(current - start);
            numberOfBytesRead = length;
            if (current < end)
            {
                // we also read the terminator
                numberOfBytesRead++;
            }

            return length;
        }

        internal int Utf8NullTerminatedOffsetOfAsciiChar(int startOffset, char asciiChar)
        {
            CheckBounds(startOffset, 0);

            Debug.Assert(asciiChar != 0 && asciiChar <= 0x7f);

            for (int i = startOffset; i < Length; i++)
            {
                byte b = Pointer[i];

                if (b == 0)
                {
                    break;
                }

                if (b == asciiChar)
                {
                    return i;
                }
            }

            return -1;
        }

        // comparison stops at null terminator, terminator parameter, or end-of-block -- whichever comes first.
        internal bool Utf8NullTerminatedEquals(int offset, string text, MetadataStringDecoder utf8Decoder, char terminator = '\0')
        {
            FastComparisonResult result = Utf8NullTerminatedFastCompare(offset, text, terminator);

            switch (result)
            {
                case FastComparisonResult.Equal:
                    return true;

                case FastComparisonResult.IsPrefix:
                case FastComparisonResult.Unequal:
                    return false;

                default:
                    Debug.Assert(result == FastComparisonResult.Inconclusive);
                    int bytesRead;
                    string decoded = PeekUtf8NullTerminated(offset, null, utf8Decoder, out bytesRead, terminator);
                    return decoded.Equals(text, StringComparison.Ordinal);
            }
        }

        // comparison stops at null terminator, terminator parameter, or end-of-block -- whichever comes first.
        internal bool Utf8NullTerminatedStartsWith(int offset, string text, MetadataStringDecoder utf8Decoder, char terminator = '\0')
        {
            FastComparisonResult result = Utf8NullTerminatedFastCompare(offset, text, terminator);

            switch (result)
            {
                case FastComparisonResult.Equal:
                case FastComparisonResult.IsPrefix:
                    return true;

                case FastComparisonResult.Unequal:
                    return false;

                default:
                    Debug.Assert(result == FastComparisonResult.Inconclusive);
                    int bytesRead;
                    string decoded = PeekUtf8NullTerminated(offset, null, utf8Decoder, out bytesRead, terminator);
                    return decoded.StartsWith(text, StringComparison.Ordinal);
            }
        }

        internal enum FastComparisonResult
        {
            Equal,
            IsPrefix,
            Unequal,
            Inconclusive
        }

        // comparison stops at null terminator, terminator parameter, or end-of-block -- whichever comes first.
        private FastComparisonResult Utf8NullTerminatedFastCompare(int offset, string text, char terminator = '\0')
        {
            CheckBounds(offset, 0);

            Debug.Assert(terminator <= 0x7F);

            byte* startPointer = Pointer + offset;
            byte* endPointer = Pointer + Length;
            byte* currentPointer = startPointer;

            int currentIndex = 0;
            while (currentIndex < text.Length && currentPointer != endPointer)
            {
                byte currentByte = *currentPointer++;
                if (currentByte == 0 || currentByte == terminator)
                {
                    break;
                }

                char currentChar = text[currentIndex++];
                if ((currentByte & 0x80) == 0)
                {
                    // current byte is in ascii range.
                    //  --> strings are unequal if current char and current byte are unequal
                    if (currentChar != currentByte)
                    {
                        return FastComparisonResult.Unequal;
                    }
                }
                else if (currentChar <= 0x7F)
                {
                    // current byte is not in ascii range, but current char is.
                    // --> strings are unequal.
                    return FastComparisonResult.Unequal;
                }
                else
                {
                    // uncommon non-ascii case --> fall back to slow allocating comparison.
                    return FastComparisonResult.Inconclusive;
                }
            }

            if (currentIndex != text.Length)
            {
                return FastComparisonResult.Unequal;
            }

            if (currentPointer != endPointer && *currentPointer != 0 && *currentPointer != terminator)
            {
                return FastComparisonResult.IsPrefix;
            }

            return FastComparisonResult.Equal;
        }

        // comparison stops at null terminator, terminator parameter, or end-of-block -- whichever comes first.
        internal bool Utf8NullTerminatedStringStartsWithAsciiPrefix(int offset, string asciiPrefix)
        {
            // Assumes stringAscii only contains ASCII characters and no nil characters.

            CheckBounds(offset, 0);

            // Make sure that we won't read beyond the block even if the block doesn't end with 0 byte.
            if (asciiPrefix.Length > Length - offset)
            {
                return false;
            }

            byte* p = Pointer + offset;

            for (int i = 0; i < asciiPrefix.Length; i++)
            {
                Debug.Assert((int)asciiPrefix[i] > 0 && (int)asciiPrefix[i] <= 0x7f);

                if (asciiPrefix[i] != *p)
                {
                    return false;
                }

                p++;
            }

            return true;
        }

        internal int CompareUtf8NullTerminatedStringWithAsciiString(int offset, string asciiString)
        {
            // Assumes stringAscii only contains ASCII characters and no nil characters.

            CheckBounds(offset, 0);

            byte* p = Pointer + offset;
            int limit = Length - offset;

            for (int i = 0; i < asciiString.Length; i++)
            {
                Debug.Assert((int)asciiString[i] > 0 && (int)asciiString[i] <= 0x7f);

                if (i > limit)
                {
                    // Heap value is shorter.
                    return -1;
                }

                if (*p != asciiString[i])
                {
                    // If we hit the end of the heap value (*p == 0)
                    // the heap value is shorter than the string, so we return negative value. 
                    return *p - asciiString[i];
                }

                p++;
            }

            // Either the heap value name matches exactly the given string or 
            // it is longer so it is considered "greater".
            return (*p == 0) ? 0 : +1;
        }

        internal byte[] PeekBytes(int offset, int byteCount)
        {
            CheckBounds(offset, byteCount);

            if (byteCount == 0)
            {
                return EmptyArray<byte>.Instance;
            }

            byte[] result = new byte[byteCount];
            Marshal.Copy((IntPtr)(Pointer + offset), result, 0, byteCount);
            return result;
        }

        internal int IndexOf(byte b, int start)
        {
            CheckBounds(start, 0);

            byte* p = Pointer + start;
            byte* end = Pointer + Length;
            while (p < end)
            {
                if (*p == b)
                {
                    return (int)(p - Pointer);
                }

                p++;
            }

            return -1;
        }

        // same as Array.BinarySearch, but without using IComparer
        internal int BinarySearch(string[] asciiKeys, int offset)
        {
            var low = 0;
            var high = asciiKeys.Length - 1;

            while (low <= high)
            {
                var middle = low + ((high - low) >> 1);
                var midValue = asciiKeys[middle];

                int comparison = CompareUtf8NullTerminatedStringWithAsciiString(offset, midValue);
                if (comparison == 0)
                {
                    return middle;
                }

                if (comparison < 0)
                {
                    high = middle - 1;
                }
                else
                {
                    low = middle + 1;
                }
            }

            return ~low;
        }

        // Returns row number [0..RowCount) or -1 if not found.
        internal int BinarySearchForSlot(
            uint rowCount,
            int rowSize,
            int referenceOffset,
            uint referenceValue,
            bool isReferenceSmall)
        {
            int startRowNumber = 0;
            int endRowNumber = (int)rowCount - 1;
            uint startValue = PeekReference(startRowNumber * rowSize + referenceOffset, isReferenceSmall);
            uint endValue = PeekReference(endRowNumber * rowSize + referenceOffset, isReferenceSmall);
            if (endRowNumber == 1)
            {
                if (referenceValue >= endValue)
                {
                    return endRowNumber;
                }

                return startRowNumber;
            }

            while ((endRowNumber - startRowNumber) > 1)
            {
                if (referenceValue <= startValue)
                {
                    return referenceValue == startValue ? startRowNumber : startRowNumber - 1;
                }
                else if (referenceValue >= endValue)
                {
                    return referenceValue == endValue ? endRowNumber : endRowNumber + 1;
                }

                int midRowNumber = (startRowNumber + endRowNumber) / 2;
                uint midReferenceValue = PeekReference(midRowNumber * rowSize + referenceOffset, isReferenceSmall);
                if (referenceValue > midReferenceValue)
                {
                    startRowNumber = midRowNumber;
                    startValue = midReferenceValue;
                }
                else if (referenceValue < midReferenceValue)
                {
                    endRowNumber = midRowNumber;
                    endValue = midReferenceValue;
                }
                else
                {
                    return midRowNumber;
                }
            }

            return startRowNumber;
        }

        // Returns row number [0..RowCount) or -1 if not found.
        internal int BinarySearchReference(
            uint rowCount,
            int rowSize,
            int referenceOffset,
            uint referenceValue,
            bool isReferenceSmall)
        {
            int startRowNumber = 0;
            int endRowNumber = (int)rowCount - 1;
            while (startRowNumber <= endRowNumber)
            {
                int midRowNumber = (startRowNumber + endRowNumber) / 2;
                uint midReferenceValue = PeekReference(midRowNumber * rowSize + referenceOffset, isReferenceSmall);
                if (referenceValue > midReferenceValue)
                {
                    startRowNumber = midRowNumber + 1;
                }
                else if (referenceValue < midReferenceValue)
                {
                    endRowNumber = midRowNumber - 1;
                }
                else
                {
                    return midRowNumber;
                }
            }

            return -1;
        }

        // Row number [0, ptrTable.Length) or -1 if not found.
        internal int BinarySearchReference(
            uint[] ptrTable,
            int rowSize,
            int referenceOffset,
            uint referenceValue,
            bool isReferenceSmall)
        {
            int startRowNumber = 0;
            int endRowNumber = ptrTable.Length - 1;
            while (startRowNumber <= endRowNumber)
            {
                int midRowNumber = (startRowNumber + endRowNumber) / 2;
                uint midReferenceValue = PeekReference(((int)ptrTable[midRowNumber] - 1) * rowSize + referenceOffset, isReferenceSmall);
                if (referenceValue > midReferenceValue)
                {
                    startRowNumber = midRowNumber + 1;
                }
                else if (referenceValue < midReferenceValue)
                {
                    endRowNumber = midRowNumber - 1;
                }
                else
                {
                    return midRowNumber;
                }
            }

            return -1;
        }

        /// <summary>
        /// Calculates a range of rows that have specified value in the specified column in a table that is sorted by that column.
        /// </summary>
        internal void BinarySearchReferenceRange(
            uint rowCount,
            int rowSize,
            int referenceOffset,
            uint referenceValue,
            bool isReferenceSmall,
            out int startRowNumber, // [0, rowCount) or -1
            out int endRowNumber)   // [0, rowCount) or -1
        {
            int foundRowNumber = BinarySearchReference(
                rowCount,
                rowSize,
                referenceOffset,
                referenceValue,
                isReferenceSmall
            );

            if (foundRowNumber == -1)
            {
                startRowNumber = -1;
                endRowNumber = -1;
                return;
            }

            startRowNumber = foundRowNumber;
            while (startRowNumber > 0 &&
                   PeekReference((startRowNumber - 1) * rowSize + referenceOffset, isReferenceSmall) == referenceValue)
            {
                startRowNumber--;
            }

            endRowNumber = foundRowNumber;
            while (endRowNumber + 1 < rowCount &&
                   PeekReference((endRowNumber + 1) * rowSize + referenceOffset, isReferenceSmall) == referenceValue)
            {
                endRowNumber++;
            }
        }

        /// <summary>
        /// Calculates a range of rows that have specified value in the specified column in a table that is sorted by that column.
        /// </summary>
        internal void BinarySearchReferenceRange(
            uint[] ptrTable,
            int rowSize,
            int referenceOffset,
            uint referenceValue,
            bool isReferenceSmall,
            out int startRowNumber, // [0, ptrTable.Length) or -1
            out int endRowNumber)   // [0, ptrTable.Length) or -1
        {
            int foundRowNumber = BinarySearchReference(
                ptrTable,
                rowSize,
                referenceOffset,
                referenceValue,
                isReferenceSmall
            );

            if (foundRowNumber == -1)
            {
                startRowNumber = -1;
                endRowNumber = -1;
                return;
            }

            startRowNumber = foundRowNumber;
            while (startRowNumber > 0 &&
                   PeekReference(((int)ptrTable[startRowNumber - 1] - 1) * rowSize + referenceOffset, isReferenceSmall) == referenceValue)
            {
                startRowNumber--;
            }

            endRowNumber = foundRowNumber;
            while (endRowNumber + 1 < ptrTable.Length &&
                   PeekReference(((int)ptrTable[endRowNumber + 1] - 1) * rowSize + referenceOffset, isReferenceSmall) == referenceValue)
            {
                endRowNumber++;
            }
        }

        // Always RowNumber....
        internal int LinearSearchReference(
            int rowSize,
            int referenceOffset,
            uint referenceValue,
            bool isReferenceSmall)
        {
            int currOffset = referenceOffset;
            int totalSize = this.Length;
            while (currOffset < totalSize)
            {
                uint currReference = PeekReference(currOffset, isReferenceSmall);
                if (currReference == referenceValue)
                {
                    return currOffset / rowSize;
                }

                currOffset += rowSize;
            }

            return -1;
        }

        internal bool IsOrderedByReferenceAscending(
            int rowSize,
            int referenceOffset,
            bool isReferenceSmall)
        {
            int offset = referenceOffset;
            int totalSize = this.Length;

            uint previous = 0;
            while (offset < totalSize)
            {
                uint current = PeekReference(offset, isReferenceSmall);
                if (current < previous)
                {
                    return false;
                }

                previous = current;
                offset += rowSize;
            }

            return true;
        }

        internal uint[] BuildPtrTable(
            int numberOfRows,
            int rowSize,
            int referenceOffset,
            bool isReferenceSmall)
        {
            uint[] ptrTable = new uint[numberOfRows];
            uint[] unsortedReferences = new uint[numberOfRows];

            for (int i = 0; i < ptrTable.Length; i++)
            {
                ptrTable[i] = (uint)(i + 1);
            }

            ReadColumn(unsortedReferences, rowSize, referenceOffset, isReferenceSmall);
            Array.Sort(ptrTable, (uint a, uint b) => { return unsortedReferences[a - 1].CompareTo(unsortedReferences[b - 1]); });
            return ptrTable;
        }

        private void ReadColumn(
            uint[] result,
            int rowSize,
            int referenceOffset,
            bool isReferenceSmall)
        {
            int offset = referenceOffset;
            int totalSize = this.Length;

            int i = 0;
            while (offset < totalSize)
            {
                result[i] = PeekTaggedReference(offset, isReferenceSmall);
                offset += rowSize;
                i++;
            }

            Debug.Assert(i == result.Length);
        }

        internal bool PeekHeapValueOffsetAndSize(int index, out int offset, out int size)
        {
            int bytesRead;
            int numberOfBytes = PeekCompressedInteger(index, out bytesRead);
            if (numberOfBytes == BlobReader.InvalidCompressedInteger)
            {
                offset = 0;
                size = 0;
                return false;
            }

            offset = index + bytesRead;
            size = numberOfBytes;
            return true;
        }
    }
}
