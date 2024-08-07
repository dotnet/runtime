// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>This class provides access to the x86 SERIALIZE hardware instruction via intrinsics.</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class X86Serialize : X86Base
    {
        internal X86Serialize() { }

        /// <summary>Gets <c>true</c> if the APIs in this class are supported; otherwise, <c>false</c> which indicates they will throw <see cref="PlatformNotSupportedException" />.</summary>
        public static new bool IsSupported { get => IsSupported; }

        /// <summary>This class provides access to the x86 SERIALIZE hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        [Intrinsic]
        public new abstract class X64 : X86Base.X64
        {
            internal X64() { }

            /// <summary>Gets <c>true</c> if the APIs in this class are supported; otherwise, <c>false</c> which indicates they will throw <see cref="PlatformNotSupportedException" />.</summary>
            public static new bool IsSupported { get => IsSupported; }
        }

        /// <summary>
        /// void _serialize (void);
        /// </summary>
        public static void Serialize() => Serialize();

    }
}
