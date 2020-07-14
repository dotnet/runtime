// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace System.Text.Encodings.Web
{
    internal static class AdvSimdHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<short> CreateEscapingMask_UnsafeRelaxedJavaScriptEncoder(Vector128<short> sourceValue)
        {
            if (!AdvSimd.Arm64.IsSupported)
            {
                throw new PlatformNotSupportedException();
            }

            // Anything in the control characters range, and anything above short.MaxValue but less than or equal char.MaxValue
            // That's because anything between 32768 and 65535 (inclusive) will overflow and become negative.
            Vector128<short> mask = AdvSimd.CompareLessThan(sourceValue, s_spaceMaskInt16);

            mask = AdvSimd.Or(mask, AdvSimd.CompareEqual(sourceValue, s_quotationMarkMaskInt16));
            mask = AdvSimd.Or(mask, AdvSimd.CompareEqual(sourceValue, s_reverseSolidusMaskInt16));

            // Anything above the ASCII range, and also including the leftover control character in the ASCII range - 0x7F
            // When this method is called with only ASCII data, 0x7F is the only value that would meet this comparison.
            // However, when called from "Default", the source could contain characters outside the ASCII range.
            mask = AdvSimd.Or(mask, AdvSimd.CompareGreaterThan(sourceValue, s_tildeMaskInt16));

            return mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<sbyte> CreateEscapingMask_UnsafeRelaxedJavaScriptEncoder(Vector128<sbyte> sourceValue)
        {
            if (!AdvSimd.Arm64.IsSupported)
            {
                throw new PlatformNotSupportedException();
            }

            // Anything in the control characters range (except 0x7F), and anything above sbyte.MaxValue but less than or equal byte.MaxValue
            // That's because anything between 128 and 255 (inclusive) will overflow and become negative.
            Vector128<sbyte> mask = AdvSimd.CompareLessThan(sourceValue, s_spaceMaskSByte);

            mask = AdvSimd.Or(mask, AdvSimd.CompareEqual(sourceValue, s_quotationMarkMaskSByte));
            mask = AdvSimd.Or(mask, AdvSimd.CompareEqual(sourceValue, s_reverseSolidusMaskSByte));

            // Leftover control character in the ASCII range - 0x7F
            // Since we are dealing with sbytes, 0x7F is the only value that would meet this comparison.
            mask = AdvSimd.Or(mask, AdvSimd.CompareGreaterThan(sourceValue, s_tildeMaskSByte));

            return mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<sbyte> CreateEscapingMask_DefaultJavaScriptEncoderBasicLatin(Vector128<sbyte> sourceValue)
        {
            if (!AdvSimd.Arm64.IsSupported)
            {
                throw new PlatformNotSupportedException();
            }

            Vector128<sbyte> mask = CreateEscapingMask_UnsafeRelaxedJavaScriptEncoder(sourceValue);

            mask = AdvSimd.Or(mask, AdvSimd.CompareEqual(sourceValue, s_ampersandMaskSByte));
            mask = AdvSimd.Or(mask, AdvSimd.CompareEqual(sourceValue, s_apostropheMaskSByte));
            mask = AdvSimd.Or(mask, AdvSimd.CompareEqual(sourceValue, s_plusSignMaskSByte));
            mask = AdvSimd.Or(mask, AdvSimd.CompareEqual(sourceValue, s_lessThanSignMaskSByte));
            mask = AdvSimd.Or(mask, AdvSimd.CompareEqual(sourceValue, s_greaterThanSignMaskSByte));
            mask = AdvSimd.Or(mask, AdvSimd.CompareEqual(sourceValue, s_graveAccentMaskSByte));

            return mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<short> CreateAsciiMask(Vector128<short> sourceValue)
        {
            if (!AdvSimd.Arm64.IsSupported)
            {
                throw new PlatformNotSupportedException();
            }

            // Anything above short.MaxValue but less than or equal char.MaxValue
            // That's because anything between 32768 and 65535 (inclusive) will overflow and become negative.
            Vector128<short> mask = AdvSimd.CompareLessThan(sourceValue, s_nullMaskInt16);

            // Anything above the ASCII range
            mask = AdvSimd.Or(mask, AdvSimd.CompareGreaterThan(sourceValue, s_maxAsciiCharacterMaskInt16));

            return mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsNonAsciiByte(Vector128<sbyte> value)
        {
            if (!AdvSimd.Arm64.IsSupported)
            {
                throw new PlatformNotSupportedException();
            }

            // most significant bit mask for a 64-bit byte vector
            const ulong MostSignficantBitMask = 0x8080808080808080;

            value = AdvSimd.Arm64.MinPairwise(value, value);
            return (value.AsUInt64().ToScalar() & MostSignficantBitMask) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndexOfFirstNonAsciiByte(Vector128<byte> value)
        {
            if (!AdvSimd.Arm64.IsSupported || !BitConverter.IsLittleEndian)
            {
                throw new PlatformNotSupportedException();
            }

            // extractedBits[i] = (value[i] >> 7) & (1 << (12 * (i % 2)));
            Vector128<byte> mostSignificantBitIsSet = AdvSimd.ShiftRightArithmetic(value.AsSByte(), 7).AsByte();
            Vector128<byte> extractedBits = AdvSimd.And(mostSignificantBitIsSet, s_bitmask);

            // collapse mask to lower bits
            extractedBits = AdvSimd.Arm64.AddPairwise(extractedBits, extractedBits);
            ulong mask = extractedBits.AsUInt64().ToScalar();

            // calculate the index
            int index = BitOperations.TrailingZeroCount(mask) >> 2;
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

        private static readonly Vector128<byte> s_bitmask = BitConverter.IsLittleEndian ?
            Vector128.Create((ushort)0x1001).AsByte() :
            Vector128.Create((ushort)0x0110).AsByte();
    }
}
