using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static double multipleArgs(double a, double b, double c, double d,
                               double e, double f, double g, double h,
                               double i) => a + b + c + d + e + f + g + h + i;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int s390xHw() => multipleArgs(1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0) == 45.0 ? 0 : 1;

    static int Main() => s390xHw();
}
