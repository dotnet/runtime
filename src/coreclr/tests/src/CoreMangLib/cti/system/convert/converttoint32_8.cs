// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// ToInt32(System.Int64)
/// </summary>

public class ConvertToInt32_8
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify method ToInt32 .");

        try
        {
            retVal = VerificationHelper(0L, 0, "001.1") && retVal;
            retVal = VerificationHelper((long)int.MaxValue, int.MaxValue, "001.2") && retVal;
            retVal = VerificationHelper((long)int.MinValue, int.MinValue, "001.3") && retVal;

            long l = TestLibrary.Generator.GetInt32(-55);
            retVal = VerificationHelper(l, (int)l, "001.4");
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

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: OverflowException is not thrown.");

        try
        {
            long i = (long)Int32.MaxValue + 1;

            Int32 r = Convert.ToInt32(i);

            TestLibrary.TestFramework.LogError("101.1", "OverflowException is not thrown.");
            retVal = false;
        }
        catch (OverflowException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: OverflowException is not thrown.");

        try
        {
            long i = (long)Int32.MinValue - 1;

            Int32 r = Convert.ToInt32(i);

            TestLibrary.TestFramework.LogError("102.1", "OverflowException is not thrown.");
            retVal = false;
        }
        catch (OverflowException)
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
        ConvertToInt32_8 test = new ConvertToInt32_8();

        TestLibrary.TestFramework.BeginTestCase("ConvertToInt32_8");

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
    private bool VerificationHelper(long value, int desired, string errorno)
    {
        bool retVal = true;

        int actual = Convert.ToInt32(value);
        if (actual != desired)
        {
            TestLibrary.TestFramework.LogError(errorno, "Convert.ToInt32 returns unexpected values");
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + actual + ", desired = " + desired + ", value = " + value);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
