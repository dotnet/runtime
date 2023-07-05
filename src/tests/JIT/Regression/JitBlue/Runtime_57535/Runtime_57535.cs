using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_57535
{
    static long z;

    [Fact]
    public static int TestEntryPoint()
    {
        z = 10;
        int[] a = F();
        long zz = z;
        int result = 0;
        for (int i = 0; i < (int) zz; i++)        
        {
            result += a[i];
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int[] F()
    {
       int[] result = new int[100];
       result[3] = 100;
       return result;
    }
}
