// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>This class provides access to X86 AVX512F hardware instructions via intrinsics</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Avx512F : Avx2
    {
        internal Avx512F() { }

        public static new bool IsSupported { get => IsSupported; }

        [Intrinsic]
        public abstract class VL
        {
            internal VL() { }

            public static bool IsSupported { get => IsSupported; }
        }

        [Intrinsic]
        public new abstract class X64 : Avx2.X64
        {
            internal X64() { }

            public static new bool IsSupported { get => IsSupported; }
        }
    }
}
