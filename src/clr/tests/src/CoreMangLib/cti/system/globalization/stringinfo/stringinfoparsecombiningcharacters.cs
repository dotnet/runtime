// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// System.Globalization.StringInfo.ParseCombiningCharacters(System.String)
/// </summary>
public class StringInfoParseCombiningCharacters
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: The mothod should return the indexes of each base character");

        try
        {
            retVal = VerificationHelper("\u4f00\u302a\ud800\udc00\u4f01", new int[] { 0, 2, 4 }, "001.1");
            retVal = VerificationHelper("abcdefgh", new int[] { 0, 1, 2, 3, 4, 5, 6, 7 }, "001.2");
            retVal = VerificationHelper("zj\uDBFF\uDFFFlk", new int[] { 0, 1, 2, 4, 5 }, "001.3");
            retVal = VerificationHelper("!@#$%^&", new int[] { 0, 1, 2, 3, 4, 5, 6 }, "001.4");
            retVal = VerificationHelper("!\u20D1bo\uFE22\u20D1\u20EB|", new int[] { 0, 2, 3, 7 }, "001.5");
            retVal = VerificationHelper("1\uDBFF\uDFFF@\uFE22\u20D1\u20EB9", new int[] { 0, 1, 3, 7 }, "001.6");
            retVal = VerificationHelper("   ", new int[] { 0, 1, 2 }, "001.7");
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: The argument string is an empty string");

        try
        {
            int[] result = StringInfo.ParseCombiningCharacters(string.Empty);
            if (result == null)
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected");
                retVal = false;
            }
            if (result.Length != 0)
            {
                TestLibrary.TestFramework.LogError("004", "The result is not the value as expected");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The argument string is a null reference");

        try
        {
            string str = null;
            int[] result = StringInfo.ParseCombiningCharacters(str);
            TestLibrary.TestFramework.LogError("101", "The ArgumentNullException was not thrown as expected");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        StringInfoParseCombiningCharacters test = new StringInfoParseCombiningCharacters();

        TestLibrary.TestFramework.BeginTestCase("StringInfoParseCombiningCharacters");

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
    private bool VerificationHelper(string str, int[] expected, string errorno)
    {
        bool retVal = true;

        int[] result = StringInfo.ParseCombiningCharacters(str);

        if (!compare<int>(result, expected))
        {
            TestLibrary.TestFramework.LogError(errorno, "The result is not the value as expected");
            retVal = false;
        }

        return retVal;
    }

    private bool compare<T>(T[] a, T[] b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }
        else
        {
            for (int i = 0; i < a.Length; i++)
            {
                if (!a[i].Equals(b[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }
    #endregion
}
