// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

public class Test570
{
    public static int Main()
    {
        int retVal = 100;
        int numDigits = 2;
        if (!TestLibrary.Utilities.IsWindows)
        {
            numDigits = 3;
        }

	    try
	    {
            CultureInfo enUS = new CultureInfo("en-US");
            TestLibrary.Logging.WriteLine("enUS.NumberFormat.NumberDecimalDigits=" + enUS.NumberFormat.NumberDecimalDigits.ToString());
            if (enUS.NumberFormat.NumberDecimalDigits != numDigits)
            {
                TestLibrary.Logging.WriteLine("Error: enUS.NumberFormat.NumberDecimalDigits=" + enUS.NumberFormat.NumberDecimalDigits.ToString() + ", expected " + numDigits.ToString());
                retVal = 0;
            }
            TestLibrary.Logging.WriteLine("enUS.NumberFormat.PercentDecimalDigits=" + enUS.NumberFormat.PercentDecimalDigits.ToString());
            if (enUS.NumberFormat.PercentDecimalDigits != numDigits)
            {
                TestLibrary.Logging.WriteLine("Error: enUS.NumberFormat.PercentDecimalDigits=" + enUS.NumberFormat.PercentDecimalDigits.ToString() + ", expected " + numDigits.ToString());
                retVal = 0;
            }
        }
	    catch (Exception ex)
	    {
		    TestLibrary.Logging.WriteLine("Exception cought in main:"+ex.Message);
            retVal = 0;
	    }
        return retVal;
    }

}
