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

            /// <summary>
            ///   LA64: FRECIPE.S fd, fj
            /// </summary>
            public static float ReciprocalExact(float value) => ReciprocalExact(value);

            /// <summary>
            ///   LA64: FRECIPE.D fd, fj
            /// </summary>
            public static double ReciprocalExact(double value) => ReciprocalExact(value);

            /// <summary>
            ///   LA64: FRSQRTE.S fd, fj
            /// </summary>
            public static float ReciprocalSqrtExact(float value) => ReciprocalSqrtExact(value);

            /// <summary>
            ///   LA64: FRSQRTE.D fd, fj
            /// </summary>
            public static double ReciprocalSqrtExact(double value) => ReciprocalSqrtExact(value);

            /// <summary>
            ///   LA64: CRC.W.B.W rd, rj, rk
            /// </summary>
            public static long CyclicRedundancyCheckIEEE8023(int crc, byte checks) => CyclicRedundancyCheckIEEE8023(crc, checks);

            /// <summary>
            ///   LA64: CRC.W.H.W rd, rj, rk
            /// </summary>
            public static long CyclicRedundancyCheckIEEE8023(int crc, ushort checks) => CyclicRedundancyCheckIEEE8023(crc, checks);

            /// <summary>
            ///   LA64: CRC.W.W.W rd, rj, rk
            /// </summary>
            public static long CyclicRedundancyCheckIEEE8023(int crc, uint checks) => CyclicRedundancyCheckIEEE8023(crc, checks);

            /// <summary>
            ///   LA64: CRC.W.D.W rd, rj, rk
            /// </summary>
            public static long CyclicRedundancyCheckIEEE8023(int crc, ulong checks) => CyclicRedundancyCheckIEEE8023(crc, checks);

            /// <summary>
            ///   LA64: CRCC.W.B.W rd, rj, rk
            /// </summary>
            public static long CyclicRedundancyCheckCastagnoli(int crc, byte checks) => CyclicRedundancyCheckCastagnoli(crc, checks);

            /// <summary>
            ///   LA64: CRCC.W.H.W rd, rj, rk
            /// </summary>
            public static long CyclicRedundancyCheckCastagnoli(int crc, ushort checks) => CyclicRedundancyCheckCastagnoli(crc, checks);

            /// <summary>
            ///   LA64: CRCC.W.W.W rd, rj, rk
            /// </summary>
            public static long CyclicRedundancyCheckCastagnoli(int crc, uint checks) => CyclicRedundancyCheckCastagnoli(crc, checks);

            /// <summary>
            ///   LA64: CRCC.W.D.W rd, rj, rk
            /// </summary>
            public static long CyclicRedundancyCheckCastagnoli(int crc, ulong checks) => CyclicRedundancyCheckCastagnoli(crc, checks);
        }

        /// <summary>
        ///   LA32/LA64: MULH.W rd, rj, rk
        /// </summary>
        public static long MultiplyHigh(int left, int right) => MultiplyHigh(left, right);

        /// <summary>
        ///   LA32/LA64: MULH.WU rd, rj, rk
        /// </summary>
        public static ulong MultiplyHigh(uint left, uint right) => MultiplyHigh(left, right);

        /// <summary>
        ///   LA32/LA64: FSQRT.S fd, fj
        /// </summary>
        public static float SquareRoot(float value) => SquareRoot(value);

        /// <summary>
        ///   LA32/LA64: FSQRT.D fd, fj
        /// </summary>
        public static double SquareRoot(double value) => SquareRoot(value);

        /// <summary>
        ///   LA32/LA64: FRECIP.S fd, fj
        /// </summary>
        public static float Reciprocal(float value) => Reciprocal(value);

        /// <summary>
        ///   LA32/LA64: FRECIP.D fd, fj
        /// </summary>
        public static double Reciprocal(double value) => Reciprocal(value);

        /// <summary>
        ///   LA32/LA64: FRSQRT.S fd, fj
        /// </summary>
        public static float ReciprocalSqrt(float value) => ReciprocalSqrt(value);

        /// <summary>
        ///   LA32/LA64: FRSQRT.D fd, fj
        /// </summary>
        public static double ReciprocalSqrt(double value) => ReciprocalSqrt(value);

        /// <summary>
        ///   LA32/LA64: FLOGB.S fd, fj
        /// </summary>
        public static float FloatLogarithm2(float value) => FloatLogarithm2(value);

        /// <summary>
        ///   LA32/LA64: FLOGB.D fd, fj
        /// </summary>
        public static double FloatLogarithm2(double value) => FloatLogarithm2(value);

        /// <summary>
        ///   LA32/LA64: FSCALEB.S fd, fj, fk
        /// </summary>
        public static float FloatScaleBinary(float value, int index) => FloatScaleBinary(value, index);

        /// <summary>
        ///   LA32/LA64: FSCALEB.D fd, fj, fk
        /// </summary>
        public static double FloatScaleBinary(double value, long index) => FloatScaleBinary(value, index);

        /// <summary>
        ///   LA32/LA64: FCOPYSIGN.S fd, fj, fk
        /// </summary>
        public static float FloatCopySign(float value, float sign) => FloatCopySign(value, sign);

        /// <summary>
        ///   LA32/LA64: FCOPYSIGN.D fd, fj, fk
        /// </summary>
        public static double FloatCopySign(double value, double sign) => FloatCopySign(value, sign);

        /// <summary>
        ///   LA32/LA64: FCLASS.S fd, fj
        /// </summary>
        public static float FloatClass(float value) => FloatClass(value);

        /// <summary>
        ///   LA32/LA64: FCLASS.S fd, fj
        /// </summary>
        public static double FloatClass(double value) => FloatClass(value);

    }
}
