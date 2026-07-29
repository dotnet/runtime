using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint addOne(uint x) => x + 1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint s390xHw() => addOne(10);

    static int Main() => s390xHw() == 11 ? 0 : 1;
}
