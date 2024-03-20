// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace System.Numerics.Tensors
{
    public static unsafe partial class TensorPrimitives
    {
        private interface IIndexOfOperator<T>
        {
            static abstract int Invoke(ref T result, T current, int resultIndex, int currentIndex);
            static abstract void Invoke(ref Vector128<T> result, Vector128<T> current, ref Vector128<T> resultIndex, Vector128<T> currentIndex);
            static abstract void Invoke(ref Vector256<T> result, Vector256<T> current, ref Vector256<T> resultIndex, Vector256<T> currentIndex);
            static abstract void Invoke(ref Vector512<T> result, Vector512<T> current, ref Vector512<T> resultIndex, Vector512<T> currentIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int IndexOfFinalAggregate<T, TIndexOfOperator>(Vector128<T> result, Vector128<T> resultIndex)
            where TIndexOfOperator : struct, IIndexOfOperator<T>
        {
            Vector128<T> tmpResult;
            Vector128<T> tmpIndex;

            if (sizeof(T) == 8)
            {
                // Compare 0 with 1
                tmpResult = Vector128.Shuffle(result.AsInt64(), Vector128.Create(1, 0)).As<long, T>();
                tmpIndex = Vector128.Shuffle(resultIndex.AsInt64(), Vector128.Create(1, 0)).As<long, T>();
                TIndexOfOperator.Invoke(ref result, tmpResult, ref resultIndex, tmpIndex);

                // Return 0
                return (int)resultIndex.As<T, long>().ToScalar();
            }

            if (sizeof(T) == 4)
            {
                // Compare 0,1 with 2,3
                tmpResult = Vector128.Shuffle(result.AsInt32(), Vector128.Create(2, 3, 0, 1)).As<int, T>();
                tmpIndex = Vector128.Shuffle(resultIndex.AsInt32(), Vector128.Create(2, 3, 0, 1)).As<int, T>();
                TIndexOfOperator.Invoke(ref result, tmpResult, ref resultIndex, tmpIndex);

                // Compare 0 with 1
                tmpResult = Vector128.Shuffle(result.AsInt32(), Vector128.Create(1, 0, 3, 2)).As<int, T>();
                tmpIndex = Vector128.Shuffle(resultIndex.AsInt32(), Vector128.Create(1, 0, 3, 2)).As<int, T>();
                TIndexOfOperator.Invoke(ref result, tmpResult, ref resultIndex, tmpIndex);

                // Return 0
                return resultIndex.As<T, int>().ToScalar();
            }

            if (sizeof(T) == 2)
            {
                // Compare 0,1,2,3 with 4,5,6,7
                tmpResult = Vector128.Shuffle(result.AsInt16(), Vector128.Create(4, 5, 6, 7, 0, 1, 2, 3)).As<short, T>();
                tmpIndex = Vector128.Shuffle(resultIndex.AsInt16(), Vector128.Create(4, 5, 6, 7, 0, 1, 2, 3)).As<short, T>();
                TIndexOfOperator.Invoke(ref result, tmpResult, ref resultIndex, tmpIndex);

                // Compare 0,1 with 2,3
                tmpResult = Vector128.Shuffle(result.AsInt16(), Vector128.Create(2, 3, 0, 1, 4, 5, 6, 7)).As<short, T>();
                tmpIndex = Vector128.Shuffle(resultIndex.AsInt16(), Vector128.Create(2, 3, 0, 1, 4, 5, 6, 7)).As<short, T>();
                TIndexOfOperator.Invoke(ref result, tmpResult, ref resultIndex, tmpIndex);

                // Compare 0 with 1
                tmpResult = Vector128.Shuffle(result.AsInt16(), Vector128.Create(1, 0, 2, 3, 4, 5, 6, 7)).As<short, T>();
                tmpIndex = Vector128.Shuffle(resultIndex.AsInt16(), Vector128.Create(1, 0, 2, 3, 4, 5, 6, 7)).As<short, T>();
                TIndexOfOperator.Invoke(ref result, tmpResult, ref resultIndex, tmpIndex);

                // Return 0
                return resultIndex.As<T, short>().ToScalar();
            }

            Debug.Assert(sizeof(T) == 1);
            {
                // Compare 0,1,2,3,4,5,6,7 with 8,9,10,11,12,13,14,15
                tmpResult = Vector128.Shuffle(result.AsByte(), Vector128.Create((byte)8, 9, 10, 11, 12, 13, 14, 15, 0, 1, 2, 3, 4, 5, 6, 7)).As<byte, T>();
                tmpIndex = Vector128.Shuffle(resultIndex.AsByte(), Vector128.Create((byte)8, 9, 10, 11, 12, 13, 14, 15, 0, 1, 2, 3, 4, 5, 6, 7)).As<byte, T>();
                TIndexOfOperator.Invoke(ref result, tmpResult, ref resultIndex, tmpIndex);

                // Compare 0,1,2,3 with 4,5,6,7
                tmpResult = Vector128.Shuffle(result.AsByte(), Vector128.Create((byte)4, 5, 6, 7, 0, 1, 2, 3, 8, 9, 10, 11, 12, 13, 14, 15)).As<byte, T>();
                tmpIndex = Vector128.Shuffle(resultIndex.AsByte(), Vector128.Create((byte)4, 5, 6, 7, 0, 1, 2, 3, 8, 9, 10, 11, 12, 13, 14, 15)).As<byte, T>();
                TIndexOfOperator.Invoke(ref result, tmpResult, ref resultIndex, tmpIndex);

                // Compare 0,1 with 2,3
                tmpResult = Vector128.Shuffle(result.AsByte(), Vector128.Create((byte)2, 3, 0, 1, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15)).As<byte, T>();
                tmpIndex = Vector128.Shuffle(resultIndex.AsByte(), Vector128.Create((byte)2, 3, 0, 1, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15)).As<byte, T>();
                TIndexOfOperator.Invoke(ref result, tmpResult, ref resultIndex, tmpIndex);

                // Compare 0 with 1
                tmpResult = Vector128.Shuffle(result.AsByte(), Vector128.Create((byte)1, 0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15)).As<byte, T>();
                tmpIndex = Vector128.Shuffle(resultIndex.AsByte(), Vector128.Create((byte)1, 0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15)).As<byte, T>();
                TIndexOfOperator.Invoke(ref result, tmpResult, ref resultIndex, tmpIndex);

                // Return 0
                return resultIndex.As<T, byte>().ToScalar();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int IndexOfFinalAggregate<T, TIndexOfOperator>(Vector256<T> result, Vector256<T> resultIndex)
            where TIndexOfOperator : struct, IIndexOfOperator<T>
        {
            // Min the upper/lower halves of the Vector256
            Vector128<T> resultLower = result.GetLower();
            Vector128<T> indexLower = resultIndex.GetLower();

            TIndexOfOperator.Invoke(ref resultLower, result.GetUpper(), ref indexLower, resultIndex.GetUpper());
            return IndexOfFinalAggregate<T, TIndexOfOperator>(resultLower, indexLower);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int IndexOfFinalAggregate<T, TIndexOfOperator>(Vector512<T> result, Vector512<T> resultIndex)
            where TIndexOfOperator : struct, IIndexOfOperator<T>
        {
            Vector256<T> resultLower = result.GetLower();
            Vector256<T> indexLower = resultIndex.GetLower();

            TIndexOfOperator.Invoke(ref resultLower, result.GetUpper(), ref indexLower, resultIndex.GetUpper());
            return IndexOfFinalAggregate<T, TIndexOfOperator>(resultLower, indexLower);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<T> IndexLessThan<T>(Vector128<T> indices1, Vector128<T> indices2) =>
            sizeof(T) == sizeof(long) ? Vector128.LessThan(indices1.AsInt64(), indices2.AsInt64()).As<long, T>() :
            sizeof(T) == sizeof(int) ? Vector128.LessThan(indices1.AsInt32(), indices2.AsInt32()).As<int, T>() :
            sizeof(T) == sizeof(short) ? Vector128.LessThan(indices1.AsInt16(), indices2.AsInt16()).As<short, T>() :
            Vector128.LessThan(indices1.AsByte(), indices2.AsByte()).As<byte, T>();
    }
}
