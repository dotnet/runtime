// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
using EditorBrowsableAttribute = System.ComponentModel.EditorBrowsableAttribute;
using EditorBrowsableState = System.ComponentModel.EditorBrowsableState;

#pragma warning disable 0809  //warning CS0809: Obsolete member 'TensorSpan<T>.Equals(object)' overrides non-obsolete member 'object.Equals(object)'
#pragma warning disable 8500 // address / sizeof of managed types

namespace System.Numerics.Tensors
{
    /// <summary>
    /// TensorSpan represents a contiguous region of arbitrary memory. Unlike arrays, it can point to either managed
    /// or native memory, or to memory allocated on the stack. It is type-safe and memory-safe.
    /// </summary>
    [DebuggerTypeProxy(typeof(TensorSpanDebugView<>))]
    [DebuggerDisplay("{ToString(),raw}")]
    public readonly ref struct TensorSpan<T>
    {
        /// <summary>A byref or a native ptr.</summary>
        internal readonly ref T _reference;
        /// <summary>The number of elements this TensorSpan contains.</summary>
        internal readonly nint _flattenedLength;
        /// <summary>The length of the underlying memory. Can be different than the number of elements in the span.</summary>
        internal readonly nint _memoryLength;
        /// <summary>The lengths of each dimension.</summary>
        internal readonly ReadOnlySpan<nint> _lengths;
        /// <summary>The strides representing the memory offsets for each dimension.</summary>
        private readonly ReadOnlySpan<nint> _strides;


        /// <summary>
        /// Creates a new span over the entirety of the target array.
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
        /// <exception cref="ArrayTypeMismatchException">Thrown when <paramref name="array"/> is covariant and array's type is not exactly T[].</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TensorSpan(T[]? array) : this(array, 0, [], [])
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
        /// <param name="lengths">The lengths of the dimensions. If default is provided its assumed to have 1 dimension with a length equal to the length of the data.</param>
        /// <param name="strides">The strides of each dimension. If default or span of length 0 is provided then strides will be automatically calculated.</param>
        /// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
        /// <exception cref="ArrayTypeMismatchException">Thrown when <paramref name="array"/> is covariant and array's type is not exactly T[].</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in the range (&lt;0 or &gt;FlattenedLength).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TensorSpan(T[]? array, int start, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
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

            strides = strides.IsEmpty ? (ReadOnlySpan<nint>)TensorSpanHelpers.CalculateStrides(lengths) : strides;
            nint maxElements = TensorSpanHelpers.ComputeMaxElementCount(strides, lengths);

            if (Environment.Is64BitProcess)
            {
                // See comment in Span<T>.Slice for how this works.
                if ((ulong)(uint)start + (ulong)(uint)maxElements > (ulong)(uint)array.Length)
                    ThrowHelper.ThrowArgumentOutOfRangeException();
            }
            else
            {
                if ((uint)start > (uint)array.Length || (uint)maxElements > (uint)(array.Length - start))
                    ThrowHelper.ThrowArgumentOutOfRangeException();
            }

            _flattenedLength = linearLength;
            _memoryLength = array.Length - start;
            _reference = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (nint)(uint)start /* force zero-extension */);

            _lengths = lengths.ToArray();
            _strides = strides.ToArray();
        }

        /// <summary>
        /// Creates a new <see cref="TensorSpan{T}"/> over the provided <see cref="Span{T}"/>. The new <see cref="TensorSpan{T}"/> will
        /// have a rank of 1 and a length equal to the length of the provided <see cref="Span{T}"/>.
        /// </summary>
        /// <param name="span">The target span.</param>
        public TensorSpan(Span<T> span) : this(span, [span.Length], []) { }

        /// <summary>
        /// Creates a new <see cref="TensorSpan{T}"/> over the provided <see cref="Span{T}"/> using the specified lengths and strides.
        /// If the strides are not provided, they will be automatically calculated.
        /// </summary>
        /// <param name="span">The target span.</param>
        /// <param name="lengths">The lengths of each dimension.</param>
        /// <param name="strides">The strides for each dimension. Will be automatically calculated if not provided.</param>
        public TensorSpan(Span<T> span, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);
            if (span.IsEmpty)
            {
                if (linearLength != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                this = default;
                return; // returns default
            }

            strides = strides.IsEmpty ? (ReadOnlySpan<nint>)TensorSpanHelpers.CalculateStrides(lengths) : strides;
            nint maxElements = TensorSpanHelpers.ComputeMaxElementCount(strides, lengths);
            if (maxElements > span.Length)
                ThrowHelper.ThrowArgument_InvalidStridesAndLengths();

            _flattenedLength = linearLength;
            _memoryLength = span.Length;
            _reference = ref MemoryMarshal.GetReference(span);

            _lengths = lengths.ToArray();
            _strides = strides.ToArray();
        }

        /// <summary>
        /// Creates a new <see cref="TensorSpan{T}"/> over the provided <see cref="Array"/>. The new <see cref="TensorSpan{T}"/> will
        /// have a rank of 1 and a length equal to the length of the provided <see cref="Array"/>.
        /// </summary>
        /// <param name="array">The target array.</param>
        public TensorSpan(Array? array) : this(array, ReadOnlySpan<int>.Empty, [], []) { }

        /// <summary>
        /// Creates a new <see cref="TensorSpan{T}"/> over the provided <see cref="Array"/> using the specified start offsets, lengths, and strides.
        /// If the strides are not provided, they will be automatically calculated.
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <param name="start">The starting offset for each dimension.</param>
        /// <param name="lengths">The lengths of each dimension.</param>
        /// <param name="strides">The strides for each dimension. Will be automatically calculated if not provided.</param>
        public TensorSpan(Array? array, scoped ReadOnlySpan<int> start, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);
            strides = strides.IsEmpty ? (ReadOnlySpan<nint>)TensorSpanHelpers.CalculateStrides(lengths) : strides;
            nint startOffset = TensorSpanHelpers.ComputeLinearIndex(start, strides, lengths);
            if (array == null)
            {
                if (!start.IsEmpty || linearLength != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                this = default;
                return; // returns default
            }
            if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
                ThrowHelper.ThrowArrayTypeMismatchException();

            nint maxElements = TensorSpanHelpers.ComputeMaxElementCount(strides, lengths);
            if (Environment.Is64BitProcess)
            {
                // See comment in Span<T>.Slice for how this works.
                if ((ulong)(uint)startOffset + (ulong)(uint)maxElements > (ulong)(uint)array.Length)
                    ThrowHelper.ThrowArgumentOutOfRangeException();
            }
            else
            {
                if ((uint)startOffset > (uint)array.Length || (uint)maxElements > (uint)(array.Length - startOffset))
                    ThrowHelper.ThrowArgumentOutOfRangeException();
            }

            _flattenedLength = linearLength;
            _memoryLength = array.Length;
            _reference = ref Unsafe.Add(ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(array)), (nint)(uint)startOffset /* force zero-extension */);

            _lengths = lengths.ToArray();
            _strides = strides.ToArray();
        }

        /// <summary>
        /// Creates a new <see cref="TensorSpan{T}"/> over the provided <see cref="Array"/> using the specified start offsets, lengths, and strides.
        /// If the strides are not provided, they will be automatically calculated.
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <param name="startIndex">The starting offset for each dimension.</param>
        /// <param name="lengths">The lengths of each dimension.</param>
        /// <param name="strides">The strides for each dimension. Will be automatically calculated if not provided.</param>
        public TensorSpan(Array? array, scoped ReadOnlySpan<NIndex> startIndex, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);
            strides = strides.IsEmpty ? (ReadOnlySpan<nint>)TensorSpanHelpers.CalculateStrides(lengths) : strides;
            nint start = TensorSpanHelpers.ComputeLinearIndex(startIndex, strides, lengths);
            if (array == null)
            {
                if (!startIndex.IsEmpty || linearLength != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                this = default;
                return; // returns default
            }
            if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
                ThrowHelper.ThrowArrayTypeMismatchException();

            nint maxElements = TensorSpanHelpers.ComputeMaxElementCount(strides, lengths);
            if (Environment.Is64BitProcess)
            {
                // See comment in Span<T>.Slice for how this works.
                if ((ulong)(uint)start + (ulong)(uint)maxElements > (ulong)(uint)array.Length)
                    ThrowHelper.ThrowArgumentOutOfRangeException();
            }
            else
            {
                if ((uint)start > (uint)array.Length || (uint)maxElements > (uint)(array.Length - start))
                    ThrowHelper.ThrowArgumentOutOfRangeException();
            }

            _flattenedLength = linearLength;
            _memoryLength = array.Length;
            _reference = ref Unsafe.Add(ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(array)), (nint)(uint)start /* force zero-extension */);

            _lengths = lengths.ToArray();
            _strides = strides.ToArray();
        }

        /// <summary>
        /// Creates a new span over the target unmanaged buffer.  Clearly this
        /// is quite dangerous the length is not checked.
        /// But if this creation is correct, then all subsequent uses are correct.
        /// </summary>
        /// <param name="data">An unmanaged data to memory.</param>
        /// <param name="dataLength">The number of elements the unmanaged memory can hold.</param>
        [CLSCompliant(false)]
        public unsafe TensorSpan(T* data, nint dataLength) : this(data, dataLength, [dataLength], []) { }

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
        public unsafe TensorSpan(T* data, nint dataLength, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides)
        {
            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));

            strides = strides.IsEmpty ? (ReadOnlySpan<nint>)TensorSpanHelpers.CalculateStrides(lengths) : strides;
            nint maxElements = TensorSpanHelpers.ComputeMaxElementCount(strides, lengths);
            if (maxElements > dataLength)
                ThrowHelper.ThrowArgument_InvalidStridesAndLengths();
            _flattenedLength = linearLength;
            _memoryLength = dataLength;
            _reference = ref *data;

            _lengths = lengths.ToArray();
            _strides = strides.ToArray();
        }

        // Constructor for internal use only. It is not safe to expose publicly.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal TensorSpan(ref T reference, scoped ReadOnlySpan<nint> lengths, scoped ReadOnlySpan<nint> strides, nint memoryLength)
        {
            nint linearLength = TensorSpanHelpers.CalculateTotalLength(lengths);

            _flattenedLength = linearLength;
            _memoryLength = memoryLength;
            _reference = ref reference;

            _lengths = lengths.ToArray();
            _strides = strides.ToArray();
        }

        /// <summary>
        /// Returns a reference to specified element of the TensorSpan.
        /// </summary>
        /// <param name="indexes"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when any index is less than 0 or any index is greater than or equal to FlattenedLength
        /// </exception>
        public ref T this[params scoped ReadOnlySpan<nint> indexes]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (indexes.Length != Rank)
                    ThrowHelper.ThrowIndexOutOfRangeException();

                nint index = TensorSpanHelpers.ComputeLinearIndex(indexes, Strides, Lengths);
                if (index >= _memoryLength || index < 0)
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
        /// Thrown when any index is less than 0 or any index is greater than or equal to FlattenedLength
        /// </exception>
        public ref T this[params scoped ReadOnlySpan<NIndex> indexes]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {

                if (indexes.Length != Rank)
                    ThrowHelper.ThrowIndexOutOfRangeException();

                nint index = TensorSpanHelpers.ComputeLinearIndex(indexes, Strides, Lengths);
                if (index >= _memoryLength || index < 0)
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
        /// Thrown when any index is less than 0 or any index is greater than or equal to FlattenedLength
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
        public nint FlattenedLength => _flattenedLength;

        /// <summary>
        /// Gets a value indicating whether this <see cref="TensorSpan{T}"/> is empty.
        /// </summary>
        /// <value><see langword="true"/> if this span is empty; otherwise, <see langword="false"/>.</value>
        public bool IsEmpty => _flattenedLength == 0;

        /// <summary>
        /// Gets the length of each dimension in this <see cref="TensorSpan{T}"/>.
        /// </summary>
        [UnscopedRef]
        public ReadOnlySpan<nint> Lengths => _lengths;

        /// <summary>
        /// Gets the rank, aka the number of dimensions, of this <see cref="TensorSpan{T}"/>.
        /// </summary>
        public int Rank => Lengths.Length;

        /// <summary>
        /// Gets the strides of this <see cref="TensorSpan{T}"/>
        /// </summary>
        [UnscopedRef]
        public ReadOnlySpan<nint> Strides => _strides;

        /// <summary>
        /// Returns false if left and right point at the same memory and have the same length.  Note that
        /// this does *not* check to see if the *contents* are equal.
        /// </summary>
        public static bool operator !=(TensorSpan<T> left, TensorSpan<T> right) => !(left == right);

        /// <summary>
        /// Returns true if left and right point at the same memory and have the same length.  Note that
        /// this does *not* check to see if the *contents* are equal.
        /// </summary>
        public static bool operator ==(TensorSpan<T> left, TensorSpan<T> right) =>
            left._flattenedLength == right._flattenedLength &&
            left.Rank == right.Rank &&
            left._lengths.SequenceEqual(right._lengths) &&
            Unsafe.AreSame(ref left._reference, ref right._reference);

        /// <summary>
        /// This method is not supported as spans cannot be boxed. To compare two spans, use operator==.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// Always thrown by this method.
        /// </exception>
        [Obsolete("Equals() on TensorSpan will always throw an exception. Use the equality operator instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object? obj) =>
            throw new NotSupportedException(SR.NotSupported_CannotCallEqualsOnSpan);

        /// <summary>
        /// This method is not supported as spans cannot be boxed.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// Always thrown by this method.
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
            if (_flattenedLength != 0) ret = ref _reference;
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
            if (Rank > 6)
            {
                curIndexesArray = ArrayPool<nint>.Shared.Rent(Rank);
                curIndexes = curIndexesArray;
            }
            else
            {
                curIndexesArray = null;
                curIndexes = stackalloc nint[Rank];
            }

            nint clearedValues = 0;
            while (clearedValues < _flattenedLength)
            {
                TensorSpanHelpers.Clear(ref Unsafe.Add(ref _reference, TensorSpanHelpers.ComputeLinearIndex(curIndexes, Strides, Lengths)), (nuint)Lengths[Rank - 1]);
                TensorSpanHelpers.AdjustIndexes(Rank - 2, 1, curIndexes, _lengths);
                clearedValues += Lengths[Rank - 1];
            }
            Debug.Assert(clearedValues == _flattenedLength, "Didn't clear the right amount");

            if (curIndexesArray != null)
                ArrayPool<nint>.Shared.Return(curIndexesArray);
        }

        /// <summary>
        /// Fills the contents of this span with the given value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(T value)
        {
            Span<nint> curIndexes = stackalloc nint[Rank];
            nint filledValues = 0;
            // REVIEW: If we track the actual length of the backing data, because FlattenedLength doesn't always equal the actual length, we could use that here to not need to loop.
            while (filledValues < _flattenedLength)
            {
                TensorSpanHelpers.Fill(ref Unsafe.Add(ref _reference, TensorSpanHelpers.ComputeLinearIndex(curIndexes, Strides, Lengths)), (nuint)Lengths[Rank - 1], value);
                TensorSpanHelpers.AdjustIndexes(Rank - 2, 1, curIndexes, _lengths);
                filledValues += Lengths[Rank - 1];
            }

            Debug.Assert(filledValues == _flattenedLength, "Didn't copy the right amount to the array.");

        }

        /// <summary>
        /// Copies the contents of this span into destination span. If the source
        /// and destinations overlap, this method behaves as if the original values in
        /// a temporary location before the destination is overwritten.
        /// </summary>
        /// <param name="destination">The span to copy items into.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the destination TensorSpan is shorter than the source Span.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(scoped TensorSpan<T> destination)
        {
            // Using "if (!TryCopyTo(...))" results in two branches: one for the length
            // check, and one for the result of TryCopyTo. Since these checks are equivalent,
            // we can optimize by performing the check once ourselves then calling Memmove directly.
            if (_flattenedLength > destination.FlattenedLength)
                ThrowHelper.ThrowArgumentException_DestinationTooShort();

            scoped Span<nint> curIndexes;
            nint[]? curIndexesArray;
            if (Rank > 6)
            {
                curIndexesArray = ArrayPool<nint>.Shared.Rent(Rank);
                curIndexes = curIndexesArray;
            }
            else
            {
                curIndexesArray = null;
                curIndexes = stackalloc nint[Rank];
            }

            nint copiedValues = 0;
            TensorSpan<T> slice = destination.Slice(_lengths);
            while (copiedValues < _flattenedLength)
            {
                TensorSpanHelpers.Memmove(ref Unsafe.Add(ref slice._reference, TensorSpanHelpers.ComputeLinearIndex(curIndexes, destination.Strides, Lengths)), ref Unsafe.Add(ref _reference, TensorSpanHelpers.ComputeLinearIndex(curIndexes, Strides, Lengths)), Lengths[Rank - 1]);
                TensorSpanHelpers.AdjustIndexes(Rank - 2, 1, curIndexes, _lengths);
                copiedValues += Lengths[Rank - 1];
            }
            Debug.Assert(copiedValues == _flattenedLength, "Didn't copy the right amount to the array.");

            if (curIndexesArray != null)
                ArrayPool<nint>.Shared.Return(curIndexesArray);

        }

        /// <summary>
        /// Copies the contents of this span into destination span. If the source
        /// and destinations overlap, this method behaves as if the original values in
        /// a temporary location before the destination is overwritten.
        /// </summary>
        /// <param name="destination">The span to copy items into.</param>
        /// <returns>If the destination span is shorter than the source span, this method
        /// return false and no data is written to the destination.</returns>
        public bool TryCopyTo(scoped TensorSpan<T> destination)
        {
            bool retVal = false;

            if (_flattenedLength <= destination.FlattenedLength)
            {
                scoped Span<nint> curIndexes;
                nint[]? curIndexesArray;
                if (Rank > 6)
                {
                    curIndexesArray = ArrayPool<nint>.Shared.Rent(Rank);
                    curIndexes = curIndexesArray;
                }
                else
                {
                    curIndexesArray = null;
                    curIndexes = stackalloc nint[Rank];
                }

                nint copiedValues = 0;
                TensorSpan<T> slice = destination.Slice(_lengths);
                while (copiedValues < _flattenedLength)
                {
                    TensorSpanHelpers.Memmove(ref Unsafe.Add(ref slice._reference, TensorSpanHelpers.ComputeLinearIndex(curIndexes, Strides, Lengths)), ref Unsafe.Add(ref _reference, TensorSpanHelpers.ComputeLinearIndex(curIndexes, Strides, Lengths)), Lengths[Rank - 1]);
                    TensorSpanHelpers.AdjustIndexes(Rank - 2, 1, curIndexes, _lengths);
                    copiedValues += Lengths[Rank - 1];
                }
                retVal = true;
                Debug.Assert(copiedValues == _flattenedLength, "Didn't copy the right amount to the array.");

                if (curIndexesArray != null)
                    ArrayPool<nint>.Shared.Return(curIndexesArray);
            }
            return retVal;
        }

        //public static explicit operator TensorSpan<T>(Array? array);
        public static implicit operator TensorSpan<T>(T[]? array) => new TensorSpan<T>(array);

        /// <summary>
        /// Defines an implicit conversion of a <see cref="TensorSpan{T}"/> to a <see cref="ReadOnlyTensorSpan{T}"/>
        /// </summary>
        public static implicit operator ReadOnlyTensorSpan<T>(TensorSpan<T> span) =>
            new ReadOnlyTensorSpan<T>(ref span._reference, span._lengths, span._strides, span._memoryLength);

        /// <summary>
        /// For <see cref="Span{Char}"/>, returns a new instance of string that represents the characters pointed to by the span.
        /// Otherwise, returns a <see cref="string"/> with the name of the type and the number of elements.
        /// </summary>
        public override string ToString() => $"System.Numerics.Tensors.TensorSpan<{typeof(T).Name}>[{_flattenedLength}]";

        /// <summary>
        /// Returns a reference to specified element of the TensorSpan.
        /// </summary>
        /// <param name="indexes">The indexes for the slice.</param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when any index is less than 0 or any index is greater than or equal to FlattenedLength
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
        /// Takes in the lengths of the dimensions and slices according to them.
        /// </summary>
        /// <param name="lengths">The dimension lengths</param>
        /// <returns>A <see cref="ReadOnlyTensorSpan{T}"/> based on the provided <paramref name="lengths"/></returns>
        internal TensorSpan<T> Slice(params scoped ReadOnlySpan<nint> lengths)
        {
            NRange[] ranges = new NRange[lengths.Length];
            for(int i = 0; i < lengths.Length; i++)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TensorSpan<T> Slice(params scoped ReadOnlySpan<NRange> ranges)
        {
            if (ranges.Length != Lengths.Length)
                ThrowHelper.ThrowIndexOutOfRangeException();

            nint[] lengths = new nint[ranges.Length];
            nint[] offsets = new nint[ranges.Length];

            for (int i = 0; i < ranges.Length; i++)
            {
                (offsets[i], lengths[i]) = ranges[i].GetOffsetAndLength(Lengths[i]);
            }

            nint index = 0;
            for (int i = 0; i < offsets.Length; i++)
            {
                if (offsets[i] < 0 || offsets[i] >= Lengths[i])
                    ThrowHelper.ThrowIndexOutOfRangeException();

                index += Strides[i] * (offsets[i]);
            }

            if (index >= _memoryLength || index < 0)
                ThrowHelper.ThrowIndexOutOfRangeException();

            return new TensorSpan<T>(ref Unsafe.Add(ref _reference, index), lengths, _strides, _memoryLength - index);
        }

        /// <summary>
        /// Flattens the contents of this span into the provided <see cref="Span{T}"/>.
        /// </summary>
        /// <param name="destination">The span to copy items into.</param>
        public bool TryFlattenTo(scoped Span<T> destination)
        {
            bool retVal = false;
            if (destination.Length < _flattenedLength)
            {
                scoped Span<nint> curIndexes;
                nint[]? curIndexesArray;
                if (Rank > 6)
                {
                    curIndexesArray = ArrayPool<nint>.Shared.Rent(Rank);
                    curIndexes = curIndexesArray;
                }
                else
                {
                    curIndexesArray = null;
                    curIndexes = stackalloc nint[Rank];
                }

                nint copiedValues = 0;
                while (copiedValues < _flattenedLength)
                {
                    TensorSpanHelpers.Memmove(destination.Slice(checked((int)copiedValues)), ref Unsafe.Add(ref _reference, TensorSpanHelpers.ComputeLinearIndex(curIndexes, Strides, Lengths)), Lengths[Rank - 1]);
                    TensorSpanHelpers.AdjustIndexes(Rank - 2, 1, curIndexes, _lengths);
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
            if (destination.Length < _flattenedLength)
                ThrowHelper.ThrowArgumentException_DestinationTooShort();

            if (_flattenedLength == 0)
                return;

            scoped Span<nint> curIndexes;
            nint[]? curIndexesArray;
            if (Rank > 6)
            {
                curIndexesArray = ArrayPool<nint>.Shared.Rent(Rank);
                curIndexes = curIndexesArray;
            }
            else
            {
                curIndexesArray = null;
                curIndexes = stackalloc nint[Rank];
            }

            nint copiedValues = 0;
            while (copiedValues < _flattenedLength)
            {
                TensorSpanHelpers.Memmove(destination.Slice(checked((int)copiedValues)), ref Unsafe.Add(ref _reference, TensorSpanHelpers.ComputeLinearIndex(curIndexes, Strides, Lengths)), Lengths[Rank - 1]);
                TensorSpanHelpers.AdjustIndexes(Rank - 2, 1, curIndexes, _lengths);
                copiedValues += Lengths[Rank - 1];
            }

            if (curIndexesArray != null)
                ArrayPool<nint>.Shared.Return(curIndexesArray);
        }
    }
}
