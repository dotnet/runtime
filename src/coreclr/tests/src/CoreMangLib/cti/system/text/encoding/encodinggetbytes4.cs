// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

/// <summary>
/// GetBytes(System.String)
/// </summary>

public class EncodingGetBytes4
{
    #region Private Fields
    private const string c_TEST_STR = "za\u0306\u01FD\u03B2\uD8FF\uDCFF";
    #endregion

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");

        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        
        //
        // TODO: Add your negative test cases here
        //
        // TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
 

    public bool PosTest2()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify method GetBytes(System.String) with UTF8.");

        try
        {
            Encoding u8 = Encoding.UTF8;

            byte[] actualBytesUTF8 = new byte[] {
                0x7A, 0x61, 0xCC ,0x86, 0xC7 ,0xBD,
                0xCE ,0xB2 ,0xF1, 0x8F ,0xB3 ,0xBF};

            if (!VerifyByteItemValue(u8.GetBytes(c_TEST_STR), actualBytesUTF8))
            {
                TestLibrary.TestFramework.LogError("002.1", "Method GetBytes Err.");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify method GetBytes(System.String) with Unicode.");

        try
        {
            Encoding u16LE = Encoding.Unicode;

            byte[] actualBytesUnicode = new byte[]{
                0x7A, 0x00, 0x61, 0x00, 0x06, 0x03,
                0xFD, 0x01, 0xB2, 0x03, 0xFF, 0xD8, 
                0xFF, 0xDC};

            if (!VerifyByteItemValue(u16LE.GetBytes(c_TEST_STR), actualBytesUnicode))
            {
                TestLibrary.TestFramework.LogError("003.1", "Method GetBytes Err.");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify method GetBytes(System.String) with BigEndianUnicode.");

        try
        {
            Encoding u16BE = Encoding.BigEndianUnicode;

            byte[] actualBytesBigEndianUnicode = new byte[]{
                0x00, 0x7A, 0x00, 0x61, 0x03, 0x06, 
                0x01, 0xFD, 0x03, 0xB2, 0xD8, 0xFF, 
                0xDC, 0xFF};

            if (!VerifyByteItemValue(u16BE.GetBytes(c_TEST_STR), actualBytesBigEndianUnicode))
            {
                TestLibrary.TestFramework.LogError("004.1", "Method GetBytes Err.");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentNullException is not thrown.");

        try
        {
            String testStr = null;
            Encoding u16BE = Encoding.BigEndianUnicode;

            byte[] getBytes = u16BE.GetBytes(testStr);

            TestLibrary.TestFramework.LogError("101.1", "ArgumentNullException is not thrown.");
            retVal = false;
        }
        catch (ArgumentNullException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        EncodingGetBytes4 test = new EncodingGetBytes4();

        TestLibrary.TestFramework.BeginTestCase("EncodingGetBytes4");

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

    #region Private Method
    private bool VerifyByteItemValue(byte[] getBytes, byte[] actualBytes)
    {
        if (getBytes.Length != actualBytes.Length)
            return false;
        else
        {
            for (int i = 0; i < getBytes.Length; i++)
                if (getBytes[i] != actualBytes[i])
                    return false;
        }

        return true;
    }
    #endregion
}
