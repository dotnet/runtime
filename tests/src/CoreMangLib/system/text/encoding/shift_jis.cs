// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;
using TestLibrary;

public class Shift_JisTest
{
    public static int Main(string[] args)
    {
        Shift_JisTest test = new Shift_JisTest();
        TestLibrary.TestFramework.BeginTestCase("Testing Shift_Jis encoding support in CoreCLR");

        if (test.RunTests())
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.Logging.WriteLine("PASS");
            return 100;
        }
        else
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.Logging.WriteLine("FAIL");
            return 0;
        }
    }

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.Logging.WriteLine("[Positive]");
        // We now support only Unicode and UTF8 encodings
        //retVal = PosTest1() && retVal;
        //retVal = PosTest2() && retVal;
        //retVal = PosTest3() && retVal;
        //retVal = PosTest4() && retVal;
        retVal = NegTest1() && retVal;

        return retVal;
    }

    public bool NegTest1()
    {
        bool ret = true;
        TestLibrary.TestFramework.BeginScenario("Creating the shift_jis encoding");

        try
        {
            Encoding enc = Encoding.GetEncoding("shift_jis");

            ret = false;
            TestFramework.LogError("00F", "Encoding created unexpectedly. Expected argument exception. Actual: Create encoding with name: " + enc.WebName);
        }
        catch (NotSupportedException)
        {
            // Expected
        }
		catch (ArgumentException)
		{
			// Expected
		}
		catch (Exception exc)
        {
            ret = false;
            TestFramework.LogError("010", "Unexpected error: " + exc.ToString());
        }
        return ret;
    }

    public bool PosTest1()
    {
        bool ret = true;
        TestLibrary.TestFramework.BeginScenario("Creating the shift_jis encoding");

        try
        {
            Encoding enc2 = Encoding.GetEncoding("shift_jis");
            if (enc2.WebName != "shift_jis")
            {
                ret = false;
                TestFramework.LogError("002", "Error creating encoding. Web name not as expected. Expected: shift_jis Actual: " + enc2.WebName);
            }

            Encoding enc3 = Encoding.GetEncoding("sHiFT_JIs");
            if (enc3.WebName != "shift_jis")
            {
                ret = false;
                TestFramework.LogError("003", "Error creating encoding. Web name not as expected. Expected: shift_jis Actual: " + enc3.WebName);
            }
        }
        catch (Exception exc)
        {
            ret = false;
            TestFramework.LogError("004", "Unexpected error: " + exc.ToString());
        }
        return ret;
    }

    public bool PosTest2()
    {
        bool ret = true;
        TestLibrary.TestFramework.BeginScenario("Encoding strings with the shift_jis encoding");

        try
        {
            Encoding enc = Encoding.GetEncoding("shift_jis");

            string str = "ABc";
            byte[] bytes = enc.GetBytes(str);
            byte[] expected = new byte[] { 0x41, 0x42, 0x63 };

            if (!Utilities.CompareBytes(bytes, expected))
            {
                ret = false;
                TestFramework.LogError("005", "Encoding str -> bytes not as expected. Str: " + str + " Expected bytes: " + Utilities.ByteArrayToString(expected) + " Actual bytes: " + Utilities.ByteArrayToString(bytes));
            }

            str = "";
            bytes = enc.GetBytes(str);
            expected = new byte[0];

            if (!Utilities.CompareBytes(bytes, expected))
            {
                ret = false;
                TestFramework.LogError("006", "Encoding str -> bytes not as expected. Str: " + str + " Expected bytes: " + Utilities.ByteArrayToString(expected) + " Actual bytes: " + Utilities.ByteArrayToString(bytes));
            }

            str = "A\xff70\x3000\x00b6\x25ef\x044f\x9adc\x9ed1";
            bytes = enc.GetBytes(str);
            expected = new byte[] { 0x41, 0xb0, 0x81, 0x40, 0x81, 0xf7, 0x81, 0xfc, 0x84, 0x91, 0xfc, 0x40, 0xfc, 0x4b };

            if (!Utilities.CompareBytes(bytes, expected))
            {
                ret = false;
                TestFramework.LogError("007", "Encoding str -> bytes not as expected. Str: " + str + " Expected bytes: " + Utilities.ByteArrayToString(expected) + " Actual bytes: " + Utilities.ByteArrayToString(bytes));
            }
        }
        catch (Exception exc)
        {
            ret = false;
            TestFramework.LogError("008", "Unexpected error: " + exc.ToString());
        }
        return ret;
    }

    public bool PosTest3()
    {
        bool ret = true;
        TestLibrary.TestFramework.BeginScenario("Encoding char[]s with the shift_jis encoding");

        try
        {
            Encoding enc = Encoding.GetEncoding("shift_jis");

            char[] str = new char[] { 'A', 'B', 'c' };
            byte[] bytes = enc.GetBytes(str);
            byte[] expected = new byte[] { 0x41, 0x42, 0x63 };

            if (!Utilities.CompareBytes(bytes, expected))
            {
                ret = false;
                TestFramework.LogError("009", "Encoding char[] -> bytes not as expected. Str: " + new string(str) + " Expected bytes: " + Utilities.ByteArrayToString(expected) + " Actual bytes: " + Utilities.ByteArrayToString(bytes));
            }

            str = new char[0];
            bytes = enc.GetBytes(str);
            expected = new byte[0];

            if (!Utilities.CompareBytes(bytes, expected))
            {
                ret = false;
                TestFramework.LogError("00A", "Encoding char[] -> bytes not as expected. Str: " + new string(str) + " Expected bytes: " + Utilities.ByteArrayToString(expected) + " Actual bytes: " + Utilities.ByteArrayToString(bytes));
            }

            str = new char[] { 'A', '\xff70', '\x3000', '\x00b6', '\x25ef', '\x044f', '\x9adc', '\x9ed1' };
            bytes = enc.GetBytes(str);
            expected = new byte[] { 0x41, 0xb0, 0x81, 0x40, 0x81, 0xf7, 0x81, 0xfc, 0x84, 0x91, 0xfc, 0x40, 0xfc, 0x4b };

            if (!Utilities.CompareBytes(bytes, expected))
            {
                ret = false;
                TestFramework.LogError("00B", "Encoding char[] -> bytes not as expected. Str: " + new string(str) + " Expected bytes: " + Utilities.ByteArrayToString(expected) + " Actual bytes: " + Utilities.ByteArrayToString(bytes));
            }
        }
        catch (Exception exc)
        {
            ret = false;
            TestFramework.LogError("00C", "Unexpected error: " + exc.ToString());
        }
        return ret;
    }

    public bool PosTest4()
    {
        bool ret = true;
        TestLibrary.TestFramework.BeginScenario("Decoding byte[]s with the shift_jis encoding");

        try
        {
            Encoding enc = Encoding.GetEncoding("shift_jis");

            byte[] bytes = { 0x87, 0x90 };
            char[] expected = new char[] {'\x2252'};

            char[] actual = enc.GetChars(bytes);

            if (!Utilities.CompareChars(actual, expected))
            {
                ret = false;
                TestFramework.LogError("00D", "Decoding byte[] -> char[] not as expected! Expected: 0x8786 Actual ");
                foreach (char c in actual) Logging.Write("0x" + ((int)c).ToString("x") + " ");
            }
        }
        catch (Exception exc)
        {
            ret = false;
            TestFramework.LogError("00E", "Unexpected error: " + exc.ToString());
        }
        return ret;
    }

}
