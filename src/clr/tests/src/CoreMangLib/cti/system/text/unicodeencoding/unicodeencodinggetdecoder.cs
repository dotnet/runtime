// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

///<summary>
///System.Test.UnicodeEncoding.GetDecoder() [v-zuolan]
///</summary>

public class UnicodeEncodingGetDecoder
{

    public static int Main()
    {
        UnicodeEncodingGetDecoder testObj = new UnicodeEncodingGetDecoder();
        TestLibrary.TestFramework.BeginTestCase("for method of System.Test.UnicodeEncoding.GetDecoder");
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
        int buffer;
        int outChars;
        bool completed;

        UnicodeEncoding uEncoding = new UnicodeEncoding();
        int byteCount = uEncoding.GetBytes(srcChars, 0, 10, bytes, 0);

        bool expectedValue = true;
        bool actualValue = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Invoke the method");
        try
        {
            Decoder dC = uEncoding.GetDecoder();
            
            dC.Convert(bytes, 0, 20, desChars, 0, 10, true,out buffer,out outChars, out completed);

            if (completed)
            {
                for (int i = 0; i < 10; i++)
                {
                    actualValue = actualValue & (desChars[i] == srcChars[i]);
                }
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

    #endregion
}
