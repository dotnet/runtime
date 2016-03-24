// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using TestLibrary;


public class StringConcat6
{
    private int c_MINI_STRING_LENGTH = 8;
    private int c_MAX_STRING_LENGTH = 256;
    public static int Main()
    {
        StringConcat6 sc6 = new StringConcat6();

        TestLibrary.TestFramework.BeginTestCase("StringConcat6");

        if (sc6.RunTests())
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

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        string ActualResult;
        string strA;
        string strB;
        string strC;

        TestLibrary.TestFramework.BeginScenario("PostTest1:Concat three nulls ");
        try
        {
            strA = null;
            strB = null;
            strC = null;
            ActualResult = string.Concat(strA, strB,strC);
            if (ActualResult != MergeString(strA, strB,strC))
            {
                TestLibrary.TestFramework.LogError("001", "Concat three nulls ExpectResult is" + MergeString(strA, strB,strC) + ",ActualResult is (" + ActualResult + ")");
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
        string strC;

        TestLibrary.TestFramework.BeginScenario("PostTest2:Concat two nulls and one empty");
        try
        {
            strA = null;
            strB = "";
            strC = null;
            ActualResult = string.Concat(strA, strB,strC);
            if (ActualResult != MergeString(strA,strB,strC))
            {
                TestLibrary.TestFramework.LogError("003", "Concat null string and empty ExpectResult is" + MergeString(strA,strB,strC) + " ActualResult is (" + ActualResult + ")");
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
        string strC;
        TestLibrary.TestFramework.BeginScenario("PosTest3:Concat one null and two empty");
        try
        {
            strA = null;
            strB = "";
            strC = "";
            ActualResult = string.Concat(strA, strB,strC);
            if (ActualResult != MergeString(strA,strB,strC))
            {
                TestLibrary.TestFramework.LogError("005", "Concat one null and two empty ExpectResult is equel" + MergeString(strA,strB,strC) + ",ActualResult is (" + ActualResult + ")");
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
        string strC;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Concat three empty");
        try
        {
            strA = "";
            strB = "";
            strC = "";
            ActualResult = string.Concat(strA, strB,strC);
            if (ActualResult != MergeString(strA,strB,strC))
            {
                TestLibrary.TestFramework.LogError("007", "Concat three empty ExpectResult is" + MergeString(strA,strB,strC) + " ,ActualResult is (" + ActualResult + ")");
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
        string strC;
        TestLibrary.TestFramework.BeginScenario("PosTest5:Concat null,space and empty");
        try
        {
            strA = null;
            strB = "\u0020";
            strC = "";
            ActualResult = string.Concat(strA, strB,strC);
            if (ActualResult != MergeString(strA,strB,strC))
            {
                TestLibrary.TestFramework.LogError("009", "Concat null,space and empty ExpectResult is equel" + MergeString(strA, strB, strC) + " ,ActualResult is (" + ActualResult + ")");
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
        string strC;
        TestLibrary.TestFramework.BeginScenario("PosTest6: Concat null,not nullstring and empty");
        try
        {
            strA = null;
            strB = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            strC = "";
            ActualResult = string.Concat(strA, strB,strC);
            if (ActualResult != MergeString(strA,strB,strC))
            {
                TestLibrary.TestFramework.LogError("011", "Concat null,not nullstring and empty ExpectResult is equel" + MergeString(strA,strB,strC) + ",ActualResult is (" + ActualResult + ")");
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
        string strC;
        TestLibrary.TestFramework.BeginScenario("PosTest7: Concat empty and not nullstring");
        try
        {
            strA = null;
            strB = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            strC = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            ActualResult = string.Concat(strA, strB,strC);
            if (ActualResult != MergeString(strA,strB,strC))
            {
                TestLibrary.TestFramework.LogError("013", "Concat empty and not nullstring ExpectResult is equel" + MergeString(strA,strB,strC) + ",ActualResult is (" + ActualResult + ")");
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
        string strC;
        TestLibrary.TestFramework.BeginScenario("PosTest8:Concat one empty and two not nullstrings");
        try
        {
            strA = "";
            strB = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            strC = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            ActualResult = string.Concat(strA, strB,strC);
            if (ActualResult != MergeString(strA,strB,strC))
            {
                TestLibrary.TestFramework.LogError("015", "Concat one empty and two not nullstrings ExpectResult is equel" + MergeString(strA, strB, strC) + " ,ActualResult is (" + ActualResult + ")");
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
        string strC;
        TestLibrary.TestFramework.BeginScenario("PosTest9: Concat three not nullstrings one");
        try
        {
            string strBasic = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            strA = strBasic + "\n";
            strB = strBasic + "\t";
            strC = "\0" + strBasic;
            ActualResult = string.Concat(strA, strB,strC);
            if (ActualResult != MergeString(strA,strB,strC))
            {
                TestLibrary.TestFramework.LogError("017", "Concat three not nullstrings one ExpectResult is equel" + MergeString(strA,strB,strC) + ",ActualResult is (" + ActualResult + ")");
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
        string strC;
        TestLibrary.TestFramework.BeginScenario("PosTest10:Concat three nullstrings");
        try
        {
            strA = "Hello\0Word!\u0020";
            string strb = "\nhellobeijing!\t";
            string strc = "\u0020HELLOEVERYONE!";
            strB = strb.ToUpper();
            strC = strc.ToLower();
            ActualResult = string.Concat(strA, strB,strC);
            if (ActualResult != MergeString(strA,strB,strC))
            {
                TestLibrary.TestFramework.LogError("019", "Concat three nullstrings ExpectResult is equel" + MergeString(strA,strB,strC) + ",ActualResult is (" + ActualResult + ")");
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

    #region Help Method
    private string MergeString(string strA, string strB,string strC)
    {
        string ResultMerge = "";
        if (strA == null || strA == string.Empty)
        {
            ResultMerge += string.Empty;
        }
        else
        {
            ResultMerge += strA.ToString();
        }
        if (strB == null || strB == string.Empty)
        {
            ResultMerge += string.Empty;
        }
        else
        {
            ResultMerge += strB.ToString();
        }
        if (strC == null || strC == string.Empty)
        {
            ResultMerge += string.Empty;
        }
        else
        {
            ResultMerge += strC.ToString();
        }

        return ResultMerge;
    }
    #endregion
}
