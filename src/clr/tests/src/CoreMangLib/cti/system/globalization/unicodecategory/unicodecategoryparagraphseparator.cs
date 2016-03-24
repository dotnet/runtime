// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
/// UnicodeCategory.ParagraphSeparator [v-minch]
/// </summary>
public class UnicodeCategoryParagraphSeparator
{
    public static int Main()
    {
        UnicodeCategoryParagraphSeparator unicodeParagraphSeparator = new UnicodeCategoryParagraphSeparator();
        TestLibrary.TestFramework.BeginTestCase("UnicodeCategoryParagraphSeparator");
        if (unicodeParagraphSeparator.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1:Return the ParagraphSeparator Value in UnicodeCategory Enumerator");
        try
        {
            UnicodeCategory myUnicodeCategory = UnicodeCategory.ParagraphSeparator;
            if (myUnicodeCategory != (UnicodeCategory)13)
            {
                TestLibrary.TestFramework.LogError("001", "the ExpectResult is 13 but the ActualResult is " + myUnicodeCategory.GetHashCode());
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