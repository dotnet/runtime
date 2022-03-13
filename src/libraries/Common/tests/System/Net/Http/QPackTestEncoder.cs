// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace System.Net.Test.Common
{
    public static class QPackTestEncoder
    {
        public const int MaxVarIntLength = 6;
        public const int MaxPrefixLength = MaxVarIntLength * 2;

        private const QPackFlags FlagsIndexMask = QPackFlags.StaticIndex | QPackFlags.DynamicIndex | QPackFlags.DynamicIndexPostBase;

        public static int EncodePrefix(Span<byte> buffer, int requiredInsertCount, int deltaBase)
        {
            int bytesWritten = 0;

            bytesWritten += EncodeInteger(buffer.Slice(bytesWritten), requiredInsertCount, 0, 0);
            bytesWritten += EncodeInteger(buffer.Slice(bytesWritten), Math.Abs(deltaBase), deltaBase < 0 ? (byte)0x80 : (byte)0, 0x80);

            return bytesWritten;
        }

        public static int EncodeHeader(Span<byte> buffer, int nameValueIdx, QPackFlags flags = QPackFlags.StaticIndex)
        {
            byte prefix, prefixMask;

            switch (flags & FlagsIndexMask)
            {
                case QPackFlags.StaticIndex:
                    prefix = 0b1100_0000;
                    prefixMask = 0b1100_0000;
                    break;
                case QPackFlags.DynamicIndex:
                    prefix = 0b1000_0000;
                    prefixMask = 0b1100_0000;
                    break;
                case QPackFlags.DynamicIndexPostBase:
                    prefix = 0b0001_0000;
                    prefixMask = 0b1111_0000;
                    break;
                default:
                    Debug.Fail($"Invalid {nameof(QPackFlags)}.");
                    throw new Exception();
            }

            return EncodeInteger(buffer, nameValueIdx, prefix, prefixMask);
        }

        public static int EncodeHeader(Span<byte> buffer, int nameIdx, string value, Encoding valueEncoding, QPackFlags flags = QPackFlags.StaticIndex)
        {
            byte prefix, prefixMask;

            switch (flags & FlagsIndexMask)
            {
                case QPackFlags.StaticIndex:
                    prefix = 0b0101_0000;
                    prefixMask = 0b1111_0000;
                    if (flags.HasFlag(QPackFlags.NeverIndexed)) prefix |= 0b0010_0000;
                    break;
                case QPackFlags.DynamicIndex:
                    prefix = 0b0100_0000;
                    prefixMask = 0b1111_0000;
                    if (flags.HasFlag(QPackFlags.NeverIndexed)) prefix |= 0b0010_0000;
                    break;
                case QPackFlags.DynamicIndexPostBase:
                    prefix = 0b0000_0000;
                    prefixMask = 0b1111_1000;
                    if (flags.HasFlag(QPackFlags.NeverIndexed)) prefix |= 0b0000_1000;
                    break;
                default:
                    Debug.Fail($"Invalid {nameof(QPackFlags)}.");
                    throw new Exception();
            }

            int nameLen = EncodeInteger(buffer, nameIdx, prefix, prefixMask);
            int valueLen = EncodeString(buffer.Slice(nameLen), value, valueEncoding, flags.HasFlag(QPackFlags.HuffmanEncodeValue));

            return nameLen + valueLen;
        }

        public static int EncodeHeader(Span<byte> buffer, string name, string value, Encoding valueEncoding, QPackFlags flags = QPackFlags.None)
        {
            byte[] data = Encoding.ASCII.GetBytes(name);
            byte prefix;

            if (!flags.HasFlag(QPackFlags.HuffmanEncodeName))
            {
                prefix = 0b0010_0000;
            }
            else
            {
                int len = HuffmanEncoder.GetEncodedLength(data);

                byte[] huffmanData = new byte[len];
                HuffmanEncoder.Encode(data, huffmanData);

                data = huffmanData;
                prefix = 0b0010_1000;
            }

            if (flags.HasFlag(QPackFlags.NeverIndexed))
            {
                prefix |= 0b0001_0000;
            }

            int bytesGenerated = 0;

            // write name string header.
            bytesGenerated += EncodeInteger(buffer, data.Length, prefix, 0b1111_1000);

            // write name string.
            data.AsSpan().CopyTo(buffer.Slice(bytesGenerated));
            bytesGenerated += data.Length;

            // write value string.
            bytesGenerated += EncodeString(buffer.Slice(bytesGenerated), value, valueEncoding, flags.HasFlag(QPackFlags.HuffmanEncodeValue));

            return bytesGenerated;
        }

        public static int EncodeString(Span<byte> buffer, string value, Encoding valueEncoding, bool huffmanCoded = false)
        {
            return HPackEncoder.EncodeString(value, valueEncoding, buffer, huffmanCoded);
        }

        public static int EncodeInteger(Span<byte> buffer, int value, byte prefix, byte prefixMask)
        {
            return HPackEncoder.EncodeInteger(value, prefix, prefixMask, buffer);
        }

        // from System.Net.Http.QPack.H3StaticTable
        private static readonly Dictionary<int, int> s_statusIndex = new Dictionary<int, int>
        {
            [103] = 24,
            [200] = 25,
            [304] = 26,
            [404] = 27,
            [503] = 28,
            [100] = 63,
            [204] = 64,
            [206] = 65,
            [302] = 66,
            [400] = 67,
            [403] = 68,
            [421] = 69,
            [425] = 70,
            [500] = 71,
        };

        public static int EncodeStatusCode(int statusCode, Span<byte> buffer)
        {
            if (s_statusIndex.TryGetValue(statusCode, out var statusIdx))
            {
                // Indexed Header Field
                return EncodeHeader(buffer, statusIdx);
            }
            else
            {
                // Literal Header Field With Name Reference -- Index of any status present in the table can be used for reference
                return EncodeHeader(buffer, s_statusIndex[100], statusCode.ToString(CultureInfo.InvariantCulture), valueEncoding: null);
            }
        }
    }

    [Flags]
    public enum QPackFlags
    {
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
        /// The header is using an indexed header from the static table.
        /// </summary>
        StaticIndex = 4,

        /// <summary>
        /// The header is using an indexed header from the dynamic table.
        /// </summary>
        DynamicIndex = 8,

        /// <summary>
        /// The header is using an indexed header from the dynamic table, adjusted from a base index.
        /// </summary>
        DynamicIndexPostBase = 16,

        /// <summary>
        /// Intermediaries (such as a proxy) must not index the value when forwarding the header.
        /// </summary>
        NeverIndexed = 32
    }
}
