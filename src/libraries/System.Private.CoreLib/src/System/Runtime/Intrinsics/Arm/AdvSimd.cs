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
            /// uint8_t vqaddb_u8 (uint8_t a, uint8_t b)
            ///   A64: UQADD Bd, Bn, Bm
            /// </summary>
            public static Vector64<byte> AddSaturateScalar(Vector64<byte> left, Vector64<byte> right) => AddSaturateScalar(left, right);

            /// <summary>
            /// int16_t vqaddh_s16 (int16_t a, int16_t b)
            ///   A64: SQADD Hd, Hn, Hm
            /// </summary>
            public static Vector64<short> AddSaturateScalar(Vector64<short> left, Vector64<short> right) => AddSaturateScalar(left, right);

            /// <summary>
            /// int32_t vqadds_s32 (int32_t a, int32_t b)
            ///   A64: SQADD Sd, Sn, Sm
            /// </summary>
            public static Vector64<int> AddSaturateScalar(Vector64<int> left, Vector64<int> right) => AddSaturateScalar(left, right);

            /// <summary>
            /// int8_t vqaddb_s8 (int8_t a, int8_t b)
            ///   A64: SQADD Bd, Bn, Bm
            /// </summary>
            public static Vector64<sbyte> AddSaturateScalar(Vector64<sbyte> left, Vector64<sbyte> right) => AddSaturateScalar(left, right);

            /// <summary>
            /// uint16_t vqaddh_u16 (uint16_t a, uint16_t b)
            ///   A64: UQADD Hd, Hn, Hm
            /// </summary>
            public static Vector64<ushort> AddSaturateScalar(Vector64<ushort> left, Vector64<ushort> right) => AddSaturateScalar(left, right);

            /// <summary>
            /// uint32_t vqadds_u32 (uint32_t a, uint32_t b)
            ///   A64: UQADD Sd, Sn, Sm
            /// </summary>
            public static Vector64<uint> AddSaturateScalar(Vector64<uint> left, Vector64<uint> right) => AddSaturateScalar(left, right);

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
            /// float64x2_t vdupq_laneq_f64 (float64x2_t vec, const int lane)
            ///   A64: DUP Vd.2D, Vn.D[index]
            /// </summary>
            public static System.Runtime.Intrinsics.Vector128<double> DuplicateSelectedScalarToVector128(System.Runtime.Intrinsics.Vector128<double> value, byte index) => DuplicateSelectedScalarToVector128(value, index);

            /// <summary>
            /// int64x2_t vdupq_laneq_s64 (int64x2_t vec, const int lane)
            ///   A64: DUP Vd.2D, Vn.D[index]
            /// </summary>
            public static System.Runtime.Intrinsics.Vector128<long> DuplicateSelectedScalarToVector128(System.Runtime.Intrinsics.Vector128<long> value, byte index) => DuplicateSelectedScalarToVector128(value, index);

            /// <summary>
            /// uint64x2_t vdupq_laneq_u64 (uint64x2_t vec, const int lane)
            ///   A64: DUP Vd.2D, Vn.D[index]
            /// </summary>
            public static System.Runtime.Intrinsics.Vector128<ulong> DuplicateSelectedScalarToVector128(System.Runtime.Intrinsics.Vector128<ulong> value, byte index) => DuplicateSelectedScalarToVector128(value, index);

            /// <summary>
            /// float64x2_t vdupq_n_f64 (float64_t value)
            ///   A64: DUP Vd.2D, Vn.D[0]
            /// </summary>
            public static Vector128<double> DuplicateToVector128(double value) => DuplicateToVector128(value);

            /// <summary>
            /// int64x2_t vdupq_n_s64 (int64_t value)
            ///   A64: DUP Vd.2D, Rn
            /// </summary>
            public static Vector128<long> DuplicateToVector128(long value) => DuplicateToVector128(value);

            /// <summary>
            /// uint64x2_t vdupq_n_s64 (uint64_t value)
            ///   A64: DUP Vd.2D, Rn
            /// </summary>
            public static Vector128<ulong> DuplicateToVector128(ulong value) => DuplicateToVector128(value);

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
            /// float32x2_t vmulx_f32 (float32x2_t a, float32x2_t b)
            ///   A64: FMULX Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<float> MultiplyExtended(Vector64<float> left, Vector64<float> right) => MultiplyExtended(left, right);

            /// <summary>
            /// float64x2_t vmulxq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FMULX Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> MultiplyExtended(Vector128<double> left, Vector128<double> right) => MultiplyExtended(left, right);

            /// <summary>
            /// float32x4_t vmulxq_f32 (float32x4_t a, float32x4_t b)
            ///   A64: FMULX Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<float> MultiplyExtended(Vector128<float> left, Vector128<float> right) => MultiplyExtended(left, right);

            /// <summary>
            /// float64x1_t vmulx_f64 (float64x1_t a, float64x1_t b)
            ///   A64: FMULX Dd, Dn, Dm
            /// </summary>
            public static Vector64<double> MultiplyExtendedScalar(Vector64<double> left, Vector64<double> right) => MultiplyExtendedScalar(left, right);

            /// <summary>
            /// float32_t vmulxs_f32 (float32_t a, float32_t b)
            ///   A64: FMULX Sd, Sn, Sm
            /// </summary>
            public static Vector64<float> MultiplyExtendedScalar(Vector64<float> left, Vector64<float> right) => MultiplyExtendedScalar(left, right);

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
            /// float64x2_t vrecpeq_f64 (float64x2_t a)
            ///   A64: FRECPE Vd.2D, Vn.2D
            /// </summary>
            public static Vector128<double> ReciprocalEstimate(Vector128<double> value) => ReciprocalEstimate(value);

            /// <summary>
            /// float64x1_t vrecpe_f64 (float64x1_t a)
            ///   A64: FRECPE Dd, Dn
            /// </summary>
            public static Vector64<double> ReciprocalEstimateScalar(Vector64<double> value) => ReciprocalEstimateScalar(value);

            /// <summary>
            /// float32_t vrecpes_f32 (float32_t a)
            ///   A64: FRECPE Sd, Sn
            /// </summary>
            public static Vector64<float> ReciprocalEstimateScalar(Vector64<float> value) => ReciprocalEstimateScalar(value);

            /// <summary>
            /// float64_t vrecpxd_f64 (float64_t a)
            ///   A64: FRECPX Dd, Dn
            /// </summary>
            public static Vector64<double> ReciprocalExponentScalar(Vector64<double> value) => ReciprocalExponentScalar(value);

            /// <summary>
            /// float32_t vrecpxs_f32 (float32_t a)
            ///   A64: FRECPX Sd, Sn
            /// </summary>
            public static Vector64<float> ReciprocalExponentScalar(Vector64<float> value) => ReciprocalExponentScalar(value);

            /// <summary>
            /// float64x2_t vrsqrteq_f64 (float64x2_t a)
            ///   A64: FRSQRTE Vd.2D, Vn.2D
            /// </summary>
            public static Vector128<double> ReciprocalSquareRootEstimate(Vector128<double> value) => ReciprocalSquareRootEstimate(value);

            /// <summary>
            /// float64x1_t vrsqrte_f64 (float64x1_t a)
            ///   A64: FRSQRTE Dd, Dn
            /// </summary>
            public static Vector64<double> ReciprocalSquareRootEstimateScalar(Vector64<double> value) => ReciprocalSquareRootEstimateScalar(value);

            /// <summary>
            /// float32_t vrsqrtes_f32 (float32_t a)
            ///   A64: FRSQRTE Sd, Sn
            /// </summary>
            public static Vector64<float> ReciprocalSquareRootEstimateScalar(Vector64<float> value) => ReciprocalSquareRootEstimateScalar(value);

            /// <summary>
            /// float64x2_t vrsqrtsq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FRSQRTS Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> ReciprocalSquareRootStep(Vector128<double> left, Vector128<double> right) => ReciprocalSquareRootStep(left, right);

            /// <summary>
            /// float64x1_t vrsqrts_f64 (float64x1_t a, float64x1_t b)
            ///   A64: FRSQRTS Dd, Dn, Dm
            /// </summary>
            public static Vector64<double> ReciprocalSquareRootStepScalar(Vector64<double> left, Vector64<double> right) => ReciprocalSquareRootStepScalar(left, right);

            /// <summary>
            /// float32_t vrsqrtss_f32 (float32_t a, float32_t b)
            ///   A64: FRSQRTS Sd, Sn, Sm
            /// </summary>
            public static Vector64<float> ReciprocalSquareRootStepScalar(Vector64<float> left, Vector64<float> right) => ReciprocalSquareRootStepScalar(left, right);

            /// <summary>
            /// float64x2_t vrecpsq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FRECPS Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> ReciprocalStep(Vector128<double> left, Vector128<double> right) => ReciprocalStep(left, right);

            /// <summary>
            /// float64x1_t vrecps_f64 (float64x1_t a, float64x1_t b)
            ///   A64: FRECPS Dd, Dn, Dm
            /// </summary>
            public static Vector64<double> ReciprocalStepScalar(Vector64<double> left, Vector64<double> right) => ReciprocalStepScalar(left, right);

            /// <summary>
            /// float32_t vrecpss_f32 (float32_t a, float32_t b)
            ///   A64: FRECPS Sd, Sn, Sm
            /// </summary>
            public static Vector64<float> ReciprocalStepScalar(Vector64<float> left, Vector64<float> right) => ReciprocalStepScalar(left, right);

            /// <summary>
            /// int16_t vqrshlh_s16 (int16_t a, int16_t b)
            ///   A64: SQRSHL Hd, Hn, Hm
            /// </summary>
            public static Vector64<short> ShiftArithmeticRoundedSaturateScalar(Vector64<short> value, Vector64<short> count) => ShiftArithmeticRoundedSaturateScalar(value, count);

            /// <summary>
            /// int32_t vqrshls_s32 (int32_t a, int32_t b)
            ///   A64: SQRSHL Sd, Sn, Sm
            /// </summary>
            public static Vector64<int> ShiftArithmeticRoundedSaturateScalar(Vector64<int> value, Vector64<int> count) => ShiftArithmeticRoundedSaturateScalar(value, count);

            /// <summary>
            /// int8_t vqrshlb_s8 (int8_t a, int8_t b)
            ///   A64: SQRSHL Bd, Bn, Bm
            /// </summary>
            public static Vector64<sbyte> ShiftArithmeticRoundedSaturateScalar(Vector64<sbyte> value, Vector64<sbyte> count) => ShiftArithmeticRoundedSaturateScalar(value, count);

            /// <summary>
            /// int16_t vqshlh_s16 (int16_t a, int16_t b)
            ///   A64: SQSHL Hd, Hn, Hm
            /// </summary>
            public static Vector64<short> ShiftArithmeticSaturateScalar(Vector64<short> value, Vector64<short> count) => ShiftArithmeticSaturateScalar(value, count);

            /// <summary>
            /// int32_t vqshls_s32 (int32_t a, int32_t b)
            ///   A64: SQSHL Sd, Sn, Sm
            /// </summary>
            public static Vector64<int> ShiftArithmeticSaturateScalar(Vector64<int> value, Vector64<int> count) => ShiftArithmeticSaturateScalar(value, count);

            /// <summary>
            /// int8_t vqshlb_s8 (int8_t a, int8_t b)
            ///   A64: SQSHL Bd, Bn, Bm
            /// </summary>
            public static Vector64<sbyte> ShiftArithmeticSaturateScalar(Vector64<sbyte> value, Vector64<sbyte> count) => ShiftArithmeticSaturateScalar(value, count);

            /// <summary>
            /// uint8_t vqshlb_n_u8 (uint8_t a, const int n)
            ///   A64: UQSHL Bd, Bn, #n
            /// </summary>
            public static Vector64<byte> ShiftLeftLogicalSaturateScalar(Vector64<byte> value, byte count) => ShiftLeftLogicalSaturateScalar(value, count);

            /// <summary>
            /// int16_t vqshlh_n_s16 (int16_t a, const int n)
            ///   A64: SQSHL Hd, Hn, #n
            /// </summary>
            public static Vector64<short> ShiftLeftLogicalSaturateScalar(Vector64<short> value, byte count) => ShiftLeftLogicalSaturateScalar(value, count);

            /// <summary>
            /// int32_t vqshls_n_s32 (int32_t a, const int n)
            ///   A64: SQSHL Sd, Sn, #n
            /// </summary>
            public static Vector64<int> ShiftLeftLogicalSaturateScalar(Vector64<int> value, byte count) => ShiftLeftLogicalSaturateScalar(value, count);

            /// <summary>
            /// int8_t vqshlb_n_s8 (int8_t a, const int n)
            ///   A64: SQSHL Bd, Bn, #n
            /// </summary>
            public static Vector64<sbyte> ShiftLeftLogicalSaturateScalar(Vector64<sbyte> value, byte count) => ShiftLeftLogicalSaturateScalar(value, count);

            /// <summary>
            /// uint16_t vqshlh_n_u16 (uint16_t a, const int n)
            ///   A64: UQSHL Hd, Hn, #n
            /// </summary>
            public static Vector64<ushort> ShiftLeftLogicalSaturateScalar(Vector64<ushort> value, byte count) => ShiftLeftLogicalSaturateScalar(value, count);

            /// <summary>
            /// uint32_t vqshls_n_u32 (uint32_t a, const int n)
            ///   A64: UQSHL Sd, Sn, #n
            /// </summary>
            public static Vector64<uint> ShiftLeftLogicalSaturateScalar(Vector64<uint> value, byte count) => ShiftLeftLogicalSaturateScalar(value, count);

            /// <summary>
            /// uint16_t vqshluh_n_s16 (int16_t a, const int n)
            ///   A64: SQSHLU Hd, Hn, #n
            /// </summary>
            public static Vector64<ushort> ShiftLeftLogicalSaturateUnsignedScalar(Vector64<short> value, byte count) => ShiftLeftLogicalSaturateUnsignedScalar(value, count);

            /// <summary>
            /// uint32_t vqshlus_n_s32 (int32_t a, const int n)
            ///   A64: SQSHLU Sd, Sn, #n
            /// </summary>
            public static Vector64<uint> ShiftLeftLogicalSaturateUnsignedScalar(Vector64<int> value, byte count) => ShiftLeftLogicalSaturateUnsignedScalar(value, count);

            /// <summary>
            /// uint8_t vqshlub_n_s8 (int8_t a, const int n)
            ///   A64: SQSHLU Bd, Bn, #n
            /// </summary>
            public static Vector64<byte> ShiftLeftLogicalSaturateUnsignedScalar(Vector64<sbyte> value, byte count) => ShiftLeftLogicalSaturateUnsignedScalar(value, count);

            /// <summary>
            /// uint8_t vqrshlb_u8 (uint8_t a, int8_t b)
            ///   A64: UQRSHL Bd, Bn, Bm
            /// </summary>
            public static Vector64<byte> ShiftLogicalRoundedSaturateScalar(Vector64<byte> value, Vector64<sbyte> count) => ShiftLogicalRoundedSaturateScalar(value, count);

            /// <summary>
            /// uint16_t vqrshlh_u16 (uint16_t a, int16_t b)
            ///   A64: UQRSHL Hd, Hn, Hm
            /// </summary>
            public static Vector64<short> ShiftLogicalRoundedSaturateScalar(Vector64<short> value, Vector64<short> count) => ShiftLogicalRoundedSaturateScalar(value, count);

            /// <summary>
            /// uint32_t vqrshls_u32 (uint32_t a, int32_t b)
            ///   A64: UQRSHL Sd, Sn, Sm
            /// </summary>
            public static Vector64<int> ShiftLogicalRoundedSaturateScalar(Vector64<int> value, Vector64<int> count) => ShiftLogicalRoundedSaturateScalar(value, count);

            /// <summary>
            /// uint8_t vqrshlb_u8 (uint8_t a, int8_t b)
            ///   A64: UQRSHL Bd, Bn, Bm
            /// </summary>
            public static Vector64<sbyte> ShiftLogicalRoundedSaturateScalar(Vector64<sbyte> value, Vector64<sbyte> count) => ShiftLogicalRoundedSaturateScalar(value, count);

            /// <summary>
            /// uint16_t vqrshlh_u16 (uint16_t a, int16_t b)
            ///   A64: UQRSHL Hd, Hn, Hm
            /// </summary>
            public static Vector64<ushort> ShiftLogicalRoundedSaturateScalar(Vector64<ushort> value, Vector64<short> count) => ShiftLogicalRoundedSaturateScalar(value, count);

            /// <summary>
            /// uint32_t vqrshls_u32 (uint32_t a, int32_t b)
            ///   A64: UQRSHL Sd, Sn, Sm
            /// </summary>
            public static Vector64<uint> ShiftLogicalRoundedSaturateScalar(Vector64<uint> value, Vector64<int> count) => ShiftLogicalRoundedSaturateScalar(value, count);

            /// <summary>
            /// uint8_t vqshlb_u8 (uint8_t a, int8_t b)
            ///   A64: UQSHL Bd, Bn, Bm
            /// </summary>
            public static Vector64<byte> ShiftLogicalSaturateScalar(Vector64<byte> value, Vector64<sbyte> count) => ShiftLogicalSaturateScalar(value, count);

            /// <summary>
            /// uint16_t vqshlh_u16 (uint16_t a, int16_t b)
            ///   A64: UQSHL Hd, Hn, Hm
            /// </summary>
            public static Vector64<short> ShiftLogicalSaturateScalar(Vector64<short> value, Vector64<short> count) => ShiftLogicalSaturateScalar(value, count);

            /// <summary>
            /// uint32_t vqshls_u32 (uint32_t a, int32_t b)
            ///   A64: UQSHL Sd, Sn, Sm
            /// </summary>
            public static Vector64<int> ShiftLogicalSaturateScalar(Vector64<int> value, Vector64<int> count) => ShiftLogicalSaturateScalar(value, count);

            /// <summary>
            /// uint8_t vqshlb_u8 (uint8_t a, int8_t b)
            ///   A64: UQSHL Bd, Bn, Bm
            /// </summary>
            public static Vector64<sbyte> ShiftLogicalSaturateScalar(Vector64<sbyte> value, Vector64<sbyte> count) => ShiftLogicalSaturateScalar(value, count);

            /// <summary>
            /// uint16_t vqshlh_u16 (uint16_t a, int16_t b)
            ///   A64: UQSHL Hd, Hn, Hm
            /// </summary>
            public static Vector64<ushort> ShiftLogicalSaturateScalar(Vector64<ushort> value, Vector64<short> count) => ShiftLogicalSaturateScalar(value, count);

            /// <summary>
            /// uint32_t vqshls_u32 (uint32_t a, int32_t b)
            ///   A64: UQSHL Sd, Sn, Sm
            /// </summary>
            public static Vector64<uint> ShiftLogicalSaturateScalar(Vector64<uint> value, Vector64<int> count) => ShiftLogicalSaturateScalar(value, count);

            /// <summary>
            /// int16_t vqshrns_n_s32 (int32_t a, const int n)
            ///   A64: SQSHRN Hd, Sn, #n
            /// </summary>
            public static Vector64<short> ShiftRightArithmeticNarrowingSaturateScalar(Vector64<int> value, byte count) => ShiftRightArithmeticNarrowingSaturateScalar(value, count);

            /// <summary>
            /// int32_t vqshrnd_n_s64 (int64_t a, const int n)
            ///   A64: SQSHRN Sd, Dn, #n
            /// </summary>
            public static Vector64<int> ShiftRightArithmeticNarrowingSaturateScalar(Vector64<long> value, byte count) => ShiftRightArithmeticNarrowingSaturateScalar(value, count);

            /// <summary>
            /// int8_t vqshrnh_n_s16 (int16_t a, const int n)
            ///   A64: SQSHRN Bd, Hn, #n
            /// </summary>
            public static Vector64<sbyte> ShiftRightArithmeticNarrowingSaturateScalar(Vector64<short> value, byte count) => ShiftRightArithmeticNarrowingSaturateScalar(value, count);

            /// <summary>
            /// uint8_t vqshrunh_n_s16 (int16_t a, const int n)
            ///   A64: SQSHRUN Bd, Hn, #n
            /// </summary>
            public static Vector64<byte> ShiftRightArithmeticNarrowingSaturateUnsignedScalar(Vector64<short> value, byte count) => ShiftRightArithmeticNarrowingSaturateUnsignedScalar(value, count);

            /// <summary>
            /// uint16_t vqshruns_n_s32 (int32_t a, const int n)
            ///   A64: SQSHRUN Hd, Sn, #n
            /// </summary>
            public static Vector64<ushort> ShiftRightArithmeticNarrowingSaturateUnsignedScalar(Vector64<int> value, byte count) => ShiftRightArithmeticNarrowingSaturateUnsignedScalar(value, count);

            /// <summary>
            /// uint32_t vqshrund_n_s64 (int64_t a, const int n)
            ///   A64: SQSHRUN Sd, Dn, #n
            /// </summary>
            public static Vector64<uint> ShiftRightArithmeticNarrowingSaturateUnsignedScalar(Vector64<long> value, byte count) => ShiftRightArithmeticNarrowingSaturateUnsignedScalar(value, count);

            /// <summary>
            /// int16_t vqrshrns_n_s32 (int32_t a, const int n)
            ///   A64: SQRSHRN Hd, Sn, #n
            /// </summary>
            public static Vector64<short> ShiftRightArithmeticRoundedNarrowingSaturateScalar(Vector64<int> value, byte count) => ShiftRightArithmeticRoundedNarrowingSaturateScalar(value, count);

            /// <summary>
            /// int32_t vqrshrnd_n_s64 (int64_t a, const int n)
            ///   A64: SQRSHRN Sd, Dn, #n
            /// </summary>
            public static Vector64<int> ShiftRightArithmeticRoundedNarrowingSaturateScalar(Vector64<long> value, byte count) => ShiftRightArithmeticRoundedNarrowingSaturateScalar(value, count);

            /// <summary>
            /// int8_t vqrshrnh_n_s16 (int16_t a, const int n)
            ///   A64: SQRSHRN Bd, Hn, #n
            /// </summary>
            public static Vector64<sbyte> ShiftRightArithmeticRoundedNarrowingSaturateScalar(Vector64<short> value, byte count) => ShiftRightArithmeticRoundedNarrowingSaturateScalar(value, count);

            /// <summary>
            /// uint8_t vqrshrunh_n_s16 (int16_t a, const int n)
            ///   A64: SQRSHRUN Bd, Hn, #n
            /// </summary>
            public static Vector64<byte> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedScalar(Vector64<short> value, byte count) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedScalar(value, count);

            /// <summary>
            /// uint16_t vqrshruns_n_s32 (int32_t a, const int n)
            ///   A64: SQRSHRUN Hd, Sn, #n
            /// </summary>
            public static Vector64<ushort> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedScalar(Vector64<int> value, byte count) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedScalar(value, count);

            /// <summary>
            /// uint32_t vqrshrund_n_s64 (int64_t a, const int n)
            ///   A64: SQRSHRUN Sd, Dn, #n
            /// </summary>
            public static Vector64<uint> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedScalar(Vector64<long> value, byte count) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedScalar(value, count);

            /// <summary>
            /// uint8_t vqshrnh_n_u16 (uint16_t a, const int n)
            ///   A64: UQSHRN Bd, Hn, #n
            /// </summary>
            public static Vector64<byte> ShiftRightLogicalNarrowingSaturateScalar(Vector64<ushort> value, byte count) => ShiftRightLogicalNarrowingSaturateScalar(value, count);

            /// <summary>
            /// uint16_t vqshrns_n_u32 (uint32_t a, const int n)
            ///   A64: UQSHRN Hd, Sn, #n
            /// </summary>
            public static Vector64<short> ShiftRightLogicalNarrowingSaturateScalar(Vector64<int> value, byte count) => ShiftRightLogicalNarrowingSaturateScalar(value, count);

            /// <summary>
            /// uint32_t vqshrnd_n_u64 (uint64_t a, const int n)
            ///   A64: UQSHRN Sd, Dn, #n
            /// </summary>
            public static Vector64<int> ShiftRightLogicalNarrowingSaturateScalar(Vector64<long> value, byte count) => ShiftRightLogicalNarrowingSaturateScalar(value, count);

            /// <summary>
            /// uint8_t vqshrnh_n_u16 (uint16_t a, const int n)
            ///   A64: UQSHRN Bd, Hn, #n
            /// </summary>
            public static Vector64<sbyte> ShiftRightLogicalNarrowingSaturateScalar(Vector64<short> value, byte count) => ShiftRightLogicalNarrowingSaturateScalar(value, count);

            /// <summary>
            /// uint16_t vqshrns_n_u32 (uint32_t a, const int n)
            ///   A64: UQSHRN Hd, Sn, #n
            /// </summary>
            public static Vector64<ushort> ShiftRightLogicalNarrowingSaturateScalar(Vector64<uint> value, byte count) => ShiftRightLogicalNarrowingSaturateScalar(value, count);

            /// <summary>
            /// uint32_t vqshrnd_n_u64 (uint64_t a, const int n)
            ///   A64: UQSHRN Sd, Dn, #n
            /// </summary>
            public static Vector64<uint> ShiftRightLogicalNarrowingSaturateScalar(Vector64<ulong> value, byte count) => ShiftRightLogicalNarrowingSaturateScalar(value, count);

            /// <summary>
            /// uint8_t vqrshrnh_n_u16 (uint16_t a, const int n)
            ///   A64: UQRSHRN Bd, Hn, #n
            /// </summary>
            public static Vector64<byte> ShiftRightLogicalRoundedNarrowingSaturateScalar(Vector64<ushort> value, byte count) => ShiftRightLogicalRoundedNarrowingSaturateScalar(value, count);

            /// <summary>
            /// uint16_t vqrshrns_n_u32 (uint32_t a, const int n)
            ///   A64: UQRSHRN Hd, Sn, #n
            /// </summary>
            public static Vector64<short> ShiftRightLogicalRoundedNarrowingSaturateScalar(Vector64<int> value, byte count) => ShiftRightLogicalRoundedNarrowingSaturateScalar(value, count);

            /// <summary>
            /// uint32_t vqrshrnd_n_u64 (uint64_t a, const int n)
            ///   A64: UQRSHRN Sd, Dn, #n
            /// </summary>
            public static Vector64<int> ShiftRightLogicalRoundedNarrowingSaturateScalar(Vector64<long> value, byte count) => ShiftRightLogicalRoundedNarrowingSaturateScalar(value, count);

            /// <summary>
            /// uint8_t vqrshrnh_n_u16 (uint16_t a, const int n)
            ///   A64: UQRSHRN Bd, Hn, #n
            /// </summary>
            public static Vector64<sbyte> ShiftRightLogicalRoundedNarrowingSaturateScalar(Vector64<short> value, byte count) => ShiftRightLogicalRoundedNarrowingSaturateScalar(value, count);

            /// <summary>
            /// uint16_t vqrshrns_n_u32 (uint32_t a, const int n)
            ///   A64: UQRSHRN Hd, Sn, #n
            /// </summary>
            public static Vector64<ushort> ShiftRightLogicalRoundedNarrowingSaturateScalar(Vector64<uint> value, byte count) => ShiftRightLogicalRoundedNarrowingSaturateScalar(value, count);

            /// <summary>
            /// uint32_t vqrshrnd_n_u64 (uint64_t a, const int n)
            ///   A64: UQRSHRN Sd, Dn, #n
            /// </summary>
            public static Vector64<uint> ShiftRightLogicalRoundedNarrowingSaturateScalar(Vector64<ulong> value, byte count) => ShiftRightLogicalRoundedNarrowingSaturateScalar(value, count);

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
            /// uint8_t vqsubb_u8 (uint8_t a, uint8_t b)
            ///   A64: UQSUB Bd, Bn, Bm
            /// </summary>
            public static Vector64<byte> SubtractSaturateScalar(Vector64<byte> left, Vector64<byte> right) => SubtractSaturateScalar(left, right);

            /// <summary>
            /// int16_t vqsubh_s16 (int16_t a, int16_t b)
            ///   A64: SQSUB Hd, Hn, Hm
            /// </summary>
            public static Vector64<short> SubtractSaturateScalar(Vector64<short> left, Vector64<short> right) => SubtractSaturateScalar(left, right);

            /// <summary>
            /// int32_t vqsubs_s32 (int32_t a, int32_t b)
            ///   A64: SQSUB Sd, Sn, Sm
            /// </summary>
            public static Vector64<int> SubtractSaturateScalar(Vector64<int> left, Vector64<int> right) => SubtractSaturateScalar(left, right);

            /// <summary>
            /// int8_t vqsubb_s8 (int8_t a, int8_t b)
            ///   A64: SQSUB Bd, Bn, Bm
            /// </summary>
            public static Vector64<sbyte> SubtractSaturateScalar(Vector64<sbyte> left, Vector64<sbyte> right) => SubtractSaturateScalar(left, right);

            /// <summary>
            /// uint16_t vqsubh_u16 (uint16_t a, uint16_t b)
            ///   A64: UQSUB Hd, Hn, Hm
            /// </summary>
            public static Vector64<ushort> SubtractSaturateScalar(Vector64<ushort> left, Vector64<ushort> right) => SubtractSaturateScalar(left, right);

            /// <summary>
            /// uint32_t vqsubs_u32 (uint32_t a, uint32_t b)
            ///   A64: UQSUB Sd, Sn, Sm
            /// </summary>
            public static Vector64<uint> SubtractSaturateScalar(Vector64<uint> left, Vector64<uint> right) => SubtractSaturateScalar(left, right);

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
            ///  uint8x16_t vqvtbl1q_u8(uint8x16_t t, uint8x16_t idx)
            ///   A64: TBL Vd.16B, {Vn.16B}, Vm.16B
            /// </summary>
            public static Vector128<byte> VectorTableLookup(Vector128<byte> table, Vector128<byte> byteIndexes) => VectorTableLookup(table, byteIndexes);

            /// <summary>
            ///  int8x16_t vqvtbl1q_s8(int8x16_t t, uint8x16_t idx)
            ///   A64: TBL Vd.16B, {Vn.16B}, Vm.16B
            /// </summary>
            public static Vector128<sbyte> VectorTableLookup(Vector128<sbyte> table, Vector128<sbyte> byteIndexes) => VectorTableLookup(table, byteIndexes);

            /// <summary>
            ///  uint8x16_t vqvtbx1q_u8(uint8x16_t r, int8x16_t t, uint8x16_t idx)
            ///   A64: TBX Vd.16B, {Vn.16B}, Vm.16B
            /// </summary>
            public static Vector128<byte> VectorTableLookupExtension(Vector128<byte> defaultValues, Vector128<byte> table, Vector128<byte> byteIndexes) => VectorTableLookupExtension(defaultValues, table, byteIndexes);

            /// <summary>
            ///  int8x16_t vqvtbx1q_s8(int8x16_t r, int8x16_t t, uint8x16_t idx)
            ///   A64: TBX Vd.16B, {Vn.16B}, Vm.16B
            /// </summary>
            public static Vector128<sbyte> VectorTableLookupExtension(Vector128<sbyte> defaultValues, Vector128<sbyte> table, Vector128<sbyte> byteIndexes) => VectorTableLookupExtension(defaultValues, table, byteIndexes);

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
        /// uint8x8_t vaba_u8 (uint8x8_t a, uint8x8_t b, uint8x8_t c)
        ///   A32: VABA.U8 Dd, Dn, Dm
        ///   A64: UABA Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> AbsoluteDifferenceAdd(Vector64<byte> addend, Vector64<byte> left, Vector64<byte> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// int16x4_t vaba_s16 (int16x4_t a, int16x4_t b, int16x4_t c)
        ///   A32: VABA.S16 Dd, Dn, Dm
        ///   A64: SABA Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> AbsoluteDifferenceAdd(Vector64<short> addend, Vector64<short> left, Vector64<short> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// int32x2_t vaba_s32 (int32x2_t a, int32x2_t b, int32x2_t c)
        ///   A32: VABA.S32 Dd, Dn, Dm
        ///   A64: SABA Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> AbsoluteDifferenceAdd(Vector64<int> addend, Vector64<int> left, Vector64<int> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// int8x8_t vaba_s8 (int8x8_t a, int8x8_t b, int8x8_t c)
        ///   A32: VABA.S8 Dd, Dn, Dm
        ///   A64: SABA Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> AbsoluteDifferenceAdd(Vector64<sbyte> addend, Vector64<sbyte> left, Vector64<sbyte> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// uint16x4_t vaba_u16 (uint16x4_t a, uint16x4_t b, uint16x4_t c)
        ///   A32: VABA.U16 Dd, Dn, Dm
        ///   A64: UABA Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> AbsoluteDifferenceAdd(Vector64<ushort> addend, Vector64<ushort> left, Vector64<ushort> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// uint32x2_t vaba_u32 (uint32x2_t a, uint32x2_t b, uint32x2_t c)
        ///   A32: VABA.U32 Dd, Dn, Dm
        ///   A64: UABA Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> AbsoluteDifferenceAdd(Vector64<uint> addend, Vector64<uint> left, Vector64<uint> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// uint8x16_t vabaq_u8 (uint8x16_t a, uint8x16_t b, uint8x16_t c)
        ///   A32: VABA.U8 Qd, Qn, Qm
        ///   A64: UABA Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> AbsoluteDifferenceAdd(Vector128<byte> addend, Vector128<byte> left, Vector128<byte> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// int16x8_t vabaq_s16 (int16x8_t a, int16x8_t b, int16x8_t c)
        ///   A32: VABA.S16 Qd, Qn, Qm
        ///   A64: SABA Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> AbsoluteDifferenceAdd(Vector128<short> addend, Vector128<short> left, Vector128<short> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// int32x4_t vabaq_s32 (int32x4_t a, int32x4_t b, int32x4_t c)
        ///   A32: VABA.S32 Qd, Qn, Qm
        ///   A64: SABA Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> AbsoluteDifferenceAdd(Vector128<int> addend, Vector128<int> left, Vector128<int> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// int8x16_t vabaq_s8 (int8x16_t a, int8x16_t b, int8x16_t c)
        ///   A32: VABA.S8 Qd, Qn, Qm
        ///   A64: SABA Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> AbsoluteDifferenceAdd(Vector128<sbyte> addend, Vector128<sbyte> left, Vector128<sbyte> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// uint16x8_t vabaq_u16 (uint16x8_t a, uint16x8_t b, uint16x8_t c)
        ///   A32: VABA.U16 Qd, Qn, Qm
        ///   A64: UABA Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> AbsoluteDifferenceAdd(Vector128<ushort> addend, Vector128<ushort> left, Vector128<ushort> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// uint32x4_t vabaq_u32 (uint32x4_t a, uint32x4_t b, uint32x4_t c)
        ///   A32: VABA.U32 Qd, Qn, Qm
        ///   A64: UABA Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> AbsoluteDifferenceAdd(Vector128<uint> addend, Vector128<uint> left, Vector128<uint> right) => AbsoluteDifferenceAdd(addend, left, right);

        /// <summary>
        /// uint16x8_t vabdl_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VABDL.U8 Qd, Dn, Dm
        ///   A64: UABDL Vd.8H, Vn.8B, Vm.8B
        /// </summary>
        public static Vector128<ushort> AbsoluteDifferenceWideningLower(Vector64<byte> left, Vector64<byte> right) => AbsoluteDifferenceWideningLower(left, right);

        /// <summary>
        /// int32x4_t vabdl_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VABDL.S16 Qd, Dn, Dm
        ///   A64: SABDL Vd.4S, Vn.4H, Vm.4H
        /// </summary>
        public static Vector128<uint> AbsoluteDifferenceWideningLower(Vector64<short> left, Vector64<short> right) => AbsoluteDifferenceWideningLower(left, right);

        /// <summary>
        /// int64x2_t vabdl_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VABDL.S32 Qd, Dn, Dm
        ///   A64: SABDL Vd.2D, Vn.2S, Vm.2S
        /// </summary>
        public static Vector128<ulong> AbsoluteDifferenceWideningLower(Vector64<int> left, Vector64<int> right) => AbsoluteDifferenceWideningLower(left, right);

        /// <summary>
        /// int16x8_t vabdl_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VABDL.S8 Qd, Dn, Dm
        ///   A64: SABDL Vd.8H, Vn.8B, Vm.8B
        /// </summary>
        public static Vector128<ushort> AbsoluteDifferenceWideningLower(Vector64<sbyte> left, Vector64<sbyte> right) => AbsoluteDifferenceWideningLower(left, right);

        /// <summary>
        /// uint32x4_t vabdl_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VABDL.U16 Qd, Dn, Dm
        ///   A64: UABDL Vd.4S, Vn.4H, Vm.4H
        /// </summary>
        public static Vector128<uint> AbsoluteDifferenceWideningLower(Vector64<ushort> left, Vector64<ushort> right) => AbsoluteDifferenceWideningLower(left, right);

        /// <summary>
        /// uint64x2_t vabdl_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VABDL.U32 Qd, Dn, Dm
        ///   A64: UABDL Vd.2D, Vn.2S, Vm.2S
        /// </summary>
        public static Vector128<ulong> AbsoluteDifferenceWideningLower(Vector64<uint> left, Vector64<uint> right) => AbsoluteDifferenceWideningLower(left, right);

        /// <summary>
        /// uint16x8_t vabal_u8 (uint16x8_t a, uint8x8_t b, uint8x8_t c)
        ///   A32: VABAL.U8 Qd, Dn, Dm
        ///   A64: UABAL Vd.8H, Vn.8B, Vm.8B
        /// </summary>
        public static Vector128<ushort> AbsoluteDifferenceWideningLowerAndAdd(Vector128<ushort> addend, Vector64<byte> left, Vector64<byte> right) => AbsoluteDifferenceWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// int32x4_t vabal_s16 (int32x4_t a, int16x4_t b, int16x4_t c)
        ///   A32: VABAL.S16 Qd, Dn, Dm
        ///   A64: SABAL Vd.4S, Vn.4H, Vm.4H
        /// </summary>
        public static Vector128<int> AbsoluteDifferenceWideningLowerAndAdd(Vector128<int> addend, Vector64<short> left, Vector64<short> right) => AbsoluteDifferenceWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// int64x2_t vabal_s32 (int64x2_t a, int32x2_t b, int32x2_t c)
        ///   A32: VABAL.S32 Qd, Dn, Dm
        ///   A64: SABAL Vd.2D, Vn.2S, Vm.2S
        /// </summary>
        public static Vector128<long> AbsoluteDifferenceWideningLowerAndAdd(Vector128<long> addend, Vector64<int> left, Vector64<int> right) => AbsoluteDifferenceWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// int16x8_t vabal_s8 (int16x8_t a, int8x8_t b, int8x8_t c)
        ///   A32: VABAL.S8 Qd, Dn, Dm
        ///   A64: SABAL Vd.8H, Vn.8B, Vm.8B
        /// </summary>
        public static Vector128<short> AbsoluteDifferenceWideningLowerAndAdd(Vector128<short> addend, Vector64<sbyte> left, Vector64<sbyte> right) => AbsoluteDifferenceWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// uint32x4_t vabal_u16 (uint32x4_t a, uint16x4_t b, uint16x4_t c)
        ///   A32: VABAL.U16 Qd, Dn, Dm
        ///   A64: UABAL Vd.4S, Vn.4H, Vm.4H
        /// </summary>
        public static Vector128<uint> AbsoluteDifferenceWideningLowerAndAdd(Vector128<uint> addend, Vector64<ushort> left, Vector64<ushort> right) => AbsoluteDifferenceWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// uint64x2_t vabal_u32 (uint64x2_t a, uint32x2_t b, uint32x2_t c)
        ///   A32: VABAL.U32 Qd, Dn, Dm
        ///   A64: UABAL Vd.2D, Vn.2S, Vm.2S
        /// </summary>
        public static Vector128<ulong> AbsoluteDifferenceWideningLowerAndAdd(Vector128<ulong> addend, Vector64<uint> left, Vector64<uint> right) => AbsoluteDifferenceWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// uint16x8_t vabdl_high_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VABDL.U8 Qd, Dn+1, Dm+1
        ///   A64: UABDL2 Vd.8H, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<ushort> AbsoluteDifferenceWideningUpper(Vector128<byte> left, Vector128<byte> right) => AbsoluteDifferenceWideningUpper(left, right);

        /// <summary>
        /// int32x4_t vabdl_high_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VABDL.S16 Qd, Dn+1, Dm+1
        ///   A64: SABDL2 Vd.4S, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<uint> AbsoluteDifferenceWideningUpper(Vector128<short> left, Vector128<short> right) => AbsoluteDifferenceWideningUpper(left, right);

        /// <summary>
        /// int64x2_t vabdl_high_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VABDL.S32 Qd, Dn+1, Dm+1
        ///   A64: SABDL2 Vd.2D, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<ulong> AbsoluteDifferenceWideningUpper(Vector128<int> left, Vector128<int> right) => AbsoluteDifferenceWideningUpper(left, right);

        /// <summary>
        /// int16x8_t vabdl_high_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VABDL.S8 Qd, Dn+1, Dm+1
        ///   A64: SABDL2 Vd.8H, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<ushort> AbsoluteDifferenceWideningUpper(Vector128<sbyte> left, Vector128<sbyte> right) => AbsoluteDifferenceWideningUpper(left, right);

        /// <summary>
        /// uint32x4_t vabdl_high_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VABDL.U16 Qd, Dn+1, Dm+1
        ///   A64: UABDL2 Vd.4S, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<uint> AbsoluteDifferenceWideningUpper(Vector128<ushort> left, Vector128<ushort> right) => AbsoluteDifferenceWideningUpper(left, right);

        /// <summary>
        /// uint64x2_t vabdl_high_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VABDL.U32 Qd, Dn+1, Dm+1
        ///   A64: UABDL2 Vd.2D, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<ulong> AbsoluteDifferenceWideningUpper(Vector128<uint> left, Vector128<uint> right) => AbsoluteDifferenceWideningUpper(left, right);

        /// <summary>
        /// uint16x8_t vabal_high_u8 (uint16x8_t a, uint8x16_t b, uint8x16_t c)
        ///   A32: VABAL.U8 Qd, Dn+1, Dm+1
        ///   A64: UABAL2 Vd.8H, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<ushort> AbsoluteDifferenceWideningUpperAndAdd(Vector128<ushort> addend, Vector128<byte> left, Vector128<byte> right) => AbsoluteDifferenceWideningUpperAndAdd(addend, left, right);

        /// <summary>
        /// int32x4_t vabal_high_s16 (int32x4_t a, int16x8_t b, int16x8_t c)
        ///   A32: VABAL.S16 Qd, Dn+1, Dm+1
        ///   A64: SABAL2 Vd.4S, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<int> AbsoluteDifferenceWideningUpperAndAdd(Vector128<int> addend, Vector128<short> left, Vector128<short> right) => AbsoluteDifferenceWideningUpperAndAdd(addend, left, right);

        /// <summary>
        /// int64x2_t vabal_high_s32 (int64x2_t a, int32x4_t b, int32x4_t c)
        ///   A32: VABAL.S32 Qd, Dn+1, Dm+1
        ///   A64: SABAL2 Vd.2D, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<long> AbsoluteDifferenceWideningUpperAndAdd(Vector128<long> addend, Vector128<int> left, Vector128<int> right) => AbsoluteDifferenceWideningUpperAndAdd(addend, left, right);

        /// <summary>
        /// int16x8_t vabal_high_s8 (int16x8_t a, int8x16_t b, int8x16_t c)
        ///   A32: VABAL.S8 Qd, Dn+1, Dm+1
        ///   A64: SABAL2 Vd.8H, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<short> AbsoluteDifferenceWideningUpperAndAdd(Vector128<short> addend, Vector128<sbyte> left, Vector128<sbyte> right) => AbsoluteDifferenceWideningUpperAndAdd(addend, left, right);

        /// <summary>
        /// uint32x4_t vabal_high_u16 (uint32x4_t a, uint16x8_t b, uint16x8_t c)
        ///   A32: VABAL.U16 Qd, Dn+1, Dm+1
        ///   A64: UABAL2 Vd.4S, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<uint> AbsoluteDifferenceWideningUpperAndAdd(Vector128<uint> addend, Vector128<ushort> left, Vector128<ushort> right) => AbsoluteDifferenceWideningUpperAndAdd(addend, left, right);

        /// <summary>
        /// uint64x2_t vabal_high_u32 (uint64x2_t a, uint32x4_t b, uint32x4_t c)
        ///   A32: VABAL.U32 Qd, Dn+1, Dm+1
        ///   A64: UABAL2 Vd.2D, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<ulong> AbsoluteDifferenceWideningUpperAndAdd(Vector128<ulong> addend, Vector128<uint> left, Vector128<uint> right) => AbsoluteDifferenceWideningUpperAndAdd(addend, left, right);

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
        /// uint8x8_t vaddhn_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VADDHN.I16 Dd, Qn, Qm
        ///   A64: ADDHN Vd.8B, Vn.8H, Vm.8H
        /// </summary>
        public static Vector64<byte> AddHighNarrowingLower(Vector128<ushort> left, Vector128<ushort> right) => AddHighNarrowingLower(left, right);

        /// <summary>
        /// int16x4_t vaddhn_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VADDHN.I32 Dd, Qn, Qm
        ///   A64: ADDHN Vd.4H, Vn.4S, Vm.4S
        /// </summary>
        public static Vector64<short> AddHighNarrowingLower(Vector128<int> left, Vector128<int> right) => AddHighNarrowingLower(left, right);

        /// <summary>
        /// int32x2_t vaddhn_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VADDHN.I64 Dd, Qn, Qm
        ///   A64: ADDHN Vd.2S, Vn.2D, Vm.2D
        /// </summary>
        public static Vector64<int> AddHighNarrowingLower(Vector128<long> left, Vector128<long> right) => AddHighNarrowingLower(left, right);

        /// <summary>
        /// int8x8_t vaddhn_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VADDHN.I16 Dd, Qn, Qm
        ///   A64: ADDHN Vd.8B, Vn.8H, Vm.8H
        /// </summary>
        public static Vector64<sbyte> AddHighNarrowingLower(Vector128<short> left, Vector128<short> right) => AddHighNarrowingLower(left, right);

        /// <summary>
        /// uint16x4_t vaddhn_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VADDHN.I32 Dd, Qn, Qm
        ///   A64: ADDHN Vd.4H, Vn.4S, Vm.4S
        /// </summary>
        public static Vector64<ushort> AddHighNarrowingLower(Vector128<uint> left, Vector128<uint> right) => AddHighNarrowingLower(left, right);

        /// <summary>
        /// uint32x2_t vaddhn_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VADDHN.I64 Dd, Qn, Qm
        ///   A64: ADDHN Vd.2S, Vn.2D, Vm.2D
        /// </summary>
        public static Vector64<uint> AddHighNarrowingLower(Vector128<ulong> left, Vector128<ulong> right) => AddHighNarrowingLower(left, right);

        /// <summary>
        /// uint8x16_t vaddhn_high_u16 (uint8x8_t r, uint16x8_t a, uint16x8_t b)
        ///   A32: VADDHN.I16 Dd+1, Qn, Qm
        ///   A64: ADDHN2 Vd.16B, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<byte> AddHighNarrowingUpper(Vector64<byte> lower, Vector128<ushort> left, Vector128<ushort> right) => AddHighNarrowingUpper(lower, left, right);

        /// <summary>
        /// int16x8_t vaddhn_high_s32 (int16x4_t r, int32x4_t a, int32x4_t b)
        ///   A32: VADDHN.I32 Dd+1, Qn, Qm
        ///   A64: ADDHN2 Vd.8H, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<short> AddHighNarrowingUpper(Vector64<short> lower, Vector128<int> left, Vector128<int> right) => AddHighNarrowingUpper(lower, left, right);

        /// <summary>
        /// int32x4_t vaddhn_high_s64 (int32x2_t r, int64x2_t a, int64x2_t b)
        ///   A32: VADDHN.I64 Dd+1, Qn, Qm
        ///   A64: ADDHN2 Vd.4S, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<int> AddHighNarrowingUpper(Vector64<int> lower, Vector128<long> left, Vector128<long> right) => AddHighNarrowingUpper(lower, left, right);

        /// <summary>
        /// int8x16_t vaddhn_high_s16 (int8x8_t r, int16x8_t a, int16x8_t b)
        ///   A32: VADDHN.I16 Dd+1, Qn, Qm
        ///   A64: ADDHN2 Vd.16B, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<sbyte> AddHighNarrowingUpper(Vector64<sbyte> lower, Vector128<short> left, Vector128<short> right) => AddHighNarrowingUpper(lower, left, right);

        /// <summary>
        /// uint16x8_t vaddhn_high_u32 (uint16x4_t r, uint32x4_t a, uint32x4_t b)
        ///   A32: VADDHN.I32 Dd+1, Qn, Qm
        ///   A64: ADDHN2 Vd.8H, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<ushort> AddHighNarrowingUpper(Vector64<ushort> lower, Vector128<uint> left, Vector128<uint> right) => AddHighNarrowingUpper(lower, left, right);

        /// <summary>
        /// uint32x4_t vaddhn_high_u64 (uint32x2_t r, uint64x2_t a, uint64x2_t b)
        ///   A32: VADDHN.I64 Dd+1, Qn, Qm
        ///   A64: ADDHN2 Vd.4S, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<uint> AddHighNarrowingUpper(Vector64<uint> lower, Vector128<ulong> left, Vector128<ulong> right) => AddHighNarrowingUpper(lower, left, right);

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
        /// uint16x4_t vpaddl_u8 (uint8x8_t a)
        ///   A32: VPADDL.U8 Dd, Dm
        ///   A64: UADDLP Vd.4H, Vn.8B
        /// </summary>
        public static Vector64<ushort> AddPairwiseWidening(Vector64<byte> value) => AddPairwiseWidening(value);

        /// <summary>
        /// int32x2_t vpaddl_s16 (int16x4_t a)
        ///   A32: VPADDL.S16 Dd, Dm
        ///   A64: SADDLP Vd.2S, Vn.4H
        /// </summary>
        public static Vector64<int> AddPairwiseWidening(Vector64<short> value) => AddPairwiseWidening(value);

        /// <summary>
        /// int16x4_t vpaddl_s8 (int8x8_t a)
        ///   A32: VPADDL.S8 Dd, Dm
        ///   A64: SADDLP Vd.4H, Vn.8B
        /// </summary>
        public static Vector64<short> AddPairwiseWidening(Vector64<sbyte> value) => AddPairwiseWidening(value);

        /// <summary>
        /// uint32x2_t vpaddl_u16 (uint16x4_t a)
        ///   A32: VPADDL.U16 Dd, Dm
        ///   A64: UADDLP Vd.2S, Vn.4H
        /// </summary>
        public static Vector64<uint> AddPairwiseWidening(Vector64<ushort> value) => AddPairwiseWidening(value);

        /// <summary>
        /// uint16x8_t vpaddlq_u8 (uint8x16_t a)
        ///   A32: VPADDL.U8 Qd, Qm
        ///   A64: UADDLP Vd.8H, Vn.16B
        /// </summary>
        public static Vector128<ushort> AddPairwiseWidening(Vector128<byte> value) => AddPairwiseWidening(value);

        /// <summary>
        /// int32x4_t vpaddlq_s16 (int16x8_t a)
        ///   A32: VPADDL.S16 Qd, Qm
        ///   A64: SADDLP Vd.4S, Vn.8H
        /// </summary>
        public static Vector128<int> AddPairwiseWidening(Vector128<short> value) => AddPairwiseWidening(value);

        /// <summary>
        /// int64x2_t vpaddlq_s32 (int32x4_t a)
        ///   A32: VPADDL.S32 Qd, Qm
        ///   A64: SADDLP Vd.2D, Vn.4S
        /// </summary>
        public static Vector128<long> AddPairwiseWidening(Vector128<int> value) => AddPairwiseWidening(value);

        /// <summary>
        /// int16x8_t vpaddlq_s8 (int8x16_t a)
        ///   A32: VPADDL.S8 Qd, Qm
        ///   A64: SADDLP Vd.8H, Vn.16B
        /// </summary>
        public static Vector128<short> AddPairwiseWidening(Vector128<sbyte> value) => AddPairwiseWidening(value);

        /// <summary>
        /// uint32x4_t vpaddlq_u16 (uint16x8_t a)
        ///   A32: VPADDL.U16 Qd, Qm
        ///   A64: UADDLP Vd.4S, Vn.8H
        /// </summary>
        public static Vector128<uint> AddPairwiseWidening(Vector128<ushort> value) => AddPairwiseWidening(value);

        /// <summary>
        /// uint64x2_t vpaddlq_u32 (uint32x4_t a)
        ///   A32: VPADDL.U32 Qd, Qm
        ///   A64: UADDLP Vd.2D, Vn.4S
        /// </summary>
        public static Vector128<ulong> AddPairwiseWidening(Vector128<uint> value) => AddPairwiseWidening(value);

        /// <summary>
        /// uint16x4_t vpadal_u8 (uint16x4_t a, uint8x8_t b)
        ///   A32: VPADAL.U8 Dd, Dm
        ///   A64: UADALP Vd.4H, Vn.8B
        /// </summary>
        public static Vector64<ushort> AddPairwiseWideningAndAdd(Vector64<ushort> addend, Vector64<byte> value) => AddPairwiseWideningAndAdd(addend, value);

        /// <summary>
        /// int32x2_t vpadal_s16 (int32x2_t a, int16x4_t b)
        ///   A32: VPADAL.S16 Dd, Dm
        ///   A64: SADALP Vd.2S, Vn.4H
        /// </summary>
        public static Vector64<int> AddPairwiseWideningAndAdd(Vector64<int> addend, Vector64<short> value) => AddPairwiseWideningAndAdd(addend, value);

        /// <summary>
        /// int16x4_t vpadal_s8 (int16x4_t a, int8x8_t b)
        ///   A32: VPADAL.S8 Dd, Dm
        ///   A64: SADALP Vd.4H, Vn.8B
        /// </summary>
        public static Vector64<short> AddPairwiseWideningAndAdd(Vector64<short> addend, Vector64<sbyte> value) => AddPairwiseWideningAndAdd(addend, value);

        /// <summary>
        /// uint32x2_t vpadal_u16 (uint32x2_t a, uint16x4_t b)
        ///   A32: VPADAL.U16 Dd, Dm
        ///   A64: UADALP Vd.2S, Vn.4H
        /// </summary>
        public static Vector64<uint> AddPairwiseWideningAndAdd(Vector64<uint> addend, Vector64<ushort> value) => AddPairwiseWideningAndAdd(addend, value);

        /// <summary>
        /// uint16x8_t vpadalq_u8 (uint16x8_t a, uint8x16_t b)
        ///   A32: VPADAL.U8 Qd, Qm
        ///   A64: UADALP Vd.8H, Vn.16B
        /// </summary>
        public static Vector128<ushort> AddPairwiseWideningAndAdd(Vector128<ushort> addend, Vector128<byte> value) => AddPairwiseWideningAndAdd(addend, value);

        /// <summary>
        /// int32x4_t vpadalq_s16 (int32x4_t a, int16x8_t b)
        ///   A32: VPADAL.S16 Qd, Qm
        ///   A64: SADALP Vd.4S, Vn.8H
        /// </summary>
        public static Vector128<int> AddPairwiseWideningAndAdd(Vector128<int> addend, Vector128<short> value) => AddPairwiseWideningAndAdd(addend, value);

        /// <summary>
        /// int64x2_t vpadalq_s32 (int64x2_t a, int32x4_t b)
        ///   A32: VPADAL.S32 Qd, Qm
        ///   A64: SADALP Vd.2D, Vn.4S
        /// </summary>
        public static Vector128<long> AddPairwiseWideningAndAdd(Vector128<long> addend, Vector128<int> value) => AddPairwiseWideningAndAdd(addend, value);

        /// <summary>
        /// int16x8_t vpadalq_s8 (int16x8_t a, int8x16_t b)
        ///   A32: VPADAL.S8 Qd, Qm
        ///   A64: SADALP Vd.8H, Vn.16B
        /// </summary>
        public static Vector128<short> AddPairwiseWideningAndAdd(Vector128<short> addend, Vector128<sbyte> value) => AddPairwiseWideningAndAdd(addend, value);

        /// <summary>
        /// uint32x4_t vpadalq_u16 (uint32x4_t a, uint16x8_t b)
        ///   A32: VPADAL.U16 Qd, Qm
        ///   A64: UADALP Vd.4S, Vn.8H
        /// </summary>
        public static Vector128<uint> AddPairwiseWideningAndAdd(Vector128<uint> addend, Vector128<ushort> value) => AddPairwiseWideningAndAdd(addend, value);

        /// <summary>
        /// uint64x2_t vpadalq_u32 (uint64x2_t a, uint32x4_t b)
        ///   A32: VPADAL.U32 Qd, Qm
        ///   A64: UADALP Vd.2D, Vn.4S
        /// </summary>
        public static Vector128<ulong> AddPairwiseWideningAndAdd(Vector128<ulong> addend, Vector128<uint> value) => AddPairwiseWideningAndAdd(addend, value);

        /// <summary>
        /// int64x1_t vpadal_s32 (int64x1_t a, int32x2_t b)
        ///   A32: VPADAL.S32 Dd, Dm
        ///   A64: SADALP Vd.1D, Vn.2S
        /// </summary>
        public static Vector64<long> AddPairwiseWideningAndAddScalar(Vector64<long> addend, Vector64<int> value) => AddPairwiseWideningAndAddScalar(addend, value);

        /// <summary>
        /// uint64x1_t vpadal_u32 (uint64x1_t a, uint32x2_t b)
        ///   A32: VPADAL.U32 Dd, Dm
        ///   A64: UADALP Vd.1D, Vn.2S
        /// </summary>
        public static Vector64<ulong> AddPairwiseWideningAndAddScalar(Vector64<ulong> addend, Vector64<uint> value) => AddPairwiseWideningAndAddScalar(addend, value);

        /// <summary>
        /// int64x1_t vpaddl_s32 (int32x2_t a)
        ///   A32: VPADDL.S32 Dd, Dm
        ///   A64: SADDLP Dd, Vn.2S
        /// </summary>
        public static Vector64<long> AddPairwiseWideningScalar(Vector64<int> value) => AddPairwiseWideningScalar(value);

        /// <summary>
        /// uint64x1_t vpaddl_u32 (uint32x2_t a)
        ///   A32: VPADDL.U32 Dd, Dm
        ///   A64: UADDLP Dd, Vn.2S
        /// </summary>
        public static Vector64<ulong> AddPairwiseWideningScalar(Vector64<uint> value) => AddPairwiseWideningScalar(value);

        /// <summary>
        /// uint8x8_t vraddhn_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VRADDHN.I16 Dd, Qn, Qm
        ///   A64: RADDHN Vd.8B, Vn.8H, Vm.8H
        /// </summary>
        public static Vector64<byte> AddRoundedHighNarrowingLower(Vector128<ushort> left, Vector128<ushort> right) => AddRoundedHighNarrowingLower(left, right);

        /// <summary>
        /// int16x4_t vraddhn_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VRADDHN.I32 Dd, Qn, Qm
        ///   A64: RADDHN Vd.4H, Vn.4S, Vm.4S
        /// </summary>
        public static Vector64<short> AddRoundedHighNarrowingLower(Vector128<int> left, Vector128<int> right) => AddRoundedHighNarrowingLower(left, right);

        /// <summary>
        /// int32x2_t vraddhn_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VRADDHN.I64 Dd, Qn, Qm
        ///   A64: RADDHN Vd.2S, Vn.2D, Vm.2D
        /// </summary>
        public static Vector64<int> AddRoundedHighNarrowingLower(Vector128<long> left, Vector128<long> right) => AddRoundedHighNarrowingLower(left, right);

        /// <summary>
        /// int8x8_t vraddhn_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VRADDHN.I16 Dd, Qn, Qm
        ///   A64: RADDHN Vd.8B, Vn.8H, Vm.8H
        /// </summary>
        public static Vector64<sbyte> AddRoundedHighNarrowingLower(Vector128<short> left, Vector128<short> right) => AddRoundedHighNarrowingLower(left, right);

        /// <summary>
        /// uint16x4_t vraddhn_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VRADDHN.I32 Dd, Qn, Qm
        ///   A64: RADDHN Vd.4H, Vn.4S, Vm.4S
        /// </summary>
        public static Vector64<ushort> AddRoundedHighNarrowingLower(Vector128<uint> left, Vector128<uint> right) => AddRoundedHighNarrowingLower(left, right);

        /// <summary>
        /// uint32x2_t vraddhn_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VRADDHN.I64 Dd, Qn, Qm
        ///   A64: RADDHN Vd.2S, Vn.2D, Vm.2D
        /// </summary>
        public static Vector64<uint> AddRoundedHighNarrowingLower(Vector128<ulong> left, Vector128<ulong> right) => AddRoundedHighNarrowingLower(left, right);

        /// <summary>
        /// uint8x16_t vraddhn_high_u16 (uint8x8_t r, uint16x8_t a, uint16x8_t b)
        ///   A32: VRADDHN.I16 Dd+1, Qn, Qm
        ///   A64: RADDHN2 Vd.16B, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<byte> AddRoundedHighNarrowingUpper(Vector64<byte> lower, Vector128<ushort> left, Vector128<ushort> right) => AddRoundedHighNarrowingUpper(lower, left, right);

        /// <summary>
        /// int16x8_t vraddhn_high_s32 (int16x4_t r, int32x4_t a, int32x4_t b)
        ///   A32: VRADDHN.I32 Dd+1, Qn, Qm
        ///   A64: RADDHN2 Vd.8H, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<short> AddRoundedHighNarrowingUpper(Vector64<short> lower, Vector128<int> left, Vector128<int> right) => AddRoundedHighNarrowingUpper(lower, left, right);

        /// <summary>
        /// int32x4_t vraddhn_high_s64 (int32x2_t r, int64x2_t a, int64x2_t b)
        ///   A32: VRADDHN.I64 Dd+1, Qn, Qm
        ///   A64: RADDHN2 Vd.4S, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<int> AddRoundedHighNarrowingUpper(Vector64<int> lower, Vector128<long> left, Vector128<long> right) => AddRoundedHighNarrowingUpper(lower, left, right);

        /// <summary>
        /// int8x16_t vraddhn_high_s16 (int8x8_t r, int16x8_t a, int16x8_t b)
        ///   A32: VRADDHN.I16 Dd+1, Qn, Qm
        ///   A64: RADDHN2 Vd.16B, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<sbyte> AddRoundedHighNarrowingUpper(Vector64<sbyte> lower, Vector128<short> left, Vector128<short> right) => AddRoundedHighNarrowingUpper(lower, left, right);

        /// <summary>
        /// uint16x8_t vraddhn_high_u32 (uint16x4_t r, uint32x4_t a, uint32x4_t b)
        ///   A32: VRADDHN.I32 Dd+1, Qn, Qm
        ///   A64: RADDHN2 Vd.8H, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<ushort> AddRoundedHighNarrowingUpper(Vector64<ushort> lower, Vector128<uint> left, Vector128<uint> right) => AddRoundedHighNarrowingUpper(lower, left, right);

        /// <summary>
        /// uint32x4_t vraddhn_high_u64 (uint32x2_t r, uint64x2_t a, uint64x2_t b)
        ///   A32: VRADDHN.I64 Dd+1, Qn, Qm
        ///   A64: RADDHN2 Vd.4S, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<uint> AddRoundedHighNarrowingUpper(Vector64<uint> lower, Vector128<ulong> left, Vector128<ulong> right) => AddRoundedHighNarrowingUpper(lower, left, right);

        /// <summary>
        /// uint8x8_t vqadd_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VQADD.U8 Dd, Dn, Dm
        ///   A64: UQADD Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> AddSaturate(Vector64<byte> left, Vector64<byte> right) => AddSaturate(left, right);

        /// <summary>
        /// int16x4_t vqadd_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VQADD.S16 Dd, Dn, Dm
        ///   A64: SQADD Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> AddSaturate(Vector64<short> left, Vector64<short> right) => AddSaturate(left, right);

        /// <summary>
        /// int32x2_t vqadd_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VQADD.S32 Dd, Dn, Dm
        ///   A64: SQADD Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> AddSaturate(Vector64<int> left, Vector64<int> right) => AddSaturate(left, right);

        /// <summary>
        /// int8x8_t vqadd_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VQADD.S8 Dd, Dn, Dm
        ///   A64: SQADD Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> AddSaturate(Vector64<sbyte> left, Vector64<sbyte> right) => AddSaturate(left, right);

        /// <summary>
        /// uint16x4_t vqadd_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VQADD.U16 Dd, Dn, Dm
        ///   A64: UQADD Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> AddSaturate(Vector64<ushort> left, Vector64<ushort> right) => AddSaturate(left, right);

        /// <summary>
        /// uint32x2_t vqadd_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VQADD.U32 Dd, Dn, Dm
        ///   A64: UQADD Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> AddSaturate(Vector64<uint> left, Vector64<uint> right) => AddSaturate(left, right);

        /// <summary>
        /// uint8x16_t vqaddq_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VQADD.U8 Qd, Qn, Qm
        ///   A64: UQADD Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> AddSaturate(Vector128<byte> left, Vector128<byte> right) => AddSaturate(left, right);

        /// <summary>
        /// int16x8_t vqaddq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VQADD.S16 Qd, Qn, Qm
        ///   A64: SQADD Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> AddSaturate(Vector128<short> left, Vector128<short> right) => AddSaturate(left, right);

        /// <summary>
        /// int32x4_t vqaddq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VQADD.S32 Qd, Qn, Qm
        ///   A64: SQADD Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> AddSaturate(Vector128<int> left, Vector128<int> right) => AddSaturate(left, right);

        /// <summary>
        /// int64x2_t vqaddq_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VQADD.S64 Qd, Qn, Qm
        ///   A64: SQADD Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<long> AddSaturate(Vector128<long> left, Vector128<long> right) => AddSaturate(left, right);

        /// <summary>
        /// int8x16_t vqaddq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VQADD.S8 Qd, Qn, Qm
        ///   A64: SQADD Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> AddSaturate(Vector128<sbyte> left, Vector128<sbyte> right) => AddSaturate(left, right);

        /// <summary>
        /// uint16x8_t vqaddq_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VQADD.U16 Qd, Qn, Qm
        ///   A64: UQADD Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> AddSaturate(Vector128<ushort> left, Vector128<ushort> right) => AddSaturate(left, right);

        /// <summary>
        /// uint32x4_t vqaddq_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VQADD.U32 Qd, Qn, Qm
        ///   A64: UQADD Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> AddSaturate(Vector128<uint> left, Vector128<uint> right) => AddSaturate(left, right);

        /// <summary>
        /// uint64x2_t vqaddq_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VQADD.U64 Qd, Qn, Qm
        ///   A64: UQADD Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<ulong> AddSaturate(Vector128<ulong> left, Vector128<ulong> right) => AddSaturate(left, right);

        /// <summary>
        /// int64x1_t vqadd_s64 (int64x1_t a, int64x1_t b)
        ///   A32: VQADD.S64 Dd, Dn, Dm
        ///   A64: SQADD Dd, Dn, Dm
        /// </summary>
        public static Vector64<long> AddSaturateScalar(Vector64<long> left, Vector64<long> right) => AddSaturateScalar(left, right);

        /// <summary>
        /// uint64x1_t vqadd_u64 (uint64x1_t a, uint64x1_t b)
        ///   A32: VQADD.U64 Dd, Dn, Dm
        ///   A64: UQADD Dd, Dn, Dm
        /// </summary>
        public static Vector64<ulong> AddSaturateScalar(Vector64<ulong> left, Vector64<ulong> right) => AddSaturateScalar(left, right);

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
        /// uint16x8_t vaddl_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VADDL.U8 Qd, Dn, Dm
        ///   A64: UADDL Vd.8H, Vn.8B, Vm.8B
        /// </summary>
        public static Vector128<ushort> AddWideningLower(Vector64<byte> left, Vector64<byte> right) => AddWideningLower(left, right);

        /// <summary>
        /// int32x4_t vaddl_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VADDL.S16 Qd, Dn, Dm
        ///   A64: SADDL Vd.4S, Vn.4H, Vm.4H
        /// </summary>
        public static Vector128<int> AddWideningLower(Vector64<short> left, Vector64<short> right) => AddWideningLower(left, right);

        /// <summary>
        /// int64x2_t vaddl_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VADDL.S32 Qd, Dn, Dm
        ///   A64: SADDL Vd.2D, Vn.2S, Vm.2S
        /// </summary>
        public static Vector128<long> AddWideningLower(Vector64<int> left, Vector64<int> right) => AddWideningLower(left, right);

        /// <summary>
        /// int16x8_t vaddl_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VADDL.S8 Qd, Dn, Dm
        ///   A64: SADDL Vd.8H, Vn.8B, Vm.8B
        /// </summary>
        public static Vector128<short> AddWideningLower(Vector64<sbyte> left, Vector64<sbyte> right) => AddWideningLower(left, right);

        /// <summary>
        /// uint32x4_t vaddl_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VADDL.U16 Qd, Dn, Dm
        ///   A64: UADDL Vd.4S, Vn.4H, Vm.4H
        /// </summary>
        public static Vector128<uint> AddWideningLower(Vector64<ushort> left, Vector64<ushort> right) => AddWideningLower(left, right);

        /// <summary>
        /// uint64x2_t vaddl_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VADDL.U32 Qd, Dn, Dm
        ///   A64: UADDL Vd.2D, Vn.2S, Vm.2S
        /// </summary>
        public static Vector128<ulong> AddWideningLower(Vector64<uint> left, Vector64<uint> right) => AddWideningLower(left, right);

        /// <summary>
        /// int16x8_t vaddw_s8 (int16x8_t a, int8x8_t b)
        ///   A32: VADDW.S8 Qd, Qn, Dm
        ///   A64: SADDW Vd.8H, Vn.8H, Vm.8B
        /// </summary>
        public static Vector128<short> AddWideningLower(Vector128<short> left, Vector64<sbyte> right) => AddWideningLower(left, right);

        /// <summary>
        /// int32x4_t vaddw_s16 (int32x4_t a, int16x4_t b)
        ///   A32: VADDW.S16 Qd, Qn, Dm
        ///   A64: SADDW Vd.4S, Vn.4S, Vm.4H
        /// </summary>
        public static Vector128<int> AddWideningLower(Vector128<int> left, Vector64<short> right) => AddWideningLower(left, right);

        /// <summary>
        /// int64x2_t vaddw_s32 (int64x2_t a, int32x2_t b)
        ///   A32: VADDW.S32 Qd, Qn, Dm
        ///   A64: SADDW Vd.2D, Vn.2D, Vm.2S
        /// </summary>
        public static Vector128<long> AddWideningLower(Vector128<long> left, Vector64<int> right) => AddWideningLower(left, right);

        /// <summary>
        /// uint16x8_t vaddw_u8 (uint16x8_t a, uint8x8_t b)
        ///   A32: VADDW.U8 Qd, Qn, Dm
        ///   A64: UADDW Vd.8H, Vn.8H, Vm.8B
        /// </summary>
        public static Vector128<ushort> AddWideningLower(Vector128<ushort> left, Vector64<byte> right) => AddWideningLower(left, right);

        /// <summary>
        /// uint32x4_t vaddw_u16 (uint32x4_t a, uint16x4_t b)
        ///   A32: VADDW.U16 Qd, Qn, Dm
        ///   A64: UADDW Vd.4S, Vn.4S, Vm.4H
        /// </summary>
        public static Vector128<uint> AddWideningLower(Vector128<uint> left, Vector64<ushort> right) => AddWideningLower(left, right);

        /// <summary>
        /// uint64x2_t vaddw_u32 (uint64x2_t a, uint32x2_t b)
        ///   A32: VADDW.U32 Qd, Qn, Dm
        ///   A64: UADDW Vd.2D, Vn.2D, Vm.2S
        /// </summary>
        public static Vector128<ulong> AddWideningLower(Vector128<ulong> left, Vector64<uint> right) => AddWideningLower(left, right);

        /// <summary>
        /// uint16x8_t vaddl_high_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VADDL.U8 Qd, Dn+1, Dm+1
        ///   A64: UADDL2 Vd.8H, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<ushort> AddWideningUpper(Vector128<byte> left, Vector128<byte> right) => AddWideningUpper(left, right);

        /// <summary>
        /// int32x4_t vaddl_high_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VADDL.S16 Qd, Dn+1, Dm+1
        ///   A64: SADDL2 Vd.4S, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<int> AddWideningUpper(Vector128<short> left, Vector128<short> right) => AddWideningUpper(left, right);

        /// <summary>
        /// int16x8_t vaddw_high_s8 (int16x8_t a, int8x16_t b)
        ///   A32: VADDW.S8 Qd, Qn, Dm+1
        ///   A64: SADDW2 Vd.8H, Vn.8H, Vm.16B
        /// </summary>
        public static Vector128<short> AddWideningUpper(Vector128<short> left, Vector128<sbyte> right) => AddWideningUpper(left, right);

        /// <summary>
        /// int32x4_t vaddw_high_s16 (int32x4_t a, int16x8_t b)
        ///   A32: VADDW.S16 Qd, Qn, Dm+1
        ///   A64: SADDW2 Vd.4S, Vn.4S, Vm.8H
        /// </summary>
        public static Vector128<int> AddWideningUpper(Vector128<int> left, Vector128<short> right) => AddWideningUpper(left, right);

        /// <summary>
        /// int64x2_t vaddl_high_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VADDL.S32 Qd, Dn+1, Dm+1
        ///   A64: SADDL2 Vd.2D, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<long> AddWideningUpper(Vector128<int> left, Vector128<int> right) => AddWideningUpper(left, right);

        /// <summary>
        /// int64x2_t vaddw_high_s32 (int64x2_t a, int32x4_t b)
        ///   A32: VADDW.S32 Qd, Qn, Dm+1
        ///   A64: SADDW2 Vd.2D, Vn.2D, Vm.4S
        /// </summary>
        public static Vector128<long> AddWideningUpper(Vector128<long> left, Vector128<int> right) => AddWideningUpper(left, right);

        /// <summary>
        /// int16x8_t vaddl_high_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VADDL.S8 Qd, Dn+1, Dm+1
        ///   A64: SADDL2 Vd.8H, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<short> AddWideningUpper(Vector128<sbyte> left, Vector128<sbyte> right) => AddWideningUpper(left, right);

        /// <summary>
        /// uint16x8_t vaddw_high_u8 (uint16x8_t a, uint8x16_t b)
        ///   A32: VADDW.U8 Qd, Qn, Dm+1
        ///   A64: UADDW2 Vd.8H, Vn.8H, Vm.16B
        /// </summary>
        public static Vector128<ushort> AddWideningUpper(Vector128<ushort> left, Vector128<byte> right) => AddWideningUpper(left, right);

        /// <summary>
        /// uint32x4_t vaddl_high_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VADDL.U16 Qd, Dn+1, Dm+1
        ///   A64: UADDL2 Vd.4S, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<uint> AddWideningUpper(Vector128<ushort> left, Vector128<ushort> right) => AddWideningUpper(left, right);

        /// <summary>
        /// uint32x4_t vaddw_high_u16 (uint32x4_t a, uint16x8_t b)
        ///   A32: VADDW.U16 Qd, Qn, Dm+1
        ///   A64: UADDW2 Vd.4S, Vn.4S, Vm.8H
        /// </summary>
        public static Vector128<uint> AddWideningUpper(Vector128<uint> left, Vector128<ushort> right) => AddWideningUpper(left, right);

        /// <summary>
        /// uint64x2_t vaddl_high_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VADDL.U32 Qd, Dn+1, Dm+1
        ///   A64: UADDL2 Vd.2D, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<ulong> AddWideningUpper(Vector128<uint> left, Vector128<uint> right) => AddWideningUpper(left, right);

        /// <summary>
        /// uint64x2_t vaddw_high_u32 (uint64x2_t a, uint32x4_t b)
        ///   A32: VADDW.U32 Qd, Qn, Dm+1
        ///   A64: UADDW2 Vd.2D, Vn.2D, Vm.4S
        /// </summary>
        public static Vector128<ulong> AddWideningUpper(Vector128<ulong> left, Vector128<uint> right) => AddWideningUpper(left, right);

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
        /// uint8x8_t vdup_lane_u8 (uint8x8_t vec, const int lane)
        ///   A32: VDUP.8 Dd, Dm[index]
        ///   A64: DUP Vd.8B, Vn.B[index]
        /// </summary>
        public static Vector64<byte> DuplicateSelectedScalarToVector64(Vector64<byte> value, byte index)=> DuplicateSelectedScalarToVector64(value, index);

        /// <summary>
        /// int16x4_t vdup_lane_s16 (int16x4_t vec, const int lane)
        ///   A32: VDUP.16 Dd, Dm[index]
        ///   A64: DUP Vd.4H, Vn.H[index]
        /// </summary>
        public static Vector64<short> DuplicateSelectedScalarToVector64(Vector64<short> value, byte index)=> DuplicateSelectedScalarToVector64(value, index);

        /// <summary>
        /// int32x2_t vdup_lane_s32 (int32x2_t vec, const int lane)
        ///   A32: VDUP.32 Dd, Dm[index]
        ///   A64: DUP Vd.2S, Vn.S[index]
        /// </summary>
        public static Vector64<int> DuplicateSelectedScalarToVector64(Vector64<int> value, byte index)=> DuplicateSelectedScalarToVector64(value, index);

        /// <summary>
        /// float32x2_t vdup_lane_f32 (float32x2_t vec, const int lane)
        ///   A32: VDUP.32 Dd, Dm[index]
        ///   A64: DUP Vd.2S, Vn.S[index]
        /// </summary>
        public static Vector64<float> DuplicateSelectedScalarToVector64(Vector64<float> value, byte index)=> DuplicateSelectedScalarToVector64(value, index);

        /// <summary>
        /// int8x8_t vdup_lane_s8 (int8x8_t vec, const int lane)
        ///   A32: VDUP.8 Dd, Dm[index]
        ///   A64: DUP Vd.8B, Vn.B[index]
        /// </summary>
        public static Vector64<sbyte> DuplicateSelectedScalarToVector64(Vector64<sbyte> value, byte index)=> DuplicateSelectedScalarToVector64(value, index);

        /// <summary>
        /// uint16x4_t vdup_lane_u16 (uint16x4_t vec, const int lane)
        ///   A32: VDUP.16 Dd, Dm[index]
        ///   A64: DUP Vd.4H, Vn.H[index]
        /// </summary>
        public static Vector64<ushort> DuplicateSelectedScalarToVector64(Vector64<ushort> value, byte index)=> DuplicateSelectedScalarToVector64(value, index);

        /// <summary>
        /// uint32x2_t vdup_lane_u32 (uint32x2_t vec, const int lane)
        ///   A32: VDUP.32 Dd, Dm[index]
        ///   A64: DUP Vd.2S, Vn.S[index]
        /// </summary>
        public static Vector64<uint> DuplicateSelectedScalarToVector64(Vector64<uint> value, byte index)=> DuplicateSelectedScalarToVector64(value, index);

        /// <summary>
        /// uint8x8_t vdup_laneq_u8 (uint8x16_t vec, const int lane)
        ///   A32: VDUP.8 Dd, Dm[index]
        ///   A64: DUP Vd.8B, Vn.B[index]
        /// </summary>
        public static Vector64<byte> DuplicateSelectedScalarToVector64(Vector128<byte> value, byte index)=> DuplicateSelectedScalarToVector64(value, index);

        /// <summary>
        /// int16x4_t vdup_laneq_s16 (int16x8_t vec, const int lane)
        ///   A32: VDUP.16 Dd, Dm[index]
        ///   A64: DUP Vd.4H, Vn.H[index]
        /// </summary>
        public static Vector64<short> DuplicateSelectedScalarToVector64(Vector128<short> value, byte index)=> DuplicateSelectedScalarToVector64(value, index);

        /// <summary>
        /// int32x2_t vdup_laneq_s32 (int32x4_t vec, const int lane)
        ///   A32: VDUP.32 Dd, Dm[index]
        ///   A64: DUP Vd.2S, Vn.S[index]
        /// </summary>
        public static Vector64<int> DuplicateSelectedScalarToVector64(Vector128<int> value, byte index)=> DuplicateSelectedScalarToVector64(value, index);

        /// <summary>
        /// float32x2_t vdup_laneq_f32 (float32x4_t vec, const int lane)
        ///   A32: VDUP.32 Dd, Dm[index]
        ///   A64: DUP Vd.2S, Vn.S[index]
        /// </summary>
        public static Vector64<float> DuplicateSelectedScalarToVector64(Vector128<float> value, byte index)=> DuplicateSelectedScalarToVector64(value, index);

        /// <summary>
        /// int8x8_t vdup_laneq_s8 (int8x16_t vec, const int lane)
        ///   A32: VDUP.8 Dd, Dm[index]
        ///   A64: DUP Vd.8B, Vn.B[index]
        /// </summary>
        public static Vector64<sbyte> DuplicateSelectedScalarToVector64(Vector128<sbyte> value, byte index)=> DuplicateSelectedScalarToVector64(value, index);

        /// <summary>
        /// uint16x4_t vdup_laneq_u16 (uint16x8_t vec, const int lane)
        ///   A32: VDUP.16 Dd, Dm[index]
        ///   A64: DUP Vd.4H, Vn.H[index]
        /// </summary>
        public static Vector64<ushort> DuplicateSelectedScalarToVector64(Vector128<ushort> value, byte index)=> DuplicateSelectedScalarToVector64(value, index);

        /// <summary>
        /// uint32x2_t vdup_laneq_u32 (uint32x4_t vec, const int lane)
        ///   A32: VDUP.32 Dd, Dm[index]
        ///   A64: DUP Vd.2S, Vn.S[index]
        /// </summary>
        public static Vector64<uint> DuplicateSelectedScalarToVector64(Vector128<uint> value, byte index)=> DuplicateSelectedScalarToVector64(value, index);

        /// <summary>
        /// uint8x16_t vdupq_lane_u8 (uint8x8_t vec, const int lane)
        ///   A32: VDUP.8 Qd, Dm[index]
        ///   A64: DUP Vd.16B, Vn.B[index]
        /// </summary>
        public static Vector128<byte> DuplicateSelectedScalarToVector128(Vector64<byte> value, byte index)=> DuplicateSelectedScalarToVector128(value, index);

        /// <summary>
        /// int16x8_t vdupq_lane_s16 (int16x4_t vec, const int lane)
        ///   A32: VDUP.16 Qd, Dm[index]
        ///   A64: DUP Vd.8H, Vn.H[index]
        /// </summary>
        public static Vector128<short> DuplicateSelectedScalarToVector128(Vector64<short> value, byte index)=> DuplicateSelectedScalarToVector128(value, index);

        /// <summary>
        /// int32x4_t vdupq_lane_s32 (int32x2_t vec, const int lane)
        ///   A32: VDUP.32 Qd, Dm[index]
        ///   A64: DUP Vd.4S, Vn.S[index]
        /// </summary>
        public static Vector128<int> DuplicateSelectedScalarToVector128(Vector64<int> value, byte index)=> DuplicateSelectedScalarToVector128(value, index);

        /// <summary>
        /// float32x4_t vdupq_lane_f32 (float32x2_t vec, const int lane)
        ///   A32: VDUP.32 Qd, Dm[index]
        ///   A64: DUP Vd.4S, Vn.S[index]
        /// </summary>
        public static Vector128<float> DuplicateSelectedScalarToVector128(Vector64<float> value, byte index)=> DuplicateSelectedScalarToVector128(value, index);

        /// <summary>
        /// int8x16_t vdupq_lane_s8 (int8x8_t vec, const int lane)
        ///   A32: VDUP.8 Qd, Dm[index]
        ///   A64: DUP Vd.16B, Vn.B[index]
        /// </summary>
        public static Vector128<sbyte> DuplicateSelectedScalarToVector128(Vector64<sbyte> value, byte index)=> DuplicateSelectedScalarToVector128(value, index);

        /// <summary>
        /// uint16x8_t vdupq_lane_u16 (uint16x4_t vec, const int lane)
        ///   A32: VDUP.16 Qd, Dm[index]
        ///   A64: DUP Vd.8H, Vn.H[index]
        /// </summary>
        public static Vector128<ushort> DuplicateSelectedScalarToVector128(Vector64<ushort> value, byte index)=> DuplicateSelectedScalarToVector128(value, index);

        /// <summary>
        /// uint32x4_t vdupq_lane_u32 (uint32x2_t vec, const int lane)
        ///   A32: VDUP.32 Qd, Dm[index]
        ///   A64: DUP Vd.4S, Vn.S[index]
        /// </summary>
        public static Vector128<uint> DuplicateSelectedScalarToVector128(Vector64<uint> value, byte index)=> DuplicateSelectedScalarToVector128(value, index);

        /// <summary>
        /// uint8x16_t vdupq_lane_u8 (uint8x16_t vec, const int lane)
        ///   A32: VDUP.8 Qd, Dm[index]
        ///   A64: DUP Vd.16B, Vn.B[index]
        /// </summary>
        public static Vector128<byte> DuplicateSelectedScalarToVector128(Vector128<byte> value, byte index)=> DuplicateSelectedScalarToVector128(value, index);

        /// <summary>
        /// int16x8_t vdupq_lane_s16 (int16x8_t vec, const int lane)
        ///   A32: VDUP.16 Qd, Dm[index]
        ///   A64: DUP Vd.8H, Vn.H[index]
        /// </summary>
        public static Vector128<short> DuplicateSelectedScalarToVector128(Vector128<short> value, byte index)=> DuplicateSelectedScalarToVector128(value, index);

        /// <summary>
        /// int32x4_t vdupq_lane_s32 (int32x4_t vec, const int lane)
        ///   A32: VDUP.32 Qd, Dm[index]
        ///   A64: DUP Vd.4S, Vn.S[index]
        /// </summary>
        public static Vector128<int> DuplicateSelectedScalarToVector128(Vector128<int> value, byte index)=> DuplicateSelectedScalarToVector128(value, index);

        /// <summary>
        /// float32x4_t vdupq_lane_f32 (float32x4_t vec, const int lane)
        ///   A32: VDUP.32 Qd, Dm[index]
        ///   A64: DUP Vd.4S, Vn.S[index]
        /// </summary>
        public static Vector128<float> DuplicateSelectedScalarToVector128(Vector128<float> value, byte index)=> DuplicateSelectedScalarToVector128(value, index);

        /// <summary>
        /// int8x16_t vdupq_lane_s8 (int8x16_t vec, const int lane)
        ///   A32: VDUP.8 Qd, Dm[index]
        ///   A64: DUP Vd.16B, Vn.B[index]
        /// </summary>
        public static Vector128<sbyte> DuplicateSelectedScalarToVector128(Vector128<sbyte> value, byte index)=> DuplicateSelectedScalarToVector128(value, index);

        /// <summary>
        /// uint16x8_t vdupq_lane_u16 (uint16x8_t vec, const int lane)
        ///   A32: VDUP.16 Qd, Dm[index]
        ///   A64: DUP Vd.8H, Vn.H[index]
        /// </summary>
        public static Vector128<ushort> DuplicateSelectedScalarToVector128(Vector128<ushort> value, byte index)=> DuplicateSelectedScalarToVector128(value, index);

        /// <summary>
        /// uint32x4_t vdupq_lane_u32 (uint32x4_t vec, const int lane)
        ///   A32: VDUP.32 Qd, Dm[index]
        ///   A64: DUP Vd.4S, Vn.S[index]
        /// </summary>
        public static Vector128<uint> DuplicateSelectedScalarToVector128(Vector128<uint> value, byte index)=> DuplicateSelectedScalarToVector128(value, index);

        /// <summary>
        /// uint8x8_t vdup_n_u8 (uint8_t value)
        ///   A32: VDUP.8 Dd, Rt
        ///   A64: DUP Vd.8B, Rn
        /// </summary>
        public static Vector64<byte> DuplicateToVector64(byte value) => DuplicateToVector64(value);

        /// <summary>
        /// int16x4_t vdup_n_s16 (int16_t value)
        ///   A32: VDUP.16 Dd, Rt
        ///   A64: DUP Vd.4H, Rn
        /// </summary>
        public static Vector64<short> DuplicateToVector64(short value) => DuplicateToVector64(value);

        /// <summary>
        /// int32x2_t vdup_n_s32 (int32_t value)
        ///   A32: VDUP.32 Dd, Rt
        ///   A64: DUP Vd.2S, Rn
        /// </summary>
        public static Vector64<int> DuplicateToVector64(int value) => DuplicateToVector64(value);

        /// <summary>
        /// int8x8_t vdup_n_s8 (int8_t value)
        ///   A32: VDUP.8 Dd, Rt
        ///   A64: DUP Vd.8B, Rn
        /// </summary>
        public static Vector64<sbyte> DuplicateToVector64(sbyte value) => DuplicateToVector64(value);

        /// <summary>
        /// float32x2_t vdup_n_f32 (float32_t value)
        ///   A32: VDUP Dd, Dm[0]
        ///   A64: DUP Vd.2S, Vn.S[0]
        /// </summary>
        public static Vector64<float> DuplicateToVector64(float value) => DuplicateToVector64(value);

        /// <summary>
        /// uint16x4_t vdup_n_u16 (uint16_t value)
        ///   A32: VDUP.16 Dd, Rt
        ///   A64: DUP Vd.4H, Rn
        /// </summary>
        public static Vector64<ushort> DuplicateToVector64(ushort value) => DuplicateToVector64(value);

        /// <summary>
        /// uint32x2_t vdup_n_u32 (uint32_t value)
        ///   A32: VDUP.32 Dd, Rt
        ///   A64: DUP Vd.2S, Rn
        /// </summary>
        public static Vector64<uint> DuplicateToVector64(uint value) => DuplicateToVector64(value);

        /// <summary>
        /// uint8x16_t vdupq_n_u8 (uint8_t value)
        ///   A32: VDUP.8 Qd, Rt
        ///   A64: DUP Vd.16B, Rn
        /// </summary>
        public static Vector128<byte> DuplicateToVector128(byte value) => DuplicateToVector128(value);

        /// <summary>
        /// int16x8_t vdupq_n_s16 (int16_t value)
        ///   A32: VDUP.16 Qd, Rt
        ///   A64: DUP Vd.8H, Rn
        /// </summary>
        public static Vector128<short> DuplicateToVector128(short value) => DuplicateToVector128(value);

        /// <summary>
        /// int32x4_t vdupq_n_s32 (int32_t value)
        ///   A32: VDUP.32 Qd, Rt
        ///   A64: DUP Vd.4S, Rn
        /// </summary>
        public static Vector128<int> DuplicateToVector128(int value) => DuplicateToVector128(value);

        /// <summary>
        /// int8x16_t vdupq_n_s8 (int8_t value)
        ///   A32: VDUP.8 Qd, Rt
        ///   A64: DUP Vd.16B, Rn
        /// </summary>
        public static Vector128<sbyte> DuplicateToVector128(sbyte value) => DuplicateToVector128(value);

        /// <summary>
        /// float32x4_t vdupq_n_f32 (float32_t value)
        ///   A32: VDUP Qd, Dm[0]
        ///   A64: DUP Vd.4S, Vn.S[0]
        /// </summary>
        public static Vector128<float> DuplicateToVector128(float value) => DuplicateToVector128(value);

        /// <summary>
        /// uint16x8_t vdupq_n_u16 (uint16_t value)
        ///   A32: VDUP.16 Qd, Rt
        ///   A64: DUP Vd.8H, Rn
        /// </summary>
        public static Vector128<ushort> DuplicateToVector128(ushort value) => DuplicateToVector128(value);

        /// <summary>
        /// uint32x4_t vdupq_n_u32 (uint32_t value)
        ///   A32: VDUP.32 Qd, Rt
        ///   A64: DUP Vd.4S, Rn
        /// </summary>
        public static Vector128<uint> DuplicateToVector128(uint value) => DuplicateToVector128(value);

        /// <summary>
        /// uint8_t vget_lane_u8 (uint8x8_t v, const int lane)
        ///   A32: VMOV.U8 Rt, Dn[lane]
        ///   A64: UMOV Wd, Vn.B[lane]
        /// </summary>
        public static byte Extract(Vector64<byte> vector, byte index) => Extract(vector, index);

        /// <summary>
        /// int16_t vget_lane_s16 (int16x4_t v, const int lane)
        ///   A32: VMOV.S16 Rt, Dn[lane]
        ///   A64: SMOV Wd, Vn.H[lane]
        /// </summary>
        public static short Extract(Vector64<short> vector, byte index) => Extract(vector, index);

        /// <summary>
        /// int32_t vget_lane_s32 (int32x2_t v, const int lane)
        ///   A32: VMOV.32 Rt, Dn[lane]
        ///   A64: SMOV Wd, Vn.S[lane]
        /// </summary>
        public static int Extract(Vector64<int> vector, byte index) => Extract(vector, index);

        /// <summary>
        /// int8_t vget_lane_s8 (int8x8_t v, const int lane)
        ///   A32: VMOV.S8 Rt, Dn[lane]
        ///   A64: SMOV Wd, Vn.B[lane]
        /// </summary>
        public static sbyte Extract(Vector64<sbyte> vector, byte index) => Extract(vector, index);

        /// <summary>
        /// float32_t vget_lane_f32 (float32x2_t v, const int lane)
        ///   A32: VMOV.F32 Sd, Sm
        ///   A64: DUP Sd, Vn.S[lane]
        /// </summary>
        public static float Extract(Vector64<float> vector, byte index) => Extract(vector, index);

        /// <summary>
        /// uint16_t vget_lane_u16 (uint16x4_t v, const int lane)
        ///   A32: VMOV.U16 Rt, Dn[lane]
        ///   A64: UMOV Wd, Vn.H[lane]
        /// </summary>
        public static ushort Extract(Vector64<ushort> vector, byte index) => Extract(vector, index);

        /// <summary>
        /// uint32_t vget_lane_u32 (uint32x2_t v, const int lane)
        ///   A32: VMOV.32 Rt, Dn[lane]
        ///   A64: UMOV Wd, Vn.S[lane]
        /// </summary>
        public static uint Extract(Vector64<uint> vector, byte index) => Extract(vector, index);

        /// <summary>
        /// uint8_t vgetq_lane_u8 (uint8x16_t v, const int lane)
        ///   A32: VMOV.U8 Rt, Dn[lane]
        ///   A64: UMOV Wd, Vn.B[lane]
        /// </summary>
        public static byte Extract(Vector128<byte> vector, byte index) => Extract(vector, index);

        /// <summary>
        /// float64_t vgetq_lane_f64 (float64x2_t v, const int lane)
        ///   A32: VMOV.F64 Dd, Dm
        ///   A64: DUP Dd, Vn.D[lane]
        /// </summary>
        public static double Extract(Vector128<double> vector, byte index) => Extract(vector, index);

        /// <summary>
        /// int16_t vgetq_lane_s16 (int16x8_t v, const int lane)
        ///   A32: VMOV.S16 Rt, Dn[lane]
        ///   A64: SMOV Wd, Vn.H[lane]
        /// </summary>
        public static short Extract(Vector128<short> vector, byte index) => Extract(vector, index);

        /// <summary>
        /// int32_t vgetq_lane_s32 (int32x4_t v, const int lane)
        ///   A32: VMOV.32 Rt, Dn[lane]
        ///   A64: SMOV Wd, Vn.S[lane]
        /// </summary>
        public static int Extract(Vector128<int> vector, byte index) => Extract(vector, index);

        /// <summary>
        /// int64_t vgetq_lane_s64 (int64x2_t v, const int lane)
        ///   A32: VMOV Rt, Rt2, Dm
        ///   A64: UMOV Xd, Vn.D[lane]
        /// </summary>
        public static long Extract(Vector128<long> vector, byte index) => Extract(vector, index);

        /// <summary>
        /// int8_t vgetq_lane_s8 (int8x16_t v, const int lane)
        ///   A32: VMOV.S8 Rt, Dn[lane]
        ///   A64: SMOV Wd, Vn.B[lane]
        /// </summary>
        public static sbyte Extract(Vector128<sbyte> vector, byte index) => Extract(vector, index);

        /// <summary>
        /// float32_t vgetq_lane_f32 (float32x4_t v, const int lane)
        ///   A32: VMOV.F32 Sd, Sm
        ///   A64: DUP Sd, Vn.S[lane]
        /// </summary>
        public static float Extract(Vector128<float> vector, byte index) => Extract(vector, index);

        /// <summary>
        /// uint16_t vgetq_lane_u16 (uint16x8_t v, const int lane)
        ///   A32: VMOV.U16 Rt, Dn[lane]
        ///   A64: UMOV Wd, Vn.H[lane]
        /// </summary>
        public static ushort Extract(Vector128<ushort> vector, byte index) => Extract(vector, index);

        /// <summary>
        /// uint32_t vgetq_lane_u32 (uint32x4_t v, const int lane)
        ///   A32: VMOV.32 Rt, Dn[lane]
        ///   A64: UMOV Wd, Vn.S[lane]
        /// </summary>
        public static uint Extract(Vector128<uint> vector, byte index) => Extract(vector, index);

        /// <summary>
        /// uint64_t vgetq_lane_u64 (uint64x2_t v, const int lane)
        ///   A32: VMOV Rt, Rt2, Dm
        ///   A64: UMOV Xd, Vn.D[lane]
        /// </summary>
        public static ulong Extract(Vector128<ulong> vector, byte index) => Extract(vector, index);

        /// <summary>
        ///  int8x16_t vmovn_high_s16 (int8x8_t r, int16x8_t a)
        ///   A32: VMOVN.I16 Dd+1, Qm
        ///   A64: XTN2 Vd.16B, Vn.8H
        /// </summary>
        public static Vector128<sbyte> ExtractNarrowingUpper(Vector64<sbyte> lower, Vector128<short> value) => ExtractNarrowingUpper(lower, value);

        /// <summary>
        ///  int16x8_t vmovn_high_s32 (int16x4_t r, int32x4_t a)
        ///   A32: VMOVN.I32 Dd+1, Qm
        ///   A64: XTN2 Vd.8H, Vn.4S
        /// </summary>
        public static Vector128<short> ExtractNarrowingUpper(Vector64<short> lower, Vector128<int> value) => ExtractNarrowingUpper(lower, value);

        /// <summary>
        ///  int32x4_t vmovn_high_s64 (int32x2_t r, int64x2_t a)
        ///   A32: VMOVN.I64 Dd+1, Qm
        ///   A64: XTN2 Vd.4S, Vn.2D
        /// </summary>
        public static Vector128<int> ExtractNarrowingUpper(Vector64<int> lower, Vector128<long> value) => ExtractNarrowingUpper(lower, value);

        /// <summary>
        ///  uint8x16_t vmovn_high_u16 (uint8x8_t r, uint16x8_t a)
        ///   A32: VMOVN.I16 Dd+1, Qm
        ///   A64: XTN2 Vd.16B, Vn.8H
        /// </summary>
        public static Vector128<byte> ExtractNarrowingUpper(Vector64<byte> lower, Vector128<ushort> value) => ExtractNarrowingUpper(lower, value);

        /// <summary>
        ///  uint16x8_t vmovn_high_u32 (uint16x4_t r, uint32x4_t a)
        ///   A32: VMOVN.I32 Dd+1, Qm
        ///   A64: XTN2 Vd.8H, Vn.4S
        /// </summary>
        public static Vector128<ushort> ExtractNarrowingUpper(Vector64<ushort> lower, Vector128<uint> value) => ExtractNarrowingUpper(lower, value);

        /// <summary>
        ///  uint32x4_t vmovn_high_u64 (uint32x2_t r, uint64x2_t a)
        ///   A32: VMOVN.I64 Dd+1, Qm
        ///   A64: XTN2 Vd.4S, Vn.2D
        /// </summary>
        public static Vector128<uint> ExtractNarrowingUpper(Vector64<uint> lower, Vector128<ulong> value) => ExtractNarrowingUpper(lower, value);

        /// <summary>
        ///  int8x8_t vmovn_s16 (int16x8_t a)
        ///   A32: VMOVN.I16 Dd, Qm
        ///   A64: XTN Vd.8B, Vn.8H
        /// </summary>
        public static Vector64<sbyte> ExtractNarrowingLower(Vector128<short> value) => ExtractNarrowingLower(value);

        /// <summary>
        ///  int16x4_t vmovn_s32 (int32x4_t a)
        ///   A32: VMOVN.I32 Dd, Qm
        ///   A64: XTN Vd.4H, Vn.4S
        /// </summary>
        public static Vector64<short> ExtractNarrowingLower(Vector128<int> value) => ExtractNarrowingLower(value);

        /// <summary>
        ///  int32x2_t vmovn_s64 (int64x2_t a)
        ///   A32: VMOVN.I64 Dd, Qm
        ///   A64: XTN Vd.2S, Vn.2D
        /// </summary>
        public static Vector64<int> ExtractNarrowingLower(Vector128<long> value) => ExtractNarrowingLower(value);

        /// <summary>
        ///  uint8x8_t vmovn_u16 (uint16x8_t a)
        ///   A32: VMOVN.I16 Dd, Qm
        ///   A64: XTN Vd.8B, Vn.8H
        /// </summary>
        public static Vector64<byte> ExtractNarrowingLower(Vector128<ushort> value) => ExtractNarrowingLower(value);

        /// <summary>
        ///  uint16x4_t vmovn_u32 (uint32x4_t a)
        ///   A32: VMOVN.I32 Dd, Qm
        ///   A64: XTN Vd.4H, Vn.4S
        /// </summary>
        public static Vector64<ushort> ExtractNarrowingLower(Vector128<uint> value) => ExtractNarrowingLower(value);

        /// <summary>
        ///  uint32x2_t vmovn_u64 (uint64x2_t a)
        ///   A32: VMOVN.I64 Dd, Qm
        ///   A64: XTN Vd.2S, Vn.2D
        /// </summary>
        public static Vector64<uint> ExtractNarrowingLower(Vector128<ulong> value) => ExtractNarrowingLower(value);

        /// <summary>
        /// uint8x8_t vext_s8 (uint8x8_t a, uint8x8_t b, const int n)
        ///   A32: VEXT.8 Dd, Dn, Dm, #n
        ///   A64: EXT Vd.8B, Vn.8B, Vm.8B, #n
        /// </summary>
        public static Vector64<byte> ExtractVector64(Vector64<byte> upper, Vector64<byte> lower, byte index) => ExtractVector64(upper, lower, index);

        /// <summary>
        /// int16x4_t vext_s16 (int16x4_t a, int16x4_t b, const int n)
        ///   A32: VEXT.8 Dd, Dn, Dm, #(n*2)
        ///   A64: EXT Vd.8B, Vn.8B, Vm.8B, #(n*2)
        /// </summary>
        public static Vector64<short> ExtractVector64(Vector64<short> upper, Vector64<short> lower, byte index) => ExtractVector64(upper, lower, index);

        /// <summary>
        /// int32x2_t vext_s32 (int32x2_t a, int32x2_t b, const int n)
        ///   A32: VEXT.8 Dd, Dn, Dm, #(n*4)
        ///   A64: EXT Vd.8B, Vn.8B, Vm.8B, #(n*4)
        /// </summary>
        public static Vector64<int> ExtractVector64(Vector64<int> upper, Vector64<int> lower, byte index) => ExtractVector64(upper, lower, index);

        /// <summary>
        /// int8x8_t vext_s8 (int8x8_t a, int8x8_t b, const int n)
        ///   A32: VEXT.8 Dd, Dn, Dm, #n
        ///   A64: EXT Vd.8B, Vn.8B, Vm.8B, #n
        /// </summary>
        public static Vector64<sbyte> ExtractVector64(Vector64<sbyte> upper, Vector64<sbyte> lower, byte index) => ExtractVector64(upper, lower, index);

        /// <summary>
        /// float32x2_t vext_f32 (float32x2_t a, float32x2_t b, const int n)
        ///   A32: VEXT.8 Dd, Dn, Dm, #(n*4)
        ///   A64: EXT Vd.8B, Vn.8B, Vm.8B, #(n*4)
        /// </summary>
        public static Vector64<float> ExtractVector64(Vector64<float> upper, Vector64<float> lower, byte index) => ExtractVector64(upper, lower, index);

        /// <summary>
        /// uint16x4_t vext_s16 (uint16x4_t a, uint16x4_t b, const int n)
        ///   A32: VEXT.8 Dd, Dn, Dm, #(n*2)
        ///   A64: EXT Vd.8B, Vn.8B, Vm.8B, #(n*2)
        /// </summary>
        public static Vector64<ushort> ExtractVector64(Vector64<ushort> upper, Vector64<ushort> lower, byte index) => ExtractVector64(upper, lower, index);

        /// <summary>
        /// uint32x2_t vext_s32 (uint32x2_t a, uint32x2_t b, const int n)
        ///   A32: VEXT.8 Dd, Dn, Dm, #(n*4)
        ///   A64: EXT Vd.8B, Vn.8B, Vm.8B, #(n*4)
        /// </summary>
        public static Vector64<uint> ExtractVector64(Vector64<uint> upper, Vector64<uint> lower, byte index) => ExtractVector64(upper, lower, index);

        /// <summary>
        /// uint8x16_t vextq_s8 (uint8x16_t a, uint8x16_t b, const int n)
        ///   A32: VEXT.8 Qd, Qn, Qm, #n
        ///   A64: EXT Vd.16B, Vn.16B, Vm.16B, #n
        /// </summary>
        public static Vector128<byte> ExtractVector128(Vector128<byte> upper, Vector128<byte> lower, byte index) => ExtractVector128(upper, lower, index);

        /// <summary>
        /// float64x2_t vextq_f64 (float64x2_t a, float64x2_t b, const int n)
        ///   A32: VEXT.8 Qd, Qn, Qm, #(n*8)
        ///   A64: EXT Vd.16B, Vn.16B, Vm.16B, #(n*8)
        /// </summary>
        public static Vector128<double> ExtractVector128(Vector128<double> upper, Vector128<double> lower, byte index) => ExtractVector128(upper, lower, index);

        /// <summary>
        /// int16x8_t vextq_s16 (int16x8_t a, int16x8_t b, const int n)
        ///   A32: VEXT.8 Qd, Qn, Qm, #(n*2)
        ///   A64: EXT Vd.16B, Vn.16B, Vm.16B, #(n*2)
        /// </summary>
        public static Vector128<short> ExtractVector128(Vector128<short> upper, Vector128<short> lower, byte index) => ExtractVector128(upper, lower, index);

        /// <summary>
        /// int32x4_t vextq_s32 (int32x4_t a, int32x4_t b, const int n)
        ///   A32: VEXT.8 Qd, Qn, Qm, #(n*4)
        ///   A64: EXT Vd.16B, Vn.16B, Vm.16B, #(n*4)
        /// </summary>
        public static Vector128<int> ExtractVector128(Vector128<int> upper, Vector128<int> lower, byte index) => ExtractVector128(upper, lower, index);

        /// <summary>
        /// int64x2_t vextq_s64 (int64x2_t a, int64x2_t b, const int n)
        ///   A32: VEXT.8 Qd, Qn, Qm, #(n*8)
        ///   A64: EXT Vd.16B, Vn.16B, Vm.16B, #(n*8)
        /// </summary>
        public static Vector128<long> ExtractVector128(Vector128<long> upper, Vector128<long> lower, byte index) => ExtractVector128(upper, lower, index);

        /// <summary>
        /// int8x16_t vextq_s8 (int8x16_t a, int8x16_t b, const int n)
        ///   A32: VEXT.8 Qd, Qn, Qm, #n
        ///   A64: EXT Vd.16B, Vn.16B, Vm.16B, #n
        /// </summary>
        public static Vector128<sbyte> ExtractVector128(Vector128<sbyte> upper, Vector128<sbyte> lower, byte index) => ExtractVector128(upper, lower, index);

        /// <summary>
        /// float32x4_t vextq_f32 (float32x4_t a, float32x4_t b, const int n)
        ///   A32: VEXT.8 Qd, Qn, Qm, #(n*4)
        ///   A64: EXT Vd.16B, Vn.16B, Vm.16B, #(n*4)
        /// </summary>
        public static Vector128<float> ExtractVector128(Vector128<float> upper, Vector128<float> lower, byte index) => ExtractVector128(upper, lower, index);

        /// <summary>
        /// uint16x8_t vextq_s16 (uint16x8_t a, uint16x8_t b, const int n)
        ///   A32: VEXT.8 Qd, Qn, Qm, #(n*2)
        ///   A64: EXT Vd.16B, Vn.16B, Vm.16B, #(n*2)
        /// </summary>
        public static Vector128<ushort> ExtractVector128(Vector128<ushort> upper, Vector128<ushort> lower, byte index) => ExtractVector128(upper, lower, index);

        /// <summary>
        /// uint32x4_t vextq_s32 (uint32x4_t a, uint32x4_t b, const int n)
        ///   A32: VEXT.8 Qd, Qn, Qm, #(n*4)
        ///   A64: EXT Vd.16B, Vn.16B, Vm.16B, #(n*4)
        /// </summary>
        public static Vector128<uint> ExtractVector128(Vector128<uint> upper, Vector128<uint> lower, byte index) => ExtractVector128(upper, lower, index);

        /// <summary>
        /// uint64x2_t vextq_s64 (uint64x2_t a, uint64x2_t b, const int n)
        ///   A32: VEXT.8 Qd, Qn, Qm, #(n*8)
        ///   A64: EXT Vd.16B, Vn.16B, Vm.16B, #(n*8)
        /// </summary>
        public static Vector128<ulong> ExtractVector128(Vector128<ulong> upper, Vector128<ulong> lower, byte index) => ExtractVector128(upper, lower, index);

        /// <summary>
        /// uint8x8_t vhadd_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VHADD.U8 Dd, Dn, Dm
        ///   A64: UHADD Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> FusedAddHalving(Vector64<byte> left, Vector64<byte> right) => FusedAddHalving(left, right);

        /// <summary>
        /// int16x4_t vhadd_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VHADD.S16 Dd, Dn, Dm
        ///   A64: SHADD Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> FusedAddHalving(Vector64<short> left, Vector64<short> right) => FusedAddHalving(left, right);

        /// <summary>
        /// int32x2_t vhadd_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VHADD.S32 Dd, Dn, Dm
        ///   A64: SHADD Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> FusedAddHalving(Vector64<int> left, Vector64<int> right) => FusedAddHalving(left, right);

        /// <summary>
        /// int8x8_t vhadd_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VHADD.S8 Dd, Dn, Dm
        ///   A64: SHADD Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> FusedAddHalving(Vector64<sbyte> left, Vector64<sbyte> right) => FusedAddHalving(left, right);

        /// <summary>
        /// uint16x4_t vhadd_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VHADD.U16 Dd, Dn, Dm
        ///   A64: UHADD Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> FusedAddHalving(Vector64<ushort> left, Vector64<ushort> right) => FusedAddHalving(left, right);

        /// <summary>
        /// uint32x2_t vhadd_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VHADD.U32 Dd, Dn, Dm
        ///   A64: UHADD Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> FusedAddHalving(Vector64<uint> left, Vector64<uint> right) => FusedAddHalving(left, right);

        /// <summary>
        /// uint8x16_t vhaddq_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VHADD.U8 Qd, Qn, Qm
        ///   A64: UHADD Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> FusedAddHalving(Vector128<byte> left, Vector128<byte> right) => FusedAddHalving(left, right);

        /// <summary>
        /// int16x8_t vhaddq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VHADD.S16 Qd, Qn, Qm
        ///   A64: SHADD Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> FusedAddHalving(Vector128<short> left, Vector128<short> right) => FusedAddHalving(left, right);

        /// <summary>
        /// int32x4_t vhaddq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VHADD.S32 Qd, Qn, Qm
        ///   A64: SHADD Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> FusedAddHalving(Vector128<int> left, Vector128<int> right) => FusedAddHalving(left, right);

        /// <summary>
        /// int8x16_t vhaddq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VHADD.S8 Qd, Qn, Qm
        ///   A64: SHADD Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> FusedAddHalving(Vector128<sbyte> left, Vector128<sbyte> right) => FusedAddHalving(left, right);

        /// <summary>
        /// uint16x8_t vhaddq_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VHADD.U16 Qd, Qn, Qm
        ///   A64: UHADD Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> FusedAddHalving(Vector128<ushort> left, Vector128<ushort> right) => FusedAddHalving(left, right);

        /// <summary>
        /// uint32x4_t vhaddq_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VHADD.U32 Qd, Qn, Qm
        ///   A64: UHADD Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> FusedAddHalving(Vector128<uint> left, Vector128<uint> right) => FusedAddHalving(left, right);

        /// <summary>
        /// uint8x8_t vrhadd_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VRHADD.U8 Dd, Dn, Dm
        ///   A64: URHADD Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> FusedAddRoundedHalving(Vector64<byte> left, Vector64<byte> right) => FusedAddRoundedHalving(left, right);

        /// <summary>
        /// int16x4_t vrhadd_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VRHADD.S16 Dd, Dn, Dm
        ///   A64: SRHADD Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> FusedAddRoundedHalving(Vector64<short> left, Vector64<short> right) => FusedAddRoundedHalving(left, right);

        /// <summary>
        /// int32x2_t vrhadd_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VRHADD.S32 Dd, Dn, Dm
        ///   A64: SRHADD Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> FusedAddRoundedHalving(Vector64<int> left, Vector64<int> right) => FusedAddRoundedHalving(left, right);

        /// <summary>
        /// int8x8_t vrhadd_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VRHADD.S8 Dd, Dn, Dm
        ///   A64: SRHADD Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> FusedAddRoundedHalving(Vector64<sbyte> left, Vector64<sbyte> right) => FusedAddRoundedHalving(left, right);

        /// <summary>
        /// uint16x4_t vrhadd_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VRHADD.U16 Dd, Dn, Dm
        ///   A64: URHADD Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> FusedAddRoundedHalving(Vector64<ushort> left, Vector64<ushort> right) => FusedAddRoundedHalving(left, right);

        /// <summary>
        /// uint32x2_t vrhadd_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VRHADD.U32 Dd, Dn, Dm
        ///   A64: URHADD Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> FusedAddRoundedHalving(Vector64<uint> left, Vector64<uint> right) => FusedAddRoundedHalving(left, right);

        /// <summary>
        /// uint8x16_t vrhaddq_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VRHADD.U8 Qd, Qn, Qm
        ///   A64: URHADD Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> FusedAddRoundedHalving(Vector128<byte> left, Vector128<byte> right) => FusedAddRoundedHalving(left, right);

        /// <summary>
        /// int16x8_t vrhaddq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VRHADD.S16 Qd, Qn, Qm
        ///   A64: SRHADD Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> FusedAddRoundedHalving(Vector128<short> left, Vector128<short> right) => FusedAddRoundedHalving(left, right);

        /// <summary>
        /// int32x4_t vrhaddq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VRHADD.S32 Qd, Qn, Qm
        ///   A64: SRHADD Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> FusedAddRoundedHalving(Vector128<int> left, Vector128<int> right) => FusedAddRoundedHalving(left, right);

        /// <summary>
        /// int8x16_t vrhaddq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VRHADD.S8 Qd, Qn, Qm
        ///   A64: SRHADD Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> FusedAddRoundedHalving(Vector128<sbyte> left, Vector128<sbyte> right) => FusedAddRoundedHalving(left, right);

        /// <summary>
        /// uint16x8_t vrhaddq_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VRHADD.U16 Qd, Qn, Qm
        ///   A64: URHADD Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> FusedAddRoundedHalving(Vector128<ushort> left, Vector128<ushort> right) => FusedAddRoundedHalving(left, right);

        /// <summary>
        /// uint32x4_t vrhaddq_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VRHADD.U32 Qd, Qn, Qm
        ///   A64: URHADD Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> FusedAddRoundedHalving(Vector128<uint> left, Vector128<uint> right) => FusedAddRoundedHalving(left, right);

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
        /// uint8x8_t vhsub_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VHSUB.U8 Dd, Dn, Dm
        ///   A64: UHSUB Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> FusedSubtractHalving(Vector64<byte> left, Vector64<byte> right) => FusedSubtractHalving(left, right);

        /// <summary>
        /// int16x4_t vhsub_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VHSUB.S16 Dd, Dn, Dm
        ///   A64: SHSUB Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> FusedSubtractHalving(Vector64<short> left, Vector64<short> right) => FusedSubtractHalving(left, right);

        /// <summary>
        /// int32x2_t vhsub_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VHSUB.S32 Dd, Dn, Dm
        ///   A64: SHSUB Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> FusedSubtractHalving(Vector64<int> left, Vector64<int> right) => FusedSubtractHalving(left, right);

        /// <summary>
        /// int8x8_t vhsub_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VHSUB.S8 Dd, Dn, Dm
        ///   A64: SHSUB Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> FusedSubtractHalving(Vector64<sbyte> left, Vector64<sbyte> right) => FusedSubtractHalving(left, right);

        /// <summary>
        /// uint16x4_t vhsub_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VHSUB.U16 Dd, Dn, Dm
        ///   A64: UHSUB Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> FusedSubtractHalving(Vector64<ushort> left, Vector64<ushort> right) => FusedSubtractHalving(left, right);

        /// <summary>
        /// uint32x2_t vhsub_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VHSUB.U32 Dd, Dn, Dm
        ///   A64: UHSUB Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> FusedSubtractHalving(Vector64<uint> left, Vector64<uint> right) => FusedSubtractHalving(left, right);

        /// <summary>
        /// uint8x16_t vhsubq_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VHSUB.U8 Qd, Qn, Qm
        ///   A64: UHSUB Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> FusedSubtractHalving(Vector128<byte> left, Vector128<byte> right) => FusedSubtractHalving(left, right);

        /// <summary>
        /// int16x8_t vhsubq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VHSUB.S16 Qd, Qn, Qm
        ///   A64: SHSUB Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> FusedSubtractHalving(Vector128<short> left, Vector128<short> right) => FusedSubtractHalving(left, right);

        /// <summary>
        /// int32x4_t vhsubq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VHSUB.S32 Qd, Qn, Qm
        ///   A64: SHSUB Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> FusedSubtractHalving(Vector128<int> left, Vector128<int> right) => FusedSubtractHalving(left, right);

        /// <summary>
        /// int8x16_t vhsubq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VHSUB.S8 Qd, Qn, Qm
        ///   A64: SHSUB Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> FusedSubtractHalving(Vector128<sbyte> left, Vector128<sbyte> right) => FusedSubtractHalving(left, right);

        /// <summary>
        /// uint16x8_t vhsubq_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VHSUB.U16 Qd, Qn, Qm
        ///   A64: UHSUB Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> FusedSubtractHalving(Vector128<ushort> left, Vector128<ushort> right) => FusedSubtractHalving(left, right);

        /// <summary>
        /// uint32x4_t vhsubq_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VHSUB.U32 Qd, Qn, Qm
        ///   A64: UHSUB Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> FusedSubtractHalving(Vector128<uint> left, Vector128<uint> right) => FusedSubtractHalving(left, right);

        /// <summary>
        /// uint8x8_t vset_lane_u8 (uint8_t a, uint8x8_t v, const int lane)
        ///   A32: VMOV.8 Dd[lane], Rt
        ///   A64: INS Vd.B[lane], Wn
        /// </summary>
        public static Vector64<byte> Insert(Vector64<byte> vector, byte index, byte data) => Insert(vector, index, data);

        /// <summary>
        /// int16x4_t vset_lane_s16 (int16_t a, int16x4_t v, const int lane)
        ///   A32: VMOV.16 Dd[lane], Rt
        ///   A64: INS Vd.H[lane], Wn
        /// </summary>
        public static Vector64<short> Insert(Vector64<short> vector, byte index, short data) => Insert(vector, index, data);

        /// <summary>
        /// int32x2_t vset_lane_s32 (int32_t a, int32x2_t v, const int lane)
        ///   A32: VMOV.32 Dd[lane], Rt
        ///   A64: INS Vd.S[lane], Wn
        /// </summary>
        public static Vector64<int> Insert(Vector64<int> vector, byte index, int data) => Insert(vector, index, data);

        /// <summary>
        /// int8x8_t vset_lane_s8 (int8_t a, int8x8_t v, const int lane)
        ///   A32: VMOV.8 Dd[lane], Rt
        ///   A64: INS Vd.B[lane], Wn
        /// </summary>
        public static Vector64<sbyte> Insert(Vector64<sbyte> vector, byte index, sbyte data) => Insert(vector, index, data);

        /// <summary>
        /// float32x2_t vset_lane_f32 (float32_t a, float32x2_t v, const int lane)
        ///   A32: VMOV.F32 Sd, Sm
        ///   A64: INS Vd.S[lane], Vn.S[0]
        /// </summary>
        public static Vector64<float> Insert(Vector64<float> vector, byte index, float data) => Insert(vector, index, data);

        /// <summary>
        /// uint16x4_t vset_lane_u16 (uint16_t a, uint16x4_t v, const int lane)
        ///   A32: VMOV.16 Dd[lane], Rt
        ///   A64: INS Vd.H[lane], Wn
        /// </summary>
        public static Vector64<ushort> Insert(Vector64<ushort> vector, byte index, ushort data) => Insert(vector, index, data);

        /// <summary>
        /// uint32x2_t vset_lane_u32 (uint32_t a, uint32x2_t v, const int lane)
        ///   A32: VMOV.32 Dd[lane], Rt
        ///   A64: INS Vd.S[lane], Wn
        /// </summary>
        public static Vector64<uint> Insert(Vector64<uint> vector, byte index, uint data) => Insert(vector, index, data);

        /// <summary>
        /// uint8x16_t vsetq_lane_u8 (uint8_t a, uint8x16_t v, const int lane)
        ///   A32: VMOV.8 Dd[lane], Rt
        ///   A64: INS Vd.B[lane], Wn
        /// </summary>
        public static Vector128<byte> Insert(Vector128<byte> vector, byte index, byte data) => Insert(vector, index, data);

        /// <summary>
        /// float64x2_t vsetq_lane_f64 (float64_t a, float64x2_t v, const int lane)
        ///   A32: VMOV.F64 Dd, Dm
        ///   A64: INS Vd.D[lane], Vn.D[0]
        /// </summary>
        public static Vector128<double> Insert(Vector128<double> vector, byte index, double data) => Insert(vector, index, data);

        /// <summary>
        /// int16x8_t vsetq_lane_s16 (int16_t a, int16x8_t v, const int lane)
        ///   A32: VMOV.16 Dd[lane], Rt
        ///   A64: INS Vd.H[lane], Wn
        /// </summary>
        public static Vector128<short> Insert(Vector128<short> vector, byte index, short data) => Insert(vector, index, data);

        /// <summary>
        /// int32x4_t vsetq_lane_s32 (int32_t a, int32x4_t v, const int lane)
        ///   A32: VMOV.32 Dd[lane], Rt
        ///   A64: INS Vd.S[lane], Wn
        /// </summary>
        public static Vector128<int> Insert(Vector128<int> vector, byte index, int data) => Insert(vector, index, data);

        /// <summary>
        /// int64x2_t vsetq_lane_s64 (int64_t a, int64x2_t v, const int lane)
        ///   A32: VMOV.64 Dd, Rt, Rt2
        ///   A64: INS Vd.D[lane], Xn
        /// </summary>
        public static Vector128<long> Insert(Vector128<long> vector, byte index, long data) => Insert(vector, index, data);

        /// <summary>
        /// int8x16_t vsetq_lane_s8 (int8_t a, int8x16_t v, const int lane)
        ///   A32: VMOV.8 Dd[lane], Rt
        ///   A64: INS Vd.B[lane], Wn
        /// </summary>
        public static Vector128<sbyte> Insert(Vector128<sbyte> vector, byte index, sbyte data) => Insert(vector, index, data);

        /// <summary>
        /// float32x4_t vsetq_lane_f32 (float32_t a, float32x4_t v, const int lane)
        ///   A32: VMOV.F32 Sd, Sm
        ///   A64: INS Vd.S[lane], Vn.S[0]
        /// </summary>
        public static Vector128<float> Insert(Vector128<float> vector, byte index, float data) => Insert(vector, index, data);

        /// <summary>
        /// uint16x8_t vsetq_lane_u16 (uint16_t a, uint16x8_t v, const int lane)
        ///   A32: VMOV.16 Dd[lane], Rt
        ///   A64: INS Vd.H[lane], Wn
        /// </summary>
        public static Vector128<ushort> Insert(Vector128<ushort> vector, byte index, ushort data) => Insert(vector, index, data);

        /// <summary>
        /// uint32x4_t vsetq_lane_u32 (uint32_t a, uint32x4_t v, const int lane)
        ///   A32: VMOV.32 Dd[lane], Rt
        ///   A64: INS Vd.S[lane], Wn
        /// </summary>
        public static Vector128<uint> Insert(Vector128<uint> vector, byte index, uint data) => Insert(vector, index, data);

        /// <summary>
        /// uint64x2_t vsetq_lane_u64 (uint64_t a, uint64x2_t v, const int lane)
        ///   A32: VMOV.64 Dd, Rt, Rt2
        ///   A64: INS Vd.D[lane], Xn
        /// </summary>
        public static Vector128<ulong> Insert(Vector128<ulong> vector, byte index, ulong data) => Insert(vector, index, data);

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
        /// uint16x8_t vmull_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VMULL.U8 Qd, Dn, Dm
        ///   A64: UMULL Vd.8H, Vn.8B, Vm.8B
        /// </summary>
        public static Vector128<ushort> MultiplyWideningLower(Vector64<byte> left, Vector64<byte> right) => MultiplyWideningLower(left, right);

        /// <summary>
        /// int32x4_t vmull_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VMULL.S16 Qd, Dn, Dm
        ///   A64: SMULL Vd.4S, Vn.4H, Vm.4H
        /// </summary>
        public static Vector128<int> MultiplyWideningLower(Vector64<short> left, Vector64<short> right) => MultiplyWideningLower(left, right);

        /// <summary>
        /// int64x2_t vmull_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VMULL.S32 Qd, Dn, Dm
        ///   A64: SMULL Vd.2D, Vn.2S, Vm.2S
        /// </summary>
        public static Vector128<long> MultiplyWideningLower(Vector64<int> left, Vector64<int> right) => MultiplyWideningLower(left, right);

        /// <summary>
        /// int16x8_t vmull_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VMULL.S8 Qd, Dn, Dm
        ///   A64: SMULL Vd.8H, Vn.8B, Vm.8B
        /// </summary>
        public static Vector128<short> MultiplyWideningLower(Vector64<sbyte> left, Vector64<sbyte> right) => MultiplyWideningLower(left, right);

        /// <summary>
        /// uint32x4_t vmull_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VMULL.U16 Qd, Dn, Dm
        ///   A64: UMULL Vd.4S, Vn.4H, Vm.4H
        /// </summary>
        public static Vector128<uint> MultiplyWideningLower(Vector64<ushort> left, Vector64<ushort> right) => MultiplyWideningLower(left, right);

        /// <summary>
        /// uint64x2_t vmull_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VMULL.U32 Qd, Dn, Dm
        ///   A64: UMULL Vd.2D, Vn.2S, Vm.2S
        /// </summary>
        public static Vector128<ulong> MultiplyWideningLower(Vector64<uint> left, Vector64<uint> right) => MultiplyWideningLower(left, right);

        /// <summary>
        /// uint16x8_t vmlal_u8 (uint16x8_t a, uint8x8_t b, uint8x8_t c)
        ///   A32: VMLAL.U8 Qd, Dn, Dm
        ///   A64: UMLAL Vd.8H, Vn.8B, Vm.8B
        /// </summary>
        public static Vector128<ushort> MultiplyWideningLowerAndAdd(Vector128<ushort> addend, Vector64<byte> left, Vector64<byte> right) => MultiplyWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// int32x4_t vmlal_s16 (int32x4_t a, int16x4_t b, int16x4_t c)
        ///   A32: VMLAL.S16 Qd, Dn, Dm
        ///   A64: SMLAL Vd.4S, Vn.4H, Vm.4H
        /// </summary>
        public static Vector128<int> MultiplyWideningLowerAndAdd(Vector128<int> addend, Vector64<short> left, Vector64<short> right) => MultiplyWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// int64x2_t vmlal_s32 (int64x2_t a, int32x2_t b, int32x2_t c)
        ///   A32: VMLAL.S32 Qd, Dn, Dm
        ///   A64: SMLAL Vd.2D, Vn.2S, Vm.2S
        /// </summary>
        public static Vector128<long> MultiplyWideningLowerAndAdd(Vector128<long> addend, Vector64<int> left, Vector64<int> right) => MultiplyWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// int16x8_t vmlal_s8 (int16x8_t a, int8x8_t b, int8x8_t c)
        ///   A32: VMLAL.S8 Qd, Dn, Dm
        ///   A64: SMLAL Vd.8H, Vn.8B, Vm.8B
        /// </summary>
        public static Vector128<short> MultiplyWideningLowerAndAdd(Vector128<short> addend, Vector64<sbyte> left, Vector64<sbyte> right) => MultiplyWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// uint32x4_t vmlal_u16 (uint32x4_t a, uint16x4_t b, uint16x4_t c)
        ///   A32: VMLAL.U16 Qd, Dn, Dm
        ///   A64: UMLAL Vd.4S, Vn.4H, Vm.4H
        /// </summary>
        public static Vector128<uint> MultiplyWideningLowerAndAdd(Vector128<uint> addend, Vector64<ushort> left, Vector64<ushort> right) => MultiplyWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// uint64x2_t vmlal_u32 (uint64x2_t a, uint32x2_t b, uint32x2_t c)
        ///   A32: VMLAL.U32 Qd, Dn, Dm
        ///   A64: UMLAL Vd.2D, Vn.2S, Vm.2S
        /// </summary>
        public static Vector128<ulong> MultiplyWideningLowerAndAdd(Vector128<ulong> addend, Vector64<uint> left, Vector64<uint> right) => MultiplyWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// uint16x8_t vmlsl_u8 (uint16x8_t a, uint8x8_t b, uint8x8_t c)
        ///   A32: VMLSL.U8 Qd, Dn, Dm
        ///   A64: UMLSL Vd.8H, Vn.8B, Vm.8B
        /// </summary>
        public static Vector128<ushort> MultiplyWideningLowerAndSubtract(Vector128<ushort> minuend, Vector64<byte> left, Vector64<byte> right) => MultiplyWideningLowerAndSubtract(minuend, left, right);

        /// <summary>
        /// int32x4_t vmlsl_s16 (int32x4_t a, int16x4_t b, int16x4_t c)
        ///   A32: VMLSL.S16 Qd, Dn, Dm
        ///   A64: SMLSL Vd.4S, Vn.4H, Vm.4H
        /// </summary>
        public static Vector128<int> MultiplyWideningLowerAndSubtract(Vector128<int> minuend, Vector64<short> left, Vector64<short> right) => MultiplyWideningLowerAndSubtract(minuend, left, right);

        /// <summary>
        /// int64x2_t vmlsl_s32 (int64x2_t a, int32x2_t b, int32x2_t c)
        ///   A32: VMLSL.S32 Qd, Dn, Dm
        ///   A64: SMLSL Vd.2D, Vn.2S, Vm.2S
        /// </summary>
        public static Vector128<long> MultiplyWideningLowerAndSubtract(Vector128<long> minuend, Vector64<int> left, Vector64<int> right) => MultiplyWideningLowerAndSubtract(minuend, left, right);

        /// <summary>
        /// int16x8_t vmlsl_s8 (int16x8_t a, int8x8_t b, int8x8_t c)
        ///   A32: VMLSL.S8 Qd, Dn, Dm
        ///   A64: SMLSL Vd.8H, Vn.8B, Vm.8B
        /// </summary>
        public static Vector128<short> MultiplyWideningLowerAndSubtract(Vector128<short> minuend, Vector64<sbyte> left, Vector64<sbyte> right) => MultiplyWideningLowerAndSubtract(minuend, left, right);

        /// <summary>
        /// uint32x4_t vmlsl_u16 (uint32x4_t a, uint16x4_t b, uint16x4_t c)
        ///   A32: VMLSL.U16 Qd, Dn, Dm
        ///   A64: UMLSL Vd.4S, Vn.4H, Vm.4H
        /// </summary>
        public static Vector128<uint> MultiplyWideningLowerAndSubtract(Vector128<uint> minuend, Vector64<ushort> left, Vector64<ushort> right) => MultiplyWideningLowerAndSubtract(minuend, left, right);

        /// <summary>
        /// uint64x2_t vmlsl_u32 (uint64x2_t a, uint32x2_t b, uint32x2_t c)
        ///   A32: VMLSL.U32 Qd, Dn, Dm
        ///   A64: UMLSL Vd.2D, Vn.2S, Vm.2S
        /// </summary>
        public static Vector128<ulong> MultiplyWideningLowerAndSubtract(Vector128<ulong> minuend, Vector64<uint> left, Vector64<uint> right) => MultiplyWideningLowerAndSubtract(minuend, left, right);

        /// <summary>
        /// uint16x8_t vmull_high_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VMULL.U8 Qd, Dn+1, Dm+1
        ///   A64: UMULL2 Vd.8H, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<ushort> MultiplyWideningUpper(Vector128<byte> left, Vector128<byte> right) => MultiplyWideningUpper(left, right);

        /// <summary>
        /// int32x4_t vmull_high_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VMULL.S16 Qd, Dn+1, Dm+1
        ///   A64: SMULL2 Vd.4S, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<int> MultiplyWideningUpper(Vector128<short> left, Vector128<short> right) => MultiplyWideningUpper(left, right);

        /// <summary>
        /// int64x2_t vmull_high_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VMULL.S32 Qd, Dn+1, Dm+1
        ///   A64: SMULL2 Vd.2D, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<long> MultiplyWideningUpper(Vector128<int> left, Vector128<int> right) => MultiplyWideningUpper(left, right);

        /// <summary>
        /// int16x8_t vmull_high_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VMULL.S8 Qd, Dn+1, Dm+1
        ///   A64: SMULL2 Vd.8H, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<short> MultiplyWideningUpper(Vector128<sbyte> left, Vector128<sbyte> right) => MultiplyWideningUpper(left, right);

        /// <summary>
        /// uint32x4_t vmull_high_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VMULL.U16 Qd, Dn+1, Dm+1
        ///   A64: UMULL2 Vd.4S, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<uint> MultiplyWideningUpper(Vector128<ushort> left, Vector128<ushort> right) => MultiplyWideningUpper(left, right);

        /// <summary>
        /// uint64x2_t vmull_high_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VMULL.U32 Qd, Dn+1, Dm+1
        ///   A64: UMULL2 Vd.2D, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<ulong> MultiplyWideningUpper(Vector128<uint> left, Vector128<uint> right) => MultiplyWideningUpper(left, right);

        /// <summary>
        /// uint16x8_t vmlal_high_u8 (uint16x8_t a, uint8x16_t b, uint8x16_t c)
        ///   A32: VMLAL.U8 Qd, Dn+1, Dm+1
        ///   A64: UMLAL2 Vd.8H, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<ushort> MultiplyWideningUpperAndAdd(Vector128<ushort> addend, Vector128<byte> left, Vector128<byte> right) => MultiplyWideningUpperAndAdd(addend, left, right);

        /// <summary>
        /// int32x4_t vmlal_high_s16 (int32x4_t a, int16x8_t b, int16x8_t c)
        ///   A32: VMLAL.S16 Qd, Dn+1, Dm+1
        ///   A64: SMLAL2 Vd.4S, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<int> MultiplyWideningUpperAndAdd(Vector128<int> addend, Vector128<short> left, Vector128<short> right) => MultiplyWideningUpperAndAdd(addend, left, right);

        /// <summary>
        /// int64x2_t vmlal_high_s32 (int64x2_t a, int32x4_t b, int32x4_t c)
        ///   A32: VMLAL.S32 Qd, Dn+1, Dm+1
        ///   A64: SMLAL2 Vd.2D, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<long> MultiplyWideningUpperAndAdd(Vector128<long> addend, Vector128<int> left, Vector128<int> right) => MultiplyWideningUpperAndAdd(addend, left, right);

        /// <summary>
        /// int16x8_t vmlal_high_s8 (int16x8_t a, int8x16_t b, int8x16_t c)
        ///   A32: VMLAL.S8 Qd, Dn+1, Dm+1
        ///   A64: SMLAL2 Vd.8H, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<short> MultiplyWideningUpperAndAdd(Vector128<short> addend, Vector128<sbyte> left, Vector128<sbyte> right) => MultiplyWideningUpperAndAdd(addend, left, right);

        /// <summary>
        /// uint32x4_t vmlal_high_u16 (uint32x4_t a, uint16x8_t b, uint16x8_t c)
        ///   A32: VMLAL.U16 Qd, Dn+1, Dm+1
        ///   A64: UMLAL2 Vd.4S, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<uint> MultiplyWideningUpperAndAdd(Vector128<uint> addend, Vector128<ushort> left, Vector128<ushort> right) => MultiplyWideningUpperAndAdd(addend, left, right);

        /// <summary>
        /// uint64x2_t vmlal_high_u32 (uint64x2_t a, uint32x4_t b, uint32x4_t c)
        ///   A32: VMLAL.U32 Qd, Dn+1, Dm+1
        ///   A64: UMLAL2 Vd.2D, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<ulong> MultiplyWideningUpperAndAdd(Vector128<ulong> addend, Vector128<uint> left, Vector128<uint> right) => MultiplyWideningUpperAndAdd(addend, left, right);

        /// <summary>
        /// uint16x8_t vmlsl_high_u8 (uint16x8_t a, uint8x16_t b, uint8x16_t c)
        ///   A32: VMLSL.U8 Qd, Dn+1, Dm+1
        ///   A64: UMLSL2 Vd.8H, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<ushort> MultiplyWideningUpperAndSubtract(Vector128<ushort> minuend, Vector128<byte> left, Vector128<byte> right) => MultiplyWideningUpperAndSubtract(minuend, left, right);

        /// <summary>
        /// int32x4_t vmlsl_high_s16 (int32x4_t a, int16x8_t b, int16x8_t c)
        ///   A32: VMLSL.S16 Qd, Dn+1, Dm+1
        ///   A64: SMLSL2 Vd.4S, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<int> MultiplyWideningUpperAndSubtract(Vector128<int> minuend, Vector128<short> left, Vector128<short> right) => MultiplyWideningUpperAndSubtract(minuend, left, right);

        /// <summary>
        /// int64x2_t vmlsl_high_s32 (int64x2_t a, int32x4_t b, int32x4_t c)
        ///   A32: VMLSL.S32 Qd, Dn+1, Dm+1
        ///   A64: SMLSL2 Vd.2D, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<long> MultiplyWideningUpperAndSubtract(Vector128<long> minuend, Vector128<int> left, Vector128<int> right) => MultiplyWideningUpperAndSubtract(minuend, left, right);

        /// <summary>
        /// int16x8_t vmlsl_high_s8 (int16x8_t a, int8x16_t b, int8x16_t c)
        ///   A32: VMLSL.S8 Qd, Dn+1, Dm+1
        ///   A64: SMLSL2 Vd.8H, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<short> MultiplyWideningUpperAndSubtract(Vector128<short> minuend, Vector128<sbyte> left, Vector128<sbyte> right) => MultiplyWideningUpperAndSubtract(minuend, left, right);

        /// <summary>
        /// uint32x4_t vmlsl_high_u16 (uint32x4_t a, uint16x8_t b, uint16x8_t c)
        ///   A32: VMLSL.U16 Qd, Dn+1, Dm+1
        ///   A64: UMLSL2 Vd.4S, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<uint> MultiplyWideningUpperAndSubtract(Vector128<uint> minuend, Vector128<ushort> left, Vector128<ushort> right) => MultiplyWideningUpperAndSubtract(minuend, left, right);

        /// <summary>
        /// uint64x2_t vmlsl_high_u32 (uint64x2_t a, uint32x4_t b, uint32x4_t c)
        ///   A32: VMLSL.U32 Qd, Dn+1, Dm+1
        ///   A64: UMLSL2 Vd.2D, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<ulong> MultiplyWideningUpperAndSubtract(Vector128<ulong> minuend, Vector128<uint> left, Vector128<uint> right) => MultiplyWideningUpperAndSubtract(minuend, left, right);

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
        /// poly8x8_t vmul_p8 (poly8x8_t a, poly8x8_t b)
        ///   A32: VMUL.P8 Dd, Dn, Dm
        ///   A64: PMUL Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> PolynomialMultiply(Vector64<byte> left, Vector64<byte> right) => PolynomialMultiply(left, right);

        /// <summary>
        /// poly8x8_t vmul_p8 (poly8x8_t a, poly8x8_t b)
        ///   A32: VMUL.P8 Dd, Dn, Dm
        ///   A64: PMUL Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> PolynomialMultiply(Vector64<sbyte> left, Vector64<sbyte> right) => PolynomialMultiply(left, right);

        /// <summary>
        /// poly8x16_t vmulq_p8 (poly8x16_t a, poly8x16_t b)
        ///   A32: VMUL.P8 Qd, Qn, Qm
        ///   A64: PMUL Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> PolynomialMultiply(Vector128<byte> left, Vector128<byte> right) => PolynomialMultiply(left, right);

        /// <summary>
        /// poly8x16_t vmulq_p8 (poly8x16_t a, poly8x16_t b)
        ///   A32: VMUL.P8 Qd, Qn, Qm
        ///   A64: PMUL Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> PolynomialMultiply(Vector128<sbyte> left, Vector128<sbyte> right) => PolynomialMultiply(left, right);

        /// <summary>
        /// poly16x8_t vmull_p8 (poly8x8_t a, poly8x8_t b)
        ///   A32: VMULL.P8 Qd, Dn, Dm
        ///   A64: PMULL Vd.16B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector128<ushort> PolynomialMultiplyWideningLower(Vector64<byte> left, Vector64<byte> right) => PolynomialMultiplyWideningLower(left, right);

        /// <summary>
        /// poly16x8_t vmull_p8 (poly8x8_t a, poly8x8_t b)
        ///   A32: VMULL.P8 Qd, Dn, Dm
        ///   A64: PMULL Vd.16B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector128<short> PolynomialMultiplyWideningLower(Vector64<sbyte> left, Vector64<sbyte> right) => PolynomialMultiplyWideningLower(left, right);

        /// <summary>
        /// poly16x8_t vmull_high_p8 (poly8x16_t a, poly8x16_t b)
        ///   A32: VMULL.P8 Qd, Dn+1, Dm+1
        ///   A64: PMULL2 Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<ushort> PolynomialMultiplyWideningUpper(Vector128<byte> left, Vector128<byte> right) => PolynomialMultiplyWideningUpper(left, right);

        /// <summary>
        /// poly16x8_t vmull_high_p8 (poly8x16_t a, poly8x16_t b)
        ///   A32: VMULL.P8 Qd, Dn+1, Dm+1
        ///   A64: PMULL2 Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<short> PolynomialMultiplyWideningUpper(Vector128<sbyte> left, Vector128<sbyte> right) => PolynomialMultiplyWideningUpper(left, right);

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
        /// float32x2_t vrecpe_f32 (float32x2_t a)
        ///   A32: VRECPE.F32 Dd, Dm
        ///   A64: FRECPE Vd.2S, Vn.2S
        /// </summary>
        public static Vector64<float> ReciprocalEstimate(Vector64<float> value) => ReciprocalEstimate(value);

        /// <summary>
        /// uint32x2_t vrecpe_u32 (uint32x2_t a)
        ///   A32: VRECPE.U32 Dd, Dm
        ///   A64: URECPE Vd.2S, Vn.2S
        /// </summary>
        public static Vector64<uint> ReciprocalEstimate(Vector64<uint> value) => ReciprocalEstimate(value);

        /// <summary>
        /// float32x4_t vrecpeq_f32 (float32x4_t a)
        ///   A32: VRECPE.F32 Qd, Qm
        ///   A64: FRECPE Vd.4S, Vn.4S
        /// </summary>
        public static Vector128<float> ReciprocalEstimate(Vector128<float> value) => ReciprocalEstimate(value);

        /// <summary>
        /// uint32x4_t vrecpeq_u32 (uint32x4_t a)
        ///   A32: VRECPE.U32 Qd, Qm
        ///   A64: URECPE Vd.4S, Vn.4S
        /// </summary>
        public static Vector128<uint> ReciprocalEstimate(Vector128<uint> value) => ReciprocalEstimate(value);

        /// <summary>
        /// float32x2_t vrsqrte_f32 (float32x2_t a)
        ///   A32: VRSQRTE.F32 Dd, Dm
        ///   A64: FRSQRTE Vd.2S, Vn.2S
        /// </summary>
        public static Vector64<float> ReciprocalSquareRootEstimate(Vector64<float> value) => ReciprocalSquareRootEstimate(value);

        /// <summary>
        /// uint32x2_t vrsqrte_u32 (uint32x2_t a)
        ///   A32: VRSQRTE.U32 Dd, Dm
        ///   A64: URSQRTE Vd.2S, Vn.2S
        /// </summary>
        public static Vector64<uint> ReciprocalSquareRootEstimate(Vector64<uint> value) => ReciprocalSquareRootEstimate(value);

        /// <summary>
        /// float32x4_t vrsqrteq_f32 (float32x4_t a)
        ///   A32: VRSQRTE.F32 Qd, Qm
        ///   A64: FRSQRTE Vd.4S, Vn.4S
        /// </summary>
        public static Vector128<float> ReciprocalSquareRootEstimate(Vector128<float> value) => ReciprocalSquareRootEstimate(value);

        /// <summary>
        /// uint32x4_t vrsqrteq_u32 (uint32x4_t a)
        ///   A32: VRSQRTE.U32 Qd, Qm
        ///   A64: URSQRTE Vd.4S, Vn.4S
        /// </summary>
        public static Vector128<uint> ReciprocalSquareRootEstimate(Vector128<uint> value) => ReciprocalSquareRootEstimate(value);

        /// <summary>
        /// float32x2_t vrsqrts_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VRSQRTS.F32 Dd, Dn, Dm
        ///   A64: FRSQRTS Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<float> ReciprocalSquareRootStep(Vector64<float> left, Vector64<float> right) => ReciprocalSquareRootStep(left, right);

        /// <summary>
        /// float32x4_t vrsqrtsq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VRSQRTS.F32 Qd, Qn, Qm
        ///   A64: FRSQRTS Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<float> ReciprocalSquareRootStep(Vector128<float> left, Vector128<float> right) => ReciprocalSquareRootStep(left, right);

        /// <summary>
        /// float32x2_t vrecps_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VRECPS.F32 Dd, Dn, Dm
        ///   A64: FRECPS Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<float> ReciprocalStep(Vector64<float> left, Vector64<float> right) => ReciprocalStep(left, right);

        /// <summary>
        /// float32x4_t vrecpsq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VRECPS.F32 Qd, Qn, Qm
        ///   A64: FRECPS Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<float> ReciprocalStep(Vector128<float> left, Vector128<float> right) => ReciprocalStep(left, right);

        /// <summary>
        /// uint8x8_t vsli_n_u8(uint8x8_t a, uint8x8_t b, __builtin_constant_p(n))
        ///   A32: VSLI.8 Dd, Dm, #n
        ///   A64: SLI Vd.8B, Vn.8B, #n
        /// </summary>
        public static Vector64<byte> ShiftLeftLogicalAndInsert(Vector64<byte> left, Vector64<byte> right, byte shift) => ShiftLeftLogicalAndInsert(left, right, shift);

        /// <summary>
        /// int16x4_t vsli_n_s16(int16x4_t a, int16x4_t b, __builtin_constant_p(n))
        ///   A32: VSLI.16 Dd, Dm, #n
        ///   A64: SLI Vd.4H, Vn.4H, #n
        /// </summary>
        public static Vector64<short> ShiftLeftLogicalAndInsert(Vector64<short> left, Vector64<short> right, byte shift) => ShiftLeftLogicalAndInsert(left, right, shift);

        /// <summary>
        /// int32x2_t vsli_n_s32(int32x2_t a, int32x2_t b, __builtin_constant_p(n))
        ///   A32: VSLI.32 Dd, Dm, #n
        ///   A64: SLI Vd.2S, Vn.2S, #n
        /// </summary>
        public static Vector64<int> ShiftLeftLogicalAndInsert(Vector64<int> left, Vector64<int> right, byte shift) => ShiftLeftLogicalAndInsert(left, right, shift);

        /// <summary>
        /// int8x8_t vsli_n_s8(int8x8_t a, int8x8_t b, __builtin_constant_p(n))
        ///   A32: VSLI.8 Dd, Dm, #n
        ///   A64: SLI Vd.8B, Vn.8B, #n
        /// </summary>
        public static Vector64<sbyte> ShiftLeftLogicalAndInsert(Vector64<sbyte> left, Vector64<sbyte> right, byte shift) => ShiftLeftLogicalAndInsert(left, right, shift);

        /// <summary>
        /// uint16x4_t vsli_n_u16(uint16x4_t a, uint16x4_t b, __builtin_constant_p(n))
        ///   A32: VSLI.16 Dd, Dm, #n
        ///   A64: SLI Vd.4H, Vn.4H, #n
        /// </summary>
        public static Vector64<ushort> ShiftLeftLogicalAndInsert(Vector64<ushort> left, Vector64<ushort> right, byte shift) => ShiftLeftLogicalAndInsert(left, right, shift);

        /// <summary>
        /// uint32x2_t vsli_n_u32(uint32x2_t a, uint32x2_t b, __builtin_constant_p(n))
        ///   A32: VSLI.32 Dd, Dm, #n
        ///   A64: SLI Vd.2S, Vn.2S, #n
        /// </summary>
        public static Vector64<uint> ShiftLeftLogicalAndInsert(Vector64<uint> left, Vector64<uint> right, byte shift) => ShiftLeftLogicalAndInsert(left, right, shift);

        /// <summary>
        /// uint8x16_t vsliq_n_u8(uint8x16_t a, uint8x16_t b, __builtin_constant_p(n))
        ///   A32: VSLI.8 Qd, Qm, #n
        ///   A64: SLI Vd.16B, Vn.16B, #n
        /// </summary>
        public static Vector128<byte> ShiftLeftLogicalAndInsert(Vector128<byte> left, Vector128<byte> right, byte shift) => ShiftLeftLogicalAndInsert(left, right, shift);

        /// <summary>
        /// int16x8_t vsliq_n_s16(int16x8_t a, int16x8_t b, __builtin_constant_p(n))
        ///   A32: VSLI.16 Qd, Qm, #n
        ///   A64: SLI Vd.8H, Vn.8H, #n
        /// </summary>
        public static Vector128<short> ShiftLeftLogicalAndInsert(Vector128<short> left, Vector128<short> right, byte shift) => ShiftLeftLogicalAndInsert(left, right, shift);

        /// <summary>
        /// int32x4_t vsliq_n_s32(int32x4_t a, int32x4_t b, __builtin_constant_p(n))
        ///   A32: VSLI.32 Qd, Qm, #n
        ///   A64: SLI Vd.4S, Vn.4S, #n
        /// </summary>
        public static Vector128<int> ShiftLeftLogicalAndInsert(Vector128<int> left, Vector128<int> right, byte shift) => ShiftLeftLogicalAndInsert(left, right, shift);

        /// <summary>
        /// int64x2_t vsliq_n_s64(int64x2_t a, int64x2_t b, __builtin_constant_p(n))
        ///   A32: VSLI.64 Qd, Qm, #n
        ///   A64: SLI Vd.2D, Vn.2D, #n
        /// </summary>
        public static Vector128<long> ShiftLeftLogicalAndInsert(Vector128<long> left, Vector128<long> right, byte shift) => ShiftLeftLogicalAndInsert(left, right, shift);

        /// <summary>
        /// int8x16_t vsliq_n_s8(int8x16_t a, int8x16_t b, __builtin_constant_p(n))
        ///   A32: VSLI.8 Qd, Qm, #n
        ///   A64: SLI Vd.16B, Vn.16B, #n
        /// </summary>
        public static Vector128<sbyte> ShiftLeftLogicalAndInsert(Vector128<sbyte> left, Vector128<sbyte> right, byte shift) => ShiftLeftLogicalAndInsert(left, right, shift);

        /// <summary>
        /// uint16x8_t vsliq_n_u16(uint16x8_t a, uint16x8_t b, __builtin_constant_p(n))
        ///   A32: VSLI.16 Qd, Qm, #n
        ///   A64: SLI Vd.8H, Vn.8H, #n
        /// </summary>
        public static Vector128<ushort> ShiftLeftLogicalAndInsert(Vector128<ushort> left, Vector128<ushort> right, byte shift) => ShiftLeftLogicalAndInsert(left, right, shift);

        /// <summary>
        /// uint32x4_t vsliq_n_u32(uint32x4_t a, uint32x4_t b, __builtin_constant_p(n))
        ///   A32: VSLI.32 Qd, Qm, #n
        ///   A64: SLI Vd.4S, Vn.4S, #n
        /// </summary>
        public static Vector128<uint> ShiftLeftLogicalAndInsert(Vector128<uint> left, Vector128<uint> right, byte shift) => ShiftLeftLogicalAndInsert(left, right, shift);

        /// <summary>
        /// uint64x2_t vsliq_n_u64(uint64x2_t a, uint64x2_t b, __builtin_constant_p(n))
        ///   A32: VSLI.64 Qd, Qm, #n
        ///   A64: SLI Vd.2D, Vn.2D, #n
        /// </summary>
        public static Vector128<ulong> ShiftLeftLogicalAndInsert(Vector128<ulong> left, Vector128<ulong> right, byte shift) => ShiftLeftLogicalAndInsert(left, right, shift);

        /// <summary>
        /// int64_t vslid_n_s64(int64_t a, int64_t b, __builtin_constant_p(n))
        ///   A32: VSLI.64 Dd, Dm, #n
        ///   A64: SLI Dd, Dn, #n
        /// </summary>
        public static Vector64<long> ShiftLeftLogicalAndInsertScalar(Vector64<long> left, Vector64<long> right, byte shift) => ShiftLeftLogicalAndInsertScalar(left, right, shift);

        /// <summary>
        /// uint64_t vslid_n_u64(uint64_t a, uint64_t b, __builtin_constant_p(n))
        ///   A32: VSLI.64 Dd, Dm, #n
        ///   A64: SLI Dd, Dn, #n
        /// </summary>
        public static Vector64<ulong> ShiftLeftLogicalAndInsertScalar(Vector64<ulong> left, Vector64<ulong> right, byte shift) => ShiftLeftLogicalAndInsertScalar(left, right, shift);

        /// <summary>
        /// uint8x8_t vsri_n_u8(uint8x8_t a, uint8x8_t b, __builtin_constant_p(n))
        ///   A32: VSRI.8 Dd, Dm, #n
        ///   A64: SRI Vd.8B, Vn.8B, #n
        /// </summary>
        public static Vector64<byte> ShiftRightAndInsert(Vector64<byte> left, Vector64<byte> right, byte shift) => ShiftRightAndInsert(left, right, shift);

        /// <summary>
        /// int16x4_t vsri_n_s16(int16x4_t a, int16x4_t b, __builtin_constant_p(n))
        ///   A32: VSRI.16 Dd, Dm, #n
        ///   A64: SRI Vd.4H, Vn.4H, #n
        /// </summary>
        public static Vector64<short> ShiftRightAndInsert(Vector64<short> left, Vector64<short> right, byte shift) => ShiftRightAndInsert(left, right, shift);

        /// <summary>
        /// int32x2_t vsri_n_s32(int32x2_t a, int32x2_t b, __builtin_constant_p(n))
        ///   A32: VSRI.32 Dd, Dm, #n
        ///   A64: SRI Vd.2S, Vn.2S, #n
        /// </summary>
        public static Vector64<int> ShiftRightAndInsert(Vector64<int> left, Vector64<int> right, byte shift) => ShiftRightAndInsert(left, right, shift);

        /// <summary>
        /// int8x8_t vsri_n_s8(int8x8_t a, int8x8_t b, __builtin_constant_p(n))
        ///   A32: VSRI.8 Dd, Dm, #n
        ///   A64: SRI Vd.8B, Vn.8B, #n
        /// </summary>
        public static Vector64<sbyte> ShiftRightAndInsert(Vector64<sbyte> left, Vector64<sbyte> right, byte shift) => ShiftRightAndInsert(left, right, shift);

        /// <summary>
        /// uint16x4_t vsri_n_u16(uint16x4_t a, uint16x4_t b, __builtin_constant_p(n))
        ///   A32: VSRI.16 Dd, Dm, #n
        ///   A64: SRI Vd.4H, Vn.4H, #n
        /// </summary>
        public static Vector64<ushort> ShiftRightAndInsert(Vector64<ushort> left, Vector64<ushort> right, byte shift) => ShiftRightAndInsert(left, right, shift);

        /// <summary>
        /// uint32x2_t vsri_n_u32(uint32x2_t a, uint32x2_t b, __builtin_constant_p(n))
        ///   A32: VSRI.32 Dd, Dm, #n
        ///   A64: SRI Vd.2S, Vn.2S, #n
        /// </summary>
        public static Vector64<uint> ShiftRightAndInsert(Vector64<uint> left, Vector64<uint> right, byte shift) => ShiftRightAndInsert(left, right, shift);

        /// <summary>
        /// uint8x16_t vsriq_n_u8(uint8x16_t a, uint8x16_t b, __builtin_constant_p(n))
        ///   A32: VSRI.8 Qd, Qm, #n
        ///   A64: SRI Vd.16B, Vn.16B, #n
        /// </summary>
        public static Vector128<byte> ShiftRightAndInsert(Vector128<byte> left, Vector128<byte> right, byte shift) => ShiftRightAndInsert(left, right, shift);

        /// <summary>
        /// int16x8_t vsriq_n_s16(int16x8_t a, int16x8_t b, __builtin_constant_p(n))
        ///   A32: VSRI.16 Qd, Qm, #n
        ///   A64: SRI Vd.8H, Vn.8H, #n
        /// </summary>
        public static Vector128<short> ShiftRightAndInsert(Vector128<short> left, Vector128<short> right, byte shift) => ShiftRightAndInsert(left, right, shift);

        /// <summary>
        /// int32x4_t vsriq_n_s32(int32x4_t a, int32x4_t b, __builtin_constant_p(n))
        ///   A32: VSRI.32 Qd, Qm, #n
        ///   A64: SRI Vd.4S, Vn.4S, #n
        /// </summary>
        public static Vector128<int> ShiftRightAndInsert(Vector128<int> left, Vector128<int> right, byte shift) => ShiftRightAndInsert(left, right, shift);

        /// <summary>
        /// int64x2_t vsriq_n_s64(int64x2_t a, int64x2_t b, __builtin_constant_p(n))
        ///   A32: VSRI.64 Qd, Qm, #n
        ///   A64: SRI Vd.2D, Vn.2D, #n
        /// </summary>
        public static Vector128<long> ShiftRightAndInsert(Vector128<long> left, Vector128<long> right, byte shift) => ShiftRightAndInsert(left, right, shift);

        /// <summary>
        /// int8x16_t vsriq_n_s8(int8x16_t a, int8x16_t b, __builtin_constant_p(n))
        ///   A32: VSRI.8 Qd, Qm, #n
        ///   A64: SRI Vd.16B, Vn.16B, #n
        /// </summary>
        public static Vector128<sbyte> ShiftRightAndInsert(Vector128<sbyte> left, Vector128<sbyte> right, byte shift) => ShiftRightAndInsert(left, right, shift);

        /// <summary>
        /// uint16x8_t vsriq_n_u16(uint16x8_t a, uint16x8_t b, __builtin_constant_p(n))
        ///   A32: VSRI.16 Qd, Qm, #n
        ///   A64: SRI Vd.8H, Vn.8H, #n
        /// </summary>
        public static Vector128<ushort> ShiftRightAndInsert(Vector128<ushort> left, Vector128<ushort> right, byte shift) => ShiftRightAndInsert(left, right, shift);

        /// <summary>
        /// uint32x4_t vsriq_n_u32(uint32x4_t a, uint32x4_t b, __builtin_constant_p(n))
        ///   A32: VSRI.32 Qd, Qm, #n
        ///   A64: SRI Vd.4S, Vn.4S, #n
        /// </summary>
        public static Vector128<uint> ShiftRightAndInsert(Vector128<uint> left, Vector128<uint> right, byte shift) => ShiftRightAndInsert(left, right, shift);

        /// <summary>
        /// uint64x2_t vsriq_n_u64(uint64x2_t a, uint64x2_t b, __builtin_constant_p(n))
        ///   A32: VSRI.64 Qd, Qm, #n
        ///   A64: SRI Vd.2D, Vn.2D, #n
        /// </summary>
        public static Vector128<ulong> ShiftRightAndInsert(Vector128<ulong> left, Vector128<ulong> right, byte shift) => ShiftRightAndInsert(left, right, shift);

        /// <summary>
        /// int64_t vsrid_n_s64(int64_t a, int64_t b, __builtin_constant_p(n))
        ///   A32: VSRI.64 Dd, Dm, #n
        ///   A64: SRI Dd, Dn, #n
        /// </summary>
        public static Vector64<long> ShiftRightLogicalAndInsertScalar(Vector64<long> left, Vector64<long> right, byte shift) => ShiftRightLogicalAndInsertScalar(left, right, shift);

        /// <summary>
        /// uint64_t vsrid_n_u64(uint64_t a, uint64_t b, __builtin_constant_p(n))
        ///   A32: VSRI.64 Dd, Dm, #n
        ///   A64: SRI Dd, Dn, #n
        /// </summary>
        public static Vector64<ulong> ShiftRightLogicalAndInsertScalar(Vector64<ulong> left, Vector64<ulong> right, byte shift) => ShiftRightLogicalAndInsertScalar(left, right, shift);

        /// <summary>
        /// int16x4_t vshl_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VSHL.S16 Dd, Dn, Dm
        ///   A64: SSHL Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> ShiftArithmetic(Vector64<short> value, Vector64<short> count) => ShiftArithmetic(value, count);

        /// <summary>
        /// int32x2_t vshl_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VSHL.S32 Dd, Dn, Dm
        ///   A64: SSHL Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> ShiftArithmetic(Vector64<int> value, Vector64<int> count) => ShiftArithmetic(value, count);

        /// <summary>
        /// int8x8_t vshl_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VSHL.S8 Dd, Dn, Dm
        ///   A64: SSHL Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> ShiftArithmetic(Vector64<sbyte> value, Vector64<sbyte> count) => ShiftArithmetic(value, count);

        /// <summary>
        /// int16x8_t vshlq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VSHL.S16 Qd, Qn, Qm
        ///   A64: SSHL Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> ShiftArithmetic(Vector128<short> value, Vector128<short> count) => ShiftArithmetic(value, count);

        /// <summary>
        /// int32x4_t vshlq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VSHL.S32 Qd, Qn, Qm
        ///   A64: SSHL Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> ShiftArithmetic(Vector128<int> value, Vector128<int> count) => ShiftArithmetic(value, count);

        /// <summary>
        /// int64x2_t vshlq_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VSHL.S64 Qd, Qn, Qm
        ///   A64: SSHL Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<long> ShiftArithmetic(Vector128<long> value, Vector128<long> count) => ShiftArithmetic(value, count);

        /// <summary>
        /// int8x16_t vshlq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VSHL.S8 Qd, Qn, Qm
        ///   A64: SSHL Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> ShiftArithmetic(Vector128<sbyte> value, Vector128<sbyte> count) => ShiftArithmetic(value, count);

        /// <summary>
        /// int16x4_t vrshl_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VRSHL.S16 Dd, Dn, Dm
        ///   A64: SRSHL Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> ShiftArithmeticRounded(Vector64<short> value, Vector64<short> count) => ShiftArithmeticRounded(value, count);

        /// <summary>
        /// int32x2_t vrshl_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VRSHL.S32 Dd, Dn, Dm
        ///   A64: SRSHL Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> ShiftArithmeticRounded(Vector64<int> value, Vector64<int> count) => ShiftArithmeticRounded(value, count);

        /// <summary>
        /// int8x8_t vrshl_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VRSHL.S8 Dd, Dn, Dm
        ///   A64: SRSHL Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> ShiftArithmeticRounded(Vector64<sbyte> value, Vector64<sbyte> count) => ShiftArithmeticRounded(value, count);

        /// <summary>
        /// int16x8_t vrshlq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VRSHL.S16 Qd, Qn, Qm
        ///   A64: SRSHL Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> ShiftArithmeticRounded(Vector128<short> value, Vector128<short> count) => ShiftArithmeticRounded(value, count);

        /// <summary>
        /// int32x4_t vrshlq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VRSHL.S32 Qd, Qn, Qm
        ///   A64: SRSHL Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> ShiftArithmeticRounded(Vector128<int> value, Vector128<int> count) => ShiftArithmeticRounded(value, count);

        /// <summary>
        /// int64x2_t vrshlq_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VRSHL.S64 Qd, Qn, Qm
        ///   A64: SRSHL Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<long> ShiftArithmeticRounded(Vector128<long> value, Vector128<long> count) => ShiftArithmeticRounded(value, count);

        /// <summary>
        /// int8x16_t vrshlq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VRSHL.S8 Qd, Qn, Qm
        ///   A64: SRSHL Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> ShiftArithmeticRounded(Vector128<sbyte> value, Vector128<sbyte> count) => ShiftArithmeticRounded(value, count);

        /// <summary>
        /// int16x4_t vqrshl_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VQRSHL.S16 Dd, Dn, Dm
        ///   A64: SQRSHL Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> ShiftArithmeticRoundedSaturate(Vector64<short> value, Vector64<short> count) => ShiftArithmeticRoundedSaturate(value, count);

        /// <summary>
        /// int32x2_t vqrshl_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VQRSHL.S32 Dd, Dn, Dm
        ///   A64: SQRSHL Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> ShiftArithmeticRoundedSaturate(Vector64<int> value, Vector64<int> count) => ShiftArithmeticRoundedSaturate(value, count);

        /// <summary>
        /// int8x8_t vqrshl_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VQRSHL.S8 Dd, Dn, Dm
        ///   A64: SQRSHL Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> ShiftArithmeticRoundedSaturate(Vector64<sbyte> value, Vector64<sbyte> count) => ShiftArithmeticRoundedSaturate(value, count);

        /// <summary>
        /// int16x8_t vqrshlq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VQRSHL.S16 Qd, Qn, Qm
        ///   A64: SQRSHL Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> ShiftArithmeticRoundedSaturate(Vector128<short> value, Vector128<short> count) => ShiftArithmeticRoundedSaturate(value, count);

        /// <summary>
        /// int32x4_t vqrshlq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VQRSHL.S32 Qd, Qn, Qm
        ///   A64: SQRSHL Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> ShiftArithmeticRoundedSaturate(Vector128<int> value, Vector128<int> count) => ShiftArithmeticRoundedSaturate(value, count);

        /// <summary>
        /// int64x2_t vqrshlq_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VQRSHL.S64 Qd, Qn, Qm
        ///   A64: SQRSHL Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<long> ShiftArithmeticRoundedSaturate(Vector128<long> value, Vector128<long> count) => ShiftArithmeticRoundedSaturate(value, count);

        /// <summary>
        /// int8x16_t vqrshlq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VQRSHL.S8 Qd, Qn, Qm
        ///   A64: SQRSHL Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> ShiftArithmeticRoundedSaturate(Vector128<sbyte> value, Vector128<sbyte> count) => ShiftArithmeticRoundedSaturate(value, count);

        /// <summary>
        /// int64x1_t vqrshl_s64 (int64x1_t a, int64x1_t b)
        ///   A32: VQRSHL.S64 Dd, Dn, Dm
        ///   A64: SQRSHL Dd, Dn, Dm
        /// </summary>
        public static Vector64<long> ShiftArithmeticRoundedSaturateScalar(Vector64<long> value, Vector64<long> count) => ShiftArithmeticRoundedSaturateScalar(value, count);

        /// <summary>
        /// int64x1_t vrshl_s64 (int64x1_t a, int64x1_t b)
        ///   A32: VRSHL.S64 Dd, Dn, Dm
        ///   A64: SRSHL Dd, Dn, Dm
        /// </summary>
        public static Vector64<long> ShiftArithmeticRoundedScalar(Vector64<long> value, Vector64<long> count) => ShiftArithmeticRoundedScalar(value, count);

        /// <summary>
        /// int16x4_t vqshl_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VQSHL.S16 Dd, Dn, Dm
        ///   A64: SQSHL Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> ShiftArithmeticSaturate(Vector64<short> value, Vector64<short> count) => ShiftArithmeticSaturate(value, count);

        /// <summary>
        /// int32x2_t vqshl_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VQSHL.S32 Dd, Dn, Dm
        ///   A64: SQSHL Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> ShiftArithmeticSaturate(Vector64<int> value, Vector64<int> count) => ShiftArithmeticSaturate(value, count);

        /// <summary>
        /// int8x8_t vqshl_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VQSHL.S8 Dd, Dn, Dm
        ///   A64: SQSHL Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> ShiftArithmeticSaturate(Vector64<sbyte> value, Vector64<sbyte> count) => ShiftArithmeticSaturate(value, count);

        /// <summary>
        /// int16x8_t vqshlq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VQSHL.S16 Qd, Qn, Qm
        ///   A64: SQSHL Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> ShiftArithmeticSaturate(Vector128<short> value, Vector128<short> count) => ShiftArithmeticSaturate(value, count);

        /// <summary>
        /// int32x4_t vqshlq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VQSHL.S32 Qd, Qn, Qm
        ///   A64: SQSHL Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> ShiftArithmeticSaturate(Vector128<int> value, Vector128<int> count) => ShiftArithmeticSaturate(value, count);

        /// <summary>
        /// int64x2_t vqshlq_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VQSHL.S64 Qd, Qn, Qm
        ///   A64: SQSHL Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<long> ShiftArithmeticSaturate(Vector128<long> value, Vector128<long> count) => ShiftArithmeticSaturate(value, count);

        /// <summary>
        /// int8x16_t vqshlq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VQSHL.S8 Qd, Qn, Qm
        ///   A64: SQSHL Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> ShiftArithmeticSaturate(Vector128<sbyte> value, Vector128<sbyte> count) => ShiftArithmeticSaturate(value, count);

        /// <summary>
        /// int64x1_t vqshl_s64 (int64x1_t a, int64x1_t b)
        ///   A32: VQSHL.S64 Dd, Dn, Dm
        ///   A64: SQSHL Dd, Dn, Dm
        /// </summary>
        public static Vector64<long> ShiftArithmeticSaturateScalar(Vector64<long> value, Vector64<long> count) => ShiftArithmeticSaturateScalar(value, count);

        /// <summary>
        /// int64x1_t vshl_s64 (int64x1_t a, int64x1_t b)
        ///   A32: VSHL.S64 Dd, Dn, Dm
        ///   A64: SSHL Dd, Dn, Dm
        /// </summary>
        public static Vector64<long> ShiftArithmeticScalar(Vector64<long> value, Vector64<long> count) => ShiftArithmeticScalar(value, count);

        /// <summary>
        /// uint8x8_t vshl_n_u8 (uint8x8_t a, const int n)
        ///   A32: VSHL.I8 Dd, Dm, #n
        ///   A64: SHL Vd.8B, Vn.8B, #n
        /// </summary>
        public static Vector64<byte> ShiftLeftLogical(Vector64<byte> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// int16x4_t vshl_n_s16 (int16x4_t a, const int n)
        ///   A32: VSHL.I16 Dd, Dm, #n
        ///   A64: SHL Vd.4H, Vn.4H, #n
        /// </summary>
        public static Vector64<short> ShiftLeftLogical(Vector64<short> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// int32x2_t vshl_n_s32 (int32x2_t a, const int n)
        ///   A32: VSHL.I32 Dd, Dm, #n
        ///   A64: SHL Vd.2S, Vn.2S, #n
        /// </summary>
        public static Vector64<int> ShiftLeftLogical(Vector64<int> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// int8x8_t vshl_n_s8 (int8x8_t a, const int n)
        ///   A32: VSHL.I8 Dd, Dm, #n
        ///   A64: SHL Vd.8B, Vn.8B, #n
        /// </summary>
        public static Vector64<sbyte> ShiftLeftLogical(Vector64<sbyte> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// uint16x4_t vshl_n_u16 (uint16x4_t a, const int n)
        ///   A32: VSHL.I16 Dd, Dm, #n
        ///   A64: SHL Vd.4H, Vn.4H, #n
        /// </summary>
        public static Vector64<ushort> ShiftLeftLogical(Vector64<ushort> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// uint32x2_t vshl_n_u32 (uint32x2_t a, const int n)
        ///   A32: VSHL.I32 Dd, Dm, #n
        ///   A64: SHL Vd.2S, Vn.2S, #n
        /// </summary>
        public static Vector64<uint> ShiftLeftLogical(Vector64<uint> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// uint8x16_t vshlq_n_u8 (uint8x16_t a, const int n)
        ///   A32: VSHL.I8 Qd, Qm, #n
        ///   A64: SHL Vd.16B, Vn.16B, #n
        /// </summary>
        public static Vector128<byte> ShiftLeftLogical(Vector128<byte> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// int16x8_t vshlq_n_s16 (int16x8_t a, const int n)
        ///   A32: VSHL.I16 Qd, Qm, #n
        ///   A64: SHL Vd.8H, Vn.8H, #n
        /// </summary>
        public static Vector128<short> ShiftLeftLogical(Vector128<short> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// int64x2_t vshlq_n_s64 (int64x2_t a, const int n)
        ///   A32: VSHL.I64 Qd, Qm, #n
        ///   A64: SHL Vd.2D, Vn.2D, #n
        /// </summary>
        public static Vector128<long> ShiftLeftLogical(Vector128<long> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// int8x16_t vshlq_n_s8 (int8x16_t a, const int n)
        ///   A32: VSHL.I8 Qd, Qm, #n
        ///   A64: SHL Vd.16B, Vn.16B, #n
        /// </summary>
        public static Vector128<sbyte> ShiftLeftLogical(Vector128<sbyte> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// uint16x8_t vshlq_n_u16 (uint16x8_t a, const int n)
        ///   A32: VSHL.I16 Qd, Qm, #n
        ///   A64: SHL Vd.8H, Vn.8H, #n
        /// </summary>
        public static Vector128<ushort> ShiftLeftLogical(Vector128<ushort> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// uint32x4_t vshlq_n_u32 (uint32x4_t a, const int n)
        ///   A32: VSHL.I32 Qd, Qm, #n
        ///   A64: SHL Vd.4S, Vn.4S, #n
        /// </summary>
        public static Vector128<uint> ShiftLeftLogical(Vector128<uint> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// uint64x2_t vshlq_n_u64 (uint64x2_t a, const int n)
        ///   A32: VSHL.I64 Qd, Qm, #n
        ///   A64: SHL Vd.2D, Vn.2D, #n
        /// </summary>
        public static Vector128<ulong> ShiftLeftLogical(Vector128<ulong> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// uint8x8_t vqshl_n_u8 (uint8x8_t a, const int n)
        ///   A32: VQSHL.U8 Dd, Dm, #n
        ///   A64: UQSHL Vd.8B, Vn.8B, #n
        /// </summary>
        public static Vector64<byte> ShiftLeftLogicalSaturate(Vector64<byte> value, byte count) => ShiftLeftLogicalSaturate(value, count);

        /// <summary>
        /// int16x4_t vqshl_n_s16 (int16x4_t a, const int n)
        ///   A32: VQSHL.S16 Dd, Dm, #n
        ///   A64: SQSHL Vd.4H, Vn.4H, #n
        /// </summary>
        public static Vector64<short> ShiftLeftLogicalSaturate(Vector64<short> value, byte count) => ShiftLeftLogicalSaturate(value, count);

        /// <summary>
        /// int32x2_t vqshl_n_s32 (int32x2_t a, const int n)
        ///   A32: VQSHL.S32 Dd, Dm, #n
        ///   A64: SQSHL Vd.2S, Vn.2S, #n
        /// </summary>
        public static Vector64<int> ShiftLeftLogicalSaturate(Vector64<int> value, byte count) => ShiftLeftLogicalSaturate(value, count);

        /// <summary>
        /// int8x8_t vqshl_n_s8 (int8x8_t a, const int n)
        ///   A32: VQSHL.S8 Dd, Dm, #n
        ///   A64: SQSHL Vd.8B, Vn.8B, #n
        /// </summary>
        public static Vector64<sbyte> ShiftLeftLogicalSaturate(Vector64<sbyte> value, byte count) => ShiftLeftLogicalSaturate(value, count);

        /// <summary>
        /// uint16x4_t vqshl_n_u16 (uint16x4_t a, const int n)
        ///   A32: VQSHL.U16 Dd, Dm, #n
        ///   A64: UQSHL Vd.4H, Vn.4H, #n
        /// </summary>
        public static Vector64<ushort> ShiftLeftLogicalSaturate(Vector64<ushort> value, byte count) => ShiftLeftLogicalSaturate(value, count);

        /// <summary>
        /// uint32x2_t vqshl_n_u32 (uint32x2_t a, const int n)
        ///   A32: VQSHL.U32 Dd, Dm, #n
        ///   A64: UQSHL Vd.2S, Vn.2S, #n
        /// </summary>
        public static Vector64<uint> ShiftLeftLogicalSaturate(Vector64<uint> value, byte count) => ShiftLeftLogicalSaturate(value, count);

        /// <summary>
        /// uint8x16_t vqshlq_n_u8 (uint8x16_t a, const int n)
        ///   A32: VQSHL.U8 Qd, Qm, #n
        ///   A64: UQSHL Vd.16B, Vn.16B, #n
        /// </summary>
        public static Vector128<byte> ShiftLeftLogicalSaturate(Vector128<byte> value, byte count) => ShiftLeftLogicalSaturate(value, count);

        /// <summary>
        /// int16x8_t vqshlq_n_s16 (int16x8_t a, const int n)
        ///   A32: VQSHL.S16 Qd, Qm, #n
        ///   A64: SQSHL Vd.8H, Vn.8H, #n
        /// </summary>
        public static Vector128<short> ShiftLeftLogicalSaturate(Vector128<short> value, byte count) => ShiftLeftLogicalSaturate(value, count);

        /// <summary>
        /// int32x4_t vqshlq_n_s32 (int32x4_t a, const int n)
        ///   A32: VQSHL.S32 Qd, Qm, #n
        ///   A64: SQSHL Vd.4S, Vn.4S, #n
        /// </summary>
        public static Vector128<int> ShiftLeftLogicalSaturate(Vector128<int> value, byte count) => ShiftLeftLogicalSaturate(value, count);

        /// <summary>
        /// int64x2_t vqshlq_n_s64 (int64x2_t a, const int n)
        ///   A32: VQSHL.S64 Qd, Qm, #n
        ///   A64: SQSHL Vd.2D, Vn.2D, #n
        /// </summary>
        public static Vector128<long> ShiftLeftLogicalSaturate(Vector128<long> value, byte count) => ShiftLeftLogicalSaturate(value, count);

        /// <summary>
        /// int8x16_t vqshlq_n_s8 (int8x16_t a, const int n)
        ///   A32: VQSHL.S8 Qd, Qm, #n
        ///   A64: SQSHL Vd.16B, Vn.16B, #n
        /// </summary>
        public static Vector128<sbyte> ShiftLeftLogicalSaturate(Vector128<sbyte> value, byte count) => ShiftLeftLogicalSaturate(value, count);

        /// <summary>
        /// uint16x8_t vqshlq_n_u16 (uint16x8_t a, const int n)
        ///   A32: VQSHL.U16 Qd, Qm, #n
        ///   A64: UQSHL Vd.8H, Vn.8H, #n
        /// </summary>
        public static Vector128<ushort> ShiftLeftLogicalSaturate(Vector128<ushort> value, byte count) => ShiftLeftLogicalSaturate(value, count);

        /// <summary>
        /// uint32x4_t vqshlq_n_u32 (uint32x4_t a, const int n)
        ///   A32: VQSHL.U32 Qd, Qm, #n
        ///   A64: UQSHL Vd.4S, Vn.4S, #n
        /// </summary>
        public static Vector128<uint> ShiftLeftLogicalSaturate(Vector128<uint> value, byte count) => ShiftLeftLogicalSaturate(value, count);

        /// <summary>
        /// uint64x2_t vqshlq_n_u64 (uint64x2_t a, const int n)
        ///   A32: VQSHL.U64 Qd, Qm, #n
        ///   A64: UQSHL Vd.2D, Vn.2D, #n
        /// </summary>
        public static Vector128<ulong> ShiftLeftLogicalSaturate(Vector128<ulong> value, byte count) => ShiftLeftLogicalSaturate(value, count);

        /// <summary>
        /// int64x1_t vqshl_n_s64 (int64x1_t a, const int n)
        ///   A32: VQSHL.S64 Dd, Dm, #n
        ///   A64: SQSHL Dd, Dn, #n
        /// </summary>
        public static Vector64<long> ShiftLeftLogicalSaturateScalar(Vector64<long> value, byte count) => ShiftLeftLogicalSaturateScalar(value, count);

        /// <summary>
        /// uint64x1_t vqshl_n_u64 (uint64x1_t a, const int n)
        ///   A32: VQSHL.U64 Dd, Dm, #n
        ///   A64: UQSHL Dd, Dn, #n
        /// </summary>
        public static Vector64<ulong> ShiftLeftLogicalSaturateScalar(Vector64<ulong> value, byte count) => ShiftLeftLogicalSaturateScalar(value, count);

        /// <summary>
        /// uint16x4_t vqshlu_n_s16 (int16x4_t a, const int n)
        ///   A32: VQSHLU.S16 Dd, Dm, #n
        ///   A64: SQSHLU Vd.4H, Vn.4H, #n
        /// </summary>
        public static Vector64<ushort> ShiftLeftLogicalSaturateUnsigned(Vector64<short> value, byte count) => ShiftLeftLogicalSaturateUnsigned(value, count);

        /// <summary>
        /// uint32x2_t vqshlu_n_s32 (int32x2_t a, const int n)
        ///   A32: VQSHLU.S32 Dd, Dm, #n
        ///   A64: SQSHLU Vd.2S, Vn.2S, #n
        /// </summary>
        public static Vector64<uint> ShiftLeftLogicalSaturateUnsigned(Vector64<int> value, byte count) => ShiftLeftLogicalSaturateUnsigned(value, count);

        /// <summary>
        /// uint8x8_t vqshlu_n_s8 (int8x8_t a, const int n)
        ///   A32: VQSHLU.S8 Dd, Dm, #n
        ///   A64: SQSHLU Vd.8B, Vn.8B, #n
        /// </summary>
        public static Vector64<byte> ShiftLeftLogicalSaturateUnsigned(Vector64<sbyte> value, byte count) => ShiftLeftLogicalSaturateUnsigned(value, count);

        /// <summary>
        /// uint16x8_t vqshluq_n_s16 (int16x8_t a, const int n)
        ///   A32: VQSHLU.S16 Qd, Qm, #n
        ///   A64: SQSHLU Vd.8H, Vn.8H, #n
        /// </summary>
        public static Vector128<ushort> ShiftLeftLogicalSaturateUnsigned(Vector128<short> value, byte count) => ShiftLeftLogicalSaturateUnsigned(value, count);

        /// <summary>
        /// uint32x4_t vqshluq_n_s32 (int32x4_t a, const int n)
        ///   A32: VQSHLU.S32 Qd, Qm, #n
        ///   A64: SQSHLU Vd.4S, Vn.4S, #n
        /// </summary>
        public static Vector128<uint> ShiftLeftLogicalSaturateUnsigned(Vector128<int> value, byte count) => ShiftLeftLogicalSaturateUnsigned(value, count);

        /// <summary>
        /// uint64x2_t vqshluq_n_s64 (int64x2_t a, const int n)
        ///   A32: VQSHLU.S64 Qd, Qm, #n
        ///   A64: SQSHLU Vd.2D, Vn.2D, #n
        /// </summary>
        public static Vector128<ulong> ShiftLeftLogicalSaturateUnsigned(Vector128<long> value, byte count) => ShiftLeftLogicalSaturateUnsigned(value, count);

        /// <summary>
        /// uint8x16_t vqshluq_n_s8 (int8x16_t a, const int n)
        ///   A32: VQSHLU.S8 Qd, Qm, #n
        ///   A64: SQSHLU Vd.16B, Vn.16B, #n
        /// </summary>
        public static Vector128<byte> ShiftLeftLogicalSaturateUnsigned(Vector128<sbyte> value, byte count) => ShiftLeftLogicalSaturateUnsigned(value, count);

        /// <summary>
        /// uint64x1_t vqshlu_n_s64 (int64x1_t a, const int n)
        ///   A32: VQSHLU.S64 Dd, Dm, #n
        ///   A64: SQSHLU Dd, Dn, #n
        /// </summary>
        public static Vector64<ulong> ShiftLeftLogicalSaturateUnsignedScalar(Vector64<long> value, byte count) => ShiftLeftLogicalSaturateUnsignedScalar(value, count);

        /// <summary>
        /// int64x1_t vshl_n_s64 (int64x1_t a, const int n)
        ///   A32: VSHL.I64 Dd, Dm, #n
        ///   A64: SHL Dd, Dn, #n
        /// </summary>
        public static Vector64<long> ShiftLeftLogicalScalar(Vector64<long> value, byte count) => ShiftLeftLogicalScalar(value, count);

        /// <summary>
        /// uint64x1_t vshl_n_u64 (uint64x1_t a, const int n)
        ///   A32: VSHL.I64 Dd, Dm, #n
        ///   A64: SHL Dd, Dn, #n
        /// </summary>
        public static Vector64<ulong> ShiftLeftLogicalScalar(Vector64<ulong> value, byte count) => ShiftLeftLogicalScalar(value, count);

        /// <summary>
        /// uint16x8_t vshll_n_u8 (uint8x8_t a, const int n)
        ///   A32: VSHLL.U8 Qd, Dm, #n
        ///   A64: USHLL Vd.8H, Vn.8B, #n
        /// </summary>
        public static Vector128<ushort> ShiftLeftLogicalWideningLower(Vector64<byte> value, byte count) => ShiftLeftLogicalWideningLower(value, count);

        /// <summary>
        /// int32x4_t vshll_n_s16 (int16x4_t a, const int n)
        ///   A32: VSHLL.S16 Qd, Dm, #n
        ///   A64: SSHLL Vd.4S, Vn.4H, #n
        /// </summary>
        public static Vector128<int> ShiftLeftLogicalWideningLower(Vector64<short> value, byte count) => ShiftLeftLogicalWideningLower(value, count);

        /// <summary>
        /// int64x2_t vshll_n_s32 (int32x2_t a, const int n)
        ///   A32: VSHLL.S32 Qd, Dm, #n
        ///   A64: SSHLL Vd.2D, Vn.2S, #n
        /// </summary>
        public static Vector128<long> ShiftLeftLogicalWideningLower(Vector64<int> value, byte count) => ShiftLeftLogicalWideningLower(value, count);

        /// <summary>
        /// int16x8_t vshll_n_s8 (int8x8_t a, const int n)
        ///   A32: VSHLL.S8 Qd, Dm, #n
        ///   A64: SSHLL Vd.8H, Vn.8B, #n
        /// </summary>
        public static Vector128<short> ShiftLeftLogicalWideningLower(Vector64<sbyte> value, byte count) => ShiftLeftLogicalWideningLower(value, count);

        /// <summary>
        /// uint32x4_t vshll_n_u16 (uint16x4_t a, const int n)
        ///   A32: VSHLL.U16 Qd, Dm, #n
        ///   A64: USHLL Vd.4S, Vn.4H, #n
        /// </summary>
        public static Vector128<uint> ShiftLeftLogicalWideningLower(Vector64<ushort> value, byte count) => ShiftLeftLogicalWideningLower(value, count);

        /// <summary>
        /// uint64x2_t vshll_n_u32 (uint32x2_t a, const int n)
        ///   A32: VSHLL.U32 Qd, Dm, #n
        ///   A64: USHLL Vd.2D, Vn.2S, #n
        /// </summary>
        public static Vector128<ulong> ShiftLeftLogicalWideningLower(Vector64<uint> value, byte count) => ShiftLeftLogicalWideningLower(value, count);

        /// <summary>
        /// uint16x8_t vshll_high_n_u8 (uint8x16_t a, const int n)
        ///   A32: VSHLL.U8 Qd, Dm+1, #n
        ///   A64: USHLL2 Vd.8H, Vn.16B, #n
        /// </summary>
        public static Vector128<ushort> ShiftLeftLogicalWideningUpper(Vector128<byte> value, byte count) => ShiftLeftLogicalWideningUpper(value, count);

        /// <summary>
        /// int32x4_t vshll_high_n_s16 (int16x8_t a, const int n)
        ///   A32: VSHLL.S16 Qd, Dm+1, #n
        ///   A64: SSHLL2 Vd.4S, Vn.8H, #n
        /// </summary>
        public static Vector128<int> ShiftLeftLogicalWideningUpper(Vector128<short> value, byte count) => ShiftLeftLogicalWideningUpper(value, count);

        /// <summary>
        /// int64x2_t vshll_high_n_s32 (int32x4_t a, const int n)
        ///   A32: VSHLL.S32 Qd, Dm+1, #n
        ///   A64: SSHLL2 Vd.2D, Vn.4S, #n
        /// </summary>
        public static Vector128<long> ShiftLeftLogicalWideningUpper(Vector128<int> value, byte count) => ShiftLeftLogicalWideningUpper(value, count);

        /// <summary>
        /// int16x8_t vshll_high_n_s8 (int8x16_t a, const int n)
        ///   A32: VSHLL.S8 Qd, Dm+1, #n
        ///   A64: SSHLL2 Vd.8H, Vn.16B, #n
        /// </summary>
        public static Vector128<short> ShiftLeftLogicalWideningUpper(Vector128<sbyte> value, byte count) => ShiftLeftLogicalWideningUpper(value, count);

        /// <summary>
        /// uint32x4_t vshll_high_n_u16 (uint16x8_t a, const int n)
        ///   A32: VSHLL.U16 Qd, Dm+1, #n
        ///   A64: USHLL2 Vd.4S, Vn.8H, #n
        /// </summary>
        public static Vector128<uint> ShiftLeftLogicalWideningUpper(Vector128<ushort> value, byte count) => ShiftLeftLogicalWideningUpper(value, count);

        /// <summary>
        /// uint64x2_t vshll_high_n_u32 (uint32x4_t a, const int n)
        ///   A32: VSHLL.U32 Qd, Dm+1, #n
        ///   A64: USHLL2 Vd.2D, Vn.4S, #n
        /// </summary>
        public static Vector128<ulong> ShiftLeftLogicalWideningUpper(Vector128<uint> value, byte count) => ShiftLeftLogicalWideningUpper(value, count);

        /// <summary>
        /// uint8x8_t vshl_u8 (uint8x8_t a, int8x8_t b)
        ///   A32: VSHL.U8 Dd, Dn, Dm
        ///   A64: USHL Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> ShiftLogical(Vector64<byte> value, Vector64<sbyte> count) => ShiftLogical(value, count);

        /// <summary>
        /// uint16x4_t vshl_u16 (uint16x4_t a, int16x4_t b)
        ///   A32: VSHL.U16 Dd, Dn, Dm
        ///   A64: USHL Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> ShiftLogical(Vector64<short> value, Vector64<short> count) => ShiftLogical(value, count);

        /// <summary>
        /// uint32x2_t vshl_u32 (uint32x2_t a, int32x2_t b)
        ///   A32: VSHL.U32 Dd, Dn, Dm
        ///   A64: USHL Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> ShiftLogical(Vector64<int> value, Vector64<int> count) => ShiftLogical(value, count);

        /// <summary>
        /// uint8x8_t vshl_u8 (uint8x8_t a, int8x8_t b)
        ///   A32: VSHL.U8 Dd, Dn, Dm
        ///   A64: USHL Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> ShiftLogical(Vector64<sbyte> value, Vector64<sbyte> count) => ShiftLogical(value, count);

        /// <summary>
        /// uint16x4_t vshl_u16 (uint16x4_t a, int16x4_t b)
        ///   A32: VSHL.U16 Dd, Dn, Dm
        ///   A64: USHL Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> ShiftLogical(Vector64<ushort> value, Vector64<short> count) => ShiftLogical(value, count);

        /// <summary>
        /// uint32x2_t vshl_u32 (uint32x2_t a, int32x2_t b)
        ///   A32: VSHL.U32 Dd, Dn, Dm
        ///   A64: USHL Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> ShiftLogical(Vector64<uint> value, Vector64<int> count) => ShiftLogical(value, count);

        /// <summary>
        /// uint8x16_t vshlq_u8 (uint8x16_t a, int8x16_t b)
        ///   A32: VSHL.U8 Qd, Qn, Qm
        ///   A64: USHL Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> ShiftLogical(Vector128<byte> value, Vector128<sbyte> count) => ShiftLogical(value, count);

        /// <summary>
        /// uint16x8_t vshlq_u16 (uint16x8_t a, int16x8_t b)
        ///   A32: VSHL.U16 Qd, Qn, Qm
        ///   A64: USHL Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> ShiftLogical(Vector128<short> value, Vector128<short> count) => ShiftLogical(value, count);

        /// <summary>
        /// uint32x4_t vshlq_u32 (uint32x4_t a, int32x4_t b)
        ///   A32: VSHL.U32 Qd, Qn, Qm
        ///   A64: USHL Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> ShiftLogical(Vector128<int> value, Vector128<int> count) => ShiftLogical(value, count);

        /// <summary>
        /// uint64x2_t vshlq_u64 (uint64x2_t a, int64x2_t b)
        ///   A32: VSHL.U64 Qd, Qn, Qm
        ///   A64: USHL Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<long> ShiftLogical(Vector128<long> value, Vector128<long> count) => ShiftLogical(value, count);

        /// <summary>
        /// uint8x16_t vshlq_u8 (uint8x16_t a, int8x16_t b)
        ///   A32: VSHL.U8 Qd, Qn, Qm
        ///   A64: USHL Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> ShiftLogical(Vector128<sbyte> value, Vector128<sbyte> count) => ShiftLogical(value, count);

        /// <summary>
        /// uint16x8_t vshlq_u16 (uint16x8_t a, int16x8_t b)
        ///   A32: VSHL.U16 Qd, Qn, Qm
        ///   A64: USHL Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> ShiftLogical(Vector128<ushort> value, Vector128<short> count) => ShiftLogical(value, count);

        /// <summary>
        /// uint32x4_t vshlq_u32 (uint32x4_t a, int32x4_t b)
        ///   A32: VSHL.U32 Qd, Qn, Qm
        ///   A64: USHL Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> ShiftLogical(Vector128<uint> value, Vector128<int> count) => ShiftLogical(value, count);

        /// <summary>
        /// uint64x2_t vshlq_u64 (uint64x2_t a, int64x2_t b)
        ///   A32: VSHL.U64 Qd, Qn, Qm
        ///   A64: USHL Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<ulong> ShiftLogical(Vector128<ulong> value, Vector128<long> count) => ShiftLogical(value, count);

        /// <summary>
        /// uint8x8_t vrshl_u8 (uint8x8_t a, int8x8_t b)
        ///   A32: VRSHL.U8 Dd, Dn, Dm
        ///   A64: URSHL Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> ShiftLogicalRounded(Vector64<byte> value, Vector64<sbyte> count) => ShiftLogicalRounded(value, count);

        /// <summary>
        /// uint16x4_t vrshl_u16 (uint16x4_t a, int16x4_t b)
        ///   A32: VRSHL.U16 Dd, Dn, Dm
        ///   A64: URSHL Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> ShiftLogicalRounded(Vector64<short> value, Vector64<short> count) => ShiftLogicalRounded(value, count);

        /// <summary>
        /// uint32x2_t vrshl_u32 (uint32x2_t a, int32x2_t b)
        ///   A32: VRSHL.U32 Dd, Dn, Dm
        ///   A64: URSHL Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> ShiftLogicalRounded(Vector64<int> value, Vector64<int> count) => ShiftLogicalRounded(value, count);

        /// <summary>
        /// uint8x8_t vrshl_u8 (uint8x8_t a, int8x8_t b)
        ///   A32: VRSHL.U8 Dd, Dn, Dm
        ///   A64: URSHL Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> ShiftLogicalRounded(Vector64<sbyte> value, Vector64<sbyte> count) => ShiftLogicalRounded(value, count);

        /// <summary>
        /// uint16x4_t vrshl_u16 (uint16x4_t a, int16x4_t b)
        ///   A32: VRSHL.U16 Dd, Dn, Dm
        ///   A64: URSHL Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> ShiftLogicalRounded(Vector64<ushort> value, Vector64<short> count) => ShiftLogicalRounded(value, count);

        /// <summary>
        /// uint32x2_t vrshl_u32 (uint32x2_t a, int32x2_t b)
        ///   A32: VRSHL.U32 Dd, Dn, Dm
        ///   A64: URSHL Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> ShiftLogicalRounded(Vector64<uint> value, Vector64<int> count) => ShiftLogicalRounded(value, count);

        /// <summary>
        /// uint8x16_t vrshlq_u8 (uint8x16_t a, int8x16_t b)
        ///   A32: VRSHL.U8 Qd, Qn, Qm
        ///   A64: URSHL Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> ShiftLogicalRounded(Vector128<byte> value, Vector128<sbyte> count) => ShiftLogicalRounded(value, count);

        /// <summary>
        /// uint16x8_t vrshlq_u16 (uint16x8_t a, int16x8_t b)
        ///   A32: VRSHL.U16 Qd, Qn, Qm
        ///   A64: URSHL Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> ShiftLogicalRounded(Vector128<short> value, Vector128<short> count) => ShiftLogicalRounded(value, count);

        /// <summary>
        /// uint32x4_t vrshlq_u32 (uint32x4_t a, int32x4_t b)
        ///   A32: VRSHL.U32 Qd, Qn, Qm
        ///   A64: URSHL Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> ShiftLogicalRounded(Vector128<int> value, Vector128<int> count) => ShiftLogicalRounded(value, count);

        /// <summary>
        /// uint64x2_t vrshlq_u64 (uint64x2_t a, int64x2_t b)
        ///   A32: VRSHL.U64 Qd, Qn, Qm
        ///   A64: URSHL Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<long> ShiftLogicalRounded(Vector128<long> value, Vector128<long> count) => ShiftLogicalRounded(value, count);

        /// <summary>
        /// uint8x16_t vrshlq_u8 (uint8x16_t a, int8x16_t b)
        ///   A32: VRSHL.U8 Qd, Qn, Qm
        ///   A64: URSHL Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> ShiftLogicalRounded(Vector128<sbyte> value, Vector128<sbyte> count) => ShiftLogicalRounded(value, count);

        /// <summary>
        /// uint16x8_t vrshlq_u16 (uint16x8_t a, int16x8_t b)
        ///   A32: VRSHL.U16 Qd, Qn, Qm
        ///   A64: URSHL Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> ShiftLogicalRounded(Vector128<ushort> value, Vector128<short> count) => ShiftLogicalRounded(value, count);

        /// <summary>
        /// uint32x4_t vrshlq_u32 (uint32x4_t a, int32x4_t b)
        ///   A32: VRSHL.U32 Qd, Qn, Qm
        ///   A64: URSHL Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> ShiftLogicalRounded(Vector128<uint> value, Vector128<int> count) => ShiftLogicalRounded(value, count);

        /// <summary>
        /// uint64x2_t vrshlq_u64 (uint64x2_t a, int64x2_t b)
        ///   A32: VRSHL.U64 Qd, Qn, Qm
        ///   A64: URSHL Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<ulong> ShiftLogicalRounded(Vector128<ulong> value, Vector128<long> count) => ShiftLogicalRounded(value, count);

        /// <summary>
        /// uint8x8_t vqrshl_u8 (uint8x8_t a, int8x8_t b)
        ///   A32: VQRSHL.U8 Dd, Dn, Dm
        ///   A64: UQRSHL Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> ShiftLogicalRoundedSaturate(Vector64<byte> value, Vector64<sbyte> count) => ShiftLogicalRoundedSaturate(value, count);

        /// <summary>
        /// uint16x4_t vqrshl_u16 (uint16x4_t a, int16x4_t b)
        ///   A32: VQRSHL.U16 Dd, Dn, Dm
        ///   A64: UQRSHL Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> ShiftLogicalRoundedSaturate(Vector64<short> value, Vector64<short> count) => ShiftLogicalRoundedSaturate(value, count);

        /// <summary>
        /// uint32x2_t vqrshl_u32 (uint32x2_t a, int32x2_t b)
        ///   A32: VQRSHL.U32 Dd, Dn, Dm
        ///   A64: UQRSHL Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> ShiftLogicalRoundedSaturate(Vector64<int> value, Vector64<int> count) => ShiftLogicalRoundedSaturate(value, count);

        /// <summary>
        /// uint8x8_t vqrshl_u8 (uint8x8_t a, int8x8_t b)
        ///   A32: VQRSHL.U8 Dd, Dn, Dm
        ///   A64: UQRSHL Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> ShiftLogicalRoundedSaturate(Vector64<sbyte> value, Vector64<sbyte> count) => ShiftLogicalRoundedSaturate(value, count);

        /// <summary>
        /// uint16x4_t vqrshl_u16 (uint16x4_t a, int16x4_t b)
        ///   A32: VQRSHL.U16 Dd, Dn, Dm
        ///   A64: UQRSHL Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> ShiftLogicalRoundedSaturate(Vector64<ushort> value, Vector64<short> count) => ShiftLogicalRoundedSaturate(value, count);

        /// <summary>
        /// uint32x2_t vqrshl_u32 (uint32x2_t a, int32x2_t b)
        ///   A32: VQRSHL.U32 Dd, Dn, Dm
        ///   A64: UQRSHL Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> ShiftLogicalRoundedSaturate(Vector64<uint> value, Vector64<int> count) => ShiftLogicalRoundedSaturate(value, count);

        /// <summary>
        /// uint8x16_t vqrshlq_u8 (uint8x16_t a, int8x16_t b)
        ///   A32: VQRSHL.U8 Qd, Qn, Qm
        ///   A64: UQRSHL Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> ShiftLogicalRoundedSaturate(Vector128<byte> value, Vector128<sbyte> count) => ShiftLogicalRoundedSaturate(value, count);

        /// <summary>
        /// uint16x8_t vqrshlq_u16 (uint16x8_t a, int16x8_t b)
        ///   A32: VQRSHL.U16 Qd, Qn, Qm
        ///   A64: UQRSHL Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> ShiftLogicalRoundedSaturate(Vector128<short> value, Vector128<short> count) => ShiftLogicalRoundedSaturate(value, count);

        /// <summary>
        /// uint32x4_t vqrshlq_u32 (uint32x4_t a, int32x4_t b)
        ///   A32: VQRSHL.U32 Qd, Qn, Qm
        ///   A64: UQRSHL Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> ShiftLogicalRoundedSaturate(Vector128<int> value, Vector128<int> count) => ShiftLogicalRoundedSaturate(value, count);

        /// <summary>
        /// uint64x2_t vqrshlq_u64 (uint64x2_t a, int64x2_t b)
        ///   A32: VQRSHL.U64 Qd, Qn, Qm
        ///   A64: UQRSHL Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<long> ShiftLogicalRoundedSaturate(Vector128<long> value, Vector128<long> count) => ShiftLogicalRoundedSaturate(value, count);

        /// <summary>
        /// uint8x16_t vqrshlq_u8 (uint8x16_t a, int8x16_t b)
        ///   A32: VQRSHL.U8 Qd, Qn, Qm
        ///   A64: UQRSHL Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> ShiftLogicalRoundedSaturate(Vector128<sbyte> value, Vector128<sbyte> count) => ShiftLogicalRoundedSaturate(value, count);

        /// <summary>
        /// uint16x8_t vqrshlq_u16 (uint16x8_t a, int16x8_t b)
        ///   A32: VQRSHL.U16 Qd, Qn, Qm
        ///   A64: UQRSHL Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> ShiftLogicalRoundedSaturate(Vector128<ushort> value, Vector128<short> count) => ShiftLogicalRoundedSaturate(value, count);

        /// <summary>
        /// uint32x4_t vqrshlq_u32 (uint32x4_t a, int32x4_t b)
        ///   A32: VQRSHL.U32 Qd, Qn, Qm
        ///   A64: UQRSHL Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> ShiftLogicalRoundedSaturate(Vector128<uint> value, Vector128<int> count) => ShiftLogicalRoundedSaturate(value, count);

        /// <summary>
        /// uint64x2_t vqrshlq_u64 (uint64x2_t a, int64x2_t b)
        ///   A32: VQRSHL.U64 Qd, Qn, Qm
        ///   A64: UQRSHL Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<ulong> ShiftLogicalRoundedSaturate(Vector128<ulong> value, Vector128<long> count) => ShiftLogicalRoundedSaturate(value, count);

        /// <summary>
        /// uint64x1_t vqrshl_u64 (uint64x1_t a, int64x1_t b)
        ///   A32: VQRSHL.U64 Dd, Dn, Dm
        ///   A64: UQRSHL Dd, Dn, Dm
        /// </summary>
        public static Vector64<long> ShiftLogicalRoundedSaturateScalar(Vector64<long> value, Vector64<long> count) => ShiftLogicalRoundedSaturateScalar(value, count);

        /// <summary>
        /// uint64x1_t vqrshl_u64 (uint64x1_t a, int64x1_t b)
        ///   A32: VQRSHL.U64 Dd, Dn, Dm
        ///   A64: UQRSHL Dd, Dn, Dm
        /// </summary>
        public static Vector64<ulong> ShiftLogicalRoundedSaturateScalar(Vector64<ulong> value, Vector64<long> count) => ShiftLogicalRoundedSaturateScalar(value, count);

        /// <summary>
        /// uint64x1_t vrshl_u64 (uint64x1_t a, int64x1_t b)
        ///   A32: VRSHL.U64 Dd, Dn, Dm
        ///   A64: URSHL Dd, Dn, Dm
        /// </summary>
        public static Vector64<long> ShiftLogicalRoundedScalar(Vector64<long> value, Vector64<long> count) => ShiftLogicalRoundedScalar(value, count);

        /// <summary>
        /// uint64x1_t vrshl_u64 (uint64x1_t a, int64x1_t b)
        ///   A32: VRSHL.U64 Dd, Dn, Dm
        ///   A64: URSHL Dd, Dn, Dm
        /// </summary>
        public static Vector64<ulong> ShiftLogicalRoundedScalar(Vector64<ulong> value, Vector64<long> count) => ShiftLogicalRoundedScalar(value, count);

        /// <summary>
        /// uint8x8_t vqshl_u8 (uint8x8_t a, int8x8_t b)
        ///   A32: VQSHL.U8 Dd, Dn, Dm
        ///   A64: UQSHL Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> ShiftLogicalSaturate(Vector64<byte> value, Vector64<sbyte> count) => ShiftLogicalSaturate(value, count);

        /// <summary>
        /// uint16x4_t vqshl_u16 (uint16x4_t a, int16x4_t b)
        ///   A32: VQSHL.U16 Dd, Dn, Dm
        ///   A64: UQSHL Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> ShiftLogicalSaturate(Vector64<short> value, Vector64<short> count) => ShiftLogicalSaturate(value, count);

        /// <summary>
        /// uint32x2_t vqshl_u32 (uint32x2_t a, int32x2_t b)
        ///   A32: VQSHL.U32 Dd, Dn, Dm
        ///   A64: UQSHL Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> ShiftLogicalSaturate(Vector64<int> value, Vector64<int> count) => ShiftLogicalSaturate(value, count);

        /// <summary>
        /// uint8x8_t vqshl_u8 (uint8x8_t a, int8x8_t b)
        ///   A32: VQSHL.U8 Dd, Dn, Dm
        ///   A64: UQSHL Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> ShiftLogicalSaturate(Vector64<sbyte> value, Vector64<sbyte> count) => ShiftLogicalSaturate(value, count);

        /// <summary>
        /// uint16x4_t vqshl_u16 (uint16x4_t a, int16x4_t b)
        ///   A32: VQSHL.U16 Dd, Dn, Dm
        ///   A64: UQSHL Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> ShiftLogicalSaturate(Vector64<ushort> value, Vector64<short> count) => ShiftLogicalSaturate(value, count);

        /// <summary>
        /// uint32x2_t vqshl_u32 (uint32x2_t a, int32x2_t b)
        ///   A32: VQSHL.U32 Dd, Dn, Dm
        ///   A64: UQSHL Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> ShiftLogicalSaturate(Vector64<uint> value, Vector64<int> count) => ShiftLogicalSaturate(value, count);

        /// <summary>
        /// uint8x16_t vqshlq_u8 (uint8x16_t a, int8x16_t b)
        ///   A32: VQSHL.U8 Qd, Qn, Qm
        ///   A64: UQSHL Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> ShiftLogicalSaturate(Vector128<byte> value, Vector128<sbyte> count) => ShiftLogicalSaturate(value, count);

        /// <summary>
        /// uint16x8_t vqshlq_u16 (uint16x8_t a, int16x8_t b)
        ///   A32: VQSHL.U16 Qd, Qn, Qm
        ///   A64: UQSHL Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> ShiftLogicalSaturate(Vector128<short> value, Vector128<short> count) => ShiftLogicalSaturate(value, count);

        /// <summary>
        /// uint32x4_t vqshlq_u32 (uint32x4_t a, int32x4_t b)
        ///   A32: VQSHL.U32 Qd, Qn, Qm
        ///   A64: UQSHL Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> ShiftLogicalSaturate(Vector128<int> value, Vector128<int> count) => ShiftLogicalSaturate(value, count);

        /// <summary>
        /// uint64x2_t vqshlq_u64 (uint64x2_t a, int64x2_t b)
        ///   A32: VQSHL.U64 Qd, Qn, Qm
        ///   A64: UQSHL Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<long> ShiftLogicalSaturate(Vector128<long> value, Vector128<long> count) => ShiftLogicalSaturate(value, count);

        /// <summary>
        /// uint8x16_t vqshlq_u8 (uint8x16_t a, int8x16_t b)
        ///   A32: VQSHL.U8 Qd, Qn, Qm
        ///   A64: UQSHL Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> ShiftLogicalSaturate(Vector128<sbyte> value, Vector128<sbyte> count) => ShiftLogicalSaturate(value, count);

        /// <summary>
        /// uint16x8_t vqshlq_u16 (uint16x8_t a, int16x8_t b)
        ///   A32: VQSHL.U16 Qd, Qn, Qm
        ///   A64: UQSHL Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> ShiftLogicalSaturate(Vector128<ushort> value, Vector128<short> count) => ShiftLogicalSaturate(value, count);

        /// <summary>
        /// uint32x4_t vqshlq_u32 (uint32x4_t a, int32x4_t b)
        ///   A32: VQSHL.U32 Qd, Qn, Qm
        ///   A64: UQSHL Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> ShiftLogicalSaturate(Vector128<uint> value, Vector128<int> count) => ShiftLogicalSaturate(value, count);

        /// <summary>
        /// uint64x2_t vqshlq_u64 (uint64x2_t a, int64x2_t b)
        ///   A32: VQSHL.U64 Qd, Qn, Qm
        ///   A64: UQSHL Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<ulong> ShiftLogicalSaturate(Vector128<ulong> value, Vector128<long> count) => ShiftLogicalSaturate(value, count);

        /// <summary>
        /// uint64x1_t vqshl_u64 (uint64x1_t a, int64x1_t b)
        ///   A32: VQSHL.U64 Dd, Dn, Dm
        ///   A64: UQSHL Dd, Dn, Dm
        /// </summary>
        public static Vector64<long> ShiftLogicalSaturateScalar(Vector64<long> value, Vector64<long> count) => ShiftLogicalSaturateScalar(value, count);

        /// <summary>
        /// uint64x1_t vqshl_u64 (uint64x1_t a, int64x1_t b)
        ///   A32: VQSHL.U64 Dd, Dn, Dm
        ///   A64: UQSHL Dd, Dn, Dm
        /// </summary>
        public static Vector64<ulong> ShiftLogicalSaturateScalar(Vector64<ulong> value, Vector64<long> count) => ShiftLogicalSaturateScalar(value, count);

        /// <summary>
        /// uint64x1_t vshl_u64 (uint64x1_t a, int64x1_t b)
        ///   A32: VSHL.U64 Dd, Dn, Dm
        ///   A64: USHL Dd, Dn, Dm
        /// </summary>
        public static Vector64<long> ShiftLogicalScalar(Vector64<long> value, Vector64<long> count) => ShiftLogicalScalar(value, count);

        /// <summary>
        /// uint64x1_t vshl_u64 (uint64x1_t a, int64x1_t b)
        ///   A32: VSHL.U64 Dd, Dn, Dm
        ///   A64: USHL Dd, Dn, Dm
        /// </summary>
        public static Vector64<ulong> ShiftLogicalScalar(Vector64<ulong> value, Vector64<long> count) => ShiftLogicalScalar(value, count);

        /// <summary>
        /// int16x4_t vshr_n_s16 (int16x4_t a, const int n)
        ///   A32: VSHR.S16 Dd, Dm, #n
        ///   A64: SSHR Vd.4H, Vn.4H, #n
        /// </summary>
        public static Vector64<short> ShiftRightArithmetic(Vector64<short> value, byte count) => ShiftRightArithmetic(value, count);

        /// <summary>
        /// int32x2_t vshr_n_s32 (int32x2_t a, const int n)
        ///   A32: VSHR.S32 Dd, Dm, #n
        ///   A64: SSHR Vd.2S, Vn.2S, #n
        /// </summary>
        public static Vector64<int> ShiftRightArithmetic(Vector64<int> value, byte count) => ShiftRightArithmetic(value, count);

        /// <summary>
        /// int8x8_t vshr_n_s8 (int8x8_t a, const int n)
        ///   A32: VSHR.S8 Dd, Dm, #n
        ///   A64: SSHR Vd.8B, Vn.8B, #n
        /// </summary>
        public static Vector64<sbyte> ShiftRightArithmetic(Vector64<sbyte> value, byte count) => ShiftRightArithmetic(value, count);

        /// <summary>
        /// int16x8_t vshrq_n_s16 (int16x8_t a, const int n)
        ///   A32: VSHR.S16 Qd, Qm, #n
        ///   A64: SSHR Vd.8H, Vn.8H, #n
        /// </summary>
        public static Vector128<short> ShiftRightArithmetic(Vector128<short> value, byte count) => ShiftRightArithmetic(value, count);

        /// <summary>
        /// int32x4_t vshrq_n_s32 (int32x4_t a, const int n)
        ///   A32: VSHR.S32 Qd, Qm, #n
        ///   A64: SSHR Vd.4S, Vn.4S, #n
        /// </summary>
        public static Vector128<int> ShiftRightArithmetic(Vector128<int> value, byte count) => ShiftRightArithmetic(value, count);

        /// <summary>
        /// int64x2_t vshrq_n_s64 (int64x2_t a, const int n)
        ///   A32: VSHR.S64 Qd, Qm, #n
        ///   A64: SSHR Vd.2D, Vn.2D, #n
        /// </summary>
        public static Vector128<long> ShiftRightArithmetic(Vector128<long> value, byte count) => ShiftRightArithmetic(value, count);

        /// <summary>
        /// int8x16_t vshrq_n_s8 (int8x16_t a, const int n)
        ///   A32: VSHR.S8 Qd, Qm, #n
        ///   A64: SSHR Vd.16B, Vn.16B, #n
        /// </summary>
        public static Vector128<sbyte> ShiftRightArithmetic(Vector128<sbyte> value, byte count) => ShiftRightArithmetic(value, count);

        /// <summary>
        /// int16x4_t vsra_n_s16 (int16x4_t a, int16x4_t b, const int n)
        ///   A32: VSRA.S16 Dd, Dm, #n
        ///   A64: SSRA Vd.4H, Vn.4H, #n
        /// </summary>
        public static Vector64<short> ShiftRightArithmeticAdd(Vector64<short> addend, Vector64<short> value, byte count) => ShiftRightArithmeticAdd(addend, value, count);

        /// <summary>
        /// int32x2_t vsra_n_s32 (int32x2_t a, int32x2_t b, const int n)
        ///   A32: VSRA.S32 Dd, Dm, #n
        ///   A64: SSRA Vd.2S, Vn.2S, #n
        /// </summary>
        public static Vector64<int> ShiftRightArithmeticAdd(Vector64<int> addend, Vector64<int> value, byte count) => ShiftRightArithmeticAdd(addend, value, count);

        /// <summary>
        /// int8x8_t vsra_n_s8 (int8x8_t a, int8x8_t b, const int n)
        ///   A32: VSRA.S8 Dd, Dm, #n
        ///   A64: SSRA Vd.8B, Vn.8B, #n
        /// </summary>
        public static Vector64<sbyte> ShiftRightArithmeticAdd(Vector64<sbyte> addend, Vector64<sbyte> value, byte count) => ShiftRightArithmeticAdd(addend, value, count);

        /// <summary>
        /// int16x8_t vsraq_n_s16 (int16x8_t a, int16x8_t b, const int n)
        ///   A32: VSRA.S16 Qd, Qm, #n
        ///   A64: SSRA Vd.8H, Vn.8H, #n
        /// </summary>
        public static Vector128<short> ShiftRightArithmeticAdd(Vector128<short> addend, Vector128<short> value, byte count) => ShiftRightArithmeticAdd(addend, value, count);

        /// <summary>
        /// int32x4_t vsraq_n_s32 (int32x4_t a, int32x4_t b, const int n)
        ///   A32: VSRA.S32 Qd, Qm, #n
        ///   A64: SSRA Vd.4S, Vn.4S, #n
        /// </summary>
        public static Vector128<int> ShiftRightArithmeticAdd(Vector128<int> addend, Vector128<int> value, byte count) => ShiftRightArithmeticAdd(addend, value, count);

        /// <summary>
        /// int64x2_t vsraq_n_s64 (int64x2_t a, int64x2_t b, const int n)
        ///   A32: VSRA.S64 Qd, Qm, #n
        ///   A64: SSRA Vd.2D, Vn.2D, #n
        /// </summary>
        public static Vector128<long> ShiftRightArithmeticAdd(Vector128<long> addend, Vector128<long> value, byte count) => ShiftRightArithmeticAdd(addend, value, count);

        /// <summary>
        /// int8x16_t vsraq_n_s8 (int8x16_t a, int8x16_t b, const int n)
        ///   A32: VSRA.S8 Qd, Qm, #n
        ///   A64: SSRA Vd.16B, Vn.16B, #n
        /// </summary>
        public static Vector128<sbyte> ShiftRightArithmeticAdd(Vector128<sbyte> addend, Vector128<sbyte> value, byte count) => ShiftRightArithmeticAdd(addend, value, count);

        /// <summary>
        /// int64x1_t vsra_n_s64 (int64x1_t a, int64x1_t b, const int n)
        ///   A32: VSRA.S64 Dd, Dm, #n
        ///   A64: SSRA Dd, Dn, #n
        /// </summary>
        public static Vector64<long> ShiftRightArithmeticAddScalar(Vector64<long> addend, Vector64<long> value, byte count) => ShiftRightArithmeticAddScalar(addend, value, count);

        /// <summary>
        /// int16x4_t vqshrn_n_s32 (int32x4_t a, const int n)
        ///   A32: VQSHRN.S32 Dd, Qm, #n
        ///   A64: SQSHRN Vd.4H, Vn.4S, #n
        /// </summary>
        public static Vector64<short> ShiftRightArithmeticNarrowingSaturateLower(Vector128<int> value, byte count) => ShiftRightArithmeticNarrowingSaturateLower(value, count);

        /// <summary>
        /// int32x2_t vqshrn_n_s64 (int64x2_t a, const int n)
        ///   A32: VQSHRN.S64 Dd, Qm, #n
        ///   A64: SQSHRN Vd.2S, Vn.2D, #n
        /// </summary>
        public static Vector64<int> ShiftRightArithmeticNarrowingSaturateLower(Vector128<long> value, byte count) => ShiftRightArithmeticNarrowingSaturateLower(value, count);

        /// <summary>
        /// int8x8_t vqshrn_n_s16 (int16x8_t a, const int n)
        ///   A32: VQSHRN.S16 Dd, Qm, #n
        ///   A64: SQSHRN Vd.8B, Vn.8H, #n
        /// </summary>
        public static Vector64<sbyte> ShiftRightArithmeticNarrowingSaturateLower(Vector128<short> value, byte count) => ShiftRightArithmeticNarrowingSaturateLower(value, count);

        /// <summary>
        /// uint8x8_t vqshrun_n_s16 (int16x8_t a, const int n)
        ///   A32: VQSHRUN.S16 Dd, Qm, #n
        ///   A64: SQSHRUN Vd.8B, Vn.8H, #n
        /// </summary>
        public static Vector64<byte> ShiftRightArithmeticNarrowingSaturateUnsignedLower(Vector128<short> value, byte count) => ShiftRightArithmeticNarrowingSaturateUnsignedLower(value, count);

        /// <summary>
        /// uint16x4_t vqshrun_n_s32 (int32x4_t a, const int n)
        ///   A32: VQSHRUN.S32 Dd, Qm, #n
        ///   A64: SQSHRUN Vd.4H, Vn.4S, #n
        /// </summary>
        public static Vector64<ushort> ShiftRightArithmeticNarrowingSaturateUnsignedLower(Vector128<int> value, byte count) => ShiftRightArithmeticNarrowingSaturateUnsignedLower(value, count);

        /// <summary>
        /// uint32x2_t vqshrun_n_s64 (int64x2_t a, const int n)
        ///   A32: VQSHRUN.S64 Dd, Qm, #n
        ///   A64: SQSHRUN Vd.2S, Vn.2D, #n
        /// </summary>
        public static Vector64<uint> ShiftRightArithmeticNarrowingSaturateUnsignedLower(Vector128<long> value, byte count) => ShiftRightArithmeticNarrowingSaturateUnsignedLower(value, count);

        /// <summary>
        /// uint8x16_t vqshrun_high_n_s16 (uint8x8_t r, int16x8_t a, const int n)
        ///   A32: VQSHRUN.S16 Dd+1, Dn, #n
        ///   A64: SQSHRUN2 Vd.16B, Vn.8H, #n
        /// </summary>
        public static Vector128<byte> ShiftRightArithmeticNarrowingSaturateUnsignedUpper(Vector64<byte> lower, Vector128<short> value, byte count) => ShiftRightArithmeticNarrowingSaturateUnsignedUpper(lower, value, count);

        /// <summary>
        /// uint16x8_t vqshrun_high_n_s32 (uint16x4_t r, int32x4_t a, const int n)
        ///   A32: VQSHRUN.S32 Dd+1, Dn, #n
        ///   A64: SQSHRUN2 Vd.8H, Vn.4S, #n
        /// </summary>
        public static Vector128<ushort> ShiftRightArithmeticNarrowingSaturateUnsignedUpper(Vector64<ushort> lower, Vector128<int> value, byte count) => ShiftRightArithmeticNarrowingSaturateUnsignedUpper(lower, value, count);

        /// <summary>
        /// uint32x4_t vqshrun_high_n_s64 (uint32x2_t r, int64x2_t a, const int n)
        ///   A32: VQSHRUN.S64 Dd+1, Dn, #n
        ///   A64: SQSHRUN2 Vd.4S, Vn.2D, #n
        /// </summary>
        public static Vector128<uint> ShiftRightArithmeticNarrowingSaturateUnsignedUpper(Vector64<uint> lower, Vector128<long> value, byte count) => ShiftRightArithmeticNarrowingSaturateUnsignedUpper(lower, value, count);

        /// <summary>
        /// int16x8_t vqshrn_high_n_s32 (int16x4_t r, int32x4_t a, const int n)
        ///   A32: VQSHRN.S32 Dd+1, Qm, #n
        ///   A64: SQSHRN2 Vd.8H, Vn.4S, #n
        /// </summary>
        public static Vector128<short> ShiftRightArithmeticNarrowingSaturateUpper(Vector64<short> lower, Vector128<int> value, byte count) => ShiftRightArithmeticNarrowingSaturateUpper(lower, value, count);

        /// <summary>
        /// int32x4_t vqshrn_high_n_s64 (int32x2_t r, int64x2_t a, const int n)
        ///   A32: VQSHRN.S64 Dd+1, Qm, #n
        ///   A64: SQSHRN2 Vd.4S, Vn.2D, #n
        /// </summary>
        public static Vector128<int> ShiftRightArithmeticNarrowingSaturateUpper(Vector64<int> lower, Vector128<long> value, byte count) => ShiftRightArithmeticNarrowingSaturateUpper(lower, value, count);

        /// <summary>
        /// int8x16_t vqshrn_high_n_s16 (int8x8_t r, int16x8_t a, const int n)
        ///   A32: VQSHRN.S16 Dd+1, Qm, #n
        ///   A64: SQSHRN2 Vd.16B, Vn.8H, #n
        /// </summary>
        public static Vector128<sbyte> ShiftRightArithmeticNarrowingSaturateUpper(Vector64<sbyte> lower, Vector128<short> value, byte count) => ShiftRightArithmeticNarrowingSaturateUpper(lower, value, count);

        /// <summary>
        /// int16x4_t vrshr_n_s16 (int16x4_t a, const int n)
        ///   A32: VRSHR.S16 Dd, Dm, #n
        ///   A64: SRSHR Vd.4H, Vn.4H, #n
        /// </summary>
        public static Vector64<short> ShiftRightArithmeticRounded(Vector64<short> value, byte count) => ShiftRightArithmeticRounded(value, count);

        /// <summary>
        /// int32x2_t vrshr_n_s32 (int32x2_t a, const int n)
        ///   A32: VRSHR.S32 Dd, Dm, #n
        ///   A64: SRSHR Vd.2S, Vn.2S, #n
        /// </summary>
        public static Vector64<int> ShiftRightArithmeticRounded(Vector64<int> value, byte count) => ShiftRightArithmeticRounded(value, count);

        /// <summary>
        /// int8x8_t vrshr_n_s8 (int8x8_t a, const int n)
        ///   A32: VRSHR.S8 Dd, Dm, #n
        ///   A64: SRSHR Vd.8B, Vn.8B, #n
        /// </summary>
        public static Vector64<sbyte> ShiftRightArithmeticRounded(Vector64<sbyte> value, byte count) => ShiftRightArithmeticRounded(value, count);

        /// <summary>
        /// int16x8_t vrshrq_n_s16 (int16x8_t a, const int n)
        ///   A32: VRSHR.S16 Qd, Qm, #n
        ///   A64: SRSHR Vd.8H, Vn.8H, #n
        /// </summary>
        public static Vector128<short> ShiftRightArithmeticRounded(Vector128<short> value, byte count) => ShiftRightArithmeticRounded(value, count);

        /// <summary>
        /// int32x4_t vrshrq_n_s32 (int32x4_t a, const int n)
        ///   A32: VRSHR.S32 Qd, Qm, #n
        ///   A64: SRSHR Vd.4S, Vn.4S, #n
        /// </summary>
        public static Vector128<int> ShiftRightArithmeticRounded(Vector128<int> value, byte count) => ShiftRightArithmeticRounded(value, count);

        /// <summary>
        /// int64x2_t vrshrq_n_s64 (int64x2_t a, const int n)
        ///   A32: VRSHR.S64 Qd, Qm, #n
        ///   A64: SRSHR Vd.2D, Vn.2D, #n
        /// </summary>
        public static Vector128<long> ShiftRightArithmeticRounded(Vector128<long> value, byte count) => ShiftRightArithmeticRounded(value, count);

        /// <summary>
        /// int8x16_t vrshrq_n_s8 (int8x16_t a, const int n)
        ///   A32: VRSHR.S8 Qd, Qm, #n
        ///   A64: SRSHR Vd.16B, Vn.16B, #n
        /// </summary>
        public static Vector128<sbyte> ShiftRightArithmeticRounded(Vector128<sbyte> value, byte count) => ShiftRightArithmeticRounded(value, count);

        /// <summary>
        /// int16x4_t vrsra_n_s16 (int16x4_t a, int16x4_t b, const int n)
        ///   A32: VRSRA.S16 Dd, Dm, #n
        ///   A64: SRSRA Vd.4H, Vn.4H, #n
        /// </summary>
        public static Vector64<short> ShiftRightArithmeticRoundedAdd(Vector64<short> addend, Vector64<short> value, byte count) => ShiftRightArithmeticRoundedAdd(addend, value, count);

        /// <summary>
        /// int32x2_t vrsra_n_s32 (int32x2_t a, int32x2_t b, const int n)
        ///   A32: VRSRA.S32 Dd, Dm, #n
        ///   A64: SRSRA Vd.2S, Vn.2S, #n
        /// </summary>
        public static Vector64<int> ShiftRightArithmeticRoundedAdd(Vector64<int> addend, Vector64<int> value, byte count) => ShiftRightArithmeticRoundedAdd(addend, value, count);

        /// <summary>
        /// int8x8_t vrsra_n_s8 (int8x8_t a, int8x8_t b, const int n)
        ///   A32: VRSRA.S8 Dd, Dm, #n
        ///   A64: SRSRA Vd.8B, Vn.8B, #n
        /// </summary>
        public static Vector64<sbyte> ShiftRightArithmeticRoundedAdd(Vector64<sbyte> addend, Vector64<sbyte> value, byte count) => ShiftRightArithmeticRoundedAdd(addend, value, count);

        /// <summary>
        /// int16x8_t vrsraq_n_s16 (int16x8_t a, int16x8_t b, const int n)
        ///   A32: VRSRA.S16 Qd, Qm, #n
        ///   A64: SRSRA Vd.8H, Vn.8H, #n
        /// </summary>
        public static Vector128<short> ShiftRightArithmeticRoundedAdd(Vector128<short> addend, Vector128<short> value, byte count) => ShiftRightArithmeticRoundedAdd(addend, value, count);

        /// <summary>
        /// int32x4_t vrsraq_n_s32 (int32x4_t a, int32x4_t b, const int n)
        ///   A32: VRSRA.S32 Qd, Qm, #n
        ///   A64: SRSRA Vd.4S, Vn.4S, #n
        /// </summary>
        public static Vector128<int> ShiftRightArithmeticRoundedAdd(Vector128<int> addend, Vector128<int> value, byte count) => ShiftRightArithmeticRoundedAdd(addend, value, count);

        /// <summary>
        /// int64x2_t vrsraq_n_s64 (int64x2_t a, int64x2_t b, const int n)
        ///   A32: VRSRA.S64 Qd, Qm, #n
        ///   A64: SRSRA Vd.2D, Vn.2D, #n
        /// </summary>
        public static Vector128<long> ShiftRightArithmeticRoundedAdd(Vector128<long> addend, Vector128<long> value, byte count) => ShiftRightArithmeticRoundedAdd(addend, value, count);

        /// <summary>
        /// int8x16_t vrsraq_n_s8 (int8x16_t a, int8x16_t b, const int n)
        ///   A32: VRSRA.S8 Qd, Qm, #n
        ///   A64: SRSRA Vd.16B, Vn.16B, #n
        /// </summary>
        public static Vector128<sbyte> ShiftRightArithmeticRoundedAdd(Vector128<sbyte> addend, Vector128<sbyte> value, byte count) => ShiftRightArithmeticRoundedAdd(addend, value, count);

        /// <summary>
        /// int64x1_t vrsra_n_s64 (int64x1_t a, int64x1_t b, const int n)
        ///   A32: VRSRA.S64 Dd, Dm, #n
        ///   A64: SRSRA Dd, Dn, #n
        /// </summary>
        public static Vector64<long> ShiftRightArithmeticRoundedAddScalar(Vector64<long> addend, Vector64<long> value, byte count) => ShiftRightArithmeticRoundedAddScalar(addend, value, count);

        /// <summary>
        /// int16x4_t vqrshrn_n_s32 (int32x4_t a, const int n)
        ///   A32: VQRSHRN.S32 Dd, Qm, #n
        ///   A64: SQRSHRN Vd.4H, Vn.4S, #n
        /// </summary>
        public static Vector64<short> ShiftRightArithmeticRoundedNarrowingSaturateLower(Vector128<int> value, byte count) => ShiftRightArithmeticRoundedNarrowingSaturateLower(value, count);

        /// <summary>
        /// int32x2_t vqrshrn_n_s64 (int64x2_t a, const int n)
        ///   A32: VQRSHRN.S64 Dd, Qm, #n
        ///   A64: SQRSHRN Vd.2S, Vn.2D, #n
        /// </summary>
        public static Vector64<int> ShiftRightArithmeticRoundedNarrowingSaturateLower(Vector128<long> value, byte count) => ShiftRightArithmeticRoundedNarrowingSaturateLower(value, count);

        /// <summary>
        /// int8x8_t vqrshrn_n_s16 (int16x8_t a, const int n)
        ///   A32: VQRSHRN.S16 Dd, Qm, #n
        ///   A64: SQRSHRN Vd.8B, Vn.8H, #n
        /// </summary>
        public static Vector64<sbyte> ShiftRightArithmeticRoundedNarrowingSaturateLower(Vector128<short> value, byte count) => ShiftRightArithmeticRoundedNarrowingSaturateLower(value, count);

        /// <summary>
        /// uint8x8_t vqrshrun_n_s16 (int16x8_t a, const int n)
        ///   A32: VQRSHRUN.S16 Dd, Qm, #n
        ///   A64: SQRSHRUN Vd.8B, Vn.8H, #n
        /// </summary>
        public static Vector64<byte> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLower(Vector128<short> value, byte count) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLower(value, count);

        /// <summary>
        /// uint16x4_t vqrshrun_n_s32 (int32x4_t a, const int n)
        ///   A32: VQRSHRUN.S32 Dd, Qm, #n
        ///   A64: SQRSHRUN Vd.4H, Vn.4S, #n
        /// </summary>
        public static Vector64<ushort> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLower(Vector128<int> value, byte count) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLower(value, count);

        /// <summary>
        /// uint32x2_t vqrshrun_n_s64 (int64x2_t a, const int n)
        ///   A32: VQRSHRUN.S64 Dd, Qm, #n
        ///   A64: SQRSHRUN Vd.2S, Vn.2D, #n
        /// </summary>
        public static Vector64<uint> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLower(Vector128<long> value, byte count) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLower(value, count);

        /// <summary>
        /// uint8x16_t vqrshrun_high_n_s16 (uint8x8_t r, int16x8_t a, const int n)
        ///   A32: VQRSHRUN.S16 Dd+1, Dn, #n
        ///   A64: SQRSHRUN2 Vd.16B, Vn.8H, #n
        /// </summary>
        public static Vector128<byte> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedUpper(Vector64<byte> lower, Vector128<short> value, byte count) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedUpper(lower, value, count);

        /// <summary>
        /// uint16x8_t vqrshrun_high_n_s32 (uint16x4_t r, int32x4_t a, const int n)
        ///   A32: VQRSHRUN.S32 Dd+1, Dn, #n
        ///   A64: SQRSHRUN2 Vd.8H, Vn.4S, #n
        /// </summary>
        public static Vector128<ushort> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedUpper(Vector64<ushort> lower, Vector128<int> value, byte count) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedUpper(lower, value, count);

        /// <summary>
        /// uint32x4_t vqrshrun_high_n_s64 (uint32x2_t r, int64x2_t a, const int n)
        ///   A32: VQRSHRUN.S64 Dd+1, Dn, #n
        ///   A64: SQRSHRUN2 Vd.4S, Vn.2D, #n
        /// </summary>
        public static Vector128<uint> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedUpper(Vector64<uint> lower, Vector128<long> value, byte count) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedUpper(lower, value, count);

        /// <summary>
        /// int16x8_t vqrshrn_high_n_s32 (int16x4_t r, int32x4_t a, const int n)
        ///   A32: VQRSHRN.S32 Dd+1, Dn, #n
        ///   A64: SQRSHRN2 Vd.8H, Vn.4S, #n
        /// </summary>
        public static Vector128<short> ShiftRightArithmeticRoundedNarrowingSaturateUpper(Vector64<short> lower, Vector128<int> value, byte count) => ShiftRightArithmeticRoundedNarrowingSaturateUpper(lower, value, count);

        /// <summary>
        /// int32x4_t vqrshrn_high_n_s64 (int32x2_t r, int64x2_t a, const int n)
        ///   A32: VQRSHRN.S64 Dd+1, Dn, #n
        ///   A64: SQRSHRN2 Vd.4S, Vn.2D, #n
        /// </summary>
        public static Vector128<int> ShiftRightArithmeticRoundedNarrowingSaturateUpper(Vector64<int> lower, Vector128<long> value, byte count) => ShiftRightArithmeticRoundedNarrowingSaturateUpper(lower, value, count);

        /// <summary>
        /// int8x16_t vqrshrn_high_n_s16 (int8x8_t r, int16x8_t a, const int n)
        ///   A32: VQRSHRN.S16 Dd+1, Dn, #n
        ///   A64: SQRSHRN2 Vd.16B, Vn.8H, #n
        /// </summary>
        public static Vector128<sbyte> ShiftRightArithmeticRoundedNarrowingSaturateUpper(Vector64<sbyte> lower, Vector128<short> value, byte count) => ShiftRightArithmeticRoundedNarrowingSaturateUpper(lower, value, count);

        /// <summary>
        /// int64x1_t vrshr_n_s64 (int64x1_t a, const int n)
        ///   A32: VRSHR.S64 Dd, Dm, #n
        ///   A64: SRSHR Dd, Dn, #n
        /// </summary>
        public static Vector64<long> ShiftRightArithmeticRoundedScalar(Vector64<long> value, byte count) => ShiftRightArithmeticRoundedScalar(value, count);

        /// <summary>
        /// int64x1_t vshr_n_s64 (int64x1_t a, const int n)
        ///   A32: VSHR.S64 Dd, Dm, #n
        ///   A64: SSHR Dd, Dn, #n
        /// </summary>
        public static Vector64<long> ShiftRightArithmeticScalar(Vector64<long> value, byte count) => ShiftRightArithmeticScalar(value, count);

        /// <summary>
        /// uint8x8_t vshr_n_u8 (uint8x8_t a, const int n)
        ///   A32: VSHR.U8 Dd, Dm, #n
        ///   A64: USHR Vd.8B, Vn.8B, #n
        /// </summary>
        public static Vector64<byte> ShiftRightLogical(Vector64<byte> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint16x4_t vshr_n_u16 (uint16x4_t a, const int n)
        ///   A32: VSHR.U16 Dd, Dm, #n
        ///   A64: USHR Vd.4H, Vn.4H, #n
        /// </summary>
        public static Vector64<short> ShiftRightLogical(Vector64<short> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint32x2_t vshr_n_u32 (uint32x2_t a, const int n)
        ///   A32: VSHR.U32 Dd, Dm, #n
        ///   A64: USHR Vd.2S, Vn.2S, #n
        /// </summary>
        public static Vector64<int> ShiftRightLogical(Vector64<int> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint8x8_t vshr_n_u8 (uint8x8_t a, const int n)
        ///   A32: VSHR.U8 Dd, Dm, #n
        ///   A64: USHR Vd.8B, Vn.8B, #n
        /// </summary>
        public static Vector64<sbyte> ShiftRightLogical(Vector64<sbyte> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint16x4_t vshr_n_u16 (uint16x4_t a, const int n)
        ///   A32: VSHR.U16 Dd, Dm, #n
        ///   A64: USHR Vd.4H, Vn.4H, #n
        /// </summary>
        public static Vector64<ushort> ShiftRightLogical(Vector64<ushort> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint32x2_t vshr_n_u32 (uint32x2_t a, const int n)
        ///   A32: VSHR.U32 Dd, Dm, #n
        ///   A64: USHR Vd.2S, Vn.2S, #n
        /// </summary>
        public static Vector64<uint> ShiftRightLogical(Vector64<uint> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint8x16_t vshrq_n_u8 (uint8x16_t a, const int n)
        ///   A32: VSHR.U8 Qd, Qm, #n
        ///   A64: USHR Vd.16B, Vn.16B, #n
        /// </summary>
        public static Vector128<byte> ShiftRightLogical(Vector128<byte> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint16x8_t vshrq_n_u16 (uint16x8_t a, const int n)
        ///   A32: VSHR.U16 Qd, Qm, #n
        ///   A64: USHR Vd.8H, Vn.8H, #n
        /// </summary>
        public static Vector128<short> ShiftRightLogical(Vector128<short> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint32x4_t vshrq_n_u32 (uint32x4_t a, const int n)
        ///   A32: VSHR.U32 Qd, Qm, #n
        ///   A64: USHR Vd.4S, Vn.4S, #n
        /// </summary>
        public static Vector128<int> ShiftRightLogical(Vector128<int> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint64x2_t vshrq_n_u64 (uint64x2_t a, const int n)
        ///   A32: VSHR.U64 Qd, Qm, #n
        ///   A64: USHR Vd.2D, Vn.2D, #n
        /// </summary>
        public static Vector128<long> ShiftRightLogical(Vector128<long> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint8x16_t vshrq_n_u8 (uint8x16_t a, const int n)
        ///   A32: VSHR.U8 Qd, Qm, #n
        ///   A64: USHR Vd.16B, Vn.16B, #n
        /// </summary>
        public static Vector128<sbyte> ShiftRightLogical(Vector128<sbyte> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint16x8_t vshrq_n_u16 (uint16x8_t a, const int n)
        ///   A32: VSHR.U16 Qd, Qm, #n
        ///   A64: USHR Vd.8H, Vn.8H, #n
        /// </summary>
        public static Vector128<ushort> ShiftRightLogical(Vector128<ushort> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint32x4_t vshrq_n_u32 (uint32x4_t a, const int n)
        ///   A32: VSHR.U32 Qd, Qm, #n
        ///   A64: USHR Vd.4S, Vn.4S, #n
        /// </summary>
        public static Vector128<uint> ShiftRightLogical(Vector128<uint> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint64x2_t vshrq_n_u64 (uint64x2_t a, const int n)
        ///   A32: VSHR.U64 Qd, Qm, #n
        ///   A64: USHR Vd.2D, Vn.2D, #n
        /// </summary>
        public static Vector128<ulong> ShiftRightLogical(Vector128<ulong> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint8x8_t vsra_n_u8 (uint8x8_t a, uint8x8_t b, const int n)
        ///   A32: VSRA.U8 Dd, Dm, #n
        ///   A64: USRA Vd.8B, Vn.8B, #n
        /// </summary>
        public static Vector64<byte> ShiftRightLogicalAdd(Vector64<byte> addend, Vector64<byte> value, byte count) => ShiftRightLogicalAdd(addend, value, count);

        /// <summary>
        /// uint16x4_t vsra_n_u16 (uint16x4_t a, uint16x4_t b, const int n)
        ///   A32: VSRA.U16 Dd, Dm, #n
        ///   A64: USRA Vd.4H, Vn.4H, #n
        /// </summary>
        public static Vector64<short> ShiftRightLogicalAdd(Vector64<short> addend, Vector64<short> value, byte count) => ShiftRightLogicalAdd(addend, value, count);

        /// <summary>
        /// uint32x2_t vsra_n_u32 (uint32x2_t a, uint32x2_t b, const int n)
        ///   A32: VSRA.U32 Dd, Dm, #n
        ///   A64: USRA Vd.2S, Vn.2S, #n
        /// </summary>
        public static Vector64<int> ShiftRightLogicalAdd(Vector64<int> addend, Vector64<int> value, byte count) => ShiftRightLogicalAdd(addend, value, count);

        /// <summary>
        /// uint8x8_t vsra_n_u8 (uint8x8_t a, uint8x8_t b, const int n)
        ///   A32: VSRA.U8 Dd, Dm, #n
        ///   A64: USRA Vd.8B, Vn.8B, #n
        /// </summary>
        public static Vector64<sbyte> ShiftRightLogicalAdd(Vector64<sbyte> addend, Vector64<sbyte> value, byte count) => ShiftRightLogicalAdd(addend, value, count);

        /// <summary>
        /// uint16x4_t vsra_n_u16 (uint16x4_t a, uint16x4_t b, const int n)
        ///   A32: VSRA.U16 Dd, Dm, #n
        ///   A64: USRA Vd.4H, Vn.4H, #n
        /// </summary>
        public static Vector64<ushort> ShiftRightLogicalAdd(Vector64<ushort> addend, Vector64<ushort> value, byte count) => ShiftRightLogicalAdd(addend, value, count);

        /// <summary>
        /// uint32x2_t vsra_n_u32 (uint32x2_t a, uint32x2_t b, const int n)
        ///   A32: VSRA.U32 Dd, Dm, #n
        ///   A64: USRA Vd.2S, Vn.2S, #n
        /// </summary>
        public static Vector64<uint> ShiftRightLogicalAdd(Vector64<uint> addend, Vector64<uint> value, byte count) => ShiftRightLogicalAdd(addend, value, count);

        /// <summary>
        /// uint8x16_t vsraq_n_u8 (uint8x16_t a, uint8x16_t b, const int n)
        ///   A32: VSRA.U8 Qd, Qm, #n
        ///   A64: USRA Vd.16B, Vn.16B, #n
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalAdd(Vector128<byte> addend, Vector128<byte> value, byte count) => ShiftRightLogicalAdd(addend, value, count);

        /// <summary>
        /// uint16x8_t vsraq_n_u16 (uint16x8_t a, uint16x8_t b, const int n)
        ///   A32: VSRA.U16 Qd, Qm, #n
        ///   A64: USRA Vd.8H, Vn.8H, #n
        /// </summary>
        public static Vector128<short> ShiftRightLogicalAdd(Vector128<short> addend, Vector128<short> value, byte count) => ShiftRightLogicalAdd(addend, value, count);

        /// <summary>
        /// uint32x4_t vsraq_n_u32 (uint32x4_t a, uint32x4_t b, const int n)
        ///   A32: VSRA.U32 Qd, Qm, #n
        ///   A64: USRA Vd.4S, Vn.4S, #n
        /// </summary>
        public static Vector128<int> ShiftRightLogicalAdd(Vector128<int> addend, Vector128<int> value, byte count) => ShiftRightLogicalAdd(addend, value, count);

        /// <summary>
        /// uint64x2_t vsraq_n_u64 (uint64x2_t a, uint64x2_t b, const int n)
        ///   A32: VSRA.U64 Qd, Qm, #n
        ///   A64: USRA Vd.2D, Vn.2D, #n
        /// </summary>
        public static Vector128<long> ShiftRightLogicalAdd(Vector128<long> addend, Vector128<long> value, byte count) => ShiftRightLogicalAdd(addend, value, count);

        /// <summary>
        /// uint8x16_t vsraq_n_u8 (uint8x16_t a, uint8x16_t b, const int n)
        ///   A32: VSRA.U8 Qd, Qm, #n
        ///   A64: USRA Vd.16B, Vn.16B, #n
        /// </summary>
        public static Vector128<sbyte> ShiftRightLogicalAdd(Vector128<sbyte> addend, Vector128<sbyte> value, byte count) => ShiftRightLogicalAdd(addend, value, count);

        /// <summary>
        /// uint16x8_t vsraq_n_u16 (uint16x8_t a, uint16x8_t b, const int n)
        ///   A32: VSRA.U16 Qd, Qm, #n
        ///   A64: USRA Vd.8H, Vn.8H, #n
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalAdd(Vector128<ushort> addend, Vector128<ushort> value, byte count) => ShiftRightLogicalAdd(addend, value, count);

        /// <summary>
        /// uint32x4_t vsraq_n_u32 (uint32x4_t a, uint32x4_t b, const int n)
        ///   A32: VSRA.U32 Qd, Qm, #n
        ///   A64: USRA Vd.4S, Vn.4S, #n
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalAdd(Vector128<uint> addend, Vector128<uint> value, byte count) => ShiftRightLogicalAdd(addend, value, count);

        /// <summary>
        /// uint64x2_t vsraq_n_u64 (uint64x2_t a, uint64x2_t b, const int n)
        ///   A32: VSRA.U64 Qd, Qm, #n
        ///   A64: USRA Vd.2D, Vn.2D, #n
        /// </summary>
        public static Vector128<ulong> ShiftRightLogicalAdd(Vector128<ulong> addend, Vector128<ulong> value, byte count) => ShiftRightLogicalAdd(addend, value, count);

        /// <summary>
        /// uint64x1_t vsra_n_u64 (uint64x1_t a, uint64x1_t b, const int n)
        ///   A32: VSRA.U64 Dd, Dm, #n
        ///   A64: USRA Dd, Dn, #n
        /// </summary>
        public static Vector64<long> ShiftRightLogicalAddScalar(Vector64<long> addend, Vector64<long> value, byte count) => ShiftRightLogicalAddScalar(addend, value, count);

        /// <summary>
        /// uint64x1_t vsra_n_u64 (uint64x1_t a, uint64x1_t b, const int n)
        ///   A32: VSRA.U64 Dd, Dm, #n
        ///   A64: USRA Dd, Dn, #n
        /// </summary>
        public static Vector64<ulong> ShiftRightLogicalAddScalar(Vector64<ulong> addend, Vector64<ulong> value, byte count) => ShiftRightLogicalAddScalar(addend, value, count);

        /// <summary>
        /// uint8x8_t vshrn_n_u16 (uint16x8_t a, const int n)
        ///   A32: VSHRN.I16 Dd, Qm, #n
        ///   A64: SHRN Vd.8B, Vn.8H, #n
        /// </summary>
        public static Vector64<byte> ShiftRightLogicalNarrowingLower(Vector128<ushort> value, byte count) => ShiftRightLogicalNarrowingLower(value, count);

        /// <summary>
        /// int16x4_t vshrn_n_s32 (int32x4_t a, const int n)
        ///   A32: VSHRN.I32 Dd, Qm, #n
        ///   A64: SHRN Vd.4H, Vn.4S, #n
        /// </summary>
        public static Vector64<short> ShiftRightLogicalNarrowingLower(Vector128<int> value, byte count) => ShiftRightLogicalNarrowingLower(value, count);

        /// <summary>
        /// int32x2_t vshrn_n_s64 (int64x2_t a, const int n)
        ///   A32: VSHRN.I64 Dd, Qm, #n
        ///   A64: SHRN Vd.2S, Vn.2D, #n
        /// </summary>
        public static Vector64<int> ShiftRightLogicalNarrowingLower(Vector128<long> value, byte count) => ShiftRightLogicalNarrowingLower(value, count);

        /// <summary>
        /// int8x8_t vshrn_n_s16 (int16x8_t a, const int n)
        ///   A32: VSHRN.I16 Dd, Qm, #n
        ///   A64: SHRN Vd.8B, Vn.8H, #n
        /// </summary>
        public static Vector64<sbyte> ShiftRightLogicalNarrowingLower(Vector128<short> value, byte count) => ShiftRightLogicalNarrowingLower(value, count);

        /// <summary>
        /// uint16x4_t vshrn_n_u32 (uint32x4_t a, const int n)
        ///   A32: VSHRN.I32 Dd, Qm, #n
        ///   A64: SHRN Vd.4H, Vn.4S, #n
        /// </summary>
        public static Vector64<ushort> ShiftRightLogicalNarrowingLower(Vector128<uint> value, byte count) => ShiftRightLogicalNarrowingLower(value, count);

        /// <summary>
        /// uint32x2_t vshrn_n_u64 (uint64x2_t a, const int n)
        ///   A32: VSHRN.I64 Dd, Qm, #n
        ///   A64: SHRN Vd.2S, Vn.2D, #n
        /// </summary>
        public static Vector64<uint> ShiftRightLogicalNarrowingLower(Vector128<ulong> value, byte count) => ShiftRightLogicalNarrowingLower(value, count);

        /// <summary>
        /// uint8x8_t vqshrn_n_u16 (uint16x8_t a, const int n)
        ///   A32: VQSHRN.U16 Dd, Qm, #n
        ///   A64: UQSHRN Vd.8B, Vn.8H, #n
        /// </summary>
        public static Vector64<byte> ShiftRightLogicalNarrowingSaturateLower(Vector128<ushort> value, byte count) => ShiftRightLogicalNarrowingSaturateLower(value, count);

        /// <summary>
        /// uint16x4_t vqshrn_n_u32 (uint32x4_t a, const int n)
        ///   A32: VQSHRN.U32 Dd, Qm, #n
        ///   A64: UQSHRN Vd.4H, Vn.4S, #n
        /// </summary>
        public static Vector64<short> ShiftRightLogicalNarrowingSaturateLower(Vector128<int> value, byte count) => ShiftRightLogicalNarrowingSaturateLower(value, count);

        /// <summary>
        /// uint32x2_t vqshrn_n_u64 (uint64x2_t a, const int n)
        ///   A32: VQSHRN.U64 Dd, Qm, #n
        ///   A64: UQSHRN Vd.2S, Vn.2D, #n
        /// </summary>
        public static Vector64<int> ShiftRightLogicalNarrowingSaturateLower(Vector128<long> value, byte count) => ShiftRightLogicalNarrowingSaturateLower(value, count);

        /// <summary>
        /// uint8x8_t vqshrn_n_u16 (uint16x8_t a, const int n)
        ///   A32: VQSHRN.U16 Dd, Qm, #n
        ///   A64: UQSHRN Vd.8B, Vn.8H, #n
        /// </summary>
        public static Vector64<sbyte> ShiftRightLogicalNarrowingSaturateLower(Vector128<short> value, byte count) => ShiftRightLogicalNarrowingSaturateLower(value, count);

        /// <summary>
        /// uint16x4_t vqshrn_n_u32 (uint32x4_t a, const int n)
        ///   A32: VQSHRN.U32 Dd, Qm, #n
        ///   A64: UQSHRN Vd.4H, Vn.4S, #n
        /// </summary>
        public static Vector64<ushort> ShiftRightLogicalNarrowingSaturateLower(Vector128<uint> value, byte count) => ShiftRightLogicalNarrowingSaturateLower(value, count);

        /// <summary>
        /// uint32x2_t vqshrn_n_u64 (uint64x2_t a, const int n)
        ///   A32: VQSHRN.U64 Dd, Qm, #n
        ///   A64: UQSHRN Vd.2S, Vn.2D, #n
        /// </summary>
        public static Vector64<uint> ShiftRightLogicalNarrowingSaturateLower(Vector128<ulong> value, byte count) => ShiftRightLogicalNarrowingSaturateLower(value, count);

        /// <summary>
        /// uint8x16_t vqshrn_high_n_u16 (uint8x8_t r, uint16x8_t a, const int n)
        ///   A32: VQSHRN.U16 Dd+1, Qm, #n
        ///   A64: UQSHRN2 Vd.16B, Vn.8H, #n
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalNarrowingSaturateUpper(Vector64<byte> lower, Vector128<ushort> value, byte count) => ShiftRightLogicalNarrowingSaturateUpper(lower, value, count);

        /// <summary>
        /// uint16x8_t vqshrn_high_n_u32 (uint16x4_t r, uint32x4_t a, const int n)
        ///   A32: VQSHRN.U32 Dd+1, Qm, #n
        ///   A64: UQSHRN2 Vd.8H, Vn.4S, #n
        /// </summary>
        public static Vector128<short> ShiftRightLogicalNarrowingSaturateUpper(Vector64<short> lower, Vector128<int> value, byte count) => ShiftRightLogicalNarrowingSaturateUpper(lower, value, count);

        /// <summary>
        /// uint32x4_t vqshrn_high_n_u64 (uint32x2_t r, uint64x2_t a, const int n)
        ///   A32: VQSHRN.U64 Dd+1, Qm, #n
        ///   A64: UQSHRN2 Vd.4S, Vn.2D, #n
        /// </summary>
        public static Vector128<int> ShiftRightLogicalNarrowingSaturateUpper(Vector64<int> lower, Vector128<long> value, byte count) => ShiftRightLogicalNarrowingSaturateUpper(lower, value, count);

        /// <summary>
        /// uint8x16_t vqshrn_high_n_u16 (uint8x8_t r, uint16x8_t a, const int n)
        ///   A32: VQSHRN.U16 Dd+1, Qm, #n
        ///   A64: UQSHRN2 Vd.16B, Vn.8H, #n
        /// </summary>
        public static Vector128<sbyte> ShiftRightLogicalNarrowingSaturateUpper(Vector64<sbyte> lower, Vector128<short> value, byte count) => ShiftRightLogicalNarrowingSaturateUpper(lower, value, count);

        /// <summary>
        /// uint16x8_t vqshrn_high_n_u32 (uint16x4_t r, uint32x4_t a, const int n)
        ///   A32: VQSHRN.U32 Dd+1, Qm, #n
        ///   A64: UQSHRN2 Vd.8H, Vn.4S, #n
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalNarrowingSaturateUpper(Vector64<ushort> lower, Vector128<uint> value, byte count) => ShiftRightLogicalNarrowingSaturateUpper(lower, value, count);

        /// <summary>
        /// uint32x4_t vqshrn_high_n_u64 (uint32x2_t r, uint64x2_t a, const int n)
        ///   A32: VQSHRN.U64 Dd+1, Qm, #n
        ///   A64: UQSHRN2 Vd.4S, Vn.2D, #n
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalNarrowingSaturateUpper(Vector64<uint> lower, Vector128<ulong> value, byte count) => ShiftRightLogicalNarrowingSaturateUpper(lower, value, count);

        /// <summary>
        /// uint8x16_t vshrn_high_n_u16 (uint8x8_t r, uint16x8_t a, const int n)
        ///   A32: VSHRN.I16 Dd+1, Qm, #n
        ///   A64: SHRN2 Vd.16B, Vn.8H, #n
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalNarrowingUpper(Vector64<byte> lower, Vector128<ushort> value, byte count) => ShiftRightLogicalNarrowingUpper(lower, value, count);

        /// <summary>
        /// int16x8_t vshrn_high_n_s32 (int16x4_t r, int32x4_t a, const int n)
        ///   A32: VSHRN.I32 Dd+1, Qm, #n
        ///   A64: SHRN2 Vd.8H, Vn.4S, #n
        /// </summary>
        public static Vector128<short> ShiftRightLogicalNarrowingUpper(Vector64<short> lower, Vector128<int> value, byte count) => ShiftRightLogicalNarrowingUpper(lower, value, count);

        /// <summary>
        /// int32x4_t vshrn_high_n_s64 (int32x2_t r, int64x2_t a, const int n)
        ///   A32: VSHRN.I64 Dd+1, Qm, #n
        ///   A64: SHRN2 Vd.4S, Vn.2D, #n
        /// </summary>
        public static Vector128<int> ShiftRightLogicalNarrowingUpper(Vector64<int> lower, Vector128<long> value, byte count) => ShiftRightLogicalNarrowingUpper(lower, value, count);

        /// <summary>
        /// int8x16_t vshrn_high_n_s16 (int8x8_t r, int16x8_t a, const int n)
        ///   A32: VSHRN.I16 Dd+1, Qm, #n
        ///   A64: SHRN2 Vd.16B, Vn.8H, #n
        /// </summary>
        public static Vector128<sbyte> ShiftRightLogicalNarrowingUpper(Vector64<sbyte> lower, Vector128<short> value, byte count) => ShiftRightLogicalNarrowingUpper(lower, value, count);

        /// <summary>
        /// uint16x8_t vshrn_high_n_u32 (uint16x4_t r, uint32x4_t a, const int n)
        ///   A32: VSHRN.I32 Dd+1, Qm, #n
        ///   A64: SHRN2 Vd.8H, Vn.4S, #n
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalNarrowingUpper(Vector64<ushort> lower, Vector128<uint> value, byte count) => ShiftRightLogicalNarrowingUpper(lower, value, count);

        /// <summary>
        /// uint32x4_t vshrn_high_n_u64 (uint32x2_t r, uint64x2_t a, const int n)
        ///   A32: VSHRN.I64 Dd+1, Qm, #n
        ///   A64: SHRN2 Vd.4S, Vn.2D, #n
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalNarrowingUpper(Vector64<uint> lower, Vector128<ulong> value, byte count) => ShiftRightLogicalNarrowingUpper(lower, value, count);

        /// <summary>
        /// uint8x8_t vrshr_n_u8 (uint8x8_t a, const int n)
        ///   A32: VRSHR.U8 Dd, Dm, #n
        ///   A64: URSHR Vd.8B, Vn.8B, #n
        /// </summary>
        public static Vector64<byte> ShiftRightLogicalRounded(Vector64<byte> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// uint16x4_t vrshr_n_u16 (uint16x4_t a, const int n)
        ///   A32: VRSHR.U16 Dd, Dm, #n
        ///   A64: URSHR Vd.4H, Vn.4H, #n
        /// </summary>
        public static Vector64<short> ShiftRightLogicalRounded(Vector64<short> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// uint32x2_t vrshr_n_u32 (uint32x2_t a, const int n)
        ///   A32: VRSHR.U32 Dd, Dm, #n
        ///   A64: URSHR Vd.2S, Vn.2S, #n
        /// </summary>
        public static Vector64<int> ShiftRightLogicalRounded(Vector64<int> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// uint8x8_t vrshr_n_u8 (uint8x8_t a, const int n)
        ///   A32: VRSHR.U8 Dd, Dm, #n
        ///   A64: URSHR Vd.8B, Vn.8B, #n
        /// </summary>
        public static Vector64<sbyte> ShiftRightLogicalRounded(Vector64<sbyte> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// uint16x4_t vrshr_n_u16 (uint16x4_t a, const int n)
        ///   A32: VRSHR.U16 Dd, Dm, #n
        ///   A64: URSHR Vd.4H, Vn.4H, #n
        /// </summary>
        public static Vector64<ushort> ShiftRightLogicalRounded(Vector64<ushort> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// uint32x2_t vrshr_n_u32 (uint32x2_t a, const int n)
        ///   A32: VRSHR.U32 Dd, Dm, #n
        ///   A64: URSHR Vd.2S, Vn.2S, #n
        /// </summary>
        public static Vector64<uint> ShiftRightLogicalRounded(Vector64<uint> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// uint8x16_t vrshrq_n_u8 (uint8x16_t a, const int n)
        ///   A32: VRSHR.U8 Qd, Qm, #n
        ///   A64: URSHR Vd.16B, Vn.16B, #n
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalRounded(Vector128<byte> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// uint16x8_t vrshrq_n_u16 (uint16x8_t a, const int n)
        ///   A32: VRSHR.U16 Qd, Qm, #n
        ///   A64: URSHR Vd.8H, Vn.8H, #n
        /// </summary>
        public static Vector128<short> ShiftRightLogicalRounded(Vector128<short> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// uint32x4_t vrshrq_n_u32 (uint32x4_t a, const int n)
        ///   A32: VRSHR.U32 Qd, Qm, #n
        ///   A64: URSHR Vd.4S, Vn.4S, #n
        /// </summary>
        public static Vector128<int> ShiftRightLogicalRounded(Vector128<int> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// uint64x2_t vrshrq_n_u64 (uint64x2_t a, const int n)
        ///   A32: VRSHR.U64 Qd, Qm, #n
        ///   A64: URSHR Vd.2D, Vn.2D, #n
        /// </summary>
        public static Vector128<long> ShiftRightLogicalRounded(Vector128<long> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// uint8x16_t vrshrq_n_u8 (uint8x16_t a, const int n)
        ///   A32: VRSHR.U8 Qd, Qm, #n
        ///   A64: URSHR Vd.16B, Vn.16B, #n
        /// </summary>
        public static Vector128<sbyte> ShiftRightLogicalRounded(Vector128<sbyte> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// uint16x8_t vrshrq_n_u16 (uint16x8_t a, const int n)
        ///   A32: VRSHR.U16 Qd, Qm, #n
        ///   A64: URSHR Vd.8H, Vn.8H, #n
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalRounded(Vector128<ushort> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// uint32x4_t vrshrq_n_u32 (uint32x4_t a, const int n)
        ///   A32: VRSHR.U32 Qd, Qm, #n
        ///   A64: URSHR Vd.4S, Vn.4S, #n
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalRounded(Vector128<uint> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// uint64x2_t vrshrq_n_u64 (uint64x2_t a, const int n)
        ///   A32: VRSHR.U64 Qd, Qm, #n
        ///   A64: URSHR Vd.2D, Vn.2D, #n
        /// </summary>
        public static Vector128<ulong> ShiftRightLogicalRounded(Vector128<ulong> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// uint8x8_t vrsra_n_u8 (uint8x8_t a, uint8x8_t b, const int n)
        ///   A32: VRSRA.U8 Dd, Dm, #n
        ///   A64: URSRA Vd.8B, Vn.8B, #n
        /// </summary>
        public static Vector64<byte> ShiftRightLogicalRoundedAdd(Vector64<byte> addend, Vector64<byte> value, byte count) => ShiftRightLogicalRoundedAdd(addend, value, count);

        /// <summary>
        /// uint16x4_t vrsra_n_u16 (uint16x4_t a, uint16x4_t b, const int n)
        ///   A32: VRSRA.U16 Dd, Dm, #n
        ///   A64: URSRA Vd.4H, Vn.4H, #n
        /// </summary>
        public static Vector64<short> ShiftRightLogicalRoundedAdd(Vector64<short> addend, Vector64<short> value, byte count) => ShiftRightLogicalRoundedAdd(addend, value, count);

        /// <summary>
        /// uint32x2_t vrsra_n_u32 (uint32x2_t a, uint32x2_t b, const int n)
        ///   A32: VRSRA.U32 Dd, Dm, #n
        ///   A64: URSRA Vd.2S, Vn.2S, #n
        /// </summary>
        public static Vector64<int> ShiftRightLogicalRoundedAdd(Vector64<int> addend, Vector64<int> value, byte count) => ShiftRightLogicalRoundedAdd(addend, value, count);

        /// <summary>
        /// uint8x8_t vrsra_n_u8 (uint8x8_t a, uint8x8_t b, const int n)
        ///   A32: VRSRA.U8 Dd, Dm, #n
        ///   A64: URSRA Vd.8B, Vn.8B, #n
        /// </summary>
        public static Vector64<sbyte> ShiftRightLogicalRoundedAdd(Vector64<sbyte> addend, Vector64<sbyte> value, byte count) => ShiftRightLogicalRoundedAdd(addend, value, count);

        /// <summary>
        /// uint16x4_t vrsra_n_u16 (uint16x4_t a, uint16x4_t b, const int n)
        ///   A32: VRSRA.U16 Dd, Dm, #n
        ///   A64: URSRA Vd.4H, Vn.4H, #n
        /// </summary>
        public static Vector64<ushort> ShiftRightLogicalRoundedAdd(Vector64<ushort> addend, Vector64<ushort> value, byte count) => ShiftRightLogicalRoundedAdd(addend, value, count);

        /// <summary>
        /// uint32x2_t vrsra_n_u32 (uint32x2_t a, uint32x2_t b, const int n)
        ///   A32: VRSRA.U32 Dd, Dm, #n
        ///   A64: URSRA Vd.2S, Vn.2S, #n
        /// </summary>
        public static Vector64<uint> ShiftRightLogicalRoundedAdd(Vector64<uint> addend, Vector64<uint> value, byte count) => ShiftRightLogicalRoundedAdd(addend, value, count);

        /// <summary>
        /// uint8x16_t vrsraq_n_u8 (uint8x16_t a, uint8x16_t b, const int n)
        ///   A32: VRSRA.U8 Qd, Qm, #n
        ///   A64: URSRA Vd.16B, Vn.16B, #n
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalRoundedAdd(Vector128<byte> addend, Vector128<byte> value, byte count) => ShiftRightLogicalRoundedAdd(addend, value, count);

        /// <summary>
        /// uint16x8_t vrsraq_n_u16 (uint16x8_t a, uint16x8_t b, const int n)
        ///   A32: VRSRA.U16 Qd, Qm, #n
        ///   A64: URSRA Vd.8H, Vn.8H, #n
        /// </summary>
        public static Vector128<short> ShiftRightLogicalRoundedAdd(Vector128<short> addend, Vector128<short> value, byte count) => ShiftRightLogicalRoundedAdd(addend, value, count);

        /// <summary>
        /// uint32x4_t vrsraq_n_u32 (uint32x4_t a, uint32x4_t b, const int n)
        ///   A32: VRSRA.U32 Qd, Qm, #n
        ///   A64: URSRA Vd.4S, Vn.4S, #n
        /// </summary>
        public static Vector128<int> ShiftRightLogicalRoundedAdd(Vector128<int> addend, Vector128<int> value, byte count) => ShiftRightLogicalRoundedAdd(addend, value, count);

        /// <summary>
        /// uint64x2_t vrsraq_n_u64 (uint64x2_t a, uint64x2_t b, const int n)
        ///   A32: VRSRA.U64 Qd, Qm, #n
        ///   A64: URSRA Vd.2D, Vn.2D, #n
        /// </summary>
        public static Vector128<long> ShiftRightLogicalRoundedAdd(Vector128<long> addend, Vector128<long> value, byte count) => ShiftRightLogicalRoundedAdd(addend, value, count);

        /// <summary>
        /// uint8x16_t vrsraq_n_u8 (uint8x16_t a, uint8x16_t b, const int n)
        ///   A32: VRSRA.U8 Qd, Qm, #n
        ///   A64: URSRA Vd.16B, Vn.16B, #n
        /// </summary>
        public static Vector128<sbyte> ShiftRightLogicalRoundedAdd(Vector128<sbyte> addend, Vector128<sbyte> value, byte count) => ShiftRightLogicalRoundedAdd(addend, value, count);

        /// <summary>
        /// uint16x8_t vrsraq_n_u16 (uint16x8_t a, uint16x8_t b, const int n)
        ///   A32: VRSRA.U16 Qd, Qm, #n
        ///   A64: URSRA Vd.8H, Vn.8H, #n
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalRoundedAdd(Vector128<ushort> addend, Vector128<ushort> value, byte count) => ShiftRightLogicalRoundedAdd(addend, value, count);

        /// <summary>
        /// uint32x4_t vrsraq_n_u32 (uint32x4_t a, uint32x4_t b, const int n)
        ///   A32: VRSRA.U32 Qd, Qm, #n
        ///   A64: URSRA Vd.4S, Vn.4S, #n
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalRoundedAdd(Vector128<uint> addend, Vector128<uint> value, byte count) => ShiftRightLogicalRoundedAdd(addend, value, count);

        /// <summary>
        /// uint64x2_t vrsraq_n_u64 (uint64x2_t a, uint64x2_t b, const int n)
        ///   A32: VRSRA.U64 Qd, Qm, #n
        ///   A64: URSRA Vd.2D, Vn.2D, #n
        /// </summary>
        public static Vector128<ulong> ShiftRightLogicalRoundedAdd(Vector128<ulong> addend, Vector128<ulong> value, byte count) => ShiftRightLogicalRoundedAdd(addend, value, count);

        /// <summary>
        /// uint64x1_t vrsra_n_u64 (uint64x1_t a, uint64x1_t b, const int n)
        ///   A32: VRSRA.U64 Dd, Dm, #n
        ///   A64: URSRA Dd, Dn, #n
        /// </summary>
        public static Vector64<long> ShiftRightLogicalRoundedAddScalar(Vector64<long> addend, Vector64<long> value, byte count) => ShiftRightLogicalRoundedAddScalar(addend, value, count);

        /// <summary>
        /// uint64x1_t vrsra_n_u64 (uint64x1_t a, uint64x1_t b, const int n)
        ///   A32: VRSRA.U64 Dd, Dm, #n
        ///   A64: URSRA Dd, Dn, #n
        /// </summary>
        public static Vector64<ulong> ShiftRightLogicalRoundedAddScalar(Vector64<ulong> addend, Vector64<ulong> value, byte count) => ShiftRightLogicalRoundedAddScalar(addend, value, count);

        /// <summary>
        /// uint8x8_t vrshrn_n_u16 (uint16x8_t a, const int n)
        ///   A32: VRSHRN.I16 Dd, Qm, #n
        ///   A64: RSHRN Vd.8B, Vn.8H, #n
        /// </summary>
        public static Vector64<byte> ShiftRightLogicalRoundedNarrowingLower(Vector128<ushort> value, byte count) => ShiftRightLogicalRoundedNarrowingLower(value, count);

        /// <summary>
        /// int16x4_t vrshrn_n_s32 (int32x4_t a, const int n)
        ///   A32: VRSHRN.I32 Dd, Qm, #n
        ///   A64: RSHRN Vd.4H, Vn.4S, #n
        /// </summary>
        public static Vector64<short> ShiftRightLogicalRoundedNarrowingLower(Vector128<int> value, byte count) => ShiftRightLogicalRoundedNarrowingLower(value, count);

        /// <summary>
        /// int32x2_t vrshrn_n_s64 (int64x2_t a, const int n)
        ///   A32: VRSHRN.I64 Dd, Qm, #n
        ///   A64: RSHRN Vd.2S, Vn.2D, #n
        /// </summary>
        public static Vector64<int> ShiftRightLogicalRoundedNarrowingLower(Vector128<long> value, byte count) => ShiftRightLogicalRoundedNarrowingLower(value, count);

        /// <summary>
        /// int8x8_t vrshrn_n_s16 (int16x8_t a, const int n)
        ///   A32: VRSHRN.I16 Dd, Qm, #n
        ///   A64: RSHRN Vd.8B, Vn.8H, #n
        /// </summary>
        public static Vector64<sbyte> ShiftRightLogicalRoundedNarrowingLower(Vector128<short> value, byte count) => ShiftRightLogicalRoundedNarrowingLower(value, count);

        /// <summary>
        /// uint16x4_t vrshrn_n_u32 (uint32x4_t a, const int n)
        ///   A32: VRSHRN.I32 Dd, Qm, #n
        ///   A64: RSHRN Vd.4H, Vn.4S, #n
        /// </summary>
        public static Vector64<ushort> ShiftRightLogicalRoundedNarrowingLower(Vector128<uint> value, byte count) => ShiftRightLogicalRoundedNarrowingLower(value, count);

        /// <summary>
        /// uint32x2_t vrshrn_n_u64 (uint64x2_t a, const int n)
        ///   A32: VRSHRN.I64 Dd, Qm, #n
        ///   A64: RSHRN Vd.2S, Vn.2D, #n
        /// </summary>
        public static Vector64<uint> ShiftRightLogicalRoundedNarrowingLower(Vector128<ulong> value, byte count) => ShiftRightLogicalRoundedNarrowingLower(value, count);

        /// <summary>
        /// uint8x8_t vqrshrn_n_u16 (uint16x8_t a, const int n)
        ///   A32: VQRSHRN.U16 Dd, Qm, #n
        ///   A64: UQRSHRN Vd.8B, Vn.8H, #n
        /// </summary>
        public static Vector64<byte> ShiftRightLogicalRoundedNarrowingSaturateLower(Vector128<ushort> value, byte count) => ShiftRightLogicalRoundedNarrowingSaturateLower(value, count);

        /// <summary>
        /// uint16x4_t vqrshrn_n_u32 (uint32x4_t a, const int n)
        ///   A32: VQRSHRN.U32 Dd, Qm, #n
        ///   A64: UQRSHRN Vd.4H, Vn.4S, #n
        /// </summary>
        public static Vector64<short> ShiftRightLogicalRoundedNarrowingSaturateLower(Vector128<int> value, byte count) => ShiftRightLogicalRoundedNarrowingSaturateLower(value, count);

        /// <summary>
        /// uint32x2_t vqrshrn_n_u64 (uint64x2_t a, const int n)
        ///   A32: VQRSHRN.U64 Dd, Qm, #n
        ///   A64: UQRSHRN Vd.2S, Vn.2D, #n
        /// </summary>
        public static Vector64<int> ShiftRightLogicalRoundedNarrowingSaturateLower(Vector128<long> value, byte count) => ShiftRightLogicalRoundedNarrowingSaturateLower(value, count);

        /// <summary>
        /// uint8x8_t vqrshrn_n_u16 (uint16x8_t a, const int n)
        ///   A32: VQRSHRN.U16 Dd, Qm, #n
        ///   A64: UQRSHRN Vd.8B, Vn.8H, #n
        /// </summary>
        public static Vector64<sbyte> ShiftRightLogicalRoundedNarrowingSaturateLower(Vector128<short> value, byte count) => ShiftRightLogicalRoundedNarrowingSaturateLower(value, count);

        /// <summary>
        /// uint16x4_t vqrshrn_n_u32 (uint32x4_t a, const int n)
        ///   A32: VQRSHRN.U32 Dd, Qm, #n
        ///   A64: UQRSHRN Vd.4H, Vn.4S, #n
        /// </summary>
        public static Vector64<ushort> ShiftRightLogicalRoundedNarrowingSaturateLower(Vector128<uint> value, byte count) => ShiftRightLogicalRoundedNarrowingSaturateLower(value, count);

        /// <summary>
        /// uint32x2_t vqrshrn_n_u64 (uint64x2_t a, const int n)
        ///   A32: VQRSHRN.U64 Dd, Qm, #n
        ///   A64: UQRSHRN Vd.2S, Vn.2D, #n
        /// </summary>
        public static Vector64<uint> ShiftRightLogicalRoundedNarrowingSaturateLower(Vector128<ulong> value, byte count) => ShiftRightLogicalRoundedNarrowingSaturateLower(value, count);

        /// <summary>
        /// uint8x16_t vqrshrn_high_n_u16 (uint8x8_t r, uint16x8_t a, const int n)
        ///   A32: VQRSHRN.U16 Dd+1, Dn, #n
        ///   A64: UQRSHRN2 Vd.16B, Vn.8H, #n
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalRoundedNarrowingSaturateUpper(Vector64<byte> lower, Vector128<ushort> value, byte count) => ShiftRightLogicalRoundedNarrowingSaturateUpper(lower, value, count);

        /// <summary>
        /// uint16x8_t vqrshrn_high_n_u32 (uint16x4_t r, uint32x4_t a, const int n)
        ///   A32: VQRSHRN.U32 Dd+1, Dn, #n
        ///   A64: UQRSHRN2 Vd.8H, Vn.4S, #n
        /// </summary>
        public static Vector128<short> ShiftRightLogicalRoundedNarrowingSaturateUpper(Vector64<short> lower, Vector128<int> value, byte count) => ShiftRightLogicalRoundedNarrowingSaturateUpper(lower, value, count);

        /// <summary>
        /// uint32x4_t vqrshrn_high_n_u64 (uint32x2_t r, uint64x2_t a, const int n)
        ///   A32: VQRSHRN.U64 Dd+1, Dn, #n
        ///   A64: UQRSHRN2 Vd.4S, Vn.2D, #n
        /// </summary>
        public static Vector128<int> ShiftRightLogicalRoundedNarrowingSaturateUpper(Vector64<int> lower, Vector128<long> value, byte count) => ShiftRightLogicalRoundedNarrowingSaturateUpper(lower, value, count);

        /// <summary>
        /// uint8x16_t vqrshrn_high_n_u16 (uint8x8_t r, uint16x8_t a, const int n)
        ///   A32: VQRSHRN.U16 Dd+1, Dn, #n
        ///   A64: UQRSHRN2 Vd.16B, Vn.8H, #n
        /// </summary>
        public static Vector128<sbyte> ShiftRightLogicalRoundedNarrowingSaturateUpper(Vector64<sbyte> lower, Vector128<short> value, byte count) => ShiftRightLogicalRoundedNarrowingSaturateUpper(lower, value, count);

        /// <summary>
        /// uint16x8_t vqrshrn_high_n_u32 (uint16x4_t r, uint32x4_t a, const int n)
        ///   A32: VQRSHRN.U32 Dd+1, Dn, #n
        ///   A64: UQRSHRN2 Vd.8H, Vn.4S, #n
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalRoundedNarrowingSaturateUpper(Vector64<ushort> lower, Vector128<uint> value, byte count) => ShiftRightLogicalRoundedNarrowingSaturateUpper(lower, value, count);

        /// <summary>
        /// uint32x4_t vqrshrn_high_n_u64 (uint32x2_t r, uint64x2_t a, const int n)
        ///   A32: VQRSHRN.U64 Dd+1, Dn, #n
        ///   A64: UQRSHRN2 Vd.4S, Vn.2D, #n
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalRoundedNarrowingSaturateUpper(Vector64<uint> lower, Vector128<ulong> value, byte count) => ShiftRightLogicalRoundedNarrowingSaturateUpper(lower, value, count);

        /// <summary>
        /// uint8x16_t vrshrn_high_n_u16 (uint8x8_t r, uint16x8_t a, const int n)
        ///   A32: VRSHRN.I16 Dd+1, Qm, #n
        ///   A64: RSHRN2 Vd.16B, Vn.8H, #n
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalRoundedNarrowingUpper(Vector64<byte> lower, Vector128<ushort> value, byte count) => ShiftRightLogicalRoundedNarrowingUpper(lower, value, count);

        /// <summary>
        /// int16x8_t vrshrn_high_n_s32 (int16x4_t r, int32x4_t a, const int n)
        ///   A32: VRSHRN.I32 Dd+1, Qm, #n
        ///   A64: RSHRN2 Vd.8H, Vn.4S, #n
        /// </summary>
        public static Vector128<short> ShiftRightLogicalRoundedNarrowingUpper(Vector64<short> lower, Vector128<int> value, byte count) => ShiftRightLogicalRoundedNarrowingUpper(lower, value, count);

        /// <summary>
        /// int32x4_t vrshrn_high_n_s64 (int32x2_t r, int64x2_t a, const int n)
        ///   A32: VRSHRN.I64 Dd+1, Qm, #n
        ///   A64: RSHRN2 Vd.4S, Vn.2D, #n
        /// </summary>
        public static Vector128<int> ShiftRightLogicalRoundedNarrowingUpper(Vector64<int> lower, Vector128<long> value, byte count) => ShiftRightLogicalRoundedNarrowingUpper(lower, value, count);

        /// <summary>
        /// int8x16_t vrshrn_high_n_s16 (int8x8_t r, int16x8_t a, const int n)
        ///   A32: VRSHRN.I16 Dd+1, Qm, #n
        ///   A64: RSHRN2 Vd.16B, Vn.8H, #n
        /// </summary>
        public static Vector128<sbyte> ShiftRightLogicalRoundedNarrowingUpper(Vector64<sbyte> lower, Vector128<short> value, byte count) => ShiftRightLogicalRoundedNarrowingUpper(lower, value, count);

        /// <summary>
        /// uint16x8_t vrshrn_high_n_u32 (uint16x4_t r, uint32x4_t a, const int n)
        ///   A32: VRSHRN.I32 Dd+1, Qm, #n
        ///   A64: RSHRN2 Vd.8H, Vn.4S, #n
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalRoundedNarrowingUpper(Vector64<ushort> lower, Vector128<uint> value, byte count) => ShiftRightLogicalRoundedNarrowingUpper(lower, value, count);

        /// <summary>
        /// uint32x4_t vrshrn_high_n_u64 (uint32x2_t r, uint64x2_t a, const int n)
        ///   A32: VRSHRN.I64 Dd+1, Qm, #n
        ///   A64: RSHRN2 Vd.4S, Vn.2D, #n
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalRoundedNarrowingUpper(Vector64<uint> lower, Vector128<ulong> value, byte count) => ShiftRightLogicalRoundedNarrowingUpper(lower, value, count);

        /// <summary>
        /// uint64x1_t vrshr_n_u64 (uint64x1_t a, const int n)
        ///   A32: VRSHR.U64 Dd, Dm, #n
        ///   A64: URSHR Dd, Dn, #n
        /// </summary>
        public static Vector64<long> ShiftRightLogicalRoundedScalar(Vector64<long> value, byte count) => ShiftRightLogicalRoundedScalar(value, count);

        /// <summary>
        /// uint64x1_t vrshr_n_u64 (uint64x1_t a, const int n)
        ///   A32: VRSHR.U64 Dd, Dm, #n
        ///   A64: URSHR Dd, Dn, #n
        /// </summary>
        public static Vector64<ulong> ShiftRightLogicalRoundedScalar(Vector64<ulong> value, byte count) => ShiftRightLogicalRoundedScalar(value, count);

        /// <summary>
        /// uint64x1_t vshr_n_u64 (uint64x1_t a, const int n)
        ///   A32: VSHR.U64 Dd, Dm, #n
        ///   A64: USHR Dd, Dn, #n
        /// </summary>
        public static Vector64<long> ShiftRightLogicalScalar(Vector64<long> value, byte count) => ShiftRightLogicalScalar(value, count);

        /// <summary>
        /// uint64x1_t vshr_n_u64 (uint64x1_t a, const int n)
        ///   A32: VSHR.U64 Dd, Dm, #n
        ///   A64: USHR Dd, Dn, #n
        /// </summary>
        public static Vector64<ulong> ShiftRightLogicalScalar(Vector64<ulong> value, byte count) => ShiftRightLogicalScalar(value, count);

        /// <summary>
        /// int32x4_t vmovl_s16 (int16x4_t a)
        ///   A32: VMOVL.S16 Qd, Dm
        ///   A64: SXTL Vd.4S, Vn.4H
        /// </summary>
        public static Vector128<int> SignExtendWideningLower(Vector64<short> value) => SignExtendWideningLower(value);

        /// <summary>
        /// int64x2_t vmovl_s32 (int32x2_t a)
        ///   A32: VMOVL.S32 Qd, Dm
        ///   A64: SXTL Vd.2D, Vn.2S
        /// </summary>
        public static Vector128<long> SignExtendWideningLower(Vector64<int> value) => SignExtendWideningLower(value);

        /// <summary>
        /// int16x8_t vmovl_s8 (int8x8_t a)
        ///   A32: VMOVL.S8 Qd, Dm
        ///   A64: SXTL Vd.8H, Vn.8B
        /// </summary>
        public static Vector128<short> SignExtendWideningLower(Vector64<sbyte> value) => SignExtendWideningLower(value);

        /// <summary>
        /// int32x4_t vmovl_high_s16 (int16x8_t a)
        ///   A32: VMOVL.S16 Qd, Dm+1
        ///   A64: SXTL2 Vd.4S, Vn.8H
        /// </summary>
        public static Vector128<int> SignExtendWideningUpper(Vector128<short> value) => SignExtendWideningUpper(value);

        /// <summary>
        /// int64x2_t vmovl_high_s32 (int32x4_t a)
        ///   A32: VMOVL.S32 Qd, Dm+1
        ///   A64: SXTL2 Vd.2D, Vn.4S
        /// </summary>
        public static Vector128<long> SignExtendWideningUpper(Vector128<int> value) => SignExtendWideningUpper(value);

        /// <summary>
        /// int16x8_t vmovl_high_s8 (int8x16_t a)
        ///   A32: VMOVL.S8 Qd, Dm+1
        ///   A64: SXTL2 Vd.8H, Vn.16B
        /// </summary>
        public static Vector128<short> SignExtendWideningUpper(Vector128<sbyte> value) => SignExtendWideningUpper(value);

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
        /// uint8x8_t vsubhn_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VSUBHN.I16 Dd, Qn, Qm
        ///   A64: SUBHN Vd.8B, Vn.8H, Vm.8H
        /// </summary>
        public static Vector64<byte> SubtractHighNarrowingLower(Vector128<ushort> left, Vector128<ushort> right) => SubtractHighNarrowingLower(left, right);

        /// <summary>
        /// int16x4_t vsubhn_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VSUBHN.I32 Dd, Qn, Qm
        ///   A64: SUBHN Vd.4H, Vn.4S, Vm.4S
        /// </summary>
        public static Vector64<short> SubtractHighNarrowingLower(Vector128<int> left, Vector128<int> right) => SubtractHighNarrowingLower(left, right);

        /// <summary>
        /// int32x2_t vsubhn_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VSUBHN.I64 Dd, Qn, Qm
        ///   A64: SUBHN Vd.2S, Vn.2D, Vm.2D
        /// </summary>
        public static Vector64<int> SubtractHighNarrowingLower(Vector128<long> left, Vector128<long> right) => SubtractHighNarrowingLower(left, right);

        /// <summary>
        /// int8x8_t vsubhn_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VSUBHN.I16 Dd, Qn, Qm
        ///   A64: SUBHN Vd.8B, Vn.8H, Vm.8H
        /// </summary>
        public static Vector64<sbyte> SubtractHighNarrowingLower(Vector128<short> left, Vector128<short> right) => SubtractHighNarrowingLower(left, right);

        /// <summary>
        /// uint16x4_t vsubhn_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VSUBHN.I32 Dd, Qn, Qm
        ///   A64: SUBHN Vd.4H, Vn.4S, Vm.4S
        /// </summary>
        public static Vector64<ushort> SubtractHighNarrowingLower(Vector128<uint> left, Vector128<uint> right) => SubtractHighNarrowingLower(left, right);

        /// <summary>
        /// uint32x2_t vsubhn_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VSUBHN.I64 Dd, Qn, Qm
        ///   A64: SUBHN Vd.2S, Vn.2D, Vm.2D
        /// </summary>
        public static Vector64<uint> SubtractHighNarrowingLower(Vector128<ulong> left, Vector128<ulong> right) => SubtractHighNarrowingLower(left, right);

        /// <summary>
        /// uint8x16_t vsubhn_high_u16 (uint8x8_t r, uint16x8_t a, uint16x8_t b)
        ///   A32: VSUBHN.I16 Dd+1, Qn, Qm
        ///   A64: SUBHN2 Vd.16B, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<byte> SubtractHighNarrowingUpper(Vector64<byte> lower, Vector128<ushort> left, Vector128<ushort> right) => SubtractHighNarrowingUpper(lower, left, right);

        /// <summary>
        /// int16x8_t vsubhn_high_s32 (int16x4_t r, int32x4_t a, int32x4_t b)
        ///   A32: VSUBHN.I32 Dd+1, Qn, Qm
        ///   A64: SUBHN2 Vd.8H, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<short> SubtractHighNarrowingUpper(Vector64<short> lower, Vector128<int> left, Vector128<int> right) => SubtractHighNarrowingUpper(lower, left, right);

        /// <summary>
        /// int32x4_t vsubhn_high_s64 (int32x2_t r, int64x2_t a, int64x2_t b)
        ///   A32: VSUBHN.I64 Dd+1, Qn, Qm
        ///   A64: SUBHN2 Vd.4S, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<int> SubtractHighNarrowingUpper(Vector64<int> lower, Vector128<long> left, Vector128<long> right) => SubtractHighNarrowingUpper(lower, left, right);

        /// <summary>
        /// int8x16_t vsubhn_high_s16 (int8x8_t r, int16x8_t a, int16x8_t b)
        ///   A32: VSUBHN.I16 Dd+1, Qn, Qm
        ///   A64: SUBHN2 Vd.16B, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<sbyte> SubtractHighNarrowingUpper(Vector64<sbyte> lower, Vector128<short> left, Vector128<short> right) => SubtractHighNarrowingUpper(lower, left, right);

        /// <summary>
        /// uint16x8_t vsubhn_high_u32 (uint16x4_t r, uint32x4_t a, uint32x4_t b)
        ///   A32: VSUBHN.I32 Dd+1, Qn, Qm
        ///   A64: SUBHN2 Vd.8H, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<ushort> SubtractHighNarrowingUpper(Vector64<ushort> lower, Vector128<uint> left, Vector128<uint> right) => SubtractHighNarrowingUpper(lower, left, right);

        /// <summary>
        /// uint32x4_t vsubhn_high_u64 (uint32x2_t r, uint64x2_t a, uint64x2_t b)
        ///   A32: VSUBHN.I64 Dd+1, Qn, Qm
        ///   A64: SUBHN2 Vd.4S, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<uint> SubtractHighNarrowingUpper(Vector64<uint> lower, Vector128<ulong> left, Vector128<ulong> right) => SubtractHighNarrowingUpper(lower, left, right);

        /// <summary>
        /// uint8x8_t vrsubhn_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VRSUBHN.I16 Dd, Qn, Qm
        ///   A64: RSUBHN Vd.8B, Vn.8H, Vm.8H
        /// </summary>
        public static Vector64<byte> SubtractRoundedHighNarrowingLower(Vector128<ushort> left, Vector128<ushort> right) => SubtractRoundedHighNarrowingLower(left, right);

        /// <summary>
        /// int16x4_t vrsubhn_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VRSUBHN.I32 Dd, Qn, Qm
        ///   A64: RSUBHN Vd.4H, Vn.4S, Vm.4S
        /// </summary>
        public static Vector64<short> SubtractRoundedHighNarrowingLower(Vector128<int> left, Vector128<int> right) => SubtractRoundedHighNarrowingLower(left, right);

        /// <summary>
        /// int32x2_t vrsubhn_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VRSUBHN.I64 Dd, Qn, Qm
        ///   A64: RSUBHN Vd.2S, Vn.2D, Vm.2D
        /// </summary>
        public static Vector64<int> SubtractRoundedHighNarrowingLower(Vector128<long> left, Vector128<long> right) => SubtractRoundedHighNarrowingLower(left, right);

        /// <summary>
        /// int8x8_t vrsubhn_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VRSUBHN.I16 Dd, Qn, Qm
        ///   A64: RSUBHN Vd.8B, Vn.8H, Vm.8H
        /// </summary>
        public static Vector64<sbyte> SubtractRoundedHighNarrowingLower(Vector128<short> left, Vector128<short> right) => SubtractRoundedHighNarrowingLower(left, right);

        /// <summary>
        /// uint16x4_t vrsubhn_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VRSUBHN.I32 Dd, Qn, Qm
        ///   A64: RSUBHN Vd.4H, Vn.4S, Vm.4S
        /// </summary>
        public static Vector64<ushort> SubtractRoundedHighNarrowingLower(Vector128<uint> left, Vector128<uint> right) => SubtractRoundedHighNarrowingLower(left, right);

        /// <summary>
        /// uint32x2_t vrsubhn_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VRSUBHN.I64 Dd, Qn, Qm
        ///   A64: RSUBHN Vd.2S, Vn.2D, Vm.2D
        /// </summary>
        public static Vector64<uint> SubtractRoundedHighNarrowingLower(Vector128<ulong> left, Vector128<ulong> right) => SubtractRoundedHighNarrowingLower(left, right);

        /// <summary>
        /// uint8x16_t vrsubhn_high_u16 (uint8x8_t r, uint16x8_t a, uint16x8_t b)
        ///   A32: VRSUBHN.I16 Dd+1, Qn, Qm
        ///   A64: RSUBHN2 Vd.16B, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<byte> SubtractRoundedHighNarrowingUpper(Vector64<byte> lower, Vector128<ushort> left, Vector128<ushort> right) => SubtractRoundedHighNarrowingUpper(lower, left, right);

        /// <summary>
        /// int16x8_t vrsubhn_high_s32 (int16x4_t r, int32x4_t a, int32x4_t b)
        ///   A32: VRSUBHN.I32 Dd+1, Qn, Qm
        ///   A64: RSUBHN2 Vd.8H, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<short> SubtractRoundedHighNarrowingUpper(Vector64<short> lower, Vector128<int> left, Vector128<int> right) => SubtractRoundedHighNarrowingUpper(lower, left, right);

        /// <summary>
        /// int32x4_t vrsubhn_high_s64 (int32x2_t r, int64x2_t a, int64x2_t b)
        ///   A32: VRSUBHN.I64 Dd+1, Qn, Qm
        ///   A64: RSUBHN2 Vd.4S, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<int> SubtractRoundedHighNarrowingUpper(Vector64<int> lower, Vector128<long> left, Vector128<long> right) => SubtractRoundedHighNarrowingUpper(lower, left, right);

        /// <summary>
        /// int8x16_t vrsubhn_high_s16 (int8x8_t r, int16x8_t a, int16x8_t b)
        ///   A32: VRSUBHN.I16 Dd+1, Qn, Qm
        ///   A64: RSUBHN2 Vd.16B, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<sbyte> SubtractRoundedHighNarrowingUpper(Vector64<sbyte> lower, Vector128<short> left, Vector128<short> right) => SubtractRoundedHighNarrowingUpper(lower, left, right);

        /// <summary>
        /// uint16x8_t vrsubhn_high_u32 (uint16x4_t r, uint32x4_t a, uint32x4_t b)
        ///   A32: VRSUBHN.I32 Dd+1, Qn, Qm
        ///   A64: RSUBHN2 Vd.8H, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<ushort> SubtractRoundedHighNarrowingUpper(Vector64<ushort> lower, Vector128<uint> left, Vector128<uint> right) => SubtractRoundedHighNarrowingUpper(lower, left, right);

        /// <summary>
        /// uint32x4_t vrsubhn_high_u64 (uint32x2_t r, uint64x2_t a, uint64x2_t b)
        ///   A32: VRSUBHN.I64 Dd+1, Qn, Qm
        ///   A64: RSUBHN2 Vd.4S, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<uint> SubtractRoundedHighNarrowingUpper(Vector64<uint> lower, Vector128<ulong> left, Vector128<ulong> right) => SubtractRoundedHighNarrowingUpper(lower, left, right);

        /// <summary>
        /// uint8x8_t vqsub_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VQSUB.U8 Dd, Dn, Dm
        ///   A64: UQSUB Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> SubtractSaturate(Vector64<byte> left, Vector64<byte> right) => SubtractSaturate(left, right);

        /// <summary>
        /// int16x4_t vqsub_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VQSUB.S16 Dd, Dn, Dm
        ///   A64: SQSUB Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> SubtractSaturate(Vector64<short> left, Vector64<short> right) => SubtractSaturate(left, right);

        /// <summary>
        /// int32x2_t vqsub_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VQSUB.S32 Dd, Dn, Dm
        ///   A64: SQSUB Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> SubtractSaturate(Vector64<int> left, Vector64<int> right) => SubtractSaturate(left, right);

        /// <summary>
        /// int8x8_t vqsub_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VQSUB.S8 Dd, Dn, Dm
        ///   A64: SQSUB Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> SubtractSaturate(Vector64<sbyte> left, Vector64<sbyte> right) => SubtractSaturate(left, right);

        /// <summary>
        /// uint16x4_t vqsub_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VQSUB.U16 Dd, Dn, Dm
        ///   A64: UQSUB Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> SubtractSaturate(Vector64<ushort> left, Vector64<ushort> right) => SubtractSaturate(left, right);

        /// <summary>
        /// uint32x2_t vqsub_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VQSUB.U32 Dd, Dn, Dm
        ///   A64: UQSUB Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> SubtractSaturate(Vector64<uint> left, Vector64<uint> right) => SubtractSaturate(left, right);

        /// <summary>
        /// uint8x16_t vqsubq_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VQSUB.U8 Qd, Qn, Qm
        ///   A64: UQSUB Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> SubtractSaturate(Vector128<byte> left, Vector128<byte> right) => SubtractSaturate(left, right);

        /// <summary>
        /// int16x8_t vqsubq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VQSUB.S16 Qd, Qn, Qm
        ///   A64: SQSUB Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> SubtractSaturate(Vector128<short> left, Vector128<short> right) => SubtractSaturate(left, right);

        /// <summary>
        /// int32x4_t vqsubq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VQSUB.S32 Qd, Qn, Qm
        ///   A64: SQSUB Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> SubtractSaturate(Vector128<int> left, Vector128<int> right) => SubtractSaturate(left, right);

        /// <summary>
        /// int64x2_t vqsubq_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VQSUB.S64 Qd, Qn, Qm
        ///   A64: SQSUB Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<long> SubtractSaturate(Vector128<long> left, Vector128<long> right) => SubtractSaturate(left, right);

        /// <summary>
        /// int8x16_t vqsubq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VQSUB.S8 Qd, Qn, Qm
        ///   A64: SQSUB Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> SubtractSaturate(Vector128<sbyte> left, Vector128<sbyte> right) => SubtractSaturate(left, right);

        /// <summary>
        /// uint16x8_t vqsubq_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VQSUB.U16 Qd, Qn, Qm
        ///   A64: UQSUB Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> SubtractSaturate(Vector128<ushort> left, Vector128<ushort> right) => SubtractSaturate(left, right);

        /// <summary>
        /// uint32x4_t vqsubq_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VQSUB.U32 Qd, Qn, Qm
        ///   A64: UQSUB Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> SubtractSaturate(Vector128<uint> left, Vector128<uint> right) => SubtractSaturate(left, right);

        /// <summary>
        /// uint64x2_t vqsubq_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VQSUB.U64 Qd, Qn, Qm
        ///   A64: UQSUB Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<ulong> SubtractSaturate(Vector128<ulong> left, Vector128<ulong> right) => SubtractSaturate(left, right);

        /// <summary>
        /// int64x1_t vqsub_s64 (int64x1_t a, int64x1_t b)
        ///   A32: VQSUB.S64 Dd, Dn, Dm
        ///   A64: SQSUB Dd, Dn, Dm
        /// </summary>
        public static Vector64<long> SubtractSaturateScalar(Vector64<long> left, Vector64<long> right) => SubtractSaturateScalar(left, right);

        /// <summary>
        /// uint64x1_t vqsub_u64 (uint64x1_t a, uint64x1_t b)
        ///   A32: VQSUB.U64 Dd, Dn, Dm
        ///   A64: UQSUB Dd, Dn, Dm
        /// </summary>
        public static Vector64<ulong> SubtractSaturateScalar(Vector64<ulong> left, Vector64<ulong> right) => SubtractSaturateScalar(left, right);

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
        /// uint16x8_t vsubl_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VSUBL.U8 Qd, Dn, Dm
        ///   A64: USUBL Vd.8H, Vn.8B, Vm.8B
        /// </summary>
        public static Vector128<ushort> SubtractWideningLower(Vector64<byte> left, Vector64<byte> right) => SubtractWideningLower(left, right);

        /// <summary>
        /// int32x4_t vsubl_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VSUBL.S16 Qd, Dn, Dm
        ///   A64: SSUBL Vd.4S, Vn.4H, Vm.4H
        /// </summary>
        public static Vector128<int> SubtractWideningLower(Vector64<short> left, Vector64<short> right) => SubtractWideningLower(left, right);

        /// <summary>
        /// int64x2_t vsubl_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VSUBL.S32 Qd, Dn, Dm
        ///   A64: SSUBL Vd.2D, Vn.2S, Vm.2S
        /// </summary>
        public static Vector128<long> SubtractWideningLower(Vector64<int> left, Vector64<int> right) => SubtractWideningLower(left, right);

        /// <summary>
        /// int16x8_t vsubl_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VSUBL.S8 Qd, Dn, Dm
        ///   A64: SSUBL Vd.8H, Vn.8B, Vm.8B
        /// </summary>
        public static Vector128<short> SubtractWideningLower(Vector64<sbyte> left, Vector64<sbyte> right) => SubtractWideningLower(left, right);

        /// <summary>
        /// uint32x4_t vsubl_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VSUBL.U16 Qd, Dn, Dm
        ///   A64: USUBL Vd.4S, Vn.4H, Vm.4H
        /// </summary>
        public static Vector128<uint> SubtractWideningLower(Vector64<ushort> left, Vector64<ushort> right) => SubtractWideningLower(left, right);

        /// <summary>
        /// uint64x2_t vsubl_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VSUBL.U32 Qd, Dn, Dm
        ///   A64: USUBL Vd.2D, Vn.2S, Vm.2S
        /// </summary>
        public static Vector128<ulong> SubtractWideningLower(Vector64<uint> left, Vector64<uint> right) => SubtractWideningLower(left, right);

        /// <summary>
        /// int16x8_t vsubw_s8 (int16x8_t a, int8x8_t b)
        ///   A32: VSUBW.S8 Qd, Qn, Dm
        ///   A64: SSUBW Vd.8H, Vn.8H, Vm.8B
        /// </summary>
        public static Vector128<short> SubtractWideningLower(Vector128<short> left, Vector64<sbyte> right) => SubtractWideningLower(left, right);

        /// <summary>
        /// int32x4_t vsubw_s16 (int32x4_t a, int16x4_t b)
        ///   A32: VSUBW.S16 Qd, Qn, Dm
        ///   A64: SSUBW Vd.4S, Vn.4S, Vm.4H
        /// </summary>
        public static Vector128<int> SubtractWideningLower(Vector128<int> left, Vector64<short> right) => SubtractWideningLower(left, right);

        /// <summary>
        /// int64x2_t vsubw_s32 (int64x2_t a, int32x2_t b)
        ///   A32: VSUBW.S32 Qd, Qn, Dm
        ///   A64: SSUBW Vd.2D, Vn.2D, Vm.2S
        /// </summary>
        public static Vector128<long> SubtractWideningLower(Vector128<long> left, Vector64<int> right) => SubtractWideningLower(left, right);

        /// <summary>
        /// uint16x8_t vsubw_u8 (uint16x8_t a, uint8x8_t b)
        ///   A32: VSUBW.U8 Qd, Qn, Dm
        ///   A64: USUBW Vd.8H, Vn.8H, Vm.8B
        /// </summary>
        public static Vector128<ushort> SubtractWideningLower(Vector128<ushort> left, Vector64<byte> right) => SubtractWideningLower(left, right);

        /// <summary>
        /// uint32x4_t vsubw_u16 (uint32x4_t a, uint16x4_t b)
        ///   A32: VSUBW.U16 Qd, Qn, Dm
        ///   A64: USUBW Vd.4S, Vn.4S, Vm.4H
        /// </summary>
        public static Vector128<uint> SubtractWideningLower(Vector128<uint> left, Vector64<ushort> right) => SubtractWideningLower(left, right);

        /// <summary>
        /// uint64x2_t vsubw_u32 (uint64x2_t a, uint32x2_t b)
        ///   A32: VSUBW.U32 Qd, Qn, Dm
        ///   A64: USUBW Vd.2D, Vn.2D, Vm.2S
        /// </summary>
        public static Vector128<ulong> SubtractWideningLower(Vector128<ulong> left, Vector64<uint> right) => SubtractWideningLower(left, right);

        /// <summary>
        /// uint16x8_t vsubl_high_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VSUBL.U8 Qd, Dn+1, Dm+1
        ///   A64: USUBL2 Vd.8H, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<ushort> SubtractWideningUpper(Vector128<byte> left, Vector128<byte> right) => SubtractWideningUpper(left, right);

        /// <summary>
        /// int32x4_t vsubl_high_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VSUBL.S16 Qd, Dn+1, Dm+1
        ///   A64: SSUBL2 Vd.4S, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<int> SubtractWideningUpper(Vector128<short> left, Vector128<short> right) => SubtractWideningUpper(left, right);

        /// <summary>
        /// int16x8_t vsubw_high_s8 (int16x8_t a, int8x16_t b)
        ///   A32: VSUBW.S8 Qd, Qn, Dm+1
        ///   A64: SSUBW2 Vd.8H, Vn.8H, Vm.16B
        /// </summary>
        public static Vector128<short> SubtractWideningUpper(Vector128<short> left, Vector128<sbyte> right) => SubtractWideningUpper(left, right);

        /// <summary>
        /// int32x4_t vsubw_high_s16 (int32x4_t a, int16x8_t b)
        ///   A32: VSUBW.S16 Qd, Qn, Dm+1
        ///   A64: SSUBW2 Vd.4S, Vn.4S, Vm.8H
        /// </summary>
        public static Vector128<int> SubtractWideningUpper(Vector128<int> left, Vector128<short> right) => SubtractWideningUpper(left, right);

        /// <summary>
        /// int64x2_t vsubl_high_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VSUBL.S32 Qd, Dn+1, Dm+1
        ///   A64: SSUBL2 Vd.2D, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<long> SubtractWideningUpper(Vector128<int> left, Vector128<int> right) => SubtractWideningUpper(left, right);

        /// <summary>
        /// int64x2_t vsubw_high_s32 (int64x2_t a, int32x4_t b)
        ///   A32: VSUBW.S32 Qd, Qn, Dm+1
        ///   A64: SSUBW2 Vd.2D, Vn.2D, Vm.4S
        /// </summary>
        public static Vector128<long> SubtractWideningUpper(Vector128<long> left, Vector128<int> right) => SubtractWideningUpper(left, right);

        /// <summary>
        /// int16x8_t vsubl_high_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VSUBL.S8 Qd, Dn+1, Dm+1
        ///   A64: SSUBL2 Vd.8H, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<short> SubtractWideningUpper(Vector128<sbyte> left, Vector128<sbyte> right) => SubtractWideningUpper(left, right);

        /// <summary>
        /// uint16x8_t vsubw_high_u8 (uint16x8_t a, uint8x16_t b)
        ///   A32: VSUBW.U8 Qd, Qn, Dm+1
        ///   A64: USUBW2 Vd.8H, Vn.8H, Vm.16B
        /// </summary>
        public static Vector128<ushort> SubtractWideningUpper(Vector128<ushort> left, Vector128<byte> right) => SubtractWideningUpper(left, right);

        /// <summary>
        /// uint32x4_t vsubl_high_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VSUBL.U16 Qd, Dn+1, Dm+1
        ///   A64: USUBL2 Vd.4S, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<uint> SubtractWideningUpper(Vector128<ushort> left, Vector128<ushort> right) => SubtractWideningUpper(left, right);

        /// <summary>
        /// uint32x4_t vsubw_high_u16 (uint32x4_t a, uint16x8_t b)
        ///   A32: VSUBW.U16 Qd, Qn, Dm+1
        ///   A64: USUBW2 Vd.4S, Vn.4S, Vm.8H
        /// </summary>
        public static Vector128<uint> SubtractWideningUpper(Vector128<uint> left, Vector128<ushort> right) => SubtractWideningUpper(left, right);

        /// <summary>
        /// uint64x2_t vsubl_high_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VSUBL.U32 Qd, Dn+1, Dm+1
        ///   A64: USUBL2 Vd.2D, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<ulong> SubtractWideningUpper(Vector128<uint> left, Vector128<uint> right) => SubtractWideningUpper(left, right);

        /// <summary>
        /// uint64x2_t vsubw_high_u32 (uint64x2_t a, uint32x4_t b)
        ///   A32: VSUBW.U32 Qd, Qn, Dm+1
        ///   A64: USUBW2 Vd.2D, Vn.2D, Vm.4S
        /// </summary>
        public static Vector128<ulong> SubtractWideningUpper(Vector128<ulong> left, Vector128<uint> right) => SubtractWideningUpper(left, right);

        /// <summary>
        ///  uint8x8_t vqvtbl1_u8(uint8x16_t t, uint8x8_t idx)
        ///   A32: VTBL Dd, {Dn, Dn+1}, Dm
        ///   A64: TBL Vd.8B, {Vn.16B}, Vm.8B
        /// </summary>
        public static Vector64<byte> VectorTableLookup(Vector128<byte> table, Vector64<byte> byteIndexes) => VectorTableLookup(table, byteIndexes);

        /// <summary>
        ///  int8x8_t vqvtbl1_s8(int8x16_t t, uint8x8_t idx)
        ///   A32: VTBL Dd, {Dn, Dn+1}, Dm
        ///   A64: TBL Vd.8B, {Vn.16B}, Vm.8B
        /// </summary>
        public static Vector64<sbyte> VectorTableLookup(Vector128<sbyte> table, Vector64<sbyte> byteIndexes) => VectorTableLookup(table, byteIndexes);

        /// <summary>
        ///  uint8x8_t vqvtbx1_u8(uint8x8_t r, uint8x16_t t, uint8x8_t idx)
        ///   A32: VTBX Dd, {Dn, Dn+1}, Dm
        ///   A64: TBX Vd.8B, {Vn.16B}, Vm.8B
        /// </summary>
        public static Vector64<byte> VectorTableLookupExtension(Vector64<byte> defaultValues, Vector128<byte> table, Vector64<byte> byteIndexes) => VectorTableLookupExtension(defaultValues, table, byteIndexes);

        /// <summary>
        ///  int8x8_t vqvtbx1_s8(int8x8_t r, int8x16_t t, uint8x8_t idx)
        ///   A32: VTBX Dd, {Dn, Dn+1}, Dm
        ///   A64: TBX Vd.8B, {Vn.16B}, Vm.8B
        /// </summary>
        public static Vector64<sbyte> VectorTableLookupExtension(Vector64<sbyte> defaultValues, Vector128<sbyte> table, Vector64<sbyte> byteIndexes) => VectorTableLookupExtension(defaultValues, table, byteIndexes);

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

        /// <summary>
        /// uint16x8_t vmovl_u8 (uint8x8_t a)
        ///   A32: VMOVL.U8 Qd, Dm
        ///   A64: UXTL Vd.8H, Vn.8B
        /// </summary>
        public static Vector128<ushort> ZeroExtendWideningLower(Vector64<byte> value) => ZeroExtendWideningLower(value);

        /// <summary>
        /// uint32x4_t vmovl_u16 (uint16x4_t a)
        ///   A32: VMOVL.U16 Qd, Dm
        ///   A64: UXTL Vd.4S, Vn.4H
        /// </summary>
        public static Vector128<int> ZeroExtendWideningLower(Vector64<short> value) => ZeroExtendWideningLower(value);

        /// <summary>
        /// uint64x2_t vmovl_u32 (uint32x2_t a)
        ///   A32: VMOVL.U32 Qd, Dm
        ///   A64: UXTL Vd.2D, Vn.2S
        /// </summary>
        public static Vector128<long> ZeroExtendWideningLower(Vector64<int> value) => ZeroExtendWideningLower(value);

        /// <summary>
        /// uint16x8_t vmovl_u8 (uint8x8_t a)
        ///   A32: VMOVL.U8 Qd, Dm
        ///   A64: UXTL Vd.8H, Vn.8B
        /// </summary>
        public static Vector128<short> ZeroExtendWideningLower(Vector64<sbyte> value) => ZeroExtendWideningLower(value);

        /// <summary>
        /// uint32x4_t vmovl_u16 (uint16x4_t a)
        ///   A32: VMOVL.U16 Qd, Dm
        ///   A64: UXTL Vd.4S, Vn.4H
        /// </summary>
        public static Vector128<uint> ZeroExtendWideningLower(Vector64<ushort> value) => ZeroExtendWideningLower(value);

        /// <summary>
        /// uint64x2_t vmovl_u32 (uint32x2_t a)
        ///   A32: VMOVL.U32 Qd, Dm
        ///   A64: UXTL Vd.2D, Vn.2S
        /// </summary>
        public static Vector128<ulong> ZeroExtendWideningLower(Vector64<uint> value) => ZeroExtendWideningLower(value);

        /// <summary>
        /// uint16x8_t vmovl_high_u8 (uint8x16_t a)
        ///   A32: VMOVL.U8 Qd, Dm+1
        ///   A64: UXTL2 Vd.8H, Vn.16B
        /// </summary>
        public static Vector128<ushort> ZeroExtendWideningUpper(Vector128<byte> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// uint32x4_t vmovl_high_u16 (uint16x8_t a)
        ///   A32: VMOVL.U16 Qd, Dm+1
        ///   A64: UXTL2 Vd.4S, Vn.8H
        /// </summary>
        public static Vector128<int> ZeroExtendWideningUpper(Vector128<short> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// uint64x2_t vmovl_high_u32 (uint32x4_t a)
        ///   A32: VMOVL.U32 Qd, Dm+1
        ///   A64: UXTL2 Vd.2D, Vn.4S
        /// </summary>
        public static Vector128<long> ZeroExtendWideningUpper(Vector128<int> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// uint16x8_t vmovl_high_u8 (uint8x16_t a)
        ///   A32: VMOVL.U8 Qd, Dm+1
        ///   A64: UXTL2 Vd.8H, Vn.16B
        /// </summary>
        public static Vector128<short> ZeroExtendWideningUpper(Vector128<sbyte> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// uint32x4_t vmovl_high_u16 (uint16x8_t a)
        ///   A32: VMOVL.U16 Qd, Dm+1
        ///   A64: UXTL2 Vd.4S, Vn.8H
        /// </summary>
        public static Vector128<uint> ZeroExtendWideningUpper(Vector128<ushort> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// uint64x2_t vmovl_high_u32 (uint32x4_t a)
        ///   A32: VMOVL.U32 Qd, Dm+1
        ///   A64: UXTL2 Vd.2D, Vn.4S
        /// </summary>
        public static Vector128<ulong> ZeroExtendWideningUpper(Vector128<uint> value) => ZeroExtendWideningUpper(value);
    }
}
