// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

///<summary>
///System.Test.UnicodeEncoding.Equals(System.Object) [v-zuolan]
///</summary>

public class UnicodeEncodingEquals
{

    public static int Main()
    {
        UnicodeEncodingEquals testObj = new UnicodeEncodingEquals();
        TestLibrary.TestFramework.BeginTestCase("for field of System.Test.UnicodeEncoding");
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

        return retVal;
    }


    #region Test Logic
    public bool PosTest1()
    {
        bool retVal = true;

        UnicodeEncoding uEncoding1 = new UnicodeEncoding();
        UnicodeEncoding uEncoding2 = new UnicodeEncoding(false,true);

        bool expectedValue = true;
        bool actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Invoke the method with two instance that equal.");
        try
        {
            actualValue = uEncoding1.Equals(uEncoding2);

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

        UnicodeEncoding uEncoding1 = new UnicodeEncoding();
        UnicodeEncoding uEncoding2 = new UnicodeEncoding(false, false);

        bool expectedValue = false;
        bool actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest2:Invoke the method with two instance that not equal.");
        try
        {
            actualValue = uEncoding1.Equals(uEncoding2);

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

        UnicodeEncoding uEncoding = new UnicodeEncoding();

        bool expectedValue = false;
        bool actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest3:Compare the instances between deference type.");
        try
        {
            actualValue = uEncoding.Equals(new TimeSpan());

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
}
