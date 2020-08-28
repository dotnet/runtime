// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;

namespace System.Net.Test.Common
{
    public static class HPackEncoder
    {
        /// <summary>
        /// Static table indexes are [0..61]. Indexes larger than this are dynamic indexes.
        /// </summary>
        public const int LargestStaticIndex = 61;

        /// <summary>
        /// Dynamic table indexes are [62..], with 62 being the most recently added entry. Indexes smaller than this are static indexes.
        /// </summary>
        public const int SmallestDynamicIndex = LargestStaticIndex + 1;

        /// <summary>
        /// Encodes a dynamic table size update.
        /// </summary>
        /// <param name="newMaximumSize">The new maximum size of the dynamic table. This must be less than or equal to the connection's maximum table size setting, which defaults to 4096 bytes.</param>
        /// <param name="headerBlock">A span to write the encoded header to.</param>
        /// <returns>The number of bytes written to <paramref name="headerBlock"/>.</returns>
        public static int EncodeDynamicTableSizeUpdate(int newMaximumSize, Span<byte> headerBlock)
        {
            return EncodeInteger(newMaximumSize, 0b00100000, 0b11100000, headerBlock);
        }

        /// <summary>
        /// Encodes a header using an index for both name and value.
        /// </summary>
        /// <param name="headerIndex">The header index to encode.</param>
        /// <param name="headerBlock">A span to write the encoded header to.</param>
        /// <returns>The number of bytes written to <paramref name="headerBlock"/>.</returns>
        public static int EncodeHeader(int headerIndex, Span<byte> headerBlock)
        {
            Debug.Assert(headerIndex > 0);
            return EncodeInteger(headerIndex, 0b10000000, 0b10000000, headerBlock);
        }

        /// <summary>
        /// Encodes a header using an indexed name and literal value.
        /// </summary>
        /// <param name="nameIdx">An index of a header containing the name for this header.</param>
        /// <param name="value">A literal value to encode for this header.</param>
        /// <param name="headerBlock">A span to write the encoded header to.</param>
        /// <returns>The number of bytes written to <paramref name="headerBlock"/>.</returns>
        public static int EncodeHeader(int nameIdx, string value, HPackFlags flags, Span<byte> headerBlock)
        {
            Debug.Assert(nameIdx > 0);
            return EncodeHeaderImpl(nameIdx, null, value, valueEncoding: null, flags, headerBlock);
        }

        /// <summary>
        /// Encodes a header using a literal name and value.
        /// </summary>
        /// <param name="name">A literal name to encode for this header.</param>
        /// <param name="value">A literal value to encode for this header.</param>
        /// <param name="headerBlock">A span to write the encoded header to.</param>
        /// <returns>The number of bytes written to <paramref name="headerBlock"/>.</returns>
        public static int EncodeHeader(string name, string value, HPackFlags flags, Span<byte> headerBlock)
        {
            return EncodeHeader(name, value, valueEncoding: null, flags, headerBlock);
        }

        public static int EncodeHeader(string name, string value, Encoding valueEncoding, HPackFlags flags, Span<byte> headerBlock)
        {
            return EncodeHeaderImpl(0, name, value, valueEncoding, flags, headerBlock);
        }

        private static int EncodeHeaderImpl(int nameIdx, string name, string value, Encoding valueEncoding, HPackFlags flags, Span<byte> headerBlock)
        {
            const HPackFlags IndexingMask = HPackFlags.NeverIndexed | HPackFlags.NewIndexed | HPackFlags.WithoutIndexing;

            Debug.Assert((nameIdx != 0) != (name != null), $"Only one of {nameof(nameIdx)} or {nameof(name)} can be used.");
            Debug.Assert(name != null || (flags & HPackFlags.HuffmanEncodeName) == 0, "An indexed name can not be huffman encoded.");

            byte prefix, prefixMask;

            switch (flags & IndexingMask)
            {
                case HPackFlags.WithoutIndexing:
                    prefix = 0;
                    prefixMask = 0b11110000;
                    break;
                case HPackFlags.NewIndexed:
                    prefix = 0b01000000;
                    prefixMask = 0b11000000;
                    break;
                case HPackFlags.NeverIndexed:
                    prefix = 0b00010000;
                    prefixMask = 0b11110000;
                    break;
                default:
                    throw new Exception("invalid indexing flag");
            }

            int bytesGenerated = EncodeInteger(nameIdx, prefix, prefixMask, headerBlock);

            if (name != null)
            {
                bytesGenerated += EncodeString(name, Encoding.ASCII, headerBlock.Slice(bytesGenerated), (flags & HPackFlags.HuffmanEncodeName) != 0);
            }

            bytesGenerated += EncodeString(value, valueEncoding, headerBlock.Slice(bytesGenerated), (flags & HPackFlags.HuffmanEncodeValue) != 0);
            return bytesGenerated;
        }

        public static int EncodeString(string value, Encoding valueEncoding, Span<byte> headerBlock, bool huffmanEncode)
        {
            byte[] data = (valueEncoding ?? Encoding.ASCII).GetBytes(value);
            byte prefix;

            if (!huffmanEncode)
            {
                prefix = 0;
            }
            else
            {
                int len = HuffmanEncoder.GetEncodedLength(data);

                byte[] huffmanData = new byte[len];
                HuffmanEncoder.Encode(data, huffmanData);

                data = huffmanData;
                prefix = 0x80;
            }

            int bytesGenerated = 0;

            bytesGenerated += EncodeInteger(data.Length, prefix, 0x80, headerBlock);

            data.AsSpan().CopyTo(headerBlock.Slice(bytesGenerated));
            bytesGenerated += data.Length;

            return bytesGenerated;
        }

        public static int EncodeInteger(int value, byte prefix, byte prefixMask, Span<byte> headerBlock)
        {
            byte prefixLimit = (byte)(~prefixMask);

            if (value < prefixLimit)
            {
                headerBlock[0] = (byte)(prefix | value);
                return 1;
            }

            headerBlock[0] = (byte)(prefix | prefixLimit);
            int bytesGenerated = 1;

            value -= prefixLimit;

            while (value >= 0x80)
            {
                headerBlock[bytesGenerated] = (byte)((value & 0x7F) | 0x80);
                value >>= 7;
                bytesGenerated++;
            }

            headerBlock[bytesGenerated] = (byte)value;
            bytesGenerated++;

            return bytesGenerated;
        }
    }

    public enum HPackFlags
    {
        /// <summary>
        /// Encodes a header literal without indexing and without huffman encoding.
        /// </summary>
        None = 0,

        /// <summary>
        /// Applies Huffman encoding to the header's name.
        /// </summary>
        HuffmanEncodeName = 1,

        /// <summary>
        /// Applies Huffman encoding to the header's value.
        /// </summary>
        HuffmanEncodeValue = 2,

        /// <summary>
        /// Applies Huffman encoding to both the name and the value of the header.
        /// </summary>
        HuffmanEncode = HuffmanEncodeName | HuffmanEncodeValue,

        /// <summary>
        /// Encode a literal value without adding a new dynamic index. Intermediaries (such as a proxy) are still allowed to index the value when forwarding the header.
        /// </summary>
        WithoutIndexing = 0,

        /// <summary>
        /// Encode a literal value to a new dynamic index.
        /// </summary>
        NewIndexed = 4,

        /// <summary>
        /// Encode a literal value without adding a new dynamic index. Intermediaries (such as a proxy) must not index the value when forwarding the header.
        /// </summary>
        NeverIndexed = 8
    }
}
