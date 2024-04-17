// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

#pragma warning disable 8500 // address / sizeof of managed types

namespace System
{
    public readonly partial struct NativeIndex : System.IEquatable<System.NativeIndex>
    {
        private readonly int _dummyPrimitive;
        public NativeIndex(nint value, bool fromEnd = false) { throw null; }
        public static System.NativeIndex End { get { throw null; } }
        public bool IsFromEnd { get { throw null; } }
        public static System.NativeIndex Start { get { throw null; } }
        public nint Value { get { throw null; } }
        public bool Equals(System.NativeIndex other) { throw null; }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? value) { throw null; }
        public static System.NativeIndex FromEnd(nint value) { throw null; }
        public static System.NativeIndex FromStart(nint value) { throw null; }
        public override int GetHashCode() { throw null; }
        public nint GetOffset(nint length) { throw null; }
        public static implicit operator System.NativeIndex (System.Index index) { throw null; }
        public static implicit operator System.NativeIndex (int value) { throw null; }
        public static implicit operator System.NativeIndex (nint value) { throw null; }
        public override string ToString() { throw null; }
    }
    public readonly partial struct NativeRange : System.IEquatable<System.NativeRange>
    {
        private readonly int _dummyPrimitive;
        public NativeRange(System.NativeIndex start, System.NativeIndex end) { throw null; }
        public static System.NativeRange All { get { throw null; } }
        public System.NativeIndex End { get { throw null; } }
        public System.NativeIndex Start { get { throw null; } }
        public static System.NativeRange EndAt(System.NativeIndex end) { throw null; }
        public bool Equals(System.NativeRange other) { throw null; }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? value) { throw null; }
        public override int GetHashCode() { throw null; }
        public (nint Offset, nint Length) GetOffsetAndLength(nint length) { throw null; }
        public static implicit operator System.NativeRange (System.Range range) { throw null; }
        public static System.NativeRange StartAt(System.NativeIndex start) { throw null; }
        public override string ToString() { throw null; }
    }
}
namespace System.Numerics.Tensors
{
    public partial interface ITensor<TSelf, T> : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.IEquatable<TSelf>, System.Numerics.IEqualityOperators<TSelf, TSelf, bool> where TSelf : System.Numerics.Tensors.ITensor<TSelf, T> where T : System.IEquatable<T>
    {
        static TSelf? Empty { get { throw null; } }
        bool IsEmpty { get; }
        bool IsPinned { get; }
        ref T this[params nint[] indices] { get; }
        ref T this[System.ReadOnlySpan<nint> indices] { get; }
        int Rank { get; }
        System.ReadOnlySpan<nint> Strides { get; }
        System.Numerics.Tensors.ReadOnlySpanND<T> AsReadOnlySpan(params System.NativeRange[] ranges);
        System.Numerics.Tensors.SpanND<T> AsSpan(params System.NativeRange[] ranges);
        void Clear();
        void CopyTo(System.Numerics.Tensors.SpanND<T> destination);
        void Fill(T value);
        ref T GetPinnableReference();
        static abstract bool operator ==(TSelf left, TSelf right);
        static abstract bool operator ==(TSelf left, T right);
        static abstract implicit operator System.Numerics.Tensors.ReadOnlySpanND<T> (TSelf value);
        static abstract implicit operator System.Numerics.Tensors.SpanND<T> (TSelf value);
        static abstract bool operator !=(TSelf left, TSelf right);
        static abstract bool operator !=(TSelf left, T right);
        TSelf Slice(params System.NativeRange[] ranges);
        bool TryCopyTo(System.Numerics.Tensors.SpanND<T> destination);
    }
    public readonly ref partial struct ReadOnlySpanND<T>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        [System.CLSCompliantAttribute(false)]
        public unsafe ReadOnlySpanND(void* pointer, System.ReadOnlySpan<nint> lengths, bool isPinned, System.ReadOnlySpan<nint> strides = default(System.ReadOnlySpan<nint>)) { throw null; }
        public ReadOnlySpanND(T[]? array, nint start, System.ReadOnlySpan<nint> lengths) { throw null; }
        public ReadOnlySpanND(T[]? array, System.ReadOnlySpan<nint> lengths) { throw null; }
        public static System.Numerics.Tensors.ReadOnlySpanND<T> Empty { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public bool IsPinned { get { throw null; } }
        public ref readonly T this[params nint[] indices] { get { throw null; } }
        public ref readonly T this[System.ReadOnlySpan<nint> indices] { get { throw null; } }
        public System.ReadOnlySpan<nint> Lengths { get { throw null; } }
        public int Rank { get { throw null; } }
        public System.ReadOnlySpan<nint> Strides { get { throw null; } }
        public static System.Numerics.Tensors.ReadOnlySpanND<T> CastUp<TDerived>(System.Numerics.Tensors.ReadOnlySpanND<TDerived> items) where TDerived : class?, T? { throw null; }
        public void CopyTo(System.Numerics.Tensors.SpanND<T> destination) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("Equals() on ReadOnlySpanND will always throw an exception. Use the equality operator instead.")]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
        public override bool Equals(object? obj) { throw null; }
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
        public System.Numerics.Tensors.ReadOnlySpanND<T>.Enumerator GetEnumerator() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("GetHashCode() on ReadOnlySpanND will always throw an exception.")]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
        public override int GetHashCode() { throw null; }
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public ref readonly T GetPinnableReference() { throw null; }
        public static bool operator ==(System.Numerics.Tensors.ReadOnlySpanND<T> left, System.Numerics.Tensors.ReadOnlySpanND<T> right) { throw null; }
        public static bool operator !=(System.Numerics.Tensors.ReadOnlySpanND<T> left, System.Numerics.Tensors.ReadOnlySpanND<T> right) { throw null; }
        public System.Numerics.Tensors.ReadOnlySpanND<T> Slice(params System.NativeRange[] ranges) { throw null; }
        public T[] ToArray() { throw null; }
        public override string ToString() { throw null; }
        public bool TryCopyTo(System.Numerics.Tensors.SpanND<T> destination) { throw null; }
        public ref partial struct Enumerator
        {
            private object _dummy;
            private int _dummyPrimitive;
            public ref readonly T Current { get { throw null; } }
            public bool MoveNext() { throw null; }
        }
    }
    public static partial class SpanNDExtensions
    {
        public static System.Numerics.Tensors.SpanND<T> AsSpanND<T>(this T[]? array, params nint[] lengths) { throw null; }
        public static System.Numerics.Tensors.SpanND<T> AsSpanND<T>(this T[]? array, System.ReadOnlySpan<nint> lengths) { throw null; }
        public static bool SequenceEqual<T>(this System.Numerics.Tensors.SpanND<T> span, System.Numerics.Tensors.SpanND<T> other) where T : System.IEquatable<T>? { throw null; }
    }
    public readonly ref partial struct SpanND<T>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        [System.CLSCompliantAttribute(false)]
        public unsafe SpanND(void* pointer, System.ReadOnlySpan<nint> lengths, bool isPinned, System.ReadOnlySpan<nint> strides = default(System.ReadOnlySpan<nint>)) { throw null; }
        public SpanND(T[]? array, nint start, System.ReadOnlySpan<nint> lengths) { throw null; }
        public SpanND(T[]? array, System.ReadOnlySpan<nint> lengths) { throw null; }
        public SpanND(T[]? array, System.ReadOnlySpan<nint> lengths, bool isPinned) { throw null; }
        public static System.Numerics.Tensors.SpanND<T> Empty { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public bool IsPinned { get { throw null; } }
        public ref T this[params nint[] indices] { get { throw null; } }
        public System.Numerics.Tensors.SpanND<T> this[params System.NativeRange[] indices] { get { throw null; } set { } }
        public ref T this[System.ReadOnlySpan<nint> indices] { get { throw null; } }
        public System.ReadOnlySpan<nint> Lengths { get { throw null; } }
        public int Rank { get { throw null; } }
        public System.ReadOnlySpan<nint> Strides { get { throw null; } }
        public void Clear() { }
        public void CopyTo(System.Numerics.Tensors.SpanND<T> destination) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("Equals() on SpanND will always throw an exception. Use the equality operator instead.")]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
        public override bool Equals(object? obj) { throw null; }
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
        public void Fill(T value) { }
        public System.Numerics.Tensors.SpanND<T>.Enumerator GetEnumerator() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("GetHashCode() on SpanND will always throw an exception.")]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
        public override int GetHashCode() { throw null; }
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public ref T GetPinnableReference() { throw null; }
        public static bool operator ==(System.Numerics.Tensors.SpanND<T> left, System.Numerics.Tensors.SpanND<T> right) { throw null; }
        public static implicit operator System.Numerics.Tensors.ReadOnlySpanND<T> (System.Numerics.Tensors.SpanND<T> span) { throw null; }
        public static bool operator !=(System.Numerics.Tensors.SpanND<T> left, System.Numerics.Tensors.SpanND<T> right) { throw null; }
        public System.Numerics.Tensors.SpanND<T> Slice(params System.NativeRange[] ranges) { throw null; }
        public T[] ToArray() { throw null; }
        public string ToCSharpString() { throw null; }
        public override string ToString() { throw null; }
        public bool TryCopyTo(System.Numerics.Tensors.SpanND<T> destination) { throw null; }
        public ref partial struct Enumerator
        {
            private object _dummy;
            private int _dummyPrimitive;
            public ref T Current { get { throw null; } }
            public bool MoveNext() { throw null; }
        }
    }
    public static partial class Tensor
    {
        public static System.Numerics.Tensors.SpanND<T> AddInPlace<T>(System.Numerics.Tensors.SpanND<T> input, System.Numerics.Tensors.SpanND<T> other) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> AddInPlace<T>(System.Numerics.Tensors.SpanND<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> AddInPlace<T>(System.Numerics.Tensors.Tensor<T> input, System.Numerics.Tensors.Tensor<T> other) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> AddInPlace<T>(System.Numerics.Tensors.Tensor<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> Add<T>(System.Numerics.Tensors.SpanND<T> input, System.Numerics.Tensors.SpanND<T> other) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> Add<T>(System.Numerics.Tensors.SpanND<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Add<T>(System.Numerics.Tensors.Tensor<T> input, System.Numerics.Tensors.Tensor<T> other) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Add<T>(System.Numerics.Tensors.Tensor<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { throw null; }
        public static bool AreShapesBroadcastToCompatible(System.ReadOnlySpan<nint> shape1, System.ReadOnlySpan<nint> shape2) { throw null; }
        public static bool AreShapesBroadcastToCompatible<T>(System.Numerics.Tensors.Tensor<T> tensor1, System.Numerics.Tensors.Tensor<T> tensor2) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> BroadcastTo<T>(System.Numerics.Tensors.Tensor<T> input, System.ReadOnlySpan<nint> shape) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Concatenate<T>(System.ReadOnlySpan<System.Numerics.Tensors.Tensor<T>> tensors, int axis = 0) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> CosInPlace<T>(System.Numerics.Tensors.SpanND<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ITrigonometricFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> CosInPlace<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ITrigonometricFunctions<T> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> Cos<T>(System.Numerics.Tensors.SpanND<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ITrigonometricFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Cos<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ITrigonometricFunctions<T> { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe static System.Numerics.Tensors.ReadOnlySpanND<T> CreateReadOnlySpan<T>(T* address, System.ReadOnlySpan<nint> lengths) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe static System.Numerics.Tensors.ReadOnlySpanND<T> CreateReadOnlySpan<T>(T* address, System.ReadOnlySpan<nint> lengths, System.ReadOnlySpan<nint> strides) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe static System.Numerics.Tensors.SpanND<T> CreateSpan<T>(T* address, System.ReadOnlySpan<nint> lengths) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe static System.Numerics.Tensors.SpanND<T> CreateSpan<T>(T* address, System.ReadOnlySpan<nint> lengths, System.ReadOnlySpan<nint> strides) { throw null; }
        public static System.Numerics.Tensors.Tensor<T> CreateUninitialized<T>(bool mustPin, System.ReadOnlySpan<nint> lengths) where T : System.IEquatable<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> CreateUninitialized<T>(bool mustPin, System.ReadOnlySpan<nint> lengths, System.ReadOnlySpan<nint> strides) where T : System.IEquatable<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Create<T>(bool mustPin, System.ReadOnlySpan<nint> lengths) where T : System.IEquatable<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Create<T>(bool mustPin, System.ReadOnlySpan<nint> lengths, System.ReadOnlySpan<nint> strides) where T : System.IEquatable<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Create<T>(T[] values, System.ReadOnlySpan<nint> lengths) where T : System.IEquatable<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Create<T>(T[] values, System.ReadOnlySpan<nint> lengths, System.ReadOnlySpan<nint> strides) where T : System.IEquatable<T> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> DivideInPlace<T>(System.Numerics.Tensors.SpanND<T> input, System.Numerics.Tensors.SpanND<T> other) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IDivisionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> DivideInPlace<T>(System.Numerics.Tensors.SpanND<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IDivisionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> DivideInPlace<T>(System.Numerics.Tensors.Tensor<T> input, System.Numerics.Tensors.Tensor<T> other) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IDivisionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> DivideInPlace<T>(System.Numerics.Tensors.Tensor<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IDivisionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> DivideInPlace<T>(T val, System.Numerics.Tensors.SpanND<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IDivisionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> DivideInPlace<T>(T val, System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IDivisionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> Divide<T>(System.Numerics.Tensors.SpanND<T> input, System.Numerics.Tensors.SpanND<T> other) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IDivisionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> Divide<T>(System.Numerics.Tensors.SpanND<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IDivisionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Divide<T>(System.Numerics.Tensors.Tensor<T> input, System.Numerics.Tensors.Tensor<T> other) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IDivisionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Divide<T>(System.Numerics.Tensors.Tensor<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IDivisionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> Divide<T>(T val, System.Numerics.Tensors.SpanND<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IDivisionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Divide<T>(T val, System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IDivisionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> FillRange<T>(System.Collections.Generic.IEnumerable<T> data) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> FilteredUpdate<T>(System.Numerics.Tensors.Tensor<T> left, System.Numerics.Tensors.Tensor<bool> filter, System.Numerics.Tensors.Tensor<T> values) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> FilteredUpdate<T>(System.Numerics.Tensors.Tensor<T> left, System.Numerics.Tensors.Tensor<bool> filter, T value) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static nint[] GetIntermediateShape(System.ReadOnlySpan<nint> shape1, int shape2Length) { throw null; }
        public static bool GreaterThanAll<T>(System.Numerics.Tensors.Tensor<T> left, System.Numerics.Tensors.Tensor<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IComparisonOperators<T, T, bool> { throw null; }
        public static bool GreaterThanAny<T>(System.Numerics.Tensors.Tensor<T> left, System.Numerics.Tensors.Tensor<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IComparisonOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<bool> GreaterThan<T>(System.Numerics.Tensors.Tensor<T> left, System.Numerics.Tensors.Tensor<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IComparisonOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<bool> GreaterThan<T>(System.Numerics.Tensors.Tensor<T> left, T right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IComparisonOperators<T, T, bool> { throw null; }
        public static bool LessThanAll<T>(System.Numerics.Tensors.Tensor<T> left, System.Numerics.Tensors.Tensor<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IComparisonOperators<T, T, bool> { throw null; }
        public static bool LessThanAny<T>(System.Numerics.Tensors.Tensor<T> left, System.Numerics.Tensors.Tensor<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IComparisonOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<bool> LessThan<T>(System.Numerics.Tensors.Tensor<T> left, System.Numerics.Tensors.Tensor<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IComparisonOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<bool> LessThan<T>(System.Numerics.Tensors.Tensor<T> left, T right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IComparisonOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> Log10InPlace<T>(System.Numerics.Tensors.SpanND<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ILogarithmicFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Log10InPlace<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ILogarithmicFunctions<T> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> Log10<T>(System.Numerics.Tensors.SpanND<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ILogarithmicFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Log10<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ILogarithmicFunctions<T> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> Log2InPlace<T>(System.Numerics.Tensors.SpanND<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ILogarithmicFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Log2InPlace<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ILogarithmicFunctions<T> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> Log2<T>(System.Numerics.Tensors.SpanND<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ILogarithmicFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Log2<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ILogarithmicFunctions<T> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> LogInPlace<T>(System.Numerics.Tensors.SpanND<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ILogarithmicFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> LogInPlace<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ILogarithmicFunctions<T> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> Log<T>(System.Numerics.Tensors.SpanND<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ILogarithmicFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Log<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ILogarithmicFunctions<T> { throw null; }
        public static T Mean<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IFloatingPoint<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Mean<T>(System.Numerics.Tensors.Tensor<T> input, int axis) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IFloatingPoint<T> { throw null; }
        public static TResult Mean<T, TResult>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.INumber<T> where TResult : System.IEquatable<TResult>, System.Numerics.IEqualityOperators<TResult, TResult, bool>, System.Numerics.IFloatingPoint<TResult> { throw null; }
        public static System.Numerics.Tensors.Tensor<TResult> Mean<T, TResult>(System.Numerics.Tensors.Tensor<T> input, int axis) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.INumber<T> where TResult : System.IEquatable<TResult>, System.Numerics.IEqualityOperators<TResult, TResult, bool>, System.Numerics.IFloatingPoint<TResult> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> MultiplyInPlace<T>(System.Numerics.Tensors.SpanND<T> input, System.Numerics.Tensors.Tensor<T> other) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IMultiplicativeIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> MultiplyInPlace<T>(System.Numerics.Tensors.SpanND<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IMultiplicativeIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> MultiplyInPlace<T>(System.Numerics.Tensors.Tensor<T> input, System.Numerics.Tensors.Tensor<T> other) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IMultiplicativeIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> MultiplyInPlace<T>(System.Numerics.Tensors.Tensor<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IMultiplicativeIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> Multiply<T>(System.Numerics.Tensors.SpanND<T> input, System.Numerics.Tensors.Tensor<T> other) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IMultiplicativeIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> Multiply<T>(System.Numerics.Tensors.SpanND<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IMultiplicativeIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Multiply<T>(System.Numerics.Tensors.Tensor<T> input, System.Numerics.Tensors.Tensor<T> other) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IMultiplicativeIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Multiply<T>(System.Numerics.Tensors.Tensor<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IMultiplicativeIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Normal<T>(params nint[] lengths) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IFloatingPoint<T> { throw null; }
        public static T Norm<T>(System.Numerics.Tensors.SpanND<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IRootFunctions<T> { throw null; }
        public static T Norm<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IRootFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Permute<T>(System.Numerics.Tensors.Tensor<T> input, params int[] axis) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Permute<T>(System.Numerics.Tensors.Tensor<T> input, System.ReadOnlySpan<int> axis) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Reshape<T>(this System.Numerics.Tensors.Tensor<T> input, params nint[] lengths) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Reshape<T>(this System.Numerics.Tensors.Tensor<T> input, System.ReadOnlySpan<nint> lengths) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> Resize<T>(System.Numerics.Tensors.SpanND<T> input, System.ReadOnlySpan<nint> shape) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Resize<T>(System.Numerics.Tensors.Tensor<T> input, System.ReadOnlySpan<nint> shape) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> Reverse<T>(System.Numerics.Tensors.SpanND<T> input, nint axis = -1) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Reverse<T>(System.Numerics.Tensors.Tensor<T> input, nint axis = -1) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<bool> SequenceEqual<T>(System.Numerics.Tensors.Tensor<T> left, System.Numerics.Tensors.Tensor<T> right) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> SetSlice<T>(this System.Numerics.Tensors.Tensor<T> tensor, System.Numerics.Tensors.Tensor<T> values, params System.NativeRange[] ranges) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> SinInPlace<T>(System.Numerics.Tensors.SpanND<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ITrigonometricFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> SinInPlace<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ITrigonometricFunctions<T> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> Sin<T>(System.Numerics.Tensors.SpanND<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ITrigonometricFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Sin<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ITrigonometricFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T>[] Split<T>(System.Numerics.Tensors.Tensor<T> input, nint numSplits, nint axis) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> SqrtInPlace<T>(System.Numerics.Tensors.SpanND<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IRootFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> SqrtInPlace<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IRootFunctions<T> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> Sqrt<T>(System.Numerics.Tensors.SpanND<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IRootFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Sqrt<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IRootFunctions<T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Squeeze<T>(System.Numerics.Tensors.Tensor<T> input, int axis = -1) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Stack<T>(System.Numerics.Tensors.Tensor<T>[] input, int axis = 0) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static T StdDev<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IFloatingPoint<T>, System.Numerics.IPowerFunctions<T>, System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { throw null; }
        public static TResult StdDev<T, TResult>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.INumber<T> where TResult : System.IEquatable<TResult>, System.Numerics.IEqualityOperators<TResult, TResult, bool>, System.Numerics.IFloatingPoint<TResult> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> SubtractInPlace<T>(System.Numerics.Tensors.SpanND<T> input, System.Numerics.Tensors.Tensor<T> other) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ISubtractionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> SubtractInPlace<T>(System.Numerics.Tensors.SpanND<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ISubtractionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> SubtractInPlace<T>(System.Numerics.Tensors.Tensor<T> input, System.Numerics.Tensors.Tensor<T> other) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ISubtractionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> SubtractInPlace<T>(System.Numerics.Tensors.Tensor<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ISubtractionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> SubtractInPlace<T>(T val, System.Numerics.Tensors.SpanND<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ISubtractionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> SubtractInPlace<T>(T val, System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ISubtractionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> Subtract<T>(System.Numerics.Tensors.SpanND<T> input, System.Numerics.Tensors.Tensor<T> other) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ISubtractionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> Subtract<T>(System.Numerics.Tensors.SpanND<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ISubtractionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Subtract<T>(System.Numerics.Tensors.Tensor<T> input, System.Numerics.Tensors.Tensor<T> other) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ISubtractionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Subtract<T>(System.Numerics.Tensors.Tensor<T> input, T val) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ISubtractionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> Subtract<T>(T val, System.Numerics.Tensors.SpanND<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ISubtractionOperators<T, T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Subtract<T>(T val, System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.ISubtractionOperators<T, T, T> { throw null; }
        public static T Sum<T>(System.Numerics.Tensors.SpanND<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.SpanND<T> Sum<T>(System.Numerics.Tensors.SpanND<T> input, int axis) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { throw null; }
        public static T Sum<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Sum<T>(System.Numerics.Tensors.Tensor<T> input, int axis) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Transpose<T>(System.Numerics.Tensors.Tensor<T> input) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool> { throw null; }
        public static System.Numerics.Tensors.Tensor<T> Uniform<T>(params nint[] lengths) where T : System.IEquatable<T>, System.Numerics.IEqualityOperators<T, T, bool>, System.Numerics.IFloatingPoint<T> { throw null; }
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
    public sealed partial class Tensor<T> : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.IDisposable, System.IEquatable<System.Numerics.Tensors.Tensor<T>>, System.Numerics.IEqualityOperators<System.Numerics.Tensors.Tensor<T>, System.Numerics.Tensors.Tensor<T>, bool>, System.Numerics.Tensors.ITensor<System.Numerics.Tensors.Tensor<T>, T> where T : System.IEquatable<T>
    {
        internal Tensor() { }
        public static System.Numerics.Tensors.Tensor<T> Empty { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public bool IsPinned { get { throw null; } }
        public ref T this[params nint[] indices] { get { throw null; } }
        public System.Numerics.Tensors.Tensor<T> this[System.Numerics.Tensors.Tensor<bool> filter] { get { throw null; } }
        public ref T this[System.ReadOnlySpan<nint> indices] { get { throw null; } }
        public System.ReadOnlySpan<nint> Lengths { get { throw null; } }
        public int Rank { get { throw null; } }
        public System.ReadOnlySpan<nint> Strides { get { throw null; } }
        public System.Numerics.Tensors.ReadOnlySpanND<T> AsReadOnlySpan() { throw null; }
        public System.Numerics.Tensors.ReadOnlySpanND<T> AsReadOnlySpan(params System.NativeRange[] ranges) { throw null; }
        public System.Numerics.Tensors.SpanND<T> AsSpan() { throw null; }
        public System.Numerics.Tensors.SpanND<T> AsSpan(params System.NativeRange[] ranges) { throw null; }
        public void Clear() { }
        public void CopyTo(System.Numerics.Tensors.SpanND<T> destination) { }
        public void Dispose() { }
        public bool Equals(System.Numerics.Tensors.Tensor<T>? other) { throw null; }
        public override bool Equals(object? obj) { throw null; }
        public void Fill(T value) { }
        public System.Collections.Generic.IEnumerator<T> GetEnumerator() { throw null; }
        public override int GetHashCode() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public ref T GetPinnableReference() { throw null; }
        public static bool operator ==(System.Numerics.Tensors.Tensor<T>? left, System.Numerics.Tensors.Tensor<T>? right) { throw null; }
        public static bool operator ==(System.Numerics.Tensors.Tensor<T> left, T right) { throw null; }
        public static implicit operator System.Numerics.Tensors.ReadOnlySpanND<T> (System.Numerics.Tensors.Tensor<T> value) { throw null; }
        public static implicit operator System.Numerics.Tensors.SpanND<T> (System.Numerics.Tensors.Tensor<T> value) { throw null; }
        public static bool operator !=(System.Numerics.Tensors.Tensor<T>? left, System.Numerics.Tensors.Tensor<T>? right) { throw null; }
        public static bool operator !=(System.Numerics.Tensors.Tensor<T> left, T right) { throw null; }
        public System.Numerics.Tensors.Tensor<T> Slice(params System.NativeRange[] ranges) { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public string ToCSharpString() { throw null; }
        public bool TryCopyTo(System.Numerics.Tensors.SpanND<T> destination) { throw null; }
        public sealed partial class Enumerator : System.Collections.Generic.IEnumerator<T>, System.Collections.IEnumerator, System.IDisposable
        {
            internal Enumerator() { }
            T System.Collections.Generic.IEnumerator<T>.Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            public void Reset() { }
        }
    }
}
