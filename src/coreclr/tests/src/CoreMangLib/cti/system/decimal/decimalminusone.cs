// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Decimal.MinusOne
/// </summary>
public class DecimalMinusOne
{

    public static int Main()
    {
        DecimalMinusOne dMinusOne = new DecimalMinusOne();
        TestLibrary.TestFramework.BeginTestCase("for Field:System.Decimal.MinusOne");

        if (dMinusOne.RunTests())
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
    public bool RunTests()
    {
        bool retVal = true;
        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        return retVal;
    }

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1:Verify the Decimal.MinusOne is equal -1";
        const string c_TEST_ID = "P001";

        Decimal dMinusOne = Convert.ToDecimal(-1);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {

            if (Decimal.MinusOne != dMinusOne)
            {
                string errorDesc = "Value is not " + dMinusOne.ToString() + " as expected: Actual(" + Decimal.MinusOne.ToString() + ")";
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }


        return retVal;
    }


    #endregion
}

