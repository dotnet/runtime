// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;
/// <summary>
/// Encoding.GetCharCount(Byte[],Int32,Int32)
/// </summary>
public class EncodingGetCharCount2
{
    public static int Main()
    {
        EncodingGetCharCount2 egcc2 = new EncodingGetCharCount2();
        TestLibrary.TestFramework.BeginTestCase("EncodingGetCharCount2");
        if (egcc2.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Invoke the GetCharCount method 1");
        try
        {
            byte[] bytes = new byte[0];
            Encoding myEncode = Encoding.Unicode;
            int startIndex = 0;
            int count = 0;
            int intVal = myEncode.GetCharCount(bytes, startIndex, count);
            if (intVal != 0)
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Invoke the GetCharCount method 2");
        try
        {
            string myStr = "za\u0306\u01fd\u03b2";
            Encoding myEncode = Encoding.Unicode;
            byte[] bytes = myEncode.GetBytes(myStr);
            int startIndex = 0;
            int count = 0;
            int intVal = myEncode.GetCharCount(bytes, startIndex, count);
            if (intVal != 0)
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
        TestLibrary.TestFramework.BeginScenario("PosTest3:Invoke the GetCharCount method 3");
        try
        {
            string myStr = "za\u0306\u01fd\u03b2";
            Encoding myEncode = Encoding.Unicode;
            byte[] bytes = myEncode.GetBytes(myStr);
            int startIndex = 0;
            int count = bytes.Length;
            int intVal = myEncode.GetCharCount(bytes, startIndex, count);
            if (intVal != myStr.Length)
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
        TestLibrary.TestFramework.BeginScenario("PosTest4:Invoke the GetCharCount method 4");
        try
        {
            string myStr = "\\abc\\u0020";
            Encoding myEncode = Encoding.Unicode;
            byte[] bytes = myEncode.GetBytes(myStr);
            int startIndex = 0;
            int count = bytes.Length;
            int intVal = myEncode.GetCharCount(bytes, startIndex, count);
            if (intVal != myStr.Length)
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
    #endregion
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1:The byte array is null");
        try
        {
            byte[] bytes = null;
            Encoding myEncode = Encoding.Unicode;
            int startIndex = 0;
            int count = 0;
            int intVal = myEncode.GetCharCount(bytes, startIndex, count);
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
        TestLibrary.TestFramework.BeginScenario("NegTest2:The startIndex is less than zero");
        try
        {
            string myStr = "helloworld";
            Encoding myEncode = Encoding.Unicode;
            byte[] bytes = myEncode.GetBytes(myStr);
            int startIndex = -1;
            int count = myStr.Length;
            int intVal = myEncode.GetCharCount(bytes, startIndex, count);
            TestLibrary.TestFramework.LogError("N003", "the startIndex is less than zero but not throw exception");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException) { }
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
        TestLibrary.TestFramework.BeginScenario("NegTest3:The count is less than zero");
        try
        {
            string myStr = "helloworld";
            Encoding myEncode = Encoding.Unicode;
            byte[] bytes = myEncode.GetBytes(myStr);
            int startIndex = 0;
            int count = -1;
            int intVal = myEncode.GetCharCount(bytes, startIndex, count);
            TestLibrary.TestFramework.LogError("N005", "the count is less than zero but not throw exception");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException) { }
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
        TestLibrary.TestFramework.BeginScenario("NegTest4:The startIndex and count do not denote a valid range of bytes");
        try
        {
            string myStr = "helloworld";
            Encoding myEncode = Encoding.Unicode;
            byte[] bytes = myEncode.GetBytes(myStr);
            int startIndex = 0;
            int count = bytes.Length + 1;
            int intVal = myEncode.GetCharCount(bytes, startIndex, count);
            TestLibrary.TestFramework.LogError("N007", "The startIndex and count do not denote a valid range of bytes but not throw exception");
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
    #endregion
}