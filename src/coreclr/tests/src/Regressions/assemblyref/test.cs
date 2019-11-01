// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

// regression test for CoreCLR #433
using System;

public class Test
{
	public static int Main()
	{
        try
        {
            A obj = new A();

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
