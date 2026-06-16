using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int oneArg(int x) => x + 1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int s390xHw() => oneArg(10);

    static int Main() => s390xHw() == 11 ? 0 : 1;
}
