// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.JavaScript.JSType;
using EditorBrowsableAttribute = System.ComponentModel.EditorBrowsableAttribute;
using EditorBrowsableState = System.ComponentModel.EditorBrowsableState;

#pragma warning disable 0809  //warning CS0809: Obsolete member 'ReadOnlyTensorSpan<T>.Equals(object)' overrides non-obsolete member 'object.Equals(object)'

namespace System.Numerics.Tensors
{
    /// <summary>
    /// ReadOnlyTensorSpan represents a contiguous region of arbitrary memory. Unlike arrays, it can point to either managed
    /// or native memory, or to memory allocated on the stack. It is type-safe and memory-safe.
    /// </summary>
    [DebuggerTypeProxy(typeof(TensorSpanDebugView<>))]
    [DebuggerDisplay("{ToString(),raw}")]
    [Experimental(Experimentals.TensorTDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public readonly ref struct ReadOnlyTensorSpan<T>
    {
        /// <summary>A byref or a native ptr.</summary>
        internal readonly ref T _reference;
        internal readonly TensorShape _shape;


        /// <summary>
        /// Creates a new span over the entirety of the target array.
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
        /// <exception cref="ArrayTypeMismatchException">Thrown when <paramref name="array"/> is covariant and array's type is not exactly T[].</exception>
        public ReadOnlyTensorSpan(T[]? array) : this(array, 0, [array?.Length ?? 0], [])
        {
        }

        /// <summary>
        /// Creates a new span over the portion of the target array beginning
        /// at 'start' index and ending at 'end' index (exclusive).
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <param name="startIndex">The index at which to begin the span.</param>
        /// <param name="lengths">The lengths of the dimensions. If default is provided its assumed to have 1 dimension with a length equal to the length of the data.</param>
        /// <param name="strides">The strides of each dimension. If default or span of length 0 is provided then strides will be automatically calculated.</param>
        /// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
        /// <exception cref="ArrayTypeMismatchException">Thrown when <paramref name="array"/> is covariant and array's type is not exactly T[].</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="startIndex"/> or end index is not in the range (&lt;0 or &gt;FlattenedLength).
        /// </exception>
        public ReadOnlyTensorSpan(T[]? array, Index startIndex, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
            : this(array, startIndex.GetOffset(array?.Length ?? 0), lengths, strides)
        {
        }

        /// <summary>
        /// Creates a new span over the portion of the target array beginning
        /// at 'start' index and ending at 'end' index (exclusive).
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <param name="start">The index at which to begin the span.</param>
        /// <param name="lengths">The lengths of the dimensions. If default is provided its assumed to have 1 dimension with a length equal to the length of the data.</param>
        /// <param name="strides">The strides of each dimension. If default or span of length 0 is provided then strides will be automatically calculated.</param>
        /// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
        /// <exception cref="ArrayTypeMismatchException">Thrown when <paramref name="array"/> is covariant and array's type is not exactly T[].</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in the range (&lt;0 or &gt;FlattenedLength).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyTensorSpan(T[]? array, int start, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            if (lengths.IsEmpty && array != null)
                lengths = [array.Length];

            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);
            if (array == null)
            {
                if (start != 0 || linearLength != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                this = default;
                return; // returns default
            }
            if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
                ThrowHelper.ThrowArrayTypeMismatchException();

            strides = strides.IsEmpty ? (ReadOnlySpan<nint>)TensorSpanHelpers.CalculateStrides(lengths, linearLength) : strides;
            TensorSpanHelpers.ValidateStrides(strides, lengths);
            nint maxElements = TensorSpanHelpers.ComputeMaxLinearIndex(strides, lengths);

            if (Environment.Is64BitProcess)
            {
                // See comment in Span<T>.Slice for how this works.
                if ((ulong)(uint)start + (ulong)(uint)maxElements >= (ulong)(uint)array.Length && array.Length != 0)
                    ThrowHelper.ThrowArgument_InvalidStridesAndLengths();
            }
            else
            {
                if (((uint)start > (uint)array.Length || (uint)maxElements >= (uint)(array.Length - start)) && array.Length != 0)
                    ThrowHelper.ThrowArgument_InvalidStridesAndLengths();
            }

            _shape = new TensorShape(array.Length - start, lengths, strides);
            _reference = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (nint)(uint)start /* force zero-extension */);
        }

        /// <summary>
        /// Creates a new <see cref="ReadOnlyTensorSpan{T}"/> over the provided <see cref="ReadOnlySpan{T}"/>. The new <see cref="ReadOnlyTensorSpan{T}"/> will
        /// have a rank of 1 and a length equal to the length of the provided <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        /// <param name="span">The target span.</param>
        public ReadOnlyTensorSpan(ReadOnlySpan<T> span) : this(span, [span.Length], []) { }

        /// <summary>
        /// Creates a new <see cref="ReadOnlyTensorSpan{T}"/> over the provided <see cref="Span{T}"/> using the specified lengths and strides.
        /// If the strides are not provided, they will be automatically calculated.
        /// </summary>
        /// <param name="span">The target span.</param>
        /// <param name="lengths">The lengths of each dimension.</param>
        /// <param name="strides">The strides for each dimension. Will be automatically calculated if not provided.</param>
        public ReadOnlyTensorSpan(ReadOnlySpan<T> span, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            if (lengths.IsEmpty)
                lengths = [span.Length];

            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);

            strides = strides.IsEmpty ? (ReadOnlySpan<nint>)TensorSpanHelpers.CalculateStrides(lengths, linearLength) : strides;
            TensorSpanHelpers.ValidateStrides(strides, lengths);
            nint maxElements = TensorSpanHelpers.ComputeMaxLinearIndex(strides, lengths);
            if (maxElements >= span.Length && span.Length != 0)
                ThrowHelper.ThrowArgument_InvalidStridesAndLengths();

            _shape = new TensorShape(span.Length, lengths, strides);
            _reference = ref MemoryMarshal.GetReference(span);
        }

        /// <summary>
        /// Creates a new <see cref="ReadOnlyTensorSpan{T}"/> over the provided <see cref="Array"/>. The new <see cref="ReadOnlyTensorSpan{T}"/> will
        /// have a rank of 1 and a length equal to the length of the provided <see cref="Array"/>.
        /// </summary>
        /// <param name="array">The target array.</param>
        public ReadOnlyTensorSpan(Array? array) : this(array, ReadOnlySpan<int>.Empty, array == null ? [0] : (from dim in Enumerable.Range(0, array.Rank) select (nint)array.GetLength(dim)).ToArray(), []) { }

        /// <summary>
        /// Creates a new <see cref="ReadOnlyTensorSpan{T}"/> over the provided <see cref="Array"/> using the specified start offsets, lengths, and strides.
        /// If the strides are not provided, they will be automatically calculated.
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <param name="start">The starting offset for each dimension.</param>
        /// <param name="lengths">The lengths of each dimension.</param>
        /// <param name="strides">The strides for each dimension. Will be automatically calculated if not provided.</param>
        public ReadOnlyTensorSpan(Array? array, scoped ReadOnlySpan<int> start, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            if (lengths.IsEmpty && array != null)
                lengths = (from dim in Enumerable.Range(0, array.Rank) select (nint)array.GetLength(dim)).ToArray();

            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);
            if (array == null)
            {
                if (!start.IsEmpty || linearLength != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                this = default;
                return; // returns default
            }
            if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
                ThrowHelper.ThrowArrayTypeMismatchException();

            strides = strides.IsEmpty ? (ReadOnlySpan<nint>)TensorSpanHelpers.CalculateStrides(lengths, linearLength) : strides;
            TensorSpanHelpers.ValidateStrides(strides, lengths);

            nint startOffset = TensorSpanHelpers.ComputeStartOffsetSystemArray(array, start);
            nint maxElements = TensorSpanHelpers.ComputeMaxLinearIndex(strides, lengths);
            if (Environment.Is64BitProcess)
            {
                // See comment in Span<T>.Slice for how this works.
                if ((ulong)(uint)startOffset + (ulong)(uint)maxElements >= (ulong)(uint)array.Length && array.Length != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException();
            }
            else
            {
                if (((uint)startOffset > (uint)array.Length || (uint)maxElements >= (uint)(array.Length - startOffset)) && array.Length != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException();
            }

            _shape = new TensorShape(array.Length - startOffset, lengths, strides);
            _reference = ref Unsafe.Add(ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(array)), (nint)(uint)startOffset /* force zero-extension */);
        }

        /// <summary>
        /// Creates a new <see cref="ReadOnlyTensorSpan{T}"/> over the provided <see cref="Array"/> using the specified start offsets, lengths, and strides.
        /// If the strides are not provided, they will be automatically calculated.
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <param name="startIndex">The starting offset for each dimension.</param>
        /// <param name="lengths">The lengths of each dimension.</param>
        /// <param name="strides">The strides for each dimension. Will be automatically calculated if not provided.</param>
        public ReadOnlyTensorSpan(Array? array, scoped ReadOnlySpan<NIndex> startIndex, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            if (lengths.IsEmpty && array != null)
                lengths = (from dim in Enumerable.Range(0, array.Rank) select (nint)array.GetLength(dim)).ToArray();

            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);
            if (array == null)
            {
                if (!startIndex.IsEmpty || linearLength != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                this = default;
                return; // returns default
            }
            if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
                ThrowHelper.ThrowArrayTypeMismatchException();

            strides = strides.IsEmpty ? (ReadOnlySpan<nint>)TensorSpanHelpers.CalculateStrides(lengths, linearLength) : strides;
            TensorSpanHelpers.ValidateStrides(strides, lengths);

            nint startOffset = TensorSpanHelpers.ComputeStartOffsetSystemArray(array, startIndex);
            nint maxElements = TensorSpanHelpers.ComputeMaxLinearIndex(strides, lengths);
            if (Environment.Is64BitProcess)
            {
                // See comment in Span<T>.Slice for how this works.
                if ((ulong)(uint)startOffset + (ulong)(uint)maxElements > (ulong)(uint)array.Length)
                    ThrowHelper.ThrowArgumentOutOfRangeException();
            }
            else
            {
                if ((uint)startOffset > (uint)array.Length || (uint)maxElements >= (uint)(array.Length - startOffset))
                    ThrowHelper.ThrowArgumentOutOfRangeException();
            }

            _shape = new TensorShape(array.Length - startOffset, lengths, strides);
            _reference = ref Unsafe.Add(ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(array)), (nint)(uint)startOffset /* force zero-extension */);
        }

        /// <summary>
        /// Creates a new span over the target unmanaged buffer.  Clearly this
        /// is quite dangerous the length is not checked.
        /// But if this creation is correct, then all subsequent uses are correct.
        /// </summary>
        /// <param name="data">An unmanaged data to memory.</param>
        /// <param name="dataLength">The number of elements the unmanaged memory can hold.</param>
        [CLSCompliant(false)]
        public unsafe ReadOnlyTensorSpan(T* data, nint dataLength) : this(data, dataLength, [dataLength], []) { }

        /// <summary>
        /// Creates a new span over the target unmanaged buffer.  Clearly this
        /// is quite dangerous, because the length is not checked.
        /// But if this creation is correct, then all subsequent uses are correct.
        /// </summary>
        /// <param name="data">An unmanaged data to memory.</param>
        /// <param name="dataLength">The number of elements the unmanaged memory can hold.</param>
        /// <param name="lengths">The lengths of the dimensions. If default is provided its assumed to have 1 dimension with a length equal to the length of the data.</param>
        /// <param name="strides">The lengths of the strides. If nothing is provided it figures out the default stride configuration.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <typeparamref name="T"/> is reference type or contains pointers and hence cannot be stored in unmanaged memory.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified length is negative.
        /// </exception>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ReadOnlyTensorSpan(T* data, nint dataLength, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            if (dataLength < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException();

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));

            if (lengths.IsEmpty)
                lengths = [dataLength];

            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);

            strides = strides.IsEmpty ? (ReadOnlySpan<nint>)TensorSpanHelpers.CalculateStrides(lengths, linearLength) : strides;
            TensorSpanHelpers.ValidateStrides(strides, lengths);
            nint maxElements = TensorSpanHelpers.ComputeMaxLinearIndex(strides, lengths);
            if (maxElements >= dataLength && dataLength != 0)
                ThrowHelper.ThrowArgument_InvalidStridesAndLengths();

            _shape = new TensorShape(dataLength, lengths, strides);
            _reference = ref *data;
        }

        // Constructor for internal use only. It is not safe to expose publicly.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlyTensorSpan(ref T reference, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, nint memoryLength)
        {
            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);

            _shape = new TensorShape(memoryLength, lengths, strides);
            _reference = ref reference;
        }

        /// <summary>
        /// Returns a reference to specified element of the ReadOnlyTensorSpan.
        /// </summary>
        /// <param name="indexes"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when any index is less than 0 or any index is greater than or equal to FlattenedLength
        /// </exception>
        public ref readonly T this[params scoped ReadOnlySpan<nint> indexes]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (indexes.Length != Rank)
                    ThrowHelper.ThrowIndexOutOfRangeException();

                nint index = TensorSpanHelpers.ComputeLinearIndex(indexes, Strides, Lengths);
                if (index >= _shape._memoryLength || index < 0)
                    ThrowHelper.ThrowIndexOutOfRangeException();

                return ref Unsafe.Add(ref _reference, index /* force zero-extension */);
            }
        }

        /// <summary>
        /// Returns a reference to specified element of the ReadOnlyTensorSpan.
        /// </summary>
        /// <param name="indexes"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when any index is less than 0 or any index is greater than or equal to FlattenedLength
        /// </exception>
        public ref readonly T this[params scoped ReadOnlySpan<NIndex> indexes]
        {
            get
            {
                if (indexes.Length != Rank)
                    ThrowHelper.ThrowIndexOutOfRangeException();

                nint index = TensorSpanHelpers.ComputeLinearIndex(indexes, Strides, Lengths);
                if (index >= _shape._memoryLength || index < 0)
                    ThrowHelper.ThrowIndexOutOfRangeException();

                return ref Unsafe.Add(ref _reference, index /* force zero-extension */);
            }
        }

        /// <summary>
        /// Returns a slice of the ReadOnlyTensorSpan.
        /// </summary>
        /// <param name="ranges"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when any index is less than 0 or any index is greater than or equal to FlattenedLength
        /// </exception>
        public ReadOnlyTensorSpan<T> this[params scoped ReadOnlySpan<NRange> ranges]
        {
            get
            {
                if (ranges.Length != Rank)
                    ThrowHelper.ThrowIndexOutOfRangeException();

                return Slice(ranges);
            }
        }

        /// <summary>
        /// The number of items in the span.
        /// </summary>
        public nint FlattenedLength => _shape.FlattenedLength;

        /// <summary>
        /// Gets a value indicating whether this <see cref="TensorSpan{T}"/> is empty.
        /// </summary>
        /// <value><see langword="true"/> if this span is empty; otherwise, <see langword="false"/>.</value>
        public bool IsEmpty => _shape.IsEmpty;

        /// <summary>
        /// Gets the length of each dimension in this <see cref="TensorSpan{T}"/>.
        /// </summary>
        [UnscopedRef]
        public ReadOnlySpan<nint> Lengths => _shape.Lengths;

        /// <summary>
        /// Gets the rank, aka the number of dimensions, of this <see cref="TensorSpan{T}"/>.
        /// </summary>
        public int Rank => Lengths.Length;

        /// <summary>
        /// Gets the strides of this <see cref="TensorSpan{T}"/>
        /// </summary>
        [UnscopedRef]
        public ReadOnlySpan<nint> Strides => _shape.Strides;

        /// <summary>
        /// Returns false if left and right point at the same memory and have the same length.  Note that
        /// this does *not* check to see if the *contents* are equal.
        /// </summary>
        public static bool operator !=(ReadOnlyTensorSpan<T> left, ReadOnlyTensorSpan<T> right) => !(left == right);

        /// <summary>
        /// Returns true if left and right point at the same memory and have the same length.  Note that
        /// this does *not* check to see if the *contents* are equal.
        /// </summary>
        public static bool operator ==(ReadOnlyTensorSpan<T> left, ReadOnlyTensorSpan<T> right) =>
            left._shape.FlattenedLength == right._shape.FlattenedLength &&
            left.Rank == right.Rank &&
            left._shape.Lengths.SequenceEqual(right._shape.Lengths )&&
            Unsafe.AreSame(ref left._reference, ref right._reference);

        /// <summary>
        /// This method is not supported as spans cannot be boxed. To compare two spans, use operator==.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// Always thrown by this method.
        /// </exception>
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
        [Obsolete("Equals() on ReadOnlyTensorSpan will always throw an exception. Use the equality operator instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object? obj) =>
            throw new NotSupportedException(SR.NotSupported_CannotCallEqualsOnSpan);

        /// <summary>
        /// This method is not supported as spans cannot be boxed.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// Always thrown by this method.
        /// </exception>
        [Obsolete("GetHashCode() on ReadOnlyTensorSpan will always throw an exception.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() =>
            throw new NotSupportedException(SR.NotSupported_CannotCallGetHashCodeOnSpan);
#pragma warning restore CS0809

        /// <summary>
        /// Returns a 0-length read-only span whose base is the null pointer.
        /// </summary>
        public static ReadOnlyTensorSpan<T> Empty => default;

        /// <summary>
        /// Casts a read-only span of <typeparamref name="TDerived"/> to a read-only span of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="TDerived">The element type of the source read-only span, which must be derived from <typeparamref name="T"/>.</typeparam>
        /// <param name="items">The source read-only span. No copy is made.</param>
        /// <returns>A read-only span with elements cast to the new type.</returns>
        /// <remarks>This method uses a covariant cast, producing a read-only span that shares the same memory as the source. The relationships expressed in the type constraints ensure that the cast is a safe operation.</remarks>
        public static ReadOnlyTensorSpan<T> CastUp<TDerived>(ReadOnlyTensorSpan<TDerived> items) where TDerived : class?, T
        {
            return new ReadOnlyTensorSpan<T>(ref Unsafe.As<TDerived, T>(ref items._reference), items._shape.Lengths, items._shape.Strides, items._shape._memoryLength);
        }

        /// <summary>Gets an enumerator for this span.</summary>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>Enumerates the elements of a <see cref="ReadOnlyTensorSpan{T}"/>.</summary>
        public ref struct Enumerator
        {
            /// <summary>The span being enumerated.</summary>
            private readonly ReadOnlyTensorSpan<T> _span;
            /// <summary>The current index that the enumerator is on.</summary>
            private Span<nint> _curIndexes;
            /// <summary>The total item count.</summary>
            private nint _items;

            /// <summary>Initialize the enumerator.</summary>
            /// <param name="span">The span to enumerate.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(ReadOnlyTensorSpan<T> span)
            {
                _span = span;
                _items = -1;
                _curIndexes = new nint[_span.Rank];
                _curIndexes[_span.Rank - 1] = -1;
            }

            /// <summary>Advances the enumerator to the next element of the span.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                TensorSpanHelpers.AdjustIndexes(_span.Rank - 1, 1, _curIndexes, _span.Lengths);

                if (_items < _span.FlattenedLength)
                    _items++;

                return _items < _span.FlattenedLength;
            }

            /// <summary>Gets the element at the current position of the enumerator.</summary>
            public ref readonly T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _span[_curIndexes];
            }
        }

        /// <summary>
        /// Returns a reference to the 0th element of the ReadOnlyTensorSpan. If the ReadOnlyTensorSpan is empty, returns null reference.
        /// It can be used for pinning and is required to support the use of span within a fixed statement.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref readonly T GetPinnableReference()
        {
            // Ensure that the native code has just one forward branch that is predicted-not-taken.
            ref T ret = ref Unsafe.NullRef<T>();
            if (_shape.FlattenedLength != 0) ret = ref _reference;
            return ref ret;
        }

        /// <summary>
        /// Copies the contents of this read-only span into destination span. If the source
        /// and destinations overlap, this method behaves as if the original values in
        /// a temporary location before the destination is overwritten.
        /// </summary>
        /// <param name="destination">The span to copy items into.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the destination ReadOnlyTensorSpan is shorter than the source ReadOnlyTensorSpan.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(scoped TensorSpan<T> destination)
        {
            // Using "if (!TryCopyTo(...))" results in two branches: one for the length
            // check, and one for the result of TryCopyTo. Since these checks are equivalent,
            // we can optimize by performing the check once ourselves then calling Memmove directly.
            if (_shape.FlattenedLength <= destination.FlattenedLength)
            {
                scoped Span<nint> curIndexes;
                nint[]? curIndexesArray;
                if (Rank > TensorShape.MaxInlineRank)
                {
                    curIndexesArray = ArrayPool<nint>.Shared.Rent(Rank);
                    curIndexes = curIndexesArray;
                    curIndexes = curIndexes.Slice(0, Rank);
                }
                else
                {
                    curIndexesArray = null;
                    curIndexes = stackalloc nint[Rank];
                }

                nint copiedValues = 0;
                TensorSpan<T> slice = destination.Slice(_shape.Lengths);
                while (copiedValues < _shape.FlattenedLength)
                {
                    TensorSpanHelpers.Memmove(ref Unsafe.Add(ref slice._reference, TensorSpanHelpers.ComputeLinearIndex(curIndexes, Strides, Lengths)), ref Unsafe.Add(ref _reference, TensorSpanHelpers.ComputeLinearIndex(curIndexes, Strides, Lengths)), Lengths[Rank - 1]);
                    TensorSpanHelpers.AdjustIndexes(Rank - 2, 1, curIndexes, _shape.Lengths);
                    copiedValues += Lengths[Rank - 1];
                }
                Debug.Assert(copiedValues == _shape.FlattenedLength, "Didn't copy the right amount to the array.");

                if (curIndexesArray != null)
                    ArrayPool<nint>.Shared.Return(curIndexesArray);
            }
            else
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }
        }

        /// <summary>
        /// Copies the contents of this read-only span into destination span. If the source
        /// and destinations overlap, this method behaves as if the original values in
        /// a temporary location before the destination is overwritten.
        /// </summary>
        /// <returns>If the destination span is shorter than the source span, this method
        /// return false and no data is written to the destination.</returns>
        /// <param name="destination">The span to copy items into.</param>
        public bool TryCopyTo(scoped TensorSpan<T> destination)
        {
            bool retVal = false;

            if (_shape.FlattenedLength <= destination.FlattenedLength)
            {
                scoped Span<nint> curIndexes;
                nint[]? curIndexesArray;
                if (Rank > TensorShape.MaxInlineRank)
                {
                    curIndexesArray = ArrayPool<nint>.Shared.Rent(Rank);
                    curIndexes = curIndexesArray;
                    curIndexes = curIndexes.Slice(0, Rank);
                }
                else
                {
                    curIndexesArray = null;
                    curIndexes = stackalloc nint[Rank];
                }

                nint copiedValues = 0;
                TensorSpan<T> slice = destination.Slice(_shape.Lengths);
                while (copiedValues < _shape.FlattenedLength)
                {
                    TensorSpanHelpers.Memmove(ref Unsafe.Add(ref slice._reference, TensorSpanHelpers.ComputeLinearIndex(curIndexes, Strides, Lengths)), ref Unsafe.Add(ref _reference, TensorSpanHelpers.ComputeLinearIndex(curIndexes, Strides, Lengths)), Lengths[Rank - 1]);
                    TensorSpanHelpers.AdjustIndexes(Rank - 2, 1, curIndexes, _shape.Lengths);
                    copiedValues += Lengths[Rank - 1];
                }
                retVal = true;
                Debug.Assert(copiedValues == _shape.FlattenedLength, "Didn't copy the right amount to the array.");

                if (curIndexesArray != null)
                    ArrayPool<nint>.Shared.Return(curIndexesArray);
            }

            return retVal;
        }

        //public static explicit operator TensorSpan<T>(Array? array);
        public static implicit operator ReadOnlyTensorSpan<T>(T[]? array) => new ReadOnlyTensorSpan<T>(array);

        /// <summary>
        /// Returns a <see cref="string"/> with the name of the type and the number of elements.
        /// </summary>
        public override string ToString() => $"System.Numerics.Tensors.ReadOnlyTensorSpan<{typeof(T).Name}>[{_shape.FlattenedLength}]";

        /// <summary>
        /// Returns a reference to specified element of the TensorSpan.
        /// </summary>
        /// <param name="indexes">The indexes for the slice.</param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when any index is less than 0 or any index is greater than or equal to FlattenedLength
        /// </exception>
        public ReadOnlyTensorSpan<T> Slice(params scoped ReadOnlySpan<NIndex> indexes)
        {
            NRange[] ranges = new NRange[indexes.Length];
            for (int i = 0; i < indexes.Length; i++)
            {
                ranges[i] = new NRange(checked((int)indexes[i].GetOffset(Lengths[i])), Lengths[i]);
            }
            return Slice(ranges);
        }

        /// <summary>
        /// Takes in the lengths of the dimensions and slices according to them.
        /// </summary>
        /// <param name="lengths">The dimension lengths</param>
        /// <returns>A <see cref="ReadOnlyTensorSpan{T}"/> based on the provided <paramref name="lengths"/></returns>
        internal ReadOnlyTensorSpan<T> Slice(scoped ReadOnlySpan<nint> lengths)
        {
            NRange[] ranges = new NRange[lengths.Length];
            for (int i = 0; i < lengths.Length; i++)
            {
                ranges[i] = new NRange(0, lengths[i]);
            }
            return Slice(ranges);
        }

        /// <summary>
        /// Forms a slice out of the given span
        /// </summary>
        /// <param name="ranges">The ranges for the slice</param>
        /// <returns>A <see cref="ReadOnlyTensorSpan{T}"/> based on the provided <paramref name="ranges"/></returns>

        public ReadOnlyTensorSpan<T> Slice(params scoped ReadOnlySpan<NRange> ranges)
        {
            if (ranges.Length != Lengths.Length)
                throw new ArgumentOutOfRangeException(nameof(ranges), "Number of dimensions to slice does not equal the number of dimensions in the span");

            scoped Span<nint> lengths;
            scoped Span<nint> offsets;
            if (Rank > TensorShape.MaxInlineRank)
            {
                lengths = stackalloc nint[Rank];
                offsets = stackalloc nint[Rank];
            }
            else
            {
                lengths = new nint[Rank];
                offsets = new nint[Rank];
            }

            for (int i = 0; i < ranges.Length; i++)
            {
                (offsets[i], lengths[i]) = ranges[i].GetOffsetAndLength(Lengths[i]);
            }

            nint index = 0;
            for (int i = 0; i < offsets.Length; i++)
            {
                index += Strides[i] * (offsets[i]);
            }

            if (index >= _shape._memoryLength || index < 0)
                ThrowHelper.ThrowIndexOutOfRangeException();

            return new ReadOnlyTensorSpan<T>(ref Unsafe.Add(ref _reference, index), lengths, _shape.Strides, _shape._memoryLength - index);
        }

        /// <summary>
        /// Flattens the contents of this span into the provided <see cref="Span{T}"/>.
        /// </summary>
        /// <param name="destination">The span to copy items into.</param>
        public bool TryFlattenTo(scoped Span<T> destination)
        {
            bool retVal = false;
            if (destination.Length < _shape.FlattenedLength)
            {
                scoped Span<nint> curIndexes;
                nint[]? curIndexesArray;
                if (Rank > TensorShape.MaxInlineRank)
                {
                    curIndexesArray = ArrayPool<nint>.Shared.Rent(Rank);
                    curIndexes = curIndexesArray;
                    curIndexes = curIndexes.Slice(0, Rank);
                }
                else
                {
                    curIndexesArray = null;
                    curIndexes = stackalloc nint[Rank];
                }

                nint copiedValues = 0;
                while (copiedValues < _shape.FlattenedLength)
                {
                    TensorSpanHelpers.Memmove(destination.Slice(checked((int)copiedValues)), ref Unsafe.Add(ref _reference, TensorSpanHelpers.ComputeLinearIndex(curIndexes, Strides, Lengths)), Lengths[Rank - 1]);
                    TensorSpanHelpers.AdjustIndexes(Rank - 2, 1, curIndexes, _shape.Lengths);
                    copiedValues += Lengths[Rank - 1];
                }

                if (curIndexesArray != null)
                    ArrayPool<nint>.Shared.Return(curIndexesArray);
                retVal = true;
            }
            return retVal;
        }

        /// <summary>
        /// Flattens the contents of this span into the provided <see cref="Span{T}"/>.
        /// </summary>
        /// <param name="destination">The span to copy items into.</param>
        public void FlattenTo(scoped Span<T> destination)
        {
            if (destination.Length < _shape.FlattenedLength)
                ThrowHelper.ThrowArgumentException_DestinationTooShort();

            if (_shape.FlattenedLength == 0)
                return;

            scoped Span<nint> curIndexes;
            nint[]? curIndexesArray;
            if (Rank > TensorShape.MaxInlineRank)
            {
                curIndexesArray = ArrayPool<nint>.Shared.Rent(Rank);
                curIndexes = curIndexesArray;
                curIndexes = curIndexes.Slice(0, Rank);
            }
            else
            {
                curIndexesArray = null;
                curIndexes = stackalloc nint[Rank];
            }

            nint copiedValues = 0;
            while (copiedValues < _shape.FlattenedLength)
            {
                if (Strides[Rank - 1] == 0)
                {
                    destination.Slice(checked((int)copiedValues), (int)Lengths[Rank - 1]).Fill(Unsafe.Add(ref _reference, TensorSpanHelpers.ComputeLinearIndex(curIndexes, Strides, Lengths)));
                }
                else
                {
                    TensorSpanHelpers.Memmove(destination.Slice(checked((int)copiedValues)), ref Unsafe.Add(ref _reference, TensorSpanHelpers.ComputeLinearIndex(curIndexes, Strides, Lengths)), Lengths[Rank - 1]);
                }
                TensorSpanHelpers.AdjustIndexes(Rank - 2, 1, curIndexes, _shape.Lengths);
                copiedValues += Lengths[Rank - 1];
            }

            if (curIndexesArray != null)
                ArrayPool<nint>.Shared.Return(curIndexesArray);
        }
    }
}
