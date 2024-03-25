// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using static System.Runtime.InteropServices.JavaScript.JSType;

#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable 8500 // address / sizeof of managed types

namespace System.Numerics.Tensors
{
    public sealed class Tensor<T>
        : IDisposable,
          ITensor<Tensor<T>, T>
        where T : IEquatable<T>, IEqualityOperators<T, T, bool>
    {

        /// <summary>A byref or a native ptr.</summary>
        internal readonly IntPtr _reference;
        /// <summary>The number of elements this Tensor contains.</summary>
        private readonly nint _linearLength;
        /// <summary>The lengths of each dimension.</summary>
        private nint[] _lengths;
        private nint[] _strides;
        /// <summary>If the backing memory is permanently pinned (so not just using a fixed statement).</summary>
        private readonly bool _isPinned;

        /// <summary>
        /// Creates a new empty Tensor.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Tensor()
        {
            _linearLength = 0;

            _lengths = Array.Empty<nint>();
            _strides = Array.Empty<nint>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Tensor(IntPtr reference, bool mustPin, nint[] lengths)
        {
            _linearLength = SpanHelpers.CalculateTotalLength(ref lengths);

            Debug.Assert(_linearLength >= 0);

            _reference = reference;
            _lengths = lengths;
            _strides = SpanHelpers.CalculateStrides(Rank, lengths);
            _isPinned = mustPin;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Tensor(IntPtr reference, bool mustPin, nint[] lengths, nint[] strides)
        {
            _linearLength = SpanHelpers.CalculateTotalLength(ref lengths);

            Debug.Assert(_linearLength >= 0);

            _reference = reference;
            _lengths = lengths;
            _strides = strides;
            _isPinned = mustPin;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Tensor(IntPtr reference, nint[] lengths, bool isPinned)
        {
            _linearLength = SpanHelpers.CalculateTotalLength(ref lengths);

            Debug.Assert(_linearLength >= 0);

            _reference = reference;

            _lengths = lengths;
            _strides = SpanHelpers.CalculateStrides(Rank, _lengths);
            _isPinned = isPinned;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Tensor(IntPtr reference, nint[] lengths, nint[] strides, bool isPinned)
        {
            _linearLength = SpanHelpers.CalculateTotalLength(ref lengths);

            Debug.Assert(_linearLength >= 0);

            _reference = reference;

            _lengths = lengths;
            _strides = strides;
            _isPinned = isPinned;
        }

        // IDisposable

        public void Dispose()
        {
            //Marshal.FreeHGlobal(_reference);
        }

        private static class EmptyTensor
        {
#pragma warning disable CA1825 // this is the implementation of Tensor.Empty<T>()
            internal static readonly Tensor<T> Value = new Tensor<T>();
#pragma warning restore CA1825
        }
        // ITensor

        public static Tensor<T> Empty => EmptyTensor.Value;

        public bool IsEmpty { get { return _lengths.Length == 0; } }

        public bool IsPinned { get { return _isPinned; } }

        public int Rank { get { return _lengths.Length; } }

        /// <summary>
        /// The number of items in the span.
        /// </summary>
        internal nint LinearLength
        {
            //[Intrinsic]
            get => _linearLength;
        }
        // REIVEW: SHOULD WE CALL THIS DIMS OR DIMENSIONS?
        /// <summary>
        /// Gets the length of each dimension in this <see cref="Tensor{T}"/>.
        /// </summary>
        public ReadOnlySpan<nint> Lengths { get { return _lengths; } }
        public ReadOnlySpan<nint> Strides { get { return _strides; } }

        // REVIEW: WHEN IS params ReadOnlySpan<T> GOING TO BE SUPPORTED.
        /// <summary>
        /// Returns a reference to specified element of the Span.
        /// </summary>
        /// <param name="indices"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when index less than 0 or index greater than or equal to Length
        /// </exception>
        public ref T this[params nint[] indices] => ref this[indices.AsSpan()];

        // REVIEW: WHEN IS params ReadOnlySpan<T> GOING TO BE SUPPORTED.
        /// <summary>
        /// Returns a reference to specified element of the Span.
        /// </summary>
        /// <param name="indices"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when index less than 0 or index greater than or equal to Length
        /// </exception>
        public ref T this[ReadOnlySpan<nint> indices]
        {
            //[Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (indices.Length != Rank)
                    ThrowHelper.ThrowIndexOutOfRangeException();

                var index = SpanHelpers.GetIndex(indices, Strides, Lengths);
                unsafe
                {
                    T* ptr = (T*)_reference;
                    return ref ptr[index];
                }
            }
        }

        public static implicit operator SpanND<T>(Tensor<T> value)
        {
            unsafe
            {
                return new SpanND<T>(value._reference.ToPointer(), value._lengths, value._isPinned, value._strides);
            }
        }

        public static implicit operator ReadOnlySpanND<T>(Tensor<T> value)
        {
            unsafe
            {
                return new ReadOnlySpanND<T>(value._reference.ToPointer(), value._lengths, value._isPinned, value._strides);
            }
        }

        public SpanND<T> AsSpan(params NativeRange[] ranges) => Slice(ranges);
        public ReadOnlySpanND<T> AsReadOnlySpan(params NativeRange[] ranges) => Slice(ranges);

        /// <summary>
        /// Returns a reference to the 0th element of the Tensor. If the Tensor is empty, returns null reference.
        /// It can be used for pinning and is required to support the use of Tensor within a fixed statement.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref T GetPinnableReference()
        {
            // Ensure that the native code has just one forward branch that is predicted-not-taken.
            ref T ret = ref Unsafe.NullRef<T>();
            if (_linearLength != 0)
            {
                unsafe
                {
                    T* ptr = (T*)_reference;
                    return ref ptr[0];
                }
            }
            return ref ret;
        }

        /// <summary>
        /// Forms a slice out of the given tensor
        /// </summary>
        /// <param name="ranges">The ranges for the slice</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Tensor<T> Slice(params NativeRange[] ranges)
        {
            if (ranges.Length != Lengths.Length)
                throw new ArgumentOutOfRangeException(nameof(ranges), "Number of dimensions to slice does not equal the number of dimensions in the span");

            nint length = 1;
            var lengths = new nint[ranges.Length];
            var offsets = new nint[ranges.Length];

            for (var i = 0; i < ranges.Length; i++)
            {
                if (ranges[i].End > Lengths[i])
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                length *= (nint)(ranges[i].End - ranges[i].Start);
                lengths[i] = (nint)(ranges[i].End - ranges[i].Start);
                offsets[i] = (nint)ranges[i].Start;
            }

            nint index = 0;
            for (int i = 0; i < offsets.Length; i++)
            {
                index += Strides[i] * (offsets[i]);
            }
            unsafe
            {
                return new Tensor<T>(_reference + (index * sizeof(T)), lengths, _strides, _isPinned);
            }
        }

        /// <summary>
        /// Clears the contents of this tensor.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Clear()
        {
            var curIndices = new nint[Rank];
            nint copiedValues = 0;
            while (copiedValues < _linearLength)
            {
                unsafe
                {
                    NativeMemory.Clear((void*)(_reference + (SpanHelpers.GetIndex(curIndices, Strides, Lengths) * (nint)sizeof(T))), (nuint)Lengths[Rank - 1] * (nuint)sizeof(T));
                }
                SpanHelpers.AdjustIndices(Rank - 2, 1, ref curIndices, _lengths);
                copiedValues += Lengths[Rank - 1];
            }
        }

        /// <summary>
        /// Copies the contents of this tensor into destination span. If the source
        /// and destinations overlap, this method behaves as if the original values in
        /// a temporary location before the destination is overwritten.
        /// </summary>
        /// <param name="destination">The span to copy items into.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the destination SpanND is shorter than the source Tensor.
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
                unsafe
                {
                    fixed (T* dst = slice)
                    {
                        NativeMemory.Copy((void*)(_reference + (SpanHelpers.GetIndex(curIndices, Strides, Lengths) * (nint)sizeof(T))), (void*)(dst + SpanHelpers.GetIndex(curIndices, Strides, Lengths)), (nuint)Lengths[Rank - 1] * (nuint)sizeof(T));
                    }
                    SpanHelpers.AdjustIndices(Rank - 2, 1, ref curIndices, _lengths);
                    copiedValues += Lengths[Rank - 1];
                }
            }
        }

        /// <summary>
        /// Fills the contents of this span with the given value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(T value)
        {
            var curIndices = new nint[Rank];
            nint filledValues = 0;
            while (filledValues < _linearLength)
            {
                unsafe
                {
                    T* ptr = (T*)_reference.ToPointer();
                    SpanHelpers.Fill(ref *(ptr + SpanHelpers.GetIndex(curIndices, Strides, Lengths)), (nuint)Lengths[Rank - 1], value);
                }
                SpanHelpers.AdjustIndices(Rank - 2, 1, ref curIndices, _lengths);
                filledValues += Lengths[Rank - 1];
            }
        }

        /// <summary>
        /// Copies the contents of this tensor into destination span. If the source
        /// and destinations overlap, this method behaves as if the original values in
        /// a temporary location before the destination is overwritten.
        /// </summary>
        /// <param name="destination">The span to copy items into.</param>
        /// <returns>If the destination span is shorter than the source tensor, this method
        /// return false and no data is written to the destination.</returns>
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
                    unsafe
                    {
                        fixed (T* dst = slice)
                        {
                            NativeMemory.Copy((void*)(_reference + (SpanHelpers.GetIndex(curIndices, Strides, Lengths) * (nint)sizeof(T))), (void*)(dst + SpanHelpers.GetIndex(curIndices, Strides, Lengths)), (nuint)Lengths[Rank - 1] * (nuint)sizeof(T));
                        }
                    }
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

        // IEnumerable
        public IEnumerator<T> GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

        // IEquatable

        public bool Equals(Tensor<T>? other) => throw new NotImplementedException();

        // IEqualityOperators

        public static bool operator ==(Tensor<T>? left, Tensor<T>? right) => throw new NotImplementedException();
        public static bool operator ==(Tensor<T> left, T right) => throw new NotImplementedException();

        public static bool operator !=(Tensor<T>? left, Tensor<T>? right) => !(left == right);
        public static bool operator !=(Tensor<T> left, T right) => !(left == right);

        public sealed class Enumerator : IEnumerator<T>
        {
            /// <summary>The span being enumerated.</summary>
            private readonly Tensor<T> _tensor;
            private nint[] _curIndices;
            /// <summary>The total item count.</summary>
            private nint _items;

            /// <summary>Initialize the enumerator.</summary>
            /// <param name="tensor">The tensor to enumerate.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(Tensor<T> tensor)
            {
                _tensor = tensor;
                _items = -1;
                _curIndices = new nint[_tensor.Rank];
                _curIndices[_tensor.Rank - 1] = -1;
            }

            /// <summary>Advances the enumerator to the next element of the span.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                SpanHelpers.AdjustIndices(_tensor.Rank - 1, 1, ref _curIndices, _tensor.Lengths);

                _items++;
                return _items < _tensor.LinearLength;
            }

            public void Reset()
            {
                Array.Clear(_curIndices);
                _curIndices[_tensor.Rank - 1] = -1;
            }

            public void Dispose()
            {

            }

            T IEnumerator<T>.Current => _tensor[_curIndices];

            object IEnumerator.Current => _tensor[_curIndices];
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Tensor<T>);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        internal void Reshape(nint[] lengths)
        {
            var tempLinear = SpanHelpers.CalculateTotalLength(ref lengths);
            if (tempLinear != _linearLength)
                throw new ArgumentException("Provided dimensions are not valid for reshaping");
            _lengths = lengths;
            _strides = SpanHelpers.CalculateStrides(Rank, lengths);
        }

        internal void Squeeze(nint axis = -1)
        {
            List<nint> tempLengths = new List<nint>();
            if (axis == -1)
            {
                for (int i = 0; i < _lengths.Length; i++)
                {
                    if (_lengths[i] != 1)
                    {
                        tempLengths.Add(_lengths[i]);
                    }
                }
                _lengths = tempLengths.ToArray();
                _strides = SpanHelpers.CalculateStrides(Rank, Lengths);
            }
            else
            {
                if (_lengths[axis] != 1)
                {
                    throw new ArgumentException("Cannot select an axis to squeeze which has size not equal to one");
                }
                for (int i = 0; i < _lengths.Length; i++)
                {
                    if (i != axis)
                    {
                        tempLengths.Add(_lengths[i]);
                    }
                }
                _lengths = tempLengths.ToArray();
                _strides = SpanHelpers.CalculateStrides(Rank, Lengths);
            }
        }

        // REVIEW: NUMPY CALLS THIS EXPAND_DIMS
        internal void Unsqueeze(nint axis)
        {
            if (axis < 0 || axis > _lengths.Length)
                throw new ArgumentException("Cannot select an axis less than 0 or greater than the current Rank");
            List<nint> tempLengths = _lengths.ToList();
            tempLengths.Insert((int)axis, 1);
            _lengths = tempLengths.ToArray();
            _strides = SpanHelpers.CalculateStrides(Rank, Lengths);
        }
    }

    public static partial class Tensor
    {
        public static Tensor<T> Create<T>(bool mustPin, ReadOnlySpan<nint> lengths)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            var linearLength = SpanHelpers.CalculateTotalLength(ref lengths);
            unsafe
            {
                var data = Marshal.AllocHGlobal(linearLength * sizeof(T));
                NativeMemory.Clear(data.ToPointer(), (nuint)(linearLength * sizeof(T)));
                return new Tensor<T>(data, mustPin, lengths.ToArray());
            }
        }
        public static Tensor<T> Create<T>(bool mustPin, ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            var linearLength = SpanHelpers.CalculateTotalLength(ref lengths);
            unsafe
            {
                var data = Marshal.AllocHGlobal(linearLength * sizeof(T));
                NativeMemory.Clear(data.ToPointer(), (nuint)(linearLength * sizeof(T)));
                return new Tensor<T>(data, mustPin, lengths.ToArray(), strides.ToArray());
            }
        }

        //public static Tensor<T> Create(T* address, ReadOnlySpan<nint> lengths);
        //public static Tensor<T> Create(T* address, ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides);

        public static Tensor<T> CreateUninitialized<T>(bool mustPin, ReadOnlySpan<nint> lengths)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            var linearLength = SpanHelpers.CalculateTotalLength(ref lengths);
            unsafe
            {
                return new Tensor<T>(Marshal.AllocHGlobal(linearLength * sizeof(T)), mustPin, lengths.ToArray());
            }
        }
        public static Tensor<T> CreateUninitialized<T>(bool mustPin, ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            var linearLength = SpanHelpers.CalculateTotalLength(ref lengths);
            unsafe
            {
                return new Tensor<T>(Marshal.AllocHGlobal(linearLength * sizeof(T)), mustPin, lengths.ToArray(), strides.ToArray());
            }
        }

        //public static SpanND<T> CreateSpan(T* address, ReadOnlySpan<nint> lengths);
        //public static SpanND<T> CreateSpan(T* address, ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides);

        //public static ReadOnlySpanND<T> CreateReadOnlySpan(T* address, ReadOnlySpan<nint> lengths);
        //public static ReadOnlySpanND<T> CreateReadOnlySpan(T* address, ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides);
    }

    public static partial class Tensor
    {
        public static Tensor<T> Reshape<T>(Tensor<T> input, ReadOnlySpan<nint> lengths)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            input.Reshape(lengths.ToArray());
            return input;
        }

        public static Tensor<T> Squeeze<T>(Tensor<T> input, nint axis = -1)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            input.Squeeze(axis);
            return input;
        }

        public static Tensor<T> Unsqueeze<T>(Tensor<T> input, nint axis)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            input.Unsqueeze(axis);
            return input;
        }
    }
}
