// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    // This test only works when the "depth cutoff" parameter of generic cycle detector
    // is reduced to 1, otherwise the combinatorical explosion overflows the code generator.
    private struct Breadth<T1, T2, T3, T4, T5, T6>
    {
        public static long TypeNestedFactorial(int count)
        {
            if (count <= 1)
            {
                return 1;
            }
            long result = 0;
            if (result < count) result = Breadth<T2, T3, T4, T5, T6, Oper1<T1>>.TypeNestedFactorial(count - 1);
            if (result < count) result = Breadth<T2, T3, T4, T5, T6, Oper2<T1>>.TypeNestedFactorial(count - 1);
            if (result < count) result = Breadth<T2, T3, T4, T5, T6, Oper3<T1>>.TypeNestedFactorial(count - 1);
            if (result < count) result = Breadth<T2, T3, T4, T5, T6, Oper4<T1>>.TypeNestedFactorial(count - 1);
            if (result < count) result = Breadth<T2, T3, T4, T5, T6, Oper5<T1>>.TypeNestedFactorial(count - 1);
            return count * result;
        }
    }
    
    private struct Oper1<T> {}
    private struct Oper2<T> {}
    private struct Oper3<T> {}
    private struct Oper4<T> {}
    private struct Oper5<T> {}
    
    [Fact]
    public static void BreadthTest()
    {
        const long Factorial20 = 20L * 19L * 18L * 17L * 16L * 15L * 14L * 13L * 12L * 11L * 10L * 9L * 8L * 7L * 6L * 5L * 4L * 3L * 2L;
        Assert.Equal(Factorial20, Breadth<byte, char, int, long, float, double>.TypeNestedFactorial(ReturnTwentyAndDontTellJIT()));
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ReturnTwentyAndDontTellJIT() => 20;
}
