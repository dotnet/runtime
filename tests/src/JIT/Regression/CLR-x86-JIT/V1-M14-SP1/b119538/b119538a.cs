// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
