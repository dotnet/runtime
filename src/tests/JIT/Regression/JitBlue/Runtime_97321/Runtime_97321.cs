using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_97321
{
    [Fact]
    public static int TestEntryPoint() => Foo(false);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Foo(bool b)
    {
        int sum = 1;
        int i = 0;
        goto Header;
Top:;
        sum += 33;

Header:;
        if (b)
        {
            goto Header;
        }

Bottom:
        i++;
        if (i < 4)
        {
            goto Top;
        }

        return sum;
    }
}
