// Finding: Complex<T>.Tan and Tanh compute sin(2x) / (cos(2x) + cosh(2y)) (and the hyperbolic mirror). At the representable
// point nearest a pole the denominator cancels to exactly zero, so Tan(pi/2) is (Infinity, NaN) although Math.Tan(pi/2) is
// 1.633e16; and for huge real arguments 2x overflows, so Tan(1e308) is (NaN, NaN) although Math.Tan(1e308) is finite. The same
// happens for Complex<float>/Complex<Half> at their own nearest-pole values, and for Tanh on the imaginary axis. The non-generic
// Complex delegates to Complex<double> and gives the same results.
// Run: dotnet run 31-Complex-TanPoles.cs
using System.Globalization;
using System.Numerics;

bool reproduced = false;
double halfPi = Math.PI / 2;

Check("Complex<double>.Tan(pi/2 + 0i)", Complex<double>.Tan(new(halfPi, 0)), Math.Tan(halfPi), 0);
Check("Complex.Tan(pi/2 + 0i)", ToGeneric(Complex.Tan(new Complex(halfPi, 0))), Math.Tan(halfPi), 0);
Check("Complex<double>.Tanh(0 + i pi/2)", Complex<double>.Tanh(new(0, halfPi)), 0, Math.Tan(halfPi));
Check("Complex<double>.Tan(1e308 + 0i)", Complex<double>.Tan(new(1e308, 0)), Math.Tan(1e308), 0);
Check("Complex<double>.Tanh(0 + 1e308i)", Complex<double>.Tanh(new(0, 1e308)), 0, Math.Tan(1e308));
float f = -1.5707964f;
Complex<float> tf = Complex<float>.Tan(new(f, 0));
Check("Complex<float>.Tan(-1.5707964f + 0i)", new(tf.Real, tf.Imaginary), MathF.Tan(f), 0);

Console.WriteLine(reproduced ? "REPRODUCED" : "NOT REPRODUCED");

void Check(string what, Complex<double> actual, double expectedRe, double expectedIm)
{
    bool ok = Math.Abs(actual.Real - expectedRe) <= 1e-6 * Math.Max(1, Math.Abs(expectedRe))
        && Math.Abs(actual.Imaginary - expectedIm) <= 1e-6 * Math.Max(1, Math.Abs(expectedIm));
    reproduced |= !ok;
    Console.WriteLine($"{what,-40} = ({F(actual.Real)}, {F(actual.Imaginary)}), expected ({F(expectedRe)}, {F(expectedIm)}){(ok ? "" : "   <-- wrong")}");
}

static string F(double v) => v.ToString("G6", CultureInfo.InvariantCulture);
static Complex<double> ToGeneric(Complex z) => new(z.Real, z.Imaginary);
