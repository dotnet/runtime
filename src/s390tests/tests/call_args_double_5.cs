using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static double multipleArgs(double a, double b, double c, double d, double e) => a + b + c + d + e;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int s390xHw() => multipleArgs(1.0, 2.0, 3.0, 4.0, 5.0) == 15.0 ? 0 : 1;

    static int Main() => s390xHw();
}
