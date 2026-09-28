// Finding: Complex<T>.Exp computes e^x and cis(y) separately (FromPolarCoordinates(T.Exp(x), y)), and Sinh, Cosh, Sin, Cos and
// Pow follow the same pattern. Once e^x overflows, infinity times zero turns a zero component into NaN, so a real input gives a
// NaN imaginary part (Exp(710) = (Infinity, NaN) instead of (Infinity, 0)), and infinity times a small cos/sin gives a spurious
// infinity where the true value is finite. Complex<float> and Complex<Half> hit it at much smaller inputs (Exp(89f), Exp(12)),
// and the non-generic System.Numerics.Complex delegates to Complex<double>, so it has the same results.
// Run: dotnet run 29-Complex-OverflowNaN.cs
using System.Globalization;
using System.Numerics;

bool reproduced = false;

Check("Complex<double>.Exp(710 + 0i)", Complex<double>.Exp(new(710, 0)), double.PositiveInfinity, 0);
Check("Complex.Exp(710 + 0i)", ToGeneric(Complex.Exp(new Complex(710, 0))), double.PositiveInfinity, 0);
Check("Complex<double>.Sinh(711 + 0i)", Complex<double>.Sinh(new(711, 0)), double.PositiveInfinity, 0);
Check("Complex<double>.Cosh(711 + 0i)", Complex<double>.Cosh(new(711, 0)), double.PositiveInfinity, 0);
Check("Complex<double>.Sin(0 + 711i)", Complex<double>.Sin(new(0, 711)), 0, double.PositiveInfinity);
Check("Complex<double>.Cos(0 + 711i)", Complex<double>.Cos(new(0, 711)), double.PositiveInfinity, 0);
Check("Complex<double>.Pow(1e200 + 0i, 2)", Complex<double>.Pow(new(1e200, 0), 2.0), double.PositiveInfinity, 0);
Check("Complex<float>.Exp(89 + 0i)", Widen(Complex<float>.Exp(new(89f, 0f))), double.PositiveInfinity, 0);
Check("Complex<Half>.Exp(12 + 0i)", Widen(Complex<Half>.Exp(new((Half)12, (Half)0))), double.PositiveInfinity, 0);

// e^710 * cos(1.5707963267948966) = 2.23e308 * 6.12e-17 = 1.37e292, well inside the double range.
double y = 1.5707963267948966;
double expectedReal = Math.Exp(355) * Math.Cos(y) * Math.Exp(355);
Check("Complex<double>.Exp(710 + 1.5707963267948966i)", Complex<double>.Exp(new(710, y)), expectedReal, double.PositiveInfinity);

Console.WriteLine(reproduced ? "REPRODUCED" : "NOT REPRODUCED");

void Check(string what, Complex<double> actual, double expectedRe, double expectedIm)
{
    bool ok = Close(actual.Real, expectedRe) && Close(actual.Imaginary, expectedIm);
    reproduced |= !ok;
    Console.WriteLine($"{what,-48} = ({F(actual.Real)}, {F(actual.Imaginary)}), expected ({F(expectedRe)}, {F(expectedIm)}){(ok ? "" : "   <-- wrong")}");
}

static bool Close(double a, double e) => double.IsFinite(e) ? double.IsFinite(a) && Math.Abs(a - e) <= 1e-12 * Math.Max(1, Math.Abs(e)) : a == e;
static string F(double v) => v.ToString("G6", CultureInfo.InvariantCulture);
static Complex<double> ToGeneric(Complex z) => new(z.Real, z.Imaginary);
static Complex<double> Widen<T>(Complex<T> z) where T : IFloatingPointIeee754<T>, IMinMaxValue<T> => new(double.CreateTruncating(z.Real), double.CreateTruncating(z.Imaginary));
