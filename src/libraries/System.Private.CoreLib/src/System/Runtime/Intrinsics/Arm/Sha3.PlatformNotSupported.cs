// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>Provides access to the ARM Sha3 hardware instructions via intrinsics.</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Sha3 : ArmBase
    {
        internal Sha3() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { get => IsSupported; }

        /// <summary>Provides access to the ARM Sha3 hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        [Intrinsic]
        public new abstract class Arm64 : ArmBase.Arm64
        {
            internal Arm64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { get => IsSupported; }
        }


        // Bit Clear and Exclusive OR

        /// <summary>
        /// uint8x16_t vbcaxq_u8(uint8x16_t a, uint8x16_t b, uint8x16_t c)
        ///   BCAX Vd.16B,Vn.16B,Vm.16B,Va.16B
        /// </summary>
        public static Vector128<byte> BitwiseClearXor(Vector128<byte> xor, Vector128<byte> value, Vector128<byte> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x8_t vbcaxq_s16(int16x8_t a, int16x8_t b, int16x8_t c)
        ///   BCAX Vd.16B,Vn.16B,Vm.16B,Va.16B
        /// </summary>
        public static Vector128<short> BitwiseClearXor(Vector128<short> xor, Vector128<short> value, Vector128<short> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x4_t vbcaxq_s32(int32x4_t a, int32x4_t b, int32x4_t c)
        ///   BCAX Vd.16B,Vn.16B,Vm.16B,Va.16B
        /// </summary>
        public static Vector128<int> BitwiseClearXor(Vector128<int> xor, Vector128<int> value, Vector128<int> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x2_t vbcaxq_s64(int64x2_t a, int64x2_t b, int64x2_t c)
        ///   BCAX Vd.16B,Vn.16B,Vm.16B,Va.16B
        /// </summary>
        public static Vector128<long> BitwiseClearXor(Vector128<long> xor, Vector128<long> value, Vector128<long> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x16_t vbcaxq_s8(int8x16_t a, int8x16_t b, int8x16_t c)
        ///   BCAX Vd.16B,Vn.16B,Vm.16B,Va.16B
        /// </summary>
        public static Vector128<sbyte> BitwiseClearXor(Vector128<sbyte> xor, Vector128<sbyte> value, Vector128<sbyte> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x8_t vbcaxq_u16(uint16x8_t a, uint16x8_t b, uint16x8_t c)
        ///   BCAX Vd.16B,Vn.16B,Vm.16B,Va.16B
        /// </summary>
        public static Vector128<ushort> BitwiseClearXor(Vector128<ushort> xor, Vector128<ushort> value, Vector128<ushort> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x4_t vbcaxq_u32(uint32x4_t a, uint32x4_t b, uint32x4_t c)
        ///   BCAX Vd.16B,Vn.16B,Vm.16B,Va.16B
        /// </summary>
        public static Vector128<uint> BitwiseClearXor(Vector128<uint> xor, Vector128<uint> value, Vector128<uint> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x2_t vbcaxq_u64(uint64x2_t a, uint64x2_t b, uint64x2_t c)
        ///   BCAX Vd.16B,Vn.16B,Vm.16B,Va.16B
        /// </summary>
        public static Vector128<ulong> BitwiseClearXor(Vector128<ulong> xor, Vector128<ulong> value, Vector128<ulong> mask) { throw new PlatformNotSupportedException(); }


        // Rotate and Exclusive OR

        /// <summary>
        /// uint64x2_t vrax1q_u64(uint64x2_t a, uint64x2_t b)
        ///   RAX1 Vd.2D,Vn.2D,Vm.2D
        /// </summary>
        public static Vector128<ulong> BitwiseRotateLeftBy1AndXor(Vector128<ulong> xor, Vector128<ulong> rol1) { throw new PlatformNotSupportedException(); }


        // Three-way Exclusive OR performs

        /// <summary>
        /// uint8x16_t veor3q_u8(uint8x16_t a, uint8x16_t b, uint8x16_t c)
        ///   EOR3 Vd.16B,Vn.16B,Vm.16B,Va.16B
        /// </summary>
        public static Vector128<byte> Xor(Vector128<byte> value1, Vector128<byte> value2, Vector128<byte> value3) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x8_t veor3q_s16(int16x8_t a, int16x8_t b, int16x8_t c)
        ///   EOR3 Vd.16B,Vn.16B,Vm.16B,Va.16B
        /// </summary>
        public static Vector128<short> Xor(Vector128<short> value1, Vector128<short> value2, Vector128<short> value3) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x4_t veor3q_s32(int32x4_t a, int32x4_t b, int32x4_t c)
        ///   EOR3 Vd.16B,Vn.16B,Vm.16B,Va.16B
        /// </summary>
        public static Vector128<int> Xor(Vector128<int> value1, Vector128<int> value2, Vector128<int> value3) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x2_t veor3q_s64(int64x2_t a, int64x2_t b, int64x2_t c)
        ///   EOR3 Vd.16B,Vn.16B,Vm.16B,Va.16B
        /// </summary>
        public static Vector128<long> Xor(Vector128<long> value1, Vector128<long> value2, Vector128<long> value3) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x16_t veor3q_s8(int8x16_t a, int8x16_t b, int8x16_t c)
        ///   EOR3 Vd.16B,Vn.16B,Vm.16B,Va.16B
        /// </summary>
        public static Vector128<sbyte> Xor(Vector128<sbyte> value1, Vector128<sbyte> value2, Vector128<sbyte> value3) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x8_t veor3q_u16(uint16x8_t a, uint16x8_t b, uint16x8_t c)
        ///   EOR3 Vd.16B,Vn.16B,Vm.16B,Va.16B
        /// </summary>
        public static Vector128<ushort> Xor(Vector128<ushort> value1, Vector128<ushort> value2, Vector128<ushort> value3) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x4_t veor3q_u32(uint32x4_t a, uint32x4_t b, uint32x4_t c)
        ///   EOR3 Vd.16B,Vn.16B,Vm.16B,Va.16B
        /// </summary>
        public static Vector128<uint> Xor(Vector128<uint> value1, Vector128<uint> value2, Vector128<uint> value3) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x2_t veor3q_u64(uint64x2_t a, uint64x2_t b, uint64x2_t c)
        ///   EOR3 Vd.16B,Vn.16B,Vm.16B,Va.16B
        /// </summary>
        public static Vector128<ulong> Xor(Vector128<ulong> value1, Vector128<ulong> value2, Vector128<ulong> value3) { throw new PlatformNotSupportedException(); }


        // Exclusive OR and Rotate

        /// <summary>
        /// uint64x2_t vxarq_u64(uint64x2_t a, uint64x2_t b, const int imm6)
        ///   XAR Vd.2D,Vn.2D,Vm.2D,imm6
        /// </summary>
        public static Vector128<ulong> XorRotateRight(Vector128<ulong> left, Vector128<ulong> right, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

    }
}
