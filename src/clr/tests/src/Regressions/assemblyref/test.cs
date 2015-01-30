// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
