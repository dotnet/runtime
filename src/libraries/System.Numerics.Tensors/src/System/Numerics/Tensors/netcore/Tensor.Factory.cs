// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic;

#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace System.Numerics.Tensors
{
    public static partial class Tensor
    {
        /// <summary>
        /// Creates a <see cref="Tensor{T}"/> and initializes it with the default value of T. If <paramref name="pinned"/> is true, the memory will be pinned.
        /// </summary>
        /// <param name="lengths">A <see cref="ReadOnlySpan{T}"/> indicating the lengths of each dimension.</param>
        /// <param name="pinned">A <see cref="bool"/> whether the underlying data should be pinned or not.</param>
        public static Tensor<T> Create<T>(scoped ReadOnlySpan<nint> lengths, bool pinned = false)
        {
            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);
            T[] values = pinned ? GC.AllocateArray<T>((int)linearLength, pinned) : (new T[linearLength]);
            return Create(values, lengths, [], pinned);
        }

        /// <summary>
        /// Creates a <see cref="Tensor{T}"/> and initializes it with the default value of T. If <paramref name="pinned"/> is true, the memory will be pinned.
        /// </summary>
        /// <param name="lengths">A <see cref="ReadOnlySpan{T}"/> indicating the lengths of each dimension.</param>
        /// <param name="strides">A <see cref="ReadOnlySpan{T}"/> indicating the strides of each dimension.</param>
        /// <param name="pinned">A <see cref="bool"/> whether the underlying data should be pinned or not.</param>
        public static Tensor<T> Create<T>(scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, bool pinned = false)
        {
            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);
            T[] values = pinned ? GC.AllocateArray<T>((int)linearLength, pinned) : (new T[linearLength]);
            return Create(values, lengths, strides, pinned);
        }

        /// <summary>
        /// Creates a <see cref="Tensor{T}"/> from the provided <paramref name="values"/>. If the product of the
        /// <paramref name="lengths"/> does not equal the length of the <paramref name="values"/> array, an exception will be thrown.
        /// </summary>
        /// <param name="values">An array of the backing memory.</param>
        /// <param name="lengths">A <see cref="ReadOnlySpan{T}"/> indicating the lengths of each dimension.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static Tensor<T> Create<T>(T[] values, scoped ReadOnlySpan<nint> lengths)
            => Create(values, lengths, []);

        /// <summary>
        /// Creates a <see cref="Tensor{T}"/> from the provided <paramref name="values"/>. If the product of the
        /// <paramref name="lengths"/> does not equal the length of the <paramref name="values"/> array, an exception will be thrown.
        /// </summary>
        /// <param name="values">An array of the backing memory.</param>
        /// <param name="lengths">A <see cref="ReadOnlySpan{T}"/> indicating the lengths of each dimension.</param>
        /// <param name="strides">A <see cref="ReadOnlySpan{T}"/> indicating the strides of each dimension.</param>
        /// <param name="isPinned">A <see cref="bool"/> indicating whether the <paramref name="values"/> were pinned or not.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static Tensor<T> Create<T>(T[] values, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, bool isPinned = false)
        {
            return new Tensor<T>(values, lengths, strides, isPinned);
        }

        /// <summary>
        /// Creates a <see cref="Tensor{T}"/> and initializes it with the data from <paramref name="data"/>.
        /// </summary>
        /// <param name="data">A <see cref="IEnumerable{T}"/> with the data to use for the initialization.</param>
        /// <param name="lengths"></param>
        public static Tensor<T> Create<T>(IEnumerable<T> data, scoped ReadOnlySpan<nint> lengths)
        {
            T[] values = data.ToArray();
            return new Tensor<T>(values, lengths.IsEmpty ? [values.Length] : lengths, false);
        }

        /// <summary>
        /// Creates a <see cref="Tensor{T}"/> and initializes it with the data from <paramref name="data"/>.
        /// </summary>
        /// <param name="data">A <see cref="IEnumerable{T}"/> with the data to use for the initialization.</param>
        /// <param name="lengths"></param>
        /// <param name="strides"></param>
        /// <param name="isPinned"></param>
        public static Tensor<T> Create<T>(IEnumerable<T> data, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, bool isPinned = false)
        {
            T[] values = data.ToArray();
            return new Tensor<T>(values, lengths.IsEmpty ? [values.Length] : lengths, strides, isPinned);
        }

        /// <summary>
        /// Creates a <see cref="Tensor{T}"/> and initializes it with random data uniformly distributed.
        /// </summary>
        /// <param name="lengths">A <see cref="ReadOnlySpan{T}"/> indicating the lengths of each dimension.</param>
        public static Tensor<T> CreateAndFillUniformDistribution<T>(params scoped ReadOnlySpan<nint> lengths)
            where T : IFloatingPoint<T>
        {
            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);
            T[] values = new T[linearLength];
            Random rand = Random.Shared;
            for (int i = 0; i < values.Length; i++)
                values[i] = T.CreateChecked(rand.NextDouble());

            return new Tensor<T>(values, lengths, false);
        }

        #region Normal
        /// <summary>
        /// Creates a <see cref="Tensor{T}"/> and initializes it with random data in a gaussian normal distribution.
        /// </summary>
        /// <param name="lengths">A <see cref="ReadOnlySpan{T}"/> indicating the lengths of each dimension.</param>
        public static Tensor<T> CreateAndFillGaussianNormalDistribution<T>(params scoped ReadOnlySpan<nint> lengths)
            where T : IFloatingPoint<T>
        {
            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);
            T[] values = new T[linearLength];
            GaussianDistribution<T>(values, linearLength);
            return new Tensor<T>(values, lengths, false);
        }

        private static void GaussianDistribution<T>(in Span<T> values, nint linearLength)
             where T : IFloatingPoint<T>
        {
            Random rand = Random.Shared;
            for (int i = 0; i < linearLength; i++)
            {
                double u1 = 1.0 - rand.NextDouble();
                double u2 = 1.0 - rand.NextDouble();
                values[i] = T.CreateChecked(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2));
            }
        }
        #endregion

        /// <summary>
        /// Creates a <see cref="Tensor{T}"/> and does not initialize it. If <paramref name="pinned"/> is true, the memory will be pinned.
        /// </summary>
        /// <param name="lengths">A <see cref="ReadOnlySpan{T}"/> indicating the lengths of each dimension.</param>
        /// <param name="pinned">A <see cref="bool"/> whether the underlying data should be pinned or not.</param>
        public static Tensor<T> CreateUninitialized<T>(scoped ReadOnlySpan<nint> lengths, bool pinned = false)
            => CreateUninitialized<T>(lengths, [], pinned);

        /// <summary>
        /// Creates a <see cref="Tensor{T}"/> and does not initialize it. If <paramref name="pinned"/> is true, the memory will be pinned.
        /// </summary>
        /// <param name="lengths">A <see cref="ReadOnlySpan{T}"/> indicating the lengths of each dimension.</param>
        /// <param name="strides">A <see cref="ReadOnlySpan{T}"/> indicating the strides of each dimension.</param>
        /// <param name="pinned">A <see cref="bool"/> whether the underlying data should be pinned or not.</param>
        public static Tensor<T> CreateUninitialized<T>(scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, bool pinned = false)
        {
            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);
            T[] values = GC.AllocateUninitializedArray<T>((int)linearLength, pinned);
            return new Tensor<T>(values, lengths, strides, pinned);
        }

        public static ref readonly TensorSpan<T> FillGaussianNormalDistribution<T>(in TensorSpan<T> destination) where T : IFloatingPoint<T>
        {
            Span<T> span = MemoryMarshal.CreateSpan<T>(ref destination._reference, (int)destination._shape._memoryLength);

            GaussianDistribution<T>(span, destination._shape._memoryLength);

            return ref destination;
        }

        public static ref readonly TensorSpan<T> FillUniformDistribution<T>(in TensorSpan<T> destination) where T : IFloatingPoint<T>
        {
            Span<T> span = MemoryMarshal.CreateSpan<T>(ref destination._reference, (int)destination._shape._memoryLength);

            for (int i = 0; i < span.Length; i++)
                span[i] = T.CreateChecked(Random.Shared.NextDouble());

            return ref destination;
        }
    }
}
