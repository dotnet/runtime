// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

public class Test
{
    public static int Main()
    {
        try
        {
            CultureInfo ci = new CultureInfo("hu");
		
	    if (ci.ToString() != "hu")
	    {
	        Console.WriteLine("!!!ERROR-001: Result not as expected; expected value: hu, actual value: " + ci.ToString());
                Console.WriteLine("FAIL");
                return 99;
	    }
	    Console.WriteLine("Pass");
	    return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine("!!!ERROR-XXX: Unexpected exception : " + e);
            Console.WriteLine("FAIL");
            return 101;
        }
    }
}