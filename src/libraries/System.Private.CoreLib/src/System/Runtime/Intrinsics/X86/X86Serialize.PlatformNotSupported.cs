// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>
    /// This class provides access to Intel SERIALIZE hardware instruction via intrinsics
    /// </summary>
    [CLSCompliant(false)]
    public abstract class X86Serialize : X86Base
    {
        internal X86Serialize() { }

        public static new bool IsSupported { [Intrinsic] get => false; }

        [Intrinsic]
        public new abstract class X64 : X86Base.X64
        {
            internal X64() { }

            public static new bool IsSupported { [Intrinsic] get => false; }
        }

        /// <summary>
        /// void _serialize (void);
        /// </summary>
        public static void Serialize() { throw new PlatformNotSupportedException(); }

    }
}
