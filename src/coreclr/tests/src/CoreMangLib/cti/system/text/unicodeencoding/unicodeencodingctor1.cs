// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

///<summary>
///System.Test.UnicodeEncoding.Ctor() [v-zuolan]
///</summary>

public class UnicodeEncodingCtor
{

    public static int Main()
    {
        UnicodeEncodingCtor testObj = new UnicodeEncodingCtor();
        TestLibrary.TestFramework.BeginTestCase("for constructor of System.Test.UnicodeEncoding");
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

        UnicodeEncoding expectedValue = new UnicodeEncoding(false,true);
        UnicodeEncoding actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Create a instance");
        try
        {
            actualValue = new UnicodeEncoding();

            if (!expectedValue.Equals(actualValue))
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
