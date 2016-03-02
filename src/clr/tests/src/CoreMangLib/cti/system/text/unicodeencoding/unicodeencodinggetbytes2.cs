// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

///<summary>
///System.Test.UnicodeEncoding.GetBytes(System.String,System.Int32,System.Int32,System.Byte[],System.Int32) [v-zuolan]
///</summary>

public class UnicodeEncodingGetBytes
{

    public static int Main()
    {
        UnicodeEncodingGetBytes testObj = new UnicodeEncodingGetBytes();
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
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;
        retVal = NegTest6() && retVal;
        retVal = NegTest7() && retVal;
        retVal = NegTest8() && retVal;
        retVal = NegTest9() && retVal;
        retVal = NegTest10() && retVal;

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
        for (int i = 0; i < chars.Length; i++)
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

        String chars = GetString(10);
        Byte[] bytes = new Byte[30];


        UnicodeEncoding uEncoding = new UnicodeEncoding();

        int expectedValue = 20;
        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Invoke the method");
        try
        {
            actualValue = uEncoding.GetBytes(chars, 0, 10, bytes, 5);

            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("001", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")" + " when chars is :" + ToString(chars));
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception:" + e + " when chars is :" + ToString(chars));
            retVal = false;
        }
        return retVal;
    }


    public bool PosTest2()
    {
        bool retVal = true;

        String chars = GetString(10);
        Byte[] bytes = new Byte[30];


        UnicodeEncoding uEncoding = new UnicodeEncoding();

        int expectedValue = 2;
        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest2:Invoke the method and set charCount as 1 and byteIndex as 0.");
        try
        {
            actualValue = uEncoding.GetBytes(chars, 0, 1, bytes, 0);

            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("003", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")" + " when chars is :" + ToString(chars));
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception:" + e + " when chars is :" + ToString(chars));
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        String chars = GetString(10);
        Byte[] bytes = new Byte[30];

        UnicodeEncoding uEncoding = new UnicodeEncoding();

        int expectedValue = 0;
        int actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest3:Invoke the method and set charIndex as 20");
        try
        {
            actualValue = uEncoding.GetBytes(chars, 0, 0, bytes, 30);

            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("025", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")" + " when chars is :" + ToString(chars));
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("026", "Unexpected exception:" + e + " when chars is :" + ToString(chars));
            retVal = false;
        }
        return retVal;
    }    
    #endregion

    #region Negative Test Logic
    public bool NegTest1()
    {
        bool retVal = true;

        String chars = null;
        Byte[] bytes = new Byte[30];

        UnicodeEncoding uEncoding = new UnicodeEncoding();

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("NegTest1:Invoke the method and set chars as null");
        try
        {
            actualValue = uEncoding.GetBytes(chars, 0, 0, bytes, 0);

            TestLibrary.TestFramework.LogError("005", "No ArgumentNullException throw out expected.");
            retVal = false;
        }
        catch (ArgumentNullException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }


    public bool NegTest2()
    {
        bool retVal = true;

        String chars = GetString(10);
        Byte[] bytes = null;

        UnicodeEncoding uEncoding = new UnicodeEncoding();

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("NegTest2:Invoke the method and set bytes as null");
        try
        {
            actualValue = uEncoding.GetBytes(chars, 0, 0, bytes, 0);

            TestLibrary.TestFramework.LogError("007", "No ArgumentNullException throw out expected" + " when chars is :" + ToString(chars));
            retVal = false;
        }
        catch (ArgumentNullException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception:" + e + " when chars is :" + ToString(chars));
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        String chars = GetString(10);
        Byte[] bytes = new Byte[30];

        UnicodeEncoding uEncoding = new UnicodeEncoding();

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("NegTest3:Invoke the method and the destination buffer is not enough");
        try
        {
            actualValue = uEncoding.GetBytes(chars, 0, 10, bytes, 15);

            TestLibrary.TestFramework.LogError("009", "No ArgumentException throw out expected" + " when chars is :" + ToString(chars));
            retVal = false;
        }
        catch (ArgumentException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception:" + e + " when chars is :" + ToString(chars));
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;

        String chars = GetString(10);
        Byte[] bytes = new Byte[10];

        UnicodeEncoding uEncoding = new UnicodeEncoding();

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("NegTest4:Invoke the method and the destination buffer is not enough");
        try
        {
            actualValue = uEncoding.GetBytes(chars, 0, 10, bytes, 0);

            TestLibrary.TestFramework.LogError("011", "No ArgumentException throw out expected" + " when chars is :" + ToString(chars));
            retVal = false;
        }
        catch (ArgumentException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception:" + e + " when chars is :" + ToString(chars));
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest5()
    {
        bool retVal = true;

        String chars = GetString(10);
        Byte[] bytes = new Byte[30];

        UnicodeEncoding uEncoding = new UnicodeEncoding();

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("NegTest5:Invoke the method and set charIndex as -1");
        try
        {
            actualValue = uEncoding.GetBytes(chars, -1, 1, bytes, 0);

            TestLibrary.TestFramework.LogError("013", "No ArgumentOutOfRangeException throw out expected" + " when chars is :" + ToString(chars));
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected exception:" + e + " when chars is :" + ToString(chars));
            retVal = false;
        }
        return retVal;
    }


    public bool NegTest6()
    {
        bool retVal = true;

        String chars = GetString(10);
        Byte[] bytes = new Byte[30];

        UnicodeEncoding uEncoding = new UnicodeEncoding();

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("NegTest6:Invoke the method and set charIndex as 15");
        try
        {
            actualValue = uEncoding.GetBytes(chars, 15, 1, bytes, 0);

            TestLibrary.TestFramework.LogError("015", "No ArgumentOutOfRangeException throw out expected" + " when chars is :" + ToString(chars));
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016", "Unexpected exception:" + e + " when chars is :" + ToString(chars));
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest7()
    {
        bool retVal = true;

        String chars = GetString(10);
        Byte[] bytes = new Byte[30];

        UnicodeEncoding uEncoding = new UnicodeEncoding();

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("NegTest7:Invoke the method and set charCount as -1");
        try
        {
            actualValue = uEncoding.GetBytes(chars, 0, -1, bytes, 0);

            TestLibrary.TestFramework.LogError("017", "No ArgumentOutOfRangeException throw out expected" + " when chars is :" + ToString(chars));
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018", "Unexpected exception:" + e + " when chars is :" + ToString(chars));
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest8()
    {
        bool retVal = true;

        String chars = GetString(10);
        Byte[] bytes = new Byte[30];

        UnicodeEncoding uEncoding = new UnicodeEncoding();

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("NegTest8:Invoke the method and set charCount as 12");
        try
        {
            actualValue = uEncoding.GetBytes(chars, 0, 12, bytes, 0);

            TestLibrary.TestFramework.LogError("019", "No ArgumentOutOfRangeException throw out expected" + " when chars is :" + ToString(chars));
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("020", "Unexpected exception:" + e + " when chars is :" + ToString(chars));
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest9()
    {
        bool retVal = true;

        String chars = GetString(10);
        Byte[] bytes = new Byte[30];

        UnicodeEncoding uEncoding = new UnicodeEncoding();

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("NegTest9:Invoke the method and set byteIndex as -1");
        try
        {
            actualValue = uEncoding.GetBytes(chars, 0, 1, bytes, -1);

            TestLibrary.TestFramework.LogError("021", "No ArgumentOutOfRangeException throw out expected" + " when chars is :" + ToString(chars));
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("022", "Unexpected exception:" + e + " when chars is :" + ToString(chars));
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest10()
    {
        bool retVal = true;

        String chars = GetString(10);
        Byte[] bytes = new Byte[30];

        UnicodeEncoding uEncoding = new UnicodeEncoding();

        int actualValue;

        TestLibrary.TestFramework.BeginScenario("NegTest10:Invoke the method and set charIndex as 31");
        try
        {
            actualValue = uEncoding.GetBytes(chars, 0, 0, bytes, 31);

            TestLibrary.TestFramework.LogError("023", "No ArgumentOutOfRangeException throw out expected" + " when chars is :" + ToString(chars));
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
            //Expected Exception
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("024", "Unexpected exception:" + e + " when chars is :" + ToString(chars));
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
