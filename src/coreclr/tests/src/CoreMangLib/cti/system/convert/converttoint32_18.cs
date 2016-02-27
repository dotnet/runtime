// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// ToInt32(System.UInt64)
/// </summary>

public class ConvertToInt32_18
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: ToInt32 should Converts the value of an uint to the equivalent int.");

        try
        {
            int expected = TestLibrary.Generator.GetInt16(-55);

            int actual = Convert.ToInt32((ulong)expected);

            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError("001.1", "ToInt32 can not Converts the value of an ushort to the equivalent int");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + actual + ", expected = " + expected);
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
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: OverflowException should be thrown when value is greater than Int32.MaxValue");

        try
        {
            uint c = uint.MaxValue;

            int actual = Convert.ToInt32(c);

            TestLibrary.TestFramework.LogError("101.1", "OverflowException is not thrown when value is greater than uint.MaxValue");
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + actual);
            retVal = false;
        }
        catch (OverflowException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        ConvertToInt32_18 test = new ConvertToInt32_18();

        TestLibrary.TestFramework.BeginTestCase("ConvertToInt32_18");

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
