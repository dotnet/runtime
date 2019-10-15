// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Resources;
using System.IO;
using System.Collections;
using System.Globalization;

// Regression test for DevDiv Telesto 570 
// MAC: Globalization: DateTime.Parse throws FormatException when passed the ToString value of another DateTime
// Simply ensure that Mac no longer throws an exception when trying to parse the ToString of another DateTime.
public class Test570
{
    private static bool Test()
    {
        bool retVal = true;
        try
        {
            DateTime dtTemp = DateTime.Parse(DateTime.Now.ToString());

        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception thrown DateTime.Parse(DateTime.Now.ToString()): " + ex);
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
