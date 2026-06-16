using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int oneArg(int a, int b, int c, int d, int e) => a + b + c + d + e;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int s390xHw() => oneArg(1, 2, 3, 4, 5);

    static int Main() => s390xHw() == 15 ? 0 : 1;
}
