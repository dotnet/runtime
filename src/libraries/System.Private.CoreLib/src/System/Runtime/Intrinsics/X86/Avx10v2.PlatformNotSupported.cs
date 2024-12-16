// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to X86 Avx10.2 hardware instructions via intrinsics.</summary>
    [CLSCompliant(false)]
    public abstract class Avx10v2 : Avx10v1
    {
        internal Avx10v2() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { [Intrinsic] get { return false; } }

        // VMINMAXPD xmm1{k1}{z}, xmm2, xmm3/m128/m64bcst, imm8
        public static Vector128<double> MinMax(Vector128<double> left, Vector128<double> right, [ConstantExpected] byte control)  { throw new PlatformNotSupportedException(); }

        // VMINMAXPD ymm1{k1}{z}, ymm2, ymm3/m256/m64bcst {sae}, imm8
        public static Vector256<double> MinMax(Vector256<double> left, Vector256<double> right, [ConstantExpected] byte control)  { throw new PlatformNotSupportedException(); }

        // VMINMAXPS xmm1{k1}{z}, xmm2, xmm3/m128/m32bcst, imm8
        public static Vector128<float> MinMax(Vector128<float> left, Vector128<float> right, [ConstantExpected] byte control)  { throw new PlatformNotSupportedException(); }

        // VMINMAXPS ymm1{k1}{z}, ymm2, ymm3/m256/m32bcst {sae}, imm8
        public static Vector256<float> MinMax(Vector256<float> left, Vector256<float> right, [ConstantExpected] byte control)  { throw new PlatformNotSupportedException(); }

        // VMINMAXSD xmm1{k1}{z}, xmm2, xmm3/m64 {sae}, imm8
        public static Vector128<double> MinMaxScalar(Vector128<double> left, Vector128<double> right, [ConstantExpected] byte control)  { throw new PlatformNotSupportedException(); }

        // VMINMAXSS xmm1{k1}{z}, xmm2, xmm3/m32 {sae}, imm8
        public static Vector128<float> MinMaxScalar(Vector128<float> left, Vector128<float> right, [ConstantExpected] byte control)  { throw new PlatformNotSupportedException(); }

        // VADDPD ymm1{k1}{z}, ymm2, ymm3/m256/m64bcst {er}
        public static Vector256<double> Add(Vector256<double> left, Vector256<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        // VADDPS ymm1{k1}{z}, ymm2, ymm3/m256/m32bcst {er}
        public static Vector256<float> Add(Vector256<float> left, Vector256<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        // VDIVPD ymm1{k1}{z}, ymm2, ymm3/m256/m64bcst {er}
        public static Vector256<double> Divide(Vector256<double> left, Vector256<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        // VDIVPS ymm1{k1}{z}, ymm2, ymm3/m256/m32bcst {er}
        public static Vector256<float> Divide(Vector256<float> left, Vector256<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        // VCVTPS2IBS xmm1{k1}{z}, xmm2/m128/m32bcst
        public static Vector128<int> ConvertToSByteWithSaturationAndZeroExtendToInt32(Vector128<float> value)  { throw new PlatformNotSupportedException(); }

        // VCVTPS2IBS ymm1{k1}{z}, ymm2/m256/m32bcst {er}
        public static Vector256<int> ConvertToSByteWithSaturationAndZeroExtendToInt32(Vector256<float> value)  { throw new PlatformNotSupportedException(); }

        // VCVTPS2IBS ymm1{k1}{z}, ymm2/m256/m32bcst {er}
        public static Vector256<int> ConvertToSByteWithSaturationAndZeroExtendToInt32(Vector256<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        // VCVTPS2IUBS xmm1{k1}{z}, xmm2/m128/m32bcst
        public static Vector128<int> ConvertToByteWithSaturationAndZeroExtendToInt32(Vector128<float> value)  { throw new PlatformNotSupportedException(); }

        // VCVTPS2IUBS ymm1{k1}{z}, ymm2/m256/m32bcst {er}
        public static Vector256<int> ConvertToByteWithSaturationAndZeroExtendToInt32(Vector256<float> value)  { throw new PlatformNotSupportedException(); }

        // VCVTPS2IUBS ymm1{k1}{z}, ymm2/m256/m32bcst {er}
        public static Vector256<int> ConvertToByteWithSaturationAndZeroExtendToInt32(Vector256<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        // VCVTTPS2IBS xmm1{k1}{z}, xmm2/m128/m32bcst
        public static Vector128<int> ConvertToSByteWithTruncatedSaturationAndZeroExtendToInt32(Vector128<float> value)  { throw new PlatformNotSupportedException(); }

        // VCVTTPS2IBS ymm1{k1}{z}, ymm2/m256/m32bcst {sae}
        public static Vector256<int> ConvertToSByteWithTruncatedSaturationAndZeroExtendToInt32(Vector256<float> value)  { throw new PlatformNotSupportedException(); }

        // VCVTTPS2IUBS xmm1{k1}{z}, xmm2/m128/m32bcst
        public static Vector128<int> ConvertToByteWithTruncatedSaturationAndZeroExtendToInt32(Vector128<float> value)  { throw new PlatformNotSupportedException(); }

        // VCVTTPS2IUBS ymm1{k1}{z}, ymm2/m256/m32bcst {sae}
        public static Vector256<int> ConvertToByteWithTruncatedSaturationAndZeroExtendToInt32(Vector256<float> value)  { throw new PlatformNotSupportedException(); }

        // VMOVD xmm1, xmm2/m32
        public static Vector128<uint> ConvertToVector128UInt32(Vector128<uint> value)  { throw new PlatformNotSupportedException(); }

        // VMOVW xmm1, xmm2/m16
        public static Vector128<ushort> ConvertToVector128UInt16(Vector128<ushort> value)  { throw new PlatformNotSupportedException(); }

        //The below instructions are those where 
        //embedded rouding support have been added 
        //to the existing API

        // VCVTDQ2PS ymm1{k1}{z}, ymm2/m256/m32bcst {er}
        public static Vector256<float> ConvertToVector256Single(Vector256<int> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        // VCVTPD2DQ xmm1{k1}{z}, ymm2/m256/m64bcst {er}
        public static Vector128<int> ConvertToVector128Int32(Vector256<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        // VCVTPD2PS xmm1{k1}{z}, ymm2/m256/m64bcst {er}
        public static Vector128<float> ConvertToVector128Single(Vector256<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        // VCVTPD2QQ ymm1{k1}{z}, ymm2/m256/m64bcst {er}
        public static Vector256<long> ConvertToVector256Int64(Vector256<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        // VCVTPD2UDQ xmm1{k1}{z}, ymm2/m256/m64bcst {er}
        public static Vector128<uint> ConvertToVector128UInt32(Vector256<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        // VCVTPD2UQQ ymm1{k1}{z}, ymm2/m256/m64bcst {er}
        public static Vector256<ulong> ConvertToVector256UInt64(Vector256<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        // VCVTPS2DQ ymm1{k1}{z}, ymm2/m256/m32bcst {er}
        public static Vector256<int> ConvertToVector256Int32(Vector256<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        // VCVTPS2QQ ymm1{k1}{z}, xmm2/m128/m32bcst {er}
        public static Vector256<long> ConvertToVector256Int64(Vector128<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        // VCVTPS2UDQ ymm1{k1}{z}, ymm2/m256/m32bcst {er}
        public static Vector256<uint> ConvertToVector256UInt32(Vector256<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        // VCVTPS2UQQ ymm1{k1}{z}, xmm2/m128/m32bcst {er}
        public static Vector256<ulong> ConvertToVector256UInt64(Vector128<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        // VCVTQQ2PS xmm1{k1}{z}, ymm2/m256/m64bcst {er}
        public static Vector128<float> ConvertToVector128Single(Vector256<ulong> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        // VCVTQQ2PD ymm1{k1}{z}, ymm2/m256/m64bcst {er}
        public static Vector256<double> ConvertToVector256Double(Vector256<ulong> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        // VCVTUDQ2PS ymm1{k1}{z}, ymm2/m256/m32bcst {er}
        public static Vector256<float> ConvertToVector256Single(Vector256<uint> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        // VCVTUQQ2PS xmm1{k1}{z}, ymm2/m256/m64bcst {er}
        public static Vector128<float> ConvertToVector128Single(Vector256<long> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        // VCVTUQQ2PD ymm1{k1}{z}, ymm2/m256/m64bcst {er}
        public static Vector256<double> ConvertToVector256Double(Vector256<long> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        // VMULPD ymm1{k1}{z}, ymm2, ymm3/m256/m64bcst {er}
        public static Vector256<double> Multiply(Vector256<double> left, Vector256<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        // VMULPS ymm1{k1}{z}, ymm2, ymm3/m256/m32bcst {er}
        public static Vector256<float> Multiply(Vector256<float> left, Vector256<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        // VSCALEFPD ymm1{k1}{z}, ymm2, ymm3/m256/m64bcst {er}
        public static Vector256<double> Scale(Vector256<double> left, Vector256<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        // VSCALEFPS ymm1{k1}{z}, ymm2, ymm3/m256/m32bcst {er}
        public static Vector256<float> Scale(Vector256<float> left, Vector256<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        // VSQRTPD ymm1{k1}{z}, ymm2/m256/m64bcst {er}
        public static Vector256<double> Sqrt(Vector256<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        // VSQRTPS ymm1{k1}{z}, ymm2/m256/m32bcst {er}
        public static Vector256<float> Sqrt(Vector256<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        // VSUBPD ymm1{k1}{z}, ymm2, ymm3/m256/m64bcst {er}
        public static Vector256<double> Subtract(Vector256<double> left, Vector256<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        // VSUBPS ymm1{k1}{z}, ymm2, ymm3/m256/m32bcst {er}
        public static Vector256<float> Subtract(Vector256<float> left, Vector256<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

        /// <summary>Provides access to the x86 AVX10.2 hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        public new abstract class X64 : Avx10v1.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { [Intrinsic] get { return false; } }

        }

        /// <summary>Provides access to the x86 AVX10.2/512 hardware instructions via intrinsics.</summary>
        public abstract class V512 : Avx10v1.V512
        {
            internal V512() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { [Intrinsic] get { return false; } }

            // VMINMAXPD zmm1{k1}{z}, zmm2, zmm3/m512/m64bcst {sae}, imm8
            public static Vector512<double> MinMax(Vector512<double> left, Vector512<double> right, [ConstantExpected] byte control)  { throw new PlatformNotSupportedException(); }

            // VMINMAXPS zmm1{k1}{z}, zmm2, zmm3/m512/m32bcst {sae}, imm8
            public static Vector512<float> MinMax(Vector512<float> left, Vector512<float> right, [ConstantExpected] byte control)  { throw new PlatformNotSupportedException(); }

            // VCVTPS2IBS zmm1{k1}{z}, zmm2/m512/m32bcst {er}
            public static Vector512<int> ConvertToSByteWithSaturationAndZeroExtendToInt32(Vector512<float> value)  { throw new PlatformNotSupportedException(); }

            // VCVTPS2IBS zmm1{k1}{z}, zmm2/m512/m32bcst {er}
            public static Vector512<int> ConvertToSByteWithSaturationAndZeroExtendToInt32(Vector512<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

            // VCVTPS2IUBS zmm1{k1}{z}, zmm2/m512/m32bcst {er}
            public static Vector512<int> ConvertToByteWithSaturationAndZeroExtendToInt32(Vector512<float> value)  { throw new PlatformNotSupportedException(); }

            // VCVTPS2IUBS zmm1{k1}{z}, zmm2/m512/m32bcst {er}
            public static Vector512<int> ConvertToByteWithSaturationAndZeroExtendToInt32(Vector512<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode)  { throw new PlatformNotSupportedException(); }

            // VCVTTPS2IUBS zmm1{k1}{z}, zmm2/m512/m32bcst {sae}
            public static Vector512<int> ConvertToSByteWithTruncatedSaturationAndZeroExtendToInt32(Vector512<float> value)  { throw new PlatformNotSupportedException(); }

            // VCVTTPS2IUBS zmm1{k1}{z}, zmm2/m512/m32bcst {sae}
            public static Vector512<int> ConvertToByteWithTruncatedSaturationAndZeroExtendToInt32(Vector512<float> value)  { throw new PlatformNotSupportedException(); }

            // This is a 512 extension of previously existing 128/26 inrinsic
            // VMPSADBW zmm1{k1}{z}, zmm2, zmm3/m512, imm8
            public static Vector512<ushort> MultipleSumAbsoluteDifferences(Vector512<byte> left, Vector512<byte> right, [ConstantExpected] byte mask)  { throw new PlatformNotSupportedException(); }

            /// <summary>Provides access to the x86 AVX10.1/512 hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
            [Intrinsic]
            public new abstract class X64 : Avx10v1.V512.X64
            {
                internal X64() { }

                /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
                /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
                /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
                public static new bool IsSupported { [Intrinsic] get { return false; } }
            }
        }
    }
}
