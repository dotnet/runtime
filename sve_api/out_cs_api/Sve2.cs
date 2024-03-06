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
    public abstract class Sve2 : AdvSimd
    {
        internal Sve2() { }

        public static new bool IsSupported { get => IsSupported; }

        [Intrinsic]
        public new abstract class Arm64 : AdvSimd.Arm64
        {
            internal Arm64() { }

            public static new bool IsSupported { get => IsSupported; }
        }

        ///  AbsoluteDifferenceAdd : Absolute difference and accumulate

        /// <summary>
        /// svint8_t svaba[_s8](svint8_t op1, svint8_t op2, svint8_t op3)
        /// </summary>
        public static unsafe Vector<sbyte> AbsoluteDifferenceAdd(Vector<sbyte> addend, Vector<sbyte> left, Vector<sbyte> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// svint16_t svaba[_s16](svint16_t op1, svint16_t op2, svint16_t op3)
        /// </summary>
        public static unsafe Vector<short> AbsoluteDifferenceAdd(Vector<short> addend, Vector<short> left, Vector<short> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// svint32_t svaba[_s32](svint32_t op1, svint32_t op2, svint32_t op3)
        /// </summary>
        public static unsafe Vector<int> AbsoluteDifferenceAdd(Vector<int> addend, Vector<int> left, Vector<int> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// svint64_t svaba[_s64](svint64_t op1, svint64_t op2, svint64_t op3)
        /// </summary>
        public static unsafe Vector<long> AbsoluteDifferenceAdd(Vector<long> addend, Vector<long> left, Vector<long> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// svuint8_t svaba[_u8](svuint8_t op1, svuint8_t op2, svuint8_t op3)
        /// </summary>
        public static unsafe Vector<byte> AbsoluteDifferenceAdd(Vector<byte> addend, Vector<byte> left, Vector<byte> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// svuint16_t svaba[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3)
        /// </summary>
        public static unsafe Vector<ushort> AbsoluteDifferenceAdd(Vector<ushort> addend, Vector<ushort> left, Vector<ushort> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// svuint32_t svaba[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3)
        /// </summary>
        public static unsafe Vector<uint> AbsoluteDifferenceAdd(Vector<uint> addend, Vector<uint> left, Vector<uint> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// svuint64_t svaba[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3)
        /// </summary>
        public static unsafe Vector<ulong> AbsoluteDifferenceAdd(Vector<ulong> addend, Vector<ulong> left, Vector<ulong> right) => AbsoluteDifferenceAdd(addend, left, right);


        ///  AbsoluteDifferenceAddWideningLower : Absolute difference and accumulate long (bottom)

        /// <summary>
        /// svint16_t svabalb[_s16](svint16_t op1, svint8_t op2, svint8_t op3)
        /// </summary>
        public static unsafe Vector<short> AbsoluteDifferenceAddWideningLower(Vector<short> addend, Vector<sbyte> left, Vector<sbyte> right) => AbsoluteDifferenceAddWideningLower(addend, left, right);

        /// <summary>
        /// svint32_t svabalb[_s32](svint32_t op1, svint16_t op2, svint16_t op3)
        /// </summary>
        public static unsafe Vector<int> AbsoluteDifferenceAddWideningLower(Vector<int> addend, Vector<short> left, Vector<short> right) => AbsoluteDifferenceAddWideningLower(addend, left, right);

        /// <summary>
        /// svint64_t svabalb[_s64](svint64_t op1, svint32_t op2, svint32_t op3)
        /// </summary>
        public static unsafe Vector<long> AbsoluteDifferenceAddWideningLower(Vector<long> addend, Vector<int> left, Vector<int> right) => AbsoluteDifferenceAddWideningLower(addend, left, right);

        /// <summary>
        /// svuint16_t svabalb[_u16](svuint16_t op1, svuint8_t op2, svuint8_t op3)
        /// </summary>
        public static unsafe Vector<ushort> AbsoluteDifferenceAddWideningLower(Vector<ushort> addend, Vector<byte> left, Vector<byte> right) => AbsoluteDifferenceAddWideningLower(addend, left, right);

        /// <summary>
        /// svuint32_t svabalb[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3)
        /// </summary>
        public static unsafe Vector<uint> AbsoluteDifferenceAddWideningLower(Vector<uint> addend, Vector<ushort> left, Vector<ushort> right) => AbsoluteDifferenceAddWideningLower(addend, left, right);

        /// <summary>
        /// svuint64_t svabalb[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3)
        /// </summary>
        public static unsafe Vector<ulong> AbsoluteDifferenceAddWideningLower(Vector<ulong> addend, Vector<uint> left, Vector<uint> right) => AbsoluteDifferenceAddWideningLower(addend, left, right);


        ///  AbsoluteDifferenceAddWideningUpper : Absolute difference and accumulate long (top)

        /// <summary>
        /// svint16_t svabalt[_s16](svint16_t op1, svint8_t op2, svint8_t op3)
        /// </summary>
        public static unsafe Vector<short> AbsoluteDifferenceAddWideningUpper(Vector<short> addend, Vector<sbyte> left, Vector<sbyte> right) => AbsoluteDifferenceAddWideningUpper(addend, left, right);

        /// <summary>
        /// svint32_t svabalt[_s32](svint32_t op1, svint16_t op2, svint16_t op3)
        /// </summary>
        public static unsafe Vector<int> AbsoluteDifferenceAddWideningUpper(Vector<int> addend, Vector<short> left, Vector<short> right) => AbsoluteDifferenceAddWideningUpper(addend, left, right);

        /// <summary>
        /// svint64_t svabalt[_s64](svint64_t op1, svint32_t op2, svint32_t op3)
        /// </summary>
        public static unsafe Vector<long> AbsoluteDifferenceAddWideningUpper(Vector<long> addend, Vector<int> left, Vector<int> right) => AbsoluteDifferenceAddWideningUpper(addend, left, right);

        /// <summary>
        /// svuint16_t svabalt[_u16](svuint16_t op1, svuint8_t op2, svuint8_t op3)
        /// </summary>
        public static unsafe Vector<ushort> AbsoluteDifferenceAddWideningUpper(Vector<ushort> addend, Vector<byte> left, Vector<byte> right) => AbsoluteDifferenceAddWideningUpper(addend, left, right);

        /// <summary>
        /// svuint32_t svabalt[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3)
        /// </summary>
        public static unsafe Vector<uint> AbsoluteDifferenceAddWideningUpper(Vector<uint> addend, Vector<ushort> left, Vector<ushort> right) => AbsoluteDifferenceAddWideningUpper(addend, left, right);

        /// <summary>
        /// svuint64_t svabalt[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3)
        /// </summary>
        public static unsafe Vector<ulong> AbsoluteDifferenceAddWideningUpper(Vector<ulong> addend, Vector<uint> left, Vector<uint> right) => AbsoluteDifferenceAddWideningUpper(addend, left, right);


        ///  AbsoluteDifferenceWideningLower : Absolute difference long (bottom)

        /// <summary>
        /// svint16_t svabdlb[_s16](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<short> AbsoluteDifferenceWideningLower(Vector<sbyte> left, Vector<sbyte> right) => AbsoluteDifferenceWideningLower(left, right);

        /// <summary>
        /// svint32_t svabdlb[_s32](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<int> AbsoluteDifferenceWideningLower(Vector<short> left, Vector<short> right) => AbsoluteDifferenceWideningLower(left, right);

        /// <summary>
        /// svint64_t svabdlb[_s64](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<long> AbsoluteDifferenceWideningLower(Vector<int> left, Vector<int> right) => AbsoluteDifferenceWideningLower(left, right);

        /// <summary>
        /// svuint16_t svabdlb[_u16](svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<ushort> AbsoluteDifferenceWideningLower(Vector<byte> left, Vector<byte> right) => AbsoluteDifferenceWideningLower(left, right);

        /// <summary>
        /// svuint32_t svabdlb[_u32](svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<uint> AbsoluteDifferenceWideningLower(Vector<ushort> left, Vector<ushort> right) => AbsoluteDifferenceWideningLower(left, right);

        /// <summary>
        /// svuint64_t svabdlb[_u64](svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<ulong> AbsoluteDifferenceWideningLower(Vector<uint> left, Vector<uint> right) => AbsoluteDifferenceWideningLower(left, right);


        ///  AbsoluteDifferenceWideningUpper : Absolute difference long (top)

        /// <summary>
        /// svint16_t svabdlt[_s16](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<short> AbsoluteDifferenceWideningUpper(Vector<sbyte> left, Vector<sbyte> right) => AbsoluteDifferenceWideningUpper(left, right);

        /// <summary>
        /// svint32_t svabdlt[_s32](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<int> AbsoluteDifferenceWideningUpper(Vector<short> left, Vector<short> right) => AbsoluteDifferenceWideningUpper(left, right);

        /// <summary>
        /// svint64_t svabdlt[_s64](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<long> AbsoluteDifferenceWideningUpper(Vector<int> left, Vector<int> right) => AbsoluteDifferenceWideningUpper(left, right);

        /// <summary>
        /// svuint16_t svabdlt[_u16](svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<ushort> AbsoluteDifferenceWideningUpper(Vector<byte> left, Vector<byte> right) => AbsoluteDifferenceWideningUpper(left, right);

        /// <summary>
        /// svuint32_t svabdlt[_u32](svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<uint> AbsoluteDifferenceWideningUpper(Vector<ushort> left, Vector<ushort> right) => AbsoluteDifferenceWideningUpper(left, right);

        /// <summary>
        /// svuint64_t svabdlt[_u64](svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<ulong> AbsoluteDifferenceWideningUpper(Vector<uint> left, Vector<uint> right) => AbsoluteDifferenceWideningUpper(left, right);


        ///  AddCarryWideningLower : Add with carry long (bottom)

        /// <summary>
        /// svuint32_t svadclb[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3)
        /// </summary>
        public static unsafe Vector<uint> AddCarryWideningLower(Vector<uint> op1, Vector<uint> op2, Vector<uint> op3) => AddCarryWideningLower(op1, op2, op3);

        /// <summary>
        /// svuint64_t svadclb[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3)
        /// </summary>
        public static unsafe Vector<ulong> AddCarryWideningLower(Vector<ulong> op1, Vector<ulong> op2, Vector<ulong> op3) => AddCarryWideningLower(op1, op2, op3);


        ///  AddCarryWideningUpper : Add with carry long (top)

        /// <summary>
        /// svuint32_t svadclt[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3)
        /// </summary>
        public static unsafe Vector<uint> AddCarryWideningUpper(Vector<uint> op1, Vector<uint> op2, Vector<uint> op3) => AddCarryWideningUpper(op1, op2, op3);

        /// <summary>
        /// svuint64_t svadclt[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3)
        /// </summary>
        public static unsafe Vector<ulong> AddCarryWideningUpper(Vector<ulong> op1, Vector<ulong> op2, Vector<ulong> op3) => AddCarryWideningUpper(op1, op2, op3);


        ///  AddHighNarowingLower : Add narrow high part (bottom)

        /// <summary>
        /// svint8_t svaddhnb[_s16](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> AddHighNarowingLower(Vector<short> left, Vector<short> right) => AddHighNarowingLower(left, right);

        /// <summary>
        /// svint16_t svaddhnb[_s32](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<short> AddHighNarowingLower(Vector<int> left, Vector<int> right) => AddHighNarowingLower(left, right);

        /// <summary>
        /// svint32_t svaddhnb[_s64](svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<int> AddHighNarowingLower(Vector<long> left, Vector<long> right) => AddHighNarowingLower(left, right);

        /// <summary>
        /// svuint8_t svaddhnb[_u16](svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<byte> AddHighNarowingLower(Vector<ushort> left, Vector<ushort> right) => AddHighNarowingLower(left, right);

        /// <summary>
        /// svuint16_t svaddhnb[_u32](svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<ushort> AddHighNarowingLower(Vector<uint> left, Vector<uint> right) => AddHighNarowingLower(left, right);

        /// <summary>
        /// svuint32_t svaddhnb[_u64](svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<uint> AddHighNarowingLower(Vector<ulong> left, Vector<ulong> right) => AddHighNarowingLower(left, right);


        ///  AddHighNarowingUpper : Add narrow high part (top)

        /// <summary>
        /// svint8_t svaddhnt[_s16](svint8_t even, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> AddHighNarowingUpper(Vector<sbyte> even, Vector<short> left, Vector<short> right) => AddHighNarowingUpper(even, left, right);

        /// <summary>
        /// svint16_t svaddhnt[_s32](svint16_t even, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<short> AddHighNarowingUpper(Vector<short> even, Vector<int> left, Vector<int> right) => AddHighNarowingUpper(even, left, right);

        /// <summary>
        /// svint32_t svaddhnt[_s64](svint32_t even, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<int> AddHighNarowingUpper(Vector<int> even, Vector<long> left, Vector<long> right) => AddHighNarowingUpper(even, left, right);

        /// <summary>
        /// svuint8_t svaddhnt[_u16](svuint8_t even, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<byte> AddHighNarowingUpper(Vector<byte> even, Vector<ushort> left, Vector<ushort> right) => AddHighNarowingUpper(even, left, right);

        /// <summary>
        /// svuint16_t svaddhnt[_u32](svuint16_t even, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<ushort> AddHighNarowingUpper(Vector<ushort> even, Vector<uint> left, Vector<uint> right) => AddHighNarowingUpper(even, left, right);

        /// <summary>
        /// svuint32_t svaddhnt[_u64](svuint32_t even, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<uint> AddHighNarowingUpper(Vector<uint> even, Vector<ulong> left, Vector<ulong> right) => AddHighNarowingUpper(even, left, right);


        ///  AddPairwise : Add pairwise

        /// <summary>
        /// svint8_t svaddp[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svaddp[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> AddPairwise(Vector<sbyte> left, Vector<sbyte> right) => AddPairwise(left, right);

        /// <summary>
        /// svint16_t svaddp[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svaddp[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> AddPairwise(Vector<short> left, Vector<short> right) => AddPairwise(left, right);

        /// <summary>
        /// svint32_t svaddp[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svaddp[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> AddPairwise(Vector<int> left, Vector<int> right) => AddPairwise(left, right);

        /// <summary>
        /// svint64_t svaddp[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svaddp[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> AddPairwise(Vector<long> left, Vector<long> right) => AddPairwise(left, right);

        /// <summary>
        /// svuint8_t svaddp[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svaddp[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> AddPairwise(Vector<byte> left, Vector<byte> right) => AddPairwise(left, right);

        /// <summary>
        /// svuint16_t svaddp[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svaddp[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> AddPairwise(Vector<ushort> left, Vector<ushort> right) => AddPairwise(left, right);

        /// <summary>
        /// svuint32_t svaddp[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svaddp[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> AddPairwise(Vector<uint> left, Vector<uint> right) => AddPairwise(left, right);

        /// <summary>
        /// svuint64_t svaddp[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svaddp[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> AddPairwise(Vector<ulong> left, Vector<ulong> right) => AddPairwise(left, right);

        /// <summary>
        /// svfloat32_t svaddp[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svaddp[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> AddPairwise(Vector<float> left, Vector<float> right) => AddPairwise(left, right);

        /// <summary>
        /// svfloat64_t svaddp[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svaddp[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> AddPairwise(Vector<double> left, Vector<double> right) => AddPairwise(left, right);


        ///  AddPairwiseWidening : Add and accumulate long pairwise

        /// <summary>
        /// svint16_t svadalp[_s16]_m(svbool_t pg, svint16_t op1, svint8_t op2)
        /// svint16_t svadalp[_s16]_x(svbool_t pg, svint16_t op1, svint8_t op2)
        /// svint16_t svadalp[_s16]_z(svbool_t pg, svint16_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<short> AddPairwiseWidening(Vector<short> left, Vector<sbyte> right) => AddPairwiseWidening(left, right);

        /// <summary>
        /// svint32_t svadalp[_s32]_m(svbool_t pg, svint32_t op1, svint16_t op2)
        /// svint32_t svadalp[_s32]_x(svbool_t pg, svint32_t op1, svint16_t op2)
        /// svint32_t svadalp[_s32]_z(svbool_t pg, svint32_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<int> AddPairwiseWidening(Vector<int> left, Vector<short> right) => AddPairwiseWidening(left, right);

        /// <summary>
        /// svint64_t svadalp[_s64]_m(svbool_t pg, svint64_t op1, svint32_t op2)
        /// svint64_t svadalp[_s64]_x(svbool_t pg, svint64_t op1, svint32_t op2)
        /// svint64_t svadalp[_s64]_z(svbool_t pg, svint64_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<long> AddPairwiseWidening(Vector<long> left, Vector<int> right) => AddPairwiseWidening(left, right);

        /// <summary>
        /// svuint16_t svadalp[_u16]_m(svbool_t pg, svuint16_t op1, svuint8_t op2)
        /// svuint16_t svadalp[_u16]_x(svbool_t pg, svuint16_t op1, svuint8_t op2)
        /// svuint16_t svadalp[_u16]_z(svbool_t pg, svuint16_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<ushort> AddPairwiseWidening(Vector<ushort> left, Vector<byte> right) => AddPairwiseWidening(left, right);

        /// <summary>
        /// svuint32_t svadalp[_u32]_m(svbool_t pg, svuint32_t op1, svuint16_t op2)
        /// svuint32_t svadalp[_u32]_x(svbool_t pg, svuint32_t op1, svuint16_t op2)
        /// svuint32_t svadalp[_u32]_z(svbool_t pg, svuint32_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<uint> AddPairwiseWidening(Vector<uint> left, Vector<ushort> right) => AddPairwiseWidening(left, right);

        /// <summary>
        /// svuint64_t svadalp[_u64]_m(svbool_t pg, svuint64_t op1, svuint32_t op2)
        /// svuint64_t svadalp[_u64]_x(svbool_t pg, svuint64_t op1, svuint32_t op2)
        /// svuint64_t svadalp[_u64]_z(svbool_t pg, svuint64_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<ulong> AddPairwiseWidening(Vector<ulong> left, Vector<uint> right) => AddPairwiseWidening(left, right);


        ///  AddRotateComplex : Complex add with rotate

        /// <summary>
        /// svint8_t svcadd[_s8](svint8_t op1, svint8_t op2, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<sbyte> AddRotateComplex(Vector<sbyte> left, Vector<sbyte> right, [ConstantExpected] byte rotation) => AddRotateComplex(left, right, rotation);

        /// <summary>
        /// svint16_t svcadd[_s16](svint16_t op1, svint16_t op2, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<short> AddRotateComplex(Vector<short> left, Vector<short> right, [ConstantExpected] byte rotation) => AddRotateComplex(left, right, rotation);

        /// <summary>
        /// svint32_t svcadd[_s32](svint32_t op1, svint32_t op2, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<int> AddRotateComplex(Vector<int> left, Vector<int> right, [ConstantExpected] byte rotation) => AddRotateComplex(left, right, rotation);

        /// <summary>
        /// svint64_t svcadd[_s64](svint64_t op1, svint64_t op2, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<long> AddRotateComplex(Vector<long> left, Vector<long> right, [ConstantExpected] byte rotation) => AddRotateComplex(left, right, rotation);

        /// <summary>
        /// svuint8_t svcadd[_u8](svuint8_t op1, svuint8_t op2, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<byte> AddRotateComplex(Vector<byte> left, Vector<byte> right, [ConstantExpected] byte rotation) => AddRotateComplex(left, right, rotation);

        /// <summary>
        /// svuint16_t svcadd[_u16](svuint16_t op1, svuint16_t op2, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<ushort> AddRotateComplex(Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte rotation) => AddRotateComplex(left, right, rotation);

        /// <summary>
        /// svuint32_t svcadd[_u32](svuint32_t op1, svuint32_t op2, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<uint> AddRotateComplex(Vector<uint> left, Vector<uint> right, [ConstantExpected] byte rotation) => AddRotateComplex(left, right, rotation);

        /// <summary>
        /// svuint64_t svcadd[_u64](svuint64_t op1, svuint64_t op2, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<ulong> AddRotateComplex(Vector<ulong> left, Vector<ulong> right, [ConstantExpected] byte rotation) => AddRotateComplex(left, right, rotation);


        ///  AddSaturate : Saturating add

        /// <summary>
        /// svint8_t svqadd[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svqadd[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svqadd[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> AddSaturate(Vector<sbyte> left, Vector<sbyte> right) => AddSaturate(left, right);

        /// <summary>
        /// svint16_t svqadd[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svqadd[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svqadd[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> AddSaturate(Vector<short> left, Vector<short> right) => AddSaturate(left, right);

        /// <summary>
        /// svint32_t svqadd[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svqadd[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svqadd[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> AddSaturate(Vector<int> left, Vector<int> right) => AddSaturate(left, right);

        /// <summary>
        /// svint64_t svqadd[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svqadd[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svqadd[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> AddSaturate(Vector<long> left, Vector<long> right) => AddSaturate(left, right);

        /// <summary>
        /// svuint8_t svqadd[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svqadd[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svqadd[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> AddSaturate(Vector<byte> left, Vector<byte> right) => AddSaturate(left, right);

        /// <summary>
        /// svuint16_t svqadd[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svqadd[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svqadd[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> AddSaturate(Vector<ushort> left, Vector<ushort> right) => AddSaturate(left, right);

        /// <summary>
        /// svuint32_t svqadd[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svqadd[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svqadd[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> AddSaturate(Vector<uint> left, Vector<uint> right) => AddSaturate(left, right);

        /// <summary>
        /// svuint64_t svqadd[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svqadd[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svqadd[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> AddSaturate(Vector<ulong> left, Vector<ulong> right) => AddSaturate(left, right);


        ///  AddSaturateWithSignedAddend : Saturating add with signed addend

        /// <summary>
        /// svuint8_t svsqadd[_u8]_m(svbool_t pg, svuint8_t op1, svint8_t op2)
        /// svuint8_t svsqadd[_u8]_x(svbool_t pg, svuint8_t op1, svint8_t op2)
        /// svuint8_t svsqadd[_u8]_z(svbool_t pg, svuint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> AddSaturateWithSignedAddend(Vector<byte> left, Vector<sbyte> right) => AddSaturateWithSignedAddend(left, right);

        /// <summary>
        /// svuint16_t svsqadd[_u16]_m(svbool_t pg, svuint16_t op1, svint16_t op2)
        /// svuint16_t svsqadd[_u16]_x(svbool_t pg, svuint16_t op1, svint16_t op2)
        /// svuint16_t svsqadd[_u16]_z(svbool_t pg, svuint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> AddSaturateWithSignedAddend(Vector<ushort> left, Vector<short> right) => AddSaturateWithSignedAddend(left, right);

        /// <summary>
        /// svuint32_t svsqadd[_u32]_m(svbool_t pg, svuint32_t op1, svint32_t op2)
        /// svuint32_t svsqadd[_u32]_x(svbool_t pg, svuint32_t op1, svint32_t op2)
        /// svuint32_t svsqadd[_u32]_z(svbool_t pg, svuint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> AddSaturateWithSignedAddend(Vector<uint> left, Vector<int> right) => AddSaturateWithSignedAddend(left, right);

        /// <summary>
        /// svuint64_t svsqadd[_u64]_m(svbool_t pg, svuint64_t op1, svint64_t op2)
        /// svuint64_t svsqadd[_u64]_x(svbool_t pg, svuint64_t op1, svint64_t op2)
        /// svuint64_t svsqadd[_u64]_z(svbool_t pg, svuint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> AddSaturateWithSignedAddend(Vector<ulong> left, Vector<long> right) => AddSaturateWithSignedAddend(left, right);


        ///  AddSaturateWithUnsignedAddend : Saturating add with unsigned addend

        /// <summary>
        /// svint8_t svuqadd[_s8]_m(svbool_t pg, svint8_t op1, svuint8_t op2)
        /// svint8_t svuqadd[_s8]_x(svbool_t pg, svint8_t op1, svuint8_t op2)
        /// svint8_t svuqadd[_s8]_z(svbool_t pg, svint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> AddSaturateWithUnsignedAddend(Vector<sbyte> left, Vector<byte> right) => AddSaturateWithUnsignedAddend(left, right);

        /// <summary>
        /// svint16_t svuqadd[_s16]_m(svbool_t pg, svint16_t op1, svuint16_t op2)
        /// svint16_t svuqadd[_s16]_x(svbool_t pg, svint16_t op1, svuint16_t op2)
        /// svint16_t svuqadd[_s16]_z(svbool_t pg, svint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<short> AddSaturateWithUnsignedAddend(Vector<short> left, Vector<ushort> right) => AddSaturateWithUnsignedAddend(left, right);

        /// <summary>
        /// svint32_t svuqadd[_s32]_m(svbool_t pg, svint32_t op1, svuint32_t op2)
        /// svint32_t svuqadd[_s32]_x(svbool_t pg, svint32_t op1, svuint32_t op2)
        /// svint32_t svuqadd[_s32]_z(svbool_t pg, svint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<int> AddSaturateWithUnsignedAddend(Vector<int> left, Vector<uint> right) => AddSaturateWithUnsignedAddend(left, right);

        /// <summary>
        /// svint64_t svuqadd[_s64]_m(svbool_t pg, svint64_t op1, svuint64_t op2)
        /// svint64_t svuqadd[_s64]_x(svbool_t pg, svint64_t op1, svuint64_t op2)
        /// svint64_t svuqadd[_s64]_z(svbool_t pg, svint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<long> AddSaturateWithUnsignedAddend(Vector<long> left, Vector<ulong> right) => AddSaturateWithUnsignedAddend(left, right);


        ///  AddWideLower : Add wide (bottom)

        /// <summary>
        /// svint16_t svaddwb[_s16](svint16_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<short> AddWideLower(Vector<short> left, Vector<sbyte> right) => AddWideLower(left, right);

        /// <summary>
        /// svint32_t svaddwb[_s32](svint32_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<int> AddWideLower(Vector<int> left, Vector<short> right) => AddWideLower(left, right);

        /// <summary>
        /// svint64_t svaddwb[_s64](svint64_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<long> AddWideLower(Vector<long> left, Vector<int> right) => AddWideLower(left, right);

        /// <summary>
        /// svuint16_t svaddwb[_u16](svuint16_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<ushort> AddWideLower(Vector<ushort> left, Vector<byte> right) => AddWideLower(left, right);

        /// <summary>
        /// svuint32_t svaddwb[_u32](svuint32_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<uint> AddWideLower(Vector<uint> left, Vector<ushort> right) => AddWideLower(left, right);

        /// <summary>
        /// svuint64_t svaddwb[_u64](svuint64_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<ulong> AddWideLower(Vector<ulong> left, Vector<uint> right) => AddWideLower(left, right);


        ///  AddWideUpper : Add wide (top)

        /// <summary>
        /// svint16_t svaddwt[_s16](svint16_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<short> AddWideUpper(Vector<short> left, Vector<sbyte> right) => AddWideUpper(left, right);

        /// <summary>
        /// svint32_t svaddwt[_s32](svint32_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<int> AddWideUpper(Vector<int> left, Vector<short> right) => AddWideUpper(left, right);

        /// <summary>
        /// svint64_t svaddwt[_s64](svint64_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<long> AddWideUpper(Vector<long> left, Vector<int> right) => AddWideUpper(left, right);

        /// <summary>
        /// svuint16_t svaddwt[_u16](svuint16_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<ushort> AddWideUpper(Vector<ushort> left, Vector<byte> right) => AddWideUpper(left, right);

        /// <summary>
        /// svuint32_t svaddwt[_u32](svuint32_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<uint> AddWideUpper(Vector<uint> left, Vector<ushort> right) => AddWideUpper(left, right);

        /// <summary>
        /// svuint64_t svaddwt[_u64](svuint64_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<ulong> AddWideUpper(Vector<ulong> left, Vector<uint> right) => AddWideUpper(left, right);


        ///  AddWideningLower : Add long (bottom)

        /// <summary>
        /// svint16_t svaddlb[_s16](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<short> AddWideningLower(Vector<sbyte> left, Vector<sbyte> right) => AddWideningLower(left, right);

        /// <summary>
        /// svint32_t svaddlb[_s32](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<int> AddWideningLower(Vector<short> left, Vector<short> right) => AddWideningLower(left, right);

        /// <summary>
        /// svint64_t svaddlb[_s64](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<long> AddWideningLower(Vector<int> left, Vector<int> right) => AddWideningLower(left, right);

        /// <summary>
        /// svuint16_t svaddlb[_u16](svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<ushort> AddWideningLower(Vector<byte> left, Vector<byte> right) => AddWideningLower(left, right);

        /// <summary>
        /// svuint32_t svaddlb[_u32](svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<uint> AddWideningLower(Vector<ushort> left, Vector<ushort> right) => AddWideningLower(left, right);

        /// <summary>
        /// svuint64_t svaddlb[_u64](svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<ulong> AddWideningLower(Vector<uint> left, Vector<uint> right) => AddWideningLower(left, right);


        ///  AddWideningLowerUpper : Add long (bottom + top)

        /// <summary>
        /// svint16_t svaddlbt[_s16](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<short> AddWideningLowerUpper(Vector<sbyte> left, Vector<sbyte> right) => AddWideningLowerUpper(left, right);

        /// <summary>
        /// svint32_t svaddlbt[_s32](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<int> AddWideningLowerUpper(Vector<short> left, Vector<short> right) => AddWideningLowerUpper(left, right);

        /// <summary>
        /// svint64_t svaddlbt[_s64](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<long> AddWideningLowerUpper(Vector<int> left, Vector<int> right) => AddWideningLowerUpper(left, right);


        ///  AddWideningUpper : Add long (top)

        /// <summary>
        /// svint16_t svaddlt[_s16](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<short> AddWideningUpper(Vector<sbyte> left, Vector<sbyte> right) => AddWideningUpper(left, right);

        /// <summary>
        /// svint32_t svaddlt[_s32](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<int> AddWideningUpper(Vector<short> left, Vector<short> right) => AddWideningUpper(left, right);

        /// <summary>
        /// svint64_t svaddlt[_s64](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<long> AddWideningUpper(Vector<int> left, Vector<int> right) => AddWideningUpper(left, right);

        /// <summary>
        /// svuint16_t svaddlt[_u16](svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<ushort> AddWideningUpper(Vector<byte> left, Vector<byte> right) => AddWideningUpper(left, right);

        /// <summary>
        /// svuint32_t svaddlt[_u32](svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<uint> AddWideningUpper(Vector<ushort> left, Vector<ushort> right) => AddWideningUpper(left, right);

        /// <summary>
        /// svuint64_t svaddlt[_u64](svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<ulong> AddWideningUpper(Vector<uint> left, Vector<uint> right) => AddWideningUpper(left, right);


        ///  BitwiseClearXor : Bitwise clear and exclusive OR

        /// <summary>
        /// svint8_t svbcax[_s8](svint8_t op1, svint8_t op2, svint8_t op3)
        /// </summary>
        public static unsafe Vector<sbyte> BitwiseClearXor(Vector<sbyte> xor, Vector<sbyte> value, Vector<sbyte> mask) => BitwiseClearXor(xor, value, mask);

        /// <summary>
        /// svint16_t svbcax[_s16](svint16_t op1, svint16_t op2, svint16_t op3)
        /// </summary>
        public static unsafe Vector<short> BitwiseClearXor(Vector<short> xor, Vector<short> value, Vector<short> mask) => BitwiseClearXor(xor, value, mask);

        /// <summary>
        /// svint32_t svbcax[_s32](svint32_t op1, svint32_t op2, svint32_t op3)
        /// </summary>
        public static unsafe Vector<int> BitwiseClearXor(Vector<int> xor, Vector<int> value, Vector<int> mask) => BitwiseClearXor(xor, value, mask);

        /// <summary>
        /// svint64_t svbcax[_s64](svint64_t op1, svint64_t op2, svint64_t op3)
        /// </summary>
        public static unsafe Vector<long> BitwiseClearXor(Vector<long> xor, Vector<long> value, Vector<long> mask) => BitwiseClearXor(xor, value, mask);

        /// <summary>
        /// svuint8_t svbcax[_u8](svuint8_t op1, svuint8_t op2, svuint8_t op3)
        /// </summary>
        public static unsafe Vector<byte> BitwiseClearXor(Vector<byte> xor, Vector<byte> value, Vector<byte> mask) => BitwiseClearXor(xor, value, mask);

        /// <summary>
        /// svuint16_t svbcax[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3)
        /// </summary>
        public static unsafe Vector<ushort> BitwiseClearXor(Vector<ushort> xor, Vector<ushort> value, Vector<ushort> mask) => BitwiseClearXor(xor, value, mask);

        /// <summary>
        /// svuint32_t svbcax[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3)
        /// </summary>
        public static unsafe Vector<uint> BitwiseClearXor(Vector<uint> xor, Vector<uint> value, Vector<uint> mask) => BitwiseClearXor(xor, value, mask);

        /// <summary>
        /// svuint64_t svbcax[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3)
        /// </summary>
        public static unsafe Vector<ulong> BitwiseClearXor(Vector<ulong> xor, Vector<ulong> value, Vector<ulong> mask) => BitwiseClearXor(xor, value, mask);


        ///  BitwiseSelect : Bitwise select

        /// <summary>
        /// svint8_t svbsl[_s8](svint8_t op1, svint8_t op2, svint8_t op3)
        /// </summary>
        public static unsafe Vector<sbyte> BitwiseSelect(Vector<sbyte> select, Vector<sbyte> left, Vector<sbyte> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// svint16_t svbsl[_s16](svint16_t op1, svint16_t op2, svint16_t op3)
        /// </summary>
        public static unsafe Vector<short> BitwiseSelect(Vector<short> select, Vector<short> left, Vector<short> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// svint32_t svbsl[_s32](svint32_t op1, svint32_t op2, svint32_t op3)
        /// </summary>
        public static unsafe Vector<int> BitwiseSelect(Vector<int> select, Vector<int> left, Vector<int> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// svint64_t svbsl[_s64](svint64_t op1, svint64_t op2, svint64_t op3)
        /// </summary>
        public static unsafe Vector<long> BitwiseSelect(Vector<long> select, Vector<long> left, Vector<long> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// svuint8_t svbsl[_u8](svuint8_t op1, svuint8_t op2, svuint8_t op3)
        /// </summary>
        public static unsafe Vector<byte> BitwiseSelect(Vector<byte> select, Vector<byte> left, Vector<byte> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// svuint16_t svbsl[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3)
        /// </summary>
        public static unsafe Vector<ushort> BitwiseSelect(Vector<ushort> select, Vector<ushort> left, Vector<ushort> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// svuint32_t svbsl[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3)
        /// </summary>
        public static unsafe Vector<uint> BitwiseSelect(Vector<uint> select, Vector<uint> left, Vector<uint> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// svuint64_t svbsl[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3)
        /// </summary>
        public static unsafe Vector<ulong> BitwiseSelect(Vector<ulong> select, Vector<ulong> left, Vector<ulong> right) => BitwiseSelect(select, left, right);



        ///  BitwiseSelectLeftInverted : Bitwise select with first input inverted

        /// <summary>
        /// svint8_t svbsl1n[_s8](svint8_t op1, svint8_t op2, svint8_t op3)
        /// </summary>
        public static unsafe Vector<sbyte> BitwiseSelectLeftInverted(Vector<sbyte> select, Vector<sbyte> left, Vector<sbyte> right) => BitwiseSelectLeftInverted(select, left, right);

        /// <summary>
        /// svint16_t svbsl1n[_s16](svint16_t op1, svint16_t op2, svint16_t op3)
        /// </summary>
        public static unsafe Vector<short> BitwiseSelectLeftInverted(Vector<short> select, Vector<short> left, Vector<short> right) => BitwiseSelectLeftInverted(select, left, right);

        /// <summary>
        /// svint32_t svbsl1n[_s32](svint32_t op1, svint32_t op2, svint32_t op3)
        /// </summary>
        public static unsafe Vector<int> BitwiseSelectLeftInverted(Vector<int> select, Vector<int> left, Vector<int> right) => BitwiseSelectLeftInverted(select, left, right);

        /// <summary>
        /// svint64_t svbsl1n[_s64](svint64_t op1, svint64_t op2, svint64_t op3)
        /// </summary>
        public static unsafe Vector<long> BitwiseSelectLeftInverted(Vector<long> select, Vector<long> left, Vector<long> right) => BitwiseSelectLeftInverted(select, left, right);

        /// <summary>
        /// svuint8_t svbsl1n[_u8](svuint8_t op1, svuint8_t op2, svuint8_t op3)
        /// </summary>
        public static unsafe Vector<byte> BitwiseSelectLeftInverted(Vector<byte> select, Vector<byte> left, Vector<byte> right) => BitwiseSelectLeftInverted(select, left, right);

        /// <summary>
        /// svuint16_t svbsl1n[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3)
        /// </summary>
        public static unsafe Vector<ushort> BitwiseSelectLeftInverted(Vector<ushort> select, Vector<ushort> left, Vector<ushort> right) => BitwiseSelectLeftInverted(select, left, right);

        /// <summary>
        /// svuint32_t svbsl1n[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3)
        /// </summary>
        public static unsafe Vector<uint> BitwiseSelectLeftInverted(Vector<uint> select, Vector<uint> left, Vector<uint> right) => BitwiseSelectLeftInverted(select, left, right);

        /// <summary>
        /// svuint64_t svbsl1n[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3)
        /// </summary>
        public static unsafe Vector<ulong> BitwiseSelectLeftInverted(Vector<ulong> select, Vector<ulong> left, Vector<ulong> right) => BitwiseSelectLeftInverted(select, left, right);


        ///  BitwiseSelectRightInverted : Bitwise select with second input inverted

        /// <summary>
        /// svint8_t svbsl2n[_s8](svint8_t op1, svint8_t op2, svint8_t op3)
        /// </summary>
        public static unsafe Vector<sbyte> BitwiseSelectRightInverted(Vector<sbyte> select, Vector<sbyte> left, Vector<sbyte> right) => BitwiseSelectRightInverted(select, left, right);

        /// <summary>
        /// svint16_t svbsl2n[_s16](svint16_t op1, svint16_t op2, svint16_t op3)
        /// </summary>
        public static unsafe Vector<short> BitwiseSelectRightInverted(Vector<short> select, Vector<short> left, Vector<short> right) => BitwiseSelectRightInverted(select, left, right);

        /// <summary>
        /// svint32_t svbsl2n[_s32](svint32_t op1, svint32_t op2, svint32_t op3)
        /// </summary>
        public static unsafe Vector<int> BitwiseSelectRightInverted(Vector<int> select, Vector<int> left, Vector<int> right) => BitwiseSelectRightInverted(select, left, right);

        /// <summary>
        /// svint64_t svbsl2n[_s64](svint64_t op1, svint64_t op2, svint64_t op3)
        /// </summary>
        public static unsafe Vector<long> BitwiseSelectRightInverted(Vector<long> select, Vector<long> left, Vector<long> right) => BitwiseSelectRightInverted(select, left, right);

        /// <summary>
        /// svuint8_t svbsl2n[_u8](svuint8_t op1, svuint8_t op2, svuint8_t op3)
        /// </summary>
        public static unsafe Vector<byte> BitwiseSelectRightInverted(Vector<byte> select, Vector<byte> left, Vector<byte> right) => BitwiseSelectRightInverted(select, left, right);

        /// <summary>
        /// svuint16_t svbsl2n[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3)
        /// </summary>
        public static unsafe Vector<ushort> BitwiseSelectRightInverted(Vector<ushort> select, Vector<ushort> left, Vector<ushort> right) => BitwiseSelectRightInverted(select, left, right);

        /// <summary>
        /// svuint32_t svbsl2n[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3)
        /// </summary>
        public static unsafe Vector<uint> BitwiseSelectRightInverted(Vector<uint> select, Vector<uint> left, Vector<uint> right) => BitwiseSelectRightInverted(select, left, right);

        /// <summary>
        /// svuint64_t svbsl2n[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3)
        /// </summary>
        public static unsafe Vector<ulong> BitwiseSelectRightInverted(Vector<ulong> select, Vector<ulong> left, Vector<ulong> right) => BitwiseSelectRightInverted(select, left, right);


        ///  CountMatchingElements : Count matching elements

        /// <summary>
        /// svuint32_t svhistcnt[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> CountMatchingElements(Vector<int> mask, Vector<int> left, Vector<int> right) => CountMatchingElements(mask, left, right);

        /// <summary>
        /// svuint32_t svhistcnt[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> CountMatchingElements(Vector<uint> mask, Vector<uint> left, Vector<uint> right) => CountMatchingElements(mask, left, right);

        /// <summary>
        /// svuint64_t svhistcnt[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CountMatchingElements(Vector<long> mask, Vector<long> left, Vector<long> right) => CountMatchingElements(mask, left, right);

        /// <summary>
        /// svuint64_t svhistcnt[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CountMatchingElements(Vector<ulong> mask, Vector<ulong> left, Vector<ulong> right) => CountMatchingElements(mask, left, right);


        ///  CountMatchingElementsIn128BitSegments : Count matching elements in 128-bit segments

        /// <summary>
        /// svuint8_t svhistseg[_s8](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> CountMatchingElementsIn128BitSegments(Vector<sbyte> left, Vector<sbyte> right) => CountMatchingElementsIn128BitSegments(left, right);

        /// <summary>
        /// svuint8_t svhistseg[_u8](svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> CountMatchingElementsIn128BitSegments(Vector<byte> left, Vector<byte> right) => CountMatchingElementsIn128BitSegments(left, right);


        ///  CreateWhileGreaterThanMask : While decrementing scalar is greater than

        /// <summary>
        /// svbool_t svwhilegt_b8[_s32](int32_t op1, int32_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateWhileGreaterThanMask(int left, int right) => CreateWhileGreaterThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilegt_b8[_s64](int64_t op1, int64_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateWhileGreaterThanMask(long left, long right) => CreateWhileGreaterThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilegt_b8[_u32](uint32_t op1, uint32_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateWhileGreaterThanMask(uint left, uint right) => CreateWhileGreaterThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilegt_b8[_u64](uint64_t op1, uint64_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateWhileGreaterThanMask(ulong left, ulong right) => CreateWhileGreaterThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilegt_b16[_s32](int32_t op1, int32_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileGreaterThanMask(int left, int right) => CreateWhileGreaterThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilegt_b16[_s64](int64_t op1, int64_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileGreaterThanMask(long left, long right) => CreateWhileGreaterThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilegt_b16[_u32](uint32_t op1, uint32_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileGreaterThanMask(uint left, uint right) => CreateWhileGreaterThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilegt_b16[_u64](uint64_t op1, uint64_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileGreaterThanMask(ulong left, ulong right) => CreateWhileGreaterThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilegt_b32[_s32](int32_t op1, int32_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateWhileGreaterThanMask(int left, int right) => CreateWhileGreaterThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilegt_b32[_s64](int64_t op1, int64_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateWhileGreaterThanMask(long left, long right) => CreateWhileGreaterThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilegt_b32[_u32](uint32_t op1, uint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateWhileGreaterThanMask(uint left, uint right) => CreateWhileGreaterThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilegt_b32[_u64](uint64_t op1, uint64_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateWhileGreaterThanMask(ulong left, ulong right) => CreateWhileGreaterThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilegt_b64[_s32](int32_t op1, int32_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileGreaterThanMask(int left, int right) => CreateWhileGreaterThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilegt_b64[_s64](int64_t op1, int64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileGreaterThanMask(long left, long right) => CreateWhileGreaterThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilegt_b64[_u32](uint32_t op1, uint32_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileGreaterThanMask(uint left, uint right) => CreateWhileGreaterThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilegt_b64[_u64](uint64_t op1, uint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileGreaterThanMask(ulong left, ulong right) => CreateWhileGreaterThanMask(left, right);


        ///  CreateWhileGreaterThanOrEqualMask : While decrementing scalar is greater than or equal to

        /// <summary>
        /// svbool_t svwhilege_b8[_s32](int32_t op1, int32_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateWhileGreaterThanOrEqualMask(int left, int right) => CreateWhileGreaterThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilege_b8[_s64](int64_t op1, int64_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateWhileGreaterThanOrEqualMask(long left, long right) => CreateWhileGreaterThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilege_b8[_u32](uint32_t op1, uint32_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateWhileGreaterThanOrEqualMask(uint left, uint right) => CreateWhileGreaterThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilege_b8[_u64](uint64_t op1, uint64_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateWhileGreaterThanOrEqualMask(ulong left, ulong right) => CreateWhileGreaterThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilege_b16[_s32](int32_t op1, int32_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileGreaterThanOrEqualMask(int left, int right) => CreateWhileGreaterThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilege_b16[_s64](int64_t op1, int64_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileGreaterThanOrEqualMask(long left, long right) => CreateWhileGreaterThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilege_b16[_u32](uint32_t op1, uint32_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileGreaterThanOrEqualMask(uint left, uint right) => CreateWhileGreaterThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilege_b16[_u64](uint64_t op1, uint64_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileGreaterThanOrEqualMask(ulong left, ulong right) => CreateWhileGreaterThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilege_b32[_s32](int32_t op1, int32_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateWhileGreaterThanOrEqualMask(int left, int right) => CreateWhileGreaterThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilege_b32[_s64](int64_t op1, int64_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateWhileGreaterThanOrEqualMask(long left, long right) => CreateWhileGreaterThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilege_b32[_u32](uint32_t op1, uint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateWhileGreaterThanOrEqualMask(uint left, uint right) => CreateWhileGreaterThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilege_b32[_u64](uint64_t op1, uint64_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateWhileGreaterThanOrEqualMask(ulong left, ulong right) => CreateWhileGreaterThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilege_b64[_s32](int32_t op1, int32_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileGreaterThanOrEqualMask(int left, int right) => CreateWhileGreaterThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilege_b64[_s64](int64_t op1, int64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileGreaterThanOrEqualMask(long left, long right) => CreateWhileGreaterThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilege_b64[_u32](uint32_t op1, uint32_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileGreaterThanOrEqualMask(uint left, uint right) => CreateWhileGreaterThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilege_b64[_u64](uint64_t op1, uint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileGreaterThanOrEqualMask(ulong left, ulong right) => CreateWhileGreaterThanOrEqualMask(left, right);


        ///  CreateWhileReadAfterWriteMask : While free of read-after-write conflicts

        /// <summary>
        /// svbool_t svwhilerw[_s8](const int8_t *op1, const int8_t *op2)
        /// </summary>
        public static unsafe Vector<sbyte> CreateWhileReadAfterWriteMask(sbyte* left, sbyte* right) => CreateWhileReadAfterWriteMask(left, right);

        /// <summary>
        /// svbool_t svwhilerw[_s16](const int16_t *op1, const int16_t *op2)
        /// </summary>
        public static unsafe Vector<short> CreateWhileReadAfterWriteMask(short* left, short* right) => CreateWhileReadAfterWriteMask(left, right);

        /// <summary>
        /// svbool_t svwhilerw[_s32](const int32_t *op1, const int32_t *op2)
        /// </summary>
        public static unsafe Vector<int> CreateWhileReadAfterWriteMask(int* left, int* right) => CreateWhileReadAfterWriteMask(left, right);

        /// <summary>
        /// svbool_t svwhilerw[_s64](const int64_t *op1, const int64_t *op2)
        /// </summary>
        public static unsafe Vector<long> CreateWhileReadAfterWriteMask(long* left, long* right) => CreateWhileReadAfterWriteMask(left, right);

        /// <summary>
        /// svbool_t svwhilerw[_u8](const uint8_t *op1, const uint8_t *op2)
        /// </summary>
        public static unsafe Vector<byte> CreateWhileReadAfterWriteMask(byte* left, byte* right) => CreateWhileReadAfterWriteMask(left, right);

        /// <summary>
        /// svbool_t svwhilerw[_u16](const uint16_t *op1, const uint16_t *op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileReadAfterWriteMask(ushort* left, ushort* right) => CreateWhileReadAfterWriteMask(left, right);

        /// <summary>
        /// svbool_t svwhilerw[_u32](const uint32_t *op1, const uint32_t *op2)
        /// </summary>
        public static unsafe Vector<uint> CreateWhileReadAfterWriteMask(uint* left, uint* right) => CreateWhileReadAfterWriteMask(left, right);

        /// <summary>
        /// svbool_t svwhilerw[_u64](const uint64_t *op1, const uint64_t *op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileReadAfterWriteMask(ulong* left, ulong* right) => CreateWhileReadAfterWriteMask(left, right);

        /// <summary>
        /// svbool_t svwhilerw[_f32](const float32_t *op1, const float32_t *op2)
        /// </summary>
        public static unsafe Vector<float> CreateWhileReadAfterWriteMask(float* left, float* right) => CreateWhileReadAfterWriteMask(left, right);

        /// <summary>
        /// svbool_t svwhilerw[_f64](const float64_t *op1, const float64_t *op2)
        /// </summary>
        public static unsafe Vector<double> CreateWhileReadAfterWriteMask(double* left, double* right) => CreateWhileReadAfterWriteMask(left, right);


        ///  CreateWhileWriteAfterReadMask : While free of write-after-read conflicts

        /// <summary>
        /// svbool_t svwhilewr[_s8](const int8_t *op1, const int8_t *op2)
        /// </summary>
        public static unsafe Vector<sbyte> CreateWhileWriteAfterReadMask(sbyte* left, sbyte* right) => CreateWhileWriteAfterReadMask(left, right);

        /// <summary>
        /// svbool_t svwhilewr[_s16](const int16_t *op1, const int16_t *op2)
        /// </summary>
        public static unsafe Vector<short> CreateWhileWriteAfterReadMask(short* left, short* right) => CreateWhileWriteAfterReadMask(left, right);

        /// <summary>
        /// svbool_t svwhilewr[_s32](const int32_t *op1, const int32_t *op2)
        /// </summary>
        public static unsafe Vector<int> CreateWhileWriteAfterReadMask(int* left, int* right) => CreateWhileWriteAfterReadMask(left, right);

        /// <summary>
        /// svbool_t svwhilewr[_s64](const int64_t *op1, const int64_t *op2)
        /// </summary>
        public static unsafe Vector<long> CreateWhileWriteAfterReadMask(long* left, long* right) => CreateWhileWriteAfterReadMask(left, right);

        /// <summary>
        /// svbool_t svwhilewr[_u8](const uint8_t *op1, const uint8_t *op2)
        /// </summary>
        public static unsafe Vector<byte> CreateWhileWriteAfterReadMask(byte* left, byte* right) => CreateWhileWriteAfterReadMask(left, right);

        /// <summary>
        /// svbool_t svwhilewr[_u16](const uint16_t *op1, const uint16_t *op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileWriteAfterReadMask(ushort* left, ushort* right) => CreateWhileWriteAfterReadMask(left, right);

        /// <summary>
        /// svbool_t svwhilewr[_u32](const uint32_t *op1, const uint32_t *op2)
        /// </summary>
        public static unsafe Vector<uint> CreateWhileWriteAfterReadMask(uint* left, uint* right) => CreateWhileWriteAfterReadMask(left, right);

        /// <summary>
        /// svbool_t svwhilewr[_u64](const uint64_t *op1, const uint64_t *op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileWriteAfterReadMask(ulong* left, ulong* right) => CreateWhileWriteAfterReadMask(left, right);

        /// <summary>
        /// svbool_t svwhilewr[_f32](const float32_t *op1, const float32_t *op2)
        /// </summary>
        public static unsafe Vector<float> CreateWhileWriteAfterReadMask(float* left, float* right) => CreateWhileWriteAfterReadMask(left, right);

        /// <summary>
        /// svbool_t svwhilewr[_f64](const float64_t *op1, const float64_t *op2)
        /// </summary>
        public static unsafe Vector<double> CreateWhileWriteAfterReadMask(double* left, double* right) => CreateWhileWriteAfterReadMask(left, right);


        ///  DotProductComplex : Complex dot product

        /// <summary>
        /// svint32_t svcdot[_s32](svint32_t op1, svint8_t op2, svint8_t op3, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<int> DotProductComplex(Vector<int> op1, Vector<sbyte> op2, Vector<sbyte> op3, [ConstantExpected] byte rotation) => DotProductComplex(op1, op2, op3, rotation);

        /// <summary>
        /// svint32_t svcdot_lane[_s32](svint32_t op1, svint8_t op2, svint8_t op3, uint64_t imm_index, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<int> DotProductComplex(Vector<int> op1, Vector<sbyte> op2, Vector<sbyte> op3, ulong imm_index, [ConstantExpected] byte rotation) => DotProductComplex(op1, op2, op3, imm_index, rotation);

        /// <summary>
        /// svint64_t svcdot[_s64](svint64_t op1, svint16_t op2, svint16_t op3, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<long> DotProductComplex(Vector<long> op1, Vector<short> op2, Vector<short> op3, [ConstantExpected] byte rotation) => DotProductComplex(op1, op2, op3, rotation);

        /// <summary>
        /// svint64_t svcdot_lane[_s64](svint64_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<long> DotProductComplex(Vector<long> op1, Vector<short> op2, Vector<short> op3, ulong imm_index, [ConstantExpected] byte rotation) => DotProductComplex(op1, op2, op3, imm_index, rotation);


        ///  DownConvertNarrowingUpper : Down convert and narrow (top)

        /// <summary>
        /// svfloat32_t svcvtnt_f32[_f64]_m(svfloat32_t even, svbool_t pg, svfloat64_t op)
        /// svfloat32_t svcvtnt_f32[_f64]_x(svfloat32_t even, svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<float> DownConvertNarrowingUpper(Vector<double> value) => DownConvertNarrowingUpper(value);


        ///  DownConvertRoundingOdd : Down convert, rounding to odd

        /// <summary>
        /// svfloat32_t svcvtx_f32[_f64]_m(svfloat32_t inactive, svbool_t pg, svfloat64_t op)
        /// svfloat32_t svcvtx_f32[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svfloat32_t svcvtx_f32[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<float> DownConvertRoundingOdd(Vector<double> value) => DownConvertRoundingOdd(value);


        ///  DownConvertRoundingOddUpper : Down convert, rounding to odd (top)

        /// <summary>
        /// svfloat32_t svcvtxnt_f32[_f64]_m(svfloat32_t even, svbool_t pg, svfloat64_t op)
        /// svfloat32_t svcvtxnt_f32[_f64]_x(svfloat32_t even, svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<float> DownConvertRoundingOddUpper(Vector<double> value) => DownConvertRoundingOddUpper(value);


        ///  GatherVectorByteZeroExtendNonTemporal : Load 8-bit data and zero-extend, non-temporal

        /// <summary>
        /// svint32_t svldnt1ub_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteZeroExtendNonTemporal(Vector<int> mask, Vector<uint> addresses) => GatherVectorByteZeroExtendNonTemporal(mask, addresses);

        /// <summary>
        /// svint32_t svldnt1ub_gather_[u32]offset_s32(svbool_t pg, const uint8_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteZeroExtendNonTemporal(Vector<int> mask, byte* address, Vector<uint> offsets) => GatherVectorByteZeroExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svint64_t svldnt1ub_gather_[s64]offset_s64(svbool_t pg, const uint8_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtendNonTemporal(Vector<long> mask, byte* address, Vector<long> offsets) => GatherVectorByteZeroExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svint64_t svldnt1ub_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtendNonTemporal(Vector<long> mask, Vector<ulong> addresses) => GatherVectorByteZeroExtendNonTemporal(mask, addresses);

        /// <summary>
        /// svint64_t svldnt1ub_gather_[u64]offset_s64(svbool_t pg, const uint8_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtendNonTemporal(Vector<long> mask, byte* address, Vector<ulong> offsets) => GatherVectorByteZeroExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svuint32_t svldnt1ub_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteZeroExtendNonTemporal(Vector<uint> mask, Vector<uint> addresses) => GatherVectorByteZeroExtendNonTemporal(mask, addresses);

        /// <summary>
        /// svuint32_t svldnt1ub_gather_[u32]offset_u32(svbool_t pg, const uint8_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteZeroExtendNonTemporal(Vector<uint> mask, byte* address, Vector<uint> offsets) => GatherVectorByteZeroExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldnt1ub_gather_[s64]offset_u64(svbool_t pg, const uint8_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtendNonTemporal(Vector<ulong> mask, byte* address, Vector<long> offsets) => GatherVectorByteZeroExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldnt1ub_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtendNonTemporal(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorByteZeroExtendNonTemporal(mask, addresses);

        /// <summary>
        /// svuint64_t svldnt1ub_gather_[u64]offset_u64(svbool_t pg, const uint8_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtendNonTemporal(Vector<ulong> mask, byte* address, Vector<ulong> offsets) => GatherVectorByteZeroExtendNonTemporal(mask, address, offsets);


        ///  GatherVectorInt16SignExtendNonTemporal : Load 16-bit data and sign-extend, non-temporal

        /// <summary>
        /// svint32_t svldnt1sh_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtendNonTemporal(Vector<int> mask, Vector<uint> addresses) => GatherVectorInt16SignExtendNonTemporal(mask, addresses);

        /// <summary>
        /// svint64_t svldnt1sh_gather_[s64]index_s64(svbool_t pg, const int16_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtendNonTemporal(Vector<long> mask, short* address, Vector<long> indices) => GatherVectorInt16SignExtendNonTemporal(mask, address, indices);

        /// <summary>
        /// svint64_t svldnt1sh_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtendNonTemporal(Vector<long> mask, Vector<ulong> addresses) => GatherVectorInt16SignExtendNonTemporal(mask, addresses);

        /// <summary>
        /// svint64_t svldnt1sh_gather_[u64]index_s64(svbool_t pg, const int16_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtendNonTemporal(Vector<long> mask, short* address, Vector<ulong> indices) => GatherVectorInt16SignExtendNonTemporal(mask, address, indices);

        /// <summary>
        /// svuint32_t svldnt1sh_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtendNonTemporal(Vector<uint> mask, Vector<uint> addresses) => GatherVectorInt16SignExtendNonTemporal(mask, addresses);

        /// <summary>
        /// svuint64_t svldnt1sh_gather_[s64]index_u64(svbool_t pg, const int16_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtendNonTemporal(Vector<ulong> mask, short* address, Vector<long> indices) => GatherVectorInt16SignExtendNonTemporal(mask, address, indices);

        /// <summary>
        /// svuint64_t svldnt1sh_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtendNonTemporal(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorInt16SignExtendNonTemporal(mask, addresses);

        /// <summary>
        /// svuint64_t svldnt1sh_gather_[u64]index_u64(svbool_t pg, const int16_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtendNonTemporal(Vector<ulong> mask, short* address, Vector<ulong> indices) => GatherVectorInt16SignExtendNonTemporal(mask, address, indices);


        ///  GatherVectorInt16WithByteOffsetsSignExtendNonTemporal : Load 16-bit data and sign-extend, non-temporal

        /// <summary>
        /// svint32_t svldnt1sh_gather_[u32]offset_s32(svbool_t pg, const int16_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16WithByteOffsetsSignExtendNonTemporal(Vector<int> mask, short* address, Vector<uint> offsets) => GatherVectorInt16WithByteOffsetsSignExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svint64_t svldnt1sh_gather_[s64]offset_s64(svbool_t pg, const int16_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16WithByteOffsetsSignExtendNonTemporal(Vector<long> mask, short* address, Vector<long> offsets) => GatherVectorInt16WithByteOffsetsSignExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svint64_t svldnt1sh_gather_[u64]offset_s64(svbool_t pg, const int16_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16WithByteOffsetsSignExtendNonTemporal(Vector<long> mask, short* address, Vector<ulong> offsets) => GatherVectorInt16WithByteOffsetsSignExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svuint32_t svldnt1sh_gather_[u32]offset_u32(svbool_t pg, const int16_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16WithByteOffsetsSignExtendNonTemporal(Vector<uint> mask, short* address, Vector<uint> offsets) => GatherVectorInt16WithByteOffsetsSignExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldnt1sh_gather_[s64]offset_u64(svbool_t pg, const int16_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16WithByteOffsetsSignExtendNonTemporal(Vector<ulong> mask, short* address, Vector<long> offsets) => GatherVectorInt16WithByteOffsetsSignExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldnt1sh_gather_[u64]offset_u64(svbool_t pg, const int16_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16WithByteOffsetsSignExtendNonTemporal(Vector<ulong> mask, short* address, Vector<ulong> offsets) => GatherVectorInt16WithByteOffsetsSignExtendNonTemporal(mask, address, offsets);


        ///  GatherVectorInt32SignExtendNonTemporal : Load 32-bit data and sign-extend, non-temporal

        /// <summary>
        /// svint64_t svldnt1sw_gather_[s64]index_s64(svbool_t pg, const int32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt32SignExtendNonTemporal(Vector<int> mask, int* address, Vector<int> indices) => GatherVectorInt32SignExtendNonTemporal(mask, address, indices);

        /// <summary>
        /// svint64_t svldnt1sw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt32SignExtendNonTemporal(Vector<int> mask, Vector<uint> addresses) => GatherVectorInt32SignExtendNonTemporal(mask, addresses);

        /// <summary>
        /// svint64_t svldnt1sw_gather_[u64]index_s64(svbool_t pg, const int32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt32SignExtendNonTemporal(Vector<int> mask, int* address, Vector<uint> indices) => GatherVectorInt32SignExtendNonTemporal(mask, address, indices);

        /// <summary>
        /// svint64_t svldnt1sw_gather_[s64]index_s64(svbool_t pg, const int32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtendNonTemporal(Vector<long> mask, int* address, Vector<long> indices) => GatherVectorInt32SignExtendNonTemporal(mask, address, indices);

        /// <summary>
        /// svint64_t svldnt1sw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtendNonTemporal(Vector<long> mask, Vector<ulong> addresses) => GatherVectorInt32SignExtendNonTemporal(mask, addresses);

        /// <summary>
        /// svint64_t svldnt1sw_gather_[u64]index_s64(svbool_t pg, const int32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtendNonTemporal(Vector<long> mask, int* address, Vector<ulong> indices) => GatherVectorInt32SignExtendNonTemporal(mask, address, indices);

        /// <summary>
        /// svuint64_t svldnt1sw_gather_[s64]index_u64(svbool_t pg, const int32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt32SignExtendNonTemporal(Vector<uint> mask, int* address, Vector<int> indices) => GatherVectorInt32SignExtendNonTemporal(mask, address, indices);

        /// <summary>
        /// svuint64_t svldnt1sw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt32SignExtendNonTemporal(Vector<uint> mask, Vector<uint> addresses) => GatherVectorInt32SignExtendNonTemporal(mask, addresses);

        /// <summary>
        /// svuint64_t svldnt1sw_gather_[u64]index_u64(svbool_t pg, const int32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt32SignExtendNonTemporal(Vector<uint> mask, int* address, Vector<uint> indices) => GatherVectorInt32SignExtendNonTemporal(mask, address, indices);

        /// <summary>
        /// svuint64_t svldnt1sw_gather_[s64]index_u64(svbool_t pg, const int32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtendNonTemporal(Vector<ulong> mask, int* address, Vector<long> indices) => GatherVectorInt32SignExtendNonTemporal(mask, address, indices);

        /// <summary>
        /// svuint64_t svldnt1sw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtendNonTemporal(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorInt32SignExtendNonTemporal(mask, addresses);

        /// <summary>
        /// svuint64_t svldnt1sw_gather_[u64]index_u64(svbool_t pg, const int32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtendNonTemporal(Vector<ulong> mask, int* address, Vector<ulong> indices) => GatherVectorInt32SignExtendNonTemporal(mask, address, indices);


        ///  GatherVectorInt32WithByteOffsetsSignExtendNonTemporal : Load 32-bit data and sign-extend, non-temporal

        /// <summary>
        /// svint64_t svldnt1sw_gather_[s64]offset_s64(svbool_t pg, const int32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(Vector<int> mask, int* address, Vector<int> offsets) => GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svint64_t svldnt1sw_gather_[u64]offset_s64(svbool_t pg, const int32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(Vector<int> mask, int* address, Vector<uint> offsets) => GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svint64_t svldnt1sw_gather_[s64]offset_s64(svbool_t pg, const int32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(Vector<long> mask, int* address, Vector<long> offsets) => GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svint64_t svldnt1sw_gather_[u64]offset_s64(svbool_t pg, const int32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(Vector<long> mask, int* address, Vector<ulong> offsets) => GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldnt1sw_gather_[s64]offset_u64(svbool_t pg, const int32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(Vector<uint> mask, int* address, Vector<int> offsets) => GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldnt1sw_gather_[u64]offset_u64(svbool_t pg, const int32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(Vector<uint> mask, int* address, Vector<uint> offsets) => GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldnt1sw_gather_[s64]offset_u64(svbool_t pg, const int32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(Vector<ulong> mask, int* address, Vector<long> offsets) => GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldnt1sw_gather_[u64]offset_u64(svbool_t pg, const int32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(Vector<ulong> mask, int* address, Vector<ulong> offsets) => GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(mask, address, offsets);


        ///  GatherVectorNonTemporal : Unextended load, non-temporal

        /// <summary>
        /// svint32_t svldnt1_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorNonTemporal(Vector<int> mask, Vector<uint> addresses) => GatherVectorNonTemporal(mask, addresses);

        /// <summary>
        /// svint32_t svldnt1_gather_[u32]offset[_s32](svbool_t pg, const int32_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorNonTemporal(Vector<int> mask, int* address, Vector<uint> offsets) => GatherVectorNonTemporal(mask, address, offsets);

        /// <summary>
        /// svint64_t svldnt1_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorNonTemporal(Vector<long> mask, Vector<ulong> addresses) => GatherVectorNonTemporal(mask, addresses);

        /// <summary>
        /// svint64_t svldnt1_gather_[s64]offset[_s64](svbool_t pg, const int64_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorNonTemporal(Vector<long> mask, long* address, Vector<long> offsets) => GatherVectorNonTemporal(mask, address, offsets);

        /// <summary>
        /// svint64_t svldnt1_gather_[u64]offset[_s64](svbool_t pg, const int64_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorNonTemporal(Vector<long> mask, long* address, Vector<ulong> offsets) => GatherVectorNonTemporal(mask, address, offsets);

        /// <summary>
        /// svint64_t svldnt1_gather_[s64]index[_s64](svbool_t pg, const int64_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorNonTemporal(Vector<long> mask, long* address, Vector<long> indices) => GatherVectorNonTemporal(mask, address, indices);

        /// <summary>
        /// svint64_t svldnt1_gather_[u64]index[_s64](svbool_t pg, const int64_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorNonTemporal(Vector<long> mask, long* address, Vector<ulong> indices) => GatherVectorNonTemporal(mask, address, indices);

        /// <summary>
        /// svuint32_t svldnt1_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorNonTemporal(Vector<uint> mask, Vector<uint> addresses) => GatherVectorNonTemporal(mask, addresses);

        /// <summary>
        /// svuint32_t svldnt1_gather_[u32]offset[_u32](svbool_t pg, const uint32_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorNonTemporal(Vector<uint> mask, uint* address, Vector<uint> offsets) => GatherVectorNonTemporal(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldnt1_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorNonTemporal(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorNonTemporal(mask, addresses);

        /// <summary>
        /// svuint64_t svldnt1_gather_[s64]offset[_u64](svbool_t pg, const uint64_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorNonTemporal(Vector<ulong> mask, ulong* address, Vector<long> offsets) => GatherVectorNonTemporal(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldnt1_gather_[u64]offset[_u64](svbool_t pg, const uint64_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorNonTemporal(Vector<ulong> mask, ulong* address, Vector<ulong> offsets) => GatherVectorNonTemporal(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldnt1_gather_[s64]index[_u64](svbool_t pg, const uint64_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorNonTemporal(Vector<ulong> mask, ulong* address, Vector<long> indices) => GatherVectorNonTemporal(mask, address, indices);

        /// <summary>
        /// svuint64_t svldnt1_gather_[u64]index[_u64](svbool_t pg, const uint64_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorNonTemporal(Vector<ulong> mask, ulong* address, Vector<ulong> indices) => GatherVectorNonTemporal(mask, address, indices);

        /// <summary>
        /// svfloat32_t svldnt1_gather[_u32base]_f32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<float> GatherVectorNonTemporal(Vector<float> mask, Vector<uint> addresses) => GatherVectorNonTemporal(mask, addresses);

        /// <summary>
        /// svfloat32_t svldnt1_gather_[u32]offset[_f32](svbool_t pg, const float32_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<float> GatherVectorNonTemporal(Vector<float> mask, float* address, Vector<uint> offsets) => GatherVectorNonTemporal(mask, address, offsets);

        /// <summary>
        /// svfloat64_t svldnt1_gather_[s64]offset[_f64](svbool_t pg, const float64_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<double> GatherVectorNonTemporal(Vector<double> mask, double* address, Vector<long> offsets) => GatherVectorNonTemporal(mask, address, offsets);

        /// <summary>
        /// svfloat64_t svldnt1_gather_[s64]index[_f64](svbool_t pg, const float64_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<double> GatherVectorNonTemporal(Vector<double> mask, double* address, Vector<long> indices) => GatherVectorNonTemporal(mask, address, indices);

        /// <summary>
        /// svfloat64_t svldnt1_gather[_u64base]_f64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<double> GatherVectorNonTemporal(Vector<double> mask, Vector<ulong> addresses) => GatherVectorNonTemporal(mask, addresses);

        /// <summary>
        /// svfloat64_t svldnt1_gather_[u64]offset[_f64](svbool_t pg, const float64_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<double> GatherVectorNonTemporal(Vector<double> mask, double* address, Vector<ulong> offsets) => GatherVectorNonTemporal(mask, address, offsets);

        /// <summary>
        /// svfloat64_t svldnt1_gather_[u64]index[_f64](svbool_t pg, const float64_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<double> GatherVectorNonTemporal(Vector<double> mask, double* address, Vector<ulong> indices) => GatherVectorNonTemporal(mask, address, indices);


        ///  GatherVectorSByteSignExtendNonTemporal : Load 8-bit data and sign-extend, non-temporal

        /// <summary>
        /// svint32_t svldnt1sb_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorSByteSignExtendNonTemporal(Vector<int> mask, Vector<uint> addresses) => GatherVectorSByteSignExtendNonTemporal(mask, addresses);

        /// <summary>
        /// svint32_t svldnt1sb_gather_[u32]offset_s32(svbool_t pg, const int8_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorSByteSignExtendNonTemporal(Vector<int> mask, sbyte* address, Vector<uint> offsets) => GatherVectorSByteSignExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svint64_t svldnt1sb_gather_[s64]offset_s64(svbool_t pg, const int8_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorSByteSignExtendNonTemporal(Vector<long> mask, sbyte* address, Vector<long> offsets) => GatherVectorSByteSignExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svint64_t svldnt1sb_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorSByteSignExtendNonTemporal(Vector<long> mask, Vector<ulong> addresses) => GatherVectorSByteSignExtendNonTemporal(mask, addresses);

        /// <summary>
        /// svint64_t svldnt1sb_gather_[u64]offset_s64(svbool_t pg, const int8_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorSByteSignExtendNonTemporal(Vector<long> mask, sbyte* address, Vector<ulong> offsets) => GatherVectorSByteSignExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svuint32_t svldnt1sb_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorSByteSignExtendNonTemporal(Vector<uint> mask, Vector<uint> addresses) => GatherVectorSByteSignExtendNonTemporal(mask, addresses);

        /// <summary>
        /// svuint32_t svldnt1sb_gather_[u32]offset_u32(svbool_t pg, const int8_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorSByteSignExtendNonTemporal(Vector<uint> mask, sbyte* address, Vector<uint> offsets) => GatherVectorSByteSignExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldnt1sb_gather_[s64]offset_u64(svbool_t pg, const int8_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorSByteSignExtendNonTemporal(Vector<ulong> mask, sbyte* address, Vector<long> offsets) => GatherVectorSByteSignExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldnt1sb_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorSByteSignExtendNonTemporal(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorSByteSignExtendNonTemporal(mask, addresses);

        /// <summary>
        /// svuint64_t svldnt1sb_gather_[u64]offset_u64(svbool_t pg, const int8_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorSByteSignExtendNonTemporal(Vector<ulong> mask, sbyte* address, Vector<ulong> offsets) => GatherVectorSByteSignExtendNonTemporal(mask, address, offsets);


        ///  GatherVectorUInt16WithByteOffsetsZeroExtendNonTemporal : Load 16-bit data and zero-extend, non-temporal

        /// <summary>
        /// svint32_t svldnt1uh_gather_[u32]offset_s32(svbool_t pg, const uint16_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16WithByteOffsetsZeroExtendNonTemporal(Vector<int> mask, ushort* address, Vector<uint> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svint64_t svldnt1uh_gather_[s64]offset_s64(svbool_t pg, const uint16_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16WithByteOffsetsZeroExtendNonTemporal(Vector<long> mask, ushort* address, Vector<long> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svint64_t svldnt1uh_gather_[u64]offset_s64(svbool_t pg, const uint16_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16WithByteOffsetsZeroExtendNonTemporal(Vector<long> mask, ushort* address, Vector<ulong> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svuint32_t svldnt1uh_gather_[u32]offset_u32(svbool_t pg, const uint16_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16WithByteOffsetsZeroExtendNonTemporal(Vector<uint> mask, ushort* address, Vector<uint> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldnt1uh_gather_[s64]offset_u64(svbool_t pg, const uint16_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16WithByteOffsetsZeroExtendNonTemporal(Vector<ulong> mask, ushort* address, Vector<long> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldnt1uh_gather_[u64]offset_u64(svbool_t pg, const uint16_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16WithByteOffsetsZeroExtendNonTemporal(Vector<ulong> mask, ushort* address, Vector<ulong> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtendNonTemporal(mask, address, offsets);


        ///  GatherVectorUInt16ZeroExtendNonTemporal : Load 16-bit data and zero-extend, non-temporal

        /// <summary>
        /// svint32_t svldnt1uh_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16ZeroExtendNonTemporal(Vector<int> mask, Vector<uint> addresses) => GatherVectorUInt16ZeroExtendNonTemporal(mask, addresses);

        /// <summary>
        /// svint64_t svldnt1uh_gather_[s64]index_s64(svbool_t pg, const uint16_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16ZeroExtendNonTemporal(Vector<long> mask, ushort* address, Vector<long> indices) => GatherVectorUInt16ZeroExtendNonTemporal(mask, address, indices);

        /// <summary>
        /// svint64_t svldnt1uh_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16ZeroExtendNonTemporal(Vector<long> mask, Vector<ulong> addresses) => GatherVectorUInt16ZeroExtendNonTemporal(mask, addresses);

        /// <summary>
        /// svint64_t svldnt1uh_gather_[u64]index_s64(svbool_t pg, const uint16_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16ZeroExtendNonTemporal(Vector<long> mask, ushort* address, Vector<ulong> indices) => GatherVectorUInt16ZeroExtendNonTemporal(mask, address, indices);

        /// <summary>
        /// svuint32_t svldnt1uh_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16ZeroExtendNonTemporal(Vector<uint> mask, Vector<uint> addresses) => GatherVectorUInt16ZeroExtendNonTemporal(mask, addresses);

        /// <summary>
        /// svuint64_t svldnt1uh_gather_[s64]index_u64(svbool_t pg, const uint16_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtendNonTemporal(Vector<ulong> mask, ushort* address, Vector<long> indices) => GatherVectorUInt16ZeroExtendNonTemporal(mask, address, indices);

        /// <summary>
        /// svuint64_t svldnt1uh_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtendNonTemporal(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorUInt16ZeroExtendNonTemporal(mask, addresses);

        /// <summary>
        /// svuint64_t svldnt1uh_gather_[u64]index_u64(svbool_t pg, const uint16_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtendNonTemporal(Vector<ulong> mask, ushort* address, Vector<ulong> indices) => GatherVectorUInt16ZeroExtendNonTemporal(mask, address, indices);


        ///  GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal : Load 32-bit data and zero-extend, non-temporal

        /// <summary>
        /// svint64_t svldnt1uw_gather_[s64]offset_s64(svbool_t pg, const uint32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(Vector<int> mask, uint* address, Vector<int> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svint64_t svldnt1uw_gather_[u64]offset_s64(svbool_t pg, const uint32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(Vector<int> mask, uint* address, Vector<uint> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svint64_t svldnt1uw_gather_[s64]offset_s64(svbool_t pg, const uint32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(Vector<long> mask, uint* address, Vector<long> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svint64_t svldnt1uw_gather_[u64]offset_s64(svbool_t pg, const uint32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(Vector<long> mask, uint* address, Vector<ulong> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldnt1uw_gather_[s64]offset_u64(svbool_t pg, const uint32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(Vector<uint> mask, uint* address, Vector<int> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldnt1uw_gather_[u64]offset_u64(svbool_t pg, const uint32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(Vector<uint> mask, uint* address, Vector<uint> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldnt1uw_gather_[s64]offset_u64(svbool_t pg, const uint32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(Vector<ulong> mask, uint* address, Vector<long> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldnt1uw_gather_[u64]offset_u64(svbool_t pg, const uint32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(Vector<ulong> mask, uint* address, Vector<ulong> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(mask, address, offsets);


        ///  GatherVectorUInt32ZeroExtendNonTemporal : Load 32-bit data and zero-extend, non-temporal

        /// <summary>
        /// svint64_t svldnt1uw_gather_[s64]index_s64(svbool_t pg, const uint32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32ZeroExtendNonTemporal(Vector<int> mask, uint* address, Vector<int> indices) => GatherVectorUInt32ZeroExtendNonTemporal(mask, address, indices);

        /// <summary>
        /// svint64_t svldnt1uw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32ZeroExtendNonTemporal(Vector<int> mask, Vector<uint> addresses) => GatherVectorUInt32ZeroExtendNonTemporal(mask, addresses);

        /// <summary>
        /// svint64_t svldnt1uw_gather_[u64]index_s64(svbool_t pg, const uint32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32ZeroExtendNonTemporal(Vector<int> mask, uint* address, Vector<uint> indices) => GatherVectorUInt32ZeroExtendNonTemporal(mask, address, indices);

        /// <summary>
        /// svint64_t svldnt1uw_gather_[s64]index_s64(svbool_t pg, const uint32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32ZeroExtendNonTemporal(Vector<long> mask, uint* address, Vector<long> indices) => GatherVectorUInt32ZeroExtendNonTemporal(mask, address, indices);

        /// <summary>
        /// svint64_t svldnt1uw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32ZeroExtendNonTemporal(Vector<long> mask, Vector<ulong> addresses) => GatherVectorUInt32ZeroExtendNonTemporal(mask, addresses);

        /// <summary>
        /// svint64_t svldnt1uw_gather_[u64]index_s64(svbool_t pg, const uint32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32ZeroExtendNonTemporal(Vector<long> mask, uint* address, Vector<ulong> indices) => GatherVectorUInt32ZeroExtendNonTemporal(mask, address, indices);

        /// <summary>
        /// svuint64_t svldnt1uw_gather_[s64]index_u64(svbool_t pg, const uint32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32ZeroExtendNonTemporal(Vector<uint> mask, uint* address, Vector<int> indices) => GatherVectorUInt32ZeroExtendNonTemporal(mask, address, indices);

        /// <summary>
        /// svuint64_t svldnt1uw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32ZeroExtendNonTemporal(Vector<uint> mask, Vector<uint> addresses) => GatherVectorUInt32ZeroExtendNonTemporal(mask, addresses);

        /// <summary>
        /// svuint64_t svldnt1uw_gather_[u64]index_u64(svbool_t pg, const uint32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32ZeroExtendNonTemporal(Vector<uint> mask, uint* address, Vector<uint> indices) => GatherVectorUInt32ZeroExtendNonTemporal(mask, address, indices);

        /// <summary>
        /// svuint64_t svldnt1uw_gather_[s64]index_u64(svbool_t pg, const uint32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtendNonTemporal(Vector<ulong> mask, uint* address, Vector<long> indices) => GatherVectorUInt32ZeroExtendNonTemporal(mask, address, indices);

        /// <summary>
        /// svuint64_t svldnt1uw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtendNonTemporal(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorUInt32ZeroExtendNonTemporal(mask, addresses);

        /// <summary>
        /// svuint64_t svldnt1uw_gather_[u64]index_u64(svbool_t pg, const uint32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtendNonTemporal(Vector<ulong> mask, uint* address, Vector<ulong> indices) => GatherVectorUInt32ZeroExtendNonTemporal(mask, address, indices);


        ///  HalvingAdd : Halving add

        /// <summary>
        /// svint8_t svhadd[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svhadd[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svhadd[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> HalvingAdd(Vector<sbyte> left, Vector<sbyte> right) => HalvingAdd(left, right);

        /// <summary>
        /// svint16_t svhadd[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svhadd[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svhadd[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> HalvingAdd(Vector<short> left, Vector<short> right) => HalvingAdd(left, right);

        /// <summary>
        /// svint32_t svhadd[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svhadd[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svhadd[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> HalvingAdd(Vector<int> left, Vector<int> right) => HalvingAdd(left, right);

        /// <summary>
        /// svint64_t svhadd[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svhadd[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svhadd[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> HalvingAdd(Vector<long> left, Vector<long> right) => HalvingAdd(left, right);

        /// <summary>
        /// svuint8_t svhadd[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svhadd[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svhadd[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> HalvingAdd(Vector<byte> left, Vector<byte> right) => HalvingAdd(left, right);

        /// <summary>
        /// svuint16_t svhadd[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svhadd[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svhadd[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> HalvingAdd(Vector<ushort> left, Vector<ushort> right) => HalvingAdd(left, right);

        /// <summary>
        /// svuint32_t svhadd[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svhadd[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svhadd[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> HalvingAdd(Vector<uint> left, Vector<uint> right) => HalvingAdd(left, right);

        /// <summary>
        /// svuint64_t svhadd[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svhadd[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svhadd[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> HalvingAdd(Vector<ulong> left, Vector<ulong> right) => HalvingAdd(left, right);


        ///  HalvingSubtract : Halving subtract

        /// <summary>
        /// svint8_t svhsub[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svhsub[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svhsub[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> HalvingSubtract(Vector<sbyte> left, Vector<sbyte> right) => HalvingSubtract(left, right);

        /// <summary>
        /// svint16_t svhsub[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svhsub[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svhsub[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> HalvingSubtract(Vector<short> left, Vector<short> right) => HalvingSubtract(left, right);

        /// <summary>
        /// svint32_t svhsub[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svhsub[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svhsub[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> HalvingSubtract(Vector<int> left, Vector<int> right) => HalvingSubtract(left, right);

        /// <summary>
        /// svint64_t svhsub[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svhsub[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svhsub[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> HalvingSubtract(Vector<long> left, Vector<long> right) => HalvingSubtract(left, right);

        /// <summary>
        /// svuint8_t svhsub[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svhsub[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svhsub[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> HalvingSubtract(Vector<byte> left, Vector<byte> right) => HalvingSubtract(left, right);

        /// <summary>
        /// svuint16_t svhsub[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svhsub[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svhsub[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> HalvingSubtract(Vector<ushort> left, Vector<ushort> right) => HalvingSubtract(left, right);

        /// <summary>
        /// svuint32_t svhsub[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svhsub[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svhsub[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> HalvingSubtract(Vector<uint> left, Vector<uint> right) => HalvingSubtract(left, right);

        /// <summary>
        /// svuint64_t svhsub[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svhsub[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svhsub[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> HalvingSubtract(Vector<ulong> left, Vector<ulong> right) => HalvingSubtract(left, right);


        ///  HalvingSubtractReversed : Halving subtract reversed

        /// <summary>
        /// svint8_t svhsubr[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svhsubr[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svhsubr[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> HalvingSubtractReversed(Vector<sbyte> left, Vector<sbyte> right) => HalvingSubtractReversed(left, right);

        /// <summary>
        /// svint16_t svhsubr[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svhsubr[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svhsubr[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> HalvingSubtractReversed(Vector<short> left, Vector<short> right) => HalvingSubtractReversed(left, right);

        /// <summary>
        /// svint32_t svhsubr[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svhsubr[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svhsubr[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> HalvingSubtractReversed(Vector<int> left, Vector<int> right) => HalvingSubtractReversed(left, right);

        /// <summary>
        /// svint64_t svhsubr[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svhsubr[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svhsubr[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> HalvingSubtractReversed(Vector<long> left, Vector<long> right) => HalvingSubtractReversed(left, right);

        /// <summary>
        /// svuint8_t svhsubr[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svhsubr[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svhsubr[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> HalvingSubtractReversed(Vector<byte> left, Vector<byte> right) => HalvingSubtractReversed(left, right);

        /// <summary>
        /// svuint16_t svhsubr[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svhsubr[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svhsubr[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> HalvingSubtractReversed(Vector<ushort> left, Vector<ushort> right) => HalvingSubtractReversed(left, right);

        /// <summary>
        /// svuint32_t svhsubr[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svhsubr[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svhsubr[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> HalvingSubtractReversed(Vector<uint> left, Vector<uint> right) => HalvingSubtractReversed(left, right);

        /// <summary>
        /// svuint64_t svhsubr[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svhsubr[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svhsubr[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> HalvingSubtractReversed(Vector<ulong> left, Vector<ulong> right) => HalvingSubtractReversed(left, right);


        ///  InterleavingXorLowerUpper : Interleaving exclusive OR (bottom, top)

        /// <summary>
        /// svint8_t sveorbt[_s8](svint8_t odd, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> InterleavingXorLowerUpper(Vector<sbyte> odd, Vector<sbyte> left, Vector<sbyte> right) => InterleavingXorLowerUpper(odd, left, right);

        /// <summary>
        /// svint16_t sveorbt[_s16](svint16_t odd, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> InterleavingXorLowerUpper(Vector<short> odd, Vector<short> left, Vector<short> right) => InterleavingXorLowerUpper(odd, left, right);

        /// <summary>
        /// svint32_t sveorbt[_s32](svint32_t odd, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> InterleavingXorLowerUpper(Vector<int> odd, Vector<int> left, Vector<int> right) => InterleavingXorLowerUpper(odd, left, right);

        /// <summary>
        /// svint64_t sveorbt[_s64](svint64_t odd, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> InterleavingXorLowerUpper(Vector<long> odd, Vector<long> left, Vector<long> right) => InterleavingXorLowerUpper(odd, left, right);

        /// <summary>
        /// svuint8_t sveorbt[_u8](svuint8_t odd, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> InterleavingXorLowerUpper(Vector<byte> odd, Vector<byte> left, Vector<byte> right) => InterleavingXorLowerUpper(odd, left, right);

        /// <summary>
        /// svuint16_t sveorbt[_u16](svuint16_t odd, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> InterleavingXorLowerUpper(Vector<ushort> odd, Vector<ushort> left, Vector<ushort> right) => InterleavingXorLowerUpper(odd, left, right);

        /// <summary>
        /// svuint32_t sveorbt[_u32](svuint32_t odd, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> InterleavingXorLowerUpper(Vector<uint> odd, Vector<uint> left, Vector<uint> right) => InterleavingXorLowerUpper(odd, left, right);

        /// <summary>
        /// svuint64_t sveorbt[_u64](svuint64_t odd, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> InterleavingXorLowerUpper(Vector<ulong> odd, Vector<ulong> left, Vector<ulong> right) => InterleavingXorLowerUpper(odd, left, right);


        ///  InterleavingXorUpperLower : Interleaving exclusive OR (top, bottom)

        /// <summary>
        /// svint8_t sveortb[_s8](svint8_t even, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> InterleavingXorUpperLower(Vector<sbyte> even, Vector<sbyte> left, Vector<sbyte> right) => InterleavingXorUpperLower(even, left, right);

        /// <summary>
        /// svint16_t sveortb[_s16](svint16_t even, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> InterleavingXorUpperLower(Vector<short> even, Vector<short> left, Vector<short> right) => InterleavingXorUpperLower(even, left, right);

        /// <summary>
        /// svint32_t sveortb[_s32](svint32_t even, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> InterleavingXorUpperLower(Vector<int> even, Vector<int> left, Vector<int> right) => InterleavingXorUpperLower(even, left, right);

        /// <summary>
        /// svint64_t sveortb[_s64](svint64_t even, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> InterleavingXorUpperLower(Vector<long> even, Vector<long> left, Vector<long> right) => InterleavingXorUpperLower(even, left, right);

        /// <summary>
        /// svuint8_t sveortb[_u8](svuint8_t even, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> InterleavingXorUpperLower(Vector<byte> even, Vector<byte> left, Vector<byte> right) => InterleavingXorUpperLower(even, left, right);

        /// <summary>
        /// svuint16_t sveortb[_u16](svuint16_t even, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> InterleavingXorUpperLower(Vector<ushort> even, Vector<ushort> left, Vector<ushort> right) => InterleavingXorUpperLower(even, left, right);

        /// <summary>
        /// svuint32_t sveortb[_u32](svuint32_t even, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> InterleavingXorUpperLower(Vector<uint> even, Vector<uint> left, Vector<uint> right) => InterleavingXorUpperLower(even, left, right);

        /// <summary>
        /// svuint64_t sveortb[_u64](svuint64_t even, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> InterleavingXorUpperLower(Vector<ulong> even, Vector<ulong> left, Vector<ulong> right) => InterleavingXorUpperLower(even, left, right);


        ///  Log2 : Base 2 logarithm as integer

        /// <summary>
        /// svint32_t svlogb[_f32]_m(svint32_t inactive, svbool_t pg, svfloat32_t op)
        /// svint32_t svlogb[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svint32_t svlogb[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<int> Log2(Vector<float> value) => Log2(value);

        /// <summary>
        /// svint64_t svlogb[_f64]_m(svint64_t inactive, svbool_t pg, svfloat64_t op)
        /// svint64_t svlogb[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svint64_t svlogb[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<long> Log2(Vector<double> value) => Log2(value);


        ///  Match : Detect any matching elements

        /// <summary>
        /// svbool_t svmatch[_s8](svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> Match(Vector<sbyte> mask, Vector<sbyte> left, Vector<sbyte> right) => Match(mask, left, right);

        /// <summary>
        /// svbool_t svmatch[_s16](svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> Match(Vector<short> mask, Vector<short> left, Vector<short> right) => Match(mask, left, right);

        /// <summary>
        /// svbool_t svmatch[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> Match(Vector<byte> mask, Vector<byte> left, Vector<byte> right) => Match(mask, left, right);

        /// <summary>
        /// svbool_t svmatch[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> Match(Vector<ushort> mask, Vector<ushort> left, Vector<ushort> right) => Match(mask, left, right);


        ///  MaxNumberPairwise : Maximum number pairwise

        /// <summary>
        /// svfloat32_t svmaxnmp[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmaxnmp[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> MaxNumberPairwise(Vector<float> left, Vector<float> right) => MaxNumberPairwise(left, right);

        /// <summary>
        /// svfloat64_t svmaxnmp[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmaxnmp[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> MaxNumberPairwise(Vector<double> left, Vector<double> right) => MaxNumberPairwise(left, right);


        ///  MaxPairwise : Maximum pairwise

        /// <summary>
        /// svint8_t svmaxp[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svmaxp[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> MaxPairwise(Vector<sbyte> left, Vector<sbyte> right) => MaxPairwise(left, right);

        /// <summary>
        /// svint16_t svmaxp[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svmaxp[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> MaxPairwise(Vector<short> left, Vector<short> right) => MaxPairwise(left, right);

        /// <summary>
        /// svint32_t svmaxp[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svmaxp[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> MaxPairwise(Vector<int> left, Vector<int> right) => MaxPairwise(left, right);

        /// <summary>
        /// svint64_t svmaxp[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svmaxp[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> MaxPairwise(Vector<long> left, Vector<long> right) => MaxPairwise(left, right);

        /// <summary>
        /// svuint8_t svmaxp[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svmaxp[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> MaxPairwise(Vector<byte> left, Vector<byte> right) => MaxPairwise(left, right);

        /// <summary>
        /// svuint16_t svmaxp[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svmaxp[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> MaxPairwise(Vector<ushort> left, Vector<ushort> right) => MaxPairwise(left, right);

        /// <summary>
        /// svuint32_t svmaxp[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svmaxp[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> MaxPairwise(Vector<uint> left, Vector<uint> right) => MaxPairwise(left, right);

        /// <summary>
        /// svuint64_t svmaxp[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svmaxp[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> MaxPairwise(Vector<ulong> left, Vector<ulong> right) => MaxPairwise(left, right);

        /// <summary>
        /// svfloat32_t svmaxp[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmaxp[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> MaxPairwise(Vector<float> left, Vector<float> right) => MaxPairwise(left, right);

        /// <summary>
        /// svfloat64_t svmaxp[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmaxp[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> MaxPairwise(Vector<double> left, Vector<double> right) => MaxPairwise(left, right);


        ///  MinNumberPairwise : Minimum number pairwise

        /// <summary>
        /// svfloat32_t svminnmp[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svminnmp[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> MinNumberPairwise(Vector<float> left, Vector<float> right) => MinNumberPairwise(left, right);

        /// <summary>
        /// svfloat64_t svminnmp[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svminnmp[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> MinNumberPairwise(Vector<double> left, Vector<double> right) => MinNumberPairwise(left, right);


        ///  MinPairwise : Minimum pairwise

        /// <summary>
        /// svint8_t svminp[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svminp[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> MinPairwise(Vector<sbyte> left, Vector<sbyte> right) => MinPairwise(left, right);

        /// <summary>
        /// svint16_t svminp[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svminp[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> MinPairwise(Vector<short> left, Vector<short> right) => MinPairwise(left, right);

        /// <summary>
        /// svint32_t svminp[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svminp[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> MinPairwise(Vector<int> left, Vector<int> right) => MinPairwise(left, right);

        /// <summary>
        /// svint64_t svminp[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svminp[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> MinPairwise(Vector<long> left, Vector<long> right) => MinPairwise(left, right);

        /// <summary>
        /// svuint8_t svminp[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svminp[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> MinPairwise(Vector<byte> left, Vector<byte> right) => MinPairwise(left, right);

        /// <summary>
        /// svuint16_t svminp[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svminp[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> MinPairwise(Vector<ushort> left, Vector<ushort> right) => MinPairwise(left, right);

        /// <summary>
        /// svuint32_t svminp[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svminp[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> MinPairwise(Vector<uint> left, Vector<uint> right) => MinPairwise(left, right);

        /// <summary>
        /// svuint64_t svminp[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svminp[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> MinPairwise(Vector<ulong> left, Vector<ulong> right) => MinPairwise(left, right);

        /// <summary>
        /// svfloat32_t svminp[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svminp[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> MinPairwise(Vector<float> left, Vector<float> right) => MinPairwise(left, right);

        /// <summary>
        /// svfloat64_t svminp[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svminp[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> MinPairwise(Vector<double> left, Vector<double> right) => MinPairwise(left, right);


        ///  MoveWideningLower : Move long (bottom)

        /// <summary>
        /// svint16_t svmovlb[_s16](svint8_t op)
        /// </summary>
        public static unsafe Vector<short> MoveWideningLower(Vector<sbyte> value) => MoveWideningLower(value);

        /// <summary>
        /// svint32_t svmovlb[_s32](svint16_t op)
        /// </summary>
        public static unsafe Vector<int> MoveWideningLower(Vector<short> value) => MoveWideningLower(value);

        /// <summary>
        /// svint64_t svmovlb[_s64](svint32_t op)
        /// </summary>
        public static unsafe Vector<long> MoveWideningLower(Vector<int> value) => MoveWideningLower(value);

        /// <summary>
        /// svuint16_t svmovlb[_u16](svuint8_t op)
        /// </summary>
        public static unsafe Vector<ushort> MoveWideningLower(Vector<byte> value) => MoveWideningLower(value);

        /// <summary>
        /// svuint32_t svmovlb[_u32](svuint16_t op)
        /// </summary>
        public static unsafe Vector<uint> MoveWideningLower(Vector<ushort> value) => MoveWideningLower(value);

        /// <summary>
        /// svuint64_t svmovlb[_u64](svuint32_t op)
        /// </summary>
        public static unsafe Vector<ulong> MoveWideningLower(Vector<uint> value) => MoveWideningLower(value);


        ///  MoveWideningUpper : Move long (top)

        /// <summary>
        /// svint16_t svmovlt[_s16](svint8_t op)
        /// </summary>
        public static unsafe Vector<short> MoveWideningUpper(Vector<sbyte> value) => MoveWideningUpper(value);

        /// <summary>
        /// svint32_t svmovlt[_s32](svint16_t op)
        /// </summary>
        public static unsafe Vector<int> MoveWideningUpper(Vector<short> value) => MoveWideningUpper(value);

        /// <summary>
        /// svint64_t svmovlt[_s64](svint32_t op)
        /// </summary>
        public static unsafe Vector<long> MoveWideningUpper(Vector<int> value) => MoveWideningUpper(value);

        /// <summary>
        /// svuint16_t svmovlt[_u16](svuint8_t op)
        /// </summary>
        public static unsafe Vector<ushort> MoveWideningUpper(Vector<byte> value) => MoveWideningUpper(value);

        /// <summary>
        /// svuint32_t svmovlt[_u32](svuint16_t op)
        /// </summary>
        public static unsafe Vector<uint> MoveWideningUpper(Vector<ushort> value) => MoveWideningUpper(value);

        /// <summary>
        /// svuint64_t svmovlt[_u64](svuint32_t op)
        /// </summary>
        public static unsafe Vector<ulong> MoveWideningUpper(Vector<uint> value) => MoveWideningUpper(value);


        ///  MultiplyAddBySelectedScalar : Multiply-add, addend first

        /// <summary>
        /// svint16_t svmla_lane[_s16](svint16_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<short> MultiplyAddBySelectedScalar(Vector<short> addend, Vector<short> left, Vector<short> right, [ConstantExpected] byte rightIndex) => MultiplyAddBySelectedScalar(addend, left, right, rightIndex);

        /// <summary>
        /// svint32_t svmla_lane[_s32](svint32_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<int> MultiplyAddBySelectedScalar(Vector<int> addend, Vector<int> left, Vector<int> right, [ConstantExpected] byte rightIndex) => MultiplyAddBySelectedScalar(addend, left, right, rightIndex);

        /// <summary>
        /// svint64_t svmla_lane[_s64](svint64_t op1, svint64_t op2, svint64_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<long> MultiplyAddBySelectedScalar(Vector<long> addend, Vector<long> left, Vector<long> right, [ConstantExpected] byte rightIndex) => MultiplyAddBySelectedScalar(addend, left, right, rightIndex);

        /// <summary>
        /// svuint16_t svmla_lane[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<ushort> MultiplyAddBySelectedScalar(Vector<ushort> addend, Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte rightIndex) => MultiplyAddBySelectedScalar(addend, left, right, rightIndex);

        /// <summary>
        /// svuint32_t svmla_lane[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<uint> MultiplyAddBySelectedScalar(Vector<uint> addend, Vector<uint> left, Vector<uint> right, [ConstantExpected] byte rightIndex) => MultiplyAddBySelectedScalar(addend, left, right, rightIndex);

        /// <summary>
        /// svuint64_t svmla_lane[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<ulong> MultiplyAddBySelectedScalar(Vector<ulong> addend, Vector<ulong> left, Vector<ulong> right, [ConstantExpected] byte rightIndex) => MultiplyAddBySelectedScalar(addend, left, right, rightIndex);


        ///  MultiplyAddRotateComplex : Complex multiply-add with rotate

        /// <summary>
        /// svint8_t svcmla[_s8](svint8_t op1, svint8_t op2, svint8_t op3, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<sbyte> MultiplyAddRotateComplex(Vector<sbyte> addend, Vector<sbyte> left, Vector<sbyte> right, [ConstantExpected] byte rotation) => MultiplyAddRotateComplex(addend, left, right, rotation);

        /// <summary>
        /// svint16_t svcmla[_s16](svint16_t op1, svint16_t op2, svint16_t op3, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<short> MultiplyAddRotateComplex(Vector<short> addend, Vector<short> left, Vector<short> right, [ConstantExpected] byte rotation) => MultiplyAddRotateComplex(addend, left, right, rotation);

        /// <summary>
        /// svint32_t svcmla[_s32](svint32_t op1, svint32_t op2, svint32_t op3, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<int> MultiplyAddRotateComplex(Vector<int> addend, Vector<int> left, Vector<int> right, [ConstantExpected] byte rotation) => MultiplyAddRotateComplex(addend, left, right, rotation);

        /// <summary>
        /// svint64_t svcmla[_s64](svint64_t op1, svint64_t op2, svint64_t op3, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<long> MultiplyAddRotateComplex(Vector<long> addend, Vector<long> left, Vector<long> right, [ConstantExpected] byte rotation) => MultiplyAddRotateComplex(addend, left, right, rotation);

        /// <summary>
        /// svuint8_t svcmla[_u8](svuint8_t op1, svuint8_t op2, svuint8_t op3, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<byte> MultiplyAddRotateComplex(Vector<byte> addend, Vector<byte> left, Vector<byte> right, [ConstantExpected] byte rotation) => MultiplyAddRotateComplex(addend, left, right, rotation);

        /// <summary>
        /// svuint16_t svcmla[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<ushort> MultiplyAddRotateComplex(Vector<ushort> addend, Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte rotation) => MultiplyAddRotateComplex(addend, left, right, rotation);

        /// <summary>
        /// svuint32_t svcmla[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<uint> MultiplyAddRotateComplex(Vector<uint> addend, Vector<uint> left, Vector<uint> right, [ConstantExpected] byte rotation) => MultiplyAddRotateComplex(addend, left, right, rotation);

        /// <summary>
        /// svuint64_t svcmla[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<ulong> MultiplyAddRotateComplex(Vector<ulong> addend, Vector<ulong> left, Vector<ulong> right, [ConstantExpected] byte rotation) => MultiplyAddRotateComplex(addend, left, right, rotation);


        ///  MultiplyAddRotateComplexBySelectedScalar : Complex multiply-add with rotate

        /// <summary>
        /// svint16_t svcmla_lane[_s16](svint16_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<short> MultiplyAddRotateComplexBySelectedScalar(Vector<short> addend, Vector<short> left, Vector<short> right, [ConstantExpected] byte rightIndex, [ConstantExpected] byte rotation) => MultiplyAddRotateComplexBySelectedScalar(addend, left, right, rightIndex, rotation);

        /// <summary>
        /// svint32_t svcmla_lane[_s32](svint32_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<int> MultiplyAddRotateComplexBySelectedScalar(Vector<int> addend, Vector<int> left, Vector<int> right, [ConstantExpected] byte rightIndex, [ConstantExpected] byte rotation) => MultiplyAddRotateComplexBySelectedScalar(addend, left, right, rightIndex, rotation);

        /// <summary>
        /// svuint16_t svcmla_lane[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3, uint64_t imm_index, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<ushort> MultiplyAddRotateComplexBySelectedScalar(Vector<ushort> addend, Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte rightIndex, [ConstantExpected] byte rotation) => MultiplyAddRotateComplexBySelectedScalar(addend, left, right, rightIndex, rotation);

        /// <summary>
        /// svuint32_t svcmla_lane[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3, uint64_t imm_index, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<uint> MultiplyAddRotateComplexBySelectedScalar(Vector<uint> addend, Vector<uint> left, Vector<uint> right, [ConstantExpected] byte rightIndex, [ConstantExpected] byte rotation) => MultiplyAddRotateComplexBySelectedScalar(addend, left, right, rightIndex, rotation);


        ///  MultiplyAddWideningLower : Multiply-add long (bottom)

        /// <summary>
        /// svint16_t svmlalb[_s16](svint16_t op1, svint8_t op2, svint8_t op3)
        /// </summary>
        public static unsafe Vector<short> MultiplyAddWideningLower(Vector<short> op1, Vector<sbyte> op2, Vector<sbyte> op3) => MultiplyAddWideningLower(op1, op2, op3);

        /// <summary>
        /// svint32_t svmlalb[_s32](svint32_t op1, svint16_t op2, svint16_t op3)
        /// </summary>
        public static unsafe Vector<int> MultiplyAddWideningLower(Vector<int> op1, Vector<short> op2, Vector<short> op3) => MultiplyAddWideningLower(op1, op2, op3);

        /// <summary>
        /// svint32_t svmlalb_lane[_s32](svint32_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<int> MultiplyAddWideningLower(Vector<int> op1, Vector<short> op2, Vector<short> op3, ulong imm_index) => MultiplyAddWideningLower(op1, op2, op3, imm_index);

        /// <summary>
        /// svint64_t svmlalb[_s64](svint64_t op1, svint32_t op2, svint32_t op3)
        /// </summary>
        public static unsafe Vector<long> MultiplyAddWideningLower(Vector<long> op1, Vector<int> op2, Vector<int> op3) => MultiplyAddWideningLower(op1, op2, op3);

        /// <summary>
        /// svint64_t svmlalb_lane[_s64](svint64_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<long> MultiplyAddWideningLower(Vector<long> op1, Vector<int> op2, Vector<int> op3, ulong imm_index) => MultiplyAddWideningLower(op1, op2, op3, imm_index);

        /// <summary>
        /// svuint16_t svmlalb[_u16](svuint16_t op1, svuint8_t op2, svuint8_t op3)
        /// </summary>
        public static unsafe Vector<ushort> MultiplyAddWideningLower(Vector<ushort> op1, Vector<byte> op2, Vector<byte> op3) => MultiplyAddWideningLower(op1, op2, op3);

        /// <summary>
        /// svuint32_t svmlalb[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3)
        /// </summary>
        public static unsafe Vector<uint> MultiplyAddWideningLower(Vector<uint> op1, Vector<ushort> op2, Vector<ushort> op3) => MultiplyAddWideningLower(op1, op2, op3);

        /// <summary>
        /// svuint32_t svmlalb_lane[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<uint> MultiplyAddWideningLower(Vector<uint> op1, Vector<ushort> op2, Vector<ushort> op3, ulong imm_index) => MultiplyAddWideningLower(op1, op2, op3, imm_index);

        /// <summary>
        /// svuint64_t svmlalb[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3)
        /// </summary>
        public static unsafe Vector<ulong> MultiplyAddWideningLower(Vector<ulong> op1, Vector<uint> op2, Vector<uint> op3) => MultiplyAddWideningLower(op1, op2, op3);

        /// <summary>
        /// svuint64_t svmlalb_lane[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<ulong> MultiplyAddWideningLower(Vector<ulong> op1, Vector<uint> op2, Vector<uint> op3, ulong imm_index) => MultiplyAddWideningLower(op1, op2, op3, imm_index);


        ///  MultiplyAddWideningUpper : Multiply-add long (top)

        /// <summary>
        /// svint16_t svmlalt[_s16](svint16_t op1, svint8_t op2, svint8_t op3)
        /// </summary>
        public static unsafe Vector<short> MultiplyAddWideningUpper(Vector<short> op1, Vector<sbyte> op2, Vector<sbyte> op3) => MultiplyAddWideningUpper(op1, op2, op3);

        /// <summary>
        /// svint32_t svmlalt[_s32](svint32_t op1, svint16_t op2, svint16_t op3)
        /// </summary>
        public static unsafe Vector<int> MultiplyAddWideningUpper(Vector<int> op1, Vector<short> op2, Vector<short> op3) => MultiplyAddWideningUpper(op1, op2, op3);

        /// <summary>
        /// svint32_t svmlalt_lane[_s32](svint32_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<int> MultiplyAddWideningUpper(Vector<int> op1, Vector<short> op2, Vector<short> op3, ulong imm_index) => MultiplyAddWideningUpper(op1, op2, op3, imm_index);

        /// <summary>
        /// svint64_t svmlalt[_s64](svint64_t op1, svint32_t op2, svint32_t op3)
        /// </summary>
        public static unsafe Vector<long> MultiplyAddWideningUpper(Vector<long> op1, Vector<int> op2, Vector<int> op3) => MultiplyAddWideningUpper(op1, op2, op3);

        /// <summary>
        /// svint64_t svmlalt_lane[_s64](svint64_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<long> MultiplyAddWideningUpper(Vector<long> op1, Vector<int> op2, Vector<int> op3, ulong imm_index) => MultiplyAddWideningUpper(op1, op2, op3, imm_index);

        /// <summary>
        /// svuint16_t svmlalt[_u16](svuint16_t op1, svuint8_t op2, svuint8_t op3)
        /// </summary>
        public static unsafe Vector<ushort> MultiplyAddWideningUpper(Vector<ushort> op1, Vector<byte> op2, Vector<byte> op3) => MultiplyAddWideningUpper(op1, op2, op3);

        /// <summary>
        /// svuint32_t svmlalt[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3)
        /// </summary>
        public static unsafe Vector<uint> MultiplyAddWideningUpper(Vector<uint> op1, Vector<ushort> op2, Vector<ushort> op3) => MultiplyAddWideningUpper(op1, op2, op3);

        /// <summary>
        /// svuint32_t svmlalt_lane[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<uint> MultiplyAddWideningUpper(Vector<uint> op1, Vector<ushort> op2, Vector<ushort> op3, ulong imm_index) => MultiplyAddWideningUpper(op1, op2, op3, imm_index);

        /// <summary>
        /// svuint64_t svmlalt[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3)
        /// </summary>
        public static unsafe Vector<ulong> MultiplyAddWideningUpper(Vector<ulong> op1, Vector<uint> op2, Vector<uint> op3) => MultiplyAddWideningUpper(op1, op2, op3);

        /// <summary>
        /// svuint64_t svmlalt_lane[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<ulong> MultiplyAddWideningUpper(Vector<ulong> op1, Vector<uint> op2, Vector<uint> op3, ulong imm_index) => MultiplyAddWideningUpper(op1, op2, op3, imm_index);


        ///  MultiplyBySelectedScalar : Multiply

        /// <summary>
        /// svint16_t svmul_lane[_s16](svint16_t op1, svint16_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<short> MultiplyBySelectedScalar(Vector<short> left, Vector<short> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalar(left, right, rightIndex);

        /// <summary>
        /// svint32_t svmul_lane[_s32](svint32_t op1, svint32_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<int> MultiplyBySelectedScalar(Vector<int> left, Vector<int> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalar(left, right, rightIndex);

        /// <summary>
        /// svint64_t svmul_lane[_s64](svint64_t op1, svint64_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<long> MultiplyBySelectedScalar(Vector<long> left, Vector<long> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalar(left, right, rightIndex);

        /// <summary>
        /// svuint16_t svmul_lane[_u16](svuint16_t op1, svuint16_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<ushort> MultiplyBySelectedScalar(Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalar(left, right, rightIndex);

        /// <summary>
        /// svuint32_t svmul_lane[_u32](svuint32_t op1, svuint32_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<uint> MultiplyBySelectedScalar(Vector<uint> left, Vector<uint> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalar(left, right, rightIndex);

        /// <summary>
        /// svuint64_t svmul_lane[_u64](svuint64_t op1, svuint64_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<ulong> MultiplyBySelectedScalar(Vector<ulong> left, Vector<ulong> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalar(left, right, rightIndex);


        ///  MultiplySubtractBySelectedScalar : Multiply-subtract, minuend first

        /// <summary>
        /// svint16_t svmls_lane[_s16](svint16_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<short> MultiplySubtractBySelectedScalar(Vector<short> minuend, Vector<short> left, Vector<short> right, [ConstantExpected] byte rightIndex) => MultiplySubtractBySelectedScalar(minuend, left, right, rightIndex);

        /// <summary>
        /// svint32_t svmls_lane[_s32](svint32_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<int> MultiplySubtractBySelectedScalar(Vector<int> minuend, Vector<int> left, Vector<int> right, [ConstantExpected] byte rightIndex) => MultiplySubtractBySelectedScalar(minuend, left, right, rightIndex);

        /// <summary>
        /// svint64_t svmls_lane[_s64](svint64_t op1, svint64_t op2, svint64_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<long> MultiplySubtractBySelectedScalar(Vector<long> minuend, Vector<long> left, Vector<long> right, [ConstantExpected] byte rightIndex) => MultiplySubtractBySelectedScalar(minuend, left, right, rightIndex);

        /// <summary>
        /// svuint16_t svmls_lane[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<ushort> MultiplySubtractBySelectedScalar(Vector<ushort> minuend, Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte rightIndex) => MultiplySubtractBySelectedScalar(minuend, left, right, rightIndex);

        /// <summary>
        /// svuint32_t svmls_lane[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<uint> MultiplySubtractBySelectedScalar(Vector<uint> minuend, Vector<uint> left, Vector<uint> right, [ConstantExpected] byte rightIndex) => MultiplySubtractBySelectedScalar(minuend, left, right, rightIndex);

        /// <summary>
        /// svuint64_t svmls_lane[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<ulong> MultiplySubtractBySelectedScalar(Vector<ulong> minuend, Vector<ulong> left, Vector<ulong> right, [ConstantExpected] byte rightIndex) => MultiplySubtractBySelectedScalar(minuend, left, right, rightIndex);


        ///  MultiplySubtractWideningLower : Multiply-subtract long (bottom)

        /// <summary>
        /// svint16_t svmlslb[_s16](svint16_t op1, svint8_t op2, svint8_t op3)
        /// </summary>
        public static unsafe Vector<short> MultiplySubtractWideningLower(Vector<short> op1, Vector<sbyte> op2, Vector<sbyte> op3) => MultiplySubtractWideningLower(op1, op2, op3);

        /// <summary>
        /// svint32_t svmlslb[_s32](svint32_t op1, svint16_t op2, svint16_t op3)
        /// </summary>
        public static unsafe Vector<int> MultiplySubtractWideningLower(Vector<int> op1, Vector<short> op2, Vector<short> op3) => MultiplySubtractWideningLower(op1, op2, op3);

        /// <summary>
        /// svint32_t svmlslb_lane[_s32](svint32_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<int> MultiplySubtractWideningLower(Vector<int> op1, Vector<short> op2, Vector<short> op3, ulong imm_index) => MultiplySubtractWideningLower(op1, op2, op3, imm_index);

        /// <summary>
        /// svint64_t svmlslb[_s64](svint64_t op1, svint32_t op2, svint32_t op3)
        /// </summary>
        public static unsafe Vector<long> MultiplySubtractWideningLower(Vector<long> op1, Vector<int> op2, Vector<int> op3) => MultiplySubtractWideningLower(op1, op2, op3);

        /// <summary>
        /// svint64_t svmlslb_lane[_s64](svint64_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<long> MultiplySubtractWideningLower(Vector<long> op1, Vector<int> op2, Vector<int> op3, ulong imm_index) => MultiplySubtractWideningLower(op1, op2, op3, imm_index);

        /// <summary>
        /// svuint16_t svmlslb[_u16](svuint16_t op1, svuint8_t op2, svuint8_t op3)
        /// </summary>
        public static unsafe Vector<ushort> MultiplySubtractWideningLower(Vector<ushort> op1, Vector<byte> op2, Vector<byte> op3) => MultiplySubtractWideningLower(op1, op2, op3);

        /// <summary>
        /// svuint32_t svmlslb[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3)
        /// </summary>
        public static unsafe Vector<uint> MultiplySubtractWideningLower(Vector<uint> op1, Vector<ushort> op2, Vector<ushort> op3) => MultiplySubtractWideningLower(op1, op2, op3);

        /// <summary>
        /// svuint32_t svmlslb_lane[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<uint> MultiplySubtractWideningLower(Vector<uint> op1, Vector<ushort> op2, Vector<ushort> op3, ulong imm_index) => MultiplySubtractWideningLower(op1, op2, op3, imm_index);

        /// <summary>
        /// svuint64_t svmlslb[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3)
        /// </summary>
        public static unsafe Vector<ulong> MultiplySubtractWideningLower(Vector<ulong> op1, Vector<uint> op2, Vector<uint> op3) => MultiplySubtractWideningLower(op1, op2, op3);

        /// <summary>
        /// svuint64_t svmlslb_lane[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<ulong> MultiplySubtractWideningLower(Vector<ulong> op1, Vector<uint> op2, Vector<uint> op3, ulong imm_index) => MultiplySubtractWideningLower(op1, op2, op3, imm_index);


        ///  MultiplySubtractWideningUpper : Multiply-subtract long (top)

        /// <summary>
        /// svint16_t svmlslt[_s16](svint16_t op1, svint8_t op2, svint8_t op3)
        /// </summary>
        public static unsafe Vector<short> MultiplySubtractWideningUpper(Vector<short> op1, Vector<sbyte> op2, Vector<sbyte> op3) => MultiplySubtractWideningUpper(op1, op2, op3);

        /// <summary>
        /// svint32_t svmlslt[_s32](svint32_t op1, svint16_t op2, svint16_t op3)
        /// </summary>
        public static unsafe Vector<int> MultiplySubtractWideningUpper(Vector<int> op1, Vector<short> op2, Vector<short> op3) => MultiplySubtractWideningUpper(op1, op2, op3);

        /// <summary>
        /// svint32_t svmlslt_lane[_s32](svint32_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<int> MultiplySubtractWideningUpper(Vector<int> op1, Vector<short> op2, Vector<short> op3, ulong imm_index) => MultiplySubtractWideningUpper(op1, op2, op3, imm_index);

        /// <summary>
        /// svint64_t svmlslt[_s64](svint64_t op1, svint32_t op2, svint32_t op3)
        /// </summary>
        public static unsafe Vector<long> MultiplySubtractWideningUpper(Vector<long> op1, Vector<int> op2, Vector<int> op3) => MultiplySubtractWideningUpper(op1, op2, op3);

        /// <summary>
        /// svint64_t svmlslt_lane[_s64](svint64_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<long> MultiplySubtractWideningUpper(Vector<long> op1, Vector<int> op2, Vector<int> op3, ulong imm_index) => MultiplySubtractWideningUpper(op1, op2, op3, imm_index);

        /// <summary>
        /// svuint16_t svmlslt[_u16](svuint16_t op1, svuint8_t op2, svuint8_t op3)
        /// </summary>
        public static unsafe Vector<ushort> MultiplySubtractWideningUpper(Vector<ushort> op1, Vector<byte> op2, Vector<byte> op3) => MultiplySubtractWideningUpper(op1, op2, op3);

        /// <summary>
        /// svuint32_t svmlslt[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3)
        /// </summary>
        public static unsafe Vector<uint> MultiplySubtractWideningUpper(Vector<uint> op1, Vector<ushort> op2, Vector<ushort> op3) => MultiplySubtractWideningUpper(op1, op2, op3);

        /// <summary>
        /// svuint32_t svmlslt_lane[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<uint> MultiplySubtractWideningUpper(Vector<uint> op1, Vector<ushort> op2, Vector<ushort> op3, ulong imm_index) => MultiplySubtractWideningUpper(op1, op2, op3, imm_index);

        /// <summary>
        /// svuint64_t svmlslt[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3)
        /// </summary>
        public static unsafe Vector<ulong> MultiplySubtractWideningUpper(Vector<ulong> op1, Vector<uint> op2, Vector<uint> op3) => MultiplySubtractWideningUpper(op1, op2, op3);

        /// <summary>
        /// svuint64_t svmlslt_lane[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<ulong> MultiplySubtractWideningUpper(Vector<ulong> op1, Vector<uint> op2, Vector<uint> op3, ulong imm_index) => MultiplySubtractWideningUpper(op1, op2, op3, imm_index);


        ///  MultiplyWideningLower : Multiply long (bottom)

        /// <summary>
        /// svint16_t svmullb[_s16](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<short> MultiplyWideningLower(Vector<sbyte> left, Vector<sbyte> right) => MultiplyWideningLower(left, right);

        /// <summary>
        /// svint32_t svmullb[_s32](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<int> MultiplyWideningLower(Vector<short> left, Vector<short> right) => MultiplyWideningLower(left, right);

        /// <summary>
        /// svint32_t svmullb_lane[_s32](svint16_t op1, svint16_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<int> MultiplyWideningLower(Vector<short> op1, Vector<short> op2, ulong imm_index) => MultiplyWideningLower(op1, op2, imm_index);

        /// <summary>
        /// svint64_t svmullb[_s64](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<long> MultiplyWideningLower(Vector<int> left, Vector<int> right) => MultiplyWideningLower(left, right);

        /// <summary>
        /// svint64_t svmullb_lane[_s64](svint32_t op1, svint32_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<long> MultiplyWideningLower(Vector<int> op1, Vector<int> op2, ulong imm_index) => MultiplyWideningLower(op1, op2, imm_index);

        /// <summary>
        /// svuint16_t svmullb[_u16](svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<ushort> MultiplyWideningLower(Vector<byte> left, Vector<byte> right) => MultiplyWideningLower(left, right);

        /// <summary>
        /// svuint32_t svmullb[_u32](svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<uint> MultiplyWideningLower(Vector<ushort> left, Vector<ushort> right) => MultiplyWideningLower(left, right);

        /// <summary>
        /// svuint32_t svmullb_lane[_u32](svuint16_t op1, svuint16_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<uint> MultiplyWideningLower(Vector<ushort> op1, Vector<ushort> op2, ulong imm_index) => MultiplyWideningLower(op1, op2, imm_index);

        /// <summary>
        /// svuint64_t svmullb[_u64](svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<ulong> MultiplyWideningLower(Vector<uint> left, Vector<uint> right) => MultiplyWideningLower(left, right);

        /// <summary>
        /// svuint64_t svmullb_lane[_u64](svuint32_t op1, svuint32_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<ulong> MultiplyWideningLower(Vector<uint> op1, Vector<uint> op2, ulong imm_index) => MultiplyWideningLower(op1, op2, imm_index);


        ///  MultiplyWideningUpper : Multiply long (top)

        /// <summary>
        /// svint16_t svmullt[_s16](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<short> MultiplyWideningUpper(Vector<sbyte> left, Vector<sbyte> right) => MultiplyWideningUpper(left, right);

        /// <summary>
        /// svint32_t svmullt[_s32](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<int> MultiplyWideningUpper(Vector<short> left, Vector<short> right) => MultiplyWideningUpper(left, right);

        /// <summary>
        /// svint32_t svmullt_lane[_s32](svint16_t op1, svint16_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<int> MultiplyWideningUpper(Vector<short> op1, Vector<short> op2, ulong imm_index) => MultiplyWideningUpper(op1, op2, imm_index);

        /// <summary>
        /// svint64_t svmullt[_s64](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<long> MultiplyWideningUpper(Vector<int> left, Vector<int> right) => MultiplyWideningUpper(left, right);

        /// <summary>
        /// svint64_t svmullt_lane[_s64](svint32_t op1, svint32_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<long> MultiplyWideningUpper(Vector<int> op1, Vector<int> op2, ulong imm_index) => MultiplyWideningUpper(op1, op2, imm_index);

        /// <summary>
        /// svuint16_t svmullt[_u16](svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<ushort> MultiplyWideningUpper(Vector<byte> left, Vector<byte> right) => MultiplyWideningUpper(left, right);

        /// <summary>
        /// svuint32_t svmullt[_u32](svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<uint> MultiplyWideningUpper(Vector<ushort> left, Vector<ushort> right) => MultiplyWideningUpper(left, right);

        /// <summary>
        /// svuint32_t svmullt_lane[_u32](svuint16_t op1, svuint16_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<uint> MultiplyWideningUpper(Vector<ushort> op1, Vector<ushort> op2, ulong imm_index) => MultiplyWideningUpper(op1, op2, imm_index);

        /// <summary>
        /// svuint64_t svmullt[_u64](svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<ulong> MultiplyWideningUpper(Vector<uint> left, Vector<uint> right) => MultiplyWideningUpper(left, right);

        /// <summary>
        /// svuint64_t svmullt_lane[_u64](svuint32_t op1, svuint32_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<ulong> MultiplyWideningUpper(Vector<uint> op1, Vector<uint> op2, ulong imm_index) => MultiplyWideningUpper(op1, op2, imm_index);


        ///  NoMatch : Detect no matching elements

        /// <summary>
        /// svbool_t svnmatch[_s8](svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> NoMatch(Vector<sbyte> mask, Vector<sbyte> left, Vector<sbyte> right) => NoMatch(mask, left, right);

        /// <summary>
        /// svbool_t svnmatch[_s16](svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> NoMatch(Vector<short> mask, Vector<short> left, Vector<short> right) => NoMatch(mask, left, right);

        /// <summary>
        /// svbool_t svnmatch[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> NoMatch(Vector<byte> mask, Vector<byte> left, Vector<byte> right) => NoMatch(mask, left, right);

        /// <summary>
        /// svbool_t svnmatch[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> NoMatch(Vector<ushort> mask, Vector<ushort> left, Vector<ushort> right) => NoMatch(mask, left, right);


        ///  PolynomialMultiply : Polynomial multiply

        /// <summary>
        /// svuint8_t svpmul[_u8](svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> PolynomialMultiply(Vector<byte> left, Vector<byte> right) => PolynomialMultiply(left, right);


        ///  PolynomialMultiplyWideningLower : Polynomial multiply long (bottom)

        /// <summary>
        /// svuint8_t svpmullb_pair[_u8](svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> PolynomialMultiplyWideningLower(Vector<byte> left, Vector<byte> right) => PolynomialMultiplyWideningLower(left, right);

        /// <summary>
        /// svuint16_t svpmullb[_u16](svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<ushort> PolynomialMultiplyWideningLower(Vector<byte> left, Vector<byte> right) => PolynomialMultiplyWideningLower(left, right);

        /// <summary>
        /// svuint32_t svpmullb_pair[_u32](svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> PolynomialMultiplyWideningLower(Vector<uint> left, Vector<uint> right) => PolynomialMultiplyWideningLower(left, right);

        /// <summary>
        /// svuint64_t svpmullb[_u64](svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<ulong> PolynomialMultiplyWideningLower(Vector<uint> left, Vector<uint> right) => PolynomialMultiplyWideningLower(left, right);


        ///  PolynomialMultiplyWideningUpper : Polynomial multiply long (top)

        /// <summary>
        /// svuint8_t svpmullt_pair[_u8](svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> PolynomialMultiplyWideningUpper(Vector<byte> left, Vector<byte> right) => PolynomialMultiplyWideningUpper(left, right);

        /// <summary>
        /// svuint16_t svpmullt[_u16](svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<ushort> PolynomialMultiplyWideningUpper(Vector<byte> left, Vector<byte> right) => PolynomialMultiplyWideningUpper(left, right);

        /// <summary>
        /// svuint32_t svpmullt_pair[_u32](svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> PolynomialMultiplyWideningUpper(Vector<uint> left, Vector<uint> right) => PolynomialMultiplyWideningUpper(left, right);

        /// <summary>
        /// svuint64_t svpmullt[_u64](svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<ulong> PolynomialMultiplyWideningUpper(Vector<uint> left, Vector<uint> right) => PolynomialMultiplyWideningUpper(left, right);


        ///  ReciprocalEstimate : Reciprocal estimate

        /// <summary>
        /// svuint32_t svrecpe[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        /// svuint32_t svrecpe[_u32]_x(svbool_t pg, svuint32_t op)
        /// svuint32_t svrecpe[_u32]_z(svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> ReciprocalEstimate(Vector<uint> value) => ReciprocalEstimate(value);


        ///  ReciprocalSqrtEstimate : Reciprocal square root estimate

        /// <summary>
        /// svuint32_t svrsqrte[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        /// svuint32_t svrsqrte[_u32]_x(svbool_t pg, svuint32_t op)
        /// svuint32_t svrsqrte[_u32]_z(svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> ReciprocalSqrtEstimate(Vector<uint> value) => ReciprocalSqrtEstimate(value);


        ///  RoundingAddHighNarowingLower : Rounding add narrow high part (bottom)

        /// <summary>
        /// svint8_t svraddhnb[_s16](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> RoundingAddHighNarowingLower(Vector<short> left, Vector<short> right) => RoundingAddHighNarowingLower(left, right);

        /// <summary>
        /// svint16_t svraddhnb[_s32](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<short> RoundingAddHighNarowingLower(Vector<int> left, Vector<int> right) => RoundingAddHighNarowingLower(left, right);

        /// <summary>
        /// svint32_t svraddhnb[_s64](svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<int> RoundingAddHighNarowingLower(Vector<long> left, Vector<long> right) => RoundingAddHighNarowingLower(left, right);

        /// <summary>
        /// svuint8_t svraddhnb[_u16](svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<byte> RoundingAddHighNarowingLower(Vector<ushort> left, Vector<ushort> right) => RoundingAddHighNarowingLower(left, right);

        /// <summary>
        /// svuint16_t svraddhnb[_u32](svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<ushort> RoundingAddHighNarowingLower(Vector<uint> left, Vector<uint> right) => RoundingAddHighNarowingLower(left, right);

        /// <summary>
        /// svuint32_t svraddhnb[_u64](svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<uint> RoundingAddHighNarowingLower(Vector<ulong> left, Vector<ulong> right) => RoundingAddHighNarowingLower(left, right);


        ///  RoundingAddHighNarowingUpper : Rounding add narrow high part (top)

        /// <summary>
        /// svint8_t svraddhnt[_s16](svint8_t even, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> RoundingAddHighNarowingUpper(Vector<sbyte> even, Vector<short> left, Vector<short> right) => RoundingAddHighNarowingUpper(even, left, right);

        /// <summary>
        /// svint16_t svraddhnt[_s32](svint16_t even, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<short> RoundingAddHighNarowingUpper(Vector<short> even, Vector<int> left, Vector<int> right) => RoundingAddHighNarowingUpper(even, left, right);

        /// <summary>
        /// svint32_t svraddhnt[_s64](svint32_t even, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<int> RoundingAddHighNarowingUpper(Vector<int> even, Vector<long> left, Vector<long> right) => RoundingAddHighNarowingUpper(even, left, right);

        /// <summary>
        /// svuint8_t svraddhnt[_u16](svuint8_t even, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<byte> RoundingAddHighNarowingUpper(Vector<byte> even, Vector<ushort> left, Vector<ushort> right) => RoundingAddHighNarowingUpper(even, left, right);

        /// <summary>
        /// svuint16_t svraddhnt[_u32](svuint16_t even, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<ushort> RoundingAddHighNarowingUpper(Vector<ushort> even, Vector<uint> left, Vector<uint> right) => RoundingAddHighNarowingUpper(even, left, right);

        /// <summary>
        /// svuint32_t svraddhnt[_u64](svuint32_t even, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<uint> RoundingAddHighNarowingUpper(Vector<uint> even, Vector<ulong> left, Vector<ulong> right) => RoundingAddHighNarowingUpper(even, left, right);


        ///  RoundingHalvingAdd : Rounding halving add

        /// <summary>
        /// svint8_t svrhadd[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svrhadd[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svrhadd[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> RoundingHalvingAdd(Vector<sbyte> left, Vector<sbyte> right) => RoundingHalvingAdd(left, right);

        /// <summary>
        /// svint16_t svrhadd[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svrhadd[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svrhadd[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> RoundingHalvingAdd(Vector<short> left, Vector<short> right) => RoundingHalvingAdd(left, right);

        /// <summary>
        /// svint32_t svrhadd[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svrhadd[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svrhadd[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> RoundingHalvingAdd(Vector<int> left, Vector<int> right) => RoundingHalvingAdd(left, right);

        /// <summary>
        /// svint64_t svrhadd[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svrhadd[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svrhadd[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> RoundingHalvingAdd(Vector<long> left, Vector<long> right) => RoundingHalvingAdd(left, right);

        /// <summary>
        /// svuint8_t svrhadd[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svrhadd[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svrhadd[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> RoundingHalvingAdd(Vector<byte> left, Vector<byte> right) => RoundingHalvingAdd(left, right);

        /// <summary>
        /// svuint16_t svrhadd[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svrhadd[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svrhadd[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> RoundingHalvingAdd(Vector<ushort> left, Vector<ushort> right) => RoundingHalvingAdd(left, right);

        /// <summary>
        /// svuint32_t svrhadd[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svrhadd[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svrhadd[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> RoundingHalvingAdd(Vector<uint> left, Vector<uint> right) => RoundingHalvingAdd(left, right);

        /// <summary>
        /// svuint64_t svrhadd[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svrhadd[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svrhadd[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> RoundingHalvingAdd(Vector<ulong> left, Vector<ulong> right) => RoundingHalvingAdd(left, right);


        ///  RoundingSubtractHighNarowingLower : Rounding subtract narrow high part (bottom)

        /// <summary>
        /// svint8_t svrsubhnb[_s16](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> RoundingSubtractHighNarowingLower(Vector<short> left, Vector<short> right) => RoundingSubtractHighNarowingLower(left, right);

        /// <summary>
        /// svint16_t svrsubhnb[_s32](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<short> RoundingSubtractHighNarowingLower(Vector<int> left, Vector<int> right) => RoundingSubtractHighNarowingLower(left, right);

        /// <summary>
        /// svint32_t svrsubhnb[_s64](svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<int> RoundingSubtractHighNarowingLower(Vector<long> left, Vector<long> right) => RoundingSubtractHighNarowingLower(left, right);

        /// <summary>
        /// svuint8_t svrsubhnb[_u16](svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<byte> RoundingSubtractHighNarowingLower(Vector<ushort> left, Vector<ushort> right) => RoundingSubtractHighNarowingLower(left, right);

        /// <summary>
        /// svuint16_t svrsubhnb[_u32](svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<ushort> RoundingSubtractHighNarowingLower(Vector<uint> left, Vector<uint> right) => RoundingSubtractHighNarowingLower(left, right);

        /// <summary>
        /// svuint32_t svrsubhnb[_u64](svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<uint> RoundingSubtractHighNarowingLower(Vector<ulong> left, Vector<ulong> right) => RoundingSubtractHighNarowingLower(left, right);


        ///  RoundingSubtractHighNarowingUpper : Rounding subtract narrow high part (top)

        /// <summary>
        /// svint8_t svrsubhnt[_s16](svint8_t even, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> RoundingSubtractHighNarowingUpper(Vector<sbyte> even, Vector<short> left, Vector<short> right) => RoundingSubtractHighNarowingUpper(even, left, right);

        /// <summary>
        /// svint16_t svrsubhnt[_s32](svint16_t even, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<short> RoundingSubtractHighNarowingUpper(Vector<short> even, Vector<int> left, Vector<int> right) => RoundingSubtractHighNarowingUpper(even, left, right);

        /// <summary>
        /// svint32_t svrsubhnt[_s64](svint32_t even, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<int> RoundingSubtractHighNarowingUpper(Vector<int> even, Vector<long> left, Vector<long> right) => RoundingSubtractHighNarowingUpper(even, left, right);

        /// <summary>
        /// svuint8_t svrsubhnt[_u16](svuint8_t even, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<byte> RoundingSubtractHighNarowingUpper(Vector<byte> even, Vector<ushort> left, Vector<ushort> right) => RoundingSubtractHighNarowingUpper(even, left, right);

        /// <summary>
        /// svuint16_t svrsubhnt[_u32](svuint16_t even, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<ushort> RoundingSubtractHighNarowingUpper(Vector<ushort> even, Vector<uint> left, Vector<uint> right) => RoundingSubtractHighNarowingUpper(even, left, right);

        /// <summary>
        /// svuint32_t svrsubhnt[_u64](svuint32_t even, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<uint> RoundingSubtractHighNarowingUpper(Vector<uint> even, Vector<ulong> left, Vector<ulong> right) => RoundingSubtractHighNarowingUpper(even, left, right);


        ///  SaturatingAbs : Saturating absolute value

        /// <summary>
        /// svint8_t svqabs[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op)
        /// svint8_t svqabs[_s8]_x(svbool_t pg, svint8_t op)
        /// svint8_t svqabs[_s8]_z(svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<sbyte> SaturatingAbs(Vector<sbyte> value) => SaturatingAbs(value);

        /// <summary>
        /// svint16_t svqabs[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        /// svint16_t svqabs[_s16]_x(svbool_t pg, svint16_t op)
        /// svint16_t svqabs[_s16]_z(svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<short> SaturatingAbs(Vector<short> value) => SaturatingAbs(value);

        /// <summary>
        /// svint32_t svqabs[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        /// svint32_t svqabs[_s32]_x(svbool_t pg, svint32_t op)
        /// svint32_t svqabs[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> SaturatingAbs(Vector<int> value) => SaturatingAbs(value);

        /// <summary>
        /// svint64_t svqabs[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        /// svint64_t svqabs[_s64]_x(svbool_t pg, svint64_t op)
        /// svint64_t svqabs[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> SaturatingAbs(Vector<long> value) => SaturatingAbs(value);


        ///  SaturatingComplexAddRotate : Saturating complex add with rotate

        /// <summary>
        /// svint8_t svqcadd[_s8](svint8_t op1, svint8_t op2, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<sbyte> SaturatingComplexAddRotate(Vector<sbyte> op1, Vector<sbyte> op2, [ConstantExpected] byte rotation) => SaturatingComplexAddRotate(op1, op2, rotation);

        /// <summary>
        /// svint16_t svqcadd[_s16](svint16_t op1, svint16_t op2, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<short> SaturatingComplexAddRotate(Vector<short> op1, Vector<short> op2, [ConstantExpected] byte rotation) => SaturatingComplexAddRotate(op1, op2, rotation);

        /// <summary>
        /// svint32_t svqcadd[_s32](svint32_t op1, svint32_t op2, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<int> SaturatingComplexAddRotate(Vector<int> op1, Vector<int> op2, [ConstantExpected] byte rotation) => SaturatingComplexAddRotate(op1, op2, rotation);

        /// <summary>
        /// svint64_t svqcadd[_s64](svint64_t op1, svint64_t op2, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<long> SaturatingComplexAddRotate(Vector<long> op1, Vector<long> op2, [ConstantExpected] byte rotation) => SaturatingComplexAddRotate(op1, op2, rotation);


        ///  SaturatingDoublingMultiplyAddWideningLower : Saturating doubling multiply-add long (bottom)

        /// <summary>
        /// svint16_t svqdmlalb[_s16](svint16_t op1, svint8_t op2, svint8_t op3)
        /// </summary>
        public static unsafe Vector<short> SaturatingDoublingMultiplyAddWideningLower(Vector<short> op1, Vector<sbyte> op2, Vector<sbyte> op3) => SaturatingDoublingMultiplyAddWideningLower(op1, op2, op3);

        /// <summary>
        /// svint32_t svqdmlalb[_s32](svint32_t op1, svint16_t op2, svint16_t op3)
        /// </summary>
        public static unsafe Vector<int> SaturatingDoublingMultiplyAddWideningLower(Vector<int> op1, Vector<short> op2, Vector<short> op3) => SaturatingDoublingMultiplyAddWideningLower(op1, op2, op3);

        /// <summary>
        /// svint32_t svqdmlalb_lane[_s32](svint32_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<int> SaturatingDoublingMultiplyAddWideningLower(Vector<int> op1, Vector<short> op2, Vector<short> op3, ulong imm_index) => SaturatingDoublingMultiplyAddWideningLower(op1, op2, op3, imm_index);

        /// <summary>
        /// svint64_t svqdmlalb[_s64](svint64_t op1, svint32_t op2, svint32_t op3)
        /// </summary>
        public static unsafe Vector<long> SaturatingDoublingMultiplyAddWideningLower(Vector<long> op1, Vector<int> op2, Vector<int> op3) => SaturatingDoublingMultiplyAddWideningLower(op1, op2, op3);

        /// <summary>
        /// svint64_t svqdmlalb_lane[_s64](svint64_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<long> SaturatingDoublingMultiplyAddWideningLower(Vector<long> op1, Vector<int> op2, Vector<int> op3, ulong imm_index) => SaturatingDoublingMultiplyAddWideningLower(op1, op2, op3, imm_index);


        ///  SaturatingDoublingMultiplyAddWideningLowerUpper : Saturating doubling multiply-add long (bottom × top)

        /// <summary>
        /// svint16_t svqdmlalbt[_s16](svint16_t op1, svint8_t op2, svint8_t op3)
        /// </summary>
        public static unsafe Vector<short> SaturatingDoublingMultiplyAddWideningLowerUpper(Vector<short> op1, Vector<sbyte> op2, Vector<sbyte> op3) => SaturatingDoublingMultiplyAddWideningLowerUpper(op1, op2, op3);

        /// <summary>
        /// svint32_t svqdmlalbt[_s32](svint32_t op1, svint16_t op2, svint16_t op3)
        /// </summary>
        public static unsafe Vector<int> SaturatingDoublingMultiplyAddWideningLowerUpper(Vector<int> op1, Vector<short> op2, Vector<short> op3) => SaturatingDoublingMultiplyAddWideningLowerUpper(op1, op2, op3);

        /// <summary>
        /// svint64_t svqdmlalbt[_s64](svint64_t op1, svint32_t op2, svint32_t op3)
        /// </summary>
        public static unsafe Vector<long> SaturatingDoublingMultiplyAddWideningLowerUpper(Vector<long> op1, Vector<int> op2, Vector<int> op3) => SaturatingDoublingMultiplyAddWideningLowerUpper(op1, op2, op3);


        ///  SaturatingDoublingMultiplyAddWideningUpper : Saturating doubling multiply-add long (top)

        /// <summary>
        /// svint16_t svqdmlalt[_s16](svint16_t op1, svint8_t op2, svint8_t op3)
        /// </summary>
        public static unsafe Vector<short> SaturatingDoublingMultiplyAddWideningUpper(Vector<short> op1, Vector<sbyte> op2, Vector<sbyte> op3) => SaturatingDoublingMultiplyAddWideningUpper(op1, op2, op3);

        /// <summary>
        /// svint32_t svqdmlalt[_s32](svint32_t op1, svint16_t op2, svint16_t op3)
        /// </summary>
        public static unsafe Vector<int> SaturatingDoublingMultiplyAddWideningUpper(Vector<int> op1, Vector<short> op2, Vector<short> op3) => SaturatingDoublingMultiplyAddWideningUpper(op1, op2, op3);

        /// <summary>
        /// svint32_t svqdmlalt_lane[_s32](svint32_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<int> SaturatingDoublingMultiplyAddWideningUpper(Vector<int> op1, Vector<short> op2, Vector<short> op3, ulong imm_index) => SaturatingDoublingMultiplyAddWideningUpper(op1, op2, op3, imm_index);

        /// <summary>
        /// svint64_t svqdmlalt[_s64](svint64_t op1, svint32_t op2, svint32_t op3)
        /// </summary>
        public static unsafe Vector<long> SaturatingDoublingMultiplyAddWideningUpper(Vector<long> op1, Vector<int> op2, Vector<int> op3) => SaturatingDoublingMultiplyAddWideningUpper(op1, op2, op3);

        /// <summary>
        /// svint64_t svqdmlalt_lane[_s64](svint64_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<long> SaturatingDoublingMultiplyAddWideningUpper(Vector<long> op1, Vector<int> op2, Vector<int> op3, ulong imm_index) => SaturatingDoublingMultiplyAddWideningUpper(op1, op2, op3, imm_index);


        ///  SaturatingDoublingMultiplyHigh : Saturating doubling multiply high

        /// <summary>
        /// svint8_t svqdmulh[_s8](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> SaturatingDoublingMultiplyHigh(Vector<sbyte> left, Vector<sbyte> right) => SaturatingDoublingMultiplyHigh(left, right);

        /// <summary>
        /// svint16_t svqdmulh[_s16](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> SaturatingDoublingMultiplyHigh(Vector<short> left, Vector<short> right) => SaturatingDoublingMultiplyHigh(left, right);

        /// <summary>
        /// svint16_t svqdmulh_lane[_s16](svint16_t op1, svint16_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<short> SaturatingDoublingMultiplyHigh(Vector<short> op1, Vector<short> op2, ulong imm_index) => SaturatingDoublingMultiplyHigh(op1, op2, imm_index);

        /// <summary>
        /// svint32_t svqdmulh[_s32](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> SaturatingDoublingMultiplyHigh(Vector<int> left, Vector<int> right) => SaturatingDoublingMultiplyHigh(left, right);

        /// <summary>
        /// svint32_t svqdmulh_lane[_s32](svint32_t op1, svint32_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<int> SaturatingDoublingMultiplyHigh(Vector<int> op1, Vector<int> op2, ulong imm_index) => SaturatingDoublingMultiplyHigh(op1, op2, imm_index);

        /// <summary>
        /// svint64_t svqdmulh[_s64](svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> SaturatingDoublingMultiplyHigh(Vector<long> left, Vector<long> right) => SaturatingDoublingMultiplyHigh(left, right);

        /// <summary>
        /// svint64_t svqdmulh_lane[_s64](svint64_t op1, svint64_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<long> SaturatingDoublingMultiplyHigh(Vector<long> op1, Vector<long> op2, ulong imm_index) => SaturatingDoublingMultiplyHigh(op1, op2, imm_index);


        ///  SaturatingDoublingMultiplySubtractWideningLower : Saturating doubling multiply-subtract long (bottom)

        /// <summary>
        /// svint16_t svqdmlslb[_s16](svint16_t op1, svint8_t op2, svint8_t op3)
        /// </summary>
        public static unsafe Vector<short> SaturatingDoublingMultiplySubtractWideningLower(Vector<short> op1, Vector<sbyte> op2, Vector<sbyte> op3) => SaturatingDoublingMultiplySubtractWideningLower(op1, op2, op3);

        /// <summary>
        /// svint32_t svqdmlslb[_s32](svint32_t op1, svint16_t op2, svint16_t op3)
        /// </summary>
        public static unsafe Vector<int> SaturatingDoublingMultiplySubtractWideningLower(Vector<int> op1, Vector<short> op2, Vector<short> op3) => SaturatingDoublingMultiplySubtractWideningLower(op1, op2, op3);

        /// <summary>
        /// svint32_t svqdmlslb_lane[_s32](svint32_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<int> SaturatingDoublingMultiplySubtractWideningLower(Vector<int> op1, Vector<short> op2, Vector<short> op3, ulong imm_index) => SaturatingDoublingMultiplySubtractWideningLower(op1, op2, op3, imm_index);

        /// <summary>
        /// svint64_t svqdmlslb[_s64](svint64_t op1, svint32_t op2, svint32_t op3)
        /// </summary>
        public static unsafe Vector<long> SaturatingDoublingMultiplySubtractWideningLower(Vector<long> op1, Vector<int> op2, Vector<int> op3) => SaturatingDoublingMultiplySubtractWideningLower(op1, op2, op3);

        /// <summary>
        /// svint64_t svqdmlslb_lane[_s64](svint64_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<long> SaturatingDoublingMultiplySubtractWideningLower(Vector<long> op1, Vector<int> op2, Vector<int> op3, ulong imm_index) => SaturatingDoublingMultiplySubtractWideningLower(op1, op2, op3, imm_index);


        ///  SaturatingDoublingMultiplySubtractWideningLowerUpper : Saturating doubling multiply-subtract long (bottom × top)

        /// <summary>
        /// svint16_t svqdmlslbt[_s16](svint16_t op1, svint8_t op2, svint8_t op3)
        /// </summary>
        public static unsafe Vector<short> SaturatingDoublingMultiplySubtractWideningLowerUpper(Vector<short> op1, Vector<sbyte> op2, Vector<sbyte> op3) => SaturatingDoublingMultiplySubtractWideningLowerUpper(op1, op2, op3);

        /// <summary>
        /// svint32_t svqdmlslbt[_s32](svint32_t op1, svint16_t op2, svint16_t op3)
        /// </summary>
        public static unsafe Vector<int> SaturatingDoublingMultiplySubtractWideningLowerUpper(Vector<int> op1, Vector<short> op2, Vector<short> op3) => SaturatingDoublingMultiplySubtractWideningLowerUpper(op1, op2, op3);

        /// <summary>
        /// svint64_t svqdmlslbt[_s64](svint64_t op1, svint32_t op2, svint32_t op3)
        /// </summary>
        public static unsafe Vector<long> SaturatingDoublingMultiplySubtractWideningLowerUpper(Vector<long> op1, Vector<int> op2, Vector<int> op3) => SaturatingDoublingMultiplySubtractWideningLowerUpper(op1, op2, op3);


        ///  SaturatingDoublingMultiplySubtractWideningUpper : Saturating doubling multiply-subtract long (top)

        /// <summary>
        /// svint16_t svqdmlslt[_s16](svint16_t op1, svint8_t op2, svint8_t op3)
        /// </summary>
        public static unsafe Vector<short> SaturatingDoublingMultiplySubtractWideningUpper(Vector<short> op1, Vector<sbyte> op2, Vector<sbyte> op3) => SaturatingDoublingMultiplySubtractWideningUpper(op1, op2, op3);

        /// <summary>
        /// svint32_t svqdmlslt[_s32](svint32_t op1, svint16_t op2, svint16_t op3)
        /// </summary>
        public static unsafe Vector<int> SaturatingDoublingMultiplySubtractWideningUpper(Vector<int> op1, Vector<short> op2, Vector<short> op3) => SaturatingDoublingMultiplySubtractWideningUpper(op1, op2, op3);

        /// <summary>
        /// svint32_t svqdmlslt_lane[_s32](svint32_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<int> SaturatingDoublingMultiplySubtractWideningUpper(Vector<int> op1, Vector<short> op2, Vector<short> op3, ulong imm_index) => SaturatingDoublingMultiplySubtractWideningUpper(op1, op2, op3, imm_index);

        /// <summary>
        /// svint64_t svqdmlslt[_s64](svint64_t op1, svint32_t op2, svint32_t op3)
        /// </summary>
        public static unsafe Vector<long> SaturatingDoublingMultiplySubtractWideningUpper(Vector<long> op1, Vector<int> op2, Vector<int> op3) => SaturatingDoublingMultiplySubtractWideningUpper(op1, op2, op3);

        /// <summary>
        /// svint64_t svqdmlslt_lane[_s64](svint64_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<long> SaturatingDoublingMultiplySubtractWideningUpper(Vector<long> op1, Vector<int> op2, Vector<int> op3, ulong imm_index) => SaturatingDoublingMultiplySubtractWideningUpper(op1, op2, op3, imm_index);


        ///  SaturatingDoublingMultiplyWideningLower : Saturating doubling multiply long (bottom)

        /// <summary>
        /// svint16_t svqdmullb[_s16](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<short> SaturatingDoublingMultiplyWideningLower(Vector<sbyte> left, Vector<sbyte> right) => SaturatingDoublingMultiplyWideningLower(left, right);

        /// <summary>
        /// svint32_t svqdmullb[_s32](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<int> SaturatingDoublingMultiplyWideningLower(Vector<short> left, Vector<short> right) => SaturatingDoublingMultiplyWideningLower(left, right);

        /// <summary>
        /// svint32_t svqdmullb_lane[_s32](svint16_t op1, svint16_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<int> SaturatingDoublingMultiplyWideningLower(Vector<short> op1, Vector<short> op2, ulong imm_index) => SaturatingDoublingMultiplyWideningLower(op1, op2, imm_index);

        /// <summary>
        /// svint64_t svqdmullb[_s64](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<long> SaturatingDoublingMultiplyWideningLower(Vector<int> left, Vector<int> right) => SaturatingDoublingMultiplyWideningLower(left, right);

        /// <summary>
        /// svint64_t svqdmullb_lane[_s64](svint32_t op1, svint32_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<long> SaturatingDoublingMultiplyWideningLower(Vector<int> op1, Vector<int> op2, ulong imm_index) => SaturatingDoublingMultiplyWideningLower(op1, op2, imm_index);


        ///  SaturatingDoublingMultiplyWideningUpper : Saturating doubling multiply long (top)

        /// <summary>
        /// svint16_t svqdmullt[_s16](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<short> SaturatingDoublingMultiplyWideningUpper(Vector<sbyte> left, Vector<sbyte> right) => SaturatingDoublingMultiplyWideningUpper(left, right);

        /// <summary>
        /// svint32_t svqdmullt[_s32](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<int> SaturatingDoublingMultiplyWideningUpper(Vector<short> left, Vector<short> right) => SaturatingDoublingMultiplyWideningUpper(left, right);

        /// <summary>
        /// svint32_t svqdmullt_lane[_s32](svint16_t op1, svint16_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<int> SaturatingDoublingMultiplyWideningUpper(Vector<short> op1, Vector<short> op2, ulong imm_index) => SaturatingDoublingMultiplyWideningUpper(op1, op2, imm_index);

        /// <summary>
        /// svint64_t svqdmullt[_s64](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<long> SaturatingDoublingMultiplyWideningUpper(Vector<int> left, Vector<int> right) => SaturatingDoublingMultiplyWideningUpper(left, right);

        /// <summary>
        /// svint64_t svqdmullt_lane[_s64](svint32_t op1, svint32_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<long> SaturatingDoublingMultiplyWideningUpper(Vector<int> op1, Vector<int> op2, ulong imm_index) => SaturatingDoublingMultiplyWideningUpper(op1, op2, imm_index);


        ///  SaturatingExtractNarrowingLower : Saturating extract narrow (bottom)

        /// <summary>
        /// svint8_t svqxtnb[_s16](svint16_t op)
        /// </summary>
        public static unsafe Vector<sbyte> SaturatingExtractNarrowingLower(Vector<short> value) => SaturatingExtractNarrowingLower(value);

        /// <summary>
        /// svint16_t svqxtnb[_s32](svint32_t op)
        /// </summary>
        public static unsafe Vector<short> SaturatingExtractNarrowingLower(Vector<int> value) => SaturatingExtractNarrowingLower(value);

        /// <summary>
        /// svint32_t svqxtnb[_s64](svint64_t op)
        /// </summary>
        public static unsafe Vector<int> SaturatingExtractNarrowingLower(Vector<long> value) => SaturatingExtractNarrowingLower(value);

        /// <summary>
        /// svuint8_t svqxtnb[_u16](svuint16_t op)
        /// </summary>
        public static unsafe Vector<byte> SaturatingExtractNarrowingLower(Vector<ushort> value) => SaturatingExtractNarrowingLower(value);

        /// <summary>
        /// svuint16_t svqxtnb[_u32](svuint32_t op)
        /// </summary>
        public static unsafe Vector<ushort> SaturatingExtractNarrowingLower(Vector<uint> value) => SaturatingExtractNarrowingLower(value);

        /// <summary>
        /// svuint32_t svqxtnb[_u64](svuint64_t op)
        /// </summary>
        public static unsafe Vector<uint> SaturatingExtractNarrowingLower(Vector<ulong> value) => SaturatingExtractNarrowingLower(value);


        ///  SaturatingExtractNarrowingUpper : Saturating extract narrow (top)

        /// <summary>
        /// svint8_t svqxtnt[_s16](svint8_t even, svint16_t op)
        /// </summary>
        public static unsafe Vector<sbyte> SaturatingExtractNarrowingUpper(Vector<sbyte> even, Vector<short> op) => SaturatingExtractNarrowingUpper(even, op);

        /// <summary>
        /// svint16_t svqxtnt[_s32](svint16_t even, svint32_t op)
        /// </summary>
        public static unsafe Vector<short> SaturatingExtractNarrowingUpper(Vector<short> even, Vector<int> op) => SaturatingExtractNarrowingUpper(even, op);

        /// <summary>
        /// svint32_t svqxtnt[_s64](svint32_t even, svint64_t op)
        /// </summary>
        public static unsafe Vector<int> SaturatingExtractNarrowingUpper(Vector<int> even, Vector<long> op) => SaturatingExtractNarrowingUpper(even, op);

        /// <summary>
        /// svuint8_t svqxtnt[_u16](svuint8_t even, svuint16_t op)
        /// </summary>
        public static unsafe Vector<byte> SaturatingExtractNarrowingUpper(Vector<byte> even, Vector<ushort> op) => SaturatingExtractNarrowingUpper(even, op);

        /// <summary>
        /// svuint16_t svqxtnt[_u32](svuint16_t even, svuint32_t op)
        /// </summary>
        public static unsafe Vector<ushort> SaturatingExtractNarrowingUpper(Vector<ushort> even, Vector<uint> op) => SaturatingExtractNarrowingUpper(even, op);

        /// <summary>
        /// svuint32_t svqxtnt[_u64](svuint32_t even, svuint64_t op)
        /// </summary>
        public static unsafe Vector<uint> SaturatingExtractNarrowingUpper(Vector<uint> even, Vector<ulong> op) => SaturatingExtractNarrowingUpper(even, op);


        ///  SaturatingExtractUnsignedNarrowingLower : Saturating extract unsigned narrow (bottom)

        /// <summary>
        /// svuint8_t svqxtunb[_s16](svint16_t op)
        /// </summary>
        public static unsafe Vector<byte> SaturatingExtractUnsignedNarrowingLower(Vector<short> value) => SaturatingExtractUnsignedNarrowingLower(value);

        /// <summary>
        /// svuint16_t svqxtunb[_s32](svint32_t op)
        /// </summary>
        public static unsafe Vector<ushort> SaturatingExtractUnsignedNarrowingLower(Vector<int> value) => SaturatingExtractUnsignedNarrowingLower(value);

        /// <summary>
        /// svuint32_t svqxtunb[_s64](svint64_t op)
        /// </summary>
        public static unsafe Vector<uint> SaturatingExtractUnsignedNarrowingLower(Vector<long> value) => SaturatingExtractUnsignedNarrowingLower(value);


        ///  SaturatingExtractUnsignedNarrowingUpper : Saturating extract unsigned narrow (top)

        /// <summary>
        /// svuint8_t svqxtunt[_s16](svuint8_t even, svint16_t op)
        /// </summary>
        public static unsafe Vector<byte> SaturatingExtractUnsignedNarrowingUpper(Vector<byte> even, Vector<short> op) => SaturatingExtractUnsignedNarrowingUpper(even, op);

        /// <summary>
        /// svuint16_t svqxtunt[_s32](svuint16_t even, svint32_t op)
        /// </summary>
        public static unsafe Vector<ushort> SaturatingExtractUnsignedNarrowingUpper(Vector<ushort> even, Vector<int> op) => SaturatingExtractUnsignedNarrowingUpper(even, op);

        /// <summary>
        /// svuint32_t svqxtunt[_s64](svuint32_t even, svint64_t op)
        /// </summary>
        public static unsafe Vector<uint> SaturatingExtractUnsignedNarrowingUpper(Vector<uint> even, Vector<long> op) => SaturatingExtractUnsignedNarrowingUpper(even, op);


        ///  SaturatingNegate : Saturating negate

        /// <summary>
        /// svint8_t svqneg[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op)
        /// svint8_t svqneg[_s8]_x(svbool_t pg, svint8_t op)
        /// svint8_t svqneg[_s8]_z(svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<sbyte> SaturatingNegate(Vector<sbyte> value) => SaturatingNegate(value);

        /// <summary>
        /// svint16_t svqneg[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        /// svint16_t svqneg[_s16]_x(svbool_t pg, svint16_t op)
        /// svint16_t svqneg[_s16]_z(svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<short> SaturatingNegate(Vector<short> value) => SaturatingNegate(value);

        /// <summary>
        /// svint32_t svqneg[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        /// svint32_t svqneg[_s32]_x(svbool_t pg, svint32_t op)
        /// svint32_t svqneg[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> SaturatingNegate(Vector<int> value) => SaturatingNegate(value);

        /// <summary>
        /// svint64_t svqneg[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        /// svint64_t svqneg[_s64]_x(svbool_t pg, svint64_t op)
        /// svint64_t svqneg[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> SaturatingNegate(Vector<long> value) => SaturatingNegate(value);


        ///  SaturatingRoundingDoublingComplexMultiplyAddHighRotate : Saturating rounding doubling complex multiply-add high with rotate

        /// <summary>
        /// svint8_t svqrdcmlah[_s8](svint8_t op1, svint8_t op2, svint8_t op3, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<sbyte> SaturatingRoundingDoublingComplexMultiplyAddHighRotate(Vector<sbyte> op1, Vector<sbyte> op2, Vector<sbyte> op3, [ConstantExpected] byte rotation) => SaturatingRoundingDoublingComplexMultiplyAddHighRotate(op1, op2, op3, rotation);

        /// <summary>
        /// svint16_t svqrdcmlah[_s16](svint16_t op1, svint16_t op2, svint16_t op3, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<short> SaturatingRoundingDoublingComplexMultiplyAddHighRotate(Vector<short> op1, Vector<short> op2, Vector<short> op3, [ConstantExpected] byte rotation) => SaturatingRoundingDoublingComplexMultiplyAddHighRotate(op1, op2, op3, rotation);

        /// <summary>
        /// svint16_t svqrdcmlah_lane[_s16](svint16_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<short> SaturatingRoundingDoublingComplexMultiplyAddHighRotate(Vector<short> op1, Vector<short> op2, Vector<short> op3, ulong imm_index, [ConstantExpected] byte rotation) => SaturatingRoundingDoublingComplexMultiplyAddHighRotate(op1, op2, op3, imm_index, rotation);

        /// <summary>
        /// svint32_t svqrdcmlah[_s32](svint32_t op1, svint32_t op2, svint32_t op3, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<int> SaturatingRoundingDoublingComplexMultiplyAddHighRotate(Vector<int> op1, Vector<int> op2, Vector<int> op3, [ConstantExpected] byte rotation) => SaturatingRoundingDoublingComplexMultiplyAddHighRotate(op1, op2, op3, rotation);

        /// <summary>
        /// svint32_t svqrdcmlah_lane[_s32](svint32_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<int> SaturatingRoundingDoublingComplexMultiplyAddHighRotate(Vector<int> op1, Vector<int> op2, Vector<int> op3, ulong imm_index, [ConstantExpected] byte rotation) => SaturatingRoundingDoublingComplexMultiplyAddHighRotate(op1, op2, op3, imm_index, rotation);

        /// <summary>
        /// svint64_t svqrdcmlah[_s64](svint64_t op1, svint64_t op2, svint64_t op3, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<long> SaturatingRoundingDoublingComplexMultiplyAddHighRotate(Vector<long> op1, Vector<long> op2, Vector<long> op3, [ConstantExpected] byte rotation) => SaturatingRoundingDoublingComplexMultiplyAddHighRotate(op1, op2, op3, rotation);


        ///  SaturatingRoundingDoublingMultiplyAddHigh : Saturating rounding doubling multiply-add high

        /// <summary>
        /// svint8_t svqrdmlah[_s8](svint8_t op1, svint8_t op2, svint8_t op3)
        /// </summary>
        public static unsafe Vector<sbyte> SaturatingRoundingDoublingMultiplyAddHigh(Vector<sbyte> op1, Vector<sbyte> op2, Vector<sbyte> op3) => SaturatingRoundingDoublingMultiplyAddHigh(op1, op2, op3);

        /// <summary>
        /// svint16_t svqrdmlah[_s16](svint16_t op1, svint16_t op2, svint16_t op3)
        /// </summary>
        public static unsafe Vector<short> SaturatingRoundingDoublingMultiplyAddHigh(Vector<short> op1, Vector<short> op2, Vector<short> op3) => SaturatingRoundingDoublingMultiplyAddHigh(op1, op2, op3);

        /// <summary>
        /// svint16_t svqrdmlah_lane[_s16](svint16_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<short> SaturatingRoundingDoublingMultiplyAddHigh(Vector<short> op1, Vector<short> op2, Vector<short> op3, ulong imm_index) => SaturatingRoundingDoublingMultiplyAddHigh(op1, op2, op3, imm_index);

        /// <summary>
        /// svint32_t svqrdmlah[_s32](svint32_t op1, svint32_t op2, svint32_t op3)
        /// </summary>
        public static unsafe Vector<int> SaturatingRoundingDoublingMultiplyAddHigh(Vector<int> op1, Vector<int> op2, Vector<int> op3) => SaturatingRoundingDoublingMultiplyAddHigh(op1, op2, op3);

        /// <summary>
        /// svint32_t svqrdmlah_lane[_s32](svint32_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<int> SaturatingRoundingDoublingMultiplyAddHigh(Vector<int> op1, Vector<int> op2, Vector<int> op3, ulong imm_index) => SaturatingRoundingDoublingMultiplyAddHigh(op1, op2, op3, imm_index);

        /// <summary>
        /// svint64_t svqrdmlah[_s64](svint64_t op1, svint64_t op2, svint64_t op3)
        /// </summary>
        public static unsafe Vector<long> SaturatingRoundingDoublingMultiplyAddHigh(Vector<long> op1, Vector<long> op2, Vector<long> op3) => SaturatingRoundingDoublingMultiplyAddHigh(op1, op2, op3);

        /// <summary>
        /// svint64_t svqrdmlah_lane[_s64](svint64_t op1, svint64_t op2, svint64_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<long> SaturatingRoundingDoublingMultiplyAddHigh(Vector<long> op1, Vector<long> op2, Vector<long> op3, ulong imm_index) => SaturatingRoundingDoublingMultiplyAddHigh(op1, op2, op3, imm_index);


        ///  SaturatingRoundingDoublingMultiplyHigh : Saturating rounding doubling multiply high

        /// <summary>
        /// svint8_t svqrdmulh[_s8](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> SaturatingRoundingDoublingMultiplyHigh(Vector<sbyte> left, Vector<sbyte> right) => SaturatingRoundingDoublingMultiplyHigh(left, right);

        /// <summary>
        /// svint16_t svqrdmulh[_s16](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> SaturatingRoundingDoublingMultiplyHigh(Vector<short> left, Vector<short> right) => SaturatingRoundingDoublingMultiplyHigh(left, right);

        /// <summary>
        /// svint16_t svqrdmulh_lane[_s16](svint16_t op1, svint16_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<short> SaturatingRoundingDoublingMultiplyHigh(Vector<short> op1, Vector<short> op2, ulong imm_index) => SaturatingRoundingDoublingMultiplyHigh(op1, op2, imm_index);

        /// <summary>
        /// svint32_t svqrdmulh[_s32](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> SaturatingRoundingDoublingMultiplyHigh(Vector<int> left, Vector<int> right) => SaturatingRoundingDoublingMultiplyHigh(left, right);

        /// <summary>
        /// svint32_t svqrdmulh_lane[_s32](svint32_t op1, svint32_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<int> SaturatingRoundingDoublingMultiplyHigh(Vector<int> op1, Vector<int> op2, ulong imm_index) => SaturatingRoundingDoublingMultiplyHigh(op1, op2, imm_index);

        /// <summary>
        /// svint64_t svqrdmulh[_s64](svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> SaturatingRoundingDoublingMultiplyHigh(Vector<long> left, Vector<long> right) => SaturatingRoundingDoublingMultiplyHigh(left, right);

        /// <summary>
        /// svint64_t svqrdmulh_lane[_s64](svint64_t op1, svint64_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<long> SaturatingRoundingDoublingMultiplyHigh(Vector<long> op1, Vector<long> op2, ulong imm_index) => SaturatingRoundingDoublingMultiplyHigh(op1, op2, imm_index);


        ///  SaturatingRoundingDoublingMultiplySubtractHigh : Saturating rounding doubling multiply-subtract high

        /// <summary>
        /// svint8_t svqrdmlsh[_s8](svint8_t op1, svint8_t op2, svint8_t op3)
        /// </summary>
        public static unsafe Vector<sbyte> SaturatingRoundingDoublingMultiplySubtractHigh(Vector<sbyte> op1, Vector<sbyte> op2, Vector<sbyte> op3) => SaturatingRoundingDoublingMultiplySubtractHigh(op1, op2, op3);

        /// <summary>
        /// svint16_t svqrdmlsh[_s16](svint16_t op1, svint16_t op2, svint16_t op3)
        /// </summary>
        public static unsafe Vector<short> SaturatingRoundingDoublingMultiplySubtractHigh(Vector<short> op1, Vector<short> op2, Vector<short> op3) => SaturatingRoundingDoublingMultiplySubtractHigh(op1, op2, op3);

        /// <summary>
        /// svint16_t svqrdmlsh_lane[_s16](svint16_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<short> SaturatingRoundingDoublingMultiplySubtractHigh(Vector<short> op1, Vector<short> op2, Vector<short> op3, ulong imm_index) => SaturatingRoundingDoublingMultiplySubtractHigh(op1, op2, op3, imm_index);

        /// <summary>
        /// svint32_t svqrdmlsh[_s32](svint32_t op1, svint32_t op2, svint32_t op3)
        /// </summary>
        public static unsafe Vector<int> SaturatingRoundingDoublingMultiplySubtractHigh(Vector<int> op1, Vector<int> op2, Vector<int> op3) => SaturatingRoundingDoublingMultiplySubtractHigh(op1, op2, op3);

        /// <summary>
        /// svint32_t svqrdmlsh_lane[_s32](svint32_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<int> SaturatingRoundingDoublingMultiplySubtractHigh(Vector<int> op1, Vector<int> op2, Vector<int> op3, ulong imm_index) => SaturatingRoundingDoublingMultiplySubtractHigh(op1, op2, op3, imm_index);

        /// <summary>
        /// svint64_t svqrdmlsh[_s64](svint64_t op1, svint64_t op2, svint64_t op3)
        /// </summary>
        public static unsafe Vector<long> SaturatingRoundingDoublingMultiplySubtractHigh(Vector<long> op1, Vector<long> op2, Vector<long> op3) => SaturatingRoundingDoublingMultiplySubtractHigh(op1, op2, op3);

        /// <summary>
        /// svint64_t svqrdmlsh_lane[_s64](svint64_t op1, svint64_t op2, svint64_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<long> SaturatingRoundingDoublingMultiplySubtractHigh(Vector<long> op1, Vector<long> op2, Vector<long> op3, ulong imm_index) => SaturatingRoundingDoublingMultiplySubtractHigh(op1, op2, op3, imm_index);


        ///  Scatter16BitNarrowing : Truncate to 16 bits and store, non-temporal

        /// <summary>
        /// void svstnt1h_scatter[_u32base_s32](svbool_t pg, svuint32_t bases, svint32_t data)
        /// </summary>
        public static unsafe void Scatter16BitNarrowing(Vector<int> mask, Vector<uint> addresses, Vector<int> data) => Scatter16BitNarrowing(mask, addresses, data);

        /// <summary>
        /// void svstnt1h_scatter[_u64base_s64](svbool_t pg, svuint64_t bases, svint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitNarrowing(Vector<long> mask, Vector<ulong> addresses, Vector<long> data) => Scatter16BitNarrowing(mask, addresses, data);

        /// <summary>
        /// void svstnt1h_scatter[_u32base_u32](svbool_t pg, svuint32_t bases, svuint32_t data)
        /// </summary>
        public static unsafe void Scatter16BitNarrowing(Vector<uint> mask, Vector<uint> addresses, Vector<uint> data) => Scatter16BitNarrowing(mask, addresses, data);

        /// <summary>
        /// void svstnt1h_scatter[_u64base_u64](svbool_t pg, svuint64_t bases, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitNarrowing(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data) => Scatter16BitNarrowing(mask, addresses, data);


        ///  Scatter16BitWithByteOffsetsNarrowing : Truncate to 16 bits and store, non-temporal

        /// <summary>
        /// void svstnt1h_scatter_[u32]offset[_s32](svbool_t pg, int16_t *base, svuint32_t offsets, svint32_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<int> mask, short* address, Vector<uint> offsets, Vector<int> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svstnt1h_scatter_[s64]offset[_s64](svbool_t pg, int16_t *base, svint64_t offsets, svint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, short* address, Vector<long> offsets, Vector<long> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svstnt1h_scatter_[u64]offset[_s64](svbool_t pg, int16_t *base, svuint64_t offsets, svint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, short* address, Vector<ulong> offsets, Vector<long> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svstnt1h_scatter_[s64]index[_s64](svbool_t pg, int16_t *base, svint64_t indices, svint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, short* address, Vector<long> indices, Vector<long> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, indices, data);

        /// <summary>
        /// void svstnt1h_scatter_[u64]index[_s64](svbool_t pg, int16_t *base, svuint64_t indices, svint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, short* address, Vector<ulong> indices, Vector<long> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, indices, data);

        /// <summary>
        /// void svstnt1h_scatter_[u32]offset[_u32](svbool_t pg, uint16_t *base, svuint32_t offsets, svuint32_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<uint> mask, ushort* address, Vector<uint> offsets, Vector<uint> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svstnt1h_scatter_[s64]offset[_u64](svbool_t pg, uint16_t *base, svint64_t offsets, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, ushort* address, Vector<long> offsets, Vector<ulong> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svstnt1h_scatter_[u64]offset[_u64](svbool_t pg, uint16_t *base, svuint64_t offsets, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, ushort* address, Vector<ulong> offsets, Vector<ulong> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svstnt1h_scatter_[s64]index[_u64](svbool_t pg, uint16_t *base, svint64_t indices, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, ushort* address, Vector<long> indices, Vector<ulong> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, indices, data);

        /// <summary>
        /// void svstnt1h_scatter_[u64]index[_u64](svbool_t pg, uint16_t *base, svuint64_t indices, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, ushort* address, Vector<ulong> indices, Vector<ulong> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, indices, data);


        ///  Scatter32BitNarrowing : Truncate to 32 bits and store, non-temporal

        /// <summary>
        /// void svstnt1w_scatter[_u64base_s64](svbool_t pg, svuint64_t bases, svint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitNarrowing(Vector<long> mask, Vector<ulong> addresses, Vector<long> data) => Scatter32BitNarrowing(mask, addresses, data);

        /// <summary>
        /// void svstnt1w_scatter[_u64base_u64](svbool_t pg, svuint64_t bases, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitNarrowing(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data) => Scatter32BitNarrowing(mask, addresses, data);


        ///  Scatter32BitWithByteOffsetsNarrowing : Truncate to 32 bits and store, non-temporal

        /// <summary>
        /// void svstnt1w_scatter_[s64]offset[_s64](svbool_t pg, int32_t *base, svint64_t offsets, svint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<long> offsets, Vector<long> data) => Scatter32BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svstnt1w_scatter_[u64]offset[_s64](svbool_t pg, int32_t *base, svuint64_t offsets, svint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<ulong> offsets, Vector<long> data) => Scatter32BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svstnt1w_scatter_[s64]index[_s64](svbool_t pg, int32_t *base, svint64_t indices, svint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<long> indices, Vector<long> data) => Scatter32BitWithByteOffsetsNarrowing(mask, address, indices, data);

        /// <summary>
        /// void svstnt1w_scatter_[u64]index[_s64](svbool_t pg, int32_t *base, svuint64_t indices, svint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<ulong> indices, Vector<long> data) => Scatter32BitWithByteOffsetsNarrowing(mask, address, indices, data);

        /// <summary>
        /// void svstnt1w_scatter_[s64]offset[_u64](svbool_t pg, uint32_t *base, svint64_t offsets, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<long> offsets, Vector<ulong> data) => Scatter32BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svstnt1w_scatter_[u64]offset[_u64](svbool_t pg, uint32_t *base, svuint64_t offsets, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<ulong> offsets, Vector<ulong> data) => Scatter32BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svstnt1w_scatter_[s64]index[_u64](svbool_t pg, uint32_t *base, svint64_t indices, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<long> indices, Vector<ulong> data) => Scatter32BitWithByteOffsetsNarrowing(mask, address, indices, data);

        /// <summary>
        /// void svstnt1w_scatter_[u64]index[_u64](svbool_t pg, uint32_t *base, svuint64_t indices, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<ulong> indices, Vector<ulong> data) => Scatter32BitWithByteOffsetsNarrowing(mask, address, indices, data);


        ///  Scatter8BitNarrowing : Truncate to 8 bits and store, non-temporal

        /// <summary>
        /// void svstnt1b_scatter[_u32base_s32](svbool_t pg, svuint32_t bases, svint32_t data)
        /// </summary>
        public static unsafe void Scatter8BitNarrowing(Vector<int> mask, Vector<uint> addresses, Vector<int> data) => Scatter8BitNarrowing(mask, addresses, data);

        /// <summary>
        /// void svstnt1b_scatter[_u64base_s64](svbool_t pg, svuint64_t bases, svint64_t data)
        /// </summary>
        public static unsafe void Scatter8BitNarrowing(Vector<long> mask, Vector<ulong> addresses, Vector<long> data) => Scatter8BitNarrowing(mask, addresses, data);

        /// <summary>
        /// void svstnt1b_scatter[_u32base_u32](svbool_t pg, svuint32_t bases, svuint32_t data)
        /// </summary>
        public static unsafe void Scatter8BitNarrowing(Vector<uint> mask, Vector<uint> addresses, Vector<uint> data) => Scatter8BitNarrowing(mask, addresses, data);

        /// <summary>
        /// void svstnt1b_scatter[_u64base_u64](svbool_t pg, svuint64_t bases, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter8BitNarrowing(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data) => Scatter8BitNarrowing(mask, addresses, data);


        ///  Scatter8BitWithByteOffsetsNarrowing : Truncate to 8 bits and store, non-temporal

        /// <summary>
        /// void svstnt1b_scatter_[u32]offset[_s32](svbool_t pg, int8_t *base, svuint32_t offsets, svint32_t data)
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<int> mask, sbyte* address, Vector<uint> offsets, Vector<int> data) => Scatter8BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svstnt1b_scatter_[s64]offset[_s64](svbool_t pg, int8_t *base, svint64_t offsets, svint64_t data)
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<long> mask, sbyte* address, Vector<long> offsets, Vector<long> data) => Scatter8BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svstnt1b_scatter_[u64]offset[_s64](svbool_t pg, int8_t *base, svuint64_t offsets, svint64_t data)
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<long> mask, sbyte* address, Vector<ulong> offsets, Vector<long> data) => Scatter8BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svstnt1b_scatter_[u32]offset[_u32](svbool_t pg, uint8_t *base, svuint32_t offsets, svuint32_t data)
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<uint> mask, byte* address, Vector<uint> offsets, Vector<uint> data) => Scatter8BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svstnt1b_scatter_[s64]offset[_u64](svbool_t pg, uint8_t *base, svint64_t offsets, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<ulong> mask, byte* address, Vector<long> offsets, Vector<ulong> data) => Scatter8BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svstnt1b_scatter_[u64]offset[_u64](svbool_t pg, uint8_t *base, svuint64_t offsets, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<ulong> mask, byte* address, Vector<ulong> offsets, Vector<ulong> data) => Scatter8BitWithByteOffsetsNarrowing(mask, address, offsets, data);


        ///  ScatterNonTemporal : Non-truncating store, non-temporal

        /// <summary>
        /// void svstnt1_scatter[_u32base_s32](svbool_t pg, svuint32_t bases, svint32_t data)
        /// </summary>
        public static unsafe void ScatterNonTemporal(Vector<int> mask, Vector<uint> addresses, Vector<int> data) => ScatterNonTemporal(mask, addresses, data);

        /// <summary>
        /// void svstnt1_scatter_[u32]offset[_s32](svbool_t pg, int32_t *base, svuint32_t offsets, svint32_t data)
        /// </summary>
        public static unsafe void ScatterNonTemporal(Vector<int> mask, int* base, Vector<uint> offsets, Vector<int> data) => ScatterNonTemporal(mask, base, offsets, data);

        /// <summary>
        /// void svstnt1_scatter[_u64base_s64](svbool_t pg, svuint64_t bases, svint64_t data)
        /// </summary>
        public static unsafe void ScatterNonTemporal(Vector<long> mask, Vector<ulong> addresses, Vector<long> data) => ScatterNonTemporal(mask, addresses, data);

        /// <summary>
        /// void svstnt1_scatter_[s64]offset[_s64](svbool_t pg, int64_t *base, svint64_t offsets, svint64_t data)
        /// </summary>
        public static unsafe void ScatterNonTemporal(Vector<long> mask, long* base, Vector<long> offsets, Vector<long> data) => ScatterNonTemporal(mask, base, offsets, data);

        /// <summary>
        /// void svstnt1_scatter_[u64]offset[_s64](svbool_t pg, int64_t *base, svuint64_t offsets, svint64_t data)
        /// </summary>
        public static unsafe void ScatterNonTemporal(Vector<long> mask, long* base, Vector<ulong> offsets, Vector<long> data) => ScatterNonTemporal(mask, base, offsets, data);

        /// <summary>
        /// void svstnt1_scatter_[s64]index[_s64](svbool_t pg, int64_t *base, svint64_t indices, svint64_t data)
        /// </summary>
        public static unsafe void ScatterNonTemporal(Vector<long> mask, long* base, Vector<long> indices, Vector<long> data) => ScatterNonTemporal(mask, base, indices, data);

        /// <summary>
        /// void svstnt1_scatter_[u64]index[_s64](svbool_t pg, int64_t *base, svuint64_t indices, svint64_t data)
        /// </summary>
        public static unsafe void ScatterNonTemporal(Vector<long> mask, long* base, Vector<ulong> indices, Vector<long> data) => ScatterNonTemporal(mask, base, indices, data);

        /// <summary>
        /// void svstnt1_scatter[_u32base_u32](svbool_t pg, svuint32_t bases, svuint32_t data)
        /// </summary>
        public static unsafe void ScatterNonTemporal(Vector<uint> mask, Vector<uint> addresses, Vector<uint> data) => ScatterNonTemporal(mask, addresses, data);

        /// <summary>
        /// void svstnt1_scatter_[u32]offset[_u32](svbool_t pg, uint32_t *base, svuint32_t offsets, svuint32_t data)
        /// </summary>
        public static unsafe void ScatterNonTemporal(Vector<uint> mask, uint* base, Vector<uint> offsets, Vector<uint> data) => ScatterNonTemporal(mask, base, offsets, data);

        /// <summary>
        /// void svstnt1_scatter[_u64base_u64](svbool_t pg, svuint64_t bases, svuint64_t data)
        /// </summary>
        public static unsafe void ScatterNonTemporal(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data) => ScatterNonTemporal(mask, addresses, data);

        /// <summary>
        /// void svstnt1_scatter_[s64]offset[_u64](svbool_t pg, uint64_t *base, svint64_t offsets, svuint64_t data)
        /// </summary>
        public static unsafe void ScatterNonTemporal(Vector<ulong> mask, ulong* base, Vector<long> offsets, Vector<ulong> data) => ScatterNonTemporal(mask, base, offsets, data);

        /// <summary>
        /// void svstnt1_scatter_[u64]offset[_u64](svbool_t pg, uint64_t *base, svuint64_t offsets, svuint64_t data)
        /// </summary>
        public static unsafe void ScatterNonTemporal(Vector<ulong> mask, ulong* base, Vector<ulong> offsets, Vector<ulong> data) => ScatterNonTemporal(mask, base, offsets, data);

        /// <summary>
        /// void svstnt1_scatter_[s64]index[_u64](svbool_t pg, uint64_t *base, svint64_t indices, svuint64_t data)
        /// </summary>
        public static unsafe void ScatterNonTemporal(Vector<ulong> mask, ulong* base, Vector<long> indices, Vector<ulong> data) => ScatterNonTemporal(mask, base, indices, data);

        /// <summary>
        /// void svstnt1_scatter_[u64]index[_u64](svbool_t pg, uint64_t *base, svuint64_t indices, svuint64_t data)
        /// </summary>
        public static unsafe void ScatterNonTemporal(Vector<ulong> mask, ulong* base, Vector<ulong> indices, Vector<ulong> data) => ScatterNonTemporal(mask, base, indices, data);

        /// <summary>
        /// void svstnt1_scatter[_u32base_f32](svbool_t pg, svuint32_t bases, svfloat32_t data)
        /// </summary>
        public static unsafe void ScatterNonTemporal(Vector<float> mask, Vector<uint> addresses, Vector<float> data) => ScatterNonTemporal(mask, addresses, data);

        /// <summary>
        /// void svstnt1_scatter_[u32]offset[_f32](svbool_t pg, float32_t *base, svuint32_t offsets, svfloat32_t data)
        /// </summary>
        public static unsafe void ScatterNonTemporal(Vector<float> mask, float* base, Vector<uint> offsets, Vector<float> data) => ScatterNonTemporal(mask, base, offsets, data);

        /// <summary>
        /// void svstnt1_scatter_[s64]offset[_f64](svbool_t pg, float64_t *base, svint64_t offsets, svfloat64_t data)
        /// </summary>
        public static unsafe void ScatterNonTemporal(Vector<double> mask, double* base, Vector<long> offsets, Vector<double> data) => ScatterNonTemporal(mask, base, offsets, data);

        /// <summary>
        /// void svstnt1_scatter_[s64]index[_f64](svbool_t pg, float64_t *base, svint64_t indices, svfloat64_t data)
        /// </summary>
        public static unsafe void ScatterNonTemporal(Vector<double> mask, double* base, Vector<long> indices, Vector<double> data) => ScatterNonTemporal(mask, base, indices, data);

        /// <summary>
        /// void svstnt1_scatter[_u64base_f64](svbool_t pg, svuint64_t bases, svfloat64_t data)
        /// </summary>
        public static unsafe void ScatterNonTemporal(Vector<double> mask, Vector<ulong> addresses, Vector<double> data) => ScatterNonTemporal(mask, addresses, data);

        /// <summary>
        /// void svstnt1_scatter_[u64]offset[_f64](svbool_t pg, float64_t *base, svuint64_t offsets, svfloat64_t data)
        /// </summary>
        public static unsafe void ScatterNonTemporal(Vector<double> mask, double* base, Vector<ulong> offsets, Vector<double> data) => ScatterNonTemporal(mask, base, offsets, data);

        /// <summary>
        /// void svstnt1_scatter_[u64]index[_f64](svbool_t pg, float64_t *base, svuint64_t indices, svfloat64_t data)
        /// </summary>
        public static unsafe void ScatterNonTemporal(Vector<double> mask, double* base, Vector<ulong> indices, Vector<double> data) => ScatterNonTemporal(mask, base, indices, data);


        ///  ShiftArithmeticRounded : Rounding shift left

        /// <summary>
        /// svint8_t svrshl[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svrshl[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svrshl[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftArithmeticRounded(Vector<sbyte> value, Vector<sbyte> count) => ShiftArithmeticRounded(value, count);

        /// <summary>
        /// svint16_t svrshl[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svrshl[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svrshl[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> ShiftArithmeticRounded(Vector<short> value, Vector<short> count) => ShiftArithmeticRounded(value, count);

        /// <summary>
        /// svint32_t svrshl[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svrshl[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svrshl[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> ShiftArithmeticRounded(Vector<int> value, Vector<int> count) => ShiftArithmeticRounded(value, count);

        /// <summary>
        /// svint64_t svrshl[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svrshl[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svrshl[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> ShiftArithmeticRounded(Vector<long> value, Vector<long> count) => ShiftArithmeticRounded(value, count);


        ///  ShiftArithmeticRoundedSaturate : Saturating rounding shift left

        /// <summary>
        /// svint8_t svqrshl[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svqrshl[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svqrshl[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftArithmeticRoundedSaturate(Vector<sbyte> value, Vector<sbyte> count) => ShiftArithmeticRoundedSaturate(value, count);

        /// <summary>
        /// svint16_t svqrshl[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svqrshl[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svqrshl[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> ShiftArithmeticRoundedSaturate(Vector<short> value, Vector<short> count) => ShiftArithmeticRoundedSaturate(value, count);

        /// <summary>
        /// svint32_t svqrshl[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svqrshl[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svqrshl[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> ShiftArithmeticRoundedSaturate(Vector<int> value, Vector<int> count) => ShiftArithmeticRoundedSaturate(value, count);

        /// <summary>
        /// svint64_t svqrshl[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svqrshl[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svqrshl[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> ShiftArithmeticRoundedSaturate(Vector<long> value, Vector<long> count) => ShiftArithmeticRoundedSaturate(value, count);


        ///  ShiftArithmeticSaturate : Saturating shift left

        /// <summary>
        /// svint8_t svqshl[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svqshl[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svqshl[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftArithmeticSaturate(Vector<sbyte> value, Vector<sbyte> count) => ShiftArithmeticSaturate(value, count);

        /// <summary>
        /// svint16_t svqshl[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svqshl[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svqshl[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> ShiftArithmeticSaturate(Vector<short> value, Vector<short> count) => ShiftArithmeticSaturate(value, count);

        /// <summary>
        /// svint32_t svqshl[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svqshl[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svqshl[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> ShiftArithmeticSaturate(Vector<int> value, Vector<int> count) => ShiftArithmeticSaturate(value, count);

        /// <summary>
        /// svint64_t svqshl[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svqshl[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svqshl[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> ShiftArithmeticSaturate(Vector<long> value, Vector<long> count) => ShiftArithmeticSaturate(value, count);


        ///  ShiftLeftAndInsert : Shift left and insert

        /// <summary>
        /// svint8_t svsli[_n_s8](svint8_t op1, svint8_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftLeftAndInsert(Vector<sbyte> left, Vector<sbyte> right, [ConstantExpected] byte shift) => ShiftLeftAndInsert(left, right, shift);

        /// <summary>
        /// svint16_t svsli[_n_s16](svint16_t op1, svint16_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<short> ShiftLeftAndInsert(Vector<short> left, Vector<short> right, [ConstantExpected] byte shift) => ShiftLeftAndInsert(left, right, shift);

        /// <summary>
        /// svint32_t svsli[_n_s32](svint32_t op1, svint32_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<int> ShiftLeftAndInsert(Vector<int> left, Vector<int> right, [ConstantExpected] byte shift) => ShiftLeftAndInsert(left, right, shift);

        /// <summary>
        /// svint64_t svsli[_n_s64](svint64_t op1, svint64_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<long> ShiftLeftAndInsert(Vector<long> left, Vector<long> right, [ConstantExpected] byte shift) => ShiftLeftAndInsert(left, right, shift);

        /// <summary>
        /// svuint8_t svsli[_n_u8](svuint8_t op1, svuint8_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<byte> ShiftLeftAndInsert(Vector<byte> left, Vector<byte> right, [ConstantExpected] byte shift) => ShiftLeftAndInsert(left, right, shift);

        /// <summary>
        /// svuint16_t svsli[_n_u16](svuint16_t op1, svuint16_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<ushort> ShiftLeftAndInsert(Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte shift) => ShiftLeftAndInsert(left, right, shift);

        /// <summary>
        /// svuint32_t svsli[_n_u32](svuint32_t op1, svuint32_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<uint> ShiftLeftAndInsert(Vector<uint> left, Vector<uint> right, [ConstantExpected] byte shift) => ShiftLeftAndInsert(left, right, shift);

        /// <summary>
        /// svuint64_t svsli[_n_u64](svuint64_t op1, svuint64_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<ulong> ShiftLeftAndInsert(Vector<ulong> left, Vector<ulong> right, [ConstantExpected] byte shift) => ShiftLeftAndInsert(left, right, shift);


        ///  ShiftLeftLogicalSaturate : Saturating shift left

        /// <summary>
        /// svuint8_t svqshl[_u8]_m(svbool_t pg, svuint8_t op1, svint8_t op2)
        /// svuint8_t svqshl[_u8]_x(svbool_t pg, svuint8_t op1, svint8_t op2)
        /// svuint8_t svqshl[_u8]_z(svbool_t pg, svuint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> ShiftLeftLogicalSaturate(Vector<byte> value, Vector<sbyte> count) => ShiftLeftLogicalSaturate(value, count);

        /// <summary>
        /// svuint16_t svqshl[_u16]_m(svbool_t pg, svuint16_t op1, svint16_t op2)
        /// svuint16_t svqshl[_u16]_x(svbool_t pg, svuint16_t op1, svint16_t op2)
        /// svuint16_t svqshl[_u16]_z(svbool_t pg, svuint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftLeftLogicalSaturate(Vector<ushort> value, Vector<short> count) => ShiftLeftLogicalSaturate(value, count);

        /// <summary>
        /// svuint32_t svqshl[_u32]_m(svbool_t pg, svuint32_t op1, svint32_t op2)
        /// svuint32_t svqshl[_u32]_x(svbool_t pg, svuint32_t op1, svint32_t op2)
        /// svuint32_t svqshl[_u32]_z(svbool_t pg, svuint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> ShiftLeftLogicalSaturate(Vector<uint> value, Vector<int> count) => ShiftLeftLogicalSaturate(value, count);

        /// <summary>
        /// svuint64_t svqshl[_u64]_m(svbool_t pg, svuint64_t op1, svint64_t op2)
        /// svuint64_t svqshl[_u64]_x(svbool_t pg, svuint64_t op1, svint64_t op2)
        /// svuint64_t svqshl[_u64]_z(svbool_t pg, svuint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> ShiftLeftLogicalSaturate(Vector<ulong> value, Vector<long> count) => ShiftLeftLogicalSaturate(value, count);


        ///  ShiftLeftLogicalSaturateUnsigned : Saturating shift left unsigned

        /// <summary>
        /// svuint8_t svqshlu[_n_s8]_m(svbool_t pg, svint8_t op1, uint64_t imm2)
        /// svuint8_t svqshlu[_n_s8]_x(svbool_t pg, svint8_t op1, uint64_t imm2)
        /// svuint8_t svqshlu[_n_s8]_z(svbool_t pg, svint8_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<byte> ShiftLeftLogicalSaturateUnsigned(Vector<sbyte> value, [ConstantExpected] byte count) => ShiftLeftLogicalSaturateUnsigned(value, count);

        /// <summary>
        /// svuint16_t svqshlu[_n_s16]_m(svbool_t pg, svint16_t op1, uint64_t imm2)
        /// svuint16_t svqshlu[_n_s16]_x(svbool_t pg, svint16_t op1, uint64_t imm2)
        /// svuint16_t svqshlu[_n_s16]_z(svbool_t pg, svint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftLeftLogicalSaturateUnsigned(Vector<short> value, [ConstantExpected] byte count) => ShiftLeftLogicalSaturateUnsigned(value, count);

        /// <summary>
        /// svuint32_t svqshlu[_n_s32]_m(svbool_t pg, svint32_t op1, uint64_t imm2)
        /// svuint32_t svqshlu[_n_s32]_x(svbool_t pg, svint32_t op1, uint64_t imm2)
        /// svuint32_t svqshlu[_n_s32]_z(svbool_t pg, svint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<uint> ShiftLeftLogicalSaturateUnsigned(Vector<int> value, [ConstantExpected] byte count) => ShiftLeftLogicalSaturateUnsigned(value, count);

        /// <summary>
        /// svuint64_t svqshlu[_n_s64]_m(svbool_t pg, svint64_t op1, uint64_t imm2)
        /// svuint64_t svqshlu[_n_s64]_x(svbool_t pg, svint64_t op1, uint64_t imm2)
        /// svuint64_t svqshlu[_n_s64]_z(svbool_t pg, svint64_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<ulong> ShiftLeftLogicalSaturateUnsigned(Vector<long> value, [ConstantExpected] byte count) => ShiftLeftLogicalSaturateUnsigned(value, count);


        ///  ShiftLeftLogicalWideningEven : Shift left long (bottom)

        /// <summary>
        /// svint16_t svshllb[_n_s16](svint8_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<short> ShiftLeftLogicalWideningEven(Vector<sbyte> value, [ConstantExpected] byte count) => ShiftLeftLogicalWideningEven(value, count);

        /// <summary>
        /// svint32_t svshllb[_n_s32](svint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<int> ShiftLeftLogicalWideningEven(Vector<short> value, [ConstantExpected] byte count) => ShiftLeftLogicalWideningEven(value, count);

        /// <summary>
        /// svint64_t svshllb[_n_s64](svint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<long> ShiftLeftLogicalWideningEven(Vector<int> value, [ConstantExpected] byte count) => ShiftLeftLogicalWideningEven(value, count);

        /// <summary>
        /// svuint16_t svshllb[_n_u16](svuint8_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftLeftLogicalWideningEven(Vector<byte> value, [ConstantExpected] byte count) => ShiftLeftLogicalWideningEven(value, count);

        /// <summary>
        /// svuint32_t svshllb[_n_u32](svuint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<uint> ShiftLeftLogicalWideningEven(Vector<ushort> value, [ConstantExpected] byte count) => ShiftLeftLogicalWideningEven(value, count);

        /// <summary>
        /// svuint64_t svshllb[_n_u64](svuint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<ulong> ShiftLeftLogicalWideningEven(Vector<uint> value, [ConstantExpected] byte count) => ShiftLeftLogicalWideningEven(value, count);


        ///  ShiftLeftLogicalWideningOdd : Shift left long (top)

        /// <summary>
        /// svint16_t svshllt[_n_s16](svint8_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<short> ShiftLeftLogicalWideningOdd(Vector<sbyte> value, [ConstantExpected] byte count) => ShiftLeftLogicalWideningOdd(value, count);

        /// <summary>
        /// svint32_t svshllt[_n_s32](svint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<int> ShiftLeftLogicalWideningOdd(Vector<short> value, [ConstantExpected] byte count) => ShiftLeftLogicalWideningOdd(value, count);

        /// <summary>
        /// svint64_t svshllt[_n_s64](svint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<long> ShiftLeftLogicalWideningOdd(Vector<int> value, [ConstantExpected] byte count) => ShiftLeftLogicalWideningOdd(value, count);

        /// <summary>
        /// svuint16_t svshllt[_n_u16](svuint8_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftLeftLogicalWideningOdd(Vector<byte> value, [ConstantExpected] byte count) => ShiftLeftLogicalWideningOdd(value, count);

        /// <summary>
        /// svuint32_t svshllt[_n_u32](svuint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<uint> ShiftLeftLogicalWideningOdd(Vector<ushort> value, [ConstantExpected] byte count) => ShiftLeftLogicalWideningOdd(value, count);

        /// <summary>
        /// svuint64_t svshllt[_n_u64](svuint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<ulong> ShiftLeftLogicalWideningOdd(Vector<uint> value, [ConstantExpected] byte count) => ShiftLeftLogicalWideningOdd(value, count);


        ///  ShiftLogicalRounded : Rounding shift left

        /// <summary>
        /// svuint8_t svrshl[_u8]_m(svbool_t pg, svuint8_t op1, svint8_t op2)
        /// svuint8_t svrshl[_u8]_x(svbool_t pg, svuint8_t op1, svint8_t op2)
        /// svuint8_t svrshl[_u8]_z(svbool_t pg, svuint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> ShiftLogicalRounded(Vector<byte> value, Vector<sbyte> count) => ShiftLogicalRounded(value, count);

        /// <summary>
        /// svuint16_t svrshl[_u16]_m(svbool_t pg, svuint16_t op1, svint16_t op2)
        /// svuint16_t svrshl[_u16]_x(svbool_t pg, svuint16_t op1, svint16_t op2)
        /// svuint16_t svrshl[_u16]_z(svbool_t pg, svuint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftLogicalRounded(Vector<ushort> value, Vector<short> count) => ShiftLogicalRounded(value, count);

        /// <summary>
        /// svuint32_t svrshl[_u32]_m(svbool_t pg, svuint32_t op1, svint32_t op2)
        /// svuint32_t svrshl[_u32]_x(svbool_t pg, svuint32_t op1, svint32_t op2)
        /// svuint32_t svrshl[_u32]_z(svbool_t pg, svuint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> ShiftLogicalRounded(Vector<uint> value, Vector<int> count) => ShiftLogicalRounded(value, count);

        /// <summary>
        /// svuint64_t svrshl[_u64]_m(svbool_t pg, svuint64_t op1, svint64_t op2)
        /// svuint64_t svrshl[_u64]_x(svbool_t pg, svuint64_t op1, svint64_t op2)
        /// svuint64_t svrshl[_u64]_z(svbool_t pg, svuint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> ShiftLogicalRounded(Vector<ulong> value, Vector<long> count) => ShiftLogicalRounded(value, count);


        ///  ShiftLogicalRoundedSaturate : Saturating rounding shift left

        /// <summary>
        /// svuint8_t svqrshl[_u8]_m(svbool_t pg, svuint8_t op1, svint8_t op2)
        /// svuint8_t svqrshl[_u8]_x(svbool_t pg, svuint8_t op1, svint8_t op2)
        /// svuint8_t svqrshl[_u8]_z(svbool_t pg, svuint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> ShiftLogicalRoundedSaturate(Vector<byte> value, Vector<sbyte> count) => ShiftLogicalRoundedSaturate(value, count);

        /// <summary>
        /// svuint16_t svqrshl[_u16]_m(svbool_t pg, svuint16_t op1, svint16_t op2)
        /// svuint16_t svqrshl[_u16]_x(svbool_t pg, svuint16_t op1, svint16_t op2)
        /// svuint16_t svqrshl[_u16]_z(svbool_t pg, svuint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftLogicalRoundedSaturate(Vector<ushort> value, Vector<short> count) => ShiftLogicalRoundedSaturate(value, count);

        /// <summary>
        /// svuint32_t svqrshl[_u32]_m(svbool_t pg, svuint32_t op1, svint32_t op2)
        /// svuint32_t svqrshl[_u32]_x(svbool_t pg, svuint32_t op1, svint32_t op2)
        /// svuint32_t svqrshl[_u32]_z(svbool_t pg, svuint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> ShiftLogicalRoundedSaturate(Vector<uint> value, Vector<int> count) => ShiftLogicalRoundedSaturate(value, count);

        /// <summary>
        /// svuint64_t svqrshl[_u64]_m(svbool_t pg, svuint64_t op1, svint64_t op2)
        /// svuint64_t svqrshl[_u64]_x(svbool_t pg, svuint64_t op1, svint64_t op2)
        /// svuint64_t svqrshl[_u64]_z(svbool_t pg, svuint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> ShiftLogicalRoundedSaturate(Vector<ulong> value, Vector<long> count) => ShiftLogicalRoundedSaturate(value, count);


        ///  ShiftRightAndInsert : Shift right and insert

        /// <summary>
        /// svint8_t svsri[_n_s8](svint8_t op1, svint8_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftRightAndInsert(Vector<sbyte> left, Vector<sbyte> right, [ConstantExpected] byte shift) => ShiftRightAndInsert(left, right, shift);

        /// <summary>
        /// svint16_t svsri[_n_s16](svint16_t op1, svint16_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<short> ShiftRightAndInsert(Vector<short> left, Vector<short> right, [ConstantExpected] byte shift) => ShiftRightAndInsert(left, right, shift);

        /// <summary>
        /// svint32_t svsri[_n_s32](svint32_t op1, svint32_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<int> ShiftRightAndInsert(Vector<int> left, Vector<int> right, [ConstantExpected] byte shift) => ShiftRightAndInsert(left, right, shift);

        /// <summary>
        /// svint64_t svsri[_n_s64](svint64_t op1, svint64_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<long> ShiftRightAndInsert(Vector<long> left, Vector<long> right, [ConstantExpected] byte shift) => ShiftRightAndInsert(left, right, shift);

        /// <summary>
        /// svuint8_t svsri[_n_u8](svuint8_t op1, svuint8_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<byte> ShiftRightAndInsert(Vector<byte> left, Vector<byte> right, [ConstantExpected] byte shift) => ShiftRightAndInsert(left, right, shift);

        /// <summary>
        /// svuint16_t svsri[_n_u16](svuint16_t op1, svuint16_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<ushort> ShiftRightAndInsert(Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte shift) => ShiftRightAndInsert(left, right, shift);

        /// <summary>
        /// svuint32_t svsri[_n_u32](svuint32_t op1, svuint32_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<uint> ShiftRightAndInsert(Vector<uint> left, Vector<uint> right, [ConstantExpected] byte shift) => ShiftRightAndInsert(left, right, shift);

        /// <summary>
        /// svuint64_t svsri[_n_u64](svuint64_t op1, svuint64_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<ulong> ShiftRightAndInsert(Vector<ulong> left, Vector<ulong> right, [ConstantExpected] byte shift) => ShiftRightAndInsert(left, right, shift);


        ///  ShiftRightArithmeticAdd : Shift right and accumulate

        /// <summary>
        /// svint8_t svsra[_n_s8](svint8_t op1, svint8_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftRightArithmeticAdd(Vector<sbyte> addend, Vector<sbyte> value, [ConstantExpected] byte count) => ShiftRightArithmeticAdd(addend, value, count);

        /// <summary>
        /// svint16_t svsra[_n_s16](svint16_t op1, svint16_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<short> ShiftRightArithmeticAdd(Vector<short> addend, Vector<short> value, [ConstantExpected] byte count) => ShiftRightArithmeticAdd(addend, value, count);

        /// <summary>
        /// svint32_t svsra[_n_s32](svint32_t op1, svint32_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<int> ShiftRightArithmeticAdd(Vector<int> addend, Vector<int> value, [ConstantExpected] byte count) => ShiftRightArithmeticAdd(addend, value, count);

        /// <summary>
        /// svint64_t svsra[_n_s64](svint64_t op1, svint64_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<long> ShiftRightArithmeticAdd(Vector<long> addend, Vector<long> value, [ConstantExpected] byte count) => ShiftRightArithmeticAdd(addend, value, count);


        ///  ShiftRightArithmeticNarrowingSaturateEven : Saturating shift right narrow (bottom)

        /// <summary>
        /// svint8_t svqshrnb[_n_s16](svint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftRightArithmeticNarrowingSaturateEven(Vector<short> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateEven(value, count);

        /// <summary>
        /// svint16_t svqshrnb[_n_s32](svint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<short> ShiftRightArithmeticNarrowingSaturateEven(Vector<int> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateEven(value, count);

        /// <summary>
        /// svint32_t svqshrnb[_n_s64](svint64_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<int> ShiftRightArithmeticNarrowingSaturateEven(Vector<long> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateEven(value, count);

        /// <summary>
        /// svuint8_t svqshrnb[_n_u16](svuint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<byte> ShiftRightArithmeticNarrowingSaturateEven(Vector<ushort> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateEven(value, count);

        /// <summary>
        /// svuint16_t svqshrnb[_n_u32](svuint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftRightArithmeticNarrowingSaturateEven(Vector<uint> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateEven(value, count);

        /// <summary>
        /// svuint32_t svqshrnb[_n_u64](svuint64_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<uint> ShiftRightArithmeticNarrowingSaturateEven(Vector<ulong> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateEven(value, count);


        ///  ShiftRightArithmeticNarrowingSaturateOdd : Saturating shift right narrow (top)

        /// <summary>
        /// svint8_t svqshrnt[_n_s16](svint8_t even, svint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftRightArithmeticNarrowingSaturateOdd(Vector<sbyte> even, Vector<short> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateOdd(even, value, count);

        /// <summary>
        /// svint16_t svqshrnt[_n_s32](svint16_t even, svint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<short> ShiftRightArithmeticNarrowingSaturateOdd(Vector<short> even, Vector<int> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateOdd(even, value, count);

        /// <summary>
        /// svint32_t svqshrnt[_n_s64](svint32_t even, svint64_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<int> ShiftRightArithmeticNarrowingSaturateOdd(Vector<int> even, Vector<long> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateOdd(even, value, count);

        /// <summary>
        /// svuint8_t svqshrnt[_n_u16](svuint8_t even, svuint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<byte> ShiftRightArithmeticNarrowingSaturateOdd(Vector<byte> even, Vector<ushort> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateOdd(even, value, count);

        /// <summary>
        /// svuint16_t svqshrnt[_n_u32](svuint16_t even, svuint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftRightArithmeticNarrowingSaturateOdd(Vector<ushort> even, Vector<uint> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateOdd(even, value, count);

        /// <summary>
        /// svuint32_t svqshrnt[_n_u64](svuint32_t even, svuint64_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<uint> ShiftRightArithmeticNarrowingSaturateOdd(Vector<uint> even, Vector<ulong> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateOdd(even, value, count);


        ///  ShiftRightArithmeticNarrowingSaturateUnsignedEven : Saturating shift right unsigned narrow (bottom)

        /// <summary>
        /// svuint8_t svqshrunb[_n_s16](svint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<byte> ShiftRightArithmeticNarrowingSaturateUnsignedEven(Vector<short> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateUnsignedEven(value, count);

        /// <summary>
        /// svuint16_t svqshrunb[_n_s32](svint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftRightArithmeticNarrowingSaturateUnsignedEven(Vector<int> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateUnsignedEven(value, count);

        /// <summary>
        /// svuint32_t svqshrunb[_n_s64](svint64_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<uint> ShiftRightArithmeticNarrowingSaturateUnsignedEven(Vector<long> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateUnsignedEven(value, count);


        ///  ShiftRightArithmeticNarrowingSaturateUnsignedOdd : Saturating shift right unsigned narrow (top)

        /// <summary>
        /// svuint8_t svqshrunt[_n_s16](svuint8_t even, svint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<byte> ShiftRightArithmeticNarrowingSaturateUnsignedOdd(Vector<byte> even, Vector<short> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateUnsignedOdd(even, value, count);

        /// <summary>
        /// svuint16_t svqshrunt[_n_s32](svuint16_t even, svint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftRightArithmeticNarrowingSaturateUnsignedOdd(Vector<ushort> even, Vector<int> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateUnsignedOdd(even, value, count);

        /// <summary>
        /// svuint32_t svqshrunt[_n_s64](svuint32_t even, svint64_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<uint> ShiftRightArithmeticNarrowingSaturateUnsignedOdd(Vector<uint> even, Vector<long> value, [ConstantExpected] byte count) => ShiftRightArithmeticNarrowingSaturateUnsignedOdd(even, value, count);


        ///  ShiftRightArithmeticRounded : Rounding shift right

        /// <summary>
        /// svint8_t svrshr[_n_s8]_m(svbool_t pg, svint8_t op1, uint64_t imm2)
        /// svint8_t svrshr[_n_s8]_x(svbool_t pg, svint8_t op1, uint64_t imm2)
        /// svint8_t svrshr[_n_s8]_z(svbool_t pg, svint8_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftRightArithmeticRounded(Vector<sbyte> value, [ConstantExpected] byte count) => ShiftRightArithmeticRounded(value, count);

        /// <summary>
        /// svint16_t svrshr[_n_s16]_m(svbool_t pg, svint16_t op1, uint64_t imm2)
        /// svint16_t svrshr[_n_s16]_x(svbool_t pg, svint16_t op1, uint64_t imm2)
        /// svint16_t svrshr[_n_s16]_z(svbool_t pg, svint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<short> ShiftRightArithmeticRounded(Vector<short> value, [ConstantExpected] byte count) => ShiftRightArithmeticRounded(value, count);

        /// <summary>
        /// svint32_t svrshr[_n_s32]_m(svbool_t pg, svint32_t op1, uint64_t imm2)
        /// svint32_t svrshr[_n_s32]_x(svbool_t pg, svint32_t op1, uint64_t imm2)
        /// svint32_t svrshr[_n_s32]_z(svbool_t pg, svint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<int> ShiftRightArithmeticRounded(Vector<int> value, [ConstantExpected] byte count) => ShiftRightArithmeticRounded(value, count);

        /// <summary>
        /// svint64_t svrshr[_n_s64]_m(svbool_t pg, svint64_t op1, uint64_t imm2)
        /// svint64_t svrshr[_n_s64]_x(svbool_t pg, svint64_t op1, uint64_t imm2)
        /// svint64_t svrshr[_n_s64]_z(svbool_t pg, svint64_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<long> ShiftRightArithmeticRounded(Vector<long> value, [ConstantExpected] byte count) => ShiftRightArithmeticRounded(value, count);


        ///  ShiftRightArithmeticRoundedAdd : Rounding shift right and accumulate

        /// <summary>
        /// svint8_t svrsra[_n_s8](svint8_t op1, svint8_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftRightArithmeticRoundedAdd(Vector<sbyte> addend, Vector<sbyte> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedAdd(addend, value, count);

        /// <summary>
        /// svint16_t svrsra[_n_s16](svint16_t op1, svint16_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<short> ShiftRightArithmeticRoundedAdd(Vector<short> addend, Vector<short> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedAdd(addend, value, count);

        /// <summary>
        /// svint32_t svrsra[_n_s32](svint32_t op1, svint32_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<int> ShiftRightArithmeticRoundedAdd(Vector<int> addend, Vector<int> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedAdd(addend, value, count);

        /// <summary>
        /// svint64_t svrsra[_n_s64](svint64_t op1, svint64_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<long> ShiftRightArithmeticRoundedAdd(Vector<long> addend, Vector<long> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedAdd(addend, value, count);


        ///  ShiftRightArithmeticRoundedNarrowingSaturateEven : Saturating rounding shift right narrow (bottom)

        /// <summary>
        /// svint8_t svqrshrnb[_n_s16](svint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftRightArithmeticRoundedNarrowingSaturateEven(Vector<short> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedNarrowingSaturateEven(value, count);

        /// <summary>
        /// svint16_t svqrshrnb[_n_s32](svint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<short> ShiftRightArithmeticRoundedNarrowingSaturateEven(Vector<int> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedNarrowingSaturateEven(value, count);

        /// <summary>
        /// svint32_t svqrshrnb[_n_s64](svint64_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<int> ShiftRightArithmeticRoundedNarrowingSaturateEven(Vector<long> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedNarrowingSaturateEven(value, count);


        ///  ShiftRightArithmeticRoundedNarrowingSaturateOdd : Saturating rounding shift right narrow (top)

        /// <summary>
        /// svint8_t svqrshrnt[_n_s16](svint8_t even, svint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftRightArithmeticRoundedNarrowingSaturateOdd(Vector<sbyte> even, Vector<short> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedNarrowingSaturateOdd(even, value, count);

        /// <summary>
        /// svint16_t svqrshrnt[_n_s32](svint16_t even, svint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<short> ShiftRightArithmeticRoundedNarrowingSaturateOdd(Vector<short> even, Vector<int> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedNarrowingSaturateOdd(even, value, count);

        /// <summary>
        /// svint32_t svqrshrnt[_n_s64](svint32_t even, svint64_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<int> ShiftRightArithmeticRoundedNarrowingSaturateOdd(Vector<int> even, Vector<long> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedNarrowingSaturateOdd(even, value, count);


        ///  ShiftRightArithmeticRoundedNarrowingSaturateUnsignedEven : Saturating rounding shift right unsigned narrow (bottom)

        /// <summary>
        /// svuint8_t svqrshrunb[_n_s16](svint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<byte> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedEven(Vector<short> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedEven(value, count);

        /// <summary>
        /// svuint16_t svqrshrunb[_n_s32](svint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedEven(Vector<int> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedEven(value, count);

        /// <summary>
        /// svuint32_t svqrshrunb[_n_s64](svint64_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<uint> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedEven(Vector<long> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedEven(value, count);


        ///  ShiftRightArithmeticRoundedNarrowingSaturateUnsignedOdd : Saturating rounding shift right unsigned narrow (top)

        /// <summary>
        /// svuint8_t svqrshrunt[_n_s16](svuint8_t even, svint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<byte> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedOdd(Vector<byte> even, Vector<short> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedOdd(even, value, count);

        /// <summary>
        /// svuint16_t svqrshrunt[_n_s32](svuint16_t even, svint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedOdd(Vector<ushort> even, Vector<int> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedOdd(even, value, count);

        /// <summary>
        /// svuint32_t svqrshrunt[_n_s64](svuint32_t even, svint64_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<uint> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedOdd(Vector<uint> even, Vector<long> value, [ConstantExpected] byte count) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedOdd(even, value, count);


        ///  ShiftRightLogicalAdd : Shift right and accumulate

        /// <summary>
        /// svuint8_t svsra[_n_u8](svuint8_t op1, svuint8_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<byte> ShiftRightLogicalAdd(Vector<byte> addend, Vector<byte> value, [ConstantExpected] byte count) => ShiftRightLogicalAdd(addend, value, count);

        /// <summary>
        /// svuint16_t svsra[_n_u16](svuint16_t op1, svuint16_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<ushort> ShiftRightLogicalAdd(Vector<ushort> addend, Vector<ushort> value, [ConstantExpected] byte count) => ShiftRightLogicalAdd(addend, value, count);

        /// <summary>
        /// svuint32_t svsra[_n_u32](svuint32_t op1, svuint32_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<uint> ShiftRightLogicalAdd(Vector<uint> addend, Vector<uint> value, [ConstantExpected] byte count) => ShiftRightLogicalAdd(addend, value, count);

        /// <summary>
        /// svuint64_t svsra[_n_u64](svuint64_t op1, svuint64_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<ulong> ShiftRightLogicalAdd(Vector<ulong> addend, Vector<ulong> value, [ConstantExpected] byte count) => ShiftRightLogicalAdd(addend, value, count);


        ///  ShiftRightLogicalNarrowingEven : Shift right narrow (bottom)

        /// <summary>
        /// svint8_t svshrnb[_n_s16](svint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftRightLogicalNarrowingEven(Vector<short> value, [ConstantExpected] byte count) => ShiftRightLogicalNarrowingEven(value, count);

        /// <summary>
        /// svint16_t svshrnb[_n_s32](svint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<short> ShiftRightLogicalNarrowingEven(Vector<int> value, [ConstantExpected] byte count) => ShiftRightLogicalNarrowingEven(value, count);

        /// <summary>
        /// svint32_t svshrnb[_n_s64](svint64_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<int> ShiftRightLogicalNarrowingEven(Vector<long> value, [ConstantExpected] byte count) => ShiftRightLogicalNarrowingEven(value, count);

        /// <summary>
        /// svuint8_t svshrnb[_n_u16](svuint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<byte> ShiftRightLogicalNarrowingEven(Vector<ushort> value, [ConstantExpected] byte count) => ShiftRightLogicalNarrowingEven(value, count);

        /// <summary>
        /// svuint16_t svshrnb[_n_u32](svuint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftRightLogicalNarrowingEven(Vector<uint> value, [ConstantExpected] byte count) => ShiftRightLogicalNarrowingEven(value, count);

        /// <summary>
        /// svuint32_t svshrnb[_n_u64](svuint64_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<uint> ShiftRightLogicalNarrowingEven(Vector<ulong> value, [ConstantExpected] byte count) => ShiftRightLogicalNarrowingEven(value, count);


        ///  ShiftRightLogicalNarrowingOdd : Shift right narrow (top)

        /// <summary>
        /// svint8_t svshrnt[_n_s16](svint8_t even, svint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftRightLogicalNarrowingOdd(Vector<sbyte> even, Vector<short> value, [ConstantExpected] byte count) => ShiftRightLogicalNarrowingOdd(even, value, count);

        /// <summary>
        /// svint16_t svshrnt[_n_s32](svint16_t even, svint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<short> ShiftRightLogicalNarrowingOdd(Vector<short> even, Vector<int> value, [ConstantExpected] byte count) => ShiftRightLogicalNarrowingOdd(even, value, count);

        /// <summary>
        /// svint32_t svshrnt[_n_s64](svint32_t even, svint64_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<int> ShiftRightLogicalNarrowingOdd(Vector<int> even, Vector<long> value, [ConstantExpected] byte count) => ShiftRightLogicalNarrowingOdd(even, value, count);

        /// <summary>
        /// svuint8_t svshrnt[_n_u16](svuint8_t even, svuint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<byte> ShiftRightLogicalNarrowingOdd(Vector<byte> even, Vector<ushort> value, [ConstantExpected] byte count) => ShiftRightLogicalNarrowingOdd(even, value, count);

        /// <summary>
        /// svuint16_t svshrnt[_n_u32](svuint16_t even, svuint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftRightLogicalNarrowingOdd(Vector<ushort> even, Vector<uint> value, [ConstantExpected] byte count) => ShiftRightLogicalNarrowingOdd(even, value, count);

        /// <summary>
        /// svuint32_t svshrnt[_n_u64](svuint32_t even, svuint64_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<uint> ShiftRightLogicalNarrowingOdd(Vector<uint> even, Vector<ulong> value, [ConstantExpected] byte count) => ShiftRightLogicalNarrowingOdd(even, value, count);


        ///  ShiftRightLogicalRounded : Rounding shift right

        /// <summary>
        /// svuint8_t svrshr[_n_u8]_m(svbool_t pg, svuint8_t op1, uint64_t imm2)
        /// svuint8_t svrshr[_n_u8]_x(svbool_t pg, svuint8_t op1, uint64_t imm2)
        /// svuint8_t svrshr[_n_u8]_z(svbool_t pg, svuint8_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<byte> ShiftRightLogicalRounded(Vector<byte> value, [ConstantExpected] byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// svuint16_t svrshr[_n_u16]_m(svbool_t pg, svuint16_t op1, uint64_t imm2)
        /// svuint16_t svrshr[_n_u16]_x(svbool_t pg, svuint16_t op1, uint64_t imm2)
        /// svuint16_t svrshr[_n_u16]_z(svbool_t pg, svuint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftRightLogicalRounded(Vector<ushort> value, [ConstantExpected] byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// svuint32_t svrshr[_n_u32]_m(svbool_t pg, svuint32_t op1, uint64_t imm2)
        /// svuint32_t svrshr[_n_u32]_x(svbool_t pg, svuint32_t op1, uint64_t imm2)
        /// svuint32_t svrshr[_n_u32]_z(svbool_t pg, svuint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<uint> ShiftRightLogicalRounded(Vector<uint> value, [ConstantExpected] byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// svuint64_t svrshr[_n_u64]_m(svbool_t pg, svuint64_t op1, uint64_t imm2)
        /// svuint64_t svrshr[_n_u64]_x(svbool_t pg, svuint64_t op1, uint64_t imm2)
        /// svuint64_t svrshr[_n_u64]_z(svbool_t pg, svuint64_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<ulong> ShiftRightLogicalRounded(Vector<ulong> value, [ConstantExpected] byte count) => ShiftRightLogicalRounded(value, count);


        ///  ShiftRightLogicalRoundedAdd : Rounding shift right and accumulate

        /// <summary>
        /// svuint8_t svrsra[_n_u8](svuint8_t op1, svuint8_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<byte> ShiftRightLogicalRoundedAdd(Vector<byte> addend, Vector<byte> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedAdd(addend, value, count);

        /// <summary>
        /// svuint16_t svrsra[_n_u16](svuint16_t op1, svuint16_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<ushort> ShiftRightLogicalRoundedAdd(Vector<ushort> addend, Vector<ushort> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedAdd(addend, value, count);

        /// <summary>
        /// svuint32_t svrsra[_n_u32](svuint32_t op1, svuint32_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<uint> ShiftRightLogicalRoundedAdd(Vector<uint> addend, Vector<uint> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedAdd(addend, value, count);

        /// <summary>
        /// svuint64_t svrsra[_n_u64](svuint64_t op1, svuint64_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<ulong> ShiftRightLogicalRoundedAdd(Vector<ulong> addend, Vector<ulong> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedAdd(addend, value, count);


        ///  ShiftRightLogicalRoundedNarrowingEven : Rounding shift right narrow (bottom)

        /// <summary>
        /// svint8_t svrshrnb[_n_s16](svint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftRightLogicalRoundedNarrowingEven(Vector<short> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingEven(value, count);

        /// <summary>
        /// svint16_t svrshrnb[_n_s32](svint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<short> ShiftRightLogicalRoundedNarrowingEven(Vector<int> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingEven(value, count);

        /// <summary>
        /// svint32_t svrshrnb[_n_s64](svint64_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<int> ShiftRightLogicalRoundedNarrowingEven(Vector<long> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingEven(value, count);

        /// <summary>
        /// svuint8_t svrshrnb[_n_u16](svuint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<byte> ShiftRightLogicalRoundedNarrowingEven(Vector<ushort> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingEven(value, count);

        /// <summary>
        /// svuint16_t svrshrnb[_n_u32](svuint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftRightLogicalRoundedNarrowingEven(Vector<uint> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingEven(value, count);

        /// <summary>
        /// svuint32_t svrshrnb[_n_u64](svuint64_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<uint> ShiftRightLogicalRoundedNarrowingEven(Vector<ulong> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingEven(value, count);


        ///  ShiftRightLogicalRoundedNarrowingOdd : Rounding shift right narrow (top)

        /// <summary>
        /// svint8_t svrshrnt[_n_s16](svint8_t even, svint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftRightLogicalRoundedNarrowingOdd(Vector<sbyte> even, Vector<short> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingOdd(even, value, count);

        /// <summary>
        /// svint16_t svrshrnt[_n_s32](svint16_t even, svint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<short> ShiftRightLogicalRoundedNarrowingOdd(Vector<short> even, Vector<int> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingOdd(even, value, count);

        /// <summary>
        /// svint32_t svrshrnt[_n_s64](svint32_t even, svint64_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<int> ShiftRightLogicalRoundedNarrowingOdd(Vector<int> even, Vector<long> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingOdd(even, value, count);

        /// <summary>
        /// svuint8_t svrshrnt[_n_u16](svuint8_t even, svuint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<byte> ShiftRightLogicalRoundedNarrowingOdd(Vector<byte> even, Vector<ushort> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingOdd(even, value, count);

        /// <summary>
        /// svuint16_t svrshrnt[_n_u32](svuint16_t even, svuint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftRightLogicalRoundedNarrowingOdd(Vector<ushort> even, Vector<uint> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingOdd(even, value, count);

        /// <summary>
        /// svuint32_t svrshrnt[_n_u64](svuint32_t even, svuint64_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<uint> ShiftRightLogicalRoundedNarrowingOdd(Vector<uint> even, Vector<ulong> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingOdd(even, value, count);


        ///  ShiftRightLogicalRoundedNarrowingSaturateEven : Saturating rounding shift right narrow (bottom)

        /// <summary>
        /// svuint8_t svqrshrnb[_n_u16](svuint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<byte> ShiftRightLogicalRoundedNarrowingSaturateEven(Vector<ushort> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingSaturateEven(value, count);

        /// <summary>
        /// svuint16_t svqrshrnb[_n_u32](svuint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftRightLogicalRoundedNarrowingSaturateEven(Vector<uint> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingSaturateEven(value, count);

        /// <summary>
        /// svuint32_t svqrshrnb[_n_u64](svuint64_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<uint> ShiftRightLogicalRoundedNarrowingSaturateEven(Vector<ulong> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingSaturateEven(value, count);


        ///  ShiftRightLogicalRoundedNarrowingSaturateOdd : Saturating rounding shift right narrow (top)

        /// <summary>
        /// svuint8_t svqrshrnt[_n_u16](svuint8_t even, svuint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<byte> ShiftRightLogicalRoundedNarrowingSaturateOdd(Vector<byte> even, Vector<ushort> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingSaturateOdd(even, value, count);

        /// <summary>
        /// svuint16_t svqrshrnt[_n_u32](svuint16_t even, svuint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftRightLogicalRoundedNarrowingSaturateOdd(Vector<ushort> even, Vector<uint> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingSaturateOdd(even, value, count);

        /// <summary>
        /// svuint32_t svqrshrnt[_n_u64](svuint32_t even, svuint64_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<uint> ShiftRightLogicalRoundedNarrowingSaturateOdd(Vector<uint> even, Vector<ulong> value, [ConstantExpected] byte count) => ShiftRightLogicalRoundedNarrowingSaturateOdd(even, value, count);


        ///  SubtractHighNarowingLower : Subtract narrow high part (bottom)

        /// <summary>
        /// svint8_t svsubhnb[_s16](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> SubtractHighNarowingLower(Vector<short> left, Vector<short> right) => SubtractHighNarowingLower(left, right);

        /// <summary>
        /// svint16_t svsubhnb[_s32](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<short> SubtractHighNarowingLower(Vector<int> left, Vector<int> right) => SubtractHighNarowingLower(left, right);

        /// <summary>
        /// svint32_t svsubhnb[_s64](svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<int> SubtractHighNarowingLower(Vector<long> left, Vector<long> right) => SubtractHighNarowingLower(left, right);

        /// <summary>
        /// svuint8_t svsubhnb[_u16](svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<byte> SubtractHighNarowingLower(Vector<ushort> left, Vector<ushort> right) => SubtractHighNarowingLower(left, right);

        /// <summary>
        /// svuint16_t svsubhnb[_u32](svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<ushort> SubtractHighNarowingLower(Vector<uint> left, Vector<uint> right) => SubtractHighNarowingLower(left, right);

        /// <summary>
        /// svuint32_t svsubhnb[_u64](svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<uint> SubtractHighNarowingLower(Vector<ulong> left, Vector<ulong> right) => SubtractHighNarowingLower(left, right);


        ///  SubtractHighNarowingUpper : Subtract narrow high part (top)

        /// <summary>
        /// svint8_t svsubhnt[_s16](svint8_t even, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> SubtractHighNarowingUpper(Vector<sbyte> even, Vector<short> left, Vector<short> right) => SubtractHighNarowingUpper(even, left, right);

        /// <summary>
        /// svint16_t svsubhnt[_s32](svint16_t even, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<short> SubtractHighNarowingUpper(Vector<short> even, Vector<int> left, Vector<int> right) => SubtractHighNarowingUpper(even, left, right);

        /// <summary>
        /// svint32_t svsubhnt[_s64](svint32_t even, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<int> SubtractHighNarowingUpper(Vector<int> even, Vector<long> left, Vector<long> right) => SubtractHighNarowingUpper(even, left, right);

        /// <summary>
        /// svuint8_t svsubhnt[_u16](svuint8_t even, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<byte> SubtractHighNarowingUpper(Vector<byte> even, Vector<ushort> left, Vector<ushort> right) => SubtractHighNarowingUpper(even, left, right);

        /// <summary>
        /// svuint16_t svsubhnt[_u32](svuint16_t even, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<ushort> SubtractHighNarowingUpper(Vector<ushort> even, Vector<uint> left, Vector<uint> right) => SubtractHighNarowingUpper(even, left, right);

        /// <summary>
        /// svuint32_t svsubhnt[_u64](svuint32_t even, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<uint> SubtractHighNarowingUpper(Vector<uint> even, Vector<ulong> left, Vector<ulong> right) => SubtractHighNarowingUpper(even, left, right);


        ///  SubtractSaturate : Saturating subtract

        /// <summary>
        /// svint8_t svqsub[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svqsub[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svqsub[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> SubtractSaturate(Vector<sbyte> left, Vector<sbyte> right) => SubtractSaturate(left, right);

        /// <summary>
        /// svint16_t svqsub[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svqsub[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svqsub[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> SubtractSaturate(Vector<short> left, Vector<short> right) => SubtractSaturate(left, right);

        /// <summary>
        /// svint32_t svqsub[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svqsub[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svqsub[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> SubtractSaturate(Vector<int> left, Vector<int> right) => SubtractSaturate(left, right);

        /// <summary>
        /// svint64_t svqsub[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svqsub[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svqsub[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> SubtractSaturate(Vector<long> left, Vector<long> right) => SubtractSaturate(left, right);

        /// <summary>
        /// svuint8_t svqsub[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svqsub[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svqsub[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> SubtractSaturate(Vector<byte> left, Vector<byte> right) => SubtractSaturate(left, right);

        /// <summary>
        /// svuint16_t svqsub[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svqsub[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svqsub[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> SubtractSaturate(Vector<ushort> left, Vector<ushort> right) => SubtractSaturate(left, right);

        /// <summary>
        /// svuint32_t svqsub[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svqsub[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svqsub[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> SubtractSaturate(Vector<uint> left, Vector<uint> right) => SubtractSaturate(left, right);

        /// <summary>
        /// svuint64_t svqsub[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svqsub[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svqsub[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> SubtractSaturate(Vector<ulong> left, Vector<ulong> right) => SubtractSaturate(left, right);


        ///  SubtractSaturateReversed : Saturating subtract reversed

        /// <summary>
        /// svint8_t svqsubr[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svqsubr[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svqsubr[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> SubtractSaturateReversed(Vector<sbyte> left, Vector<sbyte> right) => SubtractSaturateReversed(left, right);

        /// <summary>
        /// svint16_t svqsubr[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svqsubr[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svqsubr[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> SubtractSaturateReversed(Vector<short> left, Vector<short> right) => SubtractSaturateReversed(left, right);

        /// <summary>
        /// svint32_t svqsubr[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svqsubr[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svqsubr[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> SubtractSaturateReversed(Vector<int> left, Vector<int> right) => SubtractSaturateReversed(left, right);

        /// <summary>
        /// svint64_t svqsubr[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svqsubr[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svqsubr[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> SubtractSaturateReversed(Vector<long> left, Vector<long> right) => SubtractSaturateReversed(left, right);

        /// <summary>
        /// svuint8_t svqsubr[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svqsubr[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svqsubr[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> SubtractSaturateReversed(Vector<byte> left, Vector<byte> right) => SubtractSaturateReversed(left, right);

        /// <summary>
        /// svuint16_t svqsubr[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svqsubr[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svqsubr[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> SubtractSaturateReversed(Vector<ushort> left, Vector<ushort> right) => SubtractSaturateReversed(left, right);

        /// <summary>
        /// svuint32_t svqsubr[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svqsubr[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svqsubr[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> SubtractSaturateReversed(Vector<uint> left, Vector<uint> right) => SubtractSaturateReversed(left, right);

        /// <summary>
        /// svuint64_t svqsubr[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svqsubr[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svqsubr[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> SubtractSaturateReversed(Vector<ulong> left, Vector<ulong> right) => SubtractSaturateReversed(left, right);


        ///  SubtractWideLower : Subtract wide (bottom)

        /// <summary>
        /// svint16_t svsubwb[_s16](svint16_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<short> SubtractWideLower(Vector<short> left, Vector<sbyte> right) => SubtractWideLower(left, right);

        /// <summary>
        /// svint32_t svsubwb[_s32](svint32_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<int> SubtractWideLower(Vector<int> left, Vector<short> right) => SubtractWideLower(left, right);

        /// <summary>
        /// svint64_t svsubwb[_s64](svint64_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<long> SubtractWideLower(Vector<long> left, Vector<int> right) => SubtractWideLower(left, right);

        /// <summary>
        /// svuint16_t svsubwb[_u16](svuint16_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<ushort> SubtractWideLower(Vector<ushort> left, Vector<byte> right) => SubtractWideLower(left, right);

        /// <summary>
        /// svuint32_t svsubwb[_u32](svuint32_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<uint> SubtractWideLower(Vector<uint> left, Vector<ushort> right) => SubtractWideLower(left, right);

        /// <summary>
        /// svuint64_t svsubwb[_u64](svuint64_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<ulong> SubtractWideLower(Vector<ulong> left, Vector<uint> right) => SubtractWideLower(left, right);


        ///  SubtractWideUpper : Subtract wide (top)

        /// <summary>
        /// svint16_t svsubwt[_s16](svint16_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<short> SubtractWideUpper(Vector<short> left, Vector<sbyte> right) => SubtractWideUpper(left, right);

        /// <summary>
        /// svint32_t svsubwt[_s32](svint32_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<int> SubtractWideUpper(Vector<int> left, Vector<short> right) => SubtractWideUpper(left, right);

        /// <summary>
        /// svint64_t svsubwt[_s64](svint64_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<long> SubtractWideUpper(Vector<long> left, Vector<int> right) => SubtractWideUpper(left, right);

        /// <summary>
        /// svuint16_t svsubwt[_u16](svuint16_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<ushort> SubtractWideUpper(Vector<ushort> left, Vector<byte> right) => SubtractWideUpper(left, right);

        /// <summary>
        /// svuint32_t svsubwt[_u32](svuint32_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<uint> SubtractWideUpper(Vector<uint> left, Vector<ushort> right) => SubtractWideUpper(left, right);

        /// <summary>
        /// svuint64_t svsubwt[_u64](svuint64_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<ulong> SubtractWideUpper(Vector<ulong> left, Vector<uint> right) => SubtractWideUpper(left, right);


        ///  SubtractWideningLower : Subtract long (bottom)

        /// <summary>
        /// svint16_t svsublb[_s16](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<short> SubtractWideningLower(Vector<sbyte> left, Vector<sbyte> right) => SubtractWideningLower(left, right);

        /// <summary>
        /// svint32_t svsublb[_s32](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<int> SubtractWideningLower(Vector<short> left, Vector<short> right) => SubtractWideningLower(left, right);

        /// <summary>
        /// svint64_t svsublb[_s64](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<long> SubtractWideningLower(Vector<int> left, Vector<int> right) => SubtractWideningLower(left, right);

        /// <summary>
        /// svuint16_t svsublb[_u16](svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<ushort> SubtractWideningLower(Vector<byte> left, Vector<byte> right) => SubtractWideningLower(left, right);

        /// <summary>
        /// svuint32_t svsublb[_u32](svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<uint> SubtractWideningLower(Vector<ushort> left, Vector<ushort> right) => SubtractWideningLower(left, right);

        /// <summary>
        /// svuint64_t svsublb[_u64](svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<ulong> SubtractWideningLower(Vector<uint> left, Vector<uint> right) => SubtractWideningLower(left, right);


        ///  SubtractWideningLowerUpper : Subtract long (bottom - top)

        /// <summary>
        /// svint16_t svsublbt[_s16](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<short> SubtractWideningLowerUpper(Vector<sbyte> left, Vector<sbyte> right) => SubtractWideningLowerUpper(left, right);

        /// <summary>
        /// svint32_t svsublbt[_s32](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<int> SubtractWideningLowerUpper(Vector<short> left, Vector<short> right) => SubtractWideningLowerUpper(left, right);

        /// <summary>
        /// svint64_t svsublbt[_s64](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<long> SubtractWideningLowerUpper(Vector<int> left, Vector<int> right) => SubtractWideningLowerUpper(left, right);


        ///  SubtractWideningUpper : Subtract long (top)

        /// <summary>
        /// svint16_t svsublt[_s16](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<short> SubtractWideningUpper(Vector<sbyte> left, Vector<sbyte> right) => SubtractWideningUpper(left, right);

        /// <summary>
        /// svint32_t svsublt[_s32](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<int> SubtractWideningUpper(Vector<short> left, Vector<short> right) => SubtractWideningUpper(left, right);

        /// <summary>
        /// svint64_t svsublt[_s64](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<long> SubtractWideningUpper(Vector<int> left, Vector<int> right) => SubtractWideningUpper(left, right);

        /// <summary>
        /// svuint16_t svsublt[_u16](svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<ushort> SubtractWideningUpper(Vector<byte> left, Vector<byte> right) => SubtractWideningUpper(left, right);

        /// <summary>
        /// svuint32_t svsublt[_u32](svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<uint> SubtractWideningUpper(Vector<ushort> left, Vector<ushort> right) => SubtractWideningUpper(left, right);

        /// <summary>
        /// svuint64_t svsublt[_u64](svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<ulong> SubtractWideningUpper(Vector<uint> left, Vector<uint> right) => SubtractWideningUpper(left, right);


        ///  SubtractWideningUpperLower : Subtract long (top - bottom)

        /// <summary>
        /// svint16_t svsubltb[_s16](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<short> SubtractWideningUpperLower(Vector<sbyte> left, Vector<sbyte> right) => SubtractWideningUpperLower(left, right);

        /// <summary>
        /// svint32_t svsubltb[_s32](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<int> SubtractWideningUpperLower(Vector<short> left, Vector<short> right) => SubtractWideningUpperLower(left, right);

        /// <summary>
        /// svint64_t svsubltb[_s64](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<long> SubtractWideningUpperLower(Vector<int> left, Vector<int> right) => SubtractWideningUpperLower(left, right);


        ///  SubtractWithBorrowWideningLower : Subtract with borrow long (bottom)

        /// <summary>
        /// svuint32_t svsbclb[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3)
        /// </summary>
        public static unsafe Vector<uint> SubtractWithBorrowWideningLower(Vector<uint> op1, Vector<uint> op2, Vector<uint> op3) => SubtractWithBorrowWideningLower(op1, op2, op3);

        /// <summary>
        /// svuint64_t svsbclb[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3)
        /// </summary>
        public static unsafe Vector<ulong> SubtractWithBorrowWideningLower(Vector<ulong> op1, Vector<ulong> op2, Vector<ulong> op3) => SubtractWithBorrowWideningLower(op1, op2, op3);


        ///  SubtractWithBorrowWideningUpper : Subtract with borrow long (top)

        /// <summary>
        /// svuint32_t svsbclt[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3)
        /// </summary>
        public static unsafe Vector<uint> SubtractWithBorrowWideningUpper(Vector<uint> op1, Vector<uint> op2, Vector<uint> op3) => SubtractWithBorrowWideningUpper(op1, op2, op3);

        /// <summary>
        /// svuint64_t svsbclt[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3)
        /// </summary>
        public static unsafe Vector<ulong> SubtractWithBorrowWideningUpper(Vector<ulong> op1, Vector<ulong> op2, Vector<ulong> op3) => SubtractWithBorrowWideningUpper(op1, op2, op3);


        ///  UpConvertWideningUpper : Up convert long (top)

        /// <summary>
        /// svfloat64_t svcvtlt_f64[_f32]_m(svfloat64_t inactive, svbool_t pg, svfloat32_t op)
        /// svfloat64_t svcvtlt_f64[_f32]_x(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<double> UpConvertWideningUpper(Vector<float> value) => UpConvertWideningUpper(value);


        ///  VectorTableLookup : Table lookup in two-vector table

        /// <summary>
        /// svint8_t svtbl2[_s8](svint8x2_t data, svuint8_t indices)
        /// </summary>
        public static unsafe Vector<sbyte> VectorTableLookup((Vector<sbyte> data1, Vector<sbyte> data2), Vector<byte> indices) => VectorTableLookup(data1,, indices);

        /// <summary>
        /// svint16_t svtbl2[_s16](svint16x2_t data, svuint16_t indices)
        /// </summary>
        public static unsafe Vector<short> VectorTableLookup((Vector<short> data1, Vector<short> data2), Vector<ushort> indices) => VectorTableLookup(data1,, indices);

        /// <summary>
        /// svint32_t svtbl2[_s32](svint32x2_t data, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<int> VectorTableLookup((Vector<int> data1, Vector<int> data2), Vector<uint> indices) => VectorTableLookup(data1,, indices);

        /// <summary>
        /// svint64_t svtbl2[_s64](svint64x2_t data, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> VectorTableLookup((Vector<long> data1, Vector<long> data2), Vector<ulong> indices) => VectorTableLookup(data1,, indices);

        /// <summary>
        /// svuint8_t svtbl2[_u8](svuint8x2_t data, svuint8_t indices)
        /// </summary>
        public static unsafe Vector<byte> VectorTableLookup((Vector<byte> data1, Vector<byte> data2), Vector<byte> indices) => VectorTableLookup(data1,, indices);

        /// <summary>
        /// svuint16_t svtbl2[_u16](svuint16x2_t data, svuint16_t indices)
        /// </summary>
        public static unsafe Vector<ushort> VectorTableLookup((Vector<ushort> data1, Vector<ushort> data2), Vector<ushort> indices) => VectorTableLookup(data1,, indices);

        /// <summary>
        /// svuint32_t svtbl2[_u32](svuint32x2_t data, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> VectorTableLookup((Vector<uint> data1, Vector<uint> data2), Vector<uint> indices) => VectorTableLookup(data1,, indices);

        /// <summary>
        /// svuint64_t svtbl2[_u64](svuint64x2_t data, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> VectorTableLookup((Vector<ulong> data1, Vector<ulong> data2), Vector<ulong> indices) => VectorTableLookup(data1,, indices);

        /// <summary>
        /// svfloat32_t svtbl2[_f32](svfloat32x2_t data, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<float> VectorTableLookup((Vector<float> data1, Vector<float> data2), Vector<uint> indices) => VectorTableLookup(data1,, indices);

        /// <summary>
        /// svfloat64_t svtbl2[_f64](svfloat64x2_t data, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<double> VectorTableLookup((Vector<double> data1, Vector<double> data2), Vector<ulong> indices) => VectorTableLookup(data1,, indices);


        ///  VectorTableLookupExtension : Table lookup in single-vector table (merging)

        /// <summary>
        /// svint8_t svtbx[_s8](svint8_t fallback, svint8_t data, svuint8_t indices)
        /// </summary>
        public static unsafe Vector<sbyte> VectorTableLookupExtension(Vector<sbyte> fallback, Vector<sbyte> data, Vector<byte> indices) => VectorTableLookupExtension(fallback, data, indices);

        /// <summary>
        /// svint16_t svtbx[_s16](svint16_t fallback, svint16_t data, svuint16_t indices)
        /// </summary>
        public static unsafe Vector<short> VectorTableLookupExtension(Vector<short> fallback, Vector<short> data, Vector<ushort> indices) => VectorTableLookupExtension(fallback, data, indices);

        /// <summary>
        /// svint32_t svtbx[_s32](svint32_t fallback, svint32_t data, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<int> VectorTableLookupExtension(Vector<int> fallback, Vector<int> data, Vector<uint> indices) => VectorTableLookupExtension(fallback, data, indices);

        /// <summary>
        /// svint64_t svtbx[_s64](svint64_t fallback, svint64_t data, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> VectorTableLookupExtension(Vector<long> fallback, Vector<long> data, Vector<ulong> indices) => VectorTableLookupExtension(fallback, data, indices);

        /// <summary>
        /// svuint8_t svtbx[_u8](svuint8_t fallback, svuint8_t data, svuint8_t indices)
        /// </summary>
        public static unsafe Vector<byte> VectorTableLookupExtension(Vector<byte> fallback, Vector<byte> data, Vector<byte> indices) => VectorTableLookupExtension(fallback, data, indices);

        /// <summary>
        /// svuint16_t svtbx[_u16](svuint16_t fallback, svuint16_t data, svuint16_t indices)
        /// </summary>
        public static unsafe Vector<ushort> VectorTableLookupExtension(Vector<ushort> fallback, Vector<ushort> data, Vector<ushort> indices) => VectorTableLookupExtension(fallback, data, indices);

        /// <summary>
        /// svuint32_t svtbx[_u32](svuint32_t fallback, svuint32_t data, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> VectorTableLookupExtension(Vector<uint> fallback, Vector<uint> data, Vector<uint> indices) => VectorTableLookupExtension(fallback, data, indices);

        /// <summary>
        /// svuint64_t svtbx[_u64](svuint64_t fallback, svuint64_t data, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> VectorTableLookupExtension(Vector<ulong> fallback, Vector<ulong> data, Vector<ulong> indices) => VectorTableLookupExtension(fallback, data, indices);

        /// <summary>
        /// svfloat32_t svtbx[_f32](svfloat32_t fallback, svfloat32_t data, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<float> VectorTableLookupExtension(Vector<float> fallback, Vector<float> data, Vector<uint> indices) => VectorTableLookupExtension(fallback, data, indices);

        /// <summary>
        /// svfloat64_t svtbx[_f64](svfloat64_t fallback, svfloat64_t data, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<double> VectorTableLookupExtension(Vector<double> fallback, Vector<double> data, Vector<ulong> indices) => VectorTableLookupExtension(fallback, data, indices);


        ///  Xor : Bitwise exclusive OR of three vectors

        /// <summary>
        /// svint8_t sveor3[_s8](svint8_t op1, svint8_t op2, svint8_t op3)
        /// </summary>
        public static unsafe Vector<sbyte> Xor(Vector<sbyte> value1, Vector<sbyte> value2, Vector<sbyte> value3) => Xor(value1, value2, value3);

        /// <summary>
        /// svint16_t sveor3[_s16](svint16_t op1, svint16_t op2, svint16_t op3)
        /// </summary>
        public static unsafe Vector<short> Xor(Vector<short> value1, Vector<short> value2, Vector<short> value3) => Xor(value1, value2, value3);

        /// <summary>
        /// svint32_t sveor3[_s32](svint32_t op1, svint32_t op2, svint32_t op3)
        /// </summary>
        public static unsafe Vector<int> Xor(Vector<int> value1, Vector<int> value2, Vector<int> value3) => Xor(value1, value2, value3);

        /// <summary>
        /// svint64_t sveor3[_s64](svint64_t op1, svint64_t op2, svint64_t op3)
        /// </summary>
        public static unsafe Vector<long> Xor(Vector<long> value1, Vector<long> value2, Vector<long> value3) => Xor(value1, value2, value3);

        /// <summary>
        /// svuint8_t sveor3[_u8](svuint8_t op1, svuint8_t op2, svuint8_t op3)
        /// </summary>
        public static unsafe Vector<byte> Xor(Vector<byte> value1, Vector<byte> value2, Vector<byte> value3) => Xor(value1, value2, value3);

        /// <summary>
        /// svuint16_t sveor3[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3)
        /// </summary>
        public static unsafe Vector<ushort> Xor(Vector<ushort> value1, Vector<ushort> value2, Vector<ushort> value3) => Xor(value1, value2, value3);

        /// <summary>
        /// svuint32_t sveor3[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3)
        /// </summary>
        public static unsafe Vector<uint> Xor(Vector<uint> value1, Vector<uint> value2, Vector<uint> value3) => Xor(value1, value2, value3);

        /// <summary>
        /// svuint64_t sveor3[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3)
        /// </summary>
        public static unsafe Vector<ulong> Xor(Vector<ulong> value1, Vector<ulong> value2, Vector<ulong> value3) => Xor(value1, value2, value3);


        ///  XorRotateRight : Bitwise exclusive OR and rotate right

        /// <summary>
        /// svint8_t svxar[_n_s8](svint8_t op1, svint8_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<sbyte> XorRotateRight(Vector<sbyte> left, Vector<sbyte> right, [ConstantExpected] byte count) => XorRotateRight(left, right, count);

        /// <summary>
        /// svint16_t svxar[_n_s16](svint16_t op1, svint16_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<short> XorRotateRight(Vector<short> left, Vector<short> right, [ConstantExpected] byte count) => XorRotateRight(left, right, count);

        /// <summary>
        /// svint32_t svxar[_n_s32](svint32_t op1, svint32_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<int> XorRotateRight(Vector<int> left, Vector<int> right, [ConstantExpected] byte count) => XorRotateRight(left, right, count);

        /// <summary>
        /// svint64_t svxar[_n_s64](svint64_t op1, svint64_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<long> XorRotateRight(Vector<long> left, Vector<long> right, [ConstantExpected] byte count) => XorRotateRight(left, right, count);

        /// <summary>
        /// svuint8_t svxar[_n_u8](svuint8_t op1, svuint8_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<byte> XorRotateRight(Vector<byte> left, Vector<byte> right, [ConstantExpected] byte count) => XorRotateRight(left, right, count);

        /// <summary>
        /// svuint16_t svxar[_n_u16](svuint16_t op1, svuint16_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<ushort> XorRotateRight(Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte count) => XorRotateRight(left, right, count);

        /// <summary>
        /// svuint32_t svxar[_n_u32](svuint32_t op1, svuint32_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<uint> XorRotateRight(Vector<uint> left, Vector<uint> right, [ConstantExpected] byte count) => XorRotateRight(left, right, count);

        /// <summary>
        /// svuint64_t svxar[_n_u64](svuint64_t op1, svuint64_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<ulong> XorRotateRight(Vector<ulong> left, Vector<ulong> right, [ConstantExpected] byte count) => XorRotateRight(left, right, count);

    }
}

