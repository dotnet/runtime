// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable IDE0060 // unused parameters
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>
    /// This class provides access to the ARM SVE hardware instructions via intrinsics
    /// </summary>
    [CLSCompliant(false)]
    public abstract class Sve : AdvSimd
    {
        internal Sve() { }

        public static new bool IsSupported { [Intrinsic] get => false; }

        [Intrinsic]
        public new abstract class Arm64 : AdvSimd.Arm64
        {
            internal Arm64() { }

            public static new bool IsSupported { [Intrinsic] get => false; }
        }

        // Methods go here.
    }
}
