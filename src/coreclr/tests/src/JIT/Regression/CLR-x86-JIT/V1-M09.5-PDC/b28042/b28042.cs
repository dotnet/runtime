// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

class A
{
    public static int[] B = new int[2];

    static void Test()
    {
        A[] aa;
        int n;
        for (aa = new A[7]; true; n = B[2] + B[2]) ;
    }
    static int Main()
    {
        try
        {
            Test();
        }
        catch (IndexOutOfRangeException) { }
        return 100;
    }
}
