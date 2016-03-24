// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;
/// <summary>
/// Encoding.GetChars(byte[],Int32,Int32,char[],Int32)
/// </summary>
public class EncodingGetChars3
{
    public static int Main()
    {
        EncodingGetChars3 enGetChars3 = new EncodingGetChars3();
        TestLibrary.TestFramework.BeginTestCase("EncodingGetChars3");
        if (enGetChars3.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;
        retVal = NegTest6() && retVal;
        retVal = NegTest7() && retVal;
        retVal = NegTest8() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Invoke the GetChars method 1");
        try
        {
            byte[] bytes = new byte[0];
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            int byteIndex = 0;
            int bytecount = 0;
            char[] chars = new char[] { TestLibrary.Generator.GetChar(-55)};
            int intVal = myEncode.GetChars(bytes, byteIndex, bytecount,chars, 0);
            if (intVal != 0 || chars.Length != 1)
            {
                TestLibrary.TestFramework.LogError("001", "the ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2:Invoke the GetChars method 2");
        try
        {
            string myStr = "za\u0306\u01fd\u03b2";
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            byte[] bytes = myEncode.GetBytes(myStr);
            int byteIndex = 0;
            int bytecount = 0;
            char[] chars = new char[] { TestLibrary.Generator.GetChar(-55) };
            int intVal = myEncode.GetChars(bytes, byteIndex, bytecount, chars, 0);
            if (intVal != 0 || chars.Length != 1)
            {
                TestLibrary.TestFramework.LogError("003", "the ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3:Invoke the GetChars method 3");
        try
        {
            string myStr = "za\u0306\u01fd\u03b2";
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            byte[] bytes = myEncode.GetBytes(myStr);
            int byteIndex = 0;
            int bytecount = bytes.Length;
            char[] chars = new char[myStr.Length];
            int intVal = myEncode.GetChars(bytes, byteIndex, bytecount, chars, 0);
            if (intVal != myStr.Length || chars.Length != myStr.Length)
            {
                TestLibrary.TestFramework.LogError("005", "the ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4:Invoke the GetChars method 4");
        try
        {
            string myStr = "za\u0306\u01fd\u03b2";
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            byte[] bytes = myEncode.GetBytes(myStr);
            int byteIndex = 0;
            int bytecount = bytes.Length;
            char[] chars = new char[myStr.Length + myStr.Length];
            int intVal = myEncode.GetChars(bytes, byteIndex, bytecount, chars, myStr.Length - 1);
            string subchars = null;
            for (int i = 0; i < myStr.Length - 1; i++)
            {
                subchars += chars[i].ToString();
            }
            if (intVal != myStr.Length || subchars != "\0\0\0\0")
            {
                TestLibrary.TestFramework.LogError("007", "the ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest5:Invoke the GetChars method 5");
        try
        {
            string myStr = "za\u0306\u01fd\u03b2";
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            byte[] bytes = myEncode.GetBytes(myStr);
            int byteIndex = 0;
            int bytecount = bytes.Length - 2;
            char[] chars = new char[myStr.Length -1];
            int intVal = myEncode.GetChars(bytes, byteIndex, bytecount, chars, 0);
            string strVal = new string(chars);
            if (intVal != myStr.Length - 1 || strVal != "za\u0306\u01fd")
            {
                TestLibrary.TestFramework.LogError("009", "the ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1:the byte array is null");
        try
        {
            byte[] bytes = null;
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            int byteIndex = 0;
            int bytecount = 0;
            char[] chars = new char[0];
            int intVal = myEncode.GetChars(bytes, byteIndex, bytecount, chars, 0);
            TestLibrary.TestFramework.LogError("N001", "the byte array is null but not throw exception");
            retVal = false;
        }
        catch (ArgumentNullException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest2:the char array is null");
        try
        {
            byte[] bytes = new byte[0];
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            int byteIndex = 0;
            int bytecount = 0;
            char[] chars = null;
            int intVal = myEncode.GetChars(bytes, byteIndex, bytecount, chars, 0);
            TestLibrary.TestFramework.LogError("N003", "the char array is null but not throw exception");
            retVal = false;
        }
        catch (ArgumentNullException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest3:the char array has no enough capacity to hold the chars");
        try
        {
            string myStr = "helloworld";
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            byte[] bytes = myEncode.GetBytes(myStr);
            int byteIndex = 0;
            int bytecount = bytes.Length;
            char[] chars = new char[0];
            int intVal = myEncode.GetChars(bytes, byteIndex, bytecount, chars, 0);
            TestLibrary.TestFramework.LogError("N005", "the char array has no enough capacity to hold the chars but not throw exception");
            retVal = false;
        }
        catch (ArgumentException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N006", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest4:the byteIndex is less than zero");
        try
        {
            string myStr = "helloworld";
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            byte[] bytes = myEncode.GetBytes(myStr);
            int byteIndex = -1;
            int bytecount = bytes.Length;
            char[] chars = new char[myStr.Length];
            int intVal = myEncode.GetChars(bytes, byteIndex, bytecount, chars, 0);
            TestLibrary.TestFramework.LogError("N007", "the byteIndex is less than zero but not throw exception");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N008", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest5:the bytecount is less than zero");
        try
        {
            string myStr = "helloworld";
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            byte[] bytes = myEncode.GetBytes(myStr);
            int byteIndex = 0;
            int bytecount = -1;
            char[] chars = new char[myStr.Length];
            int intVal = myEncode.GetChars(bytes, byteIndex, bytecount, chars, 0);
            TestLibrary.TestFramework.LogError("N009", "the byteIndex is less than zero but not throw exception");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N010", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest6()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest6:the charIndex is less than zero");
        try
        {
            string myStr = "helloworld";
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            byte[] bytes = myEncode.GetBytes(myStr);
            int byteIndex = 0;
            int bytecount = bytes.Length;
            char[] chars = new char[myStr.Length];
            int intVal = myEncode.GetChars(bytes, byteIndex, bytecount, chars, -1);
            TestLibrary.TestFramework.LogError("N011", "the charIndex is less than zero but not throw exception");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N012", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest7()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest7:the charIndex is not valid index in chars array");
        try
        {
            string myStr = "helloworld";
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            byte[] bytes = myEncode.GetBytes(myStr);
            int byteIndex = 0;
            int bytecount = bytes.Length;
            char[] chars = new char[myStr.Length];
            int intVal = myEncode.GetChars(bytes, byteIndex, bytecount, chars, myStr.Length + 1);
            TestLibrary.TestFramework.LogError("N013", "the charIndex is not valid index in chars array but not throw exception");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N014", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest8()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest8:the byteIndex and bytecount do not denote valid range of the bytes arry");
        try
        {
            string myStr = "helloworld";
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            byte[] bytes = myEncode.GetBytes(myStr);
            int byteIndex = 0;
            int bytecount = bytes.Length + 1;
            char[] chars = new char[myStr.Length];
            int intVal = myEncode.GetChars(bytes, byteIndex, bytecount, chars, myStr.Length + 1);
            TestLibrary.TestFramework.LogError("N015", "the byteIndex and bytecount do not denote valid range of the bytes arry but not throw exception");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N016", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
