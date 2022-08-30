// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Text;

namespace System.Buffers.Text
{
    public static partial class Ascii
    {
        public static Range Trim(ReadOnlySpan<byte> value) => TrimHelper(value, TrimType.Both);
        public static Range Trim(ReadOnlySpan<char> value) => TrimHelper(value, TrimType.Both);
        public static Range TrimStart(ReadOnlySpan<byte> value) => TrimHelper(value, TrimType.Head);
        public static Range TrimStart(ReadOnlySpan<char> value) => TrimHelper(value, TrimType.Head);
        public static Range TrimEnd(ReadOnlySpan<byte> value) => TrimHelper(value, TrimType.Tail);
        public static Range TrimEnd(ReadOnlySpan<char> value) => TrimHelper(value, TrimType.Tail);

        private static Range TrimHelper<T>(ReadOnlySpan<T> value, TrimType trimType)
            where T : unmanaged, IBinaryInteger<T>
        {
            const uint trimMask =
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
                    if ((elementValue > 0x20) || ((trimMask & (1u << ((int)elementValue - 1))) == 0))
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
                    if ((elementValue > 0x20) || ((trimMask & (1u << ((int)elementValue - 1))) == 0))
                    {
                        break;
                    }
                }
            }

            return start..(end + 1);
        }
    }
}
