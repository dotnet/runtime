// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using static System.Numerics.Tensors.TensorOperation;

namespace System.Numerics.Tensors
{
    /// <summary>
    /// Represents a read-only view of a tensor dimension.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public readonly ref struct ReadOnlyTensorDimensionView<T>
    {
        private readonly ReadOnlyTensorSpan<T> _tensor;
        private readonly int _dimension;
        private readonly nint _count;
        //private readonly

        internal ReadOnlyTensorDimensionView(ReadOnlyTensorSpan<T> tensor, int dimension)
        {
            if (dimension < 0 || dimension >= tensor.Rank)
            {
                ThrowHelper.ThrowArgument_InvalidDimension();
            }

            _tensor = tensor;
            _dimension = dimension;
            _count = TensorPrimitives.Product(tensor.Lengths.Slice(0, tensor.Rank - dimension));
        }

        /// <summary>
        /// The length of the dimension
        /// </summary>
        public nint Count => _count;

        /// <summary>
        /// Returns a tensor that represents the slice of the tensor at the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ReadOnlyTensorSpan<T> GetSlice(int index)
        {
            // This is not optimized, but it is a correct one.
            scoped Span<NRange> indexes = RentedBuffer.CreateUninitialized(_tensor.Rank, out RentedBuffer<NRange> rentedBuffer);

            indexes.Fill(NRange.All);
            for (int i = 0; i < _dimension; i++)
            {
                indexes[i] = 0..1;
            }
            indexes[_dimension] = -1..0;
            for (int i = 0; i < index; i++)
            {
                TensorShape.AdjustToNextIndex(indexes, _dimension, _tensor.Lengths);
            }
            ReadOnlyTensorSpan<T> slice = _tensor[indexes];
            rentedBuffer.Dispose();
            return slice;
        }

        /// <summary>
        /// Gets an enumerator that iterates through the dimension.
        /// </summary>
        /// <returns></returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(_tensor, _dimension);
        }

        /// <summary>
        /// Enumerates the slices of the tensor dimension.
        /// </summary>
        public ref struct Enumerator
#if NET9_0_OR_GREATER
  : IEnumerator<ReadOnlyTensorSpan<T>>
#endif
        {
            private readonly ReadOnlyTensorSpan<T> _tensor;
            private readonly int _dimension;
            private readonly NRange[] _rentedBuffer;
            private readonly Span<NRange> _indexes;

            internal Enumerator(ReadOnlyTensorSpan<T> tensor, int dimension)
            {
                _tensor = tensor;
                _dimension = dimension;
                _rentedBuffer = ArrayPool<NRange>.Shared.Rent(tensor.Rank);

                _indexes = _rentedBuffer.AsSpan(0, tensor.Rank);
                _indexes.Clear();

                _indexes.Fill(NRange.All);
                Reset();
            }

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns><see langword="true"/> if the enumerator moved, <see langword="false"/> otherwise.</returns>
            public bool MoveNext() => TensorShape.AdjustToNextIndex(_indexes, _dimension, _tensor.Lengths);

            /// <summary>
            /// Resets the enumerator to the beginning of the span.
            /// </summary>
            public void Reset()
            {
                for (int i = 0; i < _dimension; i++)
                {
                    _indexes[i] = 0..1;
                }
                _indexes[_dimension] = -1..0;
            }

            /// <summary>
            /// Disposes of the enumerator.
            /// </summary>
            public void Dispose()
            {
                ArrayPool<NRange>.Shared.Return(_rentedBuffer);
            }

            /// <summary>
            /// Current <see cref="Tensor{T}"/> value of the <see cref="IEnumerator{T}"/>
            /// </summary>
            public ReadOnlyTensorSpan<T> Current => _tensor[_indexes];

#if NET9_0_OR_GREATER
            // This will always just throw but needs to be here.
            //TODO: What error do we throw for this Tanner?
            object IEnumerator.Current => throw new NotImplementedException();

            ReadOnlyTensorSpan<T> IEnumerator<ReadOnlyTensorSpan<T>>.Current
            {
                get => Current;
            }
#endif
        }
    }
}
