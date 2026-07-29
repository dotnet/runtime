using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static char identityChar(char x) => x;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static char s390xHw() => identityChar('A');

    static int Main() => s390xHw() == 'A' ? 0 : 1;
}
