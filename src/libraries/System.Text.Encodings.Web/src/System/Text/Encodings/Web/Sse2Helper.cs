// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Numerics;
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

            // Space ' ', anything in the control characters range, and anything above short.MaxValue but less than or equal char.MaxValue
            Vector128<short> mask = Sse2.CompareLessThan(sourceValue, s_mask_UInt16_0x20);

            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_UInt16_0x22)); // Quotation Mark '"'
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_UInt16_0x5C)); // Reverse Solidus '\'

            return mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<sbyte> CreateEscapingMask_UnsafeRelaxedJavaScriptEncoder(Vector128<sbyte> sourceValue)
        {
            Debug.Assert(Sse2.IsSupported);

            Vector128<sbyte> mask = Sse2.CompareLessThan(sourceValue, s_mask_SByte_0x20); // Control characters, and anything above 0x7E since sbyte.MaxValue is 0x7E

            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_SByte_0x22)); // Quotation Mark "
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_SByte_0x5C)); // Reverse Solidus \

            return mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<short> CreateEscapingMask_DefaultJavaScriptEncoderBasicLatin(Vector128<short> sourceValue)
        {
            Debug.Assert(Sse2.IsSupported);

            Vector128<short> mask = CreateEscapingMask_UnsafeRelaxedJavaScriptEncoder(sourceValue);

            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_UInt16_0x26)); // Ampersand '&'
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_UInt16_0x27)); // Apostrophe '''
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_UInt16_0x2B)); // Plus sign '+'
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_UInt16_0x3C)); // Less Than Sign '<'
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_UInt16_0x3E)); // Greater Than Sign '>'
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_UInt16_0x60)); // Grave Access '`'

            mask = Sse2.Or(mask, Sse2.CompareGreaterThan(sourceValue, s_mask_UInt16_0x7E)); // Tilde '~', anything above the ASCII range

            return mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<sbyte> CreateEscapingMask_DefaultJavaScriptEncoderBasicLatin(Vector128<sbyte> sourceValue)
        {
            Debug.Assert(Sse2.IsSupported);

            Vector128<sbyte> mask = CreateEscapingMask_UnsafeRelaxedJavaScriptEncoder(sourceValue);

            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_SByte_0x26)); // Ampersand &
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_SByte_0x27)); // Apostrophe '
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_SByte_0x2B)); // Plus sign +
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_SByte_0x3C)); // Less Than Sign <
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_SByte_0x3E)); // Greater Than Sign >
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_SByte_0x60)); // Grave Access `

            return mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<short> CreateAsciiMask(Vector128<short> sourceValue)
        {
            Debug.Assert(Sse2.IsSupported);

            Vector128<short> mask = Sse2.CompareLessThan(sourceValue, s_mask_UInt16_0x00); // Null, anything above short.MaxValue but less than or equal char.MaxValue
            mask = Sse2.Or(mask, Sse2.CompareGreaterThan(sourceValue, s_mask_UInt16_0x7E)); // Tilde '~', anything above the ASCII range

            return mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<sbyte> CreateAsciiMask(Vector128<sbyte> sourceValue)
        {
            Debug.Assert(Sse2.IsSupported);

            // Null, anything above sbyte.MaxValue but less than or equal byte.MaxValue (i.e. anything above the ASCII range)
            Vector128<sbyte> mask = Sse2.CompareLessThan(sourceValue, s_mask_SByte_0x00);
            return mask;
        }

        private static readonly Vector128<short> s_mask_UInt16_0x00 = Vector128<short>.Zero; // Null

        private static readonly Vector128<short> s_mask_UInt16_0x20 = Vector128.Create((short)0x20); // Space ' '

        private static readonly Vector128<short> s_mask_UInt16_0x22 = Vector128.Create((short)0x22); // Quotation Mark '"'
        private static readonly Vector128<short> s_mask_UInt16_0x26 = Vector128.Create((short)0x26); // Ampersand '&'
        private static readonly Vector128<short> s_mask_UInt16_0x27 = Vector128.Create((short)0x27); // Apostrophe '''
        private static readonly Vector128<short> s_mask_UInt16_0x2B = Vector128.Create((short)0x2B); // Plus sign '+'
        private static readonly Vector128<short> s_mask_UInt16_0x3C = Vector128.Create((short)0x3C); // Less Than Sign '<'
        private static readonly Vector128<short> s_mask_UInt16_0x3E = Vector128.Create((short)0x3E); // Greater Than Sign '>'
        private static readonly Vector128<short> s_mask_UInt16_0x5C = Vector128.Create((short)0x5C); // Reverse Solidus '\'
        private static readonly Vector128<short> s_mask_UInt16_0x60 = Vector128.Create((short)0x60); // Grave Access '`'

        private static readonly Vector128<short> s_mask_UInt16_0x7E = Vector128.Create((short)0x7E); // Tilde '~'

        private static readonly Vector128<sbyte> s_mask_SByte_0x00 = Vector128<sbyte>.Zero; // Null

        private static readonly Vector128<sbyte> s_mask_SByte_0x20 = Vector128.Create((sbyte)0x20); // Space ' '

        private static readonly Vector128<sbyte> s_mask_SByte_0x22 = Vector128.Create((sbyte)0x22); // Quotation Mark '"'
        private static readonly Vector128<sbyte> s_mask_SByte_0x26 = Vector128.Create((sbyte)0x26); // Ampersand '&'
        private static readonly Vector128<sbyte> s_mask_SByte_0x27 = Vector128.Create((sbyte)0x27); // Apostrophe '''
        private static readonly Vector128<sbyte> s_mask_SByte_0x2B = Vector128.Create((sbyte)0x2B); // Plus sign '+'
        private static readonly Vector128<sbyte> s_mask_SByte_0x3C = Vector128.Create((sbyte)0x3C); // Less Than Sign '<'
        private static readonly Vector128<sbyte> s_mask_SByte_0x3E = Vector128.Create((sbyte)0x3E); // Greater Than Sign '>'
        private static readonly Vector128<sbyte> s_mask_SByte_0x5C = Vector128.Create((sbyte)0x5C); // Reverse Solidus '\'
        private static readonly Vector128<sbyte> s_mask_SByte_0x60 = Vector128.Create((sbyte)0x60); // Grave Access '`'
    }
}
