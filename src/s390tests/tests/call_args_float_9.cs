using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static float multipleArgs(float a, float b, float c, float d,
                              float e, float f, float g, float h,
                              float i) => a + b + c + d + e + f + g + h + i;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int s390xHw() => multipleArgs(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f) == 45.0f ? 0 : 1;

    static int Main() => s390xHw();
}
