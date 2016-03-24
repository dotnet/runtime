// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using TestLibrary;


public class StringConcat8
{
    private int c_MAX_STRING_LENGTH = 256;
    private int c_MINI_STRING_LENGTH = 8;

    public static int Main()
    {
        StringConcat8 sc8 = new StringConcat8();

        TestLibrary.TestFramework.BeginTestCase("StringConcat8");

        if (sc8.RunTests())
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        Random rand = new Random(-55);
        string ActualResult;
        string[] strA = new string[rand.Next(c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH)];

        TestLibrary.TestFramework.BeginScenario("PostTest1:Concat string Array with all null member");
        try
        {
            for (int i = 0; i < strA.Length; i++)
            {
                strA[i] = null;
            }
            ActualResult = string.Concat(strA);
            if (ActualResult != MergeStrings(strA))
            {
                TestLibrary.TestFramework.LogError("001", "Concat string Array with all null member ExpectResult is" + MergeStrings(strA) + "ActualResult is (" + ActualResult + ")");
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
        Random rand = new Random(-55);
        string ActualResult;
        string[] strA = new string[rand.Next(c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH)];

        TestLibrary.TestFramework.BeginScenario("PostTest2:Concat string Array with all empty member");
        try
        {
            for (int i = 0; i < strA.Length; i++)
            {
                strA[i] = "";
            }
            ActualResult = string.Concat(strA);
            if (ActualResult != MergeStrings(strA))
            {
                TestLibrary.TestFramework.LogError("003", "Concat string Array with all empty member ExpectResult is" + MergeStrings(strA) + " ActualResult is (" + ActualResult + ")");
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
        string[] strA;
        TestLibrary.TestFramework.BeginScenario("PosTest3:Concat string Array with null and empty member");
        try
        {
            strA = new string[] { null, "", null, "", null, "" };
            ActualResult = string.Concat(strA);
            if (ActualResult != MergeStrings(strA))
            {
                TestLibrary.TestFramework.LogError("005", "Concat string Array with null and empty member ExpectResult is equel" + MergeStrings(strA) + ",ActualResult is (" + ActualResult + ")");
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
        string[] strA;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Concat string Array with single letter member");
        try
        {
            strA = new string[] {"a","b","c","d","e","f"};
            ActualResult = string.Concat(strA);
            if (ActualResult != MergeStrings(strA))
            {
                TestLibrary.TestFramework.LogError("007", "Concat string Array with single letter member ExpectResult is" + MergeStrings(strA) + " ,ActualResult is (" + ActualResult + ")");
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
        string[] strA;
        TestLibrary.TestFramework.BeginScenario("PosTest5:Concat string Array with null,empty and not nullstrings member");
        try
        {
            strA = new string[] {null,"a","null","",null,"123"};
            ActualResult = string.Concat(strA);
            if (ActualResult != MergeStrings(strA))
            {
                TestLibrary.TestFramework.LogError("009", "Concat string Array with null,empty and not nullstrings member ExpectResult is equel" + MergeStrings(strA) + " ,ActualResult is (" + ActualResult + ")");
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
        string[] strA;
        TestLibrary.TestFramework.BeginScenario("PosTest6: Concat string Array with not nullstring and some symbols member one");
        try
        {
            string str1 = "HelloWorld";
            string str2 = "\n";
            string str3 = "\t";
            string str4 = "\0";
            string str5 = "\u0041";
            strA = new string[] { str1,str2,str3,str4,str5 };
            ActualResult = string.Concat(strA);
            if (ActualResult != MergeStrings(strA))
            {
                TestLibrary.TestFramework.LogError("011", "Concat string Array with not nullstring and some symbols member ExpectResult is equel" + MergeStrings(strA) + ",ActualResult is (" + ActualResult + ")");
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
        string[] strA;
        TestLibrary.TestFramework.BeginScenario("PosTest7: Concat string Array with not nullstrings and some symbols member two");
        try
        {
            string str1 = "hello";
            string str2 = "\u0020";
            string str3 = "World";
            string str4 = "\u0020";
            string str5 = "!";
            strA = new string[] { str1, str2, str3, str4, str5 };
            ActualResult = string.Concat(strA);
            if (ActualResult != MergeStrings(strA))
            {
                TestLibrary.TestFramework.LogError("013", "Concat string Array with some strings and some symbols member two ExpectResult is equel" + MergeStrings(strA) + ",ActualResult is (" + ActualResult + ")");
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
        string[] strA;
        TestLibrary.TestFramework.BeginScenario("PosTest8:Concat string Array with some not nullstrings member one");
        try
        {
            string str1 = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            string str2 = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            string str3 = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            string str4 = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            string str5 = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            strA = new string[] { str1, str2, str3, str4, str5 };
            ActualResult = string.Concat(strA);
            if (ActualResult != MergeStrings(strA))
            {
                TestLibrary.TestFramework.LogError("015", "Concat string Array with some not nullstrings member one ExpectResult is equel" + MergeStrings(strA) + " ,ActualResult is (" + ActualResult + ")");
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
        string[] strA;
        TestLibrary.TestFramework.BeginScenario("PosTest9:Concat object Array with  some not nullstrings member two");
        try
        {
            string str1 = "helloworld".ToUpper() +"\n";
            string str2 = "hello\0world".ToUpper() + "\n";
            string str3 = "HELLOWORLD".ToLower() + "\n";
            string str4 = "HelloWorld".ToUpper() + "\n";
            string str5 = "hello world".Trim();
            strA = new string[] { str1, str2, str3, str4, str5 };
            ActualResult = string.Concat(strA);
            if (ActualResult != MergeStrings(strA))
            {
                TestLibrary.TestFramework.LogError("015", "Concat object Array with some not nullstrings member two ExpectResult is equel" + MergeStrings(strA) + " ,ActualResult is (" + ActualResult + ")");
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

    #endregion

    #region NegativeTesting
    public bool NegTest1()
    {
        bool retVal = true;
        Random rand = new Random(-55);

        TestLibrary.TestFramework.BeginScenario("NegTest1: Concat string Array is null");

        string[] strA = new string[rand.Next(c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH)];
        string ActualResult;
        try
        {
            strA = null;
            ActualResult = string.Concat(strA);
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;
        Random rand = new Random(-55);
        string[] strA = new string[50 * 1024 * 1024];
        string ActualResult;

        TestLibrary.TestFramework.BeginScenario("NegTest2: Concat string Array with many strings");

        int TotalLength = 0;
        try
        {
            for (int i = 0; i < strA.Length; i++)
            {
                strA[i] = "HelloworldHelloworldHelloworldHelloworld!";
                TotalLength += strA[i].ToString().Length;
            }
            ActualResult = string.Concat(strA);
            retVal = false;
        }
        catch (OutOfMemoryException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    #region Help method
    private string MergeStrings(string[] strS)
    {
        string ResultString = "";
        foreach (string str in strS)
        {
            if (str == null|| str ==string.Empty )
                ResultString += string.Empty;
            else
                ResultString += str.ToString();
        }
        return ResultString;
    }

    #endregion

    #endregion
}

