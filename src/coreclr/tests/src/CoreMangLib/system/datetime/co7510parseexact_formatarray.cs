// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

//Introduced as a BuildBVT as a regression test for NDPWhidbey bug #24414.
//Make sure that a basic case of DateTime.ParseExact with a format array works.
class Co7510ParseExact_formatarray
{
    public static int Main() 
    {
	try
	{
	    string[] formats = new string[24];	
	    formats[0] = "HH:mm:ss";
	    formats[1] = "HH:mm:ss.f";
	    formats[2] = "HH:mm:ss.ff";
	    formats[3] = "HH:mm:ss.fff";
	    formats[4] = "HH:mm:ss.ffff";
	    formats[5] = "HH:mm:ss.fffff";
	    formats[6] = "HH:mm:ss.ffffff";
	    formats[7] = "HH:mm:ss.fffffff";
	    formats[8] = "HH:mm:ssZ";
	    formats[9] = "HH:mm:ss.fZ";
	    formats[10] = "HH:mm:ss.ffZ";
	    formats[11] = "HH:mm:ss.fffZ";
	    formats[12] = "HH:mm:ss.ffffZ";
	    formats[13] = "HH:mm:ss.fffffZ";
	    formats[14] = "HH:mm:ss.ffffffZ";
	    formats[15] = "HH:mm:ss.fffffffZ";
	    formats[16] = "HH:mm:sszzzzzz";
	    formats[17] = "HH:mm:ss.fzzzzzz";
	    formats[18] = "HH:mm:ss.ffzzzzzz";
	    formats[19] = "HH:mm:ss.fffzzzzzz";
	    formats[20] = "HH:mm:ss.ffffzzzzzz";
	    formats[21] = "HH:mm:ss.fffffzzzzzz";
	    formats[22] = "HH:mm:ss.ffffffzzzzzz";
	    formats[23] = "HH:mm:ss.fffffffzzzzzz";
	    string time = "10:27:27.123";

	    DateTime dtReturned = 
		DateTime.ParseExact(time, formats, DateTimeFormatInfo.InvariantInfo, 
				    DateTimeStyles.AllowLeadingWhite|DateTimeStyles.AllowTrailingWhite);

	    DateTime dtNow = DateTime.Now;
	    DateTime dtExpected = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, 10, 27, 27, 123);

	    if (dtExpected == dtReturned)
	    {
		TestLibrary.Logging.WriteLine("PASS");
		return 100;
	    }
	    else
	    {
		TestLibrary.Logging.WriteLine("Expected: {0}", dtExpected.ToString("M/d/yyyy HH:mm:ss.ffff tt"));
		TestLibrary.Logging.WriteLine("Actual:   {0}", dtReturned.ToString("M/d/yyyy HH:mm:ss.ffff tt"));
		TestLibrary.Logging.WriteLine("FAIL");
		return 1;
	    }
	}
	catch (Exception e)
	{
	    TestLibrary.Logging.WriteLine("Unexpected exception: {0}", e.ToString());
	    TestLibrary.Logging.WriteLine("FAIL");
	    return 1;
	}
    }
}
