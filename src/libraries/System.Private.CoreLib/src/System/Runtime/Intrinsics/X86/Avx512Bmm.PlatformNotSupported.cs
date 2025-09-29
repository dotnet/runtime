// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    [CLSCompliant(false)]
    public abstract class Avx512Bmm : Avx512F
    {
        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { [Intrinsic] get { return false; } }

        public static Vector256<ushort> Vbmacor16x16x16(Vector256<ushort> x, Vector256<ushort> y, Vector256<ushort> z) { throw new PlatformNotSupportedException(); }
    }
}
