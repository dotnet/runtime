// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Resources;
using System.IO;
using System.Collections;
using System.Globalization;

// Regression test for DevDiv Telesto 968
// MAC: Convert.ToDateTime(string) throws incorrect exception on JPN
// This will only really be hit in the globalization runs.
public class Test968
{
    private static bool Test()
    {
        bool retVal = true;
        try
        {
            DateTime result = Convert.ToDateTime("null");

        }
        catch (System.FormatException)
        {
            Console.WriteLine("Caughed expected System.FormatException calling Convert.ToDateTime(\"null\")");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception thrown Convert.ToDateTime(\"null\"): " + ex);
            retVal = false;
        }

        return retVal;
    }

    public static int Main()
    {
        int retVal = 100;
	    try
	    {
            if (!Test())
            {
                retVal = 0;
            }
	    }
	    catch (Exception ex)
	    {
		    Console.WriteLine("Exception cought in main:"+ex.Message);
            retVal = 0;
	    }
        return retVal;
    }

}
