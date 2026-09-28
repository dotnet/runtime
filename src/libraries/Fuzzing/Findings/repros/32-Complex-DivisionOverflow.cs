// Finding: Complex<T> division uses Smith's formula without scaling, so when the divisor is close to MaxValue the denominator
// c + d * (d / c) overflows to infinity and the quotient collapses to zero. (1e308 + 1i) / (1e308 + 1e308i) is 0.5 - 0.5i, but
// comes back as (0, -0). Complex<float> has the same problem near float.MaxValue, and the non-generic Complex delegates to
// Complex<double>. Scaled variants of Smith's method (Priest, Baudin & Smith) avoid it.
// Run: dotnet run 32-Complex-DivisionOverflow.cs
using System.Globalization;
using System.Numerics;

bool reproduced = false;

Check("(1e308 + 1i) / (1e308 + 1e308i)", new Complex<double>(1e308, 1) / new Complex<double>(1e308, 1e308), 0.5, -0.5);
Complex q = new Complex(1e308, 1) / new Complex(1e308, 1e308);
Check("Complex: (1e308 + 1i) / (1e308 + 1e308i)", new(q.Real, q.Imaginary), 0.5, -0.5);
Check("(1e308 + 0i) / (1e308 + 1e308i)", new Complex<double>(1e308, 0) / new Complex<double>(1e308, 1e308), 0.5, -0.5);
Complex<float> qf = new Complex<float>(-3.4028235E+38f, 0) / new Complex<float>(-3.4028235E+38f, 1E+38f);
Check("Complex<float>: (-3.4e38 + 0i) / (-3.4e38 + 1e38i)", new(qf.Real, qf.Imaginary), 0.9205037409795793, 0.2705117443331786);

Console.WriteLine(reproduced ? "REPRODUCED" : "NOT REPRODUCED");

void Check(string what, Complex<double> actual, double expectedRe, double expectedIm)
{
    bool ok = Math.Abs(actual.Real - expectedRe) <= 1e-6 && Math.Abs(actual.Imaginary - expectedIm) <= 1e-6;
    reproduced |= !ok;
    Console.WriteLine($"{what,-52} = ({F(actual.Real)}, {F(actual.Imaginary)}), expected ({F(expectedRe)}, {F(expectedIm)}){(ok ? "" : "   <-- wrong")}");
}

static string F(double v) => v == 0 && double.IsNegative(v) ? "-0" : v.ToString("G6", CultureInfo.InvariantCulture);
