// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using TestLibrary;

public class StringCompareOrdinal1
{
    private const int c_MINI_STRINF_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

    public static int Main()
    {
        StringCompareOrdinal1 sco1 = new StringCompareOrdinal1();

        TestLibrary.TestFramework.BeginScenario("StringCompareOrdinal1");

        if (sco1.RunTests())
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
        return retVal;
    }
    #region Positive Testing
    public bool PosTest1()
    {
        string strA;
        string strB;
        int ActualResult;
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Two Null CompareOrdinal");

        try
        {
            strA = null;
            strB = null;
            ActualResult = string.CompareOrdinal(strA, strB);
            if (ActualResult != 0)
            {
                TestLibrary.TestFramework.LogError("001", "Two Null CompareOrdinal Expected Result is equel 0,Actual Result is ( " + ActualResult + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {      
        bool retVal = true;
        string strA;
        string strB;
        int ActualResult;  
        TestLibrary.TestFramework.BeginScenario("PosTest2: Null and null string CompareOrdinal");

        try
        {
            strA = null;
            strB = "";
            ActualResult = string.CompareOrdinal(strA, strB);
            if (ActualResult >= 0)
            {
                TestLibrary.TestFramework.LogError("003", "Null and null string CompareOrdinal Expected Result is less 0,Actual Result is ( " + ActualResult + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        string strA;
        string strB;
        int ActualResult; 

        TestLibrary.TestFramework.BeginScenario("PosTest3: Null and space string CompareOrdinal");

        try
        {
            strA = null;
            strB = " ";
            ActualResult = string.CompareOrdinal(strA, strB);
            if (ActualResult >= 0 )
            {
                TestLibrary.TestFramework.LogError("005", "Null and space string CompareOrdinal Expected Result is less 0,Actual Result is ( " + ActualResult + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        string strA;
        string strB;
        int ActualResult; 
        int ExpectResult = 0;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Null string and a space strings CompareOrdinal");

        try
        {
            strA = ""; 
            strB = " ";
            ActualResult = string.CompareOrdinal(strA, strB);
            if (ActualResult >= ExpectResult)
            {
                TestLibrary.TestFramework.LogError("007", "Null string and a space strings CompareOrdinal Expected Result is less 0;Actual Result is ( " + ActualResult + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        string strA;
        string strB;
        string strBasic;
        Random rand = new Random(-55);
        int ActualResult; 

        TestLibrary.TestFramework.BeginScenario("PosTest5: Two like strings embedded different tabs CompareOrdinal");

        try
        {
            char CharTab = '\t';
            strBasic = TestLibrary.Generator.GetString(-55, false, c_MINI_STRINF_LEN, c_MAX_STRING_LEN);
            strA = strBasic + new string(CharTab, rand.Next(1, 10));
            strB = strBasic + new string(CharTab, rand.Next(11, 20));
            ActualResult = string.CompareOrdinal(strA, strB);
            if (ActualResult >= 0)
            {
                TestLibrary.TestFramework.LogError("009", "Two like strings embedded different tabs CompareOrdinal Expected Result is less 0,Actual Result is ( " + ActualResult + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;
        string strA;
        string strB;
        int ActualResult; 

        TestLibrary.TestFramework.BeginScenario("PosTest6: Two like strings embedded the same tab but differet location CompareOrdinal");

        try
        {
            strA = "hello"+"\t"+"world";
            strB = "\t"+"helloworl"+ "d";
            ActualResult = string.CompareOrdinal(strA, strB);
            if (ActualResult <= 0)
            {
                TestLibrary.TestFramework.LogError("011", "Two like strings embedded the same tab but differet location CompareOrdinal: Expected Result is greater 0,Actual Result is ( " + ActualResult + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;
        string strA;
        string strB;
        int ActualResult; 

        TestLibrary.TestFramework.BeginScenario("PosTest7: String with upper chars and one with lower chars CompareOrdinal");

        try
        {
            strA = "HELLOWORD";
            strB = "helloword";
            ActualResult = string.CompareOrdinal(strA, strB);
            if (ActualResult >= 0 )
            {
                TestLibrary.TestFramework.LogError("013", "String with upper chars and one with lower chars CompareOrdinal Expected Result is equel 0,Actual Result is ( " + ActualResult + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest8()
    {
        bool retVal = true;
        string strA;
        string strB;
        int ActualResult; 

        TestLibrary.TestFramework.BeginScenario("PosTest8: Two strings with ignorecase same char CompareOrdinal");

        try
        {
            strA = "helloword";
            strB = "heLLoword";
            ActualResult = string.CompareOrdinal(strA, strB);
            if (ActualResult <= 0 )
            {
                TestLibrary.TestFramework.LogError("015", "  Two strings with ignorecase same char CompareOrdinal Expected Result is greate 0,Actual Result is ( " + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest9()
    {
        bool retVal = true;
        string strA;
        string strB;
        int ActualResult; 

        TestLibrary.TestFramework.BeginScenario("PosTest9: Two not null strings CompareOrdinal one");

        try
        {
            strA = "hello-word";
            strB = "helloword";
            ActualResult = string.CompareOrdinal(strA, strB);
            if (ActualResult >= 0)
            {
                TestLibrary.TestFramework.LogError("017", "Two not null strings CompareOrdinal one Expected Result is less 0,Actual Result is ( " + ActualResult + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest10()
    {
        bool retVal = true;
        string strA;
        string strB;
        int ActualResult; 

        TestLibrary.TestFramework.BeginScenario("PosTest10: Two not null strings CompareOrdinal two");

        try
        {
            strA = "helloword";
            strB = "hello\nword";
            ActualResult = string.CompareOrdinal(strA, strB);
            if (ActualResult <= 0)
            {
                TestLibrary.TestFramework.LogError("019", " Two not null strings CompareOrdinal two Expected Result is larger 0,Actual Result is ( " + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("020", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest11()
    {
        bool retVal = true;
        string strA;
        string strB;
        int ActualResult; 

        TestLibrary.TestFramework.BeginScenario("PosTest11: Two not null strings CompareOrdinal three");

        try
        {
            strA = "helloword";
            strB = "helloword\n";
            ActualResult = string.CompareOrdinal(strA, strB);
            if (ActualResult >= 0)
            {
                TestLibrary.TestFramework.LogError("021", " Two not null strings CompareOrdinal three Expected Result is less 0,Actual Result is ( " + ActualResult + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("022", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest12()
    {
        bool retVal = true;
        string strA;
        string strB;
        int ActualResult; 

        TestLibrary.TestFramework.BeginScenario("PosTest12: Two like not null strings CompareOrdinal four");

        try
        {
            strA = "helloword";
            strB = "helloword";
            ActualResult = string.CompareOrdinal(strA, strB);
            if (ActualResult != 0)
            {
                TestLibrary.TestFramework.LogError("023", " Two not null strings CompareOrdinal four Expected Result is equel 0,Actual Result is ( " + ActualResult + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("024", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest13()
    {
        bool retVal = true;
        string strA;
        string strB;
        int ActualResult; 

        TestLibrary.TestFramework.BeginScenario("PosTest13: Two not null strings CompareOrdinal five");

        try
        {
            strA = "\uFF21";
            strB = "A";
            ActualResult = string.CompareOrdinal(strA, strB);
            if (ActualResult <= 0 )
            {
                TestLibrary.TestFramework.LogError("025", " Two not null strings CompareOrdinal five Expected Result is greater 0,Actual Result is ( " + ActualResult + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("026", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest14()
    {
        bool retVal = true;
        string strA;
        string strB;
        int ActualResult; 

        TestLibrary.TestFramework.BeginScenario("PosTest14: Two not null strings CompareOrdinal six");

        try
        {
            strA = "\uD801\uDc00";
            strB = "\uD801\uDc28";
            ActualResult = string.CompareOrdinal(strA, strB);
            if (ActualResult >= 0)
            {
                TestLibrary.TestFramework.LogError("027", " Two not null strings CompareOrdinal six Expected Result is less 0,Actual Result is ( " + ActualResult + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("028", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest15()
    {
        bool retVal = true;
        string strA;
        string strB;
        int ActualResult;

        TestLibrary.TestFramework.BeginScenario("PosTest15: Two not null strings CompareOrdinal seven");

        try
        {
            strA = "\x200b";
            strB = "\uFEFF";
            ActualResult = string.CompareOrdinal(strA, strB);
            if (ActualResult >= 0)
            {
                TestLibrary.TestFramework.LogError("029", " Two not null strings CompareOrdinal seven Expected Result is less 0,Actual Result is ( " + ActualResult + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("030", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest16()
    {
        bool retVal = true;
        string strA;
        string strB;
        int ActualResult;

        TestLibrary.TestFramework.BeginScenario("PosTest16: Two not null strings CompareOrdinal eight");

        try
        {
            strA = "A`";
            strB = "\u00c0";
            ActualResult = string.CompareOrdinal(strA, strB);
            if (ActualResult >= 0)
            {
                TestLibrary.TestFramework.LogError("031", " Two not null strings CompareOrdinal nine Expected Result is less 0,Actual Result is ( " + ActualResult + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("032", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest17()
    {
        bool retVal = true;
        string strA;
        string strB;
        int ActualResult;

        TestLibrary.TestFramework.BeginScenario("PosTest17: Two not null strings CompareOrdinal nine");

        try
        {
            strA = "\\\\my documents\\my files\\";
            strB = @"\\my documents\my files\";
            ActualResult = string.CompareOrdinal(strA, strB);
            if (ActualResult != 0)
            {
                TestLibrary.TestFramework.LogError("033", " Two not null strings CompareOrdinal nine Expected Result is equel 0,Actual Result is ( " + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("034", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest18()
    {
        bool retVal = true;
        string strA;
        string strB;
        int ActualResult;

        TestLibrary.TestFramework.BeginScenario("PosTest18: tab and fout spaces CompareOrdinal");

        try
        {
            strA = "\t";
            strB = "    ";
            ActualResult = string.CompareOrdinal(strA, strB);
            if (ActualResult >= 0)
            {
                TestLibrary.TestFramework.LogError("035", " tab and fout spaces CompareOrdinal Expected Result is greater 0,Actual Result is ( " + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("036", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    #endregion



}

