// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Convert.ToSingle(String)
/// </summary>
public class ConvertToSingle13
{
    public static int Main()
    {
        ConvertToSingle13 testObj = new ConvertToSingle13();

        TestLibrary.TestFramework.BeginTestCase("for method: Convert.ToSingle(String)");
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

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1();
        retVal = NegTest2();
        retVal = NegTest3();
        retVal = NegTest4();
        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest1: Verfify value is a vaild string ... ";
        string c_TEST_ID = "P001";

        string actualValue = TestLibrary.Generator.GetSingle(-55).ToString();

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {

            Single resValue = Convert.ToSingle(actualValue);
            if (Single.Parse(actualValue) != resValue)
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest2: Verfify value is a null reference... ";
        string c_TEST_ID = "P002";

        String actualValue = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Single resValue = Convert.ToSingle(actualValue);
            if (0 != resValue)
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is 0";
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest3: Verfify value is a string end with a radix point... ";
        string c_TEST_ID = "P003";

        String actualValue = "7923.";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Single resValue = Convert.ToSingle(actualValue);
            Single realValue = Single.Parse(actualValue);
            if (realValue != resValue)
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is" + realValue;
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest4: Verfify value is a string started with a radix point... ";
        string c_TEST_ID = "P004";

        String actualValue = ".7923";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Single resValue = Convert.ToSingle(actualValue);
            Single realValue = Single.Parse(actualValue);

            if (realValue != resValue)
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + realValue;
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest5: Verfify value is Single.NaN... ";
        string c_TEST_ID = "P005";

        String actualValue = Single.NaN.ToString();

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Single resValue = Convert.ToSingle(actualValue);

            if (!Single.IsNaN(resValue))
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + Single.NaN;
                TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest6: Verfify value is Single.NegativeInfinity... ";
        string c_TEST_ID = "P006";

        String actualValue = Single.NegativeInfinity.ToString();

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Single resValue = Convert.ToSingle(actualValue);

            if (!Single.IsNegativeInfinity(resValue))
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + Single.NegativeInfinity;
                TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region NegativeTesting
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest1: Value represents a number greater than Single.MaxValue ";
        const string c_TEST_ID = "N001";


        string actualValue = "3.40292349638528859e+38";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            Convert.ToSingle(actualValue);
            TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, "OverflowException is not thrown as expected.");
            retVal = false;
        }
        catch (OverflowException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    public bool NegTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest2: Value represents a number less than Single.MinValue ";
        const string c_TEST_ID = "N002";


        string actualValue = "-3.40362346638528859e+38";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            Convert.ToSingle(actualValue);
            TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, "OverflowException is not thrown as expected.");
            retVal = false;
        }
        catch (OverflowException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    public bool NegTest3()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest3: value is a string cantains invalid chars ";
        const string c_TEST_ID = "N003";


        string actualValue = "1622.7sd23";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            Convert.ToSingle(actualValue);
            TestLibrary.TestFramework.LogError("013" + " TestId-" + c_TEST_ID, "FormatException is not thrown as expected.");
            retVal = false;
        }
        catch (FormatException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest4: value is a empty string ";
        const string c_TEST_ID = "N004";


        string actualValue = "";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            Convert.ToSingle(actualValue);
            TestLibrary.TestFramework.LogError("015" + " TestId-" + c_TEST_ID, "FormatException is not thrown as expected.");
            retVal = false;
        }
        catch (FormatException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    #endregion
}
