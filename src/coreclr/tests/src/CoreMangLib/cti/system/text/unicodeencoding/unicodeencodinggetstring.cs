// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

///<summary>
///System.Test.UnicodeEncoding.GetString(System.Byte[],System.Int32,System.Int32) [v-zuolan]
///</summary>

public class UnicodeEncodingGetString
{

    public static int Main()
    {
        UnicodeEncodingGetString testObj = new UnicodeEncodingGetString();
        TestLibrary.TestFramework.BeginTestCase("for method of System.Test.UnicodeEncoding.GetString");
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
        retVal = PosTest4() && retVal;
        TestLibrary.TestFramework.LogInformation("Positive");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;


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

        Char[] srcChars = GetCharArray(10);
        Char[] desChars = new Char[10];
        Byte[] bytes = new Byte[20];

        UnicodeEncoding uEncoding = new UnicodeEncoding();
        int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);

        bool expectedValue = true;
        bool actualValue = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Invoke the method");
        try
        {
            String desString = uEncoding.GetString(bytes, 0, 20);

            desChars = desString.ToCharArray();

            for (int i = 0; i < 10; i++)
            {
                actualValue = actualValue & (desChars[i] == srcChars[i]);
            }

            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("001", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")" + " when chars is :" + ToString(srcChars));
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception:" + e + " when chars is :" + ToString(srcChars));
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        Char[] srcChars = GetCharArray(10);
        Char[] desChars = new Char[10];
        Byte[] bytes = new Byte[20];

        UnicodeEncoding uEncoding = new UnicodeEncoding();
        int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);

        bool expectedValue = true;
        bool actualValue = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2:Invoke the method,convert 1 char");
        try
        {
            String desString = uEncoding.GetString(bytes, 0, 2);

            desChars = desString.ToCharArray();

            for (int i = 0; i < 1; i++)
            {
                actualValue = actualValue & (desChars[i] == srcChars[i]);
            }

            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("003", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")" + " when chars is :" + ToString(srcChars));
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception:" + e + " when chars is :" + ToString(srcChars));
            retVal = false;
        }
        return retVal;
    }


    public bool PosTest3()
    {
        bool retVal = true;

        Char[] srcChars = GetCharArray(10);
        Char[] desChars = new Char[10];
        Byte[] bytes = new Byte[20];

        UnicodeEncoding uEncoding = new UnicodeEncoding();
        int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);

        String expectedValue = "";
        String actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest3:Invoke the method and set byteIndex as 0 and byteCount as 0.");
        try
        {
            actualValue = uEncoding.GetString(bytes, 0, 0);

            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("005", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")" + " when chars is :" + ToString(srcChars));
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception:" + e + " when chars is :" + ToString(srcChars));
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        Char[] srcChars = GetCharArray(10);
        Char[] desChars = new Char[10];
        Byte[] bytes = new Byte[20];

        UnicodeEncoding uEncoding = new UnicodeEncoding();
        int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);

        String expectedValue = "";
        String actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest4:Invoke the method and set byteIndex out of range.");
        try
        {
            actualValue = uEncoding.GetString(bytes, 20, 0);

            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("017", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")" + " when chars is :" + ToString(srcChars));
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }    
    #endregion

    #region Negative Test Logic

    public bool NegTest1()
    {
        bool retVal = true;

        Char[] srcChars = GetCharArray(10);
        Char[] desChars = new Char[10];
        Byte[] bytes = new Byte[20];

        UnicodeEncoding uEncoding = new UnicodeEncoding();
        int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);

        String actualValue;

        TestLibrary.TestFramework.BeginScenario("NegTest1:Invoke the method and set chars as null");
        try
        {
            actualValue = uEncoding.GetString(null,0,0);

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

        Char[] srcChars = GetCharArray(10);
        Char[] desChars = new Char[10];
        Byte[] bytes = new Byte[20];

        UnicodeEncoding uEncoding = new UnicodeEncoding();
        int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);

        String actualValue;

        TestLibrary.TestFramework.BeginScenario("NegTest2:Invoke the method and set byteIndex out of range.");
        try
        {
            actualValue = uEncoding.GetString(bytes, 21, 0);

            TestLibrary.TestFramework.LogError("009", "No ArgumentOutOfRangeException throw out expected.");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }


    public bool NegTest3()
    {
        bool retVal = true;

        Char[] srcChars = GetCharArray(10);
        Char[] desChars = new Char[10];
        Byte[] bytes = new Byte[20];

        UnicodeEncoding uEncoding = new UnicodeEncoding();
        int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);

        String actualValue;

        TestLibrary.TestFramework.BeginScenario("NegTest3:Invoke the method and set byteIndex out of range.");
        try
        {
            actualValue = uEncoding.GetString(bytes, -1, 0);

            TestLibrary.TestFramework.LogError("011", "No ArgumentOutOfRangeException throw out expected.");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }


    public bool NegTest4()
    {
        bool retVal = true;

        Char[] srcChars = GetCharArray(10);
        Char[] desChars = new Char[10];
        Byte[] bytes = new Byte[20];

        UnicodeEncoding uEncoding = new UnicodeEncoding();
        int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);

        String actualValue;

        TestLibrary.TestFramework.BeginScenario("NegTest4:Invoke the method and set byteCount out of range.");
        try
        {
            actualValue = uEncoding.GetString(bytes, 0, -1);

            TestLibrary.TestFramework.LogError("013", "No ArgumentOutOfRangeException throw out expected.");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest5()
    {
        bool retVal = true;

        Char[] srcChars = GetCharArray(10);
        Char[] desChars = new Char[10];
        Byte[] bytes = new Byte[20];

        UnicodeEncoding uEncoding = new UnicodeEncoding();
        int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);

        String actualValue;

        TestLibrary.TestFramework.BeginScenario("NegTest5:Invoke the method and set byteCount out of range.");
        try
        {
            actualValue = uEncoding.GetString(bytes, 0, 21);

            TestLibrary.TestFramework.LogError("015", "No ArgumentOutOfRangeException throw out expected.");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
