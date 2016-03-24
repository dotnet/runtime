// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

///<summary>
///System.Globalization.CharUnicodeInfo.GetUnicodeCategory(System.Char)
///</summary>

public class CharUnicodeInfoGetUnicodeCategory
{

    public static int Main()
    {
        CharUnicodeInfoGetUnicodeCategory testObj = new CharUnicodeInfoGetUnicodeCategory();
        TestLibrary.TestFramework.BeginTestCase("for method of System.Globalization.CharUnicodeInfo.GetUnicodeCategory");
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
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;
        retVal = PosTest7() && retVal;
        retVal = PosTest8() && retVal;
        retVal = PosTest9() && retVal;
        retVal = PosTest10() && retVal;
        retVal = PosTest11() && retVal;
        retVal = PosTest12() && retVal;
        retVal = PosTest13() && retVal;
        retVal = PosTest14() && retVal;
        retVal = PosTest15() && retVal;
        retVal = PosTest16() && retVal;
        retVal = PosTest17() && retVal;
        retVal = PosTest18() && retVal;
        retVal = PosTest19() && retVal;
        retVal = PosTest20() && retVal;
        retVal = PosTest21() && retVal;
        retVal = PosTest22() && retVal;
        retVal = PosTest23() && retVal;
        retVal = PosTest24() && retVal;
        retVal = PosTest25() && retVal;
        retVal = PosTest26() && retVal;
        retVal = PosTest27() && retVal;
        retVal = PosTest28() && retVal;
        retVal = PosTest29() && retVal;
        retVal = PosTest30() && retVal;
        return retVal;
    }


    #region Test Logic
    public bool PosTest1()
    {
        bool retVal = true;

        Char ch = 'A';

        int expectedValue = (int)UnicodeCategory.UppercaseLetter;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Test the method with upper letter");

        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));

            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("001", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ") when char is '" + ch + "'" );
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        Char ch = 'a';

        int expectedValue = (int)UnicodeCategory.LowercaseLetter;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest2:Test the method with low case char");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("003", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        Char ch = '\u1fa8';   //this char is a TitlecaseLetter char

        int expectedValue = (int)UnicodeCategory.TitlecaseLetter;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest3:Test the method with '\\0'");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("005", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }


    public bool PosTest4()
    {
        bool retVal = true;

        Char ch = '\u02b0';

        int expectedValue = (int)UnicodeCategory.ModifierLetter;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest4:Test the method with '\\u02b0'");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("007", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        Char ch = '\u404e';

        int expectedValue = (int)UnicodeCategory.OtherLetter;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest5:Test the method with '\\u404e'");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("009", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;

        Char ch = '\u0300';

        int expectedValue = (int)UnicodeCategory.NonSpacingMark;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest6:Test the method with '\\u0300'");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("011", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;

        Char ch = '\u0903';

        int expectedValue = (int)UnicodeCategory.SpacingCombiningMark;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest7:Test the method with '\\u0903'");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("013", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest8()
    {
        bool retVal = true;

        Char ch = '\u0488';

        int expectedValue = (int)UnicodeCategory.EnclosingMark;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest8:Test the method with '\\u0488'");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("017", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest9()
    {
        bool retVal = true;

        Char ch = '0';

        int expectedValue = (int)UnicodeCategory.DecimalDigitNumber;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest9:Test the method with '0'");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("017", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest10()
    {
        bool retVal = true;

        Char ch = '\u16ee';

        int expectedValue = (int)UnicodeCategory.LetterNumber;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest10:Test the method with '\\u16ee'");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("019", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("020", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest11()
    {
        bool retVal = true;

        Char ch = '\u00b2';

        int expectedValue = (int)UnicodeCategory.OtherNumber;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest11:Test the method with '\\u00b2'");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("021", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("022", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest12()
    {
        bool retVal = true;

        Char ch = '\u0020';

        int expectedValue = (int)UnicodeCategory.SpaceSeparator;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest12:Test the method with '\\u0020'");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("023", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("024", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest13()
    {
        bool retVal = true;

        Char ch = '\u2028';

        int expectedValue = (int)UnicodeCategory.LineSeparator;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest13:Test the method with '\\u2028'");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("025", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("026", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest14()
    {
        bool retVal = true;

        Char ch = '\u2029';

        int expectedValue = (int)UnicodeCategory.ParagraphSeparator;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest14:Test the method with '\\u2029'");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("027", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("028", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest15()
    {
        bool retVal = true;

        Char ch = '\0';

        int expectedValue = (int)UnicodeCategory.Control;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest15:Test the method with '\\0'");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("029", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("030", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest16()
    {
        bool retVal = true;

        Char ch = '\u00ad';

        int expectedValue = (int)UnicodeCategory.Format;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest16:Test the method with '\\u00ad'");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("031", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("032", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest17()
    {
        bool retVal = true;

        Char ch = '\ud800';

        int expectedValue = (int)UnicodeCategory.Surrogate;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest17:Test the method with '\\ud800'");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("033", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("034", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest18()
    {
        bool retVal = true;

        Char ch = '\ue000';

        int expectedValue = (int)UnicodeCategory.PrivateUse;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest18:Test the method with '\\ue000'");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("035", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("036", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest19()
    {
        bool retVal = true;

        Char ch = '\u005f';

        int expectedValue = (int)UnicodeCategory.ConnectorPunctuation;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest19:Test the method with '\\u005f'");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("037", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("038", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest20()
    {
        bool retVal = true;

        Char ch = '-';

        int expectedValue = (int)UnicodeCategory.DashPunctuation;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest20:Test the method with '-'");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("039", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("040", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest21()
    {
        bool retVal = true;

        Char ch = '(';

        int expectedValue = (int)UnicodeCategory.OpenPunctuation;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest21:Test the method with '('");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("041", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("042", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest22()
    {
        bool retVal = true;

        Char ch = ')';

        int expectedValue = (int)UnicodeCategory.ClosePunctuation;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest22:Test the method with ')'");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("043", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("044", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest23()
    {
        bool retVal = true;

        Char ch = '\u00ab';

        int expectedValue = (int)UnicodeCategory.InitialQuotePunctuation;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest23:Test the method with '\\u00ab'");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("045", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("046", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest24()
    {
        bool retVal = true;

        Char ch = '\u00bb';

        int expectedValue = (int)UnicodeCategory.FinalQuotePunctuation;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest24:Test the method with '\\u00bb'");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("047", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("048", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest25()
    {
        bool retVal = true;

        Char ch = '!';

        int expectedValue = (int)UnicodeCategory.OtherPunctuation;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest25:Test the method with '!'");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("049", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("050", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest26()
    {
        bool retVal = true;

        Char ch = '+';

        int expectedValue = (int)UnicodeCategory.MathSymbol;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest26:Test the method with '+'");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("051", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("052", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest27()
    {
        bool retVal = true;

        Char ch = '$';

        int expectedValue = (int)UnicodeCategory.CurrencySymbol;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest27:Test the method with '$'");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("053", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("054", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest28()
    {
        bool retVal = true;

        Char ch = '^';

        int expectedValue = (int)UnicodeCategory.ModifierSymbol;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest28:Test the method with '^'");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("055", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("056", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest29()
    {
        bool retVal = true;

        Char ch = '\u00a6';

        int expectedValue = (int)UnicodeCategory.OtherSymbol;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest29:Test the method with '\\u00a6'");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("057", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("058", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest30()
    {
        bool retVal = true;

        Char ch = '\u0242';

        int expectedValue = (int)UnicodeCategory.LowercaseLetter;

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest30:Test the method with '\\u0242'");
        try
        {
            actualValue = (int)(CharUnicodeInfo.GetUnicodeCategory(ch));
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("059", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("060", "when char is '" + ch + "',Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
