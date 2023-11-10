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
    public abstract class SveI8mm : AdvSimd
    {
        internal SveI8mm() { }

        public static new bool IsSupported { get => IsSupported; }


        ///  DotProductSignedUnsigned : Dot product (signed × unsigned)

        /// <summary>
        /// svint32_t svsudot[_s32](svint32_t op1, svint8_t op2, svuint8_t op3)
        ///   USDOT Ztied1.S, Zop3.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; USDOT Zresult.S, Zop3.B, Zop2.B
        /// </summary>
        public static unsafe Vector<int> DotProductSignedUnsigned(Vector<int> op1, Vector<sbyte> op2, Vector<byte> op3) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svsudot_lane[_s32](svint32_t op1, svint8_t op2, svuint8_t op3, uint64_t imm_index)
        ///   SUDOT Ztied1.S, Zop2.B, Zop3.B[imm_index]
        ///   MOVPRFX Zresult, Zop1; SUDOT Zresult.S, Zop2.B, Zop3.B[imm_index]
        /// </summary>
        public static unsafe Vector<int> DotProductSignedUnsigned(Vector<int> op1, Vector<sbyte> op2, Vector<byte> op3, ulong imm_index) { throw new PlatformNotSupportedException(); }


        ///  DotProductUnsignedSigned : Dot product (unsigned × signed)

        /// <summary>
        /// svint32_t svusdot[_s32](svint32_t op1, svuint8_t op2, svint8_t op3)
        ///   USDOT Ztied1.S, Zop2.B, Zop3.B
        ///   MOVPRFX Zresult, Zop1; USDOT Zresult.S, Zop2.B, Zop3.B
        /// </summary>
        public static unsafe Vector<int> DotProductUnsignedSigned(Vector<int> op1, Vector<byte> op2, Vector<sbyte> op3) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svusdot_lane[_s32](svint32_t op1, svuint8_t op2, svint8_t op3, uint64_t imm_index)
        ///   USDOT Ztied1.S, Zop2.B, Zop3.B[imm_index]
        ///   MOVPRFX Zresult, Zop1; USDOT Zresult.S, Zop2.B, Zop3.B[imm_index]
        /// </summary>
        public static unsafe Vector<int> DotProductUnsignedSigned(Vector<int> op1, Vector<byte> op2, Vector<sbyte> op3, ulong imm_index) { throw new PlatformNotSupportedException(); }


        ///  MatrixMultiplyAccumulate : Matrix multiply-accumulate

        /// <summary>
        /// svint32_t svmmla[_s32](svint32_t op1, svint8_t op2, svint8_t op3)
        ///   SMMLA Ztied1.S, Zop2.B, Zop3.B
        ///   MOVPRFX Zresult, Zop1; SMMLA Zresult.S, Zop2.B, Zop3.B
        /// </summary>
        public static unsafe Vector<int> MatrixMultiplyAccumulate(Vector<int> op1, Vector<sbyte> op2, Vector<sbyte> op3) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svmmla[_u32](svuint32_t op1, svuint8_t op2, svuint8_t op3)
        ///   UMMLA Ztied1.S, Zop2.B, Zop3.B
        ///   MOVPRFX Zresult, Zop1; UMMLA Zresult.S, Zop2.B, Zop3.B
        /// </summary>
        public static unsafe Vector<uint> MatrixMultiplyAccumulate(Vector<uint> op1, Vector<byte> op2, Vector<byte> op3) { throw new PlatformNotSupportedException(); }


        ///  MatrixMultiplyAccumulateUnsignedSigned : Matrix multiply-accumulate (unsigned × signed)

        /// <summary>
        /// svint32_t svusmmla[_s32](svint32_t op1, svuint8_t op2, svint8_t op3)
        ///   USMMLA Ztied1.S, Zop2.B, Zop3.B
        ///   MOVPRFX Zresult, Zop1; USMMLA Zresult.S, Zop2.B, Zop3.B
        /// </summary>
        public static unsafe Vector<int> MatrixMultiplyAccumulateUnsignedSigned(Vector<int> op1, Vector<byte> op2, Vector<sbyte> op3) { throw new PlatformNotSupportedException(); }

    }
}

