using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool identityBool(bool x) => x;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool s390xHw() => identityBool(true);

    static int Main() => s390xHw() == true ? 0 : 1;
}
