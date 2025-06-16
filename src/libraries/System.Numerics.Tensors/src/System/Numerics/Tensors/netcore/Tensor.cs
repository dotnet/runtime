// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Numerics.Tensors
{
    /// <summary>Provides methods for tensor operations.</summary>
    [Experimental(Experimentals.TensorTDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public static partial class Tensor
    {
        #region AsTensorSpan
        /// <inheritdoc cref="ReadOnlyTensorSpan{T}.ReadOnlyTensorSpan(T[])" />
        public static ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan<T>(this T[]? array)
            => new ReadOnlyTensorSpan<T>(array);

        /// <inheritdoc cref="ReadOnlyTensorSpan{T}.ReadOnlyTensorSpan(T[], ReadOnlySpan{nint})" />
        public static ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan<T>(this T[]? array, scoped ReadOnlySpan<nint> lengths)
            => new ReadOnlyTensorSpan<T>(array, lengths);

        /// <inheritdoc cref="ReadOnlyTensorSpan{T}.ReadOnlyTensorSpan(T[], ReadOnlySpan{nint}, ReadOnlySpan{nint})" />
        public static ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan<T>(this T[]? array, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
            => new ReadOnlyTensorSpan<T>(array, lengths, strides);

        /// <inheritdoc cref="ReadOnlyTensorSpan{T}.ReadOnlyTensorSpan(T[], int, ReadOnlySpan{nint}, ReadOnlySpan{nint})" />
        public static ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan<T>(this T[]? array, int start, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
            => new ReadOnlyTensorSpan<T>(array, start, lengths, strides);

        /// <inheritdoc cref="TensorSpan{T}.TensorSpan(T[])" />
        public static TensorSpan<T> AsTensorSpan<T>(this T[]? array)
            => new TensorSpan<T>(array);

        /// <inheritdoc cref="TensorSpan{T}.TensorSpan(T[], ReadOnlySpan{nint})" />
        public static TensorSpan<T> AsTensorSpan<T>(this T[]? array, scoped ReadOnlySpan<nint> lengths)
            => new TensorSpan<T>(array, lengths);

        /// <inheritdoc cref="TensorSpan{T}.TensorSpan(T[], ReadOnlySpan{nint} , ReadOnlySpan{nint})" />
        public static TensorSpan<T> AsTensorSpan<T>(this T[]? array, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
            => new TensorSpan<T>(array, lengths, strides);

        /// <inheritdoc cref="TensorSpan{T}.TensorSpan(T[], int, ReadOnlySpan{nint}, ReadOnlySpan{nint})" />
        public static TensorSpan<T> AsTensorSpan<T>(this T[]? array, int start, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides) => new TensorSpan<T>(array, start, lengths, strides);
        #endregion

        #region Broadcast
        /// <summary>
        /// Broadcast the data from <paramref name="source"/> to the smallest broadcastable shape compatible with <paramref name="lengthsSource"/>. Creates a new <see cref="Tensor{T}"/> and allocates new memory.
        /// </summary>
        /// <param name="source">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="lengthsSource">Other <see cref="Tensor{T}"/> to make shapes broadcastable.</param>
        public static Tensor<T> Broadcast<T>(scoped in ReadOnlyTensorSpan<T> source, scoped in ReadOnlyTensorSpan<T> lengthsSource)
        {
            return Broadcast(source, lengthsSource.Lengths);
        }

        /// <summary>
        /// Broadcast the data from <paramref name="source"/> to the new shape <paramref name="lengths"/>. Creates a new <see cref="Tensor{T}"/> and allocates new memory.
        /// If the shape of the <paramref name="source"/> is not compatible with the new shape, an exception is thrown.
        /// </summary>
        /// <param name="source">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="lengths"><see cref="ReadOnlySpan{T}"/> of the desired new shape.</param>
        /// <exception cref="ArgumentException">The shapes are not broadcast compatible.</exception>
        public static Tensor<T> Broadcast<T>(scoped in ReadOnlyTensorSpan<T> source, scoped ReadOnlySpan<nint> lengths)
        {
            TensorOperation.ValidateCompatibility<T>(source, lengths);
            Tensor<T> destination = Tensor.CreateUninitialized<T>(lengths);
            TensorOperation.Invoke<TensorOperation.CopyTo<T>, T, T>(source, destination);
            return destination;
        }
        #endregion

        #region BroadcastTo
        /// <summary>
        /// Broadcast the data from <paramref name="source"/> to <paramref name="destination"/>.
        /// </summary>
        /// <param name="source">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static void BroadcastTo<T>(this Tensor<T> source, in TensorSpan<T> destination)
        {
            TensorOperation.ValidateCompatibility<T, T>(source, destination);
            TensorOperation.Invoke<TensorOperation.CopyTo<T>, T, T>(source, destination);
        }

        /// <summary>
        /// Broadcast the data from <paramref name="source"/> to <paramref name="destination"/>.
        /// </summary>
        /// <param name="source">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="destination">Other <see cref="TensorSpan{T}"/> to make shapes broadcastable.</param>
        public static void BroadcastTo<T>(in this TensorSpan<T> source, in TensorSpan<T> destination)
        {
            TensorOperation.ValidateCompatibility<T, T>(source, destination);
            TensorOperation.Invoke<TensorOperation.CopyTo<T>, T, T>(source, destination);
        }

        /// <summary>
        /// Broadcast the data from <paramref name="source"/> to <paramref name="destination"/>.
        /// </summary>
        /// <param name="source">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static void BroadcastTo<T>(in this ReadOnlyTensorSpan<T> source, in TensorSpan<T> destination)
        {
            TensorOperation.ValidateCompatibility<T, T>(source, destination);
            TensorOperation.Invoke<TensorOperation.CopyTo<T>, T, T>(source, destination);
        }
        #endregion

        #region Concatenate
        /// <summary>
        /// Join a sequence of tensors along an existing axis.
        /// </summary>
        /// <param name="tensors">The tensors must have the same shape, except in the dimension corresponding to axis (the first, by default).</param>
        public static Tensor<T> Concatenate<T>(params scoped ReadOnlySpan<Tensor<T>> tensors)
        {
            return ConcatenateOnDimension(0, tensors);
        }

        /// <summary>
        /// Join a sequence of tensors along an existing axis.
        /// </summary>
        /// <param name="tensors">The tensors must have the same shape, except in the dimension corresponding to axis (the first, by default).</param>
        /// <param name="dimension">The axis along which the tensors will be joined. If axis is -1, arrays are flattened before use. Default is 0.</param>
        public static Tensor<T> ConcatenateOnDimension<T>(int dimension, params scoped ReadOnlySpan<Tensor<T>> tensors)
        {
            if (tensors.Length < 2)
                ThrowHelper.ThrowArgument_ConcatenateTooFewTensors();

            if (dimension < -1 || dimension > tensors[0].Rank)
                ThrowHelper.ThrowArgument_InvalidDimension();

            Tensor<T> tensor;

            // If axis != -1, make sure all dimensions except the one to concatenate on match.
            if (dimension != -1)
            {
                nint sumOfAxis = tensors[0].Lengths[dimension];
                for (int i = 1; i < tensors.Length; i++)
                {
                    if (tensors[0].Rank != tensors[i].Rank)
                        ThrowHelper.ThrowArgument_InvalidConcatenateShape();
                    for (int j = 0; j < tensors[0].Rank; j++)
                    {
                        if (j != dimension)
                        {
                            if (tensors[0].Lengths[j] != tensors[i].Lengths[j])
                                ThrowHelper.ThrowArgument_InvalidConcatenateShape();
                        }
                    }
                    checked
                    {
                        sumOfAxis += tensors[i].Lengths[dimension];
                    }
                }

                nint[] lengths = new nint[tensors[0].Rank];
                tensors[0].Lengths.CopyTo(lengths);
                lengths[dimension] = sumOfAxis;
                tensor = Tensor.Create<T>(lengths);
            }
            else
            {
                // Calculate total space needed.
                nint totalLength = 0;
                for (int i = 0; i < tensors.Length; i++)
                {
                    checked
                    {
                        totalLength += tensors[i].FlattenedLength;
                    }
                }

                tensor = Tensor.Create<T>([totalLength]);
            }

            ConcatenateOnDimension(dimension, tensors, tensor);
            return tensor;
        }

        /// <summary>
        /// Join a sequence of tensors along an existing axis.
        /// </summary>
        /// <param name="tensors">The tensors must have the same shape, except in the dimension corresponding to axis (the first, by default).</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Concatenate<T>(scoped ReadOnlySpan<Tensor<T>> tensors, in TensorSpan<T> destination)
        {
            return ref ConcatenateOnDimension(0, tensors, destination);
        }

        /// <summary>
        /// Join a sequence of tensors along an existing axis.
        /// </summary>
        /// <param name="tensors">The tensors must have the same shape, except in the dimension corresponding to axis (the first, by default).</param>
        /// <param name="dimension">The axis along which the tensors will be joined. If axis is -1, arrays are flattened before use. Default is 0.</param>
        /// <param name="destination"></param>

        public static ref readonly TensorSpan<T> ConcatenateOnDimension<T>(int dimension, scoped ReadOnlySpan<Tensor<T>> tensors, in TensorSpan<T> destination)
        {
            if (tensors.Length < 2)
                ThrowHelper.ThrowArgument_ConcatenateTooFewTensors();

            if (dimension < -1 || dimension > tensors[0].Rank)
                ThrowHelper.ThrowArgument_InvalidDimension();

            // Calculate total space needed.
            nint totalLength = 0;
            for (int i = 0; i < tensors.Length; i++)
                totalLength += tensors[i].FlattenedLength;

            // If axis != -1, make sure all dimensions except the one to concatenate on match.
            if (dimension != -1)
            {
                nint sumOfAxis = tensors[0].Lengths[dimension];
                int rank = tensors[0].Rank;
                for (int i = 1; i < tensors.Length; i++)
                {
                    if (rank != tensors[i].Rank)
                        ThrowHelper.ThrowArgument_InvalidConcatenateShape();
                    for (int j = 0; j < rank; j++)
                    {
                        if (j != dimension)
                        {
                            if (tensors[0].Lengths[j] != tensors[i].Lengths[j])
                                ThrowHelper.ThrowArgument_InvalidConcatenateShape();
                        }
                    }
                    sumOfAxis += tensors[i].Lengths[dimension];
                }

                // Make sure the destination tensor has the correct shape.
                nint[] lengths = new nint[rank];
                tensors[0].Lengths.CopyTo(lengths);
                lengths[dimension] = sumOfAxis;

                if (!TensorShape.AreLengthsTheSame(destination.Lengths, lengths))
                    ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(destination));
            }
            Span<T> dstSpan = MemoryMarshal.CreateSpan(ref destination._reference, (int)totalLength);

            if (dimension == 0 || dimension == -1)
            {
                for (int i = 0; i < tensors.Length; i++)
                {
                    TensorOperation.Invoke<TensorOperation.CopyTo<T>, T, T>(tensors[i], dstSpan);
                    dstSpan = dstSpan.Slice((int)tensors[i].FlattenedLength);
                }
            }
            else
            {
                Span<NRange> ranges = TensorOperation.RentedBuffer.CreateUninitialized(destination.Rank, out TensorOperation.RentedBuffer<NRange> rentedBuffer);
                for (int i = 0; i < dimension; i++)
                {
                    ranges[i] = 0..1;
                }
                for (int i = dimension; i < destination.Rank; i++)
                {
                    ranges[i] = ..;
                }

                bool hasMore = true;
                while (hasMore)
                {
                    for (int i = 0; i < tensors.Length; i++)
                    {
                        Tensor<T> slice = tensors[i].Slice(ranges);
                        TensorOperation.Invoke<TensorOperation.CopyTo<T>, T, T>(slice, dstSpan);
                        dstSpan = dstSpan.Slice((int)slice.FlattenedLength);
                    }
                    // We do dimension - 1 because we want to include the dimension we concatenated on.
                    hasMore = TensorShape.AdjustToNextIndex(ranges, dimension - 1, destination.Lengths);
                }
                rentedBuffer.Dispose();
            }
            return ref destination;
        }

        private static nint CalculateCopyLength(ReadOnlySpan<nint> lengths, int startingAxis)
        {
            // When starting axis is -1 we want all the data at once same as if starting axis is 0
            if (startingAxis == -1)
                startingAxis = 0;
            nint length = 1;
            for (int i = startingAxis; i < lengths.Length; i++)
            {
                length *= lengths[i];
            }
            return length;
        }
        #endregion

        #region Create
        /// <inheritdoc cref="ITensor{TSelf, T}.Create(ReadOnlySpan{nint}, bool)" />
        /// <returns>A new tensor with the specified lengths.</returns>
        public static Tensor<T> Create<T>(scoped ReadOnlySpan<nint> lengths, bool pinned = false)
            => new Tensor<T>(lengths, strides: [], pinned);

        /// <inheritdoc cref="ITensor{TSelf, T}.Create(ReadOnlySpan{nint}, ReadOnlySpan{nint}, bool)" />
        /// <returns>A new tensor with the specified <paramref name="lengths" /> and <paramref name="strides" />.</returns>
        public static Tensor<T> Create<T>(scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, bool pinned = false)
            => new Tensor<T>(lengths, strides, pinned);

        /// <inheritdoc cref="ReadOnlyTensorSpan{T}.ReadOnlyTensorSpan(T[])" />
        /// <returns>A new tensor that uses <paramref name="array" /> as its backing buffer.</returns>
        public static Tensor<T> Create<T>(T[] array)
            => new Tensor<T>(array);

        /// <inheritdoc cref="ReadOnlyTensorSpan{T}.ReadOnlyTensorSpan(T[], ReadOnlySpan{nint})" />
        /// <returns>A new tensor that uses <paramref name="array" /> as its backing buffer and with the specified <paramref name="lengths" />.</returns>
        public static Tensor<T> Create<T>(T[] array, scoped ReadOnlySpan<nint> lengths)
            => new Tensor<T>(array, lengths);

        /// <inheritdoc cref="ReadOnlyTensorSpan{T}.ReadOnlyTensorSpan(T[], ReadOnlySpan{nint}, ReadOnlySpan{nint})" />
        /// <returns>A new tensor that uses <paramref name="array" /> as its backing buffer and with the specified <paramref name="lengths" /> and <paramref name="strides"/>.</returns>
        public static Tensor<T> Create<T>(T[] array, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
            => new Tensor<T>(array, lengths, strides);

        /// <inheritdoc cref="ReadOnlyTensorSpan{T}.ReadOnlyTensorSpan(T[], int, ReadOnlySpan{nint}, ReadOnlySpan{nint})" />
        /// <returns>A new tensor that uses <paramref name="array" /> as its backing buffer and with the specified <paramref name="lengths" /> and <paramref name="strides" />.</returns>
        public static Tensor<T> Create<T>(T[] array, int start, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
            => new Tensor<T>(array, start, lengths, strides);

        /// <returns>A new tensor that contains elements copied from <paramref name="enumerable" />.</returns>
        public static Tensor<T> Create<T>(IEnumerable<T> enumerable, bool pinned = false)
        {
            T[] array = enumerable.ToArray();

            if (pinned)
            {
                Tensor<T> tensor = CreateUninitialized<T>([array.Length], pinned);
                array.CopyTo(tensor._values);

                return tensor;
            }
            else
            {
                return Create(array);
            }
        }

        /// <inheritdoc cref="Create{T}(IEnumerable{T}, ReadOnlySpan{nint}, bool)" />
        /// <returns>A new tensor that contains elements copied from <paramref name="enumerable" /> and with the specified <paramref name="lengths" />.</returns>
        public static Tensor<T> Create<T>(IEnumerable<T> enumerable, scoped ReadOnlySpan<nint> lengths, bool pinned = false)
            => Create(enumerable, lengths, strides: [], pinned);

        /// <inheritdoc cref="Create{T}(IEnumerable{T}, ReadOnlySpan{nint}, ReadOnlySpan{nint}, bool)" />
        /// <returns>A new tensor that contains elements copied from <paramref name="enumerable" /> and with the specified <paramref name="lengths" /> and <paramref name="strides" />.</returns>
        public static Tensor<T> Create<T>(IEnumerable<T> enumerable, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, bool pinned = false)
        {
            T[] array = enumerable.ToArray();

            if (pinned)
            {
                Tensor<T> tensor = CreateUninitialized<T>(lengths, strides, pinned);
                array.CopyTo(tensor._values);

                return tensor;
            }
            else
            {
                return Create(array, lengths, strides);
            }
        }

        /// <summary>
        /// Creates a <see cref="Tensor{T}"/> and initializes it with random data in a gaussian normal distribution.
        /// </summary>
        /// <param name="lengths">A <see cref="ReadOnlySpan{T}"/> indicating the lengths of each dimension.</param>
        public static Tensor<T> CreateAndFillGaussianNormalDistribution<T>(scoped ReadOnlySpan<nint> lengths)
            where T : IFloatingPoint<T>
        {
            return CreateAndFillGaussianNormalDistribution<T>(Random.Shared, lengths);
        }

        /// <summary>
        /// Creates a <see cref="Tensor{T}"/> and initializes it with random data in a gaussian normal distribution.
        /// </summary>
        /// <param name="random"></param>
        /// <param name="lengths">A <see cref="ReadOnlySpan{T}"/> indicating the lengths of each dimension.</param>
        public static Tensor<T> CreateAndFillGaussianNormalDistribution<T>(Random random, scoped ReadOnlySpan<nint> lengths)
            where T : IFloatingPoint<T>
        {
            Tensor<T> tensor = CreateUninitialized<T>(lengths);
            FillGaussianNormalDistribution<T>(tensor, random);
            return tensor;
        }


        /// <summary>
        /// Creates a <see cref="Tensor{T}"/> and initializes it with random data uniformly distributed.
        /// </summary>
        /// <param name="lengths">A <see cref="ReadOnlySpan{T}"/> indicating the lengths of each dimension.</param>
        public static Tensor<T> CreateAndFillUniformDistribution<T>(scoped ReadOnlySpan<nint> lengths)
            where T : IFloatingPoint<T>
        {
            return CreateAndFillUniformDistribution<T>(Random.Shared, lengths);
        }

        /// <summary>
        /// Creates a <see cref="Tensor{T}"/> and initializes it with random data uniformly distributed.
        /// </summary>
        /// <param name="random"></param>
        /// <param name="lengths">A <see cref="ReadOnlySpan{T}"/> indicating the lengths of each dimension.</param>
        public static Tensor<T> CreateAndFillUniformDistribution<T>(Random random, scoped ReadOnlySpan<nint> lengths)
            where T : IFloatingPoint<T>
        {
            Tensor<T> tensor = CreateUninitialized<T>(lengths);
            FillUniformDistribution<T>(tensor, random);
            return tensor;
        }

        /// <inheritdoc cref="ITensor{TSelf, T}.CreateUninitialized(ReadOnlySpan{nint}, bool)" />
        public static Tensor<T> CreateUninitialized<T>(scoped ReadOnlySpan<nint> lengths, bool pinned = false)
        {
            TensorShape shape = TensorShape.Create(lengths, strides: []);
            T[] array = GC.AllocateUninitializedArray<T>(checked((int)(shape.LinearLength)), pinned);
            return new Tensor<T>(array, in shape, pinned);
        }

        /// <inheritdoc cref="ITensor{TSelf, T}.CreateUninitialized(ReadOnlySpan{nint}, ReadOnlySpan{nint}, bool)" />
        public static Tensor<T> CreateUninitialized<T>(scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, bool pinned = false)
        {
            TensorShape shape = TensorShape.Create(lengths, strides);
            T[] values = GC.AllocateUninitializedArray<T>(checked((int)(shape.LinearLength)), pinned);
            return new Tensor<T>(values, in shape, pinned);
        }
        #endregion

        #region Fill
        /// <summary>
        /// Fills the given <see cref="TensorSpan{T}"/> with random data in a Gaussian normal distribution. <see cref="System.Random"/>
        /// can optionally be provided for seeding.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="destination">The destination <see cref="TensorSpan{T}"/> where the data will be stored.</param>
        /// <param name="random"><see cref="System.Random"/> to provide random seeding. Defaults to <see cref="Random.Shared"/> if not provided.</param>
        /// <returns></returns>
        public static ref readonly TensorSpan<T> FillGaussianNormalDistribution<T>(in TensorSpan<T> destination, Random? random = null) where T : IFloatingPoint<T>
        {
            Span<T> span = MemoryMarshal.CreateSpan<T>(ref destination._reference, (int)destination._shape.LinearLength);
            random ??= Random.Shared;

            for (int i = 0; i < span.Length; i++)
            {
                double u1 = 1.0 - random.NextDouble();
                double u2 = 1.0 - random.NextDouble();
                span[i] = T.CreateChecked(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2));
            }

            return ref destination;
        }

        /// <summary>
        /// Fills the given <see cref="TensorSpan{T}"/> with random data in a uniform distribution. <see cref="System.Random"/>
        /// can optionally be provided for seeding.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="destination">The destination <see cref="TensorSpan{T}"/> where the data will be stored.</param>
        /// <param name="random"><see cref="System.Random"/> to provide random seeding. Defaults to <see cref="Random.Shared"/> if not provided.</param>
        /// <returns></returns>
        public static ref readonly TensorSpan<T> FillUniformDistribution<T>(in TensorSpan<T> destination, Random? random = null) where T : IFloatingPoint<T>
        {
            Span<T> span = MemoryMarshal.CreateSpan<T>(ref destination._reference, (int)destination._shape.LinearLength);
            random ??= Random.Shared;
            for (int i = 0; i < span.Length; i++)
                span[i] = T.CreateChecked(random.NextDouble());

            return ref destination;
        }
        #endregion

        #region Equals
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> for equality. If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size
        /// before they are compared. It returns a <see cref="TensorSpan{Boolean}"/> where the value is true if the elements are equal and false if they are not."/>
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <returns>A <see cref="TensorSpan{Boolean}"/> where the value is true if the elements are equal and false if they are not.</returns>
        public static Tensor<bool> Equals<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IEqualityOperators<T, T, bool>
        {
            TensorOperation.ValidateCompatibility(x, y, out Tensor<bool> destination);
            TensorOperation.Invoke<TensorOperation.Equals<T>, T, bool>(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> for equality. If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size
        /// before they are compared. It returns a <see cref="TensorSpan{Boolean}"/> where the value is true if the elements are equal and false if they are not."/>
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="destination"></param>
        /// <returns>A <see cref="TensorSpan{Boolean}"/> where the value is true if the elements are equal and false if they are not.</returns>
        public static ref readonly TensorSpan<bool> Equals<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<bool> destination)
            where T : IEqualityOperators<T, T, bool>
        {
            TensorOperation.ValidateCompatibility(x, y, destination);
            TensorOperation.Invoke<TensorOperation.Equals<T>, T, bool>(x, y, destination);
            return ref destination;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> for equality. If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size
        /// before they are compared. It returns a <see cref="TensorSpan{Boolean}"/> where the value is true if the elements are equal and false if they are not."/>
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second value to compare.</param>
        /// <returns>A <see cref="TensorSpan{Boolean}"/> where the value is true if the elements are equal and false if they are not.</returns>
        public static Tensor<bool> Equals<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IEqualityOperators<T, T, bool>
        {
            Tensor<bool> destination = CreateUninitialized<bool>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Equals<T>, T, bool>(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> for equality. If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size
        /// before they are compared. It returns a <see cref="TensorSpan{Boolean}"/> where the value is true if the elements are equal and false if they are not."/>
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second value to compare.</param>
        /// <param name="destination"></param>
        /// <returns>A <see cref="TensorSpan{Boolean}"/> where the value is true if the elements are equal and false if they are not.</returns>
        public static ref readonly TensorSpan<bool> Equals<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<bool> destination)
            where T : IEqualityOperators<T, T, bool>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Equals<T>, T, bool>(x, y, destination);
            return ref destination;
        }
        #endregion

        #region EqualsAll
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if all elements of <paramref name="x"/> are equal to <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are eqaul to <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are equal to <paramref name="y"/>.</returns>
        public static bool EqualsAll<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IEqualityOperators<T, T, bool>
        {
            TensorOperation.ValidateCompatibility(x, y);
            return TensorOperation.Invoke<TensorOperation.Equals<T>, T>(x, y);
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if all elements of <paramref name="x"/> are equal to <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are eqaul to <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are equal to <paramref name="y"/>.</returns>
        public static bool EqualsAll<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IEqualityOperators<T, T, bool> => TensorOperation.Invoke<TensorOperation.Equals<T>, T>(x, y);
        #endregion

        #region EqualsAny
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if any elements of <paramref name="x"/> are equal to <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are equal to <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are equal to <paramref name="y"/>.</returns>
        public static bool EqualsAny<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IEqualityOperators<T, T, bool>
        {
            // The main loop early exits at the first false condition, so the TensorOperation
            // checks x != y and returns false on first equal. This means we want to negate
            // whatever the main loop returns as `true` means none are equal.

            TensorOperation.ValidateCompatibility(x, y);
            return !TensorOperation.Invoke<TensorOperation.EqualsAny<T>, T>(x, y);
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if any elements of <paramref name="x"/> are equal to <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are equal to <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Value to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are equal to <paramref name="y"/>.</returns>
        public static bool EqualsAny<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IEqualityOperators<T, T, bool> => !TensorOperation.Invoke<TensorOperation.EqualsAny<T>, T>(x, y);
        #endregion

        #region FilteredUpdate
        /// <summary>
        /// Updates the <paramref name="tensor"/> tensor with the <paramref name="value"/> where the <paramref name="filter"/> is true.
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="filter">Input filter where if the index is true then it will update the <paramref name="tensor"/>.</param>
        /// <param name="value">Value to update in the <paramref name="tensor"/>.</param>
        public static ref readonly TensorSpan<T> FilteredUpdate<T>(in this TensorSpan<T> tensor, scoped in ReadOnlyTensorSpan<bool> filter, T value)
        {
            TensorOperation.ValidateCompatibility(filter, tensor);
            TensorOperation.Invoke<TensorOperation.FilteredUpdate<T>, bool, T, T>(filter, value, tensor);
            return ref tensor;
        }

        /// <summary>
        /// Updates the <paramref name="tensor"/> tensor with the <paramref name="values"/> where the <paramref name="filter"/> is true.
        /// If dimensions are not the same an exception is thrown.
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="filter">Input filter where if the index is true then it will update the <paramref name="tensor"/>.</param>
        /// <param name="values">Values to update in the <paramref name="tensor"/>.</param>
        public static ref readonly TensorSpan<T> FilteredUpdate<T>(in this TensorSpan<T> tensor, scoped in ReadOnlyTensorSpan<bool> filter, scoped in ReadOnlyTensorSpan<T> values)
        {
            TensorOperation.ValidateCompatibility(filter, values, tensor);
            TensorOperation.Invoke<TensorOperation.FilteredUpdate<T>, bool, T, T>(filter, values, tensor);
            return ref tensor;
        }
        #endregion

        #region GreaterThan
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see which elements of <paramref name="x"/> are greater than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <returns>A <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/> and
        /// false if they are not.</returns>
        public static Tensor<bool> GreaterThan<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            TensorOperation.ValidateCompatibility(x, y, out Tensor<bool> destination);
            TensorOperation.Invoke<TensorOperation.GreaterThan<T>, T, bool>(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see which elements of <paramref name="x"/> are greater than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="destination"></param>
        /// <returns>A <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/> and
        /// false if they are not.</returns>
        public static ref readonly TensorSpan<bool> GreaterThan<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<bool> destination)
            where T : IComparisonOperators<T, T, bool>
        {
            TensorOperation.ValidateCompatibility(x, y, destination);
            TensorOperation.Invoke<TensorOperation.GreaterThan<T>, T, bool>(x, y, destination);
            return ref destination;
        }

        /// <summary>
        /// Compares the elements of a <see cref="ReadOnlyTensorSpan{T}"/> to see which elements are greater than <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static Tensor<bool> GreaterThan<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IComparisonOperators<T, T, bool>
        {
            Tensor<bool> destination = Tensor.Create<bool>(x.Lengths, false);
            GreaterThan(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Compares the elements of a <see cref="ReadOnlyTensorSpan{T}"/> to see which elements are greater than <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <param name="destination"></param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static ref readonly TensorSpan<bool> GreaterThan<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<bool> destination)
            where T : IComparisonOperators<T, T, bool>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.GreaterThan<T>, T, bool>(x, y, destination);
            return ref destination;
        }

        /// <summary>
        /// Compares <paramref name="x"/> to see which elements are greater than <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static Tensor<bool> GreaterThan<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool> => LessThan(y, x);

        /// <summary>
        /// Compares <paramref name="x"/> to see which elements are greater than <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <param name="destination"></param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static ref readonly TensorSpan<bool> GreaterThan<T>(T x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<bool> destination)
            where T : IComparisonOperators<T, T, bool> => ref LessThan(y, x, destination);
        #endregion

        #region GreaterThanAll
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if all elements of <paramref name="x"/> are greater than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are greater than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are greater than <paramref name="y"/>.</returns>
        public static bool GreaterThanAll<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            TensorOperation.ValidateCompatibility(x, y);
            return TensorOperation.Invoke<TensorOperation.GreaterThan<T>, T>(x, y);
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if all elements of <paramref name="x"/> are greater than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are greater than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are greater than <paramref name="y"/>.</returns>
        public static bool GreaterThanAll<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IComparisonOperators<T, T, bool> => TensorOperation.Invoke<TensorOperation.GreaterThan<T>, T>(x, y);

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if all elements of <paramref name="y"/> are greater than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="y"/> are greater than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="y"/> are greater than <paramref name="y"/>.</returns>
        public static bool GreaterThanAll<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool> => LessThanAll(y, x);
        #endregion

        #region GreaterThanAny
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if any elements of <paramref name="x"/> are greater than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are greater than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are greater than <paramref name="y"/>.</returns>
        public static bool GreaterThanAny<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            // The main loop early exits at the first false condition, so the TensorOperation
            // checks !(x > y) and returns false on first equal. This means we want to negate
            // whatever the main loop returns as `true` means none are equal.

            TensorOperation.ValidateCompatibility(x, y);
            return !TensorOperation.Invoke<TensorOperation.GreaterThanAny<T>, T>(x, y);
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if any elements of <paramref name="x"/> are greater than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are greater than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Value to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are greater than <paramref name="y"/>.</returns>
        public static bool GreaterThanAny<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IComparisonOperators<T, T, bool> => !TensorOperation.Invoke<TensorOperation.GreaterThanAny<T>, T>(x, y);

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if any elements of <paramref name="y"/> are greater than <paramref name="x"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="y"/> are greater than <paramref name="x"/>.
        /// </summary>
        /// <param name="y">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="x">Value to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="y"/> are greater than <paramref name="x"/>.</returns>
        public static bool GreaterThanAny<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool> => LessThanAny(y, x);
        #endregion

        #region GreaterThanOrEqual
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see which elements of <paramref name="x"/> are greater than or equal to <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <returns>A <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/> and
        /// false if they are not.</returns>
        public static Tensor<bool> GreaterThanOrEqual<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            TensorOperation.ValidateCompatibility(x, y, out Tensor<bool> destination);
            TensorOperation.Invoke<TensorOperation.GreaterThanOrEqual<T>, T, bool>(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see which elements of <paramref name="x"/> are greater than or equal to <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="destination"></param>
        /// <returns>A <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/> and
        /// false if they are not.</returns>
        public static ref readonly TensorSpan<bool> GreaterThanOrEqual<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<bool> destination)
            where T : IComparisonOperators<T, T, bool>
        {
            TensorOperation.ValidateCompatibility(x, y, destination);
            TensorOperation.Invoke<TensorOperation.GreaterThanOrEqual<T>, T, bool>(x, y, destination);
            return ref destination;
        }

        /// <summary>
        /// Compares the elements of a <see cref="ReadOnlyTensorSpan{T}"/> to see which elements are greater than or equal to <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static Tensor<bool> GreaterThanOrEqual<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IComparisonOperators<T, T, bool>
        {
            Tensor<bool> destination = Tensor.Create<bool>(x.Lengths, false);
            GreaterThanOrEqual(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Compares the elements of a <see cref="ReadOnlyTensorSpan{T}"/> to see which elements are greater than or equal to <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <param name="destination"></param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static ref readonly TensorSpan<bool> GreaterThanOrEqual<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<bool> destination)
            where T : IComparisonOperators<T, T, bool>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.GreaterThanOrEqual<T>, T, bool>(x, y, destination);
            return ref destination;
        }

        /// <summary>
        /// Compares <paramref name="x"/> to see which elements are greater than or equal to <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static Tensor<bool> GreaterThanOrEqual<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool> => LessThanOrEqual(y, x);

        /// <summary>
        /// Compares <paramref name="x"/> to see which elements are greater than or equal to <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <param name="destination"></param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are greater than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static ref readonly TensorSpan<bool> GreaterThanOrEqual<T>(T x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<bool> destination)
            where T : IComparisonOperators<T, T, bool> => ref LessThanOrEqual(y, x, destination);
        #endregion

        #region GreaterThanOrEqualAll
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if all elements of <paramref name="x"/> are greater than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are greater than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are greater than <paramref name="y"/>.</returns>
        public static bool GreaterThanOrEqualAll<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            TensorOperation.ValidateCompatibility(x, y);
            return TensorOperation.Invoke<TensorOperation.GreaterThanOrEqual<T>, T>(x, y);
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if all elements of <paramref name="x"/> are greater than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are greater than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are greater than <paramref name="y"/>.</returns>
        public static bool GreaterThanOrEqualAll<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IComparisonOperators<T, T, bool> => TensorOperation.Invoke<TensorOperation.GreaterThanOrEqual<T>, T>(x, y);

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if all elements of <paramref name="y"/> are greater than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="y"/> are greater than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="y"/> are greater than <paramref name="y"/>.</returns>
        public static bool GreaterThanOrEqualAll<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool> => LessThanOrEqualAll(y, x);
        #endregion

        #region GreaterThanOrEqualAny
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if any elements of <paramref name="x"/> are greater than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are greater than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are greater than <paramref name="y"/>.</returns>
        public static bool GreaterThanOrEqualAny<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            // The main loop early exits at the first false condition, so the TensorOperation
            // checks !(x >= y) and returns false on first equal. This means we want to negate
            // whatever the main loop returns as `true` means none are equal.

            TensorOperation.ValidateCompatibility(x, y);
            return !TensorOperation.Invoke<TensorOperation.GreaterThanOrEqualAny<T>, T>(x, y);
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if any elements of <paramref name="x"/> are greater than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are greater than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Value to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are greater than <paramref name="y"/>.</returns>
        public static bool GreaterThanOrEqualAny<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IComparisonOperators<T, T, bool> => !TensorOperation.Invoke<TensorOperation.GreaterThanOrEqualAny<T>, T>(x, y);

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if any elements of <paramref name="y"/> are greater than <paramref name="x"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="y"/> are greater than <paramref name="x"/>.
        /// </summary>
        /// <param name="y">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="x">Value to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="y"/> are greater than <paramref name="x"/>.</returns>
        public static bool GreaterThanOrEqualAny<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool> => LessThanOrEqualAny(y, x);
        #endregion

        #region LessThan
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see which elements of <paramref name="x"/> are less than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <returns>A <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/> and
        /// false if they are not.</returns>
        public static Tensor<bool> LessThan<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            TensorOperation.ValidateCompatibility(x, y, out Tensor<bool> destination);
            TensorOperation.Invoke<TensorOperation.LessThan<T>, T, bool>(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see which elements of <paramref name="x"/> are less than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="destination"></param>
        /// <returns>A <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/> and
        /// false if they are not.</returns>
        public static ref readonly TensorSpan<bool> LessThan<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<bool> destination)
            where T : IComparisonOperators<T, T, bool>
        {
            TensorOperation.ValidateCompatibility(x, y, destination);
            TensorOperation.Invoke<TensorOperation.LessThan<T>, T, bool>(x, y, destination);
            return ref destination;
        }

        /// <summary>
        /// Compares the elements of a <see cref="Tensor{T}"/> to see which elements are less than <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="Tensor{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static Tensor<bool> LessThan<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IComparisonOperators<T, T, bool>
        {
            Tensor<bool> destination = Tensor.Create<bool>(x.Lengths, false);
            LessThan(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Compares the elements of a <see cref="Tensor{T}"/> to see which elements are less than <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="Tensor{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <param name="destination"></param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static ref readonly TensorSpan<bool> LessThan<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<bool> destination)
            where T : IComparisonOperators<T, T, bool>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.LessThan<T>, T, bool>(x, y, destination);
            return ref destination;
        }

        /// <summary>
        /// Compares the elements of a <see cref="Tensor{T}"/> to see which elements are less than <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="Tensor{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static Tensor<bool> LessThan<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool> => GreaterThan(y, x);

        /// <summary>
        /// Compares the elements of a <see cref="Tensor{T}"/> to see which elements are less than <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="Tensor{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <param name="destination"></param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static ref readonly TensorSpan<bool> LessThan<T>(T x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<bool> destination)
            where T : IComparisonOperators<T, T, bool> => ref GreaterThan(y, x, destination);
        #endregion

        #region LessThanAll
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if all elements of <paramref name="x"/> are less than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are less than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are less than <paramref name="y"/>.</returns>
        public static bool LessThanAll<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            TensorOperation.ValidateCompatibility(x, y);
            return TensorOperation.Invoke<TensorOperation.LessThan<T>, T>(x, y);
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if all elements of <paramref name="x"/> are less than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are less than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second value to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are less than <paramref name="y"/>.</returns>
        public static bool LessThanAll<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IComparisonOperators<T, T, bool> => TensorOperation.Invoke<TensorOperation.LessThan<T>, T>(x, y);

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if all elements of <paramref name="y"/> are less than <paramref name="x"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="y"/> are less than <paramref name="x"/>.
        /// </summary>
        /// <param name="y">First value to compare.</param>
        /// <param name="x">Second value to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="y"/> are less than <paramref name="x"/>.</returns>
        public static bool LessThanAll<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool> => GreaterThanAll(y, x);
        #endregion

        #region LessThanAny
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if any elements of <paramref name="x"/> are less than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are less than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are less than <paramref name="y"/>.</returns>
        public static bool LessThanAny<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            // The main loop early exits at the first false condition, so the TensorOperation
            // checks !(x < y) and returns false on first equal. This means we want to negate
            // whatever the main loop returns as `true` means none are equal.

            TensorOperation.ValidateCompatibility(x, y);
            return !TensorOperation.Invoke<TensorOperation.LessThanAny<T>, T>(x, y);
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if any elements of <paramref name="x"/> are less than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are less than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second value to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are less than <paramref name="y"/>.</returns>
        public static bool LessThanAny<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IComparisonOperators<T, T, bool> => !TensorOperation.Invoke<TensorOperation.LessThanAny<T>, T>(x, y);

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if any elements of <paramref name="y"/> are less than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="y"/> are less than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First value to compare.</param>
        /// <param name="y">Second value to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="y"/> are less than <paramref name="y"/>.</returns>
        public static bool LessThanAny<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool> => GreaterThanAny(y, x);
        #endregion

        #region LessThanOrEqual
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see which elements of <paramref name="x"/> are less than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <returns>A <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/> and
        /// false if they are not.</returns>
        public static Tensor<bool> LessThanOrEqual<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            TensorOperation.ValidateCompatibility(x, y, out Tensor<bool> destination);
            TensorOperation.Invoke<TensorOperation.LessThanOrEqual<T>, T, bool>(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see which elements of <paramref name="x"/> are less than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="destination"></param>
        /// <returns>A <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/> and
        /// false if they are not.</returns>
        public static ref readonly TensorSpan<bool> LessThanOrEqual<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<bool> destination)
            where T : IComparisonOperators<T, T, bool>
        {
            TensorOperation.ValidateCompatibility(x, y, destination);
            TensorOperation.Invoke<TensorOperation.LessThanOrEqual<T>, T, bool>(x, y, destination);
            return ref destination;
        }

        /// <summary>
        /// Compares the elements of a <see cref="Tensor{T}"/> to see which elements are less than <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="Tensor{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static Tensor<bool> LessThanOrEqual<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IComparisonOperators<T, T, bool>
        {
            Tensor<bool> destination = Tensor.Create<bool>(x.Lengths, false);
            LessThanOrEqual(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Compares the elements of a <see cref="Tensor{T}"/> to see which elements are less than <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="Tensor{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <param name="destination"></param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static ref readonly TensorSpan<bool> LessThanOrEqual<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<bool> destination)
            where T : IComparisonOperators<T, T, bool>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.LessThanOrEqual<T>, T, bool>(x, y, destination);
            return ref destination;
        }

        /// <summary>
        /// Compares the elements of a <see cref="Tensor{T}"/> to see which elements are less than <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="Tensor{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static Tensor<bool> LessThanOrEqual<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool> => GreaterThanOrEqual(y, x);

        /// <summary>
        /// Compares the elements of a <see cref="Tensor{T}"/> to see which elements are less than <paramref name="y"/>.
        /// It returns a <see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not."/>
        /// </summary>
        /// <param name="x"><see cref="Tensor{T}"/> to compare.</param>
        /// <param name="y"><typeparamref name="T"/> to compare against <paramref name="x"/>.</param>
        /// <param name="destination"></param>
        /// <returns><see cref="Tensor{Boolean}"/> where the value is true if the elements in <paramref name="x"/> are less than <paramref name="y"/>
        /// and false if they are not.</returns>
        public static ref readonly TensorSpan<bool> LessThanOrEqual<T>(T x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<bool> destination)
            where T : IComparisonOperators<T, T, bool> => ref GreaterThanOrEqual(y, x, destination);
        #endregion

        #region LessThanOrEqualAll
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if all elements of <paramref name="x"/> are less than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are less than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are less than <paramref name="y"/>.</returns>
        public static bool LessThanOrEqualAll<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            TensorOperation.ValidateCompatibility(x, y);
            return TensorOperation.Invoke<TensorOperation.LessThanOrEqual<T>, T>(x, y);
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if all elements of <paramref name="x"/> are less than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are less than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second value to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="x"/> are less than <paramref name="y"/>.</returns>
        public static bool LessThanOrEqualAll<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IComparisonOperators<T, T, bool> => TensorOperation.Invoke<TensorOperation.LessThanOrEqual<T>, T>(x, y);

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if all elements of <paramref name="y"/> are less than <paramref name="x"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if all elements in <paramref name="y"/> are less than <paramref name="x"/>.
        /// </summary>
        /// <param name="y">First value to compare.</param>
        /// <param name="x">Second value to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if all elements in <paramref name="y"/> are less than <paramref name="x"/>.</returns>
        public static bool LessThanOrEqualAll<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool> => GreaterThanOrEqualAll(y, x);
        #endregion

        #region LessThanOrEqualAny
        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if any elements of <paramref name="x"/> are less than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are less than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second <see cref="ReadOnlyTensorSpan{T}"/> to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are less than <paramref name="y"/>.</returns>
        public static bool LessThanOrEqualAny<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool>
        {
            // The main loop early exits at the first false condition, so the TensorOperation
            // checks !(x <= y) and returns false on first equal. This means we want to negate
            // whatever the main loop returns as `true` means none are equal.

            TensorOperation.ValidateCompatibility(x, y);
            return !TensorOperation.Invoke<TensorOperation.LessThanOrEqualAny<T>, T>(x, y);
        }

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if any elements of <paramref name="x"/> are less than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are less than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First <see cref="ReadOnlyTensorSpan{T}"/> to compare.</param>
        /// <param name="y">Second value to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="x"/> are less than <paramref name="y"/>.</returns>
        public static bool LessThanOrEqualAny<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IComparisonOperators<T, T, bool> => !TensorOperation.Invoke<TensorOperation.LessThanOrEqualAny<T>, T>(x, y);

        /// <summary>
        /// Compares the elements of two <see cref="ReadOnlyTensorSpan{T}"/> to see if any elements of <paramref name="y"/> are less than <paramref name="y"/>.
        /// If the shapes are not the same, the tensors are broadcasted to the smallest broadcastable size before they are compared.
        /// It returns a <see cref="bool"/> where the value is true if any elements in <paramref name="y"/> are less than <paramref name="y"/>.
        /// </summary>
        /// <param name="x">First value to compare.</param>
        /// <param name="y">Second value to compare against.</param>
        /// <returns><see cref="bool"/> where the value is true if any elements in <paramref name="y"/> are less than <paramref name="y"/>.</returns>
        public static bool LessThanOrEqualAny<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IComparisonOperators<T, T, bool> => GreaterThanOrEqualAny(y, x);
        #endregion

        #region Permute

        /// <summary>
        /// Swaps the dimensions of the <paramref name="tensor"/> tensor according to the <paramref name="dimensions"/> parameter.
        /// If <paramref name="tensor"/> is a 1D tensor, it will return <paramref name="tensor"/>. Otherwise it creates a new <see cref="Tensor{T}"/>
        /// with the new axis ordering by allocating new memory.
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/></param>
        /// <param name="dimensions"><see cref="ReadOnlySpan{T}"/> with the new axis ordering.</param>
        public static Tensor<T> PermuteDimensions<T>(this Tensor<T> tensor, ReadOnlySpan<int> dimensions)
        {
            if (tensor.Rank == 1)
            {
                return tensor;
            }
            else
            {
                if (!dimensions.IsEmpty && dimensions.Length != tensor.Lengths.Length)
                    ThrowHelper.ThrowArgument_PermuteAxisOrder();

                scoped Span<nint> newLengths = TensorOperation.RentedBuffer.CreateUninitialized(tensor.Rank, out TensorOperation.RentedBuffer<nint> lengthsRentedBuffer);
                scoped Span<nint> newStrides = TensorOperation.RentedBuffer.CreateUninitialized(tensor.Rank, out TensorOperation.RentedBuffer<nint> stridesRentedBuffer);
                scoped Span<int> newLinearOrder = TensorOperation.RentedBuffer.CreateUninitialized(tensor.Rank, out TensorOperation.RentedBuffer<int> linearOrderRentedBuffer);

                Tensor<T> outTensor;

                if (dimensions.IsEmpty)
                {
                    for (int i = 0; i < tensor.Rank; i++)
                    {
                        newLengths[i] = tensor.Lengths[tensor.Rank - 1 - i];
                        newStrides[i] = tensor.Strides[tensor.Rank - 1 - i];
                        newLinearOrder[i] = tensor._shape.LinearRankOrder[tensor.Rank - 1 - i];
                    }
                }
                else
                {
                    for (int i = 0; i < dimensions.Length; i++)
                    {
                        if (dimensions[i] >= tensor.Lengths.Length || dimensions[i] < 0)
                        {
                            ThrowHelper.ThrowArgument_InvalidDimension();
                        }
                        newLengths[i] = tensor.Lengths[dimensions[i]];
                        newStrides[i] = tensor.Strides[dimensions[i]];
                        newLinearOrder[i] = tensor._shape.LinearRankOrder[dimensions[i]];
                    }
                }
                outTensor = new Tensor<T>(tensor._values, tensor._start, newLengths, newStrides, newLinearOrder);

                lengthsRentedBuffer.Dispose();
                stridesRentedBuffer.Dispose();
                linearOrderRentedBuffer.Dispose();

                return outTensor;
            }
        }
        #endregion

        #region Reshape
        /// <summary>
        /// Reshapes the <paramref name="tensor"/> tensor to the specified <paramref name="lengths"/>. If one of the lengths is -1, it will be calculated automatically.
        /// Does not change the length of the underlying memory nor does it allocate new memory. If the new shape is not compatible with the old shape,
        /// an exception is thrown.
        /// </summary>
        /// <param name="tensor"><see cref="Tensor{T}"/> you want to reshape.</param>
        /// <param name="lengths"><see cref="ReadOnlySpan{T}"/> with the new dimensions.</param>
        public static Tensor<T> Reshape<T>(this Tensor<T> tensor, ReadOnlySpan<nint> lengths)
        {
            if (tensor.Lengths.SequenceEqual(lengths))
                return tensor;

            if (!tensor.IsDense && !tensor.Strides.Contains(0))
            {
                ThrowHelper.ThrowArgument_CannotReshapeNonContiguousOrDense();
            }

            nint[] newLengths = lengths.ToArray();
            // Calculate wildcard info.
            int wildcardIndex = lengths.IndexOf(-1);
            if (wildcardIndex >= 0)
            {
                if (lengths.Count(-1) > 1)
                    ThrowHelper.ThrowArgument_OnlyOneWildcard();
                nint tempTotal = tensor.FlattenedLength;
                for (int i = 0; i < lengths.Length; i++)
                {
                    if (lengths[i] != -1)
                    {
                        tempTotal /= lengths[i];
                    }
                }
                newLengths[wildcardIndex] = tempTotal;
            }

            nint tempLinear = TensorPrimitives.Product(newLengths);
            if (tempLinear != tensor.FlattenedLength)
                ThrowHelper.ThrowArgument_InvalidReshapeDimensions();

            nint[] strides;

            // If all our strides are 0 we can reshape however we like and keep all new strides at 0
            if (!tensor.Strides.ContainsAnyExcept(0))
            {
                strides = new nint[newLengths.Length];
            }
            // If we contain a 0 stride we can only add dimensions of length 1.
            else if (tensor.Strides.Contains(0))
            {
                List<nint> origStrides = new List<nint>(tensor.Strides.ToArray());
                int lengthOffset = 0;
                for (int i = 0; i < newLengths.Length; i++)
                {
                    if (lengthOffset < tensor.Rank && newLengths[i] == tensor.Lengths[lengthOffset])
                        lengthOffset++;
                    else if (newLengths[i] == 1)
                    {
                        if (lengthOffset == tensor.Rank)
                            origStrides.Add(tensor.Strides[lengthOffset - 1]);
                        else
                            origStrides.Insert(i, tensor.Strides[i] * tensor.Lengths[i]);
                    }
                    else
                        ThrowHelper.ThrowArgument_InvalidReshapeDimensions();
                }
                strides = origStrides.ToArray();
            }
            else
                strides = [];

            return new Tensor<T>(tensor._values, tensor._start, lengths, strides);
        }

        /// <summary>
        /// Reshapes the <paramref name="tensor"/> tensor to the specified <paramref name="lengths"/>. If one of the lengths is -1, it will be calculated automatically.
        /// Does not change the length of the underlying memory nor does it allocate new memory. If the new shape is not compatible with the old shape,
        /// an exception is thrown.
        /// </summary>
        /// <param name="tensor"><see cref="TensorSpan{T}"/> you want to reshape.</param>
        /// <param name="lengths"><see cref="ReadOnlySpan{T}"/> with the new dimensions.</param>
        public static TensorSpan<T> Reshape<T>(this scoped in TensorSpan<T> tensor, scoped ReadOnlySpan<nint> lengths)
        {
            if (tensor.Lengths.SequenceEqual(lengths))
                return tensor;

            if (!tensor.IsDense && !tensor.Strides.Contains(0))
            {
                ThrowHelper.ThrowArgument_CannotReshapeNonContiguousOrDense();
            }

            nint[] newLengths = lengths.ToArray();
            int wildcardIndex = lengths.IndexOf(-1);
            if (wildcardIndex >= 0)
            {
                if (lengths.Count(-1) > 1)
                    ThrowHelper.ThrowArgument_OnlyOneWildcard();
                nint tempTotal = tensor.FlattenedLength;
                for (int i = 0; i < lengths.Length; i++)
                {
                    if (lengths[i] != -1)
                    {
                        tempTotal /= lengths[i];
                    }
                }
                newLengths[wildcardIndex] = tempTotal;

            }

            nint tempLinear = TensorPrimitives.Product(newLengths);
            if (tempLinear != tensor.FlattenedLength)
                ThrowHelper.ThrowArgument_InvalidReshapeDimensions();

            nint[] strides;

            // If all our strides are 0 we can reshape however we like and keep all new strides at 0
            if (!tensor.Strides.ContainsAnyExcept(0))
            {
                strides = new nint[newLengths.Length];
            }
            // If we contain a 0 stride we can only add dimensions of length 1.
            else if (tensor.Strides.Contains(0))
            {
                List<nint> origStrides = new List<nint>(tensor.Strides.ToArray());
                int lengthOffset = 0;
                for (int i = 0; i < newLengths.Length; i++)
                {
                    if (lengthOffset < tensor.Rank && newLengths[i] == tensor.Lengths[lengthOffset])
                    {
                        lengthOffset++;
                    }
                    else if (newLengths[i] == 1)
                    {
                        if (lengthOffset == tensor.Rank)
                            origStrides.Add(tensor.Strides[lengthOffset - 1]);
                        else
                            origStrides.Insert(i, tensor.Strides[i] * tensor.Lengths[i]);
                    }
                    else
                        ThrowHelper.ThrowArgument_InvalidReshapeDimensions();
                }
                strides = origStrides.ToArray();
            }
            else
                strides = [];

            TensorSpan<T> output = new TensorSpan<T>(ref tensor._reference, tensor._shape.LinearLength, newLengths, strides);
            return output;
        }

        /// <summary>
        /// Reshapes the <paramref name="tensor"/> tensor to the specified <paramref name="lengths"/>. If one of the lengths is -1, it will be calculated automatically.
        /// Does not change the length of the underlying memory nor does it allocate new memory. If the new shape is not compatible with the old shape,
        /// an exception is thrown.
        /// </summary>
        /// <param name="tensor"><see cref="TensorSpan{T}"/> you want to reshape.</param>
        /// <param name="lengths"><see cref="ReadOnlySpan{T}"/> with the new dimensions.</param>
        public static ReadOnlyTensorSpan<T> Reshape<T>(this scoped in ReadOnlyTensorSpan<T> tensor, scoped ReadOnlySpan<nint> lengths)
        {
            if (tensor.Lengths.SequenceEqual(lengths))
                return tensor;

            if (!tensor.IsDense && !tensor.Strides.Contains(0))
            {
                ThrowHelper.ThrowArgument_CannotReshapeNonContiguousOrDense();
            }

            nint[] newLengths = lengths.ToArray();
            // Calculate wildcard info.
            int wildcardIndex = lengths.IndexOf(-1);
            if (wildcardIndex >= 0)
            {
                if (lengths.Count(-1) > 1)
                    ThrowHelper.ThrowArgument_OnlyOneWildcard();
                nint tempTotal = tensor.FlattenedLength;
                for (int i = 0; i < lengths.Length; i++)
                {
                    if (lengths[i] != -1)
                    {
                        tempTotal /= lengths[i];
                    }
                }
                newLengths[wildcardIndex] = tempTotal;

            }

            nint tempLinear = TensorPrimitives.Product(newLengths);
            if (tempLinear != tensor.FlattenedLength)
                ThrowHelper.ThrowArgument_InvalidReshapeDimensions();

            nint[] strides;

            // If all our strides are 0 we can reshape however we like and keep all new strides at 0
            if (!tensor.Strides.ContainsAnyExcept(0))
            {
                strides = new nint[newLengths.Length];
            }
            // If we contain a 0 stride we can only add dimensions of length 1.
            else if (tensor.Strides.Contains(0))
            {
                List<nint> origStrides = new List<nint>(tensor.Strides.ToArray());
                int lengthOffset = 0;
                for (int i = 0; i < newLengths.Length; i++)
                {
                    if (lengthOffset < tensor.Rank && newLengths[i] == tensor.Lengths[lengthOffset])
                        lengthOffset++;
                    else if (newLengths[i] == 1)
                    {
                        if (lengthOffset == tensor.Rank)
                            origStrides.Add(tensor.Strides[lengthOffset - 1]);
                        else
                            origStrides.Insert(i, tensor.Strides[i] * tensor.Lengths[i]);
                    }
                    else
                        ThrowHelper.ThrowArgument_InvalidReshapeDimensions();
                }
                strides = origStrides.ToArray();
            }
            else
                strides = [];

            ReadOnlyTensorSpan<T> output = new ReadOnlyTensorSpan<T>(ref tensor._reference, tensor._shape.LinearLength, newLengths, strides);
            return output;
        }
        #endregion

        #region Resize
        /// <summary>
        /// Creates a new <see cref="Tensor{T}"/>, allocates new memory, and copies the data from <paramref name="tensor"/>. If the final shape is smaller all data after
        /// that point is ignored.
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="lengths"><see cref="ReadOnlySpan{T}"/> of the desired new shape.</param>
        public static Tensor<T> Resize<T>(Tensor<T> tensor, ReadOnlySpan<nint> lengths)
        {
            nint newSize = TensorPrimitives.Product(lengths);
            T[] values = tensor.IsPinned ? GC.AllocateArray<T>((int)newSize) : (new T[newSize]);
            Tensor<T> output = Tensor.Create(values, 0, lengths, []);
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref tensor.AsTensorSpan()._reference, tensor._start), (int)tensor._values.Length - tensor._start);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref output.AsTensorSpan()._reference, (int)output.FlattenedLength);
            if (newSize >= span.Length)
                span.CopyTo(ospan);
            else
                span.Slice(0, ospan.Length).CopyTo(ospan);

            return output;
        }

        /// <summary>
        /// Copies the data from <paramref name="tensor"/>. If the final shape is smaller all data after that point is ignored.
        /// If the final shape is bigger it is filled with 0s.
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="destination">Destination <see cref="TensorSpan{T}"/> with the desired new shape.</param>
        public static void ResizeTo<T>(scoped in Tensor<T> tensor, in TensorSpan<T> destination)
        {
            ResizeTo(tensor.AsReadOnlyTensorSpan(), destination);
        }

        /// <summary>
        /// Copies the data from <paramref name="tensor"/>. If the final shape is smaller all data after that point is ignored.
        /// If the final shape is bigger it is filled with 0s.
        /// </summary>
        /// <param name="tensor">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="destination">Destination <see cref="TensorSpan{T}"/> with the desired new shape.</param>
        public static void ResizeTo<T>(scoped in TensorSpan<T> tensor, in TensorSpan<T> destination)
        {
            ResizeTo(tensor.AsReadOnlyTensorSpan(), destination);
        }

        /// <summary>
        /// Copies the data from <paramref name="tensor"/>. If the final shape is smaller all data after that point is ignored.
        /// If the final shape is bigger it is filled with 0s.
        /// </summary>
        /// <param name="tensor">Input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination">Destination <see cref="TensorSpan{T}"/> with the desired new shape.</param>
        public static void ResizeTo<T>(scoped in ReadOnlyTensorSpan<T> tensor, in TensorSpan<T> destination)
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref tensor._reference, (int)tensor._shape.LinearLength);
            Span<T> ospan = MemoryMarshal.CreateSpan(ref destination._reference, (int)destination._shape.LinearLength);
            if (ospan.Length >= span.Length)
                span.CopyTo(ospan);
            else
                span.Slice(0, ospan.Length).CopyTo(ospan);
        }
        #endregion

        #region Reverse
        /// <summary>
        /// Reverse the order of elements in the <paramref name="tensor"/>. The shape of the tensor is preserved, but the elements are reordered.
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Reverse<T>(in ReadOnlyTensorSpan<T> tensor)
        {
            Tensor<T> output = Tensor.Create<T>(tensor.Lengths);
            ReverseDimension(tensor, output, -1);

            return output;
        }

        /// <summary>
        /// Reverse the order of elements in the <paramref name="tensor"/> along the given dimension. The shape of the tensor is preserved, but the elements are reordered.
        /// <paramref name="dimension"/> defaults to -1 when not provided, which reverses the entire tensor.
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="dimension">dimension along which to reverse over. -1 will reverse over all of the dimensions of the left tensor.</param>
        public static Tensor<T> ReverseDimension<T>(in ReadOnlyTensorSpan<T> tensor, int dimension)
        {
            Tensor<T> output = Tensor.Create<T>(tensor.Lengths);
            ReverseDimension(tensor, output, dimension);

            return output;
        }

        /// <summary>
        /// Reverse the order of elements in the <paramref name="tensor"/>. The shape of the tensor is preserved, but the elements are reordered.
        /// </summary>
        /// <param name="tensor">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Reverse<T>(scoped in ReadOnlyTensorSpan<T> tensor, in TensorSpan<T> destination)
        {
            return ref ReverseDimension(tensor, destination, -1);
        }

        /// <summary>
        /// Reverse the order of elements in the <paramref name="tensor"/> along the given axis. The shape of the tensor is preserved, but the elements are reordered.
        /// <paramref name="dimension"/> defaults to -1 when not provided, which reverses the entire span.
        /// </summary>
        /// <param name="tensor">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        /// <param name="dimension">dimension along which to reverse over. -1 will reverse over all of the dimensions of the left tensor.</param>
        public static ref readonly TensorSpan<T> ReverseDimension<T>(scoped in ReadOnlyTensorSpan<T> tensor, in TensorSpan<T> destination, int dimension)
        {
            // When the dimension is -1, its just a straight reverse copy.
            if (dimension == -1)
            {
                TensorOperation.ValidateCompatibility(tensor, destination);
                TensorOperation.ReverseInvoke<TensorOperation.CopyTo<T>, T, T>(tensor, destination);
            }
            // With any other dimension, we need to copy the data in reverse order based on the provided dimension.
            else
            {
                TensorOperation.ValidateCompatibility(tensor, destination);
                Span<NRange> srcIndexes = TensorOperation.RentedBuffer.CreateUninitialized(tensor.Rank, out TensorOperation.RentedBuffer<NRange> srcIndexesRentedBuffer);
                Span<NRange> dstIndexes = TensorOperation.RentedBuffer.CreateUninitialized(tensor.Rank, out TensorOperation.RentedBuffer<NRange> dstIndexesRentedBuffer);

                for (int i = 0; i < srcIndexes.Length; i++)
                {
                    srcIndexes[i] = NRange.All;
                    dstIndexes[i] = NRange.All;
                }

                for (int i = (int)tensor.Lengths[dimension]; i > 0; i--)
                {
                    srcIndexes[dimension] = new NRange(i - 1, i);
                    dstIndexes[dimension] = new NRange(tensor.Lengths[dimension] - i, tensor.Lengths[dimension] - i + 1);
                    TensorOperation.Invoke<TensorOperation.CopyTo<T>, T, T>(tensor.Slice(srcIndexes), destination.Slice(dstIndexes));
                }
                srcIndexesRentedBuffer.Dispose();
                dstIndexesRentedBuffer.Dispose();
            }

            return ref destination;
        }
        #endregion

        #region SequenceEqual
        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements using IEquatable{T}.Equals(T).
        /// </summary>
        public static bool SequenceEqual<T>(this scoped in TensorSpan<T> tensor, scoped in ReadOnlyTensorSpan<T> other)
            where T : IEquatable<T>?
        {
            return tensor.FlattenedLength == other.FlattenedLength
                && tensor._shape.LinearLength == other._shape.LinearLength
                && tensor.Lengths.SequenceEqual(other.Lengths)
                && MemoryMarshal.CreateReadOnlySpan(in tensor.GetPinnableReference(), (int)tensor._shape.LinearLength).SequenceEqual(MemoryMarshal.CreateReadOnlySpan(in other.GetPinnableReference(), (int)other._shape.LinearLength));
        }

        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements using IEquatable{T}.Equals(T).
        /// </summary>
        public static bool SequenceEqual<T>(this scoped in ReadOnlyTensorSpan<T> tensor, scoped in ReadOnlyTensorSpan<T> other)
            where T : IEquatable<T>?
        {
            return tensor.FlattenedLength == other.FlattenedLength
                && tensor._shape.LinearLength == other._shape.LinearLength
                && tensor.Lengths.SequenceEqual(other.Lengths)
                && MemoryMarshal.CreateReadOnlySpan(in tensor.GetPinnableReference(), (int)tensor._shape.LinearLength).SequenceEqual(MemoryMarshal.CreateReadOnlySpan(in other.GetPinnableReference(), (int)other._shape.LinearLength));
        }
        #endregion

        #region SetSlice
        /// <summary>
        /// Sets a slice of the given <paramref name="tensor"/> with the provided <paramref name="values"/> for the given <paramref name="ranges"/>
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="values">The values you want to set in the <paramref name="tensor"/>.</param>
        /// <param name="ranges">The ranges you want to set.</param>
        public static Tensor<T> SetSlice<T>(this Tensor<T> tensor, in ReadOnlyTensorSpan<T> values, params ReadOnlySpan<NRange> ranges)
        {
            tensor.AsTensorSpan().SetSlice(values, ranges);
            return tensor;
        }

        /// <summary>
        /// Sets a slice of the given <paramref name="tensor"/> with the provided <paramref name="values"/> for the given <paramref name="ranges"/>
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="values">The values you want to set in the <paramref name="tensor"/>.</param>
        /// <param name="ranges">The ranges you want to set.</param>
        public static ref readonly TensorSpan<T> SetSlice<T>(this in TensorSpan<T> tensor, scoped in ReadOnlyTensorSpan<T> values, params scoped ReadOnlySpan<NRange> ranges)
        {
            if (ranges.IsEmpty)
            {
                values.CopyTo(tensor);
            }
            else
            {
                values.CopyTo(tensor.Slice(ranges));
            }
            return ref tensor;
        }
        #endregion

        #region Split
        /// <summary>
        /// Split a <see cref="Tensor{T}"/> into <paramref name="splitCount"/> along the given <paramref name="dimension"/>. If the tensor cannot be split
        /// evenly on the given <paramref name="dimension"/> an exception is thrown.
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="splitCount">How many times to split the <paramref name="tensor"/></param>
        /// <param name="dimension">The axis to split on.</param>
        public static Tensor<T>[] Split<T>(scoped in ReadOnlyTensorSpan<T> tensor, int splitCount, nint dimension)
        {
            if (dimension < 0 || dimension >= tensor.Rank)
                ThrowHelper.ThrowArgument_AxisLargerThanRank();
            if (tensor.Lengths[(int)dimension] % splitCount != 0)
                ThrowHelper.ThrowArgument_SplitNotSplitEvenly();

            Tensor<T>[] outputs = new Tensor<T>[splitCount];

            nint totalToCopy = tensor.FlattenedLength / splitCount;

            nint[] newShape = tensor.Lengths.ToArray();
            nint splitLength = newShape[dimension] / splitCount;
            newShape[dimension] = splitLength;

            scoped Span<NRange> sliceDims = TensorOperation.RentedBuffer.CreateUninitialized(tensor.Rank, out TensorOperation.RentedBuffer<NRange> lengthsRentedBuffer);
            for (int i = 0; i < sliceDims.Length; i++)
            {
                sliceDims[i] = NRange.All;
            }
            nint start = 0;
            for (int i = 0; i < outputs.Length; i++)
            {
                sliceDims[(int)dimension] = new NRange(start, start + splitLength);
                T[] values = new T[(int)totalToCopy];
                outputs[i] = new Tensor<T>(values, 0, newShape, [], tensor._shape.LinearRankOrder);

                tensor.Slice(sliceDims).CopyTo(outputs[i]);
                start += splitLength;
            }

            lengthsRentedBuffer.Dispose();
            return outputs;
        }
        #endregion

        #region Squeeze
        /// <summary>
        /// Removes all dimensions of length one from the <paramref name="tensor"/>.
        /// </summary>
        /// <param name="tensor">The <see cref="Tensor{T}"/> to remove all dimensions of length 1.</param>
        public static Tensor<T> Squeeze<T>(this Tensor<T> tensor)
        {
            return SqueezeDimension(tensor, -1);
        }

        /// <summary>
        /// Removes axis of length one from the <paramref name="tensor"/> for the given <paramref name="dimension"/>.
        /// If the dimension is not of length one it will throw an exception.
        /// </summary>
        /// <param name="tensor">The <see cref="Tensor{T}"/> to remove dimension of length 1.</param>
        /// <param name="dimension">The dimension to remove.</param>
        public static Tensor<T> SqueezeDimension<T>(this Tensor<T> tensor, int dimension)
        {
            if (dimension >= tensor.Rank || dimension < -1)
                ThrowHelper.ThrowArgument_AxisLargerThanRank();

            scoped Span<nint> lengths = TensorOperation.RentedBuffer.CreateUninitialized(tensor.Rank, out TensorOperation.RentedBuffer<nint> lengthsRentedBuffer);
            scoped Span<nint> strides = TensorOperation.RentedBuffer.CreateUninitialized(tensor.Rank, out TensorOperation.RentedBuffer<nint> stridesRentedBuffer);
            scoped Span<int> strideOrder = TensorOperation.RentedBuffer.CreateUninitialized(tensor.Rank, out TensorOperation.RentedBuffer<int> stridesOrderRentedBuffer);
            int newRank = 0;
            int index = 0;

            if (dimension == -1)
            {
                int removalCount = tensor.Lengths.Count(1);
                int removedIndex = 0;
                Span<int> removed = TensorOperation.RentedBuffer.CreateUninitialized(removalCount, out TensorOperation.RentedBuffer<int> removedRentedBuffer);

                for (int i = 0; i < tensor.Lengths.Length; i++)
                {
                    if (tensor.Lengths[i] != 1)
                    {
                        lengths[index] = tensor.Lengths[i];
                        strides[index] = tensor.Strides[i];
                        newRank++;
                        strideOrder[index++] = tensor._shape.LinearRankOrder[i];
                    }
                    else
                    {
                        removed[removedIndex++] = tensor._shape.LinearRankOrder[i];
                    }
                }
                SqueezeHelper(removed, strideOrder);
                removedRentedBuffer.Dispose();
            }
            else
            {
                if (tensor.Lengths[dimension] != 1)
                {
                    ThrowHelper.ThrowArgument_InvalidSqueezeAxis();
                }
                int removed = default;
                for (int i = 0; i < tensor.Lengths.Length; i++)
                {
                    if (i != dimension)
                    {
                        lengths[index] = tensor.Lengths[i];
                        strides[index] = tensor.Strides[i];
                        newRank++;
                        strideOrder[index++] = tensor._shape.LinearRankOrder[i];
                    }
                    else
                    {
                        removed = tensor._shape.LinearRankOrder[i];
                    }
                }
                SqueezeHelper(removed, strideOrder);
            }

            Tensor<T> output = new Tensor<T>(tensor._values, tensor._start, lengths[..newRank], strides[..newRank], strideOrder[..newRank]);

            lengthsRentedBuffer.Dispose();
            stridesRentedBuffer.Dispose();
            stridesOrderRentedBuffer.Dispose();

            return output;
        }

        /// <summary>
        /// Removes all dimensions of length one from the <paramref name="tensor"/>.
        /// </summary>
        /// <param name="tensor">The <see cref="TensorSpan{T}"/> to remove all dimensions of length 1.</param>
        public static TensorSpan<T> Squeeze<T>(this scoped in TensorSpan<T> tensor)
        {
            return SqueezeDimension(tensor, -1);
        }

        /// <summary>
        /// Removes axis of length one from the <paramref name="tensor"/> for the given <paramref name="dimension"/>.
        /// If the dimension is not of length one it will throw an exception.
        /// </summary>
        /// <param name="tensor">The <see cref="TensorSpan{T}"/> to remove dimension of length 1.</param>
        /// <param name="dimension">The dimension to remove.</param>
        public static TensorSpan<T> SqueezeDimension<T>(this scoped in TensorSpan<T> tensor, int dimension)
        {
            if (dimension >= tensor.Rank || dimension < -1)
                ThrowHelper.ThrowArgument_AxisLargerThanRank();

            scoped Span<nint> lengths = TensorOperation.RentedBuffer.CreateUninitialized(tensor.Rank, out TensorOperation.RentedBuffer<nint> lengthsRentedBuffer);
            scoped Span<nint> strides = TensorOperation.RentedBuffer.CreateUninitialized(tensor.Rank, out TensorOperation.RentedBuffer<nint> stridesRentedBuffer);
            scoped Span<int> strideOrder = TensorOperation.RentedBuffer.CreateUninitialized(tensor.Rank, out TensorOperation.RentedBuffer<int> stridesOrderRentedBuffer);
            int newRank = 0;
            int index = 0;

            if (dimension == -1)
            {
                int removalCount = tensor.Lengths.Count(1);
                int removedIndex = 0;
                Span<int> removed = TensorOperation.RentedBuffer.CreateUninitialized(removalCount, out TensorOperation.RentedBuffer<int> removedRentedBuffer);

                for (int i = 0; i < tensor.Lengths.Length; i++)
                {
                    if (tensor.Lengths[i] != 1)
                    {
                        lengths[index] = tensor.Lengths[i];
                        strides[index] = tensor.Strides[i];
                        newRank++;
                        strideOrder[index++] = tensor._shape.LinearRankOrder[i];
                    }
                    else
                    {
                        removed[removedIndex++] = tensor._shape.LinearRankOrder[i];
                    }
                }
                SqueezeHelper(removed, strideOrder);
                removedRentedBuffer.Dispose();
            }
            else
            {
                if (tensor.Lengths[dimension] != 1)
                {
                    ThrowHelper.ThrowArgument_InvalidSqueezeAxis();
                }
                int removed = default;
                for (int i = 0; i < tensor.Lengths.Length; i++)
                {
                    if (i != dimension)
                    {
                        lengths[index] = tensor.Lengths[i];
                        strides[index] = tensor.Strides[i];
                        newRank++;
                        strideOrder[index++] = tensor._shape.LinearRankOrder[i];
                    }
                    else
                    {
                        removed = tensor._shape.LinearRankOrder[i];
                    }
                }
                SqueezeHelper(removed, strideOrder);
            }

            TensorSpan<T> output = new TensorSpan<T>(ref tensor._reference, tensor._shape.LinearLength, lengths[..newRank], strides[..newRank], strideOrder[..newRank]);

            lengthsRentedBuffer.Dispose();
            stridesRentedBuffer.Dispose();
            stridesOrderRentedBuffer.Dispose();

            return output;
        }

        /// <summary>
        /// Removes all dimensions of length one from the <paramref name="tensor"/>.
        /// </summary>
        /// <param name="tensor">The <see cref="ReadOnlyTensorSpan{T}"/> to remove all dimensions of length 1.</param>
        public static ReadOnlyTensorSpan<T> Squeeze<T>(this scoped in ReadOnlyTensorSpan<T> tensor)
        {
            return SqueezeDimension(tensor, -1);
        }

        /// <summary>
        /// Removes axis of length one from the <paramref name="tensor"/> for the given <paramref name="dimension"/>.
        /// If the dimension is not of length one it will throw an exception.
        /// </summary>
        /// <param name="tensor">The <see cref="ReadOnlyTensorSpan{T}"/> to remove dimension of length 1.</param>
        /// <param name="dimension">The dimension to remove.</param>
        public static ReadOnlyTensorSpan<T> SqueezeDimension<T>(this scoped in ReadOnlyTensorSpan<T> tensor, int dimension)
        {
            if (dimension >= tensor.Rank || dimension < -1)
                ThrowHelper.ThrowArgument_AxisLargerThanRank();

            scoped Span<nint> lengths = TensorOperation.RentedBuffer.CreateUninitialized(tensor.Rank, out TensorOperation.RentedBuffer<nint> lengthsRentedBuffer);
            scoped Span<nint> strides = TensorOperation.RentedBuffer.CreateUninitialized(tensor.Rank, out TensorOperation.RentedBuffer<nint> stridesRentedBuffer);
            scoped Span<int> strideOrder = TensorOperation.RentedBuffer.CreateUninitialized(tensor.Rank, out TensorOperation.RentedBuffer<int> stridesOrderRentedBuffer);
            int newRank = 0;
            int index = 0;

            if (dimension == -1)
            {
                int removalCount = tensor.Lengths.Count(1);
                int removedIndex = 0;
                Span<int> removed = TensorOperation.RentedBuffer.CreateUninitialized(removalCount, out TensorOperation.RentedBuffer<int> removedRentedBuffer);

                for (int i = 0; i < tensor.Lengths.Length; i++)
                {
                    if (tensor.Lengths[i] != 1)
                    {
                        lengths[index] = tensor.Lengths[i];
                        strides[index] = tensor.Strides[i];
                        newRank++;
                        strideOrder[index++] = tensor._shape.LinearRankOrder[i];
                    }
                    else
                    {
                        removed[removedIndex++] = tensor._shape.LinearRankOrder[i];
                    }
                }
                SqueezeHelper(removed, strideOrder);
                removedRentedBuffer.Dispose();
            }
            else
            {
                if (tensor.Lengths[dimension] != 1)
                {
                    ThrowHelper.ThrowArgument_InvalidSqueezeAxis();
                }
                int removed = default;
                for (int i = 0; i < tensor.Lengths.Length; i++)
                {
                    if (i != dimension)
                    {
                        lengths[index] = tensor.Lengths[i];
                        strides[index] = tensor.Strides[i];
                        newRank++;
                        strideOrder[index++] = tensor._shape.LinearRankOrder[i];
                    }
                    else
                    {
                        removed = tensor._shape.LinearRankOrder[i];
                    }
                }
                SqueezeHelper(removed, strideOrder);
            }

            ReadOnlyTensorSpan<T> output =  new ReadOnlyTensorSpan<T>(ref tensor._reference, tensor._shape.LinearLength, lengths[..newRank], strides[..newRank], strideOrder[..newRank]);

            lengthsRentedBuffer.Dispose();
            stridesRentedBuffer.Dispose();
            stridesOrderRentedBuffer.Dispose();

            return output;
        }

        internal static void SqueezeHelper(scoped in Span<int> removed, scoped in Span<int> strideOrder)
        {
            for (int i = 0; i < strideOrder.Length; i++)
            {
                for (int j = removed.Length - 1; j >= 0; j--)
                {
                    if (strideOrder[i] > removed[j])
                    {
                        strideOrder[i]--;
                    }
                }
            }
        }

        internal static void SqueezeHelper(int removed, scoped in Span<int> strideOrder)
        {
            for (int i = 0; i < strideOrder.Length; i++)
            {
                if (strideOrder[i] > removed)
                {
                    strideOrder[i]--;
                }
            }
        }
        #endregion

        #region Stack
        /// <summary>
        /// Join multiple <see cref="Tensor{T}"/> along a new dimension that is added at position 0. All tensors must have the same shape.
        /// </summary>
        /// <param name="tensors">Input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Stack<T>(params ReadOnlySpan<Tensor<T>> tensors)
        {
            return StackAlongDimension(0, tensors);
        }

        /// <summary>
        /// Join multiple <see cref="Tensor{T}"/> along a new dimension. The axis parameter specifies the index of the new dimension. All tensors must have the same shape.
        /// </summary>
        /// <param name="tensors">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="dimension">Index of where the new dimension will be.</param>
        public static Tensor<T> StackAlongDimension<T>(int dimension, params ReadOnlySpan<Tensor<T>> tensors)
        {
            if (tensors.Length < 2)
                ThrowHelper.ThrowArgument_StackTooFewTensors();

            for (int i = 1; i < tensors.Length; i++)
            {
                if (!tensors[0].Lengths.SequenceEqual(tensors[i].Lengths))
                    ThrowHelper.ThrowArgument_StackShapesNotSame();
            }

            // We are safe to do dimension > tensors[0].Rank instead of >= because we are adding a new dimension
            // with our call to Unsqueeze.
            if (dimension < 0 || dimension > tensors[0].Rank)
                ThrowHelper.ThrowArgument_AxisLargerThanRank();

            Tensor<T>[] outputs = new Tensor<T>[tensors.Length];
            for (int i = 0; i < tensors.Length; i++)
            {
                outputs[i] = Tensor.Unsqueeze(tensors[i], dimension);
            }
            return Tensor.ConcatenateOnDimension<T>(dimension, outputs);
        }

        /// <summary>
        /// Join multiple <see cref="Tensor{T}"/> along a new dimension that is added at position 0. All tensors must have the same shape.
        /// </summary>
        /// <param name="tensors">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Stack<T>(scoped in ReadOnlySpan<Tensor<T>> tensors, in TensorSpan<T> destination)
        {
            return ref StackAlongDimension(tensors, destination, 0);
        }

        /// <summary>
        /// Join multiple <see cref="Tensor{T}"/> along a new dimension. The axis parameter specifies the index of the new dimension. All tensors must have the same shape.
        /// </summary>
        /// <param name="tensors">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="destination"></param>
        /// <param name="dimension">Index of where the new dimension will be.</param>
        public static ref readonly TensorSpan<T> StackAlongDimension<T>(scoped ReadOnlySpan<Tensor<T>> tensors, in TensorSpan<T> destination, int dimension)
        {
            if (tensors.Length < 2)
                ThrowHelper.ThrowArgument_StackTooFewTensors();

            for (int i = 1; i < tensors.Length; i++)
            {
                if (!tensors[0].Lengths.SequenceEqual(tensors[i].Lengths))
                    ThrowHelper.ThrowArgument_StackShapesNotSame();
            }

            // We are safe to do dimension > tensors[0].Rank instead of >= because we are adding a new dimension
            // with our call to Unsqueeze.
            if (dimension < 0 || dimension > tensors[0].Rank)
                ThrowHelper.ThrowArgument_AxisLargerThanRank();

            Tensor<T>[] outputs = new Tensor<T>[tensors.Length];
            for (int i = 0; i < tensors.Length; i++)
            {
                outputs[i] = Tensor.Unsqueeze(tensors[i], dimension);
            }
            return ref Tensor.ConcatenateOnDimension<T>(dimension, outputs, destination);
        }
        #endregion

        #region ToString
        /// <summary>
        /// Creates a <see cref="string"/> representation of the <see cref="TensorSpan{T}"/>."/>
        /// </summary>
        /// <param name="tensor">The <see cref="TensorSpan{T}"/> you want to represent as a string.</param>
        /// <param name="maximumLengths">Maximum Length of each dimension</param>
        /// <returns>A <see cref="string"/> representation of the <paramref name="tensor"/></returns>
        public static string ToString<T>(this in TensorSpan<T> tensor, ReadOnlySpan<nint> maximumLengths)
            => tensor.AsReadOnlyTensorSpan().ToString(maximumLengths);

        /// <summary>
        /// Creates a <see cref="string"/> representation of the <see cref="ReadOnlyTensorSpan{T}"/>."/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tensor">The <see cref="ReadOnlyTensorSpan{T}"/> you want to represent as a string.</param>
        /// <param name="maximumLengths">Maximum Length of each dimension</param>
        public static string ToString<T>(this in ReadOnlyTensorSpan<T> tensor, ReadOnlySpan<nint> maximumLengths)
        {
            if (maximumLengths.Length != tensor.Rank)
            {
                ThrowHelper.ThrowArgument_DimensionsNotSame(nameof(tensor));
            }

            StringBuilder sb = new();
            ToString(in tensor, maximumLengths, sb);
            return sb.ToString();
        }

        internal static void ToString<T>(in ReadOnlyTensorSpan<T> tensor, ReadOnlySpan<nint> maximumLengths, StringBuilder sb, int indentLevel = 0)
        {
            Debug.Assert(maximumLengths.Length != tensor.Rank);

            sb.Append(' ', indentLevel * 2);
            sb.Append('[');

            if (tensor.Rank != 0)
            {
                nint length = nint.Max(tensor.Lengths[0], maximumLengths[0]);

                if (tensor.Rank != 1)
                {
                    string separator = string.Empty;

                    for (nint i = 0; i < length; i++)
                    {
                        sb.AppendLine(separator);

                        TensorShape tmpShape = TensorShape.Create(tensor.Lengths[1..], tensor.Strides[1..]);
                        ReadOnlyTensorSpan<T> tmpTensor = new ReadOnlyTensorSpan<T>(ref Unsafe.Add(ref tensor._reference, i * tensor.Strides[0]), tmpShape);
                        ToString(tmpTensor, maximumLengths[1..], sb, indentLevel + 1);

                        separator = ",";
                    }

                    if (length != tensor.Lengths[0])
                    {
                        sb.AppendLine(separator);
                        sb.Append(' ', indentLevel * 2);
                        sb.AppendLine("...");
                    }
                }
                else
                {
                    string separator = " ";

                    for (nint i = 0; i < length; i++)
                    {
                        sb.Append(separator);
                        sb.Append(Unsafe.Add(ref tensor._reference, i));
                        separator = ", ";
                    }

                    if (length != tensor.Lengths[0])
                    {
                        sb.Append(separator);
                        sb.Append("...");
                    }

                    sb.Append(separator);
                }
            }
            sb.Append(']');
        }

        /// <summary>
        /// Creates a <see cref="string"/> representation of the <see cref="Tensor{T}"/>."/>
        /// </summary>
        /// <param name="tensor">The <see cref="Span{T}"/> you want to represent as a string.</param>
        /// <param name="maximumLengths">Maximum Length of each dimension</param>
        /// <returns>A <see cref="string"/> representation of the <paramref name="tensor"/></returns>
        public static string ToString<T>(this Tensor<T> tensor, ReadOnlySpan<nint> maximumLengths)
            => tensor.AsReadOnlyTensorSpan().ToString(maximumLengths);

        #endregion

        #region Transpose
        /// <summary>
        /// Swaps the last two dimensions of the <paramref name="tensor"/> tensor.
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/>.</param>
        public static Tensor<T> Transpose<T>(Tensor<T> tensor)
        {
            if (tensor.Lengths.Length < 2)
                ThrowHelper.ThrowArgument_TransposeTooFewDimensions();

            scoped Span<nint> lengths = TensorOperation.RentedBuffer.CreateUninitialized(tensor.Rank, out TensorOperation.RentedBuffer<nint> lengthsRentedBuffer);
            scoped Span<nint> strides = TensorOperation.RentedBuffer.CreateUninitialized(tensor.Rank, out TensorOperation.RentedBuffer<nint> stridesRentedBuffer);
            scoped Span<int> strideOrder = TensorOperation.RentedBuffer.CreateUninitialized(tensor.Rank, out TensorOperation.RentedBuffer<int> stridesOrderRentedBuffer);

            tensor.Lengths.CopyTo(lengths);
            tensor.Strides.CopyTo(strides);
            tensor._shape.LinearRankOrder.CopyTo(strideOrder);

            nint temp = lengths[^1];
            lengths[^1] = lengths[^2];
            lengths[^2] = temp;

            temp = strides[^1];
            strides[^1] = strides[^2];
            strides[^2] = temp;

            int tempOrder = strideOrder[^1];
            strideOrder[^1] = strideOrder[^2];
            strideOrder[^2] = tempOrder;

            Tensor<T> output = new Tensor<T>(tensor._values, tensor._start, lengths, strides, strideOrder);

            lengthsRentedBuffer.Dispose();
            stridesRentedBuffer.Dispose();
            stridesOrderRentedBuffer.Dispose();

            return output;
        }
        #endregion

        #region TryBroadcastTo
        /// <summary>
        /// Broadcast the data from <paramref name="tensor"/> to the smallest broadcastable shape compatible with <paramref name="destination"/> and stores it in <paramref name="destination"/>
        /// If the shapes are not compatible, false is returned.
        /// </summary>
        /// <param name="tensor">Input <see cref="Tensor{T}"/>.</param>
        /// <param name="destination">Destination <see cref="TensorSpan{T}"/>.</param>
        public static bool TryBroadcastTo<T>(this Tensor<T> tensor, in TensorSpan<T> destination)
        {
            return tensor.AsReadOnlyTensorSpan().TryBroadcastTo(destination);
        }

        /// <summary>
        /// Broadcast the data from <paramref name="tensor"/> to the smallest broadcastable shape compatible with <paramref name="destination"/> and stores it in <paramref name="destination"/>
        /// If the shapes are not compatible, false is returned.
        /// </summary>
        /// <param name="tensor">Input <see cref="TensorSpan{T}"/>.</param>
        /// <param name="destination">Destination <see cref="TensorSpan{T}"/>.</param>
        public static bool TryBroadcastTo<T>(in this TensorSpan<T> tensor, in TensorSpan<T> destination)
        {
            return tensor.AsReadOnlyTensorSpan().TryBroadcastTo(destination);
        }

        /// <summary>
        /// Broadcast the data from <paramref name="tensor"/> to the smallest broadcastable shape compatible with <paramref name="destination"/> and stores it in <paramref name="destination"/>
        /// If the shapes are not compatible, false is returned.
        /// </summary>
        /// <param name="tensor">Input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination">Destination <see cref="TensorSpan{T}"/>.</param>
        public static bool TryBroadcastTo<T>(in this ReadOnlyTensorSpan<T> tensor, in TensorSpan<T> destination)
        {
            TensorOperation.ValidateCompatibility(tensor, destination);
            if (!TensorShape.AreCompatible(destination._shape, tensor._shape, false))
                return false;

            BroadcastTo(tensor, destination);
            return true;
        }
        #endregion

        #region Unsqueeze
        /// <summary>
        /// Insert a new dimension of length 1 that will appear at the dimension position.
        /// </summary>
        /// <param name="tensor">The <see cref="Tensor{T}"/> to add a dimension of length 1.</param>
        /// <param name="dimension">The index of the dimension to add.</param>
        public static Tensor<T> Unsqueeze<T>(this Tensor<T> tensor, int dimension)
        {
            if (dimension > tensor.Lengths.Length)
                ThrowHelper.ThrowArgument_AxisLargerThanRank();
            if (dimension < 0)
                dimension = tensor.Rank - dimension;

            scoped Span<nint> newLengths = TensorOperation.RentedBuffer.CreateUninitialized(tensor.Rank + 1, out TensorOperation.RentedBuffer<nint> lengthsRentedBuffer);

            tensor.Lengths.Slice(0, dimension).CopyTo(newLengths);
            tensor.Lengths.Slice(dimension).CopyTo(newLengths.Slice(dimension + 1));
            newLengths[dimension] = 1;

            Span<nint> newStrides = TensorOperation.RentedBuffer.CreateUninitialized(tensor.Rank + 1, out TensorOperation.RentedBuffer<nint> stridesRentedBuffer);
            if (dimension == tensor.Rank)
            {
                tensor.Strides.CopyTo(newStrides);
                newStrides[dimension] = 0;
            }
            else
            {
                tensor.Strides.Slice(0, dimension).CopyTo(newStrides);
                tensor.Strides.Slice(dimension).CopyTo(newStrides.Slice(dimension + 1));
                newStrides[dimension] = 0;
            }

            Tensor<T> output = new Tensor<T>(tensor._values, tensor._start, newLengths, newStrides);
            lengthsRentedBuffer.Dispose();
            stridesRentedBuffer.Dispose();
            return output;
        }

        /// <summary>
        /// Insert a new dimension of length 1 that will appear at the dimension position.
        /// </summary>
        /// <param name="tensor">The <see cref="TensorSpan{T}"/> to add a dimension of length 1.</param>
        /// <param name="dimension">The index of the dimension to add.</param>
        public static TensorSpan<T> Unsqueeze<T>(this scoped in TensorSpan<T> tensor, int dimension)
        {
            if (dimension > tensor.Lengths.Length)
                ThrowHelper.ThrowArgument_AxisLargerThanRank();
            if (dimension < 0)
                dimension = tensor.Rank - dimension;

            scoped Span<nint> newLengths = TensorOperation.RentedBuffer.CreateUninitialized(tensor.Rank + 1, out TensorOperation.RentedBuffer<nint> lengthsRentedBuffer);

            tensor.Lengths.Slice(0, dimension).CopyTo(newLengths);
            tensor.Lengths.Slice(dimension).CopyTo(newLengths.Slice(dimension + 1));
            newLengths[dimension] = 1;

            Span<nint> newStrides = TensorOperation.RentedBuffer.CreateUninitialized(tensor.Rank + 1, out TensorOperation.RentedBuffer<nint> stridesRentedBuffer);
            if (dimension == tensor.Rank)
            {
                tensor.Strides.CopyTo(newStrides);
                newStrides[dimension] = 0;
            }
            else
            {
                tensor.Strides.Slice(0, dimension).CopyTo(newStrides);
                tensor.Strides.Slice(dimension).CopyTo(newStrides.Slice(dimension + 1));
                newStrides[dimension] = 0;
            }

            TensorSpan<T> output = new TensorSpan<T>(ref tensor._reference, tensor._shape.LinearLength, newLengths, newStrides);
            lengthsRentedBuffer.Dispose();
            stridesRentedBuffer.Dispose();
            return output;
        }

        /// <summary>
        /// Insert a new dimension of length 1 that will appear at the dimension position.
        /// </summary>
        /// <param name="tensor">The <see cref="ReadOnlyTensorSpan{T}"/> to add a dimension of length 1.</param>
        /// <param name="dimension">The index of the dimension to add.</param>
        public static ReadOnlyTensorSpan<T> Unsqueeze<T>(this scoped in ReadOnlyTensorSpan<T> tensor, int dimension)
        {
            if (dimension > tensor.Lengths.Length)
                ThrowHelper.ThrowArgument_AxisLargerThanRank();
            if (dimension < 0)
                dimension = tensor.Rank - dimension;

            scoped Span<nint> newLengths = TensorOperation.RentedBuffer.CreateUninitialized(tensor.Rank + 1, out TensorOperation.RentedBuffer<nint> lengthsRentedBuffer);

            tensor.Lengths.Slice(0, dimension).CopyTo(newLengths);
            tensor.Lengths.Slice(dimension).CopyTo(newLengths.Slice(dimension + 1));
            newLengths[dimension] = 1;

            Span<nint> newStrides = TensorOperation.RentedBuffer.CreateUninitialized(tensor.Rank + 1, out TensorOperation.RentedBuffer<nint> stridesRentedBuffer);
            if (dimension == tensor.Rank)
            {
                tensor.Strides.CopyTo(newStrides);
                newStrides[dimension] = 0;
            }
            else
            {
                tensor.Strides.Slice(0, dimension).CopyTo(newStrides);
                tensor.Strides.Slice(dimension).CopyTo(newStrides.Slice(dimension + 1));
                newStrides[dimension] = 0;
            }

            ReadOnlyTensorSpan<T> output = new ReadOnlyTensorSpan<T>(ref tensor._reference, tensor._shape.LinearLength, newLengths, newStrides);
            lengthsRentedBuffer.Dispose();
            stridesRentedBuffer.Dispose();
            return output;
        }
        #endregion

        #region TensorPrimitives
        #region Abs
        /// <summary>
        /// Takes the absolute value of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the abs of.</param>
        public static Tensor<T> Abs<T>(in ReadOnlyTensorSpan<T> x)
            where T : INumberBase<T>
        {
            Tensor<T> destination = CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Abs<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>
        /// Takes the absolute value of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="TensorSpan{T}"/> to take the abs of.</param>
        /// <param name="destination">The <see cref="TensorSpan{T}"/> destination.</param>
        public static ref readonly TensorSpan<T> Abs<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : INumberBase<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Abs<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region Acos
        /// <summary>
        /// Takes the inverse cosine of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        public static Tensor<T> Acos<T>(in ReadOnlyTensorSpan<T> x)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Acos<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>
        /// Takes the inverse cosine of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Acos<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Acos<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region Acosh
        /// <summary>
        /// Takes the inverse hyperbolic cosine of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        public static Tensor<T> Acosh<T>(in ReadOnlyTensorSpan<T> x)
            where T : IHyperbolicFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Acosh<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>
        /// Takes the inverse hyperbolic cosine of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="TensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Acosh<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IHyperbolicFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Acosh<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region AcosPi
        /// <summary>
        /// Takes the inverse hyperbolic cosine divided by pi of each element of the <see cref="Tensor{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        public static Tensor<T> AcosPi<T>(in ReadOnlyTensorSpan<T> x)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.AcosPi<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>
        /// Takes the inverse hyperbolic cosine divided by pi of each element of the <see cref="TensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> AcosPi<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.AcosPi<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region Add
        /// <summary>
        /// Adds each element of <paramref name="x"/> to each element of <paramref name="y"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> of values to add.</param>
        /// <param name="y">The second <see cref="ReadOnlyTensorSpan{T}"/> of values to add.</param>
        public static Tensor<T> Add<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            TensorOperation.ValidateCompatibility(x, y, out Tensor<T> destination);
            TensorOperation.Invoke<TensorOperation.Add<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Adds <paramref name="y"/> to each element of <paramref name="x"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> of values to add.</param>
        /// <param name="y">The <typeparamref name="T"/> to add to each element of <paramref name="x"/>.</param>
        public static Tensor<T> Add<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Add<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Adds each element of <paramref name="x"/> to each element of <paramref name="y"/> and returns a new <see cref="ReadOnlyTensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> of values to add.</param>
        /// <param name="y">The second <see cref="ReadOnlyTensorSpan{T}"/> of values to add.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Add<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            TensorOperation.ValidateCompatibility(x, y, destination);
            TensorOperation.Invoke<TensorOperation.Add<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>
        /// Adds <paramref name="y"/> to each element of <paramref name="x"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> of values to add.</param>
        /// <param name="y">The <typeparamref name="T"/> to add to each element of <paramref name="x"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Add<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Add<T>, T, T>(x, y, destination);
            return ref destination;
        }
        #endregion

        #region Asin
        /// <summary>
        /// Takes the inverse sin of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        public static Tensor<T> Asin<T>(in ReadOnlyTensorSpan<T> x)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Asin<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>
        /// Takes the inverse sin of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Asin<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Asin<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region Asinh
        /// <summary>
        /// Takes the inverse hyperbolic sine of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        public static Tensor<T> Asinh<T>(in ReadOnlyTensorSpan<T> x)
            where T : IHyperbolicFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Asinh<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>
        /// Takes the inverse hyperbolic sine of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Asinh<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IHyperbolicFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Asinh<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region AsinPi
        /// <summary>
        /// Takes the inverse hyperbolic sine divided by pi of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        public static Tensor<T> AsinPi<T>(in ReadOnlyTensorSpan<T> x)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.AsinPi<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>
        /// Takes the inverse hyperbolic sine divided by pi of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> AsinPi<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.AsinPi<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region Atan
        /// <summary>
        /// Takes the arc tangent of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/></param>
        public static Tensor<T> Atan<T>(in ReadOnlyTensorSpan<T> x)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Atan<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>
        /// Takes the arc tangent of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Atan<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Atan<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region Atan2
        /// <summary>
        /// Takes the arc tangent of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Atan2<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IFloatingPointIeee754<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Atan2<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Takes the arc tangent of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Atan2<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IFloatingPointIeee754<T>
        {
            TensorOperation.ValidateCompatibility(x, y, destination);
            TensorOperation.Invoke<TensorOperation.Atan2<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>
        /// Takes the arc tangent of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Atan2<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IFloatingPointIeee754<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Atan2<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Takes the arc tangent of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Atan2<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : IFloatingPointIeee754<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Atan2<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>
        /// Takes the arc tangent of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Atan2<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IFloatingPointIeee754<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(y.Lengths);
            TensorOperation.Invoke<TensorOperation.Atan2<T>, T, T>(y, x, destination);
            return destination;
        }

        /// <summary>
        /// Takes the arc tangent of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Atan2<T>(T x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IFloatingPointIeee754<T>
        {
            TensorOperation.ValidateCompatibility(y, destination);
            TensorOperation.Invoke<TensorOperation.Atan2<T>, T, T>(y, x, destination);
            return ref destination;
        }
        #endregion

        #region Atan2Pi
        /// <summary>
        /// Takes the arc tangent of the two input <see cref="ReadOnlyTensorSpan{T}"/>, divides each element by pi, and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Atan2Pi<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IFloatingPointIeee754<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Atan2Pi<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Takes the arc tangent of the two input <see cref="ReadOnlyTensorSpan{T}"/>, divides each element by pi, and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Atan2Pi<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IFloatingPointIeee754<T>
        {
            TensorOperation.ValidateCompatibility(x, y, destination);
            TensorOperation.Invoke<TensorOperation.Atan2Pi<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>
        /// Takes the arc tangent of the two input <see cref="ReadOnlyTensorSpan{T}"/>, divides each element by pi, and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Atan2Pi<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IFloatingPointIeee754<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Atan2Pi<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Takes the arc tangent of the two input <see cref="ReadOnlyTensorSpan{T}"/>, divides each element by pi, and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Atan2Pi<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : IFloatingPointIeee754<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Atan2Pi<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>
        /// Takes the arc tangent of the two input <see cref="ReadOnlyTensorSpan{T}"/>, divides each element by pi, and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Atan2Pi<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IFloatingPointIeee754<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(y.Lengths);
            TensorOperation.Invoke<TensorOperation.Atan2Pi<T>, T, T>(y, x, destination);
            return destination;
        }

        /// <summary>
        /// Takes the arc tangent of the two input <see cref="ReadOnlyTensorSpan{T}"/>, divides each element by pi, and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Atan2Pi<T>(T x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IFloatingPointIeee754<T>
        {
            TensorOperation.ValidateCompatibility(y, destination);
            TensorOperation.Invoke<TensorOperation.Atan2Pi<T>, T, T>(y, x, destination);
            return ref destination;
        }
        #endregion

        #region Atanh
        /// <summary>
        /// Takes the inverse hyperbolic tangent of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Atanh<T>(in ReadOnlyTensorSpan<T> x)
            where T : IHyperbolicFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Atanh<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>
        /// Takes the inverse hyperbolic tangent of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Atanh<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IHyperbolicFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Atanh<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region AtanPi
        /// <summary>
        /// Takes the inverse hyperbolic tangent divided by pi of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The input<see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> AtanPi<T>(in ReadOnlyTensorSpan<T> x)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.AtanPi<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>
        /// Takes the inverse hyperbolic tangent divided by pi of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The input<see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> AtanPi<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.AtanPi<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region Average
        /// <summary>
        /// Returns the average of the elements in the <paramref name="x"/> tensor.
        /// </summary>
        /// <param name="x">The <see cref="TensorSpan{T}"/> to take the mean of.</param>
        /// <returns><typeparamref name="T"/> representing the mean.</returns>
        public static T Average<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : INumberBase<T>
        {
            // Get the flattenedLength first so we don't spend time computing if we'll fail due to overflow

            T flattenedLength = T.CreateChecked(x.FlattenedLength);
            T sum = Sum(x);
            return sum / flattenedLength;
        }
        #endregion

        #region BitwiseAnd
        /// <summary>
        /// Computes the element-wise bitwise and of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> BitwiseAnd<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IBitwiseOperators<T, T, T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.BitwiseAnd<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Computes the element-wise bitwise and of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> BitwiseAnd<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IBitwiseOperators<T, T, T>
        {
            TensorOperation.ValidateCompatibility(x, y, destination);
            TensorOperation.Invoke<TensorOperation.BitwiseAnd<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>
        /// Computes the element-wise bitwise and of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The second value.</param>
        public static Tensor<T> BitwiseAnd<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IBitwiseOperators<T, T, T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.BitwiseAnd<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Computes the element-wise bitwise and of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The second value.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> BitwiseAnd<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : IBitwiseOperators<T, T, T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.BitwiseAnd<T>, T, T>(x, y, destination);
            return ref destination;
        }
        #endregion

        #region BitwiseOr
        /// <summary>
        /// Computes the element-wise bitwise of of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> BitwiseOr<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IBitwiseOperators<T, T, T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.BitwiseOr<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Computes the element-wise bitwise of of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> BitwiseOr<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IBitwiseOperators<T, T, T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.BitwiseOr<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>
        /// Computes the element-wise bitwise or of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The second value.</param>
        public static Tensor<T> BitwiseOr<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IBitwiseOperators<T, T, T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.BitwiseOr<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Computes the element-wise bitwise or of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The second value.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> BitwiseOr<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : IBitwiseOperators<T, T, T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.BitwiseOr<T>, T, T>(x, y, destination);
            return ref destination;
        }
        #endregion

        #region CubeRoot
        /// <summary>
        /// Computes the element-wise cube root of the input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Cbrt<T>(in ReadOnlyTensorSpan<T> x)
            where T : IRootFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Cbrt<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>
        /// Computes the element-wise cube root of the input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Cbrt<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IRootFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Cbrt<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region Ceiling
        /// <summary>
        /// Computes the element-wise ceiling of the input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Ceiling<T>(in ReadOnlyTensorSpan<T> x)
            where T : IFloatingPoint<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Ceiling<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>
        /// Computes the element-wise ceiling of the input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Ceiling<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IFloatingPoint<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Ceiling<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region ConvertChecked
        /// <summary>
        /// Copies <paramref name="source"/> to a new <see cref="ReadOnlyTensorSpan{TTO}"/> converting each <typeparamref name="TFrom"/>
        /// value to a <typeparamref name="TTo"/> value.
        /// </summary>
        /// <param name="source">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<TTo> ConvertChecked<TFrom, TTo>(in ReadOnlyTensorSpan<TFrom> source)
            where TFrom : IEquatable<TFrom>, IEqualityOperators<TFrom, TFrom, bool>, INumberBase<TFrom>
            where TTo : INumberBase<TTo>
        {
            Tensor<TTo> destination = Tensor.CreateUninitialized<TTo>(source.Lengths);
            TensorOperation.Invoke<TensorOperation.ConvertChecked<TFrom, TTo>, TFrom, TTo>(source, destination);
            return destination;
        }

        /// <summary>
        /// Copies <paramref name="source"/> to a new <see cref="TensorSpan{TTo}"/> converting each <typeparamref name="TFrom"/>
        /// value to a <typeparamref name="TTo"/> value.
        /// </summary>
        /// <param name="source">The input <see cref="TensorSpan{TFrom}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<TTo> ConvertChecked<TFrom, TTo>(scoped in ReadOnlyTensorSpan<TFrom> source, in TensorSpan<TTo> destination)
            where TFrom : IEquatable<TFrom>, IEqualityOperators<TFrom, TFrom, bool>, INumberBase<TFrom>
            where TTo : INumberBase<TTo>
        {
            TensorOperation.ValidateCompatibility(source, destination);
            TensorOperation.Invoke<TensorOperation.ConvertChecked<TFrom, TTo>, TFrom, TTo>(source, destination);
            return ref destination;
        }
        #endregion

        #region ConvertSaturating
        /// <summary>
        /// Copies <paramref name="source"/> to a new <see cref="ReadOnlyTensorSpan{TTO}"/> converting each <typeparamref name="TFrom"/>
        /// value to a <typeparamref name="TTo"/> value.
        /// </summary>
        /// <param name="source">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<TTo> ConvertSaturating<TFrom, TTo>(in ReadOnlyTensorSpan<TFrom> source)
            where TFrom : IEquatable<TFrom>, IEqualityOperators<TFrom, TFrom, bool>, INumberBase<TFrom>
            where TTo : INumberBase<TTo>
        {
            Tensor<TTo> destination = Tensor.CreateUninitialized<TTo>(source.Lengths);
            TensorOperation.Invoke<TensorOperation.ConvertSaturating<TFrom, TTo>, TFrom, TTo>(source, destination);
            return destination;
        }

        /// <summary>
        /// Copies <paramref name="source"/> to a new <see cref="TensorSpan{TTo}"/> converting each <typeparamref name="TFrom"/>
        /// value to a <typeparamref name="TTo"/> value.
        /// </summary>
        /// <param name="source">The input <see cref="TensorSpan{TFrom}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<TTo> ConvertSaturating<TFrom, TTo>(scoped in ReadOnlyTensorSpan<TFrom> source, in TensorSpan<TTo> destination)
            where TFrom : IEquatable<TFrom>, IEqualityOperators<TFrom, TFrom, bool>, INumberBase<TFrom>
            where TTo : INumberBase<TTo>
        {
            TensorOperation.ValidateCompatibility(source, destination);
            TensorOperation.Invoke<TensorOperation.ConvertSaturating<TFrom, TTo>, TFrom, TTo>(source, destination);
            return ref destination;
        }
        #endregion

        #region ConvertTruncating
        /// <summary>
        /// Copies <paramref name="source"/> to a new <see cref="ReadOnlyTensorSpan{TTO}"/> converting each <typeparamref name="TFrom"/>
        /// value to a <typeparamref name="TTo"/> value.
        /// </summary>
        /// <param name="source">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<TTo> ConvertTruncating<TFrom, TTo>(in ReadOnlyTensorSpan<TFrom> source)
            where TFrom : IEquatable<TFrom>, IEqualityOperators<TFrom, TFrom, bool>, INumberBase<TFrom>
            where TTo : INumberBase<TTo>
        {
            Tensor<TTo> destination = Tensor.CreateUninitialized<TTo>(source.Lengths);
            TensorOperation.Invoke<TensorOperation.ConvertTruncating<TFrom, TTo>, TFrom, TTo>(source, destination);
            return destination;
        }

        /// <summary>
        /// Copies <paramref name="source"/> to a new <see cref="TensorSpan{TTo}"/> converting each <typeparamref name="TFrom"/>
        /// value to a <typeparamref name="TTo"/> value.
        /// </summary>
        /// <param name="source">The input <see cref="TensorSpan{TFrom}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<TTo> ConvertTruncating<TFrom, TTo>(scoped in ReadOnlyTensorSpan<TFrom> source, in TensorSpan<TTo> destination)
            where TFrom : IEquatable<TFrom>, IEqualityOperators<TFrom, TFrom, bool>, INumberBase<TFrom>
            where TTo : INumberBase<TTo>
        {
            TensorOperation.ValidateCompatibility(source, destination);
            TensorOperation.Invoke<TensorOperation.ConvertTruncating<TFrom, TTo>, TFrom, TTo>(source, destination);
            return ref destination;
        }
        #endregion

        #region CopySign
        /// <summary>
        /// Computes the element-wise result of copying the sign from one number to another number in the specified tensors and returns a new tensor with the result.
        /// </summary>
        /// <param name="x">Input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="sign">The number with the associated sign.</param>
        public static Tensor<T> CopySign<T>(in ReadOnlyTensorSpan<T> x, T sign)
            where T : INumber<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.CopySign<T>, T, T>(x, sign, destination);
            return destination;
        }

        /// <summary>
        /// Computes the element-wise result of copying the sign from one number to another number in the specified tensors and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">Input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="sign">The <see cref="ReadOnlyTensorSpan{T}"/> with the associated signs.</param>
        public static Tensor<T> CopySign<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> sign)
            where T : INumber<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.CopySign<T>, T, T>(x, sign, destination);
            return destination;
        }

        /// <summary>
        /// Computes the element-wise result of copying the sign from one number to another number in the specified tensors and returns a new tensor with the result.
        /// </summary>
        /// <param name="x">Input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="sign">The number with the associated sign.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> CopySign<T>(scoped in ReadOnlyTensorSpan<T> x, T sign, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.CopySign<T>, T, T>(x, sign, destination);
            return ref destination;
        }

        /// <summary>
        /// Computes the element-wise result of copying the sign from one number to another number in the specified tensors and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">Input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="sign">The <see cref="ReadOnlyTensorSpan{T}"/> with the associated signs.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> CopySign<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> sign, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            TensorOperation.ValidateCompatibility(x, sign, destination);
            TensorOperation.Invoke<TensorOperation.CopySign<T>, T, T>(x, sign, destination);
            return ref destination;
        }
        #endregion

        #region Cos
        /// <summary>
        /// Takes the cosine of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the cosine of.</param>
        public static Tensor<T> Cos<T>(in ReadOnlyTensorSpan<T> x)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Cos<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>
        /// Takes the cosine of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the cosine of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Cos<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Cos<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region Cosh
        /// <summary>
        /// Takes the hyperbolic cosine of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the cosine of.</param>
        public static Tensor<T> Cosh<T>(in ReadOnlyTensorSpan<T> x)
            where T : IHyperbolicFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Cosh<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>
        /// Takes the hyperbolic cosine of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the cosine of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Cosh<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IHyperbolicFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Cosh<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region CosineSimilarity
        /// <summary>
        /// Compute cosine similarity between <paramref name="x"/> and <paramref name="y"/>.
        /// </summary>
        /// <param name="x">The first <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="y">The second <see cref="ReadOnlyTensorSpan{T}"/></param>
        public static T CosineSimilarity<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IRootFunctions<T>
        {
            TensorOperation.ValidateCompatibility<T, T>(x, y);
            ValueTuple<T, T, T> result = (T.AdditiveIdentity, T.AdditiveIdentity, T.AdditiveIdentity);
            TensorOperation.Invoke<TensorOperation.CosineSimilarity<T>, T, ValueTuple<T, T, T>>(x, y, ref result);
            return result.Item1 / (T.Sqrt(result.Item2) * T.Sqrt(result.Item3));
        }
        #endregion

        #region CosPi
        /// <summary>Computes the element-wise cosine of the value in the specified tensor that has been multiplied by Pi and returns a new <see cref="Tensor{T}"/> with the results.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><typeparamref name="T"/>.CosPi(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// The angles in x must be in radians. Use <see cref="M:System.Single.DegreesToRadians(System.Single)"/> or multiply by <typeparamref name="T"/>.Pi/180 to convert degrees to radians.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static Tensor<T> CosPi<T>(in ReadOnlyTensorSpan<T> x)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.CosPi<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>Computes the element-wise cosine of the value in the specified tensor that has been multiplied by Pi and returns a new <see cref="TensorSpan{T}"/> with the results.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="destination"></param>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><typeparamref name="T"/>.CosPi(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// The angles in x must be in radians. Use <see cref="M:System.Single.DegreesToRadians(System.Single)"/> or multiply by <typeparamref name="T"/>.Pi/180 to convert degrees to radians.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static ref readonly TensorSpan<T> CosPi<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.CosPi<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region DegreesToRadians
        /// <summary>
        /// Computes the element-wise conversion of each number of degrees in the specified tensor to radians and returns a new tensor with the results.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> DegreesToRadians<T>(in ReadOnlyTensorSpan<T> x)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.DegreesToRadians<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>
        /// Computes the element-wise conversion of each number of degrees in the specified tensor to radians and returns a new tensor with the results.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> DegreesToRadians<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.DegreesToRadians<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region Distance
        /// <summary>
        /// Computes the distance between two points, specified as non-empty, equal-length tensors of numbers, in Euclidean space.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static T Distance<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y)
            where T : IRootFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, y);
            T result = T.AdditiveIdentity;
            TensorOperation.Invoke<TensorOperation.SumOfSquaredDifferences<T>, T, T>(x, y, ref result);
            return T.Sqrt(result);
        }
        #endregion

        #region Divide
        /// <summary>
        /// Divides each element of <paramref name="x"/> by <paramref name="y"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">Input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The divisor</param>
        public static Tensor<T> Divide<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IDivisionOperators<T, T, T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Divide<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Divides <paramref name="x"/> by each element of <paramref name="y"/> and returns a new <see cref="Tensor{T}"/> with the result."/>
        /// </summary>
        /// <param name="x">The value to be divided.</param>
        /// <param name="y">The <see cref="ReadOnlyTensorSpan{T}"/> divisor.</param>
        public static Tensor<T> Divide<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IDivisionOperators<T, T, T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(y.Lengths);
            TensorOperation.Invoke<TensorOperation.Divide<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Divides each element of <paramref name="x"/> by its corresponding element in <paramref name="y"/> and returns
        /// a new <see cref="ReadOnlyTensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to be divided.</param>
        /// <param name="y">The <see cref="ReadOnlyTensorSpan{T}"/> divisor.</param>
        public static Tensor<T> Divide<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IDivisionOperators<T, T, T>
        {
            TensorOperation.ValidateCompatibility(x, y, out Tensor<T> destination);
            TensorOperation.Invoke<TensorOperation.Divide<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Divides each element of <paramref name="x"/> by <paramref name="y"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">Input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The divisor</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Divide<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : IDivisionOperators<T, T, T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Divide<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>
        /// Divides <paramref name="x"/> by each element of <paramref name="y"/> and returns a new <see cref="TensorSpan{T}"/> with the result."/>
        /// </summary>
        /// <param name="x">The value to be divided.</param>
        /// <param name="y">The <see cref="ReadOnlyTensorSpan{T}"/> divisor.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Divide<T>(T x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IDivisionOperators<T, T, T>
        {
            TensorOperation.ValidateCompatibility(y, destination);
            TensorOperation.Invoke<TensorOperation.Divide<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>
        /// Divides each element of <paramref name="x"/> by its corresponding element in <paramref name="y"/> and returns
        /// a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to be divided.</param>
        /// <param name="y">The <see cref="ReadOnlyTensorSpan{T}"/> divisor.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Divide<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IDivisionOperators<T, T, T>
        {
            TensorOperation.ValidateCompatibility(x, y, destination);
            TensorOperation.Invoke<TensorOperation.Divide<T>, T, T>(x, y, destination);
            return ref destination;
        }
        #endregion

        #region Dot
        /// <summary>
        /// Computes the dot product of two tensors containing numbers.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static T Dot<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>, IMultiplicativeIdentity<T, T>, IMultiplyOperators<T, T, T>
        {
            TensorOperation.ValidateCompatibility(x, y);
            T result = T.AdditiveIdentity;
            TensorOperation.Invoke<TensorOperation.Dot<T>, T, T>(x, y, ref result);
            return result;
        }
        #endregion

        #region Exp
        /// <summary>
        /// Computes the element-wise result of raising <c>e</c> to the single-precision floating-point number powers in the specified tensor.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Exp<T>(in ReadOnlyTensorSpan<T> x)
            where T : IExponentialFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Exp<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>
        /// Computes the element-wise result of raising <c>e</c> to the single-precision floating-point number powers in the specified tensor.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Exp<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IExponentialFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Exp<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region Exp10
        /// <summary>
        /// Computes the element-wise result of raising 10 to the number powers in the specified tensor.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Exp10<T>(in ReadOnlyTensorSpan<T> x)
            where T : IExponentialFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Exp10<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>
        /// Computes the element-wise result of raising 10 to the number powers in the specified tensor.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Exp10<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IExponentialFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Exp10<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region Exp10M1
        /// <summary>Computes the element-wise result of raising 10 to the number powers in the specified tensor, minus one.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Exp10M1<T>(in ReadOnlyTensorSpan<T> x)
            where T : IExponentialFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Exp10M1<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>Computes the element-wise result of raising 10 to the number powers in the specified tensor, minus one.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Exp10M1<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IExponentialFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Exp10M1<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region Exp2
        /// <summary>Computes the element-wise result of raising 2 to the number powers in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Exp2<T>(in ReadOnlyTensorSpan<T> x)
            where T : IExponentialFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Exp2<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>Computes the element-wise result of raising 2 to the number powers in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Exp2<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IExponentialFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Exp2<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region Exp2M1
        /// <summary>Computes the element-wise result of raising 2 to the number powers in the specified tensor, minus one.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Exp2M1<T>(in ReadOnlyTensorSpan<T> x)
            where T : IExponentialFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Exp2M1<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>Computes the element-wise result of raising 2 to the number powers in the specified tensor, minus one.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Exp2M1<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IExponentialFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Exp2M1<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region ExpM1
        /// <summary>Computes the element-wise result of raising <c>e</c> to the number powers in the specified tensor, minus 1.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> ExpM1<T>(in ReadOnlyTensorSpan<T> x)
            where T : IExponentialFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.ExpM1<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>Computes the element-wise result of raising <c>e</c> to the number powers in the specified tensor, minus 1.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> ExpM1<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IExponentialFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.ExpM1<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region Floor
        /// <summary>Computes the element-wise floor of numbers in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Floor<T>(in ReadOnlyTensorSpan<T> x)
            where T : IFloatingPoint<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Floor<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>Computes the element-wise floor of numbers in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Floor<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IFloatingPoint<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Floor<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region Hypotenuse
        /// <summary>
        /// Computes the element-wise hypotenuse given values from two tensors representing the lengths of the shorter sides in a right-angled triangle.
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// </summary>
        /// <param name="x">Left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">Right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Hypot<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IRootFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, y, out Tensor<T> destination);
            TensorOperation.Invoke<TensorOperation.Hypot<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Computes the element-wise hypotenuse given values from two tensors representing the lengths of the shorter sides in a right-angled triangle.
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// </summary>
        /// <param name="x">Left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">Right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Hypot<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IRootFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, y, destination);
            TensorOperation.Invoke<TensorOperation.Hypot<T>, T, T>(x, y, destination);
            return ref destination;
        }
        #endregion

        #region Ieee754Remainder
        /// <summary>Computes the element-wise remainder of the numbers in the specified tensors.</summary>
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// <param name="x">Left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">Right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Ieee754Remainder<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IFloatingPointIeee754<T>
        {
            TensorOperation.ValidateCompatibility(x, y, out Tensor<T> destination);
            TensorOperation.Invoke<TensorOperation.Ieee754Remainder<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Computes the element-wise remainder of the numbers in the specified tensors.</summary>
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// <param name="x">Left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">Right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Ieee754Remainder<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IFloatingPointIeee754<T>
        {
            TensorOperation.ValidateCompatibility(x, y, destination);
            TensorOperation.Invoke<TensorOperation.Ieee754Remainder<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>Computes the element-wise remainder of the numbers in the specified tensors.</summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Ieee754Remainder<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IFloatingPointIeee754<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Ieee754Remainder<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Computes the element-wise remainder of the numbers in the specified tensors.</summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Ieee754Remainder<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : IFloatingPointIeee754<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Ieee754Remainder<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>Computes the element-wise remainder of the numbers in the specified tensors.</summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Ieee754Remainder<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IFloatingPointIeee754<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(y.Lengths);
            TensorOperation.Invoke<TensorOperation.Ieee754Remainder<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Computes the element-wise remainder of the numbers in the specified tensors.</summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Ieee754Remainder<T>(T x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IFloatingPointIeee754<T>
        {
            TensorOperation.ValidateCompatibility(y, destination);
            TensorOperation.Invoke<TensorOperation.Ieee754Remainder<T>, T, T>(x, y, destination);
            return ref destination;
        }
        #endregion

        #region ILogB
        /// <summary>Computes the element-wise integer logarithm of numbers in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<int> ILogB<T>(in ReadOnlyTensorSpan<T> x)
            where T : IFloatingPointIeee754<T>
        {
            Tensor<int> destination = Tensor.CreateUninitialized<int>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.ILogB<T>, T, int>(x, destination);
            return destination;
        }

        /// <summary>Computes the element-wise integer logarithm of numbers in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<int> ILogB<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<int> destination)
            where T : IFloatingPointIeee754<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.ILogB<T>, T, int>(x, destination);
            return ref destination;
        }
        #endregion

        #region IndexOfMax
        /// <summary>Searches for the index of the largest number in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static nint IndexOfMax<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : INumber<T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref x._reference, (int)x._shape.LinearLength);
            return TensorPrimitives.IndexOfMax(span);
        }

        #endregion

        #region IndexOfMaxMagnitude
        /// <summary>Searches for the index of the number with the largest magnitude in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static nint IndexOfMaxMagnitude<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : INumber<T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref x._reference, (int)x._shape.LinearLength);
            return TensorPrimitives.IndexOfMaxMagnitude(span);
        }
        #endregion

        #region IndexOfMin
        /// <summary>Searches for the index of the smallest number in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static nint IndexOfMin<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : INumber<T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref x._reference, (int)x._shape.LinearLength);
            return TensorPrimitives.IndexOfMin(span);
        }
        #endregion

        #region IndexOfMinMagnitude
        /// <summary>
        /// Searches for the index of the number with the smallest magnitude in the specified tensor.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static nint IndexOfMinMagnitude<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : INumber<T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref x._reference, (int)x._shape.LinearLength);
            return TensorPrimitives.IndexOfMinMagnitude(span);
        }
        #endregion

        #region LeadingZeroCount
        /// <summary>
        /// Computes the element-wise leading zero count of numbers in the specified tensor.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> LeadingZeroCount<T>(in ReadOnlyTensorSpan<T> x)
            where T : IBinaryInteger<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.LeadingZeroCount<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>
        /// Computes the element-wise leading zero count of numbers in the specified tensor.
        /// </summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> LeadingZeroCount<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IBinaryInteger<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.LeadingZeroCount<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region Log
        /// <summary>
        /// Takes the natural logarithm of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the natural logarithm of.</param>
        public static Tensor<T> Log<T>(in ReadOnlyTensorSpan<T> x)
            where T : ILogarithmicFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Log<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>
        /// Takes the natural logarithm of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the natural logarithm of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Log<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ILogarithmicFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Log<T>, T, T>(x, destination);
            return ref destination;
        }

        /// <summary>Computes the element-wise logarithm of the numbers in a specified tensor to the specified base in another specified tensor.</summary>
        /// <param name="x">The first tensor</param>
        /// <param name="y">The second tensor</param>
        public static Tensor<T> Log<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : ILogarithmicFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, y, out Tensor<T> destination);
            TensorOperation.Invoke<TensorOperation.Log<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Computes the element-wise logarithm of the numbers in a specified tensor to the specified base in another specified tensor.</summary>
        /// <param name="x">The first tensor</param>
        /// <param name="y">The second tensor</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> Log<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : ILogarithmicFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, y, destination);
            TensorOperation.Invoke<TensorOperation.Log<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>Computes the element-wise logarithm of the numbers in a specified tensor to the specified base in another specified tensor.</summary>
        /// <param name="x">The first tensor</param>
        /// <param name="y">The second tensor</param>
        public static Tensor<T> Log<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : ILogarithmicFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Log<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Computes the element-wise logarithm of the numbers in a specified tensor to the specified base in another specified tensor.</summary>
        /// <param name="x">The first tensor</param>
        /// <param name="y">The second tensor</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> Log<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : ILogarithmicFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Log<T>, T, T>(x, y, destination);
            return ref destination;
        }
        #endregion

        #region Log10
        /// <summary>
        /// Takes the base 10 logarithm of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the base 10 logarithm of.</param>
        public static Tensor<T> Log10<T>(in ReadOnlyTensorSpan<T> x)
            where T : ILogarithmicFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Log10<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>
        /// Takes the base 10 logarithm of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the base 10 logarithm of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Log10<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ILogarithmicFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Log10<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region Log10P1
        /// <summary>
        /// Takes the base 10 logarithm plus 1 of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the base 10 logarithm of.</param>
        public static Tensor<T> Log10P1<T>(in ReadOnlyTensorSpan<T> x)
            where T : ILogarithmicFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Log10P1<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>
        /// Takes the base 10 logarithm plus 1 of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the base 10 logarithm of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Log10P1<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ILogarithmicFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Log10P1<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region Log2
        /// <summary>
        /// Takes the base 2 logarithm of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the base 2 logarithm of.</param>
        public static Tensor<T> Log2<T>(in ReadOnlyTensorSpan<T> x)
            where T : ILogarithmicFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Log2<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>
        /// Takes the base 2 logarithm of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the base 2 logarithm of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Log2<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ILogarithmicFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Log2<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region Log2P1
        /// <summary>
        /// Takes the base 2 logarithm plus 1 of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the base 2 logarithm of.</param>
        public static Tensor<T> Log2P1<T>(in ReadOnlyTensorSpan<T> x)
            where T : ILogarithmicFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Log2P1<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>
        /// Takes the base 2 logarithm plus 1 of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the base 2 logarithm of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Log2P1<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ILogarithmicFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Log2P1<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region LogP1
        /// <summary>
        /// Takes the natural logarithm plus 1 of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the natural logarithm of.</param>
        public static Tensor<T> LogP1<T>(in ReadOnlyTensorSpan<T> x)
            where T : ILogarithmicFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.LogP1<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>
        /// Takes the natural logarithm plus 1 of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the natural logarithm of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> LogP1<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ILogarithmicFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.LogP1<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region Max
        /// <summary>Searches for the largest number in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static T Max<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : INumber<T>
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            T result = x._reference;
            TensorOperation.Invoke<TensorOperation.Max<T>, T, T>(x, ref result);
            return result;
        }

        /// <summary>Computes the element-wise maximum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> Max<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : INumber<T>
        {
            TensorOperation.ValidateCompatibility(in x, in y, out Tensor<T> output);
            TensorOperation.Invoke<TensorOperation.Max<T>, T, T>(x, y, output);
            return output;
        }

        /// <summary>Computes the element-wise maximum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> Max<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            TensorOperation.ValidateCompatibility(in x, in y, in destination);
            TensorOperation.Invoke<TensorOperation.Max<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>Computes the element-wise maximum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> Max<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : INumber<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Max<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Computes the element-wise maximum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> Max<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            TensorOperation.ValidateCompatibility(in x, in destination);
            TensorOperation.Invoke<TensorOperation.Max<T>, T, T>(x, y, destination);
            return ref destination;
        }
        #endregion

        #region MaxMagnitude
        /// <summary>Searches for the number with the largest magnitude in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static T MaxMagnitude<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : INumber<T>
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            T result = x._reference;
            TensorOperation.Invoke<TensorOperation.MaxMagnitude<T>, T, T>(x, ref result);
            return result;
        }

        /// <summary>Computes the element-wise number with the largest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> MaxMagnitude<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : INumber<T>
        {
            TensorOperation.ValidateCompatibility(in x, in y, out Tensor<T> destination);
            TensorOperation.Invoke<TensorOperation.MaxMagnitude<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Computes the element-wise number with the largest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> MaxMagnitude<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            TensorOperation.ValidateCompatibility(in x, in y, in destination);
            TensorOperation.Invoke<TensorOperation.MaxMagnitude<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>Computes the element-wise number with the largest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> MaxMagnitude<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : INumber<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.MaxMagnitude<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Computes the element-wise number with the largest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> MaxMagnitude<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            TensorOperation.ValidateCompatibility(in x, in destination);
            TensorOperation.Invoke<TensorOperation.MaxMagnitude<T>, T, T>(x, y, destination);
            return ref destination;
        }
        #endregion

        #region MaxMagnitudeNumber
        /// <summary>Searches for the number with the largest magnitude in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static T MaxMagnitudeNumber<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : INumberBase<T>
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            T result = x._reference;
            TensorOperation.Invoke<TensorOperation.MaxMagnitudeNumber<T>, T, T>(x, ref result);
            return result;
        }

        /// <summary>Computes the element-wise number with the largest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> MaxMagnitudeNumber<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : INumber<T>
        {
            TensorOperation.ValidateCompatibility(in x, in y, out Tensor<T> destination);
            TensorOperation.Invoke<TensorOperation.MaxMagnitudeNumber<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Computes the element-wise number with the largest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> MaxMagnitudeNumber<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            TensorOperation.ValidateCompatibility(in x, in y, in destination);
            TensorOperation.Invoke<TensorOperation.MaxMagnitudeNumber<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>Computes the element-wise number with the largest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> MaxMagnitudeNumber<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : INumber<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.MaxMagnitudeNumber<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Computes the element-wise number with the largest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> MaxMagnitudeNumber<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            TensorOperation.ValidateCompatibility(in x, in destination);
            TensorOperation.Invoke<TensorOperation.MaxMagnitudeNumber<T>, T, T>(x, y, destination);
            return ref destination;
        }
        #endregion

        #region MaxNumber
        /// <summary>Searches for the largest number in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static T MaxNumber<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : INumber<T>
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            T result = x._reference;
            TensorOperation.Invoke<TensorOperation.MaxNumber<T>, T, T>(x, ref result);
            return result;
        }

        /// <summary>Computes the element-wise maximum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> MaxNumber<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : INumber<T>
        {
            TensorOperation.ValidateCompatibility(in x, in y, out Tensor<T> destination);
            TensorOperation.Invoke<TensorOperation.MaxNumber<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Computes the element-wise maximum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> MaxNumber<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            TensorOperation.ValidateCompatibility(in x, in y, in destination);
            TensorOperation.Invoke<TensorOperation.MaxNumber<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>Computes the element-wise maximum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> MaxNumber<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : INumber<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.MaxNumber<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Computes the element-wise maximum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> MaxNumber<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            TensorOperation.ValidateCompatibility(in x, in destination);
            TensorOperation.Invoke<TensorOperation.MaxNumber<T>, T, T>(x, y, destination);
            return ref destination;
        }
        #endregion

        #region Min
        /// <summary>Searches for the smallest number in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static T Min<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : INumber<T>
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            T result = x._reference;
            TensorOperation.Invoke<TensorOperation.Min<T>, T, T>(x, ref result);
            return result;
        }

        /// <summary>Computes the element-wise minimum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> Min<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : INumber<T>
        {
            TensorOperation.ValidateCompatibility(in x, in y, out Tensor<T> output);
            TensorOperation.Invoke<TensorOperation.Min<T>, T, T>(x, y, output);
            return output;
        }

        /// <summary>Computes the element-wise minimum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> Min<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            TensorOperation.ValidateCompatibility(in x, in y, in destination);
            TensorOperation.Invoke<TensorOperation.Min<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>Computes the element-wise minimum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> Min<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : INumber<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Min<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Computes the element-wise minimum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> Min<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            TensorOperation.ValidateCompatibility(in x, in destination);
            TensorOperation.Invoke<TensorOperation.Min<T>, T, T>(x, y, destination);
            return ref destination;
        }
        #endregion

        #region MinMagnitude
        /// <summary>Searches for the number with the smallest magnitude in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static T MinMagnitude<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : INumber<T>
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            T result = x._reference;
            TensorOperation.Invoke<TensorOperation.MinMagnitude<T>, T, T>(x, ref result);
            return result;
        }

        /// <summary>Computes the element-wise number with the smallest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> MinMagnitude<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : INumber<T>
        {
            TensorOperation.ValidateCompatibility(in x, in y, out Tensor<T> destination);
            TensorOperation.Invoke<TensorOperation.MinMagnitude<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Computes the element-wise number with the smallest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> MinMagnitude<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            TensorOperation.ValidateCompatibility(in x, in y, in destination);
            TensorOperation.Invoke<TensorOperation.MinMagnitude<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>Computes the element-wise number with the smallest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> MinMagnitude<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : INumber<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.MinMagnitude<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Computes the element-wise number with the smallest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> MinMagnitude<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            TensorOperation.ValidateCompatibility(in x, in destination);
            TensorOperation.Invoke<TensorOperation.MinMagnitude<T>, T, T>(x, y, destination);
            return ref destination;
        }
        #endregion

        #region MinMagnitudeNumber
        /// <summary>Searches for the number with the smallest magnitude in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static T MinMagnitudeNumber<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : INumberBase<T>
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            T result = x._reference;
            TensorOperation.Invoke<TensorOperation.MinMagnitudeNumber<T>, T, T>(x, ref result);
            return result;
        }

        /// <summary>Computes the element-wise number with the smallest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> MinMagnitudeNumber<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : INumber<T>
        {
            TensorOperation.ValidateCompatibility(in x, in y, out Tensor<T> destination);
            TensorOperation.Invoke<TensorOperation.MinMagnitudeNumber<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Computes the element-wise number with the smallest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> MinMagnitudeNumber<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            TensorOperation.ValidateCompatibility(in x, in y, in destination);
            TensorOperation.Invoke<TensorOperation.MinMagnitudeNumber<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>Computes the element-wise number with the smallest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> MinMagnitudeNumber<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : INumber<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.MinMagnitudeNumber<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Computes the element-wise number with the smallest magnitude in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> MinMagnitudeNumber<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            TensorOperation.ValidateCompatibility(in x, in destination);
            TensorOperation.Invoke<TensorOperation.MinMagnitudeNumber<T>, T, T>(x, y, destination);
            return ref destination;
        }
        #endregion

        #region MinNumber
        /// <summary>Searches for the smallest number in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static T MinNumber<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : INumber<T>
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            T result = x._reference;
            TensorOperation.Invoke<TensorOperation.MinNumber<T>, T, T>(x, ref result);
            return result;
        }

        /// <summary>Computes the element-wise minimum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> MinNumber<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : INumber<T>
        {
            TensorOperation.ValidateCompatibility(in x, in y, out Tensor<T> destination);
            TensorOperation.Invoke<TensorOperation.MinNumber<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Computes the element-wise minimum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> MinNumber<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            TensorOperation.ValidateCompatibility(in x, in y, in destination);
            TensorOperation.Invoke<TensorOperation.MinNumber<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>Computes the element-wise minimum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        public static Tensor<T> MinNumber<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : INumber<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.MinNumber<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Computes the element-wise minimum of the numbers in the specified tensors.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        public static ref readonly TensorSpan<T> MinNumber<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : INumber<T>
        {
            TensorOperation.ValidateCompatibility(in x, in destination);
            TensorOperation.Invoke<TensorOperation.MinNumber<T>, T, T>(x, y, destination);
            return ref destination;
        }
        #endregion

        #region Multiply
        /// <summary>
        /// Multiplies each element of <paramref name="x"/> with <paramref name="y"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">Input <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="y"><typeparamref name="T"/> value to multiply by.</param>
        public static Tensor<T> Multiply<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            Tensor<T> destination = Tensor.Create<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Multiply<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Multiplies each element of <paramref name="x"/> with <paramref name="y"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// </summary>
        /// <param name="x">Left <see cref="ReadOnlyTensorSpan{T}"/> for multiplication.</param>
        /// <param name="y">Right <see cref="ReadOnlyTensorSpan{T}"/> for multiplication.</param>
        public static Tensor<T> Multiply<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            TensorOperation.ValidateCompatibility(x, y, out Tensor<T> destination);
            TensorOperation.Invoke<TensorOperation.Multiply<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Multiplies each element of <paramref name="x"/> with <paramref name="y"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">Input <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="y"><typeparamref name="T"/> value to multiply by.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Multiply<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Multiply<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>
        /// Multiplies each element of <paramref name="x"/> with <paramref name="y"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// If the shapes are not the same they are broadcast to the smallest compatible shape.
        /// </summary>
        /// <param name="x">Left <see cref="ReadOnlyTensorSpan{T}"/> for multiplication.</param>
        /// <param name="y">Right <see cref="ReadOnlyTensorSpan{T}"/> for multiplication.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Multiply<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            TensorOperation.ValidateCompatibility(x, y, destination);
            TensorOperation.Invoke<TensorOperation.Multiply<T>, T, T>(x, y, destination);
            return ref destination;
        }
        #endregion

        #region Negate
        /// <summary>Computes the element-wise negation of each number in the specified tensor.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/></param>
        public static Tensor<T> Negate<T>(in ReadOnlyTensorSpan<T> x)
            where T : IUnaryNegationOperators<T, T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Negate<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>Computes the element-wise negation of each number in the specified tensor.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Negate<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IUnaryNegationOperators<T, T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Negate<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region Norm
        /// <summary>
        ///  Takes the norm of the <see cref="ReadOnlyTensorSpan{T}"/> and returns the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the norm of.</param>
        public static T Norm<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : IRootFunctions<T>
        {
            T result = T.AdditiveIdentity;
            TensorOperation.Invoke<TensorOperation.SumOfSquares<T>, T, T>(x, ref result);
            return T.Sqrt(result);
        }
        #endregion

        #region OnesComplement
        /// <summary>Computes the element-wise one's complement of numbers in the specified tensor.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/></param>
        public static Tensor<T> OnesComplement<T>(in ReadOnlyTensorSpan<T> x)
            where T : IBitwiseOperators<T, T, T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.OnesComplement<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>Computes the element-wise one's complement of numbers in the specified tensor.</summary>
        /// <param name="y">The <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> OnesComplement<T>(scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IBitwiseOperators<T, T, T>
        {
            TensorOperation.ValidateCompatibility(y, destination);
            TensorOperation.Invoke<TensorOperation.OnesComplement<T>, T, T>(y, destination);
            return ref destination;
        }
        #endregion

        #region PopCount
        /// <summary>Computes the element-wise population count of numbers in the specified tensor.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/></param>
        public static Tensor<T> PopCount<T>(in ReadOnlyTensorSpan<T> x)
            where T : IBinaryInteger<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.PopCount<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>Computes the element-wise population count of numbers in the specified tensor.</summary>
        /// <param name="y">The <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> PopCount<T>(scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IBinaryInteger<T>
        {
            TensorOperation.ValidateCompatibility(y, destination);
            TensorOperation.Invoke<TensorOperation.PopCount<T>, T, T>(y, destination);
            return ref destination;
        }
        #endregion

        #region Pow
        /// <summary>Computes the element-wise power of a number in a specified tensor raised to a number in another specified tensors.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The second input <see cref="ReadOnlyTensorSpan{T}"/></param>
        public static Tensor<T> Pow<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IPowerFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, y, out Tensor<T> destination);
            TensorOperation.Invoke<TensorOperation.Pow<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Computes the element-wise power of a number in a specified tensor raised to a number in another specified tensors.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The second input <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Pow<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IPowerFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, y, destination);
            TensorOperation.Invoke<TensorOperation.Pow<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>Computes the element-wise power of a number in a specified tensor raised to a number in another specified tensors.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The second input</param>
        public static Tensor<T> Pow<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IPowerFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Pow<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Computes the element-wise power of a number in a specified tensor raised to a number in another specified tensors.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The second input <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Pow<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : IPowerFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Pow<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>Computes the element-wise power of a number in a specified tensor raised to a number in another specified tensors.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The second input</param>
        public static Tensor<T> Pow<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : IPowerFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(y.Lengths);
            TensorOperation.Invoke<TensorOperation.Pow<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Computes the element-wise power of a number in a specified tensor raised to a number in another specified tensors.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The second input <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Pow<T>(T x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IPowerFunctions<T>
        {
            TensorOperation.ValidateCompatibility(y, destination);
            TensorOperation.Invoke<TensorOperation.Pow<T>, T, T>(x, y, destination);
            return ref destination;
        }
        #endregion

        #region Product
        /// <summary>Computes the product of all elements in the specified non-empty tensor of numbers.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static T Product<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : IMultiplicativeIdentity<T, T>, IMultiplyOperators<T, T, T>
        {
            T destination = T.MultiplicativeIdentity;
            TensorOperation.Invoke<TensorOperation.Product<T>, T, T>(x, ref destination);
            return destination;
        }
        #endregion

        #region RadiansToDegrees
        /// <summary>Computes the element-wise conversion of each number of radians in the specified tensor to degrees.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> RadiansToDegrees<T>(in ReadOnlyTensorSpan<T> x)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.RadiansToDegrees<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>Computes the element-wise conversion of each number of radians in the specified tensor to degrees.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> RadiansToDegrees<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.RadiansToDegrees<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region Reciprocal
        /// <summary>Computes the element-wise reciprocal of numbers in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Reciprocal<T>(in ReadOnlyTensorSpan<T> x)
            where T : IFloatingPoint<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Reciprocal<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>Computes the element-wise reciprocal of numbers in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Reciprocal<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IFloatingPoint<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Reciprocal<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region RootN
        /// <summary>Computes the element-wise n-th root of the values in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="n">The degree of the root to be computed, represented as a scalar.</param>
        public static Tensor<T> RootN<T>(in ReadOnlyTensorSpan<T> x, int n)
            where T : IRootFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.RootN<T>, T, T>(x, n, destination);
            return destination;
        }

        /// <summary>Computes the element-wise n-th root of the values in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <param name="n">The degree of the root to be computed, represented as a scalar.</param>
        public static ref readonly TensorSpan<T> RootN<T>(scoped in ReadOnlyTensorSpan<T> x, int n, in TensorSpan<T> destination)
            where T : IRootFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.RootN<T>, T, T>(x, n, destination);
            return ref destination;
        }
        #endregion

        #region RotateLeft
        /// <summary>Computes the element-wise rotation left of numbers in the specified tensor by the specified rotation amount.</summary>
        /// <param name="x">The tensor</param>
        /// <param name="rotateAmount">The number of bits to rotate, represented as a scalar.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        public static Tensor<T> RotateLeft<T>(in ReadOnlyTensorSpan<T> x, int rotateAmount)
            where T : IBinaryInteger<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.RotateLeft<T>, T, T>(x, rotateAmount, destination);
            return destination;
        }

        /// <summary>Computes the element-wise rotation left of numbers in the specified tensor by the specified rotation amount.</summary>
        /// <param name="x">The tensor</param>
        /// <param name="rotateAmount">The number of bits to rotate, represented as a scalar.</param>
        /// <param name="destination"></param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        public static ref readonly TensorSpan<T> RotateLeft<T>(scoped in ReadOnlyTensorSpan<T> x, int rotateAmount, in TensorSpan<T> destination)
            where T : IBinaryInteger<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.RotateLeft<T>, T, T>(x, rotateAmount, destination);
            return ref destination;
        }
        #endregion

        #region RotateRight
        /// <summary>Computes the element-wise rotation right of numbers in the specified tensor by the specified rotation amount.</summary>
        /// <param name="x">The tensor</param>
        /// <param name="rotateAmount">The number of bits to rotate, represented as a scalar.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        public static Tensor<T> RotateRight<T>(in ReadOnlyTensorSpan<T> x, int rotateAmount)
            where T : IBinaryInteger<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.RotateRight<T>, T, T>(x, rotateAmount, destination);
            return destination;
        }

        /// <summary>Computes the element-wise rotation right of numbers in the specified tensor by the specified rotation amount.</summary>
        /// <param name="x">The tensor</param>
        /// <param name="rotateAmount">The number of bits to rotate, represented as a scalar.</param>
        /// <param name="destination"></param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        public static ref readonly TensorSpan<T> RotateRight<T>(scoped in ReadOnlyTensorSpan<T> x, int rotateAmount, in TensorSpan<T> destination)
            where T : IBinaryInteger<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.RotateRight<T>, T, T>(x, rotateAmount, destination);
            return ref destination;
        }
        #endregion

        #region Round
        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Round<T>(in ReadOnlyTensorSpan<T> x)
            where T : IFloatingPoint<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Round<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Round<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IFloatingPoint<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Round<T>, T, T>(x, destination);
            return ref destination;
        }

        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="digits"></param>
        /// <param name="mode"></param>
        public static Tensor<T> Round<T>(in ReadOnlyTensorSpan<T> x, int digits, MidpointRounding mode)
            where T : IFloatingPoint<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Round<T>, T, Tuple<int, MidpointRounding>, T>(x, Tuple.Create(digits, mode), destination);
            return destination;
        }

        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="digits"></param>
        /// <param name="mode"></param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Round<T>(scoped in ReadOnlyTensorSpan<T> x, int digits, MidpointRounding mode, in TensorSpan<T> destination)
            where T : IFloatingPoint<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Round<T>, T, Tuple<int, MidpointRounding>, T>(x, Tuple.Create(digits, mode), in destination);
            return ref destination;
        }

        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="digits"></param>
        public static Tensor<T> Round<T>(in ReadOnlyTensorSpan<T> x, int digits)
            where T : IFloatingPoint<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Round<T>, T, Tuple<int, MidpointRounding>, T>(x, Tuple.Create(digits, MidpointRounding.ToEven), destination);
            return destination;
        }

        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="digits"></param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Round<T>(scoped in ReadOnlyTensorSpan<T> x, int digits, in TensorSpan<T> destination)
            where T : IFloatingPoint<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Round<T>, T, Tuple<int, MidpointRounding>, T>(x, Tuple.Create(digits, MidpointRounding.ToEven), in destination);
            return ref destination;
        }

        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="mode"></param>
        public static Tensor<T> Round<T>(in ReadOnlyTensorSpan<T> x, MidpointRounding mode)
            where T : IFloatingPoint<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Round<T>, T, Tuple<int, MidpointRounding>, T>(x, Tuple.Create(0, mode), destination);
            return destination;
        }

        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="mode"></param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Round<T>(scoped in ReadOnlyTensorSpan<T> x, MidpointRounding mode, in TensorSpan<T> destination)
            where T : IFloatingPoint<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Round<T>, T, Tuple<int, MidpointRounding>, T>(x, Tuple.Create(0, mode), in destination);
            return ref destination;
        }
        #endregion

        #region Sigmoid
        /// <summary>Computes the element-wise sigmoid function on the specified non-empty tensor of numbers.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Sigmoid<T>(in ReadOnlyTensorSpan<T> x)
            where T : IExponentialFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Sigmoid<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>Computes the element-wise sigmoid function on the specified non-empty tensor of numbers.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Sigmoid<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IExponentialFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Sigmoid<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region Sin
        /// <summary>
        /// Takes the sin of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        public static Tensor<T> Sin<T>(in ReadOnlyTensorSpan<T> x)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Sin<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>
        /// Takes the sin of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Sin<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Sin<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region Sinh
        /// <summary>Computes the element-wise hyperbolic sine of each radian angle in the specified tensor.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        public static Tensor<T> Sinh<T>(in ReadOnlyTensorSpan<T> x)
            where T : IHyperbolicFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Sinh<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>Computes the element-wise hyperbolic sine of each radian angle in the specified tensor.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Sinh<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IHyperbolicFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Sinh<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region SinPi
        /// <summary>Computes the element-wise sine of the value in the specified tensor that has been multiplied by Pi.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        public static Tensor<T> SinPi<T>(in ReadOnlyTensorSpan<T> x)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.SinPi<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>Computes the element-wise sine of the value in the specified tensor that has been multiplied by Pi.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> SinPi<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.SinPi<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region SoftMax
        /// <summary>Computes the softmax function over the specified non-empty tensor of numbers.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        public static Tensor<T> SoftMax<T>(in ReadOnlyTensorSpan<T> x)
            where T : IExponentialFunctions<T>
        {
            T sumExp = T.AdditiveIdentity;
            TensorOperation.Invoke<TensorOperation.SumExp<T>, T, T>(x, ref sumExp);

            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.SoftMax<T>, T, T>(x, sumExp, destination);
            return destination;
        }

        /// <summary>Computes the softmax function over the specified non-empty tensor of numbers.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> SoftMax<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IExponentialFunctions<T>
        {
            T sumExp = T.AdditiveIdentity;
            TensorOperation.Invoke<TensorOperation.SumExp<T>, T, T>(x, ref sumExp);

            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.SoftMax<T>, T, T>(x, sumExp, destination);
            return ref destination;
        }
        #endregion

        #region Sqrt
        /// <summary>
        /// Takes the square root of each element of the <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the square root of.</param>
        public static Tensor<T> Sqrt<T>(in ReadOnlyTensorSpan<T> x)
            where T : IRootFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Sqrt<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>
        /// Takes the square root of each element of the <paramref name="x"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the square root of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Sqrt<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IRootFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Sqrt<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region StdDev
        /// <summary>
        /// Returns the standard deviation of the elements in the <paramref name="x"/> tensor.
        /// </summary>
        /// <param name="x">The <see cref="TensorSpan{T}"/> to take the standard deviation of.</param>
        /// <returns><typeparamref name="T"/> representing the standard deviation.</returns>
        public static T StdDev<T>(in ReadOnlyTensorSpan<T> x)
            where T : IRootFunctions<T>
        {
            T mean = Average(x);
            T result = T.AdditiveIdentity;
            TensorOperation.Invoke<TensorOperation.SumOfSquaredDifferences<T>, T, T>(x, mean, ref result);
            T variance = result / T.CreateChecked(x.FlattenedLength);
            return T.Sqrt(variance);

        }
        #endregion

        #region Subtract
        /// <summary>
        /// Subtracts <paramref name="y"/> from each element of <paramref name="x"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The <typeparamref name="T"/> to subtract.</param>
        public static Tensor<T> Subtract<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : ISubtractionOperators<T, T, T>
        {
            Tensor<T> destination = CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Subtract<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Subtracts each element of <paramref name="y"/> from <paramref name="x"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <typeparamref name="T"/> to be subtracted from.</param>
        /// <param name="y">The <see cref="ReadOnlyTensorSpan{T}"/> of values to subtract.</param>
        public static Tensor<T> Subtract<T>(T x, in ReadOnlyTensorSpan<T> y)
            where T : ISubtractionOperators<T, T, T>
        {
            Tensor<T> destination = CreateUninitialized<T>(y.Lengths);
            TensorOperation.Invoke<TensorOperation.Subtract<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Subtracts each element of <paramref name="x"/> from <paramref name="y"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> with values to be subtracted from.</param>
        /// <param name="y">The <see cref="ReadOnlyTensorSpan{T}"/> with values to subtract.</param>
        public static Tensor<T> Subtract<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : ISubtractionOperators<T, T, T>
        {
            TensorOperation.ValidateCompatibility(x, y, out Tensor<T> destination);
            TensorOperation.Invoke<TensorOperation.Subtract<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Subtracts <paramref name="y"/> from each element of <paramref name="x"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> with values to be subtracted from.</param>
        /// <param name="y">The <typeparamref name="T"/> value to subtract.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Subtract<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : ISubtractionOperators<T, T, T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Subtract<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>
        /// Subtracts each element of <paramref name="y"/> from <paramref name="x"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <typeparamref name="T"/> value to be subtracted from.</param>
        /// <param name="y">The <see cref="ReadOnlyTensorSpan{T}"/> values to subtract.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Subtract<T>(T x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : ISubtractionOperators<T, T, T>
        {
            TensorOperation.ValidateCompatibility(y, destination);
            TensorOperation.Invoke<TensorOperation.Subtract<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>
        /// Subtracts each element of <paramref name="x"/> from <paramref name="y"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> of values to be subtracted from.</param>
        /// <param name="y">The <see cref="ReadOnlyTensorSpan{T}"/>of values to subtract.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Subtract<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : ISubtractionOperators<T, T, T>
        {
            TensorOperation.ValidateCompatibility(x, y, destination);
            TensorOperation.Invoke<TensorOperation.Subtract<T>, T, T>(x, y, destination);
            return ref destination;
        }
        #endregion

        #region Sum
        /// <summary>
        /// Sums the elements of the specified tensor.
        /// </summary>
        /// <param name="x">Tensor to sum</param>
        /// <returns></returns>
        public static T Sum<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            T destination = T.AdditiveIdentity;
            TensorOperation.Invoke<TensorOperation.Sum<T>, T, T>(x, ref destination);
            return destination;
        }
        #endregion

        #region SumOfSquares
        /// <summary>
        /// Sums the squared elements of the specified tensor.
        /// </summary>
        /// <param name="x">Tensor to sum squares of</param>
        /// <returns></returns>
        internal static T SumOfSquares<T>(scoped in ReadOnlyTensorSpan<T> x)
            where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>, IMultiplyOperators<T, T, T>
        {
            T result = T.AdditiveIdentity;
            TensorOperation.Invoke<TensorOperation.SumOfSquares<T>, T, T>(x, ref result);
            return result;
        }
        #endregion

        #region Tan
        /// <summary>Computes the element-wise tangent of the value in the specified tensor.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        public static Tensor<T> Tan<T>(in ReadOnlyTensorSpan<T> x)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Tan<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>Computes the element-wise tangent of the value in the specified tensor.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Tan<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Tan<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region Tanh
        /// <summary>Computes the element-wise hyperbolic tangent of each radian angle in the specified tensor.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        public static Tensor<T> Tanh<T>(in ReadOnlyTensorSpan<T> x)
            where T : IHyperbolicFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Tanh<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>Computes the element-wise hyperbolic tangent of each radian angle in the specified tensor.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Tanh<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IHyperbolicFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Tanh<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region TanPi
        /// <summary>Computes the element-wise tangent of the value in the specified tensor that has been multiplied by Pi.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        public static Tensor<T> TanPi<T>(in ReadOnlyTensorSpan<T> x)
            where T : ITrigonometricFunctions<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.TanPi<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>Computes the element-wise tangent of the value in the specified tensor that has been multiplied by Pi.</summary>
        /// <param name="x">The <see cref="ReadOnlyTensorSpan{T}"/> to take the sin of.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> TanPi<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.TanPi<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region TrailingZeroCount
        /// <summary>Computes the element-wise trailing zero count of numbers in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> TrailingZeroCount<T>(in ReadOnlyTensorSpan<T> x)
            where T : IBinaryInteger<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.TrailingZeroCount<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>Computes the element-wise trailing zero count of numbers in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> TrailingZeroCount<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IBinaryInteger<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.TrailingZeroCount<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region Truncate
        /// <summary>Computes the element-wise truncation of numbers in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Truncate<T>(in ReadOnlyTensorSpan<T> x)
            where T : IFloatingPoint<T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Truncate<T>, T, T>(x, destination);
            return destination;
        }

        /// <summary>Computes the element-wise truncation of numbers in the specified tensor.</summary>
        /// <param name="x">The input <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Truncate<T>(scoped in ReadOnlyTensorSpan<T> x, in TensorSpan<T> destination)
            where T : IFloatingPoint<T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Truncate<T>, T, T>(x, destination);
            return ref destination;
        }
        #endregion

        #region Xor
        /// <summary>Computes the element-wise XOR of numbers in the specified tensors.</summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        public static Tensor<T> Xor<T>(in ReadOnlyTensorSpan<T> x, in ReadOnlyTensorSpan<T> y)
            where T : IBitwiseOperators<T, T, T>
        {
            TensorOperation.ValidateCompatibility(x, y, out Tensor<T> destination);
            TensorOperation.Invoke<TensorOperation.Xor<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>Computes the element-wise XOR of numbers in the specified tensors.</summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The right <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Xor<T>(scoped in ReadOnlyTensorSpan<T> x, scoped in ReadOnlyTensorSpan<T> y, in TensorSpan<T> destination)
            where T : IBitwiseOperators<T, T, T>
        {
            TensorOperation.ValidateCompatibility(x, y, destination);
            TensorOperation.Invoke<TensorOperation.Xor<T>, T, T>(x, y, destination);
            return ref destination;
        }

        /// <summary>
        /// Computes the element-wise Xor of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="Tensor{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The second value.</param>
        public static Tensor<T> Xor<T>(in ReadOnlyTensorSpan<T> x, T y)
            where T : IBitwiseOperators<T, T, T>
        {
            Tensor<T> destination = Tensor.CreateUninitialized<T>(x.Lengths);
            TensorOperation.Invoke<TensorOperation.Xor<T>, T, T>(x, y, destination);
            return destination;
        }

        /// <summary>
        /// Computes the element-wise Xor of the two input <see cref="ReadOnlyTensorSpan{T}"/> and returns a new <see cref="TensorSpan{T}"/> with the result.
        /// </summary>
        /// <param name="x">The left <see cref="ReadOnlyTensorSpan{T}"/>.</param>
        /// <param name="y">The second value.</param>
        /// <param name="destination"></param>
        public static ref readonly TensorSpan<T> Xor<T>(scoped in ReadOnlyTensorSpan<T> x, T y, in TensorSpan<T> destination)
            where T : IBitwiseOperators<T, T, T>
        {
            TensorOperation.ValidateCompatibility(x, destination);
            TensorOperation.Invoke<TensorOperation.Xor<T>, T, T>(x, y, destination);
            return ref destination;
        }
        #endregion
        #endregion
    }
}
