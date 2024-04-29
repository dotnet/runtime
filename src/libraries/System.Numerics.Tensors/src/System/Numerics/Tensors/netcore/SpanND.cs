// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using System.Text;
using EditorBrowsableAttribute = System.ComponentModel.EditorBrowsableAttribute;
using EditorBrowsableState = System.ComponentModel.EditorBrowsableState;

#pragma warning disable 0809  //warning CS0809: Obsolete member 'SpanND<T>.Equals(object)' overrides non-obsolete member 'object.Equals(object)'
#pragma warning disable 8500 // address / sizeof of managed types

namespace System.Numerics.Tensors
{
    /// <summary>
    /// SpanND represents a contiguous region of arbitrary memory. Unlike arrays, it can point to either managed
    /// or native memory, or to memory allocated on the stack. It is type-safe and memory-safe.
    /// </summary>
    [DebuggerTypeProxy(typeof(SpanNDDebugView<>))]
    [DebuggerDisplay("{ToString(),raw}")]
    public readonly ref struct SpanND<T>
    {
        /// <summary>A byref or a native ptr.</summary>
        internal readonly ref T _reference;
        /// <summary>The number of elements this SpanND contains.</summary>
        internal readonly nint _linearLength;
        /// <summary>The lengths of each dimension.</summary>
        internal readonly ReadOnlySpan<nint> _lengths;
        /// <summary>The strides representing the memory offsets for each dimension.</summary>
        private readonly ReadOnlySpan<nint> _strides;
        /// <summary>If the backing memory is permanently pinned (so not just using a fixed statement).</summary>
        private readonly bool _isPinned = false;

        /// <summary>
        /// Creates a new span over the entirety of the target array.
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <param name="lengths">The lengths of the dimensions. If default is provided its assumed to have 1 dimension with a length equal to the length of the data.</param>
        /// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
        /// <exception cref="ArrayTypeMismatchException">Thrown when <paramref name="array"/> is covariant and array's type is not exactly T[].</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SpanND(T[]? array, ReadOnlySpan<nint> lengths)
        {
            if (array == null)
            {
                this = default;
                return; // returns default
            }
            if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
                ThrowHelper.ThrowArrayTypeMismatchException();
            _linearLength = SpanNDHelpers.CalculateTotalLength(lengths);
            if (_linearLength != array.Length)
                ThrowHelper.ThrowArgument_LengthsMustEqualArrayLength();

            _reference = ref MemoryMarshal.GetArrayDataReference(array);
            _lengths = lengths.ToArray();
            _strides = SpanNDHelpers.CalculateStrides(Rank, lengths);
        }

        /// <summary>
        /// Creates a new span over the entirety of the target array.
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <param name="lengths">The lengths of the dimensions. If default is provided its assumed to have 1 dimension with a length equal to the length of the data.</param>
        /// <param name="isPinned">If the underlying data is pinned.</param>
        /// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
        /// <exception cref="ArrayTypeMismatchException">Thrown when <paramref name="array"/> is covariant and array's type is not exactly T[].</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SpanND(T[]? array, ReadOnlySpan<nint> lengths, bool isPinned)
        {
            if (array == null)
            {
                this = default;
                return; // returns default
            }
            if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
                ThrowHelper.ThrowArrayTypeMismatchException();
            _linearLength = SpanNDHelpers.CalculateTotalLength(lengths);
            if (_linearLength != array.Length)
                ThrowHelper.ThrowArgument_LengthsMustEqualArrayLength();

            _reference = ref MemoryMarshal.GetArrayDataReference(array);
            _lengths = lengths.ToArray();
            _strides = SpanNDHelpers.CalculateStrides(Rank, lengths);
            _isPinned = IsPinned;
        }


        /// <summary>
        /// Creates a new span over the portion of the target array beginning
        /// at 'start' index and ending at 'end' index (exclusive).
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <param name="start">The index at which to begin the span.</param>
        /// <param name="lengths">The lengths of the dimensions. If default is provided its assumed to have 1 dimension with a length equal to the length of the data.</param>
        /// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
        /// <exception cref="ArrayTypeMismatchException">Thrown when <paramref name="array"/> is covariant and array's type is not exactly T[].</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in the range (&lt;0 or &gt;Length).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SpanND(T[]? array, nint start, ReadOnlySpan<nint> lengths)
        {
            _linearLength = SpanNDHelpers.CalculateTotalLength(lengths);
            if (array == null)
            {
                if (start != 0 || _linearLength != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                this = default;
                return; // returns default
            }
            if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
                ThrowHelper.ThrowArrayTypeMismatchException();
#if TARGET_64BIT
            // See comment in SpanND<T>.Slice for how this works.
            if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)array.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException();
#else
            if ((uint)start > (uint)array.Length || (uint)_linearLength > (uint)(array.Length - start))
                ThrowHelper.ThrowArgumentOutOfRangeException();
#endif

            _reference = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (nint)(uint)start /* force zero-extension */);

            _lengths = lengths.ToArray();
            _strides = SpanNDHelpers.CalculateStrides(Rank, lengths);
        }

        /// <summary>
        /// Creates a new span over the target unmanaged buffer.  Clearly this
        /// is quite dangerous, because we are creating arbitrarily typed T's
        /// out of a void*-typed block of memory.  And the length is not checked.
        /// But if this creation is correct, then all subsequent uses are correct.
        /// </summary>
        /// <param name="pointer">An unmanaged pointer to memory.</param>
        /// <param name="lengths">The lengths of the dimensions. If default is provided its assumed to have 1 dimension with a length equal to the length of the data.</param>
        /// <param name="isPinned">Whether the backing memory is permanently pinned or not.</param>
        /// <param name="strides">The lengths of the strides. If nothing is provided it figures out the default stride configuration.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <typeparamref name="T"/> is reference type or contains pointers and hence cannot be stored in unmanaged memory.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified length is negative.
        /// </exception>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe SpanND(void* pointer, ReadOnlySpan<nint> lengths, bool isPinned, ReadOnlySpan<nint> strides = default)
        {
            _linearLength = SpanNDHelpers.CalculateTotalLength(lengths);
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
            if (_linearLength < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException();

            _isPinned = isPinned;

            _reference = ref *(T*)pointer;

            _lengths = lengths.ToArray();
            _strides = strides.ToArray();
            if (strides == ReadOnlySpan<nint>.Empty)
                _strides = SpanNDHelpers.CalculateStrides(Rank, lengths);
        }

        // Constructor for internal use only. It is not safe to expose publicly, and is instead exposed via the unsafe MemoryMarshal.CreateSpan.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal SpanND(ref T reference, ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides, bool isPinned)
        {
            _linearLength = SpanNDHelpers.CalculateTotalLength(lengths);
            Debug.Assert(_linearLength >= 0);

            _reference = ref reference;

            _lengths = lengths.ToArray();
            _strides = strides.ToArray();
            _isPinned = isPinned;
        }

        /// <summary>
        /// Returns a reference to specified element of the Span.
        /// </summary>
        /// <param name="indices"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when index less than 0 or index greater than or equal to Length
        /// </exception>
        public ref T this[params ReadOnlySpan<nint> indices]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (indices.Length != Rank)
                    ThrowHelper.ThrowIndexOutOfRangeException();

                var index = SpanNDHelpers.GetIndex(indices, Strides, Shape);
                return ref Unsafe.Add(ref _reference, index /* force zero-extension */);
            }
        }

        // REVIEW: DO WE WANT TO TRY AND PUSH FOR THIS? OR JUST KEEP SETSLICE METHOD?
        // THIS WOULD BE TO ALLOW BEHAVIOR LIKE THIS:
        //      This modifies the first row of t0.
        //      t0[0..1] = (t1 * 1.5f) / (t1 - 0.25f);
        //   POTENTIALLY CAUSES ISSUES LIKE THIS WHERE BEHAVIOR WOULD BE DIFFERENT:
        //      tmp = t0[0..1]
        //      tmp = t1
        //      vs
        //      t0[0..1] = t1
        public SpanND<T> this[params ReadOnlySpan<NativeRange> indices]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (indices.Length != Rank)
                    ThrowHelper.ThrowIndexOutOfRangeException();

                return Slice(indices);
            }
            set
            {
                value.CopyTo(this[indices]);
            }
        }

        /// <summary>
        /// The number of items in the span.
        /// </summary>
        // REVIEW: Calling this just Length for now based on our discussions. Can rename if we desire.
        public nint Length => _linearLength;

        /// <summary>
        /// Gets a value indicating whether this <see cref="SpanND{T}"/> is empty.
        /// </summary>
        /// <value><see langword="true"/> if this span is empty; otherwise, <see langword="false"/>.</value>
        public bool IsEmpty => _linearLength == 0;

        /// <summary>
        /// Gets a value indicating whether this <see cref="SpanND{T}"/> is pinned.
        /// </summary>
        /// <value><see langword="true"/> if this span is pinned; otherwise, <see langword="false"/>.</value>
        public bool IsPinned => _isPinned;

        // REIVEW: Calling this Shape for now as Lengths didn't seem to be the most liked option based on our discussion. Can rename if we desire.
        /// <summary>
        /// Gets the length of each dimension in this <see cref="SpanND{T}"/>.
        /// </summary>
        public ReadOnlySpan<nint> Shape => _lengths;

        /// <summary>
        /// Gets the rank, aka the number of dimensions, of this <see cref="SpanND{T}"/>.
        /// </summary>
        public int Rank => Shape.Length;

        /// <summary>
        /// Gets the strides of this <see cref="SpanND{T}"/>
        /// </summary>
        public ReadOnlySpan<nint> Strides => _strides;

        /// <summary>
        /// Returns false if left and right point at the same memory and have the same length.  Note that
        /// this does *not* check to see if the *contents* are equal.
        /// </summary>
        public static bool operator !=(SpanND<T> left, SpanND<T> right) => !(left == right);

        /// <summary>
        /// This method is not supported as spans cannot be boxed. To compare two spans, use operator==.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// Always thrown by this method.
        /// </exception>
        [Obsolete("Equals() on SpanND will always throw an exception. Use the equality operator instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object? obj) =>
            throw new NotSupportedException(SR.NotSupported_CannotCallEqualsOnSpan);

        /// <summary>
        /// This method is not supported as spans cannot be boxed.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// Always thrown by this method.
        /// </exception>
        [Obsolete("GetHashCode() on SpanND will always throw an exception.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() =>
            throw new NotSupportedException(SR.NotSupported_CannotCallGetHashCodeOnSpan);

        /// <summary>
        /// Returns an empty <see cref="Span{T}"/>
        /// </summary>
        public static SpanND<T> Empty => default;

        /// <summary>Gets an enumerator for this span.</summary>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>Enumerates the elements of a <see cref="SpanND{T}"/>.</summary>
        public ref struct Enumerator
        {
            /// <summary>The span being enumerated.</summary>
            private readonly SpanND<T> _span;
            /// <summary>The current index that the enumerator is on.</summary>
            private Span<nint> _curIndices;
            /// <summary>The total item count.</summary>
            private nint _items;

            /// <summary>Initialize the enumerator.</summary>
            /// <param name="span">The span to enumerate.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(SpanND<T> span)
            {
                _span = span;
                _items = -1;
                _curIndices = new nint[_span.Rank];
                _curIndices[_span.Rank - 1] = -1;
            }

            /// <summary>Advances the enumerator to the next element of the span.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                SpanNDHelpers.AdjustIndices(_span.Rank - 1, 1, ref _curIndices, _span.Shape);

                if (_items < _span.Length)
                    _items++;

                return _items < _span.Length;
            }

            /// <summary>Gets the element at the current position of the enumerator.</summary>
            public ref T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _span[_curIndices];
            }
        }

        /// <summary>
        /// Returns a reference to the 0th element of the Span. If the SpanND is empty, returns null reference.
        /// It can be used for pinning and is required to support the use of span within a fixed statement.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref T GetPinnableReference()
        {
            // Ensure that the native code has just one forward branch that is predicted-not-taken.
            ref T ret = ref Unsafe.NullRef<T>();
            if (_linearLength != 0) ret = ref _reference;
            return ref ret;
        }

        /// <summary>
        /// Clears the contents of this span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Clear()
        {
            Span<nint> curIndices = stackalloc nint[Rank];
            nint clearedValues = 0;
            while (clearedValues < _linearLength)
            {
                SpanNDHelpers.Clear(ref Unsafe.Add(ref _reference, SpanNDHelpers.GetIndex(curIndices, Strides, Shape)), (nuint)Shape[Rank - 1]);
                SpanNDHelpers.AdjustIndices(Rank - 2, 1, ref curIndices, _lengths);
                clearedValues += Shape[Rank - 1];
            }
            Debug.Assert(clearedValues == _linearLength, "Didn't clear the right amount");
        }

        /// <summary>
        /// Fills the contents of this span with the given value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(T value)
        {
            Span<nint> curIndices = stackalloc nint[Rank];
            nint filledValues = 0;
            // REVIEW: If we track the actual length of the backing data, because Length doesn't always equal the actual length, we could use that here to not need to loop.
            while (filledValues < _linearLength)
            {
                SpanNDHelpers.Fill(ref Unsafe.Add(ref _reference, SpanNDHelpers.GetIndex(curIndices, Strides, Shape)), (nuint)Shape[Rank - 1], value);
                SpanNDHelpers.AdjustIndices(Rank - 2, 1, ref curIndices, _lengths);
                filledValues += Shape[Rank - 1];
            }

            Debug.Assert(filledValues == _linearLength, "Didn't copy the right amount to the array.");

        }

        /// <summary>
        /// Copies the contents of this span into destination span. If the source
        /// and destinations overlap, this method behaves as if the original values in
        /// a temporary location before the destination is overwritten.
        /// </summary>
        /// <param name="destination">The span to copy items into.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the destination SpanND is shorter than the source Span.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(SpanND<T> destination)
        {
            // Using "if (!TryCopyTo(...))" results in two branches: one for the length
            // check, and one for the result of TryCopyTo. Since these checks are equivalent,
            // we can optimize by performing the check once ourselves then calling Memmove directly.
            if (_linearLength <= destination.Length)
            {
                Span<nint> curIndices = stackalloc nint[Rank];
                nint copiedValues = 0;
                var slice = destination.Slice(_lengths);
                while (copiedValues < _linearLength)
                {
                    SpanNDHelpers.Memmove(ref Unsafe.Add(ref slice._reference, SpanNDHelpers.GetIndex(curIndices, destination.Strides, Shape)), ref Unsafe.Add(ref _reference, SpanNDHelpers.GetIndex(curIndices, Strides, Shape)), Shape[Rank - 1]);
                    SpanNDHelpers.AdjustIndices(Rank - 2, 1, ref curIndices, _lengths);
                    copiedValues += Shape[Rank - 1];
                }
                Debug.Assert(copiedValues == _linearLength, "Didn't copy the right amount to the array.");
            }
            else
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }
        }

        /// <summary>
        /// Copies the contents of this span into destination span. If the source
        /// and destinations overlap, this method behaves as if the original values in
        /// a temporary location before the destination is overwritten.
        /// </summary>
        /// <param name="destination">The span to copy items into.</param>
        /// <returns>If the destination span is shorter than the source span, this method
        /// return false and no data is written to the destination.</returns>
        public bool TryCopyTo(SpanND<T> destination)
        {
            bool retVal = false;

            if (_linearLength <= destination.Length)
            {
                Span<nint> curIndices = stackalloc nint[Rank];
                nint copiedValues = 0;
                var slice = destination.Slice(_lengths);
                while (copiedValues < _linearLength)
                {
                    SpanNDHelpers.Memmove(ref Unsafe.Add(ref slice._reference, SpanNDHelpers.GetIndex(curIndices, Strides, Shape)), ref Unsafe.Add(ref _reference, SpanNDHelpers.GetIndex(curIndices, Strides, Shape)), Shape[Rank - 1]);
                    SpanNDHelpers.AdjustIndices(Rank - 2, 1, ref curIndices, _lengths);
                    copiedValues += Shape[Rank - 1];
                }
                retVal = true;
                Debug.Assert(copiedValues == _linearLength, "Didn't copy the right amount to the array.");
            }
            return retVal;
        }

        /// <summary>
        /// Returns true if left and right point at the same memory and have the same length.  Note that
        /// this does *not* check to see if the *contents* are equal.
        /// </summary>
        public static bool operator ==(SpanND<T> left, SpanND<T> right) =>
            left._linearLength == right._linearLength &&
            left.Rank == right.Rank &&
            left._lengths == right._lengths &&
            Unsafe.AreSame(ref left._reference, ref right._reference);

        /// <summary>
        /// Defines an implicit conversion of a <see cref="SpanND{T}"/> to a <see cref="ReadOnlySpanND{T}"/>
        /// </summary>
        public static implicit operator ReadOnlySpanND<T>(SpanND<T> span) =>
            new ReadOnlySpanND<T>(ref span._reference, span._lengths, span._strides, span.IsPinned);

        /// <summary>
        /// For <see cref="Span{Char}"/>, returns a new instance of string that represents the characters pointed to by the span.
        /// Otherwise, returns a <see cref="string"/> with the name of the type and the number of elements.
        /// </summary>
        public override string ToString() => $"System.Numerics.Tensors.SpanND<{typeof(T).Name}>[{_linearLength}]";

        /// <summary>
        /// Takes in the lengths of the dimensions and slices according to them.
        /// </summary>
        /// <param name="lengths">The dimension lengths</param>
        /// <returns>A <see cref="ReadOnlySpanND{T}"/> based on the provided <paramref name="lengths"/></returns>
        internal SpanND<T> Slice(ReadOnlySpan<nint> lengths)
        {
            var ranges = new NativeRange[lengths.Length];
            for(int i = 0; i < lengths.Length; i++)
            {
                ranges[i] = new NativeRange(0, lengths[i]);
            }
            return Slice(ranges);
        }

        /// <summary>
        /// Forms a slice out of the given span
        /// </summary>
        /// <param name="ranges">The ranges for the slice</param>
        /// <returns>A <see cref="ReadOnlySpanND{T}"/> based on the provided <paramref name="ranges"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SpanND<T> Slice(params ReadOnlySpan<NativeRange> ranges)
        {
            if (ranges.Length != Shape.Length)
                throw new ArgumentOutOfRangeException(nameof(ranges), "Number of dimensions to slice does not equal the number of dimensions in the span");

            var lengths = new nint[ranges.Length];
            var offsets = new nint[ranges.Length];

            for (var i = 0; i < ranges.Length; i++)
            {
                (offsets[i], lengths[i]) = ranges[i].GetOffsetAndLength(Shape[i]);
            }

            nint index = 0;
            for (int i = 0; i < offsets.Length; i++)
            {
                index += Strides[i] * (offsets[i]);
            }

            return new SpanND<T>(ref Unsafe.Add(ref _reference, index), lengths, _strides, _isPinned);
        }

        /// <summary>
        /// Copies the contents of this span into a new array.  This heap
        /// allocates, so should generally be avoided, however it is sometimes
        /// necessary to bridge the gap with APIs written in terms of arrays.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToArray()
        {
            if (_linearLength == 0)
                return Array.Empty<T>();

            var destination = new T[_linearLength];
            ref T dstRef = ref MemoryMarshal.GetArrayDataReference(destination);

            Span<nint> curIndices = stackalloc nint[Rank];
            nint copiedValues = 0;
            while (copiedValues < _linearLength)
            {
                SpanNDHelpers.Memmove(ref Unsafe.Add(ref dstRef, copiedValues), ref Unsafe.Add(ref _reference, SpanNDHelpers.GetIndex(curIndices, Strides, Shape)), Shape[Rank - 1]);
                SpanNDHelpers.AdjustIndices(Rank - 2, 1, ref curIndices, _lengths);
                copiedValues += Shape[Rank - 1];
            }

            return destination;
        }
    }
}
