// Finding: Complex<T>.Sqrt(0 + yi) with |y| = T.Epsilon (the smallest subnormal) returns (0, Infinity). The result should be
// about sqrt(|y| / 2) * (1 + i), e.g. 1.57e-162 + 1.57e-162i for y = 5e-324. y / 2 underflows to zero, so the real part is 0
// and the imaginary part, computed as y / (2 * real), is infinite. It happens for double, float and Half, and the non-generic
// Complex delegates to Complex<double>. The next subnormal up (2 * Epsilon) already works.
// Run: dotnet run 43-Complex-SqrtSubnormal.cs
using System.Globalization;
using System.Numerics;

bool reproduced = false;
foreach (double y in new[] { double.Epsilon, 1e-323, 2e-323, 1e-322, 1e-321, 1e-320, 1e-310 })
{
    Complex<double> r = Complex<double>.Sqrt(new(0, y));
    double expected = Math.ScaleB(Math.Sqrt(Math.ScaleB(y, 100) / 2), -50);
    bool ok = double.IsFinite(r.Imaginary) && Math.Abs(r.Imaginary - expected) <= 1e-3 * expected;
    reproduced |= !ok;
    Console.WriteLine($"Complex<double>.Sqrt(0 + {y.ToString("G3", CultureInfo.InvariantCulture)}i) = ({r.Real:G6}, {r.Imaginary:G6}), expected about ({expected:G6}, {expected:G6}){(ok ? "" : "   <-- wrong")}");
}

Complex rc = Complex.Sqrt(new Complex(0, double.Epsilon));
Console.WriteLine($"Complex.Sqrt(0 + 5e-324i) = ({rc.Real:G6}, {rc.Imaginary:G6})");
Complex<float> rf = Complex<float>.Sqrt(new(0, float.Epsilon));
Console.WriteLine($"Complex<float>.Sqrt(0 + 1.4e-45i) = ({rf.Real:G6}, {rf.Imaginary:G6}), expected about (8.4e-23, 8.4e-23)");
Complex<Half> rh = Complex<Half>.Sqrt(new(Half.Zero, Half.Epsilon));
Console.WriteLine($"Complex<Half>.Sqrt(0 + 6e-8i) = ({rh.Real}, {rh.Imaginary}), expected about (0.000173, 0.000173)");
reproduced |= !float.IsFinite(rf.Imaginary) || !Half.IsFinite(rh.Imaginary);
Console.WriteLine(reproduced ? "REPRODUCED" : "NOT REPRODUCED");
