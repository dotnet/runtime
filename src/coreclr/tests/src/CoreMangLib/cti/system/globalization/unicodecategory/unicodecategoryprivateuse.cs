using System;
using System.Globalization;
/// <summary>
/// UnicodeCategory.PrivateUse [v-minch]
/// </summary>
public class UnicodeCategoryPrivateUse
{
    public static int Main()
    {
        UnicodeCategoryPrivateUse unicodePrivateUse = new UnicodeCategoryPrivateUse();
        TestLibrary.TestFramework.BeginTestCase("UnicodeCategoryPrivateUse");
        if (unicodePrivateUse.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1:Return the PrivateUse Value in UnicodeCategory Enumerator");
        try
        {
            UnicodeCategory myUnicodeCategory = UnicodeCategory.PrivateUse;
            if (myUnicodeCategory != (UnicodeCategory)17)
            {
                TestLibrary.TestFramework.LogError("001", "the ExpectResult is 17 but the ActualResult is " + myUnicodeCategory.GetHashCode());
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