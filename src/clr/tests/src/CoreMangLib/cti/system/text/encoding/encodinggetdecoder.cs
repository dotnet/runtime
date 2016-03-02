// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;
/// <summary>
/// Encoding.GetDecoder()
/// </summary>
public class EncodingGetDecoder
{
    public static int Main()
    {
        EncodingGetDecoder enGetDecoder = new EncodingGetDecoder();
        TestLibrary.TestFramework.BeginTestCase("EncodingGetDecoder");
        if (enGetDecoder.RunTests())
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
        // retVal = PosTest2() && retVal;
        //retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;

        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Get Decoder in the UTF8Encoding");
        try
        {
            Encoding myEncode = Encoding.GetEncoding("utf-8");
            Decoder decoderVal = myEncode.GetDecoder();
            if (decoderVal.ToString() != "System.Text.UTF8Encoding+UTF8Decoder")
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Get Decoder in the UTF32Encoding");
        try
        {
            Encoding myEncode = Encoding.GetEncoding("utf-32");
            Decoder decoderVal = myEncode.GetDecoder();
            if (decoderVal.ToString() != "System.Text.UTF32Encoding+UTF32Decoder")
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
        TestLibrary.TestFramework.BeginScenario("PosTest3:Get Decoder in the UTF7Encoding");
        try
        {
            Encoding myEncode = Encoding.GetEncoding("utf-7");            
            Decoder decoderVal = myEncode.GetDecoder();
            if (decoderVal.ToString() != "System.Text.UTF7Encoding+Decoder")
            {
                TestLibrary.TestFramework.LogError("005", "the ExpectResult is not the ActualResult. Actual: " + decoderVal.ToString());
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
        TestLibrary.TestFramework.BeginScenario("PosTest4:Get Decoder in the Unicode");
        try
        {
            Encoding myEncode = Encoding.Unicode;
            Decoder decoderVal = myEncode.GetDecoder();
            if (decoderVal.ToString() != "System.Text.UnicodeEncoding+Decoder")
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
}