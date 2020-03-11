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

        // [Intrinsic]
        // public new abstract class Arm32 : ArmBase.Arm32
        // {
        //     internal Arm32() { }
        //
        //     public static new bool IsSupported { get => IsSupported; }
        //
        //     /// <summary>
        //     /// float32x2_t vmla_f32 (float32x2_t a, float32x2_t b, float32x2_t c)
        //     ///   A32: VMLA.F32 Dd, Dn, Dm
        //     /// </summary>
        //     public static Vector64<float> MultiplyAdd(Vector64<float> acc, Vector64<float> left, Vector64<float> right) => MultiplyAdd(acc, left, right);
        //
        //     /// <summary>
        //     /// float32x4_t vmlaq_f32 (float32x4_t a, float32x4_t b, float32x4_t c)
        //     ///   A32: VMLA.F32 Qd, Qn, Qm
        //     /// </summary>
        //     public static Vector128<float> MultiplyAdd(Vector128<float> acc, Vector128<float> left, Vector128<float> right) => MultiplyAdd(acc, left, right);
        //
        //     /// <summary>
        //     /// float64x1_t vmla_f64 (float64x1_t a, float64x1_t b, float64x1_t c)
        //     ///   A32: VMLA.F64 Dd, Dn, Dm
        //     /// </summary>
        //     public static Vector64<double> MultiplyAddScalar(Vector64<double> acc, Vector64<double> left, Vector64<double> right) => MultiplyAddScalar(acc, left, right);
        //
        //     /// <summary>
        //     /// float32_t vmlas_f32 (float32_t a, float32_t b, float32_t c)
        //     ///   A32: VMLA.F32 Sd, Sn, Sm
        //     /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        //     /// </summary>
        //     public static Vector64<float> MultiplyAddScalar(Vector64<float> acc, Vector64<float> left, Vector64<float> right) => MultiplyAddScalar(acc, left, right);
        //
        //     /// <summary>
        //     /// float32x2_t vmls_f32 (float32x2_t a, float32x2_t b, float32x2_t c)
        //     ///   A32: VMLS.F32 Dd, Dn, Dm
        //     /// </summary>
        //     public static Vector64<float> MultiplySubtract(Vector64<float> acc, Vector64<float> left, Vector64<float> right) => MultiplySubtract(acc, left, right);
        //
        //     /// <summary>
        //     /// float32x4_t vmlsq_f32 (float32x4_t a, float32x4_t b, float32x4_t c)
        //     ///   A32: VMLS.F32 Qd, Qn, Qm
        //     /// </summary>
        //     public static Vector128<float> MultiplySubtract(Vector128<float> acc, Vector128<float> left, Vector128<float> right) => MultiplySubtract(acc, left, right);
        //
        //     /// <summary>
        //     /// float64x1_t vmls_f64 (float64x1_t a, float64x1_t b, float64x1_t c)
        //     ///   A32: VMLS.F64 Dd, Dn, Dm
        //     /// </summary>
        //     public static Vector64<double> MultiplySubtractScalar(Vector64<double> acc, Vector64<double> left, Vector64<double> right) => MultiplySubtractScalar(acc, left, right);
        //
        //     /// <summary>
        //     /// float32_t vmlss_f32 (float32_t a, float32_t b, float32_t c)
        //     ///   A32: VMLS.F32 Sd, Sn, Sm
        //     /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        //     /// </summary>
        //     public static Vector64<float> MultiplySubtractScalar(Vector64<float> acc, Vector64<float> left, Vector64<float> right) => MultiplySubtractScalar(acc, left, right);
        // }

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
            /// uint8_t vaddv_u8 (uint8x8_t a)
            ///   A64: ADDV Bd, Vn.8B
            /// </summary>
            public static Vector64<byte> AddAcross(Vector64<byte> value) => AddAcross(value);

            /// <summary>
            /// int16_t vaddv_s16 (int16x4_t a)
            ///   A64: ADDV Hd, Vn.4H
            /// </summary>
            public static Vector64<short> AddAcross(Vector64<short> value) => AddAcross(value);

            /// <summary>
            /// int8_t vaddv_s8 (int8x8_t a)
            ///   A64: ADDV Bd, Vn.8B
            /// </summary>
            public static Vector64<sbyte> AddAcross(Vector64<sbyte> value) => AddAcross(value);

            /// <summary>
            /// uint16_t vaddv_u16 (uint16x4_t a)
            ///   A64: ADDV Hd, Vn.4H
            /// </summary>
            public static Vector64<ushort> AddAcross(Vector64<ushort> value) => AddAcross(value);

            /// <summary>
            /// uint8_t vaddvq_u8 (uint8x16_t a)
            ///   A64: ADDV Bd, Vn.16B
            /// </summary>
            public static Vector64<byte> AddAcross(Vector128<byte> value) => AddAcross(value);

            /// <summary>
            /// int16_t vaddvq_s16 (int16x8_t a)
            ///   A64: ADDV Hd, Vn.8H
            /// </summary>
            public static Vector64<short> AddAcross(Vector128<short> value) => AddAcross(value);

            /// <summary>
            /// int32_t vaddvq_s32 (int32x4_t a)
            ///   A64: ADDV Sd, Vn.4S
            /// </summary>
            public static Vector64<int> AddAcross(Vector128<int> value) => AddAcross(value);

            /// <summary>
            /// int8_t vaddvq_s8 (int8x16_t a)
            ///   A64: ADDV Bd, Vn.16B
            /// </summary>
            public static Vector64<sbyte> AddAcross(Vector128<sbyte> value) => AddAcross(value);

            /// <summary>
            /// uint16_t vaddvq_u16 (uint16x8_t a)
            ///   A64: ADDV Hd, Vn.8H
            /// </summary>
            public static Vector64<ushort> AddAcross(Vector128<ushort> value) => AddAcross(value);

            /// <summary>
            /// uint32_t vaddvq_u32 (uint32x4_t a)
            ///   A64: ADDV Sd, Vn.4S
            /// </summary>
            public static Vector64<uint> AddAcross(Vector128<uint> value) => AddAcross(value);

            /// <summary>
            /// uint8x16_t vpaddq_u8 (uint8x16_t a, uint8x16_t b)
            ///   A64: ADDP Vd.16B, Vn.16B, Vm.16B
            /// </summary>
            public static Vector128<byte> AddPairwise(Vector128<byte> left, Vector128<byte> right) => AddPairwise(left, right);

            /// <summary>
            /// float64x2_t vpaddq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FADDP Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> AddPairwise(Vector128<double> left, Vector128<double> right) => AddPairwise(left, right);

            /// <summary>
            /// int16x8_t vpaddq_s16 (int16x8_t a, int16x8_t b)
            ///   A64: ADDP Vd.8H, Vn.8H, Vm.8H
            /// </summary>
            public static Vector128<short> AddPairwise(Vector128<short> left, Vector128<short> right) => AddPairwise(left, right);

            /// <summary>
            /// int32x4_t vpaddq_s32 (int32x4_t a, int32x4_t b)
            ///   A64: ADDP Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<int> AddPairwise(Vector128<int> left, Vector128<int> right) => AddPairwise(left, right);

            /// <summary>
            /// int64x2_t vpaddq_s64 (int64x2_t a, int64x2_t b)
            ///   A64: ADDP Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<long> AddPairwise(Vector128<long> left, Vector128<long> right) => AddPairwise(left, right);

            /// <summary>
            /// int8x16_t vpaddq_s8 (int8x16_t a, int8x16_t b)
            ///   A64: ADDP Vd.16B, Vn.16B, Vm.16B
            /// </summary>
            public static Vector128<sbyte> AddPairwise(Vector128<sbyte> left, Vector128<sbyte> right) => AddPairwise(left, right);

            /// <summary>
            /// float32x4_t vpaddq_f32 (float32x4_t a, float32x4_t b)
            ///   A64: FADDP Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<float> AddPairwise(Vector128<float> left, Vector128<float> right) => AddPairwise(left, right);

            /// <summary>
            /// uint16x8_t vpaddq_u16 (uint16x8_t a, uint16x8_t b)
            ///   A64: ADDP Vd.8H, Vn.8H, Vm.8H
            /// </summary>
            public static Vector128<ushort> AddPairwise(Vector128<ushort> left, Vector128<ushort> right) => AddPairwise(left, right);

            /// <summary>
            /// uint32x4_t vpaddq_u32 (uint32x4_t a, uint32x4_t b)
            ///   A64: ADDP Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<uint> AddPairwise(Vector128<uint> left, Vector128<uint> right) => AddPairwise(left, right);

            /// <summary>
            /// uint64x2_t vpaddq_u64 (uint64x2_t a, uint64x2_t b)
            ///   A64: ADDP Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<ulong> AddPairwise(Vector128<ulong> left, Vector128<ulong> right) => AddPairwise(left, right);

            /// <summary>
            /// float32_t vpadds_f32 (float32x2_t a)
            ///   A64: FADDP Sd, Vn.2S
            /// </summary>
            public static Vector64<float> AddPairwiseScalar(Vector64<float> value) => AddPairwiseScalar(value);

            /// <summary>
            /// float64_t vpaddd_f64 (float64x2_t a)
            ///   A64: FADDP Dd, Vn.2D
            /// </summary>
            public static Vector64<double> AddPairwiseScalar(Vector128<double> value) => AddPairwiseScalar(value);

            /// <summary>
            /// int64_t vpaddd_s64 (int64x2_t a)
            ///   A64: ADDP Dd, Vn.2D
            /// </summary>
            public static Vector64<long> AddPairwiseScalar(Vector128<long> value) => AddPairwiseScalar(value);

            /// <summary>
            /// uint64_t vpaddd_u64 (uint64x2_t a)
            ///   A64: ADDP Dd, Vn.2D
            /// </summary>
            public static Vector64<ulong> AddPairwiseScalar(Vector128<ulong> value) => AddPairwiseScalar(value);

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
            /// float32x2_t vdiv_f32 (float32x2_t a, float32x2_t b)
            ///   A64: FDIV Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<float> Divide(Vector64<float> left, Vector64<float> right) => Divide(left, right);

            /// <summary>
            /// float64x2_t vdivq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FDIV Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> Divide(Vector128<double> left, Vector128<double> right) => Divide(left, right);

            /// <summary>
            /// float32x4_t vdivq_f32 (float32x4_t a, float32x4_t b)
            ///   A64: FDIV Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<float> Divide(Vector128<float> left, Vector128<float> right) => Divide(left, right);

            /// <summary>
            /// float64x2_t vfmaq_f64 (float64x2_t a, float64x2_t b, float64x2_t c)
            ///   A64: FMLA Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> FusedMultiplyAdd(Vector128<double> acc, Vector128<double> left, Vector128<double> right) => FusedMultiplyAdd(acc, left, right);

            /// <summary>
            /// float64x2_t vfmsq_f64 (float64x2_t a, float64x2_t b, float64x2_t c)
            ///   A64: FMLS Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> FusedMultiplySubtract(Vector128<double> acc, Vector128<double> left, Vector128<double> right) => FusedMultiplySubtract(acc, left, right);

            /// <summary>
            /// float64x2_t vmaxq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FMAX Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> Max(Vector128<double> left, Vector128<double> right) => Max(left, right);

            /// <summary>
            /// uint8_t vmaxv_u8 (uint8x8_t a)
            ///   A64: UMAXV Bd, Vn.8B
            /// </summary>
            public static Vector64<byte> MaxAcross(Vector64<byte> value) => MaxAcross(value);

            /// <summary>
            /// int16_t vmaxv_s16 (int16x4_t a)
            ///   A64: SMAXV Hd, Vn.4H
            /// </summary>
            public static Vector64<short> MaxAcross(Vector64<short> value) => MaxAcross(value);

            /// <summary>
            /// int8_t vmaxv_s8 (int8x8_t a)
            ///   A64: SMAXV Bd, Vn.8B
            /// </summary>
            public static Vector64<sbyte> MaxAcross(Vector64<sbyte> value) => MaxAcross(value);

            /// <summary>
            /// uint16_t vmaxv_u16 (uint16x4_t a)
            ///   A64: UMAXV Hd, Vn.4H
            /// </summary>
            public static Vector64<ushort> MaxAcross(Vector64<ushort> value) => MaxAcross(value);

            /// <summary>
            /// uint8_t vmaxvq_u8 (uint8x16_t a)
            ///   A64: UMAXV Bd, Vn.16B
            /// </summary>
            public static Vector64<byte> MaxAcross(Vector128<byte> value) => MaxAcross(value);

            /// <summary>
            /// int16_t vmaxvq_s16 (int16x8_t a)
            ///   A64: SMAXV Hd, Vn.8H
            /// </summary>
            public static Vector64<short> MaxAcross(Vector128<short> value) => MaxAcross(value);

            /// <summary>
            /// int32_t vmaxvq_s32 (int32x4_t a)
            ///   A64: SMAXV Sd, Vn.4S
            /// </summary>
            public static Vector64<int> MaxAcross(Vector128<int> value) => MaxAcross(value);

            /// <summary>
            /// int8_t vmaxvq_s8 (int8x16_t a)
            ///   A64: SMAXV Bd, Vn.16B
            /// </summary>
            public static Vector64<sbyte> MaxAcross(Vector128<sbyte> value) => MaxAcross(value);

            /// <summary>
            /// float32_t vmaxvq_f32 (float32x4_t a)
            ///   A64: FMAXV Sd, Vn.4S
            /// </summary>
            public static Vector64<float> MaxAcross(Vector128<float> value) => MaxAcross(value);

            /// <summary>
            /// uint16_t vmaxvq_u16 (uint16x8_t a)
            ///   A64: UMAXV Hd, Vn.8H
            /// </summary>
            public static Vector64<ushort> MaxAcross(Vector128<ushort> value) => MaxAcross(value);

            /// <summary>
            /// uint32_t vmaxvq_u32 (uint32x4_t a)
            ///   A64: UMAXV Sd, Vn.4S
            /// </summary>
            public static Vector64<uint> MaxAcross(Vector128<uint> value) => MaxAcross(value);

            /// <summary>
            /// float64x2_t vmaxnmq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FMAXNM Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> MaxNumber(Vector128<double> left, Vector128<double> right) => MaxNumber(left, right);

            /// <summary>
            /// float32_t vmaxnmvq_f32 (float32x4_t a)
            ///   A64: FMAXNMV Sd, Vn.4S
            /// </summary>
            public static Vector64<float> MaxNumberAcross(Vector128<float> value) => MaxNumberAcross(value);

            /// <summary>
            /// float32x2_t vpmaxnm_f32 (float32x2_t a, float32x2_t b)
            ///   A64: FMAXNMP Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<float> MaxNumberPairwise(Vector64<float> left, Vector64<float> right) => MaxNumberPairwise(left, right);

            /// <summary>
            /// float64x2_t vpmaxnmq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FMAXNMP Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> MaxNumberPairwise(Vector128<double> left, Vector128<double> right) => MaxNumberPairwise(left, right);

            /// <summary>
            /// float32x4_t vpmaxnmq_f32 (float32x4_t a, float32x4_t b)
            ///   A64: FMAXNMP Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<float> MaxNumberPairwise(Vector128<float> left, Vector128<float> right) => MaxNumberPairwise(left, right);

            /// <summary>
            /// float32_t vpmaxnms_f32 (float32x2_t a)
            ///   A64: FMAXNMP Sd, Vn.2S
            /// </summary>
            public static Vector64<float> MaxNumberPairwiseScalar(Vector64<float> value) => MaxNumberPairwiseScalar(value);

            /// <summary>
            /// float64_t vpmaxnmqd_f64 (float64x2_t a)
            ///   A64: FMAXNMP Dd, Vn.2D
            /// </summary>
            public static Vector64<double> MaxNumberPairwiseScalar(Vector128<double> value) => MaxNumberPairwiseScalar(value);

            /// <summary>
            /// uint8x16_t vpmaxq_u8 (uint8x16_t a, uint8x16_t b)
            ///   A64: UMAXP Vd.16B, Vn.16B, Vm.16B
            /// </summary>
            public static Vector128<byte> MaxPairwise(Vector128<byte> left, Vector128<byte> right) => MaxPairwise(left, right);

            /// <summary>
            /// float64x2_t vpmaxq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FMAXP Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> MaxPairwise(Vector128<double> left, Vector128<double> right) => MaxPairwise(left, right);

            /// <summary>
            /// int16x8_t vpmaxq_s16 (int16x8_t a, int16x8_t b)
            ///   A64: SMAXP Vd.8H, Vn.8H, Vm.8H
            /// </summary>
            public static Vector128<short> MaxPairwise(Vector128<short> left, Vector128<short> right) => MaxPairwise(left, right);

            /// <summary>
            /// int32x4_t vpmaxq_s32 (int32x4_t a, int32x4_t b)
            ///   A64: SMAXP Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<int> MaxPairwise(Vector128<int> left, Vector128<int> right) => MaxPairwise(left, right);

            /// <summary>
            /// int8x16_t vpmaxq_s8 (int8x16_t a, int8x16_t b)
            ///   A64: SMAXP Vd.16B, Vn.16B, Vm.16B
            /// </summary>
            public static Vector128<sbyte> MaxPairwise(Vector128<sbyte> left, Vector128<sbyte> right) => MaxPairwise(left, right);

            /// <summary>
            /// float32x4_t vpmaxq_f32 (float32x4_t a, float32x4_t b)
            ///   A64: FMAXP Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<float> MaxPairwise(Vector128<float> left, Vector128<float> right) => MaxPairwise(left, right);

            /// <summary>
            /// uint16x8_t vpmaxq_u16 (uint16x8_t a, uint16x8_t b)
            ///   A64: UMAXP Vd.8H, Vn.8H, Vm.8H
            /// </summary>
            public static Vector128<ushort> MaxPairwise(Vector128<ushort> left, Vector128<ushort> right) => MaxPairwise(left, right);

            /// <summary>
            /// uint32x4_t vpmaxq_u32 (uint32x4_t a, uint32x4_t b)
            ///   A64: UMAXP Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<uint> MaxPairwise(Vector128<uint> left, Vector128<uint> right) => MaxPairwise(left, right);

            /// <summary>
            /// float32_t vpmaxs_f32 (float32x2_t a)
            ///   A64: FMAXP Sd, Vn.2S
            /// </summary>
            public static Vector64<float> MaxPairwiseScalar(Vector64<float> value) => MaxPairwiseScalar(value);

            /// <summary>
            /// float64_t vpmaxqd_f64 (float64x2_t a)
            ///   A64: FMAXP Dd, Vn.2D
            /// </summary>
            public static Vector64<double> MaxPairwiseScalar(Vector128<double> value) => MaxPairwiseScalar(value);

            /// <summary>
            /// float64x1_t vmax_f64 (float64x1_t a, float64x1_t b)
            ///   A64: FMAX Dd, Dn, Dm
            /// </summary>
            public static Vector64<double> MaxScalar(Vector64<double> left, Vector64<double> right) => MaxScalar(left, right);

            /// <summary>
            /// float32_t vmaxs_f32 (float32_t a, float32_t b)
            ///   A64: FMAX Sd, Sn, Sm
            /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
            /// </summary>
            public static Vector64<float> MaxScalar(Vector64<float> left, Vector64<float> right) => MaxScalar(left, right);

            /// <summary>
            /// float64x2_t vminq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FMIN Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> Min(Vector128<double> left, Vector128<double> right) => Min(left, right);

            /// <summary>
            /// uint8_t vminv_u8 (uint8x8_t a)
            ///   A64: UMINV Bd, Vn.8B
            /// </summary>
            public static Vector64<byte> MinAcross(Vector64<byte> value) => MinAcross(value);

            /// <summary>
            /// int16_t vminv_s16 (int16x4_t a)
            ///   A64: SMINV Hd, Vn.4H
            /// </summary>
            public static Vector64<short> MinAcross(Vector64<short> value) => MinAcross(value);

            /// <summary>
            /// int8_t vminv_s8 (int8x8_t a)
            ///   A64: SMINV Bd, Vn.8B
            /// </summary>
            public static Vector64<sbyte> MinAcross(Vector64<sbyte> value) => MinAcross(value);

            /// <summary>
            /// uint16_t vminv_u16 (uint16x4_t a)
            ///   A64: UMINV Hd, Vn.4H
            /// </summary>
            public static Vector64<ushort> MinAcross(Vector64<ushort> value) => MinAcross(value);

            /// <summary>
            /// uint8_t vminvq_u8 (uint8x16_t a)
            ///   A64: UMINV Bd, Vn.16B
            /// </summary>
            public static Vector64<byte> MinAcross(Vector128<byte> value) => MinAcross(value);

            /// <summary>
            /// int16_t vminvq_s16 (int16x8_t a)
            ///   A64: SMINV Hd, Vn.8H
            /// </summary>
            public static Vector64<short> MinAcross(Vector128<short> value) => MinAcross(value);

            /// <summary>
            /// int32_t vaddvq_s32 (int32x4_t a)
            ///   A64: SMINV Sd, Vn.4S
            /// </summary>
            public static Vector64<int> MinAcross(Vector128<int> value) => MinAcross(value);

            /// <summary>
            /// int8_t vminvq_s8 (int8x16_t a)
            ///   A64: SMINV Bd, Vn.16B
            /// </summary>
            public static Vector64<sbyte> MinAcross(Vector128<sbyte> value) => MinAcross(value);

            /// <summary>
            /// float32_t vminvq_f32 (float32x4_t a)
            ///   A64: FMINV Sd, Vn.4S
            /// </summary>
            public static Vector64<float> MinAcross(Vector128<float> value) => MinAcross(value);

            /// <summary>
            /// uint16_t vminvq_u16 (uint16x8_t a)
            ///   A64: UMINV Hd, Vn.8H
            /// </summary>
            public static Vector64<ushort> MinAcross(Vector128<ushort> value) => MinAcross(value);

            /// <summary>
            /// uint32_t vminvq_u32 (uint32x4_t a)
            ///   A64: UMINV Sd, Vn.4S
            /// </summary>
            public static Vector64<uint> MinAcross(Vector128<uint> value) => MinAcross(value);

            /// <summary>
            /// float64x2_t vminnmq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FMINNM Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> MinNumber(Vector128<double> left, Vector128<double> right) => MinNumber(left, right);

            /// <summary>
            /// float32_t vminnmvq_f32 (float32x4_t a)
            ///   A64: FMINNMV Sd, Vn.4S
            /// </summary>
            public static Vector64<float> MinNumberAcross(Vector128<float> value) => MinNumberAcross(value);

            /// <summary>
            /// float32x2_t vpminnm_f32 (float32x2_t a, float32x2_t b)
            ///   A64: FMINNMP Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<float> MinNumberPairwise(Vector64<float> left, Vector64<float> right) => MinNumberPairwise(left, right);

            /// <summary>
            /// float64x2_t vpminnmq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FMINNMP Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> MinNumberPairwise(Vector128<double> left, Vector128<double> right) => MinNumberPairwise(left, right);

            /// <summary>
            /// float32x4_t vpminnmq_f32 (float32x4_t a, float32x4_t b)
            ///   A64: FMINNMP Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<float> MinNumberPairwise(Vector128<float> left, Vector128<float> right) => MinNumberPairwise(left, right);

            /// <summary>
            /// float32_t vpminnms_f32 (float32x2_t a)
            ///   A64: FMINNMP Sd, Vn.2S
            /// </summary>
            public static Vector64<float> MinNumberPairwiseScalar(Vector64<float> value) => MinNumberPairwiseScalar(value);

            /// <summary>
            /// float64_t vpminnmqd_f64 (float64x2_t a)
            ///   A64: FMINNMP Dd, Vn.2D
            /// </summary>
            public static Vector64<double> MinNumberPairwiseScalar(Vector128<double> value) => MinNumberPairwiseScalar(value);

            /// <summary>
            /// uint8x16_t vpminq_u8 (uint8x16_t a, uint8x16_t b)
            ///   A64: UMINP Vd.16B, Vn.16B, Vm.16B
            /// </summary>
            public static Vector128<byte> MinPairwise(Vector128<byte> left, Vector128<byte> right) => MinPairwise(left, right);

            /// <summary>
            /// float64x2_t vpminq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FMINP Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> MinPairwise(Vector128<double> left, Vector128<double> right) => MinPairwise(left, right);

            /// <summary>
            /// int16x8_t vpminq_s16 (int16x8_t a, int16x8_t b)
            ///   A64: SMINP Vd.8H, Vn.8H, Vm.8H
            /// </summary>
            public static Vector128<short> MinPairwise(Vector128<short> left, Vector128<short> right) => MinPairwise(left, right);

            /// <summary>
            /// int32x4_t vpminq_s32 (int32x4_t a, int32x4_t b)
            ///   A64: SMINP Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<int> MinPairwise(Vector128<int> left, Vector128<int> right) => MinPairwise(left, right);

            /// <summary>
            /// int8x16_t vpminq_s8 (int8x16_t a, int8x16_t b)
            ///   A64: SMINP Vd.16B, Vn.16B, Vm.16B
            /// </summary>
            public static Vector128<sbyte> MinPairwise(Vector128<sbyte> left, Vector128<sbyte> right) => MinPairwise(left, right);

            /// <summary>
            /// float32x4_t vpminq_f32 (float32x4_t a, float32x4_t b)
            ///   A64: FMINP Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<float> MinPairwise(Vector128<float> left, Vector128<float> right) => MinPairwise(left, right);

            /// <summary>
            /// uint16x8_t vpminq_u16 (uint16x8_t a, uint16x8_t b)
            ///   A64: UMINP Vd.8H, Vn.8H, Vm.8H
            /// </summary>
            public static Vector128<ushort> MinPairwise(Vector128<ushort> left, Vector128<ushort> right) => MinPairwise(left, right);

            /// <summary>
            /// uint32x4_t vpminq_u32 (uint32x4_t a, uint32x4_t b)
            ///   A64: UMINP Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<uint> MinPairwise(Vector128<uint> left, Vector128<uint> right) => MinPairwise(left, right);

            /// <summary>
            /// float32_t vpmins_f32 (float32x2_t a)
            ///   A64: FMINP Sd, Vn.2S
            /// </summary>
            public static Vector64<float> MinPairwiseScalar(Vector64<float> value) => MinPairwiseScalar(value);

            /// <summary>
            /// float64_t vpminqd_f64 (float64x2_t a)
            ///   A64: FMINP Dd, Vn.2D
            /// </summary>
            public static Vector64<double> MinPairwiseScalar(Vector128<double> value) => MinPairwiseScalar(value);

            /// <summary>
            /// float64x1_t vmin_f64 (float64x1_t a, float64x1_t b)
            ///   A64: FMIN Dd, Dn, Dm
            /// </summary>
            public static Vector64<double> MinScalar(Vector64<double> left, Vector64<double> right) => MinScalar(left, right);

            /// <summary>
            /// float32_t vmins_f32 (float32_t a, float32_t b)
            ///   A64: FMIN Sd, Sn, Sm
            /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
            /// </summary>
            public static Vector64<float> MinScalar(Vector64<float> left, Vector64<float> right) => MinScalar(left, right);

            /// <summary>
            /// float64x2_t vmulq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FMUL Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> Multiply(Vector128<double> left, Vector128<double> right) => Multiply(left, right);

            /// <summary>
            /// float64x2_t vnegq_f64 (float64x2_t a)
            ///   A64: FNEG Vd.2D, Vn.2D
            /// </summary>
            public static Vector128<double> Negate(Vector128<double> value) => Negate(value);

            /// <summary>
            /// int64x2_t vnegq_s64 (int64x2_t a)
            ///   A64: NEG Vd.2D, Vn.2D
            /// </summary>
            public static Vector128<long> Negate(Vector128<long> value) => Negate(value);

            /// <summary>
            /// int64x1_t vneg_s64 (int64x1_t a)
            ///   A64: NEG Dd, Dn
            /// </summary>
            public static Vector64<long> NegateScalar(Vector64<long> value) => NegateScalar(value);

            /// <summary>
            /// float32x2_t vsqrt_f32 (float32x2_t a)
            ///   A64: FSQRT Vd.2S, Vn.2S
            /// </summary>
            public static Vector64<float> Sqrt(Vector64<float> value) => Sqrt(value);

            /// <summary>
            /// float64x2_t vsqrtq_f64 (float64x2_t a)
            ///   A64: FSQRT Vd.2D, Vn.2D
            /// </summary>
            public static Vector128<double> Sqrt(Vector128<double> value) => Sqrt(value);

            /// <summary>
            /// float32x4_t vsqrtq_f32 (float32x4_t a)
            ///   A64: FSQRT Vd.4S, Vn.4S
            /// </summary>
            public static Vector128<float> Sqrt(Vector128<float> value) => Sqrt(value);

            /// <summary>
            /// float64x2_t vsubq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FSUB Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> Subtract(Vector128<double> left, Vector128<double> right) => Subtract(left, right);

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
        /// uint8x8_t vpadd_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VPADD.I8 Dd, Dn, Dm
        ///   A64: ADDP Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> AddPairwise(Vector64<byte> left, Vector64<byte> right) => AddPairwise(left, right);

        /// <summary>
        /// int16x4_t vpadd_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VPADD.I16 Dd, Dn, Dm
        ///   A64: ADDP Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> AddPairwise(Vector64<short> left, Vector64<short> right) => AddPairwise(left, right);

        /// <summary>
        /// int32x2_t vpadd_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VPADD.I32 Dd, Dn, Dm
        ///   A64: ADDP Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> AddPairwise(Vector64<int> left, Vector64<int> right) => AddPairwise(left, right);

        /// <summary>
        /// int8x8_t vpadd_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VPADD.I8 Dd, Dn, Dm
        ///   A64: ADDP Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> AddPairwise(Vector64<sbyte> left, Vector64<sbyte> right) => AddPairwise(left, right);

        /// <summary>
        /// float32x2_t vpadd_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VPADD.F32 Dd, Dn, Dm
        ///   A64: FADDP Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<float> AddPairwise(Vector64<float> left, Vector64<float> right) => AddPairwise(left, right);

        /// <summary>
        /// uint16x4_t vpadd_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VPADD.I16 Dd, Dn, Dm
        ///   A64: ADDP Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> AddPairwise(Vector64<ushort> left, Vector64<ushort> right) => AddPairwise(left, right);

        /// <summary>
        /// uint32x2_t vpadd_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VPADD.I32 Dd, Dn, Dm
        ///   A64: ADDP Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> AddPairwise(Vector64<uint> left, Vector64<uint> right) => AddPairwise(left, right);

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
        ///   A64: AND Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> And(Vector64<byte> left, Vector64<byte> right) => And(left, right);

        /// <summary>
        /// float64x1_t vand_f64 (float64x1_t a, float64x1_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd.8B, Vn.8B, Vm.8B
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<double> And(Vector64<double> left, Vector64<double> right) => And(left, right);

        /// <summary>
        /// int16x4_t vand_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<short> And(Vector64<short> left, Vector64<short> right) => And(left, right);

        /// <summary>
        /// int32x2_t vand_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<int> And(Vector64<int> left, Vector64<int> right) => And(left, right);

        /// <summary>
        /// int64x1_t vand_s64 (int64x1_t a, int64x1_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<long> And(Vector64<long> left, Vector64<long> right) => And(left, right);

        /// <summary>
        /// int8x8_t vand_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> And(Vector64<sbyte> left, Vector64<sbyte> right) => And(left, right);

        /// <summary>
        /// float32x2_t vand_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd.8B, Vn.8B, Vm.8B
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> And(Vector64<float> left, Vector64<float> right) => And(left, right);

        /// <summary>
        /// uint16x4_t vand_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<ushort> And(Vector64<ushort> left, Vector64<ushort> right) => And(left, right);

        /// <summary>
        /// uint32x2_t vand_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<uint> And(Vector64<uint> left, Vector64<uint> right) => And(left, right);

        /// <summary>
        /// uint64x1_t vand_u64 (uint64x1_t a, uint64x1_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<ulong> And(Vector64<ulong> left, Vector64<ulong> right) => And(left, right);

        /// <summary>
        /// uint8x16_t vandq_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VAND Qd, Qn, Qm
        ///   A64: AND Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> And(Vector128<byte> left, Vector128<byte> right) => And(left, right);

        /// <summary>
        /// float64x2_t vandq_f64 (float64x2_t a, float64x2_t b)
        ///   A32: VAND Qd, Qn, Qm
        ///   A64: AND Vd.16B, Vn.16B, Vm.16B
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> And(Vector128<double> left, Vector128<double> right) => And(left, right);

        /// <summary>
        /// int16x8_t vandq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VAND Qd, Qn, Qm
        ///   A64: AND Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<short> And(Vector128<short> left, Vector128<short> right) => And(left, right);

        /// <summary>
        /// int32x4_t vandq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VAND Qd, Qn, Qm
        ///   A64: AND Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<int> And(Vector128<int> left, Vector128<int> right) => And(left, right);

        /// <summary>
        /// int64x2_t vandq_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VAND Qd, Qn, Qm
        ///   A64: AND Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<long> And(Vector128<long> left, Vector128<long> right) => And(left, right);

        /// <summary>
        /// int8x16_t vandq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VAND Qd, Qn, Qm
        ///   A64: AND Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> And(Vector128<sbyte> left, Vector128<sbyte> right) => And(left, right);

        /// <summary>
        /// float32x4_t vandq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VAND Qd, Qn, Qm
        ///   A64: AND Vd.16B, Vn.16B, Vm.16B
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> And(Vector128<float> left, Vector128<float> right) => And(left, right);

        /// <summary>
        /// uint16x8_t vandq_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VAND Qd, Qn, Qm
        ///   A64: AND Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<ushort> And(Vector128<ushort> left, Vector128<ushort> right) => And(left, right);

        /// <summary>
        /// uint32x4_t vandq_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VAND Qd, Qn, Qm
        ///   A64: AND Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<uint> And(Vector128<uint> left, Vector128<uint> right) => And(left, right);

        /// <summary>
        /// uint64x2_t vandq_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VAND Qd, Qn, Qm
        ///   A64: AND Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<ulong> And(Vector128<ulong> left, Vector128<ulong> right) => And(left, right);

        /// <summary>
        /// uint8x8_t vbic_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> BitwiseClear(Vector64<byte> value, Vector64<byte> mask) => BitwiseClear(value, mask);

        /// <summary>
        /// float64x1_t vbic_f64 (float64x1_t a, float64x1_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd.8B, Vn.8B, Vm.8B
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<double> BitwiseClear(Vector64<double> value, Vector64<double> mask) => BitwiseClear(value, mask);

        /// <summary>
        /// int16x4_t vbic_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<short> BitwiseClear(Vector64<short> value, Vector64<short> mask) => BitwiseClear(value, mask);

        /// <summary>
        /// int32x2_t vbic_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<int> BitwiseClear(Vector64<int> value, Vector64<int> mask) => BitwiseClear(value, mask);

        /// <summary>
        /// int64x1_t vbic_s64 (int64x1_t a, int64x1_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<long> BitwiseClear(Vector64<long> value, Vector64<long> mask) => BitwiseClear(value, mask);

        /// <summary>
        /// int8x8_t vbic_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> BitwiseClear(Vector64<sbyte> value, Vector64<sbyte> mask) => BitwiseClear(value, mask);

        /// <summary>
        /// float32x2_t vbic_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd.8B, Vn.8B, Vm.8B
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> BitwiseClear(Vector64<float> value, Vector64<float> mask) => BitwiseClear(value, mask);

        /// <summary>
        /// uint16x4_t vbic_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<ushort> BitwiseClear(Vector64<ushort> value, Vector64<ushort> mask) => BitwiseClear(value, mask);

        /// <summary>
        /// uint32x2_t vbic_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<uint> BitwiseClear(Vector64<uint> value, Vector64<uint> mask) => BitwiseClear(value, mask);

        /// <summary>
        /// uint64x1_t vbic_u64 (uint64x1_t a, uint64x1_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<ulong> BitwiseClear(Vector64<ulong> value, Vector64<ulong> mask) => BitwiseClear(value, mask);

        /// <summary>
        /// uint8x16_t vbicq_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VBIC Qd, Qn, Qm
        ///   A64: BIC Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> BitwiseClear(Vector128<byte> value, Vector128<byte> mask) => BitwiseClear(value, mask);

        /// <summary>
        /// float64x2_t vbicq_f64 (float64x2_t a, float64x2_t b)
        ///   A32: VBIC Qd, Qn, Qm
        ///   A64: BIC Vd.16B, Vn.16B, Vm.16B
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> BitwiseClear(Vector128<double> value, Vector128<double> mask) => BitwiseClear(value, mask);

        /// <summary>
        /// int16x8_t vbicq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VBIC Qd, Qn, Qm
        ///   A64: BIC Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<short> BitwiseClear(Vector128<short> value, Vector128<short> mask) => BitwiseClear(value, mask);

        /// <summary>
        /// int32x4_t vbicq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VBIC Qd, Qn, Qm
        ///   A64: BIC Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<int> BitwiseClear(Vector128<int> value, Vector128<int> mask) => BitwiseClear(value, mask);

        /// <summary>
        /// int64x2_t vbicq_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VBIC Qd, Qn, Qm
        ///   A64: BIC Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<long> BitwiseClear(Vector128<long> value, Vector128<long> mask) => BitwiseClear(value, mask);

        /// <summary>
        /// int8x16_t vbicq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VBIC Qd, Qn, Qm
        ///   A64: BIC Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> BitwiseClear(Vector128<sbyte> value, Vector128<sbyte> mask) => BitwiseClear(value, mask);

        /// <summary>
        /// float32x4_t vbicq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VBIC Qd, Qn, Qm
        ///   A64: BIC Vd.16B, Vn.16B, Vm.16B
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> BitwiseClear(Vector128<float> value, Vector128<float> mask) => BitwiseClear(value, mask);

        /// <summary>
        /// uint16x8_t vbicq_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VBIC Qd, Qn, Qm
        ///   A64: BIC Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<ushort> BitwiseClear(Vector128<ushort> value, Vector128<ushort> mask) => BitwiseClear(value, mask);

        /// <summary>
        /// uint32x4_t vbicq_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VBIC Qd, Qn, Qm
        ///   A64: BIC Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<uint> BitwiseClear(Vector128<uint> value, Vector128<uint> mask) => BitwiseClear(value, mask);

        /// <summary>
        /// uint64x2_t vbicq_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VBIC Qd, Qn, Qm
        ///   A64: BIC Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<ulong> BitwiseClear(Vector128<ulong> value, Vector128<ulong> mask) => BitwiseClear(value, mask);

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
        /// float64x1_t vdiv_f64 (float64x1_t a, float64x1_t b)
        ///   A32: VDIV.F64 Dd, Dn, Dm
        ///   A64: FDIV Dd, Dn, Dm
        /// </summary>
        public static Vector64<double> DivideScalar(Vector64<double> left, Vector64<double> right) => DivideScalar(left, right);

        /// <summary>
        /// float32_t vdivs_f32 (float32_t a, float32_t b)
        ///   A32: VDIV.F32 Sd, Sn, Sm
        ///   A64: FDIV Sd, Sn, Sm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> DivideScalar(Vector64<float> left, Vector64<float> right) => DivideScalar(left, right);

        /// <summary>
        /// float32x2_t vfma_f32 (float32x2_t a, float32x2_t b, float32x2_t c)
        ///   A32: VFMA.F32 Dd, Dn, Dm
        ///   A64: FMLA Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<float> FusedMultiplyAdd(Vector64<float> acc, Vector64<float> left, Vector64<float> right) => FusedMultiplyAdd(acc, left, right);

        /// <summary>
        /// float32x4_t vfmaq_f32 (float32x4_t a, float32x4_t b, float32x4_t c)
        ///   A32: VFMA.F32 Qd, Qn, Qm
        ///   A64: FMLA Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<float> FusedMultiplyAdd(Vector128<float> acc, Vector128<float> left, Vector128<float> right) => FusedMultiplyAdd(acc, left, right);

        /// <summary>
        /// float64x1_t vfma_f64 (float64x1_t a, float64x1_t b, float64x1_t c)
        ///   A32: VFMA.F64 Dd, Dn, Dm
        ///   A64: FMADD Dd, Dn, Dm, Da
        /// </summary>
        public static Vector64<double> FusedMultiplyAddScalar(Vector64<double> acc, Vector64<double> left, Vector64<double> right) => FusedMultiplyAddScalar(acc, left, right);

        /// <summary>
        /// float32_t vfmas_f32 (float32_t a, float32_t b, float32_t c)
        ///   A32: VFMA.F32 Sd, Sn, Sm
        ///   A64: FMADD Sd, Sn, Sm, Sa
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> FusedMultiplyAddScalar(Vector64<float> acc, Vector64<float> left, Vector64<float> right) => FusedMultiplyAddScalar(acc, left, right);

        /// <summary>
        /// float64x1_t vfnma_f64 (float64x1_t a, float64x1_t b, float64x1_t c)
        ///   A32: VFNMA.F64 Dd, Dn, Dm
        ///   A64: FNMADD Dd, Dn, Dm, Da
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<double> FusedMultiplyAddNegatedScalar(Vector64<double> acc, Vector64<double> left, Vector64<double> right) => FusedMultiplyAddNegatedScalar(acc, left, right);

        /// <summary>
        /// float32_t vfnmas_f32 (float32_t a, float32_t b, float32_t c)
        ///   A32: VFNMA.F32 Sd, Sn, Sm
        ///   A64: FNMADD Sd, Sn, Sm, Sa
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> FusedMultiplyAddNegatedScalar(Vector64<float> acc, Vector64<float> left, Vector64<float> right) => FusedMultiplyAddNegatedScalar(acc, left, right);

        /// <summary>
        /// float32x2_t vfms_f32 (float32x2_t a, float32x2_t b, float32x2_t c)
        ///   A32: VFMS.F32 Dd, Dn, Dm
        ///   A64: FMLS Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<float> FusedMultiplySubtract(Vector64<float> acc, Vector64<float> left, Vector64<float> right) => FusedMultiplySubtract(acc, left, right);

        /// <summary>
        /// float32x4_t vfmsq_f32 (float32x4_t a, float32x4_t b, float32x4_t c)
        ///   A32: VFMS.F32 Qd, Qn, Qm
        ///   A64: FMLS Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<float> FusedMultiplySubtract(Vector128<float> acc, Vector128<float> left, Vector128<float> right) => FusedMultiplySubtract(acc, left, right);

        /// <summary>
        /// float64x1_t vfms_f64 (float64x1_t a, float64x1_t b, float64x1_t c)
        ///   A32: VFMS.F64 Dd, Dn, Dm
        ///   A64: FMSUB Dd, Dn, Dm, Da
        /// </summary>
        public static Vector64<double> FusedMultiplySubtractScalar(Vector64<double> acc, Vector64<double> left, Vector64<double> right) => FusedMultiplySubtractScalar(acc, left, right);

        /// <summary>
        /// float32_t vfmss_f32 (float32_t a, float32_t b, float32_t c)
        ///   A32: VFMS.F32 Sd, Sn, Sm
        ///   A64: FMSUB Sd, Sn, Sm, Sa
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> FusedMultiplySubtractScalar(Vector64<float> acc, Vector64<float> left, Vector64<float> right) => FusedMultiplySubtractScalar(acc, left, right);

        /// <summary>
        /// float64x1_t vfnms_f64 (float64x1_t a, float64x1_t b, float64x1_t c)
        ///   A32: VFNMS.F64 Dd, Dn, Dm
        ///   A64: FNMSUB Dd, Dn, Dm, Da
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<double> FusedMultiplySubtractNegatedScalar(Vector64<double> acc, Vector64<double> left, Vector64<double> right) => FusedMultiplySubtractNegatedScalar(acc, left, right);

        /// <summary>
        /// float32_t vfnmss_f32 (float32_t a, float32_t b, float32_t c)
        ///   A32: VFNMS.F32 Sd, Sn, Sm
        ///   A64: FNMSUB Sd, Sn, Sm, Sa
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> FusedMultiplySubtractNegatedScalar(Vector64<float> acc, Vector64<float> left, Vector64<float> right) => FusedMultiplySubtractNegatedScalar(acc, left, right);

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
        /// uint8x8_t vmax_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VMAX.U8 Dd, Dn, Dm
        ///   A64: UMAX Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> Max(Vector64<byte> left, Vector64<byte> right) => Max(left, right);

        /// <summary>
        /// int16x4_t vmax_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VMAX.S16 Dd, Dn, Dm
        ///   A64: SMAX Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> Max(Vector64<short> left, Vector64<short> right) => Max(left, right);

        /// <summary>
        /// int32x2_t vmax_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VMAX.S32 Dd, Dn, Dm
        ///   A64: SMAX Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> Max(Vector64<int> left, Vector64<int> right) => Max(left, right);

        /// <summary>
        /// int8x8_t vmax_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VMAX.S8 Dd, Dn, Dm
        ///   A64: SMAX Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> Max(Vector64<sbyte> left, Vector64<sbyte> right) => Max(left, right);

        /// <summary>
        /// float32x2_t vmax_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VMAX.F32 Dd, Dn, Dm
        ///   A64: FMAX Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<float> Max(Vector64<float> left, Vector64<float> right) => Max(left, right);

        /// <summary>
        /// uint16x4_t vmax_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VMAX.U16 Dd, Dn, Dm
        ///   A64: UMAX Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> Max(Vector64<ushort> left, Vector64<ushort> right) => Max(left, right);

        /// <summary>
        /// uint32x2_t vmax_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VMAX.U32 Dd, Dn, Dm
        ///   A64: UMAX Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> Max(Vector64<uint> left, Vector64<uint> right) => Max(left, right);

        /// <summary>
        /// uint8x16_t vmaxq_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VMAX.U8 Qd, Qn, Qm
        ///   A64: UMAX Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> Max(Vector128<byte> left, Vector128<byte> right) => Max(left, right);

        /// <summary>
        /// int16x8_t vmaxq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VMAX.S16 Qd, Qn, Qm
        ///   A64: SMAX Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> Max(Vector128<short> left, Vector128<short> right) => Max(left, right);

        /// <summary>
        /// int32x4_t vmaxq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VMAX.S32 Qd, Qn, Qm
        ///   A64: SMAX Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> Max(Vector128<int> left, Vector128<int> right) => Max(left, right);

        /// <summary>
        /// int8x16_t vmaxq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VMAX.S8 Qd, Qn, Qm
        ///   A64: SMAX Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> Max(Vector128<sbyte> left, Vector128<sbyte> right) => Max(left, right);

        /// <summary>
        /// float32x4_t vmaxq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VMAX.F32 Qd, Qn, Qm
        ///   A64: FMAX Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<float> Max(Vector128<float> left, Vector128<float> right) => Max(left, right);

        /// <summary>
        /// uint16x8_t vmaxq_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VMAX.U16 Qd, Qn, Qm
        ///   A64: UMAX Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> Max(Vector128<ushort> left, Vector128<ushort> right) => Max(left, right);

        /// <summary>
        /// uint32x4_t vmaxq_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VMAX.U32 Qd, Qn, Qm
        ///   A64: UMAX Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> Max(Vector128<uint> left, Vector128<uint> right) => Max(left, right);

        /// <summary>
        /// float32x2_t vmaxnm_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VMAXNM.F32 Dd, Dn, Dm
        ///   A64: FMAXNM Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<float> MaxNumber(Vector64<float> left, Vector64<float> right) => MaxNumber(left, right);

        /// <summary>
        /// float32x4_t vmaxnmq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VMAXNM.F32 Qd, Qn, Qm
        ///   A64: FMAXNM Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<float> MaxNumber(Vector128<float> left, Vector128<float> right) => MaxNumber(left, right);

        /// <summary>
        /// float64x1_t vmaxnm_f64 (float64x1_t a, float64x1_t b)
        ///   A32: VMAXNM.F64 Dd, Dn, Dm
        ///   A64: FMAXNM Dd, Dn, Dm
        /// </summary>
        public static Vector64<double> MaxNumberScalar(Vector64<double> left, Vector64<double> right) => MaxNumberScalar(left, right);

        /// <summary>
        /// float32_t vmaxnms_f32 (float32_t a, float32_t b)
        ///   A32: VMAXNM.F32 Sd, Sn, Sm
        ///   A64: FMAXNM Sd, Sn, Sm
        /// </summary>
        public static Vector64<float> MaxNumberScalar(Vector64<float> left, Vector64<float> right) => MaxNumberScalar(left, right);

        /// <summary>
        /// uint8x8_t vpmax_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VPMAX.U8 Dd, Dn, Dm
        ///   A64: UMAXP Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> MaxPairwise(Vector64<byte> left, Vector64<byte> right) => MaxPairwise(left, right);

        /// <summary>
        /// int16x4_t vpmax_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VPMAX.S16 Dd, Dn, Dm
        ///   A64: SMAXP Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> MaxPairwise(Vector64<short> left, Vector64<short> right) => MaxPairwise(left, right);

        /// <summary>
        /// int32x2_t vpmax_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VPMAX.S32 Dd, Dn, Dm
        ///   A64: SMAXP Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> MaxPairwise(Vector64<int> left, Vector64<int> right) => MaxPairwise(left, right);

        /// <summary>
        /// int8x8_t vpmax_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VPMAX.S8 Dd, Dn, Dm
        ///   A64: SMAXP Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> MaxPairwise(Vector64<sbyte> left, Vector64<sbyte> right) => MaxPairwise(left, right);

        /// <summary>
        /// float32x2_t vpmax_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VPMAX.F32 Dd, Dn, Dm
        ///   A64: FMAXP Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<float> MaxPairwise(Vector64<float> left, Vector64<float> right) => MaxPairwise(left, right);

        /// <summary>
        /// uint16x4_t vpmax_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VPMAX.U16 Dd, Dn, Dm
        ///   A64: UMAXP Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> MaxPairwise(Vector64<ushort> left, Vector64<ushort> right) => MaxPairwise(left, right);

        /// <summary>
        /// uint32x2_t vpmax_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VPMAX.U32 Dd, Dn, Dm
        ///   A64: UMAXP Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> MaxPairwise(Vector64<uint> left, Vector64<uint> right) => MaxPairwise(left, right);

        /// <summary>
        /// uint8x8_t vmin_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VMIN.U8 Dd, Dn, Dm
        ///   A64: UMIN Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> Min(Vector64<byte> left, Vector64<byte> right) => Min(left, right);

        /// <summary>
        /// int16x4_t vmin_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VMIN.S16 Dd, Dn, Dm
        ///   A64: SMIN Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> Min(Vector64<short> left, Vector64<short> right) => Min(left, right);

        /// <summary>
        /// int32x2_t vmin_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VMIN.S32 Dd, Dn, Dm
        ///   A64: SMIN Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> Min(Vector64<int> left, Vector64<int> right) => Min(left, right);

        /// <summary>
        /// int8x8_t vmin_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VMIN.S8 Dd, Dn, Dm
        ///   A64: SMIN Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> Min(Vector64<sbyte> left, Vector64<sbyte> right) => Min(left, right);

        /// <summary>
        /// float32x2_t vmin_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VMIN.F32 Dd, Dn, Dm
        ///   A64: FMIN Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<float> Min(Vector64<float> left, Vector64<float> right) => Min(left, right);

        /// <summary>
        /// uint16x4_t vmin_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VMIN.U16 Dd, Dn, Dm
        ///   A64: UMIN Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> Min(Vector64<ushort> left, Vector64<ushort> right) => Min(left, right);

        /// <summary>
        /// uint32x2_t vmin_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VMIN.U32 Dd, Dn, Dm
        ///   A64: UMIN Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> Min(Vector64<uint> left, Vector64<uint> right) => Min(left, right);

        /// <summary>
        /// uint8x16_t vminq_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VMIN.U8 Qd, Qn, Qm
        ///   A64: UMIN Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> Min(Vector128<byte> left, Vector128<byte> right) => Min(left, right);

        /// <summary>
        /// int16x8_t vminq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VMIN.S16 Qd, Qn, Qm
        ///   A64: SMIN Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> Min(Vector128<short> left, Vector128<short> right) => Min(left, right);

        /// <summary>
        /// int32x4_t vminq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VMIN.S32 Qd, Qn, Qm
        ///   A64: SMIN Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> Min(Vector128<int> left, Vector128<int> right) => Min(left, right);

        /// <summary>
        /// int8x16_t vminq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VMIN.S8 Qd, Qn, Qm
        ///   A64: SMIN Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> Min(Vector128<sbyte> left, Vector128<sbyte> right) => Min(left, right);

        /// <summary>
        /// float32x4_t vminq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VMIN.F32 Qd, Qn, Qm
        ///   A64: FMIN Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<float> Min(Vector128<float> left, Vector128<float> right) => Min(left, right);

        /// <summary>
        /// uint16x8_t vminq_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VMIN.U16 Qd, Qn, Qm
        ///   A64: UMIN Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> Min(Vector128<ushort> left, Vector128<ushort> right) => Min(left, right);

        /// <summary>
        /// uint32x4_t vminq_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VMIN.U32 Qd, Qn, Qm
        ///   A64: UMIN Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> Min(Vector128<uint> left, Vector128<uint> right) => Min(left, right);

        /// <summary>
        /// float32x2_t vminnm_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VMINNM.F32 Dd, Dn, Dm
        ///   A64: FMINNM Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<float> MinNumber(Vector64<float> left, Vector64<float> right) => MinNumber(left, right);

        /// <summary>
        /// float32x4_t vminnmq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VMINNM.F32 Qd, Qn, Qm
        ///   A64: FMINNM Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<float> MinNumber(Vector128<float> left, Vector128<float> right) => MinNumber(left, right);

        /// <summary>
        /// float64x1_t vminnm_f64 (float64x1_t a, float64x1_t b)
        ///   A32: VMINNM.F64 Dd, Dn, Dm
        ///   A64: FMINNM Dd, Dn, Dm
        /// </summary>
        public static Vector64<double> MinNumberScalar(Vector64<double> left, Vector64<double> right) => MinNumberScalar(left, right);

        /// <summary>
        /// float32_t vminnms_f32 (float32_t a, float32_t b)
        ///   A32: VMINNM.F32 Sd, Sn, Sm
        ///   A64: FMINNM Sd, Sn, Sm
        /// </summary>
        public static Vector64<float> MinNumberScalar(Vector64<float> left, Vector64<float> right) => MinNumberScalar(left, right);

        /// <summary>
        /// uint8x8_t vpmin_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VPMIN.U8 Dd, Dn, Dm
        ///   A64: UMINP Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> MinPairwise(Vector64<byte> left, Vector64<byte> right) => MinPairwise(left, right);

        /// <summary>
        /// int16x4_t vpmin_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VPMIN.S16 Dd, Dn, Dm
        ///   A64: SMINP Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> MinPairwise(Vector64<short> left, Vector64<short> right) => MinPairwise(left, right);

        /// <summary>
        /// int32x2_t vpmin_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VPMIN.S32 Dd, Dn, Dm
        ///   A64: SMINP Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> MinPairwise(Vector64<int> left, Vector64<int> right) => MinPairwise(left, right);

        /// <summary>
        /// int8x8_t vpmin_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VPMIN.S8 Dd, Dn, Dm
        ///   A64: SMINP Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> MinPairwise(Vector64<sbyte> left, Vector64<sbyte> right) => MinPairwise(left, right);

        /// <summary>
        /// float32x2_t vpmin_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VPMIN.F32 Dd, Dn, Dm
        ///   A64: FMINP Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<float> MinPairwise(Vector64<float> left, Vector64<float> right) => MinPairwise(left, right);

        /// <summary>
        /// uint16x4_t vpmin_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VPMIN.U16 Dd, Dn, Dm
        ///   A64: UMINP Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> MinPairwise(Vector64<ushort> left, Vector64<ushort> right) => MinPairwise(left, right);

        /// <summary>
        /// uint32x2_t vpmin_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VPMIN.U32 Dd, Dn, Dm
        ///   A64: UMINP Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> MinPairwise(Vector64<uint> left, Vector64<uint> right) => MinPairwise(left, right);

        /// <summary>
        /// uint8x8_t vmul_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VMUL.I8 Dd, Dn, Dm
        ///   A64: MUL Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> Multiply(Vector64<byte> left, Vector64<byte> right) => Multiply(left, right);

        /// <summary>
        /// int16x4_t vmul_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VMUL.I16 Dd, Dn, Dm
        ///   A64: MUL Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> Multiply(Vector64<short> left, Vector64<short> right) => Multiply(left, right);

        /// <summary>
        /// int32x2_t vmul_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VMUL.I32 Dd, Dn, Dm
        ///   A64: MUL Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> Multiply(Vector64<int> left, Vector64<int> right) => Multiply(left, right);

        /// <summary>
        /// int8x8_t vmul_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VMUL.I8 Dd, Dn, Dm
        ///   A64: MUL Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> Multiply(Vector64<sbyte> left, Vector64<sbyte> right) => Multiply(left, right);

        /// <summary>
        /// float32x2_t vmul_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VMUL.F32 Dd, Dn, Dm
        ///   A64: FMUL Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<float> Multiply(Vector64<float> left, Vector64<float> right) => Multiply(left, right);

        /// <summary>
        /// uint16x4_t vmul_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VMUL.I16 Dd, Dn, Dm
        ///   A64: MUL Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> Multiply(Vector64<ushort> left, Vector64<ushort> right) => Multiply(left, right);

        /// <summary>
        /// uint32x2_t vmul_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VMUL.I32 Dd, Dn, Dm
        ///   A64: MUL Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> Multiply(Vector64<uint> left, Vector64<uint> right) => Multiply(left, right);

        /// <summary>
        /// uint8x16_t vmulq_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VMUL.I8 Qd, Qn, Qm
        ///   A64: MUL Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> Multiply(Vector128<byte> left, Vector128<byte> right) => Multiply(left, right);

        /// <summary>
        /// int16x8_t vmulq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VMUL.I16 Qd, Qn, Qm
        ///   A64: MUL Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> Multiply(Vector128<short> left, Vector128<short> right) => Multiply(left, right);

        /// <summary>
        /// int32x4_t vmulq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VMUL.I32 Qd, Qn, Qm
        ///   A64: MUL Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> Multiply(Vector128<int> left, Vector128<int> right) => Multiply(left, right);

        /// <summary>
        /// int8x16_t vmulq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VMUL.I8 Qd, Qn, Qm
        ///   A64: MUL Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> Multiply(Vector128<sbyte> left, Vector128<sbyte> right) => Multiply(left, right);

        /// <summary>
        /// float32x4_t vmulq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VMUL.F32 Qd, Qn, Qm
        ///   A64: FMUL Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<float> Multiply(Vector128<float> left, Vector128<float> right) => Multiply(left, right);

        /// <summary>
        /// uint16x8_t vmulq_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VMUL.I16 Qd, Qn, Qm
        ///   A64: MUL Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> Multiply(Vector128<ushort> left, Vector128<ushort> right) => Multiply(left, right);

        /// <summary>
        /// uint32x4_t vmulq_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VMUL.I32 Qd, Qn, Qm
        ///   A64: MUL Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> Multiply(Vector128<uint> left, Vector128<uint> right) => Multiply(left, right);

        /// <summary>
        /// float64x1_t vmul_f64 (float64x1_t a, float64x1_t b)
        ///   A32: VMUL.F64 Dd, Dn, Dm
        ///   A64: FMUL Dd, Dn, Dm
        /// </summary>
        public static Vector64<double> MultiplyScalar(Vector64<double> left, Vector64<double> right) => MultiplyScalar(left, right);

        /// <summary>
        /// float32_t vmuls_f32 (float32_t a, float32_t b)
        ///   A32: VMUL.F32 Sd, Sn, Sm
        ///   A64: FMUL Sd, Sn, Sm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> MultiplyScalar(Vector64<float> left, Vector64<float> right) => MultiplyScalar(left, right);

        /// <summary>
        /// uint8x8_t vmla_u8 (uint8x8_t a, uint8x8_t b, uint8x8_t c)
        ///   A32: VMLA.I8 Dd, Dn, Dm
        ///   A64: MLA Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> MultiplyAdd(Vector64<byte> acc, Vector64<byte> left, Vector64<byte> right) => MultiplyAdd(acc, left, right);

        /// <summary>
        /// int16x4_t vmla_s16 (int16x4_t a, int16x4_t b, int16x4_t c)
        ///   A32: VMLA.I16 Dd, Dn, Dm
        ///   A64: MLA Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> MultiplyAdd(Vector64<short> acc, Vector64<short> left, Vector64<short> right) => MultiplyAdd(acc, left, right);

        /// <summary>
        /// int32x2_t vmla_s32 (int32x2_t a, int32x2_t b, int32x2_t c)
        ///   A32: VMLA.I32 Dd, Dn, Dm
        ///   A64: MLA Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> MultiplyAdd(Vector64<int> acc, Vector64<int> left, Vector64<int> right) => MultiplyAdd(acc, left, right);

        /// <summary>
        /// int8x8_t vmla_s8 (int8x8_t a, int8x8_t b, int8x8_t c)
        ///   A32: VMLA.I8 Dd, Dn, Dm
        ///   A64: MLA Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> MultiplyAdd(Vector64<sbyte> acc, Vector64<sbyte> left, Vector64<sbyte> right) => MultiplyAdd(acc, left, right);

        /// <summary>
        /// uint16x4_t vmla_u16 (uint16x4_t a, uint16x4_t b, uint16x4_t c)
        ///   A32: VMLA.I16 Dd, Dn, Dm
        ///   A64: MLA Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> MultiplyAdd(Vector64<ushort> acc, Vector64<ushort> left, Vector64<ushort> right) => MultiplyAdd(acc, left, right);

        /// <summary>
        /// uint32x2_t vmla_u32 (uint32x2_t a, uint32x2_t b, uint32x2_t c)
        ///   A32: VMLA.I32 Dd, Dn, Dm
        ///   A64: MLA Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> MultiplyAdd(Vector64<uint> acc, Vector64<uint> left, Vector64<uint> right) => MultiplyAdd(acc, left, right);

        /// <summary>
        /// uint8x16_t vmlaq_u8 (uint8x16_t a, uint8x16_t b, uint8x16_t c)
        ///   A32: VMLA.I8 Qd, Qn, Qm
        ///   A64: MLA Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> MultiplyAdd(Vector128<byte> acc, Vector128<byte> left, Vector128<byte> right) => MultiplyAdd(acc, left, right);

        /// <summary>
        /// int16x8_t vmlaq_s16 (int16x8_t a, int16x8_t b, int16x8_t c)
        ///   A32: VMLA.I16 Qd, Qn, Qm
        ///   A64: MLA Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> MultiplyAdd(Vector128<short> acc, Vector128<short> left, Vector128<short> right) => MultiplyAdd(acc, left, right);

        /// <summary>
        /// int32x4_t vmlaq_s32 (int32x4_t a, int32x4_t b, int32x4_t c)
        ///   A32: VMLA.I32 Qd, Qn, Qm
        ///   A64: MLA Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> MultiplyAdd(Vector128<int> acc, Vector128<int> left, Vector128<int> right) => MultiplyAdd(acc, left, right);

        /// <summary>
        /// int8x16_t vmlaq_s8 (int8x16_t a, int8x16_t b, int8x16_t c)
        ///   A32: VMLA.I8 Qd, Qn, Qm
        ///   A64: MLA Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> MultiplyAdd(Vector128<sbyte> acc, Vector128<sbyte> left, Vector128<sbyte> right) => MultiplyAdd(acc, left, right);

        /// <summary>
        /// uint16x8_t vmlaq_u16 (uint16x8_t a, uint16x8_t b, uint16x8_t c)
        ///   A32: VMLA.I16 Qd, Qn, Qm
        ///   A64: MLA Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> MultiplyAdd(Vector128<ushort> acc, Vector128<ushort> left, Vector128<ushort> right) => MultiplyAdd(acc, left, right);

        /// <summary>
        /// uint32x4_t vmlaq_u32 (uint32x4_t a, uint32x4_t b, uint32x4_t c)
        ///   A32: VMLA.I32 Qd, Qn, Qm
        ///   A64: MLA Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> MultiplyAdd(Vector128<uint> acc, Vector128<uint> left, Vector128<uint> right) => MultiplyAdd(acc, left, right);

        /// <summary>
        /// uint8x8_t vmls_u8 (uint8x8_t a, uint8x8_t b, uint8x8_t c)
        ///   A32: VMLS.I8 Dd, Dn, Dm
        ///   A64: MLS Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> MultiplySubtract(Vector64<byte> acc, Vector64<byte> left, Vector64<byte> right) => MultiplySubtract(acc, left, right);

        /// <summary>
        /// int16x4_t vmls_s16 (int16x4_t a, int16x4_t b, int16x4_t c)
        ///   A32: VMLS.I16 Dd, Dn, Dm
        ///   A64: MLS Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> MultiplySubtract(Vector64<short> acc, Vector64<short> left, Vector64<short> right) => MultiplySubtract(acc, left, right);

        /// <summary>
        /// int32x2_t vmls_s32 (int32x2_t a, int32x2_t b, int32x2_t c)
        ///   A32: VMLS.I32 Dd, Dn, Dm
        ///   A64: MLS Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> MultiplySubtract(Vector64<int> acc, Vector64<int> left, Vector64<int> right) => MultiplySubtract(acc, left, right);

        /// <summary>
        /// int8x8_t vmls_s8 (int8x8_t a, int8x8_t b, int8x8_t c)
        ///   A32: VMLS.I8 Dd, Dn, Dm
        ///   A64: MLS Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> MultiplySubtract(Vector64<sbyte> acc, Vector64<sbyte> left, Vector64<sbyte> right) => MultiplySubtract(acc, left, right);

        /// <summary>
        /// uint16x4_t vmls_u16 (uint16x4_t a, uint16x4_t b, uint16x4_t c)
        ///   A32: VMLS.I16 Dd, Dn, Dm
        ///   A64: MLS Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> MultiplySubtract(Vector64<ushort> acc, Vector64<ushort> left, Vector64<ushort> right) => MultiplySubtract(acc, left, right);

        /// <summary>
        /// uint32x2_t vmls_u32 (uint32x2_t a, uint32x2_t b, uint32x2_t c)
        ///   A32: VMLS.I32 Dd, Dn, Dm
        ///   A64: MLS Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> MultiplySubtract(Vector64<uint> acc, Vector64<uint> left, Vector64<uint> right) => MultiplySubtract(acc, left, right);

        /// <summary>
        /// uint8x16_t vmlsq_u8 (uint8x16_t a, uint8x16_t b, uint8x16_t c)
        ///   A32: VMLS.I8 Qd, Qn, Qm
        ///   A64: MLS Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> MultiplySubtract(Vector128<byte> acc, Vector128<byte> left, Vector128<byte> right) => MultiplySubtract(acc, left, right);

        /// <summary>
        /// int16x8_t vmlsq_s16 (int16x8_t a, int16x8_t b, int16x8_t c)
        ///   A32: VMLS.I16 Qd, Qn, Qm
        ///   A64: MLS Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> MultiplySubtract(Vector128<short> acc, Vector128<short> left, Vector128<short> right) => MultiplySubtract(acc, left, right);

        /// <summary>
        /// int32x4_t vmlsq_s32 (int32x4_t a, int32x4_t b, int32x4_t c)
        ///   A32: VMLS.I32 Qd, Qn, Qm
        ///   A64: MLS Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> MultiplySubtract(Vector128<int> acc, Vector128<int> left, Vector128<int> right) => MultiplySubtract(acc, left, right);

        /// <summary>
        /// int8x16_t vmlsq_s8 (int8x16_t a, int8x16_t b, int8x16_t c)
        ///   A32: VMLS.I8 Qd, Qn, Qm
        ///   A64: MLS Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> MultiplySubtract(Vector128<sbyte> acc, Vector128<sbyte> left, Vector128<sbyte> right) => MultiplySubtract(acc, left, right);

        /// <summary>
        /// uint16x8_t vmlsq_u16 (uint16x8_t a, uint16x8_t b, uint16x8_t c)
        ///   A32: VMLS.I16 Qd, Qn, Qm
        ///   A64: MLS Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> MultiplySubtract(Vector128<ushort> acc, Vector128<ushort> left, Vector128<ushort> right) => MultiplySubtract(acc, left, right);

        /// <summary>
        /// uint32x4_t vmlsq_u32 (uint32x4_t a, uint32x4_t b, uint32x4_t c)
        ///   A32: VMLS.I32 Qd, Qn, Qm
        ///   A64: MLS Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> MultiplySubtract(Vector128<uint> acc, Vector128<uint> left, Vector128<uint> right) => MultiplySubtract(acc, left, right);

        /// <summary>
        /// int16x4_t vneg_s16 (int16x4_t a)
        ///   A32: VNEG.S16 Dd, Dm
        ///   A64: NEG Vd.4H, Vn.4H
        /// </summary>
        public static Vector64<short> Negate(Vector64<short> value) => Negate(value);

        /// <summary>
        /// int32x2_t vneg_s32 (int32x2_t a)
        ///   A32: VNEG.S32 Dd, Dm
        ///   A64: NEG Vd.2S, Vn.2S
        /// </summary>
        public static Vector64<int> Negate(Vector64<int> value) => Negate(value);

        /// <summary>
        /// int8x8_t vneg_s8 (int8x8_t a)
        ///   A32: VNEG.S8 Dd, Dm
        ///   A64: NEG Vd.8B, Vn.8B
        /// </summary>
        public static Vector64<sbyte> Negate(Vector64<sbyte> value) => Negate(value);

        /// <summary>
        /// float32x2_t vneg_f32 (float32x2_t a)
        ///   A32: VNEG.F32 Dd, Dm
        ///   A64: FNEG Vd.2S, Vn.2S
        /// </summary>
        public static Vector64<float> Negate(Vector64<float> value) => Negate(value);

        /// <summary>
        /// int16x8_t vnegq_s16 (int16x8_t a)
        ///   A32: VNEG.S16 Qd, Qm
        ///   A64: NEG Vd.8H, Vn.8H
        /// </summary>
        public static Vector128<short> Negate(Vector128<short> value) => Negate(value);

        /// <summary>
        /// int32x4_t vnegq_s32 (int32x4_t a)
        ///   A32: VNEG.S32 Qd, Qm
        ///   A64: NEG Vd.4S, Vn.4S
        /// </summary>
        public static Vector128<int> Negate(Vector128<int> value) => Negate(value);

        /// <summary>
        /// int8x16_t vnegq_s8 (int8x16_t a)
        ///   A32: VNEG.S8 Qd, Qm
        ///   A64: NEG Vd.16B, Vn.16B
        /// </summary>
        public static Vector128<sbyte> Negate(Vector128<sbyte> value) => Negate(value);

        /// <summary>
        /// float32x4_t vnegq_f32 (float32x4_t a)
        ///   A32: VNEG.F32 Qd, Qm
        ///   A64: FNEG Vd.4S, Vn.4S
        /// </summary>
        public static Vector128<float> Negate(Vector128<float> value) => Negate(value);

        /// <summary>
        /// float64x1_t vneg_f64 (float64x1_t a)
        ///   A32: VNEG.F64 Dd, Dm
        ///   A64: FNEG Dd, Dn
        /// </summary>
        public static Vector64<double> NegateScalar(Vector64<double> value) => NegateScalar(value);

        /// <summary>
        /// float32_t vnegs_f32 (float32_t a)
        ///   A32: VNEG.F32 Sd, Sm
        ///   A64: FNEG Sd, Sn
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> NegateScalar(Vector64<float> value) => NegateScalar(value);

        /// <summary>
        /// uint8x8_t vmvn_u8 (uint8x8_t a)
        ///   A32: VMVN Dd, Dm
        ///   A64: MVN Vd.8B, Vn.8B
        /// </summary>
        public static Vector64<byte> Not(Vector64<byte> value) => Not(value);

        /// <summary>
        /// float64x1_t vmvn_f64 (float64x1_t a)
        ///   A32: VMVN Dd, Dm
        ///   A64: MVN Vd.8B, Vn.8B
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<double> Not(Vector64<double> value) => Not(value);

        /// <summary>
        /// int16x4_t vmvn_s16 (int16x4_t a)
        ///   A32: VMVN Dd, Dm
        ///   A64: MVN Vd.8B, Vn.8B
        /// </summary>
        public static Vector64<short> Not(Vector64<short> value) => Not(value);

        /// <summary>
        /// int32x2_t vmvn_s32 (int32x2_t a)
        ///   A32: VMVN Dd, Dm
        ///   A64: MVN Vd.8B, Vn.8B
        /// </summary>
        public static Vector64<int> Not(Vector64<int> value) => Not(value);

        /// <summary>
        /// int64x1_t vmvn_s64 (int64x1_t a)
        ///   A32: VMVN Dd, Dm
        ///   A64: MVN Vd.8B, Vn.8B
        /// </summary>
        public static Vector64<long> Not(Vector64<long> value) => Not(value);

        /// <summary>
        /// int8x8_t vmvn_s8 (int8x8_t a)
        ///   A32: VMVN Dd, Dm
        ///   A64: MVN Vd.8B, Vn.8B
        /// </summary>
        public static Vector64<sbyte> Not(Vector64<sbyte> value) => Not(value);

        /// <summary>
        /// float32x2_t vmvn_f32 (float32x2_t a)
        ///   A32: VMVN Dd, Dm
        ///   A64: MVN Vd.8B, Vn.8B
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> Not(Vector64<float> value) => Not(value);

        /// <summary>
        /// uint16x4_t vmvn_u16 (uint16x4_t a)
        ///   A32: VMVN Dd, Dm
        ///   A64: MVN Vd.8B, Vn.8B
        /// </summary>
        public static Vector64<ushort> Not(Vector64<ushort> value) => Not(value);

        /// <summary>
        /// uint32x2_t vmvn_u32 (uint32x2_t a)
        ///   A32: VMVN Dd, Dm
        ///   A64: MVN Vd.8B, Vn.8B
        /// </summary>
        public static Vector64<uint> Not(Vector64<uint> value) => Not(value);

        /// <summary>
        /// uint64x1_t vmvn_u64 (uint64x1_t a)
        ///   A32: VMVN Dd, Dm
        ///   A64: MVN Vd.8B, Vn.8B
        /// </summary>
        public static Vector64<ulong> Not(Vector64<ulong> value) => Not(value);

        /// <summary>
        /// uint8x16_t vmvnq_u8 (uint8x16_t a)
        ///   A32: VMVN Qd, Qm
        ///   A64: MVN Vd.16B, Vn.16B
        /// </summary>
        public static Vector128<byte> Not(Vector128<byte> value) => Not(value);

        /// <summary>
        /// float64x2_t vmvnq_f64 (float64x2_t a)
        ///   A32: VMVN Qd, Qm
        ///   A64: MVN Vd.16B, Vn.16B
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> Not(Vector128<double> value) => Not(value);

        /// <summary>
        /// int16x8_t vmvnq_s16 (int16x8_t a)
        ///   A32: VMVN Qd, Qm
        ///   A64: MVN Vd.16B, Vn.16B
        /// </summary>
        public static Vector128<short> Not(Vector128<short> value) => Not(value);

        /// <summary>
        /// int32x4_t vmvnq_s32 (int32x4_t a)
        ///   A32: VMVN Qd, Qm
        ///   A64: MVN Vd.16B, Vn.16B
        /// </summary>
        public static Vector128<int> Not(Vector128<int> value) => Not(value);

        /// <summary>
        /// int64x2_t vmvnq_s64 (int64x2_t a)
        ///   A32: VMVN Qd, Qm
        ///   A64: MVN Vd.16B, Vn.16B
        /// </summary>
        public static Vector128<long> Not(Vector128<long> value) => Not(value);

        /// <summary>
        /// int8x16_t vmvnq_s8 (int8x16_t a)
        ///   A32: VMVN Qd, Qm
        ///   A64: MVN Vd.16B, Vn.16B
        /// </summary>
        public static Vector128<sbyte> Not(Vector128<sbyte> value) => Not(value);

        /// <summary>
        /// float32x4_t vmvnq_f32 (float32x4_t a)
        ///   A32: VMVN Qd, Qm
        ///   A64: MVN Vd.16B, Vn.16B
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> Not(Vector128<float> value) => Not(value);

        /// <summary>
        /// uint16x8_t vmvnq_u16 (uint16x8_t a)
        ///   A32: VMVN Qd, Qm
        ///   A64: MVN Vd.16B, Vn.16B
        /// </summary>
        public static Vector128<ushort> Not(Vector128<ushort> value) => Not(value);

        /// <summary>
        /// uint32x4_t vmvnq_u32 (uint32x4_t a)
        ///   A32: VMVN Qd, Qm
        ///   A64: MVN Vd.16B, Vn.16B
        /// </summary>
        public static Vector128<uint> Not(Vector128<uint> value) => Not(value);

        /// <summary>
        /// uint64x2_t vmvnq_u64 (uint64x2_t a)
        ///   A32: VMVN Qd, Qm
        ///   A64: MVN Vd.16B, Vn.16B
        /// </summary>
        public static Vector128<ulong> Not(Vector128<ulong> value) => Not(value);

        /// <summary>
        /// uint8x8_t vorr_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> Or(Vector64<byte> left, Vector64<byte> right) => Or(left, right);

        /// <summary>
        /// float64x1_t vorr_f64 (float64x1_t a, float64x1_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd.8B, Vn.8B, Vm.8B
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<double> Or(Vector64<double> left, Vector64<double> right) => Or(left, right);

        /// <summary>
        /// int16x4_t vorr_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<short> Or(Vector64<short> left, Vector64<short> right) => Or(left, right);

        /// <summary>
        /// int32x2_t vorr_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<int> Or(Vector64<int> left, Vector64<int> right) => Or(left, right);

        /// <summary>
        /// int64x1_t vorr_s64 (int64x1_t a, int64x1_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<long> Or(Vector64<long> left, Vector64<long> right) => Or(left, right);

        /// <summary>
        /// int8x8_t vorr_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> Or(Vector64<sbyte> left, Vector64<sbyte> right) => Or(left, right);

        /// <summary>
        /// float32x2_t vorr_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd.8B, Vn.8B, Vm.8B
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> Or(Vector64<float> left, Vector64<float> right) => Or(left, right);

        /// <summary>
        /// uint16x4_t vorr_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<ushort> Or(Vector64<ushort> left, Vector64<ushort> right) => Or(left, right);

        /// <summary>
        /// uint32x2_t vorr_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<uint> Or(Vector64<uint> left, Vector64<uint> right) => Or(left, right);

        /// <summary>
        /// uint64x1_t vorr_u64 (uint64x1_t a, uint64x1_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<ulong> Or(Vector64<ulong> left, Vector64<ulong> right) => Or(left, right);

        /// <summary>
        /// uint8x16_t vorrq_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VORR Qd, Qn, Qm
        ///   A64: ORR Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> Or(Vector128<byte> left, Vector128<byte> right) => Or(left, right);

        /// <summary>
        /// float64x2_t vorrq_f64 (float64x2_t a, float64x2_t b)
        ///   A32: VORR Qd, Qn, Qm
        ///   A64: ORR Vd.16B, Vn.16B, Vm.16B
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> Or(Vector128<double> left, Vector128<double> right) => Or(left, right);

        /// <summary>
        /// int16x8_t vorrq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VORR Qd, Qn, Qm
        ///   A64: ORR Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<short> Or(Vector128<short> left, Vector128<short> right) => Or(left, right);

        /// <summary>
        /// int32x4_t vorrq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VORR Qd, Qn, Qm
        ///   A64: ORR Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<int> Or(Vector128<int> left, Vector128<int> right) => Or(left, right);

        /// <summary>
        /// int64x2_t vorrq_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VORR Qd, Qn, Qm
        ///   A64: ORR Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<long> Or(Vector128<long> left, Vector128<long> right) => Or(left, right);

        /// <summary>
        /// int8x16_t vorrq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VORR Qd, Qn, Qm
        ///   A64: ORR Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> Or(Vector128<sbyte> left, Vector128<sbyte> right) => Or(left, right);

        /// <summary>
        /// float32x4_t vorrq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VORR Qd, Qn, Qm
        ///   A64: ORR Vd.16B, Vn.16B, Vm.16B
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> Or(Vector128<float> left, Vector128<float> right) => Or(left, right);

        /// <summary>
        /// uint16x8_t vorrq_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VORR Qd, Qn, Qm
        ///   A64: ORR Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<ushort> Or(Vector128<ushort> left, Vector128<ushort> right) => Or(left, right);

        /// <summary>
        /// uint32x4_t vorrq_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VORR Qd, Qn, Qm
        ///   A64: ORR Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<uint> Or(Vector128<uint> left, Vector128<uint> right) => Or(left, right);

        /// <summary>
        /// uint64x2_t vorrq_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VORR Qd, Qn, Qm
        ///   A64: ORR Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<ulong> Or(Vector128<ulong> left, Vector128<ulong> right) => Or(left, right);

        /// <summary>
        /// uint8x8_t vorn_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> OrNot(Vector64<byte> left, Vector64<byte> right) => OrNot(left, right);

        /// <summary>
        /// float64x1_t vorn_f64 (float64x1_t a, float64x1_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd.8B, Vn.8B, Vm.8B
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<double> OrNot(Vector64<double> left, Vector64<double> right) => OrNot(left, right);

        /// <summary>
        /// int16x4_t vorn_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<short> OrNot(Vector64<short> left, Vector64<short> right) => OrNot(left, right);

        /// <summary>
        /// int32x2_t vorn_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<int> OrNot(Vector64<int> left, Vector64<int> right) => OrNot(left, right);

        /// <summary>
        /// int64x1_t vorn_s64 (int64x1_t a, int64x1_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<long> OrNot(Vector64<long> left, Vector64<long> right) => OrNot(left, right);

        /// <summary>
        /// int8x8_t vorn_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> OrNot(Vector64<sbyte> left, Vector64<sbyte> right) => OrNot(left, right);

        /// <summary>
        /// float32x2_t vorn_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd.8B, Vn.8B, Vm.8B
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> OrNot(Vector64<float> left, Vector64<float> right) => OrNot(left, right);

        /// <summary>
        /// uint16x4_t vorn_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<ushort> OrNot(Vector64<ushort> left, Vector64<ushort> right) => OrNot(left, right);

        /// <summary>
        /// uint32x2_t vorn_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<uint> OrNot(Vector64<uint> left, Vector64<uint> right) => OrNot(left, right);

        /// <summary>
        /// uint64x1_t vorn_u64 (uint64x1_t a, uint64x1_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<ulong> OrNot(Vector64<ulong> left, Vector64<ulong> right) => OrNot(left, right);

        /// <summary>
        /// uint8x16_t vornq_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VORN Qd, Qn, Qm
        ///   A64: ORN Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> OrNot(Vector128<byte> left, Vector128<byte> right) => OrNot(left, right);

        /// <summary>
        /// float64x2_t vornq_f64 (float64x2_t a, float64x2_t b)
        ///   A32: VORN Qd, Qn, Qm
        ///   A64: ORN Vd.16B, Vn.16B, Vm.16B
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> OrNot(Vector128<double> left, Vector128<double> right) => OrNot(left, right);

        /// <summary>
        /// int16x8_t vornq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VORN Qd, Qn, Qm
        ///   A64: ORN Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<short> OrNot(Vector128<short> left, Vector128<short> right) => OrNot(left, right);

        /// <summary>
        /// int32x4_t vornq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VORN Qd, Qn, Qm
        ///   A64: ORN Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<int> OrNot(Vector128<int> left, Vector128<int> right) => OrNot(left, right);

        /// <summary>
        /// int64x2_t vornq_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VORN Qd, Qn, Qm
        ///   A64: ORN Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<long> OrNot(Vector128<long> left, Vector128<long> right) => OrNot(left, right);

        /// <summary>
        /// int8x16_t vornq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VORN Qd, Qn, Qm
        ///   A64: ORN Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> OrNot(Vector128<sbyte> left, Vector128<sbyte> right) => OrNot(left, right);

        /// <summary>
        /// float32x4_t vornq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VORN Qd, Qn, Qm
        ///   A64: ORN Vd.16B, Vn.16B, Vm.16B
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> OrNot(Vector128<float> left, Vector128<float> right) => OrNot(left, right);

        /// <summary>
        /// uint16x8_t vornq_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VORN Qd, Qn, Qm
        ///   A64: ORN Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<ushort> OrNot(Vector128<ushort> left, Vector128<ushort> right) => OrNot(left, right);

        /// <summary>
        /// uint32x4_t vornq_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VORN Qd, Qn, Qm
        ///   A64: ORN Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<uint> OrNot(Vector128<uint> left, Vector128<uint> right) => OrNot(left, right);

        /// <summary>
        /// uint64x2_t vornq_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VORN Qd, Qn, Qm
        ///   A64: ORN Vd.16B, Vn.16B, Vm.16B
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
        /// float64x1_t vsqrt_f64 (float64x1_t a)
        ///   A32: VSQRT.F64 Dd, Dm
        ///   A64: FSQRT Dd, Dn
        /// </summary>
        public static Vector64<double> SqrtScalar(Vector64<double> value) => SqrtScalar(value);

        /// <summary>
        /// float32_t vsqrts_f32 (float32_t a)
        ///   A32: VSQRT.F32 Sd, Sm
        ///   A64: FSQRT Sd, Sn
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> SqrtScalar(Vector64<float> value) => SqrtScalar(value);

        /// <summary>
        /// void vst1_u8 (uint8_t * ptr, uint8x8_t val)
        ///   A32: VST1.8 { Dd }, [Rn]
        ///   A64: ST1 { Vt.8B }, [Xn]
        /// </summary>
        public static unsafe void Store(byte* address, Vector64<byte> source) => Store(address, source);

        /// <summary>
        /// void vst1_f64 (float64_t * ptr, float64x1_t val)
        ///   A32: VST1.64 { Dd }, [Rn]
        ///   A64: ST1 { Vt.1D }, [Xn]
        /// </summary>
        public static unsafe void Store(double* address, Vector64<double> source) => Store(address, source);

        /// <summary>
        /// void vst1_s16 (int16_t * ptr, int16x4_t val)
        ///   A32: VST1.16 { Dd }, [Rn]
        ///   A64: ST1 {Vt.4H }, [Xn]
        /// </summary>
        public static unsafe void Store(short* address, Vector64<short> source) => Store(address, source);

        /// <summary>
        /// void vst1_s32 (int32_t * ptr, int32x2_t val)
        ///   A32: VST1.32 { Dd }, [Rn]
        ///   A64: ST1 { Vt.2S }, [Xn]
        /// </summary>
        public static unsafe void Store(int* address, Vector64<int> source) => Store(address, source);

        /// <summary>
        /// void vst1_s64 (int64_t * ptr, int64x1_t val)
        ///   A32: VST1.64 { Dd }, [Rn]
        ///   A64: ST1 { Vt.1D }, [Xn]
        /// </summary>
        public static unsafe void Store(long* address, Vector64<long> source) => Store(address, source);

        /// <summary>
        /// void vst1_s8 (int8_t * ptr, int8x8_t val)
        ///   A32: VST1.8 { Dd }, [Rn]
        ///   A64: ST1 { Vt.8B }, [Xn]
        /// </summary>
        public static unsafe void Store(sbyte* address, Vector64<sbyte> source) => Store(address, source);

        /// <summary>
        /// void vst1_f32 (float32_t * ptr, float32x2_t val)
        ///   A32: VST1.32 { Dd }, [Rn]
        ///   A64: ST1 { Vt.2S }, [Xn]
        /// </summary>
        public static unsafe void Store(float* address, Vector64<float> source) => Store(address, source);

        /// <summary>
        /// void vst1_u16 (uint16_t * ptr, uint16x4_t val)
        ///   A32: VST1.16 { Dd }, [Rn]
        ///   A64: ST1 { Vt.4H }, [Xn]
        /// </summary>
        public static unsafe void Store(ushort* address, Vector64<ushort> source) => Store(address, source);

        /// <summary>
        /// void vst1_u32 (uint32_t * ptr, uint32x2_t val)
        ///   A32: VST1.32 { Dd }, [Rn]
        ///   A64: ST1 { Vt.2S }, [Xn]
        /// </summary>
        public static unsafe void Store(uint* address, Vector64<uint> source) => Store(address, source);

        /// <summary>
        /// void vst1_u64 (uint64_t * ptr, uint64x1_t val)
        ///   A32: VST1.64 { Dd }, [Rn]
        ///   A64: ST1 { Vt.1D }, [Xn]
        /// </summary>
        public static unsafe void Store(ulong* address, Vector64<ulong> source) => Store(address, source);

        /// <summary>
        /// void vst1q_u8 (uint8_t * ptr, uint8x16_t val)
        ///   A32: VST1.8 { Dd, Dd+1 }, [Rn]
        ///   A64: ST1 { Vt.16B }, [Xn]
        /// </summary>
        public static unsafe void Store(byte* address, Vector128<byte> source) => Store(address, source);

        /// <summary>
        /// void vst1q_f64 (float64_t * ptr, float64x2_t val)
        ///   A32: VST1.64 { Dd, Dd+1 }, [Rn]
        ///   A64: ST1 { Vt.2D }, [Xn]
        /// </summary>
        public static unsafe void Store(double* address, Vector128<double> source) => Store(address, source);

        /// <summary>
        /// void vst1q_s16 (int16_t * ptr, int16x8_t val)
        ///   A32: VST1.16 { Dd, Dd+1 }, [Rn]
        ///   A64: ST1 { Vt.8H }, [Xn]
        /// </summary>
        public static unsafe void Store(short* address, Vector128<short> source) => Store(address, source);

        /// <summary>
        /// void vst1q_s32 (int32_t * ptr, int32x4_t val)
        ///   A32: VST1.32 { Dd, Dd+1 }, [Rn]
        ///   A64: ST1 { Vt.4S }, [Xn]
        /// </summary>
        public static unsafe void Store(int* address, Vector128<int> source) => Store(address, source);

        /// <summary>
        /// void vst1q_s64 (int64_t * ptr, int64x2_t val)
        ///   A32: VST1.64 { Dd, Dd+1 }, [Rn]
        ///   A64: ST1 { Vt.2D }, [Xn]
        /// </summary>
        public static unsafe void Store(long* address, Vector128<long> source) => Store(address, source);

        /// <summary>
        /// void vst1q_s8 (int8_t * ptr, int8x16_t val)
        ///   A32: VST1.8 { Dd, Dd+1 }, [Rn]
        ///   A64: ST1 { Vt.16B }, [Xn]
        /// </summary>
        public static unsafe void Store(sbyte* address, Vector128<sbyte> source) => Store(address, source);

        /// <summary>
        /// void vst1q_f32 (float32_t * ptr, float32x4_t val)
        ///   A32: VST1.32 { Dd, Dd+1 }, [Rn]
        ///   A64: ST1 { Vt.4S }, [Xn]
        /// </summary>
        public static unsafe void Store(float* address, Vector128<float> source) => Store(address, source);

        /// <summary>
        /// void vst1q_u16 (uint16_t * ptr, uint16x8_t val)
        ///   A32: VST1.16 { Dd, Dd+1 }, [Rn]
        ///   A64: ST1 { Vt.8H }, [Xn]
        /// </summary>
        public static unsafe void Store(ushort* address, Vector128<ushort> source) => Store(address, source);

        /// <summary>
        /// void vst1q_u32 (uint32_t * ptr, uint32x4_t val)
        ///   A32: VST1.32 { Dd, Dd+1 }, [Rn]
        ///   A64: ST1 { Vt.4S }, [Xn]
        /// </summary>
        public static unsafe void Store(uint* address, Vector128<uint> source) => Store(address, source);

        /// <summary>
        /// void vst1q_u64 (uint64_t * ptr, uint64x2_t val)
        ///   A32: VST1.64 { Dd, Dd+1 }, [Rn]
        ///   A64: ST1 { Vt.2D }, [Xn]
        /// </summary>
        public static unsafe void Store(ulong* address, Vector128<ulong> source) => Store(address, source);

        /// <summary>
        /// uint8x8_t vsub_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VSUB.I8 Dd, Dn, Dm
        ///   A64: SUB Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> Subtract(Vector64<byte> left, Vector64<byte> right) => Subtract(left, right);

        /// <summary>
        /// int16x4_t vsub_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VSUB.I16 Dd, Dn, Dm
        ///   A64: SUB Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> Subtract(Vector64<short> left, Vector64<short> right) => Subtract(left, right);

        /// <summary>
        /// int32x2_t vsub_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VSUB.I32 Dd, Dn, Dm
        ///   A64: SUB Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> Subtract(Vector64<int> left, Vector64<int> right) => Subtract(left, right);

        /// <summary>
        /// int8x8_t vsub_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VSUB.I8 Dd, Dn, Dm
        ///   A64: SUB Vd.8B, Vn.8B, Vm.8B
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
        ///   A64: SUB Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> Subtract(Vector64<ushort> left, Vector64<ushort> right) => Subtract(left, right);

        /// <summary>
        /// uint32x2_t vsub_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VSUB.I32 Dd, Dn, Dm
        ///   A64: SUB Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> Subtract(Vector64<uint> left, Vector64<uint> right) => Subtract(left, right);

        /// <summary>
        /// uint8x16_t vsubq_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VSUB.I8 Qd, Qn, Qm
        ///   A64: SUB Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> Subtract(Vector128<byte> left, Vector128<byte> right) => Subtract(left, right);

        /// <summary>
        /// int16x8_t vsubq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VSUB.I16 Qd, Qn, Qm
        ///   A64: SUB Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> Subtract(Vector128<short> left, Vector128<short> right) => Subtract(left, right);

        /// <summary>
        /// int32x4_t vsubq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VSUB.I32 Qd, Qn, Qm
        ///   A64: SUB Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> Subtract(Vector128<int> left, Vector128<int> right) => Subtract(left, right);

        /// <summary>
        /// int64x2_t vsubq_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VSUB.I64 Qd, Qn, Qm
        ///   A64: SUB Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<long> Subtract(Vector128<long> left, Vector128<long> right) => Subtract(left, right);

        /// <summary>
        /// int8x16_t vsubq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VSUB.I8 Qd, Qn, Qm
        ///   A64: SUB Vd.16B, Vn.16B, Vm.16B
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
        ///   A64: SUB Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> Subtract(Vector128<ushort> left, Vector128<ushort> right) => Subtract(left, right);

        /// <summary>
        /// uint32x4_t vsubq_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VSUB.I32 Qd, Qn, Qm
        ///   A64: SUB Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> Subtract(Vector128<uint> left, Vector128<uint> right) => Subtract(left, right);

        /// <summary>
        /// uint64x2_t vsubq_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VSUB.I64 Qd, Qn, Qm
        ///   A64: SUB Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<ulong> Subtract(Vector128<ulong> left, Vector128<ulong> right) => Subtract(left, right);

        /// <summary>
        /// float64x1_t vsub_f64 (float64x1_t a, float64x1_t b)
        ///   A32: VSUB.F64 Dd, Dn, Dm
        ///   A64: FSUB Dd, Dn, Dm
        /// </summary>
        public static Vector64<double> SubtractScalar(Vector64<double> left, Vector64<double> right) => SubtractScalar(left, right);

        /// <summary>
        /// int64x1_t vsub_s64 (int64x1_t a, int64x1_t b)
        ///   A32: VSUB.I64 Dd, Dn, Dm
        ///   A64: SUB Dd, Dn, Dm
        /// </summary>
        public static Vector64<long> SubtractScalar(Vector64<long> left, Vector64<long> right) => SubtractScalar(left, right);

        /// <summary>
        /// float32_t vsubs_f32 (float32_t a, float32_t b)
        ///   A32: VSUB.F32 Sd, Sn, Sm
        ///   A64: FSUB Sd, Sn, Sm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> SubtractScalar(Vector64<float> left, Vector64<float> right) => SubtractScalar(left, right);

        /// <summary>
        /// uint64x1_t vsub_u64 (uint64x1_t a, uint64x1_t b)
        ///   A32: VSUB.I64 Dd, Dn, Dm
        ///   A64: SUB Dd, Dn, Dm
        /// </summary>
        public static Vector64<ulong> SubtractScalar(Vector64<ulong> left, Vector64<ulong> right) => SubtractScalar(left, right);

        /// <summary>
        /// uint8x8_t veor_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> Xor(Vector64<byte> left, Vector64<byte> right) => Xor(left, right);

        /// <summary>
        /// float64x1_t veor_f64 (float64x1_t a, float64x1_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd.8B, Vn.8B, Vm.8B
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<double> Xor(Vector64<double> left, Vector64<double> right) => Xor(left, right);

        /// <summary>
        /// int16x4_t veor_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<short> Xor(Vector64<short> left, Vector64<short> right) => Xor(left, right);

        /// <summary>
        /// int32x2_t veor_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<int> Xor(Vector64<int> left, Vector64<int> right) => Xor(left, right);

        /// <summary>
        /// int64x1_t veor_s64 (int64x1_t a, int64x1_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<long> Xor(Vector64<long> left, Vector64<long> right) => Xor(left, right);

        /// <summary>
        /// int8x8_t veor_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> Xor(Vector64<sbyte> left, Vector64<sbyte> right) => Xor(left, right);

        /// <summary>
        /// float32x2_t veor_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd.8B, Vn.8B, Vm.8B
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> Xor(Vector64<float> left, Vector64<float> right) => Xor(left, right);

        /// <summary>
        /// uint16x4_t veor_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<ushort> Xor(Vector64<ushort> left, Vector64<ushort> right) => Xor(left, right);

        /// <summary>
        /// uint32x2_t veor_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<uint> Xor(Vector64<uint> left, Vector64<uint> right) => Xor(left, right);

        /// <summary>
        /// uint64x1_t veor_u64 (uint64x1_t a, uint64x1_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<ulong> Xor(Vector64<ulong> left, Vector64<ulong> right) => Xor(left, right);

        /// <summary>
        /// uint8x16_t veorq_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VEOR Qd, Qn, Qm
        ///   A64: EOR Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> Xor(Vector128<byte> left, Vector128<byte> right) => Xor(left, right);

        /// <summary>
        /// float64x2_t veorq_f64 (float64x2_t a, float64x2_t b)
        ///   A32: VEOR Qd, Qn, Qm
        ///   A64: EOR Vd.16B, Vn.16B, Vm.16B
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> Xor(Vector128<double> left, Vector128<double> right) => Xor(left, right);

        /// <summary>
        /// int16x8_t veorq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VEOR Qd, Qn, Qm
        ///   A64: EOR Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<short> Xor(Vector128<short> left, Vector128<short> right) => Xor(left, right);

        /// <summary>
        /// int32x4_t veorq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VEOR Qd, Qn, Qm
        ///   A64: EOR Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<int> Xor(Vector128<int> left, Vector128<int> right) => Xor(left, right);

        /// <summary>
        /// int64x2_t veorq_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VEOR Qd, Qn, Qm
        ///   A64: EOR Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<long> Xor(Vector128<long> left, Vector128<long> right) => Xor(left, right);

        /// <summary>
        /// int8x16_t veorq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VEOR Qd, Qn, Qm
        ///   A64: EOR Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> Xor(Vector128<sbyte> left, Vector128<sbyte> right) => Xor(left, right);

        /// <summary>
        /// float32x4_t veorq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VEOR Qd, Qn, Qm
        ///   A64: EOR Vd.16B, Vn.16B, Vm.16B
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> Xor(Vector128<float> left, Vector128<float> right) => Xor(left, right);

        /// <summary>
        /// uint16x8_t veorq_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VEOR Qd, Qn, Qm
        ///   A64: EOR Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<ushort> Xor(Vector128<ushort> left, Vector128<ushort> right) => Xor(left, right);

        /// <summary>
        /// uint32x4_t veorq_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VEOR Qd, Qn, Qm
        ///   A64: EOR Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<uint> Xor(Vector128<uint> left, Vector128<uint> right) => Xor(left, right);

        /// <summary>
        /// uint64x2_t veorq_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VEOR Qd, Qn, Qm
        ///   A64: EOR Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<ulong> Xor(Vector128<ulong> left, Vector128<ulong> right) => Xor(left, right);
    }
}
