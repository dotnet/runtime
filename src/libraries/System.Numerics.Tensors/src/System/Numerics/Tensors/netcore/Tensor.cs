// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using static System.Runtime.InteropServices.JavaScript.JSType;

#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable 8500 // address / sizeof of managed types

namespace System.Numerics.Tensors
{
    public sealed class Tensor<T>
        : ITensor<Tensor<T>, T>
    {
        /// <summary>A byref or a native ptr.</summary>
        internal readonly T[] _values;
        /// <summary>The number of elements this Tensor contains.</summary>
        internal readonly nint _linearLength;
        /// <summary>The lengths of each dimension.</summary>
        internal readonly nint[] _lengths;
        /// <summary>The strides representing the memory offsets for each dimension.</summary>
        internal readonly nint[] _strides;
        /// <summary>If the backing memory is permanently pinned (so not just using a fixed statement).</summary>
        internal readonly bool _isPinned;

        /// <summary>
        /// Creates a new empty Tensor.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Tensor()
        {
            _linearLength = 0;
            _values = [];
            _lengths = [];
            _strides = [];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Tensor(T[] values, ReadOnlySpan<nint> lengths, bool isPinned)
        {
            _linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);

            _values = values;
            _lengths = lengths.ToArray();
            _strides = TensorSpanHelpers.CalculateStrides(_lengths);
            _isPinned = isPinned;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Tensor(T[] values, ReadOnlySpan<nint> lengths, nint[] strides, bool isPinned)
        {
            _linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);

            _values = values;
            _lengths = lengths.ToArray();
            _strides = strides.ToArray();
            if (strides == Array.Empty<nint>())
                _strides = TensorSpanHelpers.CalculateStrides(lengths);
            _isPinned = isPinned;

        }

        // ITensor

        /// <summary>
        /// The Empty Tensor.
        /// </summary>
        public static Tensor<T> Empty { get; } = new();

        /// <summary>
        /// Gets a value indicating whether this <see cref="Tensor{T}"/> is empty.
        /// </summary>
        /// <value><see langword="true"/> if this tensor is empty; otherwise, <see langword="false"/>.</value>
        public bool IsEmpty => _lengths.Length == 0;

        /// <summary>
        /// Gets a value indicating whether the backing memory of the <see cref="Tensor{T}"/> is pinned."/>
        /// </summary>
        /// <value><see langword="true"/> if the backing memory is pinned; otherwise, <see langword="false"/>.</value>
        public bool IsPinned => _isPinned;

        /// <summary>
        /// Gets a value indicating the rank, or number of dimensions, of this <see cref="Tensor{T}"/>.
        /// </summary>
        /// <value><see cref="nint"/> with the number of dimensions.</value>
        public int Rank => _lengths.Length;

        /// <summary>
        /// The number of items in the <see cref="Tensor{T}"/>.
        /// </summary>
        /// <value><see cref="nint"/> with the number of items.</value>
        public nint LinearLength => _linearLength;

        // REIVEW: Calling this Shape for now as Lengths didn't seem to be the most liked option based on our discussion. Can rename if we desire.
        /// <summary>
        /// Gets the length of each dimension in this <see cref="Tensor{T}"/>.
        /// </summary>
        /// <value><see cref="ReadOnlySpan{T}"/> with the lengths of each dimension.</value>
        public ReadOnlySpan<nint> Shape => _lengths;

        /// <summary>
        /// Gets the strides of each dimension in this <see cref="Tensor{T}"/>.
        /// </summary>
        /// <value><see cref="ReadOnlySpan{T}"/> with the strides of each dimension.</value>
        public ReadOnlySpan<nint> Strides => _strides;

        /// <summary>
        /// Returns a reference to specified element of the Span.
        /// </summary>
        /// <param name="indices"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when index less than 0 or index greater than or equal to Length
        /// </exception>
        public ref T this[params ReadOnlySpan<nint> indices] => ref AsTensorSpan()[indices];

        // REVIEW: WE WILL WANT THIS CHANGED FROM A BOOL TO SOME FILTER EXPRESSION.
        /// <summary>
        ///
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Tensor<T> this[Tensor<bool> filter]
        {
            get
            {
                if (filter.Shape.Length != Shape.Length)
                    throw new ArgumentOutOfRangeException(nameof(filter), "Number of dimensions does not equal the number of dimensions in the span");

                for (int i = 0; i < filter.Shape.Length; i++)
                {
                    if (filter.Shape[i] != Shape[i])
                        ThrowHelper.ThrowArgument_FilterTensorMustEqualTensorLength();
                }

                Span<T> srcSpan = _values;
                Span<bool> filterSpan = filter._values;

                nint linearLength = TensorHelpers.CountTrueElements(filter);

                T[] values = _isPinned ? GC.AllocateArray<T>((int)linearLength, _isPinned) : (new T[linearLength]);
                int index = 0;
                for (int i = 0; i < filterSpan.Length; i++)
                {
                    if (filterSpan[i])
                    {
                        values[i] = srcSpan[index++];
                    }
                }

                return new Tensor<T>(values, [linearLength], _isPinned);
            }
        }

        public static implicit operator TensorSpan<T>(Tensor<T> value) => new TensorSpan<T>(ref MemoryMarshal.GetArrayDataReference(value._values), value._lengths, value._strides, value._isPinned);

        public static implicit operator ReadOnlyTensorSpan<T>(Tensor<T> value) => new ReadOnlyTensorSpan<T>(ref MemoryMarshal.GetArrayDataReference(value._values), value._lengths, value._strides, value._isPinned);

        /// <summary>
        /// Converts this <see cref="Tensor{T}"/> to a <see cref="TensorSpan{T}"/> pointing to the same backing memory."/>
        /// </summary>
        /// <returns><see cref="TensorSpan{T}"/></returns>
        public TensorSpan<T> AsTensorSpan() => new TensorSpan<T>(ref MemoryMarshal.GetArrayDataReference(_values), _lengths, _strides, _isPinned);

        /// <summary>
        /// Converts this <see cref="Tensor{T}"/> to a <see cref="ReadOnlyTensorSpan{T}"/> pointing to the same backing memory."/>
        /// </summary>
        /// <returns><see cref="ReadOnlyTensorSpan{T}"/></returns>
        public ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan() => new ReadOnlyTensorSpan<T>(ref MemoryMarshal.GetArrayDataReference(_values), _lengths, _strides, _isPinned);

        /// <summary>
        /// Converts this <see cref="Tensor{T}"/> to a <see cref="TensorSpan{T}"/> pointing to the same backing memory based on the provided ranges."/>
        /// </summary>
        /// <param name="ranges">The ranges you want in the <see cref="TensorSpan{T}"/>.</param>
        /// <returns><see cref="TensorSpan{T}"/> based on the provided ranges.</returns>
        public TensorSpan<T> AsTensorSpan(params ReadOnlySpan<NRange> ranges) => AsTensorSpan().Slice(ranges);

        /// <summary>
        /// Converts this <see cref="Tensor{T}"/> to a <see cref="ReadOnlyTensorSpan{T}"/> pointing to the same backing memory based on the provided ranges."/>
        /// </summary>
        /// <param name="ranges">The ranges you want in the <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <returns></returns>
        public ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan(params ReadOnlySpan<NRange> ranges) => AsTensorSpan().Slice(ranges);

        /// <summary>
        /// Returns a reference to the 0th element of the Tensor. If the Tensor is empty, returns null reference.
        /// It can be used for pinning and is required to support the use of Tensor within a fixed statement.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref T GetPinnableReference() => ref AsTensorSpan().GetPinnableReference();

        /// <summary>
        /// Forms a slice out of the given tensor
        /// </summary>
        /// <param name="ranges">The ranges for the slice</param>
        /// <returns><see cref="Tensor{T}"/> as a copy of the provided ranges.</returns>
        // REVIEW: CURRENTLY DOES A COPY.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Tensor<T> Slice(params ReadOnlySpan<NRange> ranges)
        {
            if (ranges.Length != Shape.Length)
                throw new ArgumentOutOfRangeException(nameof(ranges), "Number of dimensions to slice does not equal the number of dimensions in the span");

            TensorSpan<T> s = AsTensorSpan(ranges);
            T[] values = _isPinned ? GC.AllocateArray<T>(checked((int)s.LinearLength), _isPinned) : (new T[s.LinearLength]);
            var outTensor = new Tensor<T>(values, s.Shape.ToArray(), _isPinned);
            s.CopyTo(outTensor);
            return outTensor;
        }

        /// <summary>
        /// Clears the contents of this tensor.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Clear() => AsTensorSpan().Clear();

        /// <summary>
        /// Copies the contents of this tensor into destination span. If the source
        /// and destinations overlap, this method behaves as if the original values in
        /// a temporary location before the destination is overwritten.
        /// </summary>
        /// <param name="destination">The span to copy items into.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the destination TensorSpan is shorter than the source Tensor.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(TensorSpan<T> destination) => AsTensorSpan().CopyTo(destination);

        /// <summary>
        /// Fills the contents of this span with the given value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(T value) => AsTensorSpan().Fill(value);

        /// <summary>
        /// Copies the contents of this tensor into destination span. If the source
        /// and destinations overlap, this method behaves as if the original values in
        /// a temporary location before the destination is overwritten.
        /// </summary>
        /// <param name="destination">The span to copy items into.</param>
        /// <returns>If the destination span is shorter than the source tensor, this method
        /// return false and no data is written to the destination.</returns>
        public bool TryCopyTo(TensorSpan<T> destination) => AsTensorSpan().TryCopyTo(destination);

        // IEnumerable
        /// <summary>
        /// Gets an <see cref="IEnumerator{T}"/> for the <see cref="Tensor{T}"/>.
        /// </summary>
        /// <returns><see cref="IEnumerator{T}"/></returns>
        public IEnumerator<T> GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// Gets an <see cref="IEnumerator"/> for the <see cref="Tensor{T}"/>."/>
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

        // IEquatable
        /// <summary>
        /// Returns true if left and right point at the same memory and have the same length and have the same rank.  Note that
        /// this does *not* check to see if the *contents* are equal.
        /// </summary>
        /// <param name="other"></param>
        public bool Equals(Tensor<T>? other)
        {
            if (other is null)
                return false;

            return _linearLength == other._linearLength &&
            Rank == other.Rank &&
            _lengths == other._lengths &&
            Unsafe.AreSame(ref _values[0], ref other._values[0]);
        }

        // REVIEW: REFERENCE EQUALITY VS DEEP EQUALITY. ALL OTHER OPERATORS WILL DO EXPENSIVE WORK.
        // IEqualityOperators
        /// <summary>
        /// Returns true if left and right point at the same memory and have the same length and have the same rank.  Note that
        /// this does *not* check to see if the *contents* are equal.
        /// </summary>
        public static bool operator ==(Tensor<T>? left, Tensor<T>? right)
        {
            if (left is null)
                return right is null;
            else if (right is null)
                return false;

            return left._linearLength == right._linearLength &&
                left.Rank == right.Rank &&
                left._lengths == right._lengths &&
                Unsafe.AreSame(ref left._values[0], ref right._values[0]);
        }

        /// <summary>
        /// Return true if the Tensor has only a single element and that element is equal to the right value.
        /// </summary>
        public static bool operator ==(Tensor<T> left, T right)
        {
            if (left is null)
                return false;
            if (right is null)
                return false;

            if (left._values is null)
                return false;

            T val = left._values[0];
            if (val is null)
                return false;

            return TensorSpanHelpers.CalculateTotalLength(left._lengths) == 1 &&
            val.Equals(right);
        }

        /// <summary>
        /// Returns false if left and right point at the same memory and have the same length and have the same rank.  Note that
        /// this does *not* check to see if the *contents* are equal.
        /// </summary>
        public static bool operator !=(Tensor<T>? left, Tensor<T>? right) => !(left == right);

        /// <summary>
        /// Return false if the Tensor has only a single element and that element is equal to the right value.
        /// </summary>
        public static bool operator !=(Tensor<T> left, T right) => !(left == right);

        private sealed class Enumerator : IEnumerator<T>
        {
            /// <summary>The span being enumerated.</summary>
            private readonly Tensor<T> _tensor;
            /// <summary>
            ///
            /// </summary>
            private nint[] _curIndices;
            /// <summary>The total item count.</summary>
            private nint _items;

            /// <summary>Initialize the enumerator.</summary>
            /// <param name="tensor">The tensor to enumerate.</param>
            internal Enumerator(Tensor<T> tensor)
            {
                _tensor = tensor;
                _items = -1;
                _curIndices = new nint[_tensor.Rank];
                _curIndices[_tensor.Rank - 1] = -1;
            }

            /// <summary>Advances the enumerator to the next element of the span.</summary>
            public bool MoveNext()
            {
                TensorSpanHelpers.AdjustIndices(_tensor.Rank - 1, 1, ref _curIndices, _tensor.Shape);

                _items++;
                return _items < _tensor.LinearLength;
            }

            /// <summary>
            /// Resets the enumerator to the beginning of the span.
            /// </summary>
            public void Reset()
            {
                Array.Clear(_curIndices);
                _curIndices[_tensor.Rank - 1] = -1;
            }

            /// <summary>
            ///
            /// </summary>
            public void Dispose()
            {

            }

            /// <summary>
            /// Current T value of the <see cref="IEnumerator{T}"/>
            /// </summary>
            T IEnumerator<T>.Current => _tensor[_curIndices];

            /// <summary>
            /// Current <see cref="object"/> of the <see cref="IEnumerator"/>
            /// </summary>
            object? IEnumerator.Current => _tensor[_curIndices];
        }

        // REVIEW: PENDING API REVIEW TO DETERMINE IMPLEMENTATION
        public override bool Equals(object? obj)
        {
            return obj is Tensor<T> tensor && Equals(tensor);
        }

        // REVIEW: PENDING API REVIEW TO DETERMINE IMPLEMENTATION
        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get a string representation of the tensor.
        /// </summary>
        private string ToMetadataString()
        {
            var sb = new StringBuilder("[");

            int n = Rank;
            if (n == 0)
            {
                sb.Append(']');
            }
            else
            {
                for (int i = 0; i < n; i++)
                {
                    sb.Append(Shape[i]);
                    if (i + 1 < n)
                        sb.Append('x');
                }

                sb.Append(']');
            }
            sb.Append($", type = {typeof(T)}, isPinned = {IsPinned}");

            return sb.ToString();
        }

        public string ToString(int maxRows, int maxColumns)
        {
            var sb = new StringBuilder();
            sb.AppendLine(ToMetadataString());
            sb.AppendLine("{");
            sb.Append(AsTensorSpan().ToString(10, 10));
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
