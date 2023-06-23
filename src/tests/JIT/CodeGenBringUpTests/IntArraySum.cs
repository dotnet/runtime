// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_IntArraySum
{
    const int Pass = 100;
    const int Fail = -1;    

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int IntArraySum(int []a, int n)
    {
       int sum = 0;
       for (int i = 0; i < n; ++i)
          sum += a[i];
       return sum;
    }


    [Fact]
    public static int TestEntryPoint()
    {
        int [] a = new int[5] {1, 2, 3, 4, 5};
        int result = IntArraySum(a, a.Length);
        if (result == 15) return Pass;
        return Fail;        
    }
}
