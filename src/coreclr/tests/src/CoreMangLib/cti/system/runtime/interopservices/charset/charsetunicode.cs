// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.InteropServices;


///<summary>
///System.Runtime.InteropServices.CharSet.Unicode [v-zuolan]
///</summary>

public class CharSetUnicode
{

    public static int Main()
    {
        CharSetUnicode testObj = new CharSetUnicode();
        TestLibrary.TestFramework.BeginTestCase("for property of System.Runtime.InteropServices.CharSet.Unicode");
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
        TestLibrary.TestFramework.LogInformation("Positive");
        retVal = PosTest1() && retVal;

        return retVal;
    }


    #region Test Logic
    public bool PosTest1()
    {
        bool retVal = true;

        int expectedValue = 3;
        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest1:get the property");
        try
        {
            actualValue = (int)CharSet.Unicode;
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
