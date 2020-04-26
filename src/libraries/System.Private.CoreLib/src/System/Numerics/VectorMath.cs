// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

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
        public static Vector128<T> ConditionalSelectBitwise<T>(Vector128<T> selector, Vector128<T> ifTrue, Vector128<T> ifFalse)
            where T : struct
        {
            Debug.Assert(Sse.IsSupported || AdvSimd.IsSupported);

            if (Sse.IsSupported)
            {
                Vector128<float> trueMask = Sse.And(ifTrue.AsSingle(), selector.AsSingle());
                Vector128<float> falseMask  = Sse.AndNot(selector.AsSingle(), ifFalse.AsSingle());

                return Sse.Or(falseMask, trueMask).As<float, T>();
            }
            else if (AdvSimd.IsSupported)
            {
                return AdvSimd.BitwiseSelect(selector.AsByte(), ifTrue.AsByte(), ifFalse.AsByte()).As<byte, T>();
            }

            return default;
        }
    }
}
