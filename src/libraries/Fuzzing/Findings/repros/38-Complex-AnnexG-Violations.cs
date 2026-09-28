// Finding: two more C23 Annex G violations in Complex<T> (and the non-generic Complex, which delegates to Complex<double>):
// 1. G.5.2 says a finite value divided by an infinity is zero. The division code only recovers when both result parts are NaN, so
//    a large finite dividend gives (0, NaN): (MaxValue + MaxValue i) / (INF - INF i). Small dividends such as 1 + 1i are fine.
// 2. csqrt(conj z) = conj(csqrt z) and the result's imaginary part takes the sign of y. With a huge negative real part and a
//    subnormal negative imaginary part, Sqrt returns a positive imaginary part (the wrong side of the branch cut):
//    Sqrt(-9.27e307 - 5e-324i) is (-0, +9.628e153) but should be (0, -9.628e153), which is what C99 csqrt and CPython's cmath give.
// Run: dotnet run 38-Complex-AnnexG-Violations.cs
using System.Globalization;
using System.Numerics;

bool reproduced = false;
double inf = double.PositiveInfinity;

Complex<double> q = new Complex<double>(double.MaxValue, double.MaxValue) / new Complex<double>(inf, -inf);
Report("(MaxValue + MaxValue i) / (INF - INF i)", q, "(0, 0)", q.Real == 0 && q.Imaginary == 0);
Complex<double> q1 = new Complex<double>(1, 1) / new Complex<double>(inf, -inf);
Report("(1 + 1i) / (INF - INF i)  [for comparison]", q1, "(0, 0)", q1.Real == 0 && q1.Imaginary == 0);
Complex<float> qf = new Complex<float>(3.4028235E+38f, 3.4028235E+38f) / new Complex<float>(float.NegativeInfinity, float.PositiveInfinity);
Report("Complex<float>: (MaxValue + MaxValue i) / (-INF + INF i)", new(qf.Real, qf.Imaginary), "(0, 0)", qf.Real == 0 && qf.Imaginary == 0);

Complex<double> s = Complex<double>.Sqrt(new(-9.27e307, -5e-324));
Report("Sqrt(-9.27e307 - 5e-324i)", s, "(0, -9.62808e+153)", s.Imaginary < 0);
Complex<double> sp = Complex<double>.Sqrt(new(-9.27e307, 5e-324));
Report("Sqrt(-9.27e307 + 5e-324i)  [for comparison]", sp, "(0, 9.62808e+153)", sp.Imaginary > 0);

Console.WriteLine(reproduced ? "REPRODUCED" : "NOT REPRODUCED");

void Report(string what, Complex<double> actual, string expected, bool ok)
{
    reproduced |= !ok;
    Console.WriteLine($"{what,-56} = ({F(actual.Real)}, {F(actual.Imaginary)}), expected {expected}{(ok ? "" : "   <-- wrong")}");
}

static string F(double v) => v == 0 && double.IsNegative(v) ? "-0" : v.ToString("G6", CultureInfo.InvariantCulture);
