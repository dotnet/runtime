using System;

/// <summary>
/// Convert.ToDouble(System.UInt32)
/// </summary>
public class ConvertToDouble16
{
    public static int Main()
    {
        ConvertToDouble16 testObj = new ConvertToDouble16();

        TestLibrary.TestFramework.BeginTestCase("for method: Convert.ToDouble(System.UInt32)");
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

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest1: Verify value is a random UInt32... ";
        string c_TEST_ID = "P001";

        UInt32 actualValue = Convert.ToUInt32(TestLibrary.Generator.GetInt64(-55) % System.UInt32.MaxValue);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Double resValue = Convert.ToDouble(actualValue);
            if (actualValue != resValue)
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
        string c_TEST_DESC = "PosTest2: Verify value is UInt32.MaxValue... ";
        string c_TEST_ID = "P002";

        UInt32 actualValue = UInt32.MaxValue;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Double resValue = Convert.ToDouble(actualValue);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
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
        string c_TEST_DESC = "PosTest3: Verify value is UInt32.MinValue... ";
        string c_TEST_ID = "P003";

        UInt32 actualValue = UInt32.MinValue;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Double resValue = Convert.ToDouble(actualValue);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
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
    #endregion
}
