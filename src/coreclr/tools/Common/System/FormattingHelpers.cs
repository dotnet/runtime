// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.CompilerServices;

namespace System
{
    internal static class FormattingHelpers
    {
        public static string ToStringInvariant<T>(this T value) where T : IConvertible
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string ToStringInvariant<T>(this T value, string format) where T : IFormattable
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountDigits(uint value)
        {
            // Algorithm based on https://lemire.me/blog/2021/06/03/computing-the-number-of-digits-of-an-integer-even-faster.
            ReadOnlySpan<long> table =
            [
                4294967296, 8589934582, 8589934582, 8589934582, 12884901788, 12884901788, 12884901788, 17179868184,
                17179868184, 17179868184, 21474826480, 21474826480, 21474826480, 21474826480, 25769703776, 25769703776,
                25769703776, 30063771072, 30063771072, 30063771072, 34349738368, 34349738368, 34349738368, 34349738368,
                38554705664, 38554705664, 38554705664, 41949672960, 41949672960, 41949672960, 42949672960, 42949672960,
            ];
            long tableValue = table[(int)uint.Log2(value)];
            return (int)((value + tableValue) >> 32);
        }
    }
}
