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
        public static Tensor<T> Create<T>(bool mustPin, ReadOnlySpan<nint> lengths)
            where T : IEquatable<T>
        {
            nint linearLength = SpanHelpers.CalculateTotalLength(lengths);
            T[] values = mustPin ? GC.AllocateArray<T>((int)linearLength, mustPin) : (new T[linearLength]);
            return new Tensor<T>(values, lengths.ToArray(), mustPin);
        }

        public static Tensor<T> Create<T>(bool mustPin, ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides)
            where T : IEquatable<T>
        {
            nint linearLength = SpanHelpers.CalculateTotalLength(lengths);
            T[] values = mustPin ? GC.AllocateArray<T>((int)linearLength, mustPin) : (new T[linearLength]);
            return new Tensor<T>(values, lengths.ToArray(), strides.ToArray(), mustPin);
        }

        public static Tensor<T> Create<T>(T[] values, ReadOnlySpan<nint> lengths)
            where T : IEquatable<T>
        {
            nint linearLength = SpanHelpers.CalculateTotalLength(lengths);
            if (linearLength != values.Length)
                throw new Exception();

            return new Tensor<T>(values, lengths.ToArray(), false);
        }

        public static Tensor<T> Create<T>(T[] values, ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides)
            where T : IEquatable<T>
        {
            nint linearLength = SpanHelpers.CalculateTotalLength(lengths);
            if (linearLength != values.Length)
                throw new Exception();

            return new Tensor<T>(values, lengths.ToArray(), strides.ToArray(), false);
        }

        //public static Tensor<T> Create(T* address, ReadOnlySpan<nint> lengths);
        //public static Tensor<T> Create(T* address, ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides);

        public static Tensor<T> CreateUninitialized<T>(bool mustPin, ReadOnlySpan<nint> lengths)
            where T : IEquatable<T>
        {
            nint linearLength = SpanHelpers.CalculateTotalLength(lengths);
            T[] values = GC.AllocateUninitializedArray<T>((int)linearLength, mustPin);
            return new Tensor<T>(values, lengths.ToArray(), mustPin);
        }

        public static Tensor<T> CreateUninitialized<T>(bool mustPin, ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides)
            where T : IEquatable<T>
        {
            nint linearLength = SpanHelpers.CalculateTotalLength(lengths);
            T[] values = GC.AllocateUninitializedArray<T>((int)linearLength, mustPin);
            return new Tensor<T>(values, lengths.ToArray(), strides.ToArray(), mustPin);
        }

        [CLSCompliant(false)]
        public static unsafe SpanND<T> CreateSpan<T>(T* address, ReadOnlySpan<nint> lengths) => new SpanND<T>(address, lengths, false);

        [CLSCompliant(false)]
        public static unsafe SpanND<T> CreateSpan<T>(T* address, ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides) => new SpanND<T>(address, lengths, false, strides);

        [CLSCompliant(false)]
        public static unsafe ReadOnlySpanND<T> CreateReadOnlySpan<T>(T* address, ReadOnlySpan<nint> lengths) => new ReadOnlySpanND<T>(address, lengths, false);

        [CLSCompliant(false)]
        public static unsafe ReadOnlySpanND<T> CreateReadOnlySpan<T>(T* address, ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides) => new ReadOnlySpanND<T>(address, lengths, false, strides);

        public static Tensor<T> FillRange<T>(IEnumerable<T> data)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            T[] values = data.ToArray();
            return new Tensor<T>(values, [values.Length], false);
        }

        // REVIEW: BASICALLY SEEDING A TENSOR. CHANGE API SHAPE/NAME?
        public static Tensor<T> Uniform<T>(params nint[] lengths)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
        {
            var linearLength = SpanHelpers.CalculateTotalLength(ref lengths);
            T[] values = new T[linearLength];
            Random rand = new Random();
            for (int i = 0; i < values.Length; i++)
                values[i] = T.CreateChecked(rand.NextSingle());

            return new Tensor<T>(values, lengths, false);
        }

        #region Normal
        // REVIEW: BASICALLY SEEDING A TENSOR. CHANGE API SHAPE/NAME?
        public static Tensor<T> Normal<T>(params nint[] lengths)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
        {
            var linearLength = SpanHelpers.CalculateTotalLength(ref lengths);
            T[] values = new T[linearLength];
            GaussianDistribution(ref values, linearLength);
            return new Tensor<T>(values, lengths, false);
        }

        private static void GaussianDistribution<T>(ref T[] values, nint linearLength)
             where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
        {
            Random rand = new Random();
            for (int i = 0; i < linearLength; i++)
            {
                float u1 = 1.0f - rand.NextSingle();
                float u2 = 1.0f - rand.NextSingle();
                values[i] = T.CreateChecked(MathF.Sqrt(-2.0f * MathF.Log(u1)) * MathF.Sin(2.0f * MathF.PI * u2));
            }
        }
        #endregion

    }
}
