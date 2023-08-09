// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
public class Test_test
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


    [Fact]
    public static int TestEntryPoint()
    {
        Test_test obj = new Test_test();
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
