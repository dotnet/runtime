// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>This class provides access to X86 LZCNT hardware instructions via intrinsics.</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Lzcnt : X86Base
    {
        internal Lzcnt() { }

        /// <summary>Gets <c>true</c> if the APIs in this class are supported; otherwise, <c>false</c> which indicates they will throw <see cref="PlatformNotSupportedException" />.</summary>
        public static new bool IsSupported { get => IsSupported; }

        /// <summary>This class provides access to the x86 LZCNT hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        [Intrinsic]
        public new abstract class X64 : X86Base.X64
        {
            internal X64() { }

            /// <summary>Gets <c>true</c> if the APIs in this class are supported; otherwise, <c>false</c> which indicates they will throw <see cref="PlatformNotSupportedException" />.</summary>
            public static new bool IsSupported { get => IsSupported; }

            /// <summary>
            /// unsigned __int64 _lzcnt_u64 (unsigned __int64 a)
            ///   LZCNT r64, r/m64
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static ulong LeadingZeroCount(ulong value) => LeadingZeroCount(value);
        }

        /// <summary>
        /// unsigned int _lzcnt_u32 (unsigned int a)
        ///   LZCNT r32, r/m32
        /// </summary>
        public static uint LeadingZeroCount(uint value) => LeadingZeroCount(value);
    }
}
