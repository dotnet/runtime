// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

/// <summary>
/// GetByteCount(System.Char[],System.Int32,System.Int32) [v-jianq]
/// </summary>

public class UTF8EncodingGetCharCount
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        //
        // TODO: Add your negative test cases here
        //
        // TestLibrary.TestFramework.LogInformation("[Negative]");
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify method GetByteCount with a non-null Byte[]");

        try
        {
            Byte[] bytes = new Byte[] {
                                         85,  84,  70,  56,  32,  69, 110,
                                         99, 111, 100, 105, 110, 103,  32,
                                         69, 120,  97, 109, 112, 108, 101};

            UTF8Encoding utf8 = new UTF8Encoding();
            int charCount = utf8.GetCharCount(bytes, 2, 8);

            if (charCount != 8)
            {
                TestLibrary.TestFramework.LogError("001.1", "Method GetByteCount Err.");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify method GetByteCount with a null Byte[]");

        try
        {
            Byte[] bytes = new Byte[] { };

            UTF8Encoding utf8 = new UTF8Encoding();
            int charCount = utf8.GetCharCount(bytes, 0, 0);

            if (charCount != 0)
            {
                TestLibrary.TestFramework.LogError("002.1", "Method GetByteCount Err.");
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentNullException is not thrown when bytes is a null reference");

        try
        {
            Byte[] bytes = null;
            UTF8Encoding utf8 = new UTF8Encoding();
            int charCount = utf8.GetCharCount(bytes, 2, 8);

            TestLibrary.TestFramework.LogError("101.1", "ArgumentNullException is not thrown when bytes is a null reference.");
            retVal = false;
        }
        catch (ArgumentNullException) { }
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: ArgumentOutOfRangeException is not thrown when index is less than zero.");

        try
        {
            Byte[] bytes = new Byte[] {
                                         85,  84,  70,  56,  32,  69, 110,
                                         99, 111, 100, 105, 110, 103,  32,
                                         69, 120,  97, 109, 112, 108, 101};
            UTF8Encoding utf8 = new UTF8Encoding();
            int charCount = utf8.GetCharCount(bytes, -1, 8);

            TestLibrary.TestFramework.LogError("102.1", "ArgumentNullException is not thrown when index is less than zero..");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException) { }
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

        TestLibrary.TestFramework.BeginScenario("NegTest3: ArgumentOutOfRangeException is not thrown when count is less than zero.");

        try
        {
            Byte[] bytes = new Byte[] {
                                         85,  84,  70,  56,  32,  69, 110,
                                         99, 111, 100, 105, 110, 103,  32,
                                         69, 120,  97, 109, 112, 108, 101};
            UTF8Encoding utf8 = new UTF8Encoding();
            int charCount = utf8.GetCharCount(bytes, 2, -1);

            TestLibrary.TestFramework.LogError("103.1", "ArgumentNullException is not thrown when count is less than zero..");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException) { }
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

        TestLibrary.TestFramework.BeginScenario("NegTest4: ArgumentOutOfRangeException is not thrown when index and count do not denote a valid range in bytes.");

        try
        {
            Byte[] bytes = new Byte[] {
                                         85,  84,  70,  56,  32,  69, 110,
                                         99, 111, 100, 105, 110, 103,  32,
                                         69, 120,  97, 109, 112, 108, 101};
            UTF8Encoding utf8 = new UTF8Encoding();
            int charCount = utf8.GetCharCount(bytes, 2, bytes.Length);

            TestLibrary.TestFramework.LogError("104.1", "ArgumentNullException is not thrown whenindex and count do not denote a valid range in bytes.");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException) { }
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
        UTF8EncodingGetCharCount test = new UTF8EncodingGetCharCount();

        TestLibrary.TestFramework.BeginTestCase("UTF8EncodingGetCharCount");

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
