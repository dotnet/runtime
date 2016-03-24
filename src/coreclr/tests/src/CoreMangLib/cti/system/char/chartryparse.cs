// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

// Ported to CoreCLR from Desktop test Co7522TryParse.cs
// Tests Char.TryParse(str)
// 2003/02/25  KatyK
// 2007/06/28  adapted by MarielY
public class CharTryParse
{
    static bool verbose = false;

    public static int Main()
    {
        bool passed = true;

        try
        {
            // Success cases
            passed &= VerifyCharTryParse("a", 'a', true);
            passed &= VerifyCharTryParse("4", '4', true);
            passed &= VerifyCharTryParse(" ", ' ', true);
            passed &= VerifyCharTryParse("\n", '\n', true);
            passed &= VerifyCharTryParse("\0", '\0', true);
            passed &= VerifyCharTryParse("\u0135", '\u0135', true);
            passed &= VerifyCharTryParse("\u05d9", '\u05d9', true);
            passed &= VerifyCharTryParse("\ud801", '\ud801', true);  // high surrogate
            passed &= VerifyCharTryParse("\udc01", '\udc01', true);  // low surrogate
            passed &= VerifyCharTryParse("\ue001", '\ue001', true);  // private use codepoint

            // Fail cases
            passed &= VerifyCharTryParse(null, '\0', false);
            passed &= VerifyCharTryParse("", '\0', false);
            passed &= VerifyCharTryParse("\n\r", '\0', false);
            passed &= VerifyCharTryParse("kj", '\0', false);
            passed &= VerifyCharTryParse(" a", '\0', false);
            passed &= VerifyCharTryParse("a ", '\0', false);
            passed &= VerifyCharTryParse("\\u0135", '\0', false);
            passed &= VerifyCharTryParse("\u01356", '\0', false);
            passed &= VerifyCharTryParse("\ud801\udc01", '\0', false);   // surrogate pair

            ///  END TEST CASES
        }
        catch (Exception e)
        {
            TestLibrary.Logging.WriteLine("Unexpected exception!!  " + e.ToString());
            passed = false;
        }

        if (passed)
        {
            TestLibrary.Logging.WriteLine("paSs");
            return 100;
        }
        else
        {
            TestLibrary.Logging.WriteLine("FAiL");
            return 1;
        }
    }

    public static bool VerifyCharTryParse(string value, Char expectedResult, bool expectedReturn)
    {
        Char result;
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Char.TryParse, Value = '{0}', Expected Result, {1}, Expected Return = {2}",
                value, expectedResult, expectedReturn);
        }
        try
        {
            bool returnValue = Char.TryParse(value, out result);
            if (returnValue != expectedReturn)
            {
                TestLibrary.Logging.WriteLine("FAILURE: Value = '{0}', Expected Return: {1}, Actual Return: {2}", value, expectedReturn, returnValue);
                return false;
            }
            if (result != expectedResult)
            {
                TestLibrary.Logging.WriteLine("FAILURE: Value = '{0}', Expected Result: {1}, Actual Result: {2}", value, expectedResult, result);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            TestLibrary.Logging.WriteLine("FAILURE: Unexpected Exception, Value = '{0}', Exception: {1}", value, ex);
            return false;
        }
    }
}
