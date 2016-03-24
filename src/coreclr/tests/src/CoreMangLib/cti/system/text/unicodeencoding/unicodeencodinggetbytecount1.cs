// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

///<summary>
///System.Test.UnicodeEncoding.GetByteCount(System.Char[],System.Int32,System.Int32) [v-zuolan]
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
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;

        return retVal;
    }

    #region Helper Method
    //Create a None-Surrogate-Char Array.
    public Char[] GetCharArray(int length)
    {
        if (length <= 0) return new Char[] { };

        Char[] charArray = new Char[length];
        int i = 0;
        while (i < length)
        {
            Char temp = TestLibrary.Generator.GetChar(-55);
            if (!Char.IsSurrogate(temp))
            {
                charArray[i] = temp;
                i++;
            }
        }
        return charArray;
    }

    //Convert Char Array to String
    public String ToString(Char[] chars)
    {
        String str = "{";
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

        Char[] chars = new Char[] { } ;
        UnicodeEncoding uEncoding = new UnicodeEncoding();

        int expectedValue = 0;
        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Invoke the method with a empty char array.");
        try
        {
            actualValue = uEncoding.GetByteCount(chars,0,0);

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

        Char[] chars = GetCharArray(10);
        UnicodeEncoding uEncoding = new UnicodeEncoding();
        
        int expectedValue = 20;
        int actualValue;


        TestLibrary.TestFramework.BeginScenario("PosTest2:Invoke the method with max length of the char array.");
        try
        {
            actualValue = uEncoding.GetByteCount(chars, 0, chars.Length);

            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("003", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ") when chars is:" + ToString(chars));
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception:" + e + "when chars is:" + ToString(chars));
            retVal = false;
        }
        return retVal;
    }


    public bool PosTest3()
    {
        bool retVal = true;

        Char[] chars = GetCharArray(1);
        UnicodeEncoding uEncoding = new UnicodeEncoding();

        int expectedValue = 2;
        int actualValue;


        TestLibrary.TestFramework.BeginScenario("PosTest3:Invoke the method with one char array.");
        try
        {
            actualValue = uEncoding.GetByteCount(chars, 0, 1);

            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("005", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ") when chars is:" + ToString(chars));
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception:" + e + "when chars is:" + ToString(chars));
            retVal = false;
        }
        return retVal;
    }

    #endregion

    #region Negative Test Logic
    public bool NegTest1()
    {
        bool retVal = true;

        //Char[] chars = new Char[]{};
        UnicodeEncoding uEncoding = new UnicodeEncoding();
        
        int actualValue;

        TestLibrary.TestFramework.BeginScenario("NegTest1:Invoke the method with null");
        try
        {
            actualValue = uEncoding.GetByteCount(null,0,0);

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


    public bool NegTest2()
    {
        bool retVal = true;

        Char[] chars = GetCharArray(10);
        UnicodeEncoding uEncoding = new UnicodeEncoding();

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("NegTest2:Invoke the method with index out of range.");
        try
        {
            actualValue = uEncoding.GetByteCount(chars, 10, 1);

            TestLibrary.TestFramework.LogError("009", "No ArgumentOutOfRangeException throw out expected when chars is:" + ToString(chars));
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception:" + e + " when chars is:" + ToString(chars));
            retVal = false;
        }
        return retVal;
    }


    public bool NegTest3()
    {
        bool retVal = true;

        Char[] chars = GetCharArray(10);
        UnicodeEncoding uEncoding = new UnicodeEncoding();

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("NegTest3:Invoke the method with count out of range.");
        try
        {
            actualValue = uEncoding.GetByteCount(chars, 5, -1);

            TestLibrary.TestFramework.LogError("011", "No ArgumentOutOfRangeException throw out expected when chars is:" + ToString(chars));
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception:" + e + " when chars is:" + ToString(chars));
            retVal = false;
        }
        return retVal;
    } 
    #endregion
}
