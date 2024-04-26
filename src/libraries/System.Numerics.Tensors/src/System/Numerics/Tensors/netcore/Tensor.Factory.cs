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
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mustPin"></param>
        /// <param name="lengths"></param>
        /// <returns></returns>
        public static Tensor<T> Create<T>(bool mustPin, ReadOnlySpan<nint> lengths)
            where T : IEquatable<T>
        {
            nint linearLength = SpanNDHelpers.CalculateTotalLength(lengths);
            T[] values = mustPin ? GC.AllocateArray<T>((int)linearLength, mustPin) : (new T[linearLength]);
            return new Tensor<T>(values, lengths.ToArray(), mustPin);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mustPin"></param>
        /// <param name="lengths"></param>
        /// <param name="strides"></param>
        /// <returns></returns>
        public static Tensor<T> Create<T>(bool mustPin, ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides)
            where T : IEquatable<T>
        {
            nint linearLength = SpanNDHelpers.CalculateTotalLength(lengths);
            T[] values = mustPin ? GC.AllocateArray<T>((int)linearLength, mustPin) : (new T[linearLength]);
            return new Tensor<T>(values, lengths.ToArray(), strides.ToArray(), mustPin);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="lengths"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Tensor<T> Create<T>(T[] values, ReadOnlySpan<nint> lengths)
            where T : IEquatable<T>
        {
            nint linearLength = SpanNDHelpers.CalculateTotalLength(lengths);
            if (linearLength != values.Length)
                ThrowHelper.ThrowArgument_LengthsMustEqualArrayLength();

            return new Tensor<T>(values, lengths.ToArray(), false);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="lengths"></param>
        /// <param name="strides"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Tensor<T> Create<T>(T[] values, ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides)
            where T : IEquatable<T>
        {
            nint linearLength = SpanNDHelpers.CalculateTotalLength(lengths);
            if (linearLength != values.Length)
                ThrowHelper.ThrowArgument_LengthsMustEqualArrayLength();

            return new Tensor<T>(values, lengths.ToArray(), strides.ToArray(), false);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mustPin"></param>
        /// <param name="lengths"></param>
        /// <returns></returns>
        public static Tensor<T> CreateUninitialized<T>(bool mustPin, ReadOnlySpan<nint> lengths)
            where T : IEquatable<T>
        {
            nint linearLength = SpanNDHelpers.CalculateTotalLength(lengths);
            T[] values = GC.AllocateUninitializedArray<T>((int)linearLength, mustPin);
            return new Tensor<T>(values, lengths.ToArray(), mustPin);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mustPin"></param>
        /// <param name="lengths"></param>
        /// <param name="strides"></param>
        /// <returns></returns>
        public static Tensor<T> CreateUninitialized<T>(bool mustPin, ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides)
            where T : IEquatable<T>
        {
            nint linearLength = SpanNDHelpers.CalculateTotalLength(lengths);
            T[] values = GC.AllocateUninitializedArray<T>((int)linearLength, mustPin);
            return new Tensor<T>(values, lengths.ToArray(), strides.ToArray(), mustPin);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <param name="lengths"></param>
        /// <returns></returns>
        [CLSCompliant(false)]
        public static unsafe SpanND<T> CreateSpan<T>(T* address, ReadOnlySpan<nint> lengths) => new SpanND<T>(address, lengths, false);

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <param name="lengths"></param>
        /// <param name="strides"></param>
        /// <returns></returns>
        [CLSCompliant(false)]
        public static unsafe SpanND<T> CreateSpan<T>(T* address, ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides) => new SpanND<T>(address, lengths, false, strides);

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <param name="lengths"></param>
        /// <returns></returns>
        [CLSCompliant(false)]
        public static unsafe ReadOnlySpanND<T> CreateReadOnlySpan<T>(T* address, ReadOnlySpan<nint> lengths) => new ReadOnlySpanND<T>(address, lengths, false);

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <param name="lengths"></param>
        /// <param name="strides"></param>
        /// <returns></returns>
        [CLSCompliant(false)]
        public static unsafe ReadOnlySpanND<T> CreateReadOnlySpan<T>(T* address, ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides) => new ReadOnlySpanND<T>(address, lengths, false, strides);

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Tensor<T> CreateFromEnumerable<T>(IEnumerable<T> data)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            T[] values = data.ToArray();
            return new Tensor<T>(values, [values.Length], false);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lengths"></param>
        /// <returns></returns>
        public static Tensor<T> CreateUniform<T>(params ReadOnlySpan<nint> lengths)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
        {
            var linearLength = SpanNDHelpers.CalculateTotalLength(lengths);
            T[] values = new T[linearLength];
            Random rand = new Random();
            for (int i = 0; i < values.Length; i++)
                values[i] = T.CreateChecked(rand.NextSingle());

            return new Tensor<T>(values, lengths, false);
        }

        #region Normal
        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lengths"></param>
        /// <returns></returns>
        public static Tensor<T> CreateGaussianNormal<T>(params ReadOnlySpan<nint> lengths)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
        {
            var linearLength = SpanNDHelpers.CalculateTotalLength(lengths);
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
