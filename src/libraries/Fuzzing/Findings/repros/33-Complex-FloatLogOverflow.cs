// Finding: for Complex<float> and Complex<Half>, Log and Log10 compute log(|z|) with a magnitude that overflows once |z| exceeds
// MaxValue, and Atan returns NaN for such inputs. Complex<double> scales and gets the right answers for the equivalent inputs, so
// the generic code isn't doing the same for the smaller types. Log(2.5e38 + 2.5e38i) should be 88.76 + 0.785i, not Infinity.
// Run: dotnet run 33-Complex-FloatLogOverflow.cs
using System.Globalization;
using System.Numerics;

bool reproduced = false;
float m = float.MaxValue;

Check("Complex<float>.Log(2.5e38 + 2.5e38i)", Widen(Complex<float>.Log(new(2.5e38f, 2.5e38f))), Complex<double>.Log(new(2.5e38, 2.5e38)));
Check("Complex<float>.Log10(Max + Max i)", Widen(Complex<float>.Log10(new(m, m))), Complex<double>.Log10(new(m, m)));
Check("Complex<float>.Atan(Max + Max i)", Widen(Complex<float>.Atan(new(m, m))), Complex<double>.Atan(new(m, m)));
Check("Complex<Half>.Log(60000 + 60000i)", Widen(Complex<Half>.Log(new((Half)60000, (Half)60000))), Complex<double>.Log(new(60000, 60000)));
Console.WriteLine("For comparison, Complex<double>.Log(1e308 + 1e308i) = " + Complex<double>.Log(new(1e308, 1e308)));

Console.WriteLine(reproduced ? "REPRODUCED" : "NOT REPRODUCED");

void Check(string what, Complex<double> actual, Complex<double> expected)
{
    bool ok = Math.Abs(actual.Real - expected.Real) <= 1e-2 * Math.Max(1, Math.Abs(expected.Real))
        && Math.Abs(actual.Imaginary - expected.Imaginary) <= 1e-2 * Math.Max(1, Math.Abs(expected.Imaginary));
    reproduced |= !ok;
    Console.WriteLine($"{what,-40} = ({F(actual.Real)}, {F(actual.Imaginary)}), expected ({F(expected.Real)}, {F(expected.Imaginary)}){(ok ? "" : "   <-- wrong")}");
}

static string F(double v) => v.ToString("G6", CultureInfo.InvariantCulture);
static Complex<double> Widen<T>(Complex<T> z) where T : IFloatingPointIeee754<T>, IMinMaxValue<T> => new(double.CreateTruncating(z.Real), double.CreateTruncating(z.Imaginary));
