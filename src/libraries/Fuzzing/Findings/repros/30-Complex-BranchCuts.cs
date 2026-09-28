// Finding: Complex<T>.Asin, Acos and Atan land on the wrong side of their branch cuts. Asin and Acos ignore the sign of a zero
// imaginary part for real inputs with |x| > 1, so Asin(x - 0i) and Acos(-x + 0i) come out with the wrong sign of the imaginary
// part. Atan on the imaginary axis with |y| > 1 takes the sign of the real part from y instead of from the signed zero real part,
// so Atan(-0 + 1.5i) is +pi/2 + ... instead of -pi/2 + .... C23 Annex G (G.6.2) requires casin/cacos/catan to be continuous onto
// the cut from the side the zero's sign picks and casin(conj z) = conj(casin z), which the Complex<T> Annex G work
// (dotnet/runtime#131132) aims to follow. Expected values are from C99/CPython cmath. The non-generic Complex delegates to
// Complex<double> and gives the same answers.
// Run: dotnet run 30-Complex-BranchCuts.cs
using System.Globalization;
using System.Numerics;

bool reproduced = false;

// Asin(x - 0i), |x| > 1: the imaginary part must be negative.
Check("Asin(1.5 - 0i)", Complex<double>.Asin(new(1.5, -0.0)), 1.5707963267948966, -0.9624236501192069);
Check("Asin(-3 - 0i)", Complex<double>.Asin(new(-3, -0.0)), -1.5707963267948966, -1.7627471740390860);
// Acos(x + 0i), x < -1: the imaginary part must be negative.
Check("Acos(-1.5 + 0i)", Complex<double>.Acos(new(-1.5, 0.0)), Math.PI, -0.9624236501192069);
Check("Complex.Acos(-3 + 0i)", ToGeneric(Complex.Acos(new Complex(-3, 0.0))), Math.PI, -1.7627471740390860);
// Atan(+-0 + iy), |y| > 1: the real part takes the sign of the zero.
Check("Atan(-0 + 1.5i)", Complex<double>.Atan(new(-0.0, 1.5)), -1.5707963267948966, 0.8047189562170502);
Check("Atan(+0 - 11.1i)", Complex<double>.Atan(new(0.0, -11.1)), 1.5707963267948966, -0.0903350143777408);
// The same with float.
Check("Complex<float>.Asin(88.7f - 0i)", Widen(Complex<float>.Asin(new(88.7f, -0f))), 1.5707963705062866, -5.178375);

Console.WriteLine(reproduced ? "REPRODUCED" : "NOT REPRODUCED");

void Check(string what, Complex<double> actual, double expectedRe, double expectedIm)
{
    bool ok = Close(actual.Real, expectedRe) && Close(actual.Imaginary, expectedIm);
    reproduced |= !ok;
    Console.WriteLine($"{what,-32} = ({F(actual.Real)}, {F(actual.Imaginary)}), expected ({F(expectedRe)}, {F(expectedIm)}){(ok ? "" : "   <-- wrong")}");
}

static bool Close(double a, double e) => Math.Abs(a - e) <= 1e-6 * Math.Max(1, Math.Abs(e));
static string F(double v) => v.ToString("G8", CultureInfo.InvariantCulture);
static Complex<double> ToGeneric(Complex z) => new(z.Real, z.Imaginary);
static Complex<double> Widen<T>(Complex<T> z) where T : IFloatingPointIeee754<T>, IMinMaxValue<T> => new(double.CreateTruncating(z.Real), double.CreateTruncating(z.Imaginary));
