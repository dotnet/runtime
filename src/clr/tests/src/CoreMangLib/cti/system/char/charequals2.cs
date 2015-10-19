using System;

/// <summary>
/// Char.Equals(Object)  
/// Note: This method is new in the .NET Framework version 2.0. 
/// Returns a value indicating whether this instance is equal to the specified Char object.   
/// </summary>
public class CharEquals
{
    public static int Main()
    {
        CharEquals testObj = new CharEquals();

        TestLibrary.TestFramework.BeginTestCase("for method: Char.Equals(Object)");
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
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = @"PosTest1: char.MaxValue vs '\uFFFF'";
        string errorDesc;

        const char c_MAX_CHAR = '\uFFFF';

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            char actualChar = char.MaxValue;
            object obj = c_MAX_CHAR;
            bool result = actualChar.Equals(obj);
            if (!result)
            {
                errorDesc = "Char.MaxValue is not " + c_MAX_CHAR + " as expected: Actual(" + actualChar + ")";
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_ID = "P002";
        const string c_TEST_DESC = @"PosTest2: char.MinValue vs '\u0000'";
        string errorDesc;

        const char c_MIN_CHAR = '\u0000';

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            char actualChar = char.MinValue;
            object obj = c_MIN_CHAR;
            bool result = actualChar.Equals(obj);
            if (!result)
            {
                errorDesc = "char.MinValue is not " + c_MIN_CHAR + " as expected: Actual(" + actualChar + ")";
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_ID = "P003";
        const string c_TEST_DESC = "PosTest3: char.MaxValue vs char.MinValue";
        string errorDesc;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            bool expectedValue = false;
            object obj = char.MinValue;
            bool actualValue = char.MaxValue.Equals(obj);
            if (actualValue != expectedValue)
            {
                errorDesc = @"char.MaxValue('\uFFFF') does not equal char.MinValue('\u0000')";
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        const string c_TEST_ID = "P004";
        const string c_TEST_DESC = "PosTest4: equality of two random charaters";
        string errorDesc;

        char chA, chB;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            chA = TestLibrary.Generator.GetChar(-55);
            chB = TestLibrary.Generator.GetChar(-55);
            object obj = chB;
            bool expectedValue = (int)chA == (int)chB;
            bool actualValue = chA.Equals(obj);
            if (actualValue != expectedValue)
            {
                errorDesc = string.Format("The equality of character \'\\u{0:x}\' against character \'\\u{1:x}\' is not {2} as expected: Actual({3})",
                    (int)chA, (int)chB, expectedValue, actualValue);
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        const string c_TEST_ID = "P004";
        const string c_TEST_DESC = "PosTest4: char vs 32-bit integer value";
        string errorDesc;

        char chA;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            chA = TestLibrary.Generator.GetChar(-55);
            object obj = (int)chA;
            bool expectedValue = false;
            bool actualValue = chA.Equals(obj);
            if (actualValue != expectedValue)
            {
                errorDesc = string.Format("The equality of character \'\\u{0:x}\' against 32-bit integer {1:x} is not {2} as expected: Actual({3})",
                    (int)chA, (int)obj, expectedValue, actualValue);
                TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}

