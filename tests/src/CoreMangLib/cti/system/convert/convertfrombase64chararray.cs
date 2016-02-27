// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// FromBase64CharArray(System.Char[],System.Int32,System.Int32)
/// </summary>

public class ConvertFromBase64CharArray
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest2b() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify method FromBase64CharArray .");

        try
        {
            byte[] byteArray1 = new byte[256];
            byte[] byteArray2 = new byte[256];
            char[] charArray = new char[352];
            int charArrayLength;

            for (int x = 0; x < byteArray1.Length; x++)
                byteArray1[x] = (byte)x;

            charArrayLength = Convert.ToBase64CharArray(
                byteArray1,
                0,
                byteArray1.Length,
                charArray,
                0);

            byteArray2 = Convert.FromBase64CharArray(charArray, 0, charArrayLength);

            if (!ArraysAreEqual(byteArray1,byteArray2))
            {
                TestLibrary.TestFramework.LogError("001.1", "Method ChangeType Err.");
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
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: inArray is a null reference.");

        try
        {
            byte[] byteArray1 = new byte[256];
            byte[] byteArray2 = new byte[256];
            char[] charArray = new char[352];
            int charArrayLength;

            for (int x = 0; x < byteArray1.Length; x++)
                byteArray1[x] = (byte)x;

            charArrayLength = Convert.ToBase64CharArray(
                byteArray1,
                0,
                byteArray1.Length,
                charArray,
                0);

            charArray = null;
            byteArray2 = Convert.FromBase64CharArray(charArray, 0, charArrayLength);

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

        TestLibrary.TestFramework.BeginScenario("NegTest2: offset is less than 0.");

        try
        {
            byte[] byteArray1 = new byte[256];
            byte[] byteArray2 = new byte[256];
            char[] charArray = new char[352];
            int charArrayLength;

            for (int x = 0; x < byteArray1.Length; x++)
                byteArray1[x] = (byte)x;

            charArrayLength = Convert.ToBase64CharArray(
                byteArray1,
                0,
                byteArray1.Length,
                charArray,
                0);

            byteArray2 = Convert.FromBase64CharArray(charArray, -1, charArrayLength);

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

    public bool NegTest2b()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2b: length is less than 0.");

        try
        {
            byte[] byteArray1 = new byte[256];
            byte[] byteArray2 = new byte[256];
            char[] charArray = new char[352];
            int charArrayLength;

            for (int x = 0; x < byteArray1.Length; x++)
                byteArray1[x] = (byte)x;

            charArrayLength = Convert.ToBase64CharArray(
                byteArray1,
                0,
                byteArray1.Length,
                charArray,
                0);

            byteArray2 = Convert.FromBase64CharArray(charArray, 0, -1);

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

        TestLibrary.TestFramework.BeginScenario("NegTest3: offset plus length indicates a position not within inArray.");

        try
        {
            byte[] byteArray1 = new byte[256];
            byte[] byteArray2 = new byte[256];
            char[] charArray = new char[352];
            int charArrayLength;

            for (int x = 0; x < byteArray1.Length; x++)
                byteArray1[x] = (byte)x;

            charArrayLength = Convert.ToBase64CharArray(
                byteArray1,
                0,
                byteArray1.Length,
                charArray,
                0);

            byteArray2 = Convert.FromBase64CharArray(charArray, 0, charArrayLength + 10);

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

        TestLibrary.TestFramework.BeginScenario("NegTest4: The format of inArray is invalid. inArray contains a non-base 64 "+
                                                "character, more than two padding characters, or a non-white space character "+
                                                 "among the padding characters.");

        try
        {
            byte[] byteArray1 = new byte[256];
            byte[] byteArray2 = new byte[256];
            char[] charArray = new char[352];
            int charArrayLength;

            for (int x = 0; x < byteArray1.Length; x++)
                byteArray1[x] = (byte)x;

            charArrayLength = Convert.ToBase64CharArray(
                byteArray1,
                0,
                byteArray1.Length,
                charArray,
                0);

            charArray = new char[352 + 1];
            byteArray2 = Convert.FromBase64CharArray(charArray, 0, charArrayLength);

            TestLibrary.TestFramework.LogError("104.1", "FormatException is not thrown.");
            retVal = false;
        }
        catch (FormatException)
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
        ConvertFromBase64CharArray test = new ConvertFromBase64CharArray();

        TestLibrary.TestFramework.BeginTestCase("ConvertFromBase64CharArray");

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
    private bool ArraysAreEqual(byte[] a1, byte[] a2)
    {
    if (a1.Length != a2.Length) return false;
    for (int i = 0; i < a1.Length; i++)
        if (a1[i] != a2[i]) return false;
    return true;
    }

    #endregion
}
