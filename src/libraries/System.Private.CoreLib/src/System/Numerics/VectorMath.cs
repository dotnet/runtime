// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace System.Numerics
{
    internal static class VectorMath
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> ConditionalSelectBitwise(Vector128<float> selector, Vector128<float> ifTrue, Vector128<float> ifFalse)
        {
            // This implementation is based on the DirectX Math Library XMVector4NotEqual method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathVector.inl

            if (AdvSimd.IsSupported)
            {
                return AdvSimd.BitwiseSelect(selector, ifTrue, ifFalse);
            }
            else if (Sse.IsSupported)
            {
                return Sse.Or(Sse.And(ifTrue, selector), Sse.AndNot(selector, ifFalse));
            }
            else
            {
                // Redundant test so we won't prejit remainder of this method on platforms without AdvSimd.
                throw new PlatformNotSupportedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<double> ConditionalSelectBitwise(Vector128<double> selector, Vector128<double> ifTrue, Vector128<double> ifFalse)
        {
            // This implementation is based on the DirectX Math Library XMVector4NotEqual method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathVector.inl

            if (AdvSimd.IsSupported)
            {
                return AdvSimd.BitwiseSelect(selector, ifTrue, ifFalse);
            }
            else if (Sse2.IsSupported)
            {
                return Sse2.Or(Sse2.And(ifTrue, selector), Sse2.AndNot(selector, ifFalse));
            }
            else
            {
                // Redundant test so we won't prejit remainder of this method on platforms without AdvSimd.
                throw new PlatformNotSupportedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equal(Vector128<float> vector1, Vector128<float> vector2)
        {
            // This implementation is based on the DirectX Math Library XMVector4Equal method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathVector.inl

            if (AdvSimd.Arm64.IsSupported)
            {
                Vector128<uint> vResult = AdvSimd.CompareEqual(vector1, vector2).AsUInt32();

                Vector64<byte> vResult0 = vResult.GetLower().AsByte();
                Vector64<byte> vResult1 = vResult.GetUpper().AsByte();

                Vector64<byte> vTemp10 = AdvSimd.Arm64.ZipLow(vResult0, vResult1);
                Vector64<byte> vTemp11 = AdvSimd.Arm64.ZipHigh(vResult0, vResult1);

                Vector64<ushort> vTemp21 = AdvSimd.Arm64.ZipHigh(vTemp10.AsUInt16(), vTemp11.AsUInt16());
                return vTemp21.AsUInt32().GetElement(1) == 0xFFFFFFFF;
            }
            else if (Sse.IsSupported)
            {
                return Sse.MoveMask(Sse.CompareNotEqual(vector1, vector2)) == 0;
            }
            else
            {
                // Redundant test so we won't prejit remainder of this method on platforms without AdvSimd.
                throw new PlatformNotSupportedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> Lerp(Vector128<float> a, Vector128<float> b, Vector128<float> t)
        {
            // This implementation is based on the DirectX Math Library XMVectorLerp method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathVector.inl

            if (AdvSimd.IsSupported)
            {
                return AdvSimd.FusedMultiplyAdd(a, AdvSimd.Subtract(b, a), t);
            }
            else if (Fma.IsSupported)
            {
                return Fma.MultiplyAdd(Sse.Subtract(b, a), t, a);
            }
            else if (Sse.IsSupported)
            {
                return Sse.Add(Sse.Multiply(a, Sse.Subtract(Vector128.Create(1.0f), t)), Sse.Multiply(b, t));
            }
            else
            {
                // Redundant test so we won't prejit remainder of this method on platforms without AdvSimd.
                throw new PlatformNotSupportedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NotEqual(Vector128<float> vector1, Vector128<float> vector2)
        {
            // This implementation is based on the DirectX Math Library XMVector4NotEqual method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathVector.inl

            if (AdvSimd.Arm64.IsSupported)
            {
                Vector128<uint> vResult = AdvSimd.CompareEqual(vector1, vector2).AsUInt32();

                Vector64<byte> vResult0 = vResult.GetLower().AsByte();
                Vector64<byte> vResult1 = vResult.GetUpper().AsByte();

                Vector64<byte> vTemp10 = AdvSimd.Arm64.ZipLow(vResult0, vResult1);
                Vector64<byte> vTemp11 = AdvSimd.Arm64.ZipHigh(vResult0, vResult1);

                Vector64<ushort> vTemp21 = AdvSimd.Arm64.ZipHigh(vTemp10.AsUInt16(), vTemp11.AsUInt16());
                return vTemp21.AsUInt32().GetElement(1) != 0xFFFFFFFF;
            }
            else if (Sse.IsSupported)
            {
                return Sse.MoveMask(Sse.CompareNotEqual(vector1, vector2)) != 0;
            }
            else
            {
                // Redundant test so we won't prejit remainder of this method on platforms without AdvSimd.
                throw new PlatformNotSupportedException();
            }
        }
    }
}
