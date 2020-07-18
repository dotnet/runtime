// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable IDE0060 // unused parameters
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>
    /// This class provides access to the ARM base hardware instructions via intrinsics
    /// </summary>
    [CLSCompliant(false)]
#if SYSTEM_PRIVATE_CORELIB
    public
#else
    internal
#endif
    abstract class ArmBase
    {
        internal ArmBase() { }

        public static bool IsSupported { [Intrinsic] get => false; }

        public abstract class Arm64
        {
            internal Arm64() { }

            public static bool IsSupported { [Intrinsic] get => false; }

            /// <summary>
            ///   A64: CLS Wd, Wn
            /// </summary>
            public static int LeadingSignCount(int value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   A64: CLS Xd, Xn
            /// </summary>
            public static int LeadingSignCount(long value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   A64: CLZ Xd, Xn
            /// </summary>
            public static int LeadingZeroCount(long value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   A64: CLZ Xd, Xn
            /// </summary>
            public static int LeadingZeroCount(ulong value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   A64: RBIT Xd, Xn
            /// </summary>
            public static long ReverseElementBits(long value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   A64: RBIT Xd, Xn
            /// </summary>
            public static ulong ReverseElementBits(ulong value) { throw new PlatformNotSupportedException(); }
        }

        /// <summary>
        ///   A32: CLZ Rd, Rm
        ///   A64: CLZ Wd, Wn
        /// </summary>
        public static int LeadingZeroCount(int value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   A32: CLZ Rd, Rm
        ///   A64: CLZ Wd, Wn
        /// </summary>
        public static int LeadingZeroCount(uint value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   A32: RBIT Rd, Rm
        ///   A64: RBIT Wd, Wn
        /// </summary>
        public static int ReverseElementBits(int value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   A32: RBIT Rd, Rm
        ///   A64: RBIT Wd, Wn
        /// </summary>
        public static uint ReverseElementBits(uint value) { throw new PlatformNotSupportedException(); }
    }
}
