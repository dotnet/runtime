// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to X86 AVX10.2 hardware instructions via intrinsics</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Avx10v2 : Avx10v1
    {
        internal Avx10v2() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { get => IsSupported; }

        /// <summary>
        ///   <para>  VMINMAXPD xmm1{k1}{z}, xmm2, xmm3/m128/m64bcst, imm8</para>
        /// </summary>
        public static Vector128<double> MinMax(Vector128<double> left, Vector128<double> right, [ConstantExpected] byte control) => MinMax(left, right, control);

        /// <summary>
        ///   <para>  VMINMAXPD ymm1{k1}{z}, ymm2, ymm3/m256/m64bcst {sae}, imm8</para>
        /// </summary>
        public static Vector256<double> MinMax(Vector256<double> left, Vector256<double> right, [ConstantExpected] byte control) => MinMax(left, right, control);

        /// <summary>
        ///   <para>  VMINMAXPS xmm1{k1}{z}, xmm2, xmm3/m128/m32bcst, imm8</para>
        /// </summary>
        public static Vector128<float> MinMax(Vector128<float> left, Vector128<float> right, [ConstantExpected] byte control) => MinMax(left, right, control);

        /// <summary>
        ///   <para>  VMINMAXPS ymm1{k1}{z}, ymm2, ymm3/m256/m32bcst {sae}, imm8</para>
        /// </summary>
        public static Vector256<float> MinMax(Vector256<float> left, Vector256<float> right, [ConstantExpected] byte control) => MinMax(left, right, control);

        /// <summary>
        ///   <para>  VMINMAXSD xmm1{k1}{z}, xmm2, xmm3/m64 {sae}, imm8</para>
        /// </summary>
        public static Vector128<double> MinMaxScalar(Vector128<double> left, Vector128<double> right, [ConstantExpected] byte control) => MinMaxScalar(left, right, control);

        /// <summary>
        ///   <para>  VMINMAXSS xmm1{k1}{z}, xmm2, xmm3/m32 {sae}, imm8</para>
        /// </summary>
        public static Vector128<float> MinMaxScalar(Vector128<float> left, Vector128<float> right, [ConstantExpected] byte control) => MinMaxScalar(left, right, control);

        /// <summary>
        ///   <para>  VCVTPS2IBS xmm1{k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector128<int> ConvertToSByteWithSaturationAndZeroExtendToInt32(Vector128<float> value) => ConvertToSByteWithSaturationAndZeroExtendToInt32(value);

        /// <summary>
        ///   <para>  VCVTPS2IBS ymm1{k1}{z}, ymm2/m256/m32bcst {er}</para>
        /// </summary>
        public static Vector256<int> ConvertToSByteWithSaturationAndZeroExtendToInt32(Vector256<float> value) => ConvertToSByteWithSaturationAndZeroExtendToInt32(value);

        /// <summary>
        ///   <para>  VCVTPS2IBS ymm1{k1}{z}, ymm2/m256/m32bcst {er}</para>
        /// </summary>
        public static Vector256<int> ConvertToSByteWithSaturationAndZeroExtendToInt32(Vector256<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToSByteWithSaturationAndZeroExtendToInt32(value, mode);

        /// <summary>
        ///   <para>  VCVTPS2IUBS xmm1{k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector128<int> ConvertToByteWithSaturationAndZeroExtendToInt32(Vector128<float> value) => ConvertToByteWithSaturationAndZeroExtendToInt32(value);

        /// <summary>
        ///   <para>  VCVTPS2IUBS ymm1{k1}{z}, ymm2/m256/m32bcst {er}</para>
        /// </summary>
        public static Vector256<int> ConvertToByteWithSaturationAndZeroExtendToInt32(Vector256<float> value) => ConvertToByteWithSaturationAndZeroExtendToInt32(value);

        /// <summary>
        ///   <para>  VCVTPS2IUBS ymm1{k1}{z}, ymm2/m256/m32bcst {er}</para>
        /// </summary>
        public static Vector256<int> ConvertToByteWithSaturationAndZeroExtendToInt32(Vector256<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToByteWithSaturationAndZeroExtendToInt32(value, mode);

        /// <summary>
        ///   <para>  VCVTTPS2IBS xmm1{k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector128<int> ConvertToSByteWithTruncatedSaturationAndZeroExtendToInt32(Vector128<float> value) => ConvertToSByteWithTruncatedSaturationAndZeroExtendToInt32(value);

        /// <summary>
        ///   <para>  VCVTTPS2IBS ymm1{k1}{z}, ymm2/m256/m32bcst {sae}</para>
        /// </summary>
        public static Vector256<int> ConvertToSByteWithTruncatedSaturationAndZeroExtendToInt32(Vector256<float> value) => ConvertToSByteWithTruncatedSaturationAndZeroExtendToInt32(value);

        /// <summary>
        ///   <para>  VCVTTPS2IUBS xmm1{k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector128<int> ConvertToByteWithTruncatedSaturationAndZeroExtendToInt32(Vector128<float> value) => ConvertToByteWithTruncatedSaturationAndZeroExtendToInt32(value);

        /// <summary>
        ///   <para>  VCVTTPS2IUBS ymm1{k1}{z}, ymm2/m256/m32bcst {sae}</para>
        /// </summary>
        public static Vector256<int> ConvertToByteWithTruncatedSaturationAndZeroExtendToInt32(Vector256<float> value) => ConvertToByteWithTruncatedSaturationAndZeroExtendToInt32(value);

        /// <summary>
        ///   <para>  VMOVD xmm1, xmm2/m32</para>
        /// </summary>
        public static Vector128<uint> ConvertToVector128UInt32(Vector128<uint> value) => ConvertToVector128UInt32(value);

        /// <summary>
        ///   <para>  VMOVW xmm1, xmm2/m16</para>
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16(Vector128<ushort> value) => ConvertToVector128UInt16(value);

        /// <summary>
        ///   <para>  VADDPD ymm1{k1}{z}, ymm2, ymm3/m256/m64bcst {er}</para>
        /// </summary>
        public static Vector256<double> Add(Vector256<double> left, Vector256<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => Add(left, right, mode);

        /// <summary>
        ///   <para>  VADDPS ymm1{k1}{z}, ymm2, ymm3/m256/m32bcst {er}</para>
        /// </summary>
        public static Vector256<float> Add(Vector256<float> left, Vector256<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => Add(left, right, mode);

        /// <summary>
        ///   <para>  VDIVPD ymm1{k1}{z}, ymm2, ymm3/m256/m64bcst {er}</para>
        /// </summary>
        public static Vector256<double> Divide(Vector256<double> left, Vector256<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => Divide(left, right, mode);

        /// <summary>
        ///   <para>  VDIVPS ymm1{k1}{z}, ymm2, ymm3/m256/m32bcst {er}</para>
        /// </summary>
        public static Vector256<float> Divide(Vector256<float> left, Vector256<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => Divide(left, right, mode);

        /// <summary>
        ///   <para>  VCVTDQ2PS ymm1{k1}{z}, ymm2/m256/m32bcst {er}</para>
        /// </summary>
        public static Vector256<float> ConvertToVector256Single(Vector256<int> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToVector256Single(value, mode);

        /// <summary>
        ///   <para>  VCVTPD2DQ xmm1{k1}{z}, ymm2/m256/m64bcst {er}</para>
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32(Vector256<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToVector128Int32(value, mode);

        /// <summary>
        ///   <para>  VCVTPD2PS xmm1{k1}{z}, ymm2/m256/m64bcst {er}</para>
        /// </summary>
        public static Vector128<float> ConvertToVector128Single(Vector256<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToVector128Single(value, mode);

        /// <summary>
        ///   <para>  VCVTPD2QQ ymm1{k1}{z}, ymm2/m256/m64bcst {er}</para>
        /// </summary>
        public static Vector256<long> ConvertToVector256Int64(Vector256<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToVector256Int64(value, mode);

        /// <summary>
        ///   <para>  VCVTPD2UDQ xmm1{k1}{z}, ymm2/m256/m64bcst {er}</para>
        /// </summary>
        public static Vector128<uint> ConvertToVector128UInt32(Vector256<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToVector128UInt32(value, mode);

        /// <summary>
        ///   <para>  VCVTPD2UQQ ymm1{k1}{z}, ymm2/m256/m64bcst {er}</para>
        /// </summary>
        public static Vector256<ulong> ConvertToVector256UInt64(Vector256<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToVector256UInt64(value, mode);

        /// <summary>
        ///   <para>  VCVTPS2DQ ymm1{k1}{z}, ymm2/m256/m32bcst {er}</para>
        /// </summary>
        public static Vector256<int> ConvertToVector256Int32(Vector256<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToVector256Int32(value, mode);

        /// <summary>
        ///   <para>  VCVTPS2QQ ymm1{k1}{z}, xmm2/m128/m32bcst {er}</para>
        /// </summary>
        public static Vector256<long> ConvertToVector256Int64(Vector128<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToVector256Int64(value, mode);

        /// <summary>
        ///   <para>  VCVTPS2UDQ ymm1{k1}{z}, ymm2/m256/m32bcst {er}</para>
        /// </summary>
        public static Vector256<uint> ConvertToVector256UInt32(Vector256<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToVector256UInt32(value, mode);

        /// <summary>
        ///   <para>  VCVTPS2UQQ ymm1{k1}{z}, xmm2/m128/m32bcst {er}</para>
        /// </summary>
        public static Vector256<ulong> ConvertToVector256UInt64(Vector128<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToVector256UInt64(value, mode);

        /// <summary>
        ///   <para>  VCVTQQ2PS xmm1{k1}{z}, ymm2/m256/m64bcst {er}</para>
        /// </summary>
        public static Vector128<float> ConvertToVector128Single(Vector256<ulong> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToVector128Single(value, mode);

        /// <summary>
        ///   <para>  VCVTQQ2PD ymm1{k1}{z}, ymm2/m256/m64bcst {er}</para>
        /// </summary>
        public static Vector256<double> ConvertToVector256Double(Vector256<ulong> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToVector256Double(value, mode);

        /// <summary>
        ///   <para>  VCVTUDQ2PS ymm1{k1}{z}, ymm2/m256/m32bcst {er}</para>
        /// </summary>
        public static Vector256<float> ConvertToVector256Single(Vector256<uint> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToVector256Single(value, mode);

        /// <summary>
        ///   <para>  VCVTUQQ2PS xmm1{k1}{z}, ymm2/m256/m64bcst {er}</para>
        /// </summary>
        public static Vector128<float> ConvertToVector128Single(Vector256<long> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToVector128Single(value, mode);

        /// <summary>
        ///   <para>  VCVTUQQ2PD ymm1{k1}{z}, ymm2/m256/m64bcst {er}</para>
        /// </summary>
        public static Vector256<double> ConvertToVector256Double(Vector256<long> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToVector256Double(value, mode);

        /// <summary>
        ///   <para>  VMULPD ymm1{k1}{z}, ymm2, ymm3/m256/m64bcst {er}</para>
        /// </summary>
        public static Vector256<double> Multiply(Vector256<double> left, Vector256<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => Multiply(left, right, mode);

        /// <summary>
        ///   <para>  VMULPS ymm1{k1}{z}, ymm2, ymm3/m256/m32bcst {er}</para>
        /// </summary>
        public static Vector256<float> Multiply(Vector256<float> left, Vector256<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => Multiply(left, right, mode);

        /// <summary>
        ///   <para>  VSCALEFPD ymm1{k1}{z}, ymm2, ymm3/m256/m64bcst {er}</para>
        /// </summary>
        public static Vector256<double> Scale(Vector256<double> left, Vector256<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => Scale(left, right, mode);

        /// <summary>
        ///   <para>  VSCALEFPS ymm1{k1}{z}, ymm2, ymm3/m256/m32bcst {er}</para>
        /// </summary>
        public static Vector256<float> Scale(Vector256<float> left, Vector256<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => Scale(left, right, mode);

        /// <summary>
        ///   <para>  VSQRTPD ymm1{k1}{z}, ymm2/m256/m64bcst {er}</para>
        /// </summary>
        public static Vector256<double> Sqrt(Vector256<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => Sqrt(value, mode);

        /// <summary>
        ///   <para>  VSQRTPS ymm1{k1}{z}, ymm2/m256/m32bcst {er}</para>
        /// </summary>
        public static Vector256<float> Sqrt(Vector256<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => Sqrt(value, mode);

        /// <summary>
        ///   <para>  VSUBPD ymm1{k1}{z}, ymm2, ymm3/m256/m64bcst {er}</para>
        /// </summary>
        public static Vector256<double> Subtract(Vector256<double> left, Vector256<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => Subtract(left, right, mode);

        /// <summary>
        ///   <para>  VSUBPS ymm1{k1}{z}, ymm2, ymm3/m256/m32bcst {er}</para>
        /// </summary>
        public static Vector256<float> Subtract(Vector256<float> left, Vector256<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => Subtract(left, right, mode);

        /// <summary>Provides access to the x86 AVX10.2 hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        [Intrinsic]
        public new abstract class X64 : Avx10v1.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { get => IsSupported; }
        }

        /// <summary>Provides access to the x86 AVX10.2/512 hardware instructions via intrinsics.</summary>
        [Intrinsic]
        public new abstract class V512 : Avx10v1.V512
        {
            internal V512() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { get => IsSupported; }

            /// <summary>
            ///   <para>  VMINMAXPD zmm1{k1}{z}, zmm2, zmm3/m512/m64bcst {sae}, imm8</para>
            /// </summary>
            public static Vector512<double> MinMax(Vector512<double> left, Vector512<double> right, [ConstantExpected] byte control) => MinMax(left, right, control);

            /// <summary>
            ///   <para>  VMINMAXPS zmm1{k1}{z}, zmm2, zmm3/m512/m32bcst {sae}, imm8</para>
            /// </summary>
            public static Vector512<float> MinMax(Vector512<float> left, Vector512<float> right, [ConstantExpected] byte control) => MinMax(left, right, control);

            /// <summary>
            ///   <para>  VCVTPS2IBS zmm1{k1}{z}, zmm2/m512/m32bcst {er}</para>
            /// </summary>
            public static Vector512<int> ConvertToSByteWithSaturationAndZeroExtendToInt32(Vector512<float> value) => ConvertToSByteWithSaturationAndZeroExtendToInt32(value);

            /// <summary>
            ///   <para>  VCVTPS2IBS zmm1{k1}{z}, zmm2/m512/m32bcst {er}</para>
            /// </summary>
            public static Vector512<int> ConvertToSByteWithSaturationAndZeroExtendToInt32(Vector512<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToSByteWithSaturationAndZeroExtendToInt32(value, mode);

            /// <summary>
            ///   <para>  VCVTPS2IUBS zmm1{k1}{z}, zmm2/m512/m32bcst {er}</para>
            /// </summary>
            public static Vector512<int> ConvertToByteWithSaturationAndZeroExtendToInt32(Vector512<float> value) => ConvertToByteWithSaturationAndZeroExtendToInt32(value);

            /// <summary>
            ///   <para>  VCVTPS2IUBS zmm1{k1}{z}, zmm2/m512/m32bcst {er}</para>
            /// </summary>
            public static Vector512<int> ConvertToByteWithSaturationAndZeroExtendToInt32(Vector512<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToByteWithSaturationAndZeroExtendToInt32(value, mode);

            /// <summary>
            ///   <para>  VCVTTPS2IBS zmm1{k1}{z}, zmm2/m512/m32bcst {sae}</para>
            /// </summary>
            public static Vector512<int> ConvertToSByteWithTruncatedSaturationAndZeroExtendToInt32(Vector512<float> value) => ConvertToSByteWithTruncatedSaturationAndZeroExtendToInt32(value);

            /// <summary>
            ///   <para>  VCVTTPS2IUBS zmm1{k1}{z}, zmm2/m512/m32bcst {sae}</para>
            /// </summary>
            public static Vector512<int> ConvertToByteWithTruncatedSaturationAndZeroExtendToInt32(Vector512<float> value) => ConvertToByteWithTruncatedSaturationAndZeroExtendToInt32(value);

            /// <summary>
            ///   <para>  VMPSADBW zmm1{k1}{z}, zmm2, zmm3/m512, imm8</para>
            /// </summary>
            public static Vector512<ushort> MultipleSumAbsoluteDifferences(Vector512<byte> left, Vector512<byte> right, [ConstantExpected] byte mask) => MultipleSumAbsoluteDifferences(left, right, mask);

            /// <summary>Provides access to the x86 AVX10.2/512 hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
            [Intrinsic]
            public new abstract class X64 : Avx10v1.V512.X64
            {
                internal X64() { }

                /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
                /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
                /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
                public static new bool IsSupported { get => IsSupported; }
            }
        }
    }
}
