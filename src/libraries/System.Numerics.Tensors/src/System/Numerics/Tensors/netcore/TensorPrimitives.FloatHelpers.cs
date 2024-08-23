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

        private static (Vector128<float> First, Vector128<float> Second) Apply2xScalar<TOperator>(Vector128<float> floats)
            where TOperator : IUnaryInputBinaryOutput<float>
        {
            (float firstRes0, float secondRes0) = TOperator.Invoke(floats[0]);
            (float firstRes1, float secondRes1) = TOperator.Invoke(floats[1]);
            (float firstRes2, float secondRes2) = TOperator.Invoke(floats[2]);
            (float firstRes3, float secondRes3) = TOperator.Invoke(floats[3]);

            return (
                Vector128.Create(firstRes0, firstRes1, firstRes2, firstRes3),
                Vector128.Create(secondRes0, secondRes1, secondRes2, secondRes3)
            );
        }

        private static (Vector256<float> First, Vector256<float> Second) Apply2xScalar<TOperator>(Vector256<float> floats)
            where TOperator : IUnaryInputBinaryOutput<float>
        {
            (Vector128<float> firstLower, Vector128<float> secondLower) = Apply2xScalar<TOperator>(floats.GetLower());
            (Vector128<float> firstUpper, Vector128<float> secondUpper) = Apply2xScalar<TOperator>(floats.GetUpper());

            return (
                Vector256.Create(firstLower, firstUpper),
                Vector256.Create(secondLower, secondUpper)
            );
        }

        private static (Vector512<float> First, Vector512<float> Second) Apply2xScalar<TOperator>(Vector512<float> floats)
            where TOperator : IUnaryInputBinaryOutput<float>
        {
            (Vector256<float> firstLower, Vector256<float> secondLower) = Apply2xScalar<TOperator>(floats.GetLower());
            (Vector256<float> firstUpper, Vector256<float> secondUpper) = Apply2xScalar<TOperator>(floats.GetUpper());

            return (
                Vector512.Create(firstLower, firstUpper),
                Vector512.Create(secondLower, secondUpper)
            );
        }

        private static (Vector128<double> First, Vector128<double> Second) Apply2xScalar<TOperator>(Vector128<double> doubles)
            where TOperator : IUnaryInputBinaryOutput<double>
        {
            (double firstRes0, double secondRes0) = TOperator.Invoke(doubles[0]);
            (double firstRes1, double secondRes1) = TOperator.Invoke(doubles[1]);

            return (
                Vector128.Create(firstRes0, firstRes1),
                Vector128.Create(secondRes0, secondRes1)
            );
        }

        private static (Vector256<double> First, Vector256<double> Second) Apply2xScalar<TOperator>(Vector256<double> doubles)
            where TOperator : IUnaryInputBinaryOutput<double>
        {
            (Vector128<double> firstLower, Vector128<double> secondLower) = Apply2xScalar<TOperator>(doubles.GetLower());
            (Vector128<double> firstUpper, Vector128<double> secondUpper) = Apply2xScalar<TOperator>(doubles.GetUpper());

            return (
                Vector256.Create(firstLower, firstUpper),
                Vector256.Create(secondLower, secondUpper)
            );
        }

        private static (Vector512<double> First, Vector512<double> Second) Apply2xScalar<TOperator>(Vector512<double> doubles)
            where TOperator : IUnaryInputBinaryOutput<double>
        {
            (Vector256<double> firstLower, Vector256<double> secondLower) = Apply2xScalar<TOperator>(doubles.GetLower());
            (Vector256<double> firstUpper, Vector256<double> secondUpper) = Apply2xScalar<TOperator>(doubles.GetUpper());

            return (
                Vector512.Create(firstLower, firstUpper),
                Vector512.Create(secondLower, secondUpper)
            );
        }
    }
}
