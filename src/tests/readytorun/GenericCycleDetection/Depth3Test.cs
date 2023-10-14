// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    // This test exercises the "depth cutoff" parameter of generic cycle detector.
    // It is a simple algorithm generating deep generic types of the form
    // Depth1`1<Depth1`1<Depth1`1<Depth1`1<T>>>>
    private struct Depth1<T>
    {
        public static long TypeNestedFactorial(int count)
        {
            if (count <= 1)
            {
                return 1;
            }
            long result = 0;
            if (result < count) result = Depth1<Depth1<T>>.TypeNestedFactorial(count - 1);
            if (result < count) result = Depth1<Depth2<T>>.TypeNestedFactorial(count - 1);
            if (result < count) result = Depth1<Depth3<T>>.TypeNestedFactorial(count - 1);
            if (result < count) result = Depth1<Depth4<T>>.TypeNestedFactorial(count - 1);
            return count * result;
        }
    }
    
    private struct Depth2<T> {}
    private struct Depth3<T> {}
    private struct Depth4<T> {}
    
    [Fact]
    public static void DepthTest()
    {
        const long Factorial20 = 20L * 19L * 18L * 17L * 16L * 15L * 14L * 13L * 12L * 11L * 10L * 9L * 8L * 7L * 6L * 5L * 4L * 3L * 2L;
        Assert.Equal(Factorial20, Depth1<long>.TypeNestedFactorial(ReturnTwentyAndDontTellJIT()));
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ReturnTwentyAndDontTellJIT() => 20;
}
