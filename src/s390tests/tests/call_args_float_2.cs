using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static float addTwo(float a, float b) => a + b;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static float s390xHw() => addTwo(11.0f, 22.0f);

    static int Main() => s390xHw() == 33.0f ? 0 : 1;
}
