// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>
    /// This class provides access to the ARM AdvSIMD hardware instructions via intrinsics
    /// </summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class AdvSimd : ArmBase
    {
        internal AdvSimd() { }

        public static new bool IsSupported { get => IsSupported; }

        [Intrinsic]
        public new abstract class Arm64 : ArmBase.Arm64
        {
            internal Arm64() { }

            public static new bool IsSupported { get => IsSupported; }

            /// <summary>
            /// float64x2_t vabsq_f64 (float64x2_t a)
            ///   A64: FABS Vd.2D, Vn.2D
            /// </summary>
            public static Vector128<double> Abs(Vector128<double> value) => Abs(value);

            /// <summary>
            /// int64x2_t vabsq_s64 (int64x2_t a)
            ///   A64: ABS Vd.2D, Vn.2D
            /// </summary>
            public static Vector128<ulong> Abs(Vector128<long> value) => Abs(value);

            /// <summary>
            /// int64x1_t vabs_s64 (int64x1_t a)
            ///   A64: ABS Dd, Dn
            /// </summary>
            public static Vector64<ulong> AbsScalar(Vector64<long> value) => AbsScalar(value);

            /// <summary>
            /// uint64x2_t vcagtq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FACGT Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> AbsoluteCompareGreaterThan(Vector128<double> left, Vector128<double> right) => AbsoluteCompareGreaterThan(left, right);

            /// <summary>
            /// uint64x1_t vcagt_f64 (float64x1_t a, float64x1_t b)
            ///   A64: FACGT Dd, Dn, Dm
            /// </summary>
            public static Vector64<double> AbsoluteCompareGreaterThanScalar(Vector64<double> left, Vector64<double> right) => AbsoluteCompareGreaterThanScalar(left, right);

            /// <summary>
            /// uint32_t vcagts_f32 (float32_t a, float32_t b)
            ///   A64: FACGT Sd, Sn, Sm
            /// </summary>
            public static Vector64<float> AbsoluteCompareGreaterThanScalar(Vector64<float> left, Vector64<float> right) => AbsoluteCompareGreaterThanScalar(left, right);

            /// <summary>
            /// uint64x2_t vcageq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FACGE Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> AbsoluteCompareGreaterThanOrEqual(Vector128<double> left, Vector128<double> right) => AbsoluteCompareGreaterThanOrEqual(left, right);

            /// <summary>
            /// uint64x1_t vcage_f64 (float64x1_t a, float64x1_t b)
            ///   A64: FACGE Dd, Dn, Dm
            /// </summary>
            public static Vector64<double> AbsoluteCompareGreaterThanOrEqualScalar(Vector64<double> left, Vector64<double> right) => AbsoluteCompareGreaterThanOrEqualScalar(left, right);

            /// <summary>
            /// uint32_t vcages_f32 (float32_t a, float32_t b)
            ///   A64: FACGE Sd, Sn, Sm
            /// </summary>
            public static Vector64<float> AbsoluteCompareGreaterThanOrEqualScalar(Vector64<float> left, Vector64<float> right) => AbsoluteCompareGreaterThanOrEqualScalar(left, right);

            /// <summary>
            /// uint64x2_t vcaltq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FACGT Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> AbsoluteCompareLessThan(Vector128<double> left, Vector128<double> right) => AbsoluteCompareLessThan(left, right);

            /// <summary>
            /// uint64x1_t vcalt_f64 (float64x1_t a, float64x1_t b)
            ///   A64: FACGT Dd, Dn, Dm
            /// </summary>
            public static Vector64<double> AbsoluteCompareLessThanScalar(Vector64<double> left, Vector64<double> right) => AbsoluteCompareLessThanScalar(left, right);

            /// <summary>
            /// uint32_t vcalts_f32 (float32_t a, float32_t b)
            ///   A64: FACGT Sd, Sn, Sm
            /// </summary>
            public static Vector64<float> AbsoluteCompareLessThanScalar(Vector64<float> left, Vector64<float> right) => AbsoluteCompareLessThanScalar(left, right);

            /// <summary>
            /// uint64x2_t vcaleq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FACGE Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> AbsoluteCompareLessThanOrEqual(Vector128<double> left, Vector128<double> right) => AbsoluteCompareLessThanOrEqual(left, right);

            /// <summary>
            /// uint64x1_t vcale_f64 (float64x1_t a, float64x1_t b)
            ///   A64: FACGE Dd, Dn, Dm
            /// </summary>
            public static Vector64<double> AbsoluteCompareLessThanOrEqualScalar(Vector64<double> left, Vector64<double> right) => AbsoluteCompareLessThanOrEqualScalar(left, right);

            /// <summary>
            /// uint32_t vcales_f32 (float32_t a, float32_t b)
            ///   A64: FACGE Sd, Sn, Sm
            /// </summary>
            public static Vector64<float> AbsoluteCompareLessThanOrEqualScalar(Vector64<float> left, Vector64<float> right) => AbsoluteCompareLessThanOrEqualScalar(left, right);

            /// <summary>
            /// float64x2_t vabdq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FABD Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> AbsoluteDifference(Vector128<double> left, Vector128<double> right) => AbsoluteDifference(left, right);

            /// <summary>
            /// float64x1_t vabd_f64 (float64x1_t a, float64x1_t b)
            ///   A64: FABD Dd, Dn, Dm
            /// </summary>
            public static Vector64<double> AbsoluteDifferenceScalar(Vector64<double> left, Vector64<double> right) => AbsoluteDifferenceScalar(left, right);

            /// <summary>
            /// float32_t vabds_f32 (float32_t a, float32_t b)
            ///   A64: FABD Sd, Sn, Sm
            /// </summary>
            public static Vector64<float> AbsoluteDifferenceScalar(Vector64<float> left, Vector64<float> right) => AbsoluteDifferenceScalar(left, right);

            /// <summary>
            /// float64x2_t vaddq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FADD Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> Add(Vector128<double> left, Vector128<double> right) => Add(left, right);

            /// <summary>
            /// uint8_t vaddv_u8(uint8x8_t a)
            ///   A64: ADDV Bd, Vn.8B
            /// </summary>
            public static Vector64<byte> AddAcross(Vector64<byte> value) => AddAcross(value);

            /// <summary>
            /// int16_t vaddv_s16(int16x4_t a)
            ///   A64: ADDV Hd, Vn.4H
            /// </summary>
            public static Vector64<short> AddAcross(Vector64<short> value) => AddAcross(value);

            /// <summary>
            /// int8_t vaddv_s8(int8x8_t a)
            ///   A64: ADDV Bd, Vn.8B
            /// </summary>
            public static Vector64<sbyte> AddAcross(Vector64<sbyte> value) => AddAcross(value);

            /// <summary>
            /// uint16_t vaddv_u16(uint16x4_t a)
            ///   A64: ADDV Hd, Vn.4H
            /// </summary>
            public static Vector64<ushort> AddAcross(Vector64<ushort> value) => AddAcross(value);

            /// <summary>
            /// uint8_t vaddvq_u8(uint8x16_t a)
            ///   A64: ADDV Bd, Vn.16B
            /// </summary>
            public static Vector128<byte> AddAcross(Vector128<byte> value) => AddAcross(value);

            /// <summary>
            /// int16_t vaddvq_s16(int16x8_t a)
            ///   A64: ADDV Hd, Vn.8H
            /// </summary>
            public static Vector128<short> AddAcross(Vector128<short> value) => AddAcross(value);

            /// <summary>
            /// int32_t vaddvq_s32(int32x4_t a)
            ///   A64: ADDV Sd, Vn.4S
            /// </summary>
            public static Vector128<int> AddAcross(Vector128<int> value) => AddAcross(value);

            /// <summary>
            /// int8_t vaddvq_s8(int8x16_t a)
            ///   A64: ADDV Bd, Vn.16B
            /// </summary>
            public static Vector128<sbyte> AddAcross(Vector128<sbyte> value) => AddAcross(value);

            /// <summary>
            /// uint16_t vaddvq_u16(uint16x8_t a)
            ///   A64: ADDV Hd, Vn.8H
            /// </summary>
            public static Vector128<ushort> AddAcross(Vector128<ushort> value) => AddAcross(value);

            /// <summary>
            /// uint32_t vaddvq_u32(uint32x4_t a)
            ///   A64: ADDV Sd, Vn.4S
            /// </summary>
            public static Vector128<uint> AddAcross(Vector128<uint> value) => AddAcross(value);

            /// <summary>
            /// uint64x2_t vceqq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FCMEQ Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> CompareEqual(Vector128<double> left, Vector128<double> right) => CompareEqual(left, right);

            /// <summary>
            /// uint64x2_t vceqq_s64 (int64x2_t a, int64x2_t b)
            ///   A64: CMEQ Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<long> CompareEqual(Vector128<long> left, Vector128<long> right) => CompareEqual(left, right);

            /// <summary>
            /// uint64x2_t vceqq_u64 (uint64x2_t a, uint64x2_t b)
            ///   A64: CMEQ Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<ulong> CompareEqual(Vector128<ulong> left, Vector128<ulong> right) => CompareEqual(left, right);

            /// <summary>
            /// uint64x1_t vceq_f64 (float64x1_t a, float64x1_t b)
            ///   A64: FCMEQ Dd, Dn, Dm
            /// </summary>
            public static Vector64<double> CompareEqualScalar(Vector64<double> left, Vector64<double> right) => CompareEqualScalar(left, right);

            /// <summary>
            /// uint64x1_t vceq_s64 (int64x1_t a, int64x1_t b)
            ///   A64: CMEQ Dd, Dn, Dm
            /// </summary>
            public static Vector64<long> CompareEqualScalar(Vector64<long> left, Vector64<long> right) => CompareEqualScalar(left, right);

            /// <summary>
            /// uint32_t vceqs_f32 (float32_t a, float32_t b)
            ///   A64: FCMEQ Sd, Sn, Sm
            /// </summary>
            public static Vector64<float> CompareEqualScalar(Vector64<float> left, Vector64<float> right) => CompareEqualScalar(left, right);

            /// <summary>
            /// uint64x1_t vceq_u64 (uint64x1_t a, uint64x1_t b)
            ///   A64: CMEQ Dd, Dn, Dm
            /// </summary>
            public static Vector64<ulong> CompareEqualScalar(Vector64<ulong> left, Vector64<ulong> right) => CompareEqualScalar(left, right);

            /// <summary>
            /// uint64x2_t vcgtq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FCMGT Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> CompareGreaterThan(Vector128<double> left, Vector128<double> right) => CompareGreaterThan(left, right);

            /// <summary>
            /// uint64x2_t vcgtq_s64 (int64x2_t a, int64x2_t b)
            ///   A64: CMGT Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<long> CompareGreaterThan(Vector128<long> left, Vector128<long> right) => CompareGreaterThan(left, right);

            /// <summary>
            /// uint64x2_t vcgtq_u64 (uint64x2_t a, uint64x2_t b)
            ///   A64: CMHI Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<ulong> CompareGreaterThan(Vector128<ulong> left, Vector128<ulong> right) => CompareGreaterThan(left, right);

            /// <summary>
            /// uint64x1_t vcgt_f64 (float64x1_t a, float64x1_t b)
            ///   A64: FCMGT Dd, Dn, Dm
            /// </summary>
            public static Vector64<double> CompareGreaterThanScalar(Vector64<double> left, Vector64<double> right) => CompareGreaterThanScalar(left, right);

            /// <summary>
            /// uint64x1_t vcgt_s64 (int64x1_t a, int64x1_t b)
            ///   A64: CMGT Dd, Dn, Dm
            /// </summary>
            public static Vector64<long> CompareGreaterThanScalar(Vector64<long> left, Vector64<long> right) => CompareGreaterThanScalar(left, right);

            /// <summary>
            /// uint32_t vcgts_f32 (float32_t a, float32_t b)
            ///   A64: FCMGT Sd, Sn, Sm
            /// </summary>
            public static Vector64<float> CompareGreaterThanScalar(Vector64<float> left, Vector64<float> right) => CompareGreaterThanScalar(left, right);

            /// <summary>
            /// uint64x1_t vcgt_u64 (uint64x1_t a, uint64x1_t b)
            ///   A64: CMHI Dd, Dn, Dm
            /// </summary>
            public static Vector64<ulong> CompareGreaterThanScalar(Vector64<ulong> left, Vector64<ulong> right) => CompareGreaterThanScalar(left, right);

            /// <summary>
            /// uint64x2_t vcgeq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FCMGE Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> CompareGreaterThanOrEqual(Vector128<double> left, Vector128<double> right) => CompareGreaterThanOrEqual(left, right);

            /// <summary>
            /// uint64x2_t vcgeq_s64 (int64x2_t a, int64x2_t b)
            ///   A64: CMGE Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<long> CompareGreaterThanOrEqual(Vector128<long> left, Vector128<long> right) => CompareGreaterThanOrEqual(left, right);

            /// <summary>
            /// uint64x2_t vcgeq_u64 (uint64x2_t a, uint64x2_t b)
            ///   A64: CMHS Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<ulong> CompareGreaterThanOrEqual(Vector128<ulong> left, Vector128<ulong> right) => CompareGreaterThanOrEqual(left, right);

            /// <summary>
            /// uint64x1_t vcge_f64 (float64x1_t a, float64x1_t b)
            ///   A64: FCMGE Dd, Dn, Dm
            /// </summary>
            public static Vector64<double> CompareGreaterThanOrEqualScalar(Vector64<double> left, Vector64<double> right) => CompareGreaterThanOrEqualScalar(left, right);

            /// <summary>
            /// uint64x1_t vcge_s64 (int64x1_t a, int64x1_t b)
            ///   A64: CMGE Dd, Dn, Dm
            /// </summary>
            public static Vector64<long> CompareGreaterThanOrEqualScalar(Vector64<long> left, Vector64<long> right) => CompareGreaterThanOrEqualScalar(left, right);

            /// <summary>
            /// uint32_t vcges_f32 (float32_t a, float32_t b)
            ///   A64: FCMGE Sd, Sn, Sm
            /// </summary>
            public static Vector64<float> CompareGreaterThanOrEqualScalar(Vector64<float> left, Vector64<float> right) => CompareGreaterThanOrEqualScalar(left, right);

            /// <summary>
            /// uint64x1_t vcge_u64 (uint64x1_t a, uint64x1_t b)
            ///   A64: CMHS Dd, Dn, Dm
            /// </summary>
            public static Vector64<ulong> CompareGreaterThanOrEqualScalar(Vector64<ulong> left, Vector64<ulong> right) => CompareGreaterThanOrEqualScalar(left, right);

            /// <summary>
            /// uint64x2_t vcltq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FCMGT Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> CompareLessThan(Vector128<double> left, Vector128<double> right) => CompareLessThan(left, right);

            /// <summary>
            /// uint64x2_t vcltq_s64 (int64x2_t a, int64x2_t b)
            ///   A64: CMGT Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<long> CompareLessThan(Vector128<long> left, Vector128<long> right) => CompareLessThan(left, right);

            /// <summary>
            /// uint64x2_t vcltq_u64 (uint64x2_t a, uint64x2_t b)
            ///   A64: CMHI Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<ulong> CompareLessThan(Vector128<ulong> left, Vector128<ulong> right) => CompareLessThan(left, right);

            /// <summary>
            /// uint64x1_t vclt_f64 (float64x1_t a, float64x1_t b)
            ///   A64: FCMGT Dd, Dn, Dm
            /// </summary>
            public static Vector64<double> CompareLessThanScalar(Vector64<double> left, Vector64<double> right) => CompareLessThanScalar(left, right);

            /// <summary>
            /// uint64x1_t vclt_s64 (int64x1_t a, int64x1_t b)
            ///   A64: CMGT Dd, Dn, Dm
            /// </summary>
            public static Vector64<long> CompareLessThanScalar(Vector64<long> left, Vector64<long> right) => CompareLessThanScalar(left, right);

            /// <summary>
            /// uint32_t vclts_f32 (float32_t a, float32_t b)
            ///   A64: FCMGT Sd, Sn, Sm
            /// </summary>
            public static Vector64<float> CompareLessThanScalar(Vector64<float> left, Vector64<float> right) => CompareLessThanScalar(left, right);

            /// <summary>
            /// uint64x1_t vclt_u64 (uint64x1_t a, uint64x1_t b)
            ///   A64: CMHI Dd, Dn, Dm
            /// </summary>
            public static Vector64<ulong> CompareLessThanScalar(Vector64<ulong> left, Vector64<ulong> right) => CompareLessThanScalar(left, right);

            /// <summary>
            /// uint64x2_t vcleq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FCMGE Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> CompareLessThanOrEqual(Vector128<double> left, Vector128<double> right) => CompareLessThanOrEqual(left, right);

            /// <summary>
            /// uint64x2_t vcleq_s64 (int64x2_t a, int64x2_t b)
            ///   A64: CMGE Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<long> CompareLessThanOrEqual(Vector128<long> left, Vector128<long> right) => CompareLessThanOrEqual(left, right);

            /// <summary>
            /// uint64x2_t vcleq_u64 (uint64x2_t a, uint64x2_t b)
            ///   A64: CMHS Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<ulong> CompareLessThanOrEqual(Vector128<ulong> left, Vector128<ulong> right) => CompareLessThanOrEqual(left, right);

            /// <summary>
            /// uint64x1_t vcle_f64 (float64x1_t a, float64x1_t b)
            ///   A64: FCMGE Dd, Dn, Dm
            /// </summary>
            public static Vector64<double> CompareLessThanOrEqualScalar(Vector64<double> left, Vector64<double> right) => CompareLessThanOrEqualScalar(left, right);

            /// <summary>
            /// uint64x1_t vcle_s64 (int64x1_t a, int64x1_t b)
            ///   A64: CMGE Dd, Dn, Dm
            /// </summary>
            public static Vector64<long> CompareLessThanOrEqualScalar(Vector64<long> left, Vector64<long> right) => CompareLessThanOrEqualScalar(left, right);

            /// <summary>
            /// uint32_t vcles_f32 (float32_t a, float32_t b)
            ///   A64: FCMGE Sd, Sn, Sm
            /// </summary>
            public static Vector64<float> CompareLessThanOrEqualScalar(Vector64<float> left, Vector64<float> right) => CompareLessThanOrEqualScalar(left, right);

            /// <summary>
            /// uint64x1_t vcle_u64 (uint64x1_t a, uint64x1_t b)
            ///   A64: CMHS Dd, Dn, Dm
            /// </summary>
            public static Vector64<ulong> CompareLessThanOrEqualScalar(Vector64<ulong> left, Vector64<ulong> right) => CompareLessThanOrEqualScalar(left, right);

            /// <summary>
            /// uint64x2_t vtstq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: CMTST Vd.2D, Vn.2D, Vm.2D
            /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
            /// </summary>
            public static Vector128<double> CompareTest(Vector128<double> left, Vector128<double> right) => CompareTest(left, right);

            /// <summary>
            /// uint64x2_t vtstq_s64 (int64x2_t a, int64x2_t b)
            ///   A64: CMTST Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<long> CompareTest(Vector128<long> left, Vector128<long> right) => CompareTest(left, right);

            /// <summary>
            /// uint64x2_t vtstq_u64 (uint64x2_t a, uint64x2_t b)
            ///   A64: CMTST Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<ulong> CompareTest(Vector128<ulong> left, Vector128<ulong> right) => CompareTest(left, right);

            /// <summary>
            /// uint64x1_t vtst_f64 (float64x1_t a, float64x1_t b)
            ///   A64: CMTST Dd, Dn, Dm
            /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
            /// </summary>
            public static Vector64<double> CompareTestScalar(Vector64<double> left, Vector64<double> right) => CompareTestScalar(left, right);

            /// <summary>
            /// uint64x1_t vtst_s64 (int64x1_t a, int64x1_t b)
            ///   A64: CMTST Dd, Dn, Dm
            /// </summary>
            public static Vector64<long> CompareTestScalar(Vector64<long> left, Vector64<long> right) => CompareTestScalar(left, right);

            /// <summary>
            /// uint64x1_t vtst_u64 (uint64x1_t a, uint64x1_t b)
            ///   A64: CMTST Dd, Dn, Dm
            /// </summary>
            public static Vector64<ulong> CompareTestScalar(Vector64<ulong> left, Vector64<ulong> right) => CompareTestScalar(left, right);

            /// <summary>
            /// float64x2_t vsubq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FSUB Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> Subtract(Vector128<double> left, Vector128<double> right) => Add(left, right);

            /// <summary>
            /// uint8x8_t vrbit_u8 (uint8x8_t a)
            ///   A64: RBIT Vd.8B, Vn.8B
            /// </summary>
            public static Vector64<byte> ReverseElementBits(Vector64<byte> value) => ReverseElementBits(value);

            /// <summary>
            /// int8x8_t vrbit_s8 (int8x8_t a)
            ///   A64: RBIT Vd.8B, Vn.8B
            /// </summary>
            public static Vector64<sbyte> ReverseElementBits(Vector64<sbyte> value) => ReverseElementBits(value);

            /// <summary>
            /// uint8x16_t vrbitq_u8 (uint8x16_t a)
            ///   A64: RBIT Vd.16B, Vn.16B
            /// </summary>
            public static Vector128<byte> ReverseElementBits(Vector128<byte> value) => ReverseElementBits(value);

            /// <summary>
            /// int8x16_t vrbitq_s8 (int8x16_t a)
            ///   A64: RBIT Vd.16B, Vn.16B
            /// </summary>
            public static Vector128<sbyte> ReverseElementBits(Vector128<sbyte> value) => ReverseElementBits(value);

            /// <summary>
            /// uint8x8_t vtrn1_u8(uint8x8_t a, uint8x8_t b)
            ///   A64: TRN1 Vd.8B, Vn.8B, Vm.8B
            /// </summary>
            public static Vector64<byte> TransposeEven(Vector64<byte> left, Vector64<byte> right) => TransposeEven(left, right);

            /// <summary>
            /// int16x4_t vtrn1_s16(int16x4_t a, int16x4_t b)
            ///   A64: TRN1 Vd.4H, Vn.4H, Vm.4H
            /// </summary>
            public static Vector64<short> TransposeEven(Vector64<short> left, Vector64<short> right) => TransposeEven(left, right);

            /// <summary>
            /// int32x2_t vtrn1_s32(int32x2_t a, int32x2_t b)
            ///   A64: TRN1 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<int> TransposeEven(Vector64<int> left, Vector64<int> right) => TransposeEven(left, right);

            /// <summary>
            /// int8x8_t vtrn1_s8(int8x8_t a, int8x8_t b)
            ///   A64: TRN1 Vd.8B, Vn.8B, Vm.8B
            /// </summary>
            public static Vector64<sbyte> TransposeEven(Vector64<sbyte> left, Vector64<sbyte> right) => TransposeEven(left, right);

            /// <summary>
            /// float32x2_t vtrn1_f32(float32x2_t a, float32x2_t b)
            ///   A64: TRN1 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<float> TransposeEven(Vector64<float> left, Vector64<float> right) => TransposeEven(left, right);

            /// <summary>
            /// uint16x4_t vtrn1_u16(uint16x4_t a, uint16x4_t b)
            ///   A64: TRN1 Vd.4H, Vn.4H, Vm.4H
            /// </summary>
            public static Vector64<ushort> TransposeEven(Vector64<ushort> left, Vector64<ushort> right) => TransposeEven(left, right);

            /// <summary>
            /// uint32x2_t vtrn1_u32(uint32x2_t a, uint32x2_t b)
            ///   A64: TRN1 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<uint> TransposeEven(Vector64<uint> left, Vector64<uint> right) => TransposeEven(left, right);

            /// <summary>
            /// uint8x16_t vtrn1q_u8(uint8x16_t a, uint8x16_t b)
            ///   A64: TRN1 Vd.16B, Vn.16B, Vm.16B
            /// </summary>
            public static Vector128<byte> TransposeEven(Vector128<byte> left, Vector128<byte> right) => TransposeEven(left, right);

            /// <summary>
            /// float64x2_t vtrn1q_f64(float64x2_t a, float64x2_t b)
            ///   A64: TRN1 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> TransposeEven(Vector128<double> left, Vector128<double> right) => TransposeEven(left, right);

            /// <summary>
            /// int16x8_t vtrn1q_s16(int16x8_t a, int16x8_t b)
            ///   A64: TRN1 Vd.8H, Vn.8H, Vm.8H
            /// </summary>
            public static Vector128<short> TransposeEven(Vector128<short> left, Vector128<short> right) => TransposeEven(left, right);

            /// <summary>
            /// int32x4_t vtrn1q_s32(int32x4_t a, int32x4_t b)
            ///   A64: TRN1 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<int> TransposeEven(Vector128<int> left, Vector128<int> right) => TransposeEven(left, right);

            /// <summary>
            /// int64x2_t vtrn1q_s64(int64x2_t a, int64x2_t b)
            ///   A64: TRN1 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<long> TransposeEven(Vector128<long> left, Vector128<long> right) => TransposeEven(left, right);

            /// <summary>
            /// int8x16_t vtrn1q_u8(int8x16_t a, int8x16_t b)
            ///   A64: TRN1 Vd.16B, Vn.16B, Vm.16B
            /// </summary>
            public static Vector128<sbyte> TransposeEven(Vector128<sbyte> left, Vector128<sbyte> right) => TransposeEven(left, right);

            /// <summary>
            /// float32x4_t vtrn1q_f32(float32x4_t a, float32x4_t b)
            ///   A64: TRN1 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<float> TransposeEven(Vector128<float> left, Vector128<float> right) => TransposeEven(left, right);

            /// <summary>
            /// uint16x8_t vtrn1q_u16(uint16x8_t a, uint16x8_t b)
            ///   A64: TRN1 Vd.8H, Vn.8H, Vm.8H
            /// </summary>
            public static Vector128<ushort> TransposeEven(Vector128<ushort> left, Vector128<ushort> right) => TransposeEven(left, right);

            /// <summary>
            /// uint32x4_t vtrn1q_u32(uint32x4_t a, uint32x4_t b)
            ///   A64: TRN1 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<uint> TransposeEven(Vector128<uint> left, Vector128<uint> right) => TransposeEven(left, right);

            /// <summary>
            /// uint64x2_t vtrn1q_u64(uint64x2_t a, uint64x2_t b)
            ///   A64: TRN1 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<ulong> TransposeEven(Vector128<ulong> left, Vector128<ulong> right) => TransposeEven(left, right);

            /// <summary>
            /// uint8x8_t vtrn2_u8(uint8x8_t a, uint8x8_t b)
            ///   A64: TRN2 Vd.8B, Vn.8B, Vm.8B
            /// </summary>
            public static Vector64<byte> TransposeOdd(Vector64<byte> left, Vector64<byte> right) => TransposeOdd(left, right);

            /// <summary>
            /// int16x4_t vtrn2_s16(int16x4_t a, int16x4_t b)
            ///   A64: TRN2 Vd.4H, Vn.4H, Vm.4H
            /// </summary>
            public static Vector64<short> TransposeOdd(Vector64<short> left, Vector64<short> right) => TransposeOdd(left, right);

            /// <summary>
            /// int32x2_t vtrn2_s32(int32x2_t a, int32x2_t b)
            ///   A64: TRN2 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<int> TransposeOdd(Vector64<int> left, Vector64<int> right) => TransposeOdd(left, right);

            /// <summary>
            /// int8x8_t vtrn2_s8(int8x8_t a, int8x8_t b)
            ///   A64: TRN2 Vd.8B, Vn.8B, Vm.8B
            /// </summary>
            public static Vector64<sbyte> TransposeOdd(Vector64<sbyte> left, Vector64<sbyte> right) => TransposeOdd(left, right);

            /// <summary>
            /// float32x2_t vtrn2_f32(float32x2_t a, float32x2_t b)
            ///   A64: TRN2 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<float> TransposeOdd(Vector64<float> left, Vector64<float> right) => TransposeOdd(left, right);

            /// <summary>
            /// uint16x4_t vtrn2_u16(uint16x4_t a, uint16x4_t b)
            ///   A64: TRN2 Vd.4H, Vn.4H, Vm.4H
            /// </summary>
            public static Vector64<ushort> TransposeOdd(Vector64<ushort> left, Vector64<ushort> right) => TransposeOdd(left, right);

            /// <summary>
            /// uint32x2_t vtrn2_u32(uint32x2_t a, uint32x2_t b)
            ///   A64: TRN2 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<uint> TransposeOdd(Vector64<uint> left, Vector64<uint> right) => TransposeOdd(left, right);

            /// <summary>
            /// uint8x16_t vtrn2q_u8(uint8x16_t a, uint8x16_t b)
            ///   A64: TRN2 Vd.16B, Vn.16B, Vm.16B
            /// </summary>
            public static Vector128<byte> TransposeOdd(Vector128<byte> left, Vector128<byte> right) => TransposeOdd(left, right);

            /// <summary>
            /// float64x2_t vtrn2q_f64(float64x2_t a, float64x2_t b)
            ///   A64: TRN2 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> TransposeOdd(Vector128<double> left, Vector128<double> right) => TransposeOdd(left, right);

            /// <summary>
            /// int16x8_t vtrn2q_s16(int16x8_t a, int16x8_t b)
            ///   A64: TRN2 Vd.8H, Vn.8H, Vm.8H
            /// </summary>
            public static Vector128<short> TransposeOdd(Vector128<short> left, Vector128<short> right) => TransposeOdd(left, right);

            /// <summary>
            /// int32x4_t vtrn2q_s32(int32x4_t a, int32x4_t b)
            ///   A64: TRN2 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<int> TransposeOdd(Vector128<int> left, Vector128<int> right) => TransposeOdd(left, right);

            /// <summary>
            /// int64x2_t vtrn2q_s64(int64x2_t a, int64x2_t b)
            ///   A64: TRN2 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<long> TransposeOdd(Vector128<long> left, Vector128<long> right) => TransposeOdd(left, right);

            /// <summary>
            /// int8x16_t vtrn2q_u8(int8x16_t a, int8x16_t b)
            ///   A64: TRN2 Vd.16B, Vn.16B, Vm.16B
            /// </summary>
            public static Vector128<sbyte> TransposeOdd(Vector128<sbyte> left, Vector128<sbyte> right) => TransposeOdd(left, right);

            /// <summary>
            /// float32x4_t vtrn2q_f32(float32x4_t a, float32x4_t b)
            ///   A64: TRN2 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<float> TransposeOdd(Vector128<float> left, Vector128<float> right) => TransposeOdd(left, right);

            /// <summary>
            /// uint16x8_t vtrn2q_u16(uint16x8_t a, uint16x8_t b)
            ///   A64: TRN2 Vd.8H, Vn.8H, Vm.8H
            /// </summary>
            public static Vector128<ushort> TransposeOdd(Vector128<ushort> left, Vector128<ushort> right) => TransposeOdd(left, right);

            /// <summary>
            /// uint32x4_t vtrn1q_u32(uint32x4_t a, uint32x4_t b)
            ///   A64: TRN1 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<uint> TransposeOdd(Vector128<uint> left, Vector128<uint> right) => TransposeOdd(left, right);

            /// <summary>
            /// uint64x2_t vtrn1q_u64(uint64x2_t a, uint64x2_t b)
            ///   A64: TRN1 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<ulong> TransposeOdd(Vector128<ulong> left, Vector128<ulong> right) => TransposeOdd(left, right);

            /// <summary>
            /// uint8x8_t vuzp1_u8(uint8x8_t a, uint8x8_t b)
            ///   A64: UZP1 Vd.8B, Vn.8B, Vm.8B
            /// </summary>
            public static Vector64<byte> UnzipEven(Vector64<byte> left, Vector64<byte> right) => UnzipEven(left, right);

            /// <summary>
            /// int16x4_t vuzp1_s16(int16x4_t a, int16x4_t b)
            ///   A64: UZP1 Vd.4H, Vn.4H, Vm.4H
            /// </summary>
            public static Vector64<short> UnzipEven(Vector64<short> left, Vector64<short> right) => UnzipEven(left, right);

            /// <summary>
            /// int32x2_t vuzp1_s32(int32x2_t a, int32x2_t b)
            ///   A64: UZP1 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<int> UnzipEven(Vector64<int> left, Vector64<int> right) => UnzipEven(left, right);

            /// <summary>
            /// int8x8_t vuzp1_s8(int8x8_t a, int8x8_t b)
            ///   A64: UZP1 Vd.8B, Vn.8B, Vm.8B
            /// </summary>
            public static Vector64<sbyte> UnzipEven(Vector64<sbyte> left, Vector64<sbyte> right) => UnzipEven(left, right);

            /// <summary>
            /// float32x2_t vuzp1_f32(float32x2_t a, float32x2_t b)
            ///   A64: UZP1 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<float> UnzipEven(Vector64<float> left, Vector64<float> right) => UnzipEven(left, right);

            /// <summary>
            /// uint16x4_t vuzp1_u16(uint16x4_t a, uint16x4_t b)
            ///   A64: UZP1 Vd.4H, Vn.4H, Vm.4H
            /// </summary>
            public static Vector64<ushort> UnzipEven(Vector64<ushort> left, Vector64<ushort> right) => UnzipEven(left, right);

            /// <summary>
            /// uint32x2_t vuzp1_u32(uint32x2_t a, uint32x2_t b)
            ///   A64: UZP1 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<uint> UnzipEven(Vector64<uint> left, Vector64<uint> right) => UnzipEven(left, right);

            /// <summary>
            /// uint8x16_t vuzp1q_u8(uint8x16_t a, uint8x16_t b)
            ///   A64: UZP1 Vd.16B, Vn.16B, Vm.16B
            /// </summary>
            public static Vector128<byte> UnzipEven(Vector128<byte> left, Vector128<byte> right) => UnzipEven(left, right);

            /// <summary>
            /// float64x2_t vuzp1q_f64(float64x2_t a, float64x2_t b)
            ///   A64: UZP1 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> UnzipEven(Vector128<double> left, Vector128<double> right) => UnzipEven(left, right);

            /// <summary>
            /// int16x8_t vuzp1q_s16(int16x8_t a, int16x8_t b)
            ///   A64: UZP1 Vd.8H, Vn.8H, Vm.8H
            /// </summary>
            public static Vector128<short> UnzipEven(Vector128<short> left, Vector128<short> right) => UnzipEven(left, right);

            /// <summary>
            /// int32x4_t vuzp1q_s32(int32x4_t a, int32x4_t b)
            ///   A64: UZP1 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<int> UnzipEven(Vector128<int> left, Vector128<int> right) => UnzipEven(left, right);

            /// <summary>
            /// int64x2_t vuzp1q_s64(int64x2_t a, int64x2_t b)
            ///   A64: UZP1 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<long> UnzipEven(Vector128<long> left, Vector128<long> right) => UnzipEven(left, right);

            /// <summary>
            /// int8x16_t vuzp1q_u8(int8x16_t a, int8x16_t b)
            ///   A64: UZP1 Vd.16B, Vn.16B, Vm.16B
            /// </summary>
            public static Vector128<sbyte> UnzipEven(Vector128<sbyte> left, Vector128<sbyte> right) => UnzipEven(left, right);

            /// <summary>
            /// float32x4_t vuzp1q_f32(float32x4_t a, float32x4_t b)
            ///   A64: UZP1 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<float> UnzipEven(Vector128<float> left, Vector128<float> right) => UnzipEven(left, right);

            /// <summary>
            /// uint16x8_t vuzp1q_u16(uint16x8_t a, uint16x8_t b)
            ///   A64: UZP1 Vd.8H, Vn.8H, Vm.8H
            /// </summary>
            public static Vector128<ushort> UnzipEven(Vector128<ushort> left, Vector128<ushort> right) => UnzipEven(left, right);

            /// <summary>
            /// uint32x4_t vuzp1q_u32(uint32x4_t a, uint32x4_t b)
            ///   A64: UZP1 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<uint> UnzipEven(Vector128<uint> left, Vector128<uint> right) => UnzipEven(left, right);

            /// <summary>
            /// uint64x2_t vuzp1q_u64(uint64x2_t a, uint64x2_t b)
            ///   A64: UZP1 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<ulong> UnzipEven(Vector128<ulong> left, Vector128<ulong> right) => UnzipEven(left, right);

            /// <summary>
            /// uint8x8_t vuzp2_u8(uint8x8_t a, uint8x8_t b)
            ///   A64: UZP2 Vd.8B, Vn.8B, Vm.8B
            /// </summary>
            public static Vector64<byte> UnzipOdd(Vector64<byte> left, Vector64<byte> right) => UnzipOdd(left, right);

            /// <summary>
            /// int16x4_t vuzp2_s16(int16x4_t a, int16x4_t b)
            ///   A64: UZP2 Vd.4H, Vn.4H, Vm.4H
            /// </summary>
            public static Vector64<short> UnzipOdd(Vector64<short> left, Vector64<short> right) => UnzipOdd(left, right);

            /// <summary>
            /// int32x2_t vuzp2_s32(int32x2_t a, int32x2_t b)
            ///   A64: UZP2 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<int> UnzipOdd(Vector64<int> left, Vector64<int> right) => UnzipOdd(left, right);

            /// <summary>
            /// int8x8_t vuzp2_s8(int8x8_t a, int8x8_t b)
            ///   A64: UZP2 Vd.8B, Vn.8B, Vm.8B
            /// </summary>
            public static Vector64<sbyte> UnzipOdd(Vector64<sbyte> left, Vector64<sbyte> right) => UnzipOdd(left, right);

            /// <summary>
            /// float32x2_t vuzp2_f32(float32x2_t a, float32x2_t b)
            ///   A64: UZP2 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<float> UnzipOdd(Vector64<float> left, Vector64<float> right) => UnzipOdd(left, right);

            /// <summary>
            /// uint16x4_t vuzp2_u16(uint16x4_t a, uint16x4_t b)
            ///   A64: UZP2 Vd.4H, Vn.4H, Vm.4H
            /// </summary>
            public static Vector64<ushort> UnzipOdd(Vector64<ushort> left, Vector64<ushort> right) => UnzipOdd(left, right);

            /// <summary>
            /// uint32x2_t vuzp2_u32(uint32x2_t a, uint32x2_t b)
            ///   A64: UZP2 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<uint> UnzipOdd(Vector64<uint> left, Vector64<uint> right) => UnzipOdd(left, right);

            /// <summary>
            /// uint8x16_t vuzp2q_u8(uint8x16_t a, uint8x16_t b)
            ///   A64: UZP2 Vd.16B, Vn.16B, Vm.16B
            /// </summary>
            public static Vector128<byte> UnzipOdd(Vector128<byte> left, Vector128<byte> right) => UnzipOdd(left, right);

            /// <summary>
            /// float64x2_t vuzp2q_f64(float64x2_t a, float64x2_t b)
            ///   A64: UZP2 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> UnzipOdd(Vector128<double> left, Vector128<double> right) => UnzipOdd(left, right);

            /// <summary>
            /// int16x8_t vuzp2q_s16(int16x8_t a, int16x8_t b)
            ///   A64: UZP2 Vd.8H, Vn.8H, Vm.8H
            /// </summary>
            public static Vector128<short> UnzipOdd(Vector128<short> left, Vector128<short> right) => UnzipOdd(left, right);

            /// <summary>
            /// int32x4_t vuzp2q_s32(int32x4_t a, int32x4_t b)
            ///   A64: UZP2 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<int> UnzipOdd(Vector128<int> left, Vector128<int> right) => UnzipOdd(left, right);

            /// <summary>
            /// int64x2_t vuzp2q_s64(int64x2_t a, int64x2_t b)
            ///   A64: UZP2 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<long> UnzipOdd(Vector128<long> left, Vector128<long> right) => UnzipOdd(left, right);

            /// <summary>
            /// int8x16_t vuzp2q_u8(int8x16_t a, int8x16_t b)
            ///   A64: UZP2 Vd.16B, Vn.16B, Vm.16B
            /// </summary>
            public static Vector128<sbyte> UnzipOdd(Vector128<sbyte> left, Vector128<sbyte> right) => UnzipOdd(left, right);

            /// <summary>
            /// float32x4_t vuzp2q_f32(float32x4_t a, float32x4_t b)
            ///   A64: UZP2 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<float> UnzipOdd(Vector128<float> left, Vector128<float> right) => UnzipOdd(left, right);

            /// <summary>
            /// uint16x8_t vuzp2q_u16(uint16x8_t a, uint16x8_t b)
            ///   A64: UZP2 Vd.8H, Vn.8H, Vm.8H
            /// </summary>
            public static Vector128<ushort> UnzipOdd(Vector128<ushort> left, Vector128<ushort> right) => UnzipOdd(left, right);

            /// <summary>
            /// uint32x4_t vuzp2q_u32(uint32x4_t a, uint32x4_t b)
            ///   A64: UZP2 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<uint> UnzipOdd(Vector128<uint> left, Vector128<uint> right) => UnzipOdd(left, right);

            /// <summary>
            /// uint64x2_t vuzp2q_u64(uint64x2_t a, uint64x2_t b)
            ///   A64: UZP2 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<ulong> UnzipOdd(Vector128<ulong> left, Vector128<ulong> right) => UnzipOdd(left, right);

            /// <summary>
            /// uint8x8_t vzip2_u8(uint8x8_t a, uint8x8_t b)
            ///   A64: ZIP2 Vd.8B, Vn.8B, Vm.8B
            /// </summary>
            public static Vector64<byte> ZipHigh(Vector64<byte> left, Vector64<byte> right) => ZipHigh(left, right);

            /// <summary>
            /// int16x4_t vzip2_s16(int16x4_t a, int16x4_t b)
            ///   A64: ZIP2 Vd.4H, Vn.4H, Vm.4H
            /// </summary>
            public static Vector64<short> ZipHigh(Vector64<short> left, Vector64<short> right) => ZipHigh(left, right);

            /// <summary>
            /// int32x2_t vzip2_s32(int32x2_t a, int32x2_t b)
            ///   A64: ZIP2 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<int> ZipHigh(Vector64<int> left, Vector64<int> right) => ZipHigh(left, right);

            /// <summary>
            /// int8x8_t vzip2_s8(int8x8_t a, int8x8_t b)
            ///   A64: ZIP2 Vd.8B, Vn.8B, Vm.8B
            /// </summary>
            public static Vector64<sbyte> ZipHigh(Vector64<sbyte> left, Vector64<sbyte> right) => ZipHigh(left, right);

            /// <summary>
            /// float32x2_t vzip2_f32(float32x2_t a, float32x2_t b)
            ///   A64: ZIP2 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<float> ZipHigh(Vector64<float> left, Vector64<float> right) => ZipHigh(left, right);

            /// <summary>
            /// uint16x4_t vzip2_u16(uint16x4_t a, uint16x4_t b)
            ///   A64: ZIP2 Vd.4H, Vn.4H, Vm.4H
            /// </summary>
            public static Vector64<ushort> ZipHigh(Vector64<ushort> left, Vector64<ushort> right) => ZipHigh(left, right);

            /// <summary>
            /// uint32x2_t vzip2_u32(uint32x2_t a, uint32x2_t b)
            ///   A64: ZIP2 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<uint> ZipHigh(Vector64<uint> left, Vector64<uint> right) => ZipHigh(left, right);

            /// <summary>
            /// uint8x16_t vzip2q_u8(uint8x16_t a, uint8x16_t b)
            ///   A64: ZIP2 Vd.16B, Vn.16B, Vm.16B
            /// </summary>
            public static Vector128<byte> ZipHigh(Vector128<byte> left, Vector128<byte> right) => ZipHigh(left, right);

            /// <summary>
            /// float64x2_t vzip2q_f64(float64x2_t a, float64x2_t b)
            ///   A64: ZIP2 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> ZipHigh(Vector128<double> left, Vector128<double> right) => ZipHigh(left, right);

            /// <summary>
            /// int16x8_t vzip2q_s16(int16x8_t a, int16x8_t b)
            ///   A64: ZIP2 Vd.8H, Vn.8H, Vm.8H
            /// </summary>
            public static Vector128<short> ZipHigh(Vector128<short> left, Vector128<short> right) => ZipHigh(left, right);

            /// <summary>
            /// int32x4_t vzip2q_s32(int32x4_t a, int32x4_t b)
            ///   A64: ZIP2 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<int> ZipHigh(Vector128<int> left, Vector128<int> right) => ZipHigh(left, right);

            /// <summary>
            /// int64x2_t vzip2q_s64(int64x2_t a, int64x2_t b)
            ///   A64: ZIP2 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<long> ZipHigh(Vector128<long> left, Vector128<long> right) => ZipHigh(left, right);

            /// <summary>
            /// int8x16_t vzip2q_u8(int8x16_t a, int8x16_t b)
            ///   A64: ZIP2 Vd.16B, Vn.16B, Vm.16B
            /// </summary>
            public static Vector128<sbyte> ZipHigh(Vector128<sbyte> left, Vector128<sbyte> right) => ZipHigh(left, right);

            /// <summary>
            /// float32x4_t vzip2q_f32(float32x4_t a, float32x4_t b)
            ///   A64: ZIP2 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<float> ZipHigh(Vector128<float> left, Vector128<float> right) => ZipHigh(left, right);

            /// <summary>
            /// uint16x8_t vzip2q_u16(uint16x8_t a, uint16x8_t b)
            ///   A64: ZIP2 Vd.8H, Vn.8H, Vm.8H
            /// </summary>
            public static Vector128<ushort> ZipHigh(Vector128<ushort> left, Vector128<ushort> right) => ZipHigh(left, right);

            /// <summary>
            /// uint32x4_t vzip2q_u32(uint32x4_t a, uint32x4_t b)
            ///   A64: ZIP2 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<uint> ZipHigh(Vector128<uint> left, Vector128<uint> right) => ZipHigh(left, right);

            /// <summary>
            /// uint64x2_t vzip2q_u64(uint64x2_t a, uint64x2_t b)
            ///   A64: ZIP2 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<ulong> ZipHigh(Vector128<ulong> left, Vector128<ulong> right) => ZipHigh(left, right);

            /// <summary>
            /// uint8x8_t vzip1_u8(uint8x8_t a, uint8x8_t b)
            ///   A64: ZIP1 Vd.8B, Vn.8B, Vm.8B
            /// </summary>
            public static Vector64<byte> ZipLow(Vector64<byte> left, Vector64<byte> right) => ZipLow(left, right);

            /// <summary>
            /// int16x4_t vzip1_s16(int16x4_t a, int16x4_t b)
            ///   A64: ZIP1 Vd.4H, Vn.4H, Vm.4H
            /// </summary>
            public static Vector64<short> ZipLow(Vector64<short> left, Vector64<short> right) => ZipLow(left, right);

            /// <summary>
            /// int32x2_t vzip1_s32(int32x2_t a, int32x2_t b)
            ///   A64: ZIP1 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<int> ZipLow(Vector64<int> left, Vector64<int> right) => ZipLow(left, right);

            /// <summary>
            /// int8x8_t vzip1_s8(int8x8_t a, int8x8_t b)
            ///   A64: ZIP1 Vd.8B, Vn.8B, Vm.8B
            /// </summary>
            public static Vector64<sbyte> ZipLow(Vector64<sbyte> left, Vector64<sbyte> right) => ZipLow(left, right);

            /// <summary>
            /// float32x2_t vzip1_f32(float32x2_t a, float32x2_t b)
            ///   A64: ZIP1 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<float> ZipLow(Vector64<float> left, Vector64<float> right) => ZipLow(left, right);

            /// <summary>
            /// uint16x4_t vzip1_u16(uint16x4_t a, uint16x4_t b)
            ///   A64: ZIP1 Vd.4H, Vn.4H, Vm.4H
            /// </summary>
            public static Vector64<ushort> ZipLow(Vector64<ushort> left, Vector64<ushort> right) => ZipLow(left, right);

            /// <summary>
            /// uint32x2_t vzip1_u32(uint32x2_t a, uint32x2_t b)
            ///   A64: ZIP1 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<uint> ZipLow(Vector64<uint> left, Vector64<uint> right) => ZipLow(left, right);

            /// <summary>
            /// uint8x16_t vzip1q_u8(uint8x16_t a, uint8x16_t b)
            ///   A64: ZIP1 Vd.16B, Vn.16B, Vm.16B
            /// </summary>
            public static Vector128<byte> ZipLow(Vector128<byte> left, Vector128<byte> right) => ZipLow(left, right);

            /// <summary>
            /// float64x2_t vzip1q_f64(float64x2_t a, float64x2_t b)
            ///   A64: ZIP1 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> ZipLow(Vector128<double> left, Vector128<double> right) => ZipLow(left, right);

            /// <summary>
            /// int16x8_t vzip1q_s16(int16x8_t a, int16x8_t b)
            ///   A64: ZIP1 Vd.8H, Vn.8H, Vm.8H
            /// </summary>
            public static Vector128<short> ZipLow(Vector128<short> left, Vector128<short> right) => ZipLow(left, right);

            /// <summary>
            /// int32x4_t vzip1q_s32(int32x4_t a, int32x4_t b)
            ///   A64: ZIP1 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<int> ZipLow(Vector128<int> left, Vector128<int> right) => ZipLow(left, right);

            /// <summary>
            /// int64x2_t vzip1q_s64(int64x2_t a, int64x2_t b)
            ///   A64: ZIP1 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<long> ZipLow(Vector128<long> left, Vector128<long> right) => ZipLow(left, right);

            /// <summary>
            /// int8x16_t vzip1q_u8(int8x16_t a, int8x16_t b)
            ///   A64: ZIP1 Vd.16B, Vn.16B, Vm.16B
            /// </summary>
            public static Vector128<sbyte> ZipLow(Vector128<sbyte> left, Vector128<sbyte> right) => ZipLow(left, right);

            /// <summary>
            /// float32x4_t vzip1q_f32(float32x4_t a, float32x4_t b)
            ///   A64: ZIP1 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<float> ZipLow(Vector128<float> left, Vector128<float> right) => ZipLow(left, right);

            /// <summary>
            /// uint16x8_t vzip1q_u16(uint16x8_t a, uint16x8_t b)
            ///   A64: ZIP1 Vd.8H, Vn.8H, Vm.8H
            /// </summary>
            public static Vector128<ushort> ZipLow(Vector128<ushort> left, Vector128<ushort> right) => ZipLow(left, right);

            /// <summary>
            /// uint32x4_t vzip1q_u32(uint32x4_t a, uint32x4_t b)
            ///   A64: ZIP1 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<uint> ZipLow(Vector128<uint> left, Vector128<uint> right) => ZipLow(left, right);

            /// <summary>
            /// uint64x2_t vzip1q_u64(uint64x2_t a, uint64x2_t b)
            ///   A64: ZIP1 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<ulong> ZipLow(Vector128<ulong> left, Vector128<ulong> right) => ZipLow(left, right);
        }

        /// <summary>
        /// int16x4_t vabs_s16 (int16x4_t a)
        ///   A32: VABS.S16 Dd, Dm
        ///   A64: ABS Vd.4H, Vn.4H
        /// </summary>
        public static Vector64<ushort> Abs(Vector64<short> value) => Abs(value);

        /// <summary>
        /// int32x2_t vabs_s32 (int32x2_t a)
        ///   A32: VABS.S32 Dd, Dm
        ///   A64: ABS Vd.2S, Vn.2S
        /// </summary>
        public static Vector64<uint> Abs(Vector64<int> value) => Abs(value);

        /// <summary>
        /// int8x8_t vabs_s8 (int8x8_t a)
        ///   A32: VABS.S8 Dd, Dm
        ///   A64: ABS Vd.8B, Vn.8B
        /// </summary>
        public static Vector64<byte> Abs(Vector64<sbyte> value) => Abs(value);

        /// <summary>
        /// float32x2_t vabs_f32 (float32x2_t a)
        ///   A32: VABS.F32 Dd, Dm
        ///   A64: FABS Vd.2S, Vn.2S
        /// </summary>
        public static Vector64<float> Abs(Vector64<float> value) => Abs(value);

        /// <summary>
        /// int16x8_t vabsq_s16 (int16x8_t a)
        ///   A32: VABS.S16 Qd, Qm
        ///   A64: ABS Vd.8H, Vn.8H
        /// </summary>
        public static Vector128<ushort> Abs(Vector128<short> value) => Abs(value);

        /// <summary>
        /// int32x4_t vabsq_s32 (int32x4_t a)
        ///   A32: VABS.S32 Qd, Qm
        ///   A64: ABS Vd.4S, Vn.4S
        /// </summary>
        public static Vector128<uint> Abs(Vector128<int> value) => Abs(value);

        /// <summary>
        /// int8x16_t vabsq_s8 (int8x16_t a)
        ///   A32: VABS.S8 Qd, Qm
        ///   A64: ABS Vd.16B, Vn.16B
        /// </summary>
        public static Vector128<byte> Abs(Vector128<sbyte> value) => Abs(value);

        /// <summary>
        /// float32x4_t vabsq_f32 (float32x4_t a)
        ///   A32: VABS.F32 Qd, Qm
        ///   A64: FABS Vd.4S, Vn.4S
        /// </summary>
        public static Vector128<float> Abs(Vector128<float> value) => Abs(value);

        /// <summary>
        /// float64x1_t vabs_f64 (float64x1_t a)
        ///   A32: VABS.F64 Dd, Dm
        ///   A64: FABS Dd, Dn
        /// </summary>
        public static Vector64<double> AbsScalar(Vector64<double> value) => AbsScalar(value);

        /// <summary>
        /// float32_t vabss_f32 (float32_t a)
        ///   A32: VABS.F32 Sd, Sm
        ///   A64: FABS Sd, Sn
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> AbsScalar(Vector64<float> value) => AbsScalar(value);

        /// <summary>
        /// uint32x2_t vcagt_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VACGT.F32 Dd, Dn, Dm
        ///   A64: FACGT Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<float> AbsoluteCompareGreaterThan(Vector64<float> left, Vector64<float> right) => AbsoluteCompareGreaterThan(left, right);

        /// <summary>
        /// uint32x4_t vcagtq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VACGT.F32 Qd, Qn, Qm
        ///   A64: FACGT Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<float> AbsoluteCompareGreaterThan(Vector128<float> left, Vector128<float> right) => AbsoluteCompareGreaterThan(left, right);

        /// <summary>
        /// uint32x2_t vcage_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VACGE.F32 Dd, Dn, Dm
        ///   A64: FACGE Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<float> AbsoluteCompareGreaterThanOrEqual(Vector64<float> left, Vector64<float> right) => AbsoluteCompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint32x4_t vcageq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VACGE.F32 Qd, Qn, Qm
        ///   A64: FACGE Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<float> AbsoluteCompareGreaterThanOrEqual(Vector128<float> left, Vector128<float> right) => AbsoluteCompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint32x2_t vcalt_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VACLT.F32 Dd, Dn, Dm
        ///   A64: FACGT Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<float> AbsoluteCompareLessThan(Vector64<float> left, Vector64<float> right) => AbsoluteCompareLessThan(left, right);

        /// <summary>
        /// uint32x4_t vcaltq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VACLT.F32 Qd, Qn, Qm
        ///   A64: FACGT Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<float> AbsoluteCompareLessThan(Vector128<float> left, Vector128<float> right) => AbsoluteCompareLessThan(left, right);

        /// <summary>
        /// uint32x2_t vcale_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VACLE.F32 Dd, Dn, Dm
        ///   A64: FACGE Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<float> AbsoluteCompareLessThanOrEqual(Vector64<float> left, Vector64<float> right) => AbsoluteCompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint32x4_t vcaleq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VACLE.F32 Qd, Qn, Qm
        ///   A64: FACGE Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<float> AbsoluteCompareLessThanOrEqual(Vector128<float> left, Vector128<float> right) => AbsoluteCompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint8x8_t vabd_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VABD.U8 Dd, Dn, Dm
        ///   A64: UABD Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> AbsoluteDifference(Vector64<byte> left, Vector64<byte> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// int16x4_t vabd_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VABD.S16 Dd, Dn, Dm
        ///   A64: SABD Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> AbsoluteDifference(Vector64<short> left, Vector64<short> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// int32x2_t vabd_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VABD.S32 Dd, Dn, Dm
        ///   A64: SABD Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> AbsoluteDifference(Vector64<int> left, Vector64<int> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// int8x8_t vabd_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VABD.S8 Dd, Dn, Dm
        ///   A64: SABD Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> AbsoluteDifference(Vector64<sbyte> left, Vector64<sbyte> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// float32x2_t vabd_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VABD.F32 Dd, Dn, Dm
        ///   A64: FABD Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<float> AbsoluteDifference(Vector64<float> left, Vector64<float> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// uint16x4_t vabd_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VABD.U16 Dd, Dn, Dm
        ///   A64: UABD Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> AbsoluteDifference(Vector64<ushort> left, Vector64<ushort> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// uint32x2_t vabd_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VABD.U32 Dd, Dn, Dm
        ///   A64: UABD Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> AbsoluteDifference(Vector64<uint> left, Vector64<uint> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// uint8x16_t vabdq_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VABD.U8 Qd, Qn, Qm
        ///   A64: UABD Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> AbsoluteDifference(Vector128<byte> left, Vector128<byte> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// int16x8_t vabdq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VABD.S16 Qd, Qn, Qm
        ///   A64: SABD Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> AbsoluteDifference(Vector128<short> left, Vector128<short> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// int32x4_t vabdq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VABD.S32 Qd, Qn, Qm
        ///   A64: SABD Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> AbsoluteDifference(Vector128<int> left, Vector128<int> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// int8x16_t vabdq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VABD.S8 Qd, Qn, Qm
        ///   A64: SABD Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> AbsoluteDifference(Vector128<sbyte> left, Vector128<sbyte> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// float32x4_t vabdq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VABD.F32 Qd, Qn, Qm
        ///   A64: FABD Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<float> AbsoluteDifference(Vector128<float> left, Vector128<float> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// uint16x8_t vabdq_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VABD.U16 Qd, Qn, Qm
        ///   A64: UABD Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> AbsoluteDifference(Vector128<ushort> left, Vector128<ushort> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// uint32x4_t vabdq_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VABD.U32 Qd, Qn, Qm
        ///   A64: UABD Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> AbsoluteDifference(Vector128<uint> left, Vector128<uint> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// uint8x8_t vadd_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VADD.I8 Dd, Dn, Dm
        ///   A64: ADD Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> Add(Vector64<byte> left, Vector64<byte> right) => Add(left, right);

        /// <summary>
        /// int16x4_t vadd_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VADD.I16 Dd, Dn, Dm
        ///   A64: ADD Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> Add(Vector64<short> left, Vector64<short> right) => Add(left, right);

        /// <summary>
        /// int32x2_t vadd_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VADD.I32 Dd, Dn, Dm
        ///   A64: ADD Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> Add(Vector64<int> left, Vector64<int> right) => Add(left, right);

        /// <summary>
        /// int8x8_t vadd_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VADD.I8 Dd, Dn, Dm
        ///   A64: ADD Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> Add(Vector64<sbyte> left, Vector64<sbyte> right) => Add(left, right);

        /// <summary>
        /// float32x2_t vadd_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VADD.F32 Dd, Dn, Dm
        ///   A64: FADD Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<float> Add(Vector64<float> left, Vector64<float> right) => Add(left, right);

        /// <summary>
        /// uint16x4_t vadd_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VADD.I16 Dd, Dn, Dm
        ///   A64: ADD Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> Add(Vector64<ushort> left, Vector64<ushort> right) => Add(left, right);

        /// <summary>
        /// uint32x2_t vadd_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VADD.I32 Dd, Dn, Dm
        ///   A64: ADD Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> Add(Vector64<uint> left, Vector64<uint> right) => Add(left, right);

        /// <summary>
        /// uint8x16_t vaddq_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VADD.I8 Qd, Qn, Qm
        ///   A64: ADD Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> Add(Vector128<byte> left, Vector128<byte> right) => Add(left, right);

        /// <summary>
        /// int16x8_t vaddq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VADD.I16 Qd, Qn, Qm
        ///   A64: ADD Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> Add(Vector128<short> left, Vector128<short> right) => Add(left, right);

        /// <summary>
        /// int32x4_t vaddq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VADD.I32 Qd, Qn, Qm
        ///   A64: ADD Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> Add(Vector128<int> left, Vector128<int> right) => Add(left, right);

        /// <summary>
        /// int64x2_t vaddq_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VADD.I64 Qd, Qn, Qm
        ///   A64: ADD Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<long> Add(Vector128<long> left, Vector128<long> right) => Add(left, right);

        /// <summary>
        /// int8x16_t vaddq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VADD.I8 Qd, Qn, Qm
        ///   A64: ADD Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> Add(Vector128<sbyte> left, Vector128<sbyte> right) => Add(left, right);

        /// <summary>
        /// float32x4_t vaddq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VADD.F32 Qd, Qn, Qm
        ///   A64: FADD Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<float> Add(Vector128<float> left, Vector128<float> right) => Add(left, right);

        /// <summary>
        /// uint16x8_t vaddq_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VADD.I16 Qd, Qn, Qm
        ///   A64: ADD Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> Add(Vector128<ushort> left, Vector128<ushort> right) => Add(left, right);

        /// <summary>
        /// uint32x4_t vaddq_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VADD.I32 Qd, Qn, Qm
        ///   A64: ADD Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> Add(Vector128<uint> left, Vector128<uint> right) => Add(left, right);

        /// <summary>
        /// uint64x2_t vaddq_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VADD.I64 Qd, Qn, Qm
        ///   A64: ADD Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<ulong> Add(Vector128<ulong> left, Vector128<ulong> right) => Add(left, right);

        /// <summary>
        /// float64x1_t vadd_f64 (float64x1_t a, float64x1_t b)
        ///   A32: VADD.F64 Dd, Dn, Dm
        ///   A64: FADD Dd, Dn, Dm
        /// </summary>
        public static Vector64<double> AddScalar(Vector64<double> left, Vector64<double> right) => AddScalar(left, right);

        /// <summary>
        /// int64x1_t vadd_s64 (int64x1_t a, int64x1_t b)
        ///   A32: VADD.I64 Dd, Dn, Dm
        ///   A64: ADD Dd, Dn, Dm
        /// </summary>
        public static Vector64<long> AddScalar(Vector64<long> left, Vector64<long> right) => AddScalar(left, right);

        /// <summary>
        /// float32_t vadds_f32 (float32_t a, float32_t b)
        ///   A32: VADD.F32 Sd, Sn, Sm
        ///   A64: FADD Sd, Sn, Sm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> AddScalar(Vector64<float> left, Vector64<float> right) => AddScalar(left, right);

        /// <summary>
        /// uint64x1_t vadd_u64 (uint64x1_t a, uint64x1_t b)
        ///   A32: VADD.I64 Dd, Dn, Dm
        ///   A64: ADD Dd, Dn, Dm
        /// </summary>
        public static Vector64<ulong> AddScalar(Vector64<ulong> left, Vector64<ulong> right) => AddScalar(left, right);

        /// <summary>
        /// uint8x8_t vand_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector64<byte> And(Vector64<byte> left, Vector64<byte> right) => And(left, right);

        // /// <summary>
        // /// float64x1_t vand_f64 (float64x1_t a, float64x1_t b)
        // ///   A32: VAND Dd, Dn, Dm
        // ///   A64: AND Vd, Vn, Vm
        // /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        // /// </summary>
        // public static Vector64<double> And(Vector64<double> left, Vector64<double> right) => And(left, right);

        /// <summary>
        /// int16x4_t vand_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector64<short> And(Vector64<short> left, Vector64<short> right) => And(left, right);

        /// <summary>
        /// int32x2_t vand_s32(int32x2_t a, int32x2_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector64<int> And(Vector64<int> left, Vector64<int> right) => And(left, right);

        // /// <summary>
        // /// int64x1_t vand_s64 (int64x1_t a, int64x1_t b)
        // ///   A32: VAND Dd, Dn, Dm
        // ///   A64: AND Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<long> And(Vector64<long> left, Vector64<long> right) => And(left, right);

        /// <summary>
        /// int8x8_t vand_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector64<sbyte> And(Vector64<sbyte> left, Vector64<sbyte> right) => And(left, right);

        /// <summary>
        /// float32x2_t vand_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> And(Vector64<float> left, Vector64<float> right) => And(left, right);

        /// <summary>
        /// uint16x4_t vand_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector64<ushort> And(Vector64<ushort> left, Vector64<ushort> right) => And(left, right);

        /// <summary>
        /// uint32x2_t vand_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector64<uint> And(Vector64<uint> left, Vector64<uint> right) => And(left, right);

        // /// <summary>
        // /// uint64x1_t vand_u64 (uint64x1_t a, uint64x1_t b)
        // ///   A32: VAND Dd, Dn, Dm
        // ///   A64: AND Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<ulong> And(Vector64<ulong> left, Vector64<ulong> right) => And(left, right);

        /// <summary>
        /// uint8x16_t vand_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector128<byte> And(Vector128<byte> left, Vector128<byte> right) => And(left, right);

        /// <summary>
        /// float64x2_t vand_f64 (float64x2_t a, float64x2_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> And(Vector128<double> left, Vector128<double> right) => And(left, right);

        /// <summary>
        /// int16x8_t vand_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector128<short> And(Vector128<short> left, Vector128<short> right) => And(left, right);

        /// <summary>
        /// int32x4_t vand_s32(int32x4_t a, int32x4_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector128<int> And(Vector128<int> left, Vector128<int> right) => And(left, right);

        /// <summary>
        /// int64x2_t vand_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector128<long> And(Vector128<long> left, Vector128<long> right) => And(left, right);

        /// <summary>
        /// int8x16_t vand_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector128<sbyte> And(Vector128<sbyte> left, Vector128<sbyte> right) => And(left, right);

        /// <summary>
        /// float32x4_t vand_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> And(Vector128<float> left, Vector128<float> right) => And(left, right);

        /// <summary>
        /// uint16x8_t vand_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector128<ushort> And(Vector128<ushort> left, Vector128<ushort> right) => And(left, right);

        /// <summary>
        /// uint32x4_t vand_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector128<uint> And(Vector128<uint> left, Vector128<uint> right) => And(left, right);

        /// <summary>
        /// uint64x2_t vand_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector128<ulong> And(Vector128<ulong> left, Vector128<ulong> right) => And(left, right);

        /// <summary>
        /// uint8x8_t vbic_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector64<byte> AndNot(Vector64<byte> left, Vector64<byte> right) => AndNot(left, right);

        // /// <summary>
        // /// float64x1_t vbic_f64 (float64x1_t a, float64x1_t b)
        // ///   A32: VBIC Dd, Dn, Dm
        // ///   A64: BIC Vd, Vn, Vm
        // /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        // /// </summary>
        // public static Vector64<double> AndNot(Vector64<double> left, Vector64<double> right) => AndNot(left, right);

        /// <summary>
        /// int16x4_t vbic_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector64<short> AndNot(Vector64<short> left, Vector64<short> right) => AndNot(left, right);

        /// <summary>
        /// int32x2_t vbic_s32(int32x2_t a, int32x2_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector64<int> AndNot(Vector64<int> left, Vector64<int> right) => AndNot(left, right);

        // /// <summary>
        // /// int64x1_t vbic_s64 (int64x1_t a, int64x1_t b)
        // ///   A32: VBIC Dd, Dn, Dm
        // ///   A64: BIC Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<long> AndNot(Vector64<long> left, Vector64<long> right) => AndNot(left, right);

        /// <summary>
        /// int8x8_t vbic_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector64<sbyte> AndNot(Vector64<sbyte> left, Vector64<sbyte> right) => AndNot(left, right);

        /// <summary>
        /// float32x2_t vbic_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> AndNot(Vector64<float> left, Vector64<float> right) => AndNot(left, right);

        /// <summary>
        /// uint16x4_t vbic_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector64<ushort> AndNot(Vector64<ushort> left, Vector64<ushort> right) => AndNot(left, right);

        /// <summary>
        /// uint32x2_t vbic_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector64<uint> AndNot(Vector64<uint> left, Vector64<uint> right) => AndNot(left, right);

        // /// <summary>
        // /// uint64x1_t vbic_u64 (uint64x1_t a, uint64x1_t b)
        // ///   A32: VBIC Dd, Dn, Dm
        // ///   A64: BIC Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<ulong> AndNot(Vector64<ulong> left, Vector64<ulong> right) => AndNot(left, right);

        /// <summary>
        /// uint8x16_t vbic_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector128<byte> AndNot(Vector128<byte> left, Vector128<byte> right) => AndNot(left, right);

        /// <summary>
        /// float64x2_t vbic_f64 (float64x2_t a, float64x2_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> AndNot(Vector128<double> left, Vector128<double> right) => AndNot(left, right);

        /// <summary>
        /// int16x8_t vbic_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector128<short> AndNot(Vector128<short> left, Vector128<short> right) => AndNot(left, right);

        /// <summary>
        /// int32x4_t vbic_s32(int32x4_t a, int32x4_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector128<int> AndNot(Vector128<int> left, Vector128<int> right) => AndNot(left, right);

        /// <summary>
        /// int64x2_t vbic_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector128<long> AndNot(Vector128<long> left, Vector128<long> right) => AndNot(left, right);

        /// <summary>
        /// int8x16_t vbic_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector128<sbyte> AndNot(Vector128<sbyte> left, Vector128<sbyte> right) => AndNot(left, right);

        /// <summary>
        /// float32x4_t vbic_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> AndNot(Vector128<float> left, Vector128<float> right) => AndNot(left, right);

        /// <summary>
        /// uint16x8_t vbic_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector128<ushort> AndNot(Vector128<ushort> left, Vector128<ushort> right) => AndNot(left, right);

        /// <summary>
        /// uint32x4_t vbic_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector128<uint> AndNot(Vector128<uint> left, Vector128<uint> right) => AndNot(left, right);

        /// <summary>
        /// uint64x2_t vbic_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector128<ulong> AndNot(Vector128<ulong> left, Vector128<ulong> right) => AndNot(left, right);

        /// <summary>
        /// uint8x8_t vbsl_u8 (uint8x8_t a, uint8x8_t b, uint8x8_t c)
        ///   A32: VBSL Dd, Dn, Dm
        ///   A64: BSL Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> BitwiseSelect(Vector64<byte> select, Vector64<byte> left, Vector64<byte> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// float64x1_t vbsl_f64 (uint64x1_t a, float64x1_t b, float64x1_t c)
        ///   A32: VBSL Dd, Dn, Dm
        ///   A64: BSL Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<double> BitwiseSelect(Vector64<double> select, Vector64<double> left, Vector64<double> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// int16x4_t vbsl_s16 (uint16x4_t a, int16x4_t b, int16x4_t c)
        ///   A32: VBSL Dd, Dn, Dm
        ///   A64: BSL Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<short> BitwiseSelect(Vector64<short> select, Vector64<short> left, Vector64<short> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// int32x2_t vbsl_s32 (uint32x2_t a, int32x2_t b, int32x2_t c)
        ///   A32: VBSL Dd, Dn, Dm
        ///   A64: BSL Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<int> BitwiseSelect(Vector64<int> select, Vector64<int> left, Vector64<int> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// int64x1_t vbsl_s64 (uint64x1_t a, int64x1_t b, int64x1_t c)
        ///   A32: VBSL Dd, Dn, Dm
        ///   A64: BSL Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<long> BitwiseSelect(Vector64<long> select, Vector64<long> left, Vector64<long> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// int8x8_t vbsl_s8 (uint8x8_t a, int8x8_t b, int8x8_t c)
        ///   A32: VBSL Dd, Dn, Dm
        ///   A64: BSL Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> BitwiseSelect(Vector64<sbyte> select, Vector64<sbyte> left, Vector64<sbyte> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// float32x2_t vbsl_f32 (uint32x2_t a, float32x2_t b, float32x2_t c)
        ///   A32: VBSL Dd, Dn, Dm
        ///   A64: BSL Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<float> BitwiseSelect(Vector64<float> select, Vector64<float> left, Vector64<float> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// uint16x4_t vbsl_u16 (uint16x4_t a, uint16x4_t b, uint16x4_t c)
        ///   A32: VBSL Dd, Dn, Dm
        ///   A64: BSL Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<ushort> BitwiseSelect(Vector64<ushort> select, Vector64<ushort> left, Vector64<ushort> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// uint32x2_t vbsl_u32 (uint32x2_t a, uint32x2_t b, uint32x2_t c)
        ///   A32: VBSL Dd, Dn, Dm
        ///   A64: BSL Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<uint> BitwiseSelect(Vector64<uint> select, Vector64<uint> left, Vector64<uint> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// uint64x1_t vbsl_u64 (uint64x1_t a, uint64x1_t b, uint64x1_t c)
        ///   A32: VBSL Dd, Dn, Dm
        ///   A64: BSL Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<ulong> BitwiseSelect(Vector64<ulong> select, Vector64<ulong> left, Vector64<ulong> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// uint8x16_t vbslq_u8 (uint8x16_t a, uint8x16_t b, uint8x16_t c)
        ///   A32: VBSL Qd, Qn, Qm
        ///   A64: BSL Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> BitwiseSelect(Vector128<byte> select, Vector128<byte> left, Vector128<byte> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// float64x2_t vbslq_f64 (uint64x2_t a, float64x2_t b, float64x2_t c)
        ///   A32: VBSL Qd, Qn, Qm
        ///   A64: BSL Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<double> BitwiseSelect(Vector128<double> select, Vector128<double> left, Vector128<double> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// int16x8_t vbslq_s16 (uint16x8_t a, int16x8_t b, int16x8_t c)
        ///   A32: VBSL Qd, Qn, Qm
        ///   A64: BSL Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<short> BitwiseSelect(Vector128<short> select, Vector128<short> left, Vector128<short> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// int32x4_t vbslq_s32 (uint32x4_t a, int32x4_t b, int32x4_t c)
        ///   A32: VBSL Qd, Qn, Qm
        ///   A64: BSL Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<int> BitwiseSelect(Vector128<int> select, Vector128<int> left, Vector128<int> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// int64x2_t vbslq_s64 (uint64x2_t a, int64x2_t b, int64x2_t c)
        ///   A32: VBSL Qd, Qn, Qm
        ///   A64: BSL Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<long> BitwiseSelect(Vector128<long> select, Vector128<long> left, Vector128<long> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// int8x16_t vbslq_s8 (uint8x16_t a, int8x16_t b, int8x16_t c)
        ///   A32: VBSL Qd, Qn, Qm
        ///   A64: BSL Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> BitwiseSelect(Vector128<sbyte> select, Vector128<sbyte> left, Vector128<sbyte> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// float32x4_t vbslq_f32 (uint32x4_t a, float32x4_t b, float32x4_t c)
        ///   A32: VBSL Qd, Qn, Qm
        ///   A64: BSL Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<float> BitwiseSelect(Vector128<float> select, Vector128<float> left, Vector128<float> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// uint16x8_t vbslq_u16 (uint16x8_t a, uint16x8_t b, uint16x8_t c)
        ///   A32: VBSL Qd, Qn, Qm
        ///   A64: BSL Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<ushort> BitwiseSelect(Vector128<ushort> select, Vector128<ushort> left, Vector128<ushort> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// uint32x4_t vbslq_u32 (uint32x4_t a, uint32x4_t b, uint32x4_t c)
        ///   A32: VBSL Qd, Qn, Qm
        ///   A64: BSL Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<uint> BitwiseSelect(Vector128<uint> select, Vector128<uint> left, Vector128<uint> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// uint64x2_t vbslq_u64 (uint64x2_t a, uint64x2_t b, uint64x2_t c)
        ///   A32: VBSL Qd, Qn, Qm
        ///   A64: BSL Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<ulong> BitwiseSelect(Vector128<ulong> select, Vector128<ulong> left, Vector128<ulong> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// uint8x8_t vceq_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VCEQ.I8 Dd, Dn, Dm
        ///   A64: CMEQ Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> CompareEqual(Vector64<byte> left, Vector64<byte> right) => CompareEqual(left, right);

        /// <summary>
        /// uint16x4_t vceq_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VCEQ.I16 Dd, Dn, Dm
        ///   A64: CMEQ Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> CompareEqual(Vector64<short> left, Vector64<short> right) => CompareEqual(left, right);

        /// <summary>
        /// uint32x2_t vceq_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VCEQ.I32 Dd, Dn, Dm
        ///   A64: CMEQ Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> CompareEqual(Vector64<int> left, Vector64<int> right) => CompareEqual(left, right);

        /// <summary>
        /// uint8x8_t vceq_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VCEQ.I8 Dd, Dn, Dm
        ///   A64: CMEQ Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> CompareEqual(Vector64<sbyte> left, Vector64<sbyte> right) => CompareEqual(left, right);

        /// <summary>
        /// uint32x2_t vceq_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VCEQ.F32 Dd, Dn, Dm
        ///   A64: FCMEQ Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<float> CompareEqual(Vector64<float> left, Vector64<float> right) => CompareEqual(left, right);

        /// <summary>
        /// uint16x4_t vceq_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VCEQ.I16 Dd, Dn, Dm
        ///   A64: CMEQ Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> CompareEqual(Vector64<ushort> left, Vector64<ushort> right) => CompareEqual(left, right);

        /// <summary>
        /// uint32x2_t vceq_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VCEQ.I32 Dd, Dn, Dm
        ///   A64: CMEQ Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> CompareEqual(Vector64<uint> left, Vector64<uint> right) => CompareEqual(left, right);

        /// <summary>
        /// uint8x16_t vceqq_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VCEQ.I8 Qd, Qn, Qm
        ///   A64: CMEQ Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> CompareEqual(Vector128<byte> left, Vector128<byte> right) => CompareEqual(left, right);

        /// <summary>
        /// uint16x8_t vceqq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VCEQ.I16 Qd, Qn, Qm
        ///   A64: CMEQ Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> CompareEqual(Vector128<short> left, Vector128<short> right) => CompareEqual(left, right);

        /// <summary>
        /// uint32x4_t vceqq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VCEQ.I32 Qd, Qn, Qm
        ///   A64: CMEQ Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> CompareEqual(Vector128<int> left, Vector128<int> right) => CompareEqual(left, right);

        /// <summary>
        /// uint8x16_t vceqq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VCEQ.I8 Qd, Qn, Qm
        ///   A64: CMEQ Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> CompareEqual(Vector128<sbyte> left, Vector128<sbyte> right) => CompareEqual(left, right);

        /// <summary>
        /// uint32x4_t vceqq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VCEQ.F32 Qd, Qn, Qm
        ///   A64: FCMEQ Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<float> CompareEqual(Vector128<float> left, Vector128<float> right) => CompareEqual(left, right);

        /// <summary>
        /// uint16x8_t vceqq_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VCEQ.I16 Qd, Qn, Qm
        ///   A64: CMEQ Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> CompareEqual(Vector128<ushort> left, Vector128<ushort> right) => CompareEqual(left, right);

        /// <summary>
        /// uint32x4_t vceqq_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VCEQ.I32 Qd, Qn, Qm
        ///   A64: CMEQ Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> CompareEqual(Vector128<uint> left, Vector128<uint> right) => CompareEqual(left, right);

        /// <summary>
        /// uint8x8_t vcgt_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VCGT.U8 Dd, Dn, Dm
        ///   A64: CMHI Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> CompareGreaterThan(Vector64<byte> left, Vector64<byte> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint16x4_t vcgt_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VCGT.S16 Dd, Dn, Dm
        ///   A64: CMGT Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> CompareGreaterThan(Vector64<short> left, Vector64<short> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint32x2_t vcgt_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VCGT.S32 Dd, Dn, Dm
        ///   A64: CMGT Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> CompareGreaterThan(Vector64<int> left, Vector64<int> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint8x8_t vcgt_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VCGT.S8 Dd, Dn, Dm
        ///   A64: CMGT Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> CompareGreaterThan(Vector64<sbyte> left, Vector64<sbyte> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint32x2_t vcgt_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VCGT.F32 Dd, Dn, Dm
        ///   A64: FCMGT Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<float> CompareGreaterThan(Vector64<float> left, Vector64<float> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint16x4_t vcgt_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VCGT.U16 Dd, Dn, Dm
        ///   A64: CMHI Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> CompareGreaterThan(Vector64<ushort> left, Vector64<ushort> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint32x2_t vcgt_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VCGT.U32 Dd, Dn, Dm
        ///   A64: CMHI Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> CompareGreaterThan(Vector64<uint> left, Vector64<uint> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint8x16_t vcgtq_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VCGT.U8 Qd, Qn, Qm
        ///   A64: CMHI Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> CompareGreaterThan(Vector128<byte> left, Vector128<byte> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint16x8_t vcgtq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VCGT.S16 Qd, Qn, Qm
        ///   A64: CMGT Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> CompareGreaterThan(Vector128<short> left, Vector128<short> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint32x4_t vcgtq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VCGT.S32 Qd, Qn, Qm
        ///   A64: CMGT Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> CompareGreaterThan(Vector128<int> left, Vector128<int> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint8x16_t vcgtq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VCGT.S8 Qd, Qn, Qm
        ///   A64: CMGT Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> CompareGreaterThan(Vector128<sbyte> left, Vector128<sbyte> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint32x4_t vcgtq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VCGT.F32 Qd, Qn, Qm
        ///   A64: FCMGT Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<float> CompareGreaterThan(Vector128<float> left, Vector128<float> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint16x8_t vcgtq_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VCGT.U16 Qd, Qn, Qm
        ///   A64: CMHI Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> CompareGreaterThan(Vector128<ushort> left, Vector128<ushort> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint32x4_t vcgtq_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VCGT.U32 Qd, Qn, Qm
        ///   A64: CMHI Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> CompareGreaterThan(Vector128<uint> left, Vector128<uint> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint8x8_t vcge_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VCGE.U8 Dd, Dn, Dm
        ///   A64: CMHS Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> CompareGreaterThanOrEqual(Vector64<byte> left, Vector64<byte> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint16x4_t vcge_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VCGE.S16 Dd, Dn, Dm
        ///   A64: CMGE Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> CompareGreaterThanOrEqual(Vector64<short> left, Vector64<short> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint32x2_t vcge_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VCGE.S32 Dd, Dn, Dm
        ///   A64: CMGE Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> CompareGreaterThanOrEqual(Vector64<int> left, Vector64<int> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint8x8_t vcge_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VCGE.S8 Dd, Dn, Dm
        ///   A64: CMGE Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> CompareGreaterThanOrEqual(Vector64<sbyte> left, Vector64<sbyte> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint32x2_t vcge_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VCGE.F32 Dd, Dn, Dm
        ///   A64: FCMGE Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<float> CompareGreaterThanOrEqual(Vector64<float> left, Vector64<float> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint16x4_t vcge_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VCGE.U16 Dd, Dn, Dm
        ///   A64: CMHS Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> CompareGreaterThanOrEqual(Vector64<ushort> left, Vector64<ushort> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint32x2_t vcge_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VCGE.U32 Dd, Dn, Dm
        ///   A64: CMHS Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> CompareGreaterThanOrEqual(Vector64<uint> left, Vector64<uint> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint8x16_t vcgeq_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VCGE.U8 Qd, Qn, Qm
        ///   A64: CMHS Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> CompareGreaterThanOrEqual(Vector128<byte> left, Vector128<byte> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint16x8_t vcgeq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VCGE.S16 Qd, Qn, Qm
        ///   A64: CMGE Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> CompareGreaterThanOrEqual(Vector128<short> left, Vector128<short> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint32x4_t vcgeq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VCGE.S32 Qd, Qn, Qm
        ///   A64: CMGE Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> CompareGreaterThanOrEqual(Vector128<int> left, Vector128<int> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint8x16_t vcgeq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VCGE.S8 Qd, Qn, Qm
        ///   A64: CMGE Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> CompareGreaterThanOrEqual(Vector128<sbyte> left, Vector128<sbyte> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint32x4_t vcgeq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VCGE.F32 Qd, Qn, Qm
        ///   A64: FCMGE Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<float> CompareGreaterThanOrEqual(Vector128<float> left, Vector128<float> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint16x8_t vcgeq_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VCGE.U16 Qd, Qn, Qm
        ///   A64: CMHS Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> CompareGreaterThanOrEqual(Vector128<ushort> left, Vector128<ushort> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint32x4_t vcgeq_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VCGE.U32 Qd, Qn, Qm
        ///   A64: CMHS Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> CompareGreaterThanOrEqual(Vector128<uint> left, Vector128<uint> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint8x8_t vclt_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VCLT.U8 Dd, Dn, Dm
        ///   A64: CMHI Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> CompareLessThan(Vector64<byte> left, Vector64<byte> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint16x4_t vclt_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VCLT.S16 Dd, Dn, Dm
        ///   A64: CMGT Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> CompareLessThan(Vector64<short> left, Vector64<short> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint32x2_t vclt_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VCLT.S32 Dd, Dn, Dm
        ///   A64: CMGT Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> CompareLessThan(Vector64<int> left, Vector64<int> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint8x8_t vclt_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VCLT.S8 Dd, Dn, Dm
        ///   A64: CMGT Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> CompareLessThan(Vector64<sbyte> left, Vector64<sbyte> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint32x2_t vclt_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VCLT.F32 Dd, Dn, Dm
        ///   A64: FCMGT Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<float> CompareLessThan(Vector64<float> left, Vector64<float> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint16x4_t vclt_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VCLT.U16 Dd, Dn, Dm
        ///   A64: CMHI Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> CompareLessThan(Vector64<ushort> left, Vector64<ushort> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint32x2_t vclt_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VCLT.U32 Dd, Dn, Dm
        ///   A64: CMHI Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> CompareLessThan(Vector64<uint> left, Vector64<uint> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint8x16_t vcltq_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VCLT.U8 Qd, Qn, Qm
        ///   A64: CMHI Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> CompareLessThan(Vector128<byte> left, Vector128<byte> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint16x8_t vcltq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VCLT.S16 Qd, Qn, Qm
        ///   A64: CMGT Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> CompareLessThan(Vector128<short> left, Vector128<short> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint32x4_t vcltq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VCLT.S32 Qd, Qn, Qm
        ///   A64: CMGT Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> CompareLessThan(Vector128<int> left, Vector128<int> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint8x16_t vcltq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VCLT.S8 Qd, Qn, Qm
        ///   A64: CMGT Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> CompareLessThan(Vector128<sbyte> left, Vector128<sbyte> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint32x4_t vcltq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VCLT.F32 Qd, Qn, Qm
        ///   A64: FCMGT Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<float> CompareLessThan(Vector128<float> left, Vector128<float> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint16x8_t vcltq_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VCLT.U16 Qd, Qn, Qm
        ///   A64: CMHI Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> CompareLessThan(Vector128<ushort> left, Vector128<ushort> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint32x4_t vcltq_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VCLT.U32 Qd, Qn, Qm
        ///   A64: CMHI Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> CompareLessThan(Vector128<uint> left, Vector128<uint> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint8x8_t vcle_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VCLE.U8 Dd, Dn, Dm
        ///   A64: CMHS Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> CompareLessThanOrEqual(Vector64<byte> left, Vector64<byte> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint16x4_t vcle_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VCLE.S16 Dd, Dn, Dm
        ///   A64: CMGE Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> CompareLessThanOrEqual(Vector64<short> left, Vector64<short> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint32x2_t vcle_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VCLE.S32 Dd, Dn, Dm
        ///   A64: CMGE Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> CompareLessThanOrEqual(Vector64<int> left, Vector64<int> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint8x8_t vcle_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VCLE.S8 Dd, Dn, Dm
        ///   A64: CMGE Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> CompareLessThanOrEqual(Vector64<sbyte> left, Vector64<sbyte> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint32x2_t vcle_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VCLE.F32 Dd, Dn, Dm
        ///   A64: FCMGE Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<float> CompareLessThanOrEqual(Vector64<float> left, Vector64<float> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint16x4_t vcle_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VCLE.U16 Dd, Dn, Dm
        ///   A64: CMHS Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> CompareLessThanOrEqual(Vector64<ushort> left, Vector64<ushort> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint32x2_t vcle_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VCLE.U32 Dd, Dn, Dm
        ///   A64: CMHS Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> CompareLessThanOrEqual(Vector64<uint> left, Vector64<uint> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint8x16_t vcleq_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VCLE.U8 Qd, Qn, Qm
        ///   A64: CMHS Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> CompareLessThanOrEqual(Vector128<byte> left, Vector128<byte> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint16x8_t vcleq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VCLE.S16 Qd, Qn, Qm
        ///   A64: CMGE Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> CompareLessThanOrEqual(Vector128<short> left, Vector128<short> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint32x4_t vcleq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VCLE.S32 Qd, Qn, Qm
        ///   A64: CMGE Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> CompareLessThanOrEqual(Vector128<int> left, Vector128<int> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint8x16_t vcleq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VCLE.S8 Qd, Qn, Qm
        ///   A64: CMGE Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> CompareLessThanOrEqual(Vector128<sbyte> left, Vector128<sbyte> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint32x4_t vcleq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VCLE.F32 Qd, Qn, Qm
        ///   A64: FCMGE Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<float> CompareLessThanOrEqual(Vector128<float> left, Vector128<float> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint16x8_t vcleq_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VCLE.U16 Qd, Qn, Qm
        ///   A64: CMHS Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> CompareLessThanOrEqual(Vector128<ushort> left, Vector128<ushort> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint32x4_t vcleq_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VCLE.U32 Qd, Qn, Qm
        ///   A64: CMHS Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> CompareLessThanOrEqual(Vector128<uint> left, Vector128<uint> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint8x8_t vtst_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VTST.8 Dd, Dn, Dm
        ///   A64: CMTST Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> CompareTest(Vector64<byte> left, Vector64<byte> right) => CompareTest(left, right);

        /// <summary>
        /// uint16x4_t vtst_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VTST.16 Dd, Dn, Dm
        ///   A64: CMTST Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> CompareTest(Vector64<short> left, Vector64<short> right) => CompareTest(left, right);

        /// <summary>
        /// uint32x2_t vtst_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VTST.32 Dd, Dn, Dm
        ///   A64: CMTST Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> CompareTest(Vector64<int> left, Vector64<int> right) => CompareTest(left, right);

        /// <summary>
        /// uint8x8_t vtst_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VTST.8 Dd, Dn, Dm
        ///   A64: CMTST Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> CompareTest(Vector64<sbyte> left, Vector64<sbyte> right) => CompareTest(left, right);

        /// <summary>
        /// uint32x2_t vtst_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VTST.32 Dd, Dn, Dm
        ///   A64: CMTST Vd.2S, Vn.2S, Vm.2S
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> CompareTest(Vector64<float> left, Vector64<float> right) => CompareTest(left, right);

        /// <summary>
        /// uint16x4_t vtst_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VTST.16 Dd, Dn, Dm
        ///   A64: CMTST Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> CompareTest(Vector64<ushort> left, Vector64<ushort> right) => CompareTest(left, right);

        /// <summary>
        /// uint32x2_t vtst_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VTST.32 Dd, Dn, Dm
        ///   A64: CMTST Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> CompareTest(Vector64<uint> left, Vector64<uint> right) => CompareTest(left, right);

        /// <summary>
        /// uint8x16_t vtstq_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VTST.8 Qd, Qn, Qm
        ///   A64: CMTST Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> CompareTest(Vector128<byte> left, Vector128<byte> right) => CompareTest(left, right);

        /// <summary>
        /// uint16x8_t vtstq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VTST.16 Qd, Qn, Qm
        ///   A64: CMTST Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> CompareTest(Vector128<short> left, Vector128<short> right) => CompareTest(left, right);

        /// <summary>
        /// uint32x4_t vtstq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VTST.32 Qd, Qn, Qm
        ///   A64: CMTST Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> CompareTest(Vector128<int> left, Vector128<int> right) => CompareTest(left, right);

        /// <summary>
        /// uint8x16_t vtstq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VTST.8 Qd, Qn, Qm
        ///   A64: CMTST Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> CompareTest(Vector128<sbyte> left, Vector128<sbyte> right) => CompareTest(left, right);

        /// <summary>
        /// uint32x4_t vtstq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VTST.32 Qd, Qn, Qm
        ///   A64: CMTST Vd.4S, Vn.4S, Vm.4S
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> CompareTest(Vector128<float> left, Vector128<float> right) => CompareTest(left, right);

        /// <summary>
        /// uint16x8_t vtstq_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VTST.16 Qd, Qn, Qm
        ///   A64: CMTST Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> CompareTest(Vector128<ushort> left, Vector128<ushort> right) => CompareTest(left, right);

        /// <summary>
        /// uint32x4_t vtstq_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VTST.32 Qd, Qn, Qm
        ///   A64: CMTST Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> CompareTest(Vector128<uint> left, Vector128<uint> right) => CompareTest(left, right);

        /// <summary>
        /// int16x4_t vcls_s16 (int16x4_t a)
        ///   A32: VCLS.S16 Dd, Dm
        ///   A64: CLS Vd.4H, Vn.4H
        /// </summary>
        public static Vector64<short> LeadingSignCount(Vector64<short> value) => LeadingSignCount(value);

        /// <summary>
        /// int32x2_t vcls_s32 (int32x2_t a)
        ///   A32: VCLS.S32 Dd, Dm
        ///   A64: CLS Vd.2S, Vn.2S
        /// </summary>
        public static Vector64<int> LeadingSignCount(Vector64<int> value) => LeadingSignCount(value);

        /// <summary>
        /// int8x8_t vcls_s8 (int8x8_t a)
        ///   A32: VCLS.S8 Dd, Dm
        ///   A64: CLS Vd.8B, Vn.8B
        /// </summary>
        public static Vector64<sbyte> LeadingSignCount(Vector64<sbyte> value) => LeadingSignCount(value);

        /// <summary>
        /// int16x8_t vclsq_s16 (int16x8_t a)
        ///   A32: VCLS.S16 Qd, Qm
        ///   A64: CLS Vd.8H, Vn.8H
        /// </summary>
        public static Vector128<short> LeadingSignCount(Vector128<short> value) => LeadingSignCount(value);

        /// <summary>
        /// int32x4_t vclsq_s32 (int32x4_t a)
        ///   A32: VCLS.S32 Qd, Qm
        ///   A64: CLS Vd.4S, Vn.4S
        /// </summary>
        public static Vector128<int> LeadingSignCount(Vector128<int> value) => LeadingSignCount(value);

        /// <summary>
        /// int8x16_t vclsq_s8 (int8x16_t a)
        ///   A32: VCLS.S8 Qd, Qm
        ///   A64: CLS Vd.16B, Vn.16B
        /// </summary>
        public static Vector128<sbyte> LeadingSignCount(Vector128<sbyte> value) => LeadingSignCount(value);

        /// <summary>
        /// uint8x8_t vclz_u8 (uint8x8_t a)
        ///   A32: VCLZ.I8 Dd, Dm
        ///   A64: CLZ Vd.8B, Vn.8B
        /// </summary>
        public static Vector64<byte> LeadingZeroCount(Vector64<byte> value) => LeadingZeroCount(value);

        /// <summary>
        /// int16x4_t vclz_s16 (int16x4_t a)
        ///   A32: VCLZ.I16 Dd, Dm
        ///   A64: CLZ Vd.4H, Vn.4H
        /// </summary>
        public static Vector64<short> LeadingZeroCount(Vector64<short> value) => LeadingZeroCount(value);

        /// <summary>
        /// int32x2_t vclz_s32 (int32x2_t a)
        ///   A32: VCLZ.I32 Dd, Dm
        ///   A64: CLZ Vd.2S, Vn.2S
        /// </summary>
        public static Vector64<int> LeadingZeroCount(Vector64<int> value) => LeadingZeroCount(value);

        /// <summary>
        /// int8x8_t vclz_s8 (int8x8_t a)
        ///   A32: VCLZ.I8 Dd, Dm
        ///   A64: CLZ Vd.8B, Vn.8B
        /// </summary>
        public static Vector64<sbyte> LeadingZeroCount(Vector64<sbyte> value) => LeadingZeroCount(value);

        /// <summary>
        /// uint16x4_t vclz_u16 (uint16x4_t a)
        ///   A32: VCLZ.I16 Dd, Dm
        ///   A64: CLZ Vd.4H, Vn.4H
        /// </summary>
        public static Vector64<ushort> LeadingZeroCount(Vector64<ushort> value) => LeadingZeroCount(value);

        /// <summary>
        /// uint32x2_t vclz_u32 (uint32x2_t a)
        ///   A32: VCLZ.I32 Dd, Dm
        ///   A64: CLZ Vd.2S, Vn.2S
        /// </summary>
        public static Vector64<uint> LeadingZeroCount(Vector64<uint> value) => LeadingZeroCount(value);

        /// <summary>
        /// uint8x16_t vclzq_u8 (uint8x16_t a)
        ///   A32: VCLZ.I8 Qd, Qm
        ///   A64: CLZ Vd.16B, Vn.16B
        /// </summary>
        public static Vector128<byte> LeadingZeroCount(Vector128<byte> value) => LeadingZeroCount(value);

        /// <summary>
        /// int16x8_t vclzq_s16 (int16x8_t a)
        ///   A32: VCLZ.I16 Qd, Qm
        ///   A64: CLZ Vd.8H, Vn.8H
        /// </summary>
        public static Vector128<short> LeadingZeroCount(Vector128<short> value) => LeadingZeroCount(value);

        /// <summary>
        /// int32x4_t vclzq_s32 (int32x4_t a)
        ///   A32: VCLZ.I32 Qd, Qm
        ///   A64: CLZ Vd.4S, Vn.4S
        /// </summary>
        public static Vector128<int> LeadingZeroCount(Vector128<int> value) => LeadingZeroCount(value);

        /// <summary>
        /// int8x16_t vclzq_s8 (int8x16_t a)
        ///   A32: VCLZ.I8 Qd, Qm
        ///   A64: CLZ Vd.16B, Vn.16B
        /// </summary>
        public static Vector128<sbyte> LeadingZeroCount(Vector128<sbyte> value) => LeadingZeroCount(value);

        /// <summary>
        /// uint16x8_t vclzq_u16 (uint16x8_t a)
        ///   A32: VCLZ.I16 Qd, Qm
        ///   A64: CLZ Vd.8H, Vn.8H
        /// </summary>
        public static Vector128<ushort> LeadingZeroCount(Vector128<ushort> value) => LeadingZeroCount(value);

        /// <summary>
        /// uint32x4_t vclzq_u32 (uint32x4_t a)
        ///   A32: VCLZ.I32 Qd, Qm
        ///   A64: CLZ Vd.4S, Vn.4S
        /// </summary>
        public static Vector128<uint> LeadingZeroCount(Vector128<uint> value) => LeadingZeroCount(value);

        /// <summary>
        /// uint8x8_t vld1_u8 (uint8_t const * ptr)
        ///   A32: VLD1.8 Dd, [Rn]
        ///   A64: LD1 Vt.8B, [Xn]
        /// </summary>
        public static unsafe Vector64<byte> LoadVector64(byte* address) => LoadVector64(address);

        /// <summary>
        /// float64x1_t vld1_f64 (float64_t const * ptr)
        ///   A32: VLD1.64 Dd, [Rn]
        ///   A64: LD1 Vt.1D, [Xn]
        /// </summary>
        public static unsafe Vector64<double> LoadVector64(double* address) => LoadVector64(address);

        /// <summary>
        /// int16x4_t vld1_s16 (int16_t const * ptr)
        ///   A32: VLD1.16 Dd, [Rn]
        ///   A64: LD1 Vt.4H, [Xn]
        /// </summary>
        public static unsafe Vector64<short> LoadVector64(short* address) => LoadVector64(address);

        /// <summary>
        /// int32x2_t vld1_s32 (int32_t const * ptr)
        ///   A32: VLD1.32 Dd, [Rn]
        ///   A64: LD1 Vt.2S, [Xn]
        /// </summary>
        public static unsafe Vector64<int> LoadVector64(int* address) => LoadVector64(address);

        /// <summary>
        /// int64x1_t vld1_s64 (int64_t const * ptr)
        ///   A32: VLD1.64 Dd, [Rn]
        ///   A64: LD1 Vt.1D, [Xn]
        /// </summary>
        public static unsafe Vector64<long> LoadVector64(long* address) => LoadVector64(address);

        /// <summary>
        /// int8x8_t vld1_s8 (int8_t const * ptr)
        ///   A32: VLD1.8 Dd, [Rn]
        ///   A64: LD1 Vt.8B, [Xn]
        /// </summary>
        public static unsafe Vector64<sbyte> LoadVector64(sbyte* address) => LoadVector64(address);

        /// <summary>
        /// float32x2_t vld1_f32 (float32_t const * ptr)
        ///   A32: VLD1.32 Dd, [Rn]
        ///   A64: LD1 Vt.2S, [Xn]
        /// </summary>
        public static unsafe Vector64<float> LoadVector64(float* address) => LoadVector64(address);

        /// <summary>
        /// uint16x4_t vld1_u16 (uint16_t const * ptr)
        ///   A32: VLD1.16 Dd, [Rn]
        ///   A64: LD1 Vt.4H, [Xn]
        /// </summary>
        public static unsafe Vector64<ushort> LoadVector64(ushort* address) => LoadVector64(address);

        /// <summary>
        /// uint32x2_t vld1_u32 (uint32_t const * ptr)
        ///   A32: VLD1.32 Dd, [Rn]
        ///   A64: LD1 Vt.2S, [Xn]
        /// </summary>
        public static unsafe Vector64<uint> LoadVector64(uint* address) => LoadVector64(address);

        /// <summary>
        /// uint64x1_t vld1_u64 (uint64_t const * ptr)
        ///   A32: VLD1.64 Dd, [Rn]
        ///   A64: LD1 Vt.1D, [Xn]
        /// </summary>
        public static unsafe Vector64<ulong> LoadVector64(ulong* address) => LoadVector64(address);

        /// <summary>
        /// uint8x16_t vld1q_u8 (uint8_t const * ptr)
        ///   A32: VLD1.8 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.16B, [Xn]
        /// </summary>
        public static unsafe Vector128<byte> LoadVector128(byte* address) => LoadVector128(address);

        /// <summary>
        /// float64x2_t vld1q_f64 (float64_t const * ptr)
        ///   A32: VLD1.64 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.2D, [Xn]
        /// </summary>
        public static unsafe Vector128<double> LoadVector128(double* address) => LoadVector128(address);

        /// <summary>
        /// int16x8_t vld1q_s16 (int16_t const * ptr)
        ///   A32: VLD1.16 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.8H, [Xn]
        /// </summary>
        public static unsafe Vector128<short> LoadVector128(short* address) => LoadVector128(address);

        /// <summary>
        /// int32x4_t vld1q_s32 (int32_t const * ptr)
        ///   A32: VLD1.32 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.4S, [Xn]
        /// </summary>
        public static unsafe Vector128<int> LoadVector128(int* address) => LoadVector128(address);

        /// <summary>
        /// int64x2_t vld1q_s64 (int64_t const * ptr)
        ///   A32: VLD1.64 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.2D, [Xn]
        /// </summary>
        public static unsafe Vector128<long> LoadVector128(long* address) => LoadVector128(address);

        /// <summary>
        /// int8x16_t vld1q_s8 (int8_t const * ptr)
        ///   A32: VLD1.8 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.16B, [Xn]
        /// </summary>
        public static unsafe Vector128<sbyte> LoadVector128(sbyte* address) => LoadVector128(address);

        /// <summary>
        /// float32x4_t vld1q_f32 (float32_t const * ptr)
        ///   A32: VLD1.32 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.4S, [Xn]
        /// </summary>
        public static unsafe Vector128<float> LoadVector128(float* address) => LoadVector128(address);

        /// <summary>
        /// uint16x8_t vld1q_s16 (uint16_t const * ptr)
        ///   A32: VLD1.16 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.8H, [Xn]
        /// </summary>
        public static unsafe Vector128<ushort> LoadVector128(ushort* address) => LoadVector128(address);

        /// <summary>
        /// uint32x4_t vld1q_s32 (uint32_t const * ptr)
        ///   A32: VLD1.32 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.4S, [Xn]
        /// </summary>
        public static unsafe Vector128<uint> LoadVector128(uint* address) => LoadVector128(address);

        /// <summary>
        /// uint64x2_t vld1q_u64 (uint64_t const * ptr)
        ///   A32: VLD1.64 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.2D, [Xn]
        /// </summary>
        public static unsafe Vector128<ulong> LoadVector128(ulong* address) => LoadVector128(address);

        /// <summary>
        /// uint8x8_t vmvn_u8 (uint8x8_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector64<byte> Not(Vector64<byte> value) => Not(value);

        // /// <summary>
        // /// float64x1_t vmvn_f64 (float64x1_t a)
        // ///   A32: VMVN Dd, Dn, Dm
        // ///   A64: MVN Vd, Vn, Vm
        // /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        // /// </summary>
        // public static Vector64<double> Not(Vector64<double> value) => Not(value);

        /// <summary>
        /// int16x4_t vmvn_s16 (int16x4_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector64<short> Not(Vector64<short> value) => Not(value);

        /// <summary>
        /// int32x2_t vmvn_s32(int32x2_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector64<int> Not(Vector64<int> value) => Not(value);

        // /// <summary>
        // /// int64x1_t vmvn_s64 (int64x1_t a)
        // ///   A32: VMVN Dd, Dn, Dm
        // ///   A64: MVN Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<long> Not(Vector64<long> value) => Not(value);

        /// <summary>
        /// int8x8_t vmvn_s8 (int8x8_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector64<sbyte> Not(Vector64<sbyte> value) => Not(value);

        /// <summary>
        /// float32x2_t vmvn_f32 (float32x2_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> Not(Vector64<float> value) => Not(value);

        /// <summary>
        /// uint16x4_t vmvn_u16 (uint16x4_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector64<ushort> Not(Vector64<ushort> value) => Not(value);

        /// <summary>
        /// uint32x2_t vmvn_u32 (uint32x2_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector64<uint> Not(Vector64<uint> value) => Not(value);

        // /// <summary>
        // /// uint64x1_t vmvn_u64 (uint64x1_t a)
        // ///   A32: VMVN Dd, Dn, Dm
        // ///   A64: MVN Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<ulong> Not(Vector64<ulong> value) => Not(value);

        /// <summary>
        /// uint8x16_t vmvn_u8 (uint8x16_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector128<byte> Not(Vector128<byte> value) => Not(value);

        /// <summary>
        /// float64x2_t vmvn_f64 (float64x2_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> Not(Vector128<double> value) => Not(value);

        /// <summary>
        /// int16x8_t vmvn_s16 (int16x8_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector128<short> Not(Vector128<short> value) => Not(value);

        /// <summary>
        /// int32x4_t vmvn_s32(int32x4_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector128<int> Not(Vector128<int> value) => Not(value);

        /// <summary>
        /// int64x2_t vmvn_s64 (int64x2_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector128<long> Not(Vector128<long> value) => Not(value);

        /// <summary>
        /// int8x16_t vmvn_s8 (int8x16_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector128<sbyte> Not(Vector128<sbyte> value) => Not(value);

        /// <summary>
        /// float32x4_t vmvn_f32 (float32x4_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> Not(Vector128<float> value) => Not(value);

        /// <summary>
        /// uint16x8_t vmvn_u16 (uint16x8_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector128<ushort> Not(Vector128<ushort> value) => Not(value);

        /// <summary>
        /// uint32x4_t vmvn_u32 (uint32x4_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector128<uint> Not(Vector128<uint> value) => Not(value);

        /// <summary>
        /// uint64x2_t vmvn_u64 (uint64x2_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector128<ulong> Not(Vector128<ulong> value) => Not(value);

        /// <summary>
        /// uint8x8_t vorr_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector64<byte> Or(Vector64<byte> left, Vector64<byte> right) => Or(left, right);

        // /// <summary>
        // /// float64x1_t vorr_f64 (float64x1_t a, float64x1_t b)
        // ///   A32: VORR Dd, Dn, Dm
        // ///   A64: ORR Vd, Vn, Vm
        // /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        // /// </summary>
        // public static Vector64<double> Or(Vector64<double> left, Vector64<double> right) => Or(left, right);

        /// <summary>
        /// int16x4_t vorr_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector64<short> Or(Vector64<short> left, Vector64<short> right) => Or(left, right);

        /// <summary>
        /// int32x2_t vorr_s32(int32x2_t a, int32x2_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector64<int> Or(Vector64<int> left, Vector64<int> right) => Or(left, right);

        // /// <summary>
        // /// int64x1_t vorr_s64 (int64x1_t a, int64x1_t b)
        // ///   A32: VORR Dd, Dn, Dm
        // ///   A64: ORR Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<long> Or(Vector64<long> left, Vector64<long> right) => Or(left, right);

        /// <summary>
        /// int8x8_t vorr_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector64<sbyte> Or(Vector64<sbyte> left, Vector64<sbyte> right) => Or(left, right);

        /// <summary>
        /// float32x2_t vorr_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> Or(Vector64<float> left, Vector64<float> right) => Or(left, right);

        /// <summary>
        /// uint16x4_t vorr_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector64<ushort> Or(Vector64<ushort> left, Vector64<ushort> right) => Or(left, right);

        /// <summary>
        /// uint32x2_t vorr_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector64<uint> Or(Vector64<uint> left, Vector64<uint> right) => Or(left, right);

        // /// <summary>
        // /// uint64x1_t vorr_u64 (uint64x1_t a, uint64x1_t b)
        // ///   A32: VORR Dd, Dn, Dm
        // ///   A64: ORR Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<ulong> Or(Vector64<ulong> left, Vector64<ulong> right) => Or(left, right);

        /// <summary>
        /// uint8x16_t vorr_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector128<byte> Or(Vector128<byte> left, Vector128<byte> right) => Or(left, right);

        /// <summary>
        /// float64x2_t vorr_f64 (float64x2_t a, float64x2_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> Or(Vector128<double> left, Vector128<double> right) => Or(left, right);

        /// <summary>
        /// int16x8_t vorr_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector128<short> Or(Vector128<short> left, Vector128<short> right) => Or(left, right);

        /// <summary>
        /// int32x4_t vorr_s32(int32x4_t a, int32x4_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector128<int> Or(Vector128<int> left, Vector128<int> right) => Or(left, right);

        /// <summary>
        /// int64x2_t vorr_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector128<long> Or(Vector128<long> left, Vector128<long> right) => Or(left, right);

        /// <summary>
        /// int8x16_t vorr_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector128<sbyte> Or(Vector128<sbyte> left, Vector128<sbyte> right) => Or(left, right);

        /// <summary>
        /// float32x4_t vorr_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> Or(Vector128<float> left, Vector128<float> right) => Or(left, right);

        /// <summary>
        /// uint16x8_t vorr_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector128<ushort> Or(Vector128<ushort> left, Vector128<ushort> right) => Or(left, right);

        /// <summary>
        /// uint32x4_t vorr_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector128<uint> Or(Vector128<uint> left, Vector128<uint> right) => Or(left, right);

        /// <summary>
        /// uint64x2_t vorr_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector128<ulong> Or(Vector128<ulong> left, Vector128<ulong> right) => Or(left, right);

        /// <summary>
        /// uint8x8_t vorn_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector64<byte> OrNot(Vector64<byte> left, Vector64<byte> right) => OrNot(left, right);

        // /// <summary>
        // /// float64x1_t vorn_f64 (float64x1_t a, float64x1_t b)
        // ///   A32: VORN Dd, Dn, Dm
        // ///   A64: ORN Vd, Vn, Vm
        // /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        // /// </summary>
        // public static Vector64<double> OrNot(Vector64<double> left, Vector64<double> right) => OrNot(left, right);

        /// <summary>
        /// int16x4_t vorn_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector64<short> OrNot(Vector64<short> left, Vector64<short> right) => OrNot(left, right);

        /// <summary>
        /// int32x2_t vorn_s32(int32x2_t a, int32x2_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector64<int> OrNot(Vector64<int> left, Vector64<int> right) => OrNot(left, right);

        // /// <summary>
        // /// int64x1_t vorn_s64 (int64x1_t a, int64x1_t b)
        // ///   A32: VORN Dd, Dn, Dm
        // ///   A64: ORN Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<long> OrNot(Vector64<long> left, Vector64<long> right) => OrNot(left, right);

        /// <summary>
        /// int8x8_t vorn_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector64<sbyte> OrNot(Vector64<sbyte> left, Vector64<sbyte> right) => OrNot(left, right);

        /// <summary>
        /// float32x2_t vorn_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> OrNot(Vector64<float> left, Vector64<float> right) => OrNot(left, right);

        /// <summary>
        /// uint16x4_t vorn_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector64<ushort> OrNot(Vector64<ushort> left, Vector64<ushort> right) => OrNot(left, right);

        /// <summary>
        /// uint32x2_t vorn_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector64<uint> OrNot(Vector64<uint> left, Vector64<uint> right) => OrNot(left, right);

        // /// <summary>
        // /// uint64x1_t vorn_u64 (uint64x1_t a, uint64x1_t b)
        // ///   A32: VORN Dd, Dn, Dm
        // ///   A64: ORN Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<ulong> OrNot(Vector64<ulong> left, Vector64<ulong> right) => OrNot(left, right);

        /// <summary>
        /// uint8x16_t vorn_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector128<byte> OrNot(Vector128<byte> left, Vector128<byte> right) => OrNot(left, right);

        /// <summary>
        /// float64x2_t vorn_f64 (float64x2_t a, float64x2_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> OrNot(Vector128<double> left, Vector128<double> right) => OrNot(left, right);

        /// <summary>
        /// int16x8_t vorn_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector128<short> OrNot(Vector128<short> left, Vector128<short> right) => OrNot(left, right);

        /// <summary>
        /// int32x4_t vorn_s32(int32x4_t a, int32x4_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector128<int> OrNot(Vector128<int> left, Vector128<int> right) => OrNot(left, right);

        /// <summary>
        /// int64x2_t vorn_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector128<long> OrNot(Vector128<long> left, Vector128<long> right) => OrNot(left, right);

        /// <summary>
        /// int8x16_t vorn_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector128<sbyte> OrNot(Vector128<sbyte> left, Vector128<sbyte> right) => OrNot(left, right);

        /// <summary>
        /// float32x4_t vorn_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> OrNot(Vector128<float> left, Vector128<float> right) => OrNot(left, right);

        /// <summary>
        /// uint16x8_t vorn_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector128<ushort> OrNot(Vector128<ushort> left, Vector128<ushort> right) => OrNot(left, right);

        /// <summary>
        /// uint32x4_t vorn_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector128<uint> OrNot(Vector128<uint> left, Vector128<uint> right) => OrNot(left, right);

        /// <summary>
        /// uint64x2_t vorn_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector128<ulong> OrNot(Vector128<ulong> left, Vector128<ulong> right) => OrNot(left, right);

        /// <summary>
        /// uint8x8_t vcnt_u8 (uint8x8_t a)
        ///   A32: VCNT.I8 Dd, Dm
        ///   A64: CNT Vd.8B, Vn.8B
        /// </summary>
        public static Vector64<byte> PopCount(Vector64<byte> value) => PopCount(value);

        /// <summary>
        /// int8x8_t vcnt_s8 (int8x8_t a)
        ///   A32: VCNT.I8 Dd, Dm
        ///   A64: CNT Vd.8B, Vn.8B
        /// </summary>
        public static Vector64<sbyte> PopCount(Vector64<sbyte> value) => PopCount(value);

        /// <summary>
        /// uint8x16_t vcntq_u8 (uint8x16_t a)
        ///   A32: VCNT.I8 Qd, Qm
        ///   A64: CNT Vd.16B, Vn.16B
        /// </summary>
        public static Vector128<byte> PopCount(Vector128<byte> value) => PopCount(value);

        /// <summary>
        /// int8x16_t vcntq_s8 (int8x16_t a)
        ///   A32: VCNT.I8 Qd, Qm
        ///   A64: CNT Vd.16B, Vn.16B
        /// </summary>
        public static Vector128<sbyte> PopCount(Vector128<sbyte> value) => PopCount(value);

        /// <summary>
        /// uint8x8_t vsub_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VSUB.I8 Dd, Dn, Dm
        ///   A64: ADD Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> Subtract(Vector64<byte> left, Vector64<byte> right) => Subtract(left, right);

        /// <summary>
        /// int16x4_t vsub_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VSUB.I16 Dd, Dn, Dm
        ///   A64: ADD Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> Subtract(Vector64<short> left, Vector64<short> right) => Subtract(left, right);

        /// <summary>
        /// int32x2_t vsub_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VSUB.I32 Dd, Dn, Dm
        ///   A64: ADD Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> Subtract(Vector64<int> left, Vector64<int> right) => Subtract(left, right);

        /// <summary>
        /// int8x8_t vsub_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VSUB.I8 Dd, Dn, Dm
        ///   A64: ADD Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> Subtract(Vector64<sbyte> left, Vector64<sbyte> right) => Subtract(left, right);

        /// <summary>
        /// float32x2_t vsub_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VSUB.F32 Dd, Dn, Dm
        ///   A64: FSUB Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<float> Subtract(Vector64<float> left, Vector64<float> right) => Subtract(left, right);

        /// <summary>
        /// uint16x4_t vsub_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VSUB.I16 Dd, Dn, Dm
        ///   A64: ADD Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> Subtract(Vector64<ushort> left, Vector64<ushort> right) => Subtract(left, right);

        /// <summary>
        /// uint32x2_t vsub_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VSUB.I32 Dd, Dn, Dm
        ///   A64: ADD Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> Subtract(Vector64<uint> left, Vector64<uint> right) => Subtract(left, right);

        /// <summary>
        /// uint8x16_t vsubq_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VSUB.I8 Qd, Qn, Qm
        ///   A64: ADD Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> Subtract(Vector128<byte> left, Vector128<byte> right) => Subtract(left, right);

        /// <summary>
        /// int16x8_t vsubq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VSUB.I16 Qd, Qn, Qm
        ///   A64: ADD Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> Subtract(Vector128<short> left, Vector128<short> right) => Subtract(left, right);

        /// <summary>
        /// int32x4_t vsubq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VSUB.I32 Qd, Qn, Qm
        ///   A64: ADD Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> Subtract(Vector128<int> left, Vector128<int> right) => Subtract(left, right);

        /// <summary>
        /// int64x2_t vsubq_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VSUB.I64 Qd, Qn, Qm
        ///   A64: ADD Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<long> Subtract(Vector128<long> left, Vector128<long> right) => Subtract(left, right);

        /// <summary>
        /// int8x16_t vsubq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VSUB.I8 Qd, Qn, Qm
        ///   A64: ADD Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> Subtract(Vector128<sbyte> left, Vector128<sbyte> right) => Subtract(left, right);

        /// <summary>
        /// float32x4_t vsubq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VSUB.F32 Qd, Qn, Qm
        ///   A64: FSUB Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<float> Subtract(Vector128<float> left, Vector128<float> right) => Subtract(left, right);

        /// <summary>
        /// uint16x8_t vsubq_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VSUB.I16 Qd, Qn, Qm
        ///   A64: ADD Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> Subtract(Vector128<ushort> left, Vector128<ushort> right) => Subtract(left, right);

        /// <summary>
        /// uint32x4_t vsubq_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VSUB.I32 Qd, Qn, Qm
        ///   A64: ADD Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> Subtract(Vector128<uint> left, Vector128<uint> right) => Subtract(left, right);

        /// <summary>
        /// uint64x2_t vsubq_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VSUB.I64 Qd, Qn, Qm
        ///   A64: ADD Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<ulong> Subtract(Vector128<ulong> left, Vector128<ulong> right) => Subtract(left, right);

        // /// <summary>
        // /// float64x1_t vsub_f64 (float64x1_t a, float64x1_t b)
        // ///   A32: VSUB.F64 Dd, Dn, Dm
        // ///   A64: FSUB Dd, Dn, Dm
        // /// </summary>
        // public static Vector64<double> SubtractScalar(Vector64<double> left, Vector64<double> right) => Subtract(left, right);

        // /// <summary>
        // /// int64x1_t vsub_s64 (int64x1_t a, int64x1_t b)
        // ///   A32: VSUB.I64 Dd, Dn, Dm
        // ///   A64: ADD Dd, Dn, Dm
        // /// </summary>
        // public static Vector64<long> SubtractScalar(Vector64<long> left, Vector64<long> right) => SubtractScalar(left, right);

        // /// <summary>
        // /// uint64x1_t vsub_u64 (uint64x1_t a, uint64x1_t b)
        // ///   A32: VSUB.I64 Dd, Dn, Dm
        // ///   A64: ADD Dd, Dn, Dm
        // /// </summary>
        // public static Vector64<ulong> SubtractScalar(Vector64<ulong> left, Vector64<ulong> right) => SubtractScalar(left, right);

        /// <summary>
        ///   A32: VSUB.F32 Sd, Sn, Sm
        ///   A64:
        /// </summary>
        public static Vector64<float> SubtractScalar(Vector64<float> left, Vector64<float> right) => SubtractScalar(left, right);

        /// <summary>
        /// uint8x8_t veor_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector64<byte> Xor(Vector64<byte> left, Vector64<byte> right) => Xor(left, right);

        // /// <summary>
        // /// float64x1_t veor_f64 (float64x1_t a, float64x1_t b)
        // ///   A32: VEOR Dd, Dn, Dm
        // ///   A64: EOR Vd, Vn, Vm
        // /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        // /// </summary>
        // public static Vector64<double> Xor(Vector64<double> left, Vector64<double> right) => Xor(left, right);

        /// <summary>
        /// int16x4_t veor_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector64<short> Xor(Vector64<short> left, Vector64<short> right) => Xor(left, right);

        /// <summary>
        /// int32x2_t veor_s32(int32x2_t a, int32x2_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector64<int> Xor(Vector64<int> left, Vector64<int> right) => Xor(left, right);

        // /// <summary>
        // /// int64x1_t veor_s64 (int64x1_t a, int64x1_t b)
        // ///   A32: VEOR Dd, Dn, Dm
        // ///   A64: EOR Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<long> Xor(Vector64<long> left, Vector64<long> right) => Xor(left, right);

        /// <summary>
        /// int8x8_t veor_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector64<sbyte> Xor(Vector64<sbyte> left, Vector64<sbyte> right) => Xor(left, right);

        /// <summary>
        /// float32x2_t veor_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> Xor(Vector64<float> left, Vector64<float> right) => Xor(left, right);

        /// <summary>
        /// uint16x4_t veor_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector64<ushort> Xor(Vector64<ushort> left, Vector64<ushort> right) => Xor(left, right);

        /// <summary>
        /// uint32x2_t veor_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector64<uint> Xor(Vector64<uint> left, Vector64<uint> right) => Xor(left, right);

        // /// <summary>
        // /// uint64x1_t veor_u64 (uint64x1_t a, uint64x1_t b)
        // ///   A32: VEOR Dd, Dn, Dm
        // ///   A64: EOR Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<ulong> Xor(Vector64<ulong> left, Vector64<ulong> right) => Xor(left, right);

        /// <summary>
        /// uint8x16_t veor_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector128<byte> Xor(Vector128<byte> left, Vector128<byte> right) => Xor(left, right);

        /// <summary>
        /// float64x2_t veor_f64 (float64x2_t a, float64x2_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> Xor(Vector128<double> left, Vector128<double> right) => Xor(left, right);

        /// <summary>
        /// int16x8_t veor_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector128<short> Xor(Vector128<short> left, Vector128<short> right) => Xor(left, right);

        /// <summary>
        /// int32x4_t veor_s32(int32x4_t a, int32x4_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector128<int> Xor(Vector128<int> left, Vector128<int> right) => Xor(left, right);

        /// <summary>
        /// int64x2_t veor_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector128<long> Xor(Vector128<long> left, Vector128<long> right) => Xor(left, right);

        /// <summary>
        /// int8x16_t veor_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector128<sbyte> Xor(Vector128<sbyte> left, Vector128<sbyte> right) => Xor(left, right);

        /// <summary>
        /// float32x4_t veor_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> Xor(Vector128<float> left, Vector128<float> right) => Xor(left, right);

        /// <summary>
        /// uint16x8_t veor_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector128<ushort> Xor(Vector128<ushort> left, Vector128<ushort> right) => Xor(left, right);

        /// <summary>
        /// uint32x4_t veor_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector128<uint> Xor(Vector128<uint> left, Vector128<uint> right) => Xor(left, right);

        /// <summary>
        /// uint64x2_t veor_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector128<ulong> Xor(Vector128<ulong> left, Vector128<ulong> right) => Xor(left, right);
    }
}
