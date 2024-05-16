// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable 8500 // address / sizeof of managed types

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
            where T : IEquatable<T>
        {
            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);
            T[] values = pinned ? GC.AllocateArray<T>((int)linearLength, pinned) : (new T[linearLength]);
            return new Tensor<T>(values, lengths, pinned);
        }

        /// <summary>
        /// Creates a <see cref="Tensor{T}"/> and initializes it with the default value of T. If <paramref name="pinned"/> is true, the memory will be pinned.
        /// </summary>
        /// <param name="lengths">A <see cref="ReadOnlySpan{T}"/> indicating the lengths of each dimension.</param>
        /// <param name="strides">A <see cref="ReadOnlySpan{T}"/> indicating the strides of each dimension.</param>
        /// <param name="pinned">A <see cref="bool"/> whether the underlying data should be pinned or not.</param>
        public static Tensor<T> Create<T>(scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, bool pinned = false)
            where T : IEquatable<T>
        {
            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);
            T[] values = pinned ? GC.AllocateArray<T>((int)linearLength, pinned) : (new T[linearLength]);
            return new Tensor<T>(values, lengths, strides, pinned);
        }

        /// <summary>
        /// Creates a <see cref="Tensor{T}"/> from the provided <paramref name="values"/>. If the product of the
        /// <paramref name="lengths"/> does not equal the length of the <paramref name="values"/> array, an exception will be thrown.
        /// </summary>
        /// <param name="values">An array of the backing memory.</param>
        /// <param name="lengths">A <see cref="ReadOnlySpan{T}"/> indicating the lengths of each dimension.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static Tensor<T> Create<T>(T[] values, scoped ReadOnlySpan<nint> lengths)
            where T : IEquatable<T>
        {
            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);
            if (linearLength != values.Length)
                ThrowHelper.ThrowArgument_LengthsMustEqualArrayLength();

            return new Tensor<T>(values, lengths, false);
        }

        /// <summary>
        /// Creates a <see cref="Tensor{T}"/> from the provided <paramref name="values"/>. If the product of the
        /// <paramref name="lengths"/> does not equal the length of the <paramref name="values"/> array, an exception will be thrown.
        /// </summary>
        /// <param name="values">An array of the backing memory.</param>
        /// <param name="lengths">A <see cref="ReadOnlySpan{T}"/> indicating the lengths of each dimension.</param>
        /// <param name="strides">A <see cref="ReadOnlySpan{T}"/> indicating the strides of each dimension.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static Tensor<T> Create<T>(T[] values, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
            where T : IEquatable<T>
        {
            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);
            if (linearLength != values.Length)
                ThrowHelper.ThrowArgument_LengthsMustEqualArrayLength();

            return new Tensor<T>(values, lengths, strides, false);
        }

        /// <summary>
        /// Creates a <see cref="Tensor{T}"/> and does not initialize it. If <paramref name="pinned"/> is true, the memory will be pinned.
        /// </summary>
        /// <param name="lengths">A <see cref="ReadOnlySpan{T}"/> indicating the lengths of each dimension.</param>
        /// <param name="pinned">A <see cref="bool"/> whether the underlying data should be pinned or not.</param>
        public static Tensor<T> CreateUninitialized<T>(scoped ReadOnlySpan<nint> lengths, bool pinned = false)
            where T : IEquatable<T>
        {
            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);
            T[] values = GC.AllocateUninitializedArray<T>((int)linearLength, pinned);
            return new Tensor<T>(values, lengths, pinned);
        }

        /// <summary>
        /// Creates a <see cref="Tensor{T}"/> and does not initialize it. If <paramref name="pinned"/> is true, the memory will be pinned.
        /// </summary>
        /// <param name="lengths">A <see cref="ReadOnlySpan{T}"/> indicating the lengths of each dimension.</param>
        /// <param name="strides">A <see cref="ReadOnlySpan{T}"/> indicating the strides of each dimension.</param>
        /// <param name="pinned">A <see cref="bool"/> whether the underlying data should be pinned or not.</param>
        public static Tensor<T> CreateUninitialized<T>(scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, bool pinned = false )
            where T : IEquatable<T>
        {
            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);
            T[] values = GC.AllocateUninitializedArray<T>((int)linearLength, pinned);
            return new Tensor<T>(values, lengths, strides, pinned);
        }

        /// <summary>
        /// Creates a <see cref="Tensor{T}"/> and initializes it with the data from <paramref name="data"/>.
        /// </summary>
        /// <param name="data">A <see cref="IEnumerable{T}"/> with the data to use for the initialization.</param>
        public static Tensor<T> CreateFromEnumerable<T>(IEnumerable<T> data)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            T[] values = data.ToArray();
            return new Tensor<T>(values, [values.Length], false);
        }

        /// <summary>
        /// Creates a <see cref="Tensor{T}"/> and initializes it with random data uniformly distributed.
        /// </summary>
        /// <param name="lengths">A <see cref="ReadOnlySpan{T}"/> indicating the lengths of each dimension.</param>
        public static Tensor<T> CreateAndFillUniformDistribution<T>(params scoped ReadOnlySpan<nint> lengths)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
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
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
        {
            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);
            T[] values = new T[linearLength];
            GaussianDistribution(ref values, linearLength);
            return new Tensor<T>(values, lengths, false);
        }

        private static void GaussianDistribution<T>(ref T[] values, nint linearLength)
             where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
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
    }
}
