// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

///<summary>
///System.Text.UnicodeEncoding.GetMaxCharCount(int) [v-zuolan]
///</summary>

public class UnicodeEncodingGetMaxCharCount
{

    public static int Main()
    {
        UnicodeEncodingGetMaxCharCount testObj = new UnicodeEncodingGetMaxCharCount();
        TestLibrary.TestFramework.BeginTestCase("for method of System.Text.UnicodeEncoding.GetMaxCharCount");
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
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        TestLibrary.TestFramework.LogInformation("Negative");
        retVal = NegTest1() && retVal;

        return retVal;
    }


    #region Positive Test Logic
    public bool PosTest1()
    {
        bool retVal = true;

        int expectedValue = 1;
        int actualValue;

        UnicodeEncoding uE = new UnicodeEncoding();

        TestLibrary.TestFramework.BeginScenario("PosTest1:Invoke the method and set byteCount as 0.");
        try
        {
            actualValue = uE.GetMaxCharCount(0);

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


    public bool PosTest2()
    {
        bool retVal = true;

        int expectedValue = 2;
        int actualValue;

        UnicodeEncoding uE = new UnicodeEncoding();

        TestLibrary.TestFramework.BeginScenario("PosTest2:Invoke the method and set byteCount as 1.");
        try
        {
            actualValue = uE.GetMaxCharCount(1);

            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("003", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }



    public bool PosTest3()
    {
        bool retVal = true;

        int byteCount = TestLibrary.Generator.GetInt32(-55);

        int expectedValue = (byteCount + 1) / 2 + 1;
        int actualValue;

        UnicodeEncoding uE = new UnicodeEncoding();

        TestLibrary.TestFramework.BeginScenario("PosTest3:Invoke the method and set byteCount as random integer.");
        try
        {
            actualValue = uE.GetMaxCharCount(byteCount);

            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("005", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    #endregion


    #region Negative Test Logic
    public bool NegTest1()
    {
        bool retVal = true;

        int actualValue;

        UnicodeEncoding uE = new UnicodeEncoding();

        TestLibrary.TestFramework.BeginScenario("NegTest1:Invoke the method and set byteCount as -1.");
        try
        {
            actualValue = uE.GetMaxCharCount(-1);
            TestLibrary.TestFramework.LogError("007", "No ArgumentOutOfRangeException expected.");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    #endregion
}
