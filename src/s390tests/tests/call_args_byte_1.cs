using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte addOne(byte x) => (byte)(x + 1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte s390xHw() => addOne(10);

    static int Main() => s390xHw() == 11 ? 0 : 1;
}
