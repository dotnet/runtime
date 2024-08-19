// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.Wasm
{
    [Intrinsic]
    internal abstract class WasmBase
    {
        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static bool IsSupported { get => IsSupported; }

        /// <summary>
        ///   i32.clz
        /// </summary>
        public static int LeadingZeroCount(int value) => LeadingZeroCount(value);

        /// <summary>
        ///   i32.clz
        /// </summary>
        public static int LeadingZeroCount(uint value) => LeadingZeroCount(value);

        /// <summary>
        ///   i64.clz
        /// </summary>
        public static int LeadingZeroCount(long value) => LeadingZeroCount(value);

        /// <summary>
        ///   i64.clz
        /// </summary>
        public static int LeadingZeroCount(ulong value) => LeadingZeroCount(value);

        /// <summary>
        ///   i32.ctz
        /// </summary>
        public static int TrailingZeroCount(int value) => TrailingZeroCount(value);

        /// <summary>
        ///   i32.ctz
        /// </summary>
        public static int TrailingZeroCount(uint value) => TrailingZeroCount(value);

        /// <summary>
        ///   i64.ctz
        /// </summary>
        public static int TrailingZeroCount(long value) => TrailingZeroCount(value);

        /// <summary>
        ///   i64.ctz
        /// </summary>
        public static int TrailingZeroCount(ulong value) => TrailingZeroCount(value);
    }
}
