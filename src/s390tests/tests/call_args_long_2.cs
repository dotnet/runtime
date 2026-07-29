using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long addTwo(long a, long b) => a + b;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long s390xHw() => addTwo(11, 22);

    static int Main() => s390xHw() == 33 ? 0 : 1;
}
