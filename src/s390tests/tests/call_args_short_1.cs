using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static short addOne(short x) => (short)(x + 1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static short s390xHw() => addOne(10);

    static int Main() => s390xHw() == 11 ? 0 : 1;
}
