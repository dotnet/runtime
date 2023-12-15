// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace System.Text
{
    public static partial class Ascii
    {
        /// <summary>
        /// Trims all leading and trailing ASCII whitespaces from the buffer.
        /// </summary>
        /// <param name="value">The ASCII buffer.</param>
        /// <returns>The Range of the untrimmed data.</returns>
        public static Range Trim(ReadOnlySpan<byte> value) => TrimHelper(value, TrimType.Both);

        /// <inheritdoc cref="Trim(ReadOnlySpan{byte})"/>
        public static Range Trim(ReadOnlySpan<char> value) => TrimHelper(value, TrimType.Both);

        /// <summary>
        /// Trims all leading ASCII whitespaces from the buffer.
        /// </summary>
        /// <param name="value">The ASCII buffer.</param>
        /// <returns>The Range of the untrimmed data.</returns>
        public static Range TrimStart(ReadOnlySpan<byte> value) => TrimHelper(value, TrimType.Head);

        /// <inheritdoc cref="TrimStart(ReadOnlySpan{byte})"/>
        public static Range TrimStart(ReadOnlySpan<char> value) => TrimHelper(value, TrimType.Head);

        /// <summary>
        /// Trims all trailing ASCII whitespaces from the buffer.
        /// </summary>
        /// <param name="value">The ASCII buffer.</param>
        /// <returns>The Range of the untrimmed data.</returns>
        public static Range TrimEnd(ReadOnlySpan<byte> value) => TrimHelper(value, TrimType.Tail);

        /// <inheritdoc cref="TrimEnd(ReadOnlySpan{byte})"/>
        public static Range TrimEnd(ReadOnlySpan<char> value) => TrimHelper(value, TrimType.Tail);

        private static Range TrimHelper<T>(ReadOnlySpan<T> value, TrimType trimType)
            where T : unmanaged, IBinaryInteger<T>
        {
            const uint TrimMask =
                (1u << (0x09 - 1))
                | (1u << (0x0A - 1))
                | (1u << (0x0B - 1))
                | (1u << (0x0C - 1))
                | (1u << (0x0D - 1))
                | (1u << (0x20 - 1));

            int start = 0;
            if ((trimType & TrimType.Head) != 0)
            {
                for (; start < value.Length; start++)
                {
                    uint elementValue = uint.CreateTruncating(value[start]);
                    if ((elementValue > 0x20) || ((TrimMask & (1u << ((int)elementValue - 1))) == 0))
                    {
                        break;
                    }
                }
            }

            int end = value.Length - 1;
            if ((trimType & TrimType.Tail) != 0)
            {
                for (; start <= end; end--)
                {
                    uint elementValue = uint.CreateTruncating(value[end]);
                    if ((elementValue > 0x20) || ((TrimMask & (1u << ((int)elementValue - 1))) == 0))
                    {
                        break;
                    }
                }
            }

            return start..(end + 1);
        }
    }
}
