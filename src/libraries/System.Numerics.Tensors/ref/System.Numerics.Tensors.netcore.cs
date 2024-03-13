// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

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
        public static implicit operator int (System.NativeIndex index) { throw null; }
        public static implicit operator nint (System.NativeIndex index) { throw null; }
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
    public readonly ref partial struct ReadOnlySpanND<T>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        [System.CLSCompliantAttribute(false)]
        public unsafe ReadOnlySpanND(void* pointer, nint length) { throw null; }
        public ReadOnlySpanND(ref T reference) { throw null; }
        public ReadOnlySpanND(T[]? array) { throw null; }
        public ReadOnlySpanND(T[]? array, nint start, nint length) { throw null; }
        public static System.Numerics.Tensors.ReadOnlySpanND<T> Empty { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public ref readonly T this[nint index] { get { throw null; } }
        public nint Length { get { throw null; } }
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
        public static implicit operator System.Numerics.Tensors.ReadOnlySpanND<T> (System.ArraySegment<T> segment) { throw null; }
        public static implicit operator System.Numerics.Tensors.ReadOnlySpanND<T> (T[]? array) { throw null; }
        public static bool operator !=(System.Numerics.Tensors.ReadOnlySpanND<T> left, System.Numerics.Tensors.ReadOnlySpanND<T> right) { throw null; }
        public System.Numerics.Tensors.ReadOnlySpanND<T> Slice(int start) { throw null; }
        public System.Numerics.Tensors.ReadOnlySpanND<T> Slice(int start, int length) { throw null; }
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
    public readonly ref partial struct SpanND<T>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        [System.CLSCompliantAttribute(false)]
        public unsafe SpanND(void* pointer, nint length) { throw null; }
        public SpanND(ref T reference) { throw null; }
        public SpanND(T[]? array) { throw null; }
        public SpanND(T[]? array, nint start, nint length) { throw null; }
        public static System.Numerics.Tensors.SpanND<T> Empty { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public ref T this[nint index] { get { throw null; } }
        public nint Length { get { throw null; } }
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
        public static implicit operator System.Numerics.Tensors.SpanND<T> (System.ArraySegment<T> segment) { throw null; }
        public static implicit operator System.Numerics.Tensors.ReadOnlySpanND<T> (System.Numerics.Tensors.SpanND<T> span) { throw null; }
        public static implicit operator System.Numerics.Tensors.SpanND<T> (T[]? array) { throw null; }
        public static bool operator !=(System.Numerics.Tensors.SpanND<T> left, System.Numerics.Tensors.SpanND<T> right) { throw null; }
        public System.Numerics.Tensors.SpanND<T> Slice(nint start) { throw null; }
        public System.Numerics.Tensors.SpanND<T> Slice(nint start, nint length) { throw null; }
        public T[] ToArray() { throw null; }
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
}
