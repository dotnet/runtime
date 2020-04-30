// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.CompilerServices;

namespace System.Numerics
{
    internal static class VectorMath
    {
        public static Vector128<float> Lerp(Vector128<float> a, Vector128<float> b, Vector128<float> t)
        {
            Debug.Assert(Sse.IsSupported);
            return Sse.Add(a, Sse.Multiply(Sse.Subtract(b, a), t));
        }

        public static bool Equal(Vector128<float> vector1, Vector128<float> vector2)
        {
            Debug.Assert(Sse.IsSupported);
            return Sse.MoveMask(Sse.CompareNotEqual(vector1, vector2)) == 0;
        }

        public static bool NotEqual(Vector128<float> vector1, Vector128<float> vector2)
        {
            Debug.Assert(Sse.IsSupported);
            return Sse.MoveMask(Sse.CompareNotEqual(vector1, vector2)) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> ConditionalSelectBitwise(Vector128<float> selector, Vector128<float> ifTrue, Vector128<float> ifFalse)
        {
            Debug.Assert(Sse.IsSupported || AdvSimd.IsSupported);

            if (Sse.IsSupported)
            {
                return Sse.Or(
                            Sse.And(ifTrue, selector),
                            Sse.AndNot(selector, ifFalse)
                        );
            }
            else if (AdvSimd.IsSupported)
            {
                return AdvSimd.BitwiseSelect(selector.AsByte(), ifTrue.AsByte(), ifFalse.AsByte()).As<byte, float>();
            }

            return default;
        }

        public static Vector128<double> ConditionalSelectBitwise(Vector128<double> selector, Vector128<double> ifTrue, Vector128<double> ifFalse)
        {
            Debug.Assert(Sse.IsSupported || AdvSimd.IsSupported);

            if (Sse2.IsSupported)
            {
                return Sse2.Or(
                            Sse2.And(ifTrue, selector),
                            Sse2.AndNot(selector, ifFalse)
                        );
            }
            else if (Sse.IsSupported)
            {
                return Sse.Or(
                            Sse.And(ifTrue.AsSingle(), selector.AsSingle()),
                            Sse.AndNot(selector.AsSingle(), ifFalse.AsSingle())
                        ).As<float, double>();
            }
            else if (AdvSimd.IsSupported)
            {
                return AdvSimd.BitwiseSelect(selector.AsByte(), ifTrue.AsByte(), ifFalse.AsByte()).As<byte, double>();
            }

            return default;
        }
    }
}
