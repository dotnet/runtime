// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;

public class B
{
    public int method1()
    {
        return 100;
    }
}

public class Test
{
	public static int Main()
	{
        try
        {
            B obj = new B();

            if (obj.method1() == 100)
            {
                Console.WriteLine("PASS");
                return 100;
            }
            else
            {
                Console.WriteLine("FAIL");
                return 101;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Caught unexpected exception: " + e);
            return 102;
        }
	}
}
