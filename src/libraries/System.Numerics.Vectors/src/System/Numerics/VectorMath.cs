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
    }
}
