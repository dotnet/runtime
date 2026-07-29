using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static double addTwo(double a, double b) => a + b;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static double s390xHw() => addTwo(11.0, 22.0);

    static int Main() => s390xHw() == 33.0 ? 0 : 1;
}
