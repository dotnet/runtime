// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using TestLibrary;


public class StringConcat5
{
    private int c_MINI_STRING_LENGTH = 8;
    private int c_MAX_STRING_LENGTH = 256;
    public static int Main()
    {
        StringConcat5 sc5 = new StringConcat5();

        TestLibrary.TestFramework.BeginTestCase("StringConcat5");

        if (sc5.RunTests())
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

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        string ActualResult;
        string strA;
        string strB;

        TestLibrary.TestFramework.BeginScenario("PostTest1:Concat two null ");
        try
        {
            strA = null;
            strB = null;
            ActualResult = string.Concat(strA, strB);
            if (ActualResult != MergeString(strA,strB))
            {
                TestLibrary.TestFramework.LogError("001", "Concat two null ExpectResult is" + MergeString(strA,strB) + ",ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        string ActualResult;
        string strA;
        string strB;

        TestLibrary.TestFramework.BeginScenario("PostTest2:Concat null and empty");
        try
        {
            strA = null;
            strB = "";
            ActualResult = string.Concat(strA, strB);
            if (ActualResult != MergeString(strA,strB))
            {
                TestLibrary.TestFramework.LogError("003", "Concat null string and empty ExpectResult is" + MergeString(strA,strB) + " ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        string ActualResult;
        string strA;
        string strB;
        TestLibrary.TestFramework.BeginScenario("PosTest3:Concat null and space");
        try
        {
            strA = null;
            strB = "\u0020";
            ActualResult = string.Concat(strA, strB);
            if (ActualResult != MergeString(strA,strB))
            {
                TestLibrary.TestFramework.LogError("005", "Concat null and space ExpectResult is equel" + MergeString(strA,strB) + ",ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        string ActualResult;
        string strA;
        string strB;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Concat empty and space");
        try
        {
            strA = "";
            strB = "\u0020";
            ActualResult = string.Concat(strA,strB);
            if (ActualResult != MergeString(strA,strB))
            {
                TestLibrary.TestFramework.LogError("007", "Concat empty and space ExpectResult is" + MergeString(strA,strB)+ " ,ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        string ActualResult;
        string strA;
        string strB;
        TestLibrary.TestFramework.BeginScenario("PosTest5:Concat \0 and space");
        try
        {
            strA = "\0";
            strB = "u0020";
            ActualResult = string.Concat(strA, strB);
            if (ActualResult != MergeString(strA,strB))
            {
                TestLibrary.TestFramework.LogError("009", "Concat \0 and space ExpectResult is equel" + MergeString(strA,strB) + " ,ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;
        string ActualResult;
        string strA;
        string strB;
        TestLibrary.TestFramework.BeginScenario("PosTest6: Concat null and not nullstring");
        try
        {
            strA = null;
            strB = TestLibrary.Generator.GetString(-55, false,c_MINI_STRING_LENGTH,c_MAX_STRING_LENGTH);
            ActualResult = string.Concat(strA, strB);
            if (ActualResult != MergeString(strA,strB))
            {
                TestLibrary.TestFramework.LogError("011", "Concat null and not nullstring ExpectResult is equel" + MergeString(strA,strB) + ",ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;
        string ActualResult;
        string strA;
        string strB;
        TestLibrary.TestFramework.BeginScenario("PosTest7: Concat empty and not nullstring");
        try
        {
            strA = "";
            strB = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            ActualResult = string.Concat(strA, strB);
            if (ActualResult != MergeString(strA,strB))
            {
                TestLibrary.TestFramework.LogError("013", "Concat empty and not nullstring ExpectResult is equel" + MergeString(strA, strB) + ",ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest8()
    {
        bool retVal = true;
        string ActualResult;
        string strA;
        string strB;
        TestLibrary.TestFramework.BeginScenario("PosTest8:Concat two empty");
        try
        {
            strA = "";
            strB = "";
            ActualResult = string.Concat(strA, strB);
            if (ActualResult != MergeString(strA,strB))
            {
                TestLibrary.TestFramework.LogError("015", "Concat two empty ExpectResult is equel" + MergeString(strA,strB) + " ,ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest9()
    {
        bool retVal = true;
        string ActualResult;
        string strA;
        string strB;
        TestLibrary.TestFramework.BeginScenario("PosTest9: Concat two not nullstrings one");
        try
        {
            string strBasic = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            strA = strBasic + "\n";
            strB = strBasic;
            ActualResult = string.Concat(strA, strB);
            if (ActualResult != MergeString(strA,strB))
            {
                TestLibrary.TestFramework.LogError("017", "Concat two not nullstrings one ExpectResult is equel" + MergeString(strA,strB) + ",ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest10()
    {
        bool retVal = true;
        string ActualResult;
        string strA;
        string strB;
        TestLibrary.TestFramework.BeginScenario("PosTest10:Concat some special symbols ");
        try
        {
            strA = new string('\t', 2);
            strB = "\uffff";
            ActualResult = string.Concat(strA, strB);
            if (ActualResult != MergeString(strA,strB))
            {
                TestLibrary.TestFramework.LogError("019", "Concat some special symbols ExpectResult is equel" + MergeString(strA,strB) + ",ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("020", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest11()
    {
        bool retVal = true;
        string ActualResult;
        string strA;
        string strB;
        TestLibrary.TestFramework.BeginScenario("PosTest11:Concat tab and space ");
        try
        {
            strA = "\u0009";
            strB = "\u0020";
            ActualResult = string.Concat(strA, strB);
            if (ActualResult != MergeString(strA,strB))
            {
                TestLibrary.TestFramework.LogError("021", "Concat tab and space ExpectResult is equel" + MergeString(strA,strB) + ",ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("022", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest12()
    {
        bool retVal = true;
        string ActualResult;
        string strA;
        string strB;
        TestLibrary.TestFramework.BeginScenario("PosTest12:Concat two not nullstrings two ");
        try
        {
            strA = "Hello\t";
            strB = "World";
            ActualResult = string.Concat(strA, strB);
            if (ActualResult != MergeString(strA,strB))
            {
                TestLibrary.TestFramework.LogError("023", "Concat two not nullstrings two ExpectResult is equel" + MergeString(strA, strB) + ",ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("024", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest13()
    {
        bool retVal = true;
        string ActualResult;
        string strA;
        string strB;
        TestLibrary.TestFramework.BeginScenario("PosTest13:Concat two not nullstrings three");
        try
        {
            strA = "Hello\0";
            strB = "World";
            ActualResult = string.Concat(strA, strB);
            if (ActualResult != MergeString(strA,strB))
            {
                TestLibrary.TestFramework.LogError("025", "Concat two not nullstrings three ExpectResult is equel" + MergeString(strA,strB) + ",ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("026", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest14()
    {
        bool retVal = true;
        string ActualResult;
        string strA;
        string strB;
        TestLibrary.TestFramework.BeginScenario("PosTest14:Concat two not nullstrings four");
        try
        {
            strA = "Hello\nWorld";
            strB = "I am here!";
            ActualResult = string.Concat(strA, strB);
            if (ActualResult !=MergeString(strA,strB))
            {
                TestLibrary.TestFramework.LogError("027", "Concat two not nullstrings four ExpectResult is equel" + MergeString(strA,strB) + ",ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("028", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest15()
    {
        bool retVal = true;
        string ActualResult;
        string strA;
        string strB;
        TestLibrary.TestFramework.BeginScenario("PosTest15:Concat two not nullstrings five");
        try
        {
            strA = "Hello\nWorld\tI am here\0....";
            strB = "Welcome to you!";
            ActualResult = string.Concat(strA, strB);
            if (ActualResult !=MergeString(strA,strB))
            {
                TestLibrary.TestFramework.LogError("029", "Concat two not nullstrings five ExpectResult is equel" + MergeString(strA, strB) + ",ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("030", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest16()
    {
        bool retVal = true;
        string ActualResult;
        string strA;
        string strB;

        TestLibrary.TestFramework.BeginScenario("PosTest16:Concat string of two number of less than 0");
        try
        {
            string stra = "hello World! "; 
            string strb = "Come here! ";
            strA = stra.Trim();
            strB = strb.Trim();
            ActualResult = string.Concat(strA, strB);
            if (ActualResult != MergeString(strA,strB))
            {
                TestLibrary.TestFramework.LogError("031", "Concat string of two number of less than 0 ExpectResult is equel" + MergeString(strA,strB) + ",ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("032", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    #region Help Method

    private string MergeString(string strA, string strB)
    {
        if (strA == null || strA == string.Empty)
        {
            if (strB == null || strB == string.Empty)
            {
                return string.Empty;
            }
            else
                return strB.ToString();
        }
        else
            return strA.ToString() + strB.ToString();
    }
    #endregion
}

