// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class StringCtor5
{
    #region const define
    private const int c_MIN_STRING_LEN = 1;
    private const int c_MAX_STRING_LEN = 256;
    #endregion

    public static int Main(string[] args)
    {
        StringCtor5 sc = new StringCtor5();
        TestLibrary.TestFramework.BeginTestCase("StringCtor_charArray_Int32_Int32");

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

        TestLibrary.TestFramework.LogInformation("");

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;

        return retVal;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public bool PosTest1()
    {
        bool retVal = true;
        string str = TestLibrary.Generator.GetString(-55, true, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        TestLibrary.TestFramework.BeginScenario("Generate a string with valid index and length!");
        char[] charArray = new char[str.Length];
        str.CopyTo(0, charArray, 0, charArray.Length);

        try
        {
            //char[] charsArray = null; 
            string strGen = new string(charArray, 0, charArray.Length);
            if (strGen != str)
            {
                TestLibrary.TestFramework.LogError("001", "The new generated string is not uqual to the original one!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected Error :" + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// Generate a string with max range index and zero length
    /// </summary>
    /// <returns></returns>
    public bool PosTest2()
    {
        bool retVal = true;
        string str = TestLibrary.Generator.GetString(-55, true, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        TestLibrary.TestFramework.BeginScenario("Generate a string with max index and zero length...");
        char[] charArray = new char[str.Length];
        str.CopyTo(0, charArray, 0, charArray.Length);

        try
        {
            string strGen = new string(charArray, charArray.Length, 0);
            if (strGen != str.Substring(charArray.Length, 0))
            {
                TestLibrary.TestFramework.LogError("003", "sdfsadfsa");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected Exception occurs : " + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// Generate string with max-1 index and valid length...
    /// </summary>
    /// <returns></returns>
    public bool PosTest3()
    {
        bool retVal = true;
        string str = TestLibrary.Generator.GetString(-55, true, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        TestLibrary.TestFramework.BeginScenario("Generate string with max-1 index and valid length...");
        char[] charArray = new char[str.Length];
        str.CopyTo(0, charArray, 0, charArray.Length);

        try
        {
            string strGen = new string(charArray, charArray.Length - 1, 1);
            if (strGen != str.Substring(charArray.Length - 1, 1))
            {
                TestLibrary.TestFramework.LogError("005", "The new generated sring is not the expected one!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected Expection occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// Negtest1: Generate a string with a null charArray
    /// </summary>
    /// <returns></returns>
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("Negtest1: Generate a string with a null charArray");

        try
        {
            char[] charArray = null;
            string str = new string(charArray, 0, 0);

            TestLibrary.TestFramework.LogError("007", "The new string can be generated by using a charArray with null value!!");
            retVal = false;

        }
        catch (ArgumentNullException)
        {
            TestLibrary.TestFramework.LogInformation("ArgumentNullException is thrown when pass charArray with null value!");
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected Error :" + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// NegTest2: Generate string with max+1 index...
    /// </summary>
    /// <returns></returns>
    public bool NegTest2()
    {
        bool retVal = true;
        string str = TestLibrary.Generator.GetString(-55, true, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        TestLibrary.TestFramework.BeginScenario("NegTest2: Generate string with max+1 index...");
        char[] charArray = new char[str.Length];
        str.CopyTo(0, charArray, 0, charArray.Length);

        try
        {
            string strGen = new string(charArray, charArray.Length + 1, 0);

            TestLibrary.TestFramework.LogError("009", "The string is generated with no exception occures!");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
            TestLibrary.TestFramework.LogInformation("ArgumentOutOfRangeException is thrown when the index param is max+1!");
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("00X", "Unexpected Exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// NegTest3: Generate string with max+1 length...
    /// </summary>
    /// <returns></returns>
    public bool NegTest3()
    {
        bool retVal = true;
        string str = TestLibrary.Generator.GetString(-55, true, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        TestLibrary.TestFramework.BeginScenario("NegTest3: Generate string with max+1 length...");
        char[] charArray = new char[str.Length];
        str.CopyTo(0, charArray, 0, charArray.Length);

        try
        {
            string strGen = new string(charArray, 0, charArray.Length + 1);

            TestLibrary.TestFramework.LogError("010", "The string is generated with no exception occurs!");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
            TestLibrary.TestFramework.LogInformation("ArgumentOutOfRangeException is thrown when the length param is max+1");
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("011", "Unexpected Exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// NegTest4: Generate string when index with minus value...
    /// </summary>
    /// <returns></returns>
    public bool NegTest4()
    {
        bool retVal = true;
        string str = TestLibrary.Generator.GetString(-55, true, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        TestLibrary.TestFramework.BeginScenario("NegTest4: Generate string when index with minus value...");
        char[] charArray = new char[str.Length];
        str.CopyTo(0, charArray, 0, charArray.Length);

        try
        {
            string strGen = new string(charArray, -1, charArray.Length);

            TestLibrary.TestFramework.LogError("012", "The string is generated with no exception occurs!");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
            TestLibrary.TestFramework.LogInformation("ArgumentOutOfRangeException is thrown when the index param is minus");
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("013", "Unexpected Exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// NegTest4: Generate string when index with minus value...
    /// </summary>
    /// <returns></returns>
    public bool NegTest5()
    {
        bool retVal = true;
        string str = TestLibrary.Generator.GetString(-55, true, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        TestLibrary.TestFramework.BeginScenario("NegTest4: Generate string when index with minus value...");
        char[] charArray = new char[str.Length];
        str.CopyTo(0, charArray, 0, charArray.Length);

        try
        {
            string strGen = new string(charArray, 0, -1);

            TestLibrary.TestFramework.LogError("013", "The string is generated with no exception occurs!");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
            TestLibrary.TestFramework.LogInformation("ArgumentOutOfRangeException is thrown when index is minus!");
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected Exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }
}

