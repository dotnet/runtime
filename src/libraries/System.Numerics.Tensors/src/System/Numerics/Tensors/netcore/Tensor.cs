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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
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
            _values = Array.Empty<T>();
            _lengths = Array.Empty<nint>();
            _strides = Array.Empty<nint>();
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
        public ref T this[ReadOnlySpan<nint> indices] => ref AsSpan()[indices];

        // REVIEW: NOT IN API DOC BUT IN NOTEBOOK AND IN OTHER FRAMEWORKS
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
        public SpanND<T> AsSpan(params NativeRange[] ranges) => Slice(ranges);
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Tensor<T> Slice(params NativeRange[] ranges)
        {
            if (ranges.Length != Lengths.Length)
                throw new ArgumentOutOfRangeException(nameof(ranges), "Number of dimensions to slice does not equal the number of dimensions in the span");

            nint linearLength = 1;
            var lengths = new nint[ranges.Length];

            for (var i = 0; i < ranges.Length; i++)
            {
                if (ranges[i].End > Lengths[i])
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                linearLength *= (nint)(ranges[i].End - ranges[i].Start);
                lengths[i] = (nint)(ranges[i].End - ranges[i].Start);
            }

            T[] values = _isPinned ? GC.AllocateArray<T>((int)linearLength, _isPinned) : (new T[linearLength]);

            SpanND<T> span = new SpanND<T>(values, lengths);
            var s = AsSpan();
            s = s.Slice(ranges);
            s.CopyTo(span);
            return new Tensor<T>(values, lengths, _isPinned);
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

        // IEquatable
        // REVIEW: IS IStructuralEquatable/IStructuralComparable/ICloneable SOMETHING WE WANT TO ADD?
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

        /// <summary>
        /// Get a string representation of the tensor.
        /// </summary>
        private string ToMetadataString()
        {
            var sb = new StringBuilder("[");

            var n = Rank;
            if (n == 0)
            {
                sb.Append(']');
            }
            else
            {
                for (var i = 0; i < n; i++)
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

    public static partial class Tensor
    {
        public static Tensor<T> Create<T>(bool mustPin, ReadOnlySpan<nint> lengths)
            where T : IEquatable<T>
        {
            nint linearLength = SpanHelpers.CalculateTotalLength(lengths);
            T[] values = mustPin ? GC.AllocateArray<T>((int)linearLength, mustPin) : (new T[linearLength]);
            Array.Fill(values, default);
            return new Tensor<T>(values, lengths.ToArray(), mustPin);
        }
        public static Tensor<T> Create<T>(bool mustPin, ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides)
            where T : IEquatable<T>
        {
            nint linearLength = SpanHelpers.CalculateTotalLength(lengths);
            T[] values = mustPin ? GC.AllocateArray<T>((int)linearLength, mustPin) : (new T[linearLength]);
            Array.Fill(values, default);
            return new Tensor<T>(values, lengths.ToArray(), strides.ToArray(), mustPin);
        }

        //public static Tensor<T> Create(T* address, ReadOnlySpan<nint> lengths);
        //public static Tensor<T> Create(T* address, ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides);

        public static Tensor<T> CreateUninitialized<T>(bool mustPin, ReadOnlySpan<nint> lengths)
            where T : IEquatable<T>
        {
            nint linearLength = SpanHelpers.CalculateTotalLength(lengths);
            T[] values = mustPin ? GC.AllocateArray<T>((int)linearLength, mustPin) : (new T[linearLength]);
            return new Tensor<T>(values, lengths.ToArray(), mustPin);
        }

        public static Tensor<T> CreateUninitialized<T>(bool mustPin, ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides)
            where T : IEquatable<T>
        {
            nint linearLength = SpanHelpers.CalculateTotalLength(lengths);
            T[] values = mustPin ? GC.AllocateArray<T>((int)linearLength, mustPin) : (new T[linearLength]);
            return new Tensor<T>(values, lengths.ToArray(), strides.ToArray(), mustPin);
        }

        //public static SpanND<T> CreateSpan(T* address, ReadOnlySpan<nint> lengths);
        //public static SpanND<T> CreateSpan(T* address, ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides);

        //public static ReadOnlySpanND<T> CreateReadOnlySpan(T* address, ReadOnlySpan<nint> lengths);
        //public static ReadOnlySpanND<T> CreateReadOnlySpan(T* address, ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides);


        // REVIEW: NOT IN DESIGN DOC BUT IN NIKLAS' NOTEBOOK
        public static Tensor<T> FillRange<T>(IEnumerable<T> data)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            T[] values = data.ToArray();
            return new Tensor<T>(values, [values.Length], false);
        }

        // REVIEW: NOT IN DESIGN DOC BUT IN NIKLAS' NOTEBOOK
        public static Tensor<T> Uniform<T>(params nint[] lengths)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
        {
            var linearLength = SpanHelpers.CalculateTotalLength(ref lengths);
            T[] values = new T[linearLength];
            Random rand = new Random();
            for (int i = 0; i < values.Length; i++)
                values[i] = T.CreateChecked(rand.NextSingle());

            return new Tensor<T>(values, lengths, false);
        }

        // REVIEW: NOT IN DESIGN DOC BUT IN NIKLAS' NOTEBOOK
        public static Tensor<T> Zeros<T>(params nint[] lengths)
           where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            var linearLength = SpanHelpers.CalculateTotalLength(ref lengths);
            T[] values = new T[linearLength];
            Array.Fill(values, default);
            return new Tensor<T>(values, lengths, false);
        }

        #region Normal
        public static Tensor<T> Normal<T>(params nint[] lengths)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
        {
            var linearLength = SpanHelpers.CalculateTotalLength(ref lengths);
            T[] values = new T[linearLength];
            GaussianDistribution(ref values, linearLength);
            return new Tensor<T>(values, lengths, false);
        }

        private static void GaussianDistribution<T>(ref T[] values, nint linearLength)
             where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>
        {
            Random rand = new Random();
            for (int i = 0; i < linearLength; i++)
            {
                float u1 = 1.0f - rand.NextSingle();
                float u2 = 1.0f - rand.NextSingle();
                values[i] = T.CreateChecked(MathF.Sqrt(-2.0f * MathF.Log(u1)) * MathF.Sin(2.0f * MathF.PI * u2));
            }
        }
        #endregion

    }

    public static partial class Tensor
    {
        #region SetSlice
        // REVIEW: NOT IN DESIGN DOC BUT NEEDED FOR NIKLAS NOTEBOOK.
        public static Tensor<T> SetSlice<T>(this Tensor<T> tensor, Tensor<T> values, params NativeRange[] ranges)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            SpanND<T> srcSpan;
            if (ranges == Array.Empty<NativeRange>())
            {
                if (!tensor.Lengths.SequenceEqual(values.Lengths))
                    throw new ArgumentException("When no ranges are specified the values tensor must be equal in size as the input tensor.", nameof(values));
                srcSpan = tensor.AsSpan().Slice(tensor.Lengths);
            }
            else
                srcSpan = tensor.AsSpan().Slice(ranges);

            if (!srcSpan.Lengths.SequenceEqual(values.Lengths))
                throw new ArgumentException("Provided values must have the same shape as the input tensor.", nameof(values));

            values.AsSpan().CopyTo(srcSpan);

            return tensor;
        }
        #endregion

        #region FilteredUpdate
        // REVIEW: NOT IN DESIGN DOC BUT NEEDED FOR NIKLAS NOTEBOOK.
        public static Tensor<T> FilteredUpdate<T>(this Tensor<T> left, Tensor<bool> filter, T value)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (filter.Lengths.Length != left.Lengths.Length)
                throw new ArgumentOutOfRangeException(nameof(filter), "Number of dimensions to slice does not equal the number of dimensions in the span");

            var srcSpan = MemoryMarshal.CreateSpan(ref left._values[0], (int)left._linearLength);
            var filterSpan = MemoryMarshal.CreateSpan(ref filter._values[0], (int)left._linearLength);

            for (int i = 0; i < filterSpan.Length; i++)
            {
                if (filterSpan[i])
                {
                    srcSpan[i] = value;
                }
            }

            return left;
        }

        public static Tensor<T> FilteredUpdate<T>(this Tensor<T> left, Tensor<bool> filter, Tensor<T> values)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (filter.Lengths.Length != left.Lengths.Length)
                throw new ArgumentOutOfRangeException(nameof(filter), "Number of dimensions to slice does not equal the number of dimensions in the span");
            if (values.Rank != 1)
                throw new ArgumentOutOfRangeException(nameof(values), "Must be a 1d tensor");

            var numTrueElements = SpanHelpers.CountTrueElements(filter);
            if (numTrueElements != values._linearLength)
                throw new ArgumentOutOfRangeException(nameof(values), "Number of elements provided does not match the number of filters.");

            var srcSpan = MemoryMarshal.CreateSpan(ref left._values[0], (int)left._linearLength);
            var filterSpan = MemoryMarshal.CreateSpan(ref filter._values[0], (int)left._linearLength);
            var valuesSpan = MemoryMarshal.CreateSpan(ref values._values[0], (int)values._linearLength);

            var index = 0;
            for (int i = 0; i < filterSpan.Length; i++)
            {
                if (filterSpan[i])
                {
                    srcSpan[i] = valuesSpan[index++];
                }
            }

            return left;
        }
        #endregion

        #region SequenceEqual
        // REVIEW: THIS NEEDS TO SUPPORT BROADCASTING AND ADD APPROPRIATE CHECKING.
        public static Tensor<bool> SequenceEqual<T>(this Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            Tensor<bool> result = Tensor.Create<bool>(false, left.Lengths);

            for (int i = 0; i < left.LinearLength; i++)
            {
                result._values[i] = left._values[i] == right._values[i];
            }
            return result;
        }
        #endregion

        #region LessThan
        // REVIEW: ALL OF THESE NEED TO SUPPORT BROADCASTING AND ADD APPROPRIATE CHECKING.
        public static Tensor<bool> LessThan<T>(this Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result = Tensor.Create<bool>(false, left.Lengths);

            for (int i = 0; i < left.LinearLength; i++)
            {
                result._values[i] = left._values[i] < right._values[i];
            }
            return result;
        }

        public static Tensor<bool> LessThan<T>(this Tensor<T> left, T right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result = Tensor.Create<bool>(false, left.Lengths);

            for (int i = 0; i < left.LinearLength; i++)
            {
                result._values[i] = left._values[i] < right;
            }
            return result;
        }

        public static bool LessThanAny<T>(this Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            for (int i = 0; i < left.LinearLength; i++)
            {
                if (left._values[i] < right._values[i])
                    return true;
            }
            return false;
        }

        public static bool LessThanAll<T>(this Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            for (int i = 0; i < left.LinearLength; i++)
            {
                if (left._values[i] > right._values[i])
                    return false;
            }
            return true;
        }
        #endregion

        #region GreaterThan
        // REVIEW: ALL OF THESE NEED TO SUPPORT BROADCASTING AND ADD APPROPRIATE CHECKING.
        public static Tensor<bool> GreaterThan<T>(this Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result = Tensor.Create<bool>(false, left.Lengths);

            for (int i = 0; i < left.LinearLength; i++)
            {
                result._values[i] = left._values[i] > right._values[i];
            }
            return result;
        }

        public static Tensor<bool> GreaterThan<T>(this Tensor<T> left, T right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            Tensor<bool> result = Tensor.Create<bool>(false, left.Lengths);

            for (int i = 0; i < left.LinearLength; i++)
            {
                result._values[i] = left._values[i] > right;
            }
            return result;
        }

        public static bool GreaterThanAny<T>(this Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            for (int i = 0; i < left.LinearLength; i++)
            {
                if (left._values[i] > right._values[i])
                    return true;
            }
            return false;
        }

        public static bool GreaterThanAll<T>(this Tensor<T> left, Tensor<T> right)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IComparisonOperators<T, T, bool>
        {
            for (int i = 0; i < left.LinearLength; i++)
            {
                if (left._values[i] < right._values[i])
                    return false;
            }
            return true;
        }
        #endregion

        #region Stack
        public static Tensor<T> Stack<T>(Tensor<T>[] input, int axis = 0)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (input.Length < 2)
                throw new ArgumentException("Must provide at least 2 tensors to Stack.");
            if (axis < 0)
                axis = input.Rank - axis;

            Tensor<T>[] outputs = new Tensor<T>[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                outputs[i] = Tensor.Unsqueeze(input[0], axis);
            }
            return Tensor.Concatenate<T>(outputs, axis);
        }
        #endregion

        #region Reshape
        public static Tensor<T> Reshape<T>(this Tensor<T> input, params nint[] lengths)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool> => Reshape(input, lengths.AsSpan());

        public static Tensor<T> Reshape<T>(this Tensor<T> input, ReadOnlySpan<nint> lengths)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            var arrLengths = lengths.ToArray();
            // Calculate wildcard info.
            if (lengths.Contains(-1))
            {
                if (lengths.Count(-1) > 1)
                    throw new ArgumentException("Provided dimensions can only include 1 wildcard.");
                var tempTotal = input._linearLength;
                for (int i = 0; i < lengths.Length; i++)
                {
                    if (lengths[i] != -1)
                    {
                        tempTotal /= lengths[i];
                    }
                }
                arrLengths[lengths.IndexOf(-1)] = tempTotal;

            }

            var tempLinear = SpanHelpers.CalculateTotalLength(ref arrLengths);
            if (tempLinear != input.LinearLength)
                throw new ArgumentException("Provided dimensions are not valid for reshaping");
            var strides = SpanHelpers.CalculateStrides(arrLengths.Length, arrLengths);
            return new Tensor<T>(input._values, arrLengths, strides.ToArray(), input.IsPinned);
        }
        #endregion

        #region Squeeze
        public static Tensor<T> Squeeze<T>(this Tensor<T> input, int axis = -1)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (axis >= input.Rank)
                throw new ArgumentException("Cannot select an axis greater than the current Rank");

            nint[] lengths;
            nint[] strides;

            List<nint> tempLengths = new List<nint>();
            if (axis == -1)
            {
                for (int i = 0; i < input.Lengths.Length; i++)
                {
                    if (input.Lengths[i] != 1)
                    {
                        tempLengths.Add(input.Lengths[i]);
                    }
                }
                lengths = tempLengths.ToArray();
                strides = SpanHelpers.CalculateStrides(lengths.Length, lengths);
            }
            else
            {
                if (input.Lengths[axis] != 1)
                {
                    throw new ArgumentException("Cannot select an axis to squeeze which has size not equal to one");
                }
                for (int i = 0; i < input.Lengths.Length; i++)
                {
                    if (i != axis)
                    {
                        tempLengths.Add(input.Lengths[i]);
                    }
                }
                lengths = tempLengths.ToArray();
                strides = SpanHelpers.CalculateStrides(lengths.Length, lengths);
            }

            return new Tensor<T>(input._values, lengths, strides, input.IsPinned);
        }
        #endregion

        #region Unsqueeze
        public static Tensor<T> Unsqueeze<T>(this Tensor<T> input, nint axis)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (axis > input.Lengths.Length)
                throw new ArgumentException("Cannot select an axis less greater than the current Rank");
            if (axis < 0)
                axis = input.Rank - axis;

            List<nint> tempLengths = input._lengths.ToList();
            tempLengths.Insert((int)axis, 1);
            var lengths = tempLengths.ToArray();
            var strides = SpanHelpers.CalculateStrides(lengths.Length, lengths);
            return new Tensor<T>(input._values, lengths, strides, input.IsPinned);
        }
        #endregion

        #region Concatenate
        //REVIEW: SHOULD AXIS BE NULLABLE INT SO NULL CAN BE PROVIDED INSTEAD OF -1?
        /// <summary>
        /// Join a sequence of arrays along an existing axis.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tensors">The arrays must have the same shape, except in the dimension corresponding to axis (the first, by default).</param>
        /// <param name="axis">The axis along which the arrays will be joined. If axis is -1, arrays are flattened before use. Default is 0.</param>
        /// <returns></returns>
        public static Tensor<T> Concatenate<T>(ReadOnlySpan<Tensor<T>> tensors, int axis = 0)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (tensors.Length < 2)
                throw new ArgumentException("Must provide at least 2 tensors to concatenate");

            if (axis < -1 || axis > tensors[0].Rank)
                throw new ArgumentException("Invalid axis provided");

            // Calculate total space needed.
            nint totalLength = 0;
            for (int i = 0; i < tensors.Length; i++)
                totalLength += SpanHelpers.CalculateTotalLength(tensors[i].Lengths);

            nint sumOfAxis = 0;
            // If axis != -1, make sure all dimensions except the one to concatenate on match.
            if (axis != -1)
            {
                sumOfAxis = tensors[0].Lengths[axis];
                for (int i = 1; i < tensors.Length; i++)
                {
                    if (tensors[0].Rank != tensors[i].Rank)
                        throw new ArgumentException("The arrays must have the same shape, except in the dimension corresponding to axis.");
                    for (int j = 0; j < tensors[0].Rank; j++)
                    {
                        if (j != axis)
                        {
                            if (tensors[0].Lengths[j] != tensors[i].Lengths[j])
                                throw new ArgumentException("The arrays must have the same shape, except in the dimension corresponding to axis.");
                        }
                    }
                    sumOfAxis += tensors[i].Lengths[axis];
                }
            }

            T[] values = tensors[0].IsPinned ? GC.AllocateArray<T>((int)totalLength, tensors[0].IsPinned) : (new T[totalLength]);
            var dstSpan = MemoryMarshal.CreateSpan(ref values[0], (int)totalLength);
            nint valuesCopied = 0;
            nint[] indices = new nint[tensors[0].Rank];
            nint srcIndex;
            nint copyLength;

            while (valuesCopied < totalLength)
            {
                for (int i = 0; i < tensors.Length; i++)
                {
                    srcIndex = SpanHelpers.GetIndex(indices, tensors[i].Strides, tensors[i].Lengths);
                    copyLength = CalculateCopyLength(tensors[i].Lengths, axis);
                    var srcSpan = MemoryMarshal.CreateSpan(ref tensors[i]._values[srcIndex], (int)copyLength);
                    SpanHelpers.Memmove(dstSpan, srcSpan, copyLength, valuesCopied);
                    valuesCopied += copyLength;
                }
                SpanHelpers.AdjustIndices(axis - 1, 1, ref indices, tensors[0].Lengths);
            }

            Tensor<T> tensor;
            if (axis == -1)
            {
                tensor = new Tensor<T>(values, [valuesCopied], tensors[0].IsPinned);
            }
            else
            {
                nint[] lengths = new nint[tensors[0].Rank];
                tensors[0].Lengths.CopyTo(lengths);
                lengths[axis] = sumOfAxis;
                tensor = new Tensor<T>(values, lengths, tensors[0].IsPinned);
            }

            return tensor;
        }

        private static nint CalculateCopyLength(ReadOnlySpan<nint> lengths, int startingAxis)
        {
            // When starting axis is -1 we want all the data at once same as if starting axis is 0
            if (startingAxis == -1)
                startingAxis = 0;
            nint length = 1;
            for (int i = startingAxis; i < lengths.Length; i++)
            {
                length *= lengths[i];
            }
            return length;
        }
        #endregion

        #region StdDev
        public static T StdDev<T>(this Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>, IPowerFunctions<T>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>

        {
            T mean = Tensor.Mean(input);
            var span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            var output = new T[input._linearLength].AsSpan();
            TensorPrimitives.Subtract(span, mean, output);
            TensorPrimitives.Abs(output, output);
            TensorPrimitives.Pow((ReadOnlySpan<T>)output, T.CreateChecked(2), output);
            T sum = TensorPrimitives.Sum((ReadOnlySpan<T>)output);
            return T.CreateChecked(sum / T.CreateChecked(input.LinearLength));
        }

        public static TResult StdDev<T, TResult>(this Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
            where TResult : IEquatable<TResult>, IEqualityOperators<TResult, TResult, bool>, IFloatingPoint<TResult>

        {
            T sum = Tensor.Sum(input);
            return TResult.CreateChecked(TResult.CreateChecked(sum) / TResult.CreateChecked(input.LinearLength));
        }
        #endregion

        #region Mean
        public static T Mean<T>(this Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IFloatingPoint<T>

        {
            T sum = Tensor.Sum(input);
            return T.CreateChecked(sum / T.CreateChecked(input.LinearLength));
        }

        public static TResult Mean<T, TResult>(this Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, INumber<T>
            where TResult : IEquatable<TResult>, IEqualityOperators<TResult, TResult, bool>, IFloatingPoint<TResult>

        {
            T sum = Tensor.Sum(input);
            return TResult.CreateChecked(TResult.CreateChecked(sum) / TResult.CreateChecked(input.LinearLength));
        }
        #endregion

        #region Permute/Transpose
        public static Tensor<T> Transpose<T>(this Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (input.Lengths.Length < 2)
                throw new ArgumentException("Must provide a tensor with at least 2 dimensions to tranpose it.");
            var axis = Enumerable.Range(0, input.Rank).ToArray();
            var temp = axis[input.Rank - 1];
            axis[input.Rank - 1] = axis[input.Rank - 2];
            axis[input.Rank - 2] = temp;
            return Permute(input, axis.AsSpan());
        }

        public static Tensor<T> Permute<T>(this Tensor<T> input, params int[] axis)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool> => Permute(input, axis.AsSpan());

        public static Tensor<T> Permute<T>(this Tensor<T> input, ReadOnlySpan<int> axis)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>
        {
            if (input.Rank == 1)
            {
                return input;
            }
            else
            {
                T[] values = input.IsPinned ? GC.AllocateArray<T>((int)input._linearLength, input.IsPinned) : (new T[input._linearLength]);
                nint[] lengths = new nint[input.Rank];
                Tensor<T> tensor;
                SpanND<T> ospan;
                SpanND<T> ispan;
                nint[] indices;
                int[] permutation;

                if (axis.IsEmpty)
                {
                    lengths = input._lengths.Reverse().ToArray();
                    permutation = Enumerable.Range(0, input.Rank).Reverse().ToArray();
                }
                else
                {
                    if (axis.Length != input.Lengths.Length)
                        throw new ArgumentException("Must provide an axis order for each axis");
                    for (int i = 0; i < lengths.Length; i++)
                        lengths[i] = input.Lengths[axis[i]];
                    permutation = axis.ToArray();
                }
                tensor = new Tensor<T>(values, lengths, Array.Empty<nint>(), input._isPinned);
                nint[] permutedIndices = new nint[tensor.Rank];

                ospan = tensor.AsSpan();
                ispan = input.AsSpan();
                indices = new nint[tensor.Rank];
                for (int i = 0; i < input._linearLength; i++)
                {
                    PermuteIndices(ref indices, ref permutedIndices, ref permutation);
                    ospan[permutedIndices] = ispan[indices];
                    SpanHelpers.AdjustIndices(tensor.Rank - 1, 1, ref indices, input._lengths);
                }

                return tensor;
            }
        }

        private static void PermuteIndices(ref nint[] indices, ref nint[] permutedIndices, ref int[] permutation)
        {
            for(int i = 0; i < indices.Length; i++)
            {
                permutedIndices[i] = indices[permutation[i]];
            }
        }
        #endregion

        #region TensorPrimitives
        public static Tensor<T> Multiply<T>(this Tensor<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            var span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            var tensor = Tensor.Create<T>(input.IsPinned, input.Lengths);
            var rspan = MemoryMarshal.CreateSpan(ref tensor._values[0], (int)tensor._linearLength);
            TensorPrimitives.Multiply(span, val, rspan);
            return tensor;
        }

        public static Tensor<T> Multiply<T>(this Tensor<T> input, Tensor<T> other)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IMultiplyOperators<T, T, T>, IMultiplicativeIdentity<T, T>
        {
            var lspan = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            var rspan = MemoryMarshal.CreateSpan(ref other._values[0], (int)other._linearLength);
            var tensor = Tensor.Create<T>(input.IsPinned, input.Lengths);
            var ospan = MemoryMarshal.CreateSpan(ref tensor._values[0], (int)tensor._linearLength);
            TensorPrimitives.Multiply(lspan, rspan, ospan);
            return tensor;
        }

        public static Tensor<T> Divide<T>(this Tensor<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            var span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            var tensor = Tensor.Create<T>(input.IsPinned, input.Lengths);
            var rspan = MemoryMarshal.CreateSpan(ref tensor._values[0], (int)tensor._linearLength);
            TensorPrimitives.Divide(span, val, rspan);
            return tensor;
        }

        public static Tensor<T> Divide<T>(T val, Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            var span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            var tensor = Tensor.Create<T>(input.IsPinned, input.Lengths);
            var rspan = MemoryMarshal.CreateSpan(ref tensor._values[0], (int)tensor._linearLength);
            TensorPrimitives.Divide(val, span, rspan);
            return tensor;
        }

        public static Tensor<T> Divide<T>(this Tensor<T> input, Tensor<T> other)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IDivisionOperators<T, T, T>
        {
            var lspan = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            var rspan = MemoryMarshal.CreateSpan(ref other._values[0], (int)input._linearLength);
            var tensor = Tensor.Create<T>(input.IsPinned, input.Lengths);
            var resultspan = MemoryMarshal.CreateSpan(ref tensor._values[0], (int)tensor._linearLength);
            TensorPrimitives.Divide(lspan, rspan, resultspan);
            return tensor;
        }

        public static Tensor<T> Subtract<T>(this Tensor<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            var span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            var tensor = Tensor.Create<T>(input.IsPinned, input.Lengths);
            var rspan = MemoryMarshal.CreateSpan(ref tensor._values[0], (int)tensor._linearLength);
            TensorPrimitives.Subtract(span, val, rspan);
            return tensor;
        }

        public static Tensor<T> Subtract<T>(T val, Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            var span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            var tensor = Tensor.Create<T>(input.IsPinned, input.Lengths);
            var rspan = MemoryMarshal.CreateSpan(ref tensor._values[0], (int)tensor._linearLength);
            TensorPrimitives.Subtract(val, span, rspan);
            return tensor;
        }

        public static Tensor<T> Subtract<T>(this Tensor<T> input, Tensor<T> other)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ISubtractionOperators<T, T, T>
        {
            var span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            var rspan = MemoryMarshal.CreateSpan(ref other._values[0], (int)other._linearLength);
            var tensor = Tensor.Create<T>(input.IsPinned, input.Lengths);
            var ospan = MemoryMarshal.CreateSpan(ref tensor._values[0], (int)tensor._linearLength);
            TensorPrimitives.Subtract(span, rspan, ospan);
            return tensor;
        }

        public static T Sum<T>(this Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            return TensorPrimitives.Sum(span);
        }

        public static Tensor<T> Add<T>(this Tensor<T> input, Tensor<T> other)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            var span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            var rspan = MemoryMarshal.CreateSpan(ref other._values[0], (int)other._linearLength);
            var tensor = Tensor.Create<T>(input.IsPinned, input.Lengths);
            var ospan = MemoryMarshal.CreateSpan(ref tensor._values[0], (int)tensor._linearLength);
            TensorPrimitives.Add(span, rspan, ospan);
            return tensor;
        }

        public static Tensor<T> Add<T>(this Tensor<T> input, T val)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            var span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            var tensor = Tensor.Create<T>(input.IsPinned, input.Lengths);
            var ospan = MemoryMarshal.CreateSpan(ref tensor._values[0], (int)tensor._linearLength);
            TensorPrimitives.Add(span, val, ospan);
            return tensor;
        }

        public static T Norm<T>(this Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
        {
            ReadOnlySpan<T> span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            return TensorPrimitives.Norm(span);
        }

        public static Tensor<T> Cos<T>(this Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            var span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            var tensor = Tensor.Create<T>(input.IsPinned, input.Lengths);
            var ospan = MemoryMarshal.CreateSpan(ref tensor._values[0], (int)tensor._linearLength);
            TensorPrimitives.Cos(span, ospan);
            return tensor;
        }

        public static Tensor<T> Sin<T>(this Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ITrigonometricFunctions<T>
        {
            var span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            var tensor = Tensor.Create<T>(input.IsPinned, input.Lengths);
            var ospan = MemoryMarshal.CreateSpan(ref tensor._values[0], (int)tensor._linearLength);
            TensorPrimitives.Sin(span, ospan);
            return tensor;
        }

        public static Tensor<T> Sqrt<T>(this Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, IRootFunctions<T>
        {
            var span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            var tensor = Tensor.Create<T>(input.IsPinned, input.Lengths);
            var ospan = MemoryMarshal.CreateSpan(ref tensor._values[0], (int)tensor._linearLength);
            TensorPrimitives.Sqrt(span, ospan);
            return tensor;
        }

        public static Tensor<T> Log<T>(this Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            var span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            var tensor = Tensor.Create<T>(input.IsPinned, input.Lengths);
            var ospan = MemoryMarshal.CreateSpan(ref tensor._values[0], (int)tensor._linearLength);
            TensorPrimitives.Log(span, ospan);
            return tensor;
        }

        public static Tensor<T> Log10<T>(this Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            var span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            var tensor = Tensor.Create<T>(input.IsPinned, input.Lengths);
            var ospan = MemoryMarshal.CreateSpan(ref tensor._values[0], (int)tensor._linearLength);
            TensorPrimitives.Log10(span, ospan);
            return tensor;
        }

        public static Tensor<T> Log2<T>(this Tensor<T> input)
            where T : IEquatable<T>, IEqualityOperators<T, T, bool>, ILogarithmicFunctions<T>
        {
            var span = MemoryMarshal.CreateSpan(ref input._values[0], (int)input._linearLength);
            var tensor = Tensor.Create<T>(input.IsPinned, input.Lengths);
            var ospan = MemoryMarshal.CreateSpan(ref tensor._values[0], (int)tensor._linearLength);
            TensorPrimitives.Log2(span, ospan);
            return tensor;
        }
        #endregion
    }
}
