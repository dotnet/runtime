// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

// This test was ported over to CoreCLR from Co7521TryParse.cs
// Tests Boolean.TryParse(str)
// 2003/02/25  KatyK
// 2007/06/28  adapted by MarielY
public class BooleanTryParse 
{
    static bool verbose = false;

    public static int Main()
    {
        bool passed = true;

	try
	{
	    // Success cases
	    passed &= VerifyBooleanTryParse("True", true, true);
	    passed &= VerifyBooleanTryParse("true", true, true);
	    passed &= VerifyBooleanTryParse("TRUE", true, true);
	    passed &= VerifyBooleanTryParse("tRuE", true, true);
	    passed &= VerifyBooleanTryParse("False", false, true);
	    passed &= VerifyBooleanTryParse("false", false, true);
	    passed &= VerifyBooleanTryParse("FALSE", false, true);
	    passed &= VerifyBooleanTryParse("fAlSe", false, true);
	    passed &= VerifyBooleanTryParse("  True  ", true, true);
	    passed &= VerifyBooleanTryParse("False  ", false, true);
        passed &= VerifyBooleanTryParse("True\0", true, true); // VSWhidbey 465401
        passed &= VerifyBooleanTryParse("False\0", false, true);
        passed &= VerifyBooleanTryParse("True\0    ", true, true);
        passed &= VerifyBooleanTryParse(" \0 \0  True   \0 ", true, true);
        passed &= VerifyBooleanTryParse("  False \0\0\0  ", false, true);

	    // Fail cases
	    passed &= VerifyBooleanTryParse(null, false, false);
	    passed &= VerifyBooleanTryParse("", false, false);
	    passed &= VerifyBooleanTryParse(" ", false, false);
	    passed &= VerifyBooleanTryParse("Garbage", false, false);
	    passed &= VerifyBooleanTryParse("True\0Garbage", false, false);
	    passed &= VerifyBooleanTryParse("True\0True", false, false);
	    passed &= VerifyBooleanTryParse("True True", false, false);
	    passed &= VerifyBooleanTryParse("True False", false, false);
	    passed &= VerifyBooleanTryParse("False True", false, false);
	    passed &= VerifyBooleanTryParse("Fa lse", false, false);
	    passed &= VerifyBooleanTryParse("T", false, false);
	    passed &= VerifyBooleanTryParse("0", false, false);
	    passed &= VerifyBooleanTryParse("1", false, false);

	    ///  END TEST CASES
	}
	catch (Exception e)
	{
	    TestLibrary.Logging.WriteLine("Unexpected exception!!  " + e.ToString());
	    passed = false;
	}

        if (passed)
	{
            TestLibrary.Logging.WriteLine ("paSs");
            return 100;
        }
        else
	{
            TestLibrary.Logging.WriteLine ("FAiL");
            return 1;
        }
    }

    public static bool VerifyBooleanTryParse(string value, bool expectedResult, bool expectedReturn) {
        bool result;
        if (verbose) {
            TestLibrary.Logging.WriteLine ("Test: Boolean.TryParse, Value = '{0}', Expected Result, {1}, Expected Return = {2}", 
                                value, expectedResult, expectedReturn);
        }
        try {
            bool returnValue = Boolean.TryParse(value, out result);
            if (returnValue != expectedReturn) {
                TestLibrary.Logging.WriteLine ("FAILURE: Value = '{0}', Expected Return: {1}, Actual Return: {2}", value, expectedReturn, returnValue);
                return false;
            }
            if (result != expectedResult) {
                TestLibrary.Logging.WriteLine ("FAILURE: Value = '{0}', Expected Result: {1}, Actual Result: {2}", value, expectedResult, result);
                return false;
            }

            return true;
        }
        catch (Exception ex) {
            TestLibrary.Logging.WriteLine ("FAILURE: Unexpected Exception, Value = '{0}', Exception: {1}", value, ex);            
            return false;
        }
    }
}

