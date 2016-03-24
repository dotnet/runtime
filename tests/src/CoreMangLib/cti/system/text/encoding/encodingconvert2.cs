// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

/// <summary>
/// Convert(System.Text.Encoding,System.Text.Encoding,System.Byte[],System.Int32,System.Int32)
/// </summary>

public class EncodingConvert2
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;
        retVal = NegTest6() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify method Convert when count == bytes.Length .");

        try
        {
            string unicodeStr = "test string .";

            Encoding ascii = Encoding.UTF8;
            Encoding unicode = Encoding.Unicode;

            byte[] unicodeBytes = unicode.GetBytes(unicodeStr);

            byte[] asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes,0,unicodeBytes.Length);

            char[] asciiChars = new char[ascii.GetCharCount(asciiBytes, 0, asciiBytes.Length)];
            ascii.GetChars(asciiBytes, 0, asciiBytes.Length, asciiChars, 0);
            string asciiStr = new string(asciiChars);

            if (unicodeStr != asciiStr)
            {
                TestLibrary.TestFramework.LogError("001.1", "Method Convert Err !");
                retVal = false;
                return retVal;
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

    public bool PosTest2()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify method Convert when count == 0 .");

        try
        {
            string unicodeStr = "test string .";

            Encoding ascii = Encoding.UTF8;
            Encoding unicode = Encoding.Unicode;

            byte[] unicodeBytes = unicode.GetBytes(unicodeStr);

            byte[] asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes, 0, 0);

            char[] asciiChars = new char[ascii.GetCharCount(asciiBytes, 0, asciiBytes.Length)];
            ascii.GetChars(asciiBytes, 0, asciiBytes.Length, asciiChars, 0);
            string asciiStr = new string(asciiChars);

            if (asciiStr != "")
            {
                TestLibrary.TestFramework.LogError("002.1", "Method Convert Err !");
                retVal = false;
                return retVal;
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify method Convert when bytes == null.");

        try
        {
            Encoding ascii = Encoding.UTF8;
            Encoding unicode = Encoding.Unicode;

            byte[] unicodeBytes = new byte[0];

            byte[] asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes, 0, 0);

            char[] asciiChars = new char[ascii.GetCharCount(asciiBytes, 0, asciiBytes.Length)];
            ascii.GetChars(asciiBytes, 0, asciiBytes.Length, asciiChars, 0);
            string asciiStr = new string(asciiChars);

            if (asciiStr != "")
            {
                TestLibrary.TestFramework.LogError("003.1", "Method Convert Err !");
                retVal = false;
                return retVal;
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
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentNullException is not thrown.");

        try
        {
            string unicodeStr = "test string .";

            Encoding ascii = null;
            Encoding unicode = Encoding.Unicode;

            byte[] unicodeBytes = unicode.GetBytes(unicodeStr);

            byte[] asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes, 0, unicodeBytes.Length);
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
            Encoding ascii = Encoding.UTF8;
            Encoding unicode = null;

            byte[] unicodeBytes = new byte[] { 1, 2, 3 };

            byte[] asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes, 0, unicodeBytes.Length);

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

        TestLibrary.TestFramework.BeginScenario("NegTest3: ArgumentNullException is not thrown.");

        try
        {
            Encoding ascii = Encoding.UTF8;
            Encoding unicode = Encoding.Unicode;

            byte[] unicodeBytes = null;

            byte[] asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes, 0, 2);

            TestLibrary.TestFramework.LogError("103.1", "ArgumentNullException is not thrown.");
            retVal = false;
        }
        catch (ArgumentNullException)
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
            string unicodeStr = "test string .";

            Encoding ascii = Encoding.UTF8;
            Encoding unicode = Encoding.Unicode;

            byte[] unicodeBytes = unicode.GetBytes(unicodeStr);

            byte[] asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes, -1, unicodeBytes.Length);
            TestLibrary.TestFramework.LogError("104.1", "ArgumentNullException is not thrown.");
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
            string unicodeStr = "test string .";

            Encoding ascii = Encoding.UTF8;
            Encoding unicode = Encoding.Unicode;

            byte[] unicodeBytes = unicode.GetBytes(unicodeStr);

            byte[] asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes, 0, unicodeBytes.Length + 1);
            TestLibrary.TestFramework.LogError("105.1", "ArgumentNullException is not thrown.");
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
            string unicodeStr = "test string .";

            Encoding ascii = Encoding.UTF8;
            Encoding unicode = Encoding.Unicode;

            byte[] unicodeBytes = unicode.GetBytes(unicodeStr);

            byte[] asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes, 0, -1);
            TestLibrary.TestFramework.LogError("106.1", "ArgumentNullException is not thrown.");
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

   
    #endregion
    #endregion

    public static int Main()
    {
        EncodingConvert2 test = new EncodingConvert2();

        TestLibrary.TestFramework.BeginTestCase("EncodingConvert2");

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
