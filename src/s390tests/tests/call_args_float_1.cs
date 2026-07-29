using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static float addOne(float x) => x + 1.0f;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static float s390xHw() => addOne(10.0f);

    static int Main() => s390xHw() == 11.0f ? 0 : 1;
}
