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
    public abstract class Sha3 : AdvSimd
    {
        internal Sha3() { }

        public static new bool IsSupported { get => IsSupported; }

        [Intrinsic]
        public new abstract class Arm64 : AdvSimd.Arm64
        {
            internal Arm64() { }

            public static new bool IsSupported { get => IsSupported; }
        }

        ///  BitwiseClearXor : Bit Clear and Exclusive OR performs a bitwise AND of the 128-bit vector in a source SIMD&FP register and the complement of the vector in another source SIMD&FP register, then performs a bitwise exclusive OR of the resulting vector and the vector in a third source SIMD&FP register, and writes the result to the destination SIMD&FP register.

        /// <summary>
        /// int8x16_t vbcaxq_s8(int8x16_t a, int8x16_t b, int8x16_t c)
        ///   BCAX Vd.16B,Vn.16B,Vm.16B,Va.16B
        ///
        /// codegenarm64test:
        ///    theEmitter->emitIns_R_R_R(INS_sve_bcax, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_D); // BCAX <Zdn>.D, <Zdn>.D, <Zm>.D, <Zk>.D
        /// </summary>
        public static unsafe Vector128<sbyte> BitwiseClearXor(Vector128<sbyte> xor, Vector128<sbyte> value, Vector128<sbyte> mask) => BitwiseClearXor(xor, value, mask);

        /// <summary>
        /// int16x8_t vbcaxq_s16(int16x8_t a, int16x8_t b, int16x8_t c)
        ///   BCAX Vd.16B,Vn.16B,Vm.16B,Va.16B
        ///
        /// codegenarm64test:
        ///    theEmitter->emitIns_R_R_R(INS_sve_bcax, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_D); // BCAX <Zdn>.D, <Zdn>.D, <Zm>.D, <Zk>.D
        /// </summary>
        public static unsafe Vector128<short> BitwiseClearXor(Vector128<short> xor, Vector128<short> value, Vector128<short> mask) => BitwiseClearXor(xor, value, mask);

        /// <summary>
        /// int32x4_t vbcaxq_s32(int32x4_t a, int32x4_t b, int32x4_t c)
        ///   BCAX Vd.16B,Vn.16B,Vm.16B,Va.16B
        ///
        /// codegenarm64test:
        ///    theEmitter->emitIns_R_R_R(INS_sve_bcax, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_D); // BCAX <Zdn>.D, <Zdn>.D, <Zm>.D, <Zk>.D
        /// </summary>
        public static unsafe Vector128<int> BitwiseClearXor(Vector128<int> xor, Vector128<int> value, Vector128<int> mask) => BitwiseClearXor(xor, value, mask);

        /// <summary>
        /// int64x2_t vbcaxq_s64(int64x2_t a, int64x2_t b, int64x2_t c)
        ///   BCAX Vd.16B,Vn.16B,Vm.16B,Va.16B
        ///
        /// codegenarm64test:
        ///    theEmitter->emitIns_R_R_R(INS_sve_bcax, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_D); // BCAX <Zdn>.D, <Zdn>.D, <Zm>.D, <Zk>.D
        /// </summary>
        public static unsafe Vector128<long> BitwiseClearXor(Vector128<long> xor, Vector128<long> value, Vector128<long> mask) => BitwiseClearXor(xor, value, mask);

        /// <summary>
        /// uint8x16_t vbcaxq_u8(uint8x16_t a, uint8x16_t b, uint8x16_t c)
        ///   BCAX Vd.16B,Vn.16B,Vm.16B,Va.16B
        ///
        /// codegenarm64test:
        ///    theEmitter->emitIns_R_R_R(INS_sve_bcax, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_D); // BCAX <Zdn>.D, <Zdn>.D, <Zm>.D, <Zk>.D
        /// </summary>
        public static unsafe Vector128<byte> BitwiseClearXor(Vector128<byte> xor, Vector128<byte> value, Vector128<byte> mask) => BitwiseClearXor(xor, value, mask);

        /// <summary>
        /// uint16x8_t vbcaxq_u16(uint16x8_t a, uint16x8_t b, uint16x8_t c)
        ///   BCAX Vd.16B,Vn.16B,Vm.16B,Va.16B
        ///
        /// codegenarm64test:
        ///    theEmitter->emitIns_R_R_R(INS_sve_bcax, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_D); // BCAX <Zdn>.D, <Zdn>.D, <Zm>.D, <Zk>.D
        /// </summary>
        public static unsafe Vector128<ushort> BitwiseClearXor(Vector128<ushort> xor, Vector128<ushort> value, Vector128<ushort> mask) => BitwiseClearXor(xor, value, mask);

        /// <summary>
        /// uint32x4_t vbcaxq_u32(uint32x4_t a, uint32x4_t b, uint32x4_t c)
        ///   BCAX Vd.16B,Vn.16B,Vm.16B,Va.16B
        ///
        /// codegenarm64test:
        ///    theEmitter->emitIns_R_R_R(INS_sve_bcax, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_D); // BCAX <Zdn>.D, <Zdn>.D, <Zm>.D, <Zk>.D
        /// </summary>
        public static unsafe Vector128<uint> BitwiseClearXor(Vector128<uint> xor, Vector128<uint> value, Vector128<uint> mask) => BitwiseClearXor(xor, value, mask);

        /// <summary>
        /// uint64x2_t vbcaxq_u64(uint64x2_t a, uint64x2_t b, uint64x2_t c)
        ///   BCAX Vd.16B,Vn.16B,Vm.16B,Va.16B
        ///
        /// codegenarm64test:
        ///    theEmitter->emitIns_R_R_R(INS_sve_bcax, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_D); // BCAX <Zdn>.D, <Zdn>.D, <Zm>.D, <Zk>.D
        /// </summary>
        public static unsafe Vector128<ulong> BitwiseClearXor(Vector128<ulong> xor, Vector128<ulong> value, Vector128<ulong> mask) => BitwiseClearXor(xor, value, mask);


        ///  BitwiseRotateLeftBy1AndXor : Rotate and Exclusive OR rotates each 64-bit element of the 128-bit vector in a source SIMD&FP register left by 1, performs a bitwise exclusive OR of the resulting 128-bit vector and the vector in another source SIMD&FP register, and writes the result to the destination SIMD&FP register.

        /// <summary>
        /// uint64x2_t vrax1q_u64(uint64x2_t a, uint64x2_t b)
        ///   RAX1 Vd.2D,Vn.2D,Vm.2D
        ///
        /// codegenarm64test:
        ///    theEmitter->emitIns_R_R_R(INS_sve_rax1, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_D); // RAX1 <Zd>.D, <Zn>.D, <Zm>.D
        /// </summary>
        public static unsafe Vector128<ulong> BitwiseRotateLeftBy1AndXor(Vector128<ulong> a, Vector128<ulong> b) => BitwiseRotateLeftBy1AndXor(a, b);


        ///  Xor : Three-way Exclusive OR performs a three-way exclusive OR of the values in the three source SIMD&FP registers, and writes the result to the destination SIMD&FP register.

        /// <summary>
        /// int8x16_t veor3q_s8(int8x16_t a, int8x16_t b, int8x16_t c)
        ///   EOR3 Vd.16B,Vn.16B,Vm.16B,Va.16B
        ///
        /// codegenarm64test:
        ///    theEmitter->emitIns_R_R_R(INS_sve_eor3, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_D); // EOR3 <Zdn>.D, <Zdn>.D, <Zm>.D, <Zk>.D
        /// </summary>
        public static unsafe Vector128<sbyte> Xor(Vector128<sbyte> value1, Vector128<sbyte> value2, Vector128<sbyte> value3) => Xor(value1, value2, value3);

        /// <summary>
        /// int16x8_t veor3q_s16(int16x8_t a, int16x8_t b, int16x8_t c)
        ///   EOR3 Vd.16B,Vn.16B,Vm.16B,Va.16B
        ///
        /// codegenarm64test:
        ///    theEmitter->emitIns_R_R_R(INS_sve_eor3, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_D); // EOR3 <Zdn>.D, <Zdn>.D, <Zm>.D, <Zk>.D
        /// </summary>
        public static unsafe Vector128<short> Xor(Vector128<short> value1, Vector128<short> value2, Vector128<short> value3) => Xor(value1, value2, value3);

        /// <summary>
        /// int32x4_t veor3q_s32(int32x4_t a, int32x4_t b, int32x4_t c)
        ///   EOR3 Vd.16B,Vn.16B,Vm.16B,Va.16B
        ///
        /// codegenarm64test:
        ///    theEmitter->emitIns_R_R_R(INS_sve_eor3, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_D); // EOR3 <Zdn>.D, <Zdn>.D, <Zm>.D, <Zk>.D
        /// </summary>
        public static unsafe Vector128<int> Xor(Vector128<int> value1, Vector128<int> value2, Vector128<int> value3) => Xor(value1, value2, value3);

        /// <summary>
        /// int64x2_t veor3q_s64(int64x2_t a, int64x2_t b, int64x2_t c)
        ///   EOR3 Vd.16B,Vn.16B,Vm.16B,Va.16B
        ///
        /// codegenarm64test:
        ///    theEmitter->emitIns_R_R_R(INS_sve_eor3, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_D); // EOR3 <Zdn>.D, <Zdn>.D, <Zm>.D, <Zk>.D
        /// </summary>
        public static unsafe Vector128<long> Xor(Vector128<long> value1, Vector128<long> value2, Vector128<long> value3) => Xor(value1, value2, value3);

        /// <summary>
        /// uint8x16_t veor3q_u8(uint8x16_t a, uint8x16_t b, uint8x16_t c)
        ///   EOR3 Vd.16B,Vn.16B,Vm.16B,Va.16B
        ///
        /// codegenarm64test:
        ///    theEmitter->emitIns_R_R_R(INS_sve_eor3, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_D); // EOR3 <Zdn>.D, <Zdn>.D, <Zm>.D, <Zk>.D
        /// </summary>
        public static unsafe Vector128<byte> Xor(Vector128<byte> value1, Vector128<byte> value2, Vector128<byte> value3) => Xor(value1, value2, value3);

        /// <summary>
        /// uint16x8_t veor3q_u16(uint16x8_t a, uint16x8_t b, uint16x8_t c)
        ///   EOR3 Vd.16B,Vn.16B,Vm.16B,Va.16B
        ///
        /// codegenarm64test:
        ///    theEmitter->emitIns_R_R_R(INS_sve_eor3, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_D); // EOR3 <Zdn>.D, <Zdn>.D, <Zm>.D, <Zk>.D
        /// </summary>
        public static unsafe Vector128<ushort> Xor(Vector128<ushort> value1, Vector128<ushort> value2, Vector128<ushort> value3) => Xor(value1, value2, value3);

        /// <summary>
        /// uint32x4_t veor3q_u32(uint32x4_t a, uint32x4_t b, uint32x4_t c)
        ///   EOR3 Vd.16B,Vn.16B,Vm.16B,Va.16B
        ///
        /// codegenarm64test:
        ///    theEmitter->emitIns_R_R_R(INS_sve_eor3, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_D); // EOR3 <Zdn>.D, <Zdn>.D, <Zm>.D, <Zk>.D
        /// </summary>
        public static unsafe Vector128<uint> Xor(Vector128<uint> value1, Vector128<uint> value2, Vector128<uint> value3) => Xor(value1, value2, value3);

        /// <summary>
        /// uint64x2_t veor3q_u64(uint64x2_t a, uint64x2_t b, uint64x2_t c)
        ///   EOR3 Vd.16B,Vn.16B,Vm.16B,Va.16B
        ///
        /// codegenarm64test:
        ///    theEmitter->emitIns_R_R_R(INS_sve_eor3, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_D); // EOR3 <Zdn>.D, <Zdn>.D, <Zm>.D, <Zk>.D
        /// </summary>
        public static unsafe Vector128<ulong> Xor(Vector128<ulong> value1, Vector128<ulong> value2, Vector128<ulong> value3) => Xor(value1, value2, value3);


        ///  XorRotateRight : Exclusive OR and Rotate performs a bitwise exclusive OR of the 128-bit vectors in the two source SIMD&FP registers, rotates each 64-bit element of the resulting 128-bit vector right by the value specified by a 6-bit immediate value, and writes the result to the destination SIMD&FP register.

        /// <summary>
        /// uint64x2_t vxarq_u64(uint64x2_t a, uint64x2_t b, const int imm6)
        ///   XAR Vd.2D,Vn.2D,Vm.2D,imm6
        ///
        /// codegenarm64test:
        ///    theEmitter->emitIns_R_R_I(INS_sve_xar, EA_SCALABLE, REG_V0, REG_V1, 1, INS_OPTS_SCALABLE_B); // XAR <Zdn>.<T>, <Zdn>.<T>, <Zm>.<T>, #<const>
        ///    theEmitter->emitIns_R_R_I(INS_sve_xar, EA_SCALABLE, REG_V2, REG_V3, 8, INS_OPTS_SCALABLE_B); // XAR <Zdn>.<T>, <Zdn>.<T>, <Zm>.<T>, #<const>
        ///    theEmitter->emitIns_R_R_I(INS_sve_xar, EA_SCALABLE, REG_V4, REG_V5, 2, INS_OPTS_SCALABLE_H); // XAR <Zdn>.<T>, <Zdn>.<T>, <Zm>.<T>, #<const>
        ///    theEmitter->emitIns_R_R_I(INS_sve_xar, EA_SCALABLE, REG_V6, REG_V7, 16, INS_OPTS_SCALABLE_H); // XAR <Zdn>.<T>, <Zdn>.<T>, <Zm>.<T>, #<const>
        ///    theEmitter->emitIns_R_R_I(INS_sve_xar, EA_SCALABLE, REG_V8, REG_V9, 3, INS_OPTS_SCALABLE_S); // XAR <Zdn>.<T>, <Zdn>.<T>, <Zm>.<T>, #<const>
        ///    theEmitter->emitIns_R_R_I(INS_sve_xar, EA_SCALABLE, REG_V10, REG_V11, 32, INS_OPTS_SCALABLE_S); // XAR <Zdn>.<T>, <Zdn>.<T>, <Zm>.<T>, #<const>
        ///    theEmitter->emitIns_R_R_I(INS_sve_xar, EA_SCALABLE, REG_V12, REG_V13, 4, INS_OPTS_SCALABLE_D); // XAR <Zdn>.<T>, <Zdn>.<T>, <Zm>.<T>, #<const>
        ///    theEmitter->emitIns_R_R_I(INS_sve_xar, EA_SCALABLE, REG_V14, REG_V15, 64, INS_OPTS_SCALABLE_D); // XAR <Zdn>.<T>, <Zdn>.<T>, <Zm>.<T>, #<const>
        /// </summary>
        public static unsafe Vector128<ulong> XorRotateRight(Vector128<ulong> left, Vector128<ulong> right, [ConstantExpected] byte count) => XorRotateRight(left, right, count);

    }
}

