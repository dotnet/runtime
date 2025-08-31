#pragma warning disable SYSLIB5001

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Numerics.Tensors;

var arr = new MyComplex[] { new(new(1, 2)), new(new(3, 4)) };
var tensor = Tensor.Create(arr);
Console.WriteLine("TensorPrimitives.StdDev: " + TensorPrimitives.StdDev<MyComplex>(arr).ToString());
Console.WriteLine("Tensor.StdDev:           " + Tensor.StdDev(tensor.AsReadOnlyTensorSpan()).ToString());

public readonly struct MyComplex(Complex value) : IRootFunctions<MyComplex>
{
    private readonly Complex _value = value;
    public static MyComplex operator +(MyComplex left, MyComplex right) => new(left._value + right._value);
    public static MyComplex operator *(MyComplex left, MyComplex right) => new(left._value * right._value);
    public static MyComplex operator /(MyComplex left, MyComplex right) => new(left._value / right._value);
    public static MyComplex operator -(MyComplex left, MyComplex right) => new(left._value - right._value);
    public static MyComplex Sqrt(MyComplex x) => new(Complex.Sqrt(x._value));
    public static MyComplex Abs(MyComplex value) => new(new(Complex.Abs(value._value), 0));
    public static MyComplex AdditiveIdentity => new(Complex.Zero);
    public static MyComplex CreateChecked<TOther>(TOther value) where TOther : INumberBase<TOther> => new(Complex.CreateChecked(value));
    public override string ToString() => _value.ToString();
    
    // Everything below just throws NotImplementedException

    public bool Equals(MyComplex other) => throw new NotImplementedException();
    public string ToString(string? format, IFormatProvider? formatProvider) => throw new NotImplementedException();
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => throw new NotImplementedException();
    public static MyComplex Parse(string s, IFormatProvider? provider) => throw new NotImplementedException();
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out MyComplex result) => throw new NotImplementedException();
    public static MyComplex Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => throw new NotImplementedException();
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out MyComplex result) => throw new NotImplementedException();
    public static MyComplex operator --(MyComplex value) => throw new NotImplementedException();
    public static bool operator ==(MyComplex left, MyComplex right) => throw new NotImplementedException();
    public static bool operator !=(MyComplex left, MyComplex right) => throw new NotImplementedException();
    public static MyComplex operator ++(MyComplex value) => throw new NotImplementedException();
    public static MyComplex MultiplicativeIdentity => throw new NotImplementedException();
    public static MyComplex operator -(MyComplex value) => throw new NotImplementedException();
    public static MyComplex operator +(MyComplex value) => throw new NotImplementedException();
    public static bool IsCanonical(MyComplex value) => throw new NotImplementedException();
    public static bool IsComplexNumber(MyComplex value) => throw new NotImplementedException();
    public static bool IsEvenInteger(MyComplex value) => throw new NotImplementedException();
    public static bool IsFinite(MyComplex value) => throw new NotImplementedException();
    public static bool IsImaginaryNumber(MyComplex value) => throw new NotImplementedException();
    public static bool IsInfinity(MyComplex value) => throw new NotImplementedException();
    public static bool IsInteger(MyComplex value) => throw new NotImplementedException();
    public static bool IsNaN(MyComplex value) => throw new NotImplementedException();
    public static bool IsNegative(MyComplex value) => throw new NotImplementedException();
    public static bool IsNegativeInfinity(MyComplex value) => throw new NotImplementedException();
    public static bool IsNormal(MyComplex value) => throw new NotImplementedException();
    public static bool IsOddInteger(MyComplex value) => throw new NotImplementedException();
    public static bool IsPositive(MyComplex value) => throw new NotImplementedException();
    public static bool IsPositiveInfinity(MyComplex value) => throw new NotImplementedException();
    public static bool IsRealNumber(MyComplex value) => throw new NotImplementedException();
    public static bool IsSubnormal(MyComplex value) => throw new NotImplementedException();
    public static bool IsZero(MyComplex value) => throw new NotImplementedException();
    public static MyComplex MaxMagnitude(MyComplex x, MyComplex y) => throw new NotImplementedException();
    public static MyComplex MaxMagnitudeNumber(MyComplex x, MyComplex y) => throw new NotImplementedException();
    public static MyComplex MinMagnitude(MyComplex x, MyComplex y) => throw new NotImplementedException();
    public static MyComplex MinMagnitudeNumber(MyComplex x, MyComplex y) => throw new NotImplementedException();
    public static MyComplex Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => throw new NotImplementedException();
    public static MyComplex Parse(string s, NumberStyles style, IFormatProvider? provider) => throw new NotImplementedException();
    public static bool TryConvertFromSaturating<TOther>(TOther value, out MyComplex result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
    public static bool TryConvertFromTruncating<TOther>(TOther value, out MyComplex result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
    public static bool TryConvertFromChecked<TOther>(TOther value, out MyComplex result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
    public static bool TryConvertToChecked<TOther>(MyComplex value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
    public static bool TryConvertToSaturating<TOther>(MyComplex value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
    public static bool TryConvertToTruncating<TOther>(MyComplex value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
    public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out MyComplex result) => throw new NotImplementedException();
    public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out MyComplex result) => throw new NotImplementedException();
    public static MyComplex One => throw new NotImplementedException();
    public static int Radix => throw new NotImplementedException();
    public static MyComplex Zero => throw new NotImplementedException();
    public static MyComplex E => throw new NotImplementedException();
    public static MyComplex Pi => throw new NotImplementedException();
    public static MyComplex Tau => throw new NotImplementedException();
    public static MyComplex Cbrt(MyComplex x) => throw new NotImplementedException();
    public static MyComplex Hypot(MyComplex x, MyComplex y) => throw new NotImplementedException();
    public static MyComplex RootN(MyComplex x, int n) => throw new NotImplementedException();
}