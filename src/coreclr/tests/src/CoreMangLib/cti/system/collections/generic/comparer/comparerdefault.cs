// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

public class ComparerDefault
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call Default to return default order comparer of T");

        try
        {
            retVal = VerificationHelper(Comparer<int>.Default, 1, 2, -1, "001.1") && retVal;
            retVal = VerificationHelper(Comparer<int>.Default, 2, 1, 1, "001.2") && retVal;
            retVal = VerificationHelper(Comparer<int>.Default, 1, 1, 0, "001.3") && retVal;
            retVal = VerificationHelper(Comparer<char>.Default, 'a', 'b', -1, "001.4") && retVal;
            retVal = VerificationHelper(Comparer<char>.Default, 'b', 'a', 1, "001.5") && retVal;
            retVal = VerificationHelper(Comparer<char>.Default, 'a', 'a', 0, "001.6") && retVal;
            retVal = VerificationHelper(Comparer<string>.Default, "ABCDEFG", "abcdefg", 1, "001.7") && retVal;
            retVal = VerificationHelper(Comparer<string>.Default, "ABCDEFG", "ABCDEFG", 0, "001.8") && retVal;
            retVal = VerificationHelper(Comparer<string>.Default, "abcdefg", "ABCDEFG", -1, "001.9") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        ComparerDefault test = new ComparerDefault();

        TestLibrary.TestFramework.BeginTestCase("ComparerDefault");

        if (test.RunTests())
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.TestFramework.LogInformation("PASS");
            return 100;
        }
        else
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.TestFramework.LogInformation("FAIL");
            return 0;
        }
    }

    #region Private Methods
    private bool VerificationHelper<T>(Comparer<T> defaultCompare, T x, T y, int expected, string errorno)
    {
        bool retVal = true;

        int actual = defaultCompare.Compare(x, y);
        if (expected != actual)
        {
            TestLibrary.TestFramework.LogError(errorno, "Compare returns unexpected value");
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] x = " + x + ", y = " + y + ", expected = " + expected + ", actual = " + actual);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
