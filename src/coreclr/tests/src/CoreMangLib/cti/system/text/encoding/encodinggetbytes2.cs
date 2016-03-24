// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

/// <summary>
/// GetBytes(System.Char[],System.Int32,System.Int32)
/// </summary>

public class EncodingGetBytes2
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify method GetBytes.");

        try
        {
            char[] testChar = new char[] { 'z', 'a', '\u0306', '\u01FD', '\u03B2', '\uD8FF', '\uDCFF' };


            Encoding u8 = Encoding.UTF8;
            Encoding u16LE = Encoding.Unicode;
            Encoding u16BE = Encoding.BigEndianUnicode;


            byte[] actualBytesUTF8 = new byte[] {
                0x7A, 0x61, 0xCC ,0x86, 0xC7 ,0xBD,
                0xCE ,0xB2 ,0xF1, 0x8F ,0xB3 ,0xBF};

            byte[] actualBytesUnicode = new byte[]{
                0x7A, 0x00, 0x61, 0x00, 0x06, 0x03,
                0xFD, 0x01, 0xB2, 0x03, 0xFF, 0xD8, 
                0xFF, 0xDC};

            byte[] actualBytesBigEndianUnicode = new byte[]{
                0x00, 0x7A, 0x00, 0x61, 0x03, 0x06, 
                0x01, 0xFD, 0x03, 0xB2, 0xD8, 0xFF, 
                0xDC, 0xFF};


            if (!VerifyByteItemValue(u8.GetBytes(testChar,0,testChar.Length), actualBytesUTF8)                ||
                !VerifyByteItemValue(u16LE.GetBytes(testChar,0,testChar.Length), actualBytesUnicode)          ||
                !VerifyByteItemValue(u16BE.GetBytes(testChar,0,testChar.Length), actualBytesBigEndianUnicode))
            {
                TestLibrary.TestFramework.LogError("001.1", "Method GetBytes Err.");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify method GetBytes when chars is null.");

        try
        {
            char[] testChar = new char[0];

             Encoding u8 = Encoding.UTF8;
            Encoding u16LE = Encoding.Unicode;
            Encoding u16BE = Encoding.BigEndianUnicode;

            byte[] result = new byte[0];

            if (!VerifyByteItemValue(u8.GetBytes(testChar, 0, testChar.Length), result)    ||
                !VerifyByteItemValue(u16LE.GetBytes(testChar, 0, testChar.Length), result) ||
                !VerifyByteItemValue(u16BE.GetBytes(testChar, 0, testChar.Length), result))
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
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentNullException is not thrown.");

        try
        {
            char[] testChar = null;

            Encoding u7 = Encoding.UTF8;

            byte[] result = u7.GetBytes(testChar, 2, 1);

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

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: ArgumentOutOfRangeException is not thrown.");

        try
        {
            char[] testChar = new char[] { 'z', 'a', '\u0306', '\u01FD', '\u03B2', '\uD8FF', '\uDCFF' };

            Encoding u7 = Encoding.UTF8;

            byte[] result = u7.GetBytes(testChar, -1, 1);

            TestLibrary.TestFramework.LogError("102.1", "ArgumentOutOfRangeException is not thrown.");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest3: ArgumentOutOfRangeException is not thrown.");

        try
        {
            char[] testChar = new char[] { 'z', 'a', '\u0306', '\u01FD', '\u03B2', '\uD8FF', '\uDCFF' };

            Encoding u7 = Encoding.UTF8;

            byte[] result = u7.GetBytes(testChar, 0, -1);

            TestLibrary.TestFramework.LogError("103.1", "ArgumentOutOfRangeException is not thrown.");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("103.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest4: ArgumentOutOfRangeException is not thrown.");

        try
        {
            char[] testChar = new char[] { 'z', 'a', '\u0306', '\u01FD', '\u03B2', '\uD8FF', '\uDCFF' };

            Encoding u7 = Encoding.UTF8;

            byte[] result = u7.GetBytes(testChar, 0, testChar.Length + 1);

            TestLibrary.TestFramework.LogError("104.1", "ArgumentOutOfRangeException is not thrown.");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        EncodingGetBytes2 test = new EncodingGetBytes2();

        TestLibrary.TestFramework.BeginTestCase("EncodingGetBytes2");

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
