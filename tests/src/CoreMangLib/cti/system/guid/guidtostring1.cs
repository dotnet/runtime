// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// ToString
/// </summary>
public class GuidToString1
{
    #region Private Fields
    private const int c_SIZE_OF_ARRAY = 16;
    #endregion

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: ToString should return a string formated in pattern xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx");

        try
        {
            byte[] expected = new byte[c_SIZE_OF_ARRAY];
            TestLibrary.Generator.GetBytes(-55, expected);

            Guid guid = new Guid(expected);
            retVal = VerificationHelper(guid, expected, "001.1") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: ToString should return a string formated in pattern xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx on Guid.Empty");

        try
        {
            byte[] expected = new byte[c_SIZE_OF_ARRAY];

            retVal = VerificationHelper(Guid.Empty, expected, "002.1") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        GuidToString1 test = new GuidToString1();

        TestLibrary.TestFramework.BeginTestCase("GuidToString1");

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
    private string ByteToHexString(int b)
    {
        string ret = String.Empty;

        int h = b >> 4;
        h = h & 0xf;
        ret += (char)((h > 9) ? h - 10 + 0x61 : h + 0x30);
        b = b & 0xf;
        ret += (char)((b > 9) ? b - 10 + 0x61 : b + 0x30);

        return ret;
    }

    private bool VerificationHelper(Guid guid, byte[] bytes, string errorNo)
    {
        bool retVal = true;

        string val = guid.ToString();
        string expected = String.Empty;

        int[] fmt = new int[] { 4, 2, 2, 2, 6};
        bool[] reverseOrder = new bool[] { true, true, true, false, false };
        int index = 0;

        for ( int f = 0; f < fmt.Length; ++f )
        {
            for (int i = fmt[f]; i > 0; --i)
            {
                int curIndex = 0;
                if (reverseOrder[f])
                    curIndex = index + i - 1;
                else
                    curIndex = index + fmt[f] - i;

                expected += ByteToHexString(bytes[curIndex]);
            }

            if (f < fmt.Length - 1)
            {
                expected += '-';
            }

            index += fmt[f];
        }

        if (expected != val)
        {
            TestLibrary.TestFramework.LogError(errorNo, "ToString returns invalid guid string");
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] expected = " + expected + ", val = " + val);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
