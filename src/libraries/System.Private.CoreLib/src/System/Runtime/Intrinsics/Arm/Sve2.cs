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
    [Experimental(Experimentals.ArmSveDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public abstract class Sve2 : Sve
    {
        internal Sve2() { }

        public static new bool IsSupported { get => IsSupported; }

        [Intrinsic]
        public new abstract class Arm64 : Sve.Arm64
        {
            internal Arm64() { }

            public static new bool IsSupported { get => IsSupported; }
        }

        // Absolute difference and accumulate

        /// <summary>
        /// svuint8_t svaba[_u8](svuint8_t op1, svuint8_t op2, svuint8_t op3)
        ///   UABA Ztied1.B, Zop2.B, Zop3.B
        /// </summary>
        public static unsafe Vector<byte> AbsoluteDifferenceAdd(Vector<byte> addend, Vector<byte> left, Vector<byte> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// svint16_t svaba[_s16](svint16_t op1, svint16_t op2, svint16_t op3)
        ///   SABA Ztied1.H, Zop2.H, Zop3.H
        /// </summary>
        public static unsafe Vector<short> AbsoluteDifferenceAdd(Vector<short> addend, Vector<short> left, Vector<short> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// svint32_t svaba[_s32](svint32_t op1, svint32_t op2, svint32_t op3)
        ///   SABA Ztied1.S, Zop2.S, Zop3.S
        /// </summary>
        public static unsafe Vector<int> AbsoluteDifferenceAdd(Vector<int> addend, Vector<int> left, Vector<int> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// svint64_t svaba[_s64](svint64_t op1, svint64_t op2, svint64_t op3)
        ///   SABA Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static unsafe Vector<long> AbsoluteDifferenceAdd(Vector<long> addend, Vector<long> left, Vector<long> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// svint8_t svaba[_s8](svint8_t op1, svint8_t op2, svint8_t op3)
        ///   SABA Ztied1.B, Zop2.B, Zop3.B
        /// </summary>
        public static unsafe Vector<sbyte> AbsoluteDifferenceAdd(Vector<sbyte> addend, Vector<sbyte> left, Vector<sbyte> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// svuint16_t svaba[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3)
        ///   UABA Ztied1.H, Zop2.H, Zop3.H
        /// </summary>
        public static unsafe Vector<ushort> AbsoluteDifferenceAdd(Vector<ushort> addend, Vector<ushort> left, Vector<ushort> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// svuint32_t svaba[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3)
        ///   UABA Ztied1.S, Zop2.S, Zop3.S
        /// </summary>
        public static unsafe Vector<uint> AbsoluteDifferenceAdd(Vector<uint> addend, Vector<uint> left, Vector<uint> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// svuint64_t svaba[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3)
        ///   UABA Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static unsafe Vector<ulong> AbsoluteDifferenceAdd(Vector<ulong> addend, Vector<ulong> left, Vector<ulong> right) => AbsoluteDifferenceAdd(addend, left, right);

        // Absolute difference and accumulate long (bottom)

        /// <summary>
        /// svint16_t svabalb[_s16](svint16_t op1, svint8_t op2, svint8_t op3)
        ///   SABALB Ztied1.H, Zop2.B, Zop3.B
        /// </summary>
        public static unsafe Vector<short> AbsoluteDifferenceAddWideningLower(Vector<short> addend, Vector<sbyte> left, Vector<sbyte> right) => AbsoluteDifferenceAddWideningLower(addend, left, right);

        /// <summary>
        /// svint32_t svabalb[_s32](svint32_t op1, svint16_t op2, svint16_t op3)
        ///   SABALB Ztied1.S, Zop2.H, Zop3.H
        /// </summary>
        public static unsafe Vector<int> AbsoluteDifferenceAddWideningLower(Vector<int> addend, Vector<short> left, Vector<short> right) => AbsoluteDifferenceAddWideningLower(addend, left, right);

        /// <summary>
        /// svint64_t svabalb[_s64](svint64_t op1, svint32_t op2, svint32_t op3)
        ///   SABALB Ztied1.D, Zop2.S, Zop3.S
        /// </summary>
        public static unsafe Vector<long> AbsoluteDifferenceAddWideningLower(Vector<long> addend, Vector<int> left, Vector<int> right) => AbsoluteDifferenceAddWideningLower(addend, left, right);

        /// <summary>
        /// svuint16_t svabalb[_u16](svuint16_t op1, svuint8_t op2, svuint8_t op3)
        ///   UABALB Ztied1.H, Zop2.B, Zop3.B
        /// </summary>
        public static unsafe Vector<ushort> AbsoluteDifferenceAddWideningLower(Vector<ushort> addend, Vector<byte> left, Vector<byte> right) => AbsoluteDifferenceAddWideningLower(addend, left, right);

        /// <summary>
        /// svuint32_t svabalb[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3)
        ///   UABALB Ztied1.S, Zop2.H, Zop3.H
        /// </summary>
        public static unsafe Vector<uint> AbsoluteDifferenceAddWideningLower(Vector<uint> addend, Vector<ushort> left, Vector<ushort> right) => AbsoluteDifferenceAddWideningLower(addend, left, right);

        /// <summary>
        /// svuint64_t svabalb[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3)
        ///   UABALB Ztied1.D, Zop2.S, Zop3.S
        /// </summary>
        public static unsafe Vector<ulong> AbsoluteDifferenceAddWideningLower(Vector<ulong> addend, Vector<uint> left, Vector<uint> right) => AbsoluteDifferenceAddWideningLower(addend, left, right);

        // Absolute difference and accumulate long (top)

        /// <summary>
        /// svint16_t svabalt[_s16](svint16_t op1, svint8_t op2, svint8_t op3)
        ///   SABALT Ztied1.H, Zop2.B, Zop3.B
        /// </summary>
        public static unsafe Vector<short> AbsoluteDifferenceAddWideningUpper(Vector<short> addend, Vector<sbyte> left, Vector<sbyte> right) => AbsoluteDifferenceAddWideningUpper(addend, left, right);

        /// <summary>
        /// svint32_t svabalt[_s32](svint32_t op1, svint16_t op2, svint16_t op3)
        ///   SABALT Ztied1.S, Zop2.H, Zop3.H
        /// </summary>
        public static unsafe Vector<int> AbsoluteDifferenceAddWideningUpper(Vector<int> addend, Vector<short> left, Vector<short> right) => AbsoluteDifferenceAddWideningUpper(addend, left, right);

        /// <summary>
        /// svint64_t svabalt[_s64](svint64_t op1, svint32_t op2, svint32_t op3)
        ///   SABALT Ztied1.D, Zop2.S, Zop3.S
        /// </summary>
        public static unsafe Vector<long> AbsoluteDifferenceAddWideningUpper(Vector<long> addend, Vector<int> left, Vector<int> right) => AbsoluteDifferenceAddWideningUpper(addend, left, right);

        /// <summary>
        /// svuint16_t svabalt[_u16](svuint16_t op1, svuint8_t op2, svuint8_t op3)
        ///   UABALT Ztied1.H, Zop2.B, Zop3.B
        /// </summary>
        public static unsafe Vector<ushort> AbsoluteDifferenceAddWideningUpper(Vector<ushort> addend, Vector<byte> left, Vector<byte> right) => AbsoluteDifferenceAddWideningUpper(addend, left, right);

        /// <summary>
        /// svuint32_t svabalt[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3)
        ///   UABALT Ztied1.S, Zop2.H, Zop3.H
        /// </summary>
        public static unsafe Vector<uint> AbsoluteDifferenceAddWideningUpper(Vector<uint> addend, Vector<ushort> left, Vector<ushort> right) => AbsoluteDifferenceAddWideningUpper(addend, left, right);

        /// <summary>
        /// svuint64_t svabalt[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3)
        ///   UABALT Ztied1.D, Zop2.S, Zop3.S
        /// </summary>
        public static unsafe Vector<ulong> AbsoluteDifferenceAddWideningUpper(Vector<ulong> addend, Vector<uint> left, Vector<uint> right) => AbsoluteDifferenceAddWideningUpper(addend, left, right);

        // Bitwise clear and exclusive OR

        /// <summary>
        /// svuint8_t svbcax[_u8](svuint8_t op1, svuint8_t op2, svuint8_t op3)
        ///   BCAX Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static unsafe Vector<byte> BitwiseClearXor(Vector<byte> xor, Vector<byte> value, Vector<byte> mask) => BitwiseClearXor(xor, value, mask);

        /// <summary>
        /// svint16_t svbcax[_s16](svint16_t op1, svint16_t op2, svint16_t op3)
        ///   BCAX Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static unsafe Vector<short> BitwiseClearXor(Vector<short> xor, Vector<short> value, Vector<short> mask) => BitwiseClearXor(xor, value, mask);

        /// <summary>
        /// svint32_t svbcax[_s32](svint32_t op1, svint32_t op2, svint32_t op3)
        ///   BCAX Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static unsafe Vector<int> BitwiseClearXor(Vector<int> xor, Vector<int> value, Vector<int> mask) => BitwiseClearXor(xor, value, mask);

        /// <summary>
        /// svint64_t svbcax[_s64](svint64_t op1, svint64_t op2, svint64_t op3)
        ///   BCAX Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static unsafe Vector<long> BitwiseClearXor(Vector<long> xor, Vector<long> value, Vector<long> mask) => BitwiseClearXor(xor, value, mask);

        /// <summary>
        /// svint8_t svbcax[_s8](svint8_t op1, svint8_t op2, svint8_t op3)
        ///   BCAX Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static unsafe Vector<sbyte> BitwiseClearXor(Vector<sbyte> xor, Vector<sbyte> value, Vector<sbyte> mask) => BitwiseClearXor(xor, value, mask);

        /// <summary>
        /// svuint16_t svbcax[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3)
        ///   BCAX Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static unsafe Vector<ushort> BitwiseClearXor(Vector<ushort> xor, Vector<ushort> value, Vector<ushort> mask) => BitwiseClearXor(xor, value, mask);

        /// <summary>
        /// svuint32_t svbcax[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3)
        ///   BCAX Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static unsafe Vector<uint> BitwiseClearXor(Vector<uint> xor, Vector<uint> value, Vector<uint> mask) => BitwiseClearXor(xor, value, mask);

        /// <summary>
        /// svuint64_t svbcax[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3)
        ///   BCAX Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static unsafe Vector<ulong> BitwiseClearXor(Vector<ulong> xor, Vector<ulong> value, Vector<ulong> mask) => BitwiseClearXor(xor, value, mask);
    }
}
