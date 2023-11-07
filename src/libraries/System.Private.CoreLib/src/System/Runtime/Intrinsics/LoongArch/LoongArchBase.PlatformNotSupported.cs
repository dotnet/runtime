// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable IDE0060 // unused parameters
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.LoongArch64
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
    abstract class LA64Base
    {
        internal LA64Base() { }

        public static bool IsSupported { [Intrinsic] get => false; }

        /// <summary>
        ///   LA64: CLO.W rd, rj
        /// </summary>
        public static int LeadingSignCount(int value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA64: CLO.D rd, rj
        /// </summary>
        public static int LeadingSignCount(long value) { throw new PlatformNotSupportedException(); }

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
        ///   LA64: FSQRT.S fd, fj
        /// </summary>
        public static float SquareRoot(float value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA64: FSQRT.D fd, fj
        /// </summary>
        public static double SquareRoot(double value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA64: FRECIP.S fd, fj
        /// </summary>
        public static float Reciprocal(float value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA64: FRECIP.D fd, fj
        /// </summary>
        public static double Reciprocal(double value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA64: FRSQRT.S fd, fj
        /// </summary>
        public static float ReciprocalSqrt(float value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA64: FRSQRT.D fd, fj
        /// </summary>
        public static double ReciprocalSqrt(double value) { throw new PlatformNotSupportedException(); }

#if false
        // TODO-LA: adding  bstrins, bstrpick, bytepick
        /// <summary>
        ///   LA64: BYTEPICK.W rd, rj, sa2
        /// </summary>
        public static int SpliceAndCutBits(int left, int right, uint start) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA64: BYTEPICK.W rd, rj, sa2
        /// </summary>
        public static uint SpliceAndCutBits(uint left, uint right, uint start) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA64: BYTEPICK.D rd, rj, sa3
        /// </summary>
        public static long SpliceAndCutBits(long left, long right, uint start) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA64: BYTEPICK.D rd, rj, sa3
        /// </summary>
        public static ulong SpliceAndCutBits(ulong left, ulong right, uint start) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA64: BSTRINS.W rd, rj, msbw, lsbw
        /// </summary>
        public static int ReplaceBits(int left, int right, uint end, uint start) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA64: BSTRINS.W rd, rj, msbw, lsbw
        /// </summary>
        public static uint ReplaceBits(uint left, uint right, uint end, uint start) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA64: BSTRINS.D rd, rj, msbd, lsbd
        /// </summary>
        public static long ReplaceBits(long left, long right, uint end, uint start) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA64: BSTRINS.D rd, rj, msbd, lsbd
        /// </summary>
        public static ulong ReplaceBits(ulong left, ulong right, uint end, uint start) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA64: BSTRPICK.W rd, rj, msbw, lsbw
        /// </summary>
        public static int CutBits(int left, int right, uint end, uint start) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA64: BSTRPICK.W rd, rj, msbw, lsbw
        /// </summary>
        public static uint CutBits(uint left, uint right, uint end, uint start) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA64: BSTRPICK.D rd, rj, msbd, lsbd
        /// </summary>
        public static long CutBits(long left, long right, uint end, uint start) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   LA64: BSTRPICK.D rd, rj, msbd, lsbd
        /// </summary>
        public static ulong CutBits(ulong left, ulong right, uint end, uint start) { throw new PlatformNotSupportedException(); }
#endif
    }
}
