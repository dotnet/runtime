// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>
    /// This class provides access to the ARM base hardware instructions via intrinsics
    /// </summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class ArmBase
    {
        internal ArmBase() { }

        public static bool IsSupported { get => IsSupported; }

        [Intrinsic]
        public abstract class Arm64
        {
            internal Arm64() { }

            public static bool IsSupported { get => IsSupported; }

            /// <summary>
            ///   A64: CLS Wd, Wn
            /// </summary>
            public static int LeadingSignCount(int value) => LeadingSignCount(value);

            /// <summary>
            ///   A64: CLS Xd, Xn
            /// </summary>
            public static int LeadingSignCount(long value) => LeadingSignCount(value);

            /// <summary>
            ///   A64: CLZ Xd, Xn
            /// </summary>
            public static int LeadingZeroCount(long value) => LeadingZeroCount(value);

            /// <summary>
            ///   A64: CLZ Xd, Xn
            /// </summary>
            public static int LeadingZeroCount(ulong value) => LeadingZeroCount(value);

            /// <summary>
            ///   A64: RBIT Xd, Xn
            /// </summary>
            public static long ReverseElementBits(long value) => ReverseElementBits(value);

            /// <summary>
            ///   A64: RBIT Xd, Xn
            /// </summary>
            public static ulong ReverseElementBits(ulong value) => ReverseElementBits(value);
        }

        /// <summary>
        ///   A32: CLZ Rd, Rm
        ///   A64: CLZ Wd, Wn
        /// </summary>
        public static int LeadingZeroCount(int value) => LeadingZeroCount(value);

        /// <summary>
        ///   A32: CLZ Rd, Rm
        ///   A64: CLZ Wd, Wn
        /// </summary>
        public static int LeadingZeroCount(uint value) => LeadingZeroCount(value);

        /// <summary>
        ///   A32: RBIT Rd, Rm
        ///   A64: RBIT Wd, Wn
        /// </summary>
        public static int ReverseElementBits(int value) => ReverseElementBits(value);

        /// <summary>
        ///   A32: RBIT Rd, Rm
        ///   A64: RBIT Wd, Wn
        /// </summary>
        public static uint ReverseElementBits(uint value) => ReverseElementBits(value);
    }
}
