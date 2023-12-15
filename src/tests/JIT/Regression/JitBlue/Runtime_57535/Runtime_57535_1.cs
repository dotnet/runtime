using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_57535_1
{
    static long z;

    [Fact]
    public static int TestEntryPoint()
    {
        z = 2;
        int[] a = F();
        long zz = z;
        int result = 0;
        for (int i = (int) zz; i < a.Length; i++)
        {
            result += a[i];
        }
        Bar(zz);
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int[] F()
    {
       int[] result = new int[100];
       result[3] = 100;
       return result;
    }

    static void Bar(long z) {}
}
