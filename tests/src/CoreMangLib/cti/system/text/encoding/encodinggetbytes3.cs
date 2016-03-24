// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

/// <summary>
/// GetBytes(System.Char[],System.Int32,System.Int32,System.Byte[],System.Int32)
/// </summary>

public class EncodingGetBytes3
{
    #region Private Fileds
    private char[] testChar = new char[] { 'z', 'a', '\u0306', '\u01FD', '\u03B2', '\uD8FF', '\uDCFF' };
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
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;
        retVal = NegTest6() && retVal;
        retVal = NegTest7() && retVal;

        return retVal;
    }

    #region Positive Test Cases


    public bool PosTest2()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify method GetBytes(System.Char[],System.Int32,System.Int32,System.Byte[],System.Int32) with UTF8.");

        try
        {
            Encoding u8 = Encoding.UTF8;

            byte[] u8Bytes = u8.GetBytes(testChar, 4, 3);
            int u8ByteIndex = u8Bytes.GetLowerBound(0);

            if (u8.GetBytes(testChar, 4, 3, u8Bytes, u8ByteIndex) != 6)
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify method GetBytes(System.Char[],System.Int32,System.Int32,System.Byte[],System.Int32) with Unicode.");

        try
        {
            Encoding u16LE = Encoding.Unicode;

            byte[] u16LEBytes = u16LE.GetBytes(testChar, 4, 3);
            int u16LEByteIndex = u16LEBytes.GetLowerBound(0);

            if (u16LE.GetBytes(testChar, 4, 3, u16LEBytes, u16LEByteIndex) != 6)
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify method GetBytes(System.Char[],System.Int32,System.Int32,System.Byte[],System.Int32) with BigEndianUnicode.");

        try
        {
            Encoding u16BE = Encoding.BigEndianUnicode;

            byte[] u16BEBytes = u16BE.GetBytes(testChar, 4, 3);
            int u16BEByteIndex = u16BEBytes.GetLowerBound(0);

            if (u16BE.GetBytes(testChar, 4, 3, u16BEBytes, u16BEByteIndex) != 6)
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
            char[] testNullChar = null;

            Encoding u7 = Encoding.UTF8;

            byte[] u7Bytes = u7.GetBytes(testChar, 4, 3);
            int u7ByteIndex = u7Bytes.GetLowerBound(0);

            int result = u7.GetBytes(testNullChar, 4, 3, u7Bytes, u7ByteIndex);

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

        TestLibrary.TestFramework.BeginScenario("NegTest2: ArgumentNullException is not thrown.");

        try
        {
            Encoding u7 = Encoding.UTF8;

            byte[] u7Bytes = null;
            int u7ByteIndex = 1;

            int result = u7.GetBytes(testChar, 4, 3, u7Bytes, u7ByteIndex);

            TestLibrary.TestFramework.LogError("102.1", "ArgumentNullException is not thrown.");
            retVal = false;
        }
        catch (ArgumentNullException)
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
            Encoding u7 = Encoding.UTF8;

            byte[] u7Bytes = u7.GetBytes(testChar, 4, 3);
            int u7ByteIndex = u7Bytes.GetLowerBound(0);

            int result = u7.GetBytes(testChar, -1, 3, u7Bytes, u7ByteIndex);

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
            Encoding u7 = Encoding.UTF8;

            byte[] u7Bytes = u7.GetBytes(testChar, 4, 3);
            int u7ByteIndex = u7Bytes.GetLowerBound(0);

            int result = u7.GetBytes(testChar, 4, -1, u7Bytes, u7ByteIndex);

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

    public bool NegTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest5: ArgumentOutOfRangeException is not thrown.");

        try
        {
            Encoding u7 = Encoding.UTF8;

            byte[] u7Bytes = u7.GetBytes(testChar, 4, 3);
            int u7ByteIndex = u7Bytes.GetLowerBound(0);

            int result = u7.GetBytes(testChar, testChar.Length - 1, 3, u7Bytes, u7ByteIndex);

            TestLibrary.TestFramework.LogError("105.1", "ArgumentOutOfRangeException is not thrown.");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("105.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest6: ArgumentOutOfRangeException is not thrown.");

        try
        {
            Encoding u7 = Encoding.UTF8;

            byte[] u7Bytes = u7.GetBytes(testChar, 4, 3);

            int result = u7.GetBytes(testChar, 4, 3, u7Bytes, -1);

            TestLibrary.TestFramework.LogError("106.1", "ArgumentOutOfRangeException is not thrown.");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("106.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest7()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest7: ArgumentException is not thrown.");

        try
        {
            Encoding u7 = Encoding.UTF8;

            byte[] u7Bytes = u7.GetBytes(testChar, 4, 3);
            int u7ByteIndex = u7Bytes.GetLowerBound(0);

            int result = u7.GetBytes(testChar, 4, 3, u7Bytes, u7Bytes.Length - 1);

            TestLibrary.TestFramework.LogError("107.1", "ArgumentException is not thrown.");
            retVal = false;
        }
        catch (ArgumentException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("107.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        EncodingGetBytes3 test = new EncodingGetBytes3();

        TestLibrary.TestFramework.BeginTestCase("EncodingGetBytes3");

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
