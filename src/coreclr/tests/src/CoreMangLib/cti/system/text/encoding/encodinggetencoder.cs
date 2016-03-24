// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;
/// <summary>
/// Encoding.GetEncoder()
/// </summary>
public class EncodingGetEncoder
{
    public static int Main()
    {
        EncodingGetEncoder enGetEncoder = new EncodingGetEncoder();
        TestLibrary.TestFramework.BeginTestCase("EncodingGetEncoder");
        if (enGetEncoder.RunTests())
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
        retVal = PosTest4() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Get Encoder in the UTF8Encoding");
        try
        {
            Encoding myEncode = Encoding.GetEncoding("utf-8");
            Encoder encoderVal = myEncode.GetEncoder();
            if (encoderVal.ToString() != "System.Text.UTF8Encoding+UTF8Encoder")
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Get Encoder in the UTF32Encoding");
        try
        {
            Encoding myEncode = Encoding.GetEncoding("utf-32");
            Encoder encoderVal = myEncode.GetEncoder();
            if (encoderVal.ToString() != "System.Text.EncoderNLS")
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
  
    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4:Get Decoder in the Unicode");
        try
        {
            Encoding myEncode = Encoding.Unicode;
            Encoder encoderVal = myEncode.GetEncoder();
            if (encoderVal.ToString() != "System.Text.EncoderNLS")
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