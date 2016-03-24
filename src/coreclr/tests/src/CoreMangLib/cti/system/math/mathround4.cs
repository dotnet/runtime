// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Round(System.Double,System.Int32)
/// </summary>

public class MathRound4
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify Round(d, 0)");

        try
        {
            int i = 0;
            if (Math.Round(3.4d, i) != 3 || Math.Round(3.5d, i) != 4 || Math.Round(3.6d, i) != 4)
            {
                TestLibrary.TestFramework.LogError("001.1", "Return value is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify Round(d, 1)");

        try
        {
            if (Math.Round(3.44d, 1) != 3.4 || Math.Round(3.45d, 1) != 3.4 || Math.Round(3.46d, 1) != 3.5)
            {
                TestLibrary.TestFramework.LogError("002.1", "Return value is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify Round(d, 15)");

        try
        {
            double d1 =                 1234567890.123454;
            double expectedResult1 =    1234567890.12345;

            double d2 =                 1234567890.123455;
            double expectedResult2 =    1234567890.12346;

            double d3 =                 1234567890.123456;
            double expectedResult3 =    1234567890.12346;

            int i = 15;

            if (Math.Round(d1, i).ToString() != expectedResult1.ToString()
             || Math.Round(d2, i).ToString() != expectedResult2.ToString()
             || Math.Round(d3, i).ToString() != expectedResult3.ToString())
            {
                TestLibrary.TestFramework.LogError("003.1", "Return value is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentOutOfRangeException is not thrown.");

        try
        {
            double d = Math.Round(3.45, -1);
            TestLibrary.TestFramework.LogError("101.1", " OverflowException is not thrown.");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: ArgumentOutOfRangeException is not thrown.");

        try
        {
            double d = Math.Round(1234567890.1234567, 16);
            TestLibrary.TestFramework.LogError("102.1", " OverflowException is not thrown.");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        MathRound4 test = new MathRound4();

        TestLibrary.TestFramework.BeginTestCase("MathRound4");

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
}
