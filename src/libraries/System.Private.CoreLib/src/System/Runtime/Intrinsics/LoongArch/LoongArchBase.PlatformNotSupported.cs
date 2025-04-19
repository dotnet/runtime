// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable IDE0060 // unused parameters
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.LoongArch
{
    /// <summary>
    /// This class provides access to the LA64 base hardware instructions via intrinsics
    /// </summary>
    [CLSCompliant(false)]
#if SYSTEM_PRIVATE_CORELIB
    public
#else
    internal
#endif
    abstract class LoongArchBase
    {
        internal LoongArchBase() { }

        public static bool IsSupported { [Intrinsic] get => false; }

        [Intrinsic]
        public abstract class LoongArch64
        {
            internal LoongArch64() { }

            public static bool IsSupported { [Intrinsic] get => false; }

            /// <summary>
            ///   LA64: CLO.W rd, rj
            /// </summary>
            public static int LeadingSignCount(int value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: CLO.W rd, rj
            /// </summary>
            public static int LeadingSignCount(uint value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: CLO.D rd, rj
            /// </summary>
            public static int LeadingSignCount(long value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: CLO.D rd, rj
            /// </summary>
            public static int LeadingSignCount(ulong value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: CLZ.W rd, rj
            /// </summary>
            public static int LeadingZeroCount(int value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: CLZ.W rd, rj
            /// </summary>
            public static int LeadingZeroCount(uint value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: CLZ.D rd, rj
            /// </summary>
            public static int LeadingZeroCount(long value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: CLZ.D rd, rj
            /// </summary>
            public static int LeadingZeroCount(ulong value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: CTO.W rd, rj
            /// </summary>
            public static int TrailingOneCount(int value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: CTO.W rd, rj
            /// </summary>
            public static int TrailingOneCount(uint value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: CTO.D rd, rj
            /// </summary>
            public static int TrailingOneCount(long value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: CTO.D rd, rj
            /// </summary>
            public static int TrailingOneCount(ulong value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: CTZ.W rd, rj
            /// </summary>
            public static int TrailingZeroCount(int value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: CTZ.W rd, rj
            /// </summary>
            public static int TrailingZeroCount(uint value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: CTZ.D rd, rj
            /// </summary>
            public static int TrailingZeroCount(long value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: CTZ.D rd, rj
            /// </summary>
            public static int TrailingZeroCount(ulong value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: MULH.D rd, rj, rk
            /// </summary>
            public static long MultiplyHigh(long left, long right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: MULH.DU rd, rj, rk
            /// </summary>
            public static ulong MultiplyHigh(ulong left, ulong right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: BITREV.D rd, rj
            /// </summary>
            public static long ReverseElementBits(long value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: BITREV.D rd, rj
            /// </summary>
            public static ulong ReverseElementBits(ulong value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: BITREV.W rd, rj
            /// </summary>
            public static long ReverseElementBits(int value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: BITREV.W rd, rj
            /// </summary>
            public static ulong ReverseElementBits(uint value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: REVB.2W rd, rj
            /// </summary>
            public static int ReverseElementBits(int value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: REVB.2W rd, rj
            /// </summary>
            public static uint ReverseElementBits(uint value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: REVB.D rd, rj
            /// </summary>
            public static long ReverseElementBits(long value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: REVB.D rd, rj
            /// </summary>
            public static ulong ReverseElementBits(ulong value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: FRECIPE.S fd, fj
            /// </summary>
            public static float ReciprocalEstimate(float value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: FRECIPE.D fd, fj
            /// </summary>
            public static double ReciprocalEstimate(double value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: FRSQRTE.S fd, fj
            /// </summary>
            public static float ReciprocalSqrtEstimate(float value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: FRSQRTE.D fd, fj
            /// </summary>
            public static double ReciprocalSqrtEstimate(double value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: CRC.W.B.W rd, rj, rk
            /// </summary>
            public static long CyclicRedundancyCheckIEEE8023(int crc, byte checks) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: CRC.W.H.W rd, rj, rk
            /// </summary>
            public static long CyclicRedundancyCheckIEEE8023(int crc, ushort checks) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: CRC.W.W.W rd, rj, rk
            /// </summary>
            public static long CyclicRedundancyCheckIEEE8023(int crc, uint checks) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: CRC.W.D.W rd, rj, rk
            /// </summary>
            public static long CyclicRedundancyCheckIEEE8023(int crc, ulong checks) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: CRCC.W.B.W rd, rj, rk
            /// </summary>
            public static long CyclicRedundancyCheckCastagnoli(int crc, byte checks) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: CRCC.W.H.W rd, rj, rk
            /// </summary>
            public static long CyclicRedundancyCheckCastagnoli(int crc, ushort checks) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: CRCC.W.W.W rd, rj, rk
            /// </summary>
            public static long CyclicRedundancyCheckCastagnoli(int crc, uint checks) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   LA64: CRCC.W.D.W rd, rj, rk
            /// </summary>
            public static long CyclicRedundancyCheckCastagnoli(int crc, ulong checks) { throw new PlatformNotSupportedException(); }
        }

        /// <summary>
        ///   LA32/LA64: MULH.W rd, rj, rk
        /// </summary>
        public static long MultiplyHigh(int left, int right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA32/LA64: MULH.WU rd, rj, rk
        /// </summary>
        public static ulong MultiplyHigh(uint left, uint right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA32/LA64: FSQRT.S fd, fj
        /// </summary>
        public static float SquareRoot(float value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA32/LA64: FSQRT.D fd, fj
        /// </summary>
        public static double SquareRoot(double value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA32/LA64: FRECIP.S fd, fj
        /// </summary>
        public static float Reciprocal(float value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA32/LA64: FRECIP.D fd, fj
        /// </summary>
        public static double Reciprocal(double value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA32/LA64: FRSQRT.S fd, fj
        /// </summary>
        public static float ReciprocalSqrt(float value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA32/LA64: FRSQRT.D fd, fj
        /// </summary>
        public static double ReciprocalSqrt(double value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA32/LA64: FLOGB.S fd, fj
        /// </summary>
        public static float FloatLogarithm2(float value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA32/LA64: FLOGB.D fd, fj
        /// </summary>
        public static double FloatLogarithm2(double value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA32/LA64: FSCALEB.S fd, fj, fk
        /// </summary>
        public static float FloatScaleBinary(float value, int index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA32/LA64: FSCALEB.D fd, fj, fk
        /// </summary>
        public static double FloatScaleBinary(double value, long index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA32/LA64: FCOPYSIGN.S fd, fj, fk
        /// </summary>
        public static float FloatCopySign(float value, float sign) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA32/LA64: FCOPYSIGN.D fd, fj, fk
        /// </summary>
        public static double FloatCopySign(double value, double sign) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA32/LA64: FCLASS.S fd, fj
        /// </summary>
        public static float FloatClass(float value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA32/LA64: FCLASS.S fd, fj
        /// </summary>
        public static double FloatClass(double value) { throw new PlatformNotSupportedException(); }
    }
}
