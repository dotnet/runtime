// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        private static Vector128<float> ApplyScalar<TOperator>(Vector128<float> floats) where TOperator : IUnaryOperator<float, float> =>
            Vector128.Create(TOperator.Invoke(floats[0]), TOperator.Invoke(floats[1]), TOperator.Invoke(floats[2]), TOperator.Invoke(floats[3]));

        private static Vector256<float> ApplyScalar<TOperator>(Vector256<float> floats) where TOperator : IUnaryOperator<float, float> =>
            Vector256.Create(ApplyScalar<TOperator>(floats.GetLower()), ApplyScalar<TOperator>(floats.GetUpper()));

        private static Vector512<float> ApplyScalar<TOperator>(Vector512<float> floats) where TOperator : IUnaryOperator<float, float> =>
            Vector512.Create(ApplyScalar<TOperator>(floats.GetLower()), ApplyScalar<TOperator>(floats.GetUpper()));

        private static Vector128<double> ApplyScalar<TOperator>(Vector128<double> doubles) where TOperator : IUnaryOperator<double, double> =>
            Vector128.Create(TOperator.Invoke(doubles[0]), TOperator.Invoke(doubles[1]));

        private static Vector256<double> ApplyScalar<TOperator>(Vector256<double> doubles) where TOperator : IUnaryOperator<double, double> =>
            Vector256.Create(ApplyScalar<TOperator>(doubles.GetLower()), ApplyScalar<TOperator>(doubles.GetUpper()));

        private static Vector512<double> ApplyScalar<TOperator>(Vector512<double> doubles) where TOperator : IUnaryOperator<double, double> =>
            Vector512.Create(ApplyScalar<TOperator>(doubles.GetLower()), ApplyScalar<TOperator>(doubles.GetUpper()));
    }
}
