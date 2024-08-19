// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>Provides access to the ARM base hardware instructions via intrinsics.</summary>
    [CLSCompliant(false)]
#if SYSTEM_PRIVATE_CORELIB
    public
#else
    internal
#endif
    abstract class ArmBase
    {
        internal ArmBase() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static bool IsSupported { [Intrinsic] get => false; }

        /// <summary>Provides access to the ARM base hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        public abstract class Arm64
        {
            internal Arm64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static bool IsSupported { [Intrinsic] get => false; }

            /// <summary>
            ///   <para>  A64: CLS Wd, Wn</para>
            /// </summary>
            public static int LeadingSignCount(int value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>  A64: CLS Xd, Xn</para>
            /// </summary>
            public static int LeadingSignCount(long value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>  A64: CLZ Xd, Xn</para>
            /// </summary>
            public static int LeadingZeroCount(long value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>  A64: CLZ Xd, Xn</para>
            /// </summary>
            public static int LeadingZeroCount(ulong value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>  A64: SMULH Xd, Xn, Xm</para>
            /// </summary>
            public static long MultiplyHigh(long left, long right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>  A64: UMULH Xd, Xn, Xm</para>
            /// </summary>
            public static ulong MultiplyHigh(ulong left, ulong right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>  A64: RBIT Xd, Xn</para>
            /// </summary>
            public static long ReverseElementBits(long value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>  A64: RBIT Xd, Xn</para>
            /// </summary>
            public static ulong ReverseElementBits(ulong value) { throw new PlatformNotSupportedException(); }
        }

        /// <summary>
        ///   <para>  A32: CLZ Rd, Rm</para>
        ///   <para>  A64: CLZ Wd, Wn</para>
        /// </summary>
        public static int LeadingZeroCount(int value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  A32: CLZ Rd, Rm</para>
        ///   <para>  A64: CLZ Wd, Wn</para>
        /// </summary>
        public static int LeadingZeroCount(uint value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  A32: RBIT Rd, Rm</para>
        ///   <para>  A64: RBIT Wd, Wn</para>
        /// </summary>
        public static int ReverseElementBits(int value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  A32: RBIT Rd, Rm</para>
        ///   <para>  A64: RBIT Wd, Wn</para>
        /// </summary>
        public static uint ReverseElementBits(uint value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  A32: YIELD</para>
        ///   <para>  A64: YIELD</para>
        /// </summary>
        public static void Yield() { throw new PlatformNotSupportedException(); }
    }
}
