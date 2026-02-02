// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Avx512Bmm : Avx512F
    {
        internal Avx512Bmm() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { get => IsSupported; }
        public static Vector128<byte> BitReverse(Vector128<byte> x) => BitReverse(x);
        public static Vector256<byte> BitReverse(Vector256<byte> x) => BitReverse(x);
        public static Vector512<byte> BitReverse(Vector512<byte> x) => BitReverse(x);
        public static Vector256<ushort> Vbmacor16x16x16(Vector256<ushort> x, Vector256<ushort> y, Vector256<ushort> z) => Vbmacor16x16x16(x, y, z);
        public static Vector512<ushort> Vbmacor16x16x16(Vector512<ushort> x, Vector512<ushort> y, Vector512<ushort> z) => Vbmacor16x16x16(x, y, z);
        public static Vector256<ushort> Vbmacxor16x16x16(Vector256<ushort> x, Vector256<ushort> y, Vector256<ushort> z) => Vbmacxor16x16x16(x, y, z);
        public static Vector512<ushort> Vbmacxor16x16x16(Vector512<ushort> x, Vector512<ushort> y, Vector512<ushort> z) => Vbmacxor16x16x16(x, y, z);
        [Intrinsic]
        public new abstract class X64 : Avx512F.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { get => IsSupported; }
        }
    }
}
