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
        public static T MinMagnitude<T>(System.ReadOnlySpan<T> x) where T : System.Numerics.INumberBase<T> { throw null; }
        public static void MinMagnitude<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.INumberBase<T> { }
        public static void MinMagnitude<T>(System.ReadOnlySpan<T> x, T y, System.Span<T> destination) where T : System.Numerics.INumberBase<T> { }
        public static T Min<T>(System.ReadOnlySpan<T> x) where T : System.Numerics.INumber<T> { throw null; }
        public static void Min<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.Span<T> destination) where T : System.Numerics.INumber<T> { }
        public static void Min<T>(System.ReadOnlySpan<T> x, T y, System.Span<T> destination) where T : System.Numerics.INumber<T> { }
        public static void MultiplyAdd<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.ReadOnlySpan<T> addend, System.Span<T> destination) where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IMultiplyOperators<T, T, T> { }
        public static void MultiplyAdd<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, T addend, System.Span<T> destination) where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IMultiplyOperators<T, T, T> { }
        public static void MultiplyAdd<T>(System.ReadOnlySpan<T> x, T y, System.ReadOnlySpan<T> addend, System.Span<T> destination) where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.IMultiplyOperators<T, T, T> { }
        public static void FusedMultiplyAdd<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, System.ReadOnlySpan<T> addend, System.Span<T> destination) where T : System.Numerics.IFloatingPointIeee754<T> { }
        public static void FusedMultiplyAdd<T>(System.ReadOnlySpan<T> x, System.ReadOnlySpan<T> y, T addend, System.Span<T> destination) where T : System.Numerics.IFloatingPointIeee754<T> { }
        public static void FusedMultiplyAdd<T>(System.ReadOnlySpan<T> x, T y, System.ReadOnlySpan<T> addend, System.Span<T> destination) where T : System.Numerics.IFloatingPointIeee754<T> { }
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
