// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        public static void Abs<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.INumberBase<T> { }
        public static void AddMultiply<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.ReadOnlySpan<T> multiplier, System.Span<T> destination) where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IMultiplyOperators<T, T, T> { }
        public static void AddMultiply<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, T multiplier, System.Span<T> destination) where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IMultiplyOperators<T, T, T> { }
        public static void AddMultiply<T>(System.ReadOnlySpan<T> x, T y, System.ReadOnlySpan<T> multiplier, System.Span<T> destination) where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IMultiplyOperators<T, T, T> { }
        public static void Add<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { }
        public static void Add<T>(System.ReadOnlySpan<T> x, T y, System.Span<T> destination) where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { }
        public static void ConvertToHalf(System.ReadOnlySpan<float> source, System.Span<System.Half> destination) { throw null; }
        public static void ConvertToSingle(System.ReadOnlySpan<System.Half> source, System.Span<float> destination) { throw null; }
        public static void Cosh<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IHyperbolicFunctions<T> { }
        public static T CosineSimilarity<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y) where T : System.Numerics.IRootFunctions<T> { throw null; }
        public static T Distance<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y) where T : System.Numerics.IRootFunctions<T> { throw null; }
        public static void Divide<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.IDivisionOperators<T, T, T> { }
        public static void Divide<T>(System.ReadOnlySpan<T> x, T y, System.Span<T> destination) where T : System.Numerics.IDivisionOperators<T, T, T> { }
        public static T Dot<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y) where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T>, System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IMultiplicativeIdentity<T, T> { throw null; }
        public static void Exp<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IExponentialFunctions<T> { }
        public static void Log2<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.ILogarithmicFunctions<T> { }
        public static void Log<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.ILogarithmicFunctions<T> { }
        public static T MaxMagnitude<T>(System.ReadOnlySpan<T> x) where T : System.Numerics.INumberBase<T> { throw null; }
        public static void MaxMagnitude<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.INumberBase<T> { }
        public static T Max<T>(System.ReadOnlySpan<T> x) where T : System.Numerics.INumber<T> { throw null; }
        public static void Max<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.INumber<T> { }
        public static T MinMagnitude<T>(System.ReadOnlySpan<T> x) where T : System.Numerics.INumberBase<T> { throw null; }
        public static void MinMagnitude<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.INumberBase<T> { }
        public static T Min<T>(System.ReadOnlySpan<T> x) where T : System.Numerics.INumber<T> { throw null; }
        public static void Min<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.INumber<T> { }
        public static void MultiplyAdd<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.ReadOnlySpan<T> addend, System.Span<T> destination) where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IMultiplyOperators<T, T, T> { }
        public static void MultiplyAdd<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, T addend, System.Span<T> destination) where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IMultiplyOperators<T, T, T> { }
        public static void MultiplyAdd<T>(System.ReadOnlySpan<T> x, T y, System.ReadOnlySpan<T> addend, System.Span<T> destination) where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IMultiplyOperators<T, T, T> { }
        public static void Multiply<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IMultiplicativeIdentity<T, T> { }
        public static void Multiply<T>(System.ReadOnlySpan<T> x, T y, System.Span<T> destination) where T : System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IMultiplicativeIdentity<T, T> { }
        public static void Negate<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IUnaryNegationOperators<T, T> { }
        public static T Norm<T>(System.ReadOnlySpan<T> x) where T : System.Numerics.IRootFunctions<T> { throw null; }
        public static T ProductOfDifferences<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y) where T : System.Numerics.ISubtractionOperators<T, T, T>, System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IMultiplicativeIdentity<T, T> { throw null; }
        public static T ProductOfSums<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y) where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T>, System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IMultiplicativeIdentity<T, T> { throw null; }
        public static T Product<T>(System.ReadOnlySpan<T> x) where T : System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IMultiplicativeIdentity<T, T> { throw null; }
        public static void Sigmoid<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IExponentialFunctions<T> { }
        public static void Sinh<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IHyperbolicFunctions<T> { }
        public static void SoftMax<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IExponentialFunctions<T> { }
        public static void Subtract<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.ISubtractionOperators<T, T, T> { }
        public static void Subtract<T>(System.ReadOnlySpan<T> x, T y, System.Span<T> destination) where T : System.Numerics.ISubtractionOperators<T, T, T> { }
        public static T SumOfMagnitudes<T>(System.ReadOnlySpan<T> x) where T : System.Numerics.INumberBase<T> { throw null; }
        public static T SumOfSquares<T>(System.ReadOnlySpan<T> x) where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T>, System.Numerics.IMultiplyOperators<T, T, T> { throw null; }
        public static T Sum<T>(System.ReadOnlySpan<T> x) where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IAdditiveIdentity<T, T> { throw null; }
        public static void Tanh<T>(System.ReadOnlySpan<T> x, System.Span<T> destination) where T : System.Numerics.IHyperbolicFunctions<T> { }
    }
}
