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
        public static Vector<byte> AbsoluteDifferenceAdd(Vector<byte> addend, Vector<byte> left, Vector<byte> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// svint16_t svaba[_s16](svint16_t op1, svint16_t op2, svint16_t op3)
        ///   SABA Ztied1.H, Zop2.H, Zop3.H
        /// </summary>
        public static Vector<short> AbsoluteDifferenceAdd(Vector<short> addend, Vector<short> left, Vector<short> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// svint32_t svaba[_s32](svint32_t op1, svint32_t op2, svint32_t op3)
        ///   SABA Ztied1.S, Zop2.S, Zop3.S
        /// </summary>
        public static Vector<int> AbsoluteDifferenceAdd(Vector<int> addend, Vector<int> left, Vector<int> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// svint64_t svaba[_s64](svint64_t op1, svint64_t op2, svint64_t op3)
        ///   SABA Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<long> AbsoluteDifferenceAdd(Vector<long> addend, Vector<long> left, Vector<long> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// svint8_t svaba[_s8](svint8_t op1, svint8_t op2, svint8_t op3)
        ///   SABA Ztied1.B, Zop2.B, Zop3.B
        /// </summary>
        public static Vector<sbyte> AbsoluteDifferenceAdd(Vector<sbyte> addend, Vector<sbyte> left, Vector<sbyte> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// svuint16_t svaba[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3)
        ///   UABA Ztied1.H, Zop2.H, Zop3.H
        /// </summary>
        public static Vector<ushort> AbsoluteDifferenceAdd(Vector<ushort> addend, Vector<ushort> left, Vector<ushort> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// svuint32_t svaba[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3)
        ///   UABA Ztied1.S, Zop2.S, Zop3.S
        /// </summary>
        public static Vector<uint> AbsoluteDifferenceAdd(Vector<uint> addend, Vector<uint> left, Vector<uint> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// svuint64_t svaba[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3)
        ///   UABA Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<ulong> AbsoluteDifferenceAdd(Vector<ulong> addend, Vector<ulong> left, Vector<ulong> right) => AbsoluteDifferenceAdd(addend, left, right);

        // Absolute difference and accumulate long (bottom)

        /// <summary>
        /// svint16_t svabalb[_s16](svint16_t op1, svint8_t op2, svint8_t op3)
        ///   SABALB Ztied1.H, Zop2.B, Zop3.B
        /// </summary>
        public static Vector<short> AbsoluteDifferenceAddWideningLower(Vector<short> addend, Vector<sbyte> left, Vector<sbyte> right) => AbsoluteDifferenceAddWideningLower(addend, left, right);

        /// <summary>
        /// svint32_t svabalb[_s32](svint32_t op1, svint16_t op2, svint16_t op3)
        ///   SABALB Ztied1.S, Zop2.H, Zop3.H
        /// </summary>
        public static Vector<int> AbsoluteDifferenceAddWideningLower(Vector<int> addend, Vector<short> left, Vector<short> right) => AbsoluteDifferenceAddWideningLower(addend, left, right);

        /// <summary>
        /// svint64_t svabalb[_s64](svint64_t op1, svint32_t op2, svint32_t op3)
        ///   SABALB Ztied1.D, Zop2.S, Zop3.S
        /// </summary>
        public static Vector<long> AbsoluteDifferenceAddWideningLower(Vector<long> addend, Vector<int> left, Vector<int> right) => AbsoluteDifferenceAddWideningLower(addend, left, right);

        /// <summary>
        /// svuint16_t svabalb[_u16](svuint16_t op1, svuint8_t op2, svuint8_t op3)
        ///   UABALB Ztied1.H, Zop2.B, Zop3.B
        /// </summary>
        public static Vector<ushort> AbsoluteDifferenceAddWideningLower(Vector<ushort> addend, Vector<byte> left, Vector<byte> right) => AbsoluteDifferenceAddWideningLower(addend, left, right);

        /// <summary>
        /// svuint32_t svabalb[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3)
        ///   UABALB Ztied1.S, Zop2.H, Zop3.H
        /// </summary>
        public static Vector<uint> AbsoluteDifferenceAddWideningLower(Vector<uint> addend, Vector<ushort> left, Vector<ushort> right) => AbsoluteDifferenceAddWideningLower(addend, left, right);

        /// <summary>
        /// svuint64_t svabalb[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3)
        ///   UABALB Ztied1.D, Zop2.S, Zop3.S
        /// </summary>
        public static Vector<ulong> AbsoluteDifferenceAddWideningLower(Vector<ulong> addend, Vector<uint> left, Vector<uint> right) => AbsoluteDifferenceAddWideningLower(addend, left, right);

        // Absolute difference and accumulate long (top)

        /// <summary>
        /// svint16_t svabalt[_s16](svint16_t op1, svint8_t op2, svint8_t op3)
        ///   SABALT Ztied1.H, Zop2.B, Zop3.B
        /// </summary>
        public static Vector<short> AbsoluteDifferenceAddWideningUpper(Vector<short> addend, Vector<sbyte> left, Vector<sbyte> right) => AbsoluteDifferenceAddWideningUpper(addend, left, right);

        /// <summary>
        /// svint32_t svabalt[_s32](svint32_t op1, svint16_t op2, svint16_t op3)
        ///   SABALT Ztied1.S, Zop2.H, Zop3.H
        /// </summary>
        public static Vector<int> AbsoluteDifferenceAddWideningUpper(Vector<int> addend, Vector<short> left, Vector<short> right) => AbsoluteDifferenceAddWideningUpper(addend, left, right);

        /// <summary>
        /// svint64_t svabalt[_s64](svint64_t op1, svint32_t op2, svint32_t op3)
        ///   SABALT Ztied1.D, Zop2.S, Zop3.S
        /// </summary>
        public static Vector<long> AbsoluteDifferenceAddWideningUpper(Vector<long> addend, Vector<int> left, Vector<int> right) => AbsoluteDifferenceAddWideningUpper(addend, left, right);

        /// <summary>
        /// svuint16_t svabalt[_u16](svuint16_t op1, svuint8_t op2, svuint8_t op3)
        ///   UABALT Ztied1.H, Zop2.B, Zop3.B
        /// </summary>
        public static Vector<ushort> AbsoluteDifferenceAddWideningUpper(Vector<ushort> addend, Vector<byte> left, Vector<byte> right) => AbsoluteDifferenceAddWideningUpper(addend, left, right);

        /// <summary>
        /// svuint32_t svabalt[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3)
        ///   UABALT Ztied1.S, Zop2.H, Zop3.H
        /// </summary>
        public static Vector<uint> AbsoluteDifferenceAddWideningUpper(Vector<uint> addend, Vector<ushort> left, Vector<ushort> right) => AbsoluteDifferenceAddWideningUpper(addend, left, right);

        /// <summary>
        /// svuint64_t svabalt[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3)
        ///   UABALT Ztied1.D, Zop2.S, Zop3.S
        /// </summary>
        public static Vector<ulong> AbsoluteDifferenceAddWideningUpper(Vector<ulong> addend, Vector<uint> left, Vector<uint> right) => AbsoluteDifferenceAddWideningUpper(addend, left, right);

        // Absolute difference long (bottom)

        /// <summary>
        /// svint16_t svabdlb[_s16](svint8_t op1, svint8_t op2)
        ///   SABDLB Zresult.H, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<short> AbsoluteDifferenceWideningLower(Vector<sbyte> left, Vector<sbyte> right) => AbsoluteDifferenceWideningLower(left, right);

        /// <summary>
        /// svint32_t svabdlb[_s32](svint16_t op1, svint16_t op2)
        ///   SABDLB Zresult.S, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<int> AbsoluteDifferenceWideningLower(Vector<short> left, Vector<short> right) => AbsoluteDifferenceWideningLower(left, right);

        /// <summary>
        /// svint64_t svabdlb[_s64](svint32_t op1, svint32_t op2)
        ///   SABDLB Zresult.D, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<long> AbsoluteDifferenceWideningLower(Vector<int> left, Vector<int> right) => AbsoluteDifferenceWideningLower(left, right);

        /// <summary>
        /// svuint16_t svabdlb[_u16](svuint8_t op1, svuint8_t op2)
        ///   UABDLB Zresult.H, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<ushort> AbsoluteDifferenceWideningLower(Vector<byte> left, Vector<byte> right) => AbsoluteDifferenceWideningLower(left, right);

        /// <summary>
        /// svuint32_t svabdlb[_u32](svuint16_t op1, svuint16_t op2)
        ///   UABDLB Zresult.S, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<uint> AbsoluteDifferenceWideningLower(Vector<ushort> left, Vector<ushort> right) => AbsoluteDifferenceWideningLower(left, right);

        /// <summary>
        /// svuint64_t svabdlb[_u64](svuint32_t op1, svuint32_t op2)
        ///   UABDLB Zresult.D, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<ulong> AbsoluteDifferenceWideningLower(Vector<uint> left, Vector<uint> right) => AbsoluteDifferenceWideningLower(left, right);

        // Absolute difference long (top)

        /// <summary>
        /// svint16_t svabdlt[_s16](svint8_t op1, svint8_t op2)
        ///   SABDLT Zresult.H, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<short> AbsoluteDifferenceWideningUpper(Vector<sbyte> left, Vector<sbyte> right) => AbsoluteDifferenceWideningUpper(left, right);

        /// <summary>
        /// svint32_t svabdlt[_s32](svint16_t op1, svint16_t op2)
        ///   SABDLT Zresult.S, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<int> AbsoluteDifferenceWideningUpper(Vector<short> left, Vector<short> right) => AbsoluteDifferenceWideningUpper(left, right);

        /// <summary>
        /// svint64_t svabdlt[_s64](svint32_t op1, svint32_t op2)
        ///   SABDLT Zresult.D, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<long> AbsoluteDifferenceWideningUpper(Vector<int> left, Vector<int> right) => AbsoluteDifferenceWideningUpper(left, right);

        /// <summary>
        /// svuint16_t svabdlt[_u16](svuint8_t op1, svuint8_t op2)
        ///   UABDLT Zresult.H, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<ushort> AbsoluteDifferenceWideningUpper(Vector<byte> left, Vector<byte> right) => AbsoluteDifferenceWideningUpper(left, right);

        /// <summary>
        /// svuint32_t svabdlt[_u32](svuint16_t op1, svuint16_t op2)
        ///   UABDLT Zresult.S, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<uint> AbsoluteDifferenceWideningUpper(Vector<ushort> left, Vector<ushort> right) => AbsoluteDifferenceWideningUpper(left, right);

        /// <summary>
        /// svuint64_t svabdlt[_u64](svuint32_t op1, svuint32_t op2)
        ///   UABDLT Zresult.D, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<ulong> AbsoluteDifferenceWideningUpper(Vector<uint> left, Vector<uint> right) => AbsoluteDifferenceWideningUpper(left, right);

        // Add with carry long (bottom)

        /// <summary>
        /// svuint32_t svadclb[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3)
        ///   ADCLB Ztied1.S, Zop2.S, Zop3.S
        /// </summary>
        public static unsafe Vector<uint> AddCarryWideningLower(Vector<uint> op1, Vector<uint> op2, Vector<uint> op3) => AddCarryWideningLower(op1, op2, op3);

        /// <summary>
        /// svuint64_t svadclb[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3)
        ///   ADCLB Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static unsafe Vector<ulong> AddCarryWideningLower(Vector<ulong> op1, Vector<ulong> op2, Vector<ulong> op3) => AddCarryWideningLower(op1, op2, op3);

        // Add with carry long (top)

        /// <summary>
        /// svuint32_t svadclt[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3)
        ///   ADCLT Ztied1.S, Zop2.S, Zop3.S
        /// </summary>
        public static unsafe Vector<uint> AddCarryWideningUpper(Vector<uint> op1, Vector<uint> op2, Vector<uint> op3) => AddCarryWideningUpper(op1, op2, op3);

        /// <summary>
        /// svuint64_t svadclt[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3)
        ///   ADCLT Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static unsafe Vector<ulong> AddCarryWideningUpper(Vector<ulong> op1, Vector<ulong> op2, Vector<ulong> op3) => AddCarryWideningUpper(op1, op2, op3);

        // Add narrow high part (bottom)

        /// <summary>
        /// svuint8_t svaddhnb[_u16](svuint16_t op1, svuint16_t op2)
        ///   ADDHNB Zresult.B, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<byte> AddHighNarrowingEven(Vector<ushort> left, Vector<ushort> right) => AddHighNarrowingEven(left, right);

        /// <summary>
        /// svint16_t svaddhnb[_s32](svint32_t op1, svint32_t op2)
        ///   ADDHNB Zresult.H, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<short> AddHighNarrowingEven(Vector<int> left, Vector<int> right) => AddHighNarrowingEven(left, right);

        /// <summary>
        /// svint32_t svaddhnb[_s64](svint64_t op1, svint64_t op2)
        ///   ADDHNB Zresult.S, Zop1.D, Zop2.D
        /// </summary>
        public static Vector<int> AddHighNarrowingEven(Vector<long> left, Vector<long> right) => AddHighNarrowingEven(left, right);

        /// <summary>
        /// svint8_t svaddhnb[_s16](svint16_t op1, svint16_t op2)
        ///   ADDHNB Zresult.B, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<sbyte> AddHighNarrowingEven(Vector<short> left, Vector<short> right) => AddHighNarrowingEven(left, right);

        /// <summary>
        /// svuint16_t svaddhnb[_u32](svuint32_t op1, svuint32_t op2)
        ///   ADDHNB Zresult.H, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<ushort> AddHighNarrowingEven(Vector<uint> left, Vector<uint> right) => AddHighNarrowingEven(left, right);

        /// <summary>
        /// svuint32_t svaddhnb[_u64](svuint64_t op1, svuint64_t op2)
        ///   ADDHNB Zresult.S, Zop1.D, Zop2.D
        /// </summary>
        public static Vector<uint> AddHighNarrowingEven(Vector<ulong> left, Vector<ulong> right) => AddHighNarrowingEven(left, right);

        // Add narrow high part (top)

        /// <summary>
        /// svuint8_t svaddhnt[_u16](svuint8_t even, svuint16_t op1, svuint16_t op2)
        ///   ADDHNT Ztied.B, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<byte> AddHighNarrowingOdd(Vector<byte> even, Vector<ushort> left, Vector<ushort> right) => AddHighNarrowingOdd(even, left, right);

        /// <summary>
        /// svint16_t svaddhnt[_s32](svint16_t even, svint32_t op1, svint32_t op2)
        ///   ADDHNT Ztied.H, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<short> AddHighNarrowingOdd(Vector<short> even, Vector<int> left, Vector<int> right) => AddHighNarrowingOdd(even, left, right);

        /// <summary>
        /// svint32_t svaddhnt[_s64](svint32_t even, svint64_t op1, svint64_t op2)
        ///   ADDHNT Ztied.S, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<int> AddHighNarrowingOdd(Vector<int> even, Vector<long> left, Vector<long> right) => AddHighNarrowingOdd(even, left, right);

        /// <summary>
        /// svint8_t svaddhnt[_s16](svint8_t even, svint16_t op1, svint16_t op2)
        ///   ADDHNT Ztied.B, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<sbyte> AddHighNarrowingOdd(Vector<sbyte> even, Vector<short> left, Vector<short> right) => AddHighNarrowingOdd(even, left, right);

        /// <summary>
        /// svuint16_t svaddhnt[_u32](svuint16_t even, svuint32_t op1, svuint32_t op2)
        ///   ADDHNT Ztied.H, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<ushort> AddHighNarrowingOdd(Vector<ushort> even, Vector<uint> left, Vector<uint> right) => AddHighNarrowingOdd(even, left, right);

        /// <summary>
        /// svuint32_t svaddhnt[_u64](svuint32_t even, svuint64_t op1, svuint64_t op2)
        ///   ADDHNT Ztied.S, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<uint> AddHighNarrowingOdd(Vector<uint> even, Vector<ulong> left, Vector<ulong> right) => AddHighNarrowingOdd(even, left, right);

        // Bitwise clear and exclusive OR

        /// <summary>
        /// svuint8_t svbcax[_u8](svuint8_t op1, svuint8_t op2, svuint8_t op3)
        ///   BCAX Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<byte> BitwiseClearXor(Vector<byte> xor, Vector<byte> value, Vector<byte> mask) => BitwiseClearXor(xor, value, mask);

        /// <summary>
        /// svint16_t svbcax[_s16](svint16_t op1, svint16_t op2, svint16_t op3)
        ///   BCAX Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<short> BitwiseClearXor(Vector<short> xor, Vector<short> value, Vector<short> mask) => BitwiseClearXor(xor, value, mask);

        /// <summary>
        /// svint32_t svbcax[_s32](svint32_t op1, svint32_t op2, svint32_t op3)
        ///   BCAX Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<int> BitwiseClearXor(Vector<int> xor, Vector<int> value, Vector<int> mask) => BitwiseClearXor(xor, value, mask);

        /// <summary>
        /// svint64_t svbcax[_s64](svint64_t op1, svint64_t op2, svint64_t op3)
        ///   BCAX Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<long> BitwiseClearXor(Vector<long> xor, Vector<long> value, Vector<long> mask) => BitwiseClearXor(xor, value, mask);

        /// <summary>
        /// svint8_t svbcax[_s8](svint8_t op1, svint8_t op2, svint8_t op3)
        ///   BCAX Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<sbyte> BitwiseClearXor(Vector<sbyte> xor, Vector<sbyte> value, Vector<sbyte> mask) => BitwiseClearXor(xor, value, mask);

        /// <summary>
        /// svuint16_t svbcax[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3)
        ///   BCAX Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<ushort> BitwiseClearXor(Vector<ushort> xor, Vector<ushort> value, Vector<ushort> mask) => BitwiseClearXor(xor, value, mask);

        /// <summary>
        /// svuint32_t svbcax[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3)
        ///   BCAX Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<uint> BitwiseClearXor(Vector<uint> xor, Vector<uint> value, Vector<uint> mask) => BitwiseClearXor(xor, value, mask);

        /// <summary>
        /// svuint64_t svbcax[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3)
        ///   BCAX Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<ulong> BitwiseClearXor(Vector<ulong> xor, Vector<ulong> value, Vector<ulong> mask) => BitwiseClearXor(xor, value, mask);


        // Bitwise select

        /// <summary>
        /// svuint8_t svbsl[_u8](svuint8_t op1, svuint8_t op2, svuint8_t op3)
        ///   BSL Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<byte> BitwiseSelect(Vector<byte> select, Vector<byte> left, Vector<byte> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// svint16_t svbsl[_s16](svint16_t op1, svint16_t op2, svint16_t op3)
        ///   BSL Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<short> BitwiseSelect(Vector<short> select, Vector<short> left, Vector<short> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// svint32_t svbsl[_s32](svint32_t op1, svint32_t op2, svint32_t op3)
        ///   BSL Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<int> BitwiseSelect(Vector<int> select, Vector<int> left, Vector<int> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// svint64_t svbsl[_s64](svint64_t op1, svint64_t op2, svint64_t op3)
        ///   BSL Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<long> BitwiseSelect(Vector<long> select, Vector<long> left, Vector<long> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// svint8_t svbsl[_s8](svint8_t op1, svint8_t op2, svint8_t op3)
        ///   BSL Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<sbyte> BitwiseSelect(Vector<sbyte> select, Vector<sbyte> left, Vector<sbyte> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// svuint16_t svbsl[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3)
        ///   BSL Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<ushort> BitwiseSelect(Vector<ushort> select, Vector<ushort> left, Vector<ushort> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// svuint32_t svbsl[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3)
        ///   BSL Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<uint> BitwiseSelect(Vector<uint> select, Vector<uint> left, Vector<uint> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// svuint64_t svbsl[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3)
        ///   BSL Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<ulong> BitwiseSelect(Vector<ulong> select, Vector<ulong> left, Vector<ulong> right) => BitwiseSelect(select, left, right);


        // Bitwise select with first input inverted

        /// <summary>
        /// svuint8_t svbsl1n[_u8](svuint8_t op1, svuint8_t op2, svuint8_t op3)
        ///   BSL1N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<byte> BitwiseSelectLeftInverted(Vector<byte> select, Vector<byte> left, Vector<byte> right) => BitwiseSelectLeftInverted(select, left, right);

        /// <summary>
        /// svint16_t svbsl1n[_s16](svint16_t op1, svint16_t op2, svint16_t op3)
        ///   BSL1N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<short> BitwiseSelectLeftInverted(Vector<short> select, Vector<short> left, Vector<short> right) => BitwiseSelectLeftInverted(select, left, right);

        /// <summary>
        /// svint32_t svbsl1n[_s32](svint32_t op1, svint32_t op2, svint32_t op3)
        ///   BSL1N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<int> BitwiseSelectLeftInverted(Vector<int> select, Vector<int> left, Vector<int> right) => BitwiseSelectLeftInverted(select, left, right);

        /// <summary>
        /// svint64_t svbsl1n[_s64](svint64_t op1, svint64_t op2, svint64_t op3)
        ///   BSL1N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<long> BitwiseSelectLeftInverted(Vector<long> select, Vector<long> left, Vector<long> right) => BitwiseSelectLeftInverted(select, left, right);

        /// <summary>
        /// svint8_t svbsl1n[_s8](svint8_t op1, svint8_t op2, svint8_t op3)
        ///   BSL1N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<sbyte> BitwiseSelectLeftInverted(Vector<sbyte> select, Vector<sbyte> left, Vector<sbyte> right) => BitwiseSelectLeftInverted(select, left, right);

        /// <summary>
        /// svuint16_t svbsl1n[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3)
        ///   BSL1N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<ushort> BitwiseSelectLeftInverted(Vector<ushort> select, Vector<ushort> left, Vector<ushort> right) => BitwiseSelectLeftInverted(select, left, right);

        /// <summary>
        /// svuint32_t svbsl1n[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3)
        ///   BSL1N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<uint> BitwiseSelectLeftInverted(Vector<uint> select, Vector<uint> left, Vector<uint> right) => BitwiseSelectLeftInverted(select, left, right);

        /// <summary>
        /// svuint64_t svbsl1n[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3)
        ///   BSL1N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<ulong> BitwiseSelectLeftInverted(Vector<ulong> select, Vector<ulong> left, Vector<ulong> right) => BitwiseSelectLeftInverted(select, left, right);


        // Bitwise select with second input inverted

        /// <summary>
        /// svuint8_t svbsl2n[_u8](svuint8_t op1, svuint8_t op2, svuint8_t op3)
        ///   BSL2N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<byte> BitwiseSelectRightInverted(Vector<byte> select, Vector<byte> left, Vector<byte> right) => BitwiseSelectRightInverted(select, left, right);

        /// <summary>
        /// svint16_t svbsl2n[_s16](svint16_t op1, svint16_t op2, svint16_t op3)
        ///   BSL2N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<short> BitwiseSelectRightInverted(Vector<short> select, Vector<short> left, Vector<short> right) => BitwiseSelectRightInverted(select, left, right);

        /// <summary>
        /// svint32_t svbsl2n[_s32](svint32_t op1, svint32_t op2, svint32_t op3)
        ///   BSL2N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<int> BitwiseSelectRightInverted(Vector<int> select, Vector<int> left, Vector<int> right) => BitwiseSelectRightInverted(select, left, right);

        /// <summary>
        /// svint64_t svbsl2n[_s64](svint64_t op1, svint64_t op2, svint64_t op3)
        ///   BSL2N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<long> BitwiseSelectRightInverted(Vector<long> select, Vector<long> left, Vector<long> right) => BitwiseSelectRightInverted(select, left, right);

        /// <summary>
        /// svint8_t svbsl2n[_s8](svint8_t op1, svint8_t op2, svint8_t op3)
        ///   BSL2N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<sbyte> BitwiseSelectRightInverted(Vector<sbyte> select, Vector<sbyte> left, Vector<sbyte> right) => BitwiseSelectRightInverted(select, left, right);

        /// <summary>
        /// svuint16_t svbsl2n[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3)
        ///   BSL2N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<ushort> BitwiseSelectRightInverted(Vector<ushort> select, Vector<ushort> left, Vector<ushort> right) => BitwiseSelectRightInverted(select, left, right);

        /// <summary>
        /// svuint32_t svbsl2n[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3)
        ///   BSL2N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<uint> BitwiseSelectRightInverted(Vector<uint> select, Vector<uint> left, Vector<uint> right) => BitwiseSelectRightInverted(select, left, right);

        /// <summary>
        /// svuint64_t svbsl2n[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3)
        ///   BSL2N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<ulong> BitwiseSelectRightInverted(Vector<ulong> select, Vector<ulong> left, Vector<ulong> right) => BitwiseSelectRightInverted(select, left, right);

        /// Interleaving Xor

        /// <summary>
        /// svint8_t sveorbt[_s8](svint8_t odd, svint8_t op1, svint8_t op2)
        ///   EORBT Zd.B, Zn.B, Zm.B
        /// </summary>
        public static Vector<byte> InterleavingXorEvenOdd(Vector<byte> odd, Vector<byte> left, Vector<byte> right) => InterleavingXorEvenOdd(odd, left, right);

        /// <summary>
        /// svint16_t sveorbt[_s16](svint16_t odd, svint16_t op1, svint16_t op2)
        ///   EORBT Zd.H, Zn.H, Zm.H
        /// </summary>
        public static Vector<short> InterleavingXorEvenOdd(Vector<short> odd, Vector<short> left, Vector<short> right) => InterleavingXorEvenOdd(odd, left, right);

        /// <summary>
        /// svint32_t sveorbt[_s32](svint32_t odd, svint32_t op1, svint32_t op2)
        ///   EORBT Zd.S, Zn.S, Zm.S
        /// </summary>
        public static Vector<int> InterleavingXorEvenOdd(Vector<int> odd, Vector<int> left, Vector<int> right) => InterleavingXorEvenOdd(odd, left, right);

        /// <summary>
        /// svint64_t sveorbt[_s64](svint64_t odd, svint64_t op1, svint64_t op2)
        ///   EORBT Zd.D, Zn.D, Zm.D
        /// </summary>
        public static Vector<long> InterleavingXorEvenOdd(Vector<long> odd, Vector<long> left, Vector<long> right) => InterleavingXorEvenOdd(odd, left, right);

        /// <summary>
        /// svint8_t sveorbt[_s8](svint8_t odd, svint8_t op1, svint8_t op2)
        ///   EORBT Zd.B, Zn.B, Zm.B
        /// </summary>
        public static Vector<sbyte> InterleavingXorEvenOdd(Vector<sbyte> odd, Vector<sbyte> left, Vector<sbyte> right) => InterleavingXorEvenOdd(odd, left, right);

        /// <summary>
        /// svint16_t sveorbt[_s16](svint16_t odd, svint16_t op1, svint16_t op2)
        ///   EORBT Zd.H, Zn.H, Zm.H
        /// </summary>
        public static Vector<ushort> InterleavingXorEvenOdd(Vector<ushort> odd, Vector<ushort> left, Vector<ushort> right) => InterleavingXorEvenOdd(odd, left, right);

        /// <summary>
        /// svint32_t sveorbt[_s32](svint32_t odd, svint32_t op1, svint32_t op2)
        ///   EORBT Zd.S, Zn.S, Zm.S
        /// </summary>
        public static Vector<uint> InterleavingXorEvenOdd(Vector<uint> odd, Vector<uint> left, Vector<uint> right) => InterleavingXorEvenOdd(odd, left, right);

        /// <summary>
        /// svint64_t sveorbt[_s64](svint64_t odd, svint64_t op1, svint64_t op2)
        ///   EORBT Zd.D, Zn.D, Zm.D
        /// </summary>
        public static Vector<ulong> InterleavingXorEvenOdd(Vector<ulong> odd, Vector<ulong> left, Vector<ulong> right) => InterleavingXorEvenOdd(odd, left, right);

        /// <summary>
        /// svint8_t sveortb[_s8](svint8_t even, svint8_t op1, svint8_t op2)
        ///   EORTB Zd.B, Zn.B, Zm.B
        /// </summary>
        public static Vector<byte> InterleavingXorOddEven(Vector<byte> even, Vector<byte> left, Vector<byte> right) => InterleavingXorOddEven(even, left, right);

        /// <summary>
        /// svint16_t sveortb[_s16](svint16_t even, svint16_t op1, svint16_t op2)
        ///   EORTB Zd.H, Zn.H, Zm.H
        /// </summary>
        public static Vector<short> InterleavingXorOddEven(Vector<short> even, Vector<short> left, Vector<short> right) => InterleavingXorOddEven(even, left, right);

        /// <summary>
        /// svint32_t sveortb[_s32](svint32_t even, svint32_t op1, svint32_t op2)
        ///   EORTB Zd.S, Zn.S, Zm.S
        /// </summary>
        public static Vector<int> InterleavingXorOddEven(Vector<int> even, Vector<int> left, Vector<int> right) => InterleavingXorOddEven(even, left, right);

        /// <summary>
        /// svint64_t sveortb[_s64](svint64_t even, svint64_t op1, svint64_t op2)
        ///   EORTB Zd.D, Zn.D, Zm.D
        /// </summary>
        public static Vector<long> InterleavingXorOddEven(Vector<long> even, Vector<long> left, Vector<long> right) => InterleavingXorOddEven(even, left, right);

        /// <summary>
        /// svint8_t sveortb[_s8](svint8_t even, svint8_t op1, svint8_t op2)
        ///   EORTB Zd.B, Zn.B, Zm.B
        /// </summary>
        public static Vector<sbyte> InterleavingXorOddEven(Vector<sbyte> even, Vector<sbyte> left, Vector<sbyte> right) => InterleavingXorOddEven(even, left, right);

        /// <summary>
        /// svint16_t sveortb[_s16](svint16_t even, svint16_t op1, svint16_t op2)
        ///   EORTB Zd.H, Zn.H, Zm.H
        /// </summary>
        public static Vector<ushort> InterleavingXorOddEven(Vector<ushort> even, Vector<ushort> left, Vector<ushort> right) => InterleavingXorOddEven(even, left, right);

        /// <summary>
        /// svint32_t sveortb[_s32](svint32_t even, svint32_t op1, svint32_t op2)
        ///   EORTB Zd.S, Zn.S, Zm.S
        /// </summary>
        public static Vector<uint> InterleavingXorOddEven(Vector<uint> even, Vector<uint> left, Vector<uint> right) => InterleavingXorOddEven(even, left, right);

        /// <summary>
        /// svint64_t sveortb[_s64](svint64_t even, svint64_t op1, svint64_t op2)
        ///   EORTB Zd.D, Zn.D, Zm.D
        /// </summary>
        public static Vector<ulong> InterleavingXorOddEven(Vector<ulong> even, Vector<ulong> left, Vector<ulong> right) => InterleavingXorOddEven(even, left, right);


        // Rounding shift left

        /// <summary>
        /// svint16_t svrshl[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SRSHL Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        /// </summary>
        public static Vector<short> ShiftArithmeticRounded(Vector<short> value, Vector<short> count) => ShiftArithmeticRounded(value, count);

        /// <summary>
        /// svint32_t svrshl[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SRSHL Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// </summary>
        public static Vector<int> ShiftArithmeticRounded(Vector<int> value, Vector<int> count) => ShiftArithmeticRounded(value, count);

        /// <summary>
        /// svint64_t svrshl[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SRSHL Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// </summary>
        public static Vector<long> ShiftArithmeticRounded(Vector<long> value, Vector<long> count) => ShiftArithmeticRounded(value, count);

        /// <summary>
        /// svint8_t svrshl[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SRSHL Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        /// </summary>
        public static Vector<sbyte> ShiftArithmeticRounded(Vector<sbyte> value, Vector<sbyte> count) => ShiftArithmeticRounded(value, count);


        // Saturating rounding shift left

        /// <summary>
        /// svint16_t svqrshl[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SQRSHL Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        /// </summary>
        public static Vector<short> ShiftArithmeticRoundedSaturate(Vector<short> value, Vector<short> count) => ShiftArithmeticRoundedSaturate(value, count);

        /// <summary>
        /// svint32_t svqrshl[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SQRSHL Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// </summary>
        public static Vector<int> ShiftArithmeticRoundedSaturate(Vector<int> value, Vector<int> count) => ShiftArithmeticRoundedSaturate(value, count);

        /// <summary>
        /// svint64_t svqrshl[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SQRSHL Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// </summary>
        public static Vector<long> ShiftArithmeticRoundedSaturate(Vector<long> value, Vector<long> count) => ShiftArithmeticRoundedSaturate(value, count);

        /// <summary>
        /// svint8_t svqrshl[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SQRSHL Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        /// </summary>
        public static Vector<sbyte> ShiftArithmeticRoundedSaturate(Vector<sbyte> value, Vector<sbyte> count) => ShiftArithmeticRoundedSaturate(value, count);


        // Saturating shift left

        /// <summary>
        /// svint16_t svqshl[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SQSHL Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        /// </summary>
        public static Vector<short> ShiftArithmeticSaturate(Vector<short> value, Vector<short> count) => ShiftArithmeticSaturate(value, count);

        /// <summary>
        /// svint32_t svqshl[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SQSHL Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// </summary>
        public static Vector<int> ShiftArithmeticSaturate(Vector<int> value, Vector<int> count) => ShiftArithmeticSaturate(value, count);

        /// <summary>
        /// svint64_t svqshl[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SQSHL Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// </summary>
        public static Vector<long> ShiftArithmeticSaturate(Vector<long> value, Vector<long> count) => ShiftArithmeticSaturate(value, count);

        /// <summary>
        /// svint8_t svqshl[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SQSHL Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        /// </summary>
        public static Vector<sbyte> ShiftArithmeticSaturate(Vector<sbyte> value, Vector<sbyte> count) => ShiftArithmeticSaturate(value, count);


        // Shift left and insert

        /// <summary>
        /// svuint8_t svsli[_n_u8](svuint8_t op1, svuint8_t op2, uint64_t imm3)
        ///   SLI Ztied1.B, Zop2.B, #imm3
        /// </summary>
        public static Vector<byte> ShiftLeftAndInsert(Vector<byte> left, Vector<byte> right, [ConstantExpected] byte shift) => ShiftLeftAndInsert(left, right, shift);

        /// <summary>
        /// svint16_t svsli[_n_s16](svint16_t op1, svint16_t op2, uint64_t imm3)
        ///   SLI Ztied1.H, Zop2.H, #imm3
        /// </summary>
        public static Vector<short> ShiftLeftAndInsert(Vector<short> left, Vector<short> right, [ConstantExpected] byte shift) => ShiftLeftAndInsert(left, right, shift);

        /// <summary>
        /// svint32_t svsli[_n_s32](svint32_t op1, svint32_t op2, uint64_t imm3)
        ///   SLI Ztied1.S, Zop2.S, #imm3
        /// </summary>
        public static Vector<int> ShiftLeftAndInsert(Vector<int> left, Vector<int> right, [ConstantExpected] byte shift) => ShiftLeftAndInsert(left, right, shift);

        /// <summary>
        /// svint64_t svsli[_n_s64](svint64_t op1, svint64_t op2, uint64_t imm3)
        ///   SLI Ztied1.D, Zop2.D, #imm3
        /// </summary>
        public static Vector<long> ShiftLeftAndInsert(Vector<long> left, Vector<long> right, [ConstantExpected] byte shift) => ShiftLeftAndInsert(left, right, shift);

        /// <summary>
        /// svint8_t svsli[_n_s8](svint8_t op1, svint8_t op2, uint64_t imm3)
        ///   SLI Ztied1.B, Zop2.B, #imm3
        /// </summary>
        public static Vector<sbyte> ShiftLeftAndInsert(Vector<sbyte> left, Vector<sbyte> right, [ConstantExpected] byte shift) => ShiftLeftAndInsert(left, right, shift);

        /// <summary>
        /// svuint16_t svsli[_n_u16](svuint16_t op1, svuint16_t op2, uint64_t imm3)
        ///   SLI Ztied1.H, Zop2.H, #imm3
        /// </summary>
        public static Vector<ushort> ShiftLeftAndInsert(Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte shift) => ShiftLeftAndInsert(left, right, shift);

        /// <summary>
        /// svuint32_t svsli[_n_u32](svuint32_t op1, svuint32_t op2, uint64_t imm3)
        ///   SLI Ztied1.S, Zop2.S, #imm3
        /// </summary>
        public static Vector<uint> ShiftLeftAndInsert(Vector<uint> left, Vector<uint> right, [ConstantExpected] byte shift) => ShiftLeftAndInsert(left, right, shift);

        /// <summary>
        /// svuint64_t svsli[_n_u64](svuint64_t op1, svuint64_t op2, uint64_t imm3)
        ///   SLI Ztied1.D, Zop2.D, #imm3
        /// </summary>
        public static Vector<ulong> ShiftLeftAndInsert(Vector<ulong> left, Vector<ulong> right, [ConstantExpected] byte shift) => ShiftLeftAndInsert(left, right, shift);


        // Saturating shift left

        /// <summary>
        /// svuint8_t svqshl[_u8]_m(svbool_t pg, svuint8_t op1, svint8_t op2)
        ///   UQSHL Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        /// </summary>
        public static Vector<byte> ShiftLeftLogicalSaturate(Vector<byte> value, Vector<sbyte> count) => ShiftLeftLogicalSaturate(value, count);

        /// <summary>
        /// svuint16_t svqshl[_u16]_m(svbool_t pg, svuint16_t op1, svint16_t op2)
        ///   UQSHL Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        /// </summary>
        public static Vector<ushort> ShiftLeftLogicalSaturate(Vector<ushort> value, Vector<short> count) => ShiftLeftLogicalSaturate(value, count);

        /// <summary>
        /// svuint32_t svqshl[_u32]_m(svbool_t pg, svuint32_t op1, svint32_t op2)
        ///   UQSHL Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// </summary>
        public static Vector<uint> ShiftLeftLogicalSaturate(Vector<uint> value, Vector<int> count) => ShiftLeftLogicalSaturate(value, count);

        /// <summary>
        /// svuint64_t svqshl[_u64]_m(svbool_t pg, svuint64_t op1, svint64_t op2)
        ///   UQSHL Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// </summary>
        public static Vector<ulong> ShiftLeftLogicalSaturate(Vector<ulong> value, Vector<long> count) => ShiftLeftLogicalSaturate(value, count);


        // Saturating shift left unsigned

        /// <summary>
        /// svuint8_t svqshlu[_n_s8]_m(svbool_t pg, svint8_t op1, uint64_t imm2)
        ///   SQSHLU Ztied1.B, Pg/M, Ztied1.B, #imm2
        /// </summary>
        public static Vector<byte> ShiftLeftLogicalSaturateUnsigned(Vector<sbyte> value, [ConstantExpected] byte count) => ShiftLeftLogicalSaturateUnsigned(value, count);

        /// <summary>
        /// svuint16_t svqshlu[_n_s16]_m(svbool_t pg, svint16_t op1, uint64_t imm2)
        ///   SQSHLU Ztied1.H, Pg/M, Ztied1.H, #imm2
        /// </summary>
        public static Vector<ushort> ShiftLeftLogicalSaturateUnsigned(Vector<short> value, [ConstantExpected] byte count) => ShiftLeftLogicalSaturateUnsigned(value, count);

        /// <summary>
        /// svuint32_t svqshlu[_n_s32]_m(svbool_t pg, svint32_t op1, uint64_t imm2)
        ///   SQSHLU Ztied1.S, Pg/M, Ztied1.S, #imm2
        /// </summary>
        public static Vector<uint> ShiftLeftLogicalSaturateUnsigned(Vector<int> value, [ConstantExpected] byte count) => ShiftLeftLogicalSaturateUnsigned(value, count);

        /// <summary>
        /// svuint64_t svqshlu[_n_s64]_m(svbool_t pg, svint64_t op1, uint64_t imm2)
        ///   SQSHLU Ztied1.D, Pg/M, Ztied1.D, #imm2
        /// </summary>
        public static Vector<ulong> ShiftLeftLogicalSaturateUnsigned(Vector<long> value, [ConstantExpected] byte count) => ShiftLeftLogicalSaturateUnsigned(value, count);


        // Shift left long (bottom)

        /// <summary>
        /// svint16_t svshllb[_n_s16](svint8_t op1, uint64_t imm2)
        ///   SSHLLB Zresult.H, Zop1.B, #imm2
        /// </summary>
        public static Vector<short> ShiftLeftLogicalWideningEven(Vector<sbyte> value, [ConstantExpected] byte count) => ShiftLeftLogicalWideningEven(value, count);

        /// <summary>
        /// svint32_t svshllb[_n_s32](svint16_t op1, uint64_t imm2)
        ///   SSHLLB Zresult.S, Zop1.H, #imm2
        /// </summary>
        public static Vector<int> ShiftLeftLogicalWideningEven(Vector<short> value, [ConstantExpected] byte count) => ShiftLeftLogicalWideningEven(value, count);

        /// <summary>
        /// svint64_t svshllb[_n_s64](svint32_t op1, uint64_t imm2)
        ///   SSHLLB Zresult.D, Zop1.S, #imm2
        /// </summary>
        public static Vector<long> ShiftLeftLogicalWideningEven(Vector<int> value, [ConstantExpected] byte count) => ShiftLeftLogicalWideningEven(value, count);

        /// <summary>
        /// svuint16_t svshllb[_n_u16](svuint8_t op1, uint64_t imm2)
        ///   USHLLB Zresult.H, Zop1.B, #imm2
        /// </summary>
        public static Vector<ushort> ShiftLeftLogicalWideningEven(Vector<byte> value, [ConstantExpected] byte count) => ShiftLeftLogicalWideningEven(value, count);

        /// <summary>
        /// svuint32_t svshllb[_n_u32](svuint16_t op1, uint64_t imm2)
        ///   USHLLB Zresult.S, Zop1.H, #imm2
        /// </summary>
        public static Vector<uint> ShiftLeftLogicalWideningEven(Vector<ushort> value, [ConstantExpected] byte count) => ShiftLeftLogicalWideningEven(value, count);

        /// <summary>
        /// svuint64_t svshllb[_n_u64](svuint32_t op1, uint64_t imm2)
        ///   USHLLB Zresult.D, Zop1.S, #imm2
        /// </summary>
        public static Vector<ulong> ShiftLeftLogicalWideningEven(Vector<uint> value, [ConstantExpected] byte count) => ShiftLeftLogicalWideningEven(value, count);


        // Shift left long (top)

        /// <summary>
        /// svint16_t svshllt[_n_s16](svint8_t op1, uint64_t imm2)
        ///   SSHLLT Zresult.H, Zop1.B, #imm2
        /// </summary>
        public static Vector<short> ShiftLeftLogicalWideningOdd(Vector<sbyte> value, [ConstantExpected] byte count) => ShiftLeftLogicalWideningOdd(value, count);

        /// <summary>
        /// svint32_t svshllt[_n_s32](svint16_t op1, uint64_t imm2)
        ///   SSHLLT Zresult.S, Zop1.H, #imm2
        /// </summary>
        public static Vector<int> ShiftLeftLogicalWideningOdd(Vector<short> value, [ConstantExpected] byte count) => ShiftLeftLogicalWideningOdd(value, count);

        /// <summary>
        /// svint64_t svshllt[_n_s64](svint32_t op1, uint64_t imm2)
        ///   SSHLLT Zresult.D, Zop1.S, #imm2
        /// </summary>
        public static Vector<long> ShiftLeftLogicalWideningOdd(Vector<int> value, [ConstantExpected] byte count) => ShiftLeftLogicalWideningOdd(value, count);

        /// <summary>
        /// svuint16_t svshllt[_n_u16](svuint8_t op1, uint64_t imm2)
        ///   USHLLT Zresult.H, Zop1.B, #imm2
        /// </summary>
        public static Vector<ushort> ShiftLeftLogicalWideningOdd(Vector<byte> value, [ConstantExpected] byte count) => ShiftLeftLogicalWideningOdd(value, count);

        /// <summary>
        /// svuint32_t svshllt[_n_u32](svuint16_t op1, uint64_t imm2)
        ///   USHLLT Zresult.S, Zop1.H, #imm2
        /// </summary>
        public static Vector<uint> ShiftLeftLogicalWideningOdd(Vector<ushort> value, [ConstantExpected] byte count) => ShiftLeftLogicalWideningOdd(value, count);

        /// <summary>
        /// svuint64_t svshllt[_n_u64](svuint32_t op1, uint64_t imm2)
        ///   USHLLT Zresult.D, Zop1.S, #imm2
        /// </summary>
        public static Vector<ulong> ShiftLeftLogicalWideningOdd(Vector<uint> value, [ConstantExpected] byte count) => ShiftLeftLogicalWideningOdd(value, count);


        // Rounding shift left

        /// <summary>
        /// svuint8_t svrshl[_u8]_m(svbool_t pg, svuint8_t op1, svint8_t op2)
        ///   URSHL Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        /// </summary>
        public static Vector<byte> ShiftLogicalRounded(Vector<byte> value, Vector<sbyte> count) => ShiftLogicalRounded(value, count);

        /// <summary>
        /// svuint16_t svrshl[_u16]_m(svbool_t pg, svuint16_t op1, svint16_t op2)
        ///   URSHL Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        /// </summary>
        public static Vector<ushort> ShiftLogicalRounded(Vector<ushort> value, Vector<short> count) => ShiftLogicalRounded(value, count);

        /// <summary>
        /// svuint32_t svrshl[_u32]_m(svbool_t pg, svuint32_t op1, svint32_t op2)
        ///   URSHL Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// </summary>
        public static Vector<uint> ShiftLogicalRounded(Vector<uint> value, Vector<int> count) => ShiftLogicalRounded(value, count);

        /// <summary>
        /// svuint64_t svrshl[_u64]_m(svbool_t pg, svuint64_t op1, svint64_t op2)
        ///   URSHL Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// </summary>
        public static Vector<ulong> ShiftLogicalRounded(Vector<ulong> value, Vector<long> count) => ShiftLogicalRounded(value, count);


        // Saturating rounding shift left

        /// <summary>
        /// svuint8_t svqrshl[_u8]_m(svbool_t pg, svuint8_t op1, svint8_t op2)
        ///   UQRSHL Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        /// </summary>
        public static Vector<byte> ShiftLogicalRoundedSaturate(Vector<byte> value, Vector<sbyte> count) => ShiftLogicalRoundedSaturate(value, count);

        /// <summary>
        /// svuint16_t svqrshl[_u16]_m(svbool_t pg, svuint16_t op1, svint16_t op2)
        ///   UQRSHL Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        /// </summary>
        public static Vector<ushort> ShiftLogicalRoundedSaturate(Vector<ushort> value, Vector<short> count) => ShiftLogicalRoundedSaturate(value, count);

        /// <summary>
        /// svuint32_t svqrshl[_u32]_m(svbool_t pg, svuint32_t op1, svint32_t op2)
        ///   UQRSHL Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// </summary>
        public static Vector<uint> ShiftLogicalRoundedSaturate(Vector<uint> value, Vector<int> count) => ShiftLogicalRoundedSaturate(value, count);

        /// <summary>
        /// svuint64_t svqrshl[_u64]_m(svbool_t pg, svuint64_t op1, svint64_t op2)
        ///   UQRSHL Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// </summary>
        public static Vector<ulong> ShiftLogicalRoundedSaturate(Vector<ulong> value, Vector<long> count) => ShiftLogicalRoundedSaturate(value, count);


        // Shift right and insert

        /// <summary>
        /// svuint8_t svsri[_n_u8](svuint8_t op1, svuint8_t op2, uint64_t imm3)
        ///   SRI Ztied1.B, Zop2.B, #imm3
        /// </summary>
        public static Vector<byte> ShiftRightAndInsert(Vector<byte> left, Vector<byte> right, [ConstantExpected] byte shift) => ShiftRightAndInsert(left, right, shift);

        /// <summary>
        /// svint16_t svsri[_n_s16](svint16_t op1, svint16_t op2, uint64_t imm3)
        ///   SRI Ztied1.H, Zop2.H, #imm3
        /// </summary>
        public static Vector<short> ShiftRightAndInsert(Vector<short> left, Vector<short> right, [ConstantExpected] byte shift) => ShiftRightAndInsert(left, right, shift);

        /// <summary>
        /// svint32_t svsri[_n_s32](svint32_t op1, svint32_t op2, uint64_t imm3)
        ///   SRI Ztied1.S, Zop2.S, #imm3
        /// </summary>
        public static Vector<int> ShiftRightAndInsert(Vector<int> left, Vector<int> right, [ConstantExpected] byte shift) => ShiftRightAndInsert(left, right, shift);

        /// <summary>
        /// svint64_t svsri[_n_s64](svint64_t op1, svint64_t op2, uint64_t imm3)
        ///   SRI Ztied1.D, Zop2.D, #imm3
        /// </summary>
        public static Vector<long> ShiftRightAndInsert(Vector<long> left, Vector<long> right, [ConstantExpected] byte shift) => ShiftRightAndInsert(left, right, shift);

        /// <summary>
        /// svint8_t svsri[_n_s8](svint8_t op1, svint8_t op2, uint64_t imm3)
        ///   SRI Ztied1.B, Zop2.B, #imm3
        /// </summary>
        public static Vector<sbyte> ShiftRightAndInsert(Vector<sbyte> left, Vector<sbyte> right, [ConstantExpected] byte shift) => ShiftRightAndInsert(left, right, shift);

        /// <summary>
        /// svuint16_t svsri[_n_u16](svuint16_t op1, svuint16_t op2, uint64_t imm3)
        ///   SRI Ztied1.H, Zop2.H, #imm3
        /// </summary>
        public static Vector<ushort> ShiftRightAndInsert(Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte shift) => ShiftRightAndInsert(left, right, shift);

        /// <summary>
        /// svuint32_t svsri[_n_u32](svuint32_t op1, svuint32_t op2, uint64_t imm3)
        ///   SRI Ztied1.S, Zop2.S, #imm3
        /// </summary>
        public static Vector<uint> ShiftRightAndInsert(Vector<uint> left, Vector<uint> right, [ConstantExpected] byte shift) => ShiftRightAndInsert(left, right, shift);

        /// <summary>
        /// svuint64_t svsri[_n_u64](svuint64_t op1, svuint64_t op2, uint64_t imm3)
        ///   SRI Ztied1.D, Zop2.D, #imm3
        /// </summary>
        public static Vector<ulong> ShiftRightAndInsert(Vector<ulong> left, Vector<ulong> right, [ConstantExpected] byte shift) => ShiftRightAndInsert(left, right, shift);


        // Shift right and accumulate

        /// <summary>
        /// svint16_t svsra[_n_s16](svint16_t op1, svint16_t op2, uint64_t imm3)
        ///   SSRA Ztied1.H, Zop2.H, #imm3
        /// </summary>
        public static Vector<short> ShiftRightArithmeticAdd(Vector<short> addend, Vector<short> value, [ConstantExpected] byte count) => ShiftRightArithmeticAdd(addend, value, count);

        /// <summary>
        /// svint32_t svsra[_n_s32](svint32_t op1, svint32_t op2, uint64_t imm3)
        ///   SSRA Ztied1.S, Zop2.S, #imm3
        /// </summary>
        public static Vector<int> ShiftRightArithmeticAdd(Vector<int> addend, Vector<int> value, [ConstantExpected] byte count) => ShiftRightArithmeticAdd(addend, value, count);

        /// <summary>
        /// svint64_t svsra[_n_s64](svint64_t op1, svint64_t op2, uint64_t imm3)
        ///   SSRA Ztied1.D, Zop2.D, #imm3
        /// </summary>
        public static Vector<long> ShiftRightArithmeticAdd(Vector<long> addend, Vector<long> value, [ConstantExpected] byte count) => ShiftRightArithmeticAdd(addend, value, count);

        /// <summary>
        /// svint8_t svsra[_n_s8](svint8_t op1, svint8_t op2, uint64_t imm3)
        ///   SSRA Ztied1.B, Zop2.B, #imm3
        /// </summary>
        public static Vector<sbyte> ShiftRightArithmeticAdd(Vector<sbyte> addend, Vector<sbyte> value, [ConstantExpected] byte count) => ShiftRightArithmeticAdd(addend, value, count);


        // Saturating shift right narrow (bottom)

        /// <summary>
        /// svuint8_t svqshrnb[_n_u16](svuint16_t op1, uint64_t imm2)
        ///   UQSHRNB Zresult.B, Zop1.H, #imm2
        /// </summary>
        public static Vector<byte> ShiftRightArithmeticNarrowingSaturateEven(Vector<ushort> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateEven(value, count);

        /// <summary>
        /// svint16_t svqshrnb[_n_s32](svint32_t op1, uint64_t imm2)
        ///   SQSHRNB Zresult.H, Zop1.S, #imm2
        /// </summary>
        public static Vector<short> ShiftRightArithmeticNarrowingSaturateEven(Vector<int> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateEven(value, count);

        /// <summary>
        /// svint32_t svqshrnb[_n_s64](svint64_t op1, uint64_t imm2)
        ///   SQSHRNB Zresult.S, Zop1.D, #imm2
        /// </summary>
        public static Vector<int> ShiftRightArithmeticNarrowingSaturateEven(Vector<long> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateEven(value, count);

        /// <summary>
        /// svint8_t svqshrnb[_n_s16](svint16_t op1, uint64_t imm2)
        ///   SQSHRNB Zresult.B, Zop1.H, #imm2
        /// </summary>
        public static Vector<sbyte> ShiftRightArithmeticNarrowingSaturateEven(Vector<short> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateEven(value, count);

        /// <summary>
        /// svuint16_t svqshrnb[_n_u32](svuint32_t op1, uint64_t imm2)
        ///   UQSHRNB Zresult.H, Zop1.S, #imm2
        /// </summary>
        public static Vector<ushort> ShiftRightArithmeticNarrowingSaturateEven(Vector<uint> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateEven(value, count);

        /// <summary>
        /// svuint32_t svqshrnb[_n_u64](svuint64_t op1, uint64_t imm2)
        ///   UQSHRNB Zresult.S, Zop1.D, #imm2
        /// </summary>
        public static Vector<uint> ShiftRightArithmeticNarrowingSaturateEven(Vector<ulong> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateEven(value, count);


        // Saturating shift right narrow (top)

        /// <summary>
        /// svuint8_t svqshrnt[_n_u16](svuint8_t even, svuint16_t op1, uint64_t imm2)
        ///   UQSHRNT Ztied.B, Zop1.H, #imm2
        /// </summary>
        public static Vector<byte> ShiftRightArithmeticNarrowingSaturateOdd(Vector<byte> even, Vector<ushort> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateOdd(even, value, count);

        /// <summary>
        /// svint16_t svqshrnt[_n_s32](svint16_t even, svint32_t op1, uint64_t imm2)
        ///   SQSHRNT Ztied.H, Zop1.S, #imm2
        /// </summary>
        public static Vector<short> ShiftRightArithmeticNarrowingSaturateOdd(Vector<short> even, Vector<int> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateOdd(even, value, count);

        /// <summary>
        /// svint32_t svqshrnt[_n_s64](svint32_t even, svint64_t op1, uint64_t imm2)
        ///   SQSHRNT Ztied.S, Zop1.D, #imm2
        /// </summary>
        public static Vector<int> ShiftRightArithmeticNarrowingSaturateOdd(Vector<int> even, Vector<long> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateOdd(even, value, count);

        /// <summary>
        /// svint8_t svqshrnt[_n_s16](svint8_t even, svint16_t op1, uint64_t imm2)
        ///   SQSHRNT Ztied.B, Zop1.H, #imm2
        /// </summary>
        public static Vector<sbyte> ShiftRightArithmeticNarrowingSaturateOdd(Vector<sbyte> even, Vector<short> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateOdd(even, value, count);

        /// <summary>
        /// svuint16_t svqshrnt[_n_u32](svuint16_t even, svuint32_t op1, uint64_t imm2)
        ///   UQSHRNT Ztied.H, Zop1.S, #imm2
        /// </summary>
        public static Vector<ushort> ShiftRightArithmeticNarrowingSaturateOdd(Vector<ushort> even, Vector<uint> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateOdd(even, value, count);

        /// <summary>
        /// svuint32_t svqshrnt[_n_u64](svuint32_t even, svuint64_t op1, uint64_t imm2)
        ///   UQSHRNT Ztied.S, Zop1.D, #imm2
        /// </summary>
        public static Vector<uint> ShiftRightArithmeticNarrowingSaturateOdd(Vector<uint> even, Vector<ulong> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateOdd(even, value, count);


        // Saturating shift right unsigned narrow (bottom)

        /// <summary>
        /// svuint8_t svqshrunb[_n_s16](svint16_t op1, uint64_t imm2)
        ///   SQSHRUNB Zresult.B, Zop1.H, #imm2
        /// </summary>
        public static Vector<byte> ShiftRightArithmeticNarrowingSaturateUnsignedEven(Vector<short> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateUnsignedEven(value, count);

        /// <summary>
        /// svuint16_t svqshrunb[_n_s32](svint32_t op1, uint64_t imm2)
        ///   SQSHRUNB Zresult.H, Zop1.S, #imm2
        /// </summary>
        public static Vector<ushort> ShiftRightArithmeticNarrowingSaturateUnsignedEven(Vector<int> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateUnsignedEven(value, count);

        /// <summary>
        /// svuint32_t svqshrunb[_n_s64](svint64_t op1, uint64_t imm2)
        ///   SQSHRUNB Zresult.S, Zop1.D, #imm2
        /// </summary>
        public static Vector<uint> ShiftRightArithmeticNarrowingSaturateUnsignedEven(Vector<long> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateUnsignedEven(value, count);


        // Saturating shift right unsigned narrow (top)

        /// <summary>
        /// svuint8_t svqshrunt[_n_s16](svuint8_t even, svint16_t op1, uint64_t imm2)
        ///   SQSHRUNT Ztied.B, Zop1.H, #imm2
        /// </summary>
        public static Vector<byte> ShiftRightArithmeticNarrowingSaturateUnsignedOdd(Vector<byte> even, Vector<short> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateUnsignedOdd(even, value, count);

        /// <summary>
        /// svuint16_t svqshrunt[_n_s32](svuint16_t even, svint32_t op1, uint64_t imm2)
        ///   SQSHRUNT Ztied.H, Zop1.S, #imm2
        /// </summary>
        public static Vector<ushort> ShiftRightArithmeticNarrowingSaturateUnsignedOdd(Vector<ushort> even, Vector<int> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateUnsignedOdd(even, value, count);

        /// <summary>
        /// svuint32_t svqshrunt[_n_s64](svuint32_t even, svint64_t op1, uint64_t imm2)
        ///   SQSHRUNT Ztied.S, Zop1.D, #imm2
        /// </summary>
        public static Vector<uint> ShiftRightArithmeticNarrowingSaturateUnsignedOdd(Vector<uint> even, Vector<long> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateUnsignedOdd(even, value, count);


        // Rounding shift right

        /// <summary>
        /// svint16_t svrshr[_n_s16]_m(svbool_t pg, svint16_t op1, uint64_t imm2)
        ///   SRSHR Ztied1.H, Pg/M, Ztied1.H, #imm2
        /// </summary>
        public static Vector<short> ShiftRightArithmeticRounded(Vector<short> value, [ConstantExpected] byte count) => ShiftRightArithmeticRounded(value, count);

        /// <summary>
        /// svint32_t svrshr[_n_s32]_m(svbool_t pg, svint32_t op1, uint64_t imm2)
        ///   SRSHR Ztied1.S, Pg/M, Ztied1.S, #imm2
        /// </summary>
        public static Vector<int> ShiftRightArithmeticRounded(Vector<int> value, [ConstantExpected] byte count) => ShiftRightArithmeticRounded(value, count);

        /// <summary>
        /// svint64_t svrshr[_n_s64]_m(svbool_t pg, svint64_t op1, uint64_t imm2)
        ///   SRSHR Ztied1.D, Pg/M, Ztied1.D, #imm2
        /// </summary>
        public static Vector<long> ShiftRightArithmeticRounded(Vector<long> value, [ConstantExpected] byte count) => ShiftRightArithmeticRounded(value, count);

        /// <summary>
        /// svint8_t svrshr[_n_s8]_m(svbool_t pg, svint8_t op1, uint64_t imm2)
        ///   SRSHR Ztied1.B, Pg/M, Ztied1.B, #imm2
        /// </summary>
        public static Vector<sbyte> ShiftRightArithmeticRounded(Vector<sbyte> value, [ConstantExpected] byte count) => ShiftRightArithmeticRounded(value, count);


        // Rounding shift right and accumulate

        /// <summary>
        /// svint16_t svrsra[_n_s16](svint16_t op1, svint16_t op2, uint64_t imm3)
        ///   SRSRA Ztied1.H, Zop2.H, #imm3
        /// </summary>
        public static Vector<short> ShiftRightArithmeticRoundedAdd(Vector<short> addend, Vector<short> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedAdd(addend, value, count);

        /// <summary>
        /// svint32_t svrsra[_n_s32](svint32_t op1, svint32_t op2, uint64_t imm3)
        ///   SRSRA Ztied1.S, Zop2.S, #imm3
        /// </summary>
        public static Vector<int> ShiftRightArithmeticRoundedAdd(Vector<int> addend, Vector<int> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedAdd(addend, value, count);

        /// <summary>
        /// svint64_t svrsra[_n_s64](svint64_t op1, svint64_t op2, uint64_t imm3)
        ///   SRSRA Ztied1.D, Zop2.D, #imm3
        /// </summary>
        public static Vector<long> ShiftRightArithmeticRoundedAdd(Vector<long> addend, Vector<long> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedAdd(addend, value, count);

        /// <summary>
        /// svint8_t svrsra[_n_s8](svint8_t op1, svint8_t op2, uint64_t imm3)
        ///   SRSRA Ztied1.B, Zop2.B, #imm3
        /// </summary>
        public static Vector<sbyte> ShiftRightArithmeticRoundedAdd(Vector<sbyte> addend, Vector<sbyte> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedAdd(addend, value, count);


        // Saturating rounding shift right narrow (bottom)

        /// <summary>
        /// svint16_t svqrshrnb[_n_s32](svint32_t op1, uint64_t imm2)
        ///   SQRSHRNB Zresult.H, Zop1.S, #imm2
        /// </summary>
        public static Vector<short> ShiftRightArithmeticRoundedNarrowingSaturateEven(Vector<int> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedNarrowingSaturateEven(value, count);

        /// <summary>
        /// svint32_t svqrshrnb[_n_s64](svint64_t op1, uint64_t imm2)
        ///   SQRSHRNB Zresult.S, Zop1.D, #imm2
        /// </summary>
        public static Vector<int> ShiftRightArithmeticRoundedNarrowingSaturateEven(Vector<long> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedNarrowingSaturateEven(value, count);

        /// <summary>
        /// svint8_t svqrshrnb[_n_s16](svint16_t op1, uint64_t imm2)
        ///   SQRSHRNB Zresult.B, Zop1.H, #imm2
        /// </summary>
        public static Vector<sbyte> ShiftRightArithmeticRoundedNarrowingSaturateEven(Vector<short> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedNarrowingSaturateEven(value, count);


        // Saturating rounding shift right narrow (top)

        /// <summary>
        /// svint16_t svqrshrnt[_n_s32](svint16_t even, svint32_t op1, uint64_t imm2)
        ///   SQRSHRNT Ztied.H, Zop1.S, #imm2
        /// </summary>
        public static Vector<short> ShiftRightArithmeticRoundedNarrowingSaturateOdd(Vector<short> even, Vector<int> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedNarrowingSaturateOdd(even, value, count);

        /// <summary>
        /// svint32_t svqrshrnt[_n_s64](svint32_t even, svint64_t op1, uint64_t imm2)
        ///   SQRSHRNT Ztied.S, Zop1.D, #imm2
        /// </summary>
        public static Vector<int> ShiftRightArithmeticRoundedNarrowingSaturateOdd(Vector<int> even, Vector<long> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedNarrowingSaturateOdd(even, value, count);

        /// <summary>
        /// svint8_t svqrshrnt[_n_s16](svint8_t even, svint16_t op1, uint64_t imm2)
        ///   SQRSHRNT Ztied.B, Zop1.H, #imm2
        /// </summary>
        public static Vector<sbyte> ShiftRightArithmeticRoundedNarrowingSaturateOdd(Vector<sbyte> even, Vector<short> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedNarrowingSaturateOdd(even, value, count);


        // Saturating rounding shift right unsigned narrow (bottom)

        /// <summary>
        /// svuint8_t svqrshrunb[_n_s16](svint16_t op1, uint64_t imm2)
        ///   SQRSHRUNB Zresult.B, Zop1.H, #imm2
        /// </summary>
        public static Vector<byte> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedEven(Vector<short> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedEven(value, count);

        /// <summary>
        /// svuint16_t svqrshrunb[_n_s32](svint32_t op1, uint64_t imm2)
        ///   SQRSHRUNB Zresult.H, Zop1.S, #imm2
        /// </summary>
        public static Vector<ushort> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedEven(Vector<int> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedEven(value, count);

        /// <summary>
        /// svuint32_t svqrshrunb[_n_s64](svint64_t op1, uint64_t imm2)
        ///   SQRSHRUNB Zresult.S, Zop1.D, #imm2
        /// </summary>
        public static Vector<uint> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedEven(Vector<long> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedEven(value, count);


        // Saturating rounding shift right unsigned narrow (top)

        /// <summary>
        /// svuint8_t svqrshrunt[_n_s16](svuint8_t even, svint16_t op1, uint64_t imm2)
        ///   SQRSHRUNT Ztied.B, Zop1.H, #imm2
        /// </summary>
        public static Vector<byte> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedOdd(Vector<byte> even, Vector<short> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedOdd(even, value, count);

        /// <summary>
        /// svuint16_t svqrshrunt[_n_s32](svuint16_t even, svint32_t op1, uint64_t imm2)
        ///   SQRSHRUNT Ztied.H, Zop1.S, #imm2
        /// </summary>
        public static Vector<ushort> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedOdd(Vector<ushort> even, Vector<int> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedOdd(even, value, count);

        /// <summary>
        /// svuint32_t svqrshrunt[_n_s64](svuint32_t even, svint64_t op1, uint64_t imm2)
        ///   SQRSHRUNT Ztied.S, Zop1.D, #imm2
        /// </summary>
        public static Vector<uint> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedOdd(Vector<uint> even, Vector<long> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedOdd(even, value, count);


        // Shift right and accumulate

        /// <summary>
        /// svuint8_t svsra[_n_u8](svuint8_t op1, svuint8_t op2, uint64_t imm3)
        ///   USRA Ztied1.B, Zop2.B, #imm3
        /// </summary>
        public static Vector<byte> ShiftRightLogicalAdd(Vector<byte> addend, Vector<byte> value, [ConstantExpected] byte count) => ShiftRightLogicalAdd(addend, value, count);

        /// <summary>
        /// svuint16_t svsra[_n_u16](svuint16_t op1, svuint16_t op2, uint64_t imm3)
        ///   USRA Ztied1.H, Zop2.H, #imm3
        /// </summary>
        public static Vector<ushort> ShiftRightLogicalAdd(Vector<ushort> addend, Vector<ushort> value, [ConstantExpected] byte count) => ShiftRightLogicalAdd(addend, value, count);

        /// <summary>
        /// svuint32_t svsra[_n_u32](svuint32_t op1, svuint32_t op2, uint64_t imm3)
        ///   USRA Ztied1.S, Zop2.S, #imm3
        /// </summary>
        public static Vector<uint> ShiftRightLogicalAdd(Vector<uint> addend, Vector<uint> value, [ConstantExpected] byte count) => ShiftRightLogicalAdd(addend, value, count);

        /// <summary>
        /// svuint64_t svsra[_n_u64](svuint64_t op1, svuint64_t op2, uint64_t imm3)
        ///   USRA Ztied1.D, Zop2.D, #imm3
        /// </summary>
        public static Vector<ulong> ShiftRightLogicalAdd(Vector<ulong> addend, Vector<ulong> value, [ConstantExpected] byte count) => ShiftRightLogicalAdd(addend, value, count);


        // Shift right narrow (bottom)

        /// <summary>
        /// svuint8_t svshrnb[_n_u16](svuint16_t op1, uint64_t imm2)
        ///   SHRNB Zresult.B, Zop1.H, #imm2
        /// </summary>
        public static Vector<byte> ShiftRightLogicalNarrowingEven(Vector<ushort> value, [ConstantExpected] byte count) => ShiftRightLogicalNarrowingEven(value, count);

        /// <summary>
        /// svint16_t svshrnb[_n_s32](svint32_t op1, uint64_t imm2)
        ///   SHRNB Zresult.H, Zop1.S, #imm2
        /// </summary>
        public static Vector<short> ShiftRightLogicalNarrowingEven(Vector<int> value, [ConstantExpected] byte count) => ShiftRightLogicalNarrowingEven(value, count);

        /// <summary>
        /// svint32_t svshrnb[_n_s64](svint64_t op1, uint64_t imm2)
        ///   SHRNB Zresult.S, Zop1.D, #imm2
        /// </summary>
        public static Vector<int> ShiftRightLogicalNarrowingEven(Vector<long> value, [ConstantExpected] byte count) => ShiftRightLogicalNarrowingEven(value, count);

        /// <summary>
        /// svint8_t svshrnb[_n_s16](svint16_t op1, uint64_t imm2)
        ///   SHRNB Zresult.B, Zop1.H, #imm2
        /// </summary>
        public static Vector<sbyte> ShiftRightLogicalNarrowingEven(Vector<short> value, [ConstantExpected] byte count) => ShiftRightLogicalNarrowingEven(value, count);

        /// <summary>
        /// svuint16_t svshrnb[_n_u32](svuint32_t op1, uint64_t imm2)
        ///   SHRNB Zresult.H, Zop1.S, #imm2
        /// </summary>
        public static Vector<ushort> ShiftRightLogicalNarrowingEven(Vector<uint> value, [ConstantExpected] byte count) => ShiftRightLogicalNarrowingEven(value, count);

        /// <summary>
        /// svuint32_t svshrnb[_n_u64](svuint64_t op1, uint64_t imm2)
        ///   SHRNB Zresult.S, Zop1.D, #imm2
        /// </summary>
        public static Vector<uint> ShiftRightLogicalNarrowingEven(Vector<ulong> value, [ConstantExpected] byte count) => ShiftRightLogicalNarrowingEven(value, count);


        // Shift right narrow (top)

        /// <summary>
        /// svuint8_t svshrnt[_n_u16](svuint8_t even, svuint16_t op1, uint64_t imm2)
        ///   SHRNT Ztied.B, Zop1.H, #imm2
        /// </summary>
        public static Vector<byte> ShiftRightLogicalNarrowingOdd(Vector<byte> even, Vector<ushort> value, [ConstantExpected] byte count) => ShiftRightLogicalNarrowingOdd(even, value, count);

        /// <summary>
        /// svint16_t svshrnt[_n_s32](svint16_t even, svint32_t op1, uint64_t imm2)
        ///   SHRNT Ztied.H, Zop1.S, #imm2
        /// </summary>
        public static Vector<short> ShiftRightLogicalNarrowingOdd(Vector<short> even, Vector<int> value, [ConstantExpected] byte count) => ShiftRightLogicalNarrowingOdd(even, value, count);

        /// <summary>
        /// svint32_t svshrnt[_n_s64](svint32_t even, svint64_t op1, uint64_t imm2)
        ///   SHRNT Ztied.S, Zop1.D, #imm2
        /// </summary>
        public static Vector<int> ShiftRightLogicalNarrowingOdd(Vector<int> even, Vector<long> value, [ConstantExpected] byte count) => ShiftRightLogicalNarrowingOdd(even, value, count);

        /// <summary>
        /// svint8_t svshrnt[_n_s16](svint8_t even, svint16_t op1, uint64_t imm2)
        ///   SHRNT Ztied.B, Zop1.H, #imm2
        /// </summary>
        public static Vector<sbyte> ShiftRightLogicalNarrowingOdd(Vector<sbyte> even, Vector<short> value, [ConstantExpected] byte count) => ShiftRightLogicalNarrowingOdd(even, value, count);

        /// <summary>
        /// svuint16_t svshrnt[_n_u32](svuint16_t even, svuint32_t op1, uint64_t imm2)
        ///   SHRNT Ztied.H, Zop1.S, #imm2
        /// </summary>
        public static Vector<ushort> ShiftRightLogicalNarrowingOdd(Vector<ushort> even, Vector<uint> value, [ConstantExpected] byte count) => ShiftRightLogicalNarrowingOdd(even, value, count);

        /// <summary>
        /// svuint32_t svshrnt[_n_u64](svuint32_t even, svuint64_t op1, uint64_t imm2)
        ///   SHRNT Ztied.S, Zop1.D, #imm2
        /// </summary>
        public static Vector<uint> ShiftRightLogicalNarrowingOdd(Vector<uint> even, Vector<ulong> value, [ConstantExpected] byte count) => ShiftRightLogicalNarrowingOdd(even, value, count);


        // Rounding shift right

        /// <summary>
        /// svuint8_t svrshr[_n_u8]_m(svbool_t pg, svuint8_t op1, uint64_t imm2)
        ///   URSHR Ztied1.B, Pg/M, Ztied1.B, #imm2
        /// </summary>
        public static Vector<byte> ShiftRightLogicalRounded(Vector<byte> value, [ConstantExpected] byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// svuint16_t svrshr[_n_u16]_m(svbool_t pg, svuint16_t op1, uint64_t imm2)
        ///   URSHR Ztied1.H, Pg/M, Ztied1.H, #imm2
        /// </summary>
        public static Vector<ushort> ShiftRightLogicalRounded(Vector<ushort> value, [ConstantExpected] byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// svuint32_t svrshr[_n_u32]_m(svbool_t pg, svuint32_t op1, uint64_t imm2)
        ///   URSHR Ztied1.S, Pg/M, Ztied1.S, #imm2
        /// </summary>
        public static Vector<uint> ShiftRightLogicalRounded(Vector<uint> value, [ConstantExpected] byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// svuint64_t svrshr[_n_u64]_m(svbool_t pg, svuint64_t op1, uint64_t imm2)
        ///   URSHR Ztied1.D, Pg/M, Ztied1.D, #imm2
        /// </summary>
        public static Vector<ulong> ShiftRightLogicalRounded(Vector<ulong> value, [ConstantExpected] byte count) => ShiftRightLogicalRounded(value, count);


        // Rounding shift right and accumulate

        /// <summary>
        /// svuint8_t svrsra[_n_u8](svuint8_t op1, svuint8_t op2, uint64_t imm3)
        ///   URSRA Ztied1.B, Zop2.B, #imm3
        /// </summary>
        public static Vector<byte> ShiftRightLogicalRoundedAdd(Vector<byte> addend, Vector<byte> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedAdd(addend, value, count);

        /// <summary>
        /// svuint16_t svrsra[_n_u16](svuint16_t op1, svuint16_t op2, uint64_t imm3)
        ///   URSRA Ztied1.H, Zop2.H, #imm3
        /// </summary>
        public static Vector<ushort> ShiftRightLogicalRoundedAdd(Vector<ushort> addend, Vector<ushort> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedAdd(addend, value, count);

        /// <summary>
        /// svuint32_t svrsra[_n_u32](svuint32_t op1, svuint32_t op2, uint64_t imm3)
        ///   URSRA Ztied1.S, Zop2.S, #imm3
        /// </summary>
        public static Vector<uint> ShiftRightLogicalRoundedAdd(Vector<uint> addend, Vector<uint> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedAdd(addend, value, count);

        /// <summary>
        /// svuint64_t svrsra[_n_u64](svuint64_t op1, svuint64_t op2, uint64_t imm3)
        ///   URSRA Ztied1.D, Zop2.D, #imm3
        /// </summary>
        public static Vector<ulong> ShiftRightLogicalRoundedAdd(Vector<ulong> addend, Vector<ulong> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedAdd(addend, value, count);


        // Rounding shift right narrow (bottom)

        /// <summary>
        /// svuint8_t svrshrnb[_n_u16](svuint16_t op1, uint64_t imm2)
        ///   RSHRNB Zresult.B, Zop1.H, #imm2
        /// </summary>
        public static Vector<byte> ShiftRightLogicalRoundedNarrowingEven(Vector<ushort> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingEven(value, count);

        /// <summary>
        /// svint16_t svrshrnb[_n_s32](svint32_t op1, uint64_t imm2)
        ///   RSHRNB Zresult.H, Zop1.S, #imm2
        /// </summary>
        public static Vector<short> ShiftRightLogicalRoundedNarrowingEven(Vector<int> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingEven(value, count);

        /// <summary>
        /// svint32_t svrshrnb[_n_s64](svint64_t op1, uint64_t imm2)
        ///   RSHRNB Zresult.S, Zop1.D, #imm2
        /// </summary>
        public static Vector<int> ShiftRightLogicalRoundedNarrowingEven(Vector<long> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingEven(value, count);

        /// <summary>
        /// svint8_t svrshrnb[_n_s16](svint16_t op1, uint64_t imm2)
        ///   RSHRNB Zresult.B, Zop1.H, #imm2
        /// </summary>
        public static Vector<sbyte> ShiftRightLogicalRoundedNarrowingEven(Vector<short> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingEven(value, count);

        /// <summary>
        /// svuint16_t svrshrnb[_n_u32](svuint32_t op1, uint64_t imm2)
        ///   RSHRNB Zresult.H, Zop1.S, #imm2
        /// </summary>
        public static Vector<ushort> ShiftRightLogicalRoundedNarrowingEven(Vector<uint> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingEven(value, count);

        /// <summary>
        /// svuint32_t svrshrnb[_n_u64](svuint64_t op1, uint64_t imm2)
        ///   RSHRNB Zresult.S, Zop1.D, #imm2
        /// </summary>
        public static Vector<uint> ShiftRightLogicalRoundedNarrowingEven(Vector<ulong> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingEven(value, count);


        // Rounding shift right narrow (top)

        /// <summary>
        /// svuint8_t svrshrnt[_n_u16](svuint8_t even, svuint16_t op1, uint64_t imm2)
        ///   RSHRNT Ztied.B, Zop1.H, #imm2
        /// </summary>
        public static Vector<byte> ShiftRightLogicalRoundedNarrowingOdd(Vector<byte> even, Vector<ushort> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingOdd(even, value, count);

        /// <summary>
        /// svint16_t svrshrnt[_n_s32](svint16_t even, svint32_t op1, uint64_t imm2)
        ///   RSHRNT Ztied.H, Zop1.S, #imm2
        /// </summary>
        public static Vector<short> ShiftRightLogicalRoundedNarrowingOdd(Vector<short> even, Vector<int> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingOdd(even, value, count);

        /// <summary>
        /// svint32_t svrshrnt[_n_s64](svint32_t even, svint64_t op1, uint64_t imm2)
        ///   RSHRNT Ztied.S, Zop1.D, #imm2
        /// </summary>
        public static Vector<int> ShiftRightLogicalRoundedNarrowingOdd(Vector<int> even, Vector<long> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingOdd(even, value, count);

        /// <summary>
        /// svint8_t svrshrnt[_n_s16](svint8_t even, svint16_t op1, uint64_t imm2)
        ///   RSHRNT Ztied.B, Zop1.H, #imm2
        /// </summary>
        public static Vector<sbyte> ShiftRightLogicalRoundedNarrowingOdd(Vector<sbyte> even, Vector<short> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingOdd(even, value, count);

        /// <summary>
        /// svuint16_t svrshrnt[_n_u32](svuint16_t even, svuint32_t op1, uint64_t imm2)
        ///   RSHRNT Ztied.H, Zop1.S, #imm2
        /// </summary>
        public static Vector<ushort> ShiftRightLogicalRoundedNarrowingOdd(Vector<ushort> even, Vector<uint> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingOdd(even, value, count);

        /// <summary>
        /// svuint32_t svrshrnt[_n_u64](svuint32_t even, svuint64_t op1, uint64_t imm2)
        ///   RSHRNT Ztied.S, Zop1.D, #imm2
        /// </summary>
        public static Vector<uint> ShiftRightLogicalRoundedNarrowingOdd(Vector<uint> even, Vector<ulong> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingOdd(even, value, count);


        // Saturating rounding shift right narrow (bottom)

        /// <summary>
        /// svuint8_t svqrshrnb[_n_u16](svuint16_t op1, uint64_t imm2)
        ///   UQRSHRNB Zresult.B, Zop1.H, #imm2
        /// </summary>
        public static Vector<byte> ShiftRightLogicalRoundedNarrowingSaturateEven(Vector<ushort> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingSaturateEven(value, count);

        /// <summary>
        /// svuint16_t svqrshrnb[_n_u32](svuint32_t op1, uint64_t imm2)
        ///   UQRSHRNB Zresult.H, Zop1.S, #imm2
        /// </summary>
        public static Vector<ushort> ShiftRightLogicalRoundedNarrowingSaturateEven(Vector<uint> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingSaturateEven(value, count);

        /// <summary>
        /// svuint32_t svqrshrnb[_n_u64](svuint64_t op1, uint64_t imm2)
        ///   UQRSHRNB Zresult.S, Zop1.D, #imm2
        /// </summary>
        public static Vector<uint> ShiftRightLogicalRoundedNarrowingSaturateEven(Vector<ulong> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingSaturateEven(value, count);


        // Saturating rounding shift right narrow (top)

        /// <summary>
        /// svuint8_t svqrshrnt[_n_u16](svuint8_t even, svuint16_t op1, uint64_t imm2)
        ///   UQRSHRNT Ztied.B, Zop1.H, #imm2
        /// </summary>
        public static Vector<byte> ShiftRightLogicalRoundedNarrowingSaturateOdd(Vector<byte> even, Vector<ushort> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingSaturateOdd(even, value, count);

        /// <summary>
        /// svuint16_t svqrshrnt[_n_u32](svuint16_t even, svuint32_t op1, uint64_t imm2)
        ///   UQRSHRNT Ztied.H, Zop1.S, #imm2
        /// </summary>
        public static Vector<ushort> ShiftRightLogicalRoundedNarrowingSaturateOdd(Vector<ushort> even, Vector<uint> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingSaturateOdd(even, value, count);

        /// <summary>
        /// svuint32_t svqrshrnt[_n_u64](svuint32_t even, svuint64_t op1, uint64_t imm2)
        ///   UQRSHRNT Ztied.S, Zop1.D, #imm2
        /// </summary>
        public static Vector<uint> ShiftRightLogicalRoundedNarrowingSaturateOdd(Vector<uint> even, Vector<ulong> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingSaturateOdd(even, value, count);


        // Bitwise exclusive OR of three vectors

        /// <summary>
        /// svuint8_t sveor3[_u8](svuint8_t op1, svuint8_t op2, svuint8_t op3)
        ///   EOR3 Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<byte> Xor(Vector<byte> value1, Vector<byte> value2, Vector<byte> value3) => Xor(value1, value2, value3);

        /// <summary>
        /// svint16_t sveor3[_s16](svint16_t op1, svint16_t op2, svint16_t op3)
        ///   EOR3 Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<short> Xor(Vector<short> value1, Vector<short> value2, Vector<short> value3) => Xor(value1, value2, value3);

        /// <summary>
        /// svint32_t sveor3[_s32](svint32_t op1, svint32_t op2, svint32_t op3)
        ///   EOR3 Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<int> Xor(Vector<int> value1, Vector<int> value2, Vector<int> value3) => Xor(value1, value2, value3);

        /// <summary>
        /// svint64_t sveor3[_s64](svint64_t op1, svint64_t op2, svint64_t op3)
        ///   EOR3 Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<long> Xor(Vector<long> value1, Vector<long> value2, Vector<long> value3) => Xor(value1, value2, value3);

        /// <summary>
        /// svint8_t sveor3[_s8](svint8_t op1, svint8_t op2, svint8_t op3)
        ///   EOR3 Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<sbyte> Xor(Vector<sbyte> value1, Vector<sbyte> value2, Vector<sbyte> value3) => Xor(value1, value2, value3);

        /// <summary>
        /// svuint16_t sveor3[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3)
        ///   EOR3 Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<ushort> Xor(Vector<ushort> value1, Vector<ushort> value2, Vector<ushort> value3) => Xor(value1, value2, value3);

        /// <summary>
        /// svuint32_t sveor3[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3)
        ///   EOR3 Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<uint> Xor(Vector<uint> value1, Vector<uint> value2, Vector<uint> value3) => Xor(value1, value2, value3);

        /// <summary>
        /// svuint64_t sveor3[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3)
        ///   EOR3 Ztied1.D, Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<ulong> Xor(Vector<ulong> value1, Vector<ulong> value2, Vector<ulong> value3) => Xor(value1, value2, value3);


        // Bitwise exclusive OR and rotate right

        /// <summary>
        /// svuint8_t svxar[_n_u8](svuint8_t op1, svuint8_t op2, uint64_t imm3)
        ///   XAR Ztied1.B, Ztied1.B, Zop2.B, #imm3
        /// </summary>
        public static Vector<byte> XorRotateRight(Vector<byte> left, Vector<byte> right, [ConstantExpected] byte count) => XorRotateRight(left, right, count);

        /// <summary>
        /// svint16_t svxar[_n_s16](svint16_t op1, svint16_t op2, uint64_t imm3)
        ///   XAR Ztied1.H, Ztied1.H, Zop2.H, #imm3
        /// </summary>
        public static Vector<short> XorRotateRight(Vector<short> left, Vector<short> right, [ConstantExpected] byte count) => XorRotateRight(left, right, count);

        /// <summary>
        /// svint32_t svxar[_n_s32](svint32_t op1, svint32_t op2, uint64_t imm3)
        ///   XAR Ztied1.S, Ztied1.S, Zop2.S, #imm3
        /// </summary>
        public static Vector<int> XorRotateRight(Vector<int> left, Vector<int> right, [ConstantExpected] byte count) => XorRotateRight(left, right, count);

        /// <summary>
        /// svint64_t svxar[_n_s64](svint64_t op1, svint64_t op2, uint64_t imm3)
        ///   XAR Ztied1.D, Ztied1.D, Zop2.D, #imm3
        /// </summary>
        public static Vector<long> XorRotateRight(Vector<long> left, Vector<long> right, [ConstantExpected] byte count) => XorRotateRight(left, right, count);

        /// <summary>
        /// svint8_t svxar[_n_s8](svint8_t op1, svint8_t op2, uint64_t imm3)
        ///   XAR Ztied1.B, Ztied1.B, Zop2.B, #imm3
        /// </summary>
        public static Vector<sbyte> XorRotateRight(Vector<sbyte> left, Vector<sbyte> right, [ConstantExpected] byte count) => XorRotateRight(left, right, count);

        /// <summary>
        /// svuint16_t svxar[_n_u16](svuint16_t op1, svuint16_t op2, uint64_t imm3)
        ///   XAR Ztied1.H, Ztied1.H, Zop2.H, #imm3
        /// </summary>
        public static Vector<ushort> XorRotateRight(Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte count) => XorRotateRight(left, right, count);

        /// <summary>
        /// svuint32_t svxar[_n_u32](svuint32_t op1, svuint32_t op2, uint64_t imm3)
        ///   XAR Ztied1.S, Ztied1.S, Zop2.S, #imm3
        /// </summary>
        public static Vector<uint> XorRotateRight(Vector<uint> left, Vector<uint> right, [ConstantExpected] byte count) => XorRotateRight(left, right, count);

        /// <summary>
        /// svuint64_t svxar[_n_u64](svuint64_t op1, svuint64_t op2, uint64_t imm3)
        ///   XAR Ztied1.D, Ztied1.D, Zop2.D, #imm3
        /// </summary>
        public static Vector<ulong> XorRotateRight(Vector<ulong> left, Vector<ulong> right, [ConstantExpected] byte count) => XorRotateRight(left, right, count);
    }
}
