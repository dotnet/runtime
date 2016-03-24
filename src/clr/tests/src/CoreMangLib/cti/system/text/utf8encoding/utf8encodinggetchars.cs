// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

/// <summary>
/// GetChars(System.Byte[],System.Int32,System.Int32,System.Char[],System.Int32) [v-jianq]
/// </summary>

public class UTF8EncodingGetChars
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
        retVal = NegTest5() && retVal;
        retVal = NegTest6() && retVal;
        retVal = NegTest7() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify method GetChars with non-null chars");

        try
        {
            Byte[] bytes;
            Char[] chars = new Char[] {
                            '\u0023', 
                            '\u0025', 
                            '\u03a0', 
                            '\u03a3'  };

            UTF8Encoding utf8 = new UTF8Encoding();

            int byteCount = utf8.GetByteCount(chars, 1, 2);
            bytes = new Byte[byteCount];
            int charsEncodedCount = utf8.GetChars(bytes, 1, 2, chars, 0);
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify method GetChars with null chars");

        try
        {
            Byte[] bytes;
            Char[] chars = new Char[] { };

            UTF8Encoding utf8 = new UTF8Encoding();

            int byteCount = utf8.GetByteCount(chars, 0, 0);
            bytes = new Byte[byteCount];
            int charsEncodedCount = utf8.GetChars(bytes, 0, 0, chars, 0);

            if (charsEncodedCount != 0)
            {
                TestLibrary.TestFramework.LogError("002.1", "Method GetChars Err.");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.1", "Unexpected exception: " + e);
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentNullException is not thrown when bytes is a null reference ");

        try
        {
            Byte[] bytes;
            Char[] chars = new Char[] {
                            '\u0023', 
                            '\u0025', 
                            '\u03a0', 
                            '\u03a3'  };

            UTF8Encoding utf8 = new UTF8Encoding();

            int byteCount = utf8.GetByteCount(chars, 1, 2);
            bytes = null;
            int charsEncodedCount = utf8.GetChars(bytes, 1, 2, chars, 0);

            TestLibrary.TestFramework.LogError("101.1", "ArgumentNullException is not thrown when bytes is a null reference ");
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: ArgumentNullException is not thrown when chars is a null reference  ");

        try
        {
            Byte[] bytes;
            Char[] chars = new Char[] {
                            '\u0023', 
                            '\u0025', 
                            '\u03a0', 
                            '\u03a3'  };

            UTF8Encoding utf8 = new UTF8Encoding();

            int byteCount = utf8.GetByteCount(chars, 1, 2);
            bytes = new Byte[byteCount];
            chars = null;
            int charsEncodedCount = utf8.GetChars(bytes, 1, 2, chars, 0);

            TestLibrary.TestFramework.LogError("102.1", "ArgumentNullException is not thrown when chars is a null reference  ");
            retVal = false;
        }
        catch (ArgumentNullException) { }
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

        TestLibrary.TestFramework.BeginScenario("NegTest3: ArgumentOutOfRangeException is not thrown when byteIndex is less than zero  ");

        try
        {
            Byte[] bytes;
            Char[] chars = new Char[] {
                            '\u0023', 
                            '\u0025', 
                            '\u03a0', 
                            '\u03a3'  };

            UTF8Encoding utf8 = new UTF8Encoding();

            int byteCount = utf8.GetByteCount(chars, 1, 2);
            bytes = new Byte[byteCount];
            int charsEncodedCount = utf8.GetChars(bytes, -1, 2, chars, 0);

            TestLibrary.TestFramework.LogError("103.1", "ArgumentOutOfRangeException is not thrown when charIndex is less than zero.");
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

        TestLibrary.TestFramework.BeginScenario("NegTest4: ArgumentOutOfRangeException is not thrown when byteCount is less than zero  ");

        try
        {
            Byte[] bytes;
            Char[] chars = new Char[] {
                            '\u0023', 
                            '\u0025', 
                            '\u03a0', 
                            '\u03a3'  };

            UTF8Encoding utf8 = new UTF8Encoding();

            int byteCount = utf8.GetByteCount(chars, 1, 2);
            bytes = new Byte[byteCount];
            int charsEncodedCount = utf8.GetChars(bytes, 1, -2, chars, 0);

            TestLibrary.TestFramework.LogError("104.1", "ArgumentOutOfRangeException is not thrown when byteIndex is less than zero.");
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

    public bool NegTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest5: ArgumentOutOfRangeException is not thrown when charIndex is less than zero.  ");

        try
        {
            Byte[] bytes;
            Char[] chars = new Char[] {
                            '\u0023', 
                            '\u0025', 
                            '\u03a0', 
                            '\u03a3'  };

            UTF8Encoding utf8 = new UTF8Encoding();

            int byteCount = utf8.GetByteCount(chars, 1, 2);
            bytes = new Byte[byteCount];
            int charsEncodedCount = utf8.GetChars(bytes, 1, 2, chars, -1);

            TestLibrary.TestFramework.LogError("105.1", "ArgumentOutOfRangeException is not thrown whenchar charIndex is less than zero..");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException) { }
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

        TestLibrary.TestFramework.BeginScenario("NegTest6: ArgumentOutOfRangeException is not thrown when byteIndex and byteCount do not denote a valid range in chars.  ");

        try
        {
            Byte[] bytes;
            Char[] chars = new Char[] {
                            '\u0023', 
                            '\u0025', 
                            '\u03a0', 
                            '\u03a3'  };

            UTF8Encoding utf8 = new UTF8Encoding();

            int byteCount = utf8.GetByteCount(chars, 1, 2);
            bytes = new Byte[byteCount];
            int charsEncodedCount = utf8.GetChars(bytes, 2, 2, chars, 1);

            TestLibrary.TestFramework.LogError("106.1", "ArgumentOutOfRangeException is not thrown when byteIndex and byteCount do not denote a valid range in chars.");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException) { }
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

        TestLibrary.TestFramework.BeginScenario("NegTest7: ArgumentException is not thrown when chars does not have enough capacity from charIndex to the end of the array to accommodate the resulting characters");

        try
        {
            Byte[] bytes;
            Char[] chars = new Char[] {
                            '\u0023', 
                            '\u0025', 
                            '\u03a0', 
                            '\u03a3'  };

            UTF8Encoding utf8 = new UTF8Encoding();

            int byteCount = utf8.GetByteCount(chars, 1, 2);
            bytes = new Byte[byteCount];
            int charsEncodedCount = utf8.GetChars(bytes, 1, 2, chars, chars.Length);

            TestLibrary.TestFramework.LogError("107.1", "ArgumentException is not thrown when byteIndex is not a valid index in bytes.");
            retVal = false;
        }
        catch (ArgumentException) { }
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
        UTF8EncodingGetChars test = new UTF8EncodingGetChars();

        TestLibrary.TestFramework.BeginTestCase("UTF8EncodingGetChars");

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
