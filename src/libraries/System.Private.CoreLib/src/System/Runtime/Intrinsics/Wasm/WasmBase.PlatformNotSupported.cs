// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

#pragma warning disable IDE0060 // Remove unused parameter

namespace System.Runtime.Intrinsics.Wasm
{
    internal abstract class WasmBase
    {
        public static bool IsSupported => false;

        /// <summary>
        ///   i32.clz
        /// </summary>
        public static int LeadingZeroCount(int value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   i32.clz
        /// </summary>
        public static int LeadingZeroCount(uint value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   i64.clz
        /// </summary>
        public static int LeadingZeroCount(long value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   i64.clz
        /// </summary>
        public static int LeadingZeroCount(ulong value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   i32.ctz
        /// </summary>
        public static int TrailingZeroCount(int value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   i32.ctz
        /// </summary>
        public static int TrailingZeroCount(uint value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   i64.ctz
        /// </summary>
        public static int TrailingZeroCount(long value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   i64.ctz
        /// </summary>
        public static int TrailingZeroCount(ulong value) { throw new PlatformNotSupportedException(); }

    }
}
