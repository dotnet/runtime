// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace System.Numerics.Tensors
{
    internal static class TensorHelpers
    {

        /// <summary>
        /// Counts the number of true elements in a boolean filter tensor so we know how much space we will need.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns>How many boolean values are true.</returns>
        public static nint CountTrueElements(scoped in ReadOnlyTensorSpan<bool> filter)
        {
            Span<bool> filterSpan = MemoryMarshal.CreateSpan(ref filter._reference, (int)filter._shape._memoryLength);
            nint count = 0;
            for (int i = 0; i < filterSpan.Length; i++)
            {
                if (filterSpan[i])
                    count++;
            }

            return count;
        }

        internal static bool IsBroadcastableTo<T>(Tensor<T> tensor1, Tensor<T> tensor2)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool> => IsBroadcastableTo(tensor1.Lengths, tensor2.Lengths);

        internal static bool IsBroadcastableTo(ReadOnlySpan<nint> lengths1, ReadOnlySpan<nint> lengths2)
        {
            int lengths1Index = lengths1.Length - 1;
            int lengths2Index = lengths2.Length - 1;

            bool areCompatible = true;

            nint s1;
            nint s2;

            while (lengths1Index >= 0 || lengths2Index >= 0)
            {
                // if a dimension is missing in one of the shapes, it is considered to be 1
                if (lengths1Index < 0)
                    s1 = 1;
                else
                    s1 = lengths1[lengths1Index--];

                if (lengths2Index < 0)
                    s2 = 1;
                else
                    s2 = lengths2[lengths2Index--];

                if (s1 == s2 || (s1 == 1 && s2 != 1) || (s2 == 1 && s1 != 1)) { }
                else
                {
                    areCompatible = false;
                    break;
                }
            }

            return areCompatible;
        }

        internal static nint[] GetIntermediateShape(ReadOnlySpan<nint> shape1, int shape2Length)
        {
            int shape1Index = shape1.Length - 1;
            int newShapeIndex = Math.Max(shape1.Length, shape2Length) - 1;
            nint[] newShape = new nint[Math.Max(shape1.Length, shape2Length)];

            while (newShapeIndex >= 0)
            {
                // if a dimension is missing in one of the shapes, it is considered to be 1
                if (shape1Index < 0)
                    newShape[newShapeIndex--] = 1;
                else
                    newShape[newShapeIndex--] = shape1[shape1Index--];
            }

            return newShape;
        }

        internal static bool IsUnderlyingStorageSameSize<T>(scoped in ReadOnlyTensorSpan<T> tensor1, scoped in ReadOnlyTensorSpan<T> tensor2)
            => tensor1._shape._memoryLength == tensor2._shape._memoryLength;

        internal static bool IsUnderlyingStorageSameSize<T>(Tensor<T> tensor1, Tensor<T> tensor2)
    => tensor1._values.Length == tensor2._values.Length;

        internal static bool AreLengthsTheSame<T>(scoped in ReadOnlyTensorSpan<T> tensor1, scoped in ReadOnlyTensorSpan<T> tensor2)
            => tensor1.Lengths.SequenceEqual(tensor2.Lengths);

        internal static bool AreLengthsTheSame(ReadOnlySpan<nint> lengths1, ReadOnlySpan<nint> lengths2)
            => lengths1.SequenceEqual(lengths2);

        internal static void PermuteIndices(Span<nint> indices, Span<nint> permutedIndices, ReadOnlySpan<int> permutation)
        {
            for (int i = 0; i < indices.Length; i++)
            {
                permutedIndices[i] = indices[permutation[i]];
            }
        }
    }
}
