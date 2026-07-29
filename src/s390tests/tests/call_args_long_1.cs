using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long addOne(long x) => x + 1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long s390xHw() => addOne(10);

    static int Main() => s390xHw() == 11 ? 0 : 1;
}
