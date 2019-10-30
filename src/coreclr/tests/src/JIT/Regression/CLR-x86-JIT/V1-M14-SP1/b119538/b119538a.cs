// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

class A
{
}

class B
{
    public static int Main()
    {
        object[,] oa = new B[1, 1];
        B[,] ba = (B[,])oa;
        try
        {
            oa[0, 0] = new A();
        }
        catch (System.ArrayTypeMismatchException)
        {
            System.Console.WriteLine("PASSED");
            return 100;
        }
        System.Console.WriteLine("FAILED");
        return 1;
    }
}
