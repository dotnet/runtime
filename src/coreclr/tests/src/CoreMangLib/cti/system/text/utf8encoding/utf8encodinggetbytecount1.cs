// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

/// <summary>
/// GetByteCount(System.Char[],System.Int32,System.Int32) [v-jianq]
/// </summary>

public class UTF8EncodingGetByteCount1
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify method GetByteCount(Char[],Int32,Int32) with non-null char[]");

        try
        {
            Char[] chars = new Char[] {
                            '\u0023', 
                            '\u0025', 
                            '\u03a0', 
                            '\u03a3'  };

            UTF8Encoding utf8 = new UTF8Encoding();
            int byteCount = utf8.GetByteCount(chars, 1, 2);

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

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify method GetByteCount(Char[],Int32,Int32) with null char[]");

        try
        {
            Char[] chars = new Char[] { };

            UTF8Encoding utf8 = new UTF8Encoding();
            int byteCount = utf8.GetByteCount(chars, 0, 0);

            if (byteCount != 0)
            {
                TestLibrary.TestFramework.LogError("001.1", "Method GetByteCount Err.");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentNullException is not thrown when chars is a null reference");

        try
        {
            Char[] chars = null;

            UTF8Encoding utf8 = new UTF8Encoding();
            int byteCount = utf8.GetByteCount(chars, 0, 0);

            TestLibrary.TestFramework.LogError("101.1", "ArgumentNullException is not thrown when chars is a null reference.");
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
            Char[] chars = new Char[] {
                            '\u0023', 
                            '\u0025', 
                            '\u03a0', 
                            '\u03a3'  };

            UTF8Encoding utf8 = new UTF8Encoding();
            int byteCount = utf8.GetByteCount(chars, -1, 2);

            TestLibrary.TestFramework.LogError("102.1", "ArgumentOutOfRangeException is not thrown when index is less than zero.");
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
            Char[] chars = new Char[] {
                            '\u0023', 
                            '\u0025', 
                            '\u03a0', 
                            '\u03a3'  };

            UTF8Encoding utf8 = new UTF8Encoding();
            int byteCount = utf8.GetByteCount(chars, 1, -2);

            TestLibrary.TestFramework.LogError("103.1", "ArgumentOutOfRangeException is not thrown when count is less than zero.");
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

        TestLibrary.TestFramework.BeginScenario("NegTest4: ArgumentOutOfRangeException is not thrown when index and count do not denote a valid range in chars.");

        try
        {
            Char[] chars = new Char[] {
                            '\u0023', 
                            '\u0025', 
                            '\u03a0', 
                            '\u03a3'  };

            UTF8Encoding utf8 = new UTF8Encoding();
            int byteCount = utf8.GetByteCount(chars, 1, chars.Length);

            TestLibrary.TestFramework.LogError("104.1", "ArgumentOutOfRangeException is not thrown when index and count do not denote a valid range in chars.");
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
        UTF8EncodingGetByteCount1 test = new UTF8EncodingGetByteCount1();

        TestLibrary.TestFramework.BeginTestCase("UTF8EncodingGetByteCount1");

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
