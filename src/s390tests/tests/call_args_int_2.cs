using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int addTwo(int a, int b) => a + b;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int s390xHw() => addTwo(11, 22);

    static int Main() => s390xHw() == 33 ? 0 : 1;
}
