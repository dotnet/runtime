// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

///<summary>
///System.Globalization.DateTimeStyles.AllowLeadingWhite
///</summary>

public class DateTimeStylesAllowLeadingWhite
{

    public static int Main()
    {
        DateTimeStylesAllowLeadingWhite testObj = new DateTimeStylesAllowLeadingWhite();
        TestLibrary.TestFramework.BeginTestCase("for property of System.Globalization.DateTimeStyles.AllowLeadingWhite");
        if (testObj.RunTests())
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
        retVal = PosTest1() && retVal;
        return retVal;
    }


    #region Test Logic

    public bool PosTest1()
    {
        bool retVal = true;

        UInt64 expectedValue = 0x00000001;
        UInt64 actualValue;


        TestLibrary.TestFramework.BeginScenario("PosTest1:get DateTimeStyles.AllowLeadingWhite");
        try
        {
            actualValue = (UInt64)DateTimeStyles.AllowLeadingWhite;

            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("001", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }


    #endregion
}
