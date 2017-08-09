using System;
using System.Collections.Generic;
using System.Text;
using TestLibrary;


public class StringCompareOrdinal2
{
    private const int c_MINI_STRINF_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

    private string strA;
    private int indexA;
    private string strB;
    private int indexB;
    private int length;

    public static int Main()
    {
        StringCompareOrdinal2 sco2 = new StringCompareOrdinal2();
	TestLibrary.TestFramework.BeginTestCase("StringCompareOrdinal2");
        TestLibrary.TestFramework.BeginScenario("StringCompareOrdinal2");

        if (sco2.RunTests())
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
        retVal = PosTest19() && retVal;
        retVal = PosTest20() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;
        retVal = NegTest6() && retVal;
        retVal = NegTest7() && retVal;
        retVal = NegTest8() && retVal;
        retVal = NegTest9() && retVal;
        retVal = NegTest10() && retVal;
        retVal = NegTest11() && retVal;
        retVal = NegTest12() && retVal;
        retVal = NegTest13() && retVal;
        retVal = NegTest14() && retVal;
        retVal = NegTest15() && retVal;
        retVal = NegTest16() && retVal;
        retVal = NegTest17() && retVal;

        return retVal;
    }

    #region Positive Testing
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Two empty Strings CompareOrdinal");

        try
        {
            strA = String.Empty;
            strB = String.Empty;
            indexA = 0;
            indexB = 0;
            length = 0;
            int ExpectResult = 0;

                int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
                if (ActualResult != ExpectResult)
                {
                    TestLibrary.TestFramework.LogError("001", "Null Strings CompareOrdinal Expected Result is (" + ExpectResult + ");Actual Result is ( " + ActualResult + ")");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:NullString and not nullString CompareOrdinal");
        Random rand = new Random(-55);
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, 0, 0);
            strB = TestLibrary.Generator.GetString(-55, false, c_MINI_STRINF_LEN, c_MAX_STRING_LEN);
            indexA = rand.Next(0, strA.Length);
            indexB = rand.Next(0, strB.Length);
            length = rand.Next();
            if (indexA == 0 && indexB >= 0 && indexB <= strB.Length && length >= 0)
            {
                int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
                if (ActualResult > 0 || ActualResult == 0)
                {
                    TestLibrary.TestFramework.LogError("003", "Null string and not null string CompareOrdinal Expected Result is less 0£¬Actual Result is ( " + ActualResult + ")");
                    retVal = false;
                }
            }
            else
                TestLibrary.TestFramework.LogError("003", "Index or Length Number is out of range");

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
        TestLibrary.TestFramework.BeginScenario("PosTest3:Not nullString and nullString CompareOrdinal");
        Random rand = new Random(-55);
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRINF_LEN, c_MAX_STRING_LEN);
            strB = TestLibrary.Generator.GetString(-55, false, 0, 0);
            indexA = rand.Next(0, strA.Length);
            indexB = rand.Next(0, strB.Length);
            length = rand.Next();
            if (indexB == 0 && indexA >= 0 && indexA <= strA.Length && length >= 0)
            {
                int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
                if (ActualResult < 0 || ActualResult == 0)
                {
                    TestLibrary.TestFramework.LogError("005", "Null string and not null string CompareOrdinal Expected Result is less 0,Actual Result is ( " + ActualResult + ")");
                    retVal = false;
                }
            }
            else
                TestLibrary.TestFramework.LogError("005", "Index or Length Number is out of range");

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4:Two like not NullStrings CompareOrdinal one");
        Random rand = new Random(-55);
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRINF_LEN, c_MAX_STRING_LEN);
            strB = strA.ToString();
            indexA = rand.Next(0, strA.Length);
            indexB = indexA;
            length = rand.Next();

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
            if (ActualResult != 0)
            {
                TestLibrary.TestFramework.LogError("007", "Two like not NullStrings CompareOrdinal one ExpectResult is 0,ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest5:Two like not NullStrings CompareOrdinal eighteen");
        Random rand = new Random(-55);
        try
        {
            strA = "heloredmon!";
            strB = strA.ToString();
            indexA = 3;
            indexB = 4;
            length = 2;

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
            if (ActualResult >= 0)
            {
                TestLibrary.TestFramework.LogError("026", "Two like not NullStrings CompareOrdinal eighteen ExpectResult is less 0,ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("027", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest6:Two like not NullStrings CompareOrdinal nineteen");
        Random rand = new Random(-55);
        try
        {
            strA = "This\0string\0js\0validghjgj";
            strB = "This\0string\0is\0valid";
            indexA = 0;
            indexB = 0;
            length = 13;

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
            if (ActualResult <= 0)
            {
                TestLibrary.TestFramework.LogError("026", "Two like not NullStrings CompareOrdinal eighteen ExpectResult is less 0,ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("027", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest7:Two like not NullStrings CompareOrdinal twenty");
        Random rand = new Random(-55);
        try
        {
            strA = "abccde";
            strB = "abcccde";
            indexA = 3;//strA.Length - indexA = 2
            indexB = 4;//strB.Length - indexB = 3
            length = 4;//length               = 4

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
            if (ActualResult != 0)
            {
                TestLibrary.TestFramework.LogError("028", "Two like not NullStrings CompareOrdinal eighteen ExpectResult is equel 0,ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("029", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest8()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest8:Two like not NullStrings CompareOrdinal twentyone");
        Random rand = new Random(-55);
        try
        {
            strA = "adsfasd";
            strB = "ADSFASD";
            indexA = 3;
            indexB = 4;
            length = 0;

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
            if (ActualResult != 0)
            {
                TestLibrary.TestFramework.LogError("030", "Two like not NullStrings CompareOrdinal eighteen ExpectResult is equel 0,ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("031", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest9()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest9:Two like not NullStrings CompareOrdinal twentytwo");
        Random rand = new Random(-55);
        try
        {
            strA = null;
            strB = null;
            indexA = rand.Next();
            indexB = rand.Next();
            length = rand.Next();

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
            if (ActualResult != 0)
            {
                TestLibrary.TestFramework.LogError("031", "Two like not NullStrings CompareOrdinal eighteen ExpectResult is equel 0,ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("032", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest10()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest10:Two like not NullStrings CompareOrdinal twentythree");
        Random rand = new Random(-55);
        try
        {
            strA = null;
            strB = "asdfasdf";
            indexA = rand.Next();
            indexB = rand.Next(0, strB.Length);
            length = rand.Next();

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
            if (ActualResult >= 0)
            {
                TestLibrary.TestFramework.LogError("033", "Two like not NullStrings CompareOrdinal eighteen ExpectResult is less 0,ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("034", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest11()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest11:Two like not NullStrings CompareOrdinal twentyfour");
        Random rand = new Random(-55);
        try
        {
            strA = null;
            strB = "asdfasdf";
            indexA = rand.Next();
            indexB = rand.Next(strB.Length, strB.Length + 10);
            length = rand.Next();

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
            if (ActualResult >= 0)
            {
                TestLibrary.TestFramework.LogError("035", "Two like not NullStrings CompareOrdinal eighteen ExpectResult is less 0,ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("036", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest12()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest12:Two like not NullStrings CompareOrdinal twentyfive");
        Random rand = new Random(-55);
        try
        {
            strA = null;
            strB = "";
            indexA = rand.Next();
            indexB = rand.Next();
            length = rand.Next();

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
            if (ActualResult >= 0)
            {
                TestLibrary.TestFramework.LogError("038", "Two like not NullStrings CompareOrdinal eighteen ExpectResult is greater 0,ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("039", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest13()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest13:Two like not NullStrings CompareOrdinal twentyseven");
        Random rand = new Random(-55);
        try
        {
            strA = "    "; //four spaces
            strB = "    "; //one tab
            indexA = rand.Next(0, strA.Length);
            indexB = indexA;
            length = rand.Next();

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
            if (ActualResult != 0)
            {
                TestLibrary.TestFramework.LogError("040", "Two like not NullStrings CompareOrdinal eighteen ExpectResult is equel 0,ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("041", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest14()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest14:Two like not NullStrings CompareOrdinal twentyeight");
        Random rand = new Random(-55);
        try
        {
            strA = "    "; //four spaces
            strB = "    "; //one tab
            indexA = rand.Next(0, strA.Length);
            indexB = indexA + 1;
            length = rand.Next();

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
            if (ActualResult <= 0)
            {
                TestLibrary.TestFramework.LogError("042", "Two like not NullStrings CompareOrdinal eighteen ExpectResult is greater 0,ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("043", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest15()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest15:Two like not NullStrings CompareOrdinal twentynine");
        Random rand = new Random(-55);
        try
        {
            strA = "    "; //four spaces
            strB = "    "; //one tab
            indexB = rand.Next(0, strB.Length);
            indexA = indexB + 1;
            length = rand.Next();

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
            if (ActualResult >= 0)
            {
                TestLibrary.TestFramework.LogError("044", "Two like not NullStrings CompareOrdinal eighteen ExpectResult is less 0,ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("045", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }


    public bool PosTest16()
    {
        bool retVal = true;
        int ActualResult;

        TestLibrary.TestFramework.BeginScenario("PosTest16: Two not null strings CompareOrdinal eight");

        try
        {
            strA = "\\\\my documents\\my files\\";
            strB = @"\\my documents\my files\";
            indexA = 0;
            indexB = 0;
            length = 10;
            ActualResult = string.CompareOrdinal(strA, strB);
            if (ActualResult != 0)
            {
                TestLibrary.TestFramework.LogError("046", " Two not null strings CompareOrdinal seven Expected Result is equel 0,Actual Result is ( " + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("047", "Unexpected exception: " + e);
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

        TestLibrary.TestFramework.BeginScenario("PosTest17: Two not null strings CompareOrdinal six");

        try
        {
            strA = "\uD801\uDc00";
            strB = "\uD801\uDc28";
            ActualResult = string.CompareOrdinal(strA, strB);
            if (ActualResult >= 0)
            {
                TestLibrary.TestFramework.LogError("048", " Two not null strings CompareOrdinal six Expected Result is less 0,Actual Result is ( " + ActualResult + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("049", "Unexpected exception: " + e);
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

        TestLibrary.TestFramework.BeginScenario("PosTest18: Two not null strings CompareOrdinal seven");

        try
        {
            strA = "\x200b";
            strB = "\uFEFF";
            ActualResult = string.CompareOrdinal(strA, strB);
            if (ActualResult >= 0)
            {
                TestLibrary.TestFramework.LogError("050", " Two not null strings CompareOrdinal seven Expected Result is less 0,Actual Result is ( " + ActualResult + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("051", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest19()
    {
        bool retVal = true;
        string strA;
        string strB;
        int ActualResult;

        TestLibrary.TestFramework.BeginScenario("PosTest19: Two not null strings CompareOrdinal five");

        try
        {
            strA = "\uFF21";
            strB = "A";
            ActualResult = string.CompareOrdinal(strA, strB);
            if (ActualResult <= 0)
            {
                TestLibrary.TestFramework.LogError("052", " Two not null strings CompareOrdinal five Expected Result is greater 0,Actual Result is ( " + ActualResult + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("053", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest20()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest0:Two like not NullStrings CompareOrdinal thirty");
        Random rand = new Random(-55);
        try
        {
            strA = "ABCDE";
            strB = "abcde";
            indexA = strA.Length;
            indexB = strB.Length;
            length = rand.Next();

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
            if (ActualResult != 0)
            {
                TestLibrary.TestFramework.LogError("N001", "ExpectResult is 0,ActualResult is (" + ActualResult + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N002", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;

    }


    #endregion


    #region Negative Testing
    /// <summary>
    /// indexA>strA.Length,indexB normal and length normal
    /// </summary>
    /// <returns></returns>
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1:Two like not NullStrings CompareOrdinal two");
        Random rand = new Random(-55);
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRINF_LEN, c_MAX_STRING_LEN);
            strB = strA.ToString();
            indexA = rand.Next(strA.Length + 1, strA.Length + 10);
            indexB = rand.Next(0, strB.Length);
            length = rand.Next();

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
	    TestLibrary.TestFramework.LogError("N00A", "Expected exception did not occur. Expected ArgumentOutOfRangeExeption");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException e)
        {
            TestLibrary.TestFramework.LogInformation("It occurred an expect exception:" + e);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N003", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    /// <summary>
    /// indexA<0,indexB normal and length normal
    /// </summary>
    /// <returns></returns>
    public bool NegTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest2:Two like not NullStrings CompareOrdinal three");
        Random rand = new Random(-55);
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRINF_LEN, c_MAX_STRING_LEN);
            strB = strA.ToString();
            indexA = (1 + rand.Next()) * (-1);
            indexB = rand.Next(0, strB.Length);
            length = rand.Next();

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
	    TestLibrary.TestFramework.LogError("N00B", "Expected exception did not occur. Expected ArgumentOutOfRangeExeption");
             retVal = false;
        }
        catch (ArgumentOutOfRangeException e)
        {
            TestLibrary.TestFramework.LogInformation("It occurred an expect exception:" + e);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N005", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    /// <summary>
    /// indexA normal, indexB>strB.length and length normal
    /// </summary>
    /// <returns></returns>
    public bool NegTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest3:Two like not NullStrings CompareOrdinal four");
        Random rand = new Random(-55);
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRINF_LEN, c_MAX_STRING_LEN);
            strB = strA.ToString();
            indexA = rand.Next(0, strA.Length);
            indexB = rand.Next(strB.Length + 1, strB.Length + 10);
            length = rand.Next();

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
	    TestLibrary.TestFramework.LogError("N00C", "Expected exception did not occur. Expected ArgumentOutOfRangeExeption");
             retVal = false;
        }
        catch (ArgumentOutOfRangeException e)
        {
            TestLibrary.TestFramework.LogInformation("It occurred an expect exception:" + e);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N007", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    /// <summary>
    /// indexA normal,indexB<0 and length normal
    /// </summary>
    /// <returns></returns>
    public bool NegTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest4:Two like not NullStrings CompareOrdinal five");
        Random rand = new Random(-55);
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRINF_LEN, c_MAX_STRING_LEN);
            strB = strA.ToString();
            indexA = rand.Next(0, strA.Length);
            indexB = rand.Next(1, strB.Length) * (-1);
            length = rand.Next();

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
	    TestLibrary.TestFramework.LogError("N00D", "Expected exception did not occur. Expected ArgumentOutOfRangeExeption");
             retVal = false;
        }
        catch (ArgumentOutOfRangeException e)
        {
            TestLibrary.TestFramework.LogInformation("It occurred an expect exception:" + e);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N009", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    /// <summary>
    /// indexA normal, indexB normal and length<0
    /// </summary>
    /// <returns></returns>
    public bool NegTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest5:Two like not NullStrings CompareOrdinal six");
        Random rand = new Random(-55);
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRINF_LEN, c_MAX_STRING_LEN);
            strB = strA.ToString();
            indexA = rand.Next(0, strA.Length);
            indexB = rand.Next(0, strB.Length);
            length = (1 + rand.Next()) * (-1);

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
	    TestLibrary.TestFramework.LogError("N00E", "Expected exception did not occur. Expected ArgumentOutOfRangeExeption");
             retVal = false;
        }
        catch (ArgumentOutOfRangeException e)
        {
            TestLibrary.TestFramework.LogInformation("It occurred an expect exception:" + e);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N011", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    /// <summary>
    /// indexA>strA.Length,indexB<0,length normal
    /// </summary>
    /// <returns></returns>
    public bool NegTest6()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest6:Two like not NullStrings CompareOrdinal seven");
        Random rand = new Random(-55);
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRINF_LEN, c_MAX_STRING_LEN);
            strB = strA.ToString();
            indexA = rand.Next(strA.Length + 1, strA.Length + 10);
            indexB = rand.Next(1, strB.Length) * (-1);
            length = rand.Next();

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
	    TestLibrary.TestFramework.LogError("N00F", "Expected exception did not occur. Expected ArgumentOutOfRangeExeption");
             retVal = false;
        }
        catch (ArgumentOutOfRangeException e)
        {
            TestLibrary.TestFramework.LogInformation("It occurred an expect exception:" + e);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N013", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    /// <summary>
    /// IndexA>strA.Length,indexB normal and length<0
    /// </summary>
    /// <returns></returns>
    public bool NegTest7()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest7:Two like not NullStrings CompareOrdinal eight");
        Random rand = new Random(-55);
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRINF_LEN, c_MAX_STRING_LEN);
            strB = strA.ToString();
            indexA = rand.Next(strA.Length + 1, strA.Length + 10);
            indexB = rand.Next(0, strB.Length);
            length = (1 + rand.Next()) * (-1);

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
	    TestLibrary.TestFramework.LogError("N00G", "Expected exception did not occur. Expected ArgumentOutOfRangeExeption");
             retVal = false;
        }
        catch (ArgumentOutOfRangeException e)
        {
            TestLibrary.TestFramework.LogInformation("It occurred an expect exception:" + e);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N015", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    /// <summary>
    /// indexA<0,indexB normal and length<0
    /// </summary>
    /// <returns></returns>
    public bool NegTest8()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest8:Two like not NullStrings CompareOrdinal nine");
        Random rand = new Random(-55);
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRINF_LEN, c_MAX_STRING_LEN);
            strB = strA.ToString();
            indexA = rand.Next(1, strA.Length) * (-1);
            indexB = rand.Next(0, strB.Length);
            length = (1 + rand.Next()) * (-1);

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
	    TestLibrary.TestFramework.LogError("N00H", "Expected exception did not occur. Expected ArgumentOutOfRangeExeption");
             retVal = false;
        }
        catch (ArgumentOutOfRangeException e)
        {
            TestLibrary.TestFramework.LogInformation("It occurred an expect exception:" + e);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N017", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    /// <summary>
    /// indexA<0,indexB<0 and length normal
    /// </summary>
    /// <returns></returns>
    public bool NegTest9()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest9:Two like not NullStrings CompareOrdinal nine");
        Random rand = new Random(-55);
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRINF_LEN, c_MAX_STRING_LEN);
            strB = strA.ToString();
            indexA = rand.Next(1, strA.Length) * (-1);
            indexB = rand.Next(1, strB.Length) * (-1);
            length = rand.Next();

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
	    TestLibrary.TestFramework.LogError("N00I", "Expected exception did not occur. Expected ArgumentOutOfRangeExeption");
             retVal = false;
        }
        catch (ArgumentOutOfRangeException e)
        {
            TestLibrary.TestFramework.LogInformation("It occurred an expect exception:" + e);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N019", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    /// <summary>
    /// indexA normal,indexB<0 and length<0
    /// </summary>
    /// <returns></returns>
    public bool NegTest10()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest10:Two like not NullStrings CompareOrdinal ten");
        Random rand = new Random(-55);
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRINF_LEN, c_MAX_STRING_LEN);
            strB = strA.ToString();
            indexA = rand.Next(0, strA.Length);
            indexB = rand.Next(1, strB.Length) * (-1);
            length = (1 + rand.Next()) * (-1);

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
	    TestLibrary.TestFramework.LogError("N00J", "Expected exception did not occur. Expected ArgumentOutOfRangeExeption");
             retVal = false;
        }
        catch (ArgumentOutOfRangeException e)
        {
            TestLibrary.TestFramework.LogInformation("It occurred an expect exception:" + e);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N021", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    /// <summary>
    /// indexA normal,indexB>strB.Length and length<0
    /// </summary>
    /// <returns></returns>
    public bool NegTest11()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest11:Two like not NullStrings CompareOrdinal eleven");
        Random rand = new Random(-55);
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRINF_LEN, c_MAX_STRING_LEN);
            strB = strA.ToString();
            indexA = rand.Next(0, strA.Length);
            indexB = rand.Next(strB.Length + 1, strB.Length + 10);
            length = (1 + rand.Next()) * (-1);

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
	    TestLibrary.TestFramework.LogError("N00K", "Expected exception did not occur. Expected ArgumentOutOfRangeExeption");
             retVal = false;
        }
        catch (ArgumentOutOfRangeException e)
        {
            TestLibrary.TestFramework.LogInformation("It occurred an expect exception:" + e);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N023", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    /// <summary>
    /// indexA<0,indexB<0 and length <0
    /// </summary>
    /// <returns></returns>
    public bool NegTest12()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest12:Two like not NullStrings CompareOrdinal twelve");
        Random rand = new Random(-55);
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRINF_LEN, c_MAX_STRING_LEN);
            strB = strA.ToString();
            indexA = rand.Next(1, strA.Length) * (-1);
            indexB = rand.Next(1, strB.Length) * (-1);
            length = rand.Next() * (-1);

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
	    TestLibrary.TestFramework.LogError("N00L", "Expected exception did not occur. Expected ArgumentOutOfRangeExeption");
             retVal = false;
        }
        catch (ArgumentOutOfRangeException e)
        {
            TestLibrary.TestFramework.LogInformation("It occurred an expect exception:" + e);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N025", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    /// <summary>
    /// indexA>strA.Length,indexB>strB.Length and length <0
    /// </summary>
    /// <returns></returns>
    public bool NegTest13()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest13:Two like not NullStrings CompareOrdinal thirteen");
        Random rand = new Random(-55);
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRINF_LEN, c_MAX_STRING_LEN);
            strB = strA.ToString();
            indexA = rand.Next(strA.Length + 1, strA.Length + 10);
            indexB = rand.Next(strB.Length + 1, strB.Length + 10);
            length = (1 + rand.Next()) * (-1);
            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
	    TestLibrary.TestFramework.LogError("N00M", "Expected exception did not occur. Expected ArgumentOutOfRangeExeption");
             retVal = false;
        }
        catch (ArgumentOutOfRangeException e)
        {
            TestLibrary.TestFramework.LogInformation("It occurred an expect exception:" + e);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N027", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    /// <summary>
    /// indexA>strA.Length indexB>strB.Length and length normal
    /// </summary>
    /// <returns></returns>
    public bool NegTest14()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest14:Two like not NullStrings CompareOrdinal fourteen");
        Random rand = new Random(-55);
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRINF_LEN, c_MAX_STRING_LEN);
            strB = strA.ToString();
            indexA = rand.Next(strA.Length + 1, strA.Length + 10);
            indexB = rand.Next(strB.Length + 1, strB.Length + 10);
            length = rand.Next();

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
	    TestLibrary.TestFramework.LogError("N00N", "Expected exception did not occur. Expected ArgumentOutOfRangeExeption");
             retVal = false;
        }
        catch (ArgumentOutOfRangeException e)
        {
            TestLibrary.TestFramework.LogInformation("It occurred an expect exception:" + e);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N029", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    /// <summary>
    /// indexA<0,indexB<strB.Length and length normal
    /// </summary>
    /// <returns></returns>
    public bool NegTest15()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest15:Two like not NullStrings CompareOrdinal fifteen");
        Random rand = new Random(-55);
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRINF_LEN, c_MAX_STRING_LEN);
            strB = strA.ToString();
            indexA = rand.Next(1, strA.Length) * (-1);
            indexB = rand.Next(strB.Length + 1, strB.Length + 10);
            length = rand.Next();

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
	    TestLibrary.TestFramework.LogError("N00O", "Expected exception did not occur. Expected ArgumentOutOfRangeExeption");
             retVal = false;
        }
        catch (ArgumentOutOfRangeException e)
        {
            TestLibrary.TestFramework.LogInformation("It occurred an expect exception:" + e);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N031", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    /// <summary>
    /// indexA <0,indexB>strB.Length and length<0
    /// </summary>
    /// <returns></returns>
    public bool NegTest16()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest16:Two like not NullStrings CompareOrdinal sixteen");
        Random rand = new Random(-55);
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRINF_LEN, c_MAX_STRING_LEN);
            strB = strA.ToString();
            indexA = rand.Next(1, strA.Length) * (-1);
            indexB = rand.Next(strB.Length + 1, strB.Length + 10);
            length = (1 + rand.Next()) * (-1);

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
	    TestLibrary.TestFramework.LogError("N00P", "Expected exception did not occur. Expected ArgumentOutOfRangeExeption");
             retVal = false;
        }
        catch (ArgumentOutOfRangeException e)
        {
            TestLibrary.TestFramework.LogInformation("It occurred an expect exception:" + e);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N033", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    /// <summary>
    /// indexA>strA.Length,indexB<0 and length<0
    /// </summary>
    /// <returns></returns>
    public bool NegTest17()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest17:Two like not NullStrings CompareOrdinal seventeen");
        Random rand = new Random(-55);
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRINF_LEN, c_MAX_STRING_LEN);
            strB = strA.ToString();
            indexA = rand.Next(strA.Length + 1, strA.Length + 10);
            indexB = rand.Next(1, strB.Length) * (-1);
            length = (1 + rand.Next()) * (-1);

            int ActualResult = string.CompareOrdinal(strA, indexA, strB, indexB, length);
	    TestLibrary.TestFramework.LogError("N00Q", "Expected exception did not occur. Expected ArgumentOutOfRangeExeption");
             retVal = false;
        }
        catch (ArgumentOutOfRangeException e)
        {
            TestLibrary.TestFramework.LogInformation("It occurred an expect exception:" + e);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N035", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion

}

