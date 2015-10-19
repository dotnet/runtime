using System;
using System.Globalization;
/// <summary>
/// UnicodeCategory.LetterNumber [v-minch]
/// </summary>
public class UnicodeCategoryLetterNumber
{
    public static int Main()
    {
        UnicodeCategoryLetterNumber unicodeLetterNumber = new UnicodeCategoryLetterNumber();
        TestLibrary.TestFramework.BeginTestCase("UnicodeCategoryLetterNumber");
        if (unicodeLetterNumber.RunTests())
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
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Return the LetterNumber Value in UnicodeCategory Enumerator");
        try
        {
            UnicodeCategory myUnicodeCategory = UnicodeCategory.LetterNumber;
            if (myUnicodeCategory != (UnicodeCategory)9)
            {
                TestLibrary.TestFramework.LogError("001", "the ExpectResult is 9 but the ActualResult is " + myUnicodeCategory.GetHashCode());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}