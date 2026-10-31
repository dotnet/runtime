// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>Provides access to the ARM FEAT_SVE_PMULL128 hardware instructions via intrinsics.</summary>
    [CLSCompliant(false)]
    [Experimental(Experimentals.ArmSveDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public abstract class SvePmull128 : ArmBase
    {
        internal SvePmull128() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { [Intrinsic] get => false; }

        /// <summary>Provides access to the ARM FEAT_SVE_PMULL128 hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        public new abstract class Arm64 : ArmBase.Arm64
        {
            internal Arm64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { [Intrinsic] get { return false; } }
        }

        public static Vector<ulong> PolynomialMultiplyWideningEven(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        public static Vector<ulong> PolynomialMultiplyWideningOdd(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }
    }
}
