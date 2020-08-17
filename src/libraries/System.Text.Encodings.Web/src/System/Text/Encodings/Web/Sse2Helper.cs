// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace System.Text.Encodings.Web
{
    internal static class Sse2Helper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<short> CreateEscapingMask_UnsafeRelaxedJavaScriptEncoder(Vector128<short> sourceValue)
        {
            Debug.Assert(Sse2.IsSupported);

            // Anything in the control characters range, and anything above short.MaxValue but less than or equal char.MaxValue
            // That's because anything between 32768 and 65535 (inclusive) will overflow and become negative.
            Vector128<short> mask = Sse2.CompareLessThan(sourceValue, s_spaceMaskInt16);

            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_quotationMarkMaskInt16));
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_reverseSolidusMaskInt16));

            // Anything above the ASCII range, and also including the leftover control character in the ASCII range - 0x7F
            // When this method is called with only ASCII data, 0x7F is the only value that would meet this comparison.
            // However, when called from "Default", the source could contain characters outside the ASCII range.
            mask = Sse2.Or(mask, Sse2.CompareGreaterThan(sourceValue, s_tildeMaskInt16));

            return mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<sbyte> CreateEscapingMask_UnsafeRelaxedJavaScriptEncoder(Vector128<sbyte> sourceValue)
        {
            Debug.Assert(Sse2.IsSupported);

            // Anything in the control characters range (except 0x7F), and anything above sbyte.MaxValue but less than or equal byte.MaxValue
            // That's because anything between 128 and 255 (inclusive) will overflow and become negative.
            Vector128<sbyte> mask = Sse2.CompareLessThan(sourceValue, s_spaceMaskSByte);

            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_quotationMarkMaskSByte));
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_reverseSolidusMaskSByte));

            // Leftover control character in the ASCII range - 0x7F
            // Since we are dealing with sbytes, 0x7F is the only value that would meet this comparison.
            mask = Sse2.Or(mask, Sse2.CompareGreaterThan(sourceValue, s_tildeMaskSByte));

            return mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<sbyte> CreateEscapingMask_DefaultJavaScriptEncoderBasicLatin(Vector128<sbyte> sourceValue)
        {
            Debug.Assert(Sse2.IsSupported);

            Vector128<sbyte> mask = CreateEscapingMask_UnsafeRelaxedJavaScriptEncoder(sourceValue);

            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_ampersandMaskSByte));
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_apostropheMaskSByte));
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_plusSignMaskSByte));
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_lessThanSignMaskSByte));
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_greaterThanSignMaskSByte));
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_graveAccentMaskSByte));

            return mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<short> CreateAsciiMask(Vector128<short> sourceValue)
        {
            Debug.Assert(Sse2.IsSupported);

            // Anything above short.MaxValue but less than or equal char.MaxValue
            // That's because anything between 32768 and 65535 (inclusive) will overflow and become negative.
            Vector128<short> mask = Sse2.CompareLessThan(sourceValue, s_nullMaskInt16);

            // Anything above the ASCII range
            mask = Sse2.Or(mask, Sse2.CompareGreaterThan(sourceValue, s_maxAsciiCharacterMaskInt16));

            return mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsNonAsciiByte(Vector128<sbyte> value)
        {
            Debug.Assert(Sse2.IsSupported);
            int mask = Sse2.MoveMask(value);
            return mask != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndexOfFirstNonAsciiByte(Vector128<byte> value)
        {
            Debug.Assert(Sse2.IsSupported);
            int mask = Sse2.MoveMask(value);
            int index = BitOperations.TrailingZeroCount(mask);
            Debug.Assert((mask != 0) ? index < 16 : index >= 16);
            return index;
        }

        private static readonly Vector128<short> s_nullMaskInt16 = Vector128<short>.Zero;
        private static readonly Vector128<short> s_spaceMaskInt16 = Vector128.Create((short)' ');
        private static readonly Vector128<short> s_quotationMarkMaskInt16 = Vector128.Create((short)'"');
        private static readonly Vector128<short> s_reverseSolidusMaskInt16 = Vector128.Create((short)'\\');
        private static readonly Vector128<short> s_tildeMaskInt16 = Vector128.Create((short)'~');
        private static readonly Vector128<short> s_maxAsciiCharacterMaskInt16 = Vector128.Create((short)0x7F); // Delete control character

        private static readonly Vector128<sbyte> s_spaceMaskSByte = Vector128.Create((sbyte)' ');
        private static readonly Vector128<sbyte> s_quotationMarkMaskSByte = Vector128.Create((sbyte)'"');
        private static readonly Vector128<sbyte> s_ampersandMaskSByte = Vector128.Create((sbyte)'&');
        private static readonly Vector128<sbyte> s_apostropheMaskSByte = Vector128.Create((sbyte)'\'');
        private static readonly Vector128<sbyte> s_plusSignMaskSByte = Vector128.Create((sbyte)'+');
        private static readonly Vector128<sbyte> s_lessThanSignMaskSByte = Vector128.Create((sbyte)'<');
        private static readonly Vector128<sbyte> s_greaterThanSignMaskSByte = Vector128.Create((sbyte)'>');
        private static readonly Vector128<sbyte> s_reverseSolidusMaskSByte = Vector128.Create((sbyte)'\\');
        private static readonly Vector128<sbyte> s_graveAccentMaskSByte = Vector128.Create((sbyte)'`');
        private static readonly Vector128<sbyte> s_tildeMaskSByte = Vector128.Create((sbyte)'~');
    }
}
