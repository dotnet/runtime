using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int multipleArgs(int a, int b, int c, int d, int e, int f) => a + b + c + d + e + f;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int s390xHw() => multipleArgs(1, 2, 3, 4, 5, 6) == 21 ? 0 : 1;

    static int Main() => s390xHw();
}
