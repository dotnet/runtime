// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Numerics;

namespace System
{
    public static class AdditionOperatorsHelper<TSelf, TOther, TResult>
        where TSelf : IAdditionOperators<TSelf, TOther, TResult>
    {
        public static TResult op_Addition(TSelf left, TOther right) => left + right;

        public static TResult op_CheckedAddition(TSelf left, TOther right) => checked(left + right);
    }

    public static class AdditiveIdentityHelper<TSelf, TResult>
        where TSelf : IAdditiveIdentity<TSelf, TResult>
    {
        public static TResult AdditiveIdentity => TSelf.AdditiveIdentity;
    }

    public static class BinaryIntegerHelper<TSelf>
        where TSelf : IBinaryInteger<TSelf>
    {
        public static (TSelf Quotient, TSelf Remainder) DivRem(TSelf left, TSelf right) => TSelf.DivRem(left, right);

        public static TSelf LeadingZeroCount(TSelf value) => TSelf.LeadingZeroCount(value);

        public static TSelf PopCount(TSelf value) => TSelf.PopCount(value);

        public static TSelf RotateLeft(TSelf value, int rotateAmount) => TSelf.RotateLeft(value, rotateAmount);

        public static TSelf RotateRight(TSelf value, int rotateAmount) => TSelf.RotateRight(value, rotateAmount);

        public static TSelf TrailingZeroCount(TSelf value) => TSelf.TrailingZeroCount(value);

        public static int GetByteCount(TSelf value) => value.GetByteCount();

        public static int GetShortestBitLength(TSelf value) => value.GetShortestBitLength();

        public static bool TryWriteBigEndian(TSelf value, Span<byte> destination, out int bytesWritten) => value.TryWriteBigEndian(destination, out bytesWritten);

        public static bool TryWriteLittleEndian(TSelf value, Span<byte> destination, out int bytesWritten) => value.TryWriteLittleEndian(destination, out bytesWritten);

        public static int WriteBigEndian(TSelf value, byte[] destination) => value.WriteBigEndian(destination);

        public static int WriteBigEndian(TSelf value, byte[] destination, int startIndex) => value.WriteBigEndian(destination, startIndex);

        public static int WriteBigEndian(TSelf value, Span<byte> destination) => value.WriteBigEndian(destination);

        public static int WriteLittleEndian(TSelf value, byte[] destination) => value.WriteLittleEndian(destination);

        public static int WriteLittleEndian(TSelf value, byte[] destination, int startIndex) => value.WriteLittleEndian(destination, startIndex);

        public static int WriteLittleEndian(TSelf value, Span<byte> destination) => value.WriteLittleEndian(destination);
    }

    public static class BinaryNumberHelper<TSelf>
        where TSelf : IBinaryNumber<TSelf>
    {
        public static bool IsPow2(TSelf value) => TSelf.IsPow2(value);

        public static TSelf Log2(TSelf value) => TSelf.Log2(value);
    }

    public static class BitwiseOperatorsHelper<TSelf, TOther, TResult>
        where TSelf : IBitwiseOperators<TSelf, TOther, TResult>
    {
        public static TResult op_BitwiseAnd(TSelf left, TOther right) => left & right;

        public static TResult op_BitwiseOr(TSelf left, TOther right) => left | right;

        public static TResult op_ExclusiveOr(TSelf left, TOther right) => left ^ right;

        public static TResult op_OnesComplement(TSelf value) => ~value;
    }

    public static class ComparisonOperatorsHelper<TSelf, TOther>
        where TSelf : IComparisonOperators<TSelf, TOther>
    {
        public static bool op_GreaterThan(TSelf left, TOther right) => left > right;

        public static bool op_GreaterThanOrEqual(TSelf left, TOther right) => left >= right;

        public static bool op_LessThan(TSelf left, TOther right) => left < right;

        public static bool op_LessThanOrEqual(TSelf left, TOther right) => left <= right;
    }

    public static class DecrementOperatorsHelper<TSelf>
        where TSelf : IDecrementOperators<TSelf>
{
        public static TSelf op_Decrement(TSelf value) => --value;

        public static TSelf op_CheckedDecrement(TSelf value) => checked(--value);
    }

    public static class DivisionOperatorsHelper<TSelf, TOther, TResult>
        where TSelf : IDivisionOperators<TSelf, TOther, TResult>
    {
        public static TResult op_Division(TSelf left, TOther right) => left / right;

        public static TResult op_CheckedDivision(TSelf left, TOther right) => checked(left / right);
    }

    public static class EqualityOperatorsHelper<TSelf, TOther>
        where TSelf : IEqualityOperators<TSelf, TOther>
    {
        public static bool op_Equality(TSelf left, TOther right) => left == right;

        public static bool op_Inequality(TSelf left, TOther right) => left != right;
    }

    public static class ExponentialFunctionsHelper<TSelf>
        where TSelf : IExponentialFunctions<TSelf>, INumberBase<TSelf>
    {
        public static TSelf Exp(TSelf x) => TSelf.Exp(x);

        public static TSelf ExpM1(TSelf x) => TSelf.ExpM1(x);

        public static TSelf Exp2(TSelf x) => TSelf.Exp2(x);

        public static TSelf Exp2M1(TSelf x) => TSelf.Exp2M1(x);

        public static TSelf Exp10(TSelf x) => TSelf.Exp10(x);

        public static TSelf Exp10M1(TSelf x) => TSelf.Exp10M1(x);
    }

    public static class FloatingPointHelper<TSelf>
        where TSelf : IFloatingPoint<TSelf>
    {
        public static TSelf Ceiling(TSelf x) => TSelf.Ceiling(x);

        public static TSelf Floor(TSelf x) => TSelf.Floor(x);

        public static TSelf Round(TSelf x) => TSelf.Round(x);

        public static TSelf Round(TSelf x, int digits) => TSelf.Round(x, digits);

        public static TSelf Round(TSelf x, MidpointRounding mode) => TSelf.Round(x, mode);

        public static TSelf Round(TSelf x, int digits, MidpointRounding mode) => TSelf.Round(x, digits, mode);

        public static TSelf Truncate(TSelf x) => TSelf.Truncate(x);

        public static int GetExponentByteCount(TSelf value) => value.GetExponentByteCount();

        public static int GetExponentShortestBitLength(TSelf value) => value.GetExponentShortestBitLength();

        public static int GetSignificandByteCount(TSelf value) => value.GetSignificandByteCount();

        public static int GetSignificandBitLength(TSelf value) => value.GetSignificandBitLength();

        public static bool TryWriteExponentBigEndian(TSelf value, Span<byte> destination, out int bytesWritten) => value.TryWriteExponentBigEndian(destination, out bytesWritten);

        public static bool TryWriteExponentLittleEndian(TSelf value, Span<byte> destination, out int bytesWritten) => value.TryWriteExponentLittleEndian(destination, out bytesWritten);

        public static bool TryWriteSignificandBigEndian(TSelf value, Span<byte> destination, out int bytesWritten) => value.TryWriteSignificandBigEndian(destination, out bytesWritten);

        public static bool TryWriteSignificandLittleEndian(TSelf value, Span<byte> destination, out int bytesWritten) => value.TryWriteSignificandLittleEndian(destination, out bytesWritten);

        public static int WriteExponentBigEndian(TSelf value, byte[] destination) => value.WriteExponentBigEndian(destination);

        public static int WriteExponentBigEndian(TSelf value, byte[] destination, int startIndex) => value.WriteExponentBigEndian(destination, startIndex);

        public static int WriteExponentBigEndian(TSelf value, Span<byte> destination) => value.WriteExponentBigEndian(destination);

        public static int WriteExponentLittleEndian(TSelf value, byte[] destination) => value.WriteExponentLittleEndian(destination);

        public static int WriteExponentLittleEndian(TSelf value, byte[] destination, int startIndex) => value.WriteExponentLittleEndian(destination, startIndex);

        public static int WriteExponentLittleEndian(TSelf value, Span<byte> destination) => value.WriteExponentLittleEndian(destination);

        public static int WriteSignificandBigEndian(TSelf value, byte[] destination) => value.WriteSignificandBigEndian(destination);

        public static int WriteSignificandBigEndian(TSelf value, byte[] destination, int startIndex) => value.WriteSignificandBigEndian(destination, startIndex);

        public static int WriteSignificandBigEndian(TSelf value, Span<byte> destination) => value.WriteSignificandBigEndian(destination);

        public static int WriteSignificandLittleEndian(TSelf value, byte[] destination) => value.WriteSignificandLittleEndian(destination);

        public static int WriteSignificandLittleEndian(TSelf value, byte[] destination, int startIndex) => value.WriteSignificandLittleEndian(destination, startIndex);

        public static int WriteSignificandLittleEndian(TSelf value, Span<byte> destination) => value.WriteSignificandLittleEndian(destination);
    }

    public static class FloatingPointIeee754Helper<TSelf>
        where TSelf : IFloatingPointIeee754<TSelf>
    {
        public static TSelf E => TSelf.E;

        public static TSelf Epsilon => TSelf.Epsilon;

        public static TSelf NaN => TSelf.NaN;

        public static TSelf NegativeInfinity => TSelf.NegativeInfinity;

        public static TSelf NegativeZero => TSelf.NegativeZero;

        public static TSelf Pi => TSelf.Pi;

        public static TSelf PositiveInfinity => TSelf.PositiveInfinity;

        public static TSelf Tau => TSelf.Tau;

        public static TSelf BitDecrement(TSelf x) => TSelf.BitDecrement(x);

        public static TSelf BitIncrement(TSelf x) => TSelf.BitIncrement(x);

        public static TSelf FusedMultiplyAdd(TSelf left, TSelf right, TSelf addend) => TSelf.FusedMultiplyAdd(left, right, addend);

        public static TSelf Ieee754Remainder(TSelf left, TSelf right) => TSelf.Ieee754Remainder(left, right);

        public static int ILogB(TSelf x) => TSelf.ILogB(x);

        public static TSelf ReciprocalEstimate(TSelf x) => TSelf.ReciprocalEstimate(x);

        public static TSelf ReciprocalSqrtEstimate(TSelf x) => TSelf.ReciprocalSqrtEstimate(x);

        public static TSelf ScaleB(TSelf x, int n) => TSelf.ScaleB(x, n);
    }

    public static class HyperbolicFunctionsHelper<TSelf>
        where TSelf : IHyperbolicFunctions<TSelf>, INumberBase<TSelf>
    {
        public static TSelf Acosh(TSelf x) => TSelf.Acosh(x);

        public static TSelf Asinh(TSelf x) => TSelf.Asinh(x);

        public static TSelf Atanh(TSelf x) => TSelf.Atanh(x);

        public static TSelf Cosh(TSelf x) => TSelf.Cosh(x);

        public static TSelf Sinh(TSelf x) => TSelf.Sinh(x);

        public static TSelf Tanh(TSelf x) => TSelf.Tanh(x);
    }

    public static class IncrementOperatorsHelper<TSelf>
        where TSelf : IIncrementOperators<TSelf>
    {
        public static TSelf op_Increment(TSelf value) => ++value;

        public static TSelf op_CheckedIncrement(TSelf value) => checked(++value);
    }

    public static class LogarithmicFunctionsHelper<TSelf>
        where TSelf : ILogarithmicFunctions<TSelf>, INumberBase<TSelf>
    {
        public static TSelf Log(TSelf x) => TSelf.Log(x);

        public static TSelf Log(TSelf x, TSelf newBase) => TSelf.Log(x, newBase);

        public static TSelf LogP1(TSelf x) => TSelf.LogP1(x);

        public static TSelf Log2(TSelf x) => TSelf.Log2(x);

        public static TSelf Log2P1(TSelf x) => TSelf.Log2P1(x);

        public static TSelf Log10(TSelf x) => TSelf.Log10(x);

        public static TSelf Log10P1(TSelf x) => TSelf.Log10P1(x);
    }

    public static class MinMaxValueHelper<TSelf>
        where TSelf : IMinMaxValue<TSelf>
    {
        public static TSelf MaxValue => TSelf.MaxValue;

        public static TSelf MinValue => TSelf.MinValue;
    }

    public static class ModulusOperatorsHelper<TSelf, TOther, TResult>
        where TSelf : IModulusOperators<TSelf, TOther, TResult>
    {
        public static TResult op_Modulus(TSelf left, TOther right) => left % right;
    }

    public static class MultiplicativeIdentityHelper<TSelf, TResult>
        where TSelf : IMultiplicativeIdentity<TSelf, TResult>
    {
        public static TResult MultiplicativeIdentity => TSelf.MultiplicativeIdentity;
    }

    public static class MultiplyOperatorsHelper<TSelf, TOther, TResult>
        where TSelf : IMultiplyOperators<TSelf, TOther, TResult>
    {
        public static TResult op_Multiply(TSelf left, TOther right) => left * right;

        public static TResult op_CheckedMultiply(TSelf left, TOther right) => checked(left * right);
    }

    public static class NumberBaseHelper<TSelf>
        where TSelf : INumberBase<TSelf>
    {
        public static TSelf One => TSelf.One;

        public static int Radix => TSelf.Radix;

        public static TSelf Zero => TSelf.Zero;

        public static TSelf Abs(TSelf value) => TSelf.Abs(value);

        public static TSelf CreateChecked<TOther>(TOther value)
            where TOther : INumberBase<TOther> => TSelf.CreateChecked(value);

        public static TSelf CreateSaturating<TOther>(TOther value)
            where TOther : INumberBase<TOther> => TSelf.CreateSaturating(value);

        public static TSelf CreateTruncating<TOther>(TOther value)
            where TOther : INumberBase<TOther> => TSelf.CreateTruncating(value);

        public static bool IsCanonical(TSelf value) => TSelf.IsCanonical(value);

        public static bool IsComplexNumber(TSelf value) => TSelf.IsComplexNumber(value);

        public static bool IsEvenInteger(TSelf value) => TSelf.IsEvenInteger(value);

        public static bool IsFinite(TSelf value) => TSelf.IsFinite(value);

        public static bool IsImaginaryNumber(TSelf value) => TSelf.IsImaginaryNumber(value);

        public static bool IsInfinity(TSelf value) => TSelf.IsInfinity(value);

        public static bool IsInteger(TSelf value) => TSelf.IsInteger(value);

        public static bool IsNaN(TSelf value) => TSelf.IsNaN(value);

        public static bool IsNegative(TSelf value) => TSelf.IsNegative(value);

        public static bool IsNegativeInfinity(TSelf value) => TSelf.IsNegativeInfinity(value);

        public static bool IsNormal(TSelf value) => TSelf.IsNormal(value);

        public static bool IsOddInteger(TSelf value) => TSelf.IsOddInteger(value);

        public static bool IsPositive(TSelf value) => TSelf.IsPositive(value);

        public static bool IsPositiveInfinity(TSelf value) => TSelf.IsPositiveInfinity(value);

        public static bool IsRealNumber(TSelf value) => TSelf.IsRealNumber(value);

        public static bool IsSubnormal(TSelf value) => TSelf.IsSubnormal(value);

        public static bool IsZero(TSelf value) => TSelf.IsZero(value);

        public static TSelf MaxMagnitude(TSelf x, TSelf y) => TSelf.MaxMagnitude(x, y);

        public static TSelf MaxMagnitudeNumber(TSelf x, TSelf y) => TSelf.MaxMagnitudeNumber(x, y);

        public static TSelf MinMagnitude(TSelf x, TSelf y) => TSelf.MinMagnitude(x, y);

        public static TSelf MinMagnitudeNumber(TSelf x, TSelf y) => TSelf.MinMagnitudeNumber(x, y);

        public static TSelf Parse(string s, NumberStyles style, IFormatProvider provider) => TSelf.Parse(s, style, provider);

        public static TSelf Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider) => TSelf.Parse(s, style, provider);

        public static bool TryParse(string s, NumberStyles style, IFormatProvider provider, out TSelf result) => TSelf.TryParse(s, style, provider, out result);

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out TSelf result) => TSelf.TryParse(s, style, provider, out result);
    }

    public static class NumberHelper<TSelf>
        where TSelf : INumber<TSelf>
    {
        public static TSelf Clamp(TSelf value, TSelf min, TSelf max) => TSelf.Clamp(value, min, max);

        public static TSelf CopySign(TSelf value, TSelf sign) => TSelf.CopySign(value, sign);

        public static TSelf Max(TSelf x, TSelf y) => TSelf.Max(x, y);

        public static TSelf MaxNumber(TSelf x, TSelf y) => TSelf.MaxNumber(x, y);

        public static TSelf Min(TSelf x, TSelf y) => TSelf.Min(x, y);

        public static TSelf MinNumber(TSelf x, TSelf y) => TSelf.MinNumber(x, y);

        public static int Sign(TSelf value) => TSelf.Sign(value);
    }

    public static class ParsableHelper<TSelf>
        where TSelf : IParsable<TSelf>
    {
        public static TSelf Parse(string s, IFormatProvider provider) => TSelf.Parse(s, provider);

        public static bool TryParse(string s, IFormatProvider provider, out TSelf result) => TSelf.TryParse(s, provider, out result);
    }

    public static class PowerFunctionsHelper<TSelf>
        where TSelf : IPowerFunctions<TSelf>, INumberBase<TSelf>
    {
        public static TSelf Pow(TSelf x, TSelf y) => TSelf.Pow(x, y);
    }

    public static class RootFunctionsHelper<TSelf>
        where TSelf : IRootFunctions<TSelf>, INumberBase<TSelf>
    {
        public static TSelf Cbrt(TSelf x) => TSelf.Cbrt(x);

        public static TSelf Sqrt(TSelf x) => TSelf.Sqrt(x);
    }

    public static class ShiftOperatorsHelper<TSelf, TResult>
        where TSelf : IShiftOperators<TSelf, TResult>
    {
        public static TResult op_LeftShift(TSelf value, int shiftAmount) => value << shiftAmount;

        public static TResult op_RightShift(TSelf value, int shiftAmount) => value >> shiftAmount;

        public static TResult op_UnsignedRightShift(TSelf value, int shiftAmount) => value >>> shiftAmount;
    }

    public static class SignedNumberHelper<TSelf>
        where TSelf : INumberBase<TSelf>, ISignedNumber<TSelf>
    {
        public static TSelf NegativeOne => TSelf.NegativeOne;
    }

    public static class SpanParsableHelper<TSelf>
        where TSelf : ISpanParsable<TSelf>
    {
        public static TSelf Parse(ReadOnlySpan<char> s, IFormatProvider provider) => TSelf.Parse(s, provider);

        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out TSelf result) => TSelf.TryParse(s, provider, out result);
    }

    public static class SubtractionOperatorsHelper<TSelf, TOther, TResult>
        where TSelf : ISubtractionOperators<TSelf, TOther, TResult>
    {
        public static TResult op_Subtraction(TSelf left, TOther right) => left - right;

        public static TResult op_CheckedSubtraction(TSelf left, TOther right) => checked(left - right);
    }

    public static class TrigonometricFunctionsHelper<TSelf>
        where TSelf : ITrigonometricFunctions<TSelf>, INumberBase<TSelf>
    {
        public static TSelf Acos(TSelf x) => TSelf.Acos(x);

        public static TSelf Asin(TSelf x) => TSelf.Asin(x);

        public static TSelf Atan(TSelf x) => TSelf.Atan(x);

        public static TSelf Atan2(TSelf y, TSelf x) => TSelf.Atan2(y, x);

        public static TSelf Cos(TSelf x) => TSelf.Cos(x);

        public static TSelf Sin(TSelf x) => TSelf.Sin(x);

        public static (TSelf Sin, TSelf Cos) SinCos(TSelf x) => TSelf.SinCos(x);

        public static TSelf Tan(TSelf x) => TSelf.Tan(x);
    }

    public static class UnaryNegationOperatorsHelper<TSelf, TResult>
        where TSelf : IUnaryNegationOperators<TSelf, TResult>
    {
        public static TResult op_UnaryNegation(TSelf value) => -value;

        public static TResult op_CheckedUnaryNegation(TSelf value) => checked(-value);
    }

    public static class UnaryPlusOperatorsHelper<TSelf, TResult>
        where TSelf : IUnaryPlusOperators<TSelf, TResult>
    {
        public static TResult op_UnaryPlus(TSelf value) => +value;
    }
}
