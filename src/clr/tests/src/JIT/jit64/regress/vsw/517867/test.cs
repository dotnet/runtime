// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

public class Test
{
    private int _counter = 0;

    public int func(int type)
    {
        int rc;

        try
        {
        }
        finally
        {
            switch (type)
            {
                case 1:
                    rc = foo();
                    break;
                case 2:
                case 4:
                    break;
                case 56:
                case 54:
                    rc = foo();
                    break;
                case 5:
                case 53:
                    break;
                default:
                    break;
            }
            rc = foo();
        }
        return rc;
    }

    public int foo()
    {
        return _counter++;
    }


    public static int Main()
    {
        Test obj = new Test();
        int val = obj.func(1);
        if (val == 1)
        {
            System.Console.WriteLine("PASSED");
            return 100;
        }
        System.Console.WriteLine("FAILED");
        return 1;
    }
}
