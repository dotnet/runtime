using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static float addFour(float a, float b, float c, float d) => a + b + c + d;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static float s390xHw() => addFour(1.0f, 2.0f, 3.0f, 4.0f);

    static int Main() => s390xHw() == 10.0f ? 0 : 1;
}
