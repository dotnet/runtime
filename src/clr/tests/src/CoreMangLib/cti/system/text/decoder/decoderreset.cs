// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

/// <summary>
/// Reset
/// </summary>
public class DecoderReset
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call Reset to reset the decoder instance without perform any convertions");

        try
        {
            Decoder decoder = Encoding.UTF8.GetDecoder();
            decoder.Reset();
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call Reset to reset the decoder instance after a valid convertions");

        try
        {
            Decoder decoder = Encoding.UTF8.GetDecoder();
            byte[] bytes = new byte[127];
            char[] chars = new char[bytes.Length];
            for ( int i = 0; i < bytes.Length; ++i )
            {
                bytes[i] = (byte)i;
            }

            decoder.GetChars(bytes, 0, bytes.Length, chars, 0, false);
            decoder.Reset();

            decoder.GetChars(bytes, 0, bytes.Length, chars, 0, true);
            decoder.Reset();
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call Reset to reset the decoder instance after a invalid convertions");

        try
        {
            Decoder decoder = Encoding.Unicode.GetDecoder();
            byte[] bytes = new byte[127];
            char[] chars = new char[bytes.Length];
            for (int i = 0; i < bytes.Length; ++i)
            {
                bytes[i] = (byte)i;
            }

            try
            {
                decoder.GetChars(bytes, 0, bytes.Length, chars, chars.Length - 1, false);
            }
            catch (ArgumentException)
            {
                decoder.Reset();
            }
            decoder.GetChars(bytes, 0, bytes.Length, chars, 0, false);

            try
            {
                decoder.GetChars(bytes, 0, bytes.Length, chars, chars.Length - 1, true);
            }
            catch (ArgumentException)
            {
                decoder.Reset();
            }
            decoder.GetChars(bytes, 0, bytes.Length, chars, 0, true);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        DecoderReset test = new DecoderReset();

        TestLibrary.TestFramework.BeginTestCase("DecoderReset");

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
