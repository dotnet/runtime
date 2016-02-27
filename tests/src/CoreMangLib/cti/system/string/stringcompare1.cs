// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using TestLibrary;

class StringCompare1
{
    const string str1 = "HELLOWORLD";
    const string str2 = "helloworld";

    /// <summary>
    /// Test String.Compare(System.String,System.Int32,System.String,System.Int32,System.Int32)
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public static int Main(string[] args)
    {
        StringCompare1 sc = new StringCompare1();
        TestLibrary.TestFramework.BeginScenario("Test String.Compare(System.String,System.Int32,System.String,System.Int32,System.Int32)");

        if (sc.RunTests())
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

        TestLibrary.TestFramework.LogInformation("");

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
        retVal = NegTest18() && retVal;

        return retVal;
    }

    /// <summary>
    /// Compare two same strings with different cases...
    /// </summary>
    /// <returns></returns>
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("Compare two same strings with different cases...");

        try
        {
            int expected = GlobLocHelper.OSCompare(str1, 0, str2, 0, str1.Length);  // 1;
            if (String.Compare(str1, 0, str2, 0, str1.Length) != expected)
            {
                TestLibrary.TestFramework.LogError("001", "The result is equal when compare different cases string!");
                TestLibrary.TestFramework.LogInformation("Expected: " + expected.ToString() + " Actual: " + String.Compare(str1, 0, str2, 0, str1.Length).ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected Exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// Compare two null string with indexed and length are zero...
    /// </summary>
    /// <returns></returns>
    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("Compare two null string with indexed and length are zero...");

        try
        {
            if (String.Compare(null, 0, null, 0, 0) != 0)
            {
                TestLibrary.TestFramework.LogError("003", "The result is not equal when compare two string with null value!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected Exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// Compare two strings which the first of them is null...
    /// </summary>
    /// <returns></returns>
    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("Compare two strings which the first of them is null...");

        try
        {
            if (String.Compare(null, 0, str1, 0, 0) != -1)
            {
                TestLibrary.TestFramework.LogError("005", "String with null value should be smaller than string with content!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected Exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// Compare two strings which the second of them is null...
    /// </summary>
    /// <returns></returns>
    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("Compare two strings which the second of them is null...");

        try
        {
            if (String.Compare(str1, 0, null, 0, 0) != 1)
            {
                TestLibrary.TestFramework.LogError("007", "String with content should be larger than string with null value!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected Exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// Compare two strings with the first index out of range...
    /// </summary>
    /// <returns></returns>
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("Compare two strings with the first index out of range...");

        try
        {
            String.Compare(str1, str1.Length + 1, str2, 0, 0);

            TestLibrary.TestFramework.LogError("009", "No exception is thrown!");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
            TestLibrary.TestFramework.LogInformation("ArgumentOutOfRangeException is thrown when the first index is out of range!");
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010","Unexpected Exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// Copmare two strings with the second index out of range...
    /// </summary>
    /// <returns></returns>
    public bool NegTest2() 
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("Copmare two strings with the second index out of range...");

        try
        {
            String.Compare(str1,0,str2,str2.Length+1,0);

            TestLibrary.TestFramework.LogError("011", "No exception is thrown!");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
            TestLibrary.TestFramework.LogInformation("ArgumentOutOfRangeException is thrown when the first index is out of range!");
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected Exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// Compare the strings with the first index is negtive...
    /// </summary>
    /// <returns></returns>
    public bool NegTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("Compare the strings with the first index is negtive...");
        try
        {
            String.Compare(str1, -1, str2, 0, str2.Length);

            TestLibrary.TestFramework.LogError("013", "No exception occurs!");
            retVal = false;

        }
        catch (ArgumentOutOfRangeException)
        {
            TestLibrary.TestFramework.LogInformation("ArgumentOutOfRangeException is thrown when a given indexes are negative!");
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// Compare the strings with the second index is negtive...
    /// </summary>
    /// <returns></returns>
    public bool NegTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("Compare the strings with the second index is negtive...");
        try
        {
            String.Compare(str1, -1, str2, 0, str2.Length);

            TestLibrary.TestFramework.LogError("015", "No exception occurs!");
            retVal = false;

        }
        catch (ArgumentOutOfRangeException)
        {
            TestLibrary.TestFramework.LogInformation("ArgumentOutOfRangeException is thrown when a given indexes are negative!");
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// Compare the strings with length is negative...
    /// </summary>
    /// <returns></returns>
    public bool NegTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("Compare the strings with length is negative...");
        try
        {
            String.Compare(str1, 0, str2, 0, -1);

            TestLibrary.TestFramework.LogError("017", "No exception occurs!");
            retVal = false;

        }
        catch (ArgumentOutOfRangeException)
        {
            TestLibrary.TestFramework.LogInformation("ArgumentOutOfRangeException is thrown when a given length is negative!");
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// Compare two strings with max+1 length... 
    /// </summary>
    /// <returns></returns>
    public bool NegTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("Compare two strings with max+1 length... ");
        try
        {
            if (String.Compare(str1, 0, str1, 0, str1.Length + 1) != 0)
            {
                TestLibrary.TestFramework.LogError("019", "Can't compare strings ignoring length exceed max!");
                retVal = false;
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            TestLibrary.TestFramework.LogInformation("ArgumentOutOfRangeException is thrown when length is max+1");
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("020","Unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// Compare two null strings with length out of range...
    /// </summary>
    /// <returns></returns>
    public bool NegTest7()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("Compare two null strings with length out of range...");
        try
        {
            String.Compare(null, 0, null, 0, 1);

            TestLibrary.TestFramework.LogError("021", "Compare failed!");
            retVal = false;

        }
        catch (ArgumentOutOfRangeException)
        {
            TestLibrary.TestFramework.LogInformation("ArgumentOutOfRangeException is thrown when a given length is out of range!");
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("022", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// Compare two null strings with first index out of range...
    /// </summary>
    /// <returns></returns>
    public bool NegTest8()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("Compare two null strings with first index out of range...");
        try
        {
            String.Compare(null, 1, null, 0, 0);

            TestLibrary.TestFramework.LogError("023", "Compare failed!");
            retVal = false;

        }
        catch (ArgumentOutOfRangeException)
        {
            TestLibrary.TestFramework.LogInformation("ArgumentOutOfRangeException is thrown when a given index is out of range!");
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("024", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// Compare two null strings with first index out of range...
    /// </summary>
    /// <returns></returns>
    public bool NegTest9()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("Compare two null strings with first index out of range...");
        try
        {
            String.Compare(null, 0, null, 1, 0);

            TestLibrary.TestFramework.LogError("025", "Compare failed!");
            retVal = false;

        }
        catch (ArgumentOutOfRangeException)
        {
            TestLibrary.TestFramework.LogInformation("ArgumentOutOfRangeException is thrown when a given index is out of range!");
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("026", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// Compare two null strings with two indexes out of range...
    /// </summary>
    /// <returns></returns>
    public bool NegTest10()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("Compare two null strings with two indexes out of range...");
        try
        {
            String.Compare(null, 1, null, 1, 0);

            TestLibrary.TestFramework.LogError("027", "Compare failed!");
            retVal = false;

        }
        catch (ArgumentOutOfRangeException)
        {
            TestLibrary.TestFramework.LogInformation("ArgumentOutOfRangeException is thrown when given indexes is out of range!");
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("028", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// Compare two null strings with two indexes are negtive...
    /// </summary>
    /// <returns></returns>
    public bool NegTest11()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("Compare two null strings with two indexes are negtive...");
        try
        {
            String.Compare(null, -1, null, -1, 0);

            TestLibrary.TestFramework.LogError("029", "Compare failed!");
            retVal = false;

        }
        catch (ArgumentOutOfRangeException)
        {
            TestLibrary.TestFramework.LogInformation("ArgumentOutOfRangeException is thrown when given indexes is out of range!");
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("030", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// Compare two null strings with the first index is negtive...
    /// </summary>
    /// <returns></returns>
    public bool NegTest12()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("Compare two null strings with the first index is negtive...");
        try
        {
            String.Compare(null, -1, null, 0, 0);

            TestLibrary.TestFramework.LogError("031", "Compare failed!");
            retVal = false;

        }
        catch (ArgumentOutOfRangeException)
        {
            TestLibrary.TestFramework.LogInformation("ArgumentOutOfRangeException is thrown when given indexes is out of range!");
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("032", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// Compare two null strings with the second index is negtive...
    /// </summary>
    /// <returns></returns>
    public bool NegTest13()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("Compare two null strings with the second index is negtive...");
        try
        {
            String.Compare(null, 0, null, -1, 0);

            TestLibrary.TestFramework.LogError("033", "Compare failed!");
            retVal = false;

        }
        catch (ArgumentOutOfRangeException)
        {
            TestLibrary.TestFramework.LogInformation("ArgumentOutOfRangeException is thrown when given indexes is out of range!");
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("034", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// Compare two strings which the first is null and the length is not zero...
    /// </summary>
    /// <returns></returns>
    public bool NegTest14()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("Compare two strings which the first string is null and the length is not zero...");
        try
        {
            String.Compare(null, 0, str1, 0, 1);

            TestLibrary.TestFramework.LogError("035", "Compare failed!");
            retVal = false;

        }
        catch (ArgumentOutOfRangeException)
        {
            TestLibrary.TestFramework.LogInformation("ArgumentOutOfRangeException is thrown when the first is null and the length is not zero!");
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("036", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// Compare two strings which the second string is null and the length is not zero...
    /// </summary>
    /// <returns></returns>
    public bool NegTest15()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("Compare two strings which the second string is null and the length is not zero...");
        try
        {
            String.Compare(str1, 0, null, 0, 1);

            TestLibrary.TestFramework.LogError("037", "Compare failed!");
            retVal = false;

        }
        catch (ArgumentOutOfRangeException)
        {
            TestLibrary.TestFramework.LogInformation("ArgumentOutOfRangeException is thrown when the second string is null and the length is not zero!");
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("038", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// Compare two null strings with max+1 length...
    /// </summary>
    /// <returns></returns>
    public bool NegTest16()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("Compare two null strings with max+1 length...");
        try
        {
            String.Compare(null, 0, null, 0, -1);

            TestLibrary.TestFramework.LogError("039", "Compare failed!");
            retVal = false;

        }
        catch (ArgumentOutOfRangeException)
        {
            TestLibrary.TestFramework.LogInformation("ArgumentOutOfRangeException is thrown when a given length is out of range!");
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("040", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// Compare two strings which the first string is null and max+1 length...
    /// </summary>
    /// <returns></returns>
    public bool NegTest17()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("Compare two strings which the first string is null and the negtive length...");
        try
        {
            String.Compare(null, 0, str1, 0, -1);

            TestLibrary.TestFramework.LogError("041", "Compare failed!");
            retVal = false;

        }
        catch (ArgumentOutOfRangeException)
        {
            TestLibrary.TestFramework.LogInformation("ArgumentOutOfRangeException is thrown when which the first string is null and the negtive length!");
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("042", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// Compare two strings which the second string is null and with max+1 length...
    /// </summary>
    /// <returns></returns>
    public bool NegTest18()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("Compare two strings which the second string is null and with the negtive length...");
        try
        {
            String.Compare(str1, 0, null, 0, 1);

            TestLibrary.TestFramework.LogError("043", "Compare failed!");
            retVal = false;

        }
        catch (ArgumentOutOfRangeException)
        {
            TestLibrary.TestFramework.LogInformation("ArgumentOutOfRangeException is thrown which the second string is null and with the negtive length!");
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("044", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }


}

