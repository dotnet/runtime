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
    public readonly ref struct ReadOnlyTensorSpan<T>
    {
        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.Empty" />
        public static ReadOnlyTensorSpan<T> Empty => default;

        internal readonly TensorShape _shape;
        internal readonly ref T _reference;

        /// <summary>Creates a new tensor over the entirety of the target array.</summary>
        /// <param name="array">The target array.</param>
        /// <remarks>
        ///   <para>Returns default when <paramref name="array"/> is null.</para>
        ///   <para>The created tensor span has a single dimension that is the same length as <paramref name="array" />.</para>
        /// </remarks>
        public ReadOnlyTensorSpan(T[]? array)
        {
            _shape = TensorShape.Create(array);
            _reference = ref (array is not null)
                       ? ref MemoryMarshal.GetArrayDataReference(array)
                       : ref Unsafe.NullRef<T>();
        }

        /// <summary>Creates a new tensor over the portion of the target array using the specified lengths.</summary>
        /// <param name="array">The target array.</param>
        /// <param name="lengths">The lengths of the dimensions. If an empty span is provided, the created tensor will have a single dimension that is the same length as <paramref name="array" />.</param>
        /// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   One of the following conditions is met:
        ///   * <paramref name="array" /> is null and <paramref name="lengths" /> is not empty.
        ///   * <paramref name="lengths" /> is not empty and contains an element that is either zero or negative.
        ///   * <paramref name="lengths" /> is not empty and has a flattened length greater than <paramref name="array" />.Length.
        /// </exception>
        public ReadOnlyTensorSpan(T[]? array, scoped ReadOnlySpan<nint> lengths)
        {
            _shape = TensorShape.Create(array, lengths);
            _reference = ref (array is not null)
                       ? ref MemoryMarshal.GetArrayDataReference(array)
                       : ref Unsafe.NullRef<T>();
        }

        /// <summary>Creates a new tensor over the portion of the target array beginning at the specified start index and using the specified lengths and strides.</summary>
        /// <param name="array">The target array.</param>
        /// <param name="lengths">The lengths of the dimensions. If an empty span is provided, the created tensor will have a single dimension that is the same length as <paramref name="array" />.</param>
        /// <param name="strides">The strides of each dimension. If an empty span is provided, then strides will be automatically calculated from <paramref name="lengths" />.</param>
        /// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   One of the following conditions is met:
        ///   * <paramref name="array" /> is null and <paramref name="lengths" /> or <paramref name="strides" /> is not empty.
        ///   * <paramref name="lengths" /> is not empty and contains an element that is either zero or negative.
        ///   * <paramref name="lengths" /> is not empty and has a flattened length greater than <paramref name="array" />.Length.
        ///   * <paramref name="strides" /> is not empty and has a length different from <paramref name="lengths"/>.
        ///   * <paramref name="strides" /> is not empty and contains an element that is negative.
        ///   * <paramref name="strides" /> is not empty and contains an element that is zero in a non leading position.
        /// </exception>
        public ReadOnlyTensorSpan(T[]? array, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            _shape = TensorShape.Create(array, lengths, strides);
            _reference = ref (array is not null)
                       ? ref MemoryMarshal.GetArrayDataReference(array)
                       : ref Unsafe.NullRef<T>();
        }

        /// <summary>Creates a new tensor over the portion of the target array beginning at the specified start index and using the specified lengths and strides.</summary>
        /// <param name="array">The target array.</param>
        /// <param name="start">The index at which to begin the tensor.</param>
        /// <param name="lengths">The lengths of the dimensions. If an empty span is provided, the created tensor will have a single dimension that is the same length as <paramref name="array" />.</param>
        /// <param name="strides">The strides of each dimension. If an empty span is provided, then strides will be automatically calculated from <paramref name="lengths" />.</param>
        /// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   One of the following conditions is met:
        ///   * <paramref name="array" /> is null and <paramref name="lengths" /> or <paramref name="strides" /> is not empty.
        ///   * <paramref name="start" /> is not in range of <paramref name="array" />.
        ///   * <paramref name="lengths" /> is not empty and contains an element that is either zero or negative.
        ///   * <paramref name="lengths" /> is not empty and has a flattened length greater than <paramref name="array" />.Length.
        ///   * <paramref name="strides" /> is not empty and has a length different from <paramref name="lengths"/>.
        ///   * <paramref name="strides" /> is not empty and contains an element that is negative.
        ///   * <paramref name="strides" /> is not empty and contains an element that is zero in a non leading position.
        /// </exception>
        public ReadOnlyTensorSpan(T[]? array, int start, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            _shape = TensorShape.Create(array, start, lengths, strides);
            _reference = ref (array is not null)
                       ? ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (uint)start)
                       : ref Unsafe.NullRef<T>();
        }

        /// <summary>Creates a new tensor span over the entirety of the target span.</summary>
        /// <param name="span">The target span.</param>
        /// <remarks>The created tensor span has a single dimension that is the same length as <paramref name="span" />.</remarks>
        public ReadOnlyTensorSpan(ReadOnlySpan<T> span)
        {
            ref T reference = ref MemoryMarshal.GetReference(span);
            _shape = TensorShape.Create(ref reference, span.Length);
            _reference = ref reference;
        }

        /// <summary>Creates a new tensor span over the target span using the specified lengths.</summary>
        /// <param name="span">The target span.</param>
        /// <param name="lengths">The lengths of the dimensions. If an empty span is provided, the created tensor span will have a single dimension that is the same length as <paramref name="span" />.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   One of the following conditions is met:
        ///   * <paramref name="lengths" /> is not empty and contains an element that is either zero or negative.
        ///   * <paramref name="lengths" /> is not empty and has a flattened length greater than <paramref name="span" />.Length.
        /// </exception>
        public ReadOnlyTensorSpan(ReadOnlySpan<T> span, scoped ReadOnlySpan<nint> lengths)
        {
            ref T reference = ref MemoryMarshal.GetReference(span);
            _shape = TensorShape.Create(ref reference, span.Length, lengths);
            _reference = ref reference;
        }

        /// <summary>Creates a new tensor span over the target span using the specified lengths and strides.</summary>
        /// <param name="span">The target span.</param>
        /// <param name="lengths">The lengths of the dimensions. If an empty span is provided, the created tensor span will have a single dimension that is the same length as <paramref name="span" />.</param>
        /// <param name="strides">The strides of each dimension. If an empty span is provided, then strides will be automatically calculated from <paramref name="lengths" />.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   One of the following conditions is met:
        ///   * <paramref name="lengths" /> is not empty and contains an element that is either zero or negative.
        ///   * <paramref name="lengths" /> is not empty and has a flattened length greater than <paramref name="span" />.Length.
        ///   * <paramref name="strides" /> is not empty and has a length different from <paramref name="lengths"/>.
        ///   * <paramref name="strides" /> is not empty and contains an element that is negative.
        ///   * <paramref name="strides" /> is not empty and contains an element that is zero in a non leading position.
        /// </exception>
        public ReadOnlyTensorSpan(ReadOnlySpan<T> span, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            ref T reference = ref MemoryMarshal.GetReference(span);
            _shape = TensorShape.Create(ref reference, span.Length, lengths, strides);
            _reference = ref reference;
        }

        /// <summary>Creates a new tensor span over the entirety of the target array.</summary>
        /// <param name="array">The target array.</param>
        /// <remarks>
        ///   <para>Returns default when <paramref name="array"/> is null.</para>
        ///   <para>The created tensor span has a single dimension that is the same length as <paramref name="array" />.</para>
        /// </remarks>
        public ReadOnlyTensorSpan(Array? array)
        {
            _shape = TensorShape.Create(array);
            _reference = ref (array is not null)
                       ? ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(array))
                       : ref Unsafe.NullRef<T>();
        }

        /// <summary>Creates a new tensor span over the portion of the target array beginning at the specified start index and using the specified lengths and strides.</summary>
        /// <param name="array">The target array.</param>
        /// <param name="start">The index at which to begin the tensor span.</param>
        /// <param name="lengths">The lengths of the dimensions. If an empty span is provided, the created tensor span will have a single dimension that is the same length as <paramref name="array" />.</param>
        /// <param name="strides">The strides of each dimension. If an empty span is provided, then strides will be automatically calculated from <paramref name="lengths" />.</param>
        /// <remarks>
        ///   <para>Returns default when <paramref name="array"/> is null.</para>
        ///   <para></para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   One of the following conditions is met:
        ///   * <paramref name="array" /> is null and <paramref name="lengths" /> or <paramref name="strides" /> is not empty.
        ///   * <paramref name="start" /> is not in range of <paramref name="array" />.
        ///   * <paramref name="lengths" /> is not empty and contains an element that is either zero or negative.
        ///   * <paramref name="lengths" /> is not empty and has a flattened length greater than <paramref name="array" />.Length.
        ///   * <paramref name="strides" /> is not empty and has a length different from <paramref name="lengths"/>.
        ///   * <paramref name="strides" /> is not empty and contains an element that is negative.
        ///   * <paramref name="strides" /> is not empty and contains an element that is zero in a non leading position.
        /// </exception>
        public ReadOnlyTensorSpan(Array? array, scoped ReadOnlySpan<int> start, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            _shape = TensorShape.Create(array, start, lengths, strides, out nint linearOffset);
            _reference = ref (array is not null)
                       ? ref Unsafe.Add(ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(array)), linearOffset)
                       : ref Unsafe.NullRef<T>();
        }

        /// <summary>Creates a new tensor span over the target unmanaged buffer.</summary>
        /// <param name="data">The pointer to the start of the target unmanaged buffer.</param>
        /// <param name="dataLength">The number of elements the target unmanaged buffer contains.</param>
        /// <remarks>Returns default when <paramref name="data" /> is null.</remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="data" /> is <c>null</c> and <paramref name="dataLength" /> is not zero</exception>

        [CLSCompliant(false)]
        public unsafe ReadOnlyTensorSpan(T* data, nint dataLength)
        {
            _shape = TensorShape.Create(data, dataLength);
            _reference = ref Unsafe.AsRef<T>(data);
        }

        /// <summary>Creates a new tensor span over the target unmanaged buffer using the specified lengths.</summary>
        /// <param name="data">The pointer to the start of the target unmanaged buffer.</param>
        /// <param name="dataLength">The number of elements the target unmanaged buffer contains.</param>
        /// <param name="lengths">The lengths of the dimensions. If an empty span is provided, the created tensor span will have a single dimension that is the same length as <paramref name="dataLength" />.</param>
        /// <remarks>Returns default when <paramref name="data" /> is null.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   One of the following conditions is met:
        ///   * <paramref name="data" /> is <c>null</c> and <paramref name="dataLength" /> is not zero.
        ///   * <paramref name="data" /> is null and <paramref name="lengths" />.
        ///   * <paramref name="lengths" /> is not empty and contains an element that is either zero or negative.
        ///   * <paramref name="lengths" /> is not empty and has a flattened length greater than <paramref name="dataLength" />.
        /// </exception>
        [CLSCompliant(false)]
        public unsafe ReadOnlyTensorSpan(T* data, nint dataLength, scoped ReadOnlySpan<nint> lengths)
        {
            _shape = TensorShape.Create(data, dataLength, lengths);
            _reference = ref Unsafe.AsRef<T>(data);
        }

        /// <summary>Creates a new tensor span over the target unmanaged buffer using the specified lengths and strides.</summary>
        /// <param name="data">The pointer to the start of the target unmanaged buffer.</param>
        /// <param name="dataLength">The number of elements the target unmanaged buffer contains.</param>
        /// <param name="lengths">The lengths of the dimensions. If an empty span is provided, the created tensor span will have a single dimension that is the same length as <paramref name="dataLength" />.</param>
        /// <param name="strides">The strides of each dimension. If an empty span is provided, then strides will be automatically calculated from <paramref name="lengths" />.</param>
        /// <remarks>Returns default when <paramref name="data" /> is null.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   One of the following conditions is met:
        ///   * <paramref name="data" /> is <c>null</c> and <paramref name="dataLength" /> is not zero.
        ///   * <paramref name="data" /> is null and <paramref name="lengths" /> or <paramref name="strides" /> is not empty.
        ///   * <paramref name="lengths" /> is not empty and contains an element that is either zero or negative.
        ///   * <paramref name="lengths" /> is not empty and has a flattened length greater than <paramref name="dataLength" />.
        ///   * <paramref name="strides" /> is not empty and has a length different from <paramref name="lengths"/>.
        ///   * <paramref name="strides" /> is not empty and contains an element that is negative.
        ///   * <paramref name="strides" /> is not empty and contains an element that is zero in a non leading position.
        /// </exception>
        [CLSCompliant(false)]
        public unsafe ReadOnlyTensorSpan(T* data, nint dataLength, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            _shape = TensorShape.Create(data, dataLength, lengths, strides);
            _reference = ref Unsafe.AsRef<T>(data);
        }

        // Constructor for internal use only. It is not safe to expose publicly.
        internal ReadOnlyTensorSpan(ref T data, nint dataLength)
        {
            _shape = TensorShape.Create(ref data, dataLength);
            _reference = ref data;
        }

        internal ReadOnlyTensorSpan(ref T data, nint dataLength, scoped ReadOnlySpan<nint> lengths)
        {
            _shape = TensorShape.Create(ref data, dataLength, lengths);
            _reference = ref data;
        }

        internal ReadOnlyTensorSpan(ref T data, nint dataLength, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            _shape = TensorShape.Create(ref data, dataLength, lengths, strides);
            _reference = ref data;
        }

        internal ReadOnlyTensorSpan(ref T data, nint dataLength, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, scoped ReadOnlySpan<int> linearRankOrder)
        {
            _shape = TensorShape.Create(ref data, dataLength, lengths, strides, linearRankOrder);
            _reference = ref data;
        }

        internal ReadOnlyTensorSpan(ref T reference, scoped in TensorShape shape)
        {
            _reference = ref reference;
            _shape = shape;
        }

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.this[ReadOnlySpan{nint}]" />
        public ref readonly T this[params scoped ReadOnlySpan<nint> indexes]
        {
            get => ref Unsafe.Add(ref _reference, _shape.GetLinearOffset<TensorShape.GetOffsetAndLengthForNInt, nint>(indexes));
        }

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.this[ReadOnlySpan{NIndex}]" />
        public ref readonly T this[params scoped ReadOnlySpan<NIndex> indexes]
        {
            get => ref Unsafe.Add(ref _reference, _shape.GetLinearOffset<TensorShape.GetOffsetAndLengthForNIndex, NIndex>(indexes));
        }

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.this[ReadOnlySpan{NRange}]" />
        public ReadOnlyTensorSpan<T> this[params scoped ReadOnlySpan<NRange> ranges]
        {
            get => Slice(ranges);
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

        /// <summary>Returns a value that indicates whether two tensor spans are equal.</summary>
        /// <param name="left">The first tensor span to compare.</param>
        /// <param name="right">The second tensor span to compare.</param>
        /// <returns><c>true</c> if the two tensor span are equal; otherwise, <c>false</c>.</returns>
        /// <remarks>Two tensor span are equal if they have the same length and the corresponding elements of <paramref name="left" /> and <paramref name="right" /> point to the same memory. Note that the test for equality does not attempt to determine whether the contents are equal.</remarks>
        public static bool operator ==(in ReadOnlyTensorSpan<T> left, in ReadOnlyTensorSpan<T> right)
            => Unsafe.AreSame(ref left._reference, ref right._reference)
            && left._shape == right._shape;

        /// <summary>Returns a value that indicates whether two tensor spans are not equal.</summary>
        /// <param name="left">The first tensor span to compare.</param>
        /// <param name="right">The second tensor span to compare.</param>
        /// <returns><c>true</c> if the two tensor span are not equal; otherwise, <c>false</c>.</returns>
        /// <remarks>Two tensor span are not equal if they have the different lengths or if the corresponding elements of <paramref name="left" /> and <paramref name="right" /> do not point to the same memory. Note that the test for equality does not attempt to determine whether the contents are not equal.</remarks>
        public static bool operator !=(in ReadOnlyTensorSpan<T> left, in ReadOnlyTensorSpan<T> right) => !(left == right);

        /// <summary>Defines an implicit conversion of an array to a readonly tensor span.</summary>
        /// <param name="array">The array to convert to a readonly tensor span.</param>
        /// <returns>The readonly tensor span that corresponds to <paramref name="array" />.</returns>
        public static implicit operator ReadOnlyTensorSpan<T>(T[]? array) => new ReadOnlyTensorSpan<T>(array);

        /// <summary>Casts a tensor span of <typeparamref name="TDerived" /> to a tensor span of <typeparamref name="T" />.</summary>
        /// <typeparam name="TDerived">The element type of the source tensor span, which must be derived from <typeparamref name="T" />.</typeparam>
        /// <param name="items">The source tensor span. No copy is made.</param>
        /// <returns>A tensor span with elements cast to the new type.</returns>
        /// <remarks>This method uses a covariant cast, producing a tensor span that shares the same memory as the source. The relationships expressed in the type constraints ensure that the cast is a safe operation.</remarks>
        public static ReadOnlyTensorSpan<T> CastUp<TDerived>(in ReadOnlyTensorSpan<TDerived> items)
            where TDerived : class?, T
        {
            return new ReadOnlyTensorSpan<T>(
                ref Unsafe.As<TDerived, T>(ref items._reference),
                items._shape
            );
        }

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.CopyTo(in TensorSpan{T})" />
        public void CopyTo(scoped in TensorSpan<T> destination)
        {
            if (!TryCopyTo(destination))
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }
        }

        /// <summary>Calls to this method are not supported.</summary>
        /// <param name="obj">Not supported.</param>
        /// <returns>Calls to this method are not supported.</returns>
        /// <exception cref="NotSupportedException">Calls to this method are not supported.</exception>
        /// <remarks>This method is not supported as tensor spans cannot be boxed. To compare two tensor spans, use operator ==.</remarks>
        [Obsolete("Equals() on ReadOnlyTensorSpan will always throw an exception. Use the equality operator instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object? obj) =>
            throw new NotSupportedException(SR.NotSupported_CannotCallEqualsOnSpan);

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.FlattenTo(Span{T})" />
        public void FlattenTo(scoped Span<T> destination)
        {
            if (!TryFlattenTo(destination))
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }
        }

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.GetDimensionSpan(int)" />
        public ReadOnlyTensorDimensionSpan<T> GetDimensionSpan(int dimension) => new ReadOnlyTensorDimensionSpan<T>(this, dimension);

        /// <summary>Gets an enumerator for the readonly tensor span.</summary>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>Calls to this method are not supported.</summary>
        /// <returns>Calls to this method are not supported.</returns>
        /// <exception cref="NotSupportedException">Calls to this method are not supported.</exception>
        /// <remarks>This method is not supported as tensor spans cannot be boxed.</remarks>
        [Obsolete("GetHashCode() on ReadOnlyTensorSpan will always throw an exception.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() =>
            throw new NotSupportedException(SR.NotSupported_CannotCallGetHashCodeOnSpan);

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.GetPinnableReference()" />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref readonly T GetPinnableReference()
        {
            // Ensure that the native code has just one forward branch that is predicted-not-taken.
            ref T ret = ref Unsafe.NullRef<T>();
            if (_shape.FlattenedLength != 0) ret = ref _reference;
            return ref ret;
        }

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.Slice(ReadOnlySpan{nint})" />
        public ReadOnlyTensorSpan<T> Slice(params scoped ReadOnlySpan<nint> startIndexes)
        {
            TensorShape shape = _shape.Slice<TensorShape.GetOffsetAndLengthForNInt, nint>(startIndexes, out nint linearOffset);
            return new ReadOnlyTensorSpan<T>(
                ref Unsafe.Add(ref _reference, linearOffset),
                shape
            );
        }

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.Slice(ReadOnlySpan{NIndex})" />
        public ReadOnlyTensorSpan<T> Slice(params scoped ReadOnlySpan<NIndex> startIndexes)
        {
            TensorShape shape = _shape.Slice<TensorShape.GetOffsetAndLengthForNIndex, NIndex>(startIndexes, out nint linearOffset);
            return new ReadOnlyTensorSpan<T>(
                ref Unsafe.Add(ref _reference, linearOffset),
                shape
            );
        }

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.Slice(ReadOnlySpan{NRange})" />
        public ReadOnlyTensorSpan<T> Slice(params scoped ReadOnlySpan<NRange> ranges)
        {
            TensorShape shape = _shape.Slice<TensorShape.GetOffsetAndLengthForNRange, NRange>(ranges, out nint linearOffset);
            return new ReadOnlyTensorSpan<T>(
                ref Unsafe.Add(ref _reference, linearOffset),
                shape
            );
        }

        /// <summary>Returns the string representation of the tensor span.</summary>
        /// <returns>The string representation of the tensor span.</returns>
        public override string ToString() => $"System.Numerics.Tensors.ReadOnlyTensorSpan<{typeof(T).Name}>[{_shape}]";

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.TryCopyTo(in TensorSpan{T})" />
        public bool TryCopyTo(scoped in TensorSpan<T> destination)
        {
            if (TensorShape.AreCompatible(destination._shape, _shape, false))
            {
                TensorOperation.Invoke<TensorOperation.CopyTo<T>, T, T>(this, destination);
                return true;
            }
            return false;
        }

        /// <inheritdoc cref="IReadOnlyTensor{TSelf, T}.TryFlattenTo(Span{T})" />
        public bool TryFlattenTo(scoped Span<T> destination)
        {
            if (_shape.FlattenedLength <= destination.Length)
            {
                TensorOperation.Invoke<TensorOperation.CopyTo<T>, T, T>(this, destination);
                return true;
            }
            return false;
        }

        /// <summary>Enumerates the elements of a tensor span.</summary>
        public ref struct Enumerator : IEnumerator<T>
        {
            private readonly ReadOnlyTensorSpan<T> _span;
            private nint[] _indexes;
            private nint _linearOffset;
            private nint _itemsEnumerated;

            internal Enumerator(ReadOnlyTensorSpan<T> span)
            {
                _span = span;
                _indexes = new nint[span.Rank];

                _indexes[^1] = -1;

                _linearOffset = 0 - (!span.IsEmpty ? span.Strides[^1] : 0);
                _itemsEnumerated = 0;
            }

            /// <summary>Gets the element at the current position of the enumerator.</summary>
            public readonly ref readonly T Current => ref Unsafe.Add(ref _span._reference, _linearOffset);

            /// <summary>Advances the enumerator to the next element of the tensor span.</summary>
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
