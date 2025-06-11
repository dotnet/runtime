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

#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member

namespace System.Numerics.Tensors
{
    /// <summary>
    /// Represents a contiguous region of arbitrary memory. Unlike arrays, it can point to either managed
    /// or native memory, or to memory allocated on the stack. It is type-safe and memory-safe.
    /// </summary>
    /// <typeparam name="T">The type of the elements within the tensor span.</typeparam>
    [DebuggerTypeProxy(typeof(TensorSpanDebugView<>))]
    [DebuggerDisplay("{ToString(),raw}")]
    [Experimental(Experimentals.TensorTDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public readonly ref struct TensorSpan<T>
    {
        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.Empty" />
        public static TensorSpan<T> Empty => default;

        internal readonly TensorShape _shape;
        internal readonly ref T _reference;

        /// <inheritdoc cref="ReadOnlyTensorSpan{T}.ReadOnlyTensorSpan(T[])" />
        /// <exception cref="ArrayTypeMismatchException"><paramref name="array"/> is covariant and its type is not exactly T[].</exception>
        public TensorSpan(T[]? array)
        {
            ThrowHelper.ThrowIfArrayTypeMismatch<T>(array);

            _shape = TensorShape.Create(array);
            _reference = ref (array is not null)
                       ? ref MemoryMarshal.GetArrayDataReference(array)
                       : ref Unsafe.NullRef<T>();
        }

        /// <inheritdoc cref="ReadOnlyTensorSpan{T}.ReadOnlyTensorSpan(T[], ReadOnlySpan{nint})" />
        /// <exception cref="ArrayTypeMismatchException"><paramref name="array"/> is covariant and its type is not exactly T[].</exception>
        public TensorSpan(T[]? array, scoped ReadOnlySpan<nint> lengths)
        {
            ThrowHelper.ThrowIfArrayTypeMismatch<T>(array);

            _shape = TensorShape.Create(array, lengths);
            _reference = ref (array is not null)
                       ? ref MemoryMarshal.GetArrayDataReference(array)
                       : ref Unsafe.NullRef<T>();
        }

        /// <inheritdoc cref="ReadOnlyTensorSpan{T}.ReadOnlyTensorSpan(T[], ReadOnlySpan{nint}, ReadOnlySpan{nint})" />
        /// <exception cref="ArrayTypeMismatchException"><paramref name="array"/> is covariant and its type is not exactly T[].</exception>
        public TensorSpan(T[]? array, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            ThrowHelper.ThrowIfArrayTypeMismatch<T>(array);

            _shape = TensorShape.Create(array, lengths, strides);
            _reference = ref (array is not null)
                       ? ref MemoryMarshal.GetArrayDataReference(array)
                       : ref Unsafe.NullRef<T>();
        }

        /// <inheritdoc cref="ReadOnlyTensorSpan{T}.ReadOnlyTensorSpan(T[], int, ReadOnlySpan{nint}, ReadOnlySpan{nint})" />
        /// <exception cref="ArrayTypeMismatchException"><paramref name="array"/> is covariant and its type is not exactly T[].</exception>
        public TensorSpan(T[]? array, int start, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            ThrowHelper.ThrowIfArrayTypeMismatch<T>(array);

            _shape = TensorShape.Create(array, start, lengths, strides);
            _reference = ref (array is not null)
                       ? ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (uint)start)
                       : ref Unsafe.NullRef<T>();
        }

        /// <inheritdoc cref="ReadOnlyTensorSpan{T}.ReadOnlyTensorSpan(ReadOnlySpan{T})" />
        public TensorSpan(Span<T> span)
        {
            ref T reference = ref MemoryMarshal.GetReference(span);
            _shape = TensorShape.Create(ref reference, span.Length);
            _reference = ref reference;
        }

        /// <inheritdoc cref="ReadOnlyTensorSpan{T}.ReadOnlyTensorSpan(ReadOnlySpan{T}, ReadOnlySpan{nint})" />
        public TensorSpan(Span<T> span, scoped ReadOnlySpan<nint> lengths)
        {
            ref T reference = ref MemoryMarshal.GetReference(span);
            _shape = TensorShape.Create(ref reference, span.Length, lengths);
            _reference = ref reference;
        }

        /// <inheritdoc cref="ReadOnlyTensorSpan{T}.ReadOnlyTensorSpan(ReadOnlySpan{T}, ReadOnlySpan{nint}, ReadOnlySpan{nint})" />
        public TensorSpan(Span<T> span, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            ref T reference = ref MemoryMarshal.GetReference(span);
            _shape = TensorShape.Create(ref reference, span.Length, lengths, strides);
            _reference = ref reference;
        }

        /// <inheritdoc cref="ReadOnlyTensorSpan{T}.ReadOnlyTensorSpan(Array)"/>
        /// <exception cref="ArrayTypeMismatchException"><paramref name="array"/> is covariant and its type is not exactly T[].</exception>
        public TensorSpan(Array? array)
        {
            ThrowHelper.ThrowIfArrayTypeMismatch<T>(array);

            _shape = TensorShape.Create(array);
            _reference = ref (array is not null)
                       ? ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(array))
                       : ref Unsafe.NullRef<T>();
        }

        /// <inheritdoc cref="ReadOnlyTensorSpan{T}.ReadOnlyTensorSpan(Array, ReadOnlySpan{int}, ReadOnlySpan{nint}, ReadOnlySpan{nint})" />
        /// <exception cref="ArrayTypeMismatchException"><paramref name="array"/> is covariant and its type is not exactly T[].</exception>
        public TensorSpan(Array? array, scoped ReadOnlySpan<int> start, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            ThrowHelper.ThrowIfArrayTypeMismatch<T>(array);

            _shape = TensorShape.Create(array, start, lengths, strides, out nint linearOffset);
            _reference = ref (array is not null)
                       ? ref Unsafe.Add(ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(array)), linearOffset)
                       : ref Unsafe.NullRef<T>();
        }

        /// <inheritdoc cref="ReadOnlyTensorSpan{T}.ReadOnlyTensorSpan(T*, nint)" />
        [CLSCompliant(false)]
        public unsafe TensorSpan(T* data, nint dataLength)
        {
            _shape = TensorShape.Create(data, dataLength);
            _reference = ref Unsafe.AsRef<T>(data);
        }

        /// <inheritdoc cref="ReadOnlyTensorSpan{T}.ReadOnlyTensorSpan(T*, nint, ReadOnlySpan{nint})" />
        [CLSCompliant(false)]
        public unsafe TensorSpan(T* data, nint dataLength, scoped ReadOnlySpan<nint> lengths)
        {
            _shape = TensorShape.Create(data, dataLength, lengths);
            _reference = ref Unsafe.AsRef<T>(data);
        }

        /// <inheritdoc cref="ReadOnlyTensorSpan{T}.ReadOnlyTensorSpan(T*, nint, ReadOnlySpan{nint}, ReadOnlySpan{nint})" />
        [CLSCompliant(false)]
        public unsafe TensorSpan(T* data, nint dataLength, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            _shape = TensorShape.Create(data, dataLength, lengths, strides);
            _reference = ref Unsafe.AsRef<T>(data);
        }

        internal TensorSpan(ref T data, nint dataLength)
        {
            _shape = TensorShape.Create(ref data, dataLength);
            _reference = ref data;
        }

        internal TensorSpan(ref T data, nint dataLength, scoped ReadOnlySpan<nint> lengths)
        {
            _shape = TensorShape.Create(ref data, dataLength, lengths);
            _reference = ref data;
        }

        internal TensorSpan(ref T data, nint dataLength, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            _shape = TensorShape.Create(ref data, dataLength, lengths, strides);
            _reference = ref data;
        }

        internal TensorSpan(ref T data, nint dataLength, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, scoped ReadOnlySpan<int> linearRankOrder)
        {
            _shape = TensorShape.Create(ref data, dataLength, lengths, strides, linearRankOrder);
            _reference = ref data;
        }

        internal TensorSpan(ref T reference, scoped in TensorShape shape)
        {
            _reference = ref reference;
            _shape = shape;
        }

        /// <inheritdoc cref="ITensor{TSelf, T}.this[ReadOnlySpan{nint}]" />
        public ref T this[params scoped ReadOnlySpan<nint> indexes]
        {
            get => ref Unsafe.Add(ref _reference, _shape.GetLinearOffset<TensorShape.GetOffsetAndLengthForNInt, nint>(indexes));
        }

        /// <inheritdoc cref="ITensor{TSelf, T}.this[ReadOnlySpan{NIndex}]" />
        public ref T this[params scoped ReadOnlySpan<NIndex> indexes]
        {
            get => ref Unsafe.Add(ref _reference, _shape.GetLinearOffset<TensorShape.GetOffsetAndLengthForNIndex, NIndex>(indexes));
        }

        /// <inheritdoc cref="ITensor{TSelf, T}.this[ReadOnlySpan{NRange}]" />
        public TensorSpan<T> this[params scoped ReadOnlySpan<NRange> ranges]
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

        /// <inheritdoc cref="IReadOnlyTensor.Lengths" />
        [UnscopedRef]
        public ReadOnlySpan<nint> Lengths => _shape.Lengths;

        /// <inheritdoc cref="IReadOnlyTensor.Rank" />
        public int Rank => Lengths.Length;

        /// <inheritdoc cref="IReadOnlyTensor.Strides" />
        [UnscopedRef]
        public ReadOnlySpan<nint> Strides => _shape.Strides;

        /// <inheritdoc cref="ReadOnlyTensorSpan{T}.operator ==(in ReadOnlyTensorSpan{T}, in ReadOnlyTensorSpan{T})" />
        public static bool operator ==(in TensorSpan<T> left, in TensorSpan<T> right)
            => Unsafe.AreSame(ref left._reference, ref right._reference)
            && left._shape == right._shape;

        /// <inheritdoc cref="ReadOnlyTensorSpan{T}.operator !=(in ReadOnlyTensorSpan{T}, in ReadOnlyTensorSpan{T})" />
        public static bool operator !=(in TensorSpan<T> left, in TensorSpan<T> right) => !(left == right);

        /// <summary>Defines an implicit conversion of an array to a tensor span.</summary>
        /// <param name="array">The array to convert to a tensor span.</param>
        /// <returns>The tensor span that corresponds to <paramref name="array" />.</returns>
        public static implicit operator TensorSpan<T>(T[]? array) => new TensorSpan<T>(array);

        /// <summary>Defines an implicit conversion of a tensor to a readonly tensor span.</summary>
        /// <param name="tensor">The tensor to convert to a readonly tensor span.</param>
        /// <returns>The tensor that corresponds to <paramref name="tensor" />.</returns>
        public static implicit operator ReadOnlyTensorSpan<T>(scoped in TensorSpan<T> tensor) =>
            new ReadOnlyTensorSpan<T>(ref tensor._reference, tensor._shape);

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.AsReadOnlyTensorSpan()" />
        public ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan() => new ReadOnlyTensorSpan<T>(ref _reference, in _shape);

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.AsReadOnlyTensorSpan(ReadOnlySpan{nint})" />
        public ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan(params scoped ReadOnlySpan<nint> startIndexes) => AsReadOnlyTensorSpan().Slice(startIndexes);

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.AsReadOnlyTensorSpan(ReadOnlySpan{NIndex})" />
        public ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan(params scoped ReadOnlySpan<NIndex> startIndexes) => AsReadOnlyTensorSpan().Slice(startIndexes);

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.AsReadOnlyTensorSpan(ReadOnlySpan{NRange})" />
        public ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan(params scoped ReadOnlySpan<NRange> ranges) => AsReadOnlyTensorSpan().Slice(ranges);

        /// <inheritdoc cref="ITensor.Clear()" />
        public void Clear() => TensorOperation.Invoke<TensorOperation.Clear<T>, T>(this);

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.CopyTo(in TensorSpan{T})" />
        public void CopyTo(scoped in TensorSpan<T> destination)
        {
            if (!TryCopyTo(destination))
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }
        }

        /// <inheritdoc cref="ReadOnlyTensorSpan{T}.Equals(object?)" />
        [Obsolete("Equals() on TensorSpan will always throw an exception. Use the equality operator instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object? obj) =>
            throw new NotSupportedException(SR.NotSupported_CannotCallEqualsOnSpan);

        /// <inheritdoc cref="ITensor{TSelf, T}.Fill(T)" />
        public void Fill(T value) => TensorOperation.Invoke<TensorOperation.Fill<T>, T, T>(this, value);

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.FlattenTo(Span{T})" />
        public void FlattenTo(scoped Span<T> destination)
        {
            if (!TryFlattenTo(destination))
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }
        }

        /// <inheritdoc cref="ITensor{TSelf, T}.GetDimensionSpan(int)" />
        public TensorDimensionSpan<T> GetDimensionSpan(int dimension) => new TensorDimensionSpan<T>(this, dimension);

        /// <summary>Gets an enumerator for the tensor span.</summary>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <inheritdoc cref="ReadOnlyTensorSpan{T}.GetHashCode" />
        [Obsolete("GetHashCode() on TensorSpan will always throw an exception.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() =>
            throw new NotSupportedException(SR.NotSupported_CannotCallGetHashCodeOnSpan);

        /// <inheritdoc cref="ITensor{TSelf, T}.GetPinnableReference()" />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref T GetPinnableReference()
        {
            // Ensure that the native code has just one forward branch that is predicted-not-taken.
            ref T ret = ref Unsafe.NullRef<T>();
            if (_shape.FlattenedLength != 0) ret = ref _reference;
            return ref ret;
        }

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.Slice(ReadOnlySpan{nint})" />
        public TensorSpan<T> Slice(params scoped ReadOnlySpan<nint> startIndexes)
        {
            TensorShape shape = _shape.Slice<TensorShape.GetOffsetAndLengthForNInt, nint>(startIndexes, out nint linearOffset);
            return new TensorSpan<T>(
                ref Unsafe.Add(ref _reference, linearOffset),
                shape
            );
        }

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.Slice(ReadOnlySpan{NIndex})" />
        public TensorSpan<T> Slice(params scoped ReadOnlySpan<NIndex> startIndexes)
        {
            TensorShape shape = _shape.Slice<TensorShape.GetOffsetAndLengthForNIndex, NIndex>(startIndexes, out nint linearOffset);
            return new TensorSpan<T>(
                ref Unsafe.Add(ref _reference, linearOffset),
                shape
            );
        }

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.Slice(ReadOnlySpan{NRange})" />
        public TensorSpan<T> Slice(params scoped ReadOnlySpan<NRange> ranges)
        {
            TensorShape shape = _shape.Slice<TensorShape.GetOffsetAndLengthForNRange, NRange>(ranges, out nint linearOffset);
            return new TensorSpan<T>(
                ref Unsafe.Add(ref _reference, linearOffset),
                shape
            );
        }

        /// <inheritdoc cref="ReadOnlyTensorSpan{T}.ToString" />
        public override string ToString() => $"System.Numerics.Tensors.TensorSpan<{typeof(T).Name}>[{_shape}]";

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.TryCopyTo(in TensorSpan{T})" />
        public bool TryCopyTo(scoped in TensorSpan<T> destination) => AsReadOnlyTensorSpan().TryCopyTo(destination);

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.TryFlattenTo(Span{T})" />
        public bool TryFlattenTo(scoped Span<T> destination) => AsReadOnlyTensorSpan().TryFlattenTo(destination);

        /// <summary>Enumerates the elements of a tensor span.</summary>
        public ref struct Enumerator : IEnumerator<T>
        {
            private readonly TensorSpan<T> _span;
            private nint[] _indexes;
            private nint _linearOffset;
            private nint _itemsEnumerated;

            internal Enumerator(TensorSpan<T> span)
            {
                _span = span;
                _indexes = new nint[span.Rank];

                _indexes[^1] = -1;

                _linearOffset = 0 - (!span.IsEmpty ? span.Strides[^1] : 0);
                _itemsEnumerated = 0;
            }

            /// <summary>Gets the element at the current position of the enumerator.</summary>
            public readonly ref T Current => ref Unsafe.Add(ref _span._reference, _linearOffset);

            /// <summary>Advances the enumerator to the next element of the span.</summary>
            public bool MoveNext()
            {
                if (_itemsEnumerated == _span._shape.FlattenedLength)
                {
                    return false;
                }

                _linearOffset = _span._shape.AdjustToNextIndex(_span._shape, _linearOffset, _indexes);

                _itemsEnumerated++;
                return true;
            }

            /// <summary>Sets the enumerator to its initial position, which is before the first element in the tensor span.</summary>
            public void Reset()
            {
                Array.Clear(_indexes);
                _indexes[^1] = -1;

                _linearOffset = 0 - (!_span.IsEmpty ? _span.Strides[^1] : 0);
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
