// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.Versioning;

namespace System.Tests
{
    [RequiresPreviewFeatures]
    public static class AdditionOperatorsHelper<TSelf, TOther, TResult>
        where TSelf : IAdditionOperators<TSelf, TOther, TResult>
    {
        public static TResult op_Addition(TSelf left, TOther right) => left + right;
    }

    [RequiresPreviewFeatures]
    public static class AdditiveIdentityHelper<TSelf, TResult>
        where TSelf : IAdditiveIdentity<TSelf, TResult>
    {
        public static TResult AdditiveIdentity => TSelf.AdditiveIdentity;
    }

    [RequiresPreviewFeatures]
    public static class BinaryIntegerHelper<TSelf>
        where TSelf : IBinaryInteger<TSelf>
    {
        public static TSelf LeadingZeroCount(TSelf value) => TSelf.LeadingZeroCount(value);

        public static TSelf PopCount(TSelf value) => TSelf.PopCount(value);

        public static TSelf RotateLeft(TSelf value, int rotateAmount) => TSelf.RotateLeft(value, rotateAmount);

        public static TSelf RotateRight(TSelf value, int rotateAmount) => TSelf.RotateRight(value, rotateAmount);

        public static TSelf TrailingZeroCount(TSelf value) => TSelf.TrailingZeroCount(value);
    }

    [RequiresPreviewFeatures]
    public static class BinaryNumberHelper<TSelf>
        where TSelf : IBinaryNumber<TSelf>
    {
        public static bool IsPow2(TSelf value) => TSelf.IsPow2(value);

        public static TSelf Log2(TSelf value) => TSelf.Log2(value);
    }

    [RequiresPreviewFeatures]
    public static class BitwiseOperatorsHelper<TSelf, TOther, TResult>
        where TSelf : IBitwiseOperators<TSelf, TOther, TResult>
    {
        public static TResult op_BitwiseAnd(TSelf left, TOther right) => left & right;

        public static TResult op_BitwiseOr(TSelf left, TOther right) => left | right;

        public static TResult op_ExclusiveOr(TSelf left, TOther right) => left ^ right;

        public static TResult op_OnesComplement(TSelf value) => ~value;
    }

    [RequiresPreviewFeatures]
    public static class ComparisonOperatorsHelper<TSelf, TOther>
        where TSelf : IComparisonOperators<TSelf, TOther>
    {
        public static bool op_GreaterThan(TSelf left, TOther right) => left > right;

        public static bool op_GreaterThanOrEqual(TSelf left, TOther right) => left >= right;

        public static bool op_LessThan(TSelf left, TOther right) => left < right;

        public static bool op_LessThanOrEqual(TSelf left, TOther right) => left <= right;
    }

    [RequiresPreviewFeatures]
    public static class DecrementOperatorsHelper<TSelf>
        where TSelf : IDecrementOperators<TSelf>
{
        public static TSelf op_Decrement(TSelf value) => --value;
    }

    [RequiresPreviewFeatures]
    public static class DivisionOperatorsHelper<TSelf, TOther, TResult>
        where TSelf : IDivisionOperators<TSelf, TOther, TResult>
    {
        public static TResult op_Division(TSelf left, TOther right) => left / right;
    }

    [RequiresPreviewFeatures]
    public static class EqualityOperatorsHelper<TSelf, TOther>
        where TSelf : IEqualityOperators<TSelf, TOther>
    {
        public static bool op_Equality(TSelf left, TOther right) => left == right;

        public static bool op_Inequality(TSelf left, TOther right) => left != right;
    }

    [RequiresPreviewFeatures]
    public static class IncrementOperatorsHelper<TSelf>
        where TSelf : IIncrementOperators<TSelf>
    {
        public static TSelf op_Increment(TSelf value) => ++value;
    }

    [RequiresPreviewFeatures]
    public static class ModulusOperatorsHelper<TSelf, TOther, TResult>
        where TSelf : IModulusOperators<TSelf, TOther, TResult>
    {
        public static TResult op_Modulus(TSelf left, TOther right) => left % right;
    }

    [RequiresPreviewFeatures]
    public static class MultiplyOperatorsHelper<TSelf, TOther, TResult>
        where TSelf : IMultiplyOperators<TSelf, TOther, TResult>
    {
        public static TResult op_Multiply(TSelf left, TOther right) => left * right;
    }

    [RequiresPreviewFeatures]
    public static class MinMaxValueHelper<TSelf>
        where TSelf : IMinMaxValue<TSelf>
    {
        public static TSelf MaxValue => TSelf.MaxValue;

        public static TSelf MinValue => TSelf.MinValue;
    }

    [RequiresPreviewFeatures]
    public static class MultiplicativeIdentityHelper<TSelf, TResult>
        where TSelf : IMultiplicativeIdentity<TSelf, TResult>
    {
        public static TResult MultiplicativeIdentity => TSelf.MultiplicativeIdentity;
    }

    [RequiresPreviewFeatures]
    public static class NumberHelper<TSelf>
        where TSelf : INumber<TSelf>
    {
        public static TSelf One => TSelf.One;

        public static TSelf Zero => TSelf.Zero;

        public static TSelf Abs(TSelf value) => TSelf.Abs(value);

        public static TSelf Clamp(TSelf value, TSelf min, TSelf max) => TSelf.Clamp(value, min, max);

        public static TSelf Create<TOther>(TOther value)
            where TOther : INumber<TOther> => TSelf.Create<TOther>(value);

        public static TSelf CreateSaturating<TOther>(TOther value)
            where TOther : INumber<TOther> => TSelf.CreateSaturating<TOther>(value);

        public static TSelf CreateTruncating<TOther>(TOther value)
            where TOther : INumber<TOther> => TSelf.CreateTruncating<TOther>(value);

        public static (TSelf Quotient, TSelf Remainder) DivRem(TSelf left, TSelf right) => TSelf.DivRem(left, right);

        public static TSelf Max(TSelf x, TSelf y) => TSelf.Max(x, y);

        public static TSelf Min(TSelf x, TSelf y) => TSelf.Min(x, y);

        public static TSelf Parse(string s, NumberStyles style, IFormatProvider provider) => TSelf.Parse(s, style, provider);

        public static TSelf Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider) => TSelf.Parse(s, style, provider);

        public static TSelf Sign(TSelf value) => TSelf.Sign(value);

        public static bool TryCreate<TOther>(TOther value, out TSelf result)
            where TOther : INumber<TOther> => TSelf.TryCreate<TOther>(value, out result);

        public static bool TryParse(string s, NumberStyles style, IFormatProvider provider, out TSelf result) => TSelf.TryParse(s, style, provider, out result);

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out TSelf result) => TSelf.TryParse(s, style, provider, out result);
    }

    [RequiresPreviewFeatures]
    public static class ParseableHelper<TSelf>
        where TSelf : IParseable<TSelf>
    {
        public static TSelf Parse(string s, IFormatProvider provider) => TSelf.Parse(s, provider);

        public static bool TryParse(string s, IFormatProvider provider, out TSelf result) => TSelf.TryParse(s, provider, out result);
    }

    [RequiresPreviewFeatures]
    public static class ShiftOperatorsHelper<TSelf, TResult>
        where TSelf : IShiftOperators<TSelf, TResult>
    {
        public static TResult op_LeftShift(TSelf value, int shiftAmount) => value << shiftAmount;

        public static TResult op_RightShift(TSelf value, int shiftAmount) => value >> shiftAmount;
    }

    [RequiresPreviewFeatures]
    public static class SignedNumberHelper<TSelf>
        where TSelf : ISignedNumber<TSelf>
    {
        public static TSelf NegativeOne => TSelf.NegativeOne;
    }

    [RequiresPreviewFeatures]
    public static class SpanParseableHelper<TSelf>
        where TSelf : ISpanParseable<TSelf>
    {
        public static TSelf Parse(ReadOnlySpan<char> s, IFormatProvider provider) => TSelf.Parse(s, provider);

        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out TSelf result) => TSelf.TryParse(s, provider, out result);
    }

    [RequiresPreviewFeatures]
    public static class SubtractionOperatorsHelper<TSelf, TOther, TResult>
        where TSelf : ISubtractionOperators<TSelf, TOther, TResult>
    {
        public static TResult op_Subtraction(TSelf left, TOther right) => left - right;
    }

    [RequiresPreviewFeatures]
    public static class UnaryNegationOperatorsHelper<TSelf, TResult>
        where TSelf : IUnaryNegationOperators<TSelf, TResult>
    {
        public static TResult op_UnaryNegation(TSelf value) => -value;
    }

    [RequiresPreviewFeatures]
    public static class UnaryPlusOperatorsHelper<TSelf, TResult>
        where TSelf : IUnaryPlusOperators<TSelf, TResult>
    {
        public static TResult op_UnaryPlus(TSelf value) => +value;
    }
}
