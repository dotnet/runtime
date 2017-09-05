// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>
    /// This class provides access to Intel SSSE3 hardware instructions via intrinsics
    /// </summary>
    public static class Ssse3
    {
        public static bool IsSupported { get { return false; } }
        
        /// <summary>
        /// __m128i _mm_abs_epi8 (__m128i a)
        /// </summary>
        public static Vector128<byte> Abs(Vector128<sbyte> value) { throw new NotImplementedException(); }
        /// <summary>
        /// __m128i _mm_abs_epi16 (__m128i a)
        /// </summary>
        public static Vector128<ushort> Abs(Vector128<short> value) { throw new NotImplementedException(); }
        /// <summary>
        /// __m128i _mm_abs_epi32 (__m128i a)
        /// </summary>
        public static Vector128<uint> Abs(Vector128<int> value) { throw new NotImplementedException(); }

        /// <summary>
        /// __m128i _mm_alignr_epi8 (__m128i a, __m128i b, int count)
        /// </summary>
        public static Vector128<sbyte> AlignRight(Vector128<sbyte> left, Vector128<sbyte> right, byte mask) { throw new NotImplementedException(); }

        /// <summary>
        /// __m128i _mm_hadd_epi16 (__m128i a, __m128i b)
        /// </summary>
        public static Vector128<short> HorizontalAdd(Vector128<short> left, Vector128<short> right) { throw new NotImplementedException(); }
        /// <summary>
        /// __m128i _mm_hadd_epi32 (__m128i a, __m128i b)
        /// </summary>
        public static Vector128<int> HorizontalAdd(Vector128<int> left, Vector128<int> right) { throw new NotImplementedException(); }

        /// <summary>
        /// __m128i _mm_hadds_epi16 (__m128i a, __m128i b)
        /// </summary>
        public static Vector128<short> HorizontalAddSaturate(Vector128<short> left, Vector128<short> right) { throw new NotImplementedException(); }

        /// <summary>
        /// __m128i _mm_hsub_epi16 (__m128i a, __m128i b)
        /// </summary>
        public static Vector128<short> HorizontalSubtract(Vector128<short> left, Vector128<short> right) { throw new NotImplementedException(); }
        /// <summary>
        /// __m128i _mm_hsub_epi32 (__m128i a, __m128i b)
        /// </summary>
        public static Vector128<int> HorizontalSubtract(Vector128<int> left, Vector128<int> right) { throw new NotImplementedException(); }

        /// <summary>
        /// __m128i _mm_hsubs_epi16 (__m128i a, __m128i b)
        /// </summary>
        public static Vector128<short> HorizontalSubtractSaturate(Vector128<short> left, Vector128<short> right) { throw new NotImplementedException(); }

        /// <summary>
        /// __m128i _mm_maddubs_epi16 (__m128i a, __m128i b)
        /// </summary>
        public static Vector128<short> MultiplyAddAdjacent(Vector128<byte> left, Vector128<sbyte> right) { throw new NotImplementedException(); }

        /// <summary>
        /// __m128i _mm_mulhrs_epi16 (__m128i a, __m128i b)
        /// </summary>
        public static Vector128<short> MultiplyHighRoundScale(Vector128<short> left, Vector128<short> right) { throw new NotImplementedException(); }

        /// <summary>
        /// __m128i _mm_shuffle_epi8 (__m128i a, __m128i b)
        /// </summary>
        public static Vector128<sbyte> Shuffle(Vector128<sbyte> value, Vector128<sbyte> mask) { throw new NotImplementedException(); }

        /// <summary>
        /// __m128i _mm_sign_epi8 (__m128i a, __m128i b)
        /// </summary>
        public static Vector128<sbyte> Sign(Vector128<sbyte> left, Vector128<sbyte> right) { throw new NotImplementedException(); }
        /// <summary>
        /// __m128i _mm_sign_epi16 (__m128i a, __m128i b)
        /// </summary>
        public static Vector128<short> Sign(Vector128<short> left, Vector128<short> right) { throw new NotImplementedException(); }
        /// <summary>
        /// __m128i _mm_sign_epi32 (__m128i a, __m128i b)
        /// </summary>
        public static Vector128<int> Sign(Vector128<int> left, Vector128<int> right) { throw new NotImplementedException(); }
    }
}
