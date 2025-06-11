// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Numerics.Tensors
{
    /// <summary>
    /// Represents a tensor.
    /// </summary>
    [Experimental(Experimentals.TensorTDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public sealed class Tensor<T> : ITensor<Tensor<T>, T>
    {
        /// <summary>Gets an empty tensor.</summary>
        public static Tensor<T> Empty { get; } = new();

        internal readonly TensorShape _shape;
        internal readonly T[] _values;

        internal readonly int _start;
        internal readonly bool _isPinned;

        internal Tensor(scoped ReadOnlySpan<nint> lengths, bool pinned)
        {
            _shape = TensorShape.Create(lengths);
            _values = GC.AllocateArray<T>(checked((int)(_shape.LinearLength)), pinned);

            _start = 0;
            _isPinned = pinned;
        }

        internal Tensor(scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, bool pinned)
        {
            _shape = TensorShape.Create(lengths, strides);
            _values = GC.AllocateArray<T>(checked((int)(_shape.LinearLength)), pinned);

            _start = 0;
            _isPinned = pinned;
        }

        internal Tensor(T[]? array)
        {
            _shape = TensorShape.Create(array);
            _values = (array is not null) ? array : [];

            _start = 0;
            _isPinned = false;
        }

        internal Tensor(T[]? array, scoped ReadOnlySpan<nint> lengths)
        {
            _shape = TensorShape.Create(array, lengths);
            _values = (array is not null) ? array : [];

            _start = 0;
            _isPinned = false;
        }

        internal Tensor(T[]? array, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            _shape = TensorShape.Create(array, lengths, strides);
            _values = (array is not null) ? array : [];

            _start = 0;
            _isPinned = false;
        }

        internal Tensor(T[]? array, int start, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            _shape = TensorShape.Create(array, start, lengths, strides);
            _values = (array is not null) ? array : [];

            _start = start;
            _isPinned = false;
        }

        internal Tensor(T[]? array, int start, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, scoped ReadOnlySpan<int> linearRankOrder)
        {
            _shape = TensorShape.Create(array, start, lengths, strides, linearRankOrder);
            _values = (array is not null) ? array : [];

            _start = start;
            _isPinned = false;
        }

        internal Tensor(T[] array, in TensorShape shape, bool isPinned)
        {
            ThrowHelper.ThrowIfArrayTypeMismatch<T>(array);

            _shape = shape;
            _values = array;

            _start = 0;
            _isPinned = isPinned;
        }

        internal Tensor(T[] array, int start, in TensorShape shape, bool isPinned)
        {
            ThrowHelper.ThrowIfArrayTypeMismatch<T>(array);

            _shape = shape;
            _values = array;

            _start = start;
            _isPinned = isPinned;
        }

        private Tensor()
        {
            _shape = default;
            _values = [];

            _start = 0;
            _isPinned = false;
        }

        /// <inheritdoc cref="TensorSpan{T}.this[ReadOnlySpan{nint}]" />
        public ref T this[params scoped ReadOnlySpan<nint> indexes]
        {
            get => ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_values), _start + _shape.GetLinearOffset<TensorShape.GetOffsetAndLengthForNInt, nint>(indexes));
        }

        /// <inheritdoc cref="TensorSpan{T}.this[ReadOnlySpan{NIndex}]" />
        public ref T this[params scoped ReadOnlySpan<NIndex> indexes]
        {
            get => ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_values), _start + _shape.GetLinearOffset<TensorShape.GetOffsetAndLengthForNIndex, NIndex>(indexes));
        }

        /// <inheritdoc cref="TensorSpan{T}.this[ReadOnlySpan{NRange}]" />
        public Tensor<T> this[params ReadOnlySpan<NRange> ranges]
        {
            get => Slice(ranges);
            set => value.CopyTo(Slice(ranges));
        }

        /// <inheritdoc cref="IReadOnlyTensor.FlattenedLength" />
        public nint FlattenedLength => _shape.FlattenedLength;

        /// <inheritdoc cref="IReadOnlyTensor.HasAnyDenseDimensions" />
        public bool HasAnyDenseDimensions => _shape.HasAnyDenseDimensions;

        /// <inheritdoc cref="IReadOnlyTensor.IsDense" />
        public bool IsDense => _shape.IsDense;

        /// <inheritdoc cref="IReadOnlyTensor.IsEmpty" />
        public bool IsEmpty => _shape.IsEmpty;

        /// <inheritdoc cref="IReadOnlyTensor.IsPinned" />
        public bool IsPinned => _isPinned;

        /// <inheritdoc cref="IReadOnlyTensor.Lengths" />
        public ReadOnlySpan<nint> Lengths => _shape.Lengths;

        /// <inheritdoc cref="IReadOnlyTensor.Rank" />
        public int Rank => _shape.Rank;

        /// <inheritdoc cref="IReadOnlyTensor.Strides" />
        public ReadOnlySpan<nint> Strides => _shape.Strides;

        /// <summary>Defines an implicit conversion of an array to a tensor.</summary>
        /// <param name="array">The array to convert to a tensor.</param>
        /// <returns>The tensor span that corresponds to <paramref name="array" />.</returns>
        public static implicit operator Tensor<T>(T[] array) => Tensor.Create(array);

        /// <summary>Defines an implicit conversion of a tensor to a tensor span.</summary>
        /// <param name="tensor">The tensor to convert to a tensor span.</param>
        /// <returns>The tensor that corresponds to <paramref name="tensor" />.</returns>
        public static implicit operator TensorSpan<T>(Tensor<T> tensor) => tensor.AsTensorSpan();

        /// <inheritdoc cref="TensorSpan{T}.implicit operator ReadOnlyTensorSpan{T}(in TensorSpan{T})" />
        public static implicit operator ReadOnlyTensorSpan<T>(Tensor<T> tensor) => tensor.AsReadOnlyTensorSpan();

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.AsReadOnlyTensorSpan()" />
        public ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan() => new ReadOnlyTensorSpan<T>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_values), _start), in _shape);

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.AsReadOnlyTensorSpan(ReadOnlySpan{nint})" />
        public ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan(params scoped ReadOnlySpan<nint> startIndexes) => AsReadOnlyTensorSpan().Slice(startIndexes);

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.AsReadOnlyTensorSpan(ReadOnlySpan{NIndex})" />
        public ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan(params scoped ReadOnlySpan<NIndex> startIndexes) => AsReadOnlyTensorSpan().Slice(startIndexes);

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.AsReadOnlyTensorSpan(ReadOnlySpan{NRange})" />
        public ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan(params scoped ReadOnlySpan<NRange> ranges) => AsReadOnlyTensorSpan().Slice(ranges);

        /// <inheritdoc cref="ITensor{TSelf, T}.AsTensorSpan()" />
        public TensorSpan<T> AsTensorSpan() => new TensorSpan<T>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_values), _start), in _shape);

        /// <inheritdoc cref="ITensor{TSelf, T}.AsTensorSpan(ReadOnlySpan{nint})" />
        public TensorSpan<T> AsTensorSpan(params scoped ReadOnlySpan<nint> startIndexes) => AsTensorSpan().Slice(startIndexes);

        /// <inheritdoc cref="ITensor{TSelf, T}.AsTensorSpan(ReadOnlySpan{NIndex})" />
        public TensorSpan<T> AsTensorSpan(params scoped ReadOnlySpan<NIndex> startIndexes) => AsTensorSpan().Slice(startIndexes);

        /// <inheritdoc cref="ITensor{TSelf, T}.AsTensorSpan(ReadOnlySpan{NRange})" />
        public TensorSpan<T> AsTensorSpan(params scoped ReadOnlySpan<NRange> ranges) => AsTensorSpan().Slice(ranges);

        /// <inheritdoc cref="ITensor.Clear()" />
        public unsafe void Clear() => AsTensorSpan().Clear();

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.CopyTo(in TensorSpan{T})" />
        public void CopyTo(scoped in TensorSpan<T> destination)
        {
            if (!TryCopyTo(destination))
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }
        }

        /// <inheritdoc cref="ITensor{TSelf, T}.Fill(T)" />
        public void Fill(T value) => AsTensorSpan().Fill(value);

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.FlattenTo(Span{T})" />
        public void FlattenTo(scoped Span<T> destination)
        {
            if (!TryFlattenTo(destination))
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }
        }

        /// <inheritdoc cref="ITensor{TSelf, T}.GetDimensionSpan(int)" />
        public TensorDimensionSpan<T> GetDimensionSpan(int dimension) => AsTensorSpan().GetDimensionSpan(dimension);

        /// <summary>Gets an enumerator for the readonly tensor.</summary>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <inheritdoc cref="ITensor{TSelf, T}.GetPinnableReference()" />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref T GetPinnableReference()
        {
            // Ensure that the native code has just one forward branch that is predicted-not-taken.
            ref T ret = ref Unsafe.NullRef<T>();
            if (_shape.FlattenedLength != 0) ret = ref MemoryMarshal.GetArrayDataReference(_values);
            return ref ret;
        }

        /// <inheritdoc cref="IReadOnlyTensor.GetPinnedHandle()" />
        public unsafe MemoryHandle GetPinnedHandle()
        {
            GCHandle handle = GCHandle.Alloc(_values, GCHandleType.Pinned);
            return new MemoryHandle(Unsafe.AsPointer(ref GetPinnableReference()), handle);
        }

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.Slice(ReadOnlySpan{nint})" />
        public Tensor<T> Slice(params ReadOnlySpan<nint> startIndexes)
        {
            TensorShape shape = _shape.Slice<TensorShape.GetOffsetAndLengthForNInt, nint>(startIndexes, out nint linearOffset);

            // The source tensor can have no more than int.MaxValue elements so linearOffset will always be in range of int.
            Debug.Assert((int)(linearOffset) == linearOffset);

            return new Tensor<T>(
                _values,
                (int)(_start + linearOffset),
                in shape,
                _isPinned
            );
        }

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.Slice(ReadOnlySpan{NIndex})" />
        public Tensor<T> Slice(params ReadOnlySpan<NIndex> startIndexes)
        {
            TensorShape shape = _shape.Slice<TensorShape.GetOffsetAndLengthForNIndex, NIndex>(startIndexes, out nint linearOffset);

            // The source tensor can have no more than int.MaxValue elements so linearOffset will always be in range of int.
            Debug.Assert((int)(linearOffset) == linearOffset);

            return new Tensor<T>(
                _values,
                (int)(_start + linearOffset),
                in shape,
                _isPinned
            );
        }

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.Slice(ReadOnlySpan{NRange})" />
        public Tensor<T> Slice(params ReadOnlySpan<NRange> ranges)
        {
            TensorShape shape = _shape.Slice<TensorShape.GetOffsetAndLengthForNRange, NRange>(ranges, out nint linearOffset);

            // The source tensor can have no more than int.MaxValue elements so linearOffset will always be in range of int.
            Debug.Assert((int)(linearOffset) == linearOffset);

            return new Tensor<T>(
                _values,
                (int)(_start + linearOffset),
                in shape,
                _isPinned
            );
        }

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.ToDenseTensor()" />
        public Tensor<T> ToDenseTensor()
        {
            Tensor<T> result = this;

            if (!IsDense)
            {
                result = Tensor.Create<T>(Lengths, IsPinned);
                CopyTo(result);
            }

            return result;
        }

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.TryCopyTo(in TensorSpan{T})" />
        public bool TryCopyTo(scoped in TensorSpan<T> destination) => AsReadOnlyTensorSpan().TryCopyTo(destination);

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.TryFlattenTo(Span{T})" />
        public bool TryFlattenTo(scoped Span<T> destination) => AsReadOnlyTensorSpan().TryFlattenTo(destination);

        /// <summary>
        /// Creates a <see cref="string"/> representation of the <see cref="TensorSpan{T}"/>."/>
        /// </summary>
        /// <param name="maximumLengths">Maximum Length of each dimension</param>
        /// <returns>A <see cref="string"/> representation of the <see cref="Tensor{T}"/></returns>
        public string ToString(params ReadOnlySpan<nint> maximumLengths)
        {
            var sb = new StringBuilder($"System.Numerics.Tensors.Tensor<{typeof(T).Name}>[{_shape}]");

            sb.AppendLine("{");
            Tensor.ToString(AsReadOnlyTensorSpan(), maximumLengths, sb);
            sb.AppendLine("}");

            return sb.ToString();
        }

        //
        // IEnumerable
        //

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        //
        // IEnumerable<T>
        //

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        //
        // IReadOnlyTensor
        //

        object? IReadOnlyTensor.this[params scoped ReadOnlySpan<NIndex> indexes] => this[indexes];

        object? IReadOnlyTensor.this[params scoped ReadOnlySpan<nint> indexes] => this[indexes];

        //
        // IReadOnlyTensor<TSelf, T>
        //

        ref readonly T IReadOnlyTensor<Tensor<T>, T>.this[params ReadOnlySpan<nint> indexes] => ref this[indexes];

        ref readonly T IReadOnlyTensor<Tensor<T>, T>.this[params ReadOnlySpan<NIndex> indexes] => ref this[indexes];

        ReadOnlyTensorDimensionSpan<T> IReadOnlyTensor<Tensor<T>, T>.GetDimensionSpan(int dimension) => AsReadOnlyTensorSpan().GetDimensionSpan(dimension);

        ref readonly T IReadOnlyTensor<Tensor<T>, T>.GetPinnableReference() => ref GetPinnableReference();

        //
        // ITensor
        //

        bool ITensor.IsReadOnly => false;

        object? ITensor.this[params scoped ReadOnlySpan<NIndex> indexes]
        {
            get => this[indexes];

            set
            {
                this[indexes] = (T)value!;
            }
        }

        object? ITensor.this[params scoped ReadOnlySpan<nint> indexes]
        {
            get => this[indexes];

            set
            {
                this[indexes] = (T)value!;
            }
        }

        void ITensor.Fill(object value) => Fill(value is T t ? t : throw new ArgumentException($"Cannot convert {value} to {typeof(T)}"));

        //
        // ITensor<TSelf, T>
        //

        static Tensor<T> ITensor<Tensor<T>, T>.Create(scoped ReadOnlySpan<nint> lengths, bool pinned) => Tensor.Create<T>(lengths, pinned);

        static Tensor<T> ITensor<Tensor<T>, T>.Create(scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, bool pinned) => Tensor.Create<T>(lengths, strides, pinned);

        static Tensor<T> ITensor<Tensor<T>, T>.CreateUninitialized(scoped ReadOnlySpan<nint> lengths, bool pinned) => Tensor.Create<T>(lengths, pinned);

        static Tensor<T> ITensor<Tensor<T>, T>.CreateUninitialized(scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, bool pinned) => Tensor.Create<T>(lengths, strides, pinned);

        /// <summary>Enumerates the elements of a tensor.</summary>
        public struct Enumerator : IEnumerator<T>
        {
            private readonly Tensor<T> _tensor;
            private nint[] _indexes;
            private nint _linearOffset;
            private nint _itemsEnumerated;

            internal Enumerator(Tensor<T> tensor)
            {
                _tensor = tensor;
                _indexes = new nint[tensor.Rank];

                _indexes[^1] = -1;

                _linearOffset = tensor._start - (!tensor.IsEmpty ? tensor.Strides[^1] : 0);
                _itemsEnumerated = 0;
            }

            /// <inheritdoc cref="IEnumerator{T}.Current" />
            public readonly ref T Current => ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_tensor._values), _linearOffset);

            /// <inheritdoc cref="IEnumerator.MoveNext()" />
            public bool MoveNext()
            {
                if (_itemsEnumerated == _tensor._shape.FlattenedLength)
                {
                    return false;
                }

                _linearOffset = _tensor._shape.AdjustToNextIndex(_tensor._shape, _linearOffset, _indexes);

                _itemsEnumerated++;
                return true;
            }

            /// <inheritdoc cref="IEnumerator.Reset()" />
            public void Reset()
            {
                Array.Clear(_indexes);
                _indexes[^1] = -1;

                _linearOffset = _tensor._start - (!_tensor.IsEmpty ? _tensor.Strides[^1] : 0);
                _itemsEnumerated = 0;
            }

            //
            // IDisposable
            //

            void IDisposable.Dispose() { }

            //
            // IEnumerator
            //

            readonly object? IEnumerator.Current => Current;

            //
            // IEnumerator<T>
            //

            readonly T IEnumerator<T>.Current => Current;
        }
    }
}
