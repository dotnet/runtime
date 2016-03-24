// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

///<summary>
///System.Text.UnicodeEncoding.GetHashCode() [v-zuolan]
///</summary>

public class UnicodeEncodingGetHashCode
{

    public static int Main()
    {
        UnicodeEncodingGetHashCode testObj = new UnicodeEncodingGetHashCode();
        TestLibrary.TestFramework.BeginTestCase("for method of System.Text.UnicodeEncoding.GetHashCode");
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

        int expectedValue;
        int actualValue;

        UnicodeEncoding uE1 = new UnicodeEncoding();
        UnicodeEncoding uE2 = new UnicodeEncoding();

        TestLibrary.TestFramework.BeginScenario("PosTest1:Invoke the method.");
        try
        {
            expectedValue = uE1.GetHashCode();
            actualValue = uE2.GetHashCode();

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
