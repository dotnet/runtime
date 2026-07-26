using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static float multipleArgs(float a, float b, float c, float d, float e) => a + b + c + d + e;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int s390xHw() => multipleArgs(1.0f, 2.0f, 3.0f, 4.0f, 5.0f) == 15.0f ? 0 : 1;

    static int Main() => s390xHw();
}
