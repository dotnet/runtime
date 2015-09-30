using System;
using System.Globalization; //for NumberFormatInfo
using TestLibrary;

/// <summary>
/// UInt16.System.IConvertible.ToString()
/// Converts the numeric value of this instance to its equivalent string representation. 
/// </summary>
public class UInt16ToString
{
    public static int Main()
    {
        UInt16ToString testObj = new UInt16ToString();

        TestLibrary.TestFramework.BeginTestCase("for method: UInt16.System.ToString()");
        if(testObj.RunTests())
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
        string errorDesc;

        UInt16 uintA;
        string expectedValue;
        string actualValue;

        uintA = (UInt16)(TestLibrary.Generator.GetInt32(-55) % (UInt16.MaxValue + 1));

        TestLibrary.TestFramework.BeginScenario("PosTest1: Random UInt16 value between 0 and UInt16.MaxValue.");
        try
        {
            actualValue = uintA.ToString();
            expectedValue = GlobLocHelper.OSUInt16ToString(uintA);

            if (actualValue != expectedValue)
            {
                errorDesc = 
                    string.Format("The char value of {0} is not the value {1} as expected: actual({2})", 
                    uintA, expectedValue, actualValue);
                TestLibrary.TestFramework.LogError("001", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            TestLibrary.TestFramework.LogError("002", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        string errorDesc;

        UInt16 uintA;
        string expectedValue;
        string actualValue;

        uintA = UInt16.MaxValue;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Value is UInt16.MaxValue.");
        try
        {
            actualValue = uintA.ToString();
            expectedValue = GlobLocHelper.OSUInt16ToString(uintA);

            if (actualValue != expectedValue)
            {
                errorDesc =
                    string.Format("The char value of {0} is not the value {1} as expected: actual({2})",
                    uintA, expectedValue, actualValue);
                TestLibrary.TestFramework.LogError("003", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            TestLibrary.TestFramework.LogError("004", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        string errorDesc;

        UInt16 uintA;
        string expectedValue;
        string actualValue;

        uintA = UInt16.MinValue;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Value is UInt16.MinValue.");
        try
        {
            actualValue = uintA.ToString();
            expectedValue = GlobLocHelper.OSUInt16ToString(uintA);

            if (actualValue != expectedValue)
            {
                errorDesc =
                    string.Format("The char value of {0} is not the value {1} as expected: actual({2})",
                    uintA, expectedValue, actualValue);
                TestLibrary.TestFramework.LogError("005", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            TestLibrary.TestFramework.LogError("006", errorDesc);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}

