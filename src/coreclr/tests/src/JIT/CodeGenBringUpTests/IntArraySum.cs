// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Runtime.CompilerServices;
public class BringUpTest
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


    public static int Main()
    {
        int [] a = new int[5] {1, 2, 3, 4, 5};
        int result = IntArraySum(a, a.Length);
        if (result == 15) return Pass;
        return Fail;        
    }
}
