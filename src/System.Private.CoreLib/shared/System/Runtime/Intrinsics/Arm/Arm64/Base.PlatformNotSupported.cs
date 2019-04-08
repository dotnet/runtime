// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.Arm.Arm64
{
    /// <summary>
    /// This class provides access to the Arm64 Base intrinsics
    ///
    /// These intrinsics are supported by all Arm64 CPUs
    /// </summary>
    [CLSCompliant(false)]
    public static class Base
    {
        public static bool IsSupported { [Intrinsic] get { return false; }}

        /// <summary>
        /// Vector LeadingSignCount
        /// Corresponds to integer forms of ARM64 CLS
        /// </summary>
        public static int LeadingSignCount(int  value) { throw new PlatformNotSupportedException(); }
        public static int LeadingSignCount(long value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// Vector LeadingZeroCount
        /// Corresponds to integer forms of ARM64 CLZ
        /// </summary>
        public static int LeadingZeroCount(int   value) { throw new PlatformNotSupportedException(); }
        public static int LeadingZeroCount(uint  value) { throw new PlatformNotSupportedException(); }
        public static int LeadingZeroCount(long  value) { throw new PlatformNotSupportedException(); }
        public static int LeadingZeroCount(ulong value) { throw new PlatformNotSupportedException(); }
    }
}
