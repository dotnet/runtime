// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

public class DecoderCtorDecoder : Decoder
{
    public override int GetCharCount(byte[] bytes, int index, int count)
    {
        throw new Exception("The method or operation is not implemented.");
    }

    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
    {
        throw new Exception("The method or operation is not implemented.");
    }
}


/// <summary>
/// Ctor
/// </summary>
public class DecoderCtor
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call ctor to create a decoder instance");

        try
        {
            DecoderCtorDecoder decoder = new DecoderCtorDecoder();
            if (decoder == null)
            {
                TestLibrary.TestFramework.LogError("001.1", "Call ctor to create a decoder instance returns null reference");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        DecoderCtor test = new DecoderCtor();

        TestLibrary.TestFramework.BeginTestCase("DecoderCtor");

        if (test.RunTests())
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
}
