// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Round(System.Decimal, System.Int32)
/// </summary>

public class MathRound2
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        
        //
        // TODO: Add your negative test cases here
        //
        // TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify Round(d ,0) .");

        try
        {
            if (Math.Round(3.45, 0) != 3 || Math.Round(3.55,0) != 4)
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify Round(d ,1) .");

        try
        {
            if (Math.Round(3.44, 1) != 3.4 || Math.Round(3.45, 1) != 3.4 || Math.Round(3.46, 1) != 3.5)
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify Round(d ,28) .");

        try
        {
            decimal d1              = 3.12345678901234567890123456744M;
            decimal expectedResult1 = 3.1234567890123456789012345674M;

            decimal d2              = 3.12345678901234567890123456745M;
            decimal expectedResult2 = 3.1234567890123456789012345674M;

            decimal d3              = 3.12345678901234567890123456746M;
            decimal expectedResult3 = 3.1234567890123456789012345675M;

            int i = 28;

            if (Math.Round(d1, i) != expectedResult1 || Math.Round(d2, i) != expectedResult2 || Math.Round(d3, i) != expectedResult3)
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
            decimal d1 = 3.123456789012345678901234567890M;
            decimal result = Math.Round(d1, -1);

            TestLibrary.TestFramework.LogError("101.1", "ArgumentOutOfRangeException is not thrown.");
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
            decimal d1 = 3.123456789012345678901234567890M;
            decimal result = Math.Round(d1, 29);

            TestLibrary.TestFramework.LogError("102.1", "ArgumentOutOfRangeException is not thrown.");
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
        MathRound2 test = new MathRound2();

        TestLibrary.TestFramework.BeginTestCase("MathRound2");

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
