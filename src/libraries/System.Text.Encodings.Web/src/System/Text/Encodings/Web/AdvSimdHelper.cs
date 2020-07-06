// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NETCOREAPP
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace System.Text.Encodings.Web
{
    internal static class AdvSimdHelper
    {
        private static readonly Vector128<byte> s_bitMask128 = BitConverter.IsLittleEndian ?
                                                Vector128.Create(0x80402010_08040201).AsByte() :
                                                Vector128.Create(0x01020408_10204080).AsByte();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort MoveMask(Vector128<byte> value)
        {
            Debug.Assert(AdvSimd.Arm64.IsSupported);

            Vector128<byte> mostSignficantBitMask = Vector128.Create((byte)0x80);
            Vector128<byte> mostSignificantBitIsSet = AdvSimd.CompareEqual(AdvSimd.And(value, mostSignficantBitMask), mostSignficantBitMask);

            // self-pairwise add until all flags have moved to the first two bytes of the vector
            Vector128<byte> extractedBits = AdvSimd.And(mostSignificantBitIsSet, s_bitMask128);
            extractedBits = AdvSimd.Arm64.AddPairwise(extractedBits, extractedBits);
            extractedBits = AdvSimd.Arm64.AddPairwise(extractedBits, extractedBits);
            extractedBits = AdvSimd.Arm64.AddPairwise(extractedBits, extractedBits);
            return extractedBits.AsUInt16().ToScalar();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<sbyte> CreateEscapingMask_UnsafeRelaxedJavaScriptEncoder(Vector128<sbyte> sourceValue)
        {
            // This is an instruction-by-instruction port of the Sse2 optimization.
            // TODO investigate further optimizations.

            Debug.Assert(AdvSimd.IsSupported);

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
            // This is an instruction-by-instruction port of the Sse2 optimization.
            // TODO investigate further optimizations.

            Debug.Assert(AdvSimd.IsSupported);

            Vector128<sbyte> mask = CreateEscapingMask_UnsafeRelaxedJavaScriptEncoder(sourceValue);

            mask = AdvSimd.Or(mask, AdvSimd.CompareEqual(sourceValue, s_ampersandMaskSByte));
            mask = AdvSimd.Or(mask, AdvSimd.CompareEqual(sourceValue, s_apostropheMaskSByte));
            mask = AdvSimd.Or(mask, AdvSimd.CompareEqual(sourceValue, s_plusSignMaskSByte));
            mask = AdvSimd.Or(mask, AdvSimd.CompareEqual(sourceValue, s_lessThanSignMaskSByte));
            mask = AdvSimd.Or(mask, AdvSimd.CompareEqual(sourceValue, s_greaterThanSignMaskSByte));
            mask = AdvSimd.Or(mask, AdvSimd.CompareEqual(sourceValue, s_graveAccentMaskSByte));

            return mask;
        }

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
#endif
