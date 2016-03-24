// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
/// UnicodeCategory.SpacingCombiningMark [v-minch]
/// </summary>
public class UnicodeCategorySpacingCombiningMark
{
    public static int Main()
    {
        UnicodeCategorySpacingCombiningMark unicodeSpacingCombiningMark = new UnicodeCategorySpacingCombiningMark();
        TestLibrary.TestFramework.BeginTestCase("UnicodeCategorySpacingCombiningMark");
        if (unicodeSpacingCombiningMark.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1:Return the SpacingCombiningMark Value in UnicodeCategory Enumerator");
        try
        {
            UnicodeCategory myUnicodeCategory = UnicodeCategory.SpacingCombiningMark;
            if (myUnicodeCategory != (UnicodeCategory)6)
            {
                TestLibrary.TestFramework.LogError("001", "the ExpectResult is 6 but the ActualResult is " + myUnicodeCategory.GetHashCode());
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