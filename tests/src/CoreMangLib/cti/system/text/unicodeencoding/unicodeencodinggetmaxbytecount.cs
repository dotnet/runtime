// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

///<summary>
///System.Text.UnicodeEncoding.GetMaxByteCount(int) [v-zuolan]
///</summary>

public class UnicodeEncodingGetMaxByteCount
{

    public static int Main()
    {
        UnicodeEncodingGetMaxByteCount testObj = new UnicodeEncodingGetMaxByteCount();
        TestLibrary.TestFramework.BeginTestCase("for method of System.Text.UnicodeEncoding.GetMaxByteCount");
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
        retVal = NegTest2() && retVal;

        return retVal;
    }


    #region Positive Test Logic
    public bool PosTest1()
    {
        bool retVal = true;

        int expectedValue = 2;
        int actualValue;

        UnicodeEncoding uE = new UnicodeEncoding();

        TestLibrary.TestFramework.BeginScenario("PosTest1:Invoke the method and set charCount as 0.");
        try
        {
            actualValue = uE.GetMaxByteCount(0);

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

        int expectedValue = 4;
        int actualValue;

        UnicodeEncoding uE = new UnicodeEncoding();

        TestLibrary.TestFramework.BeginScenario("PosTest2:Invoke the method and set charCount as 1.");
        try
        {
            actualValue = uE.GetMaxByteCount(1);

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

        int charCount = (TestLibrary.Generator.GetInt32(-55) % Int32.MaxValue + 1) / 2;

        int expectedValue = (charCount + 1) * 2;
        int actualValue;

        UnicodeEncoding uE = new UnicodeEncoding();

        TestLibrary.TestFramework.BeginScenario("PosTest3:Invoke the method and set charCount as random integer.");
        try
        {
            actualValue = uE.GetMaxByteCount(charCount);

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

        TestLibrary.TestFramework.BeginScenario("NegTest1:Invoke the method and set charCount as -1.");
        try
        {
            actualValue = uE.GetMaxByteCount(-1);
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

    public bool NegTest2()
    {
        bool retVal = true;

        int actualValue;

        UnicodeEncoding uE = new UnicodeEncoding();

        TestLibrary.TestFramework.BeginScenario("NegTest2:Invoke the method and set charCount as a large integer that lead the bytecount to overflow..");
        try
        {
            actualValue = uE.GetMaxByteCount(int.MaxValue / 2);
            TestLibrary.TestFramework.LogError("009", "No ArgumentOutOfRangeException expected.");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
