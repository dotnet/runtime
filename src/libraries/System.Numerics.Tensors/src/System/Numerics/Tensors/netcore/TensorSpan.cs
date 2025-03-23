// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EditorBrowsableAttribute = System.ComponentModel.EditorBrowsableAttribute;
using EditorBrowsableState = System.ComponentModel.EditorBrowsableState;

#pragma warning disable 0809  //warning CS0809: Obsolete member 'TensorSpan<T>.Equals(object)' overrides non-obsolete member 'object.Equals(object)'

namespace System.Numerics.Tensors
{
    /// <summary>
    /// Represents a contiguous region of arbitrary memory. Unlike arrays, it can point to either managed
    /// or native memory, or to memory allocated on the stack. It is type-safe and memory-safe.
    /// </summary>
    [DebuggerTypeProxy(typeof(TensorSpanDebugView<>))]
    [DebuggerDisplay("{ToString(),raw}")]
    [Experimental(Experimentals.TensorTDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public readonly ref struct TensorSpan<T>
    {
        /// <summary>A byref or a native ptr.</summary>
        internal readonly ref T _reference;
        internal readonly TensorShape _shape;


        /// <summary>
        /// Creates a new span over the entirety of the target array.
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
        /// <exception cref="ArrayTypeMismatchException"><paramref name="array"/> is covariant and its type is not exactly T[].</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TensorSpan(T[]? array) : this(array, 0, [array?.Length ?? 0], [])
        {
        }

        /// <summary>
        /// Creates a new span over the portion of the target array beginning
        /// at 'start' index and ending at 'end' index (exclusive).
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <param name="startIndex">The index at which to begin the span.</param>
        /// <param name="lengths">The lengths of the dimensions. If default is provided, it's assumed to have one dimension with a length equal to the length of the data.</param>
        /// <param name="strides">The strides of each dimension. If default or span of length 0 is provided, then strides will be automatically calculated.</param>
        /// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
        /// <exception cref="ArrayTypeMismatchException"><paramref name="array"/> is covariant and its type is not exactly T[].</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The specified <paramref name="startIndex"/> or end index is not in the range (&lt;0 or &gt;FlattenedLength).
        /// </exception>
        public TensorSpan(T[]? array, Index startIndex, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
            : this(array, startIndex.GetOffset(array?.Length ?? 0), lengths, strides)
        {
        }

        /// <summary>
        /// Creates a new span over the portion of the target array beginning
        /// at 'start' index and ending at 'end' index (exclusive).
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <param name="start">The index at which to begin the span.</param>
        /// <param name="lengths">The lengths of the dimensions. If default is provided, it's assumed to have one dimension with a length equal to the length of the data.</param>
        /// <param name="strides">The strides of each dimension. If default or span of length 0 is provided, then strides will be automatically calculated.</param>
        /// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
        /// <exception cref="ArrayTypeMismatchException"><paramref name="array"/> is covariant and its type is not exactly T[].</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The specified <paramref name="start"/> or end index is not in the range (&lt;0 or &gt;FlattenedLength).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TensorSpan(T[]? array, int start, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
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
        /// Creates a new <see cref="TensorSpan{T}"/> over the provided <see cref="Span{T}"/>. The new <see cref="TensorSpan{T}"/> will
        /// have a rank of 1 and a length equal to the length of the provided <see cref="Span{T}"/>.
        /// </summary>
        /// <param name="span">The target span.</param>
        public TensorSpan(Span<T> span) : this(span, [span.Length], []) { }

        /// <summary>
        /// Creates a new <see cref="TensorSpan{T}"/> over the provided <see cref="Span{T}"/> using the specified lengths and strides.
        /// </summary>
        /// <param name="span">The target span.</param>
        /// <param name="lengths">The lengths of each dimension.</param>
        /// <param name="strides">The strides for each dimension. The strides will be automatically calculated if not provided.</param>
        public TensorSpan(Span<T> span, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            if (lengths.IsEmpty)
                lengths = [span.Length];

            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);

            strides = strides.IsEmpty ? (ReadOnlySpan<nint>)TensorSpanHelpers.CalculateStrides(lengths, linearLength) : strides;
            TensorSpanHelpers.ValidateStrides(strides, lengths);

            nint maxElements = TensorSpanHelpers.ComputeMaxLinearIndex(strides, lengths);
            if (span.IsEmpty ? maxElements != 0 : maxElements >= span.Length)
                ThrowHelper.ThrowArgument_InvalidStridesAndLengths();

            _shape = new TensorShape(span.Length, lengths, strides);
            _reference = ref MemoryMarshal.GetReference(span);
        }

        /// <summary>
        /// Creates a new <see cref="TensorSpan{T}"/> over the provided <see cref="Array"/>. The new <see cref="TensorSpan{T}"/> will
        /// have a rank of 1 and a length equal to the length of the provided <see cref="Array"/>.
        /// </summary>
        /// <param name="array">The target array.</param>
        public TensorSpan(Array? array) :
            this(array,
                 ReadOnlySpan<int>.Empty,
                 array == null ?
                     [0] :
                     TensorSpanHelpers.FillLengths(array.Rank <= TensorShape.MaxInlineRank ? stackalloc nint[array.Rank] : new nint[array.Rank], array),
                 [])
        {
        }

        /// <summary>
        /// Creates a new <see cref="TensorSpan{T}"/> over the provided <see cref="Array"/> using the specified start offsets, lengths, and strides.
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <param name="start">The starting offset for each dimension.</param>
        /// <param name="lengths">The lengths of each dimension.</param>
        /// <param name="strides">The strides for each dimension. The strides will be automatically calculated if not provided.</param>
        public TensorSpan(Array? array, scoped ReadOnlySpan<int> start, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            if (lengths.IsEmpty && array != null)
            {
                lengths = TensorSpanHelpers.FillLengths(
                    array.Rank < TensorShape.MaxInlineRank ? stackalloc nint[array.Rank] : new nint[array.Rank],
                    array);
            }

            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);

            if (array == null)
            {
                if (!start.IsEmpty || linearLength != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                this = default;
                return; // returns default
            }
            if (array.GetType().GetElementType() != typeof(T))
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
        /// Creates a new <see cref="TensorSpan{T}"/> over the provided <see cref="Array"/> using the specified start offsets, lengths, and strides.
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <param name="startIndex">The starting offset for each dimension.</param>
        /// <param name="lengths">The lengths of each dimension.</param>
        /// <param name="strides">The strides for each dimension. The strides will be automatically calculated if not provided.</param>
        public TensorSpan(Array? array, scoped ReadOnlySpan<NIndex> startIndex, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            if (lengths.IsEmpty && array != null)
            {
                lengths = TensorSpanHelpers.FillLengths(
                    array.Rank <= TensorShape.MaxInlineRank ? stackalloc nint[array.Rank] : new nint[array.Rank],
                    array);
            }

            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);
            strides = strides.IsEmpty ? (ReadOnlySpan<nint>)TensorSpanHelpers.CalculateStrides(lengths, linearLength) : strides;
            TensorSpanHelpers.ValidateStrides(strides, lengths);
            if (array == null)
            {
                if (!startIndex.IsEmpty || linearLength != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                this = default;
                return; // returns default
            }
            if (array.GetType().GetElementType() != typeof(T))
                ThrowHelper.ThrowArrayTypeMismatchException();

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

            _shape = new TensorShape(array.Length, lengths, strides);
            _reference = ref Unsafe.Add(ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(array)), (nint)(uint)startOffset /* force zero-extension */);
        }

        /// <summary>
        /// Creates a new span over the target unmanaged buffer.
        /// </summary>
        /// <param name="data">An unmanaged data that points to memory.</param>
        /// <param name="dataLength">The number of elements the unmanaged memory can hold.</param>
        /// <remarks>
        /// This constructor is quite dangerous, because the length is not checked.
        /// But if this creation is correct, then all subsequent uses are correct.
        /// </remarks>
        [CLSCompliant(false)]
        public unsafe TensorSpan(T* data, nint dataLength) : this(data, dataLength, [dataLength], []) { }

        /// <summary>
        /// Creates a new span over the target unmanaged buffer.
        /// </summary>
        /// <param name="data">An unmanaged data that points to memory.</param>
        /// <param name="dataLength">The number of elements the unmanaged memory can hold.</param>
        /// <param name="lengths">The lengths of the dimensions. If default is provided, it's assumed to have one dimension with a length equal to the length of the data.</param>
        /// <param name="strides">The lengths of the strides. If nothing is provided, it figures out the default stride configuration.</param>
        /// <exception cref="ArgumentException">
        /// <typeparamref name="T"/> is a reference type or contains pointers and hence cannot be stored in unmanaged memory.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The specified length is negative.
        /// </exception>
        /// <remarks>
        /// This constructor is quite dangerous, because the length is not checked.
        /// But if this creation is correct, then all subsequent uses are correct.
        /// </remarks>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe TensorSpan(T* data, nint dataLength, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
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
        internal TensorSpan(ref T reference, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, nint memoryLength)
        {
            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);

            _shape = new TensorShape(memoryLength, lengths, strides);
            _reference = ref reference;
        }

        /// <summary>
        /// Returns a reference to specified element of the TensorSpan.
        /// </summary>
        /// <param name="indexes"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Any index is less than 0 or greater than or equal to FlattenedLength.
        /// </exception>
        public ref T this[params scoped ReadOnlySpan<nint> indexes]
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
        /// Returns a reference to specified element of the TensorSpan.
        /// </summary>
        /// <param name="indexes"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Any index is less than 0 or greater than or equal to FlattenedLength.
        /// </exception>
        public ref T this[params scoped ReadOnlySpan<NIndex> indexes]
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
        /// Returns a slice of the TensorSpan.
        /// </summary>
        /// <param name="ranges"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Any index is less than 0 or greater than or equal to FlattenedLength.
        /// </exception>
        public TensorSpan<T> this[params scoped ReadOnlySpan<NRange> ranges]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Slice(ranges);
            }
            set
            {
                value.CopyTo(this[ranges]);
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
        /// Compares two spans and returns false if left and right point at the same memory and have the same length.
        /// This operator does *not* check to see if the *contents* are equal.
        /// </summary>
        public static bool operator !=(TensorSpan<T> left, TensorSpan<T> right) => !(left == right);

        /// <summary>
        /// Compares two spans and returns true if left and right point at the same memory and have the same length.
        /// This operator does *not* check to see if the *contents* are equal.
        /// </summary>
        public static bool operator ==(TensorSpan<T> left, TensorSpan<T> right) =>
            left._shape.FlattenedLength == right._shape.FlattenedLength &&
            left.Rank == right.Rank &&
            left._shape.Lengths.SequenceEqual(right._shape.Lengths) &&
            left._shape.Strides.SequenceEqual(right._shape.Strides) &&
            Unsafe.AreSame(ref left._reference, ref right._reference);

        /// <summary>
        /// This method is not supported as spans cannot be boxed. To compare two spans, use operator ==.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// In all cases.
        /// </exception>
        [Obsolete("Equals() on TensorSpan will always throw an exception. Use the equality operator instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object? obj) =>
            throw new NotSupportedException(SR.NotSupported_CannotCallEqualsOnSpan);

        /// <summary>
        /// This method is not supported as spans cannot be boxed.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// In all cases.
        /// </exception>
        [Obsolete("GetHashCode() on TensorSpan will always throw an exception.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() =>
            throw new NotSupportedException(SR.NotSupported_CannotCallGetHashCodeOnSpan);

        /// <summary>
        /// Returns an empty <see cref="TensorSpan{T}"/>
        /// </summary>
        public static TensorSpan<T> Empty => default;

        /// <summary>Gets an enumerator for this span.</summary>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>Enumerates the elements of a <see cref="TensorSpan{T}"/>.</summary>
        public ref struct Enumerator
        {
            /// <summary>The span being enumerated.</summary>
            private readonly TensorSpan<T> _span;
            /// <summary>The current index that the enumerator is on.</summary>
            private Span<nint> _curIndexes;
            /// <summary>The total item count.</summary>
            private nint _items;

            /// <summary>Initialize the enumerator.</summary>
            /// <param name="span">The span to enumerate.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(TensorSpan<T> span)
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
            public ref T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _span[_curIndexes];
            }
        }

        /// <summary>
        /// Returns a reference to the 0th element of the TensorSpan. If the TensorSpan is empty, returns null reference.
        /// It can be used for pinning and is required to support the use of span within a fixed statement.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref T GetPinnableReference()
        {
            // Ensure that the native code has just one forward branch that is predicted-not-taken.
            ref T ret = ref Unsafe.NullRef<T>();
            if (_shape.FlattenedLength != 0) ret = ref _reference;
            return ref ret;
        }

        /// <summary>
        /// Clears the contents of this span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            scoped Span<nint> curIndexes;
            nint[]? curIndexesArray;
            if (Rank > TensorShape.MaxInlineRank)
            {
                curIndexesArray = ArrayPool<nint>.Shared.Rent(Rank);
                curIndexes = curIndexesArray.AsSpan(0, Rank);
            }
            else
            {
                curIndexesArray = null;
                curIndexes = stackalloc nint[Rank];
            }
            curIndexes.Clear();

            nint clearedValues = 0;
            while (clearedValues < _shape.FlattenedLength)
            {
                TensorSpanHelpers.Clear(ref Unsafe.Add(ref _reference, TensorSpanHelpers.ComputeLinearIndex(curIndexes, Strides, Lengths)), (nuint)Lengths[Rank - 1]);
                TensorSpanHelpers.AdjustIndexes(Rank - 2, 1, curIndexes, _shape.Lengths);
                clearedValues += Lengths[Rank - 1];
            }
        }

        /// <summary>
        /// Fills the contents of this span with the given value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(T value)
        {
            MemoryMarshal.CreateSpan<T>(ref _reference, (int)_shape._memoryLength).Fill(value);
        }

        /// <summary>
        /// Copies the contents of this span into destination span. If the source
        /// and destinations overlap, this method behaves as if the original values are in
        /// a temporary location before the destination is overwritten.
        /// </summary>
        /// <param name="destination">The span to copy items into.</param>
        /// <exception cref="ArgumentException">
        /// The destination TensorSpan is shorter than the source Span.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(scoped TensorSpan<T> destination)
        {
            // Using "if (!TryCopyTo(...))" results in two branches: one for the length
            // check, and one for the result of TryCopyTo. Since these checks are equivalent,
            // we can optimize by performing the check once ourselves then calling Memmove directly.
            if (TensorHelpers.IsBroadcastableTo(Lengths, destination.Lengths))
            {
                scoped Span<nint> curIndexes;
                nint[]? curIndexesArray;

                if (Rank > TensorShape.MaxInlineRank)
                {
                    curIndexesArray = ArrayPool<nint>.Shared.Rent(destination.Rank);
                    curIndexes = curIndexesArray.AsSpan(0, destination.Rank);
                }
                else
                {
                    curIndexesArray = null;
                    curIndexes = stackalloc nint[destination.Rank];
                }
                curIndexes.Clear();

                nint copiedValues = 0;
                nint[] tempLengths = Tensor.GetSmallestBroadcastableLengths(Lengths, destination.Lengths);

                TensorSpan<T> destinationSlice = destination.Slice(tempLengths);
                ReadOnlyTensorSpan<T> srcSlice = Tensor.LazyBroadcast(this, tempLengths);
                nint copyLength = srcSlice.Strides[^1] == 1 && TensorHelpers.IsContiguousAndDense(srcSlice) ? srcSlice.Lengths[^1] : 1;
                int indexToAdjust = srcSlice.Strides[^1] == 1 && TensorHelpers.IsContiguousAndDense(srcSlice) ? srcSlice.Rank - 2 : srcSlice.Rank - 1;

                while (copiedValues < destination.FlattenedLength)
                {
                    TensorSpanHelpers.Memmove(ref Unsafe.Add(ref destinationSlice._reference, TensorSpanHelpers.ComputeLinearIndex(curIndexes, destinationSlice.Strides, destinationSlice.Lengths)), ref Unsafe.Add(ref srcSlice._reference, TensorSpanHelpers.ComputeLinearIndex(curIndexes, srcSlice.Strides, srcSlice.Lengths)), copyLength);
                    TensorSpanHelpers.AdjustIndexes(indexToAdjust, 1, curIndexes, tempLengths);
                    copiedValues += copyLength;
                }
                Debug.Assert(copiedValues == destination.FlattenedLength, "Didn't copy the right amount to the array.");

                if (curIndexesArray != null)
                    ArrayPool<nint>.Shared.Return(curIndexesArray);
            }
            else
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }
        }

        /// <summary>
        /// Copies the contents of this span into destination span. If the source
        /// and destinations overlap, this method behaves as if the original values are in
        /// a temporary location before the destination is overwritten.
        /// </summary>
        /// <param name="destination">The span to copy items into.</param>
        /// <returns>If the destination span is shorter than the source span, this method
        /// return false and no data is written to the destination.</returns>
        public bool TryCopyTo(scoped TensorSpan<T> destination)
        {
            bool retVal = false;

            if (TensorHelpers.IsBroadcastableTo(Lengths, destination.Lengths))
            {
                scoped Span<nint> curIndexes;
                nint[]? curIndexesArray;

                if (Rank > TensorShape.MaxInlineRank)
                {
                    curIndexesArray = ArrayPool<nint>.Shared.Rent(destination.Rank);
                    curIndexes = curIndexesArray.AsSpan(0, destination.Rank);
                }
                else
                {
                    curIndexesArray = null;
                    curIndexes = stackalloc nint[destination.Rank];
                }
                curIndexes.Clear();

                nint copiedValues = 0;
                nint[] tempLengths = Tensor.GetSmallestBroadcastableLengths(Lengths, destination.Lengths);

                TensorSpan<T> destinationSlice = destination.Slice(tempLengths);
                ReadOnlyTensorSpan<T> srcSlice = Tensor.LazyBroadcast(this, tempLengths);
                nint copyLength = srcSlice.Strides[^1] == 1 && TensorHelpers.IsContiguousAndDense(srcSlice) ? srcSlice.Lengths[^1] : 1;
                int indexToAdjust = srcSlice.Strides[^1] == 1 && TensorHelpers.IsContiguousAndDense(srcSlice) ? srcSlice.Rank - 2 : srcSlice.Rank - 1;

                while (copiedValues < destination.FlattenedLength)
                {
                    TensorSpanHelpers.Memmove(ref Unsafe.Add(ref destinationSlice._reference, TensorSpanHelpers.ComputeLinearIndex(curIndexes, destinationSlice.Strides, destinationSlice.Lengths)), ref Unsafe.Add(ref srcSlice._reference, TensorSpanHelpers.ComputeLinearIndex(curIndexes, srcSlice.Strides, srcSlice.Lengths)), copyLength);
                    TensorSpanHelpers.AdjustIndexes(indexToAdjust, 1, curIndexes, tempLengths);
                    copiedValues += copyLength;
                }
                Debug.Assert(copiedValues == destination.FlattenedLength, "Didn't copy the right amount to the array.");
                retVal = true;

                if (curIndexesArray != null)
                    ArrayPool<nint>.Shared.Return(curIndexesArray);
            }
            return retVal;
        }

        /// <summary>
        /// Implicitly converts an array to a <see cref="TensorSpan{T}"/>.
        /// </summary>
        public static implicit operator TensorSpan<T>(T[]? array) => new TensorSpan<T>(array);

        /// <summary>
        /// Implicitly converts a <see cref="TensorSpan{T}"/> to a <see cref="ReadOnlyTensorSpan{T}"/>
        /// </summary>
        public static implicit operator ReadOnlyTensorSpan<T>(TensorSpan<T> span) =>
            new ReadOnlyTensorSpan<T>(ref span._reference, span._shape.Lengths, span._shape.Strides, span._shape._memoryLength);

        /// <summary>
        /// For <see cref="Span{Char}"/>, returns a new instance of string that represents the characters pointed to by the span.
        /// Otherwise, returns a <see cref="string"/> with the name of the type and the number of elements.
        /// </summary>
        public override string ToString() => $"System.Numerics.Tensors.TensorSpan<{typeof(T).Name}>[{_shape.FlattenedLength}]";

        /// <summary>
        /// Returns a reference to specified element of the TensorSpan.
        /// </summary>
        /// <param name="indexes">The indexes for the slice.</param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Any index is less than 0 or greater than or equal to <c>FlattenedLength</c>.
        /// </exception>
        public TensorSpan<T> Slice(params scoped ReadOnlySpan<NIndex> indexes)
        {
            NRange[] ranges = new NRange[indexes.Length];
            for (int i = 0; i < indexes.Length; i++)
            {
                ranges[i] = new NRange(checked((int)indexes[i].GetOffset(Lengths[i])), Lengths[i]);
            }
            return Slice(ranges);
        }

        /// <summary>
        /// Slices a span according to the provided lengths of the dimensions.
        /// </summary>
        /// <param name="lengths">The dimension lengths.</param>
        /// <returns>A <see cref="ReadOnlyTensorSpan{T}"/> based on the provided <paramref name="lengths"/>.</returns>
        internal TensorSpan<T> Slice(params scoped ReadOnlySpan<nint> lengths)
        {
            NRange[] ranges = new NRange[lengths.Length];
            for (int i = 0; i < lengths.Length; i++)
            {
                ranges[i] = new NRange(0, lengths[i]);
            }
            return Slice(ranges);
        }

        /// <summary>
        /// Forms a slice out of the given span.
        /// </summary>
        /// <param name="ranges">The ranges for the slice.</param>
        /// <returns>A <see cref="ReadOnlyTensorSpan{T}"/> based on the provided <paramref name="ranges"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TensorSpan<T> Slice(params scoped ReadOnlySpan<NRange> ranges)
        {
            if (ranges.Length != Lengths.Length)
                ThrowHelper.ThrowIndexOutOfRangeException();

            TensorSpan<T> toReturn;
            scoped Span<nint> lengths;
            scoped Span<nint> offsets;
            nint[]? lengthsArray;
            nint[]? offsetsArray;
            if (Rank > TensorShape.MaxInlineRank)
            {
                lengthsArray = ArrayPool<nint>.Shared.Rent(Rank);
                lengths = lengthsArray.AsSpan(0, Rank);

                offsetsArray = ArrayPool<nint>.Shared.Rent(Rank);
                offsets = offsetsArray.AsSpan(0, Rank);
            }
            else
            {
                lengths = stackalloc nint[Rank];
                offsets = stackalloc nint[Rank];

                lengthsArray = null;
                offsetsArray = null;
            }
            lengths.Clear();
            offsets.Clear();

            for (int i = 0; i < ranges.Length; i++)
            {
                (offsets[i], lengths[i]) = ranges[i].GetOffsetAndLength(Lengths[i]);
            }

            // When we have an empty Tensor and someone wants to slice all of it, we should return an empty Tensor.
            // FlattenedLength is computed everytime so using a local to cache the value.
            nint flattenedLength = FlattenedLength;
            nint index = 0;

            if (flattenedLength != 0)
            {
                for (int i = 0; i < offsets.Length; i++)
                {
                    index += Strides[i] * (offsets[i]);
                }
            }

            if ((index >= _shape._memoryLength || index < 0) && flattenedLength != 0)
                ThrowHelper.ThrowIndexOutOfRangeException();

            toReturn = new TensorSpan<T>(ref Unsafe.Add(ref _reference, index), lengths, _shape.Strides, _shape._memoryLength - index);

            if (offsetsArray != null)
                ArrayPool<nint>.Shared.Return(offsetsArray);
            if (lengthsArray != null)
                ArrayPool<nint>.Shared.Return(lengthsArray);

            return toReturn;
        }

        /// <summary>
        /// Flattens the contents of this span into the provided <see cref="Span{T}"/>.
        /// </summary>
        /// <param name="destination">The span to copy items into.</param>
        public bool TryFlattenTo(scoped Span<T> destination)
        {
            bool retVal = false;
            if (destination.Length <= _shape.FlattenedLength)
            {
                FlattenTo(destination);
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
                curIndexes = curIndexesArray.AsSpan(0, Rank);
            }
            else
            {
                curIndexesArray = null;
                curIndexes = stackalloc nint[Rank];
            }
            curIndexes.Clear();

            nint copiedValues = 0;
            while (copiedValues < _shape.FlattenedLength)
            {
                TensorSpanHelpers.Memmove(destination.Slice(checked((int)copiedValues)), ref Unsafe.Add(ref _reference, TensorSpanHelpers.ComputeLinearIndex(curIndexes, Strides, Lengths)), Lengths[Rank - 1]);
                TensorSpanHelpers.AdjustIndexes(Rank - 2, 1, curIndexes, _shape.Lengths);
                copiedValues += Lengths[Rank - 1];
            }

            if (curIndexesArray != null)
                ArrayPool<nint>.Shared.Return(curIndexesArray);
        }
    }
}
