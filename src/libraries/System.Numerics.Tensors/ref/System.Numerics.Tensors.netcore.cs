// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

#pragma warning disable 8500 // address / sizeof of managed types

namespace System.Buffers
{
    public readonly partial struct NIndex : System.IEquatable<System.Buffers.NIndex>
    {
        private readonly int _dummyPrimitive;
        public NIndex(System.Index index) { throw null; }
        public NIndex(nint value, bool fromEnd = false) { throw null; }
        public static System.Buffers.NIndex End { get { throw null; } }
        public bool IsFromEnd { get { throw null; } }
        public static System.Buffers.NIndex Start { get { throw null; } }
        public nint Value { get { throw null; } }
        public bool Equals(System.Buffers.NIndex other) { throw null; }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? value) { throw null; }
        public static System.Buffers.NIndex FromEnd(nint value) { throw null; }
        public static System.Buffers.NIndex FromStart(nint value) { throw null; }
        public override int GetHashCode() { throw null; }
        public nint GetOffset(nint length) { throw null; }
        public static explicit operator checked System.Index (System.Buffers.NIndex value) { throw null; }
        public static explicit operator System.Index (System.Buffers.NIndex value) { throw null; }
        public static implicit operator System.Buffers.NIndex (System.Index value) { throw null; }
        public static implicit operator System.Buffers.NIndex (nint value) { throw null; }
        public System.Index ToIndex() { throw null; }
        public System.Index ToIndexUnchecked() { throw null; }
        public override string ToString() { throw null; }
    }
    public readonly partial struct NRange : System.IEquatable<System.Buffers.NRange>
    {
        private readonly int _dummyPrimitive;
        public NRange(System.Buffers.NIndex start, System.Buffers.NIndex end) { throw null; }
        public NRange(System.Range range) { throw null; }
        public static System.Buffers.NRange All { get { throw null; } }
        public System.Buffers.NIndex End { get { throw null; } }
        public System.Buffers.NIndex Start { get { throw null; } }
        public static System.Buffers.NRange EndAt(System.Buffers.NIndex end) { throw null; }
        public bool Equals(System.Buffers.NRange other) { throw null; }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? value) { throw null; }
        public override int GetHashCode() { throw null; }
        public (nint Offset, nint Length) GetOffsetAndLength(nint length) { throw null; }
        public static explicit operator checked System.Range (System.Buffers.NRange value) { throw null; }
        public static explicit operator System.Range (System.Buffers.NRange value) { throw null; }
        public static implicit operator System.Buffers.NRange (System.Range range) { throw null; }
        public static System.Buffers.NRange StartAt(System.Buffers.NIndex start) { throw null; }
        public System.Range ToRange() { throw null; }
        public System.Range ToRangeUnchecked() { throw null; }
        public override string ToString() { throw null; }
    }
}
namespace System.Numerics.Tensors
{
    public partial interface IReadOnlyTensor<TSelf, T> : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable where TSelf : System.Numerics.Tensors.IReadOnlyTensor<TSelf, T>
    {
        static abstract TSelf? Empty { get; }
        nint FlattenedLength { get; }
        bool IsEmpty { get; }
        bool IsPinned { get; }
        T this[scoped System.ReadOnlySpan<System.Buffers.NIndex> indexes] { get; }
        TSelf this[scoped System.ReadOnlySpan<System.Buffers.NRange> ranges] { get; }
        T this[scoped System.ReadOnlySpan<nint> indexes] { get; }
        int Rank { get; }
        System.Numerics.Tensors.ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan();
        System.Numerics.Tensors.ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan(scoped System.ReadOnlySpan<System.Buffers.NIndex> startIndex);
        System.Numerics.Tensors.ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan(scoped System.ReadOnlySpan<System.Buffers.NRange> range);
        System.Numerics.Tensors.ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan(scoped System.ReadOnlySpan<nint> start);
        void CopyTo(scoped System.Numerics.Tensors.TensorSpan<T> destination);
        void FlattenTo(scoped System.Span<T> destination);
        void GetLengths(scoped System.Span<nint> destination);
        ref readonly T GetPinnableReference();
        void GetStrides(scoped System.Span<nint> destination);
        TSelf Slice(scoped System.ReadOnlySpan<System.Buffers.NIndex> startIndex);
        TSelf Slice(scoped System.ReadOnlySpan<System.Buffers.NRange> range);
        TSelf Slice(scoped System.ReadOnlySpan<nint> start);
        bool TryCopyTo(scoped System.Numerics.Tensors.TensorSpan<T> destination);
        bool TryFlattenTo(scoped System.Span<T> destination);
    }
    public partial interface ITensor<TSelf, T> : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Numerics.Tensors.IReadOnlyTensor<TSelf, T> where TSelf : System.Numerics.Tensors.ITensor<TSelf, T>
    {
        bool IsReadOnly { get; }
        new T this[scoped System.ReadOnlySpan<System.Buffers.NIndex> indexes] { get; set; }
        new TSelf this[scoped System.ReadOnlySpan<System.Buffers.NRange> ranges] { get; set; }
        new T this[scoped System.ReadOnlySpan<nint> indexes] { get; set; }
        System.Numerics.Tensors.TensorSpan<T> AsTensorSpan();
        System.Numerics.Tensors.TensorSpan<T> AsTensorSpan(scoped System.ReadOnlySpan<System.Buffers.NIndex> startIndex);
        System.Numerics.Tensors.TensorSpan<T> AsTensorSpan(scoped System.ReadOnlySpan<System.Buffers.NRange> range);
        System.Numerics.Tensors.TensorSpan<T> AsTensorSpan(scoped System.ReadOnlySpan<nint> start);
        void Clear();
        static abstract TSelf Create(scoped System.ReadOnlySpan<nint> lengths, bool pinned = false);
        static abstract TSelf Create(scoped System.ReadOnlySpan<nint> lengths, scoped System.ReadOnlySpan<nint> strides, bool pinned = false);
        static abstract TSelf CreateUninitialized(scoped System.ReadOnlySpan<nint> lengths, bool pinned = false);
        static abstract TSelf CreateUninitialized(scoped System.ReadOnlySpan<nint> lengths, scoped System.ReadOnlySpan<nint> strides, bool pinned = false);
        void Fill(T value);
        new ref T GetPinnableReference();
    }
    public readonly ref partial struct ReadOnlyTensorSpan<T>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public ReadOnlyTensorSpan(System.Array? array) { throw null; }
        public ReadOnlyTensorSpan(System.Array? array, scoped System.ReadOnlySpan<System.Buffers.NIndex> startIndex, scoped System.ReadOnlySpan<nint> lengths, scoped System.ReadOnlySpan<nint> strides) { throw null; }
        public ReadOnlyTensorSpan(System.Array? array, scoped System.ReadOnlySpan<int> start, scoped System.ReadOnlySpan<nint> lengths, scoped System.ReadOnlySpan<nint> strides) { throw null; }
        public ReadOnlyTensorSpan(System.ReadOnlySpan<T> span) { throw null; }
        public ReadOnlyTensorSpan(System.ReadOnlySpan<T> span, scoped System.ReadOnlySpan<nint> lengths, scoped System.ReadOnlySpan<nint> strides) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe ReadOnlyTensorSpan(T* data, nint dataLength) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe ReadOnlyTensorSpan(T* data, nint dataLength, scoped System.ReadOnlySpan<nint> lengths, scoped System.ReadOnlySpan<nint> strides) { throw null; }
        public ReadOnlyTensorSpan(T[]? array) { throw null; }
        public ReadOnlyTensorSpan(T[]? array, System.Index startIndex, scoped System.ReadOnlySpan<nint> lengths, scoped System.ReadOnlySpan<nint> strides) { throw null; }
        public ReadOnlyTensorSpan(T[]? array, int start, scoped System.ReadOnlySpan<nint> lengths, scoped System.ReadOnlySpan<nint> strides) { throw null; }
        public static System.Numerics.Tensors.ReadOnlyTensorSpan<T> Empty { get { throw null; } }
        public nint FlattenedLength { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public ref readonly T this[scoped System.ReadOnlySpan<System.Buffers.NIndex> indexes] { get { throw null; } }
        public System.Numerics.Tensors.ReadOnlyTensorSpan<T> this[scoped System.ReadOnlySpan<System.Buffers.NRange> ranges] { get { throw null; } }
        public ref readonly T this[scoped System.ReadOnlySpan<nint> indexes] { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.UnscopedRefAttribute]
        public System.ReadOnlySpan<nint> Lengths { get { throw null; } }
        public int Rank { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.UnscopedRefAttribute]
        public System.ReadOnlySpan<nint> Strides { get { throw null; } }
        public static System.Numerics.Tensors.ReadOnlyTensorSpan<T> CastUp<TDerived>(System.Numerics.Tensors.ReadOnlyTensorSpan<TDerived> items) where TDerived : class?, T? { throw null; }
        public void CopyTo(scoped System.Numerics.Tensors.TensorSpan<T> destination) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("Equals() on ReadOnlyTensorSpan will always throw an exception. Use the equality operator instead.")]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
        public override bool Equals(object? obj) { throw null; }
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
        public System.Numerics.Tensors.ReadOnlyTensorSpan<T>.Enumerator GetEnumerator() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("GetHashCode() on ReadOnlyTensorSpan will always throw an exception.")]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
        public override int GetHashCode() { throw null; }
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public ref readonly T GetPinnableReference() { throw null; }
        public static bool operator ==(System.Numerics.Tensors.ReadOnlyTensorSpan<T> left, System.Numerics.Tensors.ReadOnlyTensorSpan<T> right) { throw null; }
        public static implicit operator System.Numerics.Tensors.ReadOnlyTensorSpan<T> (T[]? array) { throw null; }
        public static bool operator !=(System.Numerics.Tensors.ReadOnlyTensorSpan<T> left, System.Numerics.Tensors.ReadOnlyTensorSpan<T> right) { throw null; }
        public System.Numerics.Tensors.ReadOnlyTensorSpan<T> Slice(scoped System.ReadOnlySpan<System.Buffers.NIndex> indexes) { throw null; }
        public System.Numerics.Tensors.ReadOnlyTensorSpan<T> Slice(scoped System.ReadOnlySpan<System.Buffers.NRange> ranges) { throw null; }
        public override string ToString() { throw null; }
        public bool TryCopyTo(scoped System.Numerics.Tensors.TensorSpan<T> destination) { throw null; }
        public bool TryFlattenTo(scoped System.Span<T> destination) { throw null; }
        public void FlattenTo(scoped System.Span<T> destination) { throw null; }
        public ref partial struct Enumerator
        {
            private object _dummy;
            private int _dummyPrimitive;
            public ref readonly T Current { get { throw null; } }
            public bool MoveNext() { throw null; }
        }
    }
    public static partial class Tensor
    {
        public static System.Numerics.Tensors.TensorSpan<T> AddInPlace<T>(System.Numerics.Tensors.TensorSpan<T> left, System.Numerics.Tensors.TensorSpan<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> AddInPlace<T>(System.Numerics.Tensors.TensorSpan<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> AddInPlace<T>(System.Numerics.Tensors.Tensor<T> left, System.Numerics.Tensors.Tensor<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> AddInPlace<T>(System.Numerics.Tensors.Tensor<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> Add<T>(System.Numerics.Tensors.TensorSpan<T> left, System.Numerics.Tensors.TensorSpan<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> Add<T>(System.Numerics.Tensors.TensorSpan<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Add<T>(System.Numerics.Tensors.Tensor<T> left, System.Numerics.Tensors.Tensor<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Add<T>(System.Numerics.Tensors.Tensor<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Broadcast<T>(System.Numerics.Tensors.Tensor<T> input, scoped System.ReadOnlySpan<nint> shape) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Concatenate<T>(System.ReadOnlySpan<System.Numerics.Tensors.Tensor<T>> tensors, int axis = 0) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> CosInPlace<T>(System.Numerics.Tensors.TensorSpan<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ITrigonometricFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> CosInPlace<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ITrigonometricFunctions<T> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> Cos<T>(System.Numerics.Tensors.TensorSpan<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ITrigonometricFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Cos<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ITrigonometricFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> CreateAndFillGaussianNormalDistribution<T>(scoped System.ReadOnlySpan<nint> lengths) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IFloatingPoint<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> CreateAndFillUniformDistribution<T>(scoped System.ReadOnlySpan<nint> lengths) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IFloatingPoint<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> CreateFromEnumerable<T>(System.Collections.Generic.IEnumerable<T> data) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> CreateUninitialized<T>(scoped System.ReadOnlySpan<nint> lengths, bool pinned = false) where T : System.IEquatable<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> CreateUninitialized<T>(scoped System.ReadOnlySpan<nint> lengths, scoped System.ReadOnlySpan<nint> strides, bool pinned = false) where T : System.IEquatable<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Create<T>(scoped System.ReadOnlySpan<nint> lengths, bool pinned = false) where T : System.IEquatable<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Create<T>(scoped System.ReadOnlySpan<nint> lengths, scoped System.ReadOnlySpan<nint> strides, bool pinned = false) where T : System.IEquatable<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Create<T>(T[] values, scoped System.ReadOnlySpan<nint> lengths) where T : System.IEquatable<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Create<T>(T[] values, scoped System.ReadOnlySpan<nint> lengths, scoped System.ReadOnlySpan<nint> strides) where T : System.IEquatable<T> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> DivideInPlace<T>(System.Numerics.Tensors.TensorSpan<T> left, System.Numerics.Tensors.TensorSpan<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IDivisionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> DivideInPlace<T>(System.Numerics.Tensors.TensorSpan<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IDivisionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> DivideInPlace<T>(System.Numerics.Tensors.Tensor<T> left, System.Numerics.Tensors.Tensor<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IDivisionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> DivideInPlace<T>(System.Numerics.Tensors.Tensor<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IDivisionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> DivideInPlace<T>(T val, System.Numerics.Tensors.TensorSpan<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IDivisionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> DivideInPlace<T>(T val, System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IDivisionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> Divide<T>(System.Numerics.Tensors.TensorSpan<T> left, System.Numerics.Tensors.TensorSpan<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IDivisionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> Divide<T>(System.Numerics.Tensors.TensorSpan<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IDivisionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Divide<T>(System.Numerics.Tensors.Tensor<T> left, System.Numerics.Tensors.Tensor<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IDivisionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Divide<T>(System.Numerics.Tensors.Tensor<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IDivisionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> Divide<T>(T val, System.Numerics.Tensors.TensorSpan<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IDivisionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Divide<T>(T val, System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IDivisionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> FilteredUpdate<T>(System.Numerics.Tensors.Tensor<T> tensor, System.Numerics.Tensors.Tensor<bool> filter, System.Numerics.Tensors.Tensor<T> values) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> FilteredUpdate<T>(System.Numerics.Tensors.Tensor<T> tensor, System.Numerics.Tensors.Tensor<bool> filter, T value) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static bool GreaterThanAll<T>(System.Numerics.Tensors.Tensor<T> left, System.Numerics.Tensors.Tensor<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IComparisonOperators<T, T, bool> { throw null; }
        public static bool GreaterThanAny<T>(System.Numerics.Tensors.Tensor<T> left, System.Numerics.Tensors.Tensor<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IComparisonOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<bool> GreaterThan<T>(System.Numerics.Tensors.Tensor<T> left, System.Numerics.Tensors.Tensor<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IComparisonOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<bool> GreaterThan<T>(System.Numerics.Tensors.Tensor<T> left, T right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IComparisonOperators<T, T, bool> { throw null; }
        public static bool LessThanAll<T>(System.Numerics.Tensors.Tensor<T> left, System.Numerics.Tensors.Tensor<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IComparisonOperators<T, T, bool> { throw null; }
        public static bool LessThanAny<T>(System.Numerics.Tensors.Tensor<T> left, System.Numerics.Tensors.Tensor<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IComparisonOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<bool> LessThan<T>(System.Numerics.Tensors.Tensor<T> left, System.Numerics.Tensors.Tensor<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IComparisonOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<bool> LessThan<T>(System.Numerics.Tensors.Tensor<T> left, T right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IComparisonOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> Log10InPlace<T>(System.Numerics.Tensors.TensorSpan<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ILogarithmicFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Log10InPlace<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ILogarithmicFunctions<T> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> Log10<T>(System.Numerics.Tensors.TensorSpan<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ILogarithmicFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Log10<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ILogarithmicFunctions<T> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> Log2InPlace<T>(System.Numerics.Tensors.TensorSpan<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ILogarithmicFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Log2InPlace<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ILogarithmicFunctions<T> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> Log2<T>(System.Numerics.Tensors.TensorSpan<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ILogarithmicFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Log2<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ILogarithmicFunctions<T> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> LogInPlace<T>(System.Numerics.Tensors.TensorSpan<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ILogarithmicFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> LogInPlace<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ILogarithmicFunctions<T> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> Log<T>(System.Numerics.Tensors.TensorSpan<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ILogarithmicFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Log<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ILogarithmicFunctions<T> { throw null; }
        public static T Mean<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IFloatingPoint<T> { throw null; }
        public static TResult Mean<T, TResult>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.INumber<T> where TResult : System.IEquatable<TResult>, System.Numerics.IEqualityOperators<TResult, TResult, bool>, System.Numerics.IFloatingPoint<TResult> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> MultiplyInPlace<T>(System.Numerics.Tensors.TensorSpan<T> left, System.Numerics.Tensors.TensorSpan<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IMultiplicativeIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> MultiplyInPlace<T>(System.Numerics.Tensors.TensorSpan<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IMultiplicativeIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> MultiplyInPlace<T>(System.Numerics.Tensors.Tensor<T> left, System.Numerics.Tensors.Tensor<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IMultiplicativeIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> MultiplyInPlace<T>(System.Numerics.Tensors.Tensor<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IMultiplicativeIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> Multiply<T>(System.Numerics.Tensors.TensorSpan<T> left, System.Numerics.Tensors.TensorSpan<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IMultiplicativeIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> Multiply<T>(System.Numerics.Tensors.TensorSpan<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IMultiplicativeIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Multiply<T>(System.Numerics.Tensors.Tensor<T> left, System.Numerics.Tensors.Tensor<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IMultiplicativeIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Multiply<T>(System.Numerics.Tensors.Tensor<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IMultiplicativeIdentity<T, T> { throw null; }
        public static T Norm<T>(System.Numerics.Tensors.TensorSpan<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IRootFunctions<T> { throw null; }
        public static T Norm<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IRootFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Permute<T>(System.Numerics.Tensors.Tensor<T> input, scoped System.ReadOnlySpan<int> axis) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> Reshape<T>(this System.Numerics.Tensors.TensorSpan<T> input, scoped System.ReadOnlySpan<nint> lengths) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Reshape<T>(this System.Numerics.Tensors.Tensor<T> input, scoped System.ReadOnlySpan<nint> lengths) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> Resize<T>(System.Numerics.Tensors.TensorSpan<T> input, scoped System.ReadOnlySpan<nint> shape) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Resize<T>(System.Numerics.Tensors.Tensor<T> input, scoped System.ReadOnlySpan<nint> shape) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> Reverse<T>(System.Numerics.Tensors.TensorSpan<T> input, nint axis = -1) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Reverse<T>(System.Numerics.Tensors.Tensor<T> input, nint axis = -1) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<bool> SequenceEqual<T>(System.Numerics.Tensors.Tensor<T> left, System.Numerics.Tensors.Tensor<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> SetSlice<T>(this System.Numerics.Tensors.Tensor<T> tensor, System.Numerics.Tensors.Tensor<T> values, scoped System.ReadOnlySpan<System.Buffers.NRange> ranges) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> SinInPlace<T>(System.Numerics.Tensors.TensorSpan<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ITrigonometricFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> SinInPlace<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ITrigonometricFunctions<T> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> Sin<T>(System.Numerics.Tensors.TensorSpan<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ITrigonometricFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Sin<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ITrigonometricFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T>[] Split<T>(System.Numerics.Tensors.Tensor<T> input, nint numSplits, nint axis) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> SqrtInPlace<T>(System.Numerics.Tensors.TensorSpan<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IRootFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> SqrtInPlace<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IRootFunctions<T> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> Sqrt<T>(System.Numerics.Tensors.TensorSpan<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IRootFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Sqrt<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IRootFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Squeeze<T>(System.Numerics.Tensors.Tensor<T> input, int axis = -1) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Stack<T>(System.Numerics.Tensors.Tensor<T>[] input, int axis = 0) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static T StdDev<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IFloatingPoint<T>, System.Numerics.IPowerFunctions<T>, System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { throw null; }
        public static TResult StdDev<T, TResult>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.INumber<T> where TResult : System.IEquatable<TResult>, System.Numerics.IEqualityOperators<TResult, TResult, bool>, System.Numerics.IFloatingPoint<TResult> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> SubtractInPlace<T>(System.Numerics.Tensors.TensorSpan<T> left, System.Numerics.Tensors.TensorSpan<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ISubtractionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> SubtractInPlace<T>(System.Numerics.Tensors.TensorSpan<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ISubtractionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> SubtractInPlace<T>(System.Numerics.Tensors.Tensor<T> left, System.Numerics.Tensors.Tensor<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ISubtractionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> SubtractInPlace<T>(System.Numerics.Tensors.Tensor<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ISubtractionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> SubtractInPlace<T>(T val, System.Numerics.Tensors.TensorSpan<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ISubtractionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> SubtractInPlace<T>(T val, System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ISubtractionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> Subtract<T>(System.Numerics.Tensors.TensorSpan<T> left, System.Numerics.Tensors.TensorSpan<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ISubtractionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> Subtract<T>(System.Numerics.Tensors.TensorSpan<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ISubtractionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Subtract<T>(System.Numerics.Tensors.Tensor<T> left, System.Numerics.Tensors.Tensor<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ISubtractionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Subtract<T>(System.Numerics.Tensors.Tensor<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ISubtractionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> Subtract<T>(T val, System.Numerics.Tensors.TensorSpan<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ISubtractionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Subtract<T>(T val, System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ISubtractionOperators<T, T, T> { throw null; }
        public static T Sum<T>(System.Numerics.Tensors.TensorSpan<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { throw null; }
        public static T Sum<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { throw null; }
        public static string ToString<T>(this System.Numerics.Tensors.ReadOnlyTensorSpan<T> span, scoped System.ReadOnlySpan<nint> maximumLengths) { throw null; }
        public static string ToString<T>(this System.Numerics.Tensors.TensorSpan<T> span, scoped System.ReadOnlySpan<nint> maximumLengths) { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Transpose<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Unsqueeze<T>(System.Numerics.Tensors.Tensor<T> input, int axis) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
    }
    public static partial class TensorPrimitives
    {
        public static void Abs<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.INumberBase<T> { }
        public static void Acosh<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IHyperbolicFunctions<T> { }
        public static void AcosPi<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.ITrigonometricFunctions<T> { }
        public static void Acos<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.ITrigonometricFunctions<T> { }
        public static void AddMultiply<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.ReadOnlySpan<T> multiplier, System.Span<T> destination) where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IMultiplyOperators<T, T, T> { }
        public static void AddMultiply<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, T multiplier, System.Span<T> destination) where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IMultiplyOperators<T, T, T> { }
        public static void AddMultiply<T>(System.ReadOnlySpan<T> x, T y, System.ReadOnlySpan<T> multiplier, System.Span<T> destination) where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IMultiplyOperators<T, T, T> { }
        public static void Add<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { }
        public static void Add<T>(System.ReadOnlySpan<T> x, T y, System.Span<T> destination) where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { }
        public static void Asinh<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IHyperbolicFunctions<T> { }
        public static void AsinPi<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.ITrigonometricFunctions<T> { }
        public static void Asin<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.ITrigonometricFunctions<T> { }
        public static void Atan2Pi<T>(System.ReadOnlySpan<T> y, System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IFloatingPointIeee754<T> { }
        public static void Atan2Pi<T>(System.ReadOnlySpan<T> y, T x, System.Span<T> destination) where T : System.Numerics.IFloatingPointIeee754<T> { }
        public static void Atan2Pi<T>(T y, System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IFloatingPointIeee754<T> { }
        public static void Atan2<T>(System.ReadOnlySpan<T> y, System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IFloatingPointIeee754<T> { }
        public static void Atan2<T>(System.ReadOnlySpan<T> y, T x, System.Span<T> destination) where T : System.Numerics.IFloatingPointIeee754<T> { }
        public static void Atan2<T>(T y, System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IFloatingPointIeee754<T> { }
        public static void Atanh<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IHyperbolicFunctions<T> { }
        public static void AtanPi<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.ITrigonometricFunctions<T> { }
        public static void Atan<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.ITrigonometricFunctions<T> { }
        public static void BitwiseAnd<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.IBitwiseOperators<T, T, T> { }
        public static void BitwiseAnd<T>(System.ReadOnlySpan<T> x, T y, System.Span<T> destination) where T : System.Numerics.IBitwiseOperators<T, T, T> { }
        public static void BitwiseOr<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.IBitwiseOperators<T, T, T> { }
        public static void BitwiseOr<T>(System.ReadOnlySpan<T> x, T y, System.Span<T> destination) where T : System.Numerics.IBitwiseOperators<T, T, T> { }
        public static void Cbrt<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IRootFunctions<T> { }
        public static void Ceiling<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IFloatingPoint<T> { }
        public static void ConvertChecked<TFrom, TTo>(System.ReadOnlySpan<TFrom> source, System.Span<TTo> destination) where TFrom : System.Numerics.INumberBase<TFrom> where TTo : System.Numerics.INumberBase<TTo> { }
        public static void ConvertSaturating<TFrom, TTo>(System.ReadOnlySpan<TFrom> source, System.Span<TTo> destination) where TFrom : System.Numerics.INumberBase<TFrom> where TTo : System.Numerics.INumberBase<TTo> { }
        public static void ConvertTruncating<TFrom, TTo>(System.ReadOnlySpan<TFrom> source, System.Span<TTo> destination) where TFrom : System.Numerics.INumberBase<TFrom> where TTo : System.Numerics.INumberBase<TTo> { }
        public static void ConvertToHalf(System.ReadOnlySpan<float> source, System.Span<System.Half> destination) { }
        public static void ConvertToSingle(System.ReadOnlySpan<System.Half> source, System.Span<float> destination) { }
        public static void CopySign<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> sign, System.Span<T> destination) where T : System.Numerics.INumber<T> { }
        public static void CopySign<T>(System.ReadOnlySpan<T> x, T sign, System.Span<T> destination) where T : System.Numerics.INumber<T> { }
        public static void CosPi<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.ITrigonometricFunctions<T> { }
        public static void Cos<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.ITrigonometricFunctions<T> { }
        public static void Cosh<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IHyperbolicFunctions<T> { }
        public static T CosineSimilarity<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y) where T : System.Numerics.IRootFunctions<T> { throw null; }
        public static void DegreesToRadians<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.ITrigonometricFunctions<T> { }
        public static T Distance<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y) where T : System.Numerics.IRootFunctions<T> { throw null; }
        public static void Divide<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.IDivisionOperators<T, T, T> { }
        public static void Divide<T>(System.ReadOnlySpan<T> x, T y, System.Span<T> destination) where T : System.Numerics.IDivisionOperators<T, T, T> { }
        public static void Divide<T>(T x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.IDivisionOperators<T, T, T> { }
        public static T Dot<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y) where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T>, System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IMultiplicativeIdentity<T, T> { throw null; }
        public static void Exp<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IExponentialFunctions<T> { }
        public static void Exp10M1<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IExponentialFunctions<T> { }
        public static void Exp10<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IExponentialFunctions<T> { }
        public static void Exp2M1<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IExponentialFunctions<T> { }
        public static void Exp2<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IExponentialFunctions<T> { }
        public static void ExpM1<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IExponentialFunctions<T> { }
        public static void Floor<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IFloatingPoint<T> { }
        public static void FusedMultiplyAdd<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.ReadOnlySpan<T> addend, System.Span<T> destination) where T : System.Numerics.IFloatingPointIeee754<T> { }
        public static void FusedMultiplyAdd<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, T addend, System.Span<T> destination) where T : System.Numerics.IFloatingPointIeee754<T> { }
        public static void FusedMultiplyAdd<T>(System.ReadOnlySpan<T> x, T y, System.ReadOnlySpan<T> addend, System.Span<T> destination) where T : System.Numerics.IFloatingPointIeee754<T> { }
        public static void Hypot<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.IRootFunctions<T> { }
        public static void Ieee754Remainder<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.IFloatingPointIeee754<T> { }
        public static void Ieee754Remainder<T>(System.ReadOnlySpan<T> x, T y, System.Span<T> destination) where T : System.Numerics.IFloatingPointIeee754<T> { }
        public static void Ieee754Remainder<T>(T x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.IFloatingPointIeee754<T> { }
        public static void ILogB<T>(System.ReadOnlySpan<T> x, System.Span<int> destination) where T : System.Numerics.IFloatingPointIeee754<T> { }
        public static int IndexOfMaxMagnitude<T>(System.ReadOnlySpan<T> x) where T : System.Numerics.INumber<T> { throw null; }
        public static int IndexOfMax<T>(System.ReadOnlySpan<T> x) where T : System.Numerics.INumber<T> { throw null; }
        public static int IndexOfMinMagnitude<T>(System.ReadOnlySpan<T> x) where T : System.Numerics.INumber<T> { throw null; }
        public static int IndexOfMin<T>(System.ReadOnlySpan<T> x) where T : System.Numerics.INumber<T> { throw null; }
        public static void LeadingZeroCount<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IBinaryInteger<T> { }
        public static void Lerp<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.ReadOnlySpan<T> amount, System.Span<T> destination) where T : System.Numerics.IFloatingPointIeee754<T> { }
        public static void Lerp<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, T amount, System.Span<T> destination) where T : System.Numerics.IFloatingPointIeee754<T> { }
        public static void Lerp<T>(System.ReadOnlySpan<T> x, T y, System.ReadOnlySpan<T> amount, System.Span<T> destination) where T : System.Numerics.IFloatingPointIeee754<T> { }
        public static void Log2<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.ILogarithmicFunctions<T> { }
        public static void Log2P1<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.ILogarithmicFunctions<T> { }
        public static void LogP1<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.ILogarithmicFunctions<T> { }
        public static void Log<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.ILogarithmicFunctions<T> { }
        public static void Log<T>(System.ReadOnlySpan<T> x, T y, System.Span<T> destination) where T : System.Numerics.ILogarithmicFunctions<T> { }
        public static void Log<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.ILogarithmicFunctions<T> { }
        public static void Log10P1<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.ILogarithmicFunctions<T> { }
        public static void Log10<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.ILogarithmicFunctions<T> { }
        public static T MaxMagnitude<T>(System.ReadOnlySpan<T> x) where T : System.Numerics.INumberBase<T> { throw null; }
        public static void MaxMagnitude<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.INumberBase<T> { }
        public static void MaxMagnitude<T>(System.ReadOnlySpan<T> x, T y, System.Span<T> destination) where T : System.Numerics.INumberBase<T> { }
        public static T Max<T>(System.ReadOnlySpan<T> x) where T : System.Numerics.INumber<T> { throw null; }
        public static void Max<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.INumber<T> { }
        public static void Max<T>(System.ReadOnlySpan<T> x, T y, System.Span<T> destination) where T : System.Numerics.INumber<T> { }
        public static T MaxNumber<T>(System.ReadOnlySpan<T> x) where T : System.Numerics.INumber<T> { throw null; }
        public static void MaxNumber<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.INumber<T> { }
        public static void MaxNumber<T>(System.ReadOnlySpan<T> x, T y, System.Span<T> destination) where T : System.Numerics.INumber<T> { }
        public static T MinMagnitude<T>(System.ReadOnlySpan<T> x) where T : System.Numerics.INumberBase<T> { throw null; }
        public static void MinMagnitude<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.INumberBase<T> { }
        public static void MinMagnitude<T>(System.ReadOnlySpan<T> x, T y, System.Span<T> destination) where T : System.Numerics.INumberBase<T> { }
        public static T Min<T>(System.ReadOnlySpan<T> x) where T : System.Numerics.INumber<T> { throw null; }
        public static void Min<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.INumber<T> { }
        public static void Min<T>(System.ReadOnlySpan<T> x, T y, System.Span<T> destination) where T : System.Numerics.INumber<T> { }
        public static T MinNumber<T>(System.ReadOnlySpan<T> x) where T : System.Numerics.INumber<T> { throw null; }
        public static void MinNumber<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.INumber<T> { }
        public static void MinNumber<T>(System.ReadOnlySpan<T> x, T y, System.Span<T> destination) where T : System.Numerics.INumber<T> { }
        public static void MultiplyAdd<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.ReadOnlySpan<T> addend, System.Span<T> destination) where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IMultiplyOperators<T, T, T> { }
        public static void MultiplyAdd<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, T addend, System.Span<T> destination) where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IMultiplyOperators<T, T, T> { }
        public static void MultiplyAdd<T>(System.ReadOnlySpan<T> x, T y, System.ReadOnlySpan<T> addend, System.Span<T> destination) where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IMultiplyOperators<T, T, T> { }
        public static void MultiplyAddEstimate<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.ReadOnlySpan<T> addend, System.Span<T> destination) where T : System.Numerics.INumberBase<T> { }
        public static void MultiplyAddEstimate<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, T addend, System.Span<T> destination) where T : System.Numerics.INumberBase<T> { }
        public static void MultiplyAddEstimate<T>(System.ReadOnlySpan<T> x, T y, System.ReadOnlySpan<T> addend, System.Span<T> destination) where T : System.Numerics.INumberBase<T> { }
        public static void Multiply<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IMultiplicativeIdentity<T, T> { }
        public static void Multiply<T>(System.ReadOnlySpan<T> x, T y, System.Span<T> destination) where T : System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IMultiplicativeIdentity<T, T> { }
        public static void Negate<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IUnaryNegationOperators<T, T> { }
        public static T Norm<T>(System.ReadOnlySpan<T> x) where T : System.Numerics.IRootFunctions<T> { throw null; }
        public static void OnesComplement<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IBitwiseOperators<T, T, T> { }
        public static void PopCount<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IBinaryInteger<T> { }
        public static void Pow<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.IPowerFunctions<T> { }
        public static void Pow<T>(System.ReadOnlySpan<T> x, T y, System.Span<T> destination) where T : System.Numerics.IPowerFunctions<T> { }
        public static void Pow<T>(T x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.IPowerFunctions<T> { }
        public static T ProductOfDifferences<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y) where T : System.Numerics.ISubtractionOperators<T, T, T>, System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IMultiplicativeIdentity<T, T> { throw null; }
        public static T ProductOfSums<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y) where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T>, System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IMultiplicativeIdentity<T, T> { throw null; }
        public static T Product<T>(System.ReadOnlySpan<T> x) where T : System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IMultiplicativeIdentity<T, T> { throw null; }
        public static void RadiansToDegrees<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.ITrigonometricFunctions<T> { }
        public static void ReciprocalEstimate<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IFloatingPointIeee754<T> { }
        public static void ReciprocalSqrtEstimate<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IFloatingPointIeee754<T> { }
        public static void ReciprocalSqrt<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IFloatingPointIeee754<T> { }
        public static void Reciprocal<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IFloatingPoint<T> { }
        public static void RootN<T>(System.ReadOnlySpan<T> x, int n, System.Span<T> destination) where T : System.Numerics.IRootFunctions<T> { }
        public static void RotateLeft<T>(System.ReadOnlySpan<T> x, int rotateAmount, System.Span<T> destination) where T : System.Numerics.IBinaryInteger<T> { }
        public static void RotateRight<T>(System.ReadOnlySpan<T> x, int rotateAmount, System.Span<T> destination) where T : System.Numerics.IBinaryInteger<T> { }
        public static void Round<T>(System.ReadOnlySpan<T> x, int digits, System.MidpointRounding mode, System.Span<T> destination) where T : System.Numerics.IFloatingPoint<T> { }
        public static void Round<T>(System.ReadOnlySpan<T> x, int digits, System.Span<T> destination) where T : System.Numerics.IFloatingPoint<T> { }
        public static void Round<T>(System.ReadOnlySpan<T> x, System.MidpointRounding mode, System.Span<T> destination) where T : System.Numerics.IFloatingPoint<T> { }
        public static void Round<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IFloatingPoint<T> { }
        public static void ScaleB<T>(System.ReadOnlySpan<T> x, int n, System.Span<T> destination) where T : System.Numerics.IFloatingPointIeee754<T> { }
        public static void ShiftLeft<T>(System.ReadOnlySpan<T> x, int shiftAmount, System.Span<T> destination) where T : System.Numerics.IShiftOperators<T, int, T> { }
        public static void ShiftRightArithmetic<T>(System.ReadOnlySpan<T> x, int shiftAmount, System.Span<T> destination) where T : System.Numerics.IShiftOperators<T, int, T> { }
        public static void ShiftRightLogical<T>(System.ReadOnlySpan<T> x, int shiftAmount, System.Span<T> destination) where T : System.Numerics.IShiftOperators<T, int, T> { }
        public static void Sigmoid<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IExponentialFunctions<T> { }
        public static void SinCosPi<T>(System.ReadOnlySpan<T> x, System.Span<T> sinPiDestination, System.Span<T> cosPiDestination) where T : System.Numerics.ITrigonometricFunctions<T> { }
        public static void SinCos<T>(System.ReadOnlySpan<T> x, System.Span<T> sinDestination, System.Span<T> cosDestination) where T : System.Numerics.ITrigonometricFunctions<T> { }
        public static void Sinh<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IHyperbolicFunctions<T> { }
        public static void SinPi<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.ITrigonometricFunctions<T> { }
        public static void Sin<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.ITrigonometricFunctions<T> { }
        public static void SoftMax<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IExponentialFunctions<T> { }
        public static void Sqrt<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IRootFunctions<T> { }
        public static void Subtract<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.ISubtractionOperators<T, T, T> { }
        public static void Subtract<T>(System.ReadOnlySpan<T> x, T y, System.Span<T> destination) where T : System.Numerics.ISubtractionOperators<T, T, T> { }
        public static void Subtract<T>(T x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.ISubtractionOperators<T, T, T> { }
        public static T SumOfMagnitudes<T>(System.ReadOnlySpan<T> x) where T : System.Numerics.INumberBase<T> { throw null; }
        public static T SumOfSquares<T>(System.ReadOnlySpan<T> x) where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T>, System.Numerics.IMultiplyOperators<T, T, T> { throw null; }
        public static T Sum<T>(System.ReadOnlySpan<T> x) where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { throw null; }
        public static void Tanh<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IHyperbolicFunctions<T> { }
        public static void TanPi<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.ITrigonometricFunctions<T> { }
        public static void Tan<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.ITrigonometricFunctions<T> { }
        public static void TrailingZeroCount<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IBinaryInteger<T> { }
        public static void Truncate<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IFloatingPoint<T> { }
        public static void Xor<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.IBitwiseOperators<T, T, T> { }
        public static void Xor<T>(System.ReadOnlySpan<T> x, T y, System.Span<T> destination) where T : System.Numerics.IBitwiseOperators<T, T, T> { }
    }
    public static partial class TensorSpan
    {
        public static System.Numerics.Tensors.TensorSpan<T> AsTensorSpan<T>(this T[]? array, scoped System.ReadOnlySpan<nint> shape) { throw null; }
        public static bool SequenceEqual<T>(this System.Numerics.Tensors.TensorSpan<T> span, System.Numerics.Tensors.TensorSpan<T> other) where T : System.IEquatable<T>? { throw null; }
    }
    public readonly ref partial struct TensorSpan<T>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public TensorSpan(System.Array? array) { throw null; }
        public TensorSpan(System.Array? array, scoped System.ReadOnlySpan<System.Buffers.NIndex> startIndex, scoped System.ReadOnlySpan<nint> lengths, scoped System.ReadOnlySpan<nint> strides) { throw null; }
        public TensorSpan(System.Array? array, scoped System.ReadOnlySpan<int> start, scoped System.ReadOnlySpan<nint> lengths, scoped System.ReadOnlySpan<nint> strides) { throw null; }
        public TensorSpan(System.Span<T> span) { throw null; }
        public TensorSpan(System.Span<T> span, scoped System.ReadOnlySpan<nint> lengths, scoped System.ReadOnlySpan<nint> strides) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe TensorSpan(T* data, nint dataLength) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe TensorSpan(T* data, nint dataLength, scoped System.ReadOnlySpan<nint> lengths, scoped System.ReadOnlySpan<nint> strides) { throw null; }
        public TensorSpan(T[]? array) { throw null; }
        public TensorSpan(T[]? array, System.Index startIndex, scoped System.ReadOnlySpan<nint> lengths, scoped System.ReadOnlySpan<nint> strides) { throw null; }
        public TensorSpan(T[]? array, int start, scoped System.ReadOnlySpan<nint> lengths, scoped System.ReadOnlySpan<nint> strides) { throw null; }
        public static System.Numerics.Tensors.TensorSpan<T> Empty { get { throw null; } }
        public nint FlattenedLength { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public ref T this[scoped System.ReadOnlySpan<System.Buffers.NIndex> indexes] { get { throw null; } }
        public System.Numerics.Tensors.TensorSpan<T> this[scoped System.ReadOnlySpan<System.Buffers.NRange> ranges] { get { throw null; } set { } }
        public ref T this[scoped System.ReadOnlySpan<nint> indexes] { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.UnscopedRefAttribute]
        public System.ReadOnlySpan<nint> Lengths { get { throw null; } }
        public int Rank { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.UnscopedRefAttribute]
        public System.ReadOnlySpan<nint> Strides { get { throw null; } }
        public void Clear() { }
        public void CopyTo(scoped System.Numerics.Tensors.TensorSpan<T> destination) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("Equals() on TensorSpan will always throw an exception. Use the equality operator instead.")]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
        public override bool Equals(object? obj) { throw null; }
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
        public void Fill(T value) { }
        public void FlattenTo(scoped System.Span<T> destination) { }
        public System.Numerics.Tensors.TensorSpan<T>.Enumerator GetEnumerator() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("GetHashCode() on TensorSpan will always throw an exception.")]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
        public override int GetHashCode() { throw null; }
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public ref T GetPinnableReference() { throw null; }
        public static bool operator ==(System.Numerics.Tensors.TensorSpan<T> left, System.Numerics.Tensors.TensorSpan<T> right) { throw null; }
        public static implicit operator System.Numerics.Tensors.ReadOnlyTensorSpan<T> (System.Numerics.Tensors.TensorSpan<T> span) { throw null; }
        public static implicit operator System.Numerics.Tensors.TensorSpan<T> (T[]? array) { throw null; }
        public static bool operator !=(System.Numerics.Tensors.TensorSpan<T> left, System.Numerics.Tensors.TensorSpan<T> right) { throw null; }
        public System.Numerics.Tensors.TensorSpan<T> Slice(scoped System.ReadOnlySpan<System.Buffers.NIndex> indexes) { throw null; }
        public System.Numerics.Tensors.TensorSpan<T> Slice(scoped System.ReadOnlySpan<System.Buffers.NRange> ranges) { throw null; }
        public override string ToString() { throw null; }
        public bool TryCopyTo(scoped System.Numerics.Tensors.TensorSpan<T> destination) { throw null; }
        public bool TryFlattenTo(scoped System.Span<T> destination) { throw null; }
        public ref partial struct Enumerator
        {
            private object _dummy;
            private int _dummyPrimitive;
            public ref T Current { get { throw null; } }
            public bool MoveNext() { throw null; }
        }
    }
    public sealed partial class Tensor<T> : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Numerics.Tensors.IReadOnlyTensor<System.Numerics.Tensors.Tensor<T>, T>, System.Numerics.Tensors.ITensor<System.Numerics.Tensors.Tensor<T>, T>
    {
        internal Tensor() { }
        public static System.Numerics.Tensors.Tensor<T> Empty { get { throw null; } }
        public nint FlattenedLength { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public bool IsPinned { get { throw null; } }
        public System.Numerics.Tensors.Tensor<T> this[System.Numerics.Tensors.Tensor<bool> filter] { get { throw null; } }
        public ref T this[scoped System.ReadOnlySpan<System.Buffers.NIndex> indexes] { get { throw null; } }
        public System.Numerics.Tensors.Tensor<T> this[scoped System.ReadOnlySpan<System.Buffers.NRange> ranges] { get { throw null; } set { } }
        public ref T this[scoped System.ReadOnlySpan<nint> indexes] { get { throw null; } }
        public System.ReadOnlySpan<nint> Lengths { get { throw null; } }
        public int Rank { get { throw null; } }
        public System.ReadOnlySpan<nint> Strides { get { throw null; } }
        T System.Numerics.Tensors.IReadOnlyTensor<System.Numerics.Tensors.Tensor<T>, T>.this[scoped System.ReadOnlySpan<System.Buffers.NIndex> indexes] { get { throw null; } }
        System.Numerics.Tensors.Tensor<T> System.Numerics.Tensors.IReadOnlyTensor<System.Numerics.Tensors.Tensor<T>, T>.this[scoped System.ReadOnlySpan<System.Buffers.NRange> ranges] { get { throw null; } }
        T System.Numerics.Tensors.IReadOnlyTensor<System.Numerics.Tensors.Tensor<T>, T>.this[scoped System.ReadOnlySpan<nint> indexes] { get { throw null; } }
        bool System.Numerics.Tensors.ITensor<System.Numerics.Tensors.Tensor<T>, T>.IsReadOnly { get { throw null; } }
        T System.Numerics.Tensors.ITensor<System.Numerics.Tensors.Tensor<T>, T>.this[scoped System.ReadOnlySpan<System.Buffers.NIndex> indexes] { get { throw null; } set { } }
        T System.Numerics.Tensors.ITensor<System.Numerics.Tensors.Tensor<T>, T>.this[scoped System.ReadOnlySpan<nint> indexes] { get { throw null; } set { } }
        public System.Numerics.Tensors.ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan() { throw null; }
        public System.Numerics.Tensors.ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan(scoped System.ReadOnlySpan<System.Buffers.NIndex> startIndex) { throw null; }
        public System.Numerics.Tensors.ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan(scoped System.ReadOnlySpan<System.Buffers.NRange> start) { throw null; }
        public System.Numerics.Tensors.ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan(scoped System.ReadOnlySpan<nint> start) { throw null; }
        public System.Numerics.Tensors.TensorSpan<T> AsTensorSpan() { throw null; }
        public System.Numerics.Tensors.TensorSpan<T> AsTensorSpan(scoped System.ReadOnlySpan<System.Buffers.NIndex> startIndex) { throw null; }
        public System.Numerics.Tensors.TensorSpan<T> AsTensorSpan(scoped System.ReadOnlySpan<System.Buffers.NRange> start) { throw null; }
        public System.Numerics.Tensors.TensorSpan<T> AsTensorSpan(scoped System.ReadOnlySpan<nint> start) { throw null; }
        public void Clear() { }
        public void CopyTo(System.Numerics.Tensors.TensorSpan<T> destination) { }
        public void Fill(T value) { }
        public void FlattenTo(System.Span<T> destination) { }
        public System.Collections.Generic.IEnumerator<T> GetEnumerator() { throw null; }
        public override int GetHashCode() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public ref T GetPinnableReference() { throw null; }
        public static implicit operator System.Numerics.Tensors.ReadOnlyTensorSpan<T> (System.Numerics.Tensors.Tensor<T> value) { throw null; }
        public static implicit operator System.Numerics.Tensors.TensorSpan<T> (System.Numerics.Tensors.Tensor<T> value) { throw null; }
        public System.Numerics.Tensors.Tensor<T> Slice(scoped System.ReadOnlySpan<System.Buffers.NIndex> startIndex) { throw null; }
        public System.Numerics.Tensors.Tensor<T> Slice(scoped System.ReadOnlySpan<System.Buffers.NRange> start) { throw null; }
        public System.Numerics.Tensors.Tensor<T> Slice(scoped System.ReadOnlySpan<nint> start) { throw null; }
        System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        void System.Numerics.Tensors.IReadOnlyTensor<System.Numerics.Tensors.Tensor<T>, T>.GetLengths(System.Span<nint> destination) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        ref readonly T System.Numerics.Tensors.IReadOnlyTensor<System.Numerics.Tensors.Tensor<T>, T>.GetPinnableReference() { throw null; }
        void System.Numerics.Tensors.IReadOnlyTensor<System.Numerics.Tensors.Tensor<T>, T>.GetStrides(scoped System.Span<nint> destination) { }
        static System.Numerics.Tensors.Tensor<T> System.Numerics.Tensors.ITensor<System.Numerics.Tensors.Tensor<T>, T>.Create(scoped System.ReadOnlySpan<nint> lengths, bool pinned) { throw null; }
        static System.Numerics.Tensors.Tensor<T> System.Numerics.Tensors.ITensor<System.Numerics.Tensors.Tensor<T>, T>.Create(scoped System.ReadOnlySpan<nint> lengths, scoped System.ReadOnlySpan<nint> strides, bool pinned) { throw null; }
        static System.Numerics.Tensors.Tensor<T> System.Numerics.Tensors.ITensor<System.Numerics.Tensors.Tensor<T>, T>.CreateUninitialized(scoped System.ReadOnlySpan<nint> lengths, bool pinned) { throw null; }
        static System.Numerics.Tensors.Tensor<T> System.Numerics.Tensors.ITensor<System.Numerics.Tensors.Tensor<T>, T>.CreateUninitialized(scoped System.ReadOnlySpan<nint> lengths, scoped System.ReadOnlySpan<nint> strides, bool pinned) { throw null; }
        public string ToString(scoped System.ReadOnlySpan<nint> maximumLengths) { throw null; }
        public bool TryCopyTo(System.Numerics.Tensors.TensorSpan<T> destination) { throw null; }
        public bool TryFlattenTo(System.Span<T> destination) { throw null; }
    }
}
