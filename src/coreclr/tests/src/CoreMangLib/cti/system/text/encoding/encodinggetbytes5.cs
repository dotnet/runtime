// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

/// <summary>
/// GetBytes(System.String,System.Int32,System.Int32,System.Byte[],System.Int32)
/// </summary>

public class EncodingGetBytes5
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify method GetBytes(System.String) with UTF8.");

        try
        {
            Encoding u8 = Encoding.UTF8;
            byte[] bytes = new byte[u8.GetMaxByteCount(3)];

            if (u8.GetBytes(c_TEST_STR, 4, 3, bytes, bytes.GetLowerBound(0)) != 6)
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
            byte[] bytes = new byte[u16LE.GetMaxByteCount(3)];

            if (u16LE.GetBytes(c_TEST_STR, 4, 3, bytes, bytes.GetLowerBound(0)) != 6)
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
            byte[] bytes = new byte[u16BE.GetMaxByteCount(3)];

            if (u16BE.GetBytes(c_TEST_STR, 4, 3, bytes, bytes.GetLowerBound(0)) != 6)
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
            string testNullStr = null;

            Encoding u16BE = Encoding.BigEndianUnicode;
            byte[] bytes = new byte[u16BE.GetMaxByteCount(3)];

            int i = u16BE.GetBytes(testNullStr, 4, 3, bytes, bytes.GetLowerBound(0));

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
            Encoding u16BE = Encoding.BigEndianUnicode;
            byte[] bytes = null;

            int i = u16BE.GetBytes(c_TEST_STR, 4, 3, bytes, 1);

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
            Encoding u16BE = Encoding.BigEndianUnicode;
            byte[] bytes = new byte[u16BE.GetMaxByteCount(3)];

            int i = u16BE.GetBytes(c_TEST_STR, -1, 3, bytes, bytes.GetLowerBound(0));

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
            Encoding u16BE = Encoding.BigEndianUnicode;
            byte[] bytes = new byte[u16BE.GetMaxByteCount(3)];

            int i = u16BE.GetBytes(c_TEST_STR, 4, -1, bytes, bytes.GetLowerBound(0));

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
            Encoding u16BE = Encoding.BigEndianUnicode;
            byte[] bytes = new byte[u16BE.GetMaxByteCount(3)];

            int i = u16BE.GetBytes(c_TEST_STR, 4, 3, bytes, -1);

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
            Encoding u16BE = Encoding.BigEndianUnicode;
            byte[] bytes = new byte[u16BE.GetMaxByteCount(3)];

            int i = u16BE.GetBytes(c_TEST_STR, c_TEST_STR.Length - 1, 3, bytes, bytes.GetLowerBound(0));

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
            Encoding u16BE = Encoding.BigEndianUnicode;
            byte[] bytes = new byte[u16BE.GetMaxByteCount(3)];

            int i = u16BE.GetBytes(c_TEST_STR, 4, 3, bytes, bytes.Length - 1);

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
        EncodingGetBytes5 test = new EncodingGetBytes5();

        TestLibrary.TestFramework.BeginTestCase("EncodingGetBytes5");

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
