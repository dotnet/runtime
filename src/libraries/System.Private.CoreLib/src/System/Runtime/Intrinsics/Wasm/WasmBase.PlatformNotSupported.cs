// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Intrinsics.Wasm
{
    internal abstract class WasmBase
    {
        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static bool IsSupported => false;

        /// <summary>
        ///   <para>  i32.clz</para>
        /// </summary>
        public static int LeadingZeroCount(int value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  i32.clz</para>
        /// </summary>
        public static int LeadingZeroCount(uint value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  i64.clz</para>
        /// </summary>
        public static int LeadingZeroCount(long value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  i64.clz</para>
        /// </summary>
        public static int LeadingZeroCount(ulong value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  i32.ctz</para>
        /// </summary>
        public static int TrailingZeroCount(int value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  i32.ctz</para>
        /// </summary>
        public static int TrailingZeroCount(uint value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  i64.ctz</para>
        /// </summary>
        public static int TrailingZeroCount(long value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  i64.ctz</para>
        /// </summary>
        public static int TrailingZeroCount(ulong value) { throw new PlatformNotSupportedException(); }

    }
}
