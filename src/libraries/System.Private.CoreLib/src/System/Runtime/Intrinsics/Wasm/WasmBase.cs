// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.Wasm
{
    [Intrinsic]
    internal abstract class WasmBase
    {
        public static bool IsSupported { get; }

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
