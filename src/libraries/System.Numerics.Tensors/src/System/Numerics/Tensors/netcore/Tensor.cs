// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
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
        internal readonly nint _flattenedLength;
        /// <summary>The lengths of each dimension.</summary>
        internal readonly nint[] _lengths;
        /// <summary>The strides representing the memory offsets for each dimension.</summary>
        internal readonly nint[] _strides;
        /// <summary>If the backing memory is permanently pinned (so not just using a fixed statement).</summary>
        internal readonly bool _isPinned;

        /// <summary>
        /// Creates a new empty Tensor.
        /// </summary>
        internal Tensor()
        {
            _flattenedLength = 0;
            _values = [];
            _lengths = [];
            _strides = [];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Tensor(T[]? values, ReadOnlySpan<nint> lengths, bool isPinned = false) : this(values, lengths, Array.Empty<nint>(), isPinned) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Tensor(T[]? values, ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides, bool isPinned = false)
        {
            if (values == null)
            {
                if (_flattenedLength != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                _flattenedLength = 0;
                _values = [];
                _lengths = [];
                _strides = [];
                return; // returns default
            }

            _lengths = lengths.IsEmpty ? [values.Length] : lengths.ToArray();

            _flattenedLength = TensorSpanHelpers.CalculateTotalLength(_lengths);
            _strides = strides.IsEmpty ? TensorSpanHelpers.CalculateStrides(_lengths, _flattenedLength) : strides.ToArray();
            TensorSpanHelpers.ValidateStrides(_strides, _lengths);
            nint maxElements = TensorSpanHelpers.ComputeMaxLinearIndex(_strides, _lengths);

            if (Environment.Is64BitProcess)
            {
                // See comment in Span<T>.Slice for how this works.
                if ((ulong)(uint)maxElements >= (ulong)(uint)values.Length && values.Length != 0)
                    ThrowHelper.ThrowArgument_InvalidStridesAndLengths();
            }
            else
            {
                if (((uint)maxElements >= (uint)(values.Length)) && values.Length != 0)
                    ThrowHelper.ThrowArgument_InvalidStridesAndLengths();
            }

            _values = values;
            _isPinned = isPinned;
        }

        /// <summary>
        /// Creates a <see cref="Tensor{T}"/> and initializes it with the default value of T. If <paramref name="pinned"/> is true, the memory will be pinned.
        /// </summary>
        /// <param name="lengths">A <see cref="ReadOnlySpan{T}"/> indicating the lengths of each dimension.</param>
        /// <param name="pinned">A <see cref="bool"/> whether the underlying data should be pinned or not.</param>
        static Tensor<T> ITensor<Tensor<T>, T>.Create(ReadOnlySpan<nint> lengths, bool pinned)
        {
            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);
            T[] values = pinned ? GC.AllocateArray<T>((int)linearLength, pinned) : (new T[linearLength]);
            return new Tensor<T>(values, lengths.ToArray(), pinned);
        }

        /// <summary>
        /// Creates a <see cref="Tensor{T}"/> and initializes it with the default value of T. If <paramref name="pinned"/> is true, the memory will be pinned.
        /// </summary>
        /// <param name="lengths">A <see cref="ReadOnlySpan{T}"/> indicating the lengths of each dimension.</param>
        /// <param name="strides">A <see cref="ReadOnlySpan{T}"/> indicating the strides of each dimension.</param>
        /// <param name="pinned">A <see cref="bool"/> whether the underlying data should be pinned or not.</param>
        static Tensor<T> ITensor<Tensor<T>, T>.Create(ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides, bool pinned)
        {
            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);
            T[] values = pinned ? GC.AllocateArray<T>((int)linearLength, pinned) : (new T[linearLength]);
            return new Tensor<T>(values, lengths.ToArray(), strides.ToArray(), pinned);
        }

        /// <summary>
        /// Creates a <see cref="Tensor{T}"/> and does not initialize it. If <paramref name="pinned"/> is true, the memory will be pinned.
        /// </summary>
        /// <param name="lengths">A <see cref="ReadOnlySpan{T}"/> indicating the lengths of each dimension.</param>
        /// <param name="pinned">A <see cref="bool"/> whether the underlying data should be pinned or not.</param>
        static Tensor<T> ITensor<Tensor<T>, T>.CreateUninitialized(ReadOnlySpan<nint> lengths, bool pinned)
        {
            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);
            T[] values = GC.AllocateUninitializedArray<T>((int)linearLength, pinned);
            return new Tensor<T>(values, lengths.ToArray(), pinned);
        }

        /// <summary>
        /// Creates a <see cref="Tensor{T}"/> and does not initialize it. If <paramref name="pinned"/> is true, the memory will be pinned.
        /// </summary>
        /// <param name="lengths">A <see cref="ReadOnlySpan{T}"/> indicating the lengths of each dimension.</param>
        /// <param name="strides">A <see cref="ReadOnlySpan{T}"/> indicating the strides of each dimension.</param>
        /// <param name="pinned">A <see cref="bool"/> whether the underlying data should be pinned or not.</param>
        static Tensor<T> ITensor<Tensor<T>, T>.CreateUninitialized(ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides, bool pinned)
        {
            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);
            T[] values = GC.AllocateUninitializedArray<T>((int)linearLength, pinned);
            return new Tensor<T>(values, lengths.ToArray(), strides.ToArray(), pinned);
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
        public nint FlattenedLength => _flattenedLength;

        /// <summary>
        /// Gets the length of each dimension in this <see cref="Tensor{T}"/>.
        /// </summary>
        /// <value><see cref="ReadOnlySpan{T}"/> with the lengths of each dimension.</value>
        public ReadOnlySpan<nint> Lengths => _lengths;

        /// <summary>
        /// Gets the length of each dimension in this <see cref="Tensor{T}"/>.
        /// </summary>
        /// <value><see cref="ReadOnlySpan{T}"/> with the lengths of each dimension.</value>
        void IReadOnlyTensor<Tensor<T>, T>.GetLengths(Span<nint> destination) => _lengths.CopyTo(destination);


        /// <summary>
        /// Gets the strides of each dimension in this <see cref="Tensor{T}"/>.
        /// </summary>
        /// <value><see cref="ReadOnlySpan{T}"/> with the strides of each dimension.</value>
        public ReadOnlySpan<nint> Strides => _strides;

        /// <summary>
        /// Gets the strides of each dimension in this <see cref="Tensor{T}"/>.
        /// </summary>
        /// <value><see cref="ReadOnlySpan{T}"/> with the strides of each dimension.</value>
        void IReadOnlyTensor<Tensor<T>, T>.GetStrides(scoped Span<nint> destination) => _strides.CopyTo(destination);

        bool ITensor<Tensor<T>, T>.IsReadOnly => false;

        /// <summary>
        /// Returns a reference to specified element of the Tensor.
        /// </summary>
        /// <param name="indexes"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when index less than 0 or index greater than or equal to FlattenedLength
        /// </exception>
        public ref T this[params scoped ReadOnlySpan<nint> indexes] => ref AsTensorSpan()[indexes];

        /// <summary>
        /// Returns a reference to specified element of the Tensor.
        /// </summary>
        /// <param name="indexes"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when index less than 0 or index greater than or equal to FlattenedLength
        /// </exception>
        public ref T this[params scoped ReadOnlySpan<NIndex> indexes] => ref AsTensorSpan()[indexes];

        /// <summary>
        /// Returns a slice of the Tensor.
        /// </summary>
        /// <param name="ranges"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when any index is less than 0 or any index is greater than or equal to FlattenedLength
        /// </exception>
        public Tensor<T> this[params ReadOnlySpan<NRange> ranges]
        {
            get
            {
                if (ranges.Length != Rank)
                    ThrowHelper.ThrowIndexOutOfRangeException();

                return Slice(ranges);
            }
            set
            {
                TensorSpan<T> srcSpan;
                if (ranges == ReadOnlySpan<NRange>.Empty)
                {
                    if (!Lengths.SequenceEqual(value.Lengths))
                        ThrowHelper.ThrowArgument_SetSliceNoRange(nameof(value));
                    srcSpan = AsTensorSpan().Slice(Lengths);
                }
                else
                    srcSpan = AsTensorSpan().Slice(ranges);

                if (!srcSpan.Lengths.SequenceEqual(value.Lengths))
                    ThrowHelper.ThrowArgument_SetSliceInvalidShapes(nameof(value));

                value.AsTensorSpan().CopyTo(srcSpan);
            }
        }

        /// <summary>
        /// Returns the specified element of the Tensor.
        /// </summary>
        /// <param name="indexes"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when index less than 0 or index greater than or equal to FlattenedLength
        /// </exception>
        T ITensor<Tensor<T>, T>.this[params ReadOnlySpan<nint> indexes]
        {
            get
            {
                return this[indexes];
            }
            set
            {
                this[indexes] = value;
            }
        }

        /// <summary>
        /// Returns the specified element of the Tensor.
        /// </summary>
        /// <param name="indexes"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when index less than 0 or index greater than or equal to FlattenedLength
        /// </exception>
        T ITensor<Tensor<T>, T>.this[params ReadOnlySpan<NIndex> indexes]
        {
            get
            {
                return this[indexes];
            }
            set
            {
                this[indexes] = value;
            }
        }

        /// <summary>
        /// Returns the specified element of the ReadOnlyTensor.
        /// </summary>
        /// <param name="indexes"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when index less than 0 or index greater than or equal to FlattenedLength
        /// </exception>
        T IReadOnlyTensor<Tensor<T>, T>.this[params ReadOnlySpan<nint> indexes] => AsReadOnlyTensorSpan()[indexes];

        /// <summary>
        /// Returns the specified element of the ReadOnlyTensor.
        /// </summary>
        /// <param name="indexes"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when index less than 0 or index greater than or equal to FlattenedLength
        /// </exception>
        T IReadOnlyTensor<Tensor<T>, T>.this[params ReadOnlySpan<NIndex> indexes] => AsReadOnlyTensorSpan()[indexes];

        /// <summary>
        /// Returns a slice of the ReadOnlyTensor.
        /// </summary>
        /// <param name="ranges"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when any index is less than 0 or any index is greater than or equal to FlattenedLength
        /// </exception>
        Tensor<T> IReadOnlyTensor<Tensor<T>, T>.this[params ReadOnlySpan<NRange> ranges]
        {
            get
            {
                if (ranges.Length != Rank)
                    ThrowHelper.ThrowIndexOutOfRangeException();

                return Slice(ranges);
            }
        }

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
                if (filter.Lengths.Length != Lengths.Length)
                    throw new ArgumentOutOfRangeException(nameof(filter), "Number of dimensions does not equal the number of dimensions in the span");

                for (int i = 0; i < filter.Lengths.Length; i++)
                {
                    if (filter.Lengths[i] != Lengths[i])
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

        public static implicit operator Tensor<T>(T[] array) => new Tensor<T>(array, [array.Length]);

        public static implicit operator TensorSpan<T>(Tensor<T> value) => new TensorSpan<T>(ref MemoryMarshal.GetArrayDataReference(value._values), value._lengths, value._strides, value._flattenedLength);

        public static implicit operator ReadOnlyTensorSpan<T>(Tensor<T> value) => new ReadOnlyTensorSpan<T>(ref MemoryMarshal.GetArrayDataReference(value._values), value._lengths, value._strides, value.FlattenedLength);

        /// <summary>
        /// Converts this <see cref="Tensor{T}"/> to a <see cref="TensorSpan{T}"/> pointing to the same backing memory."/>
        /// </summary>
        /// <returns><see cref="TensorSpan{T}"/></returns>
        public TensorSpan<T> AsTensorSpan() => new TensorSpan<T>(ref MemoryMarshal.GetArrayDataReference(_values), _lengths, _strides, _flattenedLength);

        /// <summary>
        /// Converts this <see cref="Tensor{T}"/> to a <see cref="TensorSpan{T}"/> pointing to the same backing memory based on the provided ranges."/>
        /// </summary>
        /// <param name="start">The ranges you want in the <see cref="TensorSpan{T}"/>.</param>
        /// <returns><see cref="TensorSpan{T}"/> based on the provided ranges.</returns>
        public TensorSpan<T> AsTensorSpan(params scoped ReadOnlySpan<NRange> start) => AsTensorSpan().Slice(start);

        /// <summary>
        /// Converts this <see cref="Tensor{T}"/> to a <see cref="TensorSpan{T}"/> pointing to the same backing memory based on the provided start locations."/>
        /// </summary>
        /// <param name="start">The start location you want in the <see cref="TensorSpan{T}"/>.</param>
        /// <returns><see cref="TensorSpan{T}"/> based on the provided ranges.</returns>
        public TensorSpan<T> AsTensorSpan(params scoped ReadOnlySpan<nint> start) => Slice(start);

        /// <summary>
        /// Converts this <see cref="Tensor{T}"/> to a <see cref="TensorSpan{T}"/> pointing to the same backing memory based on the provided start indexes."/>
        /// </summary>
        /// <param name="startIndex">The ranges you want in the <see cref="TensorSpan{T}"/>.</param>
        /// <returns><see cref="TensorSpan{T}"/> based on the provided ranges.</returns>
        public TensorSpan<T> AsTensorSpan(params scoped ReadOnlySpan<NIndex> startIndex) => AsTensorSpan().Slice(startIndex);

        /// <summary>
        /// Converts this <see cref="Tensor{T}"/> to a <see cref="ReadOnlyTensorSpan{T}"/> pointing to the same backing memory."/>
        /// </summary>
        /// <returns><see cref="ReadOnlyTensorSpan{T}"/></returns>
        public ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan() => new ReadOnlyTensorSpan<T>(ref MemoryMarshal.GetArrayDataReference(_values), _lengths, _strides, _flattenedLength);

        /// <summary>
        /// Converts this <see cref="Tensor{T}"/> to a <see cref="ReadOnlyTensorSpan{T}"/> pointing to the same backing memory based on the provided ranges."/>
        /// </summary>
        /// <param name="start">The ranges you want in the <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <returns></returns>
        public ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan(params scoped ReadOnlySpan<NRange> start) => AsTensorSpan().Slice(start);

        /// <summary>
        /// Converts this <see cref="Tensor{T}"/> to a <see cref="ReadOnlyTensorSpan{T}"/> pointing to the same backing memory based on the provided start locations."/>
        /// </summary>
        /// <param name="start">The start locations you want in the <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <returns></returns>
        public ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan(params scoped ReadOnlySpan<nint> start) => Slice(start);

        /// <summary>
        /// Converts this <see cref="Tensor{T}"/> to a <see cref="ReadOnlyTensorSpan{T}"/> pointing to the same backing memory based on the provided start indexes."/>
        /// </summary>
        /// <param name="startIndex">The start indexes you want in the <see cref="ReadOnlyTensorSpan{T}"/></param>
        /// <returns></returns>
        public ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan(params scoped ReadOnlySpan<NIndex> startIndex) => AsTensorSpan().Slice(startIndex);

        /// <summary>
        /// Returns a reference to the 0th element of the Tensor. If the Tensor is empty, returns null reference.
        /// It can be used for pinning and is required to support the use of Tensor within a fixed statement.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref T GetPinnableReference() => ref AsTensorSpan().GetPinnableReference();

        /// <summary>
        /// Returns a reference to the 0th element of the ReadOnlyTensor. If the ReadOnlyTensor is empty, returns null reference.
        /// It can be used for pinning and is required to support the use of ReadOnlyTensor within a fixed statement.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        ref readonly T IReadOnlyTensor<Tensor<T>, T>.GetPinnableReference() => ref AsReadOnlyTensorSpan().GetPinnableReference();

        /// <summary>
        /// Forms a slice out of the given tensor
        /// </summary>
        /// <param name="start">The ranges for the slice</param>
        /// <returns><see cref="Tensor{T}"/> as a copy of the provided ranges.</returns>
        // REVIEW: CURRENTLY DOES A COPY.
        public Tensor<T> Slice(params ReadOnlySpan<NRange> start)
        {
            if (start.Length != Lengths.Length)
                throw new ArgumentOutOfRangeException(nameof(start), "Number of dimensions to slice does not equal the number of dimensions in the span");

            TensorSpan<T> s = AsTensorSpan(start);
            T[] values = _isPinned ? GC.AllocateArray<T>(checked((int)s.FlattenedLength), _isPinned) : (new T[s.FlattenedLength]);
            var outTensor = new Tensor<T>(values, s.Lengths.ToArray(), _isPinned);
            s.CopyTo(outTensor);
            return outTensor;
        }

        /// <summary>
        /// Forms a slice out of the given tensor
        /// </summary>
        /// <param name="start">The start indexes for the slice</param>
        /// <returns><see cref="Tensor{T}"/> as a copy of the provided ranges.</returns>
        // REVIEW: CURRENTLY DOES A COPY.
        public Tensor<T> Slice(params ReadOnlySpan<nint> start)
        {
            NRange[] ranges = new NRange[start.Length];
            for (int i = 0; i < start.Length; i++)
            {
                ranges[i] = new NRange(start[i], new NIndex(0, fromEnd: true));
            }
            return Slice(ranges);
        }

        /// <summary>
        /// Forms a slice out of the given tensor
        /// </summary>
        /// <param name="startIndex">The start indexes for the slice</param>
        /// <returns><see cref="Tensor{T}"/> as a copy of the provided ranges.</returns>
        // REVIEW: CURRENTLY DOES A COPY.
        public Tensor<T> Slice(params ReadOnlySpan<NIndex> startIndex)
        {
            NRange[] ranges = new NRange[startIndex.Length];
            for (int i = 0; i < startIndex.Length; i++)
            {
                ranges[i] = new NRange(startIndex[i], new NIndex(0, fromEnd: true));
            }
            return Slice(ranges);
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

        /// <summary>
        /// Flattens the contents of this Tensor into the provided <see cref="Span{T}"/>.
        /// </summary>
        /// <param name="destination">The span to copy items into.</param>
        public void FlattenTo(Span<T> destination) => AsTensorSpan().FlattenTo(destination);

        /// <summary>
        /// Flattens the contents of this Tensor into the provided <see cref="Span{T}"/>.
        /// </summary>
        /// <param name="destination">The span to copy items into.</param>
        public bool TryFlattenTo(Span<T> destination) => AsTensorSpan().TryFlattenTo(destination);

        // IEnumerable
        /// <summary>
        /// Gets an <see cref="IEnumerator{T}"/> for the <see cref="Tensor{T}"/>.
        /// </summary>
        /// <returns><see cref="IEnumerator{T}"/></returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

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
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private struct Enumerator : IEnumerator<T>
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
                TensorSpanHelpers.AdjustIndexes(_tensor.Rank - 1, 1, ref _curIndices, _tensor.Lengths);

                _items++;
                return _items < _tensor.FlattenedLength;
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
                    sb.Append(Lengths[i]);
                    if (i + 1 < n)
                        sb.Append('x');
                }

                sb.Append(']');
            }
            sb.Append($", type = {typeof(T)}, isPinned = {IsPinned}");

            return sb.ToString();
        }

        /// <summary>
        /// Creates a <see cref="string"/> representation of the <see cref="TensorSpan{T}"/>."/>
        /// </summary>
        /// <param name="maximumLengths">Maximum Length of each dimension</param>
        /// <returns>A <see cref="string"/> representation of the <see cref="Tensor{T}"/></returns>
        public string ToString(params ReadOnlySpan<nint> maximumLengths)
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
