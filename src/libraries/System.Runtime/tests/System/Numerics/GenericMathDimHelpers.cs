// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Numerics.Tests
{
    public struct BinaryNumberDimHelper : IBinaryNumber<BinaryNumberDimHelper>
    {
        public int Value = 0;

        private BinaryNumberDimHelper(int value)
        {
            Value = value;
        }

        static BinaryNumberDimHelper INumberBase<BinaryNumberDimHelper>.Zero => new BinaryNumberDimHelper(0);

        static BinaryNumberDimHelper IBitwiseOperators<BinaryNumberDimHelper, BinaryNumberDimHelper, BinaryNumberDimHelper>.operator ~(BinaryNumberDimHelper value)
        {
            return new BinaryNumberDimHelper(~value.Value);
        }

        //
        // The below are all not used for existing Dim tests, so they stay unimplemented
        //

        static BinaryNumberDimHelper IMultiplicativeIdentity<BinaryNumberDimHelper, BinaryNumberDimHelper>.MultiplicativeIdentity => throw new NotImplementedException();
        static BinaryNumberDimHelper INumberBase<BinaryNumberDimHelper>.One => throw new NotImplementedException();
        static int INumberBase<BinaryNumberDimHelper>.Radix => throw new NotImplementedException();
        static BinaryNumberDimHelper IAdditiveIdentity<BinaryNumberDimHelper, BinaryNumberDimHelper>.AdditiveIdentity => throw new NotImplementedException();
        static BinaryNumberDimHelper INumberBase<BinaryNumberDimHelper>.Abs(BinaryNumberDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<BinaryNumberDimHelper>.IsCanonical(BinaryNumberDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<BinaryNumberDimHelper>.IsComplexNumber(BinaryNumberDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<BinaryNumberDimHelper>.IsEvenInteger(BinaryNumberDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<BinaryNumberDimHelper>.IsFinite(BinaryNumberDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<BinaryNumberDimHelper>.IsImaginaryNumber(BinaryNumberDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<BinaryNumberDimHelper>.IsInfinity(BinaryNumberDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<BinaryNumberDimHelper>.IsInteger(BinaryNumberDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<BinaryNumberDimHelper>.IsNaN(BinaryNumberDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<BinaryNumberDimHelper>.IsNegative(BinaryNumberDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<BinaryNumberDimHelper>.IsNegativeInfinity(BinaryNumberDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<BinaryNumberDimHelper>.IsNormal(BinaryNumberDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<BinaryNumberDimHelper>.IsOddInteger(BinaryNumberDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<BinaryNumberDimHelper>.IsPositive(BinaryNumberDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<BinaryNumberDimHelper>.IsPositiveInfinity(BinaryNumberDimHelper value) => throw new NotImplementedException();
        static bool IBinaryNumber<BinaryNumberDimHelper>.IsPow2(BinaryNumberDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<BinaryNumberDimHelper>.IsRealNumber(BinaryNumberDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<BinaryNumberDimHelper>.IsSubnormal(BinaryNumberDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<BinaryNumberDimHelper>.IsZero(BinaryNumberDimHelper value) => throw new NotImplementedException();
        static BinaryNumberDimHelper IBinaryNumber<BinaryNumberDimHelper>.Log2(BinaryNumberDimHelper value) => throw new NotImplementedException();
        static BinaryNumberDimHelper INumberBase<BinaryNumberDimHelper>.MaxMagnitude(BinaryNumberDimHelper x, BinaryNumberDimHelper y) => throw new NotImplementedException();
        static BinaryNumberDimHelper INumberBase<BinaryNumberDimHelper>.MaxMagnitudeNumber(BinaryNumberDimHelper x, BinaryNumberDimHelper y) => throw new NotImplementedException();
        static BinaryNumberDimHelper INumberBase<BinaryNumberDimHelper>.MinMagnitude(BinaryNumberDimHelper x, BinaryNumberDimHelper y) => throw new NotImplementedException();
        static BinaryNumberDimHelper INumberBase<BinaryNumberDimHelper>.MinMagnitudeNumber(BinaryNumberDimHelper x, BinaryNumberDimHelper y) => throw new NotImplementedException();
        static BinaryNumberDimHelper INumberBase<BinaryNumberDimHelper>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => throw new NotImplementedException();
        static BinaryNumberDimHelper INumberBase<BinaryNumberDimHelper>.Parse(string s, NumberStyles style, IFormatProvider? provider) => throw new NotImplementedException();
        static BinaryNumberDimHelper ISpanParsable<BinaryNumberDimHelper>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => throw new NotImplementedException();
        static BinaryNumberDimHelper IParsable<BinaryNumberDimHelper>.Parse(string s, IFormatProvider? provider) => throw new NotImplementedException();
        static bool INumberBase<BinaryNumberDimHelper>.TryConvertToChecked<TOther>(BinaryNumberDimHelper value, out TOther result) where TOther : default => throw new NotImplementedException();
        static bool INumberBase<BinaryNumberDimHelper>.TryConvertToSaturating<TOther>(BinaryNumberDimHelper value, out TOther result) where TOther : default => throw new NotImplementedException();
        static bool INumberBase<BinaryNumberDimHelper>.TryConvertToTruncating<TOther>(BinaryNumberDimHelper value, out TOther result) where TOther : default => throw new NotImplementedException();
        static bool INumberBase<BinaryNumberDimHelper>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out BinaryNumberDimHelper result) => throw new NotImplementedException();
        static bool INumberBase<BinaryNumberDimHelper>.TryParse(string? s, NumberStyles style, IFormatProvider? provider, out BinaryNumberDimHelper result) => throw new NotImplementedException();
        static bool ISpanParsable<BinaryNumberDimHelper>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out BinaryNumberDimHelper result) => throw new NotImplementedException();
        static bool IParsable<BinaryNumberDimHelper>.TryParse(string? s, IFormatProvider? provider, out BinaryNumberDimHelper result) => throw new NotImplementedException();
        int IComparable.CompareTo(object? obj) => throw new NotImplementedException();
        string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => throw new NotImplementedException();
        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => throw new NotImplementedException();
        int IComparable<BinaryNumberDimHelper>.CompareTo(BinaryNumberDimHelper other) => throw new NotImplementedException();
        static bool INumberBase<BinaryNumberDimHelper>.TryConvertFromChecked<TOther>(TOther value, out BinaryNumberDimHelper result) => throw new NotImplementedException();
        static bool INumberBase<BinaryNumberDimHelper>.TryConvertFromSaturating<TOther>(TOther value, out BinaryNumberDimHelper result) => throw new NotImplementedException();
        static bool INumberBase<BinaryNumberDimHelper>.TryConvertFromTruncating<TOther>(TOther value, out BinaryNumberDimHelper result) => throw new NotImplementedException();
        bool IEquatable<BinaryNumberDimHelper>.Equals(BinaryNumberDimHelper other) => throw new NotImplementedException();

        static BinaryNumberDimHelper IUnaryPlusOperators<BinaryNumberDimHelper, BinaryNumberDimHelper>.operator +(BinaryNumberDimHelper value) => throw new NotImplementedException();
        static BinaryNumberDimHelper IAdditionOperators<BinaryNumberDimHelper, BinaryNumberDimHelper, BinaryNumberDimHelper>.operator +(BinaryNumberDimHelper left, BinaryNumberDimHelper right) => throw new NotImplementedException();
        static BinaryNumberDimHelper IUnaryNegationOperators<BinaryNumberDimHelper, BinaryNumberDimHelper>.operator -(BinaryNumberDimHelper value) => throw new NotImplementedException();
        static BinaryNumberDimHelper ISubtractionOperators<BinaryNumberDimHelper, BinaryNumberDimHelper, BinaryNumberDimHelper>.operator -(BinaryNumberDimHelper left, BinaryNumberDimHelper right) => throw new NotImplementedException();
        static BinaryNumberDimHelper IIncrementOperators<BinaryNumberDimHelper>.operator ++(BinaryNumberDimHelper value) => throw new NotImplementedException();
        static BinaryNumberDimHelper IDecrementOperators<BinaryNumberDimHelper>.operator --(BinaryNumberDimHelper value) => throw new NotImplementedException();
        static BinaryNumberDimHelper IMultiplyOperators<BinaryNumberDimHelper, BinaryNumberDimHelper, BinaryNumberDimHelper>.operator *(BinaryNumberDimHelper left, BinaryNumberDimHelper right) => throw new NotImplementedException();
        static BinaryNumberDimHelper IDivisionOperators<BinaryNumberDimHelper, BinaryNumberDimHelper, BinaryNumberDimHelper>.operator /(BinaryNumberDimHelper left, BinaryNumberDimHelper right) => throw new NotImplementedException();
        static BinaryNumberDimHelper IModulusOperators<BinaryNumberDimHelper, BinaryNumberDimHelper, BinaryNumberDimHelper>.operator %(BinaryNumberDimHelper left, BinaryNumberDimHelper right) => throw new NotImplementedException();
        static BinaryNumberDimHelper IBitwiseOperators<BinaryNumberDimHelper, BinaryNumberDimHelper, BinaryNumberDimHelper>.operator &(BinaryNumberDimHelper left, BinaryNumberDimHelper right) => throw new NotImplementedException();
        static BinaryNumberDimHelper IBitwiseOperators<BinaryNumberDimHelper, BinaryNumberDimHelper, BinaryNumberDimHelper>.operator |(BinaryNumberDimHelper left, BinaryNumberDimHelper right) => throw new NotImplementedException();
        static BinaryNumberDimHelper IBitwiseOperators<BinaryNumberDimHelper, BinaryNumberDimHelper, BinaryNumberDimHelper>.operator ^(BinaryNumberDimHelper left, BinaryNumberDimHelper right) => throw new NotImplementedException();
        static bool IEqualityOperators<BinaryNumberDimHelper, BinaryNumberDimHelper, bool>.operator ==(BinaryNumberDimHelper left, BinaryNumberDimHelper right) => throw new NotImplementedException();
        static bool IEqualityOperators<BinaryNumberDimHelper, BinaryNumberDimHelper, bool>.operator !=(BinaryNumberDimHelper left, BinaryNumberDimHelper right) => throw new NotImplementedException();
        static bool IComparisonOperators<BinaryNumberDimHelper, BinaryNumberDimHelper, bool>.operator <(BinaryNumberDimHelper left, BinaryNumberDimHelper right) => throw new NotImplementedException();
        static bool IComparisonOperators<BinaryNumberDimHelper, BinaryNumberDimHelper, bool>.operator >(BinaryNumberDimHelper left, BinaryNumberDimHelper right) => throw new NotImplementedException();
        static bool IComparisonOperators<BinaryNumberDimHelper, BinaryNumberDimHelper, bool>.operator <=(BinaryNumberDimHelper left, BinaryNumberDimHelper right) => throw new NotImplementedException();
        static bool IComparisonOperators<BinaryNumberDimHelper, BinaryNumberDimHelper, bool>.operator >=(BinaryNumberDimHelper left, BinaryNumberDimHelper right) => throw new NotImplementedException();
    }

    public struct ExponentialFunctionsDimHelper : IExponentialFunctions<ExponentialFunctionsDimHelper>
    {
        public float Value;

        public ExponentialFunctionsDimHelper(float value)
        {
            Value = value;
        }


        static ExponentialFunctionsDimHelper IExponentialFunctions<ExponentialFunctionsDimHelper>.Exp10(ExponentialFunctionsDimHelper x) => new ExponentialFunctionsDimHelper(Single.Exp10(x.Value));
        static ExponentialFunctionsDimHelper INumberBase<ExponentialFunctionsDimHelper>.One => new ExponentialFunctionsDimHelper(1f);
        static ExponentialFunctionsDimHelper ISubtractionOperators<ExponentialFunctionsDimHelper, ExponentialFunctionsDimHelper, ExponentialFunctionsDimHelper>.operator -(ExponentialFunctionsDimHelper left, ExponentialFunctionsDimHelper right)
            => new ExponentialFunctionsDimHelper(left.Value - right.Value);

        //
        //  The below are all not used for existing Dim tests, so they stay unimplemented
        //

        static ExponentialFunctionsDimHelper IFloatingPointConstants<ExponentialFunctionsDimHelper>.E => throw new NotImplementedException();
        static ExponentialFunctionsDimHelper IFloatingPointConstants<ExponentialFunctionsDimHelper>.Pi => throw new NotImplementedException();
        static ExponentialFunctionsDimHelper IFloatingPointConstants<ExponentialFunctionsDimHelper>.Tau => throw new NotImplementedException();
        static int INumberBase<ExponentialFunctionsDimHelper>.Radix => throw new NotImplementedException();
        static ExponentialFunctionsDimHelper INumberBase<ExponentialFunctionsDimHelper>.Zero => throw new NotImplementedException();
        static ExponentialFunctionsDimHelper IAdditiveIdentity<ExponentialFunctionsDimHelper, ExponentialFunctionsDimHelper>.AdditiveIdentity => throw new NotImplementedException();
        static ExponentialFunctionsDimHelper IMultiplicativeIdentity<ExponentialFunctionsDimHelper, ExponentialFunctionsDimHelper>.MultiplicativeIdentity => throw new NotImplementedException();

        static ExponentialFunctionsDimHelper INumberBase<ExponentialFunctionsDimHelper>.Abs(ExponentialFunctionsDimHelper value) => throw new NotImplementedException();
        static ExponentialFunctionsDimHelper IExponentialFunctions<ExponentialFunctionsDimHelper>.Exp(ExponentialFunctionsDimHelper x) => throw new NotImplementedException();
        static ExponentialFunctionsDimHelper IExponentialFunctions<ExponentialFunctionsDimHelper>.Exp2(ExponentialFunctionsDimHelper x) => throw new NotImplementedException();
        static bool INumberBase<ExponentialFunctionsDimHelper>.IsCanonical(ExponentialFunctionsDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<ExponentialFunctionsDimHelper>.IsComplexNumber(ExponentialFunctionsDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<ExponentialFunctionsDimHelper>.IsEvenInteger(ExponentialFunctionsDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<ExponentialFunctionsDimHelper>.IsFinite(ExponentialFunctionsDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<ExponentialFunctionsDimHelper>.IsImaginaryNumber(ExponentialFunctionsDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<ExponentialFunctionsDimHelper>.IsInfinity(ExponentialFunctionsDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<ExponentialFunctionsDimHelper>.IsInteger(ExponentialFunctionsDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<ExponentialFunctionsDimHelper>.IsNaN(ExponentialFunctionsDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<ExponentialFunctionsDimHelper>.IsNegative(ExponentialFunctionsDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<ExponentialFunctionsDimHelper>.IsNegativeInfinity(ExponentialFunctionsDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<ExponentialFunctionsDimHelper>.IsNormal(ExponentialFunctionsDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<ExponentialFunctionsDimHelper>.IsOddInteger(ExponentialFunctionsDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<ExponentialFunctionsDimHelper>.IsPositive(ExponentialFunctionsDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<ExponentialFunctionsDimHelper>.IsPositiveInfinity(ExponentialFunctionsDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<ExponentialFunctionsDimHelper>.IsRealNumber(ExponentialFunctionsDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<ExponentialFunctionsDimHelper>.IsSubnormal(ExponentialFunctionsDimHelper value) => throw new NotImplementedException();
        static bool INumberBase<ExponentialFunctionsDimHelper>.IsZero(ExponentialFunctionsDimHelper value) => throw new NotImplementedException();
        static ExponentialFunctionsDimHelper INumberBase<ExponentialFunctionsDimHelper>.MaxMagnitude(ExponentialFunctionsDimHelper x, ExponentialFunctionsDimHelper y) => throw new NotImplementedException();
        static ExponentialFunctionsDimHelper INumberBase<ExponentialFunctionsDimHelper>.MaxMagnitudeNumber(ExponentialFunctionsDimHelper x, ExponentialFunctionsDimHelper y) => throw new NotImplementedException();
        static ExponentialFunctionsDimHelper INumberBase<ExponentialFunctionsDimHelper>.MinMagnitude(ExponentialFunctionsDimHelper x, ExponentialFunctionsDimHelper y) => throw new NotImplementedException();
        static ExponentialFunctionsDimHelper INumberBase<ExponentialFunctionsDimHelper>.MinMagnitudeNumber(ExponentialFunctionsDimHelper x, ExponentialFunctionsDimHelper y) => throw new NotImplementedException();
        static ExponentialFunctionsDimHelper INumberBase<ExponentialFunctionsDimHelper>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => throw new NotImplementedException();
        static ExponentialFunctionsDimHelper INumberBase<ExponentialFunctionsDimHelper>.Parse(string s, NumberStyles style, IFormatProvider? provider) => throw new NotImplementedException();
        static ExponentialFunctionsDimHelper ISpanParsable<ExponentialFunctionsDimHelper>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => throw new NotImplementedException();
        static ExponentialFunctionsDimHelper IParsable<ExponentialFunctionsDimHelper>.Parse(string s, IFormatProvider? provider) => throw new NotImplementedException();
        static bool INumberBase<ExponentialFunctionsDimHelper>.TryConvertFromChecked<TOther>(TOther value, out ExponentialFunctionsDimHelper result) => throw new NotImplementedException();
        static bool INumberBase<ExponentialFunctionsDimHelper>.TryConvertFromSaturating<TOther>(TOther value, out ExponentialFunctionsDimHelper result) => throw new NotImplementedException();
        static bool INumberBase<ExponentialFunctionsDimHelper>.TryConvertFromTruncating<TOther>(TOther value, out ExponentialFunctionsDimHelper result) => throw new NotImplementedException();
        static bool INumberBase<ExponentialFunctionsDimHelper>.TryConvertToChecked<TOther>(ExponentialFunctionsDimHelper value, out TOther result) => throw new NotImplementedException();
        static bool INumberBase<ExponentialFunctionsDimHelper>.TryConvertToSaturating<TOther>(ExponentialFunctionsDimHelper value, out TOther result) => throw new NotImplementedException();
        static bool INumberBase<ExponentialFunctionsDimHelper>.TryConvertToTruncating<TOther>(ExponentialFunctionsDimHelper value, out TOther result) => throw new NotImplementedException();
        static bool INumberBase<ExponentialFunctionsDimHelper>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out ExponentialFunctionsDimHelper result) => throw new NotImplementedException();
        static bool INumberBase<ExponentialFunctionsDimHelper>.TryParse(string? s, NumberStyles style, IFormatProvider? provider, out ExponentialFunctionsDimHelper result) => throw new NotImplementedException();
        static bool ISpanParsable<ExponentialFunctionsDimHelper>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out ExponentialFunctionsDimHelper result) => throw new NotImplementedException();
        static bool IParsable<ExponentialFunctionsDimHelper>.TryParse(string? s, IFormatProvider? provider, out ExponentialFunctionsDimHelper result) => throw new NotImplementedException();
        bool IEquatable<ExponentialFunctionsDimHelper>.Equals(ExponentialFunctionsDimHelper other) => throw new NotImplementedException();
        string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => throw new NotImplementedException();
        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => throw new NotImplementedException();

        static ExponentialFunctionsDimHelper IUnaryPlusOperators<ExponentialFunctionsDimHelper, ExponentialFunctionsDimHelper>.operator +(ExponentialFunctionsDimHelper value) => throw new NotImplementedException();
        static ExponentialFunctionsDimHelper IAdditionOperators<ExponentialFunctionsDimHelper, ExponentialFunctionsDimHelper, ExponentialFunctionsDimHelper>.operator +(ExponentialFunctionsDimHelper left, ExponentialFunctionsDimHelper right) => throw new NotImplementedException();
        static ExponentialFunctionsDimHelper IUnaryNegationOperators<ExponentialFunctionsDimHelper, ExponentialFunctionsDimHelper>.operator -(ExponentialFunctionsDimHelper value) => throw new NotImplementedException();
        static ExponentialFunctionsDimHelper IIncrementOperators<ExponentialFunctionsDimHelper>.operator ++(ExponentialFunctionsDimHelper value) => throw new NotImplementedException();
        static ExponentialFunctionsDimHelper IDecrementOperators<ExponentialFunctionsDimHelper>.operator --(ExponentialFunctionsDimHelper value) => throw new NotImplementedException();
        static ExponentialFunctionsDimHelper IMultiplyOperators<ExponentialFunctionsDimHelper, ExponentialFunctionsDimHelper, ExponentialFunctionsDimHelper>.operator *(ExponentialFunctionsDimHelper left, ExponentialFunctionsDimHelper right) => throw new NotImplementedException();
        static ExponentialFunctionsDimHelper IDivisionOperators<ExponentialFunctionsDimHelper, ExponentialFunctionsDimHelper, ExponentialFunctionsDimHelper>.operator /(ExponentialFunctionsDimHelper left, ExponentialFunctionsDimHelper right) => throw new NotImplementedException();
        static bool IEqualityOperators<ExponentialFunctionsDimHelper, ExponentialFunctionsDimHelper, bool>.operator ==(ExponentialFunctionsDimHelper left, ExponentialFunctionsDimHelper right) => throw new NotImplementedException();
        static bool IEqualityOperators<ExponentialFunctionsDimHelper, ExponentialFunctionsDimHelper, bool>.operator !=(ExponentialFunctionsDimHelper left, ExponentialFunctionsDimHelper right) => throw new NotImplementedException();
    }
}
