// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

///<summary>
///System.Test.UnicodeEncoding.GetByteCount(String) [v-zuolan]
///</summary>

public class UnicodeEncodingGetByteCount
{

    public static int Main()
    {
        UnicodeEncodingGetByteCount testObj = new UnicodeEncodingGetByteCount();
        TestLibrary.TestFramework.BeginTestCase("for field of System.Test.UnicodeEncoding");
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
        TestLibrary.TestFramework.LogInformation("Positive");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        TestLibrary.TestFramework.LogInformation("Positive");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Helper Method
    //Create a None-Surrogate-Char String.
    public String GetString(int length)
    {
        if (length <= 0) return "";

        String tempStr = null;

        int i = 0;
        while (i < length)
        {
            Char temp = TestLibrary.Generator.GetChar(-55);
            if (!Char.IsSurrogate(temp))
            {
                tempStr = tempStr + temp.ToString();
                i++;
            }
        }
        return tempStr;
    }

    public String ToString(String myString)
    {
        String str = "{";
        Char[] chars = myString.ToCharArray();
        for (int i = 0;i < chars.Length; i++)
        {
            str = str + @"\u" + String.Format("{0:X04}", (int)chars[i]);
            if (i != chars.Length - 1) str = str + ",";
        }
        str = str + "}";
        return str;
    }
    #endregion

    #region Positive Test Logic
    public bool PosTest1()
    {
        bool retVal = true;

        UnicodeEncoding uEncoding = new UnicodeEncoding();

        int expectedValue = 0;
        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Invoke the method with a empty String.");
        try
        {
            actualValue = uEncoding.GetByteCount("");

            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("001", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
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

        String str = GetString(10);
        UnicodeEncoding uEncoding = new UnicodeEncoding();

        int expectedValue = 20;
        int actualValue;


        TestLibrary.TestFramework.BeginScenario("PosTest2:Invoke the method with normal string");
        try
        {
            String temp = ToString(str);
            actualValue = uEncoding.GetByteCount(str);

            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("003", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ") when chars is:" + ToString(str));
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception:" + e + "when chars is:" + ToString(str));
            retVal = false;
        }
        return retVal;
    }


    public bool PosTest3()
    {
        bool retVal = true;

        String str = GetString(1);
        UnicodeEncoding uEncoding = new UnicodeEncoding();

        int expectedValue = 2;
        int actualValue;


        TestLibrary.TestFramework.BeginScenario("PosTest3:Invoke the method with one char String.");
        try
        {
            actualValue = uEncoding.GetByteCount(str);

            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("005", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ") when chars is:" + ToString(str));
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception:" + e + "when chars is:" + ToString(str));
            retVal = false;
        }
        return retVal;
    }

    #endregion

    #region Negative Test Logic
    public bool NegTest1()
    {
        bool retVal = true;

        String str = null;

        UnicodeEncoding uEncoding = new UnicodeEncoding();

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("NegTest1:Invoke the method with null");
        try
        {
            actualValue = uEncoding.GetByteCount(str);

            TestLibrary.TestFramework.LogError("007", "No ArgumentNullException throw out expected.");
            retVal = false;
        }
        catch (ArgumentNullException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    #endregion
}
