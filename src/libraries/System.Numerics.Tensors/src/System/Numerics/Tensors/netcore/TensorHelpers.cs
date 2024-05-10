// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.VisualBasic;

namespace System.Numerics.Tensors
{
    internal static class TensorHelpers
    {

        /// <summary>
        /// Counts the number of true elements in a boolean filter tensor so we know how much space we will need.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns>How many boolean values are true.</returns>
        public static nint CountTrueElements(Tensor<bool> filter)
        {
            Span<bool> filterSpan = MemoryMarshal.CreateSpan(ref filter._values[0], (int)filter._flattenedLength);
            nint count = 0;
            for (int i = 0; i < filterSpan.Length; i++)
            {
                if (filterSpan[i])
                    count++;
            }

            return count;
        }

        internal static bool AreShapesBroadcastCompatible<T>(Tensor<T> tensor1, Tensor<T> tensor2)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool> => AreShapesBroadcastCompatible(tensor1.Lengths, tensor2.Lengths);

        internal static bool AreShapesBroadcastCompatible(ReadOnlySpan<nint> shape1, ReadOnlySpan<nint> shape2)
        {
            int shape1Index = shape1.Length - 1;
            int shape2Index = shape2.Length - 1;

            bool areCompatible = true;

            nint s1;
            nint s2;

            while (shape1Index >= 0 || shape2Index >= 0)
            {
                // if a dimension is missing in one of the shapes, it is considered to be 1
                if (shape1Index < 0)
                    s1 = 1;
                else
                    s1 = shape1[shape1Index--];

                if (shape2Index < 0)
                    s2 = 1;
                else
                    s2 = shape2[shape2Index--];

                if (s1 == s2 || (s1 == 1 && s2 != 1) || (s2 == 1 && s1 != 1)) { }
                else
                {
                    areCompatible = false;
                    break;
                }
            }

            return areCompatible;
        }

        internal static nint[] GetSmallestBroadcastableSize(ReadOnlySpan<nint> shape1, ReadOnlySpan<nint> shape2)
        {
            if (!AreShapesBroadcastCompatible(shape1, shape2))
                throw new Exception("Shapes are not broadcast compatible");

            nint[] intermediateShape = GetIntermediateShape(shape1, shape2.Length);
            for (int i = 1; i <= shape1.Length; i++)
            {
                intermediateShape[^i] = Math.Max(intermediateShape[^i], shape1[^i]);
            }
            for (int i = 1; i <= shape2.Length; i++)
            {
                intermediateShape[^i] = Math.Max(intermediateShape[^i], shape2[^i]);
            }

            return intermediateShape;
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

        internal static bool IsUnderlyingStorageSameSize<T>(Tensor<T> tensor1, Tensor<T> tensor2)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool> => tensor1.Lengths.Length == tensor2.Lengths.Length;

        internal static bool AreShapesTheSame<T>(Tensor<T> tensor1, Tensor<T> tensor2)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool> => tensor1._lengths.SequenceEqual(tensor2._lengths);


        internal static void PermuteIndices(Span<nint> indices, Span<nint> permutedIndices, ReadOnlySpan<int> permutation)
        {
            for (int i = 0; i < indices.Length; i++)
            {
                permutedIndices[i] = indices[permutation[i]];
            }
        }
    }
}
