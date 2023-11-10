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
    public abstract class SveF32mm : AdvSimd
    {
        internal SveF32mm() { }

        public static new bool IsSupported { get => IsSupported; }


        ///  MatrixMultiplyAccumulate : Matrix multiply-accumulate

        /// <summary>
        /// svfloat32_t svmmla[_f32](svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        ///   FMMLA Ztied1.S, Zop2.S, Zop3.S
        ///   MOVPRFX Zresult, Zop1; FMMLA Zresult.S, Zop2.S, Zop3.S
        /// </summary>
        public static unsafe Vector<float> MatrixMultiplyAccumulate(Vector<float> op1, Vector<float> op2, Vector<float> op3) { throw new PlatformNotSupportedException(); }

    }
}

