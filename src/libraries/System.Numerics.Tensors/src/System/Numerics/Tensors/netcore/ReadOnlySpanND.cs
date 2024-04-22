// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;

using EditorBrowsableAttribute = System.ComponentModel.EditorBrowsableAttribute;
using EditorBrowsableState = System.ComponentModel.EditorBrowsableState;

#pragma warning disable 0809  //warning CS0809: Obsolete member 'Span<T>.Equals(object)' overrides non-obsolete member 'object.Equals(object)'

namespace System.Numerics.Tensors
{
    /// <summary>
    /// ReadOnlySpanND represents a contiguous region of arbitrary memory. Unlike arrays, it can point to either managed
    /// or native memory, or to memory allocated on the stack. It is type-safe and memory-safe.
    /// </summary>
    [DebuggerTypeProxy(typeof(SpanNDDebugView<>))]
    [DebuggerDisplay("{ToString(),raw}")]
#pragma warning disable SYSLIB1056 // Specified native type is invalid
    //[NativeMarshalling(typeof(ReadOnlySpanMarshaller<,>))]
#pragma warning restore SYSLIB1056 // Specified native type is invalid
    public readonly ref struct ReadOnlySpanND<T>
    {
        /// <summary>A byref or a native ptr.</summary>
        internal readonly ref T _reference;
        /// <summary>The number of elements this SpanND contains.</summary>
        private readonly nint _linearLength;
        /// <summary>The lengths of each dimension.</summary>
        private readonly ReadOnlySpan<nint> _lengths;
        private readonly ReadOnlySpan<nint> _strides;
        /// <summary>If the backing memory is permanently pinned (so not just using a fixed statement).</summary>
        private readonly bool _isPinned = false;

        /// <summary>
        /// Creates a new read-only span over the entirety of the target array.
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <param name="lengths"></param>
        /// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpanND(T[]? array, ReadOnlySpan<nint> lengths)
        {
            if (array == null)
            {
                this = default;
                return; // returns default
            }

            _linearLength = SpanHelpers.CalculateTotalLength(lengths);
            if (_linearLength != array.Length)
                ThrowHelper.ThrowArgument_LengthsMustEqualArrayLength();

            _reference = ref MemoryMarshal.GetArrayDataReference(array);
            _linearLength = array.Length;
            _lengths = lengths;
            _strides = SpanHelpers.CalculateStrides(Rank, lengths);
        }

        /// <summary>
        /// Creates a new read-only span over the portion of the target array beginning
        /// at 'start' index and ending at 'end' index (exclusive).
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <param name="start">The index at which to begin the read-only span.</param>
        /// <param name="lengths"></param>
        /// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in the range (&lt;0 or &gt;Length).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpanND(T[]? array, nint start, ReadOnlySpan<nint> lengths)
        {
            _linearLength = SpanHelpers.CalculateTotalLength(lengths);
            if (array == null)
            {
                if (start != 0 || _linearLength != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                this = default;
                return; // returns default
            }

#if TARGET_64BIT
            // See comment in SpanND<T>.Slice for how this works.
            if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)array.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException();
#else
            if ((uint)start > (uint)array.Length || (uint)_linearLength > (uint)(array.Length - start))
                ThrowHelper.ThrowArgumentOutOfRangeException();
#endif
            if (_linearLength != SpanHelpers.CalculateTotalLength(lengths))
                ThrowHelper.ThrowArgument_LengthsMustEqualArrayLength();

            _reference = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (nint)(uint)start /* force zero-extension */);

            _lengths = lengths;
            _strides = SpanHelpers.CalculateStrides(Rank, lengths);
        }

        /// <summary>
        /// Creates a new read-only span over the target unmanaged buffer.  Clearly this
        /// is quite dangerous, because we are creating arbitrarily typed T's
        /// out of a void*-typed block of memory.  And the length is not checked.
        /// But if this creation is correct, then all subsequent uses are correct.
        /// </summary>
        /// <param name="pointer">An unmanaged pointer to memory.</param>
        /// <param name="lengths"></param>
        /// <param name="isPinned"></param>
        /// <param name="strides"></param>
        /// <exception cref="ArgumentException">
        /// Thrown when <typeparamref name="T"/> is reference type or contains pointers and hence cannot be stored in unmanaged memory.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified length is negative.
        /// </exception>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ReadOnlySpanND(void* pointer, ReadOnlySpan<nint> lengths, bool isPinned, ReadOnlySpan<nint> strides = default)
        {
            _linearLength = SpanHelpers.CalculateTotalLength(lengths);

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
            if (_linearLength < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException();
            if (_linearLength != SpanHelpers.CalculateTotalLength(lengths))
                ThrowHelper.ThrowArgument_LengthsMustEqualArrayLength();

            _isPinned = isPinned;

            _reference = ref Unsafe.As<byte, T>(ref *(byte*)pointer);

            _lengths = lengths;
            _strides = strides;
            if (strides == ReadOnlySpan<nint>.Empty)
                _strides = SpanHelpers.CalculateStrides(Rank, lengths);
        }

        // Constructor for internal use only. It is not safe to expose publicly, and is instead exposed via the unsafe MemoryMarshal.CreateReadOnlySpan.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpanND(ref T reference, ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides, bool isPinned)
        {
            _linearLength = SpanHelpers.CalculateTotalLength(lengths);
            Debug.Assert(_linearLength >= 0);

            _reference = ref reference;

            _lengths = lengths;
            _strides = strides;
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
        public ref readonly T this[params nint[] indices] => ref this[indices.AsSpan()];

        // REVIEW: WHEN IS params ReadOnlySpan<T> GOING TO BE SUPPORTED.
        /// <summary>
        /// Returns a reference to specified element of the Span.
        /// </summary>
        /// <param name="indices"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when index less than 0 or index greater than or equal to Length
        /// </exception>
        public ref readonly T this[ReadOnlySpan<nint> indices]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (indices.Length != Rank)
                    ThrowHelper.ThrowIndexOutOfRangeException();

                var index = SpanHelpers.GetIndex(indices, Strides, Lengths);
                return ref Unsafe.Add(ref _reference, index /* force zero-extension */);
            }
        }

        /// <summary>
        /// The number of items in the span.
        /// </summary>
        internal nint LinearLength
        {
            get => _linearLength;
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="SpanND{T}"/> is empty.
        /// </summary>
        /// <value><see langword="true"/> if this span is empty; otherwise, <see langword="false"/>.</value>
        public bool IsEmpty
        {
            get => _linearLength == 0;
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="SpanND{T}"/> is pinned.
        /// </summary>
        /// <value><see langword="true"/> if this span is pinned; otherwise, <see langword="false"/>.</value>
        public bool IsPinned { get { return _isPinned; } }

        // REIVEW: SHOULD WE CALL THIS DIMS OR DIMENSIONS?
        /// <summary>
        /// Gets the length of each dimension in this <see cref="SpanND{T}"/>.
        /// </summary>
        public ReadOnlySpan<nint> Lengths { get { return _lengths; } }

        /// <summary>
        /// Gets the rank, aka the number of dimensions, of this <see cref="SpanND{T}"/>.
        /// </summary>
        public int Rank { get { return Lengths.Length; } }

        /// <summary>
        /// Gets the strides of this <see cref="SpanND{T}"/>
        /// </summary>
        public ReadOnlySpan<nint> Strides { get { return _strides; } }

        /// <summary>
        /// Returns false if left and right point at the same memory and have the same length.  Note that
        /// this does *not* check to see if the *contents* are equal.
        /// </summary>
        public static bool operator !=(ReadOnlySpanND<T> left, ReadOnlySpanND<T> right) => !(left == right);

        /// <summary>
        /// This method is not supported as spans cannot be boxed. To compare two spans, use operator==.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// Always thrown by this method.
        /// </exception>
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
        [Obsolete("Equals() on ReadOnlySpanND will always throw an exception. Use the equality operator instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object? obj) =>
            throw new NotSupportedException(SR.NotSupported_CannotCallEqualsOnSpan);

        /// <summary>
        /// This method is not supported as spans cannot be boxed.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// Always thrown by this method.
        /// </exception>
        [Obsolete("GetHashCode() on ReadOnlySpanND will always throw an exception.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() =>
            throw new NotSupportedException(SR.NotSupported_CannotCallGetHashCodeOnSpan);
#pragma warning restore CS0809

        /// <summary>
        /// Returns a 0-length read-only span whose base is the null pointer.
        /// </summary>
        public static ReadOnlySpanND<T> Empty => default;

        /// <summary>
        /// Casts a read-only span of <typeparamref name="TDerived"/> to a read-only span of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="TDerived">The element type of the source read-only span, which must be derived from <typeparamref name="T"/>.</typeparam>
        /// <param name="items">The source read-only span. No copy is made.</param>
        /// <returns>A read-only span with elements cast to the new type.</returns>
        /// <remarks>This method uses a covariant cast, producing a read-only span that shares the same memory as the source. The relationships expressed in the type constraints ensure that the cast is a safe operation.</remarks>
        public static ReadOnlySpanND<T> CastUp<TDerived>(ReadOnlySpanND<TDerived> items) where TDerived : class?, T
        {
            return new ReadOnlySpanND<T>(ref Unsafe.As<TDerived, T>(ref items._reference), items._lengths, items._strides, items._isPinned);
        }

        /// <summary>Gets an enumerator for this span.</summary>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>Enumerates the elements of a <see cref="ReadOnlySpan{T}"/>.</summary>
        public ref struct Enumerator
        {
            /// <summary>The span being enumerated.</summary>
            private readonly ReadOnlySpanND<T> _span;
            private readonly Span<nint> _curIndices;
            /// <summary>The total item count.</summary>
            private nint _items;

            /// <summary>Initialize the enumerator.</summary>
            /// <param name="span">The span to enumerate.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(ReadOnlySpanND<T> span)
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
                AdjustIndices(_span.Rank - 1, 1);

                _items++;
                return _items < _span.LinearLength;
            }

            private void AdjustIndices(int curIndex, nint addend)
            {
                if (addend == 0 || curIndex < 0)
                    return;
                _curIndices[curIndex] += addend;
                AdjustIndices(curIndex - 1, _curIndices[curIndex] / _span.Lengths[curIndex]);
                _curIndices[curIndex] = _curIndices[curIndex] % _span.Lengths[curIndex];
            }

            /// <summary>Gets the element at the current position of the enumerator.</summary>
            public ref readonly T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _span[_curIndices];
            }
        }

        /// <summary>
        /// Returns a reference to the 0th element of the Span. If the Span is empty, returns null reference.
        /// It can be used for pinning and is required to support the use of span within a fixed statement.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref readonly T GetPinnableReference()
        {
            // Ensure that the native code has just one forward branch that is predicted-not-taken.
            ref T ret = ref Unsafe.NullRef<T>();
            if (_linearLength != 0) ret = ref _reference;
            return ref ret;
        }

        /// <summary>
        /// Copies the contents of this read-only span into destination span. If the source
        /// and destinations overlap, this method behaves as if the original values in
        /// a temporary location before the destination is overwritten.
        /// </summary>
        /// <param name="destination">The span to copy items into.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the destination Span is shorter than the source Span.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(SpanND<T> destination)
        {
            // Using "if (!TryCopyTo(...))" results in two branches: one for the length
            // check, and one for the result of TryCopyTo. Since these checks are equivalent,
            // we can optimize by performing the check once ourselves then calling Memmove directly.

            var curIndices = new nint[Rank];
            nint copiedValues = 0;
            var slice = destination.Slice(_lengths);
            while (copiedValues < _linearLength)
            {
                SpanHelpers.Memmove(ref Unsafe.Add(ref slice._reference, SpanHelpers.GetIndex(curIndices, Strides, Lengths)), ref Unsafe.Add(ref _reference, SpanHelpers.GetIndex(curIndices, Strides, Lengths)), Lengths[Rank - 1]);
                SpanHelpers.AdjustIndices(Rank - 2, 1, ref curIndices, _lengths);
                copiedValues += Lengths[Rank - 1];
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
        public bool TryCopyTo(SpanND<T> destination)
        {
            bool retVal = false;

            try
            {
                var curIndices = new nint[Rank];
                nint copiedValues = 0;
                var slice = destination.Slice(_lengths);
                while (copiedValues < _linearLength)
                {
                    SpanHelpers.Memmove(ref Unsafe.Add(ref slice._reference, SpanHelpers.GetIndex(curIndices, Strides, Lengths)), ref Unsafe.Add(ref _reference, SpanHelpers.GetIndex(curIndices, Strides, Lengths)), Lengths[Rank - 1]);
                    SpanHelpers.AdjustIndices(Rank - 2, 1, ref curIndices, _lengths);
                    copiedValues += Lengths[Rank - 1];
                }
                retVal = true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return retVal;
            }
            return retVal;
        }

        /// <summary>
        /// Returns true if left and right point at the same memory and have the same length.  Note that
        /// this does *not* check to see if the *contents* are equal.
        /// </summary>
        public static bool operator ==(ReadOnlySpanND<T> left, ReadOnlySpanND<T> right) =>
            left._linearLength == right._linearLength &&
            left.Rank == right.Rank &&
            left._lengths == right._lengths &&
            Unsafe.AreSame(ref left._reference, ref right._reference);

        /// <summary>
        /// For <see cref="ReadOnlySpanND{Char}"/>, returns a new instance of string that represents the characters pointed to by the span.
        /// Otherwise, returns a <see cref="string"/> with the name of the type and the number of elements.
        /// </summary>
        public override string ToString()
        {
            if (typeof(T) == typeof(char))
            {
                // BUGBUG: FIX
                //return new string(new ReadOnlySpan<char>(ref Unsafe.As<T, char>(ref _reference), _length));
            }
            return $"System.Numerics.Tensors.ReadOnlySpanND<{typeof(T).Name}>[{_linearLength}]";
        }

        /// <summary>
        /// Takes in the lengths of the dimensions and slices according to them.
        /// </summary>
        /// <param name="lengths">The dimension lengths</param>
        /// <returns></returns>
        internal ReadOnlySpanND<T> Slice(ReadOnlySpan<nint> lengths)
        {
            var ranges = new NativeRange[lengths.Length];
            for (int i = 0; i < lengths.Length; i++)
            {
                ranges[i] = new NativeRange(0, lengths[i]);
            }
            return Slice(ranges);
        }

        /// <summary>
        /// Forms a slice out of the given span
        /// </summary>
        /// <param name="ranges">The ranges for the slice</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpanND<T> Slice(params NativeRange[] ranges)
        {
            if (ranges.Length != Lengths.Length)
                throw new ArgumentOutOfRangeException(nameof(ranges), "Number of dimensions to slice does not equal the number of dimensions in the span");

            var lengths = new nint[ranges.Length];
            var offsets = new nint[ranges.Length];

            for (var i = 0; i < ranges.Length; i++)
            {
                (offsets[i], lengths[i]) = ranges[i].GetOffsetAndLength(Lengths[i]);
            }

            nint index = 0;
            for (int i = 0; i < offsets.Length; i++)
            {
                index += Strides[i] * (offsets[i]);
            }

            return new ReadOnlySpanND<T>(ref Unsafe.Add(ref _reference, index), lengths.AsSpan(), _strides, _isPinned);
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

            var curIndices = new nint[Rank];
            nint copiedValues = 0;
            while (copiedValues < _linearLength)
            {
                SpanHelpers.Memmove(ref Unsafe.Add(ref dstRef, copiedValues), ref Unsafe.Add(ref _reference, SpanHelpers.GetIndex(curIndices, Strides, Lengths)), Lengths[Rank - 1]);
                SpanHelpers.AdjustIndices(Rank - 2, 1, ref curIndices, _lengths);
                copiedValues += Lengths[Rank - 1];
            }

            return destination;
        }
    }
}
