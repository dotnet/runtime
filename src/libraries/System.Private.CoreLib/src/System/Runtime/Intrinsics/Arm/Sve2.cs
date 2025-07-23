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


        // Saturating absolute value

        /// <summary>
        /// svint8_t svqabs[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op)
        /// svint8_t svqabs[_s8]_x(svbool_t pg, svint8_t op)
        /// svint8_t svqabs[_s8]_z(svbool_t pg, svint8_t op)
        ///   SQABS Ztied.B, Pg/M, Zop.B
        ///   SQABS Ztied.B, Pg/M, Ztied.B
        /// </summary>
        public static Vector<sbyte> AbsSaturate(Vector<sbyte> value) => AbsSaturate(value);

        /// <summary>
        /// svint16_t svqabs[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        /// svint16_t svqabs[_s16]_x(svbool_t pg, svint16_t op)
        /// svint16_t svqabs[_s16]_z(svbool_t pg, svint16_t op)
        ///   SQABS Ztied.H, Pg/M, Zop.H
        ///   SQABS Ztied.H, Pg/M, Ztied.H
        /// </summary>
        public static Vector<short> AbsSaturate(Vector<short> value) => AbsSaturate(value);

        /// <summary>
        /// svint32_t svqabs[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        /// svint32_t svqabs[_s32]_x(svbool_t pg, svint32_t op)
        /// svint32_t svqabs[_s32]_z(svbool_t pg, svint32_t op)
        ///   SQABS Ztied.S, Pg/M, Zop.S
        ///   SQABS Ztied.S, Pg/M, Ztied.S
        /// </summary>
        public static Vector<int> AbsSaturate(Vector<int> value) => AbsSaturate(value);

        /// <summary>
        /// svint64_t svqabs[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        /// svint64_t svqabs[_s64]_x(svbool_t pg, svint64_t op)
        /// svint64_t svqabs[_s64]_z(svbool_t pg, svint64_t op)
        ///   SQABS Ztied.D, Pg/M, Zop.D
        ///   SQABS Ztied.D, Pg/M, Ztied.D
        /// </summary>
        public static Vector<long> AbsSaturate(Vector<long> value) => AbsSaturate(value);


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
        public static Vector<short> AbsoluteDifferenceWideningLowerAndAddEven(Vector<short> addend, Vector<sbyte> left, Vector<sbyte> right) => AbsoluteDifferenceWideningLowerAndAddEven(addend, left, right);

        /// <summary>
        /// svint32_t svabalb[_s32](svint32_t op1, svint16_t op2, svint16_t op3)
        ///   SABALB Ztied1.S, Zop2.H, Zop3.H
        /// </summary>
        public static Vector<int> AbsoluteDifferenceWideningLowerAndAddEven(Vector<int> addend, Vector<short> left, Vector<short> right) => AbsoluteDifferenceWideningLowerAndAddEven(addend, left, right);

        /// <summary>
        /// svint64_t svabalb[_s64](svint64_t op1, svint32_t op2, svint32_t op3)
        ///   SABALB Ztied1.D, Zop2.S, Zop3.S
        /// </summary>
        public static Vector<long> AbsoluteDifferenceWideningLowerAndAddEven(Vector<long> addend, Vector<int> left, Vector<int> right) => AbsoluteDifferenceWideningLowerAndAddEven(addend, left, right);

        /// <summary>
        /// svuint16_t svabalb[_u16](svuint16_t op1, svuint8_t op2, svuint8_t op3)
        ///   UABALB Ztied1.H, Zop2.B, Zop3.B
        /// </summary>
        public static Vector<ushort> AbsoluteDifferenceWideningLowerAndAddEven(Vector<ushort> addend, Vector<byte> left, Vector<byte> right) => AbsoluteDifferenceWideningLowerAndAddEven(addend, left, right);

        /// <summary>
        /// svuint32_t svabalb[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3)
        ///   UABALB Ztied1.S, Zop2.H, Zop3.H
        /// </summary>
        public static Vector<uint> AbsoluteDifferenceWideningLowerAndAddEven(Vector<uint> addend, Vector<ushort> left, Vector<ushort> right) => AbsoluteDifferenceWideningLowerAndAddEven(addend, left, right);

        /// <summary>
        /// svuint64_t svabalb[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3)
        ///   UABALB Ztied1.D, Zop2.S, Zop3.S
        /// </summary>
        public static Vector<ulong> AbsoluteDifferenceWideningLowerAndAddEven(Vector<ulong> addend, Vector<uint> left, Vector<uint> right) => AbsoluteDifferenceWideningLowerAndAddEven(addend, left, right);

        // Absolute difference and accumulate long (top)

        /// <summary>
        /// svint16_t svabalt[_s16](svint16_t op1, svint8_t op2, svint8_t op3)
        ///   SABALT Ztied1.H, Zop2.B, Zop3.B
        /// </summary>
        public static Vector<short> AbsoluteDifferenceWideningLowerAndAddOdd(Vector<short> addend, Vector<sbyte> left, Vector<sbyte> right) => AbsoluteDifferenceWideningLowerAndAddOdd(addend, left, right);

        /// <summary>
        /// svint32_t svabalt[_s32](svint32_t op1, svint16_t op2, svint16_t op3)
        ///   SABALT Ztied1.S, Zop2.H, Zop3.H
        /// </summary>
        public static Vector<int> AbsoluteDifferenceWideningLowerAndAddOdd(Vector<int> addend, Vector<short> left, Vector<short> right) => AbsoluteDifferenceWideningLowerAndAddOdd(addend, left, right);

        /// <summary>
        /// svint64_t svabalt[_s64](svint64_t op1, svint32_t op2, svint32_t op3)
        ///   SABALT Ztied1.D, Zop2.S, Zop3.S
        /// </summary>
        public static Vector<long> AbsoluteDifferenceWideningLowerAndAddOdd(Vector<long> addend, Vector<int> left, Vector<int> right) => AbsoluteDifferenceWideningLowerAndAddOdd(addend, left, right);

        /// <summary>
        /// svuint16_t svabalt[_u16](svuint16_t op1, svuint8_t op2, svuint8_t op3)
        ///   UABALT Ztied1.H, Zop2.B, Zop3.B
        /// </summary>
        public static Vector<ushort> AbsoluteDifferenceWideningLowerAndAddOdd(Vector<ushort> addend, Vector<byte> left, Vector<byte> right) => AbsoluteDifferenceWideningLowerAndAddOdd(addend, left, right);

        /// <summary>
        /// svuint32_t svabalt[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3)
        ///   UABALT Ztied1.S, Zop2.H, Zop3.H
        /// </summary>
        public static Vector<uint> AbsoluteDifferenceWideningLowerAndAddOdd(Vector<uint> addend, Vector<ushort> left, Vector<ushort> right) => AbsoluteDifferenceWideningLowerAndAddOdd(addend, left, right);

        /// <summary>
        /// svuint64_t svabalt[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3)
        ///   UABALT Ztied1.D, Zop2.S, Zop3.S
        /// </summary>
        public static Vector<ulong> AbsoluteDifferenceWideningLowerAndAddOdd(Vector<ulong> addend, Vector<uint> left, Vector<uint> right) => AbsoluteDifferenceWideningLowerAndAddOdd(addend, left, right);

        // Absolute difference long (bottom)

        /// <summary>
        /// svint16_t svabdlb[_s16](svint8_t op1, svint8_t op2)
        ///   SABDLB Zresult.H, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<short> AbsoluteDifferenceWideningEven(Vector<sbyte> left, Vector<sbyte> right) => AbsoluteDifferenceWideningEven(left, right);

        /// <summary>
        /// svint32_t svabdlb[_s32](svint16_t op1, svint16_t op2)
        ///   SABDLB Zresult.S, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<int> AbsoluteDifferenceWideningEven(Vector<short> left, Vector<short> right) => AbsoluteDifferenceWideningEven(left, right);

        /// <summary>
        /// svint64_t svabdlb[_s64](svint32_t op1, svint32_t op2)
        ///   SABDLB Zresult.D, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<long> AbsoluteDifferenceWideningEven(Vector<int> left, Vector<int> right) => AbsoluteDifferenceWideningEven(left, right);

        /// <summary>
        /// svuint16_t svabdlb[_u16](svuint8_t op1, svuint8_t op2)
        ///   UABDLB Zresult.H, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<ushort> AbsoluteDifferenceWideningEven(Vector<byte> left, Vector<byte> right) => AbsoluteDifferenceWideningEven(left, right);

        /// <summary>
        /// svuint32_t svabdlb[_u32](svuint16_t op1, svuint16_t op2)
        ///   UABDLB Zresult.S, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<uint> AbsoluteDifferenceWideningEven(Vector<ushort> left, Vector<ushort> right) => AbsoluteDifferenceWideningEven(left, right);

        /// <summary>
        /// svuint64_t svabdlb[_u64](svuint32_t op1, svuint32_t op2)
        ///   UABDLB Zresult.D, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<ulong> AbsoluteDifferenceWideningEven(Vector<uint> left, Vector<uint> right) => AbsoluteDifferenceWideningEven(left, right);

        // Absolute difference long (top)

        /// <summary>
        /// svint16_t svabdlt[_s16](svint8_t op1, svint8_t op2)
        ///   SABDLT Zresult.H, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<short> AbsoluteDifferenceWideningOdd(Vector<sbyte> left, Vector<sbyte> right) => AbsoluteDifferenceWideningOdd(left, right);

        /// <summary>
        /// svint32_t svabdlt[_s32](svint16_t op1, svint16_t op2)
        ///   SABDLT Zresult.S, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<int> AbsoluteDifferenceWideningOdd(Vector<short> left, Vector<short> right) => AbsoluteDifferenceWideningOdd(left, right);

        /// <summary>
        /// svint64_t svabdlt[_s64](svint32_t op1, svint32_t op2)
        ///   SABDLT Zresult.D, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<long> AbsoluteDifferenceWideningOdd(Vector<int> left, Vector<int> right) => AbsoluteDifferenceWideningOdd(left, right);

        /// <summary>
        /// svuint16_t svabdlt[_u16](svuint8_t op1, svuint8_t op2)
        ///   UABDLT Zresult.H, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<ushort> AbsoluteDifferenceWideningOdd(Vector<byte> left, Vector<byte> right) => AbsoluteDifferenceWideningOdd(left, right);

        /// <summary>
        /// svuint32_t svabdlt[_u32](svuint16_t op1, svuint16_t op2)
        ///   UABDLT Zresult.S, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<uint> AbsoluteDifferenceWideningOdd(Vector<ushort> left, Vector<ushort> right) => AbsoluteDifferenceWideningOdd(left, right);

        /// <summary>
        /// svuint64_t svabdlt[_u64](svuint32_t op1, svuint32_t op2)
        ///   UABDLT Zresult.D, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<ulong> AbsoluteDifferenceWideningOdd(Vector<uint> left, Vector<uint> right) => AbsoluteDifferenceWideningOdd(left, right);

        // Add with carry long (bottom)

        /// <summary>
        /// svuint32_t svadclb[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3)
        ///   ADCLB Ztied1.S, Zop2.S, Zop3.S
        /// </summary>
        public static unsafe Vector<uint> AddCarryWideningEven(Vector<uint> op1, Vector<uint> op2, Vector<uint> op3) => AddCarryWideningEven(op1, op2, op3);

        /// <summary>
        /// svuint64_t svadclb[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3)
        ///   ADCLB Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static unsafe Vector<ulong> AddCarryWideningEven(Vector<ulong> op1, Vector<ulong> op2, Vector<ulong> op3) => AddCarryWideningEven(op1, op2, op3);

        // Add with carry long (top)

        /// <summary>
        /// svuint32_t svadclt[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3)
        ///   ADCLT Ztied1.S, Zop2.S, Zop3.S
        /// </summary>
        public static unsafe Vector<uint> AddCarryWideningOdd(Vector<uint> op1, Vector<uint> op2, Vector<uint> op3) => AddCarryWideningOdd(op1, op2, op3);

        /// <summary>
        /// svuint64_t svadclt[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3)
        ///   ADCLT Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static unsafe Vector<ulong> AddCarryWideningOdd(Vector<ulong> op1, Vector<ulong> op2, Vector<ulong> op3) => AddCarryWideningOdd(op1, op2, op3);

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

        // Add pairwise

        /// <summary>
        /// svuint8_t svaddp[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svaddp[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   ADDP Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   ADDP Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        /// </summary>
        public static Vector<byte> AddPairwise(Vector<byte> left, Vector<byte> right) => AddPairwise(left, right);

        /// <summary>
        /// svfloat64_t svaddp[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svaddp[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FADDP Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   FADDP Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// </summary>
        public static Vector<double> AddPairwise(Vector<double> left, Vector<double> right) => AddPairwise(left, right);

        /// <summary>
        /// svint16_t svaddp[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svaddp[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   ADDP Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   ADDP Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        /// </summary>
        public static Vector<short> AddPairwise(Vector<short> left, Vector<short> right) => AddPairwise(left, right);

        /// <summary>
        /// svint32_t svaddp[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svaddp[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   ADDP Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   ADDP Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// </summary>
        public static Vector<int> AddPairwise(Vector<int> left, Vector<int> right) => AddPairwise(left, right);

        /// <summary>
        /// svint64_t svaddp[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svaddp[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   ADDP Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   ADDP Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// </summary>
        public static Vector<long> AddPairwise(Vector<long> left, Vector<long> right) => AddPairwise(left, right);

        /// <summary>
        /// svint8_t svaddp[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svaddp[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   ADDP Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   ADDP Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        /// </summary>
        public static Vector<sbyte> AddPairwise(Vector<sbyte> left, Vector<sbyte> right) => AddPairwise(left, right);

        /// <summary>
        /// svfloat32_t svaddp[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svaddp[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FADDP Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   FADDP Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// </summary>
        public static Vector<float> AddPairwise(Vector<float> left, Vector<float> right) => AddPairwise(left, right);

        /// <summary>
        /// svuint16_t svaddp[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svaddp[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   ADDP Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   ADDP Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        /// </summary>
        public static Vector<ushort> AddPairwise(Vector<ushort> left, Vector<ushort> right) => AddPairwise(left, right);

        /// <summary>
        /// svuint32_t svaddp[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svaddp[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   ADDP Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   ADDP Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// </summary>
        public static Vector<uint> AddPairwise(Vector<uint> left, Vector<uint> right) => AddPairwise(left, right);

        /// <summary>
        /// svuint64_t svaddp[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svaddp[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   ADDP Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   ADDP Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// </summary>
        public static Vector<ulong> AddPairwise(Vector<ulong> left, Vector<ulong> right) => AddPairwise(left, right);

        // Add and accumulate long pairwise

        /// <summary>
        /// svint16_t svadalp[_s16]_m(svbool_t pg, svint16_t op1, svint8_t op2)
        /// svint16_t svadalp[_s16]_x(svbool_t pg, svint16_t op1, svint8_t op2)
        /// svint16_t svadalp[_s16]_z(svbool_t pg, svint16_t op1, svint8_t op2)
        ///   SADALP Ztied1.H, Pg/M, Zop2.B
        ///   SADALP Ztied1.H, Pg/M, Zop2.B
        /// </summary>
        public static Vector<short> AddPairwiseWideningAndAdd(Vector<short> left, Vector<sbyte> right) => AddPairwiseWideningAndAdd(left, right);

        /// <summary>
        /// svint32_t svadalp[_s32]_m(svbool_t pg, svint32_t op1, svint16_t op2)
        /// svint32_t svadalp[_s32]_x(svbool_t pg, svint32_t op1, svint16_t op2)
        /// svint32_t svadalp[_s32]_z(svbool_t pg, svint32_t op1, svint16_t op2)
        ///   SADALP Ztied1.S, Pg/M, Zop2.H
        ///   SADALP Ztied1.S, Pg/M, Zop2.H
        /// </summary>
        public static Vector<int> AddPairwiseWideningAndAdd(Vector<int> left, Vector<short> right) => AddPairwiseWideningAndAdd(left, right);

        /// <summary>
        /// svint64_t svadalp[_s64]_m(svbool_t pg, svint64_t op1, svint32_t op2)
        /// svint64_t svadalp[_s64]_x(svbool_t pg, svint64_t op1, svint32_t op2)
        /// svint64_t svadalp[_s64]_z(svbool_t pg, svint64_t op1, svint32_t op2)
        ///   SADALP Ztied1.D, Pg/M, Zop2.S
        ///   SADALP Ztied1.D, Pg/M, Zop2.S
        /// </summary>
        public static Vector<long> AddPairwiseWideningAndAdd(Vector<long> left, Vector<int> right) => AddPairwiseWideningAndAdd(left, right);

        /// <summary>
        /// svuint16_t svadalp[_u16]_m(svbool_t pg, svuint16_t op1, svuint8_t op2)
        /// svuint16_t svadalp[_u16]_x(svbool_t pg, svuint16_t op1, svuint8_t op2)
        /// svuint16_t svadalp[_u16]_z(svbool_t pg, svuint16_t op1, svuint8_t op2)
        ///   UADALP Ztied1.H, Pg/M, Zop2.B
        ///   UADALP Ztied1.H, Pg/M, Zop2.B
        /// </summary>
        public static Vector<ushort> AddPairwiseWideningAndAdd(Vector<ushort> left, Vector<byte> right) => AddPairwiseWideningAndAdd(left, right);

        /// <summary>
        /// svuint32_t svadalp[_u32]_m(svbool_t pg, svuint32_t op1, svuint16_t op2)
        /// svuint32_t svadalp[_u32]_x(svbool_t pg, svuint32_t op1, svuint16_t op2)
        /// svuint32_t svadalp[_u32]_z(svbool_t pg, svuint32_t op1, svuint16_t op2)
        ///   UADALP Ztied1.S, Pg/M, Zop2.H
        ///   UADALP Ztied1.S, Pg/M, Zop2.H
        /// </summary>
        public static Vector<uint> AddPairwiseWideningAndAdd(Vector<uint> left, Vector<ushort> right) => AddPairwiseWideningAndAdd(left, right);

        /// <summary>
        /// svuint64_t svadalp[_u64]_m(svbool_t pg, svuint64_t op1, svuint32_t op2)
        /// svuint64_t svadalp[_u64]_x(svbool_t pg, svuint64_t op1, svuint32_t op2)
        /// svuint64_t svadalp[_u64]_z(svbool_t pg, svuint64_t op1, svuint32_t op2)
        ///   UADALP Ztied1.D, Pg/M, Zop2.S
        ///   UADALP Ztied1.D, Pg/M, Zop2.S
        /// </summary>
        public static Vector<ulong> AddPairwiseWideningAndAdd(Vector<ulong> left, Vector<uint> right) => AddPairwiseWideningAndAdd(left, right);


        // Rounding add narrow high part (bottom)

        /// <summary>
        /// svuint8_t svraddhnb[_u16](svuint16_t op1, svuint16_t op2)
        ///   RADDHNB Zresult.B, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<byte> AddRoundedHighNarrowingEven(Vector<ushort> left, Vector<ushort> right) => AddRoundedHighNarrowingEven(left, right);

        /// <summary>
        /// svint16_t svraddhnb[_s32](svint32_t op1, svint32_t op2)
        ///   RADDHNB Zresult.H, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<short> AddRoundedHighNarrowingEven(Vector<int> left, Vector<int> right) => AddRoundedHighNarrowingEven(left, right);

        /// <summary>
        /// svint32_t svraddhnb[_s64](svint64_t op1, svint64_t op2)
        ///   RADDHNB Zresult.S, Zop1.D, Zop2.D
        /// </summary>
        public static Vector<int> AddRoundedHighNarrowingEven(Vector<long> left, Vector<long> right) => AddRoundedHighNarrowingEven(left, right);

        /// <summary>
        /// svint8_t svraddhnb[_s16](svint16_t op1, svint16_t op2)
        ///   RADDHNB Zresult.B, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<sbyte> AddRoundedHighNarrowingEven(Vector<short> left, Vector<short> right) => AddRoundedHighNarrowingEven(left, right);

        /// <summary>
        /// svuint16_t svraddhnb[_u32](svuint32_t op1, svuint32_t op2)
        ///   RADDHNB Zresult.H, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<ushort> AddRoundedHighNarrowingEven(Vector<uint> left, Vector<uint> right) => AddRoundedHighNarrowingEven(left, right);

        /// <summary>
        /// svuint32_t svraddhnb[_u64](svuint64_t op1, svuint64_t op2)
        ///   RADDHNB Zresult.S, Zop1.D, Zop2.D
        /// </summary>
        public static Vector<uint> AddRoundedHighNarrowingEven(Vector<ulong> left, Vector<ulong> right) => AddRoundedHighNarrowingEven(left, right);


        // Rounding add narrow high part (top)

        /// <summary>
        /// svuint8_t svraddhnt[_u16](svuint8_t even, svuint16_t op1, svuint16_t op2)
        ///   RADDHNT Ztied.B, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<byte> AddRoundedHighNarrowingOdd(Vector<byte> even, Vector<ushort> left, Vector<ushort> right) => AddRoundedHighNarrowingOdd(even, left, right);

        /// <summary>
        /// svint16_t svraddhnt[_s32](svint16_t even, svint32_t op1, svint32_t op2)
        ///   RADDHNT Ztied.H, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<short> AddRoundedHighNarrowingOdd(Vector<short> even, Vector<int> left, Vector<int> right) => AddRoundedHighNarrowingOdd(even, left, right);

        /// <summary>
        /// svint32_t svraddhnt[_s64](svint32_t even, svint64_t op1, svint64_t op2)
        ///   RADDHNT Ztied.S, Zop1.D, Zop2.D
        /// </summary>
        public static Vector<int> AddRoundedHighNarrowingOdd(Vector<int> even, Vector<long> left, Vector<long> right) => AddRoundedHighNarrowingOdd(even, left, right);

        /// <summary>
        /// svint8_t svraddhnt[_s16](svint8_t even, svint16_t op1, svint16_t op2)
        ///   RADDHNT Ztied.B, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<sbyte> AddRoundedHighNarrowingOdd(Vector<sbyte> even, Vector<short> left, Vector<short> right) => AddRoundedHighNarrowingOdd(even, left, right);

        /// <summary>
        /// svuint16_t svraddhnt[_u32](svuint16_t even, svuint32_t op1, svuint32_t op2)
        ///   RADDHNT Ztied.H, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<ushort> AddRoundedHighNarrowingOdd(Vector<ushort> even, Vector<uint> left, Vector<uint> right) => AddRoundedHighNarrowingOdd(even, left, right);

        /// <summary>
        /// svuint32_t svraddhnt[_u64](svuint32_t even, svuint64_t op1, svuint64_t op2)
        ///   RADDHNT Ztied.S, Zop1.D, Zop2.D
        /// </summary>
        public static Vector<uint> AddRoundedHighNarrowingOdd(Vector<uint> even, Vector<ulong> left, Vector<ulong> right) => AddRoundedHighNarrowingOdd(even, left, right);


        // Saturating add

        /// <summary>
        /// svuint8_t svqadd[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svqadd[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svqadd[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   UQADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   UQADD Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   UQADD Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static new Vector<byte> AddSaturate(Vector<byte> left, Vector<byte> right) => AddSaturate(left, right);

        /// <summary>
        /// svint16_t svqadd[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svqadd[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svqadd[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SQADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   SQADD Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   SQADD Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static new Vector<short> AddSaturate(Vector<short> left, Vector<short> right) => AddSaturate(left, right);

        /// <summary>
        /// svint32_t svqadd[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svqadd[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svqadd[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SQADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   SQADD Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   SQADD Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static new Vector<int> AddSaturate(Vector<int> left, Vector<int> right) => AddSaturate(left, right);

        /// <summary>
        /// svint64_t svqadd[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svqadd[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svqadd[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SQADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   SQADD Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   SQADD Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static new Vector<long> AddSaturate(Vector<long> left, Vector<long> right) => AddSaturate(left, right);

        /// <summary>
        /// svint8_t svqadd[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svqadd[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svqadd[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SQADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   SQADD Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   SQADD Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static new Vector<sbyte> AddSaturate(Vector<sbyte> left, Vector<sbyte> right) => AddSaturate(left, right);

        /// <summary>
        /// svuint16_t svqadd[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svqadd[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svqadd[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   UQADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   UQADD Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   UQADD Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static new Vector<ushort> AddSaturate(Vector<ushort> left, Vector<ushort> right) => AddSaturate(left, right);

        /// <summary>
        /// svuint32_t svqadd[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svqadd[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svqadd[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   UQADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   UQADD Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   UQADD Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static new Vector<uint> AddSaturate(Vector<uint> left, Vector<uint> right) => AddSaturate(left, right);

        /// <summary>
        /// svuint64_t svqadd[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svqadd[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svqadd[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   UQADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   UQADD Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   UQADD Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static new Vector<ulong> AddSaturate(Vector<ulong> left, Vector<ulong> right) => AddSaturate(left, right);

        // Saturating add with signed addend

        /// <summary>
        /// svuint8_t svsqadd[_u8]_m(svbool_t pg, svuint8_t op1, svint8_t op2)
        /// svuint8_t svsqadd[_u8]_x(svbool_t pg, svuint8_t op1, svint8_t op2)
        /// svuint8_t svsqadd[_u8]_z(svbool_t pg, svuint8_t op1, svint8_t op2)
        ///   USQADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   USQADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        /// </summary>
        public static Vector<byte> AddSaturateWithSignedAddend(Vector<byte> left, Vector<sbyte> right) => AddSaturateWithSignedAddend(left, right);

        /// <summary>
        /// svuint16_t svsqadd[_u16]_m(svbool_t pg, svuint16_t op1, svint16_t op2)
        /// svuint16_t svsqadd[_u16]_x(svbool_t pg, svuint16_t op1, svint16_t op2)
        /// svuint16_t svsqadd[_u16]_z(svbool_t pg, svuint16_t op1, svint16_t op2)
        ///   USQADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   USQADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        /// </summary>
        public static Vector<ushort> AddSaturateWithSignedAddend(Vector<ushort> left, Vector<short> right) => AddSaturateWithSignedAddend(left, right);

        /// <summary>
        /// svuint32_t svsqadd[_u32]_m(svbool_t pg, svuint32_t op1, svint32_t op2)
        /// svuint32_t svsqadd[_u32]_x(svbool_t pg, svuint32_t op1, svint32_t op2)
        /// svuint32_t svsqadd[_u32]_z(svbool_t pg, svuint32_t op1, svint32_t op2)
        ///   USQADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   USQADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// </summary>
        public static Vector<uint> AddSaturateWithSignedAddend(Vector<uint> left, Vector<int> right) => AddSaturateWithSignedAddend(left, right);

        /// <summary>
        /// svuint64_t svsqadd[_u64]_m(svbool_t pg, svuint64_t op1, svint64_t op2)
        /// svuint64_t svsqadd[_u64]_x(svbool_t pg, svuint64_t op1, svint64_t op2)
        /// svuint64_t svsqadd[_u64]_z(svbool_t pg, svuint64_t op1, svint64_t op2)
        ///   USQADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   USQADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// </summary>
        public static Vector<ulong> AddSaturateWithSignedAddend(Vector<ulong> left, Vector<long> right) => AddSaturateWithSignedAddend(left, right);

        // Saturating add with unsigned addend

        /// <summary>
        /// svint16_t svuqadd[_s16]_m(svbool_t pg, svint16_t op1, svuint16_t op2)
        /// svint16_t svuqadd[_s16]_x(svbool_t pg, svint16_t op1, svuint16_t op2)
        /// svint16_t svuqadd[_s16]_z(svbool_t pg, svint16_t op1, svuint16_t op2)
        ///   SUQADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   SUQADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<short> AddSaturateWithUnsignedAddend(Vector<short> left, Vector<ushort> right) => AddSaturateWithUnsignedAddend(left, right);

        /// <summary>
        /// svint32_t svuqadd[_s32]_m(svbool_t pg, svint32_t op1, svuint32_t op2)
        /// svint32_t svuqadd[_s32]_x(svbool_t pg, svint32_t op1, svuint32_t op2)
        /// svint32_t svuqadd[_s32]_z(svbool_t pg, svint32_t op1, svuint32_t op2)
        ///   SUQADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   SUQADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<int> AddSaturateWithUnsignedAddend(Vector<int> left, Vector<uint> right) => AddSaturateWithUnsignedAddend(left, right);

        /// <summary>
        /// svint64_t svuqadd[_s64]_m(svbool_t pg, svint64_t op1, svuint64_t op2)
        /// svint64_t svuqadd[_s64]_x(svbool_t pg, svint64_t op1, svuint64_t op2)
        /// svint64_t svuqadd[_s64]_z(svbool_t pg, svint64_t op1, svuint64_t op2)
        ///   SUQADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   SUQADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<long> AddSaturateWithUnsignedAddend(Vector<long> left, Vector<ulong> right) => AddSaturateWithUnsignedAddend(left, right);

        /// <summary>
        /// svint8_t svuqadd[_s8]_m(svbool_t pg, svint8_t op1, svuint8_t op2)
        /// svint8_t svuqadd[_s8]_x(svbool_t pg, svint8_t op1, svuint8_t op2)
        /// svint8_t svuqadd[_s8]_z(svbool_t pg, svint8_t op1, svuint8_t op2)
        ///   SUQADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   SUQADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<sbyte> AddSaturateWithUnsignedAddend(Vector<sbyte> left, Vector<byte> right) => AddSaturateWithUnsignedAddend(left, right);

        // Add wide (bottom)

        /// <summary>
        /// svint16_t svaddwb[_s16](svint16_t op1, svint8_t op2)
        ///   SADDWB Zresult.H, Zop1.H, Zop2.B
        /// </summary>
        public static Vector<short> AddWideningEven(Vector<short> left, Vector<sbyte> right) => AddWideningEven(left, right);

        /// <summary>
        /// svint32_t svaddwb[_s32](svint32_t op1, svint16_t op2)
        ///   SADDWB Zresult.S, Zop1.S, Zop2.H
        /// </summary>
        public static Vector<int> AddWideningEven(Vector<int> left, Vector<short> right) => AddWideningEven(left, right);

        /// <summary>
        /// svint64_t svaddwb[_s64](svint64_t op1, svint32_t op2)
        ///   SADDWB Zresult.D, Zop1.D, Zop2.S
        /// </summary>
        public static Vector<long> AddWideningEven(Vector<long> left, Vector<int> right) => AddWideningEven(left, right);

        /// <summary>
        /// svuint16_t svaddwb[_u16](svuint16_t op1, svuint8_t op2)
        ///   UADDWB Zresult.H, Zop1.H, Zop2.B
        /// </summary>
        public static Vector<ushort> AddWideningEven(Vector<ushort> left, Vector<byte> right) => AddWideningEven(left, right);

        /// <summary>
        /// svuint32_t svaddwb[_u32](svuint32_t op1, svuint16_t op2)
        ///   UADDWB Zresult.S, Zop1.S, Zop2.H
        /// </summary>
        public static Vector<uint> AddWideningEven(Vector<uint> left, Vector<ushort> right) => AddWideningEven(left, right);

        /// <summary>
        /// svuint64_t svaddwb[_u64](svuint64_t op1, svuint32_t op2)
        ///   UADDWB Zresult.D, Zop1.D, Zop2.S
        /// </summary>
        public static Vector<ulong> AddWideningEven(Vector<ulong> left, Vector<uint> right) => AddWideningEven(left, right);

        /// <summary>
        /// svint16_t svaddlb[_s16](svint8_t op1, svint8_t op2)
        ///   SADDLB Zresult.H, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<short> AddWideningEven(Vector<sbyte> left, Vector<sbyte> right) => AddWideningEven(left, right);

        /// <summary>
        /// svint32_t svaddlb[_s32](svint16_t op1, svint16_t op2)
        ///   SADDLB Zresult.S, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<int> AddWideningEven(Vector<short> left, Vector<short> right) => AddWideningEven(left, right);

        /// <summary>
        /// svint64_t svaddlb[_s64](svint32_t op1, svint32_t op2)
        ///   SADDLB Zresult.D, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<long> AddWideningEven(Vector<int> left, Vector<int> right) => AddWideningEven(left, right);

        /// <summary>
        /// svuint16_t svaddlb[_u16](svuint8_t op1, svuint8_t op2)
        ///   UADDLB Zresult.H, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<ushort> AddWideningEven(Vector<byte> left, Vector<byte> right) => AddWideningEven(left, right);

        /// <summary>
        /// svuint32_t svaddlb[_u32](svuint16_t op1, svuint16_t op2)
        ///   UADDLB Zresult.S, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<uint> AddWideningEven(Vector<ushort> left, Vector<ushort> right) => AddWideningEven(left, right);

        /// <summary>
        /// svuint64_t svaddlb[_u64](svuint32_t op1, svuint32_t op2)
        ///   UADDLB Zresult.D, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<ulong> AddWideningEven(Vector<uint> left, Vector<uint> right) => AddWideningEven(left, right);

        // Add long (bottom + top)

        /// <summary>
        /// svint16_t svaddlbt[_s16](svint8_t op1, svint8_t op2)
        ///   SADDLBT Zresult.H, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<short> AddWideningEvenOdd(Vector<sbyte> left, Vector<sbyte> right) => AddWideningEvenOdd(left, right);

        /// <summary>
        /// svint32_t svaddlbt[_s32](svint16_t op1, svint16_t op2)
        ///   SADDLBT Zresult.S, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<int> AddWideningEvenOdd(Vector<short> left, Vector<short> right) => AddWideningEvenOdd(left, right);

        /// <summary>
        /// svint64_t svaddlbt[_s64](svint32_t op1, svint32_t op2)
        ///   SADDLBT Zresult.D, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<long> AddWideningEvenOdd(Vector<int> left, Vector<int> right) => AddWideningEvenOdd(left, right);

        // Add wide (top)

        /// <summary>
        /// svint16_t svaddwt[_s16](svint16_t op1, svint8_t op2)
        ///   SADDWT Zresult.H, Zop1.H, Zop2.B
        /// </summary>
        public static Vector<short> AddWideningOdd(Vector<short> left, Vector<sbyte> right) => AddWideningOdd(left, right);

        /// <summary>
        /// svint32_t svaddwt[_s32](svint32_t op1, svint16_t op2)
        ///   SADDWT Zresult.S, Zop1.S, Zop2.H
        /// </summary>
        public static Vector<int> AddWideningOdd(Vector<int> left, Vector<short> right) => AddWideningOdd(left, right);

        /// <summary>
        /// svint64_t svaddwt[_s64](svint64_t op1, svint32_t op2)
        ///   SADDWT Zresult.D, Zop1.D, Zop2.S
        /// </summary>
        public static Vector<long> AddWideningOdd(Vector<long> left, Vector<int> right) => AddWideningOdd(left, right);

        /// <summary>
        /// svuint16_t svaddwt[_u16](svuint16_t op1, svuint8_t op2)
        ///   UADDWT Zresult.H, Zop1.H, Zop2.B
        /// </summary>
        public static Vector<ushort> AddWideningOdd(Vector<ushort> left, Vector<byte> right) => AddWideningOdd(left, right);

        /// <summary>
        /// svuint32_t svaddwt[_u32](svuint32_t op1, svuint16_t op2)
        ///   UADDWT Zresult.S, Zop1.S, Zop2.H
        /// </summary>
        public static Vector<uint> AddWideningOdd(Vector<uint> left, Vector<ushort> right) => AddWideningOdd(left, right);

        /// <summary>
        /// svuint64_t svaddwt[_u64](svuint64_t op1, svuint32_t op2)
        ///   UADDWT Zresult.D, Zop1.D, Zop2.S
        /// </summary>
        public static Vector<ulong> AddWideningOdd(Vector<ulong> left, Vector<uint> right) => AddWideningOdd(left, right);

        /// <summary>
        /// svint16_t svaddlt[_s16](svint8_t op1, svint8_t op2)
        ///   SADDLT Zresult.H, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<short> AddWideningOdd(Vector<sbyte> left, Vector<sbyte> right) => AddWideningOdd(left, right);

        /// <summary>
        /// svint32_t svaddlt[_s32](svint16_t op1, svint16_t op2)
        ///   SADDLT Zresult.S, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<int> AddWideningOdd(Vector<short> left, Vector<short> right) => AddWideningOdd(left, right);

        /// <summary>
        /// svint64_t svaddlt[_s64](svint32_t op1, svint32_t op2)
        ///   SADDLT Zresult.D, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<long> AddWideningOdd(Vector<int> left, Vector<int> right) => AddWideningOdd(left, right);

        /// <summary>
        /// svuint16_t svaddlt[_u16](svuint8_t op1, svuint8_t op2)
        ///   UADDLT Zresult.H, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<ushort> AddWideningOdd(Vector<byte> left, Vector<byte> right) => AddWideningOdd(left, right);

        /// <summary>
        /// svuint32_t svaddlt[_u32](svuint16_t op1, svuint16_t op2)
        ///   UADDLT Zresult.S, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<uint> AddWideningOdd(Vector<ushort> left, Vector<ushort> right) => AddWideningOdd(left, right);

        /// <summary>
        /// svuint64_t svaddlt[_u64](svuint32_t op1, svuint32_t op2)
        ///   UADDLT Zresult.D, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<ulong> AddWideningOdd(Vector<uint> left, Vector<uint> right) => AddWideningOdd(left, right);

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

        // Complex dot product

        /// <summary>
        /// svint32_t svcdot[_s32](svint32_t op1, svint8_t op2, svint8_t op3, uint64_t imm_rotation)
        ///   CDOT Ztied1.S, Zop2.B, Zop3.B, #imm_rotation
        /// </summary>
        public static Vector<int> DotProductRotateComplex(Vector<int> op1, Vector<sbyte> op2, Vector<sbyte> op3, [ConstantExpected(Min = 0, Max = (byte)(3))] byte rotation) => DotProductRotateComplex(op1, op2, op3, rotation);

        /// <summary>
        /// svint64_t svcdot[_s64](svint64_t op1, svint16_t op2, svint16_t op3, uint64_t imm_rotation)
        ///   CDOT Ztied1.D, Zop2.H, Zop3.H, #imm_rotation
        /// </summary>
        public static Vector<long> DotProductRotateComplex(Vector<long> op1, Vector<short> op2, Vector<short> op3, [ConstantExpected(Min = 0, Max = (byte)(3))] byte rotation) => DotProductRotateComplex(op1, op2, op3, rotation);

        /// <summary>
        /// svint32_t svcdot_lane[_s32](svint32_t op1, svint8_t op2, svint8_t op3, uint64_t imm_index, uint64_t imm_rotation)
        ///   CDOT Ztied1.S, Zop2.B, Zop3.B[imm_index], #imm_rotation
        /// </summary>
        public static Vector<int> DotProductRotateComplexBySelectedIndex(Vector<int> op1, Vector<sbyte> op2, Vector<sbyte> op3, [ConstantExpected(Min = 0, Max = (byte)(3))] byte imm_index, [ConstantExpected(Min = 0, Max = (byte)(3))] byte rotation) => DotProductRotateComplexBySelectedIndex(op1, op2, op3, imm_index, rotation);

        /// <summary>
        /// svint64_t svcdot_lane[_s64](svint64_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index, uint64_t imm_rotation)
        ///   CDOT Ztied1.D, Zop2.H, Zop3.H[imm_index], #imm_rotation
        /// </summary>
        public static Vector<long> DotProductRotateComplexBySelectedIndex(Vector<long> op1, Vector<short> op2, Vector<short> op3, [ConstantExpected(Min = 0, Max = (byte)(1))] byte imm_index, [ConstantExpected(Min = 0, Max = (byte)(3))] byte rotation) => DotProductRotateComplexBySelectedIndex(op1, op2, op3, imm_index, rotation);


        // Halving add

        /// <summary>
        /// svuint8_t svhadd[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svhadd[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svhadd[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   UHADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   UHADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   UHADD Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        /// </summary>
        public static Vector<byte> FusedAddHalving(Vector<byte> left, Vector<byte> right) => FusedAddHalving(left, right);

        /// <summary>
        /// svint16_t svhadd[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svhadd[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svhadd[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SHADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   SHADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   SHADD Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        /// </summary>
        public static Vector<short> FusedAddHalving(Vector<short> left, Vector<short> right) => FusedAddHalving(left, right);

        /// <summary>
        /// svint32_t svhadd[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svhadd[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svhadd[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SHADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   SHADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   SHADD Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        /// </summary>
        public static Vector<int> FusedAddHalving(Vector<int> left, Vector<int> right) => FusedAddHalving(left, right);

        /// <summary>
        /// svint64_t svhadd[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svhadd[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svhadd[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SHADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   SHADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   SHADD Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        /// </summary>
        public static Vector<long> FusedAddHalving(Vector<long> left, Vector<long> right) => FusedAddHalving(left, right);

        /// <summary>
        /// svint8_t svhadd[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svhadd[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svhadd[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SHADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   SHADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   SHADD Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        /// </summary>
        public static Vector<sbyte> FusedAddHalving(Vector<sbyte> left, Vector<sbyte> right) => FusedAddHalving(left, right);

        /// <summary>
        /// svuint16_t svhadd[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svhadd[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svhadd[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   UHADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   UHADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   UHADD Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        /// </summary>
        public static Vector<ushort> FusedAddHalving(Vector<ushort> left, Vector<ushort> right) => FusedAddHalving(left, right);

        /// <summary>
        /// svuint32_t svhadd[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svhadd[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svhadd[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   UHADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   UHADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   UHADD Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        /// </summary>
        public static Vector<uint> FusedAddHalving(Vector<uint> left, Vector<uint> right) => FusedAddHalving(left, right);

        /// <summary>
        /// svuint64_t svhadd[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svhadd[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svhadd[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   UHADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   UHADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   UHADD Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        /// </summary>
        public static Vector<ulong> FusedAddHalving(Vector<ulong> left, Vector<ulong> right) => FusedAddHalving(left, right);

        // Halving subtract

        /// <summary>
        /// svuint8_t svhsub[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svhsub[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svhsub[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   UHSUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   UHSUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   UHSUBR Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        /// </summary>
        public static Vector<byte> FusedSubtractHalving(Vector<byte> left, Vector<byte> right) => FusedSubtractHalving(left, right);

        /// <summary>
        /// svint16_t svhsub[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svhsub[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svhsub[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SHSUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   SHSUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   SHSUBR Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        /// </summary>
        public static Vector<short> FusedSubtractHalving(Vector<short> left, Vector<short> right) => FusedSubtractHalving(left, right);

        /// <summary>
        /// svint32_t svhsub[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svhsub[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svhsub[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SHSUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   SHSUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   SHSUBR Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        /// </summary>
        public static Vector<int> FusedSubtractHalving(Vector<int> left, Vector<int> right) => FusedSubtractHalving(left, right);

        /// <summary>
        /// svint64_t svhsub[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svhsub[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svhsub[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SHSUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   SHSUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   SHSUBR Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        /// </summary>
        public static Vector<long> FusedSubtractHalving(Vector<long> left, Vector<long> right) => FusedSubtractHalving(left, right);

        /// <summary>
        /// svint8_t svhsub[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svhsub[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svhsub[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SHSUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   SHSUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   SHSUBR Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        /// </summary>
        public static Vector<sbyte> FusedSubtractHalving(Vector<sbyte> left, Vector<sbyte> right) => FusedSubtractHalving(left, right);

        /// <summary>
        /// svuint16_t svhsub[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svhsub[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svhsub[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   UHSUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   UHSUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   UHSUBR Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        /// </summary>
        public static Vector<ushort> FusedSubtractHalving(Vector<ushort> left, Vector<ushort> right) => FusedSubtractHalving(left, right);

        /// <summary>
        /// svuint32_t svhsub[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svhsub[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svhsub[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   UHSUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   UHSUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   UHSUBR Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        /// </summary>
        public static Vector<uint> FusedSubtractHalving(Vector<uint> left, Vector<uint> right) => FusedSubtractHalving(left, right);

        /// <summary>
        /// svuint64_t svhsub[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svhsub[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svhsub[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   UHSUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   UHSUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   UHSUBR Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        /// </summary>
        public static Vector<ulong> FusedSubtractHalving(Vector<ulong> left, Vector<ulong> right) => FusedSubtractHalving(left, right);


        // Rounding halving add

        /// <summary>
        /// svuint8_t svrhadd[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   URHADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        /// </summary>
        public static Vector<byte> FusedAddRoundedHalving(Vector<byte> left, Vector<byte> right) => FusedAddRoundedHalving(left, right);

        /// <summary>
        /// svint16_t svrhadd[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SRHADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        /// </summary>
        public static Vector<short> FusedAddRoundedHalving(Vector<short> left, Vector<short> right) => FusedAddRoundedHalving(left, right);

        /// <summary>
        /// svint32_t svrhadd[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SRHADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// </summary>
        public static Vector<int> FusedAddRoundedHalving(Vector<int> left, Vector<int> right) => FusedAddRoundedHalving(left, right);

        /// <summary>
        /// svint64_t svrhadd[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SRHADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// </summary>
        public static Vector<long> FusedAddRoundedHalving(Vector<long> left, Vector<long> right) => FusedAddRoundedHalving(left, right);

        /// <summary>
        /// svint8_t svrhadd[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SRHADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        /// </summary>
        public static Vector<sbyte> FusedAddRoundedHalving(Vector<sbyte> left, Vector<sbyte> right) => FusedAddRoundedHalving(left, right);

        /// <summary>
        /// svuint16_t svrhadd[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   URHADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        /// </summary>
        public static Vector<ushort> FusedAddRoundedHalving(Vector<ushort> left, Vector<ushort> right) => FusedAddRoundedHalving(left, right);

        /// <summary>
        /// svuint32_t svrhadd[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   URHADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// </summary>
        public static Vector<uint> FusedAddRoundedHalving(Vector<uint> left, Vector<uint> right) => FusedAddRoundedHalving(left, right);

        /// <summary>
        /// svuint64_t svrhadd[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   URHADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// </summary>
        public static Vector<ulong> FusedAddRoundedHalving(Vector<ulong> left, Vector<ulong> right) => FusedAddRoundedHalving(left, right);


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

        // Maximum number pairwise

        /// <summary>
        /// svfloat64_t svmaxnmp[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmaxnmp[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FMAXNMP Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   FMAXNMP Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// </summary>
        public static Vector<double> MaxNumberPairwise(Vector<double> left, Vector<double> right) => MaxNumberPairwise(left, right);

        /// <summary>
        /// svfloat32_t svmaxnmp[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmaxnmp[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FMAXNMP Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   FMAXNMP Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// </summary>
        public static Vector<float> MaxNumberPairwise(Vector<float> left, Vector<float> right) => MaxNumberPairwise(left, right);

        // Maximum pairwise

        /// <summary>
        /// svuint8_t svmaxp[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svmaxp[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   UMAXP Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   UMAXP Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        /// </summary>
        public static Vector<byte> MaxPairwise(Vector<byte> left, Vector<byte> right) => MaxPairwise(left, right);

        /// <summary>
        /// svfloat64_t svmaxp[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmaxp[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FMAXP Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   FMAXP Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// </summary>
        public static Vector<double> MaxPairwise(Vector<double> left, Vector<double> right) => MaxPairwise(left, right);

        /// <summary>
        /// svint16_t svmaxp[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svmaxp[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SMAXP Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   SMAXP Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        /// </summary>
        public static Vector<short> MaxPairwise(Vector<short> left, Vector<short> right) => MaxPairwise(left, right);

        /// <summary>
        /// svint32_t svmaxp[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svmaxp[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SMAXP Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   SMAXP Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// </summary>
        public static Vector<int> MaxPairwise(Vector<int> left, Vector<int> right) => MaxPairwise(left, right);

        /// <summary>
        /// svint64_t svmaxp[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svmaxp[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SMAXP Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   SMAXP Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// </summary>
        public static Vector<long> MaxPairwise(Vector<long> left, Vector<long> right) => MaxPairwise(left, right);

        /// <summary>
        /// svint8_t svmaxp[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svmaxp[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SMAXP Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   SMAXP Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        /// </summary>
        public static Vector<sbyte> MaxPairwise(Vector<sbyte> left, Vector<sbyte> right) => MaxPairwise(left, right);

        /// <summary>
        /// svfloat32_t svmaxp[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmaxp[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FMAXP Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   FMAXP Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// </summary>
        public static Vector<float> MaxPairwise(Vector<float> left, Vector<float> right) => MaxPairwise(left, right);

        /// <summary>
        /// svuint16_t svmaxp[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svmaxp[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   UMAXP Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   UMAXP Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        /// </summary>
        public static Vector<ushort> MaxPairwise(Vector<ushort> left, Vector<ushort> right) => MaxPairwise(left, right);

        /// <summary>
        /// svuint32_t svmaxp[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svmaxp[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   UMAXP Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   UMAXP Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// </summary>
        public static Vector<uint> MaxPairwise(Vector<uint> left, Vector<uint> right) => MaxPairwise(left, right);

        /// <summary>
        /// svuint64_t svmaxp[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svmaxp[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   UMAXP Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   UMAXP Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// </summary>
        public static Vector<ulong> MaxPairwise(Vector<ulong> left, Vector<ulong> right) => MaxPairwise(left, right);

        // Minimum number pairwise

        /// <summary>
        /// svfloat64_t svminnmp[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svminnmp[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FMINNMP Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   FMINNMP Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// </summary>
        public static Vector<double> MinNumberPairwise(Vector<double> left, Vector<double> right) => MinNumberPairwise(left, right);

        /// <summary>
        /// svfloat32_t svminnmp[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svminnmp[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FMINNMP Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   FMINNMP Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// </summary>
        public static Vector<float> MinNumberPairwise(Vector<float> left, Vector<float> right) => MinNumberPairwise(left, right);

        // Minimum pairwise

        /// <summary>
        /// svuint8_t svminp[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svminp[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   UMINP Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   UMINP Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        /// </summary>
        public static Vector<byte> MinPairwise(Vector<byte> left, Vector<byte> right) => MinPairwise(left, right);

        /// <summary>
        /// svfloat64_t svminp[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svminp[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FMINP Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   FMINP Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// </summary>
        public static Vector<double> MinPairwise(Vector<double> left, Vector<double> right) => MinPairwise(left, right);

        /// <summary>
        /// svint16_t svminp[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svminp[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SMINP Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   SMINP Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        /// </summary>
        public static Vector<short> MinPairwise(Vector<short> left, Vector<short> right) => MinPairwise(left, right);

        /// <summary>
        /// svint32_t svminp[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svminp[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SMINP Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   SMINP Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// </summary>
        public static Vector<int> MinPairwise(Vector<int> left, Vector<int> right) => MinPairwise(left, right);

        /// <summary>
        /// svint64_t svminp[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svminp[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SMINP Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   SMINP Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// </summary>
        public static Vector<long> MinPairwise(Vector<long> left, Vector<long> right) => MinPairwise(left, right);

        /// <summary>
        /// svint8_t svminp[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svminp[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SMINP Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   SMINP Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        /// </summary>
        public static Vector<sbyte> MinPairwise(Vector<sbyte> left, Vector<sbyte> right) => MinPairwise(left, right);

        /// <summary>
        /// svfloat32_t svminp[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svminp[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FMINP Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   FMINP Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// </summary>
        public static Vector<float> MinPairwise(Vector<float> left, Vector<float> right) => MinPairwise(left, right);

        /// <summary>
        /// svuint16_t svminp[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svminp[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   UMINP Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   UMINP Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        /// </summary>
        public static Vector<ushort> MinPairwise(Vector<ushort> left, Vector<ushort> right) => MinPairwise(left, right);

        /// <summary>
        /// svuint32_t svminp[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svminp[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   UMINP Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   UMINP Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// </summary>
        public static Vector<uint> MinPairwise(Vector<uint> left, Vector<uint> right) => MinPairwise(left, right);

        /// <summary>
        /// svuint64_t svminp[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svminp[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   UMINP Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   UMINP Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// </summary>
        public static Vector<ulong> MinPairwise(Vector<ulong> left, Vector<ulong> right) => MinPairwise(left, right);

        // Multiply-add, addend first

        /// <summary>
        /// svint16_t svmla_lane[_s16](svint16_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index)
        ///   MLA Ztied1.H, Zop2.H, Zop3.H[imm_index]
        /// </summary>
        public static Vector<short> MultiplyAddBySelectedScalar(Vector<short> addend, Vector<short> left, Vector<short> right, [ConstantExpected] byte rightIndex) => MultiplyAddBySelectedScalar(addend, left, right, rightIndex);

        /// <summary>
        /// svint32_t svmla_lane[_s32](svint32_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index)
        ///   MLA Ztied1.S, Zop2.S, Zop3.S[imm_index]
        /// </summary>
        public static Vector<int> MultiplyAddBySelectedScalar(Vector<int> addend, Vector<int> left, Vector<int> right, [ConstantExpected] byte rightIndex) => MultiplyAddBySelectedScalar(addend, left, right, rightIndex);

        /// <summary>
        /// svint64_t svmla_lane[_s64](svint64_t op1, svint64_t op2, svint64_t op3, uint64_t imm_index)
        ///   MLA Ztied1.D, Zop2.D, Zop3.D[imm_index]
        /// </summary>
        public static Vector<long> MultiplyAddBySelectedScalar(Vector<long> addend, Vector<long> left, Vector<long> right, [ConstantExpected] byte rightIndex) => MultiplyAddBySelectedScalar(addend, left, right, rightIndex);

        /// <summary>
        /// svuint16_t svmla_lane[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3, uint64_t imm_index)
        ///   MLA Ztied1.H, Zop2.H, Zop3.H[imm_index]
        /// </summary>
        public static Vector<ushort> MultiplyAddBySelectedScalar(Vector<ushort> addend, Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte rightIndex) => MultiplyAddBySelectedScalar(addend, left, right, rightIndex);

        /// <summary>
        /// svuint32_t svmla_lane[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3, uint64_t imm_index)
        ///   MLA Ztied1.S, Zop2.S, Zop3.S[imm_index]
        /// </summary>
        public static Vector<uint> MultiplyAddBySelectedScalar(Vector<uint> addend, Vector<uint> left, Vector<uint> right, [ConstantExpected] byte rightIndex) => MultiplyAddBySelectedScalar(addend, left, right, rightIndex);

        /// <summary>
        /// svuint64_t svmla_lane[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3, uint64_t imm_index)
        ///   MLA Ztied1.D, Zop2.D, Zop3.D[imm_index]
        /// </summary>
        public static Vector<ulong> MultiplyAddBySelectedScalar(Vector<ulong> addend, Vector<ulong> left, Vector<ulong> right, [ConstantExpected] byte rightIndex) => MultiplyAddBySelectedScalar(addend, left, right, rightIndex);


        // Multiply-add long (bottom)

        /// <summary>
        /// svint16_t svmlalb[_s16](svint16_t op1, svint8_t op2, svint8_t op3)
        ///   SMLALB Ztied1.H, Zop2.B, Zop3.B
        /// </summary>
        public static Vector<short> MultiplyWideningEvenAndAdd(Vector<short> addend, Vector<sbyte> left, Vector<sbyte> right) => MultiplyWideningEvenAndAdd(addend, left, right);

        /// <summary>
        /// svint32_t svmlalb[_s32](svint32_t op1, svint16_t op2, svint16_t op3)
        ///   SMLALB Ztied1.S, Zop2.H, Zop3.H
        /// </summary>
        public static Vector<int> MultiplyWideningEvenAndAdd(Vector<int> addend, Vector<short> left, Vector<short> right) => MultiplyWideningEvenAndAdd(addend, left, right);

        /// <summary>
        /// svint64_t svmlalb[_s64](svint64_t op1, svint32_t op2, svint32_t op3)
        ///   SMLALB Ztied1.D, Zop2.S, Zop3.S
        /// </summary>
        public static Vector<long> MultiplyWideningEvenAndAdd(Vector<long> addend, Vector<int> left, Vector<int> right) => MultiplyWideningEvenAndAdd(addend, left, right);

        /// <summary>
        /// svuint16_t svmlalb[_u16](svuint16_t op1, svuint8_t op2, svuint8_t op3)
        ///   UMLALB Ztied1.H, Zop2.B, Zop3.B
        /// </summary>
        public static Vector<ushort> MultiplyWideningEvenAndAdd(Vector<ushort> addend, Vector<byte> left, Vector<byte> right) => MultiplyWideningEvenAndAdd(addend, left, right);

        /// <summary>
        /// svuint32_t svmlalb[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3)
        ///   UMLALB Ztied1.S, Zop2.H, Zop3.H
        /// </summary>
        public static Vector<uint> MultiplyWideningEvenAndAdd(Vector<uint> addend, Vector<ushort> left, Vector<ushort> right) => MultiplyWideningEvenAndAdd(addend, left, right);

        /// <summary>
        /// svuint64_t svmlalb[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3)
        ///   UMLALB Ztied1.D, Zop2.S, Zop3.S
        /// </summary>
        public static Vector<ulong> MultiplyWideningEvenAndAdd(Vector<ulong> addend, Vector<uint> left, Vector<uint> right) => MultiplyWideningEvenAndAdd(addend, left, right);

        /// <summary>
        /// svint32_t svmlalb_lane[_s32](svint32_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index)
        ///   SMLALB Ztied1.S, Zop2.H, Zop3.H[imm_index]
        /// </summary>
        public static Vector<int> MultiplyBySelectedScalarWideningEvenAndAdd(Vector<int> addend, Vector<short> left, Vector<short> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalarWideningEvenAndAdd(addend, left, right, rightIndex);

        /// <summary>
        /// svint64_t svmlalb_lane[_s64](svint64_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index)
        ///   SMLALB Ztied1.D, Zop2.S, Zop3.S[imm_index]
        /// </summary>
        public static Vector<long> MultiplyBySelectedScalarWideningEvenAndAdd(Vector<long> addend, Vector<int> left, Vector<int> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalarWideningEvenAndAdd(addend, left, right, rightIndex);

        /// <summary>
        /// svuint32_t svmlalb_lane[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3, uint64_t imm_index)
        ///   UMLALB Ztied1.S, Zop2.H, Zop3.H[imm_index]
        /// </summary>
        public static Vector<uint> MultiplyBySelectedScalarWideningEvenAndAdd(Vector<uint> addend, Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalarWideningEvenAndAdd(addend, left, right, rightIndex);

        /// <summary>
        /// svuint64_t svmlalb_lane[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3, uint64_t imm_index)
        ///   UMLALB Ztied1.D, Zop2.S, Zop3.S[imm_index]
        /// </summary>
        public static Vector<ulong> MultiplyBySelectedScalarWideningEvenAndAdd(Vector<ulong> addend, Vector<uint> left, Vector<uint> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalarWideningEvenAndAdd(addend, left, right, rightIndex);


        // Multiply-add long (top)

        /// <summary>
        /// svint16_t svmlalt[_s16](svint16_t op1, svint8_t op2, svint8_t op3)
        ///   SMLALT Ztied1.H, Zop2.B, Zop3.B
        /// </summary>
        public static Vector<short> MultiplyWideningOddAndAdd(Vector<short> addend, Vector<sbyte> left, Vector<sbyte> right) => MultiplyWideningOddAndAdd(addend, left, right);

        /// <summary>
        /// svint32_t svmlalt[_s32](svint32_t op1, svint16_t op2, svint16_t op3)
        ///   SMLALT Ztied1.S, Zop2.H, Zop3.H
        /// </summary>
        public static Vector<int> MultiplyWideningOddAndAdd(Vector<int> addend, Vector<short> left, Vector<short> right) => MultiplyWideningOddAndAdd(addend, left, right);

        /// <summary>
        /// svint64_t svmlalt[_s64](svint64_t op1, svint32_t op2, svint32_t op3)
        ///   SMLALT Ztied1.D, Zop2.S, Zop3.S
        /// </summary>
        public static Vector<long> MultiplyWideningOddAndAdd(Vector<long> addend, Vector<int> left, Vector<int> right) => MultiplyWideningOddAndAdd(addend, left, right);

        /// <summary>
        /// svuint16_t svmlalt[_u16](svuint16_t op1, svuint8_t op2, svuint8_t op3)
        ///   UMLALT Ztied1.H, Zop2.B, Zop3.B
        /// </summary>
        public static Vector<ushort> MultiplyWideningOddAndAdd(Vector<ushort> addend, Vector<byte> left, Vector<byte> right) => MultiplyWideningOddAndAdd(addend, left, right);

        /// <summary>
        /// svuint32_t svmlalt[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3)
        ///   UMLALT Ztied1.S, Zop2.H, Zop3.H
        /// </summary>
        public static Vector<uint> MultiplyWideningOddAndAdd(Vector<uint> addend, Vector<ushort> left, Vector<ushort> right) => MultiplyWideningOddAndAdd(addend, left, right);

        /// <summary>
        /// svuint64_t svmlalt[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3)
        ///   UMLALT Ztied1.D, Zop2.S, Zop3.S
        /// </summary>
        public static Vector<ulong> MultiplyWideningOddAndAdd(Vector<ulong> addend, Vector<uint> left, Vector<uint> right) => MultiplyWideningOddAndAdd(addend, left, right);

        /// <summary>
        /// svint32_t svmlalt_lane[_s32](svint32_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index)
        ///   SMLALT Ztied1.S, Zop2.H, Zop3.H[imm_index]
        /// </summary>
        public static Vector<int> MultiplyBySelectedScalarWideningOddAndAdd(Vector<int> addend, Vector<short> left, Vector<short> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalarWideningOddAndAdd(addend, left, right, rightIndex);

        /// <summary>
        /// svint64_t svmlalt_lane[_s64](svint64_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index)
        ///   SMLALT Ztied1.D, Zop2.S, Zop3.S[imm_index]
        /// </summary>
        public static Vector<long> MultiplyBySelectedScalarWideningOddAndAdd(Vector<long> addend, Vector<int> left, Vector<int> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalarWideningOddAndAdd(addend, left, right, rightIndex);

        /// <summary>
        /// svuint32_t svmlalt_lane[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3, uint64_t imm_index)
        ///   UMLALT Ztied1.S, Zop2.H, Zop3.H[imm_index]
        /// </summary>
        public static Vector<uint> MultiplyBySelectedScalarWideningOddAndAdd(Vector<uint> addend, Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalarWideningOddAndAdd(addend, left, right, rightIndex);

        /// <summary>
        /// svuint64_t svmlalt_lane[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3, uint64_t imm_index)
        ///   UMLALT Ztied1.D, Zop2.S, Zop3.S[imm_index]
        /// </summary>
        public static Vector<ulong> MultiplyBySelectedScalarWideningOddAndAdd(Vector<ulong> addend, Vector<uint> left, Vector<uint> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalarWideningOddAndAdd(addend, left, right, rightIndex);


        // Multiply

        /// <summary>
        /// svint16_t svmul_lane[_s16](svint16_t op1, svint16_t op2, uint64_t imm_index)
        ///   MUL Zresult.H, Zop1.H, Zop2.H[imm_index]
        /// </summary>
        public static Vector<short> MultiplyBySelectedScalar(Vector<short> left, Vector<short> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalar(left, right, rightIndex);

        /// <summary>
        /// svint32_t svmul_lane[_s32](svint32_t op1, svint32_t op2, uint64_t imm_index)
        ///   MUL Zresult.S, Zop1.S, Zop2.S[imm_index]
        /// </summary>
        public static Vector<int> MultiplyBySelectedScalar(Vector<int> left, Vector<int> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalar(left, right, rightIndex);

        /// <summary>
        /// svint64_t svmul_lane[_s64](svint64_t op1, svint64_t op2, uint64_t imm_index)
        ///   MUL Zresult.D, Zop1.D, Zop2.D[imm_index]
        /// </summary>
        public static Vector<long> MultiplyBySelectedScalar(Vector<long> left, Vector<long> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalar(left, right, rightIndex);

        /// <summary>
        /// svuint16_t svmul_lane[_u16](svuint16_t op1, svuint16_t op2, uint64_t imm_index)
        ///   MUL Zresult.H, Zop1.H, Zop2.H[imm_index]
        /// </summary>
        public static Vector<ushort> MultiplyBySelectedScalar(Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalar(left, right, rightIndex);

        /// <summary>
        /// svuint32_t svmul_lane[_u32](svuint32_t op1, svuint32_t op2, uint64_t imm_index)
        ///   MUL Zresult.S, Zop1.S, Zop2.S[imm_index]
        /// </summary>
        public static Vector<uint> MultiplyBySelectedScalar(Vector<uint> left, Vector<uint> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalar(left, right, rightIndex);

        /// <summary>
        /// svuint64_t svmul_lane[_u64](svuint64_t op1, svuint64_t op2, uint64_t imm_index)
        ///   MUL Zresult.D, Zop1.D, Zop2.D[imm_index]
        /// </summary>
        public static Vector<ulong> MultiplyBySelectedScalar(Vector<ulong> left, Vector<ulong> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalar(left, right, rightIndex);


        // Multiply-subtract, minuend first

        /// <summary>
        /// svint16_t svmls_lane[_s16](svint16_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index)
        ///   MLS Ztied1.H, Zop2.H, Zop3.H[imm_index]
        /// </summary>
        public static Vector<short> MultiplySubtractBySelectedScalar(Vector<short> minuend, Vector<short> left, Vector<short> right, [ConstantExpected] byte rightIndex) => MultiplySubtractBySelectedScalar(minuend, left, right, rightIndex);

        /// <summary>
        /// svint32_t svmls_lane[_s32](svint32_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index)
        ///   MLS Ztied1.S, Zop2.S, Zop3.S[imm_index]
        /// </summary>
        public static Vector<int> MultiplySubtractBySelectedScalar(Vector<int> minuend, Vector<int> left, Vector<int> right, [ConstantExpected] byte rightIndex) => MultiplySubtractBySelectedScalar(minuend, left, right, rightIndex);

        /// <summary>
        /// svint64_t svmls_lane[_s64](svint64_t op1, svint64_t op2, svint64_t op3, uint64_t imm_index)
        ///   MLS Ztied1.D, Zop2.D, Zop3.D[imm_index]
        /// </summary>
        public static Vector<long> MultiplySubtractBySelectedScalar(Vector<long> minuend, Vector<long> left, Vector<long> right, [ConstantExpected] byte rightIndex) => MultiplySubtractBySelectedScalar(minuend, left, right, rightIndex);

        /// <summary>
        /// svuint16_t svmls_lane[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3, uint64_t imm_index)
        ///   MLS Ztied1.H, Zop2.H, Zop3.H[imm_index]
        /// </summary>
        public static Vector<ushort> MultiplySubtractBySelectedScalar(Vector<ushort> minuend, Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte rightIndex) => MultiplySubtractBySelectedScalar(minuend, left, right, rightIndex);

        /// <summary>
        /// svuint32_t svmls_lane[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3, uint64_t imm_index)
        ///   MLS Ztied1.S, Zop2.S, Zop3.S[imm_index]
        /// </summary>
        public static Vector<uint> MultiplySubtractBySelectedScalar(Vector<uint> minuend, Vector<uint> left, Vector<uint> right, [ConstantExpected] byte rightIndex) => MultiplySubtractBySelectedScalar(minuend, left, right, rightIndex);

        /// <summary>
        /// svuint64_t svmls_lane[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3, uint64_t imm_index)
        ///   MLS Ztied1.D, Zop2.D, Zop3.D[imm_index]
        /// </summary>
        public static Vector<ulong> MultiplySubtractBySelectedScalar(Vector<ulong> minuend, Vector<ulong> left, Vector<ulong> right, [ConstantExpected] byte rightIndex) => MultiplySubtractBySelectedScalar(minuend, left, right, rightIndex);


        // Multiply-subtract long (bottom)

        /// <summary>
        /// svint16_t svmlslb[_s16](svint16_t op1, svint8_t op2, svint8_t op3)
        ///   SMLSLB Ztied1.H, Zop2.B, Zop3.B
        /// </summary>
        public static Vector<short> MultiplyWideningEvenAndSubtract(Vector<short> minuend, Vector<sbyte> left, Vector<sbyte> right) => MultiplyWideningEvenAndSubtract(minuend, left, right);

        /// <summary>
        /// svint32_t svmlslb[_s32](svint32_t op1, svint16_t op2, svint16_t op3)
        ///   SMLSLB Ztied1.S, Zop2.H, Zop3.H
        /// </summary>
        public static Vector<int> MultiplyWideningEvenAndSubtract(Vector<int> minuend, Vector<short> left, Vector<short> right) => MultiplyWideningEvenAndSubtract(minuend, left, right);

        /// <summary>
        /// svint64_t svmlslb[_s64](svint64_t op1, svint32_t op2, svint32_t op3)
        ///   SMLSLB Ztied1.D, Zop2.S, Zop3.S
        /// </summary>
        public static Vector<long> MultiplyWideningEvenAndSubtract(Vector<long> minuend, Vector<int> left, Vector<int> right) => MultiplyWideningEvenAndSubtract(minuend, left, right);

        /// <summary>
        /// svuint16_t svmlslb[_u16](svuint16_t op1, svuint8_t op2, svuint8_t op3)
        ///   UMLSLB Ztied1.H, Zop2.B, Zop3.B
        /// </summary>
        public static Vector<ushort> MultiplyWideningEvenAndSubtract(Vector<ushort> minuend, Vector<byte> left, Vector<byte> right) => MultiplyWideningEvenAndSubtract(minuend, left, right);

        /// <summary>
        /// svuint32_t svmlslb[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3)
        ///   UMLSLB Ztied1.S, Zop2.H, Zop3.H
        /// </summary>
        public static Vector<uint> MultiplyWideningEvenAndSubtract(Vector<uint> minuend, Vector<ushort> left, Vector<ushort> right) => MultiplyWideningEvenAndSubtract(minuend, left, right);

        /// <summary>
        /// svuint64_t svmlslb[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3)
        ///   UMLSLB Ztied1.D, Zop2.S, Zop3.S
        /// </summary>
        public static Vector<ulong> MultiplyWideningEvenAndSubtract(Vector<ulong> minuend, Vector<uint> left, Vector<uint> right) => MultiplyWideningEvenAndSubtract(minuend, left, right);

        /// <summary>
        /// svint32_t svmlslb_lane[_s32](svint32_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index)
        ///   SMLSLB Ztied1.S, Zop2.H, Zop3.H[imm_index]
        /// </summary>
        public static Vector<int> MultiplyBySelectedScalarWideningEvenAndSubtract(Vector<int> minuend, Vector<short> left, Vector<short> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalarWideningEvenAndSubtract(minuend, left, right, rightIndex);

        /// <summary>
        /// svint64_t svmlslb_lane[_s64](svint64_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index)
        ///   SMLSLB Ztied1.D, Zop2.S, Zop3.S[imm_index]
        /// </summary>
        public static Vector<long> MultiplyBySelectedScalarWideningEvenAndSubtract(Vector<long> minuend, Vector<int> left, Vector<int> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalarWideningEvenAndSubtract(minuend, left, right, rightIndex);

        /// <summary>
        /// svuint32_t svmlslb_lane[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3, uint64_t imm_index)
        ///   UMLSLB Ztied1.S, Zop2.H, Zop3.H[imm_index]
        /// </summary>
        public static Vector<uint> MultiplyBySelectedScalarWideningEvenAndSubtract(Vector<uint> minuend, Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalarWideningEvenAndSubtract(minuend, left, right, rightIndex);

        /// <summary>
        /// svuint64_t svmlslb_lane[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3, uint64_t imm_index)
        ///   UMLSLB Ztied1.D, Zop2.S, Zop3.S[imm_index]
        /// </summary>
        public static Vector<ulong> MultiplyBySelectedScalarWideningEvenAndSubtract(Vector<ulong> minuend, Vector<uint> left, Vector<uint> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalarWideningEvenAndSubtract(minuend, left, right, rightIndex);


        // Multiply-subtract long (top)

        /// <summary>
        /// svint16_t svmlslt[_s16](svint16_t op1, svint8_t op2, svint8_t op3)
        ///   SMLSLT Ztied1.H, Zop2.B, Zop3.B
        /// </summary>
        public static Vector<short> MultiplyWideningOddAndSubtract(Vector<short> minuend, Vector<sbyte> left, Vector<sbyte> right) => MultiplyWideningOddAndSubtract(minuend, left, right);

        /// <summary>
        /// svint32_t svmlslt[_s32](svint32_t op1, svint16_t op2, svint16_t op3)
        ///   SMLSLT Ztied1.S, Zop2.H, Zop3.H
        /// </summary>
        public static Vector<int> MultiplyWideningOddAndSubtract(Vector<int> minuend, Vector<short> left, Vector<short> right) => MultiplyWideningOddAndSubtract(minuend, left, right);

        /// <summary>
        /// svint64_t svmlslt[_s64](svint64_t op1, svint32_t op2, svint32_t op3)
        ///   SMLSLT Ztied1.D, Zop2.S, Zop3.S
        /// </summary>
        public static Vector<long> MultiplyWideningOddAndSubtract(Vector<long> minuend, Vector<int> left, Vector<int> right) => MultiplyWideningOddAndSubtract(minuend, left, right);

        /// <summary>
        /// svuint16_t svmlslt[_u16](svuint16_t op1, svuint8_t op2, svuint8_t op3)
        ///   UMLSLT Ztied1.H, Zop2.B, Zop3.B
        /// </summary>
        public static Vector<ushort> MultiplyWideningOddAndSubtract(Vector<ushort> minuend, Vector<byte> left, Vector<byte> right) => MultiplyWideningOddAndSubtract(minuend, left, right);

        /// <summary>
        /// svuint32_t svmlslt[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3)
        ///   UMLSLT Ztied1.S, Zop2.H, Zop3.H
        /// </summary>
        public static Vector<uint> MultiplyWideningOddAndSubtract(Vector<uint> minuend, Vector<ushort> left, Vector<ushort> right) => MultiplyWideningOddAndSubtract(minuend, left, right);

        /// <summary>
        /// svuint64_t svmlslt[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3)
        ///   UMLSLT Ztied1.D, Zop2.S, Zop3.S
        /// </summary>
        public static Vector<ulong> MultiplyWideningOddAndSubtract(Vector<ulong> minuend, Vector<uint> left, Vector<uint> right) => MultiplyWideningOddAndSubtract(minuend, left, right);

        /// <summary>
        /// svint32_t svmlslt_lane[_s32](svint32_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index)
        ///   SMLSLT Ztied1.S, Zop2.H, Zop3.H[imm_index]
        /// </summary>
        public static Vector<int> MultiplyBySelectedScalarWideningOddAndSubtract(Vector<int> minuend, Vector<short> left, Vector<short> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalarWideningOddAndSubtract(minuend, left, right, rightIndex);

        /// <summary>
        /// svint64_t svmlslt_lane[_s64](svint64_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index)
        ///   SMLSLT Ztied1.D, Zop2.S, Zop3.S[imm_index]
        /// </summary>
        public static Vector<long> MultiplyBySelectedScalarWideningOddAndSubtract(Vector<long> minuend, Vector<int> left, Vector<int> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalarWideningOddAndSubtract(minuend, left, right, rightIndex);

        /// <summary>
        /// svuint32_t svmlslt_lane[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3, uint64_t imm_index)
        ///   UMLSLT Ztied1.S, Zop2.H, Zop3.H[imm_index]
        /// </summary>
        public static Vector<uint> MultiplyBySelectedScalarWideningOddAndSubtract(Vector<uint> minuend, Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalarWideningOddAndSubtract(minuend, left, right, rightIndex);

        /// <summary>
        /// svuint64_t svmlslt_lane[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3, uint64_t imm_index)
        ///   UMLSLT Ztied1.D, Zop2.S, Zop3.S[imm_index]
        /// </summary>
        public static Vector<ulong> MultiplyBySelectedScalarWideningOddAndSubtract(Vector<ulong> minuend, Vector<uint> left, Vector<uint> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalarWideningOddAndSubtract(minuend, left, right, rightIndex);


        // Multiply long (bottom)

        /// <summary>
        /// svint16_t svmullb[_s16](svint8_t op1, svint8_t op2)
        ///   SMULLB Zresult.H, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<short> MultiplyWideningEven(Vector<sbyte> left, Vector<sbyte> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// svint32_t svmullb[_s32](svint16_t op1, svint16_t op2)
        ///   SMULLB Zresult.S, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<int> MultiplyWideningEven(Vector<short> left, Vector<short> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// svint64_t svmullb[_s64](svint32_t op1, svint32_t op2)
        ///   SMULLB Zresult.D, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<long> MultiplyWideningEven(Vector<int> left, Vector<int> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// svuint16_t svmullb[_u16](svuint8_t op1, svuint8_t op2)
        ///   UMULLB Zresult.H, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<ushort> MultiplyWideningEven(Vector<byte> left, Vector<byte> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// svuint32_t svmullb[_u32](svuint16_t op1, svuint16_t op2)
        ///   UMULLB Zresult.S, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<uint> MultiplyWideningEven(Vector<ushort> left, Vector<ushort> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// svuint64_t svmullb[_u64](svuint32_t op1, svuint32_t op2)
        ///   UMULLB Zresult.D, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<ulong> MultiplyWideningEven(Vector<uint> left, Vector<uint> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// svint32_t svmullb_lane[_s32](svint16_t op1, svint16_t op2, uint64_t imm_index)
        ///   SMULLB Zresult.S, Zop1.H, Zop2.H[imm_index]
        /// </summary>
        public static Vector<int> MultiplyBySelectedScalarWideningEven(Vector<short> left, Vector<short> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalarWideningEven(left, right, rightIndex);

        /// <summary>
        /// svint64_t svmullb_lane[_s64](svint32_t op1, svint32_t op2, uint64_t imm_index)
        ///   SMULLB Zresult.D, Zop1.S, Zop2.S[imm_index]
        /// </summary>
        public static Vector<long> MultiplyBySelectedScalarWideningEven(Vector<int> left, Vector<int> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalarWideningEven(left, right, rightIndex);

        /// <summary>
        /// svuint32_t svmullb_lane[_u32](svuint16_t op1, svuint16_t op2, uint64_t imm_index)
        ///   UMULLB Zresult.S, Zop1.H, Zop2.H[imm_index]
        /// </summary>
        public static Vector<uint> MultiplyBySelectedScalarWideningEven(Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalarWideningEven(left, right, rightIndex);

        /// <summary>
        /// svuint64_t svmullb_lane[_u64](svuint32_t op1, svuint32_t op2, uint64_t imm_index)
        ///   UMULLB Zresult.D, Zop1.S, Zop2.S[imm_index]
        /// </summary>
        public static Vector<ulong> MultiplyBySelectedScalarWideningEven(Vector<uint> left, Vector<uint> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalarWideningEven(left, right, rightIndex);


        // Multiply long (top)

        /// <summary>
        /// svint16_t svmullt[_s16](svint8_t op1, svint8_t op2)
        ///   SMULLT Zresult.H, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<short> MultiplyWideningOdd(Vector<sbyte> left, Vector<sbyte> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// svint32_t svmullt[_s32](svint16_t op1, svint16_t op2)
        ///   SMULLT Zresult.S, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<int> MultiplyWideningOdd(Vector<short> left, Vector<short> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// svint64_t svmullt[_s64](svint32_t op1, svint32_t op2)
        ///   SMULLT Zresult.D, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<long> MultiplyWideningOdd(Vector<int> left, Vector<int> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// svuint16_t svmullt[_u16](svuint8_t op1, svuint8_t op2)
        ///   UMULLT Zresult.H, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<ushort> MultiplyWideningOdd(Vector<byte> left, Vector<byte> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// svuint32_t svmullt[_u32](svuint16_t op1, svuint16_t op2)
        ///   UMULLT Zresult.S, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<uint> MultiplyWideningOdd(Vector<ushort> left, Vector<ushort> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// svuint64_t svmullt[_u64](svuint32_t op1, svuint32_t op2)
        ///   UMULLT Zresult.D, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<ulong> MultiplyWideningOdd(Vector<uint> left, Vector<uint> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// svint32_t svmullt_lane[_s32](svint16_t op1, svint16_t op2, uint64_t imm_index)
        ///   SMULLT Zresult.S, Zop1.H, Zop2.H[imm_index]
        /// </summary>
        public static Vector<int> MultiplyBySelectedScalarWideningOdd(Vector<short> left, Vector<short> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalarWideningOdd(left, right, rightIndex);

        /// <summary>
        /// svint64_t svmullt_lane[_s64](svint32_t op1, svint32_t op2, uint64_t imm_index)
        ///   SMULLT Zresult.D, Zop1.S, Zop2.S[imm_index]
        /// </summary>
        public static Vector<long> MultiplyBySelectedScalarWideningOdd(Vector<int> left, Vector<int> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalarWideningOdd(left, right, rightIndex);

        /// <summary>
        /// svuint32_t svmullt_lane[_u32](svuint16_t op1, svuint16_t op2, uint64_t imm_index)
        ///   UMULLT Zresult.S, Zop1.H, Zop2.H[imm_index]
        /// </summary>
        public static Vector<uint> MultiplyBySelectedScalarWideningOdd(Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalarWideningOdd(left, right, rightIndex);

        /// <summary>
        /// svuint64_t svmullt_lane[_u64](svuint32_t op1, svuint32_t op2, uint64_t imm_index)
        ///   UMULLT Zresult.D, Zop1.S, Zop2.S[imm_index]
        /// </summary>
        public static Vector<ulong> MultiplyBySelectedScalarWideningOdd(Vector<uint> left, Vector<uint> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalarWideningOdd(left, right, rightIndex);


        // Polynomial multiply

        /// <summary>
        /// svuint8_t svpmul[_u8](svuint8_t op1, svuint8_t op2)
        ///   PMUL Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<byte> PolynomialMultiply(Vector<byte> left, Vector<byte> right) => PolynomialMultiply(left, right);

        /// <summary>
        /// svuint8_t svpmul[_u8](svuint8_t op1, svuint8_t op2)
        ///   PMUL Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<sbyte> PolynomialMultiply(Vector<sbyte> left, Vector<sbyte> right) => PolynomialMultiply(left, right);


        // Polynomial multiply long (bottom)

        /// <summary>
        /// svuint16_t svpmullb[_u16](svuint8_t op1, svuint8_t op2)
        ///   PMULLB Zresult.H, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<ushort> PolynomialMultiplyWideningEven(Vector<byte> left, Vector<byte> right) => PolynomialMultiplyWideningEven(left, right);

        /// <summary>
        /// svuint64_t svpmullb[_u64](svuint32_t op1, svuint32_t op2)
        ///   PMULLB Zresult.D, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<ulong> PolynomialMultiplyWideningEven(Vector<uint> left, Vector<uint> right) => PolynomialMultiplyWideningEven(left, right);


        // Polynomial multiply long (top)

        /// <summary>
        /// svuint16_t svpmullt[_u16](svuint8_t op1, svuint8_t op2)
        ///   PMULLT Zresult.H, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<ushort> PolynomialMultiplyWideningOdd(Vector<byte> left, Vector<byte> right) => PolynomialMultiplyWideningOdd(left, right);

        /// <summary>
        /// svuint64_t svpmullt[_u64](svuint32_t op1, svuint32_t op2)
        ///   PMULLT Zresult.D, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<ulong> PolynomialMultiplyWideningOdd(Vector<uint> left, Vector<uint> right) => PolynomialMultiplyWideningOdd(left, right);


        // Saturating doubling multiply-add long (bottom)

        /// <summary>
        /// svint16_t svqdmlalb[_s16](svint16_t op1, svint8_t op2, svint8_t op3)
        ///   SQDMLALB Ztied1.H, Zop2.B, Zop3.B
        /// </summary>
        public static Vector<short> MultiplyDoublingWideningAndAddSaturateEven(Vector<short> addend, Vector<sbyte> left, Vector<sbyte> right) => MultiplyDoublingWideningAndAddSaturateEven(addend, left, right);

        /// <summary>
        /// svint32_t svqdmlalb[_s32](svint32_t op1, svint16_t op2, svint16_t op3)
        ///   SQDMLALB Ztied1.S, Zop2.H, Zop3.H
        /// </summary>
        public static Vector<int> MultiplyDoublingWideningAndAddSaturateEven(Vector<int> addend, Vector<short> left, Vector<short> right) => MultiplyDoublingWideningAndAddSaturateEven(addend, left, right);

        /// <summary>
        /// svint64_t svqdmlalb[_s64](svint64_t op1, svint32_t op2, svint32_t op3)
        ///   SQDMLALB Ztied1.D, Zop2.S, Zop3.S
        /// </summary>
        public static Vector<long> MultiplyDoublingWideningAndAddSaturateEven(Vector<long> addend, Vector<int> left, Vector<int> right) => MultiplyDoublingWideningAndAddSaturateEven(addend, left, right);


        // Saturating doubling multiply-add long (bottom × top)

        /// <summary>
        /// svint16_t svqdmlalbt[_s16](svint16_t op1, svint8_t op2, svint8_t op3)
        ///   SQDMLALBT Ztied1.H, Zop2.B, Zop3.B
        /// </summary>
        public static Vector<short> MultiplyDoublingWideningAndAddSaturateEvenOdd(Vector<short> addend, Vector<sbyte> leftEven, Vector<sbyte> rightOdd) => MultiplyDoublingWideningAndAddSaturateEvenOdd(addend, leftEven, rightOdd);

        /// <summary>
        /// svint32_t svqdmlalbt[_s32](svint32_t op1, svint16_t op2, svint16_t op3)
        ///   SQDMLALBT Ztied1.S, Zop2.H, Zop3.H
        /// </summary>
        public static Vector<int> MultiplyDoublingWideningAndAddSaturateEvenOdd(Vector<int> addend, Vector<short> leftEven, Vector<short> rightOdd) => MultiplyDoublingWideningAndAddSaturateEvenOdd(addend, leftEven, rightOdd);

        /// <summary>
        /// svint64_t svqdmlalbt[_s64](svint64_t op1, svint32_t op2, svint32_t op3)
        ///   SQDMLALBT Ztied1.D, Zop2.S, Zop3.S
        /// </summary>
        public static Vector<long> MultiplyDoublingWideningAndAddSaturateEvenOdd(Vector<long> addend, Vector<int> leftEven, Vector<int> rightOdd) => MultiplyDoublingWideningAndAddSaturateEvenOdd(addend, leftEven, rightOdd);


        // Saturating doubling multiply-add long (top)

        /// <summary>
        /// svint16_t svqdmlalt[_s16](svint16_t op1, svint8_t op2, svint8_t op3)
        ///   SQDMLALT Ztied1.H, Zop2.B, Zop3.B
        /// </summary>
        public static Vector<short> MultiplyDoublingWideningAndAddSaturateOdd(Vector<short> addend, Vector<sbyte> left, Vector<sbyte> right) => MultiplyDoublingWideningAndAddSaturateOdd(addend, left, right);

        /// <summary>
        /// svint32_t svqdmlalt[_s32](svint32_t op1, svint16_t op2, svint16_t op3)
        ///   SQDMLALT Ztied1.S, Zop2.H, Zop3.H
        /// </summary>
        public static Vector<int> MultiplyDoublingWideningAndAddSaturateOdd(Vector<int> addend, Vector<short> left, Vector<short> right) => MultiplyDoublingWideningAndAddSaturateOdd(addend, left, right);

        /// <summary>
        /// svint64_t svqdmlalt[_s64](svint64_t op1, svint32_t op2, svint32_t op3)
        ///   SQDMLALT Ztied1.D, Zop2.S, Zop3.S
        /// </summary>
        public static Vector<long> MultiplyDoublingWideningAndAddSaturateOdd(Vector<long> addend, Vector<int> left, Vector<int> right) => MultiplyDoublingWideningAndAddSaturateOdd(addend, left, right);


        // Saturating doubling multiply-subtract long (bottom)

        /// <summary>
        /// svint16_t svqdmlslb[_s16](svint16_t op1, svint8_t op2, svint8_t op3)
        ///   SQDMLSLB Ztied1.H, Zop2.B, Zop3.B
        /// </summary>
        public static Vector<short> MultiplyDoublingWideningAndSubtractSaturateEven(Vector<short> minuend, Vector<sbyte> left, Vector<sbyte> right) => MultiplyDoublingWideningAndSubtractSaturateEven(minuend, left, right);

        /// <summary>
        /// svint32_t svqdmlslb[_s32](svint32_t op1, svint16_t op2, svint16_t op3)
        ///   SQDMLSLB Ztied1.S, Zop2.H, Zop3.H
        /// </summary>
        public static Vector<int> MultiplyDoublingWideningAndSubtractSaturateEven(Vector<int> minuend, Vector<short> left, Vector<short> right) => MultiplyDoublingWideningAndSubtractSaturateEven(minuend, left, right);

        /// <summary>
        /// svint64_t svqdmlslb[_s64](svint64_t op1, svint32_t op2, svint32_t op3)
        ///   SQDMLSLB Ztied1.D, Zop2.S, Zop3.S
        /// </summary>
        public static Vector<long> MultiplyDoublingWideningAndSubtractSaturateEven(Vector<long> minuend, Vector<int> left, Vector<int> right) => MultiplyDoublingWideningAndSubtractSaturateEven(minuend, left, right);


        // Saturating doubling multiply-subtract long (bottom × top)

        /// <summary>
        /// svint16_t svqdmlslbt[_s16](svint16_t op1, svint8_t op2, svint8_t op3)
        ///   SQDMLSLBT Ztied1.H, Zop2.B, Zop3.B
        /// </summary>
        public static Vector<short> MultiplyDoublingWideningAndSubtractSaturateEvenOdd(Vector<short> minuend, Vector<sbyte> leftEven, Vector<sbyte> rightOdd) => MultiplyDoublingWideningAndSubtractSaturateEvenOdd(minuend, leftEven, rightOdd);

        /// <summary>
        /// svint32_t svqdmlslbt[_s32](svint32_t op1, svint16_t op2, svint16_t op3)
        ///   SQDMLSLBT Ztied1.S, Zop2.H, Zop3.H
        /// </summary>
        public static Vector<int> MultiplyDoublingWideningAndSubtractSaturateEvenOdd(Vector<int> minuend, Vector<short> leftEven, Vector<short> rightOdd) => MultiplyDoublingWideningAndSubtractSaturateEvenOdd(minuend, leftEven, rightOdd);

        /// <summary>
        /// svint64_t svqdmlslbt[_s64](svint64_t op1, svint32_t op2, svint32_t op3)
        ///   SQDMLSLBT Ztied1.D, Zop2.S, Zop3.S
        /// </summary>
        public static Vector<long> MultiplyDoublingWideningAndSubtractSaturateEvenOdd(Vector<long> minuend, Vector<int> leftEven, Vector<int> rightOdd) => MultiplyDoublingWideningAndSubtractSaturateEvenOdd(minuend, leftEven, rightOdd);


        // Saturating doubling multiply-subtract long (top)

        /// <summary>
        /// svint16_t svqdmlslt[_s16](svint16_t op1, svint8_t op2, svint8_t op3)
        ///   SQDMLSLT Ztied1.H, Zop2.B, Zop3.B
        /// </summary>
        public static Vector<short> MultiplyDoublingWideningAndSubtractSaturateOdd(Vector<short> minuend, Vector<sbyte> left, Vector<sbyte> right) => MultiplyDoublingWideningAndSubtractSaturateOdd(minuend, left, right);

        /// <summary>
        /// svint32_t svqdmlslt[_s32](svint32_t op1, svint16_t op2, svint16_t op3)
        ///   SQDMLSLT Ztied1.S, Zop2.H, Zop3.H
        /// </summary>
        public static Vector<int> MultiplyDoublingWideningAndSubtractSaturateOdd(Vector<int> minuend, Vector<short> left, Vector<short> right) => MultiplyDoublingWideningAndSubtractSaturateOdd(minuend, left, right);

        /// <summary>
        /// svint64_t svqdmlslt[_s64](svint64_t op1, svint32_t op2, svint32_t op3)
        ///   SQDMLSLT Ztied1.D, Zop2.S, Zop3.S
        /// </summary>
        public static Vector<long> MultiplyDoublingWideningAndSubtractSaturateOdd(Vector<long> minuend, Vector<int> left, Vector<int> right) => MultiplyDoublingWideningAndSubtractSaturateOdd(minuend, left, right);


        // Saturating doubling multiply-add long with index (bottom)

        /// <summary>
        /// svint32_t svqdmlalb_lane[_s32](svint32_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index)
        ///   SQDMLALB Ztied1.S, Zop2.H, Zop3.H[imm_index]
        /// </summary>
        public static Vector<int> MultiplyDoublingWideningBySelectedScalarAndAddSaturateEven(Vector<int> addend, Vector<short> left, Vector<short> right, [ConstantExpected] byte rightIndex) => MultiplyDoublingWideningBySelectedScalarAndAddSaturateEven(addend, left, right, rightIndex);

        /// <summary>
        /// svint64_t svqdmlalb_lane[_s64](svint64_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index)
        ///   SQDMLALB Ztied1.D, Zop2.S, Zop3.S[imm_index]
        /// </summary>
        public static Vector<long> MultiplyDoublingWideningBySelectedScalarAndAddSaturateEven(Vector<long> addend, Vector<int> left, Vector<int> right, [ConstantExpected] byte rightIndex) => MultiplyDoublingWideningBySelectedScalarAndAddSaturateEven(addend, left, right, rightIndex);


        // Saturating doubling multiply-add long with index (top)

        /// <summary>
        /// svint32_t svqdmlalt_lane[_s32](svint32_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index)
        ///   SQDMLALT Ztied1.S, Zop2.H, Zop3.H[imm_index]
        /// </summary>
        public static Vector<int> MultiplyDoublingWideningBySelectedScalarAndAddSaturateOdd(Vector<int> addend, Vector<short> left, Vector<short> right, [ConstantExpected] byte rightIndex) => MultiplyDoublingWideningBySelectedScalarAndAddSaturateOdd(addend, left, right, rightIndex);

        /// <summary>
        /// svint64_t svqdmlalt_lane[_s64](svint64_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index)
        ///   SQDMLALT Ztied1.D, Zop2.S, Zop3.S[imm_index]
        /// </summary>
        public static Vector<long> MultiplyDoublingWideningBySelectedScalarAndAddSaturateOdd(Vector<long> addend, Vector<int> left, Vector<int> right, [ConstantExpected] byte rightIndex) => MultiplyDoublingWideningBySelectedScalarAndAddSaturateOdd(addend, left, right, rightIndex);


        // Saturating doubling multiply-subtract long with index (bottom)

        /// <summary>
        /// svint32_t svqdmlslb_lane[_s32](svint32_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index)
        ///   SQDMLSLB Ztied1.S, Zop2.H, Zop3.H[imm_index]
        /// </summary>
        public static Vector<int> MultiplyDoublingWideningBySelectedScalarAndSubtractSaturateEven(Vector<int> minuend, Vector<short> left, Vector<short> right, [ConstantExpected] byte rightIndex) => MultiplyDoublingWideningBySelectedScalarAndSubtractSaturateEven(minuend, left, right, rightIndex);

        /// <summary>
        /// svint64_t svqdmlslb_lane[_s64](svint64_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index)
        ///   SQDMLSLB Ztied1.D, Zop2.S, Zop3.S[imm_index]
        /// </summary>
        public static Vector<long> MultiplyDoublingWideningBySelectedScalarAndSubtractSaturateEven(Vector<long> minuend, Vector<int> left, Vector<int> right, [ConstantExpected] byte rightIndex) => MultiplyDoublingWideningBySelectedScalarAndSubtractSaturateEven(minuend, left, right, rightIndex);


        // Saturating doubling multiply-subtract long (top)

        /// <summary>
        /// svint32_t svqdmlslt_lane[_s32](svint32_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index)
        ///   SQDMLSLT Ztied1.S, Zop2.H, Zop3.H[imm_index]
        /// </summary>
        public static Vector<int> MultiplyDoublingWideningBySelectedScalarAndSubtractSaturateOdd(Vector<int> minuend, Vector<short> left, Vector<short> right, [ConstantExpected] byte rightIndex) => MultiplyDoublingWideningBySelectedScalarAndSubtractSaturateOdd(minuend, left, right, rightIndex);

        /// <summary>
        /// svint64_t svqdmlslt_lane[_s64](svint64_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index)
        ///   SQDMLSLT Ztied1.D, Zop2.S, Zop3.S[imm_index]
        /// </summary>
        public static Vector<long> MultiplyDoublingWideningBySelectedScalarAndSubtractSaturateOdd(Vector<long> minuend, Vector<int> left, Vector<int> right, [ConstantExpected] byte rightIndex) => MultiplyDoublingWideningBySelectedScalarAndSubtractSaturateOdd(minuend, left, right, rightIndex);


        // Saturating negate

        /// <summary>
        /// svint8_t svqneg[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op)
        /// svint8_t svqneg[_s8]_x(svbool_t pg, svint8_t op)
        /// svint8_t svqneg[_s8]_z(svbool_t pg, svint8_t op)
        ///   SQNEG Ztied.B, Pg/M, Zop.B
        ///   SQNEG Ztied.B, Pg/M, Ztied.B
        /// </summary>
        public static Vector<sbyte> NegateSaturate(Vector<sbyte> value) => NegateSaturate(value);

        /// <summary>
        /// svint16_t svqneg[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        /// svint16_t svqneg[_s16]_x(svbool_t pg, svint16_t op)
        /// svint16_t svqneg[_s16]_z(svbool_t pg, svint16_t op)
        ///   SQNEG Ztied.H, Pg/M, Zop.H
        ///   SQNEG Ztied.H, Pg/M, Ztied.H
        /// </summary>
        public static Vector<short> NegateSaturate(Vector<short> value) => NegateSaturate(value);

        /// <summary>
        /// svint32_t svqneg[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        /// svint32_t svqneg[_s32]_x(svbool_t pg, svint32_t op)
        /// svint32_t svqneg[_s32]_z(svbool_t pg, svint32_t op)
        ///   SQNEG Ztied.S, Pg/M, Zop.S
        ///   SQNEG Ztied.S, Pg/M, Ztied.S
        /// </summary>
        public static Vector<int> NegateSaturate(Vector<int> value) => NegateSaturate(value);

        /// <summary>
        /// svint64_t svqneg[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        /// svint64_t svqneg[_s64]_x(svbool_t pg, svint64_t op)
        /// svint64_t svqneg[_s64]_z(svbool_t pg, svint64_t op)
        ///   SQNEG Ztied.D, Pg/M, Zop.D
        ///   SQNEG Ztied.D, Pg/M, Ztied.D
        /// </summary>
        public static Vector<long> NegateSaturate(Vector<long> value) => NegateSaturate(value);


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


        // Subtract with borrow long (bottom)

        /// <summary>
        /// svuint32_t svsbclb[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3)
        ///   SBCLB Ztied1.S, Zop2.S, Zop3.S
        /// </summary>
        public static Vector<uint> SubtractBorrowWideningEven(Vector<uint> op1, Vector<uint> op2, Vector<uint> op3) => SubtractBorrowWideningEven(op1, op2, op3);

        /// <summary>
        /// svuint64_t svsbclb[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3)
        ///   SBCLB Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<ulong> SubtractBorrowWideningEven(Vector<ulong> op1, Vector<ulong> op2, Vector<ulong> op3) => SubtractBorrowWideningEven(op1, op2, op3);


        // Subtract with borrow long (top)

        /// <summary>
        /// svuint32_t svsbclt[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3)
        ///   SBCLT Ztied1.S, Zop2.S, Zop3.S
        /// </summary>
        public static Vector<uint> SubtractBorrowWideningOdd(Vector<uint> op1, Vector<uint> op2, Vector<uint> op3) => SubtractBorrowWideningOdd(op1, op2, op3);

        /// <summary>
        /// svuint64_t svsbclt[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3)
        ///   SBCLT Ztied1.D, Zop2.D, Zop3.D
        /// </summary>
        public static Vector<ulong> SubtractBorrowWideningOdd(Vector<ulong> op1, Vector<ulong> op2, Vector<ulong> op3) => SubtractBorrowWideningOdd(op1, op2, op3);


        // Subtract narrow high part (bottom)

        /// <summary>
        /// svuint8_t svsubhnb[_u16](svuint16_t op1, svuint16_t op2)
        ///   SUBHNB Zresult.B, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<byte> SubtractHighNarrowingEven(Vector<ushort> left, Vector<ushort> right) => SubtractHighNarrowingEven(left, right);

        /// <summary>
        /// svint16_t svsubhnb[_s32](svint32_t op1, svint32_t op2)
        ///   SUBHNB Zresult.H, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<short> SubtractHighNarrowingEven(Vector<int> left, Vector<int> right) => SubtractHighNarrowingEven(left, right);

        /// <summary>
        /// svint32_t svsubhnb[_s64](svint64_t op1, svint64_t op2)
        ///   SUBHNB Zresult.S, Zop1.D, Zop2.D
        /// </summary>
        public static Vector<int> SubtractHighNarrowingEven(Vector<long> left, Vector<long> right) => SubtractHighNarrowingEven(left, right);

        /// <summary>
        /// svint8_t svsubhnb[_s16](svint16_t op1, svint16_t op2)
        ///   SUBHNB Zresult.B, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<sbyte> SubtractHighNarrowingEven(Vector<short> left, Vector<short> right) => SubtractHighNarrowingEven(left, right);

        /// <summary>
        /// svuint16_t svsubhnb[_u32](svuint32_t op1, svuint32_t op2)
        ///   SUBHNB Zresult.H, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<ushort> SubtractHighNarrowingEven(Vector<uint> left, Vector<uint> right) => SubtractHighNarrowingEven(left, right);

        /// <summary>
        /// svuint32_t svsubhnb[_u64](svuint64_t op1, svuint64_t op2)
        ///   SUBHNB Zresult.S, Zop1.D, Zop2.D
        /// </summary>
        public static Vector<uint> SubtractHighNarrowingEven(Vector<ulong> left, Vector<ulong> right) => SubtractHighNarrowingEven(left, right);


        // Subtract narrow high part (top)

        /// <summary>
        /// svuint8_t svsubhnt[_u16](svuint8_t even, svuint16_t op1, svuint16_t op2)
        ///   SUBHNT Ztied.B, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<byte> SubtractHighNarrowingOdd(Vector<byte> even, Vector<ushort> left, Vector<ushort> right) => SubtractHighNarrowingOdd(even, left, right);

        /// <summary>
        /// svint16_t svsubhnt[_s32](svint16_t even, svint32_t op1, svint32_t op2)
        ///   SUBHNT Ztied.H, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<short> SubtractHighNarrowingOdd(Vector<short> even, Vector<int> left, Vector<int> right) => SubtractHighNarrowingOdd(even, left, right);

        /// <summary>
        /// svint32_t svsubhnt[_s64](svint32_t even, svint64_t op1, svint64_t op2)
        ///   SUBHNT Ztied.S, Zop1.D, Zop2.D
        /// </summary>
        public static Vector<int> SubtractHighNarrowingOdd(Vector<int> even, Vector<long> left, Vector<long> right) => SubtractHighNarrowingOdd(even, left, right);

        /// <summary>
        /// svint8_t svsubhnt[_s16](svint8_t even, svint16_t op1, svint16_t op2)
        ///   SUBHNT Ztied.B, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<sbyte> SubtractHighNarrowingOdd(Vector<sbyte> even, Vector<short> left, Vector<short> right) => SubtractHighNarrowingOdd(even, left, right);

        /// <summary>
        /// svuint16_t svsubhnt[_u32](svuint16_t even, svuint32_t op1, svuint32_t op2)
        ///   SUBHNT Ztied.H, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<ushort> SubtractHighNarrowingOdd(Vector<ushort> even, Vector<uint> left, Vector<uint> right) => SubtractHighNarrowingOdd(even, left, right);

        /// <summary>
        /// svuint32_t svsubhnt[_u64](svuint32_t even, svuint64_t op1, svuint64_t op2)
        ///   SUBHNT Ztied.S, Zop1.D, Zop2.D
        /// </summary>
        public static Vector<uint> SubtractHighNarrowingOdd(Vector<uint> even, Vector<ulong> left, Vector<ulong> right) => SubtractHighNarrowingOdd(even, left, right);


        // Rounding subtract narrow high part (bottom)

        /// <summary>
        /// svuint8_t svrsubhnb[_u16](svuint16_t op1, svuint16_t op2)
        ///   RSUBHNB Zresult.B, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<byte> SubtractRoundedHighNarrowingEven(Vector<ushort> left, Vector<ushort> right) => SubtractRoundedHighNarrowingEven(left, right);

        /// <summary>
        /// svint16_t svrsubhnb[_s32](svint32_t op1, svint32_t op2)
        ///   RSUBHNB Zresult.H, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<short> SubtractRoundedHighNarrowingEven(Vector<int> left, Vector<int> right) => SubtractRoundedHighNarrowingEven(left, right);

        /// <summary>
        /// svint32_t svrsubhnb[_s64](svint64_t op1, svint64_t op2)
        ///   RSUBHNB Zresult.S, Zop1.D, Zop2.D
        /// </summary>
        public static Vector<int> SubtractRoundedHighNarrowingEven(Vector<long> left, Vector<long> right) => SubtractRoundedHighNarrowingEven(left, right);

        /// <summary>
        /// svint8_t svrsubhnb[_s16](svint16_t op1, svint16_t op2)
        ///   RSUBHNB Zresult.B, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<sbyte> SubtractRoundedHighNarrowingEven(Vector<short> left, Vector<short> right) => SubtractRoundedHighNarrowingEven(left, right);

        /// <summary>
        /// svuint16_t svrsubhnb[_u32](svuint32_t op1, svuint32_t op2)
        ///   RSUBHNB Zresult.H, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<ushort> SubtractRoundedHighNarrowingEven(Vector<uint> left, Vector<uint> right) => SubtractRoundedHighNarrowingEven(left, right);

        /// <summary>
        /// svuint32_t svrsubhnb[_u64](svuint64_t op1, svuint64_t op2)
        ///   RSUBHNB Zresult.S, Zop1.D, Zop2.D
        /// </summary>
        public static Vector<uint> SubtractRoundedHighNarrowingEven(Vector<ulong> left, Vector<ulong> right) => SubtractRoundedHighNarrowingEven(left, right);


        // Rounding subtract narrow high part (top)

        /// <summary>
        /// svuint8_t svrsubhnt[_u16](svuint8_t even, svuint16_t op1, svuint16_t op2)
        ///   RSUBHNT Ztied.B, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<byte> SubtractRoundedHighNarrowingOdd(Vector<byte> even, Vector<ushort> left, Vector<ushort> right) => SubtractRoundedHighNarrowingOdd(even, left, right);

        /// <summary>
        /// svint16_t svrsubhnt[_s32](svint16_t even, svint32_t op1, svint32_t op2)
        ///   RSUBHNT Ztied.H, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<short> SubtractRoundedHighNarrowingOdd(Vector<short> even, Vector<int> left, Vector<int> right) => SubtractRoundedHighNarrowingOdd(even, left, right);

        /// <summary>
        /// svint32_t svrsubhnt[_s64](svint32_t even, svint64_t op1, svint64_t op2)
        ///   RSUBHNT Ztied.S, Zop1.D, Zop2.D
        /// </summary>
        public static Vector<int> SubtractRoundedHighNarrowingOdd(Vector<int> even, Vector<long> left, Vector<long> right) => SubtractRoundedHighNarrowingOdd(even, left, right);

        /// <summary>
        /// svint8_t svrsubhnt[_s16](svint8_t even, svint16_t op1, svint16_t op2)
        ///   RSUBHNT Ztied.B, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<sbyte> SubtractRoundedHighNarrowingOdd(Vector<sbyte> even, Vector<short> left, Vector<short> right) => SubtractRoundedHighNarrowingOdd(even, left, right);

        /// <summary>
        /// svuint16_t svrsubhnt[_u32](svuint16_t even, svuint32_t op1, svuint32_t op2)
        ///   RSUBHNT Ztied.H, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<ushort> SubtractRoundedHighNarrowingOdd(Vector<ushort> even, Vector<uint> left, Vector<uint> right) => SubtractRoundedHighNarrowingOdd(even, left, right);

        /// <summary>
        /// svuint32_t svrsubhnt[_u64](svuint32_t even, svuint64_t op1, svuint64_t op2)
        ///   RSUBHNT Ztied.S, Zop1.D, Zop2.D
        /// </summary>
        public static Vector<uint> SubtractRoundedHighNarrowingOdd(Vector<uint> even, Vector<ulong> left, Vector<ulong> right) => SubtractRoundedHighNarrowingOdd(even, left, right);


        // Saturating subtract

        /// <summary>
        /// svuint8_t svqsub[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svqsub[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svqsub[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   UQSUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   UQSUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   UQSUBR Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   UQSUB Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static new Vector<byte> SubtractSaturate(Vector<byte> left, Vector<byte> right) => SubtractSaturate(left, right);

        /// <summary>
        /// svint16_t svqsub[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svqsub[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svqsub[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SQSUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   SQSUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   SQSUBR Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   SQSUB Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static new Vector<short> SubtractSaturate(Vector<short> left, Vector<short> right) => SubtractSaturate(left, right);

        /// <summary>
        /// svint32_t svqsub[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svqsub[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svqsub[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SQSUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   SQSUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   SQSUBR Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   SQSUB Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static new Vector<int> SubtractSaturate(Vector<int> left, Vector<int> right) => SubtractSaturate(left, right);

        /// <summary>
        /// svint64_t svqsub[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svqsub[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svqsub[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SQSUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   SQSUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   SQSUBR Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   SQSUB Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static new Vector<long> SubtractSaturate(Vector<long> left, Vector<long> right) => SubtractSaturate(left, right);

        /// <summary>
        /// svint8_t svqsub[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svqsub[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svqsub[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SQSUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   SQSUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   SQSUBR Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   SQSUB Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static new Vector<sbyte> SubtractSaturate(Vector<sbyte> left, Vector<sbyte> right) => SubtractSaturate(left, right);

        /// <summary>
        /// svuint16_t svqsub[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svqsub[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svqsub[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   UQSUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   UQSUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   UQSUBR Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   UQSUB Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static new Vector<ushort> SubtractSaturate(Vector<ushort> left, Vector<ushort> right) => SubtractSaturate(left, right);

        /// <summary>
        /// svuint32_t svqsub[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svqsub[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svqsub[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   UQSUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   UQSUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   UQSUBR Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   UQSUB Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static new Vector<uint> SubtractSaturate(Vector<uint> left, Vector<uint> right) => SubtractSaturate(left, right);

        /// <summary>
        /// svuint64_t svqsub[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svqsub[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svqsub[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   UQSUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   UQSUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   UQSUBR Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   UQSUB Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static new Vector<ulong> SubtractSaturate(Vector<ulong> left, Vector<ulong> right) => SubtractSaturate(left, right);


        // Subtract wide (bottom)

        /// <summary>
        /// svint16_t svsubwb[_s16](svint16_t op1, svint8_t op2)
        ///   SSUBWB Zresult.H, Zop1.H, Zop2.B
        /// </summary>
        public static Vector<short> SubtractWideningEven(Vector<short> left, Vector<sbyte> right) => SubtractWideningEven(left, right);

        /// <summary>
        /// svint32_t svsubwb[_s32](svint32_t op1, svint16_t op2)
        ///   SSUBWB Zresult.S, Zop1.S, Zop2.H
        /// </summary>
        public static Vector<int> SubtractWideningEven(Vector<int> left, Vector<short> right) => SubtractWideningEven(left, right);

        /// <summary>
        /// svint64_t svsubwb[_s64](svint64_t op1, svint32_t op2)
        ///   SSUBWB Zresult.D, Zop1.D, Zop2.S
        /// </summary>
        public static Vector<long> SubtractWideningEven(Vector<long> left, Vector<int> right) => SubtractWideningEven(left, right);

        /// <summary>
        /// svuint16_t svsubwb[_u16](svuint16_t op1, svuint8_t op2)
        ///   USUBWB Zresult.H, Zop1.H, Zop2.B
        /// </summary>
        public static Vector<ushort> SubtractWideningEven(Vector<ushort> left, Vector<byte> right) => SubtractWideningEven(left, right);

        /// <summary>
        /// svuint32_t svsubwb[_u32](svuint32_t op1, svuint16_t op2)
        ///   USUBWB Zresult.S, Zop1.S, Zop2.H
        /// </summary>
        public static Vector<uint> SubtractWideningEven(Vector<uint> left, Vector<ushort> right) => SubtractWideningEven(left, right);

        /// <summary>
        /// svuint64_t svsubwb[_u64](svuint64_t op1, svuint32_t op2)
        ///   USUBWB Zresult.D, Zop1.D, Zop2.S
        /// </summary>
        public static Vector<ulong> SubtractWideningEven(Vector<ulong> left, Vector<uint> right) => SubtractWideningEven(left, right);


        // Subtract long (bottom)

        /// <summary>
        /// svint16_t svsublb[_s16](svint8_t op1, svint8_t op2)
        ///   SSUBLB Zresult.H, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<short> SubtractWideningEven(Vector<sbyte> left, Vector<sbyte> right) => SubtractWideningEven(left, right);

        /// <summary>
        /// svint32_t svsublb[_s32](svint16_t op1, svint16_t op2)
        ///   SSUBLB Zresult.S, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<int> SubtractWideningEven(Vector<short> left, Vector<short> right) => SubtractWideningEven(left, right);

        /// <summary>
        /// svint64_t svsublb[_s64](svint32_t op1, svint32_t op2)
        ///   SSUBLB Zresult.D, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<long> SubtractWideningEven(Vector<int> left, Vector<int> right) => SubtractWideningEven(left, right);

        /// <summary>
        /// svuint16_t svsublb[_u16](svuint8_t op1, svuint8_t op2)
        ///   USUBLB Zresult.H, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<ushort> SubtractWideningEven(Vector<byte> left, Vector<byte> right) => SubtractWideningEven(left, right);

        /// <summary>
        /// svuint32_t svsublb[_u32](svuint16_t op1, svuint16_t op2)
        ///   USUBLB Zresult.S, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<uint> SubtractWideningEven(Vector<ushort> left, Vector<ushort> right) => SubtractWideningEven(left, right);

        /// <summary>
        /// svuint64_t svsublb[_u64](svuint32_t op1, svuint32_t op2)
        ///   USUBLB Zresult.D, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<ulong> SubtractWideningEven(Vector<uint> left, Vector<uint> right) => SubtractWideningEven(left, right);


        // Subtract long (bottom - top)

        /// <summary>
        /// svint16_t svsublbt[_s16](svint8_t op1, svint8_t op2)
        ///   SSUBLBT Zresult.H, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<short> SubtractWideningEvenOdd(Vector<sbyte> left, Vector<sbyte> right) => SubtractWideningEvenOdd(left, right);

        /// <summary>
        /// svint32_t svsublbt[_s32](svint16_t op1, svint16_t op2)
        ///   SSUBLBT Zresult.S, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<int> SubtractWideningEvenOdd(Vector<short> left, Vector<short> right) => SubtractWideningEvenOdd(left, right);

        /// <summary>
        /// svint64_t svsublbt[_s64](svint32_t op1, svint32_t op2)
        ///   SSUBLBT Zresult.D, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<long> SubtractWideningEvenOdd(Vector<int> left, Vector<int> right) => SubtractWideningEvenOdd(left, right);


        // Subtract wide (top)

        /// <summary>
        /// svint16_t svsubwt[_s16](svint16_t op1, svint8_t op2)
        ///   SSUBWT Zresult.H, Zop1.H, Zop2.B
        /// </summary>
        public static Vector<short> SubtractWideningOdd(Vector<short> left, Vector<sbyte> right) => SubtractWideningOdd(left, right);

        /// <summary>
        /// svint32_t svsubwt[_s32](svint32_t op1, svint16_t op2)
        ///   SSUBWT Zresult.S, Zop1.S, Zop2.H
        /// </summary>
        public static Vector<int> SubtractWideningOdd(Vector<int> left, Vector<short> right) => SubtractWideningOdd(left, right);

        /// <summary>
        /// svint64_t svsubwt[_s64](svint64_t op1, svint32_t op2)
        ///   SSUBWT Zresult.D, Zop1.D, Zop2.S
        /// </summary>
        public static Vector<long> SubtractWideningOdd(Vector<long> left, Vector<int> right) => SubtractWideningOdd(left, right);

        /// <summary>
        /// svuint16_t svsubwt[_u16](svuint16_t op1, svuint8_t op2)
        ///   USUBWT Zresult.H, Zop1.H, Zop2.B
        /// </summary>
        public static Vector<ushort> SubtractWideningOdd(Vector<ushort> left, Vector<byte> right) => SubtractWideningOdd(left, right);

        /// <summary>
        /// svuint32_t svsubwt[_u32](svuint32_t op1, svuint16_t op2)
        ///   USUBWT Zresult.S, Zop1.S, Zop2.H
        /// </summary>
        public static Vector<uint> SubtractWideningOdd(Vector<uint> left, Vector<ushort> right) => SubtractWideningOdd(left, right);

        /// <summary>
        /// svuint64_t svsubwt[_u64](svuint64_t op1, svuint32_t op2)
        ///   USUBWT Zresult.D, Zop1.D, Zop2.S
        /// </summary>
        public static Vector<ulong> SubtractWideningOdd(Vector<ulong> left, Vector<uint> right) => SubtractWideningOdd(left, right);


        // Subtract long (top)

        /// <summary>
        /// svint16_t svsublt[_s16](svint8_t op1, svint8_t op2)
        ///   SSUBLT Zresult.H, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<short> SubtractWideningOdd(Vector<sbyte> left, Vector<sbyte> right) => SubtractWideningOdd(left, right);

        /// <summary>
        /// svint32_t svsublt[_s32](svint16_t op1, svint16_t op2)
        ///   SSUBLT Zresult.S, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<int> SubtractWideningOdd(Vector<short> left, Vector<short> right) => SubtractWideningOdd(left, right);

        /// <summary>
        /// svint64_t svsublt[_s64](svint32_t op1, svint32_t op2)
        ///   SSUBLT Zresult.D, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<long> SubtractWideningOdd(Vector<int> left, Vector<int> right) => SubtractWideningOdd(left, right);

        /// <summary>
        /// svuint16_t svsublt[_u16](svuint8_t op1, svuint8_t op2)
        ///   USUBLT Zresult.H, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<ushort> SubtractWideningOdd(Vector<byte> left, Vector<byte> right) => SubtractWideningOdd(left, right);

        /// <summary>
        /// svuint32_t svsublt[_u32](svuint16_t op1, svuint16_t op2)
        ///   USUBLT Zresult.S, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<uint> SubtractWideningOdd(Vector<ushort> left, Vector<ushort> right) => SubtractWideningOdd(left, right);

        /// <summary>
        /// svuint64_t svsublt[_u64](svuint32_t op1, svuint32_t op2)
        ///   USUBLT Zresult.D, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<ulong> SubtractWideningOdd(Vector<uint> left, Vector<uint> right) => SubtractWideningOdd(left, right);


        // Subtract long (top - bottom)

        /// <summary>
        /// svint16_t svsubltb[_s16](svint8_t op1, svint8_t op2)
        ///   SSUBLTB Zresult.H, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<short> SubtractWideningOddEven(Vector<sbyte> left, Vector<sbyte> right) => SubtractWideningOddEven(left, right);

        /// <summary>
        /// svint32_t svsubltb[_s32](svint16_t op1, svint16_t op2)
        ///   SSUBLTB Zresult.S, Zop1.H, Zop2.H
        /// </summary>
        public static Vector<int> SubtractWideningOddEven(Vector<short> left, Vector<short> right) => SubtractWideningOddEven(left, right);

        /// <summary>
        /// svint64_t svsubltb[_s64](svint32_t op1, svint32_t op2)
        ///   SSUBLTB Zresult.D, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<long> SubtractWideningOddEven(Vector<int> left, Vector<int> right) => SubtractWideningOddEven(left, right);


        // Bit vector table lookups

        /// <summary>
        /// svuint8_t svtbl2[_u8](svuint8x2_t data, svuint8_t indices)
        ///   TBL Zd.B, { Zn1.B, Zn2.B }, Zm.B
        /// </summary>
        public static unsafe Vector<byte> VectorTableLookup((Vector<byte> data1, Vector<byte> data2) table, Vector<byte> indices) => VectorTableLookup(table, indices);

        /// <summary>
        /// svuint16_t svtbl2[_u16](svuint16x2_t data, svuint16_t indices)
        ///   TBL Zd.H, { Zn1.H, Zn2.H }, Zm.H
        /// </summary>
        public static unsafe Vector<ushort> VectorTableLookup((Vector<ushort> data1, Vector<ushort> data2) table, Vector<ushort> indices) => VectorTableLookup(table, indices);

        /// <summary>
        /// svuint32_t svtbl2[_u32](svuint32x2_t data, svuint32_t indices)
        ///   TBL Zd.S, { Zn1.S, Zn2.S }, Zm.S
        /// </summary>
        public static unsafe Vector<uint> VectorTableLookup((Vector<uint> data1, Vector<uint> data2) table, Vector<uint> indices) => VectorTableLookup(table, indices);

        /// <summary>
        /// svuint64_t svtbl2[_u64](svuint64x2_t data, svuint64_t indices)
        ///   TBL Zd.D, { Zn1.D, Zn2.D }, Zm.D
        /// </summary>
        public static unsafe Vector<ulong> VectorTableLookup((Vector<ulong> data1, Vector<ulong> data2) table, Vector<ulong> indices) => VectorTableLookup(table, indices);

        /// <summary>
        /// svfloat32_t svtbl2[_f32](svfloat32x2_t data, svuint32_t indices)
        ///   TBL Zd.S, { Zn1.S, Zn2.S }, Zm.S
        /// </summary>
        public static unsafe Vector<float> VectorTableLookup((Vector<float> data1, Vector<float> data2) table, Vector<uint> indices) => VectorTableLookup(table, indices);

        /// <summary>
        /// svfloat64_t svtbl2[_f64](svfloat64x2_t data, svuint64_t indices)
        ///   TBL Zd.D, { Zn1.D, Zn2.D }, Zm.D
        /// </summary>
        public static unsafe Vector<double> VectorTableLookup((Vector<double> data1, Vector<double> data2) table, Vector<ulong> indices) => VectorTableLookup(table, indices);

        /// <summary>
        /// svint8_t svtbl2[_s8](svint8x2_t data, svuint8_t indices)
        ///   TBL Zd.B, { Zn1.B, Zn2.B }, Zm.B
        /// </summary>
        public static unsafe Vector<sbyte> VectorTableLookup((Vector<sbyte> data1, Vector<sbyte> data2) table, Vector<byte> indices) => VectorTableLookup(table, indices);

        /// <summary>
        /// svint16_t svtbl2[_s16](svint16x2_t data, svuint16_t indices)
        ///   TBL Zd.H, { Zn1.H, Zn2.H }, Zm.H
        /// </summary>
        public static unsafe Vector<short> VectorTableLookup((Vector<short> data1, Vector<short> data2) table, Vector<ushort> indices) => VectorTableLookup(table, indices);

        /// <summary>
        /// svint32_t svtbl2[_s32](svint32x2_t data, svuint32_t indices)
        ///   TBL Zd.S, { Zn1.S, Zn2.S }, Zm.S
        /// </summary>
        public static unsafe Vector<int> VectorTableLookup((Vector<int> data1, Vector<int> data2) table, Vector<uint> indices) => VectorTableLookup(table, indices);

        /// <summary>
        /// svint64_t svtbl2[_s64](svint64x2_t data, svuint64_t indices)
        ///   TBL Zd.D, { Zn1.D, Zn2.D }, Zm.D
        /// </summary>
        public static unsafe Vector<long> VectorTableLookup((Vector<long> data1, Vector<long> data2) table, Vector<ulong> indices) => VectorTableLookup(table, indices);


        // Bit vector table lookup extensions

        /// <summary>
        /// svuint8_t svtbx[_u8](svuint8_t fallback, svuint8_t data, svuint8_t indices)
        ///   TBX Zd.B, Zn.B, Zm.B
        /// </summary>
        public static unsafe Vector<byte> VectorTableLookupExtension(Vector<byte> defaultValues, Vector<byte> data, Vector<byte> indices) => VectorTableLookupExtension(defaultValues, data, indices);

        /// <summary>
        /// svuint16_t svtbx[_u16](svuint16_t fallback, svuint16_t data, svuint16_t indices)
        ///   TBX Zd.H, Zn.H, Zm.H
        /// </summary>
        public static unsafe Vector<ushort> VectorTableLookupExtension(Vector<ushort> defaultValues, Vector<ushort> data, Vector<ushort> indices) => VectorTableLookupExtension(defaultValues, data, indices);

        /// <summary>
        /// svuint32_t svtbx[_u32](svuint32_t fallback, svuint32_t data, svuint32_t indices)
        ///   TBX Zd.S, Zn.S, Zm.S
        /// </summary>
        public static unsafe Vector<uint> VectorTableLookupExtension(Vector<uint> defaultValues, Vector<uint> data, Vector<uint> indices) => VectorTableLookupExtension(defaultValues, data, indices);

        /// <summary>
        /// svuint64_t svtbx[_u64](svuint64_t fallback, svuint64_t data, svuint64_t indices)
        ///   TBX Zd.D, Zn.D, Zm.D
        /// </summary>
        public static unsafe Vector<ulong> VectorTableLookupExtension(Vector<ulong> defaultValues, Vector<ulong> data, Vector<ulong> indices) => VectorTableLookupExtension(defaultValues, data, indices);

        /// <summary>
        /// svfloat32_t svtbx[_f32](svfloat32_t fallback, svfloat32_t data, svuint32_t indices)
        ///   TBX Zd.S, Zn.S, Zm.S
        /// </summary>
        public static unsafe Vector<float> VectorTableLookupExtension(Vector<float> defaultValues, Vector<float> data, Vector<uint> indices) => VectorTableLookupExtension(defaultValues, data, indices);

        /// <summary>
        /// svfloat64_t svtbx[_f64](svfloat64_t fallback, svfloat64_t data, svuint64_t indices)
        ///   TBX Zd.D, Zn.D, Zm.D
        /// </summary>
        public static unsafe Vector<double> VectorTableLookupExtension(Vector<double> defaultValues, Vector<double> data, Vector<ulong> indices) => VectorTableLookupExtension(defaultValues, data, indices);

        /// <summary>
        /// svint8_t svtbx[_s8](svint8_t fallback, svint8_t data, svuint8_t indices)
        ///   TBX Zd.B, Zn.B, Zm.B
        /// </summary>
        public static unsafe Vector<sbyte> VectorTableLookupExtension(Vector<sbyte> defaultValues, Vector<sbyte> data, Vector<byte> indices) => VectorTableLookupExtension(defaultValues, data, indices);

        /// <summary>
        /// svint16_t svtbx[_s16](svint16_t fallback, svint16_t data, svuint16_t indices)
        ///   TBX Zd.H, Zn.H, Zm.H
        /// </summary>
        public static unsafe Vector<short> VectorTableLookupExtension(Vector<short> defaultValues, Vector<short> data, Vector<ushort> indices) => VectorTableLookupExtension(defaultValues, data, indices);

        /// <summary>
        /// svint32_t svtbx[_s32](svint32_t fallback, svint32_t data, svuint32_t indices)
        ///   TBX Zd.S, Zn.S, Zm.S
        /// </summary>
        public static unsafe Vector<int> VectorTableLookupExtension(Vector<int> defaultValues, Vector<int> data, Vector<uint> indices) => VectorTableLookupExtension(defaultValues, data, indices);

        /// <summary>
        /// svint64_t svtbx[_s64](svint64_t fallback, svint64_t data, svuint64_t indices)
        ///   TBX Zd.D, Zn.D, Zm.D
        /// </summary>
        public static unsafe Vector<long> VectorTableLookupExtension(Vector<long> defaultValues, Vector<long> data, Vector<ulong> indices) => VectorTableLookupExtension(defaultValues, data, indices);


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
