// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Numerics;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>
    /// This class provides access to the ARM SVE hardware instructions via intrinsics
    /// </summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class SveSha3 : AdvSimd
    {
        internal SveSha3() { }

        public static new bool IsSupported { get => IsSupported; }


        ///  BitwiseRotateLeftBy1AndXor : Bitwise rotate left by 1 and exclusive OR

        /// <summary>
        /// svint64_t svrax1[_s64](svint64_t op1, svint64_t op2)
        ///   RAX1 Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<long> BitwiseRotateLeftBy1AndXor(Vector<long> left, Vector<long> right) => BitwiseRotateLeftBy1AndXor(left, right);

        /// <summary>
        /// svuint64_t svrax1[_u64](svuint64_t op1, svuint64_t op2)
        ///   RAX1 Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<ulong> BitwiseRotateLeftBy1AndXor(Vector<ulong> left, Vector<ulong> right) => BitwiseRotateLeftBy1AndXor(left, right);

    }
}

