// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using TestLibrary;


public class StringConcat7
{
    private int c_MINI_STRING_LENGTH = 8;
    private int c_MAX_STRING_LENGTH = 256;
    public static int Main()
    {
        StringConcat7 sc7 = new StringConcat7();

        TestLibrary.TestFramework.BeginTestCase("StringConcat7");

        if (sc7.RunTests())
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

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        string ActualResult;
        string strA;
        string strB;
        string strC;
        string strD;

        TestLibrary.TestFramework.BeginScenario("PostTest1:Concat four nulls ");
        try
        {
            strA = null;
            strB = null;
            strC = null;
            strD = null;
            ActualResult = string.Concat(strA, strB, strC,strD);
            if (ActualResult != MergeString(strA, strB, strC,strD))
            {
                TestLibrary.TestFramework.LogError("001", "Concat four nulls ExpectResult is" + MergeString(strA, strB, strC,strD) + ",ActualResult is (" + ActualResult + ")");
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
        string strD;

        TestLibrary.TestFramework.BeginScenario("PostTest2:Concat three nulls and empty");
        try
        {
            strA = null;
            strB = "";
            strC = null;
            strD = null;
            ActualResult = string.Concat(strA, strB, strC,strD);
            if (ActualResult != MergeString(strA, strB, strC,strD))
            {
                TestLibrary.TestFramework.LogError("003", "Concat three nulls and empty ExpectResult is" + MergeString(strA, strB, strC,strD) + " ActualResult is (" + ActualResult + ")");
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
        string strD;
        TestLibrary.TestFramework.BeginScenario("PosTest3:Concat two nulls and two empty");
        try
        {
            strA = null;
            strB = "";
            strC = "";
            strD = null;
            ActualResult = string.Concat(strA, strB, strC,strD);
            if (ActualResult != MergeString(strA, strB, strC,strD))
            {
                TestLibrary.TestFramework.LogError("005", "Concat two nulls and two empty ExpectResult is equel" + MergeString(strA, strB, strC,strD) + ",ActualResult is (" + ActualResult + ")");
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
        string strD;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Concat three empty and null");
        try
        {
            strA = "";
            strB = "";
            strC = "";
            strD = null;
            ActualResult = string.Concat(strA, strB, strC,strD);
            if (ActualResult != MergeString(strA, strB, strC,strD))
            {
                TestLibrary.TestFramework.LogError("007", "Concat three empty and null ExpectResult is" + MergeString(strA, strB, strC,strD) + " ,ActualResult is (" + ActualResult + ")");
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
        string strD;
        TestLibrary.TestFramework.BeginScenario("PosTest5:Concat four empty");
        try
        {
            strA = "";
            strB = "";
            strC = "";
            strD = "";
            ActualResult = string.Concat(strA, strB, strC,strD);
            if (ActualResult != MergeString(strA, strB, strC,strD))
            {
                TestLibrary.TestFramework.LogError("009", "Concat four empty ExpectResult is equel" + MergeString(strA, strB, strC,strD) + " ,ActualResult is (" + ActualResult + ")");
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
        string strD;
        TestLibrary.TestFramework.BeginScenario("PosTest6: Concat null,not nullstring and empty");
        try
        {
            strA = null;
            strB = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            strC = "";
            strD = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            ActualResult = string.Concat(strA, strB, strC,strD);
            if (ActualResult != MergeString(strA, strB, strC,strD))
            {
                TestLibrary.TestFramework.LogError("011", "Concat null,not nullstring and empty ExpectResult is equel" + MergeString(strA, strB, strC,strD) + ",ActualResult is (" + ActualResult + ")");
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
        string strD;
        TestLibrary.TestFramework.BeginScenario("PosTest7: Concat two nulls and not nullstring");
        try
        {
            strA = null;
            strB = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            strC = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            strD = null;
            ActualResult = string.Concat(strA, strB, strC,strD);
            if (ActualResult != MergeString(strA, strB, strC,strD))
            {
                TestLibrary.TestFramework.LogError("013", "Concat empty and not nullstring ExpectResult is equel" + MergeString(strA, strB, strC,strD) + ",ActualResult is (" + ActualResult + ")");
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
        string strD;
        TestLibrary.TestFramework.BeginScenario("PosTest8:Concat two empty and two not nullstrings");
        try
        {
            strA = "";
            strB = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            strC = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            strD = "";
            ActualResult = string.Concat(strA, strB, strC,strD);
            if (ActualResult != MergeString(strA, strB, strC,strD))
            {
                TestLibrary.TestFramework.LogError("015", "Concat two empty and two not nullstrings ExpectResult is equel" + MergeString(strA, strB, strC,strD) + " ,ActualResult is (" + ActualResult + ")");
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
        string strD;
        TestLibrary.TestFramework.BeginScenario("PosTest9: Concat three not nullstrings and null");
        try
        {
            string strBasic = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            strA = strBasic + "\n";
            strB = strBasic + "\t";
            strC = "\0" + strBasic;
            strD = null;
            ActualResult = string.Concat(strA, strB, strC,strD);
            if (ActualResult != MergeString(strA, strB, strC,strD))
            {
                TestLibrary.TestFramework.LogError("017", "Concat three not nullstrings and null ExpectResult is equel" + MergeString(strA, strB, strC,strD) + ",ActualResult is (" + ActualResult + ")");
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
        string strD;
        TestLibrary.TestFramework.BeginScenario("PosTest10:Concat three not nullstrings and empty");
        try
        {
            strA = "Hello\0Word!\u0020";
            string strb = "\nhellobeijing!\t";
            string strc = "\u0020HELLOEVERYONE!";
            strB = strb.ToUpper();
            strC = strc.ToLower();
            strD = "";
            ActualResult = string.Concat(strA, strB, strC,strD);
            if (ActualResult != MergeString(strA, strB, strC,strD))
            {
                TestLibrary.TestFramework.LogError("019", "Concat three not nullstrings and empty ExpectResult is equel" + MergeString(strA, strB, strC,strD) + ",ActualResult is (" + ActualResult + ")");
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
        string strC;
        string strD;
        TestLibrary.TestFramework.BeginScenario("PosTest11:Concat four nullstrings");
        try
        {
            strA = "Hello\0Word!\u0020";
            string strb = "\nhellobeijing!\t";
            string strc = "\u0020HELLOEVERYONE!";
            strB = strb.ToUpper();
            strC = strc.ToLower();
            strD = TestLibrary.Generator.GetString(-55, false,c_MINI_STRING_LENGTH,c_MAX_STRING_LENGTH);
            ActualResult = string.Concat(strA, strB, strC,strD);
            if (ActualResult != MergeString(strA, strB, strC,strD))
            {
                TestLibrary.TestFramework.LogError("021", "Concat four nullstrings ExpectResult is equel" + MergeString(strA, strB, strC,strD) + ",ActualResult is (" + ActualResult + ")");
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

    #region Help Method
    private string MergeString(string strA, string strB, string strC,string strD)
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
        if (strD == null || strD == string.Empty)
        {
            ResultMerge += string.Empty;
        }
        else
        {
            ResultMerge += strD.ToString();
        }

        return ResultMerge;
    }
    #endregion

}

