// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.LoongArch
{
    /// <summary>
    /// This class provides access to the LoongArch base hardware instructions via intrinsics
    /// </summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class LoongArchBase
    {
        internal LoongArchBase() { }

        public static bool IsSupported { get => IsSupported; }

        [Intrinsic]
        public abstract class LoongArch64
        {
            internal LoongArch64() { }

            public static bool IsSupported { get => IsSupported; }

            /// <summary>
            ///   LA64: CLO.W rd, rj
            /// </summary>
            public static int LeadingSignCount(int value) => LeadingSignCount(value);

            /// <summary>
            ///   LA64: CLO.W rd, rj
            /// </summary>
            public static int LeadingSignCount(uint value) => LeadingSignCount(value);

            /// <summary>
            ///   LA64: CLO.D rd, rj
            /// </summary>
            public static int LeadingSignCount(long value) => LeadingSignCount(value);

            /// <summary>
            ///   LA64: CLO.D rd, rj
            /// </summary>
            public static int LeadingSignCount(ulong value) => LeadingSignCount(value);

            /// <summary>
            ///   LA64: CLZ.W rd, rj
            /// </summary>
            public static int LeadingZeroCount(int value) => LeadingZeroCount(value);

            /// <summary>
            ///   LA64: CLZ.W rd, rj
            /// </summary>
            public static int LeadingZeroCount(uint value) => LeadingZeroCount(value);

            /// <summary>
            ///   LA64: CLZ.D rd, rj
            /// </summary>
            public static int LeadingZeroCount(long value) => LeadingZeroCount(value);

            /// <summary>
            ///   LA64: CLZ.D rd, rj
            /// </summary>
            public static int LeadingZeroCount(ulong value) => LeadingZeroCount(value);

            /// <summary>
            ///   LA64: CTO.W rd, rj
            /// </summary>
            public static int TrailingOneCount(int value) => TrailingOneCount(value);

            /// <summary>
            ///   LA64: CTO.W rd, rj
            /// </summary>
            public static int TrailingOneCount(uint value) => TrailingOneCount(value);

            /// <summary>
            ///   LA64: CTO.D rd, rj
            /// </summary>
            public static int TrailingOneCount(long value) => TrailingOneCount(value);

            /// <summary>
            ///   LA64: CTO.D rd, rj
            /// </summary>
            public static int TrailingOneCount(ulong value) => TrailingOneCount(value);

            /// <summary>
            ///   LA64: CTZ.W rd, rj
            /// </summary>
            public static int TrailingZeroCount(int value) => TrailingZeroCount(value);

            /// <summary>
            ///   LA64: CTZ.W rd, rj
            /// </summary>
            public static int TrailingZeroCount(uint value) => TrailingZeroCount(value);

            /// <summary>
            ///   LA64: CTZ.D rd, rj
            /// </summary>
            public static int TrailingZeroCount(long value) => TrailingZeroCount(value);

            /// <summary>
            ///   LA64: CTZ.D rd, rj
            /// </summary>
            public static int TrailingZeroCount(ulong value) => TrailingZeroCount(value);

            /// <summary>
            ///   LA64: MULH.D rd, rj, rk
            /// </summary>
            public static long MultiplyHigh(long left, long right) => MultiplyHigh(left, right);

            /// <summary>
            ///   LA64: MULH.DU rd, rj, rk
            /// </summary>
            public static ulong MultiplyHigh(ulong left, ulong right) => MultiplyHigh(left, right);

            /// <summary>
            ///   LA64: BITREV.D rd, rj
            /// </summary>
            public static long ReverseElementBits(long value) => ReverseElementBits(value);

            /// <summary>
            ///   LA64: BITREV.D rd, rj
            /// </summary>
            public static ulong ReverseElementBits(ulong value) => ReverseElementBits(value);

            /// <summary>
            ///   LA64: BITREV.W rd, rj
            /// </summary>
            public static long ReverseElementBits(int value) => ReverseElementBits(value);

            /// <summary>
            ///   LA64: BITREV.W rd, rj
            /// </summary>
            public static ulong ReverseElementBits(uint value) => ReverseElementBits(value);

            /// <summary>
            ///   LA64: REVB.2W rd, rj
            /// </summary>
            public static int ReverseElementBits(int value) => ReverseElementBits(value);

            /// <summary>
            ///   LA64: REVB.2W rd, rj
            /// </summary>
            public static uint ReverseElementBits(uint value) => ReverseElementBits(value);

            /// <summary>
            ///   LA64: REVB.D rd, rj
            /// </summary>
            public static long ReverseElementBits(long value) => ReverseElementBits(value);

            /// <summary>
            ///   LA64: REVB.D rd, rj
            /// </summary>
            public static ulong ReverseElementBits(ulong value) => ReverseElementBits(value);

        }

        /// <summary>
        ///   LA32: MULH.W rd, rj, rk
        /// </summary>
        public static long MultiplyHigh(int left, int right) => MultiplyHigh(left, right);

        /// <summary>
        ///   LA32: MULH.WU rd, rj, rk
        /// </summary>
        public static ulong MultiplyHigh(uint left, uint right) => MultiplyHigh(left, right);

        /// <summary>
        ///   LA32: FSQRT.S fd, fj
        /// </summary>
        public static float SquareRoot(float value) => SquareRoot(value);

        /// <summary>
        ///   LA32: FSQRT.D fd, fj
        /// </summary>
        public static double SquareRoot(double value) => SquareRoot(value);

        /// <summary>
        ///   LA32: FRECIP.S fd, fj
        /// </summary>
        public static float Reciprocal(float value) => Reciprocal(value);

        /// <summary>
        ///   LA32: FRECIP.D fd, fj
        /// </summary>
        public static double Reciprocal(double value) => Reciprocal(value);

        /// <summary>
        ///   LA32: FRSQRT.S fd, fj
        /// </summary>
        public static float ReciprocalSqrt(float value) => ReciprocalSqrt(value);

        /// <summary>
        ///   LA32: FRSQRT.D fd, fj
        /// </summary>
        public static double ReciprocalSqrt(double value) => ReciprocalSqrt(value);
    }
}
