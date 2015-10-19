using System;
using TestLibrary; 

/// <summary>
/// Char.ToLower(char)  
/// Converts the value of a Unicode character to its lowercase equivalent. 
/// </summary>
public class CharToLower
{
    public static int Main()
    {
        CharToLower testObj = new CharToLower();

        TestLibrary.TestFramework.BeginTestCase("for method: Char.ToLower(char)");
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

    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = "PosTest1: uppercase character";
        string errorDesc;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            char ch = 'A';
            char expectedChar = GlobLocHelper.OSToLower(ch); // 'a';
            char actualChar = char.ToLower(ch);
            if (actualChar != expectedChar)
            {
                errorDesc = string.Format("Lowercase of character \\u{0:x} is not the value ", (int)ch);
                errorDesc += string.Format("\\u{0:x} as expected: actual(\\u{1:x}", (int)expectedChar, (int)actualChar);
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
        const string c_TEST_DESC = "PosTest2: lowercase character";
        string errorDesc;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            char ch = 'a';
            char expectedChar = GlobLocHelper.OSToLower(ch); // ch;
            char actualChar = char.ToLower(ch);
            if (actualChar != expectedChar)
            {
                errorDesc = string.Format("Lowercase of character \\u{0:x} is not the value ", (int)ch);
                errorDesc += string.Format("\\u{0:x} as expected: actual(\\u{1:x}", (int)expectedChar, (int)actualChar);
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
        const string c_TEST_DESC = "PosTest3: non-alphabetic character";
        string errorDesc;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            char ch = TestLibrary.Generator.GetCharNumber(-55);
            char expectedChar = GlobLocHelper.OSToLower(ch); // ch;
            char actualChar = char.ToLower(ch);
            if (actualChar != expectedChar)
            {
                errorDesc = string.Format("Lowercase of character \\u{0:x} is not the value ", (int)ch);
                errorDesc += string.Format("\\u{0:x} as expected: actual(\\u{1:x}", (int)expectedChar, (int)actualChar);
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
}

