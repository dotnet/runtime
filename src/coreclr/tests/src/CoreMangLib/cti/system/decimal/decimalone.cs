// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Decimal.One
/// </summary>
public class DecimalOne
{

    public static int Main()
    {
        DecimalOne dOne = new DecimalOne();
        TestLibrary.TestFramework.BeginTestCase("for Field:System.Decimal.One");

        if (dOne.RunTests())
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
        const string c_TEST_DESC = "PosTest1:Verify the Decimal One is equal to 1";
        const string c_TEST_ID = "P001";

        Decimal dOne = Convert.ToDecimal(1);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {

            if (Decimal.One != dOne)
            {
                string errorDesc = "Value is not " + dOne.ToString() + " as expected: Actual(" + Decimal.One.ToString() + ")";
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

