// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using static System.Runtime.InteropServices.JavaScript.JSType;

#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable 8500 // address / sizeof of managed types

namespace System.Numerics.Tensors
{
    public sealed class Tensor<T>
        : IDisposable,
          ITensor<Tensor<T>, T>
        where T : IEquatable<T>
    {
        /// <summary>A byref or a native ptr.</summary>
        internal readonly T[] _values;
        /// <summary>The number of elements this Tensor contains.</summary>
        internal readonly nint _linearLength;
        /// <summary>The lengths of each dimension.</summary>
        internal readonly nint[] _lengths;
        internal readonly nint[] _strides;
        /// <summary>If the backing memory is permanently pinned (so not just using a fixed statement).</summary>
        internal readonly bool _isPinned;

        /// <summary>
        /// Creates a new empty Tensor.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Tensor()
        {
            _linearLength = 0;
            _values = [];
            _lengths = [];
            _strides = [];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Tensor(T[] values, nint[] lengths, bool isPinned)
        {
            _linearLength = SpanHelpers.CalculateTotalLength(ref lengths);

            Debug.Assert(_linearLength >= 0);

            _values = values;
            _lengths = lengths;
            _strides = SpanHelpers.CalculateStrides(Rank, _lengths);
            _isPinned = isPinned;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Tensor(T[] values, nint[] lengths, nint[] strides, bool isPinned)
        {
            _linearLength = SpanHelpers.CalculateTotalLength(ref lengths);

            Debug.Assert(_linearLength >= 0);

            _values = values;
            _lengths = lengths;
            _strides = strides;
            if (strides == Array.Empty<nint>())
                _strides = SpanHelpers.CalculateStrides(Rank, lengths);
            _isPinned = isPinned;

        }

        // IDisposable

        public void Dispose()
        {
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
            get => _linearLength;
        }
        // REIVEW: SHOULD WE CALL THIS DIMS OR DIMENSIONS?
        /// <summary>
        /// Gets the length of each dimension in this <see cref="Tensor{T}"/>.
        /// </summary>
        public ReadOnlySpan<nint> Lengths { get { return _lengths; } }
        public ReadOnlySpan<nint> Strides { get { return _strides; } }

        /// <summary>
        /// Returns a reference to specified element of the Span.
        /// </summary>
        /// <param name="indices"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when index less than 0 or index greater than or equal to Length
        /// </exception>
        public ref T this[params nint[] indices] => ref this[indices.AsSpan()];

        /// <summary>
        /// Returns a reference to specified element of the Span.
        /// </summary>
        /// <param name="indices"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when index less than 0 or index greater than or equal to Length
        /// </exception>
        public ref T this[ReadOnlySpan<nint> indices] => ref AsSpan()[indices];

        // REVIEW: NOT IN API DOC BUT IN NOTEBOOK AND IN OTHER FRAMEWORKS
        // REVIEW: WE WILL WANT THIS CHANGED FROM A BOOL TO SOME FILTER EXPRESSION.
        public Tensor<T> this[Tensor<bool> filter]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (filter.Lengths.Length != Lengths.Length)
                    throw new ArgumentOutOfRangeException(nameof(filter), "Number of dimensions to slice does not equal the number of dimensions in the span");

                for (var i = 0; i < filter.Lengths.Length; i++)
                {
                    if (filter.Lengths[i] != Lengths[i])
                        ThrowHelper.ThrowArgumentOutOfRangeException();
                }

                var srcSpan = MemoryMarshal.CreateSpan(ref _values[0], (int)_linearLength);
                var filterSpan = MemoryMarshal.CreateSpan(ref filter._values[0], (int)_linearLength);

                var linearLength = SpanHelpers.CountTrueElements(filter);

                T[] values = _isPinned ? GC.AllocateArray<T>((int)linearLength, _isPinned) : (new T[linearLength]);
                var index = 0;
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

        public static implicit operator SpanND<T>(Tensor<T> value) => new SpanND<T>(ref MemoryMarshal.GetArrayDataReference(value._values), value._lengths, value._strides, value._isPinned);

        public static implicit operator ReadOnlySpanND<T>(Tensor<T> value) => new ReadOnlySpanND<T>(ref MemoryMarshal.GetArrayDataReference(value._values), value._lengths, value._strides, value._isPinned);


        public SpanND<T> AsSpan() => new SpanND<T>(ref MemoryMarshal.GetArrayDataReference(_values), _lengths, _strides, _isPinned);
        public ReadOnlySpanND<T> AsReadOnlySpan() => new ReadOnlySpanND<T>(ref MemoryMarshal.GetArrayDataReference(_values), _lengths, _strides, _isPinned);
        public SpanND<T> AsSpan(params NativeRange[] ranges) => AsSpan().Slice(ranges);
        public ReadOnlySpanND<T> AsReadOnlySpan(params NativeRange[] ranges) => Slice(ranges);

        /// <summary>
        /// Returns a reference to the 0th element of the Tensor. If the Tensor is empty, returns null reference.
        /// It can be used for pinning and is required to support the use of Tensor within a fixed statement.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref T GetPinnableReference() => ref AsSpan().GetPinnableReference();

        /// <summary>
        /// Forms a slice out of the given tensor
        /// </summary>
        /// <param name="ranges">The ranges for the slice</param>
        // REVIEW: CURRENTLY DOES A COPY.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Tensor<T> Slice(params NativeRange[] ranges)
        {
            if (ranges.Length != Lengths.Length)
                throw new ArgumentOutOfRangeException(nameof(ranges), "Number of dimensions to slice does not equal the number of dimensions in the span");

            var s = AsSpan(ranges);
            T[] values = _isPinned ? GC.AllocateArray<T>(checked((int)s.LinearLength), _isPinned) : (new T[s.LinearLength]);
            var outTensor = new Tensor<T>(values, s.Lengths.ToArray(), _isPinned);
            s.CopyTo(outTensor);
            return outTensor;
        }

        /// <summary>
        /// Clears the contents of this tensor.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Clear() => AsSpan().Clear();

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
        public void CopyTo(SpanND<T> destination) => AsSpan().CopyTo(destination);

        /// <summary>
        /// Fills the contents of this span with the given value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(T value) => AsSpan().Fill(value);

        /// <summary>
        /// Copies the contents of this tensor into destination span. If the source
        /// and destinations overlap, this method behaves as if the original values in
        /// a temporary location before the destination is overwritten.
        /// </summary>
        /// <param name="destination">The span to copy items into.</param>
        /// <returns>If the destination span is shorter than the source tensor, this method
        /// return false and no data is written to the destination.</returns>
        public bool TryCopyTo(SpanND<T> destination) => AsSpan().TryCopyTo(destination);

        // IEnumerable
        public IEnumerator<T> GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

        // REVIEW: PROBABLY REMOVE.
        // IEquatable
        public bool Equals(Tensor<T>? other) => throw new NotImplementedException();

        // REVIEW: REFERENCE EQUALITY VS DEEP EQUALITY. ALL OTHER OPERATORS WILL DO EXPENSIVE WORK.
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

        // REVIEW: PROBABLY REMOVE.
        public override bool Equals(object? obj)
        {
            return Equals(obj as Tensor<T>);
        }

        // REVIEW: PROBABLY REMOVE.
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

        // REVIEW: This will be renamed.
        public string ToCSharpString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(ToMetadataString());
            sb.AppendLine("{");
            sb.Append(AsSpan().ToCSharpString());
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
