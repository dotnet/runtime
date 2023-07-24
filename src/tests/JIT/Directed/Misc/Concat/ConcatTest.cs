// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class ConcatTest
{
    static string strA = "A";
    static string strB = "B";
    static string strC = "C";
    static string strD = "D";
    static string strE = "E";

    static string strAB      = "AB";
    static string strABC     = "ABC";
    static string strABCD    = "ABCD";
    static string strABCDE   = "ABCDE";
    static string strABCDx2  = "ABCDABCD";

    static int iReturn = 100;

    [Fact]
    static public int TestEntryPoint()
    {
        iReturn = 100;
        try
        {
            string result;

            result = string.Concat(strA, strB);
            CheckResult(result, strAB);

            result = string.Concat(strA, strB, strC);
            CheckResult(result, strABC);

            result = string.Concat(strA, strB, strC, strD);
            CheckResult(result, strABCD);

            result = string.Concat(strA, strB, strC, strD, strE);
            CheckResult(result, strABCDE);

            result = string.Concat(strA, strB, strC, strD, strA, strB, strC, strD);
            CheckResult(result, strABCDx2);

            Console.WriteLine("Passed all tests.");
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed {0}", e.StackTrace);

            iReturn = 99;
        }

        return iReturn;
    }

    static void CheckResult(string result, string expected)
    {
        if (result != expected)
        {
            Console.WriteLine("FAILED: result was '" + result +
                              "', expected was '" + expected + "'");
            iReturn++;
        }
    }
}
