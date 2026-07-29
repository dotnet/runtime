using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static double addOne(double x) => x + 1.0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static double s390xHw() => addOne(10.0);

    static int Main() => s390xHw() == 11.0 ? 0 : 1;
}
