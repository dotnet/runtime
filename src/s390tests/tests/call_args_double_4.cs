using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static double addFour(double a, double b, double c, double d) => a + b + c + d;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static double s390xHw() => addFour(1.0, 2.0, 3.0, 4.0);

    static int Main() => s390xHw() == 10.0 ? 0 : 1;
}
